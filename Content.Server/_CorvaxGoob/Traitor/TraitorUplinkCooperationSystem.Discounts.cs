// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Shared.ManifestListings;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Traitor.Cooperation;

/// <summary>
/// Grants one-shot cooperation discounts and removes their cloned sale listings after purchase.
/// </summary>
public sealed partial class TraitorUplinkCooperationSystem
{
    // Tags cloned sale listings so the purchase hook can remove only our one-shot manual discounts.
    private const string ManualDiscountMarker = "TraitorUplinkManualDiscount";
    private const string RadioImplanterListingId = "UplinkRadioImplanter";
    private const string EmagListingId = "UplinkEmag";
    private const float FreeFinalCostMultiplier = 0f;

    // The multiplier is the final price: 0.6 means the buyer pays 60%, so the discount is 40%.
    private const float EmagFinalCostMultiplier = 0.6f;

    private static readonly ProtoId<CurrencyPrototype> TelecrystalCurrency = "Telecrystal";

    // Random cooperation discounts should avoid special shops, deterministic rewards, and high-variance bundles.
    private static readonly string[] RandomDiscountExclusionFragments =
    [
        "UplinkSales",
        RadioImplanterListingId,
        EmagListingId,
        "Bundle",
        "Surplus",
    ];

    /// <summary>
    /// Grants all rewards for one device after a successful pairing and refreshes the store UI once.
    /// </summary>
    private void GrantPairingRewards(Entity<TraitorUplinkCooperationComponent> uplink, int pairingCount)
    {
        if (!TryGetUplinkStore(uplink, out var store))
            return;

        var changed = false;

        if (pairingCount == 1)
        {
            changed |= TryGrantPrototypeDiscount(uplink, store, RadioImplanterListingId, FreeFinalCostMultiplier);
            changed |= TryGrantPrototypeDiscount(uplink, store, EmagListingId, EmagFinalCostMultiplier);
        }

        changed |= GrantRandomDiscounts(uplink, store, GetRandomDiscountCount(pairingCount));

        if (!changed)
            return;

        Dirty(store);
        _store.UpdateUserInterface(store.Comp.AccountOwner ?? uplink.Comp.OwnerMindId, store.Owner, store.Comp);
    }

    /// <summary>
    /// Returns how many random discounts this device should receive for its current unique pairing count.
    /// </summary>
    private static int GetRandomDiscountCount(int pairingCount)
    {
        return pairingCount switch
        {
            1 => 3,
            2 => 2,
            3 => 2,
            4 => 1,
            _ => 0,
        };
    }

    /// <summary>
    /// Chooses random eligible uplink items and creates temporary discounted sale listings for them.
    /// </summary>
    private bool GrantRandomDiscounts(
        Entity<TraitorUplinkCooperationComponent> uplink,
        Entity<StoreComponent> store,
        int count)
    {
        if (count <= 0)
            return false;

        // Use the store account owner, or the registered traitor owner if the store has no account owner.
        // The current holder can be different; rewards are added to this device's catalog.
        var buyer = store.Comp.AccountOwner ?? uplink.Comp.OwnerMindId ?? uplink.Owner;

        // Start from the store's currently available listings, then remove sale entries,
        // special listings, and items this cooperation system has already discounted.
        var available = _store.GetAvailableListings(buyer, uplink.Owner, store.Comp)
            .Where(listing => IsEligibleForRandomDiscount(listing, store.Comp, uplink.Comp))
            .ToList();
        var changed = false;

        while (count > 0 && _random.TryPickAndTake(available, out var listing))
        {
            if (!listing.Cost.TryGetValue(TelecrystalCurrency, out var oldCost))
                continue;

            var saleCost = GetRandomSaleCost(oldCost, store.Comp);
            if (saleCost.Int() >= oldCost.Int() || !TryGrantDiscount(uplink, store, listing, saleCost))
                continue;

            changed = true;
            count--;
        }

        return changed;
    }

    /// <summary>
    /// Uses the uplink's standard sale multiplier range to calculate a discounted telecrystal price.
    /// </summary>
    private FixedPoint2 GetRandomSaleCost(FixedPoint2 oldCost, StoreComponent store)
    {
        var multiplier = _random.NextFloat() * (store.Sales.MaxMultiplier - store.Sales.MinMultiplier)
            + store.Sales.MinMultiplier;
        return FixedPoint2.New(Math.Max(1, (int) MathF.Round(oldCost.Float() * multiplier)));
    }

    /// <summary>
    /// Filters the random discount pool to normal catalog items that have not already been discounted on this device.
    /// </summary>
    private bool IsEligibleForRandomDiscount(
        ListingData listing,
        StoreComponent store,
        TraitorUplinkCooperationComponent uplink)
    {
        if (listing.SaleBlacklist ||
            listing.DiscountValue > 0 ||
            listing.ProductEvent != null ||
            listing.RaiseProductEventOnUser)
            return false;

        if (!listing.Cost.TryGetValue(TelecrystalCurrency, out var cost) || cost <= FixedPoint2.New(1))
            return false;

        if (listing.Categories.Contains(store.Sales.SalesCategory) ||
            uplink.DiscountedListingIds.Contains(listing.ID))
            return false;

        var productEntity = listing.ProductEntity?.ToString() ?? string.Empty;
        return !RandomDiscountExclusionFragments.Any(exclusion =>
            listing.ID.Contains(exclusion, StringComparison.OrdinalIgnoreCase) ||
            productEntity.Contains(exclusion, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Grants a fixed one-shot discount from a known listing prototype, such as radio implanter or emag.
    /// </summary>
    private bool TryGrantPrototypeDiscount(
        Entity<TraitorUplinkCooperationComponent> uplink,
        Entity<StoreComponent> store,
        string listingId,
        float finalCostMultiplier)
    {
        if (!_prototype.TryIndex<ListingPrototype>(listingId, out var listing) ||
            !listing.Cost.TryGetValue(TelecrystalCurrency, out var oldCost))
            return false;

        var saleCost = finalCostMultiplier <= 0f
            ? FixedPoint2.Zero
            : FixedPoint2.New(Math.Max(1, (int) MathF.Round(oldCost.Float() * finalCostMultiplier)));
        return TryGrantDiscount(uplink, store, listing, saleCost);
    }

    /// <summary>
    /// Creates a temporary one-use discounted copy of a normal uplink listing.
    /// The original listing stays in the catalog at full price, while the copy appears in the sale category.
    /// </summary>
    private bool TryGrantDiscount(
        Entity<TraitorUplinkCooperationComponent> uplink,
        Entity<StoreComponent> store,
        ListingData source,
        FixedPoint2 saleCost)
    {
        if (uplink.Comp.DiscountedListingIds.Contains(source.ID))
            return false;

        if (!source.Cost.TryGetValue(TelecrystalCurrency, out var oldCost) ||
            saleCost < FixedPoint2.Zero ||
            saleCost >= oldCost)
            return false;

        // Discounts are cloned into the sale category, leaving the normal catalog listing at full price.
        var sale = (ListingData) source.Clone();
        sale.Categories = [store.Comp.Sales.SalesCategory];
        sale.OldCost = source.Cost.ToDictionary(x => x.Key, x => x.Value);
        sale.Cost = source.Cost.ToDictionary(x => x.Key, x => x.Value);
        sale.Cost[TelecrystalCurrency] = saleCost;
        sale.SaleCost = sale.Cost.ToDictionary(x => x.Key, x => x.Value);
        sale.PurchaseAmount = 0;
        sale.SaleLimit = 1;
        sale.DiscountValue = saleCost <= FixedPoint2.Zero
            ? 100
            : 100 - (saleCost / oldCost * 100).Int();
        sale.Components = source.Components.ToList();
        sale.Components.Add(ManualDiscountMarker);

        if (!store.Comp.Listings.Add(sale))
            return false;

        // Store the original listing ID, not the cloned sale listing, to prevent reissuing the same reward later.
        uplink.Comp.DiscountedListingIds.Add(source.ID);
        return true;
    }

    /// <summary>
    /// Removes a consumed manual discount clone after it is bought from an uplink store.
    /// </summary>
    private void OnListingPurchased(
        Entity<TraitorUplinkPurchaseRelayComponent> _,
        ref ListingPurchasedEvent args)
    {
        if (!TryComp<TraitorUplinkCooperationComponent>(args.Store, out var uplinkComp) ||
            !TryGetUplinkStore((args.Store, uplinkComp), out var store))
            return;

        // Only manual discount clones are consumed after purchase; ordinary listings must remain available.
        if (!args.Data.Components.Contains(ManualDiscountMarker) ||
            !store.Comp.Listings.Remove(args.Data))
            return;

        Dirty(store);
    }
}

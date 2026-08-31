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

    private bool GrantRandomDiscounts(
        Entity<TraitorUplinkCooperationComponent> uplink,
        Entity<StoreComponent> store,
        int count)
    {
        if (count <= 0)
            return false;

        var buyer = store.Comp.AccountOwner ?? uplink.Comp.OwnerMindId ?? uplink.Owner;
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

    private FixedPoint2 GetRandomSaleCost(FixedPoint2 oldCost, StoreComponent store)
    {
        var multiplier = _random.NextFloat() * (store.Sales.MaxMultiplier - store.Sales.MinMultiplier)
            + store.Sales.MinMultiplier;
        return FixedPoint2.New(Math.Max(1, (int) MathF.Round(oldCost.Float() * multiplier)));
    }

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

        uplink.Comp.DiscountedListingIds.Add(source.ID);
        return true;
    }

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

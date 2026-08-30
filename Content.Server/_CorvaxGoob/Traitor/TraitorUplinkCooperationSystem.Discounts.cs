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
    private const float EmagFinalCostMultiplier = 0.6f;
    private const int FirstPairingMinimumListingCost = 30;
    private const int SecondAndThirdPairingMinimumListingCost = 40;
    private const int FourthPairingMinimumListingCost = 60;

    // A 0.3-0.5 final price multiplier means the fourth-pairing high-value reward is discounted by 50-70%.
    private const float FourthPairingMinCostMultiplier = 0.3f;
    private const float FourthPairingMaxCostMultiplier = 0.5f;

    private static readonly ProtoId<CurrencyPrototype> TelecrystalCurrency = "Telecrystal";
    // Random cooperation discounts should avoid special shops, deterministic rewards, and high-variance bundles.
    private static readonly string[] RandomDiscountExclusionFragments =
    [
        "UplinkSales",
        RadioImplanterListingId,
        EmagListingId,
        "UplinkImplantExtractor",
        "Bundle",
        "Surplus",
    ];

    private void GrantCooperationDiscounts(Entity<TraitorUplinkCooperationComponent> uplink, int pairingCount)
    {
        switch (pairingCount)
        {
            case 1:
                // One high-value listing plus two unrestricted random listings.
                TryGrantRandomDiscount(uplink, FirstPairingMinimumListingCost);
                GrantRandomDiscounts(uplink, 2);
                break;
            case 2:
                // One high-value listing plus one unrestricted random listing.
                TryGrantRandomDiscount(uplink, SecondAndThirdPairingMinimumListingCost);
                GrantRandomDiscounts(uplink, 1);
                break;
            case 3:
                // Two high-value listings plus one unrestricted random listing.
                TryGrantRandomDiscount(uplink, SecondAndThirdPairingMinimumListingCost);
                TryGrantRandomDiscount(uplink, SecondAndThirdPairingMinimumListingCost);
                GrantRandomDiscounts(uplink, 1);
                break;
            case 4:
                // The final reward targets an expensive listing and guarantees a 50-70% discount.
                TryGrantRandomDiscount(uplink,
                    FourthPairingMinimumListingCost,
                    FourthPairingMinCostMultiplier,
                    FourthPairingMaxCostMultiplier);
                break;
        }
    }

    private void GrantRadioImplanterDiscount(Entity<TraitorUplinkCooperationComponent> uplink)
    {
        if (uplink.Comp.DiscountedListingIds.Contains(RadioImplanterListingId))
            return;

        if (!TryGetUplinkStore(uplink, out var store))
            return;

        TryGrantPrototypeDiscount(uplink, store, RadioImplanterListingId, FixedPoint2.Zero);
    }

    private void GrantEmagDiscount(Entity<TraitorUplinkCooperationComponent> uplink)
    {
        if (uplink.Comp.DiscountedListingIds.Contains(EmagListingId))
            return;

        if (!TryGetUplinkStore(uplink, out var store))
            return;

        if (!_prototype.TryIndex<ListingPrototype>(EmagListingId, out var listing))
            return;

        if (!listing.Cost.TryGetValue(TelecrystalCurrency, out var oldCost))
            return;

        var discountedCost = Math.Max(1, (int) MathF.Round(oldCost.Float() * EmagFinalCostMultiplier));
        var saleCost = FixedPoint2.New(discountedCost);
        TryGrantDiscount(uplink, store, listing, saleCost);
    }

    private void GrantRandomDiscounts(Entity<TraitorUplinkCooperationComponent> uplink, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (!TryGrantRandomDiscount(uplink))
                return;
        }
    }

    private bool TryGrantRandomDiscount(
        Entity<TraitorUplinkCooperationComponent> uplink,
        int? minimumCost = null,
        float? minCostMultiplier = null,
        float? maxCostMultiplier = null)
    {
        if (!TryGetUplinkStore(uplink, out var store))
            return false;

        var buyer = store.Comp.AccountOwner ?? uplink.Comp.OwnerMindId ?? uplink.Owner;
        var available = _store.GetAvailableListings(buyer, uplink.Owner, store.Comp)
            .Where(listing => IsEligibleForRandomDiscount(listing, store.Comp, uplink.Comp, minimumCost))
            .ToList();
        _random.Shuffle(available);

        foreach (var listing in available)
        {
            if (!listing.Cost.TryGetValue(TelecrystalCurrency, out var oldCost))
                continue;

            var saleCost = GetRandomSaleCost(oldCost, store.Comp, minCostMultiplier, maxCostMultiplier);
            if (saleCost.Int() >= oldCost.Int())
                continue;

            if (TryGrantDiscount(uplink, store, listing, saleCost))
                return true;
        }

        return false;
    }

    private FixedPoint2 GetRandomSaleCost(
        FixedPoint2 oldCost,
        StoreComponent store,
        float? minCostMultiplier = null,
        float? maxCostMultiplier = null)
    {
        var minMultiplier = minCostMultiplier ?? store.Sales.MinMultiplier;
        var maxMultiplier = maxCostMultiplier ?? store.Sales.MaxMultiplier;
        var multiplier = _random.NextFloat() * (maxMultiplier - minMultiplier) + minMultiplier;
        return FixedPoint2.New(Math.Max(1, (int) MathF.Round(oldCost.Float() * multiplier)));
    }

    private bool IsEligibleForRandomDiscount(
        ListingData listing,
        StoreComponent store,
        TraitorUplinkCooperationComponent uplink,
        int? minimumCost)
    {
        if (listing.SaleBlacklist ||
            listing.DiscountValue > 0 ||
            listing.ProductEvent != null ||
            listing.RaiseProductEventOnUser)
            return false;

        if (!listing.Cost.TryGetValue(TelecrystalCurrency, out var cost) || cost <= FixedPoint2.New(1))
            return false;

        if (minimumCost != null && cost < FixedPoint2.New(minimumCost.Value))
            return false;

        if (listing.Categories.Contains(store.Sales.SalesCategory))
            return false;

        if (uplink.DiscountedListingIds.Contains(listing.ID))
            return false;

        var productEntity = listing.ProductEntity?.ToString() ?? string.Empty;

        if (RandomDiscountExclusionFragments.Any(exclusion =>
                listing.ID.Contains(exclusion, StringComparison.OrdinalIgnoreCase) ||
                productEntity.Contains(exclusion, StringComparison.OrdinalIgnoreCase)))
            return false;

        return true;
    }

    private bool TryGrantPrototypeDiscount(
        Entity<TraitorUplinkCooperationComponent> uplink,
        Entity<StoreComponent> store,
        string listingId,
        FixedPoint2 saleCost)
    {
        if (!_prototype.TryIndex<ListingPrototype>(listingId, out var listing))
            return false;

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
        Dirty(store);
        _store.UpdateUserInterface(store.Comp.AccountOwner ?? uplink.Comp.OwnerMindId, store.Owner, store.Comp);
        return true;
    }

    private void OnListingPurchased(Entity<TraitorUplinkCooperationComponent> ent, ref ListingPurchasedEvent args)
    {
        if (!TryGetUplinkStore(ent, out var store))
            return;

        // Only manual discount clones are consumed after purchase; ordinary listings must remain available.
        if (!args.Data.Components.Contains(ManualDiscountMarker))
            return;

        if (!store.Comp.Listings.Remove(args.Data))
            return;

        Dirty(store);
    }
}

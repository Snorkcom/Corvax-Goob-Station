// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Shared.ManifestListings;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

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
    // A 0.3-0.5 final price multiplier means the fourth-link high-value reward is discounted by 50-70%.
    private const float FourthLinkMinCostMultiplier = 0.3f;
    private const float FourthLinkMaxCostMultiplier = 0.5f;

    private static readonly ProtoId<CurrencyPrototype> TelecrystalCurrency = "Telecrystal";
    // Random cooperation discounts should avoid special shops, deterministic rewards, and high-variance bundles.
    private static readonly string[] RandomDiscountExclusions =
    [
        "UplinkSales",
        RadioImplanterListingId,
        EmagListingId,
        "UplinkImplantExtractor",
        "CrateSyndicateSurplusBundle",
        "CrateSyndicateSuperSurplusBundle",
    ];

    private void GrantCooperationDiscounts(Entity<TraitorUplinkCooperationComponent> uplink, int uniqueLinks)
    {
        switch (uniqueLinks)
        {
            case 1:
                TryGrantRandomDiscount(uplink, 30);
                GrantRandomDiscounts(uplink, 2);
                break;
            case 2:
                TryGrantRandomDiscount(uplink, 40);
                GrantRandomDiscounts(uplink, 1);
                break;
            case 3:
                TryGrantRandomDiscount(uplink, 40);
                TryGrantRandomDiscount(uplink, 40);
                GrantRandomDiscounts(uplink, 1);
                break;
            case 4:
                TryGrantRandomDiscount(uplink, 60, FourthLinkMinCostMultiplier, FourthLinkMaxCostMultiplier);
                break;
        }
    }

    private void GrantRadioImplanterDiscount(Entity<TraitorUplinkCooperationComponent> uplink)
    {
        if (uplink.Comp.RadioImplanterDiscountGranted)
            return;

        if (!TryComp<StoreComponent>(uplink.Owner, out var store))
            return;

        if (TryGrantPrototypeDiscount((uplink.Owner, store), uplink.Comp, RadioImplanterListingId, FixedPoint2.Zero))
        {
            uplink.Comp.RadioImplanterDiscountGranted = true;
        }
    }

    private void GrantEmagDiscount(Entity<TraitorUplinkCooperationComponent> uplink)
    {
        if (uplink.Comp.EmagDiscountGranted)
            return;

        if (!TryComp<StoreComponent>(uplink.Owner, out var store))
            return;

        if (!_prototype.TryIndex<ListingPrototype>(EmagListingId, out var listing))
            return;

        if (!listing.Cost.TryGetValue(TelecrystalCurrency, out var oldCost))
            return;

        var saleCost = FixedPoint2.New(Math.Max(1, (int) MathF.Round(oldCost.Float() * 0.6f)));
        if (TryGrantDiscount((uplink.Owner, store), uplink.Comp, listing, saleCost))
        {
            uplink.Comp.EmagDiscountGranted = true;
        }
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
        if (!TryComp<StoreComponent>(uplink.Owner, out var store))
            return false;

        var buyer = store.AccountOwner ?? uplink.Comp.OwnerMind ?? uplink.Owner;
        var available = _store.GetAvailableListings(buyer, uplink.Owner, store)
            .Where(listing => IsEligibleForRandomDiscount(listing, store, uplink.Comp, minimumCost))
            .OrderBy(_ => _random.Next())
            .ToList();

        foreach (var listing in available)
        {
            if (!listing.Cost.TryGetValue(TelecrystalCurrency, out var oldCost))
                continue;

            var saleCost = GetRandomSaleCost(oldCost, store, minCostMultiplier, maxCostMultiplier);
            if (saleCost.Int() >= oldCost.Int())
                continue;

            if (TryGrantDiscount((uplink.Owner, store), uplink.Comp, listing, saleCost))
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
        if (listing.SaleBlacklist || listing.DiscountValue > 0 || listing.ProductEvent != null || listing.RaiseProductEventOnUser)
            return false;

        if (!listing.Cost.TryGetValue(TelecrystalCurrency, out var cost) || cost <= FixedPoint2.New(1))
            return false;

        if (minimumCost != null && cost < FixedPoint2.New(minimumCost.Value))
            return false;

        if (listing.Categories.Contains(store.Sales.SalesCategory))
            return false;

        if (uplink.GrantedManualDiscountListings.Contains(listing.ID))
            return false;

        var productEntity = listing.ProductEntity?.ToString() ?? string.Empty;

        if (RandomDiscountExclusions.Any(exclusion =>
                listing.ID.Contains(exclusion, StringComparison.OrdinalIgnoreCase) ||
                productEntity.Contains(exclusion, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (listing.ID.Contains("Bundle", StringComparison.OrdinalIgnoreCase) ||
            listing.ID.Contains("Surplus", StringComparison.OrdinalIgnoreCase) ||
            productEntity.Contains("Bundle", StringComparison.OrdinalIgnoreCase) ||
            productEntity.Contains("Surplus", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private bool TryGrantPrototypeDiscount(
        Entity<StoreComponent> store,
        TraitorUplinkCooperationComponent uplink,
        string listingId,
        FixedPoint2 saleCost)
    {
        if (!_prototype.TryIndex<ListingPrototype>(listingId, out var listing))
            return false;

        return TryGrantDiscount(store, uplink, listing, saleCost);
    }

    private bool TryGrantDiscount(
        Entity<StoreComponent> store,
        TraitorUplinkCooperationComponent uplink,
        ListingData source,
        FixedPoint2 saleCost)
    {
        if (uplink.GrantedManualDiscountListings.Contains(source.ID))
            return false;

        if (!source.Cost.TryGetValue(TelecrystalCurrency, out var oldCost) || oldCost < saleCost)
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

        uplink.GrantedManualDiscountListings.Add(source.ID);
        Dirty(store);
        _store.UpdateUserInterface(store.Comp.AccountOwner ?? uplink.OwnerMind, store.Owner, store.Comp);
        return true;
    }

    private void OnListingPurchased(Entity<TraitorUplinkCooperationComponent> ent, ref ListingPurchasedEvent args)
    {
        if (!TryComp<StoreComponent>(ent.Owner, out var store))
            return;

        // Only manual discount clones are consumed after purchase; ordinary listings must remain available.
        if (!args.Data.Components.Contains(ManualDiscountMarker))
            return;

        store.Listings.Remove(args.Data);
        Dirty(ent.Owner, store);
    }
}

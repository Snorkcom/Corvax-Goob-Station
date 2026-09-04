// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Shared.ManifestListings;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Traitor.Cooperation;

/// <summary>
/// Handles pairing rewards by adding one-use sale copies to existing traitor uplink stores.
/// </summary>
public sealed partial class TraitorUplinkCooperationSystem
{
    private const string ManualDiscountMarker = "TraitorUplinkManualDiscount";
    private const string RadioImplanterListingId = "UplinkRadioImplanter";
    private const string EmagListingId = "UplinkEmag";
    private const int EmagDiscountPercent = 40;

    private static readonly ProtoId<CurrencyPrototype> TelecrystalCurrency = "Telecrystal";

    private readonly record struct PairingDiscountRule(
        int RandomDiscountCount,
        int GuaranteedMinTelecrystalCost,
        int[] GuaranteedDiscountPercents);

    private static readonly int[] StandardGuaranteedDiscountPercents = [30, 40, 50, 60];
    private static readonly int[] FourthPairingGuaranteedDiscountPercents = [60, 70, 80];

    // Reward table for unique pairings on one uplink device.
    // Index 0 is the first successful pairing, index 1 is the second, and so on.
    // Each rule grants one guaranteed discount by minimum item cost, plus the listed amount of ordinary random discounts.
    private static readonly PairingDiscountRule[] PairingDiscountRules =
    [
        new(2, 40, StandardGuaranteedDiscountPercents),
        new(1, 40, StandardGuaranteedDiscountPercents),
        new(1, 40, StandardGuaranteedDiscountPercents),
        new(0, 50, FourthPairingGuaranteedDiscountPercents),
    ];

    // Random cooperation discounts should avoid special shops, deterministic rewards, and high-variance bundles.
    private static readonly string[] RandomDiscountExclusionFragments =
    [
        RadioImplanterListingId,
        EmagListingId,
        "Bundle",
        "Surplus",
    ];

    // Adds fixed first-pairing rewards and table-driven random rewards after a unique pairing.
    private void GrantPairingRewards(Entity<TraitorUplinkCooperationComponent> uplink, int pairingCount)
    {
        if (!TryGetUplinkStore(uplink, out var store))
            return;

        var changed = false;

        if (pairingCount == 1)
        {
            changed |= TryGrantPrototypeDiscount(uplink, store, RadioImplanterListingId, 100);
            changed |= TryGrantPrototypeDiscount(uplink, store, EmagListingId, EmagDiscountPercent);
        }

        if (pairingCount > 0 && pairingCount <= PairingDiscountRules.Length)
        {
            var rule = PairingDiscountRules[pairingCount - 1];
            var available = GetEligibleRandomDiscountListings(uplink, store);
            changed |= TryGrantGuaranteedDiscount(uplink, store, available, rule);
            changed |= GrantRandomDiscounts(uplink, store, available, rule.RandomDiscountCount);
        }

        if (!changed)
            return;

        _store.UpdateUserInterface(store.Comp.AccountOwner ?? uplink.Comp.OwnerMindId, store.Owner, store.Comp);
    }

    // Tries to add one guaranteed discount for an item that meets the rule's minimum TC cost.
    private bool TryGrantGuaranteedDiscount(
        Entity<TraitorUplinkCooperationComponent> uplink,
        Entity<StoreComponent> store,
        List<ListingData> available,
        PairingDiscountRule rule)
    {
        var expensiveListings = available
            .Where(listing => HasMinimumTelecrystalCost(listing, rule.GuaranteedMinTelecrystalCost))
            .ToList();

        while (expensiveListings.Count > 0)
        {
            var listing = PickAndTake(expensiveListings);
            available.Remove(listing);

            if (!TryGetTelecrystalCost(listing, out var oldCost))
                continue;

            var discountPercent = Pick(rule.GuaranteedDiscountPercents);
            var saleCost = GetSaleCostByDiscountPercent(oldCost, discountPercent);
            if (TryGrantDiscount(uplink, store, listing, saleCost))
                return true;
        }

        return false;
    }

    // Adds ordinary random discounts from the remaining pool without repeating this batch's picks.
    private bool GrantRandomDiscounts(
        Entity<TraitorUplinkCooperationComponent> uplink,
        Entity<StoreComponent> store,
        List<ListingData> available,
        int count)
    {
        var changed = false;

        while (count > 0 && available.Count > 0)
        {
            var listing = PickAndTake(available);

            if (!TryGetTelecrystalCost(listing, out var oldCost))
                continue;

            var saleCost = GetRandomSaleCost(oldCost, store.Comp);
            if (!TryGrantDiscount(uplink, store, listing, saleCost))
                continue;

            changed = true;
            count--;
        }

        return changed;
    }

    private T Pick<T>(IReadOnlyList<T> entries)
    {
        return entries[_random.Next(entries.Count)];
    }

    private T PickAndTake<T>(List<T> entries)
    {
        var index = _random.Next(entries.Count);
        var entry = entries[index];
        entries.RemoveAt(index);
        return entry;
    }

    // Builds the currently visible listing pool for cooperation discounts on this uplink store.
    private List<ListingData> GetEligibleRandomDiscountListings(
        Entity<TraitorUplinkCooperationComponent> uplink,
        Entity<StoreComponent> store)
    {
        var buyer = store.Comp.AccountOwner ?? uplink.Comp.OwnerMindId ?? uplink.Owner;

        return _store.GetAvailableListings(buyer, uplink.Owner, store.Comp)
            .Where(listing => IsEligibleForRandomDiscount(listing, store.Comp, uplink.Comp))
            .ToList();
    }

    // Uses the store's normal sale multiplier range for ordinary random discounts.
    private FixedPoint2 GetRandomSaleCost(FixedPoint2 oldCost, StoreComponent store)
    {
        var multiplier = _random.NextFloat() * (store.Sales.MaxMultiplier - store.Sales.MinMultiplier)
            + store.Sales.MinMultiplier;
        return FixedPoint2.New(Math.Max(1, (int) MathF.Round(oldCost.Float() * multiplier)));
    }

    // Converts a fixed discount percentage into a final TC price.
    private FixedPoint2 GetSaleCostByDiscountPercent(FixedPoint2 oldCost, int discountPercent)
    {
        if (discountPercent >= 100)
            return FixedPoint2.Zero;

        var multiplier = Math.Clamp(100 - discountPercent, 0, 100) / 100f;
        return FixedPoint2.New(Math.Max(1, (int) MathF.Round(oldCost.Float() * multiplier)));
    }

    // Excludes sales, event purchases, already discounted items, cheap filler, fixed rewards, and bundles.
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

        if (!TryGetTelecrystalCost(listing, out var cost) || cost <= FixedPoint2.New(1))
            return false;

        if (listing.Categories.Contains(store.Sales.SalesCategory) ||
            uplink.DiscountedListingIds.Contains(listing.ID))
            return false;

        var productEntity = listing.ProductEntity?.ToString() ?? string.Empty;
        return !RandomDiscountExclusionFragments.Any(exclusion =>
            listing.ID.Contains(exclusion, StringComparison.OrdinalIgnoreCase) ||
            productEntity.Contains(exclusion, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasMinimumTelecrystalCost(ListingData listing, int minTelecrystalCost)
    {
        return TryGetTelecrystalCost(listing, out var cost) &&
            cost >= FixedPoint2.New(minTelecrystalCost);
    }

    private static bool TryGetTelecrystalCost(ListingData listing, out FixedPoint2 cost)
    {
        return listing.Cost.TryGetValue(TelecrystalCurrency, out cost);
    }

    // Adds a fixed first-pairing discount for a known uplink listing prototype.
    private bool TryGrantPrototypeDiscount(
        Entity<TraitorUplinkCooperationComponent> uplink,
        Entity<StoreComponent> store,
        string listingId,
        int discountPercent)
    {
        if (!_prototype.TryIndex<ListingPrototype>(listingId, out var listing) ||
            !TryGetTelecrystalCost(listing, out var oldCost))
            return false;

        return TryGrantDiscount(uplink, store, listing, GetSaleCostByDiscountPercent(oldCost, discountPercent));
    }

    // Creates a one-use sale clone while leaving the original listing available at full price.
    // Remembering the source ID prevents duplicate cooperation discounts on the same item.
    private bool TryGrantDiscount(
        Entity<TraitorUplinkCooperationComponent> uplink,
        Entity<StoreComponent> store,
        ListingData source,
        FixedPoint2 saleCost)
    {
        if (uplink.Comp.DiscountedListingIds.Contains(source.ID))
            return false;

        if (!TryGetTelecrystalCost(source, out var oldCost) ||
            saleCost < FixedPoint2.Zero ||
            saleCost >= oldCost)
            return false;

        // The sale entry is a clone: the original catalog listing remains available at full price.
        var sale = (ListingData) source.Clone();
        sale.Categories = [store.Comp.Sales.SalesCategory];
        sale.OldCost = source.Cost.ToDictionary(x => x.Key, x => x.Value);
        sale.Cost = source.Cost.ToDictionary(x => x.Key, x => x.Value);
        sale.Cost[TelecrystalCurrency] = saleCost;
        sale.SaleCost = sale.Cost;
        sale.PurchaseAmount = 0;
        sale.SaleLimit = 1;
        sale.DiscountValue = saleCost <= FixedPoint2.Zero
            ? 100
            : 100 - (saleCost / oldCost * 100).Int();
        sale.Components = [.. source.Components, ManualDiscountMarker];

        if (!store.Comp.Listings.Add(sale))
            return false;

        uplink.Comp.DiscountedListingIds.Add(source.ID);
        return true;
    }

    // Removes bought one-use cooperation sale clones; ordinary listings do not have the marker.
    private void OnListingPurchased(
        Entity<TraitorUplinkCooperationComponent> ent,
        ref ListingPurchasedEvent args)
    {
        if (args.Store != ent.Owner || !TryGetUplinkStore(ent, out var store))
            return;

        if (args.Data.Components.Contains(ManualDiscountMarker))
            store.Comp.Listings.Remove(args.Data);
    }
}

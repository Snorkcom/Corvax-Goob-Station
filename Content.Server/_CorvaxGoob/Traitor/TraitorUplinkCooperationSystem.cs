// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.ManifestListings;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Store.Components;
using Content.Shared.Traitor.Cooperation;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Traitor.Cooperation;

/// <summary>
/// Coordinates cooperation metadata for traitor uplinks that were already created by the normal traitor flow.
/// </summary>
public sealed partial class TraitorUplinkCooperationSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraitorUplinkCooperationComponent, AfterInteractEvent>(OnUplinkAfterInteract);
        SubscribeLocalEvent<TraitorUplinkCooperationComponent, TraitorUplinkLinkDoAfterEvent>(OnUplinkLinkDoAfter);
        SubscribeLocalEvent<TraitorUplinkCooperationComponent, ListingPurchasedEvent>(OnListingPurchased);
    }

    /// <summary>
    /// Attaches the traitor owner's identity and employer to an existing uplink.
    /// </summary>
    public void RegisterTraitorUplink(EntityUid uplink, EntityUid mindId, string employer)
    {
        // Each uplink stores its traitor owner's mind ID; pairing uniqueness is checked against those stored owners.
        if (!HasExistingUplink(uplink))
            return;

        var comp = EnsureComp<TraitorUplinkCooperationComponent>(uplink);
        comp.OwnerMindId = mindId;
        comp.EmployerName = employer;
        Dirty(uplink, comp);
    }

    private bool HasExistingUplink(EntityUid uid)
    {
        return HasComp<UplinkComponent>(uid) && HasComp<StoreComponent>(uid);
    }

    private bool TryGetUplinkStore(
        Entity<TraitorUplinkCooperationComponent> uplink,
        out Entity<StoreComponent> store)
    {
        if (!TryComp<StoreComponent>(uplink.Owner, out var storeComp))
        {
            store = default;
            return false;
        }

        store = (uplink.Owner, storeComp);
        return true;
    }
}

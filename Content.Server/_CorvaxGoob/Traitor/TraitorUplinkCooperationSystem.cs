// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.ManifestListings;
using Content.Server.Chat.Systems;
using Content.Server.PDA.Ringer;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.PDA.Ringer;
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
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private RingerSystem _ringer = default!;
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RingerUplinkComponent, GotEmaggedEvent>(OnPdaEmagged);

        SubscribeLocalEvent<TraitorUplinkCooperationComponent, AfterInteractEvent>(OnUplinkAfterInteract);
        SubscribeLocalEvent<TraitorUplinkCooperationComponent, TraitorUplinkLinkDoAfterEvent>(OnUplinkLinkDoAfter);
        SubscribeLocalEvent<MindComponent, ListingPurchasedEvent>(OnListingPurchased);
    }

    public void RegisterTraitorUplink(EntityUid uplink, EntityUid mindId, string employer)
    {
        // Each uplink stores its traitor owner's mind ID; pairing uniqueness is checked against those stored owners.
        if (!HasComp<UplinkComponent>(uplink) || !HasComp<StoreComponent>(uplink))
            return;

        var comp = EnsureComp<TraitorUplinkCooperationComponent>(uplink);
        comp.OwnerMind = mindId;
        comp.Employer = employer;
        Dirty(uplink, comp);
    }
}

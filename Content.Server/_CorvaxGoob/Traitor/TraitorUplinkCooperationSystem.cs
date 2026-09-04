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
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private StoreSystem _store = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Pairing starts from item interaction and completes through a shared do-after event.
        SubscribeLocalEvent<TraitorUplinkCooperationComponent, AfterInteractEvent>(OnUplinkAfterInteract);
        SubscribeLocalEvent<TraitorUplinkCooperationComponent, TraitorUplinkLinkDoAfterEvent>(OnUplinkLinkDoAfter);

        // Manual sale clones are removed by the store that owns them, even if another character buys from the device.
        SubscribeLocalEvent<TraitorUplinkCooperationComponent, ListingPurchasedEvent>(OnListingPurchased);
    }

    /// <summary>
    /// Attaches the traitor owner's identity and employer to an uplink that was created by the normal traitor flow.
    /// </summary>
    public void RegisterTraitorUplink(EntityUid uplink, EntityUid mindId, string employer)
    {
        // Each uplink stores its traitor owner's mind ID; pairing uniqueness is checked against those stored owners.
        if (!HasExistingUplink(uplink))
            return;

        var comp = EnsureComp<TraitorUplinkCooperationComponent>(uplink);
        comp.OwnerMindId = mindId;
        comp.EmployerName = employer;
    }

    /// <summary>
    /// Confirms that the entity is already a real uplink store; this feature never creates a new uplink.
    /// </summary>
    private bool HasExistingUplink(EntityUid uid) =>
        HasComp<UplinkComponent>(uid) && HasComp<StoreComponent>(uid);

    /// <summary>
    /// Gets the StoreComponent from this registered uplink device.
    /// </summary>
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

// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Traitor.PenSpin;
using Content.Server.Popups;
using Content.Server.Traitor.Uplink;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Store.Components;

namespace Content.Goobstation.Server.Traitor.Uplink;

/// <summary>
/// Handles emag events for existing pen uplink stores.
/// </summary>
public sealed class PenUplinkEmagSystem : EntitySystem
{
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PenSpinUplinkComponent, GotEmaggedEvent>(OnPenEmagged);
    }

    /// <summary>
    /// Sets persistent unlock state for pen entities that already contain an uplink store.
    /// </summary>
    private void OnPenEmagged(Entity<PenSpinUplinkComponent> ent, ref GotEmaggedEvent args)
    {
        // Require an existing uplink store; this handler must not add uplink components to unrelated pens.
        if (ent.Comp.PermanentlyUnlocked ||
            !HasComp<UplinkComponent>(ent.Owner) ||
            !HasComp<StoreComponent>(ent.Owner))
            return;

        ent.Comp.PermanentlyUnlocked = true;
        ent.Comp.Unlocked = true;

        // The generic emag success popup is predicted from shared code, but this handler only runs on the server.
        // Show the same popup here so server-only pen uplink unlocks still notify the user.
        _popup.PopupEntity(
            Loc.GetString("emag-success", ("target", Identity.Entity(ent.Owner, EntityManager))),
            ent.Owner,
            args.UserUid,
            PopupType.Medium);

        args.Handled = true;
        args.Repeatable = true;
    }
}

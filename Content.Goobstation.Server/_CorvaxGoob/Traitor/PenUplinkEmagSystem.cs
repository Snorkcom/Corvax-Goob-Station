// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Traitor.PenSpin;
using Content.Server.Traitor.Uplink;
using Content.Shared.Emag.Systems;
using Content.Shared.Store.Components;

namespace Content.Goobstation.Server.Traitor.Uplink;

/// <summary>
/// Handles emag events for existing pen uplink stores.
/// </summary>
public sealed class PenUplinkEmagSystem : EntitySystem
{
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

        args.Handled = true;
        args.Repeatable = true;
    }
}

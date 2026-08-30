// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Server.Traitor.PenSpin;
using Content.Server.Traitor.Uplink;
using Content.Shared.Emag.Systems;
using Content.Shared.Store.Components;

namespace Content.Goobstation.Server.Traitor.Cooperation;

public sealed class PenUplinkEmagSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PenSpinUplinkComponent, GotEmaggedEvent>(OnPenEmagged);
    }

    /// <summary>
    /// Permanently unlocks the pen's existing uplink when emagged without creating a new uplink.
    /// </summary>
    private void OnPenEmagged(Entity<PenSpinUplinkComponent> ent, ref GotEmaggedEvent args)
    {
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

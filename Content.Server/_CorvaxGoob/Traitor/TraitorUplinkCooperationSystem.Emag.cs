// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Traitor.Uplink;
using Content.Shared.Emag.Systems;
using Content.Shared.PDA.Ringer;
using Content.Shared.Store.Components;

namespace Content.Server.Traitor.Cooperation;

/// <summary>
/// Handles emag opening for existing PDA uplinks.
/// </summary>
public sealed partial class TraitorUplinkCooperationSystem
{
    private void OnPdaEmagged(Entity<RingerUplinkComponent> ent, ref GotEmaggedEvent args)
    {
        // Emagging only opens an existing traitor PDA uplink; it should never create a store or uplink.
        if (ent.Comp.Unlocked || !HasExistingUplink(ent.Owner))
            return;

        if (!_ringer.UnlockUplink(ent))
            return;

        args.Handled = true;
        args.Repeatable = true;
    }

    private bool HasExistingUplink(EntityUid uid)
    {
        return HasComp<UplinkComponent>(uid) && HasComp<StoreComponent>(uid);
    }
}

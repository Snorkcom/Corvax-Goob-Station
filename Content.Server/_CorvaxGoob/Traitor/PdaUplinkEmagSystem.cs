// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.PDA.Ringer;
using Content.Shared.Emag.Systems;
using Content.Shared.PDA.Ringer;
using Content.Shared.Store.Components;

namespace Content.Server.Traitor.Uplink;

/// <summary>
/// Handles emag events for existing PDA ringer uplink stores.
/// </summary>
public sealed class PdaUplinkEmagSystem : EntitySystem
{
    [Dependency] private readonly RingerSystem _ringer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RingerUplinkComponent, GotEmaggedEvent>(OnPdaEmagged);
    }

    private void OnPdaEmagged(Entity<RingerUplinkComponent> ent, ref GotEmaggedEvent args)
    {
        // Require an existing uplink store; this handler must not add uplink components to unrelated PDAs.
        if (ent.Comp.Unlocked ||
            !HasComp<UplinkComponent>(ent.Owner) ||
            !HasComp<StoreComponent>(ent.Owner))
            return;

        if (!_ringer.UnlockUplink(ent))
            return;

        args.Handled = true;
        args.Repeatable = true;
    }
}

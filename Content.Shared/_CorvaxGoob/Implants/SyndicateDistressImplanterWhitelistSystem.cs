// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Implants.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants;

/// <summary>
/// Adds the syndicate distress implant to implanter extraction choices on both server and client.
/// This keeps the feature isolated without copying the base implanter prototype whitelist.
/// </summary>
public sealed partial class SyndicateDistressImplanterWhitelistSystem : EntitySystem
{
    private static readonly EntProtoId DistressImplantPrototype = "SyndicateDistressImplant";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, ComponentStartup>(OnImplanterStartup);
    }

    private void OnImplanterStartup(Entity<ImplanterComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.DeimplantWhitelist.Contains(DistressImplantPrototype))
            return;

        ent.Comp.DeimplantWhitelist.Add(DistressImplantPrototype);
    }
}

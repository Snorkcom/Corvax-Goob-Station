// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Implants.Components;

/// <summary>
/// Marks an implant that can broadcast the user's registered identity and position
/// to a configured radio channel.
/// </summary>
[RegisterComponent]
public sealed partial class SyndicateDistressImplantComponent : Component
{
    /// <summary>
    /// Message sent over the configured radio channel when the action is used.
    /// </summary>
    [DataField]
    public LocId DistressMessage = "syndicate-distress-implant-message";

    /// <summary>
    /// Channel that receives the distress signal.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Syndicate";
}

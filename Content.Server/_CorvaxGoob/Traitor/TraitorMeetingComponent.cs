// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.Traitor.Cooperation;

/// <summary>
/// Round-wide meeting hint shared by all traitors created under the same traitor rule.
/// </summary>
[RegisterComponent]
public sealed partial class TraitorMeetingComponent : Component
{
    /// <summary>
    /// Whether the shared hint has already been generated for this rule.
    /// </summary>
    [DataField]
    public bool Initialized;

    /// <summary>
    /// Inclusive lower bound of the suggested meeting window in shift minutes.
    /// </summary>
    [DataField]
    public int StartMinute;

    /// <summary>
    /// Inclusive upper bound of the suggested meeting window in shift minutes.
    /// </summary>
    [DataField]
    public int EndMinute;

    /// <summary>
    /// Human-readable station area selected from enabled nav map beacons.
    /// </summary>
    [DataField]
    public string Location = string.Empty;
}

// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Server.Traitor.PenSpin;

/// <summary>
/// Stores persistent unlock state for pen uplinks opened through an emag interaction.
/// </summary>
public sealed partial class PenSpinUplinkComponent
{
    /// <summary>
    /// Indicates that the pen uplink may be opened without matching the generated spin code.
    /// </summary>
    [DataField]
    public bool PermanentlyUnlocked;
}

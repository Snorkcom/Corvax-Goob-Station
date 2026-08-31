// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.PDA.Ringer;

namespace Content.Server.PDA.Ringer;

/// <summary>
/// Provides a server API for opening an existing ringer uplink without validating its generated code.
/// </summary>
public sealed partial class RingerSystem
{
    /// <summary>
    /// Opens a locked ringer uplink through the standard toggle path.
    /// </summary>
    public bool UnlockUplink(Entity<RingerUplinkComponent> ent)
    {
        if (ent.Comp.Unlocked)
            return false;

        return ToggleUplinkInternal(ent);
    }
}

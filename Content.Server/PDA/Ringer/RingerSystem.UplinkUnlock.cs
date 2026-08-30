// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.PDA.Ringer;

namespace Content.Server.PDA.Ringer;

public sealed partial class RingerSystem
{
    /// <summary>
    /// Unlocks an existing ringer uplink without requiring the generated ringtone code.
    /// </summary>
    public bool UnlockUplink(Entity<RingerUplinkComponent> ent)
    {
        if (ent.Comp.Unlocked)
            return false;

        return ToggleUplinkInternal(ent);
    }
}

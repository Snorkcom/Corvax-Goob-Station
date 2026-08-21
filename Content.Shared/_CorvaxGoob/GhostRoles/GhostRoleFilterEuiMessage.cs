// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxGoob.GhostRoles;

[NetSerializable, Serializable]
public sealed class SetGhostRoleNotificationsMessage : EuiMessageBase
{
    public bool Enabled { get; }

    public SetGhostRoleNotificationsMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

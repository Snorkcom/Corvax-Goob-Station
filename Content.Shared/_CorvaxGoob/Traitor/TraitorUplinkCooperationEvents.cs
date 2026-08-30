// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Traitor.Cooperation;

/// <summary>
/// Do-after event raised when two traitor uplink devices finish pairing.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TraitorUplinkLinkDoAfterEvent : SimpleDoAfterEvent;

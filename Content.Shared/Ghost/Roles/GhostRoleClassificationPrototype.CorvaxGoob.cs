// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles;

[Serializable, NetSerializable]
public enum GhostRoleCategory
{
    Antagonist,
    Other,
}

[Prototype("ghostRoleClassification")]
public sealed partial class GhostRoleClassificationPrototype : IPrototype
{
    public const int DefaultPriority = 1;

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public GhostRoleCategory Category = GhostRoleCategory.Other;

    [DataField]
    public bool NotifyOnAvailable;

    [DataField]
    public int Priority = DefaultPriority;
}

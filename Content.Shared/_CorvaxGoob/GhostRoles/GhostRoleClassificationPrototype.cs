// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxGoob.GhostRoles;

public enum GhostRoleCategory
{
    Antagonist,
    Other,
}

/// <summary>
/// Explicit metadata for ghost roles shown by the important-role filter.
/// </summary>
[Prototype("ghostRoleClassification")]
public sealed partial class GhostRoleClassificationPrototype : IPrototype
{
    public const int UnclassifiedPriority = 0;
    public const int DefaultPriority = 1;

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public GhostRoleCategory Category = GhostRoleCategory.Other;

    [DataField]
    public bool NotifyOnAvailable = true;

    [DataField]
    public int Priority = DefaultPriority;
}

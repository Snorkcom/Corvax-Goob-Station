// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Numerics;

namespace Content.Client._White.ItemSlotRenderer;

public sealed partial class ItemSlotRendererComponent
{
    /// <summary>
    ///     Centers the rendered slot item by its visible alpha bounds.
    /// </summary>
    [DataField("centerItemByContentBounds")]
    public bool CenterItemByContentBounds;

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<string, EntityUid> ContentBoundsCacheEntities = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<string, Vector2> ContentBoundsOffsets = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<string> ContentBoundsPendingSlots = new();
}

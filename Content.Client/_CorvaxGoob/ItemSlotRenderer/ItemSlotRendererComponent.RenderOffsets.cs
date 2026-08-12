// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Numerics;

namespace Content.Client._White.ItemSlotRenderer;

public sealed partial class ItemSlotRendererComponent
{
    /// <summary>
    ///     Per-slot render offsets in render-target pixels.
    /// </summary>
    [DataField("renderOffsets")]
    public Dictionary<string, Vector2> RenderOffsets = new();
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;

namespace Content.Client._White.ItemSlotRenderer;

public sealed partial class SpriteToLayerBullshitOverlay
{
    partial void AdjustRenderPosition(ItemSlotRendererComponent component, string slotId, EntityUid item, ref Vector2 position)
    {
        if (component.RenderOffsets.TryGetValue(slotId, out var offset))
            position += offset;

        if (!component.CenterItemByContentBounds ||
            !component.ContentBoundsCacheEntities.TryGetValue(slotId, out var cachedItem) ||
            cachedItem != item ||
            !component.ContentBoundsOffsets.TryGetValue(slotId, out var contentOffset))
        {
            return;
        }

        position += contentOffset;
    }
}

// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._White.ItemSlotRenderer;

public sealed partial class SpriteToLayerBullshitOverlay
{
    partial void AfterRenderSlot(
        ItemSlotRendererComponent component,
        string slotId,
        EntityUid item,
        IRenderTexture renderTarget,
        Vector2 targetPosition)
    {
        if (!component.CenterItemByContentBounds)
            return;

        if (component.ContentBoundsCacheEntities.TryGetValue(slotId, out var cachedItem) &&
            cachedItem == item &&
            component.ContentBoundsOffsets.ContainsKey(slotId))
        {
            return;
        }

        if (!component.ContentBoundsPendingSlots.Add(slotId))
            return;

        renderTarget.CopyPixelsToMemory<Rgba32>(image =>
        {
            try
            {
                if (TryCalculateContentBoundsOffset(image.GetPixelSpan(), image.Width, image.Height, targetPosition, out var offset))
                {
                    component.ContentBoundsCacheEntities[slotId] = item;
                    component.ContentBoundsOffsets[slotId] = offset;
                }
                else
                {
                    component.ContentBoundsCacheEntities[slotId] = item;
                    component.ContentBoundsOffsets[slotId] = Vector2.Zero;
                }
            }
            finally
            {
                component.ContentBoundsPendingSlots.Remove(slotId);
                image.Dispose();
            }
        });
    }

    private static bool TryCalculateContentBoundsOffset(
        ReadOnlySpan<Rgba32> pixels,
        int width,
        int height,
        Vector2 targetPosition,
        out Vector2 offset)
    {
        var minX = width;
        var minY = height;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (pixels[y * width + x].A == 0)
                    continue;

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
        {
            offset = Vector2.Zero;
            return false;
        }

        var contentCenter = new Vector2(
            (minX + maxX + 1) / 2f,
            (minY + maxY + 1) / 2f);

        offset = targetPosition - contentCenter;
        return true;
    }
}

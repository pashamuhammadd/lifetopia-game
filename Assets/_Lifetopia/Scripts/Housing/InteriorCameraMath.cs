using UnityEngine;

namespace Lifetopia.Housing
{
    /// <summary>
    /// Pure maths for the adaptive interior camera (unit tested). The world is never scaled: only the
    /// orthographic size and the camera position change. "Usable" is the part of the screen not covered
    /// by HUD panels and outside the safe area, as a normalised viewport rect (0..1).
    /// </summary>
    public static class InteriorCameraMath
    {
        /// <summary>
        /// Smallest orthographic size that shows <paramref name="content"/> (world size) plus
        /// <paramref name="margin"/> on each side inside the usable rect:
        /// size ≥ max(height / (2·usableH), width / (2·aspect·usableW)).
        /// </summary>
        public static float FitSize(Vector2 content, float margin, float aspect, Rect usable)
        {
            float uw = Mathf.Max(0.05f, usable.width), uh = Mathf.Max(0.05f, usable.height);
            float a = Mathf.Max(0.1f, aspect);
            float byHeight = (content.y + 2f * margin) / (2f * uh);
            float byWidth = (content.x + 2f * margin) / (2f * a * uw);
            return Mathf.Max(byHeight, byWidth);
        }

        /// <summary>Half size in world units of the usable part of the view.</summary>
        public static Vector2 UsableHalfExtents(float size, float aspect, Rect usable) =>
            new Vector2(size * aspect * usable.width, size * usable.height);

        /// <summary>Camera centre that puts world point <paramref name="focus"/> at the centre of the usable rect.</summary>
        public static Vector2 CameraForFocus(Vector2 focus, float size, float aspect, Rect usable) =>
            focus - new Vector2((usable.center.x - 0.5f) * 2f * size * aspect, (usable.center.y - 0.5f) * 2f * size);

        /// <summary>
        /// Keeps the usable view inside <paramref name="bounds"/> (expanded by margin) so the player can pan/follow
        /// to every edge but never far outside; centres an axis where the bounds fit.
        /// </summary>
        public static Vector2 ClampFocus(Vector2 focus, Rect bounds, float margin, Vector2 usableHalf)
        {
            Rect b = new Rect(bounds.xMin - margin, bounds.yMin - margin, bounds.width + 2f * margin, bounds.height + 2f * margin);
            return new Vector2(Axis(focus.x, b.xMin, b.xMax, usableHalf.x), Axis(focus.y, b.yMin, b.yMax, usableHalf.y));
        }

        private static float Axis(float v, float min, float max, float half)
        {
            if (max - min <= half * 2f) return (min + max) * 0.5f;
            return Mathf.Clamp(v, min + half, max - half);
        }

        /// <summary>Normalised viewport rect of a screen-pixel rect (clamped to 0..1).</summary>
        public static Rect Normalise(Rect screenRect, float screenW, float screenH)
        {
            if (screenW <= 0f || screenH <= 0f) return new Rect(0f, 0f, 1f, 1f);
            float x0 = Mathf.Clamp01(screenRect.xMin / screenW), x1 = Mathf.Clamp01(screenRect.xMax / screenW);
            float y0 = Mathf.Clamp01(screenRect.yMin / screenH), y1 = Mathf.Clamp01(screenRect.yMax / screenH);
            if (x1 - x0 < 0.05f || y1 - y0 < 0.05f) return new Rect(0f, 0f, 1f, 1f);
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }

        /// <summary>Cells covered by the player's foot circle (centre = feet + offset), for placement blocking.</summary>
        public static void PlayerCells(Vector2 roomLocalFeet, Vector2 colliderOffset, float radius, System.Collections.Generic.List<Vector2Int> into)
        {
            into.Clear();
            Vector2 c = roomLocalFeet + colliderOffset;
            const float pad = 0.02f;
            int x0 = Mathf.FloorToInt(c.x - radius - pad), x1 = Mathf.FloorToInt(c.x + radius + pad);
            int y0 = Mathf.FloorToInt(c.y - radius - pad), y1 = Mathf.FloorToInt(c.y + radius + pad);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    into.Add(new Vector2Int(x, y));
        }
    }
}

using UnityEngine;

namespace Lifetopia.CameraSystem
{
    /// <summary>Pure camera clamping math (unit tested).</summary>
    public static class CameraBoundsMath
    {
        /// <summary>
        /// Half the visible area of an orthographic camera in world units.
        /// orthographicSize is half the view height; width follows the screen aspect.
        /// </summary>
        public static Vector2 HalfExtents(float orthographicSize, float aspect)
            => new Vector2(orthographicSize * aspect, orthographicSize);

        /// <summary>
        /// Keeps the camera centre inside <paramref name="bounds"/> so the view never shows
        /// outside the map. If the map is smaller than the view on an axis, the camera is
        /// centred on the map on that axis.
        /// </summary>
        public static Vector2 Clamp(Vector2 desired, Rect bounds, Vector2 halfExtents)
        {
            return new Vector2(
                ClampAxis(desired.x, bounds.xMin, bounds.xMax, halfExtents.x),
                ClampAxis(desired.y, bounds.yMin, bounds.yMax, halfExtents.y));
        }

        private static float ClampAxis(float value, float min, float max, float half)
        {
            if (max - min <= half * 2f) return (min + max) * 0.5f;
            return Mathf.Clamp(value, min + half, max - half);
        }
    }
}

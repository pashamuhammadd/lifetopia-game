using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.UI
{
    /// <summary>
    /// Generates white rounded-rectangle 9-slice sprites in code (tinted by Image.color), so the
    /// placeholder UI needs no texture files. Each radius/softness is generated once and cached.
    /// Replace with authored UI art later by assigning a sprite on the Image (see <see cref="RoundedImage"/>).
    /// </summary>
    public static class RoundedSprites
    {
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        /// <param name="radius">Corner radius in pixels (2..64).</param>
        /// <param name="softness">0 = crisp edge; &gt;0 = blurred edge in pixels (for soft shadows).</param>
        public static Sprite Get(int radius, int softness = 0)
        {
            radius = Mathf.Clamp(radius, 2, 64);
            softness = Mathf.Clamp(softness, 0, 32);
            int key = radius * 100 + softness;
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            int border = radius + softness;
            int size = border * 2 + 2;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"RoundedRect_r{radius}_s{softness}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };

            var pixels = new Color32[size * size];
            float center = size * 0.5f;
            float innerHalf = 1f; // the 2 px stretchable centre
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - center) - innerHalf, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - center) - innerHalf, 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = softness == 0
                        ? Mathf.Clamp01(radius + 0.5f - distance)
                        : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(radius - softness, radius + softness, distance));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.DontSave;
            Cache[key] = sprite;
            return sprite;
        }
    }
}

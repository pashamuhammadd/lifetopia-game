using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.Fishing
{
    /// <summary>
    /// Clearly temporary fishing shapes generated in code (public repository, no image files):
    /// bobber (red/white ball, bite = squashed), ripple rings, splash droplets, and a 1-pixel line.
    /// </summary>
    public static class FishingPlaceholderSprites
    {
        private const int Size = 64;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Bobber(bool bite) => Get(bite ? "bobber_bite" : "bobber_idle", Size, new Vector2(0.5f, 0.5f));
        public static Sprite Ripple(int frame) => Get("ripple_" + (frame == 0 ? 0 : 1), Size, new Vector2(0.5f, 0.5f));
        public static Sprite Splash(int frame) => Get("splash_" + (frame == 0 ? 0 : 1), Size, new Vector2(0.5f, 0.42f));
        /// <summary>1 × 1 white pixel at 1 PPU (scale it to draw lines).</summary>
        public static Sprite Pixel => Get("pixel", 1, new Vector2(0f, 0.5f), 1f);

        private static Sprite Get(string key, int size, Vector2 pivot, float ppu = 64f)
        {
            if (Cache.TryGetValue(key, out Sprite s) && s != null) return s;
            var px = new Color32[size * size];
            if (size == 1) px[0] = new Color32(255, 255, 255, 255);
            else Paint(key, px);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "fishing_placeholder_" + key,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), pivot, ppu, 0, SpriteMeshType.FullRect);
            s.name = tex.name;
            s.hideFlags = HideFlags.DontSave;
            Cache[key] = s;
            return s;
        }

        private static void Ellipse(Color32[] px, float cx, float cy, float rx, float ry, Color32 c, float ringWidth = 0f)
        {
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    float d = dx * dx + dy * dy;
                    if (d > 1f) continue;
                    if (ringWidth > 0f)
                    {
                        float inner = 1f - ringWidth;
                        if (d < inner * inner) continue;
                    }
                    px[y * Size + x] = c;
                }
        }

        private static void Paint(string key, Color32[] px)
        {
            var white = new Color32(255, 250, 240, 255);
            var red = new Color32(220, 80, 70, 255);
            var water = new Color32(225, 245, 250, 230);
            switch (key)
            {
                case "bobber_idle":
                    Ellipse(px, 32, 28, 9, 9, white);
                    Ellipse(px, 32, 34, 9, 6, red);
                    break;
                case "bobber_bite":
                    Ellipse(px, 32, 24, 10, 6, white);
                    Ellipse(px, 32, 27, 10, 4, red);
                    break;
                case "ripple_0":
                    Ellipse(px, 32, 28, 16, 6, water, 0.25f);
                    break;
                case "ripple_1":
                    Ellipse(px, 32, 28, 26, 9, water, 0.18f);
                    break;
                case "splash_0":
                case "splash_1":
                    bool wide = key == "splash_1";
                    for (int i = 0; i < 5; i++)
                    {
                        float a = (i - 2) * (wide ? 0.55f : 0.4f);
                        Ellipse(px, 32 + Mathf.Sin(a) * (wide ? 22 : 14), 30 + Mathf.Cos(a) * (wide ? 14 : 12), 3, 3, water);
                    }
                    Ellipse(px, 32, 27, wide ? 18 : 12, wide ? 5 : 4, water, 0.3f);
                    break;
            }
        }
    }
}

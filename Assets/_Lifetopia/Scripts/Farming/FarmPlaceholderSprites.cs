using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.Farming
{
    /// <summary>
    /// Clearly temporary greybox shapes for farm plots, generated once in code (no image files), so the
    /// public repository needs no artwork. Shapes differ per state, not only colour:
    /// soil = plain / striped / striped + dots; crop = dots / short stem / tall leaves / leaves + fruit;
    /// indicators = drop (needs water) / diamond (ready).
    /// </summary>
    public static class FarmPlaceholderSprites
    {
        private const int Size = 64;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Soil(bool tilled, bool wet) =>
            Get(!tilled ? "soil_unworked" : wet ? "soil_wet" : "soil_dry", BuildSoil, new Vector2(0.5f, 0.5f));

        /// <summary>Stage index: 0 seeded, 1 sprout, 2 young, 3 ready.</summary>
        public static Sprite Crop(int stageIndex) =>
            stageIndex < 0 ? null : Get("crop_" + Mathf.Clamp(stageIndex, 0, 3), BuildCrop, new Vector2(0.5f, 0.125f));

        public static Sprite NeedsWater => Get("ind_water", BuildIndicator, new Vector2(0.5f, 0.5f));
        public static Sprite Ready => Get("ind_ready", BuildIndicator, new Vector2(0.5f, 0.5f));
        public static Sprite Highlight => Get("highlight", BuildHighlight, new Vector2(0.5f, 0.5f));
        /// <summary>Cream disc with a brown rim drawn behind status badges so they stay readable over leaves.</summary>
        public static Sprite BadgeBack => Get("badge_back", BuildBadgeBack, new Vector2(0.5f, 0.5f));

        private delegate void Painter(string key, Color32[] px);

        private static Sprite Get(string key, Painter painter, Vector2 pivot)
        {
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;
            var px = new Color32[Size * Size];
            painter(key, px);
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "placeholder_" + key,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            // 64 px at 64 PPU = 1 world unit, like the real 64 x 64 soil art.
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), pivot, Size, 0, SpriteMeshType.FullRect);
            sprite.name = "placeholder_" + key;
            sprite.hideFlags = HideFlags.DontSave;
            Cache[key] = sprite;
            return sprite;
        }

        private static void Fill(Color32[] px, int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(Size, y1); y++)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(Size, x1); x++)
                    px[y * Size + x] = c;
        }

        private static void Disc(Color32[] px, float cx, float cy, float r, Color32 c)
        {
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r * r) px[y * Size + x] = c;
                }
        }

        private static void BuildSoil(string key, Color32[] px)
        {
            Color32 baseColor = key == "soil_unworked" ? new Color32(190, 150, 100, 255)
                              : key == "soil_dry" ? new Color32(150, 105, 65, 255)
                              : new Color32(105, 70, 45, 255);
            Color32 border = new Color32(110, 75, 45, 255);
            Fill(px, 2, 2, Size - 2, Size - 2, border);
            Fill(px, 5, 5, Size - 5, Size - 5, baseColor);
            if (key == "soil_unworked") return;
            Color32 furrow = key == "soil_dry" ? new Color32(115, 78, 48, 255) : new Color32(70, 45, 30, 255);
            for (int y = 12; y < Size - 8; y += 10) Fill(px, 9, y, Size - 9, y + 3, furrow);
            if (key == "soil_wet")
                for (int i = 0; i < 6; i++) Disc(px, 14 + i * 7, 18 + (i % 3) * 12, 2f, new Color32(120, 190, 230, 255));
        }

        private static void BuildCrop(string key, Color32[] px)
        {
            int stage = key[key.Length - 1] - '0';
            Color32 seed = new Color32(140, 100, 60, 255);
            Color32 green = new Color32(80, 160, 70, 255);
            Color32 dark = new Color32(50, 115, 50, 255);
            const int baseY = 8; // pivot y = 0.125 * 64
            switch (stage)
            {
                case 0:
                    for (int i = 0; i < 3; i++) Disc(px, 24 + i * 8, baseY + 3, 2.5f, seed);
                    break;
                case 1:
                    Fill(px, 31, baseY, 33, baseY + 12, dark);
                    Disc(px, 27, baseY + 13, 4, green);
                    Disc(px, 37, baseY + 13, 4, green);
                    break;
                default:
                    for (int i = -2; i <= 2; i++)
                    {
                        int h = stage == 2 ? 26 - System.Math.Abs(i) * 4 : 32 - System.Math.Abs(i) * 4;
                        Fill(px, 30 + i * 6, baseY, 34 + i * 6, baseY + h, i % 2 == 0 ? green : dark);
                    }
                    if (stage == 3)
                    {
                        Disc(px, 32, baseY + 30, 9, new Color32(240, 190, 50, 255));
                        Fill(px, 30, baseY + 38, 34, baseY + 46, dark);
                    }
                    break;
            }
        }

        private static void BuildIndicator(string key, Color32[] px)
        {
            var white = new Color32(255, 250, 235, 255);
            if (key == "ind_water")
            {
                var blue = new Color32(70, 160, 220, 255);
                Disc(px, 32, 26, 16, white);
                Disc(px, 32, 26, 13, blue);
                for (int y = 26; y < 56; y++)
                {
                    int half = (56 - y) / 2;
                    for (int x = 32 - half; x <= 32 + half; x++) if (x >= 0 && x < Size) px[y * Size + x] = blue;
                }
                return;
            }
            var gold = new Color32(240, 185, 40, 255);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    int d = System.Math.Abs(x - 32) + System.Math.Abs(y - 32);
                    if (d <= 26) px[y * Size + x] = d >= 21 ? white : gold;
                }
        }

        private static void BuildBadgeBack(string key, Color32[] px)
        {
            Disc(px, 32, 32, 31, new Color32(120, 82, 52, 255));
            Disc(px, 32, 32, 27, new Color32(255, 246, 224, 255));
        }

        private static void BuildHighlight(string key, Color32[] px)
        {
            var c = new Color32(255, 250, 230, 255);
            Fill(px, 0, 0, Size, 4, c);
            Fill(px, 0, Size - 4, Size, Size, c);
            Fill(px, 0, 0, 4, Size, c);
            Fill(px, Size - 4, 0, Size, Size, c);
        }
    }
}

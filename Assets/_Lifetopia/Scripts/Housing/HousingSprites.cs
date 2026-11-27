using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.Housing
{
    /// <summary>
    /// Sprites generated in code (no image files): clearly temporary greybox shapes for the public
    /// build (floor, wall strips) and the neutral overlays used by Decorating Mode in both variants
    /// (grid lines, footprint cells). All 64 px = 1 world unit.
    /// </summary>
    public static class HousingSprites
    {
        private const int Size = 64;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>Plain white square, pivot bottom-centre (stretch with localScale, tint with colour).</summary>
        public static Sprite WhiteBottom => Get("white_bottom", (k, p) => Fill(p, 0, 0, Size, Size, new Color32(255, 255, 255, 255)), new Vector2(0.5f, 0f));

        /// <summary>Plain white square, pivot centre.</summary>
        public static Sprite WhiteCenter => Get("white_center", (k, p) => Fill(p, 0, 0, Size, Size, new Color32(255, 255, 255, 255)), new Vector2(0.5f, 0.5f));

        /// <summary>Greybox floor tile (two tones so the pattern reads as a grid of boards).</summary>
        public static Sprite PlaceholderFloor(int variant) => Get("floor_" + (variant & 1), BuildFloor, new Vector2(0.5f, 0.5f));

        /// <summary>Transparent cell with a thin light border (Decorating grid overlay).</summary>
        public static Sprite GridCell => Get("grid_cell", BuildGridCell, new Vector2(0.5f, 0.5f));

        /// <summary>Rounded filled cell (footprint preview), tinted green/red by the caller.</summary>
        public static Sprite FootprintCell => Get("footprint_cell", BuildFootprint, new Vector2(0.5f, 0.5f));

        /// <summary>Diagonal hatch cell marking reserved exit/spawn cells.</summary>
        public static Sprite HatchCell => Get("hatch_cell", BuildHatch, new Vector2(0.5f, 0.5f));

        private delegate void Painter(string key, Color32[] px);

        private static Sprite Get(string key, Painter painter, Vector2 pivot)
        {
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;
            var px = new Color32[Size * Size];
            painter(key, px);
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "housing_" + key,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), pivot, Size, 0, SpriteMeshType.FullRect);
            sprite.name = "housing_" + key;
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

        private static void BuildFloor(string key, Color32[] px)
        {
            bool alt = key.EndsWith("1");
            Fill(px, 0, 0, Size, Size, alt ? new Color32(201, 160, 112, 255) : new Color32(211, 170, 120, 255));
            Color32 seam = new Color32(165, 122, 80, 255);
            Fill(px, 0, 0, Size, 2, seam);
            Fill(px, 0, 31, Size, 33, seam);
            Fill(px, alt ? 20 : 44, 2, alt ? 22 : 46, 31, seam);
        }

        private static void BuildGridCell(string key, Color32[] px)
        {
            Color32 line = new Color32(255, 250, 235, 150);
            Fill(px, 0, 0, Size, 2, line);
            Fill(px, 0, Size - 2, Size, Size, line);
            Fill(px, 0, 0, 2, Size, line);
            Fill(px, Size - 2, 0, Size, Size, line);
        }

        private static void BuildFootprint(string key, Color32[] px)
        {
            const float r = 10f, inset = 4f;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - Size * 0.5f) - (Size * 0.5f - inset - r), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - Size * 0.5f) - (Size * 0.5f - inset - r), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r + 0.5f - d);
                    bool edge = d > r - 4f;
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)(a * (edge ? 235 : 120)));
                }
        }

        private static void BuildHatch(string key, Color32[] px)
        {
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    px[y * Size + x] = ((x + y) % 16) < 5 ? new Color32(255, 255, 255, 140) : new Color32(255, 255, 255, 0);
        }
    }
}

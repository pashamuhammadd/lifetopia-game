using Lifetopia.Items;
using UnityEngine;

namespace Lifetopia.UI
{
    /// <summary>Art Bible 1.0 colours (section 02) used by the inventory HUD.</summary>
    public static class UiPalette
    {
        public static readonly Color Leaf = Hex(0x74B85B);
        public static readonly Color DeepLeaf = Hex(0x377F55);
        public static readonly Color Lagoon = Hex(0x55C6C8);
        public static readonly Color Sand = Hex(0xEBD4A5);
        public static readonly Color Wood = Hex(0xA8754D);
        public static readonly Color Coral = Hex(0xEE816C);
        public static readonly Color Cream = Hex(0xFFF3D8);
        public static readonly Color Ink = Hex(0x604A3C);

        /// <summary>Placeholder tile colour per category until real icons exist.</summary>
        public static Color ForCategory(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Tool: return Wood;
                case ItemCategory.Seed: return Leaf;
                case ItemCategory.Crop: return Coral;
                case ItemCategory.Fish: return Lagoon;
                case ItemCategory.Material: return Sand;
                case ItemCategory.Furniture: return DeepLeaf;
                default: return Sand;
            }
        }

        /// <summary>Ink on light backgrounds, Cream on dark ones (readable label colour).</summary>
        public static Color LabelOn(Color background)
        {
            float luminance = 0.2126f * background.linear.r + 0.7152f * background.linear.g + 0.0722f * background.linear.b;
            return luminance > 0.3f ? Ink : Cream;
        }

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}

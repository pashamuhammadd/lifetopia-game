using System;

namespace Lifetopia.World
{
    /// <summary>What a visual (vertex) tile should show.</summary>
    public enum DualGridKind
    {
        /// <summary>Grass/path transition; <see cref="DualGridChoice.Mask"/> 0 = grass, 15 = path/sand.</summary>
        Terrain,
        /// <summary>Sand/shallow-water transition; mask counts water corners (1..14 except 5 and 10).</summary>
        Shore,
        /// <summary>All four corners are water.</summary>
        Water,
        /// <summary>A combination the art set cannot draw (diagonal shore 5/10, or grass touching water).</summary>
        Unsupported,
    }

    public readonly struct DualGridChoice
    {
        public readonly DualGridKind Kind;
        public readonly int Mask;

        public DualGridChoice(DualGridKind kind, int mask)
        {
            Kind = kind;
            Mask = mask;
        }

        public override string ToString() => Kind + ":" + Mask;
    }

    /// <summary>
    /// Dual-grid rules (Gameplay Asset Pack 01 C/E contract). The LOGICAL grid (walkability, colliders,
    /// tilemap cells) is unchanged; a separate VISUAL grid is offset by half a cell, so every visual tile
    /// is centred on a vertex where four logical cells meet.
    /// Corner bits (Unity, +Y up): NW = 1, NE = 2, SE = 4, SW = 8. For vertex (vx, vy) the cells are
    /// NW = (vx-1, vy), NE = (vx, vy), SE = (vx, vy-1), SW = (vx-1, vy-1).
    /// </summary>
    public static class DualGridMask
    {
        public const int NW = 1, NE = 2, SE = 4, SW = 8, All = 15;

        public static int Compute(bool nw, bool ne, bool se, bool sw) =>
            (nw ? NW : 0) | (ne ? NE : 0) | (se ? SE : 0) | (sw ? SW : 0);

        /// <summary>Mask of the four cells around vertex (vx, vy) for which <paramref name="sample"/> is true.</summary>
        public static int AtVertex(Func<int, int, bool> sample, int vx, int vy) =>
            Compute(sample(vx - 1, vy), sample(vx, vy), sample(vx, vy - 1), sample(vx - 1, vy - 1));

        /// <summary>Checkerboard masks (no art in the shore set).</summary>
        public static bool IsDiagonal(int mask) => mask == (NW | SE) || mask == (NE | SW);

        /// <summary>
        /// Chooses the visual tile from the path/sand and water masks of one vertex. No water corner →
        /// grass/path terrain tile. Any water corner → shore tile, which needs every non-water corner to be
        /// path/sand (the art set has no grass/water edge) and a non-diagonal water mask.
        /// </summary>
        public static DualGridChoice Classify(int pathMask, int waterMask)
        {
            pathMask &= All;
            waterMask &= All;
            if (waterMask == 0) return new DualGridChoice(DualGridKind.Terrain, pathMask);
            if (waterMask == All) return new DualGridChoice(DualGridKind.Water, All);
            int land = All & ~waterMask;
            bool grassTouchesWater = (pathMask & land) != land;
            if (grassTouchesWater || IsDiagonal(waterMask)) return new DualGridChoice(DualGridKind.Unsupported, waterMask);
            return new DualGridChoice(DualGridKind.Shore, waterMask);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Lifetopia.World
{
    /// <summary>
    /// Tiles for the dual-grid visual layer. Index = corner mask (NW 1, NE 2, SE 4, SW 8).
    /// Terrain: 0 = grass .. 15 = path/sand. Shore: water corners 1..14; 0 (sand) uses terrain 15,
    /// 15 uses <see cref="Water"/>; 5 and 10 have no art and must not appear in level design.
    /// Instances that point to original artwork are LOCAL ONLY (git-excluded).
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/World/Dual Grid Tile Set", fileName = "DualGridTileSet")]
    public sealed class DualGridTileSet : ScriptableObject
    {
        [SerializeField] private TileBase[] terrain = new TileBase[16];
        [SerializeField] private TileBase[] shore = new TileBase[16];
        [SerializeField] private TileBase water;

        public TileBase Water => water;

        public TileBase Terrain(int mask) => terrain != null && mask >= 0 && mask < terrain.Length ? terrain[mask] : null;

        public TileBase Shore(int mask)
        {
            if (mask == 0) return Terrain(DualGridMask.All);
            if (mask == DualGridMask.All) return water;
            return shore != null && mask > 0 && mask < shore.Length ? shore[mask] : null;
        }

        public TileBase For(DualGridChoice choice)
        {
            switch (choice.Kind)
            {
                case DualGridKind.Terrain: return Terrain(choice.Mask);
                case DualGridKind.Shore: return Shore(choice.Mask);
                case DualGridKind.Water: return water;
                default: return null;
            }
        }

        /// <summary>Lists missing tiles (5/10 shore are expected to be empty).</summary>
        public void Validate(List<string> problems)
        {
            for (int i = 0; i < 16; i++)
                if (Terrain(i) == null) problems.Add($"{name}: terrain mask {i} missing.");
            for (int i = 1; i < 15; i++)
                if (!DualGridMask.IsDiagonal(i) && Shore(i) == null) problems.Add($"{name}: shore mask {i} missing.");
            if (water == null) problems.Add($"{name}: water tile missing.");
        }
    }
}

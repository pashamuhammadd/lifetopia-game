using System.Collections.Generic;
using Lifetopia.Items;
using UnityEngine;

namespace Lifetopia.Housing
{
    /// <summary>Facing of a placed piece. North = back toward the upper wall; West = back toward the left wall.</summary>
    public enum FurnitureOrientation
    {
        North = 0,
        West = 1,
    }

    /// <summary>
    /// Placement layer. Floor items (rugs) never block movement and may lie under Object items;
    /// two items on the same layer never overlap.
    /// </summary>
    public enum FurnitureLayer
    {
        Object = 0,
        Floor = 1,
    }

    /// <summary>
    /// Authored, art-free placement rules for one furniture item (shared by the public and local
    /// variants). Sprites live in <see cref="HousingVisualSet"/>, never here.
    /// Footprint is in logical grid cells for the North orientation; West swaps width and depth.
    /// Only orientations with a validated directional sprite may be enabled (no flipping or rotating
    /// perspective art).
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Housing/Furniture Definition", fileName = "furniture_new")]
    public sealed class FurnitureDefinition : ScriptableObject
    {
        [SerializeField] private ItemDefinition item;
        [Tooltip("Width (x, east) and depth (y, north) in cells for the North orientation.")]
        [SerializeField] private Vector2Int footprint = Vector2Int.one;
        [Tooltip("Enable only when a validated West sprite exists.")]
        [SerializeField] private bool supportsWest;
        [SerializeField] private FurnitureLayer layer = FurnitureLayer.Object;
        [Tooltip("Solid furniture blocks the player and the path to the exit. Rugs do not.")]
        [SerializeField] private bool blocksMovement = true;

        public ItemDefinition Item => item;
        public string ItemId => item != null ? item.ItemId : "";
        public FurnitureLayer Layer => layer;
        public bool BlocksMovement => blocksMovement && layer == FurnitureLayer.Object;
        public bool SupportsRotation => supportsWest;

        public bool Supports(FurnitureOrientation orientation) =>
            orientation == FurnitureOrientation.North || (orientation == FurnitureOrientation.West && supportsWest);

        /// <summary>Footprint in cells for <paramref name="orientation"/> (x = east, y = north).</summary>
        public Vector2Int Footprint(FurnitureOrientation orientation)
        {
            Vector2Int f = Vector2Int.Max(footprint, Vector2Int.one);
            return orientation == FurnitureOrientation.West ? new Vector2Int(f.y, f.x) : f;
        }

        public void Validate(List<string> problems)
        {
            if (item == null) { problems.Add($"{name}: no item."); return; }
            if (item.Category != ItemCategory.Furniture) problems.Add($"{name}: item '{item.ItemId}' is not Furniture.");
            if (footprint.x < 1 || footprint.y < 1) problems.Add($"{name}: footprint must be at least 1x1.");
        }

        private void OnValidate() => footprint = Vector2Int.Max(footprint, Vector2Int.one);

        /// <summary>In-memory definition for tests.</summary>
        internal static FurnitureDefinition CreateRuntime(ItemDefinition item, Vector2Int footprint, bool supportsWest = false,
            FurnitureLayer layer = FurnitureLayer.Object, bool blocks = true)
        {
            var d = CreateInstance<FurnitureDefinition>();
            d.item = item;
            d.footprint = footprint;
            d.supportsWest = supportsWest;
            d.layer = layer;
            d.blocksMovement = blocks;
            d.name = item != null ? item.ItemId : "furniture_test";
            return d;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.Housing
{
    /// <summary>
    /// Sprites for the house interior and Decorating Mode. Instances pointing to original artwork are
    /// LOCAL ONLY (git-excluded folder) and referenced only from local scenes; the public build passes
    /// none and the interior draws generated placeholders. Holds no gameplay data: footprints and rules
    /// live in <see cref="FurnitureDefinition"/>. Sprite pivots come from the texture import settings.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Housing/Housing Visual Set", fileName = "HousingVisualSet")]
    public sealed class HousingVisualSet : ScriptableObject
    {
        [Serializable]
        public struct FurnitureSprite
        {
            public string itemId;
            public FurnitureOrientation orientation;
            public Sprite sprite;
            [Tooltip("Uniform visual scale (same factor on X and Y, so proportions stay). 0 or 1 = unchanged. " +
                     "Scales around the sprite pivot (the footprint anchor): PPU, pivot, footprint and collider are unchanged.")]
            [Min(0f)] public float visualScale;
        }

        [Header("Floor (64x64, picked by a fixed pattern)")]
        public Sprite[] floorTiles = Array.Empty<Sprite>();

        [Header("Walls")]
        public Sprite backWallCenter;
        public Sprite backWallLeftCap;
        public Sprite backWallRightCap;
        public Sprite sideWallLeft;
        public Sprite sideWallRight;
        public Sprite frontWallLow;
        public Sprite frontWallLeftEnd;
        public Sprite frontWallRightEnd;

        [Header("Openings")]
        public Sprite windowSingle;
        public Sprite windowDouble;

        [Header("Furniture (one entry per item and orientation)")]
        [SerializeField] private List<FurnitureSprite> furniture = new List<FurnitureSprite>();

        [Header("Decorating UI icons")]
        public Sprite iconDecorate;
        public Sprite iconMove;
        public Sprite iconRotate;
        public Sprite iconStore;
        public Sprite iconPlace;
        public Sprite iconCancel;
        public Sprite iconGrid;
        public Sprite iconExit;

        [Header("Interior background (outside the room)")]
        public Color background = new Color(0.20f, 0.14f, 0.10f, 1f);

        public IReadOnlyList<FurnitureSprite> Furniture => furniture;

        public Sprite GetFurniture(string itemId, FurnitureOrientation orientation) =>
            TryGetFurniture(itemId, orientation, out Sprite s, out _) ? s : null;

        /// <summary>Sprite plus its calibrated uniform visual scale (1 when not set).</summary>
        public bool TryGetFurniture(string itemId, FurnitureOrientation orientation, out Sprite sprite, out float scale)
        {
            for (int i = 0; i < furniture.Count; i++)
            {
                if (furniture[i].itemId != itemId || furniture[i].orientation != orientation) continue;
                sprite = furniture[i].sprite;
                scale = furniture[i].visualScale > 0f ? furniture[i].visualScale : 1f;
                return sprite != null;
            }
            sprite = null;
            scale = 1f;
            return false;
        }
    }
}

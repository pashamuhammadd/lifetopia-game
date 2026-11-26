using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.Farming
{
    /// <summary>
    /// Sprites for farm plots: soil states, crop growth stages and small status indicators.
    /// The asset that holds the real artwork is LOCAL ONLY (git-excluded) and is assigned in the local
    /// scene. Any sprite left empty falls back to a generated placeholder shape
    /// (<see cref="FarmPlaceholderSprites"/>), so the public scene works without artwork.
    /// Visual mapping: Planted = seeded; Watered = sprout, then young from <see cref="YoungAtFraction"/>
    /// of the grow time; Ready = ready.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Farming/Farm Visual Set", fileName = "FarmVisualSet")]
    public sealed class FarmVisualSet : ScriptableObject
    {
        [Serializable]
        public struct CropStageSprites
        {
            public string cropId;
            public Sprite seeded;
            public Sprite sprout;
            public Sprite young;
            public Sprite ready;
            [Tooltip("Extra scale per stage, multiplied with the sprite's own size (sprites already grow per stage). " +
                     "Tune so the visible plant is about: seed 10-15 %, sprout 25-35 %, young 60-75 %, ready 100 % of the ready plant. " +
                     "0 or less = 1. The base (pivot) stays on the plot centre.")]
            public float seededScale;
            public float sproutScale;
            public float youngScale;
            public float readyScale;
        }

        [Header("Soil (64 x 64, center pivot)")]
        [SerializeField] private Sprite soilUnworked;
        [SerializeField] private Sprite soilTilledDry;
        [SerializeField] private Sprite soilTilledWet;

        [Header("Crops (128 x 128, pivot 0.5, 0.125)")]
        [SerializeField] private List<CropStageSprites> crops = new List<CropStageSprites>();
        [Tooltip("Growth fraction (0..1) at which a watered crop switches from sprout to young.")]
        [Range(0.05f, 0.95f)] [SerializeField] private float youngAtFraction = 0.5f;

        [Header("Status indicators (shown above the plot)")]
        [SerializeField] private Sprite needsWaterIndicator;
        [SerializeField] private Sprite readyIndicator;
        [SerializeField, Min(0.1f)] private float indicatorScale = 0.4f;

        public float YoungAtFraction => youngAtFraction;
        public float IndicatorScale => indicatorScale;

        /// <summary>Returns the authored sprite for a soil state, or null (caller uses a placeholder).</summary>
        public Sprite Soil(bool tilled, bool wet) => !tilled ? soilUnworked : wet ? soilTilledWet : soilTilledDry;

        public Sprite Crop(string cropId, FarmPlotStage stage, float growthFraction)
        {
            for (int i = 0; i < crops.Count; i++)
            {
                if (crops[i].cropId != cropId) continue;
                return Pick(crops[i], stage, growthFraction, youngAtFraction);
            }
            return null;
        }

        /// <summary>Transform scale for the crop renderer at this stage (1 when not configured).</summary>
        public float CropScale(string cropId, FarmPlotStage stage, float growthFraction)
        {
            for (int i = 0; i < crops.Count; i++)
            {
                if (crops[i].cropId != cropId) continue;
                CropStageSprites c = crops[i];
                switch (VisualStageIndex(stage, growthFraction, youngAtFraction))
                {
                    case 0: return Positive(c.seededScale);
                    case 1: return Positive(c.sproutScale);
                    case 2: return Positive(c.youngScale);
                    case 3: return Positive(c.readyScale);
                }
            }
            return 1f;
        }

        private static float Positive(float s) => s > 0f ? s : 1f;

        public Sprite NeedsWaterIndicator => needsWaterIndicator;
        public Sprite ReadyIndicator => readyIndicator;

        /// <summary>Growth stage index used for visuals: 0 seeded, 1 sprout, 2 young, 3 ready, -1 none.</summary>
        public static int VisualStageIndex(FarmPlotStage stage, float growthFraction, float youngAt)
        {
            switch (stage)
            {
                case FarmPlotStage.Planted: return 0;
                case FarmPlotStage.Watered: return growthFraction < youngAt ? 1 : 2;
                case FarmPlotStage.Ready: return 3;
                default: return -1;
            }
        }

        private static Sprite Pick(CropStageSprites set, FarmPlotStage stage, float fraction, float youngAt)
        {
            switch (VisualStageIndex(stage, fraction, youngAt))
            {
                case 0: return set.seeded;
                case 1: return set.sprout;
                case 2: return set.young;
                case 3: return set.ready;
                default: return null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Lifetopia.Items;
using UnityEngine;

namespace Lifetopia.Fishing
{
    /// <summary>Plain tuning values of one fishing attempt (copied out of <see cref="FishingConfig"/>; used by tests).</summary>
    [Serializable]
    public struct FishingSettings
    {
        [Tooltip("Seconds for the cast animation before waiting starts.")]
        public float castSeconds;
        [Tooltip("Random wait for the bite, between min and max seconds.")]
        public float biteDelayMin;
        public float biteDelayMax;
        [Tooltip("Hard limit for the whole attempt (GDD: 8 s). The timing window is what is left after cast + bite.")]
        public float maxAttemptSeconds;
        [Tooltip("Full back-and-forth sweeps of the marker per second.")]
        public float markerCyclesPerSecond;
        [Tooltip("Target zone centre on the bar (0 = left, 1 = right).")]
        [Range(0f, 1f)] public float zoneCenter;
        [Tooltip("Target zone width as a fraction of the bar (proposal 0.25-0.30).")]
        [Range(0.05f, 0.9f)] public float zoneWidth;
        [Tooltip("Longest frame step the mini game accepts (prevents a catch-up burst after a hitch).")]
        public float maxStepSeconds;

        /// <summary>Initial tuning proposal (M3.4): cast 0.3 s, bite 1.2-2.0 s, 8 s limit, zone 28 %.</summary>
        public static FishingSettings Default => new FishingSettings
        {
            castSeconds = 0.3f,
            biteDelayMin = 1.2f,
            biteDelayMax = 2.0f,
            maxAttemptSeconds = 8f,
            markerCyclesPerSecond = 0.75f,
            zoneCenter = 0.62f,
            zoneWidth = 0.28f,
            maxStepSeconds = 0.1f,
        };

        public float ZoneMin => Mathf.Clamp01(zoneCenter - zoneWidth * 0.5f);
        public float ZoneMax => Mathf.Clamp01(zoneCenter + zoneWidth * 0.5f);

        public bool Validate(out string error)
        {
            error = null;
            if (!(castSeconds >= 0f)) error = "castSeconds must be >= 0";
            else if (!(biteDelayMin > 0f) || biteDelayMax < biteDelayMin) error = "bite delay must be > 0 and min <= max";
            else if (!(maxAttemptSeconds > castSeconds + biteDelayMax + 0.5f)) error = "maxAttemptSeconds must leave at least 0.5 s for the timing window";
            else if (!(markerCyclesPerSecond > 0f)) error = "markerCyclesPerSecond must be > 0";
            else if (!(zoneWidth > 0f) || zoneWidth >= 1f) error = "zoneWidth must be between 0 and 1";
            else if (zoneCenter < 0f || zoneCenter > 1f) error = "zoneCenter must be between 0 and 1";
            else if (!(maxStepSeconds > 0f)) error = "maxStepSeconds must be > 0";
            return error == null;
        }
    }

    /// <summary>
    /// Fishing rules for a scene (read-only at runtime): the rod and fish items (existing IDs
    /// <c>tool_fishing_rod</c>, <c>fish_reef</c>), the catch size and the mini-game tuning.
    /// No bait, cost, rarity or random rewards.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Fishing/Fishing Config", fileName = "FishingConfig")]
    public sealed class FishingConfig : ScriptableObject
    {
        [SerializeField] private ItemDefinition rodItem;
        [SerializeField] private ItemDefinition fishItem;
        [Min(1)] [SerializeField] private int catchQuantity = 1;
        [SerializeField] private FishingSettings settings = FishingSettings.Default;
        [Tooltip("Seconds the result (caught / missed) stays on screen before the panel closes.")]
        [Min(0.2f)] [SerializeField] private float resultSeconds = 1.4f;

        public ItemDefinition RodItem => rodItem;
        public ItemDefinition FishItem => fishItem;
        public int CatchQuantity => Mathf.Max(1, catchQuantity);
        public FishingSettings Settings => settings;
        public float ResultSeconds => Mathf.Max(0.2f, resultSeconds);

        public void Validate(List<string> problems)
        {
            if (rodItem == null) problems.Add("FishingConfig: rod item missing.");
            if (fishItem == null) problems.Add("FishingConfig: fish item missing.");
            if (!settings.Validate(out string error)) problems.Add("FishingConfig: " + error + ".");
        }

        private void OnValidate()
        {
            catchQuantity = Mathf.Max(1, catchQuantity);
            if (!settings.Validate(out string error)) Debug.LogWarning("[Lifetopia] FishingConfig: " + error, this);
        }

        /// <summary>Creates an in-memory config. For tests only.</summary>
        internal static FishingConfig CreateRuntime(ItemDefinition rod, ItemDefinition fish, FishingSettings s, int quantity = 1)
        {
            var c = CreateInstance<FishingConfig>();
            c.rodItem = rod;
            c.fishItem = fish;
            c.settings = s;
            c.catchQuantity = quantity;
            return c;
        }
    }
}

using System.Collections.Generic;
using Lifetopia.Items;
using UnityEngine;

namespace Lifetopia.Farming
{
    /// <summary>
    /// Farming rules for a scene: which items act as hoe and watering can, which crops exist,
    /// and what happens to the soil after harvesting. Read-only at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Farming/Farming Config", fileName = "FarmingConfig")]
    public sealed class FarmingConfig : ScriptableObject
    {
        [SerializeField] private ItemDefinition hoeItem;
        [SerializeField] private ItemDefinition wateringCanItem;
        [SerializeField] private List<CropDefinition> crops = new List<CropDefinition>();
        [Tooltip("True: after harvesting the plot stays tilled and can be planted again at once. " +
                 "False: the soil must be tilled with the hoe again.")]
        [SerializeField] private bool keepTilledAfterHarvest = true;

        public ItemDefinition HoeItem => hoeItem;
        public ItemDefinition WateringCanItem => wateringCanItem;
        public IReadOnlyList<CropDefinition> Crops => crops;
        public bool KeepTilledAfterHarvest => keepTilledAfterHarvest;

        /// <summary>Crop grown from <paramref name="seed"/>, or null when the item is not a seed.</summary>
        public CropDefinition CropForSeed(ItemDefinition seed)
        {
            if (seed == null) return null;
            for (int i = 0; i < crops.Count; i++)
                if (crops[i] != null && crops[i].SeedItem == seed) return crops[i];
            return null;
        }

        public CropDefinition CropById(string cropId)
        {
            if (string.IsNullOrEmpty(cropId)) return null;
            for (int i = 0; i < crops.Count; i++)
                if (crops[i] != null && crops[i].CropId == cropId) return crops[i];
            return null;
        }

        /// <summary>Adds a human-readable line to <paramref name="problems"/> for each data error.</summary>
        public void Validate(List<string> problems)
        {
            if (hoeItem == null) problems.Add("FarmingConfig: hoe item missing.");
            if (wateringCanItem == null) problems.Add("FarmingConfig: watering can item missing.");
            var ids = new HashSet<string>();
            var seeds = new HashSet<ItemDefinition>();
            foreach (CropDefinition crop in crops)
            {
                if (crop == null) { problems.Add("FarmingConfig: empty crop entry."); continue; }
                if (!crop.IsValid) problems.Add($"{crop.name}: invalid crop (ID, seed or harvest item).");
                if (!ids.Add(crop.CropId)) problems.Add($"{crop.name}: duplicate crop ID '{crop.CropId}'.");
                if (crop.SeedItem != null && !seeds.Add(crop.SeedItem)) problems.Add($"{crop.name}: seed used by two crops.");
            }
        }

        /// <summary>Creates an in-memory config. For tests only.</summary>
        internal static FarmingConfig CreateRuntime(ItemDefinition hoe, ItemDefinition can, bool keepTilled, params CropDefinition[] cropList)
        {
            var config = CreateInstance<FarmingConfig>();
            config.hoeItem = hoe;
            config.wateringCanItem = can;
            config.keepTilledAfterHarvest = keepTilled;
            config.crops = new List<CropDefinition>(cropList);
            return config;
        }
    }
}

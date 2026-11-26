using Lifetopia.Items;
using UnityEngine;

namespace Lifetopia.Farming
{
    /// <summary>
    /// Authored rules for one crop (read-only at runtime). Gameplay only: no sprites here, so the
    /// tracked asset never references local artwork. Visuals come from <see cref="FarmVisualSet"/>.
    /// GDD: plant a seed, water once, grows for 120 s of active play, harvest 1 crop. No wilting.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Farming/Crop Definition", fileName = "crop_new")]
    public sealed class CropDefinition : ScriptableObject
    {
        [Tooltip("Stable lower_snake_case ID used by saves and visual sets, e.g. tropical_fruit. Never rename after release.")]
        [SerializeField] private string cropId = "";
        [Tooltip("Item consumed (1) when planting.")]
        [SerializeField] private ItemDefinition seedItem;
        [Tooltip("Item given when harvesting.")]
        [SerializeField] private ItemDefinition harvestItem;
        [Min(1)] [SerializeField] private int harvestQuantity = 1;
        [Tooltip("Seconds of active play (clock running) from watering until ready.")]
        [Min(1f)] [SerializeField] private float growSeconds = 120f;

        public string CropId => cropId;
        public ItemDefinition SeedItem => seedItem;
        public ItemDefinition HarvestItem => harvestItem;
        public int HarvestQuantity => Mathf.Max(1, harvestQuantity);
        public double GrowSeconds => Mathf.Max(1f, growSeconds);
        public string DisplayName => harvestItem != null ? harvestItem.DisplayName : cropId;

        public bool IsValid => ItemDefinition.IsValidId(cropId) && seedItem != null && harvestItem != null;

        private void OnValidate()
        {
            harvestQuantity = Mathf.Max(1, harvestQuantity);
            growSeconds = Mathf.Max(1f, growSeconds);
        }

        /// <summary>Creates an in-memory crop. For tests only.</summary>
        internal static CropDefinition CreateRuntime(string id, ItemDefinition seed, ItemDefinition harvest, int quantity = 1, float seconds = 120f)
        {
            var crop = CreateInstance<CropDefinition>();
            crop.cropId = id;
            crop.seedItem = seed;
            crop.harvestItem = harvest;
            crop.harvestQuantity = quantity;
            crop.growSeconds = seconds;
            crop.name = id;
            return crop;
        }
    }
}

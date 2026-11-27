using System.Collections.Generic;
using Lifetopia.Items;
using UnityEngine;

namespace Lifetopia.Housing
{
    /// <summary>
    /// Every furniture item that can be placed in a house, with its placement rules. Art-free and
    /// shared by both build variants. An item that is not listed here can never be placed.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Housing/Furniture Catalog", fileName = "FurnitureCatalog")]
    public sealed class FurnitureCatalog : ScriptableObject
    {
        [SerializeField] private List<FurnitureDefinition> furniture = new List<FurnitureDefinition>();

        private Dictionary<string, FurnitureDefinition> _byId;

        public IReadOnlyList<FurnitureDefinition> All => furniture;

        public bool TryGet(string itemId, out FurnitureDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(itemId)) return false;
            if (_byId == null) Build();
            return _byId.TryGetValue(itemId, out definition);
        }

        public bool TryGet(ItemDefinition item, out FurnitureDefinition definition)
        {
            definition = null;
            return item != null && TryGet(item.ItemId, out definition) && definition.Item == item;
        }

        public void Validate(List<string> problems)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < furniture.Count; i++)
            {
                FurnitureDefinition d = furniture[i];
                if (d == null) { problems.Add($"{name}: entry {i} is empty."); continue; }
                d.Validate(problems);
                if (d.Item != null && !seen.Add(d.ItemId)) problems.Add($"{name}: '{d.ItemId}' listed twice.");
            }
        }

        private void Build()
        {
            _byId = new Dictionary<string, FurnitureDefinition>();
            foreach (FurnitureDefinition d in furniture)
                if (d != null && d.Item != null && !_byId.ContainsKey(d.ItemId)) _byId.Add(d.ItemId, d);
        }

        private void OnEnable() => _byId = null;
        private void OnValidate() => _byId = null;

        internal static FurnitureCatalog CreateRuntime(params FurnitureDefinition[] list)
        {
            var c = CreateInstance<FurnitureCatalog>();
            c.furniture = new List<FurnitureDefinition>(list);
            c.name = "FurnitureCatalog_Test";
            return c;
        }
    }
}

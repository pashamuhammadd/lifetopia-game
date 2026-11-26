using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.Items
{
    /// <summary>Looks up item definitions by stable ID (used when loading saves).</summary>
    public interface IItemCatalog
    {
        bool TryGet(string itemId, out ItemDefinition item);
    }

    /// <summary>
    /// The list of every item that exists in the game. Saves store only item IDs; this asset turns
    /// them back into definitions. Add each new ItemDefinition here.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Items/Item Database", fileName = "ItemDatabase")]
    public sealed class ItemDatabase : ScriptableObject, IItemCatalog
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        private Dictionary<string, ItemDefinition> _byId;

        public IReadOnlyList<ItemDefinition> Items => items;

        public bool TryGet(string itemId, out ItemDefinition item)
        {
            item = null;
            if (string.IsNullOrEmpty(itemId)) return false;
            if (_byId == null) BuildLookup();
            return _byId.TryGetValue(itemId, out item);
        }

        /// <summary>Adds a human-readable line to <paramref name="problems"/> for each data error.</summary>
        public void Validate(List<string> problems)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition def = items[i];
                if (def == null) { problems.Add($"Entry {i} is empty."); continue; }
                if (!ItemDefinition.IsValidId(def.ItemId)) problems.Add($"{def.name}: invalid item ID '{def.ItemId}'.");
                else if (!seen.Add(def.ItemId)) problems.Add($"{def.name}: duplicate item ID '{def.ItemId}'.");
                if (string.IsNullOrWhiteSpace(def.DisplayName)) problems.Add($"{def.name}: missing display name.");
                if (string.IsNullOrWhiteSpace(def.Description)) problems.Add($"{def.name}: missing description.");
            }
        }

        private void BuildLookup()
        {
            _byId = new Dictionary<string, ItemDefinition>();
            foreach (ItemDefinition def in items)
            {
                if (def == null || string.IsNullOrEmpty(def.ItemId)) continue;
                if (!_byId.ContainsKey(def.ItemId)) _byId.Add(def.ItemId, def);
            }
        }

        private void OnEnable() => _byId = null;
        private void OnValidate() => _byId = null;
    }
}

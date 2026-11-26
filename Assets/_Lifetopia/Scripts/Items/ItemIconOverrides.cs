using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.Items
{
    /// <summary>
    /// Icon sprites by stable item ID. Instances that point to local-only artwork must live in a
    /// git-excluded folder and be referenced only from local scenes (via <c>LocalArtBindings</c>).
    /// Does not create items or change any item data.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Items/Item Icon Overrides", fileName = "ItemIconOverrides")]
    public sealed class ItemIconOverrides : ScriptableObject, IItemIconSource
    {
        [Serializable]
        public struct Entry
        {
            public string itemId;
            public Sprite icon;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        public Sprite GetIcon(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].itemId == itemId) return entries[i].icon;
            return null;
        }

        /// <summary>Lists IDs that are not in <paramref name="catalog"/> (typos would silently show placeholders).</summary>
        public void Validate(IItemCatalog catalog, List<string> problems)
        {
            foreach (Entry e in entries)
            {
                if (string.IsNullOrEmpty(e.itemId)) { problems.Add($"{name}: entry without item ID."); continue; }
                if (catalog != null && !catalog.TryGet(e.itemId, out _)) problems.Add($"{name}: unknown item ID '{e.itemId}'.");
                if (e.icon == null) problems.Add($"{name}: '{e.itemId}' has no sprite.");
            }
        }

        /// <summary>Creates an in-memory set. For tests only.</summary>
        internal static ItemIconOverrides CreateRuntime(params Entry[] list)
        {
            var o = CreateInstance<ItemIconOverrides>();
            o.entries = new List<Entry>(list);
            return o;
        }
    }
}

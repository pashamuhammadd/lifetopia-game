using System;
using System.Collections.Generic;
using Lifetopia.Items;
using UnityEngine;

namespace Lifetopia.InventorySystem
{
    /// <summary>
    /// Authored starting kit for a brand-new local game (GDD: 100 coins, 3 seeds, basic tools).
    /// Applied once per new profile by <see cref="PlayerProfile.GrantStarterKit"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Items/Starter Profile", fileName = "StarterProfile")]
    public sealed class StarterProfile : ScriptableObject
    {
        [Serializable]
        public struct StarterItem
        {
            public ItemDefinition item;
            [Min(1)] public int quantity;
            [Tooltip("Hotbar slot 0-4 to place this item in, or -1 for none.")]
            public int hotbarSlot;
        }

        [Min(0)] [SerializeField] private int startingCoins = 100;
        [SerializeField] private List<StarterItem> items = new List<StarterItem>();

        public int StartingCoins => Mathf.Max(0, startingCoins);
        public IReadOnlyList<StarterItem> Items => items;

        internal static StarterProfile CreateRuntime(int coins, params StarterItem[] starterItems)
        {
            var kit = CreateInstance<StarterProfile>();
            kit.startingCoins = coins;
            kit.items = new List<StarterItem>(starterItems);
            return kit;
        }
    }
}

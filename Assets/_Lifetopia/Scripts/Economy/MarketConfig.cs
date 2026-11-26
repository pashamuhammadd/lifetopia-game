using System.Collections.Generic;
using Lifetopia.Items;
using UnityEngine;

namespace Lifetopia.Economy
{
    /// <summary>
    /// Authored shop definition: what a merchant sells to the player (Buy tab) and buys from the
    /// player (Sell tab). Unit prices are NOT stored here: they come from each item's
    /// <see cref="ItemDefinition.BuyPrice"/> / <see cref="ItemDefinition.SellPrice"/>, so one value per
    /// item drives every shop and the bag details (single source of truth, editable in the Inspector).
    /// Read-only at runtime. Unlimited stock, no opening hours, no fees (M3.5).
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Economy/Market Config", fileName = "MarketConfig")]
    public sealed class MarketConfig : ScriptableObject
    {
        /// <summary>Hard upper bound for one transaction (keeps totals far below int overflow).</summary>
        public const int AbsoluteMaxQuantity = 999;

        [SerializeField] private string shopName = "Rio's Market";
        [SerializeField] private string greeting = "Welcome, traveler!";
        [Tooltip("Items the player can buy here. Each needs a Buy Price > 0 on its item asset.")]
        [SerializeField] private List<ItemDefinition> buyItems = new List<ItemDefinition>();
        [Tooltip("Items the player can sell here. Each needs a Sell Price > 0 and must not be a tool.")]
        [SerializeField] private List<ItemDefinition> sellItems = new List<ItemDefinition>();
        [Tooltip("Largest quantity in one purchase or sale.")]
        [Range(1, AbsoluteMaxQuantity)]
        [SerializeField] private int maxQuantityPerTrade = 99;

        public string ShopName => shopName;
        public string Greeting => greeting;
        public IReadOnlyList<ItemDefinition> BuyItems => buyItems;
        public IReadOnlyList<ItemDefinition> SellItems => sellItems;
        public int MaxQuantityPerTrade => Mathf.Clamp(maxQuantityPerTrade, 1, AbsoluteMaxQuantity);

        public IReadOnlyList<ItemDefinition> ItemsFor(TradeMode mode) => mode == TradeMode.Buy ? buyItems : sellItems;

        /// <summary>Price per unit in <paramref name="mode"/>, or 0 when the item is not traded that way here.</summary>
        public int UnitPrice(TradeMode mode, ItemDefinition item)
        {
            if (item == null || !Contains(mode, item)) return 0;
            if (mode == TradeMode.Sell && item.Category == ItemCategory.Tool) return 0;
            return mode == TradeMode.Buy ? item.BuyPrice : item.SellPrice;
        }

        public bool Contains(TradeMode mode, ItemDefinition item)
        {
            if (item == null) return false;
            IReadOnlyList<ItemDefinition> list = ItemsFor(mode);
            for (int i = 0; i < list.Count; i++) if (list[i] == item) return true;
            return false;
        }

        /// <summary>Lists configuration mistakes (empty = valid). Invalid entries are never tradable.</summary>
        public void Validate(List<string> problems)
        {
            Check(TradeMode.Buy, buyItems, problems);
            Check(TradeMode.Sell, sellItems, problems);
        }

        private void Check(TradeMode mode, List<ItemDefinition> list, List<string> problems)
        {
            var seen = new HashSet<ItemDefinition>();
            for (int i = 0; i < list.Count; i++)
            {
                ItemDefinition item = list[i];
                if (item == null) { problems.Add($"{name}: {mode} entry {i} is empty."); continue; }
                if (!seen.Add(item)) problems.Add($"{name}: {mode} lists '{item.ItemId}' twice.");
                if (mode == TradeMode.Sell && item.Category == ItemCategory.Tool) problems.Add($"{name}: tools cannot be sold ('{item.ItemId}').");
                int price = mode == TradeMode.Buy ? item.BuyPrice : item.SellPrice;
                if (price <= 0) problems.Add($"{name}: '{item.ItemId}' has no {(mode == TradeMode.Buy ? "Buy" : "Sell")} Price.");
            }
        }

        /// <summary>In-memory config for tests.</summary>
        internal static MarketConfig CreateRuntime(IEnumerable<ItemDefinition> buy, IEnumerable<ItemDefinition> sell, int maxPerTrade = 99)
        {
            var c = CreateInstance<MarketConfig>();
            c.buyItems = new List<ItemDefinition>(buy);
            c.sellItems = new List<ItemDefinition>(sell);
            c.maxQuantityPerTrade = maxPerTrade;
            c.name = "MarketConfig_Test";
            return c;
        }
    }
}

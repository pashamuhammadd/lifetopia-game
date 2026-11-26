using UnityEngine;

namespace Lifetopia.Items
{
    /// <summary>
    /// Authored, read-only description of one kind of item (e.g. "Tropical Seeds").
    /// One asset per item type; inventories store references to it plus a quantity.
    /// Never change values at runtime: the asset is shared by every stack of that item.
    /// The <see cref="ItemId"/> is the stable key used by saves and starter kits; do not rename it
    /// after release.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Items/Item Definition", fileName = "item_new")]
    public sealed class ItemDefinition : ScriptableObject
    {
        /// <summary>Hard cap for any stack in the game (GDD: 99 per slot).</summary>
        public const int MaxStackLimit = 99;

        [Tooltip("Stable lower_snake_case ID used by saves, e.g. seed_tropical. Never rename after release.")]
        [SerializeField] private string itemId = "";
        [SerializeField] private string displayName = "";
        [TextArea(2, 4)]
        [SerializeField] private string description = "";
        [SerializeField] private ItemCategory category = ItemCategory.Material;

        [Tooltip("Optional. Leave empty to show a coloured placeholder tile with a short label.")]
        [SerializeField] private Sprite icon;

        [SerializeField] private bool stackable = true;
        [Range(1, MaxStackLimit)]
        [SerializeField] private int maxStackSize = MaxStackLimit;

        [Tooltip("Coins received when sold. 0 = cannot be sold.")]
        [Min(0)] [SerializeField] private int sellPrice;
        [Tooltip("Coins paid in a shop. 0 = not sold in shops.")]
        [Min(0)] [SerializeField] private int buyPrice;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemCategory Category => category;
        public Sprite Icon => icon;
        public bool Stackable => stackable;

        /// <summary>Effective stack size: 1 for non-stackable items, otherwise 1..99.</summary>
        public int MaxStack => stackable ? Mathf.Clamp(maxStackSize, 1, MaxStackLimit) : 1;

        public int SellPrice => Mathf.Max(0, sellPrice);
        public int BuyPrice => Mathf.Max(0, buyPrice);
        public bool CanBeSold => SellPrice > 0;
        public bool SoldInShops => BuyPrice > 0;

        /// <summary>Tools are permanent equipment and are never used up when used.</summary>
        public bool ConsumedOnUse => category != ItemCategory.Tool;

        /// <summary>True for lower_snake_case IDs: starts with a-z, then a-z, 0-9 or '_'.</summary>
        public static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 64) return false;
            if (id[0] < 'a' || id[0] > 'z') return false;
            for (int i = 1; i < id.Length; i++)
            {
                char c = id[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!ok) return false;
            }
            return true;
        }

        private void OnValidate()
        {
            maxStackSize = Mathf.Clamp(maxStackSize, 1, MaxStackLimit);
            if (!stackable) maxStackSize = 1;
            sellPrice = Mathf.Max(0, sellPrice);
            buyPrice = Mathf.Max(0, buyPrice);
        }

        /// <summary>Creates an in-memory definition. For tests only; real items are assets.</summary>
        internal static ItemDefinition CreateRuntime(string id, string name, ItemCategory category,
            bool stackable, int maxStack = MaxStackLimit, int sellPrice = 0, int buyPrice = 0)
        {
            var def = CreateInstance<ItemDefinition>();
            def.itemId = id;
            def.displayName = name;
            def.category = category;
            def.stackable = stackable;
            def.maxStackSize = maxStack;
            def.sellPrice = sellPrice;
            def.buyPrice = buyPrice;
            def.name = id;
            def.OnValidate();
            return def;
        }
    }
}

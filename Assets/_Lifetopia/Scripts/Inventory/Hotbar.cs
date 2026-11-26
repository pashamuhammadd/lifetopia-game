using System;
using Lifetopia.Items;

namespace Lifetopia.InventorySystem
{
    /// <summary>
    /// Quick-select bar of item references (runtime state). It never stores quantities: counts are
    /// always read from the <see cref="Inventory"/>, so the two can never disagree.
    /// - An item can be on at most one hotbar slot; assigning it elsewhere moves it.
    /// - Only items currently in the inventory can be assigned.
    /// - When an item's count reaches 0 the slot KEEPS its reference (shown dimmed with 0) and becomes
    ///   usable again automatically when the item is obtained again. Only <see cref="Assign"/> (another
    ///   item) or <see cref="ClearSlot"/> changes it. Use <see cref="IsAvailable"/> before using an item.
    /// Moving items between inventory slots does not affect the hotbar.
    /// </summary>
    public sealed class Hotbar : IDisposable
    {
        public const int DefaultSize = 5;

        private readonly Inventory _inventory;
        private readonly ItemDefinition[] _items;

        /// <summary>Raised when the item reference of a slot changes (index).</summary>
        public event Action<int> SlotChanged;

        /// <summary>Raised when the selected slot changes (new index).</summary>
        public event Action<int> SelectionChanged;

        public Hotbar(Inventory inventory, int size = DefaultSize)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _items = new ItemDefinition[size];
        }

        public int Size => _items.Length;
        public int SelectedIndex { get; private set; }
        public Inventory Inventory => _inventory;

        public bool IsValidSlot(int index) => index >= 0 && index < _items.Length;

        public ItemDefinition GetItem(int index) => IsValidSlot(index) ? _items[index] : null;

        /// <summary>Quantity shown on the slot, read live from the inventory.</summary>
        public int GetCount(int index)
        {
            ItemDefinition item = GetItem(index);
            return item != null ? _inventory.GetItemCount(item) : 0;
        }

        public ItemDefinition SelectedItem => GetItem(SelectedIndex);

        /// <summary>True when slot <paramref name="index"/> references an item the inventory currently holds (count &gt; 0).</summary>
        public bool IsAvailable(int index) => GetCount(index) > 0;

        /// <summary>The selected item only if at least one is in the inventory; otherwise null (never use a 0-count item).</summary>
        public ItemDefinition SelectedAvailableItem => IsAvailable(SelectedIndex) ? SelectedItem : null;

        public int IndexOf(ItemDefinition item)
        {
            if (item == null) return -1;
            for (int i = 0; i < _items.Length; i++)
                if (_items[i] == item) return i;
            return -1;
        }

        public InventoryResult Assign(int index, ItemDefinition item)
        {
            if (!IsValidSlot(index)) return InventoryResult.InvalidSlot;
            if (item == null) return InventoryResult.InvalidItem;
            if (!_inventory.HasItem(item)) return InventoryResult.ItemNotInInventory;
            if (_items[index] == item) return InventoryResult.Success;

            int previous = IndexOf(item);
            if (previous >= 0)
            {
                _items[previous] = null;
                SlotChanged?.Invoke(previous);
            }
            _items[index] = item;
            SlotChanged?.Invoke(index);
            return InventoryResult.Success;
        }

        /// <summary>
        /// Loading saves only: puts a reference back without requiring the item in the inventory
        /// (a saved slot may point at an item whose count is 0). Rejects duplicates.
        /// </summary>
        internal InventoryResult Restore(int index, ItemDefinition item)
        {
            if (!IsValidSlot(index)) return InventoryResult.InvalidSlot;
            if (item == null) return InventoryResult.InvalidItem;
            int existing = IndexOf(item);
            if (existing >= 0 && existing != index) return InventoryResult.TargetOccupied;
            if (_items[index] == item) return InventoryResult.Success;
            _items[index] = item;
            SlotChanged?.Invoke(index);
            return InventoryResult.Success;
        }

        public InventoryResult ClearSlot(int index)
        {
            if (!IsValidSlot(index)) return InventoryResult.InvalidSlot;
            if (_items[index] == null) return InventoryResult.SlotEmpty;
            _items[index] = null;
            SlotChanged?.Invoke(index);
            return InventoryResult.Success;
        }

        public bool Select(int index)
        {
            if (!IsValidSlot(index)) return false;
            if (SelectedIndex == index) return true;
            SelectedIndex = index;
            SelectionChanged?.Invoke(index);
            return true;
        }

        /// <summary>Kept for API compatibility; the hotbar no longer subscribes to inventory events.</summary>
        public void Dispose() { }
    }
}

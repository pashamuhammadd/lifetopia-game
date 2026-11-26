using System;
using Lifetopia.Items;

namespace Lifetopia.InventorySystem
{
    /// <summary>
    /// Quick-select bar of item references (runtime state). It never stores quantities: counts are
    /// always read from the <see cref="Inventory"/>, so the two can never disagree.
    /// - An item can be on at most one hotbar slot; assigning it elsewhere moves it.
    /// - Only items currently in the inventory can be assigned.
    /// - When an item's count reaches 0 its hotbar slot is cleared automatically.
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
            _inventory.Changed += OnInventoryChanged;
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

        public void Dispose() => _inventory.Changed -= OnInventoryChanged;

        private void OnInventoryChanged()
        {
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i] == null || _inventory.HasItem(_items[i])) continue;
                _items[i] = null;
                SlotChanged?.Invoke(i);
            }
        }
    }
}

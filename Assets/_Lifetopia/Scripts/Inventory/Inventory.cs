using System;
using Lifetopia.Items;

namespace Lifetopia.InventorySystem
{
    /// <summary>One inventory cell: an item definition and how many of it. Empty when Item is null.</summary>
    public readonly struct InventorySlot
    {
        public readonly ItemDefinition Item;
        public readonly int Quantity;

        public InventorySlot(ItemDefinition item, int quantity)
        {
            Item = quantity > 0 ? item : null;
            Quantity = item != null && quantity > 0 ? quantity : 0;
        }

        public bool IsEmpty => Item == null;
        public static readonly InventorySlot Empty = default;
    }

    /// <summary>
    /// Fixed-size bag of item stacks (runtime state, plain C#; no Unity scene dependency).
    /// Rules:
    /// - Quantities are always 1..item.MaxStack (≤ 99); never negative, never above the stack size.
    /// - Every operation is all-or-nothing: on failure nothing changes, so items are never lost or duplicated.
    /// - Non-stackable items (MaxStack 1) occupy one slot each.
    /// - Events fire only after a successful change: SlotChanged per touched slot, then Changed once.
    /// </summary>
    public sealed class Inventory
    {
        public const int DefaultSlotCount = 12;

        private readonly InventorySlot[] _slots;
        private readonly bool[] _dirty;

        /// <summary>Raised for every slot whose content changed (index).</summary>
        public event Action<int> SlotChanged;

        /// <summary>Raised once after any successful change.</summary>
        public event Action Changed;

        public Inventory(int slotCount = DefaultSlotCount)
        {
            if (slotCount <= 0) throw new ArgumentOutOfRangeException(nameof(slotCount));
            _slots = new InventorySlot[slotCount];
            _dirty = new bool[slotCount];
        }

        public int SlotCount => _slots.Length;

        public bool IsValidSlot(int index) => index >= 0 && index < _slots.Length;

        public InventorySlot GetSlot(int index) => IsValidSlot(index) ? _slots[index] : InventorySlot.Empty;

        public int GetItemCount(ItemDefinition item)
        {
            if (item == null) return 0;
            int total = 0;
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i].Item == item) total += _slots[i].Quantity;
            return total;
        }

        public bool HasItem(ItemDefinition item, int quantity = 1) =>
            item != null && quantity > 0 && GetItemCount(item) >= quantity;

        /// <summary>How many more of <paramref name="item"/> fit (partial stacks + empty slots).</summary>
        public int GetFreeCapacity(ItemDefinition item)
        {
            if (item == null) return 0;
            long capacity = 0;
            int max = item.MaxStack;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsEmpty) capacity += max;
                else if (_slots[i].Item == item) capacity += max - _slots[i].Quantity;
            }
            return capacity > int.MaxValue ? int.MaxValue : (int)capacity;
        }

        public InventoryResult CheckAdd(ItemDefinition item, int quantity)
        {
            if (item == null) return InventoryResult.InvalidItem;
            if (quantity <= 0) return InventoryResult.InvalidQuantity;
            return GetFreeCapacity(item) >= quantity ? InventoryResult.Success : InventoryResult.InventoryFull;
        }

        public bool CanAddItem(ItemDefinition item, int quantity) => CheckAdd(item, quantity).Succeeded();

        /// <summary>Adds all of <paramref name="quantity"/> or nothing. Tops up existing stacks first.</summary>
        public InventoryResult AddItem(ItemDefinition item, int quantity)
        {
            InventoryResult check = CheckAdd(item, quantity);
            if (!check.Succeeded()) return check;

            int remaining = quantity;
            int max = item.MaxStack;
            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (_slots[i].Item != item || _slots[i].Quantity >= max) continue;
                int moved = Math.Min(max - _slots[i].Quantity, remaining);
                Set(i, item, _slots[i].Quantity + moved);
                remaining -= moved;
            }
            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (!_slots[i].IsEmpty) continue;
                int moved = Math.Min(max, remaining);
                Set(i, item, moved);
                remaining -= moved;
            }
            Flush();
            return InventoryResult.Success;
        }

        /// <summary>Removes exactly <paramref name="quantity"/> (from the last slots first) or nothing.</summary>
        public InventoryResult RemoveItem(ItemDefinition item, int quantity)
        {
            if (item == null) return InventoryResult.InvalidItem;
            if (quantity <= 0) return InventoryResult.InvalidQuantity;
            if (GetItemCount(item) < quantity) return InventoryResult.NotEnoughItems;

            int remaining = quantity;
            for (int i = _slots.Length - 1; i >= 0 && remaining > 0; i--)
            {
                if (_slots[i].Item != item) continue;
                int taken = Math.Min(_slots[i].Quantity, remaining);
                Set(i, item, _slots[i].Quantity - taken);
                remaining -= taken;
            }
            Flush();
            return InventoryResult.Success;
        }

        /// <summary>Removes <paramref name="quantity"/> from one specific slot, or nothing.</summary>
        public InventoryResult RemoveFromSlot(int index, int quantity)
        {
            if (!IsValidSlot(index)) return InventoryResult.InvalidSlot;
            if (quantity <= 0) return InventoryResult.InvalidQuantity;
            InventorySlot slot = _slots[index];
            if (slot.IsEmpty) return InventoryResult.SlotEmpty;
            if (slot.Quantity < quantity) return InventoryResult.NotEnoughItems;
            Set(index, slot.Item, slot.Quantity - quantity);
            Flush();
            return InventoryResult.Success;
        }

        /// <summary>
        /// Moves a whole stack into an empty slot. If the target holds the same stackable item the
        /// stacks are merged (only when everything fits). Use <see cref="SwapSlot"/> for occupied targets.
        /// </summary>
        public InventoryResult MoveSlot(int from, int to)
        {
            InventoryResult check = CheckPair(from, to);
            if (!check.Succeeded()) return check;
            InventorySlot source = _slots[from];
            InventorySlot target = _slots[to];

            if (target.IsEmpty)
            {
                Set(to, source.Item, source.Quantity);
                Set(from, null, 0);
                Flush();
                return InventoryResult.Success;
            }
            if (target.Item != source.Item) return InventoryResult.TargetOccupied;
            if (!source.Item.Stackable) return InventoryResult.NotStackable;
            if (target.Quantity + source.Quantity > source.Item.MaxStack) return InventoryResult.StackFull;
            return MergeStack(from, to);
        }

        /// <summary>Exchanges two slots (either may be empty).</summary>
        public InventoryResult SwapSlot(int a, int b)
        {
            InventoryResult check = CheckIndices(a, b);
            if (!check.Succeeded()) return check;
            if (_slots[a].IsEmpty && _slots[b].IsEmpty) return InventoryResult.SlotEmpty;
            InventorySlot first = _slots[a];
            InventorySlot second = _slots[b];
            Set(a, second.Item, second.Quantity);
            Set(b, first.Item, first.Quantity);
            Flush();
            return InventoryResult.Success;
        }

        /// <summary>
        /// Moves as much as fits from <paramref name="from"/> onto the same item in <paramref name="to"/>.
        /// Any remainder stays in the source slot.
        /// </summary>
        public InventoryResult MergeStack(int from, int to)
        {
            InventoryResult check = CheckPair(from, to);
            if (!check.Succeeded()) return check;
            InventorySlot source = _slots[from];
            InventorySlot target = _slots[to];
            if (target.IsEmpty) return InventoryResult.SlotEmpty;
            if (target.Item != source.Item) return InventoryResult.DifferentItems;
            if (!source.Item.Stackable) return InventoryResult.NotStackable;

            int space = source.Item.MaxStack - target.Quantity;
            if (space <= 0) return InventoryResult.StackFull;
            int moved = Math.Min(space, source.Quantity);
            Set(to, target.Item, target.Quantity + moved);
            Set(from, source.Item, source.Quantity - moved);
            Flush();
            return InventoryResult.Success;
        }

        /// <summary>Replaces all slots in one step (used when loading). Caller validates content first.</summary>
        internal void LoadSlots(InventorySlot[] slots)
        {
            if (slots == null || slots.Length != _slots.Length) throw new ArgumentException("Slot count mismatch.", nameof(slots));
            for (int i = 0; i < _slots.Length; i++) Set(i, slots[i].Item, slots[i].Quantity);
            Flush();
        }

        private InventoryResult CheckIndices(int a, int b)
        {
            if (!IsValidSlot(a) || !IsValidSlot(b)) return InventoryResult.InvalidSlot;
            if (a == b) return InventoryResult.SameSlot;
            return InventoryResult.Success;
        }

        private InventoryResult CheckPair(int from, int to)
        {
            InventoryResult check = CheckIndices(from, to);
            if (!check.Succeeded()) return check;
            return _slots[from].IsEmpty ? InventoryResult.SlotEmpty : InventoryResult.Success;
        }

        private void Set(int index, ItemDefinition item, int quantity)
        {
            var next = new InventorySlot(item, quantity);
            if (next.Item == _slots[index].Item && next.Quantity == _slots[index].Quantity) return;
            _slots[index] = next;
            _dirty[index] = true;
        }

        private void Flush()
        {
            bool any = false;
            for (int i = 0; i < _dirty.Length; i++)
            {
                if (!_dirty[i]) continue;
                _dirty[i] = false;
                any = true;
                SlotChanged?.Invoke(i);
            }
            if (any) Changed?.Invoke();
        }
    }
}

using System;
using Lifetopia.Controls;
using Lifetopia.Core;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using UnityEngine;

namespace Lifetopia.UI
{
    /// <summary>
    /// Five-slot hotbar at the bottom of the screen. Shows the item each slot references and the
    /// live count from the inventory (the hotbar itself stores no quantities).
    /// - Inventory closed: keys 1-5 or tapping a slot selects it.
    /// - Inventory open with an item selected: keys 1-5 / tapping a slot puts that item there
    ///   (tapping the slot that already holds it removes it); dragging an inventory slot onto a
    ///   hotbar slot does the same.
    /// - Ignored while another modal (dialogue) blocks gameplay.
    /// </summary>
    public sealed class HotbarUI : MonoBehaviour
    {
        [SerializeField] private PlayerProfileHost host;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private GameplayInputGate gate;
        [SerializeField] private InventoryPanelUI inventoryPanel;
        [SerializeField] private ItemSlotView[] slots = Array.Empty<ItemSlotView>();

        private Hotbar _hotbar;
        private InventorySystem.Inventory _inventory;
        private bool _bound;

        private void Start()
        {
            if (host == null)
            {
                Debug.LogError("[Lifetopia] HotbarUI needs a PlayerProfileHost.", this);
                enabled = false;
                return;
            }
            _hotbar = host.Profile.Hotbar;
            _inventory = host.Profile.Inventory;
            _hotbar.SlotChanged += OnSlotChanged;
            _hotbar.SelectionChanged += OnSelectionChanged;
            _inventory.Changed += RefreshAll;
            if (input != null) input.HotbarSlotPerformed += Activate;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                slots[i].Index = i;
                slots[i].Clicked += OnSlotClicked;
                slots[i].Dropped += OnSlotDropped;
            }
            _bound = true;
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (!_bound) return;
            _hotbar.SlotChanged -= OnSlotChanged;
            _hotbar.SelectionChanged -= OnSelectionChanged;
            _inventory.Changed -= RefreshAll;
            if (input != null) input.HotbarSlotPerformed -= Activate;
            foreach (ItemSlotView slot in slots)
            {
                if (slot == null) continue;
                slot.Clicked -= OnSlotClicked;
                slot.Dropped -= OnSlotDropped;
            }
        }

        /// <summary>Keyboard 1-5 or tap on slot <paramref name="index"/>.</summary>
        public void Activate(int index)
        {
            if (!_bound || !_hotbar.IsValidSlot(index)) return;
            if (gate != null && gate.IsBlockedExcept(inventoryPanel)) return;

            if (inventoryPanel != null && inventoryPanel.IsOpen)
            {
                ItemDefinition item = inventoryPanel.SelectedItem;
                if (item == null) return;
                if (_hotbar.GetItem(index) == item) _hotbar.ClearSlot(index);
                else Report(_hotbar.Assign(index, item), item);
                return;
            }
            _hotbar.Select(index);
        }

        private void OnSlotClicked(ItemSlotView view) => Activate(view.Index);

        private void OnSlotDropped(ItemSlotView source, ItemSlotView target)
        {
            if (inventoryPanel == null || !inventoryPanel.Owns(source)) return;
            ItemDefinition item = _inventory.GetSlot(source.Index).Item;
            if (item != null) Report(_hotbar.Assign(target.Index, item), item);
        }

        private void Report(InventoryResult result, ItemDefinition item)
        {
            if (!result.Succeeded()) host.Notify(InventoryMessages.Describe(result, item));
        }

        private void OnSlotChanged(int index) => Refresh(index);

        private void OnSelectionChanged(int index)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null) slots[i].SetSelected(i == index);
        }

        private void RefreshAll()
        {
            for (int i = 0; i < slots.Length; i++) Refresh(i);
        }

        private void Refresh(int index)
        {
            if (index < 0 || index >= slots.Length || slots[index] == null) return;
            ItemDefinition item = _hotbar.GetItem(index);
            int count = _hotbar.GetCount(index);
            // A reference with 0 left stays on the bar, faded with "0", until it is refilled or replaced.
            slots[index].Show(item, count, true, item != null && count == 0);
            slots[index].SetSelected(index == _hotbar.SelectedIndex);
        }
    }
}

using System;
using Lifetopia.Controls;
using Lifetopia.Core;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lifetopia.UI
{
    /// <summary>
    /// Backpack window: 12 slots in a 4-column grid, item details and a close button.
    /// - Open/close: B or I (toggle), the Bag button, the close button, Escape.
    /// - While open it blocks gameplay through <see cref="GameplayInputGate"/> (no walking or world
    ///   interaction) and a full-screen backdrop swallows taps meant for the joystick.
    /// - Select a slot to see its details; drag a slot onto another to move, swap or stack.
    /// - Does not open while another modal (dialogue) is open.
    /// Refreshes only on inventory/hotbar events, never by polling.
    /// </summary>
    public sealed class InventoryPanelUI : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private PlayerProfileHost host;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private GameplayInputGate gate;

        [Header("Window")]
        [Tooltip("Backdrop + panel; hidden while closed.")]
        [SerializeField] private GameObject window;
        [SerializeField] private ItemSlotView[] slots = Array.Empty<ItemSlotView>();
        [SerializeField] private Button closeButton;
        [SerializeField] private Button openButton;

        [Header("Details")]
        [SerializeField] private ItemSlotView detailIcon;
        [SerializeField] private Text detailName;
        [SerializeField] private Text detailCategory;
        [SerializeField] private Text detailQuantity;
        [SerializeField] private Text detailPrice;
        [SerializeField] private Text detailDescription;
        [SerializeField] private Text detailHint;

        [Header("Drag and drop")]
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private ItemSlotView dragGhost;

        private InventorySystem.Inventory _inventory;
        private Hotbar _hotbar;
        private int _selected = -1;
        private bool _bound;

        public event Action Opened;
        public event Action Closed;

        public bool IsOpen { get; private set; }
        public int SelectedIndex => _selected;

        public ItemDefinition SelectedItem =>
            _inventory != null && _inventory.IsValidSlot(_selected) ? _inventory.GetSlot(_selected).Item : null;

        private void Start()
        {
            if (host == null)
            {
                Debug.LogError("[Lifetopia] InventoryPanelUI needs a PlayerProfileHost.", this);
                enabled = false;
                return;
            }
            _inventory = host.Profile.Inventory;
            _hotbar = host.Profile.Hotbar;
            _inventory.SlotChanged += OnSlotChanged;
            _hotbar.SlotChanged += OnHotbarChanged;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                slots[i].Index = i;
                slots[i].Clicked += OnSlotClicked;
                slots[i].DragStarted += OnDragStarted;
                slots[i].DragMoved += OnDragMoved;
                slots[i].DragEnded += OnDragEnded;
                slots[i].Dropped += OnSlotDropped;
            }
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (input != null) input.InventoryTogglePerformed += Toggle;
            _bound = true;

            if (window != null) window.SetActive(false);
            HideGhost();
            for (int i = 0; i < slots.Length; i++) RefreshSlot(i);
            RefreshDetail();
        }

        private void OnDestroy()
        {
            if (!_bound) return;
            _inventory.SlotChanged -= OnSlotChanged;
            _hotbar.SlotChanged -= OnHotbarChanged;
            foreach (ItemSlotView slot in slots)
            {
                if (slot == null) continue;
                slot.Clicked -= OnSlotClicked;
                slot.DragStarted -= OnDragStarted;
                slot.DragMoved -= OnDragMoved;
                slot.DragEnded -= OnDragEnded;
                slot.Dropped -= OnSlotDropped;
            }
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (openButton != null) openButton.onClick.RemoveListener(Toggle);
            if (input != null)
            {
                input.InventoryTogglePerformed -= Toggle;
                input.CancelPerformed -= Close;
            }
            if (IsOpen && gate != null) gate.Unblock(this);
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (IsOpen || !_bound) return;
            if (gate != null && gate.IsBlockedExcept(this)) return; // e.g. dialogue is open
            IsOpen = true;
            if (window != null) window.SetActive(true);
            if (gate != null) gate.Block(this);
            if (input != null) input.CancelPerformed += Close;
            if (SelectedItem == null) Select(FirstFilledSlot());
            else RefreshDetail();
            Opened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            HideGhost();
            if (window != null) window.SetActive(false);
            if (input != null) input.CancelPerformed -= Close;
            if (gate != null) gate.Unblock(this);
            Closed?.Invoke();
        }

        public void Select(int index)
        {
            int previous = _selected;
            _selected = _inventory.IsValidSlot(index) ? index : -1;
            if (previous >= 0 && previous < slots.Length && slots[previous] != null) slots[previous].SetSelected(false);
            if (_selected >= 0 && _selected < slots.Length && slots[_selected] != null) slots[_selected].SetSelected(true);
            RefreshDetail();
        }

        /// <summary>True if <paramref name="view"/> is one of this panel's inventory slots.</summary>
        public bool Owns(ItemSlotView view) => view != null && Array.IndexOf(slots, view) >= 0;

        private int FirstFilledSlot()
        {
            for (int i = 0; i < _inventory.SlotCount; i++)
                if (!_inventory.GetSlot(i).IsEmpty) return i;
            return 0;
        }

        private void OnSlotClicked(ItemSlotView view) => Select(view.Index);

        private void OnSlotDropped(ItemSlotView source, ItemSlotView target)
        {
            if (!Owns(source) || !Owns(target)) return;
            InventorySlot from = _inventory.GetSlot(source.Index);
            InventorySlot to = _inventory.GetSlot(target.Index);
            bool stack = !to.IsEmpty && to.Item == from.Item && from.Item != null && from.Item.Stackable;
            InventoryResult result = stack
                ? _inventory.MergeStack(source.Index, target.Index)
                : _inventory.SwapSlot(source.Index, target.Index);
            if (result.Succeeded()) Select(target.Index);
            else host.Notify(InventoryMessages.Describe(result, from.Item));
        }

        private void OnDragStarted(ItemSlotView view, PointerEventData eventData)
        {
            if (dragGhost == null) return;
            InventorySlot slot = _inventory.GetSlot(view.Index);
            dragGhost.Show(slot.Item, slot.Quantity, true);
            dragGhost.gameObject.SetActive(true);
            OnDragMoved(eventData);
        }

        private void OnDragMoved(PointerEventData eventData)
        {
            if (dragGhost == null || dragLayer == null || !dragGhost.gameObject.activeSelf) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, eventData.position,
                    eventData.pressEventCamera, out Vector2 local))
                ((RectTransform)dragGhost.transform).anchoredPosition = local;
        }

        private void OnDragEnded(ItemSlotView view) => HideGhost();

        private void HideGhost()
        {
            if (dragGhost != null) dragGhost.gameObject.SetActive(false);
        }

        private void OnSlotChanged(int index)
        {
            RefreshSlot(index);
            if (index == _selected) RefreshDetail();
        }

        private void OnHotbarChanged(int _) => RefreshDetail();

        private void RefreshSlot(int index)
        {
            if (index < 0 || index >= slots.Length || slots[index] == null) return;
            InventorySlot slot = _inventory.GetSlot(index);
            slots[index].Show(slot.Item, slot.Quantity, true);
            slots[index].SetSelected(index == _selected);
        }

        private void RefreshDetail()
        {
            if (_inventory == null) return;
            InventorySlot slot = _inventory.IsValidSlot(_selected) ? _inventory.GetSlot(_selected) : InventorySlot.Empty;
            ItemDefinition item = slot.Item;
            if (detailIcon != null) detailIcon.Show(item, slot.Quantity, false);

            if (item == null)
            {
                SetText(detailName, "Empty slot");
                SetText(detailCategory, "");
                SetText(detailQuantity, "");
                SetText(detailPrice, "");
                SetText(detailDescription, "Select an item to see its details.");
                SetText(detailHint, "Drag items onto another slot to move or stack them.");
                return;
            }

            SetText(detailName, item.DisplayName);
            SetText(detailCategory, item.Category.ToString());
            SetText(detailQuantity, item.Stackable
                ? $"Quantity: {slot.Quantity} / {item.MaxStack}"
                : item.ConsumedOnUse ? "Quantity: 1" : "Tool · never used up");
            SetText(detailPrice, item.CanBeSold ? $"Sells for {item.SellPrice} coins" : "Can't be sold");
            SetText(detailDescription, item.Description);

            int hotbarSlot = _hotbar.IndexOf(item);
            SetText(detailHint, hotbarSlot >= 0
                ? $"On hotbar slot {hotbarSlot + 1}. Tap that slot again to remove it."
                : "Tap a hotbar slot or press 1-5 to place it there."); // ASCII only: the built-in Web font has no en dash
        }

        private static void SetText(Text label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}

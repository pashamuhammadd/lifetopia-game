using System;
using Lifetopia.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lifetopia.UI
{
    /// <summary>
    /// Displays one item slot (inventory, hotbar, detail preview or drag ghost) and reports
    /// clicks/taps and drag-and-drop to its owner. Holds no game state: owners call <see cref="Show"/>.
    /// Items without an icon sprite show a category-coloured placeholder tile with a short label.
    /// </summary>
    public sealed class ItemSlotView : MonoBehaviour,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text iconLabel;
        [SerializeField] private Text countLabel;
        [SerializeField] private GameObject selection;
        [SerializeField] private bool draggable;

        public event Action<ItemSlotView> Clicked;
        public event Action<ItemSlotView, PointerEventData> DragStarted;
        public event Action<PointerEventData> DragMoved;
        public event Action<ItemSlotView> DragEnded;
        /// <summary>(source, target) when another slot is dropped onto this one.</summary>
        public event Action<ItemSlotView, ItemSlotView> Dropped;

        /// <summary>Slot index in the owner (inventory 0-11, hotbar 0-4).</summary>
        public int Index { get; set; }
        public ItemDefinition Item { get; private set; }

        public void Show(ItemDefinition item, int count, bool showCount)
        {
            Item = item;
            if (icon != null)
            {
                icon.enabled = item != null;
                if (item != null)
                {
                    bool hasArt = item.Icon != null;
                    icon.sprite = hasArt ? item.Icon : RoundedSprites.Get(12);
                    icon.type = hasArt ? Image.Type.Simple : Image.Type.Sliced;
                    icon.preserveAspect = hasArt;
                    icon.color = hasArt ? Color.white : UiPalette.ForCategory(item.Category);
                    if (iconLabel != null)
                    {
                        iconLabel.enabled = !hasArt;
                        iconLabel.text = hasArt ? "" : ShortLabel(item.DisplayName);
                        iconLabel.color = UiPalette.LabelOn(icon.color);
                    }
                }
            }
            if (item == null && iconLabel != null) iconLabel.enabled = false;

            if (countLabel != null)
            {
                bool visible = item != null && showCount && item.Stackable;
                countLabel.enabled = visible;
                if (visible) countLabel.text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public void SetSelected(bool selected)
        {
            if (selection != null && selection.activeSelf != selected) selection.SetActive(selected);
        }

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(this);

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!draggable || Item == null)
            {
                eventData.pointerDrag = null; // cancel: no drag events for empty / fixed slots
                return;
            }
            DragStarted?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData) => DragMoved?.Invoke(eventData);

        public void OnEndDrag(PointerEventData eventData) => DragEnded?.Invoke(this);

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null) return;
            var source = eventData.pointerDrag.GetComponent<ItemSlotView>();
            if (source != null && source != this) Dropped?.Invoke(source, this);
        }

        /// <summary>"Hoe" → "HOE", "Watering Can" → "WC", "Tropical Seeds" → "TS".</summary>
        public static string ShortLabel(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return "?";
            string[] words = displayName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1)
                return words[0].Substring(0, Math.Min(3, words[0].Length)).ToUpperInvariant();
            return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpperInvariant();
        }
    }
}

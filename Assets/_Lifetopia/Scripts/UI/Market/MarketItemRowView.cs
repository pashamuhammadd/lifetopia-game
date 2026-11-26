using System;
using System.Globalization;
using Lifetopia.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Lifetopia.UI
{
    /// <summary>One selectable item row in the market list: icon, name, owned count and unit price.</summary>
    public sealed class MarketItemRowView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private ItemSlotView icon;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text ownedLabel;
        [SerializeField] private Text priceLabel;
        [Tooltip("Frame shown on the selected row (plus a '>' marker, so selection is not colour-only).")]
        [SerializeField] private GameObject selection;
        [SerializeField] private Text selectionMarker;

        public event Action<MarketItemRowView> Clicked;

        public ItemDefinition Item { get; private set; }
        public int Index { get; set; }

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        public void Show(ItemDefinition item, int owned, int unitPrice, bool selected)
        {
            Item = item;
            if (icon != null) icon.Show(item, owned, false);
            if (nameLabel != null) nameLabel.text = item != null ? item.DisplayName : "";
            if (ownedLabel != null) ownedLabel.text = "Owned: " + owned.ToString(CultureInfo.InvariantCulture);
            if (priceLabel != null) priceLabel.text = unitPrice.ToString("N0", CultureInfo.InvariantCulture);
            if (selection != null) selection.SetActive(selected);
            if (selectionMarker != null) selectionMarker.enabled = selected;
        }
    }
}

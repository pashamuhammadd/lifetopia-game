using System;
using System.Collections.Generic;
using System.Globalization;
using Lifetopia.Controls;
using Lifetopia.Core;
using Lifetopia.Economy;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Lifetopia.UI
{
    /// <summary>
    /// Rio's Market window (Buy / Sell). All trades go through <see cref="MarketService"/> on the player's
    /// existing wallet and inventory; this class only shows quotes and forwards the confirmed choice.
    /// - Open from <c>MerchantNpc</c>; refuses while another modal (bag, dialogue, fishing) blocks gameplay.
    /// - While open: blocks movement/world interaction and pauses game time (like the bag), hides the hotbar,
    ///   and a full-screen backdrop swallows taps meant for the joystick.
    /// - Controls: mouse/touch on tabs, rows, - / + / Max, Purchase/Sell, Close.
    ///   Keyboard: W/S or Up/Down = item, A/D or Left/Right = quantity, Tab = switch Buy/Sell,
    ///   E / Enter-equivalent Interact = confirm, Esc = close.
    /// - Double submission: one confirm per frame and a short real-time cooldown; the press that opened the
    ///   market never confirms; the service itself refuses re-entrant trades.
    /// Refreshes on wallet/inventory events, not by polling.
    /// </summary>
    public sealed class MarketPanelUI : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private PlayerProfileHost host;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private GameplayInputGate gate;
        [SerializeField] private MarketConfig config;

        [Header("Window")]
        [SerializeField] private GameObject window;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text greetingLabel;
        [SerializeField] private Text balanceLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject[] hideWhileOpen = Array.Empty<GameObject>();

        [Header("Tabs")]
        [SerializeField] private Button buyTab;
        [SerializeField] private Button sellTab;
        [SerializeField] private Image buyTabFill;
        [SerializeField] private Image sellTabFill;
        [SerializeField] private Text buyTabLabel;
        [SerializeField] private Text sellTabLabel;
        [Tooltip("Bar shown under the active tab, so the active tab is not marked by colour alone.")]
        [SerializeField] private GameObject buyTabIndicator;
        [SerializeField] private GameObject sellTabIndicator;

        [Header("List")]
        [SerializeField] private MarketItemRowView rowTemplate;
        [SerializeField] private Text emptyLabel;

        [Header("Details")]
        [SerializeField] private Text itemNameLabel;
        [SerializeField] private Text unitPriceLabel;
        [SerializeField] private Text quantityLabel;
        [SerializeField] private Button minusButton;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button maxButton;
        [SerializeField] private Text totalCaption;
        [SerializeField] private Text totalLabel;
        [SerializeField] private Text balanceAfterLabel;
        [SerializeField] private Button actionButton;
        [SerializeField] private Image actionFill;
        [SerializeField] private Text actionLabel;
        [SerializeField] private Text feedbackLabel;

        [Tooltip("Minimum real seconds between two confirmed trades (prevents accidental double taps).")]
        [SerializeField, Range(0f, 1f)] private float submitCooldown = 0.25f;

        /// <summary>Dark coral: readable on cream (the message text itself says what went wrong).</summary>
        private static readonly Color ErrorText = new Color(0.70f, 0.29f, 0.21f, 1f);

        private MarketService _service;
        private PlayerProfile _profile;
        private readonly List<MarketItemRowView> _rows = new List<MarketItemRowView>(4);
        private TradeMode _mode = TradeMode.Buy;
        private int _selected;
        private int _quantity = 1;
        private string _message = "";
        private bool _messageIsError;
        private bool _bound;
        private int _openFrame = -1;
        private int _lastSubmitFrame = -1;
        private float _nextSubmitTime;
        private Vector2 _lastNav;
        private bool[] _wasActive;

        public event Action Opened;
        public event Action Closed;

        public bool IsOpen { get; private set; }
        public bool CanOpen => _bound && !IsOpen && (gate == null || !gate.IsBlocked);
        public MarketService Service => _service;
        public TradeMode Mode => _mode;
        public int Quantity => _quantity;
        public ItemDefinition SelectedItem => _selected >= 0 && _selected < CurrentItems.Count ? CurrentItems[_selected] : null;
        /// <summary>Text currently shown under the action button (tests).</summary>
        public string Feedback => feedbackLabel != null ? feedbackLabel.text : _message;

        private IReadOnlyList<ItemDefinition> CurrentItems =>
            config != null ? config.ItemsFor(_mode) : (IReadOnlyList<ItemDefinition>)Array.Empty<ItemDefinition>();

        private void Start()
        {
            if (host == null || config == null)
            {
                Debug.LogError("[Lifetopia] MarketPanelUI needs a PlayerProfileHost and a MarketConfig.", this);
                enabled = false;
                return;
            }
            var problems = new List<string>();
            config.Validate(problems);
            foreach (string p in problems) Debug.LogWarning("[Lifetopia] " + p, this);

            _profile = host.Profile;
            _service = new MarketService(config, _profile);
            _profile.Wallet.Changed += OnWalletChanged;
            _profile.Inventory.Changed += Refresh;

            if (rowTemplate != null)
            {
                rowTemplate.gameObject.SetActive(false);
                int needed = Math.Max(config.BuyItems.Count, config.SellItems.Count);
                for (int i = 0; i < needed; i++)
                {
                    MarketItemRowView row = Instantiate(rowTemplate, rowTemplate.transform.parent);
                    row.name = "Row" + i;
                    row.Index = i;
                    row.Clicked += OnRowClicked;
                    _rows.Add(row);
                }
            }
            if (titleLabel != null) titleLabel.text = config.ShopName;
            if (greetingLabel != null) greetingLabel.text = config.Greeting;

            AddClick(closeButton, Close);
            AddClick(buyTab, () => SetMode(TradeMode.Buy));
            AddClick(sellTab, () => SetMode(TradeMode.Sell));
            AddClick(minusButton, () => ChangeQuantity(-1));
            AddClick(plusButton, () => ChangeQuantity(+1));
            AddClick(maxButton, SetMaxQuantity);
            AddClick(actionButton, Submit);
            if (window != null) window.SetActive(false);
            _bound = true;
        }

        private void OnDestroy()
        {
            if (_profile != null)
            {
                _profile.Wallet.Changed -= OnWalletChanged;
                _profile.Inventory.Changed -= Refresh;
            }
            foreach (MarketItemRowView row in _rows) if (row != null) row.Clicked -= OnRowClicked;
            Unsubscribe();
            if (IsOpen && gate != null) gate.Unblock(this);
        }

        /// <summary>Opens on the Buy tab. Returns false when another modal is open.</summary>
        public bool Open()
        {
            if (!CanOpen) return false;
            IsOpen = true;
            _openFrame = Time.frameCount;
            _lastNav = Vector2.zero;
            _message = "";
            if (gate != null) gate.Block(this); // menu: pauses game time like the bag
            if (input != null)
            {
                input.CancelPerformed += Close;
                input.UiSubmitPerformed += Submit;
            }
            _wasActive = new bool[hideWhileOpen.Length];
            for (int i = 0; i < hideWhileOpen.Length; i++)
            {
                if (hideWhileOpen[i] == null) continue;
                _wasActive[i] = hideWhileOpen[i].activeSelf;
                hideWhileOpen[i].SetActive(false);
            }
            if (window != null) window.SetActive(true);
            _mode = TradeMode.Buy;
            _selected = 0;
            _quantity = 1;
            Refresh();
            Opened?.Invoke();
            return true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Unsubscribe();
            if (window != null) window.SetActive(false);
            if (_wasActive != null)
                for (int i = 0; i < hideWhileOpen.Length; i++)
                    if (hideWhileOpen[i] != null && _wasActive[i]) hideWhileOpen[i].SetActive(true);
            if (gate != null) gate.Unblock(this);
            Closed?.Invoke();
        }

        public void SetMode(TradeMode mode)
        {
            if (!IsOpen) return;
            if (_mode != mode) { _mode = mode; _selected = 0; _quantity = 1; _message = ""; }
            Refresh();
        }

        public void Select(int index)
        {
            if (!IsOpen || index < 0 || index >= CurrentItems.Count) return;
            if (_selected != index) { _selected = index; _quantity = 1; _message = ""; }
            Refresh();
        }

        public void ChangeQuantity(int delta)
        {
            if (!IsOpen) return;
            int max = Math.Max(1, MaxForSelection());
            _quantity = Mathf.Clamp(_quantity + delta, 1, max);
            _message = "";
            Refresh();
        }

        public void SetMaxQuantity()
        {
            if (!IsOpen) return;
            _quantity = Math.Max(1, MaxForSelection());
            _message = "";
            Refresh();
        }

        /// <summary>Confirms the shown trade once (button, E/touch Interact).</summary>
        public void Submit()
        {
            if (!IsOpen || _service == null) return;
            if (Time.frameCount == _openFrame || Time.frameCount == _lastSubmitFrame) return;
            if (Time.unscaledTime < _nextSubmitTime) return;
            _lastSubmitFrame = Time.frameCount;
            _nextSubmitTime = Time.unscaledTime + submitCooldown;

            ItemDefinition item = SelectedItem;
            int quantity = _quantity;
            int coinsBefore = _service.Coins;
            TradeResult result = _service.Execute(_mode, item, quantity);
            if (result == TradeResult.Success)
            {
                int coins = Math.Abs(_service.Coins - coinsBefore);
                _message = MarketText.Success(new TradeRecord(_mode, item, quantity, coins));
                _messageIsError = false;
                _quantity = 1;
            }
            else
            {
                _message = MarketText.Problem(result, item);
                _messageIsError = true;
            }
            Refresh();
        }

        private void Update()
        {
            if (!IsOpen || input == null) return;
            Vector2 nav = input.NavigateInput;
            Vector2Int now = new Vector2Int(Axis(nav.x), Axis(nav.y));
            Vector2Int before = new Vector2Int(Axis(_lastNav.x), Axis(_lastNav.y));
            _lastNav = nav;
            if (now.y != 0 && now.y != before.y) Select(Mathf.Clamp(_selected - now.y, 0, Math.Max(0, CurrentItems.Count - 1)));
            if (now.x != 0 && now.x != before.x) ChangeQuantity(now.x);
            Keyboard k = Keyboard.current;
            if (k != null && k.tabKey.wasPressedThisFrame) SetMode(_mode == TradeMode.Buy ? TradeMode.Sell : TradeMode.Buy);
        }

        private static int Axis(float v) => v > 0.5f ? 1 : (v < -0.5f ? -1 : 0);

        private int MaxForSelection()
        {
            ItemDefinition item = SelectedItem;
            return item != null && _service != null ? _service.MaxQuantity(_mode, item) : 0;
        }

        private void OnRowClicked(MarketItemRowView row) => Select(row.Index);

        private void OnWalletChanged(int _) => Refresh();

        private void Refresh()
        {
            if (!_bound || !IsOpen) return;
            IReadOnlyList<ItemDefinition> items = CurrentItems;
            if (_selected >= items.Count) _selected = items.Count - 1;
            if (_selected < 0 && items.Count > 0) _selected = 0;

            if (balanceLabel != null) balanceLabel.text = MarketText.Coins(_service.Coins);
            StyleTab(buyTabFill, buyTabLabel, buyTabIndicator, _mode == TradeMode.Buy, MarketText.BuyTab);
            StyleTab(sellTabFill, sellTabLabel, sellTabIndicator, _mode == TradeMode.Sell, MarketText.SellTab);

            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < items.Count && items[i] != null;
                _rows[i].gameObject.SetActive(used);
                if (used) _rows[i].Show(items[i], _service.Owned(items[i]), config.UnitPrice(_mode, items[i]), i == _selected);
            }
            if (emptyLabel != null) emptyLabel.enabled = items.Count == 0;

            ItemDefinition item = SelectedItem;
            int max = MaxForSelection();
            if (max > 0) _quantity = Mathf.Clamp(_quantity, 1, max);
            else _quantity = 1;
            TradeQuote q = _service.Quote(_mode, item, _quantity);

            if (itemNameLabel != null) itemNameLabel.text = item != null ? item.DisplayName : "";
            if (unitPriceLabel != null)
                unitPriceLabel.text = item != null ? $"{MarketText.Coins(q.UnitPrice)} coins each" : "";
            if (quantityLabel != null) quantityLabel.text = _quantity.ToString(CultureInfo.InvariantCulture);
            if (totalCaption != null) totalCaption.text = _mode == TradeMode.Buy ? "Total cost" : "You receive";
            if (totalLabel != null) totalLabel.text = MarketText.Coins((long)q.UnitPrice * _quantity);
            if (balanceAfterLabel != null)
            {
                long after = _mode == TradeMode.Buy ? _service.Coins - (long)q.UnitPrice * _quantity : _service.Coins + (long)q.UnitPrice * _quantity;
                balanceAfterLabel.text = q.Ok ? "Balance after: " + MarketText.Coins(after) : "";
            }
            if (minusButton != null) minusButton.interactable = _quantity > 1;
            if (plusButton != null) plusButton.interactable = _quantity < max;
            if (maxButton != null) maxButton.interactable = max > 1 && _quantity < max;
            if (actionButton != null) actionButton.interactable = q.Ok;
            if (actionFill != null) actionFill.color = q.Ok ? UiPalette.Leaf : UiPalette.Sand;
            if (actionLabel != null)
            {
                actionLabel.text = MarketText.ActionLabel(_mode, _quantity);
                actionLabel.color = q.Ok ? Color.white : UiPalette.Wood;
            }
            if (feedbackLabel != null)
            {
                bool showMessage = !string.IsNullOrEmpty(_message);
                feedbackLabel.text = showMessage ? _message : MarketText.Problem(q.Result, item);
                bool error = showMessage ? _messageIsError : !q.Ok;
                feedbackLabel.color = error ? ErrorText : UiPalette.DeepLeaf;
            }
        }

        private static void StyleTab(Image fill, Text label, GameObject indicator, bool active, string text)
        {
            if (fill != null) fill.color = active ? UiPalette.Leaf : UiPalette.Sand;
            if (indicator != null) indicator.SetActive(active);
            if (label != null)
            {
                label.text = text;
                label.color = active ? Color.white : UiPalette.Ink;
                label.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private void Unsubscribe()
        {
            if (input == null) return;
            input.CancelPerformed -= Close;
            input.UiSubmitPerformed -= Submit;
        }

        private static void AddClick(Button b, UnityEngine.Events.UnityAction a)
        {
            if (b != null) b.onClick.AddListener(a);
        }
    }
}

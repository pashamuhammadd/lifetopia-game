using System;
using System.Collections.Generic;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Lifetopia.Housing
{
    /// <summary>
    /// Decorating Mode in the house. Opening it blocks movement/world interaction and PAUSES game time
    /// (like the bag and the market), hides the hotbar, Bag button and touch stick, shows the grid and the
    /// decorating panel, and refuses to open while another menu is open (the bag/market cannot open on top
    /// because the gate is blocked).
    /// Input (all optional, same result):
    /// - Mouse / touch: tap an item in the list to start placing it; tap the floor to move the ghost
    ///   there; tap furniture to select it; buttons Place/Move, Rotate, Store, Cancel; Done to leave.
    ///   Taps that start on UI never reach the floor. No drag needed.
    /// - Keyboard: WASD/arrows nudge the ghost, E confirms (or starts Move on a selection), R rotates,
    ///   Tab picks the next bag item, Delete/Backspace stores, Esc cancels or leaves.
    /// Nothing changes until Place/Store is confirmed; one confirm per frame plus a short cooldown, and the
    /// service itself refuses re-entrant calls.
    /// </summary>
    public sealed class DecoratingController : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float submitCooldown = 0.25f;
        [Tooltip("A press that moves further than this (screen px) is a drag/scroll, not a tap.")]
        [SerializeField, Range(4f, 64f)] private float tapMaxMove = 22f;

        private static readonly Color Valid = new Color(0.40f, 0.85f, 0.45f, 0.95f);
        private static readonly Color Invalid = new Color(0.95f, 0.36f, 0.30f, 0.95f);
        private static readonly Color Selection = new Color(1.0f, 0.86f, 0.35f, 0.95f);

        private HousingContext _ctx;
        private HouseInteriorController _interior;
        private HousingHud _hud;
        private DecorateSession _session;
        private Inventory _inventory;
        private readonly List<Vector2Int> _playerCells = new List<Vector2Int>(6);
        private readonly List<KeyValuePair<ItemDefinition, int>> _items = new List<KeyValuePair<ItemDefinition, int>>(8);
        private readonly List<SpriteRenderer> _cellMarks = new List<SpriteRenderer>(8);
        private readonly List<RaycastResult> _hits = new List<RaycastResult>(8);
        private FurnitureView _ghost;
        private Transform _marksRoot;
        private bool[] _wasActive;
        private int _openFrame = -1, _lastSubmitFrame = -1;
        private float _nextSubmitTime;
        private Vector2 _pressPos;
        private bool _pressOverUi, _pressing;
        private Vector2 _lastNav;
        private string _message = "";
        private bool _messageError;
        private int _dimmedId;

        public bool IsOpen { get; private set; }
        public DecorateSession Session => _session;
        public event Action<bool> OpenChanged;

        public bool CanOpen => _ctx != null && !IsOpen && (_ctx.Gate == null || !_ctx.Gate.IsBlocked);

        public void Bind(HousingContext ctx, HouseInteriorController interior)
        {
            _ctx = ctx;
            _interior = interior;
            _hud = interior.Hud;
            _inventory = ctx.Profile.Profile.Inventory;
            _session = new DecorateSession(ctx.Service);
            _hud.DoneClicked += Close;
            _hud.PrimaryClicked += OnPrimary;
            _hud.RotateClicked += OnRotate;
            _hud.StoreClicked += OnStore;
            _hud.CancelClicked += OnCancel;
            _hud.ItemClicked += OnItem;
        }

        private void OnDestroy()
        {
            Close();
            if (_hud != null)
            {
                _hud.DoneClicked -= Close;
                _hud.PrimaryClicked -= OnPrimary;
                _hud.RotateClicked -= OnRotate;
                _hud.StoreClicked -= OnStore;
                _hud.CancelClicked -= OnCancel;
                _hud.ItemClicked -= OnItem;
            }
        }

        public bool Open()
        {
            if (!CanOpen) return false;
            IsOpen = true;
            _openFrame = Time.frameCount;
            _message = "";
            _lastNav = Vector2.zero;
            _pressing = false;
            if (_ctx.Gate != null) _ctx.Gate.Block(this); // menu: pauses game time and movement
            if (_ctx.Input != null)
            {
                _ctx.Input.CancelPerformed += OnEscape;
                _ctx.Input.UiSubmitPerformed += OnSubmitKey;
            }
            GameObject[] hide = _ctx.HideWhileDecorating ?? Array.Empty<GameObject>();
            _wasActive = new bool[hide.Length];
            for (int i = 0; i < hide.Length; i++)
            {
                if (hide[i] == null) continue;
                _wasActive[i] = hide[i].activeSelf;
                hide[i].SetActive(false);
            }

            Vector2 feet = _interior.WorldToRoom(_ctx.Player.transform.position);
            CircleCollider2D col = _ctx.PlayerCollider;
            InteriorCameraMath.PlayerCells(feet, col != null ? col.offset : new Vector2(0f, 0.2f), col != null ? col.radius : 0.28f, _playerCells);
            _session.PlayerCells = _playerCells;
            _session.Reset();
            _session.Changed += Refresh;
            _inventory.Changed += Refresh;

            _interior.SetGridVisible(true);
            _hud.ShowLiving(false);
            _hud.ShowDecorating(true);
            _interior.Framer.SetMode(true, _hud.DecoratingUsable, GhostFocus);
            Refresh();
            OpenChanged?.Invoke(true);
            return true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _session.Changed -= Refresh;
            _inventory.Changed -= Refresh;
            _session.Reset();
            if (_ctx.Input != null)
            {
                _ctx.Input.CancelPerformed -= OnEscape;
                _ctx.Input.UiSubmitPerformed -= OnSubmitKey;
            }
            GameObject[] hide = _ctx.HideWhileDecorating ?? Array.Empty<GameObject>();
            if (_wasActive != null)
                for (int i = 0; i < hide.Length && i < _wasActive.Length; i++)
                    if (hide[i] != null && _wasActive[i]) hide[i].SetActive(true);
            ClearPreview();
            if (_interior != null)
            {
                _interior.SetGridVisible(false);
                if (_interior.IsActive) _hud.ShowLiving(true);
                _hud.ShowDecorating(false);
                if (_interior.Framer != null) _interior.Framer.SetMode(false, _hud.LivingUsable, null);
            }
            if (_ctx.Gate != null) _ctx.Gate.Unblock(this);
            OpenChanged?.Invoke(false);
        }

        // ------------------------------------------------------------ actions (buttons, keys, taps)

        public void OnItem(ItemDefinition item)
        {
            if (!IsOpen) return;
            if (_session.State == DecorateState.Moving) _session.Cancel();
            if (_session.BeginPlace(item)) SetMessage("", false);
            else SetMessage(HousingText.Describe(PlacementResult.NotInInventory), true);
        }

        public void OnPrimary()
        {
            if (!IsOpen) return;
            if (_session.State == DecorateState.Selected) { _message = ""; _session.BeginMove(); return; }
            Submit();
        }

        public void OnRotate()
        {
            if (!IsOpen) return;
            _message = "";
            if (!_session.Rotate()) SetMessage(HousingText.Describe(PlacementResult.UnsupportedOrientation), true);
        }

        public void OnStore()
        {
            if (!IsOpen || !TakeSubmitSlot()) return;
            FurnitureDefinition def = _session.ActiveDefinition;
            PlacementResult r = _session.StoreSelected();
            if (r == PlacementResult.Ok) SetMessage(HousingText.Stored(Name(def)), false);
            else SetMessage(HousingText.Describe(r), true);
        }

        public void OnCancel()
        {
            if (!IsOpen) return;
            _session.Cancel();
            SetMessage("", false);
        }

        /// <summary>Places / moves the ghost (Place button, E).</summary>
        public void Submit()
        {
            if (!IsOpen || !_session.HasGhost || !TakeSubmitSlot()) return;
            bool placing = _session.State == DecorateState.Placing;
            FurnitureDefinition def = _session.ActiveDefinition;
            PlacementResult r = _session.Confirm();
            if (r == PlacementResult.Ok) SetMessage(placing ? HousingText.Placed(Name(def)) : HousingText.Moved(Name(def)), false);
            else SetMessage(HousingText.Describe(r), true);
        }

        /// <summary>Tap/click on the floor at a screen position.</summary>
        public void TapScreen(Vector2 screenPos)
        {
            if (!IsOpen || _ctx.Camera == null) return;
            Vector3 w = _ctx.Camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_ctx.Camera.transform.position.z));
            TapCell(_interior.WorldToCell(w));
        }

        public void TapCell(Vector2Int cell)
        {
            if (!IsOpen) return;
            if (_session.HasGhost)
            {
                if (_session.Layout.InBounds(cell)) { _message = ""; _session.SetGhostTarget(cell); }
                return;
            }
            _session.SelectAt(cell);
            SetMessage("", false);
        }

        private bool TakeSubmitSlot()
        {
            if (Time.frameCount == _openFrame || Time.frameCount == _lastSubmitFrame) return false;
            if (Time.unscaledTime < _nextSubmitTime) return false;
            _lastSubmitFrame = Time.frameCount;
            _nextSubmitTime = Time.unscaledTime + submitCooldown;
            return true;
        }

        private void OnSubmitKey()
        {
            if (_session.State == DecorateState.Selected) _session.BeginMove();
            else Submit();
        }

        private void OnEscape()
        {
            if (Time.frameCount == _openFrame) return;
            if (_session.State == DecorateState.Browsing) Close();
            else OnCancel();
        }

        private void Update()
        {
            if (!IsOpen) return;
            ReadPointer();
            ReadKeyboard();
        }

        private void ReadPointer()
        {
            Pointer p = Pointer.current;
            if (p == null) return;
            Vector2 pos = p.position.ReadValue();
            if (p.press.wasPressedThisFrame)
            {
                _pressing = true;
                _pressPos = pos;
                _pressOverUi = IsOverUi(pos);
            }
            if (_pressing && p.press.wasReleasedThisFrame)
            {
                _pressing = false;
                if (!_pressOverUi && (pos - _pressPos).sqrMagnitude <= tapMaxMove * tapMaxMove && Time.frameCount != _openFrame)
                    TapScreen(pos);
            }
        }

        private void ReadKeyboard()
        {
            if (_ctx.Input != null)
            {
                Vector2 nav = _ctx.Input.NavigateInput;
                Vector2Int now = new Vector2Int(Axis(nav.x), Axis(nav.y));
                Vector2Int before = new Vector2Int(Axis(_lastNav.x), Axis(_lastNav.y));
                _lastNav = nav;
                Vector2Int step = new Vector2Int(now.x != before.x ? now.x : 0, now.y != before.y ? now.y : 0);
                if (step != Vector2Int.zero) _session.Nudge(step);
            }
            Keyboard k = Keyboard.current;
            if (k == null) return;
            if (k.rKey.wasPressedThisFrame) OnRotate();
            if (k.deleteKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame) OnStore();
            if (k.tabKey.wasPressedThisFrame && _items.Count > 0)
            {
                int i = _items.FindIndex(e => e.Key == _session.PlacingItem);
                OnItem(_items[(i + 1) % _items.Count].Key);
            }
        }

        private static int Axis(float v) => v > 0.5f ? 1 : (v < -0.5f ? -1 : 0);

        private bool IsOverUi(Vector2 screenPos)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPos };
            _hits.Clear();
            es.RaycastAll(data, _hits);
            return _hits.Count > 0;
        }

        private Vector2 GhostFocus()
        {
            FurnitureDefinition d = _session != null ? _session.ActiveDefinition : null;
            if (d == null || _session.State == DecorateState.Browsing) return (Vector2)_ctx.Player.transform.position;
            Vector2Int s = d.Footprint(_session.GhostOrientation);
            return _interior.RoomToWorld(new Vector2(_session.GhostCell.x + s.x * 0.5f, _session.GhostCell.y + s.y * 0.5f));
        }

        // ------------------------------------------------------------ drawing

        private void SetMessage(string text, bool error)
        {
            _message = text;
            _messageError = error;
            Refresh();
        }

        private void Refresh()
        {
            if (!IsOpen) return;
            // Bag list: furniture items only, in bag order.
            _items.Clear();
            for (int i = 0; i < _inventory.SlotCount; i++)
            {
                InventorySlot slot = _inventory.GetSlot(i);
                if (slot.IsEmpty || !_ctx.Service.Catalog.TryGet(slot.Item, out _)) continue;
                int idx = _items.FindIndex(e => e.Key == slot.Item);
                if (idx < 0) _items.Add(new KeyValuePair<ItemDefinition, int>(slot.Item, _inventory.GetItemCount(slot.Item)));
            }
            _hud.SetItems(_items, _session.PlacingItem);

            DecorateState st = _session.State;
            bool touch = _ctx.Touch != null && _ctx.Touch.TouchVisible;
            _hud.SetHint(HousingText.Hint(st, touch));
            PlacementResult preview = _session.Preview;
            string primary = st == DecorateState.Selected ? HousingText.Move : HousingText.Place;
            bool primaryEnabled = st == DecorateState.Selected || _session.CanConfirm;
            _hud.SetActions(primary, primaryEnabled, st != DecorateState.Browsing, _session.CanRotate, _session.CanRotate,
                _session.CanStore, st != DecorateState.Browsing, st == DecorateState.Selected);

            if (!string.IsNullOrEmpty(_message)) _hud.SetFeedback(_message, _messageError);
            else if (_session.HasGhost) _hud.SetFeedback(HousingText.Describe(preview), preview != PlacementResult.Ok);
            else _hud.SetFeedback("", false);

            DrawPreview(preview);
        }

        private void DrawPreview(PlacementResult preview)
        {
            ClearDim();
            FurnitureDefinition def = _session.ActiveDefinition;
            if (def == null || _session.State == DecorateState.Browsing)
            {
                HideGhost();
                HideMarks();
                return;
            }
            Vector2Int size = def.Footprint(_session.GhostOrientation);
            if (_session.HasGhost)
            {
                if (_ghost == null) _ghost = FurnitureView.Create(_interior.transform, "DecorGhost");
                _ghost.gameObject.SetActive(true);
                Sprite art = _interior.SpriteFor(def, _session.GhostOrientation, out float k);
                _ghost.Show(0, def, _session.GhostCell, _session.GhostOrientation, art, false, "Overhead", 10, k);
                bool ok = preview == PlacementResult.Ok;
                _ghost.SetTint(ok ? new Color(1f, 1f, 1f, 0.8f) : new Color(1f, 0.6f, 0.6f, 0.7f));
                MarkCells(_session.GhostCell, size, ok ? Valid : Invalid);
                if (_session.State == DecorateState.Moving && _interior.TryGetView(_session.SelectedId, out FurnitureView orig))
                {
                    orig.SetTint(new Color(1f, 1f, 1f, 0.35f));
                    _dimmedId = _session.SelectedId;
                }
            }
            else
            {
                HideGhost();
                MarkCells(_session.GhostCell, size, Selection);
            }
        }

        private void MarkCells(Vector2Int cell, Vector2Int size, Color color)
        {
            if (_marksRoot == null)
            {
                _marksRoot = new GameObject("DecorMarks").transform;
                _marksRoot.SetParent(_interior.transform, false);
            }
            int n = size.x * size.y;
            while (_cellMarks.Count < n)
            {
                var go = new GameObject("Mark" + _cellMarks.Count);
                go.transform.SetParent(_marksRoot, false);
                var r = go.AddComponent<SpriteRenderer>();
                r.sprite = HousingSprites.FootprintCell;
                r.sortingLayerName = "Overhead";
                r.sortingOrder = 5;
                _cellMarks.Add(r);
            }
            int k = 0;
            for (int y = 0; y < size.y; y++)
                for (int x = 0; x < size.x; x++)
                {
                    SpriteRenderer r = _cellMarks[k++];
                    r.gameObject.SetActive(true);
                    r.transform.localPosition = new Vector3(cell.x + x + 0.5f, cell.y + y + 0.5f, 0f);
                    r.color = color;
                }
            for (; k < _cellMarks.Count; k++) _cellMarks[k].gameObject.SetActive(false);
        }

        private void HideMarks()
        {
            foreach (SpriteRenderer r in _cellMarks) if (r != null) r.gameObject.SetActive(false);
        }

        private void HideGhost()
        {
            if (_ghost != null) _ghost.gameObject.SetActive(false);
        }

        private void ClearDim()
        {
            if (_dimmedId != 0 && _interior != null && _interior.TryGetView(_dimmedId, out FurnitureView v)) v.SetTint(Color.white);
            _dimmedId = 0;
        }

        private void ClearPreview()
        {
            ClearDim();
            HideGhost();
            HideMarks();
        }

        private static string Name(FurnitureDefinition d) => d != null && d.Item != null ? d.Item.DisplayName : "Item";
    }
}

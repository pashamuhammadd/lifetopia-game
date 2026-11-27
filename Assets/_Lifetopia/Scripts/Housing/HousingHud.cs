using System;
using System.Collections.Generic;
using System.Globalization;
using Lifetopia.Items;
using Lifetopia.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Lifetopia.Housing
{
    /// <summary>
    /// Interior HUD on the interior scene's own Screen Space Overlay canvas (Canvas Scaler 1280x720,
    /// match 0.5, like the village HUD). Built in code once (rounded placeholder panels, optional icons
    /// from the local <see cref="HousingVisualSet"/>), everything anchored inside a safe-area fitter so it
    /// adapts to resize and orientation without per-frame layout work.
    /// - Living Mode: Decorate and Exit buttons in the right column under the Bag button.
    /// - Decorating Mode: title + hint top-left, Done top-left, right panel with the furniture list
    ///   (scrolls), feedback and four large action buttons (Place/Move, Rotate, Store, Cancel).
    /// - Two invisible rects describe the free screen area per mode for the adaptive camera.
    /// The canvas sorts BELOW the village HUD so the bag/menus always cover it.
    /// Touch targets are at least 80 reference px (about 46 CSS px on a 390 px high phone).
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public sealed class HousingHud : MonoBehaviour
    {
        [Header("Layout (reference pixels, 1280x720)")]
        [SerializeField] private float buttonSize = 84f;
        [SerializeField] private float rightColumnTop = 214f;
        [SerializeField] private float panelWidth = 344f;
        [SerializeField] private float panelTop = 104f;
        [SerializeField] private float rowHeight = 76f;
        [SerializeField] private float edge = 14f;
        [Header("Free area for the camera (insets from the safe area)")]
        [SerializeField] private Vector4 livingInsets = new Vector4(16f, 118f, 250f, 92f);   // left, bottom, right, top
        [SerializeField] private Vector4 decoratingInsets = new Vector4(16f, 16f, 372f, 120f);
        [Header("Small screens")]
        [Tooltip("Minimum CSS px per reference px for this canvas (see CanvasMinScale).")]
        [SerializeField, Range(0.3f, 1.5f)] private float minCssPerReferencePixel = 0.62f;
        [SerializeField] private float titleBarHeight = 100f;

        public event Action DecorateClicked, ExitClicked, DoneClicked, PrimaryClicked, RotateClicked, StoreClicked, CancelClicked;
        public event Action<ItemDefinition> ItemClicked;

        public RectTransform LivingUsable { get; private set; }
        public RectTransform DecoratingUsable { get; private set; }
        public bool Built { get; private set; }

        private Font _font;
        private RectTransform _safe;
        private GameObject _living, _decorating;
        private Text _hint, _feedback, _primaryLabel, _emptyLabel;
        private Button _primary, _rotate, _store, _cancel;
        private Image _primaryIcon;
        private RectTransform _listContent;
        private ScrollRect _scroll;
        private HousingVisualSet _icons;
        private readonly List<Row> _rows = new List<Row>();

        private sealed class Row
        {
            public GameObject Root;
            public Button Button;
            public Image Icon;
            public Text IconLabel;
            public Text Name;
            public Text Count;
            public Image Frame;
            public ItemDefinition Item;
        }

        // Exposed for automated UI tests.
        public Button DecorateButton { get; private set; }
        public Button ExitButtonRef { get; private set; }
        public Button DoneButton { get; private set; }
        public Button PrimaryButton => _primary;
        public Button RotateButton => _rotate;
        public Button StoreButton => _store;
        public Button CancelButton => _cancel;
        public string FeedbackText => _feedback != null ? _feedback.text : "";
        public string PrimaryLabel => _primaryLabel != null ? _primaryLabel.text : "";
        public int VisibleRowCount { get { int n = 0; foreach (Row r in _rows) if (r.Root.activeSelf) n++; return n; } }
        public Button RowButton(int i) => i >= 0 && i < _rows.Count ? _rows[i].Button : null;

        public void Build(HousingVisualSet icons)
        {
            if (Built) return;
            Built = true;
            _icons = icons;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var minScale = GetComponent<CanvasMinScale>();
            if (minScale == null) minScale = gameObject.AddComponent<CanvasMinScale>();
            minScale.Configure(minCssPerReferencePixel);

            _safe = NewRect("SafeArea", (RectTransform)transform);
            Stretch(_safe);
            _safe.gameObject.AddComponent<SafeAreaFitter>();

            LivingUsable = Inset(NewRect("LivingUsableArea", _safe), livingInsets);
            DecoratingUsable = Inset(NewRect("DecoratingUsableArea", _safe), decoratingInsets);

            BuildLiving();
            BuildDecorating();
            ShowLiving(false);
            ShowDecorating(false);
        }

        public void ShowLiving(bool show) { if (_living != null) _living.SetActive(show); }
        public void ShowDecorating(bool show) { if (_decorating != null) _decorating.SetActive(show); }

        public void SetHint(string text) { if (_hint != null) _hint.text = text; }

        public void SetFeedback(string text, bool error)
        {
            if (_feedback == null) return;
            _feedback.text = text;
            _feedback.color = error ? new Color(0.70f, 0.29f, 0.21f) : UiPalette.DeepLeaf;
        }

        public void SetActions(string primaryLabel, bool primaryEnabled, bool primaryVisible, bool rotateEnabled, bool rotateVisible,
            bool storeEnabled, bool cancelEnabled, bool primaryIsMove)
        {
            _primaryLabel.text = primaryLabel;
            _primary.gameObject.SetActive(primaryVisible);
            _primary.interactable = primaryEnabled;
            if (_primaryIcon != null)
            {
                Sprite s = _icons != null ? (primaryIsMove ? _icons.iconMove : _icons.iconPlace) : null;
                _primaryIcon.sprite = s;
                _primaryIcon.enabled = s != null;
            }
            _rotate.gameObject.SetActive(rotateVisible);
            _rotate.interactable = rotateEnabled;
            _store.interactable = storeEnabled;
            _cancel.interactable = cancelEnabled;
        }

        /// <summary>Shows one row per furniture item in the bag (count &gt; 0). Rows are pooled.</summary>
        public void SetItems(IReadOnlyList<KeyValuePair<ItemDefinition, int>> items, ItemDefinition highlighted)
        {
            while (_rows.Count < items.Count) _rows.Add(CreateRow(_rows.Count));
            for (int i = 0; i < _rows.Count; i++)
            {
                Row r = _rows[i];
                bool used = i < items.Count;
                r.Root.SetActive(used);
                if (!used) { r.Item = null; continue; }
                ItemDefinition item = items[i].Key;
                r.Item = item;
                r.Name.text = item.DisplayName;
                r.Count.text = "x" + items[i].Value.ToString(CultureInfo.InvariantCulture);
                Sprite icon = ItemIcons.Get(item);
                r.Icon.sprite = icon;
                r.Icon.color = icon != null ? Color.white : UiPalette.ForCategory(item.Category);
                r.Icon.preserveAspect = icon != null;
                r.IconLabel.text = icon != null ? "" : Short(item.DisplayName);
                bool sel = item == highlighted;
                r.Frame.color = sel ? UiPalette.Leaf : UiPalette.Sand;
                r.Name.fontStyle = sel ? FontStyle.Bold : FontStyle.Normal;
            }
            _emptyLabel.gameObject.SetActive(items.Count == 0);
        }

        // ---------------------------------------------------------------- construction

        private void BuildLiving()
        {
            _living = NewRect("LivingHud", _safe).gameObject;
            Stretch((RectTransform)_living.transform);
            DecorateButton = IconButton(_living.transform, "DecorateButton", HousingText.DecorateButton, _icons != null ? _icons.iconDecorate : null,
                new Vector2(1f, 1f), new Vector2(-edge - buttonSize - 30f, -rightColumnTop), new Vector2(buttonSize + 20f, buttonSize + 8f), UiPalette.Cream, () => DecorateClicked?.Invoke());
            // Side by side (not stacked) so the pair stays clear of the touch Interact button on short phones.
            ExitButtonRef = IconButton(_living.transform, "ExitButton", HousingText.ExitButton, _icons != null ? _icons.iconExit : null,
                new Vector2(1f, 1f), new Vector2(-edge, -rightColumnTop), new Vector2(buttonSize + 20f, buttonSize + 8f), UiPalette.Cream, () => ExitClicked?.Invoke());
        }

        private void BuildDecorating()
        {
            _decorating = NewRect("DecoratingHud", _safe).gameObject;
            Stretch((RectTransform)_decorating.transform);
            Transform root = _decorating.transform;

            // Top-left: Done + title + hint.
            DoneButton = IconButton(root, "DoneButton", HousingText.DoneButton, _icons != null ? _icons.iconPlace : null,
                new Vector2(0f, 1f), new Vector2(edge, -edge), new Vector2(buttonSize + 40f, titleBarHeight), UiPalette.Leaf, () => DoneClicked?.Invoke(), true);
            RectTransform titleBox = Panel(root, "TitleBar", new Vector2(0f, 1f), new Vector2(edge + buttonSize + 52f, -edge), new Vector2(520f, titleBarHeight), UiPalette.Cream, 14);
            Image gridIcon = null;
            if (_icons != null && _icons.iconGrid != null)
            {
                gridIcon = NewImage(titleBox, "GridIcon", _icons.iconGrid);
                Place((RectTransform)gridIcon.transform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(52f, 52f), new Vector2(0f, 0.5f));
                gridIcon.preserveAspect = true;
            }
            float tx = gridIcon != null ? 70f : 16f;
            Text title = NewText(titleBox, "Title", HousingText.Title, 28, FontStyle.Bold, TextAnchor.UpperLeft, UiPalette.Ink);
            Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(tx, -6f), new Vector2(430f, 34f), new Vector2(0f, 1f));
            _hint = NewText(titleBox, "Hint", "", 21, FontStyle.Normal, TextAnchor.UpperLeft, UiPalette.Ink);
            Place(_hint.rectTransform, new Vector2(0f, 1f), new Vector2(tx, -40f), new Vector2(520f - tx - 12f, titleBarHeight - 46f), new Vector2(0f, 1f));

            // Right panel.
            RectTransform panel = Panel(root, "Panel", new Vector2(1f, 1f), new Vector2(-edge, -panelTop), new Vector2(panelWidth, 10f), UiPalette.Cream, 18);
            panel.anchorMin = new Vector2(1f, 0f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(1f, 1f);
            panel.offsetMin = new Vector2(-edge - panelWidth, edge);
            panel.offsetMax = new Vector2(-edge, -panelTop);

            Text listTitle = NewText(panel, "ListTitle", HousingText.ListTitle, 22, FontStyle.Bold, TextAnchor.MiddleLeft, UiPalette.Ink);
            Place(listTitle.rectTransform, new Vector2(0f, 1f), new Vector2(14f, -6f), new Vector2(panelWidth - 28f, 30f), new Vector2(0f, 1f));

            float actionsHeight = buttonSize * 2f + 10f;
            const float feedbackHeight = 46f;
            RectTransform viewport = NewRect("ListViewport", panel);
            viewport.anchorMin = new Vector2(0f, 0f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.offsetMin = new Vector2(10f, 10f + actionsHeight + feedbackHeight + 8f);
            viewport.offsetMax = new Vector2(-10f, -40f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image vpBack = viewport.gameObject.AddComponent<Image>();
            vpBack.color = new Color(1f, 1f, 1f, 0.001f); // raycastable so drags scroll

            _listContent = NewRect("Content", viewport);
            _listContent.anchorMin = new Vector2(0f, 1f);
            _listContent.anchorMax = new Vector2(1f, 1f);
            _listContent.pivot = new Vector2(0.5f, 1f);
            _listContent.offsetMin = _listContent.offsetMax = Vector2.zero;
            var layout = _listContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = _listContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.content = _listContent;
            _scroll.viewport = viewport;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 30f;

            _emptyLabel = NewText(viewport, "Empty", HousingText.EmptyList, 20, FontStyle.Italic, TextAnchor.UpperLeft, UiPalette.Ink);
            Place(_emptyLabel.rectTransform, new Vector2(0f, 1f), new Vector2(6f, -6f), new Vector2(panelWidth - 40f, 70f), new Vector2(0f, 1f));

            _feedback = NewText(panel, "Feedback", "", 21, FontStyle.Bold, TextAnchor.MiddleLeft, UiPalette.DeepLeaf);
            _feedback.rectTransform.anchorMin = new Vector2(0f, 0f);
            _feedback.rectTransform.anchorMax = new Vector2(1f, 0f);
            _feedback.rectTransform.pivot = new Vector2(0.5f, 0f);
            _feedback.rectTransform.offsetMin = new Vector2(14f, 10f + actionsHeight + 4f);
            _feedback.rectTransform.offsetMax = new Vector2(-14f, 10f + actionsHeight + 4f + feedbackHeight);

            float bw = (panelWidth - 30f) * 0.5f;
            _primary = IconButton(panel, "PrimaryButton", HousingText.Place, _icons != null ? _icons.iconPlace : null,
                new Vector2(0f, 0f), new Vector2(10f, 10f + buttonSize + 10f), new Vector2(bw, buttonSize), UiPalette.Leaf, () => PrimaryClicked?.Invoke(), true, new Vector2(0f, 0f));
            _primaryLabel = _primary.transform.Find("Frame/Fill/Label").GetComponent<Text>();
            Transform pi = _primary.transform.Find("Frame/Fill/Icon");
            _primaryIcon = pi != null ? pi.GetComponent<Image>() : null;
            _rotate = IconButton(panel, "RotateButton", HousingText.Rotate, _icons != null ? _icons.iconRotate : null,
                new Vector2(0f, 0f), new Vector2(20f + bw, 10f + buttonSize + 10f), new Vector2(bw, buttonSize), UiPalette.Cream, () => RotateClicked?.Invoke(), false, new Vector2(0f, 0f));
            _store = IconButton(panel, "StoreButton", HousingText.Store, _icons != null ? _icons.iconStore : null,
                new Vector2(0f, 0f), new Vector2(10f, 10f), new Vector2(bw, buttonSize), UiPalette.Cream, () => StoreClicked?.Invoke(), false, new Vector2(0f, 0f));
            _cancel = IconButton(panel, "CancelButton", HousingText.Cancel, _icons != null ? _icons.iconCancel : null,
                new Vector2(0f, 0f), new Vector2(20f + bw, 10f), new Vector2(bw, buttonSize), UiPalette.Cream, () => CancelClicked?.Invoke(), false, new Vector2(0f, 0f));
        }

        private Row CreateRow(int index)
        {
            var row = new Row();
            RectTransform rt = NewRect("Row" + index, _listContent);
            rt.sizeDelta = new Vector2(0f, rowHeight);
            row.Root = rt.gameObject;
            row.Frame = rt.gameObject.AddComponent<Image>();
            row.Frame.sprite = RoundedSprites.Get(12);
            row.Frame.type = Image.Type.Sliced;
            row.Frame.color = UiPalette.Sand;
            row.Button = rt.gameObject.AddComponent<Button>();
            row.Button.targetGraphic = row.Frame;
            Row captured = row;
            row.Button.onClick.AddListener(() => { if (captured.Item != null) ItemClicked?.Invoke(captured.Item); });

            RectTransform iconBack = Panel(rt, "IconBack", new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(rowHeight - 14f, rowHeight - 14f), UiPalette.Cream, 10, new Vector2(0f, 0.5f));
            row.Icon = NewImage(iconBack, "Icon", null);
            Stretch(row.Icon.rectTransform, 4f);
            row.IconLabel = NewText(iconBack, "IconLabel", "", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(row.IconLabel.rectTransform);
            row.Name = NewText(rt, "Name", "", 22, FontStyle.Normal, TextAnchor.MiddleLeft, UiPalette.Ink);
            Place(row.Name.rectTransform, new Vector2(0f, 0.5f), new Vector2(rowHeight + 2f, 0f), new Vector2(panelWidth - rowHeight - 90f, rowHeight - 8f), new Vector2(0f, 0.5f));
            row.Count = NewText(rt, "Count", "", 22, FontStyle.Bold, TextAnchor.MiddleRight, UiPalette.Ink);
            Place(row.Count.rectTransform, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(60f, rowHeight - 8f), new Vector2(1f, 0.5f));
            return row;
        }

        private Button IconButton(Transform parent, string name, string label, Sprite icon, Vector2 anchor, Vector2 pos, Vector2 size,
            Color fill, Action onClick, bool primary = false, Vector2? pivot = null)
        {
            Vector2 pv = pivot ?? anchor;
            RectTransform shadow = Panel(parent, name, anchor, pos, size, new Color(0.25f, 0.17f, 0.10f, 0.35f), 16, pv);
            Image shadowImg = shadow.GetComponent<Image>();
            RectTransform frame = Panel(shadow, "Frame", new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), Vector2.zero, UiPalette.Wood, 16);
            Stretch(frame);
            frame.offsetMin = new Vector2(0f, 4f);
            RectTransform fillRt = Panel(frame, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, fill, 14);
            Stretch(fillRt, 4f);
            var button = shadow.gameObject.AddComponent<Button>();
            button.targetGraphic = fillRt.GetComponent<Image>();
            var colors = button.colors;
            colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.6f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick());
            shadowImg.raycastTarget = true;

            bool hasIcon = icon != null;
            Image iconImg = NewImage(fillRt, "Icon", icon);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconImg.enabled = hasIcon;
            float iconSize = Mathf.Min(size.y - 30f, 46f);
            Place(iconImg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(iconSize, iconSize), new Vector2(0.5f, 1f));
            Text text = NewText(fillRt, "Label", label, primary ? 23 : 21, FontStyle.Bold, hasIcon ? TextAnchor.LowerCenter : TextAnchor.MiddleCenter,
                primary ? Color.white : UiPalette.Ink);
            Stretch(text.rectTransform, 3f);
            return button;
        }

        private RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, int radius, Vector2? pivot = null)
        {
            RectTransform rt = NewRect(name, parent);
            Place(rt, anchor, pos, size, pivot ?? anchor);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = RoundedSprites.Get(radius);
            img.type = Image.Type.Sliced;
            img.color = color;
            return rt;
        }

        private Image NewImage(Transform parent, string name, Sprite sprite)
        {
            RectTransform rt = NewRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private Text NewText(Transform parent, string name, string value, int size, FontStyle style, TextAnchor align, Color color)
        {
            RectTransform rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font;
            t.text = value;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Min(size, 16); // never shrink below ~10 CSS px even at the minimum canvas scale
            t.resizeTextMaxSize = size;
            return t;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static RectTransform Inset(RectTransform rt, Vector4 ltrb)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(ltrb.x, ltrb.y);
            rt.offsetMax = new Vector2(-ltrb.z, -ltrb.w);
            return rt;
        }

        private static string Short(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            string[] parts = name.Split(' ');
            return parts.Length > 1 ? $"{parts[0][0]}{parts[1][0]}" : name.Substring(0, Mathf.Min(2, name.Length));
        }
    }
}

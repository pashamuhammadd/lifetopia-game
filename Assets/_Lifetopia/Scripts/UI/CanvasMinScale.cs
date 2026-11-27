using UnityEngine;
using UnityEngine.UI;

namespace Lifetopia.UI
{
    /// <summary>
    /// Keeps a "Scale With Screen Size" canvas from shrinking below a readable size on small phones.
    /// The Canvas Scaler normally maps 1280x720 reference pixels to the screen (match 0.5); on a 667x375
    /// phone that is ~0.52 CSS px per reference pixel, which makes 18 px text about 9 CSS px.
    /// When the automatic scale (converted to CSS pixels via Screen.dpi = 96 x devicePixelRatio on Web)
    /// would fall below <see cref="minCssPerReferencePixel"/>, the canvas switches to a constant scale at
    /// that minimum. Re-evaluated only when the screen size or dpi changes (resize/orientation), not per frame.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    [DisallowMultipleComponent]
    public sealed class CanvasMinScale : MonoBehaviour
    {
        [Tooltip("Smallest CSS pixels per reference pixel. 0.62 keeps 22 px text at ~13.6 CSS px and 84 px buttons at ~52 CSS px.")]
        [SerializeField, Range(0.3f, 1.5f)] private float minCssPerReferencePixel = 0.62f;

        private CanvasScaler _scaler;
        private Vector2 _reference = new Vector2(1280f, 720f);
        private float _match = 0.5f;
        private int _w, _h;
        private float _dpi;

        /// <summary>True while the minimum is applied (tests and reports).</summary>
        public bool Boosted { get; private set; }

        /// <summary>Current CSS pixels per reference pixel.</summary>
        public float CssPerReferencePixel { get; private set; }

        public void Configure(float minCss) => minCssPerReferencePixel = minCss;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            _reference = _scaler.referenceResolution;
            _match = _scaler.matchWidthOrHeight;
        }

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.width != _w || Screen.height != _h || !Mathf.Approximately(Screen.dpi, _dpi)) Apply();
        }

        private void Apply()
        {
            if (_scaler == null) return;
            _w = Screen.width;
            _h = Screen.height;
            _dpi = Screen.dpi;
            if (_w <= 0 || _h <= 0) return;
            float dpr = _dpi > 1f ? _dpi / 96f : 1f;
            float logW = Mathf.Log(_w / _reference.x, 2f), logH = Mathf.Log(_h / _reference.y, 2f);
            float auto = Mathf.Pow(2f, Mathf.Lerp(logW, logH, _match)); // device px per reference px
            float min = minCssPerReferencePixel * dpr;
            Boosted = auto < min;
            if (Boosted)
            {
                _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                _scaler.scaleFactor = min;
            }
            else
            {
                _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            }
            CssPerReferencePixel = (Boosted ? min : auto) / dpr;
        }
    }
}

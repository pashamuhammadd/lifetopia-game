using UnityEngine;

namespace Lifetopia.TimeSystem
{
    /// <summary>
    /// Tints the world for the time of day with one camera-sized quad using the
    /// <c>Lifetopia/DayNightMultiply</c> shader (multiply blend: white = unchanged).
    /// - Built-In 2D / WebGL compatible: no lights, no post-processing, no texture copies.
    /// - Drawn last on the "World" sorting layer, so all world sprites are tinted; the Screen Space
    ///   Overlay UI (HUD, inventory, dialogue) is drawn afterwards and is never tinted.
    /// - Colour is recomputed only when the game minute changes (event-driven), not every frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DayNightOverlay : MonoBehaviour
    {
        [SerializeField] private GameClockHost clockHost;
        [SerializeField] private DayNightPalette palette;
        [Tooltip("Material using the Lifetopia/DayNightMultiply shader.")]
        [SerializeField] private Material overlayMaterial;
        [Tooltip("Camera to cover. Defaults to Camera.main.")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private string sortingLayerName = "World";
        [SerializeField] private int sortingOrder = 32767;
        [Tooltip("Extra size so the quad always covers the view edges.")]
        [SerializeField, Range(1f, 2f)] private float coverage = 1.2f;

        private readonly Color32[] _colors = new Color32[4];
        private Mesh _mesh;
        private Transform _quad;
        private IGameClock _clock;
        private float _lastSize = -1f, _lastAspect = -1f;

        /// <summary>Tint currently applied (for tests and tools).</summary>
        public Color CurrentTint { get; private set; } = Color.white;

        private void Start()
        {
            if (clockHost == null || palette == null || overlayMaterial == null)
            {
                Debug.LogError("[Lifetopia] DayNightOverlay is missing references; no day/night tint.", this);
                enabled = false;
                return;
            }
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError("[Lifetopia] DayNightOverlay found no camera.", this);
                enabled = false;
                return;
            }
            BuildQuad();
            _clock = clockHost.Clock;
            _clock.MinuteChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_clock != null) _clock.MinuteChanged -= Refresh;
            if (_mesh != null) Destroy(_mesh);
            if (_quad != null) Destroy(_quad.gameObject);
        }

        private void LateUpdate()
        {
            // Two float compares per frame; the quad is resized only when the view changes.
            if (!Mathf.Approximately(targetCamera.orthographicSize, _lastSize) || !Mathf.Approximately(targetCamera.aspect, _lastAspect))
                FitToCamera();
        }

        /// <summary>Applies the palette colour for the current clock time.</summary>
        public void Refresh()
        {
            if (_mesh == null || _clock == null) return;
            double minute = _clock is GameClock exact ? exact.ExactMinuteOfDay : _clock.MinuteOfDay;
            CurrentTint = palette.Evaluate(minute);
            // Palette colours are authored as seen on screen (sRGB). Vertex colours are not converted
            // by Unity, so in a Linear colour-space project convert them here, otherwise the tint
            // would look much weaker than authored.
            Color applied = QualitySettings.activeColorSpace == ColorSpace.Linear ? CurrentTint.linear : CurrentTint;
            Color32 c = applied;
            c.a = 255;
            for (int i = 0; i < _colors.Length; i++) _colors[i] = c;
            _mesh.colors32 = _colors;
        }

        private void BuildQuad()
        {
            var go = new GameObject("DayNightOverlay (runtime)");
            go.layer = targetCamera.gameObject.layer;
            _quad = go.transform;
            _quad.SetParent(targetCamera.transform, false);
            _quad.localPosition = new Vector3(0f, 0f, targetCamera.nearClipPlane + 0.5f);
            _quad.localRotation = Quaternion.identity;

            _mesh = new Mesh { name = "DayNightQuad" };
            _mesh.vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f), new Vector3(0.5f, -0.5f) };
            _mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _mesh.colors32 = _colors;
            _mesh.bounds = new Bounds(Vector3.zero, new Vector3(1f, 1f, 1f));
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = overlayMaterial;
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            FitToCamera();
        }

        private void FitToCamera()
        {
            _lastSize = targetCamera.orthographicSize;
            _lastAspect = targetCamera.aspect;
            if (_quad == null) return;
            float height = _lastSize * 2f * coverage;
            _quad.localScale = new Vector3(height * _lastAspect, height, 1f);
        }
    }
}

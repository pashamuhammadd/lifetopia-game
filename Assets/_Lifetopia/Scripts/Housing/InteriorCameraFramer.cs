using System;
using UnityEngine;

namespace Lifetopia.Housing
{
    /// <summary>
    /// Adaptive orthographic camera for the house interior. Each frame (cheap maths, no allocation):
    /// 1. usable area = a UI RectTransform inside the safe area that excludes HUD panels (changes with
    ///    mode, screen size and orientation automatically through the Canvas Scaler);
    /// 2. size = what fits the whole room into the usable area, capped by a per-mode maximum so sprites
    ///    stay readable on small phones; the world, grid, colliders and sprites are never scaled;
    /// 3. if the room fits it is centred in the usable area, otherwise the camera follows the focus
    ///    (player, or the ghost in Decorating Mode) and is clamped so every edge can still be reached.
    /// Runs in LateUpdate after the player moved. Restoring the outdoor camera is the caller's job.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class InteriorCameraFramer : MonoBehaviour
    {
        [Tooltip("World margin around the room content.")]
        [SerializeField, Range(0f, 1f)] private float margin = 0.2f;
        [Tooltip("Readability floor in Living Mode: at least this many CSS pixels per world unit (cell). When the whole " +
                 "room would be smaller, the camera follows the player instead. Outdoor on a 390 px high phone is ~35.")]
        [SerializeField, Range(16f, 64f)] private float minCssPxPerUnitLiving = 30f;
        [Tooltip("Readability floor in Decorating Mode (cells must stay tappable); beyond it the camera follows the ghost.")]
        [SerializeField, Range(16f, 64f)] private float minCssPxPerUnitDecorating = 30f;
        [Tooltip("Smallest orthographic size (very large screens do not zoom in further).")]
        [SerializeField, Range(2f, 8f)] private float minSize = 5.0f;
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;

        private Camera _cam;
        private Rect _content;
        private RectTransform _usableArea;
        private Func<Vector2> _focus;
        private bool _decorating;
        private Vector2 _velocity;
        private readonly Vector3[] _corners = new Vector3[4];

        public bool Active => _cam != null;
        public float CurrentSize { get; private set; }
        public Rect CurrentUsable { get; private set; } = new Rect(0, 0, 1, 1);
        /// <summary>True when the whole room fits in the usable area (no following needed).</summary>
        public bool FitsWholeRoom { get; private set; }

        public void Begin(Camera cam, Rect contentWorld, Func<Vector2> focus)
        {
            _cam = cam;
            _content = contentWorld;
            _focus = focus;
            _velocity = Vector2.zero;
            Apply(true);
        }

        public void End()
        {
            _cam = null;
            _focus = null;
        }

        public void SetMode(bool decorating, RectTransform usableArea, Func<Vector2> focus)
        {
            _decorating = decorating;
            _usableArea = usableArea;
            if (focus != null) _focus = focus;
        }

        public void Snap() => Apply(true);

        private void LateUpdate()
        {
            if (_cam != null) Apply(false);
        }

        private void Apply(bool snap)
        {
            if (_cam == null) return;
            float aspect = _cam.aspect;
            Rect usable = ReadUsable();
            CurrentUsable = usable;
            float fit = InteriorCameraMath.FitSize(_content.size, margin, aspect, usable);
            float max = MaxSizeFor(_decorating ? minCssPxPerUnitDecorating : minCssPxPerUnitLiving);
            FitsWholeRoom = fit <= max;
            float size = Mathf.Max(minSize, Mathf.Min(fit, max));
            _cam.orthographicSize = size;
            CurrentSize = size;

            Vector2 focus = FitsWholeRoom || _focus == null ? _content.center : _focus();
            Vector2 half = InteriorCameraMath.UsableHalfExtents(size, aspect, usable);
            focus = InteriorCameraMath.ClampFocus(focus, _content, margin, half);
            Vector2 target = InteriorCameraMath.CameraForFocus(focus, size, aspect, usable);

            Vector2 current = _cam.transform.position;
            Vector2 next = snap || smoothTime <= 0f
                ? target
                : Vector2.SmoothDamp(current, target, ref _velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            if (snap) _velocity = Vector2.zero;
            _cam.transform.position = new Vector3(next.x, next.y, _cam.transform.position.z);
        }

        /// <summary>
        /// Largest size that keeps <paramref name="minCssPx"/> CSS pixels per unit. Screen.dpi on Web is
        /// 96 x devicePixelRatio, so this works in physical-ish units on phones and desktops alike.
        /// </summary>
        private static float MaxSizeFor(float minCssPx)
        {
            float dpi = Screen.dpi > 1f ? Screen.dpi : 96f;
            float cssHeight = Screen.height * 96f / dpi;
            return Mathf.Max(2f, cssHeight / (2f * Mathf.Max(1f, minCssPx)));
        }

        private Rect ReadUsable()
        {
            if (_usableArea == null || !_usableArea.gameObject.activeInHierarchy) return new Rect(0, 0, 1, 1);
            _usableArea.GetWorldCorners(_corners);
            // Screen Space Overlay canvas: world corners are screen pixels.
            Rect px = Rect.MinMaxRect(_corners[0].x, _corners[0].y, _corners[2].x, _corners[2].y);
            return InteriorCameraMath.Normalise(px, Screen.width, Screen.height);
        }
    }
}

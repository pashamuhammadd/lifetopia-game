using UnityEngine;
using UnityEngine.EventSystems;

namespace Lifetopia.Controls
{
    /// <summary>
    /// On-screen joystick built on UGUI pointer events, so it works with touch and mouse and
    /// supports a second finger on other buttons. Value is (0,0) whenever the finger is released,
    /// the component is disabled, or <see cref="ResetInput"/> is called.
    /// Put it on the joystick background Image (Raycast Target on).
    /// </summary>
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Tooltip("Moving knob; child of this background.")]
        [SerializeField] private RectTransform handle;

        [Tooltip("Max knob travel in canvas units. 0 = half of the background width.")]
        [SerializeField, Min(0f)] private float radius;

        [Tooltip("Tilt below this fraction is ignored.")]
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.15f;

        private RectTransform _background;
        private int _pointerId = int.MinValue;

        public Vector2 Value { get; private set; }
        public bool IsHeld => _pointerId != int.MinValue;

        private float Radius => radius > 0f ? radius : _background.rect.width * 0.5f;

        private void Awake() => _background = (RectTransform)transform;

        private void OnDisable() => ResetInput();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsHeld) return; // ignore a second finger on the same stick
            _pointerId = eventData.pointerId;
            UpdateValue(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == _pointerId) UpdateValue(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _pointerId) ResetInput();
        }

        public void ResetInput()
        {
            _pointerId = int.MinValue;
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }

        private void UpdateValue(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _background, eventData.position, eventData.pressEventCamera, out Vector2 local))
                return;

            float r = Radius;
            Vector2 offset = local - _background.rect.center;
            Vector2 normalized = Vector2.ClampMagnitude(offset / r, 1f);
            Value = normalized.magnitude < deadZone ? Vector2.zero : normalized;
            if (handle != null) handle.anchoredPosition = normalized * r;
        }
    }
}

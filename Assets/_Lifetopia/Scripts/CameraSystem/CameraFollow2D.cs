using Lifetopia.World;
using UnityEngine;

namespace Lifetopia.CameraSystem
{
    /// <summary>
    /// Smoothly follows a target with an orthographic camera and stays inside the map.
    /// Runs in LateUpdate, after the player's interpolated position for this frame is known,
    /// so the camera and player move in lockstep (no jitter).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private WorldBounds bounds;

        [Tooltip("Look slightly above the feet so the character sits near screen centre.")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 0.75f);

        [Tooltip("Seconds to catch up with the target. 0 = locked to the target.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;

        private Camera _camera;
        private Vector2 _velocity;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Awake() => _camera = GetComponent<Camera>();

        private void LateUpdate()
        {
            if (target == null) return;

            Vector2 current = transform.position;
            Vector2 desired = (Vector2)target.position + offset;
            Vector2 next = smoothTime > 0f
                ? Vector2.SmoothDamp(current, desired, ref _velocity, smoothTime, Mathf.Infinity, Time.deltaTime)
                : desired;

            Vector2 clamped = ClampToBounds(next);
            if (!Mathf.Approximately(clamped.x, next.x)) _velocity.x = 0f;
            if (!Mathf.Approximately(clamped.y, next.y)) _velocity.y = 0f;
            SetPosition(clamped);
        }

        /// <summary>Jumps straight to the target (used on spawn / scene load).</summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            _velocity = Vector2.zero;
            SetPosition(ClampToBounds((Vector2)target.position + offset));
        }

        private Vector2 ClampToBounds(Vector2 position)
        {
            if (bounds == null || _camera == null || !_camera.orthographic) return position;
            Vector2 half = CameraBoundsMath.HalfExtents(_camera.orthographicSize, _camera.aspect);
            return CameraBoundsMath.Clamp(position, bounds.Rect, half);
        }

        private void SetPosition(Vector2 position)
            => transform.position = new Vector3(position.x, position.y, transform.position.z);
    }
}

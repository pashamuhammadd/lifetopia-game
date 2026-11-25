using Lifetopia.Controls;
using UnityEngine;

namespace Lifetopia.Player
{
    /// <summary>
    /// Moves the player with a dynamic Rigidbody2D so walls, water and buildings block it.
    /// Runs in FixedUpdate (physics step); Rigidbody interpolation keeps rendering smooth
    /// between physics steps, which is what prevents camera jitter.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        [Tooltip("Top speed in world units (tiles) per second.")]
        [SerializeField, Min(0f)] private float moveSpeed = 4f;

        [Tooltip("How fast the player reaches top speed (units/s²). Higher = snappier.")]
        [SerializeField, Min(0.01f)] private float acceleration = 40f;

        [Tooltip("How fast the player stops when input is released (units/s²).")]
        [SerializeField, Min(0.01f)] private float deceleration = 60f;

        private Rigidbody2D _body;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        public Vector2 Velocity => _body != null ? _body.linearVelocity : Vector2.zero;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Dynamic;
            _body.gravityScale = 0f;
            _body.constraints = RigidbodyConstraints2D.FreezeRotation;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void FixedUpdate()
        {
            Vector2 intent = input != null ? input.Move : Vector2.zero;
            Vector2 target = intent * moveSpeed;
            float rate = intent.sqrMagnitude > 0f ? acceleration : deceleration;
            _body.linearVelocity = Vector2.MoveTowards(_body.linearVelocity, target, rate * Time.fixedDeltaTime);
        }

        /// <summary>Instantly places the player (used by spawn points); clears momentum.</summary>
        public void Teleport(Vector2 position)
        {
            if (_body == null) _body = GetComponent<Rigidbody2D>();
            _body.position = position;
            _body.linearVelocity = Vector2.zero;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }
    }
}

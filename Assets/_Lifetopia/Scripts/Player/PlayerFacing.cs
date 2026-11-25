using System;
using Lifetopia.Controls;
using UnityEngine;

namespace Lifetopia.Player
{
    /// <summary>
    /// Owns the character's presentation state: which of the four directions it faces and
    /// whether it is Idle or Walking. Visual components (placeholder now, Animator in M2) read
    /// this; they never read input directly.
    /// Facing follows input intent, so pushing against a wall still turns the character.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerFacing : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private FacingDirection initialFacing = FacingDirection.Down;

        [Tooltip("Near-diagonal tolerance that keeps the current facing (prevents flicker).")]
        [SerializeField, Range(0f, 0.5f)] private float diagonalTolerance = 0.2f;

        /// <summary>Raised when facing or walking state changes.</summary>
        public event Action StateChanged;

        public FacingDirection Facing { get; private set; }
        public bool IsWalking { get; private set; }

        private void Awake() => Facing = initialFacing;

        private void Update()
        {
            Vector2 move = input != null ? input.Move : Vector2.zero;
            FacingDirection facing = MovementMath.ResolveFacing(move, Facing, diagonalTolerance);
            bool walking = move.sqrMagnitude > 0f;

            if (facing == Facing && walking == IsWalking) return;
            Facing = facing;
            IsWalking = walking;
            StateChanged?.Invoke();
        }
    }
}

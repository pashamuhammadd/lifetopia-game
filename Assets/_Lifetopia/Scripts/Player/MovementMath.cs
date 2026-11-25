using UnityEngine;

namespace Lifetopia.Player
{
    /// <summary>Pure movement helpers (no Unity objects) so they can be unit tested.</summary>
    public static class MovementMath
    {
        /// <summary>
        /// Removes stick noise below <paramref name="deadZone"/> and clamps the vector to length 1.
        /// Keyboard diagonal (1,1) becomes (0.707,0.707), so diagonal speed equals straight speed.
        /// Partial joystick tilt below 1 is preserved for analog walking.
        /// </summary>
        public static Vector2 ClampInput(Vector2 raw, float deadZone = 0.1f)
        {
            float sqr = raw.sqrMagnitude;
            if (sqr < deadZone * deadZone) return Vector2.zero;
            return sqr > 1f ? raw / Mathf.Sqrt(sqr) : raw;
        }

        /// <summary>
        /// Chooses the facing for a movement vector. Near-diagonal input keeps the current
        /// facing when it still agrees with one of the axes, which prevents flicker
        /// between e.g. Right and Up while walking diagonally.
        /// </summary>
        public static FacingDirection ResolveFacing(Vector2 move, FacingDirection current, float diagonalTolerance = 0.2f)
        {
            if (move.sqrMagnitude < 0.0001f) return current;

            float ax = Mathf.Abs(move.x);
            float ay = Mathf.Abs(move.y);
            bool nearDiagonal = Mathf.Abs(ax - ay) <= diagonalTolerance * Mathf.Max(ax, ay);
            if (nearDiagonal && Agrees(current, move)) return current;

            if (ax > ay) return move.x > 0f ? FacingDirection.Right : FacingDirection.Left;
            return move.y > 0f ? FacingDirection.Up : FacingDirection.Down;
        }

        private static bool Agrees(FacingDirection facing, Vector2 move)
        {
            switch (facing)
            {
                case FacingDirection.Right: return move.x > 0f;
                case FacingDirection.Left: return move.x < 0f;
                case FacingDirection.Up: return move.y > 0f;
                default: return move.y < 0f;
            }
        }
    }
}

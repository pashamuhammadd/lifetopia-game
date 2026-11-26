namespace Lifetopia.Player
{
    /// <summary>
    /// Pure helpers for frame-by-frame sprite animation (no Unity objects, unit tested).
    /// </summary>
    public static class SpriteFrameMath
    {
        /// <summary>
        /// Looping frame index for <paramref name="elapsed"/> seconds at <paramref name="fps"/>.
        /// Returns 0 when there is at most one frame, fps is not positive or time is negative,
        /// so a single static sprite never "animates".
        /// </summary>
        public static int FrameIndex(float elapsed, float fps, int frameCount)
        {
            if (frameCount <= 1 || fps <= 0f || elapsed <= 0f) return 0;
            long step = (long)(elapsed * fps);
            return (int)(step % frameCount);
        }

        /// <summary>
        /// Walk art only plays while the player both wants to move and actually moves.
        /// Pushing straight into a wall (intent, but speed ~0) shows idle instead of walking in place.
        /// </summary>
        public static bool IsVisiblyWalking(bool moveIntent, float speed, float minWalkSpeed)
        {
            return moveIntent && speed >= minWalkSpeed;
        }

        /// <summary>
        /// Picks which clip to show when art is missing, without inventing motion or mirroring:
        /// 1. requested state and direction;
        /// 2. idle in the requested direction (walking without walk art shows a still pose);
        /// 3. idle facing Down (the A0 static front sprite).
        /// <paramref name="hasFrames"/> answers whether a (walking, direction) clip has frames.
        /// Returns false when not even idle Down exists.
        /// </summary>
        public static bool ResolveClip(bool walking, FacingDirection direction,
            System.Func<bool, FacingDirection, bool> hasFrames,
            out bool resolvedWalking, out FacingDirection resolvedDirection)
        {
            resolvedWalking = walking;
            resolvedDirection = direction;
            if (hasFrames(walking, direction)) return true;

            resolvedWalking = false;
            if (walking && hasFrames(false, direction)) return true;

            resolvedDirection = FacingDirection.Down;
            return hasFrames(false, FacingDirection.Down);
        }
    }
}

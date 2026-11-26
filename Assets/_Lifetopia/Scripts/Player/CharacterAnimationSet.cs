using System;
using UnityEngine;

namespace Lifetopia.Player
{
    /// <summary>
    /// Authored frame lists for one baked character (Art Bible: 128x128 cells, shared foot
    /// pivot, rows D/L/R/U). Read-only at runtime. Empty lists are allowed while art is
    /// pending; SpriteCharacterView then falls back as documented in SpriteFrameMath.ResolveClip.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Character Animation Set", fileName = "CharacterAnimationSet")]
    public sealed class CharacterAnimationSet : ScriptableObject
    {
        [Serializable]
        public sealed class DirectionalClip
        {
            [Min(0f)] public float framesPerSecond = 4f;
            public Sprite[] down = Array.Empty<Sprite>();
            public Sprite[] left = Array.Empty<Sprite>();
            public Sprite[] right = Array.Empty<Sprite>();
            public Sprite[] up = Array.Empty<Sprite>();

            public Sprite[] For(FacingDirection direction) => direction switch
            {
                FacingDirection.Left => left,
                FacingDirection.Right => right,
                FacingDirection.Up => up,
                _ => down
            };
        }

        [Tooltip("DECISIONS.md: 4 frames per direction at about 4 fps.")]
        [SerializeField] private DirectionalClip idle = new DirectionalClip { framesPerSecond = 4f };

        [Tooltip("DECISIONS.md: 8 frames per direction, 8-10 fps starting point.")]
        [SerializeField] private DirectionalClip walk = new DirectionalClip { framesPerSecond = 9f };

        public DirectionalClip Clip(bool walking) => walking ? walk : idle;

        public Sprite[] Frames(bool walking, FacingDirection direction) => Clip(walking)?.For(direction);

        /// <summary>True when the clip has at least one frame and no empty slots.</summary>
        public bool HasFrames(bool walking, FacingDirection direction)
        {
            Sprite[] frames = Frames(walking, direction);
            if (frames == null || frames.Length == 0) return false;
            foreach (Sprite s in frames)
            {
                if (s == null) return false;
            }
            return true;
        }
    }
}

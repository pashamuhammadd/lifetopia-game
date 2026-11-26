using UnityEngine;

namespace Lifetopia.Player
{
    /// <summary>
    /// Shows the player's baked sprite frames from a CharacterAnimationSet.
    /// Direction comes from PlayerFacing (last facing is kept when the player stops).
    /// Walk plays only while there is move intent AND real Rigidbody speed, so pushing into a
    /// wall shows idle, not walking in place. It never mirrors, bobs or scales the sprite to fake
    /// motion. Missing clips fall back to idle, then to idle Down (SpriteFrameMath.ResolveClip).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpriteCharacterView : MonoBehaviour
    {
        [SerializeField] private PlayerFacing facing;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CharacterAnimationSet animationSet;

        [Tooltip("Optional. When set, walk art needs actual speed, not only input.")]
        [SerializeField] private PlayerMotor2D motor;

        [Tooltip("Speed (units/s) at or above which the walk clip plays.")]
        [SerializeField, Min(0f)] private float minWalkSpeed = 0.5f;

        [Tooltip("How long a blocked-but-pushing state must last before switching walk -> idle. " +
                 "Prevents flicker when brushing past colliders.")]
        [SerializeField, Min(0f)] private float blockedDebounce = 0.08f;

        private Sprite[] _frames;
        private float _fps;
        private float _clipTime;
        private int _shownIndex = -1;
        private bool _hasClip;
        private bool _requestedWalking;
        private FacingDirection _requestedDirection;
        private float _pendingTime;
        private System.Func<bool, FacingDirection, bool> _hasFrames;

        /// <summary>Clip actually shown after fallback (for tests and debugging).</summary>
        public bool ShownWalking { get; private set; }
        public FacingDirection ShownDirection { get; private set; }
        public int ShownFrame => _shownIndex;
        public Sprite ShownSprite => spriteRenderer != null ? spriteRenderer.sprite : null;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            _hasFrames = (w, d) => animationSet != null && animationSet.HasFrames(w, d);
        }

        private void OnEnable()
        {
            _hasClip = false;
            _pendingTime = 0f;
            UpdateState(0f);
        }

        private void LateUpdate()
        {
            UpdateState(Time.deltaTime);
            if (_frames == null || _frames.Length <= 1) return;
            _clipTime += Time.deltaTime;
            ShowFrame(SpriteFrameMath.FrameIndex(_clipTime, _fps, _frames.Length));
        }

        private void UpdateState(float deltaTime)
        {
            if (facing == null) return;
            bool intent = facing.IsWalking;
            float speed = motor != null ? motor.Velocity.magnitude : (intent ? float.MaxValue : 0f);
            bool walking = SpriteFrameMath.IsVisiblyWalking(intent, speed, minWalkSpeed);
            FacingDirection direction = facing.Facing;

            if (_hasClip && _requestedWalking && !walking && intent)
            {
                // Still pushing but speed dropped (blocked): only switch to idle if it lasts.
                // Starting to walk and releasing input stay immediate.
                _pendingTime += deltaTime;
                if (_pendingTime < blockedDebounce) walking = _requestedWalking;
            }
            else
            {
                _pendingTime = 0f;
            }

            if (_hasClip && walking == _requestedWalking && direction == _requestedDirection) return;
            _pendingTime = 0f;
            _requestedWalking = walking;
            _requestedDirection = direction;
            _hasClip = true;
            SelectClip(walking, direction);
        }

        private void SelectClip(bool walkingRequested, FacingDirection directionRequested)
        {
            _clipTime = 0f;
            _shownIndex = -1;
            _frames = null;
            if (spriteRenderer == null || animationSet == null) return;

            if (!SpriteFrameMath.ResolveClip(walkingRequested, directionRequested, _hasFrames,
                    out bool walking, out FacingDirection direction))
            {
                return; // No art at all: keep whatever sprite the renderer already has.
            }

            ShownWalking = walking;
            ShownDirection = direction;
            _frames = animationSet.Frames(walking, direction);
            _fps = animationSet.Clip(walking).framesPerSecond;
            ShowFrame(0);
        }

        private void ShowFrame(int index)
        {
            if (index == _shownIndex) return;
            _shownIndex = index;
            spriteRenderer.sprite = _frames[index];
        }
    }
}

using UnityEngine;

namespace Lifetopia.Player
{
    /// <summary>
    /// TEMPORARY greybox visual for the player. Shows facing with a marker on the head and
    /// a small bob while walking. Replace with a sprite/Animator view in M2; PlayerFacing
    /// stays the same, so nothing else needs to change.
    /// </summary>
    public sealed class PlaceholderCharacterView : MonoBehaviour
    {
        [SerializeField] private PlayerFacing facing;

        [Tooltip("Body pivot that bobs while walking.")]
        [SerializeField] private Transform body;

        [Tooltip("Small marker that shows where the character looks.")]
        [SerializeField] private Transform facingMarker;

        [Tooltip("Optional debug text such as 'WALK D'. Disable the object to hide it.")]
        [SerializeField] private TextMesh stateLabel;

        [Header("Marker positions (local, relative to body)")]
        [SerializeField] private Vector2 markerDown = new Vector2(0f, 1.05f);
        [SerializeField] private Vector2 markerLeft = new Vector2(-0.26f, 1.15f);
        [SerializeField] private Vector2 markerRight = new Vector2(0.26f, 1.15f);
        [SerializeField] private Vector2 markerUp = new Vector2(0f, 1.38f);

        [Header("Walk bob")]
        [SerializeField, Min(0f)] private float bobHeight = 0.06f;
        [SerializeField, Min(0f)] private float bobStepsPerSecond = 4f;

        private static readonly string[] LabelTexts =
        {
            "IDLE D", "IDLE L", "IDLE R", "IDLE U",
            "WALK D", "WALK L", "WALK R", "WALK U"
        };

        private Vector3 _bodyBase;
        private float _walkTime;

        private void Awake()
        {
            if (body != null) _bodyBase = body.localPosition;
        }

        private void OnEnable()
        {
            if (facing != null) facing.StateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (facing != null) facing.StateChanged -= Refresh;
        }

        private void LateUpdate()
        {
            if (body == null || facing == null) return;
            if (facing.IsWalking)
            {
                _walkTime += Time.deltaTime;
                float bob = Mathf.Abs(Mathf.Sin(_walkTime * bobStepsPerSecond * Mathf.PI)) * bobHeight;
                body.localPosition = _bodyBase + new Vector3(0f, bob, 0f);
            }
            else if (_walkTime != 0f)
            {
                _walkTime = 0f;
                body.localPosition = _bodyBase;
            }
        }

        private void Refresh()
        {
            if (facing == null) return;
            FacingDirection dir = facing.Facing;

            if (facingMarker != null)
            {
                Vector2 p = dir switch
                {
                    FacingDirection.Left => markerLeft,
                    FacingDirection.Right => markerRight,
                    FacingDirection.Up => markerUp,
                    _ => markerDown
                };
                facingMarker.localPosition = new Vector3(p.x, p.y, 0f);
                // Seen from behind the "face" is hidden; the marker sits on top as a hair tuft.
                facingMarker.localScale = dir == FacingDirection.Up ? new Vector3(0.3f, 0.12f, 1f) : new Vector3(0.16f, 0.16f, 1f);
            }

            if (stateLabel != null)
            {
                stateLabel.text = LabelTexts[(facing.IsWalking ? 4 : 0) + (int)dir];
            }
        }
    }
}

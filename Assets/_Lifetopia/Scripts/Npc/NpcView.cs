using Lifetopia.Player;
using UnityEngine;

namespace Lifetopia.Npc
{
    /// <summary>
    /// Shows a static NPC: the directional sprite from <see cref="NpcVisualSet"/> when assigned (local scene),
    /// otherwise the labelled placeholder child (public scene). The root sits at the feet; renderers sort by
    /// pivot on the World layer like the player. No walking or action animation in M3.5.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcView : MonoBehaviour
    {
        [SerializeField] private NpcVisualSet visuals;
        [SerializeField] private SpriteRenderer art;
        [Tooltip("Shown when no visual set is assigned (public placeholder).")]
        [SerializeField] private GameObject placeholder;
        [SerializeField] private FacingDirection restingFacing = FacingDirection.Down;

        public FacingDirection Facing { get; private set; }
        public NpcVisualSet Visuals => visuals;
        public bool UsesArt => visuals != null && visuals.IsComplete && art != null;

        private void Awake()
        {
            if (art != null) art.enabled = UsesArt;
            if (placeholder != null) placeholder.SetActive(!UsesArt);
            SetFacing(restingFacing);
        }

        /// <summary>Turns to look at <paramref name="worldPoint"/> (4 directions, larger axis wins).</summary>
        public void FaceTowards(Vector2 worldPoint)
        {
            SetFacing(DirectionTo((Vector2)transform.position, worldPoint, restingFacing));
        }

        public void ResetFacing() => SetFacing(restingFacing);

        public void SetFacing(FacingDirection facing)
        {
            Facing = facing;
            if (UsesArt) art.sprite = visuals.For(facing);
        }

        /// <summary>Pure helper (tested): 4-way direction from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static FacingDirection DirectionTo(Vector2 from, Vector2 to, FacingDirection fallback)
        {
            Vector2 d = to - from;
            if (d.sqrMagnitude < 0.0001f) return fallback;
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x < 0f ? FacingDirection.Left : FacingDirection.Right;
            return d.y < 0f ? FacingDirection.Down : FacingDirection.Up;
        }
    }
}

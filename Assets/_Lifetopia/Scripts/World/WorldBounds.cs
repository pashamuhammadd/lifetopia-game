using UnityEngine;

namespace Lifetopia.World
{
    /// <summary>
    /// Playable map rectangle, centred on this transform. Single source of truth for:
    /// - the invisible wall (EdgeCollider2D loop on this object, rebuilt from Size), and
    /// - the camera limits (CameraFollow2D reads <see cref="Rect"/>).
    /// Edit Size in the Inspector; the green gizmo shows the area in the Scene view.
    /// </summary>
    [RequireComponent(typeof(EdgeCollider2D))]
    public sealed class WorldBounds : MonoBehaviour
    {
        [Tooltip("Map size in world units (1 unit = 1 tile = 64 px).")]
        [SerializeField] private Vector2 size = new Vector2(32f, 22f);

        private static readonly Vector2[] Corners = new Vector2[5];

        public Rect Rect
        {
            get
            {
                Vector2 c = transform.position;
                return new Rect(c - size * 0.5f, size);
            }
        }

        private void Awake() => ApplyCollider();

        private void OnValidate()
        {
            size = Vector2.Max(size, Vector2.one);
            ApplyCollider();
        }

        private void ApplyCollider()
        {
            var edge = GetComponent<EdgeCollider2D>();
            if (edge == null) return;
            Vector2 h = size * 0.5f;
            Corners[0] = new Vector2(-h.x, -h.y);
            Corners[1] = new Vector2(-h.x, h.y);
            Corners[2] = new Vector2(h.x, h.y);
            Corners[3] = new Vector2(h.x, -h.y);
            Corners[4] = Corners[0];
            edge.points = Corners;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
            Rect r = Rect;
            Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}

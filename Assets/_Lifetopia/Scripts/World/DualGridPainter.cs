using UnityEngine;
using UnityEngine.Tilemaps;

namespace Lifetopia.World
{
    /// <summary>
    /// Paints the dual-grid VISUAL tilemap from the LOGICAL tilemaps, once at start (and on demand in the
    /// Editor via the context menu). Logical tilemaps keep their colliders and gameplay meaning; only their
    /// renderers may be hidden in scenes that use this painter (local art scene).
    /// - Path/sand cells: any tile on <see cref="pathSources"/>.
    /// - Water cells: any tile on <see cref="waterSources"/> (e.g. Water + Dock, so the sea continues under the dock).
    /// - Cells outside <see cref="area"/> repeat the nearest edge cell (no grass rim along the map border).
    /// The target tilemap must sit at local position (-0.5, -0.5) so cell (vx, vy) is centred on vertex (vx, vy).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DualGridPainter : MonoBehaviour
    {
        [SerializeField] private DualGridTileSet tileSet;
        [SerializeField] private Tilemap target;
        [SerializeField] private Tilemap[] pathSources = new Tilemap[0];
        [SerializeField] private Tilemap[] waterSources = new Tilemap[0];
        [Tooltip("Logical cell area (min inclusive, size). Usually the WorldBounds rectangle.")]
        [SerializeField] private RectInt area = new RectInt(-16, -15, 32, 26);
        [Tooltip("Renderers of the logical tilemaps to hide after painting (their colliders stay active).")]
        [SerializeField] private TilemapRenderer[] hideRenderers = new TilemapRenderer[0];
        [SerializeField] private bool paintOnAwake = true;

        /// <summary>Number of vertices the art set could not draw in the last paint (should be 0).</summary>
        public int UnsupportedCount { get; private set; }
        public int PaintedCount { get; private set; }

        private void Awake()
        {
            if (paintOnAwake) Paint();
        }

        [ContextMenu("Paint")]
        public void Paint()
        {
            if (tileSet == null || target == null)
            {
                Debug.LogError("[Lifetopia] DualGridPainter is missing its tile set or target tilemap.", this);
                return;
            }
            target.ClearAllTiles();
            int count = (area.width + 1) * (area.height + 1);
            var positions = new Vector3Int[count];
            var tiles = new TileBase[count];
            int n = 0, unsupported = 0;
            for (int vy = area.yMin; vy <= area.yMax; vy++)
            {
                for (int vx = area.xMin; vx <= area.xMax; vx++)
                {
                    int pathMask = DualGridMask.AtVertex(IsPath, vx, vy);
                    int waterMask = DualGridMask.AtVertex(IsWater, vx, vy);
                    DualGridChoice choice = DualGridMask.Classify(pathMask, waterMask);
                    TileBase tile = tileSet.For(choice);
                    if (tile == null)
                    {
                        unsupported++;
                        if (unsupported <= 5) Debug.LogWarning($"[Lifetopia] Dual grid: no tile for vertex ({vx},{vy}) {choice} (path {pathMask}).", this);
                        // Never silently draw the wrong edge: fall back to the plainest tile of the dominant side.
                        tile = waterMask != 0 ? tileSet.Water : tileSet.Terrain(pathMask);
                    }
                    positions[n] = new Vector3Int(vx, vy, 0);
                    tiles[n] = tile;
                    n++;
                }
            }
            target.SetTiles(positions, tiles);
            UnsupportedCount = unsupported;
            PaintedCount = n;
            foreach (TilemapRenderer r in hideRenderers) if (r != null) r.enabled = false;
        }

        private Vector3Int Clamp(int x, int y) =>
            new Vector3Int(Mathf.Clamp(x, area.xMin, area.xMax - 1), Mathf.Clamp(y, area.yMin, area.yMax - 1), 0);

        private bool IsPath(int x, int y) => Any(pathSources, Clamp(x, y));
        private bool IsWater(int x, int y) => Any(waterSources, Clamp(x, y));

        private static bool Any(Tilemap[] maps, Vector3Int cell)
        {
            if (maps == null) return false;
            for (int i = 0; i < maps.Length; i++)
                if (maps[i] != null && maps[i].HasTile(cell)) return true;
            return false;
        }
    }
}

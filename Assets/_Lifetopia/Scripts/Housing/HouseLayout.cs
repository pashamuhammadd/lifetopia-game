using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.Housing
{
    /// <summary>Why a placement is (not) allowed. Anything but <see cref="Ok"/> means nothing changed.</summary>
    public enum PlacementResult
    {
        Ok = 0,
        InvalidItem,
        UnsupportedOrientation,
        OutOfBounds,
        Overlap,
        ReservedCell,
        PlayerInTheWay,
        BlocksExit,
        NotFound,
        NotInInventory,
        InventoryFull,
        Busy,
    }

    /// <summary>One placed piece. Cell = south-west (bottom-left) cell of the footprint.</summary>
    public readonly struct FurniturePlacement
    {
        public readonly int InstanceId;
        public readonly FurnitureDefinition Definition;
        public readonly Vector2Int Cell;
        public readonly FurnitureOrientation Orientation;

        public FurniturePlacement(int instanceId, FurnitureDefinition definition, Vector2Int cell, FurnitureOrientation orientation)
        {
            InstanceId = instanceId; Definition = definition; Cell = cell; Orientation = orientation;
        }

        public Vector2Int Size => Definition.Footprint(Orientation);
        public RectInt Rect => new RectInt(Cell, Size);
        public string ItemId => Definition.ItemId;
    }

    /// <summary>Versioned plain data of a house layout (for the later local save milestone; IDs + numbers only).</summary>
    [Serializable]
    public sealed class HouseLayoutData
    {
        public const int CurrentVersion = 1;

        [Serializable]
        public struct PlacementData
        {
            public int instanceId;
            public string itemId;
            public int x;
            public int y;
            public int orientation;
        }

        public int version = CurrentVersion;
        public int nextInstanceId = 1;
        public bool starterGranted;
        public List<PlacementData> placements = new List<PlacementData>();
    }

    /// <summary>
    /// Logical house grid (pure C#, no scene objects). Origin = south-west floor corner, x east, y north,
    /// 1 cell = 1 world unit. Tracks placements and answers "may this piece go here?":
    /// - inside the room, orientation supported by validated art;
    /// - no overlap with another piece on the same layer (rugs may lie under furniture);
    /// - not on reserved cells (exit and interior spawn);
    /// - solid pieces not on the cells the player stands on;
    /// - after a solid piece is added/moved, every free cell must still connect (4-way) to the exit,
    ///   so the player can never be trapped or cut off from the door.
    /// Mutations are internal: gameplay changes the layout only through <see cref="HouseDecorService"/>.
    /// </summary>
    public sealed class HouseLayout
    {
        private readonly Dictionary<int, FurniturePlacement> _placements = new Dictionary<int, FurniturePlacement>();
        private readonly List<int> _order = new List<int>();
        private readonly HashSet<Vector2Int> _reserved;
        private readonly Vector2Int[] _exitCells;

        // Scratch buffers for the reachability check (no allocation per query).
        private bool[] _solid;
        private bool[] _visited;
        private int[] _queue;

        public Vector2Int Size { get; }
        public int NextInstanceId { get; private set; } = 1;
        public bool StarterGranted { get; internal set; }
        public int Count => _placements.Count;

        /// <summary>Raised after every change (add, move, remove, load).</summary>
        public event Action Changed;

        public HouseLayout(Vector2Int size, IEnumerable<Vector2Int> exitCells, IEnumerable<Vector2Int> extraReserved = null)
        {
            Size = Vector2Int.Max(size, Vector2Int.one);
            _exitCells = exitCells != null ? new List<Vector2Int>(exitCells).ToArray() : Array.Empty<Vector2Int>();
            _reserved = new HashSet<Vector2Int>(_exitCells);
            if (extraReserved != null) foreach (Vector2Int c in extraReserved) _reserved.Add(c);
            int n = Size.x * Size.y;
            _solid = new bool[n];
            _visited = new bool[n];
            _queue = new int[n];
        }

        public IReadOnlyCollection<Vector2Int> ReservedCells => _reserved;
        public IReadOnlyList<Vector2Int> ExitCells => _exitCells;

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Size.x && c.y < Size.y;
        public bool IsReserved(Vector2Int c) => _reserved.Contains(c);

        public bool TryGet(int instanceId, out FurniturePlacement placement) => _placements.TryGetValue(instanceId, out placement);

        /// <summary>Placements in the order they were added.</summary>
        public IEnumerable<FurniturePlacement> Placements
        {
            get { foreach (int id in _order) yield return _placements[id]; }
        }

        /// <summary>Topmost piece covering <paramref name="cell"/> (Object layer wins over a rug), or false.</summary>
        public bool TryGetAt(Vector2Int cell, out FurniturePlacement placement)
        {
            placement = default;
            bool found = false;
            foreach (int id in _order)
            {
                FurniturePlacement p = _placements[id];
                if (!p.Rect.Contains(cell)) continue;
                if (!found || p.Definition.Layer == FurnitureLayer.Object) { placement = p; found = true; }
                if (p.Definition.Layer == FurnitureLayer.Object) break;
            }
            return found;
        }

        /// <summary>True when a solid piece covers the cell.</summary>
        public bool IsBlocked(Vector2Int cell)
        {
            foreach (FurniturePlacement p in _placements.Values)
                if (p.Definition.BlocksMovement && p.Rect.Contains(cell)) return true;
            return false;
        }

        public int CountOf(string itemId)
        {
            int n = 0;
            foreach (FurniturePlacement p in _placements.Values) if (p.ItemId == itemId) n++;
            return n;
        }

        /// <summary>
        /// Checks a placement without changing anything. <paramref name="ignoreInstanceId"/> excludes the
        /// piece being moved; <paramref name="playerCells"/> are the cells the player currently occupies.
        /// </summary>
        public PlacementResult Evaluate(FurnitureDefinition def, Vector2Int cell, FurnitureOrientation orientation,
            int ignoreInstanceId = 0, IReadOnlyCollection<Vector2Int> playerCells = null)
        {
            if (def == null || def.Item == null) return PlacementResult.InvalidItem;
            if (!def.Supports(orientation)) return PlacementResult.UnsupportedOrientation;
            var rect = new RectInt(cell, def.Footprint(orientation));
            if (rect.xMin < 0 || rect.yMin < 0 || rect.xMax > Size.x || rect.yMax > Size.y) return PlacementResult.OutOfBounds;

            foreach (Vector2Int c in rect.allPositionsWithin)
                if (_reserved.Contains(c)) return PlacementResult.ReservedCell;

            foreach (FurniturePlacement other in _placements.Values)
            {
                if (other.InstanceId == ignoreInstanceId) continue;
                if (other.Definition.Layer == def.Layer && other.Rect.Overlaps(rect)) return PlacementResult.Overlap;
            }

            if (!def.BlocksMovement) return PlacementResult.Ok;

            if (playerCells != null)
                foreach (Vector2Int c in playerCells)
                    if (rect.Contains(c)) return PlacementResult.PlayerInTheWay;

            return AllFreeCellsReachExit(rect, ignoreInstanceId) ? PlacementResult.Ok : PlacementResult.BlocksExit;
        }

        /// <summary>
        /// Flood fill (4-way) from the exit cells over cells that are not solid, with <paramref name="extra"/>
        /// treated as solid and <paramref name="ignoreInstanceId"/> treated as removed. True when every free
        /// cell was reached.
        /// </summary>
        private bool AllFreeCellsReachExit(RectInt extra, int ignoreInstanceId)
        {
            int w = Size.x, h = Size.y;
            Array.Clear(_solid, 0, _solid.Length);
            Array.Clear(_visited, 0, _visited.Length);
            foreach (FurniturePlacement p in _placements.Values)
            {
                if (p.InstanceId == ignoreInstanceId || !p.Definition.BlocksMovement) continue;
                Mark(p.Rect);
            }
            Mark(extra);

            int head = 0, tail = 0;
            foreach (Vector2Int e in _exitCells)
            {
                if (!InBounds(e)) continue;
                int i = e.y * w + e.x;
                if (_solid[i] || _visited[i]) continue;
                _visited[i] = true;
                _queue[tail++] = i;
            }
            while (head < tail)
            {
                int i = _queue[head++];
                int x = i % w, y = i / w;
                if (x > 0) Visit(i - 1);
                if (x < w - 1) Visit(i + 1);
                if (y > 0) Visit(i - w);
                if (y < h - 1) Visit(i + w);
            }
            for (int i = 0; i < _solid.Length; i++) if (!_solid[i] && !_visited[i]) return false;
            return true;

            void Visit(int j)
            {
                if (_solid[j] || _visited[j]) return;
                _visited[j] = true;
                _queue[tail++] = j;
            }

            void Mark(RectInt r)
            {
                foreach (Vector2Int c in r.allPositionsWithin)
                    if (InBounds(c)) _solid[c.y * w + c.x] = true;
            }
        }

        internal int Add(FurnitureDefinition def, Vector2Int cell, FurnitureOrientation orientation)
        {
            int id = NextInstanceId++;
            _placements.Add(id, new FurniturePlacement(id, def, cell, orientation));
            _order.Add(id);
            Changed?.Invoke();
            return id;
        }

        internal void Move(int instanceId, Vector2Int cell, FurnitureOrientation orientation)
        {
            FurniturePlacement p = _placements[instanceId];
            _placements[instanceId] = new FurniturePlacement(instanceId, p.Definition, cell, orientation);
            Changed?.Invoke();
        }

        internal bool Remove(int instanceId)
        {
            if (!_placements.Remove(instanceId)) return false;
            _order.Remove(instanceId);
            Changed?.Invoke();
            return true;
        }

        public HouseLayoutData ToData()
        {
            var data = new HouseLayoutData { nextInstanceId = NextInstanceId, starterGranted = StarterGranted };
            foreach (FurniturePlacement p in Placements)
                data.placements.Add(new HouseLayoutData.PlacementData
                {
                    instanceId = p.InstanceId, itemId = p.ItemId, x = p.Cell.x, y = p.Cell.y, orientation = (int)p.Orientation,
                });
            return data;
        }

        /// <summary>
        /// Replaces the layout with saved data. All-or-nothing: unknown items, invalid/duplicate IDs,
        /// overlaps or blocked exits reject the whole data and leave the layout unchanged.
        /// </summary>
        public bool TryLoad(HouseLayoutData data, FurnitureCatalog catalog, out string error)
        {
            error = null;
            if (data == null || catalog == null) { error = "Missing data."; return false; }
            if (data.version != HouseLayoutData.CurrentVersion) { error = $"Unsupported version {data.version}."; return false; }
            var trial = new HouseLayout(Size, _exitCells, _reserved);
            int maxId = 0;
            var ids = new HashSet<int>();
            foreach (HouseLayoutData.PlacementData p in data.placements ?? new List<HouseLayoutData.PlacementData>())
            {
                if (!catalog.TryGet(p.itemId, out FurnitureDefinition def)) { error = $"Unknown furniture '{p.itemId}'."; return false; }
                if (p.instanceId <= 0 || !ids.Add(p.instanceId)) { error = $"Invalid instance ID {p.instanceId}."; return false; }
                if (p.orientation < 0 || p.orientation > 1) { error = "Invalid orientation."; return false; }
                var o = (FurnitureOrientation)p.orientation;
                PlacementResult r = trial.Evaluate(def, new Vector2Int(p.x, p.y), o);
                if (r != PlacementResult.Ok) { error = $"'{p.itemId}' at ({p.x},{p.y}): {r}."; return false; }
                trial.NextInstanceId = p.instanceId;
                trial.Add(def, new Vector2Int(p.x, p.y), o);
                maxId = Math.Max(maxId, p.instanceId);
            }

            _placements.Clear();
            _order.Clear();
            foreach (int id in trial._order) { _placements.Add(id, trial._placements[id]); _order.Add(id); }
            NextInstanceId = Math.Max(data.nextInstanceId, maxId + 1);
            StarterGranted = data.starterGranted;
            Changed?.Invoke();
            return true;
        }
    }
}

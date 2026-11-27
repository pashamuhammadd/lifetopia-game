using System;
using System.Collections.Generic;
using Lifetopia.Items;
using UnityEngine;

namespace Lifetopia.Housing
{
    public enum DecorateState
    {
        /// <summary>Nothing selected; tap furniture to select it or pick an item from the bag list.</summary>
        Browsing = 0,
        /// <summary>A new item from the bag is shown as a ghost; Confirm places it.</summary>
        Placing = 1,
        /// <summary>A placed piece is selected (Move / Rotate / Store / Cancel).</summary>
        Selected = 2,
        /// <summary>A placed piece is shown as a ghost at a new spot/orientation; the original stays until Confirm.</summary>
        Moving = 3,
    }

    /// <summary>
    /// Decorating Mode state without any scene objects (unit tested). Holds the ghost preview and
    /// commits only on <see cref="Confirm"/> / <see cref="StoreSelected"/> through <see cref="HouseDecorService"/>.
    /// Cancel never changes the house or the bag.
    /// </summary>
    public sealed class DecorateSession
    {
        private readonly HouseDecorService _service;
        private static readonly IReadOnlyCollection<Vector2Int> NoCells = Array.Empty<Vector2Int>();

        public DecorateState State { get; private set; } = DecorateState.Browsing;
        public ItemDefinition PlacingItem { get; private set; }
        public int SelectedId { get; private set; }
        public Vector2Int GhostCell { get; private set; }
        public FurnitureOrientation GhostOrientation { get; private set; }

        /// <summary>Cells the player stands on (solid furniture may not be placed there). Set by the scene.</summary>
        public IReadOnlyCollection<Vector2Int> PlayerCells { get; set; } = NoCells;

        /// <summary>Raised whenever state, ghost or the house changed (UI redraw).</summary>
        public event Action Changed;

        public DecorateSession(HouseDecorService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public HouseDecorService Service => _service;
        public HouseLayout Layout => _service.Layout;
        public bool HasGhost => State == DecorateState.Placing || State == DecorateState.Moving;

        /// <summary>Definition shown as ghost/selection, or null.</summary>
        public FurnitureDefinition ActiveDefinition
        {
            get
            {
                if (State == DecorateState.Placing)
                    return _service.Catalog.TryGet(PlacingItem, out FurnitureDefinition d) ? d : null;
                return Layout.TryGet(SelectedId, out FurniturePlacement p) ? p.Definition : null;
            }
        }

        /// <summary>Validity of the current ghost (Ok when there is no ghost).</summary>
        public PlacementResult Preview
        {
            get
            {
                if (State == DecorateState.Placing) return _service.CheckPlace(PlacingItem, GhostCell, GhostOrientation, PlayerCells);
                if (State == DecorateState.Moving) return _service.CheckMove(SelectedId, GhostCell, GhostOrientation, PlayerCells);
                return PlacementResult.Ok;
            }
        }

        public bool CanConfirm => HasGhost && Preview == PlacementResult.Ok;
        public bool CanStore => (State == DecorateState.Selected || State == DecorateState.Moving) && _service.CheckStore(SelectedId) == PlacementResult.Ok;
        public bool CanMove => State == DecorateState.Selected;

        /// <summary>Rotation only for pieces with a validated West sprite (bed in M3.6).</summary>
        public bool CanRotate
        {
            get
            {
                FurnitureDefinition d = ActiveDefinition;
                return d != null && d.SupportsRotation && State != DecorateState.Browsing;
            }
        }

        public bool BeginPlace(ItemDefinition item)
        {
            if (item == null || !_service.Catalog.TryGet(item, out FurnitureDefinition def)) return false;
            if (_service.OwnedInBag(item) < 1) return false;
            State = DecorateState.Placing;
            PlacingItem = item;
            SelectedId = 0;
            GhostOrientation = FurnitureOrientation.North;
            GhostCell = FindStartCell(def, GhostOrientation);
            Raise();
            return true;
        }

        /// <summary>Selects the piece under <paramref name="cell"/> (only when no ghost is active). False = nothing there.</summary>
        public bool SelectAt(Vector2Int cell)
        {
            if (HasGhost) return false;
            if (!Layout.TryGetAt(cell, out FurniturePlacement p))
            {
                if (State == DecorateState.Selected) { ClearSelection(); Raise(); }
                return false;
            }
            Select(p.InstanceId);
            return true;
        }

        public bool Select(int instanceId)
        {
            if (HasGhost || !Layout.TryGet(instanceId, out FurniturePlacement p)) return false;
            State = DecorateState.Selected;
            SelectedId = instanceId;
            PlacingItem = null;
            GhostCell = p.Cell;
            GhostOrientation = p.Orientation;
            Raise();
            return true;
        }

        public bool BeginMove()
        {
            if (State != DecorateState.Selected || !Layout.TryGet(SelectedId, out FurniturePlacement p)) return false;
            State = DecorateState.Moving;
            GhostCell = p.Cell;
            GhostOrientation = p.Orientation;
            Raise();
            return true;
        }

        public bool Rotate()
        {
            if (!CanRotate) return false;
            if (State == DecorateState.Selected && !BeginMove()) return false;
            FurnitureDefinition d = ActiveDefinition;
            FurnitureOrientation next = GhostOrientation == FurnitureOrientation.North ? FurnitureOrientation.West : FurnitureOrientation.North;
            if (!d.Supports(next)) return false;
            GhostOrientation = next;
            GhostCell = ClampCell(GhostCell, d.Footprint(next));
            Raise();
            return true;
        }

        /// <summary>Moves the ghost so its footprint is centred on <paramref name="targetCell"/> (clamped into the room).</summary>
        public void SetGhostTarget(Vector2Int targetCell)
        {
            FurnitureDefinition d = ActiveDefinition;
            if (!HasGhost || d == null) return;
            Vector2Int size = d.Footprint(GhostOrientation);
            Vector2Int cell = ClampCell(targetCell - new Vector2Int((size.x - 1) / 2, (size.y - 1) / 2), size);
            if (cell == GhostCell) return;
            GhostCell = cell;
            Raise();
        }

        /// <summary>Keyboard nudge by one cell.</summary>
        public void Nudge(Vector2Int delta)
        {
            FurnitureDefinition d = ActiveDefinition;
            if (!HasGhost || d == null || delta == Vector2Int.zero) return;
            Vector2Int cell = ClampCell(GhostCell + delta, d.Footprint(GhostOrientation));
            if (cell == GhostCell) return;
            GhostCell = cell;
            Raise();
        }

        /// <summary>Commits the ghost. On success a new piece/moved piece becomes the selection.</summary>
        public PlacementResult Confirm()
        {
            if (!HasGhost) return PlacementResult.NotFound;
            PlacementResult r;
            if (State == DecorateState.Placing)
            {
                r = _service.Place(PlacingItem, GhostCell, GhostOrientation, PlayerCells, out int id);
                if (r == PlacementResult.Ok) { State = DecorateState.Selected; SelectedId = id; PlacingItem = null; }
            }
            else
            {
                r = _service.Move(SelectedId, GhostCell, GhostOrientation, PlayerCells);
                if (r == PlacementResult.Ok) State = DecorateState.Selected;
            }
            Raise();
            return r;
        }

        /// <summary>Returns the selected piece to the bag (stays when the bag is full).</summary>
        public PlacementResult StoreSelected()
        {
            if (State != DecorateState.Selected && State != DecorateState.Moving) return PlacementResult.NotFound;
            PlacementResult r = _service.Store(SelectedId);
            if (r == PlacementResult.Ok) ClearSelection();
            Raise();
            return r;
        }

        /// <summary>Moving → back to Selected (original spot kept); Placing/Selected → Browsing. Never changes the house.</summary>
        public void Cancel()
        {
            if (State == DecorateState.Moving && Layout.TryGet(SelectedId, out FurniturePlacement p))
            {
                State = DecorateState.Selected;
                GhostCell = p.Cell;
                GhostOrientation = p.Orientation;
            }
            else ClearSelection();
            Raise();
        }

        /// <summary>Drops any ghost/selection without committing (leaving Decorating Mode).</summary>
        public void Reset()
        {
            ClearSelection();
            Raise();
        }

        private void ClearSelection()
        {
            State = DecorateState.Browsing;
            SelectedId = 0;
            PlacingItem = null;
        }

        private Vector2Int ClampCell(Vector2Int cell, Vector2Int size) =>
            new Vector2Int(Mathf.Clamp(cell.x, 0, Mathf.Max(0, Layout.Size.x - size.x)),
                           Mathf.Clamp(cell.y, 0, Mathf.Max(0, Layout.Size.y - size.y)));

        /// <summary>First valid cell scanning outward from the room centre; the centre when none is valid.</summary>
        private Vector2Int FindStartCell(FurnitureDefinition def, FurnitureOrientation o)
        {
            Vector2Int size = def.Footprint(o);
            Vector2Int centre = ClampCell(new Vector2Int((Layout.Size.x - size.x) / 2, (Layout.Size.y - size.y) / 2 + 1), size);
            int maxR = Mathf.Max(Layout.Size.x, Layout.Size.y);
            for (int r = 0; r <= maxR; r++)
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        var c = new Vector2Int(centre.x + dx, centre.y + dy);
                        if (ClampCell(c, size) != c) continue;
                        if (Layout.Evaluate(def, c, o, 0, PlayerCells) == PlacementResult.Ok) return c;
                    }
            return centre;
        }

        private void Raise() => Changed?.Invoke();
    }
}

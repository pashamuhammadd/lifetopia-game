using System;
using System.Collections.Generic;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using UnityEngine;

namespace Lifetopia.Housing
{
    /// <summary>One free starter piece (authored on <see cref="HousingSessionHost"/>).</summary>
    [Serializable]
    public struct StarterPlacement
    {
        public FurnitureDefinition furniture;
        public Vector2Int cell;
        public FurnitureOrientation orientation;
    }

    /// <summary>
    /// The only way gameplay changes a house. Works on the player's EXISTING <see cref="Inventory"/> (no
    /// second bag) and the session's <see cref="HouseLayout"/>. Every operation is atomic: it is fully
    /// validated first and either changes inventory and layout together or changes nothing.
    /// - Place: consumes exactly one item from the bag and adds one placement with a new instance ID.
    /// - Move/rotate: keeps the same instance; the old spot stays until the new one is valid.
    /// - Store: puts the item back into the bag only if the bag can take it; when full, the piece stays.
    /// Re-entrant calls (e.g. from a change event) are refused with <see cref="PlacementResult.Busy"/>.
    /// </summary>
    public sealed class HouseDecorService
    {
        private readonly Inventory _inventory;
        private bool _busy;

        public HouseLayout Layout { get; }
        public FurnitureCatalog Catalog { get; }

        /// <summary>Successful operations (tests: one action = one change).</summary>
        public int OperationCount { get; private set; }

        public HouseDecorService(HouseLayout layout, FurnitureCatalog catalog, Inventory inventory)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            Catalog = catalog ? catalog : throw new ArgumentNullException(nameof(catalog));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public int OwnedInBag(ItemDefinition item) => _inventory.GetItemCount(item);

        /// <summary>Checks placing one <paramref name="item"/> from the bag (nothing changes).</summary>
        public PlacementResult CheckPlace(ItemDefinition item, Vector2Int cell, FurnitureOrientation orientation,
            IReadOnlyCollection<Vector2Int> playerCells = null)
        {
            if (!Catalog.TryGet(item, out FurnitureDefinition def)) return PlacementResult.InvalidItem;
            if (_inventory.GetItemCount(item) < 1) return PlacementResult.NotInInventory;
            return Layout.Evaluate(def, cell, orientation, 0, playerCells);
        }

        public PlacementResult Place(ItemDefinition item, Vector2Int cell, FurnitureOrientation orientation,
            IReadOnlyCollection<Vector2Int> playerCells, out int instanceId)
        {
            instanceId = 0;
            if (_busy) return PlacementResult.Busy;
            _busy = true;
            try
            {
                PlacementResult r = CheckPlace(item, cell, orientation, playerCells);
                if (r != PlacementResult.Ok) return r;
                Catalog.TryGet(item, out FurnitureDefinition def);
                if (!_inventory.RemoveItem(item, 1).Succeeded()) return PlacementResult.NotInInventory;
                instanceId = Layout.Add(def, cell, orientation);
                OperationCount++;
                return PlacementResult.Ok;
            }
            finally { _busy = false; }
        }

        public PlacementResult CheckMove(int instanceId, Vector2Int cell, FurnitureOrientation orientation,
            IReadOnlyCollection<Vector2Int> playerCells = null)
        {
            if (!Layout.TryGet(instanceId, out FurniturePlacement p)) return PlacementResult.NotFound;
            return Layout.Evaluate(p.Definition, cell, orientation, instanceId, playerCells);
        }

        public PlacementResult Move(int instanceId, Vector2Int cell, FurnitureOrientation orientation,
            IReadOnlyCollection<Vector2Int> playerCells = null)
        {
            if (_busy) return PlacementResult.Busy;
            _busy = true;
            try
            {
                PlacementResult r = CheckMove(instanceId, cell, orientation, playerCells);
                if (r != PlacementResult.Ok) return r;
                Layout.TryGet(instanceId, out FurniturePlacement p);
                if (p.Cell == cell && p.Orientation == orientation) return PlacementResult.Ok; // no change
                Layout.Move(instanceId, cell, orientation);
                OperationCount++;
                return PlacementResult.Ok;
            }
            finally { _busy = false; }
        }

        public PlacementResult CheckStore(int instanceId)
        {
            if (!Layout.TryGet(instanceId, out FurniturePlacement p)) return PlacementResult.NotFound;
            return _inventory.CanAddItem(p.Definition.Item, 1) ? PlacementResult.Ok : PlacementResult.InventoryFull;
        }

        public PlacementResult Store(int instanceId)
        {
            if (_busy) return PlacementResult.Busy;
            _busy = true;
            try
            {
                PlacementResult r = CheckStore(instanceId);
                if (r != PlacementResult.Ok) return r;
                Layout.TryGet(instanceId, out FurniturePlacement p);
                if (!_inventory.AddItem(p.Definition.Item, 1).Succeeded()) return PlacementResult.InventoryFull;
                if (!Layout.Remove(instanceId))
                {
                    _inventory.RemoveItem(p.Definition.Item, 1); // roll back
                    return PlacementResult.NotFound;
                }
                OperationCount++;
                return PlacementResult.Ok;
            }
            finally { _busy = false; }
        }

        /// <summary>
        /// Places the free starter furniture once per session (never also added to the bag). Returns the
        /// number placed; entries that are invalid are skipped and reported in <paramref name="problems"/>.
        /// </summary>
        public int GrantStarterLayout(IEnumerable<StarterPlacement> starter, List<string> problems = null)
        {
            if (Layout.StarterGranted || starter == null) return 0;
            int placed = 0;
            foreach (StarterPlacement s in starter)
            {
                PlacementResult r = s.furniture != null && Catalog.TryGet(s.furniture.Item, out _)
                    ? Layout.Evaluate(s.furniture, s.cell, s.orientation)
                    : PlacementResult.InvalidItem;
                if (r != PlacementResult.Ok)
                {
                    problems?.Add($"Starter '{(s.furniture != null ? s.furniture.ItemId : "null")}' at {s.cell}: {r}");
                    continue;
                }
                Layout.Add(s.furniture, s.cell, s.orientation);
                placed++;
            }
            Layout.StarterGranted = true;
            return placed;
        }
    }
}

using System.Collections.Generic;
using Lifetopia.Economy;
using Lifetopia.Housing;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using NUnit.Framework;
using UnityEngine;

namespace Lifetopia.Tests
{
    /// <summary>M3.6 housing: grid rules, exit reachability, atomic place/move/store, decorating session, camera maths.</summary>
    public class HousingTests
    {
        private static readonly Vector2Int[] Exit = { new Vector2Int(3, 0), new Vector2Int(4, 0) };
        private static readonly Vector2Int[] Spawn = { new Vector2Int(3, 1), new Vector2Int(4, 1) };

        private ItemDefinition _bedItem, _tableItem, _stoolItem, _rugItem, _shelfItem, _seed;
        private FurnitureDefinition _bed, _table, _stool, _rug, _shelf;
        private FurnitureCatalog _catalog;
        private PlayerProfile _profile;
        private HouseLayout _layout;
        private HouseDecorService _service;

        [SetUp]
        public void SetUp()
        {
            _bedItem = ItemDefinition.CreateRuntime("furniture_bed", "Tropical Bed", ItemCategory.Furniture, false, 1, 0, 150);
            _tableItem = ItemDefinition.CreateRuntime("furniture_table", "Wooden Table", ItemCategory.Furniture, false, 1, 0, 50);
            _stoolItem = ItemDefinition.CreateRuntime("furniture_wooden_stool", "Wooden Stool", ItemCategory.Furniture, false, 1, 0, 25);
            _rugItem = ItemDefinition.CreateRuntime("furniture_rug_oval", "Tropical Rug", ItemCategory.Furniture, false, 1, 0, 60);
            _shelfItem = ItemDefinition.CreateRuntime("furniture_bookshelf", "Bookshelf", ItemCategory.Furniture, false, 1, 0, 100);
            _seed = ItemDefinition.CreateRuntime("seed_tropical", "Tropical Seeds", ItemCategory.Seed, true, 99, 5, 10);
            _bed = FurnitureDefinition.CreateRuntime(_bedItem, new Vector2Int(1, 2), true);
            _table = FurnitureDefinition.CreateRuntime(_tableItem, new Vector2Int(2, 1));
            _stool = FurnitureDefinition.CreateRuntime(_stoolItem, new Vector2Int(1, 1));
            _rug = FurnitureDefinition.CreateRuntime(_rugItem, new Vector2Int(2, 2), false, FurnitureLayer.Floor, false);
            _shelf = FurnitureDefinition.CreateRuntime(_shelfItem, new Vector2Int(2, 1));
            _catalog = FurnitureCatalog.CreateRuntime(_bed, _table, _stool, _rug, _shelf);
            _profile = new PlayerProfile();
            _layout = new HouseLayout(new Vector2Int(8, 8), Exit, Spawn);
            _service = new HouseDecorService(_layout, _catalog, _profile.Inventory);
        }

        [TearDown]
        public void TearDown()
        {
            _profile.Dispose();
            foreach (Object o in new Object[] { _bedItem, _tableItem, _stoolItem, _rugItem, _shelfItem, _seed, _bed, _table, _stool, _rug, _shelf, _catalog })
                Object.DestroyImmediate(o);
        }

        private int Give(ItemDefinition item, int n = 1)
        {
            for (int i = 0; i < n; i++) Assert.That(_profile.Inventory.AddItem(item, 1).Succeeded(), Is.True);
            return _profile.Inventory.GetItemCount(item);
        }

        private int Place(ItemDefinition item, int x, int y, FurnitureOrientation o = FurnitureOrientation.North)
        {
            Assert.That(_service.Place(item, new Vector2Int(x, y), o, null, out int id), Is.EqualTo(PlacementResult.Ok));
            return id;
        }

        // ------------------------------------------------------------------ footprint & rotation

        [Test]
        public void Footprint_WestSwapsWidthAndDepth()
        {
            Assert.That(_bed.Footprint(FurnitureOrientation.North), Is.EqualTo(new Vector2Int(1, 2)));
            Assert.That(_bed.Footprint(FurnitureOrientation.West), Is.EqualTo(new Vector2Int(2, 1)));
            Assert.That(_bed.Supports(FurnitureOrientation.West), Is.True);
            Assert.That(_table.Supports(FurnitureOrientation.West), Is.False);
        }

        [Test]
        public void Anchor_FrontCentreForObjects_CentreForRugs()
        {
            Assert.That(FurnitureView.AnchorFor(_bed, new Vector2Int(0, 5), FurnitureOrientation.North), Is.EqualTo(new Vector2(0.5f, 5f)));
            Assert.That(FurnitureView.AnchorFor(_bed, new Vector2Int(0, 5), FurnitureOrientation.West), Is.EqualTo(new Vector2(1f, 5f)));
            Assert.That(FurnitureView.AnchorFor(_rug, new Vector2Int(2, 3), FurnitureOrientation.North), Is.EqualTo(new Vector2(3f, 4f)));
        }

        // ------------------------------------------------------------------ evaluate

        [Test]
        public void Evaluate_Bounds()
        {
            Assert.That(_layout.Evaluate(_table, new Vector2Int(6, 4), FurnitureOrientation.North), Is.EqualTo(PlacementResult.Ok));
            Assert.That(_layout.Evaluate(_table, new Vector2Int(7, 4), FurnitureOrientation.North), Is.EqualTo(PlacementResult.OutOfBounds));
            Assert.That(_layout.Evaluate(_bed, new Vector2Int(0, 7), FurnitureOrientation.North), Is.EqualTo(PlacementResult.OutOfBounds));
            Assert.That(_layout.Evaluate(_stool, new Vector2Int(-1, 3), FurnitureOrientation.North), Is.EqualTo(PlacementResult.OutOfBounds));
        }

        [Test]
        public void Evaluate_UnsupportedOrientationRefused()
        {
            Assert.That(_layout.Evaluate(_table, new Vector2Int(2, 4), FurnitureOrientation.West), Is.EqualTo(PlacementResult.UnsupportedOrientation));
        }

        [Test]
        public void Evaluate_ReservedExitAndSpawnCells()
        {
            Assert.That(_layout.Evaluate(_stool, new Vector2Int(3, 0), FurnitureOrientation.North), Is.EqualTo(PlacementResult.ReservedCell));
            Assert.That(_layout.Evaluate(_stool, new Vector2Int(4, 1), FurnitureOrientation.North), Is.EqualTo(PlacementResult.ReservedCell));
            Assert.That(_layout.Evaluate(_rug, new Vector2Int(2, 0), FurnitureOrientation.North), Is.EqualTo(PlacementResult.ReservedCell), "rugs keep the doorway clear too");
        }

        [Test]
        public void Evaluate_OverlapSameLayer_RugUnderFurnitureAllowed()
        {
            Give(_tableItem); Give(_rugItem); Give(_stoolItem);
            Place(_tableItem, 2, 4);
            Assert.That(_layout.Evaluate(_stool, new Vector2Int(3, 4), FurnitureOrientation.North), Is.EqualTo(PlacementResult.Overlap));
            Assert.That(_layout.Evaluate(_rug, new Vector2Int(2, 3), FurnitureOrientation.North), Is.EqualTo(PlacementResult.Ok));
            Place(_rugItem, 2, 3);
            Assert.That(_layout.Evaluate(_rug, new Vector2Int(3, 4), FurnitureOrientation.North), Is.EqualTo(PlacementResult.Overlap), "two rugs never overlap");
            Assert.That(_layout.TryGetAt(new Vector2Int(2, 4), out FurniturePlacement top) && top.Definition == _table, Is.True, "object wins over rug when selecting");
            Assert.That(_layout.TryGetAt(new Vector2Int(2, 3), out FurniturePlacement under) && under.Definition == _rug, Is.True);
        }

        [Test]
        public void Evaluate_PlayerCellsBlockSolidButNotRugs()
        {
            var player = new List<Vector2Int> { new Vector2Int(5, 5) };
            Assert.That(_layout.Evaluate(_stool, new Vector2Int(5, 5), FurnitureOrientation.North, 0, player), Is.EqualTo(PlacementResult.PlayerInTheWay));
            Assert.That(_layout.Evaluate(_rug, new Vector2Int(5, 5), FurnitureOrientation.North, 0, player), Is.EqualTo(PlacementResult.Ok));
        }

        [Test]
        public void Evaluate_ClosingAPocketIsRefused()
        {
            // Wall off the top-left corner cell (0,7): stool at (1,7) and (0,6) would leave (0,7) unreachable.
            Give(_stoolItem, 2);
            Place(_stoolItem, 1, 7);
            Assert.That(_layout.Evaluate(_stool, new Vector2Int(0, 6), FurnitureOrientation.North), Is.EqualTo(PlacementResult.BlocksExit));
            Assert.That(_layout.Evaluate(_rug, new Vector2Int(0, 5), FurnitureOrientation.North), Is.EqualTo(PlacementResult.Ok), "rugs never block");
        }

        [Test]
        public void Evaluate_CuttingTheRoomFromTheDoorIsRefused()
        {
            // A full row of shelves at y = 2 would cut the back of the room off from the door.
            Give(_shelfItem, 4);
            Place(_shelfItem, 0, 2);
            Place(_shelfItem, 2, 2);
            Place(_shelfItem, 4, 2);
            Assert.That(_layout.Evaluate(_shelf, new Vector2Int(6, 2), FurnitureOrientation.North), Is.EqualTo(PlacementResult.BlocksExit));
            Assert.That(_layout.Evaluate(_stool, new Vector2Int(6, 2), FurnitureOrientation.North), Is.EqualTo(PlacementResult.Ok), "a 1-cell gap keeps the path");
        }

        // ------------------------------------------------------------------ atomic operations

        [Test]
        public void Place_ConsumesExactlyOneAndGivesUniqueIds()
        {
            Give(_stoolItem, 2);
            int a = Place(_stoolItem, 1, 3);
            int b = Place(_stoolItem, 6, 3);
            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(_profile.Inventory.GetItemCount(_stoolItem), Is.EqualTo(0));
            Assert.That(_layout.Count, Is.EqualTo(2));
            Assert.That(_service.OperationCount, Is.EqualTo(2));
        }

        [Test]
        public void Place_WithoutItemOrInvalidSpot_ChangesNothing()
        {
            Assert.That(_service.Place(_stoolItem, new Vector2Int(1, 3), FurnitureOrientation.North, null, out _), Is.EqualTo(PlacementResult.NotInInventory));
            Give(_stoolItem);
            Assert.That(_service.Place(_stoolItem, new Vector2Int(3, 0), FurnitureOrientation.North, null, out _), Is.EqualTo(PlacementResult.ReservedCell));
            Assert.That(_service.Place(_seed, new Vector2Int(1, 3), FurnitureOrientation.North, null, out _), Is.EqualTo(PlacementResult.InvalidItem));
            Assert.That(_profile.Inventory.GetItemCount(_stoolItem), Is.EqualTo(1));
            Assert.That(_layout.Count, Is.EqualTo(0));
            Assert.That(_service.OperationCount, Is.EqualTo(0));
        }

        [Test]
        public void Place_ReentrantCallFromChangeEventIsRefused()
        {
            Give(_stoolItem, 2);
            PlacementResult inner = PlacementResult.Ok;
            _layout.Changed += () => inner = _service.Place(_stoolItem, new Vector2Int(6, 6), FurnitureOrientation.North, null, out _);
            Place(_stoolItem, 1, 3);
            Assert.That(inner, Is.EqualTo(PlacementResult.Busy));
            Assert.That(_layout.Count, Is.EqualTo(1));
            Assert.That(_profile.Inventory.GetItemCount(_stoolItem), Is.EqualTo(1));
        }

        [Test]
        public void Move_KeepsInstance_InvalidMoveKeepsOriginal()
        {
            Give(_tableItem); Give(_stoolItem);
            int table = Place(_tableItem, 2, 4);
            Place(_stoolItem, 6, 6);
            Assert.That(_service.Move(table, new Vector2Int(5, 6), FurnitureOrientation.North), Is.EqualTo(PlacementResult.Overlap));
            Assert.That(_layout.TryGet(table, out FurniturePlacement p) && p.Cell == new Vector2Int(2, 4), Is.True);
            Assert.That(_service.Move(table, new Vector2Int(3, 4), FurnitureOrientation.North), Is.EqualTo(PlacementResult.Ok), "may overlap its own old cells");
            Assert.That(_layout.TryGet(table, out p) && p.Cell == new Vector2Int(3, 4), Is.True);
            Assert.That(_layout.Count, Is.EqualTo(2));
            Assert.That(_profile.Inventory.GetItemCount(_tableItem), Is.EqualTo(0));
        }

        [Test]
        public void Rotate_BedNorthToWestAndBack()
        {
            Give(_bedItem);
            int bed = Place(_bedItem, 0, 5);
            Assert.That(_service.Move(bed, new Vector2Int(0, 6), FurnitureOrientation.West), Is.EqualTo(PlacementResult.Ok));
            Assert.That(_layout.TryGet(bed, out FurniturePlacement p), Is.True);
            Assert.That(p.Rect, Is.EqualTo(new RectInt(0, 6, 2, 1)));
            Assert.That(_service.Move(bed, new Vector2Int(0, 5), FurnitureOrientation.North), Is.EqualTo(PlacementResult.Ok));
            Assert.That(_layout.TryGet(bed, out p) && p.Rect == new RectInt(0, 5, 1, 2), Is.True);
        }

        [Test]
        public void Store_ReturnsItem_FullBagKeepsPiece()
        {
            Give(_stoolItem); Give(_tableItem);
            int stool = Place(_stoolItem, 1, 3);
            int table = Place(_tableItem, 4, 5);
            Assert.That(_service.Store(stool), Is.EqualTo(PlacementResult.Ok));
            Assert.That(_profile.Inventory.GetItemCount(_stoolItem), Is.EqualTo(1));
            Assert.That(_layout.TryGet(stool, out _), Is.False);

            // Fill every slot (non-stackable stools), then storing the table must fail and keep it placed.
            while (_profile.Inventory.CanAddItem(_stoolItem, 1)) _profile.Inventory.AddItem(_stoolItem, 1);
            Assert.That(_service.Store(table), Is.EqualTo(PlacementResult.InventoryFull));
            Assert.That(_layout.TryGet(table, out _), Is.True);
            Assert.That(_profile.Inventory.GetItemCount(_tableItem), Is.EqualTo(0));
            Assert.That(_service.Store(999), Is.EqualTo(PlacementResult.NotFound));
        }

        [Test]
        public void StarterLayout_GrantedOnce_NoBagItems()
        {
            var starter = new[]
            {
                new StarterPlacement { furniture = _bed, cell = new Vector2Int(0, 5), orientation = FurnitureOrientation.North },
                new StarterPlacement { furniture = _table, cell = new Vector2Int(4, 4), orientation = FurnitureOrientation.North },
                new StarterPlacement { furniture = _stool, cell = new Vector2Int(5, 2), orientation = FurnitureOrientation.North },
            };
            var problems = new List<string>();
            Assert.That(_service.GrantStarterLayout(starter, problems), Is.EqualTo(3));
            Assert.That(problems, Is.Empty);
            Assert.That(_service.GrantStarterLayout(starter), Is.EqualTo(0), "never twice");
            Assert.That(_layout.Count, Is.EqualTo(3));
            Assert.That(_profile.Inventory.GetItemCount(_bedItem) + _profile.Inventory.GetItemCount(_tableItem) + _profile.Inventory.GetItemCount(_stoolItem), Is.EqualTo(0));
            Assert.That(_layout.CountOf("furniture_bed"), Is.EqualTo(1));
        }

        [Test]
        public void LayoutData_RoundTrip_AndInvalidDataRejected()
        {
            Give(_bedItem); Give(_rugItem);
            Place(_bedItem, 0, 6, FurnitureOrientation.West);
            Place(_rugItem, 3, 4);
            HouseLayoutData data = _layout.ToData();

            var copy = new HouseLayout(new Vector2Int(8, 8), Exit, Spawn);
            Assert.That(copy.TryLoad(data, _catalog, out string error), Is.True, error);
            Assert.That(copy.Count, Is.EqualTo(2));
            Assert.That(copy.NextInstanceId, Is.EqualTo(_layout.NextInstanceId));

            data.placements.Add(new HouseLayoutData.PlacementData { instanceId = 50, itemId = "furniture_wooden_stool", x = 3, y = 0 });
            Assert.That(copy.TryLoad(data, _catalog, out error), Is.False, "reserved cell rejects the whole data");
            Assert.That(copy.Count, Is.EqualTo(2), "unchanged after rejection");
        }

        // ------------------------------------------------------------------ decorating session

        [Test]
        public void Session_PlaceConfirm_CancelNeverChangesState()
        {
            Give(_stoolItem);
            var s = new DecorateSession(_service);
            Assert.That(s.BeginPlace(_tableItem), Is.False, "not in bag");
            Assert.That(s.BeginPlace(_stoolItem), Is.True);
            Assert.That(s.Preview, Is.EqualTo(PlacementResult.Ok), "starts on a valid cell");
            s.SetGhostTarget(new Vector2Int(3, 0));
            Assert.That(s.CanConfirm, Is.False);
            s.Cancel();
            Assert.That(s.State, Is.EqualTo(DecorateState.Browsing));
            Assert.That(_layout.Count, Is.EqualTo(0));
            Assert.That(_profile.Inventory.GetItemCount(_stoolItem), Is.EqualTo(1));

            s.BeginPlace(_stoolItem);
            s.SetGhostTarget(new Vector2Int(6, 6));
            Assert.That(s.Confirm(), Is.EqualTo(PlacementResult.Ok));
            Assert.That(s.State, Is.EqualTo(DecorateState.Selected));
            Assert.That(s.Confirm(), Is.EqualTo(PlacementResult.NotFound), "second confirm does nothing");
            Assert.That(_layout.Count, Is.EqualTo(1));
        }

        [Test]
        public void Session_MoveCancelKeepsOriginal_RotateOnlyWhenSupported()
        {
            Give(_bedItem); Give(_tableItem);
            int bed = Place(_bedItem, 0, 5);
            int table = Place(_tableItem, 4, 4);
            var s = new DecorateSession(_service);
            Assert.That(s.Select(table), Is.True);
            Assert.That(s.CanRotate, Is.False);
            Assert.That(s.Select(bed), Is.True);
            Assert.That(s.CanRotate, Is.True);
            Assert.That(s.Rotate(), Is.True);
            Assert.That(s.State, Is.EqualTo(DecorateState.Moving));
            Assert.That(s.GhostOrientation, Is.EqualTo(FurnitureOrientation.West));
            s.Cancel();
            Assert.That(_layout.TryGet(bed, out FurniturePlacement p) && p.Orientation == FurnitureOrientation.North && p.Cell == new Vector2Int(0, 5), Is.True);

            s.BeginMove();
            s.SetGhostTarget(new Vector2Int(7, 5)); // centre target, clamped inside
            Assert.That(s.GhostCell, Is.EqualTo(new Vector2Int(7, 5)));
            Assert.That(s.Confirm(), Is.EqualTo(PlacementResult.Ok));
            Assert.That(_layout.TryGet(bed, out p) && p.Cell == new Vector2Int(7, 5), Is.True);
        }

        [Test]
        public void Session_StoreSelected()
        {
            Give(_stoolItem);
            int stool = Place(_stoolItem, 2, 5);
            var s = new DecorateSession(_service);
            Assert.That(s.SelectAt(new Vector2Int(2, 5)), Is.True);
            Assert.That(s.SelectedId, Is.EqualTo(stool));
            Assert.That(s.StoreSelected(), Is.EqualTo(PlacementResult.Ok));
            Assert.That(s.State, Is.EqualTo(DecorateState.Browsing));
            Assert.That(_profile.Inventory.GetItemCount(_stoolItem), Is.EqualTo(1));
            Assert.That(s.SelectAt(new Vector2Int(2, 5)), Is.False);
        }

        // ------------------------------------------------------------------ economy

        [Test]
        public void Market_FurnitureBuyUsesItemPrice_AndNonStackableNeedsFreeSlots()
        {
            var config = MarketConfig.CreateRuntime(new[] { _seed, _stoolItem, _bedItem }, System.Array.Empty<ItemDefinition>());
            _profile.Wallet.Add(200);
            var market = new MarketService(config, _profile);
            Assert.That(market.Quote(TradeMode.Buy, _stoolItem, 1).Total, Is.EqualTo(25));
            Assert.That(market.Buy(_bedItem, 1), Is.EqualTo(TradeResult.Success));
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(50));
            Assert.That(market.Buy(_bedItem, 1), Is.EqualTo(TradeResult.NotEnoughCoins));
            Assert.That(market.Buy(_stoolItem, 2), Is.EqualTo(TradeResult.Success));
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(0));
            Assert.That(_profile.Inventory.GetItemCount(_stoolItem), Is.EqualTo(2));
            Object.DestroyImmediate(config);
        }

#if UNITY_EDITOR
        [Test]
        public void Assets_FurniturePricesAndM35BaselineUnchanged()
        {
            int Price(string path, bool buy)
            {
                var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Lifetopia/Data/Items/" + path + ".asset");
                Assert.That(item, Is.Not.Null, path);
                return buy ? item.BuyPrice : item.SellPrice;
            }
            Assert.That(Price("item_furniture_wooden_stool", true), Is.EqualTo(25));
            Assert.That(Price("item_furniture_table", true), Is.EqualTo(50));
            Assert.That(Price("item_furniture_rug_oval", true), Is.EqualTo(60));
            Assert.That(Price("item_furniture_monstera", true), Is.EqualTo(75));
            Assert.That(Price("item_furniture_bookshelf", true), Is.EqualTo(100));
            Assert.That(Price("item_furniture_bed", true), Is.EqualTo(150));
            Assert.That(Price("item_furniture_wardrobe", true), Is.EqualTo(150));
            Assert.That(Price("item_seed_tropical", true), Is.EqualTo(10));
            Assert.That(Price("item_crop_tropical_fruit", false), Is.EqualTo(18));
            Assert.That(Price("item_fish_reef", false), Is.EqualTo(12));

            var market = UnityEditor.AssetDatabase.LoadAssetAtPath<MarketConfig>("Assets/_Lifetopia/Data/Economy/MarketConfig_Rio.asset");
            var problems = new List<string>();
            market.Validate(problems);
            Assert.That(problems, Is.Empty);
            Assert.That(market.SellItems.Count, Is.EqualTo(2), "no furniture resale");
            Assert.That(market.BuyItems.Count, Is.EqualTo(8));

            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<FurnitureCatalog>("Assets/_Lifetopia/Data/Housing/FurnitureCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
            catalog.Validate(problems);
            Assert.That(problems, Is.Empty);
            Assert.That(catalog.All.Count, Is.EqualTo(7));
            foreach (FurnitureDefinition d in catalog.All)
                Assert.That(d.SupportsRotation, Is.EqualTo(d.ItemId == "furniture_bed"), d.ItemId + ": only the bed rotates in M3.6");
        }
#endif

        // ------------------------------------------------------------------ camera maths

        [Test]
        public void Camera_FitSizeUsesTheTighterAxis()
        {
            var full = new Rect(0, 0, 1, 1);
            Assert.That(InteriorCameraMath.FitSize(new Vector2(8f, 10f), 0f, 16f / 9f, full), Is.EqualTo(5f).Within(1e-4));
            Assert.That(InteriorCameraMath.FitSize(new Vector2(8f, 10f), 0f, 0.5f, full), Is.EqualTo(8f).Within(1e-4), "portrait: width decides");
            var half = new Rect(0, 0, 1, 0.5f);
            Assert.That(InteriorCameraMath.FitSize(new Vector2(8f, 10f), 0f, 16f / 9f, half), Is.EqualTo(10f).Within(1e-4));
        }

        [Test]
        public void Camera_FocusLandsInUsableCentre_AndClampKeepsEdgesReachable()
        {
            var usable = new Rect(0f, 0f, 0.75f, 1f); // panel on the right quarter
            Vector2 cam = InteriorCameraMath.CameraForFocus(new Vector2(4f, 5f), 5f, 2f, usable);
            // Usable centre x is 0.375 → camera must sit 0.125 * 2 * 5 * 2 = 2.5 units right of the focus.
            Assert.That(cam.x, Is.EqualTo(6.5f).Within(1e-4));
            Assert.That(cam.y, Is.EqualTo(5f).Within(1e-4));

            var bounds = new Rect(0, 0, 8, 10);
            Vector2 half = new Vector2(3f, 3f);
            Assert.That(InteriorCameraMath.ClampFocus(new Vector2(-5f, 20f), bounds, 0f, half), Is.EqualTo(new Vector2(3f, 7f)));
            Assert.That(InteriorCameraMath.ClampFocus(new Vector2(1f, 1f), bounds, 0f, new Vector2(10f, 10f)), Is.EqualTo(new Vector2(4f, 5f)), "fits → centred");
        }

        [Test]
        public void Camera_NormaliseScreenRect()
        {
            Rect r = InteriorCameraMath.Normalise(new Rect(100, 50, 600, 300), 800, 400);
            Assert.That(r.xMin, Is.EqualTo(0.125f).Within(1e-5));
            Assert.That(r.yMax, Is.EqualTo(0.875f).Within(1e-5));
            Assert.That(InteriorCameraMath.Normalise(new Rect(0, 0, 1, 1), 800, 400), Is.EqualTo(new Rect(0, 0, 1, 1)), "degenerate → full screen");
        }

        [Test]
        public void PlayerCells_CoverTheFootCircle()
        {
            var cells = new List<Vector2Int>();
            InteriorCameraMath.PlayerCells(new Vector2(4.0f, 1.0f), new Vector2(0f, 0.2f), 0.28f, cells);
            // Spawn (feet 4,1): circle y 0.9..1.5 touches rows 0 and 1, columns 3 and 4 = exactly the reserved door cells.
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(3, 0), new Vector2Int(4, 0), new Vector2Int(3, 1), new Vector2Int(4, 1) }, cells);
            InteriorCameraMath.PlayerCells(new Vector2(2.5f, 2.9f), new Vector2(0f, 0.2f), 0.28f, cells);
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(2, 2), new Vector2Int(2, 3) }, cells);
        }
    }
}

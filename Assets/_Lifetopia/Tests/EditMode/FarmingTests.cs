using Lifetopia.Farming;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using Lifetopia.TimeSystem;
using NUnit.Framework;
using UnityEngine;

namespace Lifetopia.Tests
{
    /// <summary>In-memory farm setup: hoe, watering can, seeds, tropical fruit crop (120 s).</summary>
    internal sealed class FarmFixture
    {
        public readonly ItemDefinition Hoe = ItemDefinition.CreateRuntime("tool_hoe", "Hoe", ItemCategory.Tool, false);
        public readonly ItemDefinition Can = ItemDefinition.CreateRuntime("tool_watering_can", "Watering Can", ItemCategory.Tool, false);
        public readonly ItemDefinition Rod = ItemDefinition.CreateRuntime("tool_fishing_rod", "Fishing Rod", ItemCategory.Tool, false);
        public readonly ItemDefinition Seed = ItemDefinition.CreateRuntime("seed_tropical", "Tropical Seeds", ItemCategory.Seed, true, 99, 5, 10);
        public readonly ItemDefinition Fruit = ItemDefinition.CreateRuntime("crop_tropical_fruit", "Tropical Fruit", ItemCategory.Crop, true, 99, 18);
        public readonly ItemDefinition Fish = ItemDefinition.CreateRuntime("fish_reef", "Reef Fish", ItemCategory.Fish, true, 99, 12);
        public readonly CropDefinition Crop;
        public readonly FarmingConfig Config;
        public readonly FarmingConfig ConfigResetSoil;

        public FarmFixture()
        {
            Crop = CropDefinition.CreateRuntime("tropical_fruit", Seed, Fruit, 1, 120f);
            Config = FarmingConfig.CreateRuntime(Hoe, Can, true, Crop);
            ConfigResetSoil = FarmingConfig.CreateRuntime(Hoe, Can, false, Crop);
        }

        /// <summary>Starter-like bag: hoe, can, rod, 3 seeds.</summary>
        public Inventory StarterBag()
        {
            var inv = new Inventory();
            inv.AddItem(Hoe, 1);
            inv.AddItem(Can, 1);
            inv.AddItem(Rod, 1);
            inv.AddItem(Seed, 3);
            return inv;
        }

        public void Destroy()
        {
            foreach (Object o in new Object[] { Hoe, Can, Rod, Seed, Fruit, Fish, Crop, Config, ConfigResetSoil })
                Object.DestroyImmediate(o);
        }
    }

    public sealed class FarmPlotStateTests
    {
        private FarmFixture _f;
        private Inventory _inv;
        private FarmPlot _plot;

        [SetUp] public void SetUp() { _f = new FarmFixture(); _inv = _f.StarterBag(); _plot = new FarmPlot(); }
        [TearDown] public void TearDown() => _f.Destroy();

        private void ToWatered(double now)
        {
            Assert.AreEqual(FarmResult.Success, FarmActions.Till(_plot, _f.Hoe, _f.Config));
            Assert.AreEqual(FarmResult.Success, FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv));
            Assert.AreEqual(FarmResult.Success, FarmActions.Water(_plot, _f.Can, _f.Config, now));
        }

        [Test]
        public void NewPlot_IsEmptyAndUntilled()
        {
            Assert.AreEqual(FarmPlotStage.Empty, _plot.GetStage(0));
            Assert.IsFalse(_plot.Tilled);
            Assert.IsFalse(_plot.HasCrop);
        }

        [Test]
        public void FullCycle_TillPlantWaterGrowHarvest()
        {
            Assert.AreEqual(FarmResult.Success, FarmActions.Till(_plot, _f.Hoe, _f.Config));
            Assert.IsTrue(_plot.Tilled);
            Assert.AreEqual(FarmPlotStage.Empty, _plot.GetStage(0));

            Assert.AreEqual(FarmResult.Success, FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv));
            Assert.AreEqual(FarmPlotStage.Planted, _plot.GetStage(0));
            Assert.AreEqual(2, _inv.GetItemCount(_f.Seed), "exactly one seed consumed");

            Assert.AreEqual(FarmResult.Success, FarmActions.Water(_plot, _f.Can, _f.Config, 10));
            Assert.AreEqual(FarmPlotStage.Watered, _plot.GetStage(10));
            Assert.AreEqual(FarmPlotStage.Ready, _plot.GetStage(130));

            Assert.AreEqual(FarmResult.Success, FarmActions.Harvest(_plot, _f.Config, _inv, 130));
            Assert.AreEqual(1, _inv.GetItemCount(_f.Fruit));
            Assert.AreEqual(FarmPlotStage.Empty, _plot.GetStage(130));
            Assert.IsTrue(_plot.Tilled, "config keeps soil tilled after harvest");
        }

        [Test]
        public void Growth_ReadyExactlyAt120ActiveSeconds()
        {
            ToWatered(1000);
            Assert.AreEqual(FarmPlotStage.Watered, _plot.GetStage(1000 + 119.999));
            Assert.AreEqual(FarmPlotStage.Ready, _plot.GetStage(1000 + 120.0));
            Assert.AreEqual(1.0, _plot.GetSecondsLeft(1119), 1e-9);
            Assert.AreEqual(0.5f, _plot.GetGrowthFraction(1060), 1e-6f);
        }

        [Test]
        public void Planted_DoesNotGrowWithoutWater()
        {
            FarmActions.Till(_plot, _f.Hoe, _f.Config);
            FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv);
            Assert.AreEqual(FarmPlotStage.Planted, _plot.GetStage(100000), "no growth before watering; no wilting");
        }

        [Test]
        public void Ready_NeverWilts()
        {
            ToWatered(0);
            Assert.AreEqual(FarmPlotStage.Ready, _plot.GetStage(1e9));
        }

        [Test]
        public void KeepTilledFalse_SoilMustBeTilledAgain()
        {
            Assert.AreEqual(FarmResult.Success, FarmActions.Till(_plot, _f.Hoe, _f.ConfigResetSoil));
            FarmActions.Plant(_plot, _f.Seed, _f.ConfigResetSoil, _inv);
            FarmActions.Water(_plot, _f.Can, _f.ConfigResetSoil, 0);
            Assert.AreEqual(FarmResult.Success, FarmActions.Harvest(_plot, _f.ConfigResetSoil, _inv, 120));
            Assert.IsFalse(_plot.Tilled);
            Assert.AreEqual(FarmResult.NotTilled, FarmActions.Plant(_plot, _f.Seed, _f.ConfigResetSoil, _inv));
        }
    }

    public sealed class FarmActionValidationTests
    {
        private FarmFixture _f;
        private Inventory _inv;
        private FarmPlot _plot;

        [SetUp] public void SetUp() { _f = new FarmFixture(); _inv = _f.StarterBag(); _plot = new FarmPlot(); }
        [TearDown] public void TearDown() => _f.Destroy();

        [Test]
        public void Till_RequiresHoe_AndIsNotRepeated()
        {
            Assert.AreEqual(FarmResult.NeedHoe, FarmActions.Till(_plot, null, _f.Config));
            Assert.AreEqual(FarmResult.NeedHoe, FarmActions.Till(_plot, _f.Can, _f.Config));
            Assert.IsFalse(_plot.Tilled);
            Assert.AreEqual(FarmResult.Success, FarmActions.Till(_plot, _f.Hoe, _f.Config));
            Assert.AreEqual(FarmResult.AlreadyTilled, FarmActions.Till(_plot, _f.Hoe, _f.Config));
        }

        [Test]
        public void Tools_AreNeverConsumed()
        {
            FarmActions.Till(_plot, _f.Hoe, _f.Config);
            FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv);
            FarmActions.Water(_plot, _f.Can, _f.Config, 0);
            Assert.AreEqual(1, _inv.GetItemCount(_f.Hoe));
            Assert.AreEqual(1, _inv.GetItemCount(_f.Can));
        }

        [Test]
        public void Plant_OnUntilledSoil_FailsWithoutConsumingSeed()
        {
            Assert.AreEqual(FarmResult.NotTilled, FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv));
            Assert.AreEqual(3, _inv.GetItemCount(_f.Seed));
            Assert.IsFalse(_plot.HasCrop);
        }

        [Test]
        public void Plant_WithNonSeedItem_Fails()
        {
            FarmActions.Till(_plot, _f.Hoe, _f.Config);
            Assert.AreEqual(FarmResult.UnknownSeed, FarmActions.Plant(_plot, _f.Hoe, _f.Config, _inv));
            Assert.AreEqual(FarmResult.NeedSeed, FarmActions.Plant(_plot, null, _f.Config, _inv));
            Assert.IsFalse(_plot.HasCrop);
        }

        [Test]
        public void Plant_WithoutSeedsInBag_FailsAndNothingChanges()
        {
            FarmActions.Till(_plot, _f.Hoe, _f.Config);
            _inv.RemoveItem(_f.Seed, 3);
            Assert.AreEqual(FarmResult.NotEnoughSeeds, FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv));
            Assert.IsFalse(_plot.HasCrop);
        }

        [Test]
        public void Plant_OnOccupiedPlot_FailsWithoutConsumingSeed()
        {
            FarmActions.Till(_plot, _f.Hoe, _f.Config);
            FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv);
            Assert.AreEqual(FarmResult.PlotOccupied, FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv));
            Assert.AreEqual(2, _inv.GetItemCount(_f.Seed));
        }

        [Test]
        public void Water_RequiresCan_PlantedCrop_AndOnlyOnce()
        {
            Assert.AreEqual(FarmResult.NothingToWater, FarmActions.Water(_plot, _f.Can, _f.Config, 0));
            FarmActions.Till(_plot, _f.Hoe, _f.Config);
            FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv);
            Assert.AreEqual(FarmResult.NeedWateringCan, FarmActions.Water(_plot, _f.Hoe, _f.Config, 0));
            Assert.AreEqual(FarmResult.Success, FarmActions.Water(_plot, _f.Can, _f.Config, 5));
            Assert.AreEqual(FarmResult.AlreadyWatered, FarmActions.Water(_plot, _f.Can, _f.Config, 50));
            Assert.AreEqual(5, _plot.WateredAt, "second watering must not restart growth");
            Assert.AreEqual(FarmResult.AlreadyWatered, FarmActions.Water(_plot, _f.Can, _f.Config, 500), "no watering when ready");
        }

        [Test]
        public void Harvest_BeforeReady_Fails()
        {
            Assert.AreEqual(FarmResult.NotReady, FarmActions.Harvest(_plot, _f.Config, _inv, 0));
            FarmActions.Till(_plot, _f.Hoe, _f.Config);
            FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv);
            Assert.AreEqual(FarmResult.NotReady, FarmActions.Harvest(_plot, _f.Config, _inv, 999));
            FarmActions.Water(_plot, _f.Can, _f.Config, 0);
            Assert.AreEqual(FarmResult.NotReady, FarmActions.Harvest(_plot, _f.Config, _inv, 119));
            Assert.AreEqual(0, _inv.GetItemCount(_f.Fruit));
        }

        [Test]
        public void Harvest_Twice_GivesOnlyOneCrop()
        {
            FarmActions.Till(_plot, _f.Hoe, _f.Config);
            FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv);
            FarmActions.Water(_plot, _f.Can, _f.Config, 0);
            Assert.AreEqual(FarmResult.Success, FarmActions.Harvest(_plot, _f.Config, _inv, 200));
            Assert.AreEqual(FarmResult.NotReady, FarmActions.Harvest(_plot, _f.Config, _inv, 200));
            Assert.AreNotEqual(FarmResult.Success, FarmActions.Perform(_plot, _f.Rod, _f.Config, _inv, 200, out FarmAction again),
                "interacting again must not harvest");
            Assert.AreNotEqual(FarmAction.Harvest, again);
            Assert.AreEqual(1, _inv.GetItemCount(_f.Fruit));
        }

        [Test]
        public void Harvest_WithFullInventory_KeepsCropReadyAndLosesNothing()
        {
            FarmActions.Till(_plot, _f.Hoe, _f.Config);
            FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv);
            FarmActions.Water(_plot, _f.Can, _f.Config, 0);
            // Fill every free slot with fish (no room for the fruit).
            int free = _inv.GetFreeCapacity(_f.Fish);
            Assert.AreEqual(InventoryResult.Success, _inv.AddItem(_f.Fish, free));
            Assert.AreEqual(0, _inv.GetFreeCapacity(_f.Fruit));

            Assert.AreEqual(FarmResult.InventoryFull, FarmActions.Harvest(_plot, _f.Config, _inv, 200));
            Assert.AreEqual(FarmPlotStage.Ready, _plot.GetStage(200), "crop stays ready");
            Assert.AreEqual(0, _inv.GetItemCount(_f.Fruit));

            // One fish less only makes a 98-stack; the fruit needs a free slot, so it still does not fit.
            _inv.RemoveItem(_f.Fish, 1);
            Assert.AreEqual(FarmResult.InventoryFull, FarmActions.Harvest(_plot, _f.Config, _inv, 200));
            _inv.RemoveItem(_f.Fish, 98); // empties the last fish slot
            Assert.AreEqual(FarmResult.Success, FarmActions.Harvest(_plot, _f.Config, _inv, 200));
            Assert.AreEqual(1, _inv.GetItemCount(_f.Fruit));
        }

        [Test]
        public void Harvest_WorksWithAnySelectedItem()
        {
            FarmActions.Till(_plot, _f.Hoe, _f.Config);
            FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv);
            FarmActions.Water(_plot, _f.Can, _f.Config, 0);
            Assert.AreEqual(FarmAction.Harvest, FarmActions.Resolve(_plot, null, _f.Config, 200));
            Assert.AreEqual(FarmResult.Success, FarmActions.Perform(_plot, _f.Rod, _f.Config, _inv, 200, out FarmAction a));
            Assert.AreEqual(FarmAction.Harvest, a);
        }

        [Test]
        public void Resolve_FollowsSelectedItemAndStage()
        {
            Assert.AreEqual(FarmAction.Till, FarmActions.Resolve(_plot, _f.Hoe, _f.Config, 0));
            Assert.AreEqual(FarmAction.None, FarmActions.Resolve(_plot, _f.Seed, _f.Config, 0));
            FarmActions.Till(_plot, _f.Hoe, _f.Config);
            Assert.AreEqual(FarmAction.Plant, FarmActions.Resolve(_plot, _f.Seed, _f.Config, 0));
            Assert.AreEqual(FarmAction.None, FarmActions.Resolve(_plot, _f.Hoe, _f.Config, 0));
            FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv);
            Assert.AreEqual(FarmAction.Water, FarmActions.Resolve(_plot, _f.Can, _f.Config, 0));
            Assert.AreEqual(FarmAction.None, FarmActions.Resolve(_plot, _f.Seed, _f.Config, 0));
            FarmActions.Water(_plot, _f.Can, _f.Config, 0);
            Assert.AreEqual(FarmAction.Wait, FarmActions.Resolve(_plot, _f.Can, _f.Config, 60));
        }

        [Test]
        public void Perform_WithWrongItem_ExplainsAndChangesNothing()
        {
            Assert.AreEqual(FarmResult.NeedHoe, FarmActions.Perform(_plot, _f.Seed, _f.Config, _inv, 0, out _));
            FarmActions.Till(_plot, _f.Hoe, _f.Config);
            Assert.AreEqual(FarmResult.NeedSeed, FarmActions.Perform(_plot, _f.Can, _f.Config, _inv, 0, out _));
            FarmActions.Plant(_plot, _f.Seed, _f.Config, _inv);
            Assert.AreEqual(FarmResult.NeedWateringCan, FarmActions.Perform(_plot, _f.Hoe, _f.Config, _inv, 0, out _));
            Assert.AreEqual(FarmPlotStage.Planted, _plot.GetStage(0));
            Assert.AreEqual(2, _inv.GetItemCount(_f.Seed));
        }

        [Test]
        public void SeedCount_ReachingZero_KeepsHotbarReference_AndCannotPlant()
        {
            var profile = new PlayerProfile();
            profile.Inventory.AddItem(_f.Seed, 1);
            profile.Hotbar.Assign(3, _f.Seed);
            FarmActions.Till(_plot, _f.Hoe, _f.Config);
            Assert.AreEqual(FarmResult.Success, FarmActions.Plant(_plot, _f.Seed, _f.Config, profile.Inventory));
            Assert.AreEqual(_f.Seed, profile.Hotbar.GetItem(3), "reference kept");
            Assert.AreEqual(0, profile.Hotbar.GetCount(3));

            var second = new FarmPlot();
            FarmActions.Till(second, _f.Hoe, _f.Config);
            Assert.AreEqual(FarmResult.NotEnoughSeeds, FarmActions.Plant(second, profile.Hotbar.GetItem(3), _f.Config, profile.Inventory));
            Assert.IsFalse(second.HasCrop, "0 seeds never plants");
            Assert.AreEqual(0, profile.Inventory.GetItemCount(_f.Seed), "count never negative");

            profile.Inventory.AddItem(_f.Seed, 2);
            Assert.AreEqual(FarmResult.Success, FarmActions.Plant(second, profile.Hotbar.GetItem(3), _f.Config, profile.Inventory));
            Assert.AreEqual(1, profile.Inventory.GetItemCount(_f.Seed));
            profile.Dispose();
        }
    }

    public sealed class FarmFieldAndTimeTests
    {
        private FarmFixture _f;
        private Inventory _inv;

        [SetUp] public void SetUp() { _f = new FarmFixture(); _inv = _f.StarterBag(); _inv.AddItem(_f.Seed, 10); }
        [TearDown] public void TearDown() => _f.Destroy();

        [Test]
        public void SixPlots_AreIndependent()
        {
            var field = new FarmField();
            Assert.AreEqual(6, field.Count);
            FarmActions.Till(field.Get(0), _f.Hoe, _f.Config);
            FarmActions.Till(field.Get(3), _f.Hoe, _f.Config);
            FarmActions.Plant(field.Get(3), _f.Seed, _f.Config, _inv);
            FarmActions.Water(field.Get(3), _f.Can, _f.Config, 0);
            Assert.IsTrue(field.Get(0).Tilled);
            Assert.IsFalse(field.Get(1).Tilled);
            Assert.AreEqual(FarmPlotStage.Empty, field.Get(0).GetStage(500));
            Assert.AreEqual(FarmPlotStage.Ready, field.Get(3).GetStage(500));
            for (int i = 0; i < 6; i++)
                if (i != 3) Assert.IsFalse(field.Get(i).HasCrop);
        }

        [Test]
        public void Growth_FollowsActivePlaySeconds_PauseAndDayChange()
        {
            var runner = new GameTimeRunner(GameClockSettings.Default);
            runner.Clock.SetTime(1, 23 * 60 + 30); // 23:30 -> crosses midnight while growing
            var plot = new FarmPlot();
            FarmActions.Till(plot, _f.Hoe, _f.Config);
            FarmActions.Plant(plot, _f.Seed, _f.Config, _inv);
            FarmActions.Water(plot, _f.Can, _f.Config, runner.Clock.ActivePlaySeconds);

            for (int i = 0; i < 480; i++) runner.Tick(0.125); // 60 s active
            object bag = new object();
            runner.Pause.Pause(bag);
            for (int i = 0; i < 4000; i++) runner.Tick(0.125); // 500 s paused: no growth
            Assert.AreEqual(60.0, plot.GetGrowthSeconds(runner.Clock.ActivePlaySeconds), 1e-9);
            Assert.AreEqual(FarmPlotStage.Watered, plot.GetStage(runner.Clock.ActivePlaySeconds));
            runner.Pause.Resume(bag);

            for (int i = 0; i < 479; i++) runner.Tick(0.125); // 119.875 s
            Assert.AreEqual(FarmPlotStage.Watered, plot.GetStage(runner.Clock.ActivePlaySeconds));
            runner.Tick(0.125); // 120.0 s
            Assert.AreEqual(FarmPlotStage.Ready, plot.GetStage(runner.Clock.ActivePlaySeconds));
            Assert.AreEqual(2, runner.Clock.Day, "day changed while growing");
        }

        [Test]
        public void Growth_IgnoresDevSpeedAndTimeJumps()
        {
            var runner = new GameTimeRunner(GameClockSettings.Default) { Speed = 60 };
            var plot = new FarmPlot();
            FarmActions.Till(plot, _f.Hoe, _f.Config);
            FarmActions.Plant(plot, _f.Seed, _f.Config, _inv);
            FarmActions.Water(plot, _f.Can, _f.Config, runner.Clock.ActivePlaySeconds);
            for (int i = 0; i < 80; i++) runner.Tick(0.125); // 10 s real, 10 game hours at x60
            runner.Clock.SetTime(5, 12 * 60);
            Assert.AreEqual(10.0, plot.GetGrowthSeconds(runner.Clock.ActivePlaySeconds), 1e-9);
        }

        [Test]
        public void Data_RoundTrip_KeepsAllPlots()
        {
            var field = new FarmField();
            FarmActions.Till(field.Get(0), _f.Hoe, _f.Config);
            FarmActions.Till(field.Get(1), _f.Hoe, _f.Config);
            FarmActions.Plant(field.Get(1), _f.Seed, _f.Config, _inv);
            FarmActions.Till(field.Get(2), _f.Hoe, _f.Config);
            FarmActions.Plant(field.Get(2), _f.Seed, _f.Config, _inv);
            FarmActions.Water(field.Get(2), _f.Can, _f.Config, 100);
            FarmActions.Till(field.Get(5), _f.Hoe, _f.Config);
            FarmActions.Plant(field.Get(5), _f.Seed, _f.Config, _inv);
            FarmActions.Water(field.Get(5), _f.Can, _f.Config, 0);

            FarmFieldData data = field.ToData(145); // plot 2: 45 s grown; plot 5: ready
            string json = JsonUtility.ToJson(data);
            var restored = new FarmField();
            Assert.IsTrue(restored.TryLoad(JsonUtility.FromJson<FarmFieldData>(json), _f.Config, 10000, out string error), error);

            Assert.IsTrue(restored.Get(0).Tilled);
            Assert.AreEqual(FarmPlotStage.Empty, restored.Get(0).GetStage(10000));
            Assert.AreEqual(FarmPlotStage.Planted, restored.Get(1).GetStage(10000));
            Assert.AreEqual(45.0, restored.Get(2).GetGrowthSeconds(10000), 1e-9);
            Assert.AreEqual(FarmPlotStage.Ready, restored.Get(2).GetStage(10075));
            Assert.AreEqual(FarmPlotStage.Ready, restored.Get(5).GetStage(10000));
            Assert.IsFalse(restored.Get(3).Tilled);
        }

        [Test]
        public void Data_Invalid_IsRejectedWithoutChanges()
        {
            var field = new FarmField();
            FarmActions.Till(field.Get(0), _f.Hoe, _f.Config);
            var good = field.ToData(0);

            var wrongCount = field.ToData(0); wrongCount.plots.RemoveAt(0);
            var unknownCrop = field.ToData(0); unknownCrop.plots[1] = new FarmPlotData { tilled = true, cropId = "banana" };
            var cropUntilled = field.ToData(0); cropUntilled.plots[1] = new FarmPlotData { tilled = false, cropId = "tropical_fruit" };
            var negative = field.ToData(0); negative.plots[1] = new FarmPlotData { tilled = true, cropId = "tropical_fruit", watered = true, growthSeconds = -1 };
            var newer = field.ToData(0); newer.version = 99;

            var target = new FarmField();
            foreach (FarmFieldData bad in new[] { wrongCount, unknownCrop, cropUntilled, negative, newer, null })
            {
                Assert.IsFalse(target.TryLoad(bad, _f.Config, 0, out string error));
                Assert.IsNotEmpty(error);
                Assert.IsFalse(target.Get(0).Tilled, "rejected save changes nothing");
            }
            Assert.IsTrue(target.TryLoad(good, _f.Config, 0, out _));
            Assert.IsTrue(target.Get(0).Tilled);
        }

        [Test]
        public void Config_ValidatesAndMapsSeedToCrop()
        {
            var problems = new System.Collections.Generic.List<string>();
            _f.Config.Validate(problems);
            CollectionAssert.IsEmpty(problems);
            Assert.AreSame(_f.Crop, _f.Config.CropForSeed(_f.Seed));
            Assert.IsNull(_f.Config.CropForSeed(_f.Fish));
            Assert.AreSame(_f.Crop, _f.Config.CropById("tropical_fruit"));
        }
    }

    public sealed class FarmPresentationTests
    {
        [Test]
        public void VisualStage_MapsGrowthToSprites()
        {
            Assert.AreEqual(-1, FarmVisualSet.VisualStageIndex(FarmPlotStage.Empty, 0, 0.5f));
            Assert.AreEqual(0, FarmVisualSet.VisualStageIndex(FarmPlotStage.Planted, 0, 0.5f));
            Assert.AreEqual(1, FarmVisualSet.VisualStageIndex(FarmPlotStage.Watered, 0.49f, 0.5f));
            Assert.AreEqual(2, FarmVisualSet.VisualStageIndex(FarmPlotStage.Watered, 0.5f, 0.5f));
            Assert.AreEqual(3, FarmVisualSet.VisualStageIndex(FarmPlotStage.Ready, 1f, 0.5f));
        }

        [Test]
        public void CropScale_PerStage_DefaultsToOne()
        {
            var set = ScriptableObject.CreateInstance<FarmVisualSet>();
            var so = new UnityEditor.SerializedObject(set);
            var list = so.FindProperty("crops"); list.arraySize = 1;
            var e = list.GetArrayElementAtIndex(0);
            e.FindPropertyRelative("cropId").stringValue = "tropical_fruit";
            e.FindPropertyRelative("seededScale").floatValue = 0.68f;
            e.FindPropertyRelative("sproutScale").floatValue = 0.81f;
            e.FindPropertyRelative("youngScale").floatValue = 0.78f;
            e.FindPropertyRelative("readyScale").floatValue = 0f; // not set -> 1
            so.ApplyModifiedPropertiesWithoutUndo();
            Assert.AreEqual(0.68f, set.CropScale("tropical_fruit", FarmPlotStage.Planted, 0f), 1e-6f);
            Assert.AreEqual(0.81f, set.CropScale("tropical_fruit", FarmPlotStage.Watered, 0.2f), 1e-6f);
            Assert.AreEqual(0.78f, set.CropScale("tropical_fruit", FarmPlotStage.Watered, 0.7f), 1e-6f);
            Assert.AreEqual(1f, set.CropScale("tropical_fruit", FarmPlotStage.Ready, 1f));
            Assert.AreEqual(1f, set.CropScale("banana", FarmPlotStage.Ready, 1f), "unknown crop");
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Placeholders_ExistForEveryState()
        {
            Assert.IsNotNull(FarmPlaceholderSprites.Soil(false, false));
            Assert.IsNotNull(FarmPlaceholderSprites.Soil(true, false));
            Assert.IsNotNull(FarmPlaceholderSprites.Soil(true, true));
            for (int i = 0; i < 4; i++) Assert.IsNotNull(FarmPlaceholderSprites.Crop(i));
            Assert.IsNull(FarmPlaceholderSprites.Crop(-1));
            Assert.AreNotSame(FarmPlaceholderSprites.NeedsWater, FarmPlaceholderSprites.Ready);
            Assert.IsNotNull(FarmPlaceholderSprites.BadgeBack, "badge backing keeps icons readable over leaves");
            Assert.AreEqual(new Vector2(32f, 8f), FarmPlaceholderSprites.Crop(2).pivot, "crop pivot (0.5, 0.125)");
        }

        [Test]
        public void Prompts_AreEnglishAndActionSpecific()
        {
            var plot = new FarmPlot();
            Assert.AreEqual("Till soil", FarmText.Prompt(FarmAction.Till, plot, FarmPlotStage.Empty, 0));
            Assert.AreEqual("Plant seeds", FarmText.Prompt(FarmAction.Plant, plot, FarmPlotStage.Empty, 0));
            Assert.AreEqual("Water crop", FarmText.Prompt(FarmAction.Water, plot, FarmPlotStage.Planted, 0));
            Assert.AreEqual("Growing - 1:05 left", FarmText.Prompt(FarmAction.Wait, plot, FarmPlotStage.Watered, 64.2));
            Assert.AreEqual("Untilled soil: select Hoe", FarmText.Prompt(FarmAction.None, plot, FarmPlotStage.Empty, 0));
            StringAssert.StartsWith("Inventory full.", FarmText.Message(FarmResult.InventoryFull, plot));
            Assert.AreEqual("", FarmText.Message(FarmResult.Success, plot));
        }

        [Test]
        public void ItemIcons_OverrideByIdThenDefinitionThenNull()
        {
            ItemIcons.Reset();
            var item = ItemDefinition.CreateRuntime("crop_tropical_fruit", "Tropical Fruit", ItemCategory.Crop, true);
            Assert.IsNull(ItemIcons.Get(item), "no art: UI uses placeholder");
            Sprite art = FarmPlaceholderSprites.Ready;
            var overrides = ItemIconOverrides.CreateRuntime(new ItemIconOverrides.Entry { itemId = "crop_tropical_fruit", icon = art });
            ItemIcons.Register(overrides);
            Assert.AreSame(art, ItemIcons.Get(item));
            ItemIcons.Unregister(overrides);
            Assert.IsNull(ItemIcons.Get(item));
            ItemIcons.Reset();
            Object.DestroyImmediate(overrides);
            Object.DestroyImmediate(item);
        }
    }
}

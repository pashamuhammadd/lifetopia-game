using System;
using Lifetopia.Fishing;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using Lifetopia.World;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lifetopia.Tests
{
    internal sealed class FishFixture
    {
        public readonly ItemDefinition Rod = ItemDefinition.CreateRuntime("tool_fishing_rod", "Fishing Rod", ItemCategory.Tool, false);
        public readonly ItemDefinition Hoe = ItemDefinition.CreateRuntime("tool_hoe", "Hoe", ItemCategory.Tool, false);
        public readonly ItemDefinition Fish = ItemDefinition.CreateRuntime("fish_reef", "Reef Fish", ItemCategory.Fish, true, 99, 12);
        public readonly ItemDefinition Wood = ItemDefinition.CreateRuntime("material_driftwood", "Driftwood", ItemCategory.Material, true, 99, 2);
        public readonly FishingConfig Config;
        public FishFixture() { Config = FishingConfig.CreateRuntime(Rod, Fish, Settings); }

        /// <summary>Deterministic numbers: cast 0.3, bite 1.2-2.0, 8 s limit, zone 0.48-0.76, 0.75 cycles/s.</summary>
        public static FishingSettings Settings => FishingSettings.Default;

        public Inventory Bag()
        {
            var inv = new Inventory();
            inv.AddItem(Hoe, 1);
            inv.AddItem(Rod, 1);
            return inv;
        }

        public void Destroy()
        {
            foreach (Object o in new Object[] { Rod, Hoe, Fish, Wood, Config }) Object.DestroyImmediate(o);
        }

        /// <summary>Ticks in 0.05 s steps until <paramref name="until"/> is true or 10 s pass.</summary>
        public static void TickUntil(FishingSession s, Func<bool> until)
        {
            for (int i = 0; i < 200 && !until(); i++) s.Tick(0.05f);
        }

        /// <summary>Ticks during Timing until the marker is inside the zone.</summary>
        public static void TickIntoZone(FishingSession s)
        {
            TickUntil(s, () => s.State == FishingState.Timing);
            TickUntil(s, () => s.State != FishingState.Timing || s.InZone(s.Marker));
        }
    }

    public sealed class FishingSessionTests
    {
        private FishingSession _s;
        [SetUp] public void SetUp() => _s = new FishingSession(FishFixture.Settings);

        [Test]
        public void States_Idle_Cast_Wait_Timing()
        {
            Assert.AreEqual(FishingState.Idle, _s.State);
            Assert.IsTrue(_s.Begin(1.5f));
            Assert.AreEqual(FishingState.Casting, _s.State);
            Assert.AreEqual(1, _s.AttemptId);
            _s.Tick(0.1f); _s.Tick(0.1f);
            Assert.AreEqual(FishingState.Casting, _s.State, "0.2 s < 0.3 s cast");
            _s.Tick(0.1f); _s.Tick(0.1f);
            Assert.AreEqual(FishingState.WaitingForBite, _s.State);
            for (int i = 0; i < 13; i++) _s.Tick(0.1f); // 1.7 s < 0.3 cast + 1.5 bite
            Assert.AreEqual(FishingState.WaitingForBite, _s.State);
            _s.Tick(0.1f); _s.Tick(0.1f); // 1.9 s
            Assert.AreEqual(FishingState.Timing, _s.State);
        }

        [Test]
        public void BiteDelay_IsClampedToConfiguredRange()
        {
            _s.Begin(0.1f);
            Assert.AreEqual(1.2f, _s.BiteDelay, 1e-6f);
            _s.Cancel(); _s.Reset();
            _s.Begin(99f);
            Assert.AreEqual(2.0f, _s.BiteDelay, 1e-6f);
            _s.Cancel(); _s.Reset();
            _s.Begin(float.NaN);
            Assert.AreEqual(1.2f, _s.BiteDelay, 1e-6f);
        }

        [Test]
        public void PressTooEarly_DuringCastOrWait_Fails()
        {
            _s.Begin(1.5f);
            Assert.AreEqual(FishingOutcome.TooEarly, _s.Press());
            Assert.AreEqual(FishingState.Failure, _s.State);
            _s.Reset();
            _s.Begin(1.5f);
            FishFixture.TickUntil(_s, () => _s.State == FishingState.WaitingForBite);
            Assert.AreEqual(FishingOutcome.TooEarly, _s.Press());
        }

        [Test]
        public void PressInZone_Catches_OutsideMisses()
        {
            _s.Begin(1.2f);
            FishFixture.TickIntoZone(_s);
            Assert.IsTrue(_s.InZone(_s.Marker));
            Assert.AreEqual(FishingOutcome.Caught, _s.Press());
            Assert.AreEqual(FishingState.Success, _s.State);

            _s.Reset();
            _s.Begin(1.2f);
            FishFixture.TickUntil(_s, () => _s.State == FishingState.Timing);
            Assert.IsFalse(_s.InZone(_s.Marker), "marker starts at the left, outside the zone");
            Assert.AreEqual(FishingOutcome.Missed, _s.Press());
        }

        [Test]
        public void ZoneBoundaries_AreInclusive()
        {
            FishingSettings s = FishFixture.Settings;
            Assert.IsTrue(_s.InZone(s.ZoneMin));
            Assert.IsTrue(_s.InZone(s.ZoneMax));
            Assert.IsFalse(_s.InZone(s.ZoneMin - 0.001f));
            Assert.IsFalse(_s.InZone(s.ZoneMax + 0.001f));
            Assert.AreEqual(0.28f, s.ZoneMax - s.ZoneMin, 1e-5f);
        }

        [Test]
        public void NoPress_TimesOutAtEightSeconds()
        {
            _s.Begin(1.5f);
            for (int i = 0; i < 79; i++) _s.Tick(0.1f); // 7.9 s
            Assert.AreEqual(FishingState.Timing, _s.State);
            for (int i = 0; i < 3 && _s.IsActive; i++) _s.Tick(0.1f);
            Assert.AreEqual(FishingState.Failure, _s.State);
            Assert.AreEqual(FishingOutcome.TooLate, _s.Outcome);
            Assert.That(_s.Elapsed, Is.InRange(8f, 8.1f + 1e-4f), "limit is 8 s of active time");
        }

        [Test]
        public void HugeFrame_IsClamped_NoCatchUp()
        {
            _s.Begin(1.5f);
            _s.Tick(30f); // e.g. tab was hidden; the caller resumes with a huge delta
            Assert.AreEqual(0.1f, _s.Elapsed, 1e-6f);
            Assert.AreEqual(FishingState.Casting, _s.State);
            _s.Tick(-1f); _s.Tick(float.NaN); _s.Tick(float.PositiveInfinity);
            Assert.AreEqual(0.1f, _s.Elapsed, 1e-6f);
        }

        [Test]
        public void NoReentry_WhileActive_AndFinishedAttemptIsFrozen()
        {
            Assert.IsTrue(_s.Begin(1.5f));
            Assert.IsFalse(_s.Begin(1.5f));
            Assert.AreEqual(1, _s.AttemptId);
            _s.Press(); // too early
            Assert.AreEqual(FishingOutcome.None, _s.Press(), "second press after finish is ignored");
            _s.Tick(5f);
            _s.Cancel();
            Assert.AreEqual(FishingState.Failure, _s.State, "finished attempt never changes");
            Assert.IsTrue(_s.Begin(1.5f));
            Assert.AreEqual(2, _s.AttemptId);
        }

        [Test]
        public void Cancel_FromEveryActiveState()
        {
            foreach (FishingState target in new[] { FishingState.Casting, FishingState.WaitingForBite, FishingState.Timing })
            {
                _s.Reset();
                _s.Begin(1.2f);
                FishFixture.TickUntil(_s, () => _s.State == target);
                _s.Cancel();
                Assert.AreEqual(FishingState.Cancelled, _s.State, target.ToString());
                Assert.AreEqual(FishingOutcome.Cancelled, _s.Outcome);
            }
        }

        [Test]
        public void Marker_PingPongs()
        {
            Assert.AreEqual(0f, FishingSession.PingPong(0f), 1e-6f);
            Assert.AreEqual(1f, FishingSession.PingPong(0.5f), 1e-6f);
            Assert.AreEqual(0f, FishingSession.PingPong(1f), 1e-6f);
            Assert.AreEqual(0.5f, FishingSession.PingPong(0.25f), 1e-6f);
        }

        [Test]
        public void InvalidSettings_AreRejected()
        {
            FishingSettings bad = FishFixture.Settings; bad.zoneWidth = 0f;
            Assert.Throws<ArgumentException>(() => new FishingSession(bad));
            bad = FishFixture.Settings; bad.maxAttemptSeconds = 2f;
            Assert.IsFalse(bad.Validate(out _));
            bad = FishFixture.Settings; bad.biteDelayMax = 0.5f;
            Assert.IsFalse(bad.Validate(out _));
            Assert.IsTrue(FishFixture.Settings.Validate(out _));
        }
    }

    public sealed class FishingRewardTests
    {
        private FishFixture _f;
        private Inventory _inv;
        private FishingSession _s;
        private FishingRewards _r;

        [SetUp] public void SetUp() { _f = new FishFixture(); _inv = _f.Bag(); _s = new FishingSession(FishFixture.Settings); _r = new FishingRewards(); }
        [TearDown] public void TearDown() => _f.Destroy();

        private void Catch()
        {
            _s.Reset();
            _s.Begin(1.2f);
            FishFixture.TickIntoZone(_s);
            Assert.AreEqual(FishingOutcome.Caught, _s.Press());
        }

        [Test]
        public void Start_NeedsRodSelectedAndOwned()
        {
            Assert.AreEqual(FishingStartResult.NeedRod, FishingRewards.CanStart(_f.Config, null, _inv, _s));
            Assert.AreEqual(FishingStartResult.NeedRod, FishingRewards.CanStart(_f.Config, _f.Hoe, _inv, _s));
            Assert.AreEqual(FishingStartResult.Ok, FishingRewards.CanStart(_f.Config, _f.Rod, _inv, _s));
            _inv.RemoveItem(_f.Rod, 1);
            Assert.AreEqual(FishingStartResult.NeedRod, FishingRewards.CanStart(_f.Config, _f.Rod, _inv, _s), "rod reference without the item");
        }

        [Test]
        public void Start_BlockedWhenBusyOrFull()
        {
            _s.Begin(1.5f);
            Assert.AreEqual(FishingStartResult.Busy, FishingRewards.CanStart(_f.Config, _f.Rod, _inv, _s));
            _s.Cancel();
            _inv.AddItem(_f.Wood, _inv.GetFreeCapacity(_f.Wood));
            Assert.AreEqual(FishingStartResult.InventoryFull, FishingRewards.CanStart(_f.Config, _f.Rod, _inv, _s));
        }

        [Test]
        public void Catch_AwardsExactlyOneFish_RodNotConsumed()
        {
            Catch();
            Assert.AreEqual(FishingAwardResult.Awarded, _r.TryAward(_s, _f.Config, _inv));
            Assert.AreEqual(1, _inv.GetItemCount(_f.Fish));
            Assert.AreEqual(1, _inv.GetItemCount(_f.Rod));
        }

        [Test]
        public void SameAttempt_NeverPaysTwice()
        {
            Catch();
            _r.TryAward(_s, _f.Config, _inv);
            Assert.AreEqual(FishingAwardResult.AlreadyAwarded, _r.TryAward(_s, _f.Config, _inv));
            Assert.AreEqual(FishingAwardResult.AlreadyAwarded, _r.TryAward(_s, _f.Config, _inv));
            Assert.AreEqual(1, _inv.GetItemCount(_f.Fish));
            Catch();
            Assert.AreEqual(FishingAwardResult.Awarded, _r.TryAward(_s, _f.Config, _inv));
            Assert.AreEqual(2, _inv.GetItemCount(_f.Fish));
        }

        [Test]
        public void Failure_Cancel_Timeout_AwardNothing_AndLoseNothing()
        {
            int wood = 5; _inv.AddItem(_f.Wood, wood);
            _s.Begin(1.5f); _s.Press();
            Assert.AreEqual(FishingAwardResult.NotCaught, _r.TryAward(_s, _f.Config, _inv));
            _s.Reset(); _s.Begin(1.5f); _s.Cancel();
            Assert.AreEqual(FishingAwardResult.NotCaught, _r.TryAward(_s, _f.Config, _inv));
            _s.Reset(); _s.Begin(1.5f); FishFixture.TickUntil(_s, () => _s.IsFinished);
            Assert.AreEqual(FishingOutcome.TooLate, _s.Outcome);
            Assert.AreEqual(FishingAwardResult.NotCaught, _r.TryAward(_s, _f.Config, _inv));
            Assert.AreEqual(0, _inv.GetItemCount(_f.Fish));
            Assert.AreEqual(1, _inv.GetItemCount(_f.Rod));
            Assert.AreEqual(wood, _inv.GetItemCount(_f.Wood));
        }

        [Test]
        public void BagFilledDuringAttempt_NoAward_NothingLost_ThenRetryWorks()
        {
            Catch();
            _inv.AddItem(_f.Wood, _inv.GetFreeCapacity(_f.Wood)); // capacity changed after the start check
            int before = _inv.GetItemCount(_f.Wood);
            Assert.AreEqual(FishingAwardResult.InventoryFull, _r.TryAward(_s, _f.Config, _inv));
            Assert.AreEqual(0, _inv.GetItemCount(_f.Fish));
            Assert.AreEqual(before, _inv.GetItemCount(_f.Wood));
            _inv.RemoveItem(_f.Wood, 99);
            Catch();
            Assert.AreEqual(FishingAwardResult.Awarded, _r.TryAward(_s, _f.Config, _inv));
            Assert.AreEqual(1, _inv.GetItemCount(_f.Fish));
        }

        [Test]
        public void SpamPresses_OneAward()
        {
            _s.Begin(1.2f);
            FishFixture.TickIntoZone(_s);
            for (int i = 0; i < 10; i++) _s.Press();
            for (int i = 0; i < 10; i++) _r.TryAward(_s, _f.Config, _inv);
            Assert.AreEqual(1, _inv.GetItemCount(_f.Fish));
        }

        [Test]
        public void Texts_AreEnglish()
        {
            Assert.AreEqual("Wait for a bite", FishingText.Status(FishingState.WaitingForBite, FishingOutcome.None, "Reef Fish"));
            Assert.AreEqual("Caught a Reef Fish!", FishingText.Status(FishingState.Success, FishingOutcome.Caught, "Reef Fish"));
            Assert.AreEqual("Missed", FishingText.Status(FishingState.Failure, FishingOutcome.Missed, "Reef Fish"));
            StringAssert.StartsWith("Inventory full", FishingText.StartMessage(FishingStartResult.InventoryFull, _f.Fish));
        }

        [Test]
        public void Config_Validates()
        {
            var problems = new System.Collections.Generic.List<string>();
            _f.Config.Validate(problems);
            CollectionAssert.IsEmpty(problems);
            var broken = FishingConfig.CreateRuntime(null, _f.Fish, FishFixture.Settings);
            broken.Validate(problems);
            Assert.IsNotEmpty(problems);
            Object.DestroyImmediate(broken);
        }
    }

    public sealed class DualGridTests
    {
        [Test]
        public void CornerBits_FollowPackContract()
        {
            Assert.AreEqual(1, DualGridMask.Compute(true, false, false, false));
            Assert.AreEqual(2, DualGridMask.Compute(false, true, false, false));
            Assert.AreEqual(4, DualGridMask.Compute(false, false, true, false));
            Assert.AreEqual(8, DualGridMask.Compute(false, false, false, true));
            Assert.AreEqual(15, DualGridMask.Compute(true, true, true, true));
        }

        [Test]
        public void AtVertex_ReadsTheFourCellsAroundTheVertex()
        {
            // Only cell (0,0) is path: it is NE of vertex (0,0), NW of (1,0), SE of (0,1), SW of (1,1).
            Func<int, int, bool> one = (x, y) => x == 0 && y == 0;
            Assert.AreEqual(DualGridMask.NE, DualGridMask.AtVertex(one, 0, 0));
            Assert.AreEqual(DualGridMask.NW, DualGridMask.AtVertex(one, 1, 0));
            Assert.AreEqual(DualGridMask.SE, DualGridMask.AtVertex(one, 0, 1));
            Assert.AreEqual(DualGridMask.SW, DualGridMask.AtVertex(one, 1, 1));
            Assert.AreEqual(0, DualGridMask.AtVertex(one, 5, 5));
        }

        [Test]
        public void ThreeByThreePath_ProducesCornersEdgesAndCentre()
        {
            Func<int, int, bool> block = (x, y) => x >= 0 && x < 3 && y >= 0 && y < 3;
            Assert.AreEqual(DualGridMask.NE, DualGridMask.AtVertex(block, 0, 0), "outer corner SW of the block");
            Assert.AreEqual(DualGridMask.NE | DualGridMask.NW, DualGridMask.AtVertex(block, 1, 0), "bottom edge");
            Assert.AreEqual(15, DualGridMask.AtVertex(block, 1, 1), "inside");
            Func<int, int, bool> lShape = (x, y) => !(x >= 1 && y >= 1); // concave corner at vertex (1,1)
            Assert.AreEqual(15 & ~DualGridMask.NE, DualGridMask.AtVertex(lShape, 1, 1));
        }

        [Test]
        public void Classify_TerrainShoreWaterAndUnsupported()
        {
            Assert.AreEqual(new DualGridChoice(DualGridKind.Terrain, 0).ToString(), DualGridMask.Classify(0, 0).ToString());
            Assert.AreEqual(DualGridKind.Terrain, DualGridMask.Classify(5, 0).Kind, "diagonal grass/path exists in Batch C");
            Assert.AreEqual(DualGridKind.Water, DualGridMask.Classify(0, 15).Kind);
            DualGridChoice shore = DualGridMask.Classify(DualGridMask.NW | DualGridMask.NE, DualGridMask.SE | DualGridMask.SW);
            Assert.AreEqual(DualGridKind.Shore, shore.Kind);
            Assert.AreEqual(12, shore.Mask);
            Assert.AreEqual(DualGridKind.Unsupported, DualGridMask.Classify(0, 12).Kind, "grass touching water");
            Assert.AreEqual(DualGridKind.Unsupported, DualGridMask.Classify(15, 5).Kind, "diagonal shore 5 has no art");
            Assert.AreEqual(DualGridKind.Unsupported, DualGridMask.Classify(15, 10).Kind, "diagonal shore 10 has no art");
            Assert.IsTrue(DualGridMask.IsDiagonal(5));
            Assert.IsFalse(DualGridMask.IsDiagonal(3));
        }
    }
}

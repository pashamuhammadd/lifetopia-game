using System.Collections.Generic;
using Lifetopia.TimeSystem;
using NUnit.Framework;
using UnityEngine;

namespace Lifetopia.Tests
{
    public sealed class GameClockTests
    {
        private static GameClock NewClock() => new GameClock(GameClockSettings.Default);

        /// <summary>Advances in small frame-sized steps (each step is below the max-step clamp).</summary>
        private static void Play(GameTimeRunner runner, double seconds, double frame = 0.125)
        {
            for (double t = 0; t < seconds - 1e-9; t += frame) runner.Tick(frame);
        }

        private static void Play(GameClock clock, double seconds, double frame = 0.125)
        {
            for (double t = 0; t < seconds - 1e-9; t += frame) clock.Advance(frame);
        }

        [Test]
        public void StartsOnDay1_At0800_Daytime()
        {
            var clock = NewClock();
            Assert.AreEqual(1, clock.Day);
            Assert.AreEqual(8, clock.Hour);
            Assert.AreEqual(0, clock.Minute);
            Assert.AreEqual(480, clock.MinuteOfDay);
            Assert.AreEqual(TimeOfDayPeriod.Daytime, clock.Period);
            Assert.AreEqual(0.0, clock.ActivePlaySeconds);
            Assert.AreEqual("08:00", ClockText.Time24(clock.MinuteOfDay));
        }

        [Test]
        public void SixtyRealSeconds_IsOneGameHour_AndOneDayIs24Minutes()
        {
            var clock = NewClock();
            Play(clock, 1.0);
            Assert.AreEqual(481, clock.MinuteOfDay, "1 real second = 1 game minute");
            Play(clock, 59.0);
            Assert.AreEqual(9, clock.Hour);
            Assert.AreEqual(0, clock.Minute);
            Assert.AreEqual(60.0, clock.ActivePlaySeconds, 1e-6);

            Play(clock, 24 * 60.0);
            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual(540, clock.MinuteOfDay, "24 real minutes later it is the same time next day");
        }

        [Test]
        public void ConfigurableSpeed_TwoRealSecondsPerHour()
        {
            GameClockSettings s = GameClockSettings.Default;
            s.realSecondsPerGameHour = 2f;
            var clock = new GameClock(s);
            Play(clock, 1.0);
            Assert.AreEqual(8 * 60 + 30, clock.MinuteOfDay, "2 real s per hour = 30 game minutes per real second");
        }

        [Test]
        public void LongFrame_IsClampedToMaxStep()
        {
            var clock = NewClock();
            clock.Advance(30.0); // a 30 s hitch must not skip 30 game minutes
            Assert.AreEqual(0.5, clock.ActivePlaySeconds, 1e-9);
            Assert.AreEqual(480, clock.MinuteOfDay);
        }

        [TestCase(0.0)]
        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidDelta_IsIgnored(double delta)
        {
            var clock = NewClock();
            clock.Advance(delta);
            Assert.AreEqual(480, clock.MinuteOfDay);
            Assert.AreEqual(0.0, clock.ActivePlaySeconds);
        }

        [Test]
        public void MidnightRollover_GoesTo0000_AndNextDay()
        {
            var clock = NewClock();
            clock.SetTime(1, 23 * 60 + 59);
            var days = new List<int>();
            clock.DayChanged += d => days.Add(d);
            Play(clock, 1.0);
            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual(0, clock.MinuteOfDay);
            Assert.AreEqual("00:00", ClockText.Time24(clock.MinuteOfDay));
            CollectionAssert.AreEqual(new[] { 2 }, days);
        }

        [Test]
        public void DayCounter_RaisesOncePerDayPassed()
        {
            var clock = NewClock();
            var days = new List<int>();
            clock.DayChanged += d => days.Add(d);
            clock.AdvanceGameMinutes(3 * 1440);
            Assert.AreEqual(4, clock.Day);
            Assert.AreEqual(480, clock.MinuteOfDay);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, days);
            Assert.AreEqual(3 * 1440L + 480, clock.TotalMinutes);
        }

        [Test]
        public void MinuteAndHourEvents_FireOncePerChange()
        {
            var clock = NewClock();
            int minutes = 0;
            var hours = new List<int>();
            clock.MinuteChanged += () => minutes++;
            clock.HourChanged += h => hours.Add(h);
            Play(clock, 0.5);
            Assert.AreEqual(0, minutes, "no whole minute passed yet");
            Play(clock, 0.5);
            Assert.AreEqual(1, minutes);
            Play(clock, 59.0);
            Assert.AreEqual(60, minutes);
            CollectionAssert.AreEqual(new[] { 9 }, hours);
        }

        [TestCase(0, TimeOfDayPeriod.Night)]
        [TestCase(4 * 60 + 59, TimeOfDayPeriod.Night)]
        [TestCase(5 * 60, TimeOfDayPeriod.Morning)]
        [TestCase(7 * 60 + 59, TimeOfDayPeriod.Morning)]
        [TestCase(8 * 60, TimeOfDayPeriod.Daytime)]
        [TestCase(16 * 60 + 59, TimeOfDayPeriod.Daytime)]
        [TestCase(17 * 60, TimeOfDayPeriod.Sunset)]
        [TestCase(18 * 60 + 59, TimeOfDayPeriod.Sunset)]
        [TestCase(19 * 60, TimeOfDayPeriod.Night)]
        [TestCase(23 * 60 + 59, TimeOfDayPeriod.Night)]
        public void PeriodBoundaries(int minute, TimeOfDayPeriod expected) =>
            Assert.AreEqual(expected, GameClockSettings.Default.PeriodAt(minute));

        [Test]
        public void PeriodChanged_FiresWhenCrossingSunset()
        {
            var clock = NewClock();
            clock.SetTime(1, 16 * 60 + 59);
            var periods = new List<TimeOfDayPeriod>();
            clock.PeriodChanged += p => periods.Add(p);
            Play(clock, 1.0);
            CollectionAssert.AreEqual(new[] { TimeOfDayPeriod.Sunset }, periods);
            clock.SetTime(1, 22 * 60);
            Assert.AreEqual(TimeOfDayPeriod.Night, clock.Period);
        }

        [Test]
        public void SetTime_RejectsInvalidValues()
        {
            var clock = NewClock();
            Assert.IsFalse(clock.SetTime(0, 600));
            Assert.IsFalse(clock.SetTime(1, -1));
            Assert.IsFalse(clock.SetTime(1, 1440));
            Assert.AreEqual(480, clock.MinuteOfDay);
        }

        [Test]
        public void ClockText_Is24Hour()
        {
            Assert.AreEqual("00:00", ClockText.Time24(0));
            Assert.AreEqual("08:05", ClockText.Time24(485));
            Assert.AreEqual("13:30", ClockText.Time24(810));
            Assert.AreEqual("23:59", ClockText.Time24(1439));
            Assert.AreSame(ClockText.Time24(810), ClockText.Time24(810), "strings are cached");
            Assert.AreEqual("Day 12", ClockText.DayLabel(12));
            Assert.AreEqual("Sunset", ClockText.PeriodName(TimeOfDayPeriod.Sunset));
        }
    }

    public sealed class GameTimePauseTests
    {
        private static readonly object Inventory = new object();
        private static readonly object Dialogue = new object();
        private static readonly object Focus = new object();

        private static void Play(GameTimeRunner runner, double seconds)
        {
            for (double t = 0; t < seconds - 1e-9; t += 0.125) runner.Tick(0.125);
        }

        [Test]
        public void PauseAndResume()
        {
            var runner = new GameTimeRunner(GameClockSettings.Default);
            runner.Pause.Pause(Inventory);
            Play(runner, 10);
            Assert.AreEqual(480, runner.Clock.MinuteOfDay);
            runner.Pause.Resume(Inventory);
            Play(runner, 10);
            Assert.AreEqual(490, runner.Clock.MinuteOfDay);
        }

        [Test]
        public void NoTimePasses_WhileBrowserIsInactive()
        {
            var runner = new GameTimeRunner(GameClockSettings.Default);
            Play(runner, 5);
            runner.Pause.Pause(Focus); // tab hidden / window blurred
            Play(runner, 120);
            Assert.AreEqual(485, runner.Clock.MinuteOfDay);
            Assert.AreEqual(5.0, runner.Clock.ActivePlaySeconds, 1e-6, "inactive time is not active play time");
            runner.Pause.Resume(Focus);
            Play(runner, 5);
            Assert.AreEqual(490, runner.Clock.MinuteOfDay);
        }

        [Test]
        public void SeveralPauseSources_AllMustResume()
        {
            var runner = new GameTimeRunner(GameClockSettings.Default);
            var changes = new List<bool>();
            runner.Pause.PausedChanged += p => changes.Add(p);
            runner.Pause.Pause(Inventory);
            runner.Pause.Pause(Focus);
            runner.Pause.Pause(Focus); // same source twice counts once
            Assert.AreEqual(2, runner.Pause.SourceCount);
            runner.Pause.Resume(Inventory);
            Play(runner, 10);
            Assert.AreEqual(480, runner.Clock.MinuteOfDay, "still paused by focus");
            runner.Pause.Resume(Dialogue); // never paused: no effect
            Assert.IsTrue(runner.Pause.IsPaused);
            runner.Pause.Resume(Focus);
            Assert.IsFalse(runner.Pause.IsPaused);
            CollectionAssert.AreEqual(new[] { true, false }, changes);
            runner.Pause.Pause(null);
            Assert.IsFalse(runner.Pause.IsPaused, "null source is ignored");
        }

        [Test]
        public void Speed_MultipliesGameTime_NotActivePlayTime()
        {
            var runner = new GameTimeRunner(GameClockSettings.Default) { Speed = 10 };
            Play(runner, 6);
            Assert.AreEqual(9 * 60, runner.Clock.MinuteOfDay);
            Assert.AreEqual(6.0, runner.Clock.ActivePlaySeconds, 1e-6);
            runner.Speed = -5;
            Assert.AreEqual(0, runner.Speed);
            Play(runner, 5);
            Assert.AreEqual(9 * 60, runner.Clock.MinuteOfDay);
        }
    }

    public sealed class GameTimeConfigAndSaveTests
    {
        [Test]
        public void DefaultSettings_AreValid()
        {
            Assert.IsTrue(GameClockSettings.Default.Validate(out string error), error);
            Assert.AreEqual(1.0, GameClockSettings.Default.GameMinutesPerRealSecond, 1e-9);
        }

        private static GameClockSettings With(System.Func<GameClockSettings, GameClockSettings> change) => change(GameClockSettings.Default);

        [Test]
        public void InvalidSettings_AreRejected_AndClockFallsBackToDefaults()
        {
            var invalid = new[]
            {
                With(s => { s.realSecondsPerGameHour = 0f; return s; }),
                With(s => { s.realSecondsPerGameHour = -60f; return s; }),
                With(s => { s.realSecondsPerGameHour = float.NaN; return s; }),
                With(s => { s.startMinuteOfDay = 1440; return s; }),
                With(s => { s.startDay = 0; return s; }),
                With(s => { s.maxStepSeconds = 0f; return s; }),
                With(s => { s.sunsetStartMinute = 7 * 60; return s; }),   // out of order
                With(s => { s.nightStartMinute = 2000; return s; }),
            };
            foreach (GameClockSettings settings in invalid)
            {
                Assert.IsFalse(settings.Validate(out string error));
                Assert.IsNotEmpty(error);
                var clock = new GameClock(settings);
                Assert.IsFalse(clock.SettingsWereValid);
                Assert.AreEqual(480, clock.MinuteOfDay, "falls back to the default 08:00 start");
                clock.Advance(0.25);
                Assert.Greater(clock.ExactMinuteOfDay, 480.0, "and still runs at the default speed");
            }
        }

        [Test]
        public void State_RoundTripsThroughJson()
        {
            var clock = new GameClock(GameClockSettings.Default);
            for (int i = 0; i < 25; i++) clock.Advance(0.4);   // 10 s active
            clock.AdvanceGameMinutes(2 * 1440 + 125.5);
            string json = JsonUtility.ToJson(clock.ToData());

            var restored = new GameClock(GameClockSettings.Default);
            int jumps = 0, minutes = 0, days = 0;
            restored.TimeJumped += () => jumps++;
            restored.MinuteChanged += () => minutes++;
            restored.DayChanged += _ => days++;
            Assert.IsTrue(restored.TryRestore(JsonUtility.FromJson<GameClockData>(json), out string error), error);
            Assert.AreEqual(clock.Day, restored.Day);
            Assert.AreEqual(clock.ExactMinuteOfDay, restored.ExactMinuteOfDay, 1e-9);
            Assert.AreEqual(clock.ActivePlaySeconds, restored.ActivePlaySeconds, 1e-9);
            Assert.AreEqual(clock.TotalMinutes, restored.TotalMinutes);
            Assert.AreEqual(clock.Period, restored.Period);
            Assert.AreEqual(1, jumps);
            Assert.AreEqual(1, minutes);
            Assert.AreEqual(0, days, "restoring is not days passing");
        }

        [Test]
        public void InvalidState_IsRejected_AndChangesNothing()
        {
            var clock = new GameClock(GameClockSettings.Default);
            var bad = new[]
            {
                null,
                new GameClockData { day = 0, minuteOfDay = 10 },
                new GameClockData { day = 1, minuteOfDay = 1440 },
                new GameClockData { day = 1, minuteOfDay = -1 },
                new GameClockData { day = 1, minuteOfDay = double.NaN },
                new GameClockData { day = 1, minuteOfDay = 10, activePlaySeconds = -5 },
                new GameClockData { version = 99, day = 1, minuteOfDay = 10 },
            };
            foreach (GameClockData data in bad)
            {
                Assert.IsFalse(clock.TryRestore(data, out string error));
                Assert.IsNotEmpty(error);
            }
            Assert.AreEqual(1, clock.Day);
            Assert.AreEqual(480, clock.MinuteOfDay);
        }

        [Test]
        public void SaveData_HasNoRealWorldTimestamp()
        {
            foreach (var field in typeof(GameClockData).GetFields())
                Assert.AreNotEqual(typeof(System.DateTime), field.FieldType, field.Name);
        }
    }

    public sealed class DayNightColorTests
    {
        private static readonly Color A = new Color(0.6f, 0.6f, 0.9f);
        private static readonly Color B = new Color(0.8f, 0.7f, 0.9f);
        private static readonly Color Day = Color.white;

        private static List<DayNightKey> Keys() => new List<DayNightKey>
        {
            new DayNightKey(3 * 60, B),
            new DayNightKey(12 * 60, Day),
            new DayNightKey(23 * 60, A),
        };

        private static void AreClose(Color expected, Color actual, float tolerance = 0.002f)
        {
            Assert.AreEqual(expected.r, actual.r, tolerance);
            Assert.AreEqual(expected.g, actual.g, tolerance);
            Assert.AreEqual(expected.b, actual.b, tolerance);
        }

        [Test]
        public void KeyTimes_ReturnKeyColours()
        {
            var keys = Keys();
            AreClose(B, DayNightMath.Evaluate(keys, 3 * 60));
            AreClose(Day, DayNightMath.Evaluate(keys, 12 * 60));
            AreClose(A, DayNightMath.Evaluate(keys, 23 * 60));
        }

        [Test]
        public void BetweenKeys_IsHalfwayAtMidpoint()
        {
            Color mid = DayNightMath.Evaluate(Keys(), 7 * 60 + 30); // halfway 03:00 → 12:00
            AreClose(Color.Lerp(B, Day, 0.5f), mid);
        }

        [Test]
        public void AcrossMidnight_InterpolatesSmoothly()
        {
            var keys = Keys();
            AreClose(Color.Lerp(A, B, 0.5f), DayNightMath.Evaluate(keys, 1 * 60)); // halfway 23:00 → 03:00
            Color before = DayNightMath.Evaluate(keys, 1439.9);
            Color after = DayNightMath.Evaluate(keys, 0.1);
            AreClose(before, after, 0.003f);
            AreClose(DayNightMath.Evaluate(keys, 0), DayNightMath.Evaluate(keys, 1440));
            Color early = DayNightMath.Evaluate(keys, 2 * 60);  // before the first key wraps to the last key
            Assert.Greater(early.r, A.r);
            Assert.Less(early.r, B.r);
        }

        [Test]
        public void Strength_BlendsTowardWhite()
        {
            AreClose(Color.white, DayNightMath.ApplyStrength(A, 0f));
            AreClose(A, DayNightMath.ApplyStrength(A, 1f));
        }

        [Test]
        public void DefaultPalette_NightStaysReadable_AndDayIsUntinted()
        {
            var palette = DayNightPalette.CreateRuntime(DayNightPalette.DefaultKeys());
            for (int minute = 0; minute < 1440; minute += 5)
            {
                Color c = palette.Evaluate(minute);
                Assert.GreaterOrEqual(Mathf.Min(c.r, Mathf.Min(c.g, c.b)), 0.55f, $"too dark at {ClockText.Time24(minute)}");
            }
            AreClose(Color.white, palette.Evaluate(12 * 60));
            Object.DestroyImmediate(palette);
        }

        [Test]
        public void SingleOrEmptyPalette_IsSafe()
        {
            AreClose(Color.white, DayNightMath.Evaluate(new List<DayNightKey>(), 600));
            AreClose(A, DayNightMath.Evaluate(new List<DayNightKey> { new DayNightKey(100, A) }, 900));
        }
    }
}

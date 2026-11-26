using System;

namespace Lifetopia.TimeSystem
{
    /// <summary>
    /// Read-only view of the game clock for gameplay systems (farming, fishing, NPCs, quests, market).
    /// Use <see cref="TotalMinutes"/> to measure durations ("ready 6 game hours after planting"),
    /// and the events instead of polling every frame.
    /// </summary>
    public interface IGameClock
    {
        /// <summary>Day number, starting at 1.</summary>
        int Day { get; }
        /// <summary>Whole minute of the day, 0..1439.</summary>
        int MinuteOfDay { get; }
        int Hour { get; }
        int Minute { get; }
        /// <summary>Whole game minutes since 00:00 of day 1 (monotonic while the game advances).</summary>
        long TotalMinutes { get; }
        /// <summary>Real seconds of active, unpaused play.</summary>
        double ActivePlaySeconds { get; }
        TimeOfDayPeriod Period { get; }

        /// <summary>The whole minute shown on the clock changed (also after a jump or restore).</summary>
        event Action MinuteChanged;
        /// <summary>A new hour started (argument: hour 0..23).</summary>
        event Action<int> HourChanged;
        /// <summary>Raised once per day passed while time advances (argument: new day number).</summary>
        event Action<int> DayChanged;
        event Action<TimeOfDayPeriod> PeriodChanged;
        /// <summary>Time was set directly (debug jump or save restore) instead of advancing.</summary>
        event Action TimeJumped;
    }
}

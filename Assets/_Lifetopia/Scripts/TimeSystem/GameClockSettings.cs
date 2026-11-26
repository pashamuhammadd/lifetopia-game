using System;
using UnityEngine;

namespace Lifetopia.TimeSystem
{
    /// <summary>
    /// Rules of the game clock (plain data, validated). Defaults follow the GDD:
    /// day 1 starts at 08:00 and one game hour lasts 60 real seconds (one day = 24 real minutes).
    /// Minutes are counted from midnight (0..1439).
    /// </summary>
    [Serializable]
    public struct GameClockSettings
    {
        public const int MinutesPerDay = 1440;
        public const int MinutesPerHour = 60;

        [Min(1)] public int startDay;
        [Tooltip("Minute of the day the game starts at (480 = 08:00).")]
        [Range(0, MinutesPerDay - 1)] public int startMinuteOfDay;
        [Tooltip("Real seconds of active play per game hour. 60 = one game day lasts 24 real minutes.")]
        public float realSecondsPerGameHour;
        [Tooltip("Longest real time step applied in one frame, so a long hitch cannot skip game time.")]
        public float maxStepSeconds;

        [Header("Periods (minute of day)")]
        public int morningStartMinute;
        public int daytimeStartMinute;
        public int sunsetStartMinute;
        public int nightStartMinute;

        public static GameClockSettings Default => new GameClockSettings
        {
            startDay = 1,
            startMinuteOfDay = 8 * MinutesPerHour,
            realSecondsPerGameHour = 60f,
            maxStepSeconds = 0.5f,
            morningStartMinute = 5 * MinutesPerHour,
            daytimeStartMinute = 8 * MinutesPerHour,
            sunsetStartMinute = 17 * MinutesPerHour,
            nightStartMinute = 19 * MinutesPerHour,
        };

        /// <summary>Game minutes that pass per real second at normal speed.</summary>
        public double GameMinutesPerRealSecond => MinutesPerHour / (double)realSecondsPerGameHour;

        /// <summary>False (with a reason) if any value is out of range or the periods are not in order.</summary>
        public bool Validate(out string error)
        {
            error = null;
            if (startDay < 1) error = "Start day must be 1 or more.";
            else if (startMinuteOfDay < 0 || startMinuteOfDay >= MinutesPerDay) error = "Start minute must be 0..1439.";
            else if (!IsFinite(realSecondsPerGameHour) || realSecondsPerGameHour < 0.1f || realSecondsPerGameHour > 3600f)
                error = "Real seconds per game hour must be between 0.1 and 3600.";
            else if (!IsFinite(maxStepSeconds) || maxStepSeconds <= 0f || maxStepSeconds > 5f) error = "Max step must be between 0 and 5 seconds.";
            else if (!InDay(morningStartMinute) || !InDay(daytimeStartMinute) || !InDay(sunsetStartMinute) || !InDay(nightStartMinute))
                error = "Period starts must be 0..1439.";
            else if (!(morningStartMinute < daytimeStartMinute && daytimeStartMinute < sunsetStartMinute && sunsetStartMinute < nightStartMinute))
                error = "Periods must be in order: Morning < Daytime < Sunset < Night.";
            return error == null;
        }

        /// <summary>Period at a minute of day. Night wraps around midnight.</summary>
        public TimeOfDayPeriod PeriodAt(int minuteOfDay)
        {
            minuteOfDay = Wrap(minuteOfDay);
            if (minuteOfDay >= nightStartMinute || minuteOfDay < morningStartMinute) return TimeOfDayPeriod.Night;
            if (minuteOfDay < daytimeStartMinute) return TimeOfDayPeriod.Morning;
            if (minuteOfDay < sunsetStartMinute) return TimeOfDayPeriod.Daytime;
            return TimeOfDayPeriod.Sunset;
        }

        public static int Wrap(int minuteOfDay) => ((minuteOfDay % MinutesPerDay) + MinutesPerDay) % MinutesPerDay;

        private static bool InDay(int minute) => minute >= 0 && minute < MinutesPerDay;
        private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}

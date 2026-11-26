using System;

namespace Lifetopia.TimeSystem
{
    /// <summary>
    /// Runtime game clock (plain C#, no Unity scene dependency): day number, time of day and active
    /// play time. It only moves when <see cref="Advance"/> is called, so pausing is simply not calling it.
    /// Events fire at most once per call for minute/hour/period, and once per day crossed.
    /// </summary>
    public sealed class GameClock : IGameClock
    {
        private const int MinutesPerDay = GameClockSettings.MinutesPerDay;

        private GameClockSettings _settings;
        private int _day;
        private double _minuteOfDay;
        private double _activeSeconds;

        public event Action MinuteChanged;
        public event Action<int> HourChanged;
        public event Action<int> DayChanged;
        public event Action<TimeOfDayPeriod> PeriodChanged;
        public event Action TimeJumped;

        /// <summary>Creates a clock at the configured start time. Invalid settings fall back to the defaults.</summary>
        public GameClock(GameClockSettings settings)
        {
            SettingsWereValid = settings.Validate(out string error);
            SettingsError = error;
            _settings = SettingsWereValid ? settings : GameClockSettings.Default;
            _day = _settings.startDay;
            _minuteOfDay = _settings.startMinuteOfDay;
            Period = _settings.PeriodAt(MinuteOfDay);
        }

        public GameClockSettings Settings => _settings;
        public bool SettingsWereValid { get; }
        public string SettingsError { get; }

        public int Day => _day;
        public int MinuteOfDay => (int)_minuteOfDay;
        public int Hour => MinuteOfDay / GameClockSettings.MinutesPerHour;
        public int Minute => MinuteOfDay % GameClockSettings.MinutesPerHour;
        public double ExactMinuteOfDay => _minuteOfDay;
        public long TotalMinutes => (_day - 1) * (long)MinutesPerDay + MinuteOfDay;
        public double ActivePlaySeconds => _activeSeconds;
        public TimeOfDayPeriod Period { get; private set; }

        /// <summary>
        /// Moves time forward by <paramref name="realSeconds"/> of active play (clamped to the max step),
        /// multiplied by <paramref name="speed"/> (1 = normal; development tools may use more).
        /// Zero, negative, NaN or infinite input is ignored.
        /// </summary>
        public void Advance(double realSeconds, double speed = 1.0)
        {
            if (!(realSeconds > 0) || double.IsInfinity(realSeconds)) return;
            if (!(speed > 0) || double.IsInfinity(speed)) return;
            realSeconds = Math.Min(realSeconds, _settings.maxStepSeconds);
            _activeSeconds += realSeconds;
            MoveForward(realSeconds * speed * _settings.GameMinutesPerRealSecond);
        }

        /// <summary>Skips forward by game minutes (development tools). Days crossed raise <see cref="DayChanged"/>.</summary>
        public void AdvanceGameMinutes(double gameMinutes)
        {
            if (!(gameMinutes > 0) || double.IsInfinity(gameMinutes)) return;
            MoveForward(gameMinutes);
        }

        /// <summary>Jumps to a time of day on a given day (development tools). Raises <see cref="TimeJumped"/>, not DayChanged.</summary>
        public bool SetTime(int day, int minuteOfDay)
        {
            if (day < 1 || minuteOfDay < 0 || minuteOfDay >= MinutesPerDay) return false;
            Jump(day, minuteOfDay);
            return true;
        }

        public GameClockData ToData() => new GameClockData
        {
            day = _day,
            minuteOfDay = _minuteOfDay,
            activePlaySeconds = _activeSeconds,
        };

        /// <summary>Restores a saved snapshot. Rejects impossible data and changes nothing in that case.</summary>
        public bool TryRestore(GameClockData data, out string error)
        {
            error = null;
            if (data == null) error = "Missing clock data.";
            else if (data.version != GameClockData.CurrentVersion) error = $"Unsupported clock data version {data.version}.";
            else if (data.day < 1) error = "Day must be 1 or more.";
            else if (double.IsNaN(data.minuteOfDay) || data.minuteOfDay < 0 || data.minuteOfDay >= MinutesPerDay) error = "Minute of day out of range.";
            else if (double.IsNaN(data.activePlaySeconds) || double.IsInfinity(data.activePlaySeconds) || data.activePlaySeconds < 0) error = "Invalid active play time.";
            if (error != null) return false;

            _activeSeconds = data.activePlaySeconds;
            Jump(data.day, data.minuteOfDay);
            return true;
        }

        private void MoveForward(double gameMinutes)
        {
            int oldDay = _day, oldMinute = MinuteOfDay, oldHour = Hour;
            TimeOfDayPeriod oldPeriod = Period;

            double total = _minuteOfDay + gameMinutes;
            long daysPassed = (long)Math.Floor(total / MinutesPerDay);
            _minuteOfDay = total - daysPassed * (double)MinutesPerDay;
            if (_minuteOfDay >= MinutesPerDay) { _minuteOfDay -= MinutesPerDay; daysPassed++; } // floating-point guard
            if (_minuteOfDay < 0) _minuteOfDay = 0;
            _day = (int)Math.Min(int.MaxValue, _day + daysPassed);
            Period = _settings.PeriodAt(MinuteOfDay);

            for (int d = oldDay + 1; d <= _day && d > oldDay; d++) DayChanged?.Invoke(d);
            bool dayChanged = _day != oldDay;
            if (dayChanged || MinuteOfDay != oldMinute) MinuteChanged?.Invoke();
            if (dayChanged || Hour != oldHour) HourChanged?.Invoke(Hour);
            if (Period != oldPeriod) PeriodChanged?.Invoke(Period);
        }

        private void Jump(int day, double minuteOfDay)
        {
            TimeOfDayPeriod oldPeriod = Period;
            _day = day;
            _minuteOfDay = minuteOfDay;
            Period = _settings.PeriodAt(MinuteOfDay);
            TimeJumped?.Invoke();
            MinuteChanged?.Invoke();
            if (Period != oldPeriod) PeriodChanged?.Invoke(Period);
        }
    }
}

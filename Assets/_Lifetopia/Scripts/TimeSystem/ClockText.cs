namespace Lifetopia.TimeSystem
{
    /// <summary>
    /// English clock strings. 24-hour "HH:mm" (e.g. "08:00", "19:30"). Each time string is built once
    /// and cached, so updating a label every game minute does not allocate after the first day.
    /// </summary>
    public static class ClockText
    {
        private static readonly string[] TimeCache = new string[GameClockSettings.MinutesPerDay];

        public static string Time24(int minuteOfDay)
        {
            minuteOfDay = GameClockSettings.Wrap(minuteOfDay);
            string cached = TimeCache[minuteOfDay];
            if (cached != null) return cached;
            int h = minuteOfDay / 60, m = minuteOfDay % 60;
            cached = new string(new[] { (char)('0' + h / 10), (char)('0' + h % 10), ':', (char)('0' + m / 10), (char)('0' + m % 10) });
            TimeCache[minuteOfDay] = cached;
            return cached;
        }

        public static string PeriodName(TimeOfDayPeriod period)
        {
            switch (period)
            {
                case TimeOfDayPeriod.Morning: return "Morning";
                case TimeOfDayPeriod.Daytime: return "Daytime";
                case TimeOfDayPeriod.Sunset: return "Sunset";
                default: return "Night";
            }
        }

        public static string DayLabel(int day) => "Day " + day.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

using System;

namespace Lifetopia.TimeSystem
{
    /// <summary>
    /// Serializable snapshot of the clock for the future local save (JsonUtility-compatible).
    /// Holds only game time and active play time — never real-world timestamps, so nothing
    /// progresses while the game is closed.
    /// </summary>
    [Serializable]
    public sealed class GameClockData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int day = 1;
        /// <summary>Minute of the day including the fraction (0 ≤ value &lt; 1440).</summary>
        public double minuteOfDay;
        /// <summary>Real seconds the clock has been running (paused time excluded).</summary>
        public double activePlaySeconds;
    }
}

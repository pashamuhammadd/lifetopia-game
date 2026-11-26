using System;

namespace Lifetopia.Farming
{
    /// <summary>Growth status of a plot (GDD: Empty, Planted, Watered, Ready).</summary>
    public enum FarmPlotStage
    {
        Empty = 0,
        Planted = 1,
        Watered = 2,
        Ready = 3,
    }

    /// <summary>
    /// Runtime state of one farm plot (plain C#, no Unity scene dependency).
    /// - <see cref="Tilled"/> is a separate soil flag (hoe), so the four GDD growth stages stay unchanged:
    ///   a plot must be tilled before it can be planted.
    /// - Growth uses active play seconds supplied by the caller (<c>IGameClock.ActivePlaySeconds</c>),
    ///   never wall-clock time; there is no per-frame timer. Ready is derived: watered and
    ///   <c>now - wateredAt &gt;= crop.GrowSeconds</c>.
    /// - State changes only through <see cref="FarmActions"/>, which validates first (all-or-nothing).
    /// </summary>
    public sealed class FarmPlot
    {
        public bool Tilled { get; private set; }
        public CropDefinition Crop { get; private set; }
        /// <summary>Active play seconds at the moment the crop was watered (only meaningful when watered).</summary>
        public double WateredAt { get; private set; }
        private bool _watered;

        /// <summary>Raised after any state change.</summary>
        public event Action Changed;

        public bool HasCrop => Crop != null;

        /// <summary>Current stage at active play time <paramref name="now"/>.</summary>
        public FarmPlotStage GetStage(double now)
        {
            if (Crop == null) return FarmPlotStage.Empty;
            if (!_watered) return FarmPlotStage.Planted;
            return now - WateredAt >= Crop.GrowSeconds ? FarmPlotStage.Ready : FarmPlotStage.Watered;
        }

        /// <summary>Seconds of active play the crop has grown (0 before watering, capped at the grow time).</summary>
        public double GetGrowthSeconds(double now)
        {
            if (Crop == null || !_watered) return 0;
            double elapsed = now - WateredAt;
            if (elapsed < 0) elapsed = 0;
            return elapsed > Crop.GrowSeconds ? Crop.GrowSeconds : elapsed;
        }

        /// <summary>0..1 growth progress.</summary>
        public float GetGrowthFraction(double now) =>
            Crop == null ? 0f : (float)(GetGrowthSeconds(now) / Crop.GrowSeconds);

        public double GetSecondsLeft(double now) =>
            Crop == null || !_watered ? 0 : Math.Max(0, Crop.GrowSeconds - (now - WateredAt));

        // ---- mutations (used by FarmActions / loading only) ----

        internal void SetTilled(bool tilled)
        {
            if (Tilled == tilled) return;
            Tilled = tilled;
            Changed?.Invoke();
        }

        internal void SetPlanted(CropDefinition crop)
        {
            Crop = crop;
            _watered = false;
            WateredAt = 0;
            Changed?.Invoke();
        }

        internal void SetWatered(double now)
        {
            _watered = true;
            WateredAt = now;
            Changed?.Invoke();
        }

        internal void Clear(bool keepTilled)
        {
            Crop = null;
            _watered = false;
            WateredAt = 0;
            Tilled = keepTilled;
            Changed?.Invoke();
        }

        /// <summary>Loads a validated state in one step.</summary>
        internal void Load(bool tilled, CropDefinition crop, bool watered, double wateredAt)
        {
            Tilled = tilled;
            Crop = crop;
            _watered = crop != null && watered;
            WateredAt = _watered ? wateredAt : 0;
            Changed?.Invoke();
        }

        /// <summary>Moves the watering time back (development tools: speed up growth). No effect if not watered.</summary>
        internal void ShiftGrowth(double seconds)
        {
            if (Crop == null || !_watered || seconds <= 0) return;
            WateredAt -= seconds;
            Changed?.Invoke();
        }
    }
}

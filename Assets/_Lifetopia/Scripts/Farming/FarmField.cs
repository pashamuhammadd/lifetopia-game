using System;
using System.Collections.Generic;

namespace Lifetopia.Farming
{
    /// <summary>Serializable state of one plot for local saves (no wall-clock values).</summary>
    [Serializable]
    public sealed class FarmPlotData
    {
        public bool tilled;
        /// <summary>Empty string = no crop.</summary>
        public string cropId = "";
        public bool watered;
        /// <summary>Active play seconds grown since watering (relative, so it survives clock restores).</summary>
        public double growthSeconds;
    }

    /// <summary>Serializable state of the whole farm (local save later; not persisted yet).</summary>
    [Serializable]
    public sealed class FarmFieldData
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public List<FarmPlotData> plots = new List<FarmPlotData>();
    }

    /// <summary>
    /// The set of farm plots (GDD: 6 starter plots). Plain C#; plots are independent of each other.
    /// </summary>
    public sealed class FarmField
    {
        public const int DefaultPlotCount = 6;
        private readonly FarmPlot[] _plots;

        public FarmField(int plotCount = DefaultPlotCount)
        {
            if (plotCount <= 0) throw new ArgumentOutOfRangeException(nameof(plotCount));
            _plots = new FarmPlot[plotCount];
            for (int i = 0; i < plotCount; i++) _plots[i] = new FarmPlot();
        }

        public int Count => _plots.Length;
        public bool IsValidIndex(int index) => index >= 0 && index < _plots.Length;
        public FarmPlot Get(int index) => IsValidIndex(index) ? _plots[index] : null;

        public FarmFieldData ToData(double now)
        {
            var data = new FarmFieldData();
            foreach (FarmPlot plot in _plots)
            {
                FarmPlotStage stage = plot.GetStage(now);
                data.plots.Add(new FarmPlotData
                {
                    tilled = plot.Tilled,
                    cropId = plot.Crop != null ? plot.Crop.CropId : "",
                    watered = stage == FarmPlotStage.Watered || stage == FarmPlotStage.Ready,
                    growthSeconds = plot.GetGrowthSeconds(now),
                });
            }
            return data;
        }

        /// <summary>
        /// Restores all plots from saved data at active play time <paramref name="now"/>. Rejects the
        /// whole save (nothing changes) on a wrong version, plot count, unknown crop or bad numbers.
        /// </summary>
        public bool TryLoad(FarmFieldData data, FarmingConfig config, double now, out string error)
        {
            error = null;
            if (data == null || config == null) { error = "Missing data."; return false; }
            if (data.version != FarmFieldData.CurrentVersion) { error = $"Unsupported farm save version {data.version}."; return false; }
            if (data.plots == null || data.plots.Count != _plots.Length) { error = "Invalid plot count."; return false; }

            var crops = new CropDefinition[_plots.Length];
            for (int i = 0; i < _plots.Length; i++)
            {
                FarmPlotData p = data.plots[i];
                if (p == null) { error = $"Plot {i}: missing."; return false; }
                if (double.IsNaN(p.growthSeconds) || double.IsInfinity(p.growthSeconds) || p.growthSeconds < 0)
                { error = $"Plot {i}: invalid growth."; return false; }
                if (string.IsNullOrEmpty(p.cropId))
                {
                    if (p.watered || p.growthSeconds > 0) { error = $"Plot {i}: growth without crop."; return false; }
                    continue;
                }
                CropDefinition crop = config.CropById(p.cropId);
                if (crop == null) { error = $"Plot {i}: unknown crop '{p.cropId}'."; return false; }
                if (!p.tilled) { error = $"Plot {i}: crop on untilled soil."; return false; }
                if (!p.watered && p.growthSeconds > 0) { error = $"Plot {i}: growth before watering."; return false; }
                crops[i] = crop;
            }
            for (int i = 0; i < _plots.Length; i++)
            {
                FarmPlotData p = data.plots[i];
                double growth = Math.Min(p.growthSeconds, crops[i] != null ? crops[i].GrowSeconds : 0);
                _plots[i].Load(p.tilled, crops[i], p.watered, now - growth);
            }
            return true;
        }
    }
}

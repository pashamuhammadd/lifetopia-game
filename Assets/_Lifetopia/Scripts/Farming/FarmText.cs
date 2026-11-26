using System.Globalization;

namespace Lifetopia.Farming
{
    /// <summary>Player-facing English texts for farming (prompts and messages).</summary>
    public static class FarmText
    {
        public static string Prompt(FarmAction action, FarmPlot plot, FarmPlotStage stage, double secondsLeft)
        {
            switch (action)
            {
                case FarmAction.Till: return "Till soil";
                case FarmAction.Plant: return "Plant seeds";
                case FarmAction.Water: return "Water crop";
                case FarmAction.Harvest: return "Harvest " + (plot != null && plot.Crop != null ? plot.Crop.DisplayName : "crop");
                case FarmAction.Wait: return "Growing - " + FormatSeconds(secondsLeft) + " left";
            }
            switch (stage)
            {
                case FarmPlotStage.Planted: return "Needs water: select Watering Can";
                case FarmPlotStage.Empty: return plot != null && plot.Tilled ? "Ready to plant: select seeds" : "Untilled soil: select Hoe";
                default: return "Farm plot";
            }
        }

        /// <summary>Message after an action; empty when nothing needs to be said.</summary>
        public static string Message(FarmResult result, FarmPlot plot)
        {
            string crop = plot != null && plot.Crop != null ? plot.Crop.DisplayName : "The crop";
            switch (result)
            {
                case FarmResult.NeedHoe: return "Select the Hoe to till this soil.";
                case FarmResult.AlreadyTilled: return "This soil is already tilled.";
                case FarmResult.NotTilled: return "Till the soil with the Hoe first.";
                case FarmResult.NeedSeed: return "Select seeds in the hotbar to plant.";
                case FarmResult.UnknownSeed: return "That can't be planted.";
                case FarmResult.PlotOccupied: return "Something is already growing here.";
                case FarmResult.NotEnoughSeeds: return "You have no seeds left.";
                case FarmResult.NeedWateringCan: return "Select the Watering Can to water.";
                case FarmResult.NothingToWater: return "Plant seeds first.";
                case FarmResult.AlreadyWatered: return "Already watered.";
                case FarmResult.NotReady: return crop + " is still growing.";
                case FarmResult.InventoryFull: return "Inventory full. " + crop + " stays on the plot.";
                case FarmResult.InvalidCrop:
                case FarmResult.InvalidPlot: return "That didn't work.";
                default: return "";
            }
        }

        /// <summary>"1:05" style for 65 s; "0:00" minimum. Rounds up so 0.2 s shows as 0:01.</summary>
        public static string FormatSeconds(double seconds)
        {
            int s = seconds <= 0 ? 0 : (int)System.Math.Ceiling(seconds);
            return (s / 60).ToString(CultureInfo.InvariantCulture) + ":" + (s % 60).ToString("00", CultureInfo.InvariantCulture);
        }
    }
}

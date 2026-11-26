using Lifetopia.InventorySystem;
using Lifetopia.Items;

namespace Lifetopia.Farming
{
    /// <summary>What pressing Interact on a plot would do with the currently selected item.</summary>
    public enum FarmAction
    {
        None = 0,
        Till,
        Plant,
        Water,
        Harvest,
        /// <summary>Watered crop still growing: nothing to do yet.</summary>
        Wait,
    }

    /// <summary>Outcome of a farm action. Anything but Success means nothing changed.</summary>
    public enum FarmResult
    {
        Success = 0,
        InvalidPlot,
        NeedHoe,
        AlreadyTilled,
        NotTilled,
        NeedSeed,
        UnknownSeed,
        PlotOccupied,
        NotEnoughSeeds,
        NeedWateringCan,
        NothingToWater,
        AlreadyWatered,
        NotReady,
        InventoryFull,
        InvalidCrop,
    }

    /// <summary>
    /// Farming rules (plain C#, unit-tested). Every method validates everything first and changes the
    /// plot/inventory only when the whole action can succeed:
    /// - Till: hoe selected, plot not tilled and empty. Tools are never used up.
    /// - Plant: seed selected, plot tilled and empty, 1 seed removed from the inventory, then planted.
    /// - Water: watering can selected, crop planted and not yet watered (water once).
    /// - Harvest: crop ready and the harvest fits in the inventory; otherwise the crop stays Ready.
    ///   Any selected item may harvest. After harvest the plot is empty (tilled or not, per config).
    /// </summary>
    public static class FarmActions
    {
        /// <summary>The action Interact would perform; also used for the prompt text.</summary>
        public static FarmAction Resolve(FarmPlot plot, ItemDefinition selected, FarmingConfig config, double now)
        {
            if (plot == null || config == null) return FarmAction.None;
            switch (plot.GetStage(now))
            {
                case FarmPlotStage.Ready: return FarmAction.Harvest;
                case FarmPlotStage.Watered: return FarmAction.Wait;
                case FarmPlotStage.Planted: return selected != null && selected == config.WateringCanItem ? FarmAction.Water : FarmAction.None;
            }
            if (!plot.Tilled) return selected != null && selected == config.HoeItem ? FarmAction.Till : FarmAction.None;
            return config.CropForSeed(selected) != null ? FarmAction.Plant : FarmAction.None;
        }

        public static FarmResult Till(FarmPlot plot, ItemDefinition selected, FarmingConfig config)
        {
            if (plot == null || config == null) return FarmResult.InvalidPlot;
            if (selected == null || selected != config.HoeItem) return FarmResult.NeedHoe;
            if (plot.HasCrop) return FarmResult.PlotOccupied;
            if (plot.Tilled) return FarmResult.AlreadyTilled;
            plot.SetTilled(true);
            return FarmResult.Success;
        }

        public static FarmResult Plant(FarmPlot plot, ItemDefinition selected, FarmingConfig config, Inventory inventory)
        {
            if (plot == null || config == null || inventory == null) return FarmResult.InvalidPlot;
            if (selected == null) return FarmResult.NeedSeed;
            CropDefinition crop = config.CropForSeed(selected);
            if (crop == null) return FarmResult.UnknownSeed;
            if (!crop.IsValid) return FarmResult.InvalidCrop;
            if (plot.HasCrop) return FarmResult.PlotOccupied;
            if (!plot.Tilled) return FarmResult.NotTilled;
            if (!inventory.HasItem(crop.SeedItem)) return FarmResult.NotEnoughSeeds;
            // Consume first; plant only when the seed was really removed (never plant for free).
            if (!inventory.RemoveItem(crop.SeedItem, 1).Succeeded()) return FarmResult.NotEnoughSeeds;
            plot.SetPlanted(crop);
            return FarmResult.Success;
        }

        public static FarmResult Water(FarmPlot plot, ItemDefinition selected, FarmingConfig config, double now)
        {
            if (plot == null || config == null) return FarmResult.InvalidPlot;
            if (selected == null || selected != config.WateringCanItem) return FarmResult.NeedWateringCan;
            FarmPlotStage stage = plot.GetStage(now);
            if (stage == FarmPlotStage.Empty) return FarmResult.NothingToWater;
            if (stage != FarmPlotStage.Planted) return FarmResult.AlreadyWatered;
            plot.SetWatered(now);
            return FarmResult.Success;
        }

        public static FarmResult Harvest(FarmPlot plot, FarmingConfig config, Inventory inventory, double now)
        {
            if (plot == null || config == null || inventory == null) return FarmResult.InvalidPlot;
            if (plot.GetStage(now) != FarmPlotStage.Ready) return FarmResult.NotReady;
            CropDefinition crop = plot.Crop;
            if (crop == null || !crop.IsValid) return FarmResult.InvalidCrop;
            // Check space first: a full bag leaves the crop Ready on the plot (nothing lost).
            if (!inventory.CanAddItem(crop.HarvestItem, crop.HarvestQuantity)) return FarmResult.InventoryFull;
            if (!inventory.AddItem(crop.HarvestItem, crop.HarvestQuantity).Succeeded()) return FarmResult.InventoryFull;
            plot.Clear(config.KeepTilledAfterHarvest);
            return FarmResult.Success;
        }

        /// <summary>Performs whatever <see cref="Resolve"/> returns; when nothing applies, explains why.</summary>
        public static FarmResult Perform(FarmPlot plot, ItemDefinition selected, FarmingConfig config, Inventory inventory, double now, out FarmAction action)
        {
            action = Resolve(plot, selected, config, now);
            switch (action)
            {
                case FarmAction.Till: return Till(plot, selected, config);
                case FarmAction.Plant: return Plant(plot, selected, config, inventory);
                case FarmAction.Water: return Water(plot, selected, config, now);
                case FarmAction.Harvest: return Harvest(plot, config, inventory, now);
                case FarmAction.Wait: return FarmResult.NotReady;
            }
            if (plot == null || config == null) return FarmResult.InvalidPlot;
            switch (plot.GetStage(now))
            {
                case FarmPlotStage.Planted: return FarmResult.NeedWateringCan;
                case FarmPlotStage.Empty: return plot.Tilled ? FarmResult.NeedSeed : FarmResult.NeedHoe;
                default: return FarmResult.NotReady;
            }
        }
    }
}

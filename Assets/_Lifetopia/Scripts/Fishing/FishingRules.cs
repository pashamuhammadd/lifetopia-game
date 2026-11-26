using Lifetopia.InventorySystem;
using Lifetopia.Items;

namespace Lifetopia.Fishing
{
    public enum FishingStartResult
    {
        Ok = 0,
        NeedRod,
        InventoryFull,
        Busy,
        InvalidConfig,
    }

    public enum FishingAwardResult
    {
        Awarded = 0,
        NotCaught,
        AlreadyAwarded,
        InventoryFull,
        InvalidConfig,
    }

    /// <summary>
    /// Start checks and the single reward per attempt (plain C#, unit-tested).
    /// - Start needs the rod selected AND in the inventory (count &gt; 0) and room for the catch.
    /// - Award is atomic: it re-checks capacity, adds exactly the configured quantity, and remembers the
    ///   attempt ID so the same attempt can never pay out twice. The rod is never consumed; a failed
    ///   attempt costs nothing.
    /// </summary>
    public sealed class FishingRewards
    {
        public int LastAwardedAttempt { get; private set; }

        public static FishingStartResult CanStart(FishingConfig config, ItemDefinition selected, Inventory inventory, FishingSession session)
        {
            if (config == null || config.RodItem == null || config.FishItem == null || inventory == null) return FishingStartResult.InvalidConfig;
            if (session != null && session.IsActive) return FishingStartResult.Busy;
            if (selected == null || selected != config.RodItem || !inventory.HasItem(config.RodItem)) return FishingStartResult.NeedRod;
            if (!inventory.CanAddItem(config.FishItem, config.CatchQuantity)) return FishingStartResult.InventoryFull;
            return FishingStartResult.Ok;
        }

        public FishingAwardResult TryAward(FishingSession session, FishingConfig config, Inventory inventory)
        {
            if (session == null || config == null || config.FishItem == null || inventory == null) return FishingAwardResult.InvalidConfig;
            if (session.State != FishingState.Success || session.Outcome != FishingOutcome.Caught) return FishingAwardResult.NotCaught;
            if (session.AttemptId <= LastAwardedAttempt) return FishingAwardResult.AlreadyAwarded;
            if (!inventory.CanAddItem(config.FishItem, config.CatchQuantity)) return FishingAwardResult.InventoryFull;
            if (!inventory.AddItem(config.FishItem, config.CatchQuantity).Succeeded()) return FishingAwardResult.InventoryFull;
            LastAwardedAttempt = session.AttemptId;
            return FishingAwardResult.Awarded;
        }
    }

    /// <summary>Player-facing English texts for fishing.</summary>
    public static class FishingText
    {
        public const string PromptFish = "Fish";
        public const string PromptNeedRod = "Select Fishing Rod to fish";

        public static string Status(FishingState state, FishingOutcome outcome, string fishName)
        {
            switch (state)
            {
                case FishingState.Casting: return "Cast";
                case FishingState.WaitingForBite: return "Wait for a bite";
                case FishingState.Timing: return "Reel in!";
                case FishingState.Success: return "Caught a " + (string.IsNullOrEmpty(fishName) ? "fish" : fishName) + "!";
                case FishingState.Cancelled: return "Cancelled";
                case FishingState.Failure:
                    switch (outcome)
                    {
                        case FishingOutcome.TooEarly: return "Too early - the fish swam away";
                        case FishingOutcome.TooLate: return "Too late - the fish got away";
                        default: return "Missed";
                    }
                default: return "";
            }
        }

        public static string StartMessage(FishingStartResult result, ItemDefinition fish)
        {
            switch (result)
            {
                case FishingStartResult.NeedRod: return "Select the Fishing Rod in the hotbar to fish.";
                case FishingStartResult.InventoryFull: return "Inventory full. Make room for " + (fish != null ? fish.DisplayName : "the fish") + " first.";
                case FishingStartResult.InvalidConfig: return "That didn't work.";
                default: return "";
            }
        }
    }
}

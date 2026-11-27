namespace Lifetopia.Housing
{
    /// <summary>All player-facing housing text (English, ASCII only until a licensed font is bundled).</summary>
    public static class HousingText
    {
        public const string EnterPrompt = "Enter Home";
        public const string DecorateButton = "Decorate";
        public const string ExitButton = "Exit";
        public const string DoneButton = "Done";
        public const string Title = "Decorating";
        public const string ListTitle = "Your furniture";
        public const string EmptyList = "No furniture in your bag. Buy some at Rio's Market.";
        public const string Place = "Place";
        public const string Move = "Move";
        public const string Rotate = "Rotate";
        public const string Store = "Store";
        public const string Cancel = "Cancel";

        public static string Hint(DecorateState state, bool touch) => state switch
        {
            DecorateState.Placing => touch ? "Tap the floor to move it, then Place." : "Click the floor or use WASD, then Place (E).",
            DecorateState.Moving => touch ? "Tap the floor to move it, then Place." : "Click the floor or use WASD, then Place (E).",
            DecorateState.Selected => "Move, Rotate or Store this item.",
            _ => touch ? "Tap furniture to select it, or pick an item below." : "Click furniture to select it, or pick an item.",
        };

        public static string Describe(PlacementResult r) => r switch
        {
            PlacementResult.Ok => "Looks good!",
            PlacementResult.OutOfBounds => "It does not fit inside the room.",
            PlacementResult.Overlap => "Something is already there.",
            PlacementResult.ReservedCell => "Keep the doorway clear.",
            PlacementResult.PlayerInTheWay => "You are standing there.",
            PlacementResult.BlocksExit => "That would block the way to the door.",
            PlacementResult.UnsupportedOrientation => "This item cannot be rotated yet.",
            PlacementResult.NotInInventory => "That item is not in your bag.",
            PlacementResult.InventoryFull => "Your bag is full. Make room first.",
            PlacementResult.NotFound => "Nothing selected.",
            PlacementResult.Busy => "Please wait.",
            _ => "That item cannot be placed.",
        };

        public static string Placed(string name) => $"Placed {name}.";
        public static string Moved(string name) => $"Moved {name}.";
        public static string Stored(string name) => $"{name} went back to your bag.";
    }
}

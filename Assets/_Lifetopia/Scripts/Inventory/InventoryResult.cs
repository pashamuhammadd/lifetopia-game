namespace Lifetopia.InventorySystem
{
    /// <summary>Outcome of an inventory, hotbar or wallet operation. Anything but Success means nothing changed.</summary>
    public enum InventoryResult
    {
        Success = 0,
        InvalidItem,
        InvalidQuantity,
        InventoryFull,
        NotEnoughItems,
        InvalidSlot,
        SlotEmpty,
        SameSlot,
        TargetOccupied,
        DifferentItems,
        NotStackable,
        StackFull,
        ItemNotInInventory,
        NotEnoughCoins,
        CoinLimitReached,
    }

    public static class InventoryResultExtensions
    {
        public static bool Succeeded(this InventoryResult result) => result == InventoryResult.Success;
    }
}

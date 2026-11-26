using Lifetopia.Items;

namespace Lifetopia.InventorySystem
{
    /// <summary>Player-facing English text for failed operations.</summary>
    public static class InventoryMessages
    {
        public static string Describe(InventoryResult result, ItemDefinition item = null)
        {
            string name = item != null ? item.DisplayName : "That item";
            switch (result)
            {
                case InventoryResult.Success: return "";
                case InventoryResult.InventoryFull: return $"Your bag is full. {name} was not added.";
                case InventoryResult.NotEnoughItems: return $"You don't have enough {name}.";
                case InventoryResult.StackFull: return "That stack is already full.";
                case InventoryResult.NotStackable: return $"{name} can't be stacked.";
                case InventoryResult.ItemNotInInventory: return $"{name} is not in your bag.";
                case InventoryResult.NotEnoughCoins: return "Not enough coins.";
                case InventoryResult.CoinLimitReached: return "You can't carry more coins.";
                default: return "That didn't work.";
            }
        }
    }
}

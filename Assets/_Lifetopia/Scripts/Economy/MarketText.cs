using System.Globalization;
using Lifetopia.Items;

namespace Lifetopia.Economy
{
    /// <summary>English player-facing text for the market (game UI language is English).</summary>
    public static class MarketText
    {
        public const string BuyTab = "Buy";
        public const string SellTab = "Sell";
        public const string PromptTalk = "Shop at Rio's Market";

        public static string Coins(long amount) => amount.ToString("N0", CultureInfo.InvariantCulture);

        public static string ActionLabel(TradeMode mode, int quantity) =>
            (mode == TradeMode.Buy ? "Purchase" : "Sell") + " x" + quantity.ToString(CultureInfo.InvariantCulture);

        public static string Success(TradeRecord r)
        {
            string name = r.Item != null ? r.Item.DisplayName : "items";
            return r.Mode == TradeMode.Buy
                ? $"Bought {r.Quantity} {name} for {Coins(r.Coins)} coins."
                : $"Sold {r.Quantity} {name} for {Coins(r.Coins)} coins.";
        }

        /// <summary>Why a trade cannot happen (shown as text, never by colour alone). Empty for Success.</summary>
        public static string Problem(TradeResult result, ItemDefinition item)
        {
            string name = item != null ? item.DisplayName : "this item";
            switch (result)
            {
                case TradeResult.Success: return "";
                case TradeResult.NotEnoughCoins: return "Not enough coins.";
                case TradeResult.InventoryFull: return $"Inventory full. Make room for {name} first.";
                case TradeResult.NotEnoughItems: return $"You don't have enough {name}.";
                case TradeResult.CoinLimitReached: return "Coin limit reached.";
                case TradeResult.InvalidQuantity: return "Choose a valid quantity.";
                case TradeResult.NotInCatalog: return $"Rio doesn't trade {name}.";
                case TradeResult.InvalidItem: return "Choose an item first.";
                case TradeResult.Busy: return "Please wait a moment.";
                default: return "That trade is not possible.";
            }
        }
    }
}

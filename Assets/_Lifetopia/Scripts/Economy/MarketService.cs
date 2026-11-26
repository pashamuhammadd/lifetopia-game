using System;
using Lifetopia.InventorySystem;
using Lifetopia.Items;

namespace Lifetopia.Economy
{
    public enum TradeMode { Buy = 0, Sell = 1 }

    /// <summary>Outcome of a quote or trade. Anything but Success means coins and items did not change.</summary>
    public enum TradeResult
    {
        Success = 0,
        InvalidItem,
        NotInCatalog,
        InvalidQuantity,
        NotEnoughCoins,
        InventoryFull,
        NotEnoughItems,
        CoinLimitReached,
        Busy,
    }

    /// <summary>Validated price check for one trade (what the UI shows before the player confirms).</summary>
    public readonly struct TradeQuote
    {
        public readonly TradeResult Result;
        public readonly TradeMode Mode;
        public readonly ItemDefinition Item;
        public readonly int Quantity;
        public readonly int UnitPrice;
        /// <summary>Coins paid (Buy) or received (Sell). Computed in 64-bit, never overflows.</summary>
        public readonly long Total;
        /// <summary>Largest quantity that would succeed right now (0 = none).</summary>
        public readonly int MaxQuantity;

        public TradeQuote(TradeResult result, TradeMode mode, ItemDefinition item, int quantity, int unitPrice, long total, int maxQuantity)
        {
            Result = result; Mode = mode; Item = item; Quantity = quantity; UnitPrice = unitPrice; Total = total; MaxQuantity = maxQuantity;
        }

        public bool Ok => Result == TradeResult.Success;
    }

    /// <summary>A completed trade (for the UI toast and tests).</summary>
    public readonly struct TradeRecord
    {
        public readonly TradeMode Mode;
        public readonly ItemDefinition Item;
        public readonly int Quantity;
        public readonly int Coins;

        public TradeRecord(TradeMode mode, ItemDefinition item, int quantity, int coins)
        {
            Mode = mode; Item = item; Quantity = quantity; Coins = coins;
        }
    }

    /// <summary>
    /// Buys and sells against the player's EXISTING <see cref="Wallet"/> and <see cref="Inventory"/> (no
    /// parallel currency or bag). Prices come only from <see cref="MarketConfig"/> / item assets, never from UI.
    /// Every trade is atomic: it is fully validated first (item listed, positive quantity within the
    /// per-trade limit, coins, free space incl. partial stacks, owned count, coin cap, 64-bit totals), and if a
    /// step still fails the earlier step is rolled back, so coins and items are either both changed or untouched.
    /// Re-entrant calls (e.g. from a change event during a trade) are refused with <see cref="TradeResult.Busy"/>.
    /// The hotbar is not touched directly: it follows inventory events, so a 0-count seed reference becomes
    /// usable again after buying (M3.3 behaviour).
    /// </summary>
    public sealed class MarketService
    {
        private readonly MarketConfig _config;
        private readonly Wallet _wallet;
        private readonly Inventory _inventory;
        private bool _busy;

        /// <summary>Raised once after each successful trade.</summary>
        public event Action<TradeRecord> Traded;

        /// <summary>Number of successful trades (tests: proves one action = one trade).</summary>
        public int TradeCount { get; private set; }

        public MarketConfig Config => _config;

        public MarketService(MarketConfig config, PlayerProfile profile)
        {
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            _wallet = profile.Wallet;
            _inventory = profile.Inventory;
        }

        public int Owned(ItemDefinition item) => _inventory.GetItemCount(item);

        public int Coins => _wallet.Coins;

        public TradeQuote Quote(TradeMode mode, ItemDefinition item, int quantity)
        {
            if (item == null) return Fail(TradeResult.InvalidItem, mode, null, quantity, 0);
            int unit = _config.UnitPrice(mode, item);
            if (unit <= 0) return Fail(TradeResult.NotInCatalog, mode, item, quantity, 0);

            int max = MaxQuantity(mode, item, unit);
            if (quantity < 1 || quantity > _config.MaxQuantityPerTrade)
                return new TradeQuote(TradeResult.InvalidQuantity, mode, item, quantity, unit, 0, max);

            long total = (long)unit * quantity;
            if (total > Wallet.MaxCoins) return new TradeQuote(TradeResult.InvalidQuantity, mode, item, quantity, unit, total, max);

            TradeResult result;
            if (mode == TradeMode.Buy)
            {
                if (_wallet.Coins < total) result = TradeResult.NotEnoughCoins;
                else if (!_inventory.CanAddItem(item, quantity)) result = TradeResult.InventoryFull;
                else result = TradeResult.Success;
            }
            else
            {
                if (_inventory.GetItemCount(item) < quantity) result = TradeResult.NotEnoughItems;
                else if ((long)_wallet.Coins + total > Wallet.MaxCoins) result = TradeResult.CoinLimitReached;
                else result = TradeResult.Success;
            }
            return new TradeQuote(result, mode, item, quantity, unit, total, max);
        }

        /// <summary>Largest quantity that would succeed now (0 when none).</summary>
        public int MaxQuantity(TradeMode mode, ItemDefinition item)
        {
            int unit = _config.UnitPrice(mode, item);
            return unit > 0 ? MaxQuantity(mode, item, unit) : 0;
        }

        public TradeResult Buy(ItemDefinition item, int quantity) => Execute(TradeMode.Buy, item, quantity);

        public TradeResult Sell(ItemDefinition item, int quantity) => Execute(TradeMode.Sell, item, quantity);

        public TradeResult Execute(TradeMode mode, ItemDefinition item, int quantity)
        {
            if (_busy) return TradeResult.Busy;
            _busy = true;
            try
            {
                TradeQuote q = Quote(mode, item, quantity);
                if (!q.Ok) return q.Result;
                int coins = (int)q.Total; // ≤ Wallet.MaxCoins, checked by Quote

                if (mode == TradeMode.Buy)
                {
                    if (!_wallet.Spend(coins).Succeeded()) return TradeResult.NotEnoughCoins;
                    if (!_inventory.AddItem(item, quantity).Succeeded())
                    {
                        _wallet.Add(coins); // roll back: nothing changed overall
                        return TradeResult.InventoryFull;
                    }
                }
                else
                {
                    if (!_inventory.RemoveItem(item, quantity).Succeeded()) return TradeResult.NotEnoughItems;
                    if (!_wallet.Add(coins).Succeeded())
                    {
                        _inventory.AddItem(item, quantity); // roll back
                        return TradeResult.CoinLimitReached;
                    }
                }

                TradeCount++;
                Traded?.Invoke(new TradeRecord(mode, item, quantity, coins));
                return TradeResult.Success;
            }
            finally
            {
                _busy = false;
            }
        }

        private int MaxQuantity(TradeMode mode, ItemDefinition item, int unit)
        {
            long max = _config.MaxQuantityPerTrade;
            if (mode == TradeMode.Buy)
            {
                max = Math.Min(max, _wallet.Coins / unit);
                max = Math.Min(max, _inventory.GetFreeCapacity(item));
            }
            else
            {
                max = Math.Min(max, _inventory.GetItemCount(item));
                max = Math.Min(max, (Wallet.MaxCoins - (long)_wallet.Coins) / unit);
            }
            return (int)Math.Max(0, max);
        }

        private static TradeQuote Fail(TradeResult r, TradeMode mode, ItemDefinition item, int quantity, int unit) =>
            new TradeQuote(r, mode, item, quantity, unit, 0, 0);
    }
}

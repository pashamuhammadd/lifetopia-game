using System;

namespace Lifetopia.InventorySystem
{
    /// <summary>
    /// Local, offline in-game coins (not real money, not online). Never negative, capped at
    /// <see cref="MaxCoins"/>; failed operations change nothing.
    /// </summary>
    public sealed class Wallet
    {
        public const int MaxCoins = 9_999_999;

        public int Coins { get; private set; }

        /// <summary>Raised with the new balance after a successful change.</summary>
        public event Action<int> Changed;

        public bool CanAfford(int amount) => amount >= 0 && Coins >= amount;

        public InventoryResult Add(int amount)
        {
            if (amount <= 0) return InventoryResult.InvalidQuantity;
            if ((long)Coins + amount > MaxCoins) return InventoryResult.CoinLimitReached;
            Coins += amount;
            Changed?.Invoke(Coins);
            return InventoryResult.Success;
        }

        public InventoryResult Spend(int amount)
        {
            if (amount <= 0) return InventoryResult.InvalidQuantity;
            if (Coins < amount) return InventoryResult.NotEnoughCoins;
            Coins -= amount;
            Changed?.Invoke(Coins);
            return InventoryResult.Success;
        }

        internal void Load(int coins)
        {
            Coins = Math.Max(0, Math.Min(coins, MaxCoins));
            Changed?.Invoke(Coins);
        }
    }
}

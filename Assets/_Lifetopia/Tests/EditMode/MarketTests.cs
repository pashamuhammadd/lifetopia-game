using System.Collections.Generic;
using Lifetopia.Economy;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using Lifetopia.Npc;
using Lifetopia.Player;
using NUnit.Framework;
using UnityEngine;

namespace Lifetopia.Tests
{
    /// <summary>M3.5 market: prices, atomic buy/sell, failure cases, hotbar restock and NPC facing.</summary>
    public class MarketTests
    {
        private ItemDefinition _hoe, _can, _rod, _seed, _fruit, _fish, _wood;
        private MarketConfig _config;
        private PlayerProfile _profile;
        private MarketService _market;

        [SetUp]
        public void SetUp()
        {
            _hoe = ItemDefinition.CreateRuntime("tool_hoe", "Hoe", ItemCategory.Tool, false);
            _can = ItemDefinition.CreateRuntime("tool_watering_can", "Watering Can", ItemCategory.Tool, false);
            _rod = ItemDefinition.CreateRuntime("tool_fishing_rod", "Fishing Rod", ItemCategory.Tool, false);
            _seed = ItemDefinition.CreateRuntime("seed_tropical", "Tropical Seeds", ItemCategory.Seed, true, 99, 5, 10);
            _fruit = ItemDefinition.CreateRuntime("crop_tropical_fruit", "Tropical Fruit", ItemCategory.Crop, true, 99, 18);
            _fish = ItemDefinition.CreateRuntime("fish_reef", "Reef Fish", ItemCategory.Fish, true, 99, 12);
            _wood = ItemDefinition.CreateRuntime("material_driftwood", "Driftwood", ItemCategory.Material, true, 99, 2);
            _config = MarketConfig.CreateRuntime(new[] { _seed }, new[] { _fruit, _fish });
            _profile = NewStarterProfile();
            _market = new MarketService(_config, _profile);
        }

        [TearDown]
        public void TearDown()
        {
            _profile.Dispose();
            foreach (Object o in new Object[] { _hoe, _can, _rod, _seed, _fruit, _fish, _wood, _config }) Object.DestroyImmediate(o);
        }

        private PlayerProfile NewStarterProfile()
        {
            var p = new PlayerProfile();
            var kit = StarterProfile.CreateRuntime(100,
                new StarterProfile.StarterItem { item = _hoe, quantity = 1, hotbarSlot = 0 },
                new StarterProfile.StarterItem { item = _can, quantity = 1, hotbarSlot = 1 },
                new StarterProfile.StarterItem { item = _rod, quantity = 1, hotbarSlot = 2 },
                new StarterProfile.StarterItem { item = _seed, quantity = 3, hotbarSlot = 3 });
            Assert.That(p.GrantStarterKit(kit), Is.True);
            Object.DestroyImmediate(kit);
            return p;
        }

        private (int coins, string slots) Snapshot()
        {
            var s = new System.Text.StringBuilder();
            for (int i = 0; i < _profile.Inventory.SlotCount; i++)
            {
                InventorySlot slot = _profile.Inventory.GetSlot(i);
                s.Append(slot.IsEmpty ? "-" : slot.Item.ItemId + "x" + slot.Quantity).Append('|');
            }
            return (_profile.Wallet.Coins, s.ToString());
        }

        private void FillBagWith(ItemDefinition item)
        {
            int free = _profile.Inventory.GetFreeCapacity(item);
            if (free > 0) Assert.That(_profile.Inventory.AddItem(item, free).Succeeded());
        }

        // ---------- prices and starting state ----------

        [Test]
        public void Prices_AreSeed10_Fruit18_Fish12_FromItemData()
        {
            Assert.That(_config.UnitPrice(TradeMode.Buy, _seed), Is.EqualTo(10));
            Assert.That(_config.UnitPrice(TradeMode.Sell, _fruit), Is.EqualTo(18));
            Assert.That(_config.UnitPrice(TradeMode.Sell, _fish), Is.EqualTo(12));
        }

        [Test]
        public void NotListed_TradesAreRefused_NoSeedSelling_NoToolSelling()
        {
            var before = Snapshot();
            Assert.That(_market.Sell(_seed, 1), Is.EqualTo(TradeResult.NotInCatalog), "seeds are not bought back");
            Assert.That(_market.Sell(_rod, 1), Is.EqualTo(TradeResult.NotInCatalog));
            Assert.That(_market.Buy(_fruit, 1), Is.EqualTo(TradeResult.NotInCatalog));
            Assert.That(_market.Buy(null, 1), Is.EqualTo(TradeResult.InvalidItem));
            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        [Test]
        public void StarterSession_Has100Coins_ThreeSeeds_AndTools()
        {
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(100));
            Assert.That(_profile.Inventory.GetItemCount(_seed), Is.EqualTo(3));
            Assert.That(_profile.Inventory.GetItemCount(_hoe) + _profile.Inventory.GetItemCount(_can) + _profile.Inventory.GetItemCount(_rod), Is.EqualTo(3));
        }

        // ---------- buy ----------

        [Test]
        public void BuyOne_And_BuyMany_ChangeCoinsAndSeedsExactly()
        {
            Assert.That(_market.Buy(_seed, 1), Is.EqualTo(TradeResult.Success));
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(90));
            Assert.That(_profile.Inventory.GetItemCount(_seed), Is.EqualTo(4));
            Assert.That(_market.Buy(_seed, 5), Is.EqualTo(TradeResult.Success));
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(40));
            Assert.That(_profile.Inventory.GetItemCount(_seed), Is.EqualTo(9));
            Assert.That(_market.TradeCount, Is.EqualTo(2));
        }

        [Test]
        public void Buy_ExactBalance_Succeeds_ToZero()
        {
            Assert.That(_market.MaxQuantity(TradeMode.Buy, _seed), Is.EqualTo(10));
            Assert.That(_market.Buy(_seed, 10), Is.EqualTo(TradeResult.Success));
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(0));
            Assert.That(_market.MaxQuantity(TradeMode.Buy, _seed), Is.EqualTo(0));
        }

        [Test]
        public void Buy_NotEnoughCoins_ChangesNothing()
        {
            var before = Snapshot();
            Assert.That(_market.Buy(_seed, 11), Is.EqualTo(TradeResult.NotEnoughCoins));
            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        [Test]
        public void Buy_InventoryFull_IncludingPartialStacks_ChangesNothing()
        {
            // 8 free slots: fill them with driftwood, leaving only the seed stack (3/99) with 96 free.
            FillBagWith(_wood);
            Assert.That(_market.MaxQuantity(TradeMode.Buy, _seed), Is.EqualTo(10), "partial seed stack still has room");
            Assert.That(_market.Buy(_seed, 2), Is.EqualTo(TradeResult.Success), "fits into the partial stack");
            Assert.That(_profile.Inventory.GetItemCount(_seed), Is.EqualTo(5));

            // Now top the seed stack up to 99 so no seed fits anywhere.
            Assert.That(_profile.Inventory.AddItem(_seed, 94).Succeeded());
            var before = Snapshot();
            Assert.That(_market.Buy(_seed, 1), Is.EqualTo(TradeResult.InventoryFull));
            Assert.That(Snapshot(), Is.EqualTo(before), "coins not taken when the bag is full");
        }

        [Test]
        public void Buy_RestocksZeroCountHotbarSeed_KeepingTheSlot()
        {
            Assert.That(_profile.Hotbar.GetItem(3), Is.EqualTo(_seed));
            Assert.That(_profile.Inventory.RemoveItem(_seed, 3).Succeeded());
            Assert.That(_profile.Hotbar.GetItem(3), Is.EqualTo(_seed), "M3.3: reference kept at 0");
            Assert.That(_profile.Hotbar.IsAvailable(3), Is.False);

            int inventoryEvents = 0; // HotbarUI redraws on Inventory.Changed
            _profile.Inventory.Changed += () => inventoryEvents++;
            Assert.That(_market.Buy(_seed, 2), Is.EqualTo(TradeResult.Success));
            Assert.That(_profile.Hotbar.GetItem(3), Is.EqualTo(_seed));
            Assert.That(_profile.Hotbar.IsAvailable(3), Is.True, "slot usable again");
            Assert.That(_profile.Hotbar.GetCount(3), Is.EqualTo(2));
            Assert.That(inventoryEvents, Is.EqualTo(1), "one change event -> hotbar UI redraws once");
        }

        // ---------- sell ----------

        [Test]
        public void SellOne_And_SellMany()
        {
            _profile.Inventory.AddItem(_fruit, 5);
            _profile.Inventory.AddItem(_fish, 2);
            Assert.That(_market.Sell(_fruit, 1), Is.EqualTo(TradeResult.Success));
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(118));
            Assert.That(_market.Sell(_fruit, 4), Is.EqualTo(TradeResult.Success));
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(190));
            Assert.That(_profile.Inventory.GetItemCount(_fruit), Is.EqualTo(0));
            Assert.That(_market.Sell(_fish, 2), Is.EqualTo(TradeResult.Success));
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(214));
        }

        [Test]
        public void Sell_NotEnoughItems_ChangesNothing()
        {
            _profile.Inventory.AddItem(_fish, 1);
            var before = Snapshot();
            Assert.That(_market.Sell(_fish, 2), Is.EqualTo(TradeResult.NotEnoughItems));
            Assert.That(_market.Sell(_fruit, 1), Is.EqualTo(TradeResult.NotEnoughItems));
            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        [Test]
        public void Sell_AtCoinCap_IsRefused_AndChangesNothing()
        {
            _profile.Wallet.Add(Wallet.MaxCoins - 100 - 10); // 10 below the cap
            _profile.Inventory.AddItem(_fish, 1);
            var before = Snapshot();
            Assert.That(_market.Sell(_fish, 1), Is.EqualTo(TradeResult.CoinLimitReached));
            Assert.That(Snapshot(), Is.EqualTo(before));
            Assert.That(_market.MaxQuantity(TradeMode.Sell, _fish), Is.EqualTo(0));
        }

        [Test]
        public void Sell_FromSeveralStacks_RemovesExactly()
        {
            _profile.Inventory.AddItem(_fish, 99);
            _profile.Inventory.AddItem(_fish, 10); // second stack
            Assert.That(_market.Sell(_fish, 99), Is.EqualTo(TradeResult.Success));
            Assert.That(_profile.Inventory.GetItemCount(_fish), Is.EqualTo(10));
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(100 + 99 * 12));
        }

        // ---------- quantities ----------

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-999)]
        [TestCase(100)]           // above maxQuantityPerTrade (99)
        [TestCase(int.MaxValue)]  // overflow attempt
        [TestCase(int.MinValue)]
        public void InvalidQuantities_AreRefused_WithoutChanges(int quantity)
        {
            _profile.Inventory.AddItem(_fish, 50);
            var before = Snapshot();
            Assert.That(_market.Buy(_seed, quantity), Is.EqualTo(TradeResult.InvalidQuantity));
            Assert.That(_market.Sell(_fish, quantity), Is.EqualTo(TradeResult.InvalidQuantity));
            Assert.That(Snapshot(), Is.EqualTo(before));
        }

        [Test]
        public void Quote_TotalUses64Bit_AndMatchesUnitTimesQuantity()
        {
            TradeQuote q = _market.Quote(TradeMode.Buy, _seed, 7);
            Assert.That(q.Ok, Is.True);
            Assert.That(q.Total, Is.EqualTo(70L));
            Assert.That(q.UnitPrice, Is.EqualTo(10));
        }

        // ---------- duplication / re-entrancy ----------

        [Test]
        public void ReentrantTrade_FromChangeEvent_IsRefused_OneActionOneTrade()
        {
            var results = new List<TradeResult>();
            _profile.Wallet.Changed += _ => results.Add(_market.Buy(_seed, 1)); // would double-buy if allowed
            Assert.That(_market.Buy(_seed, 1), Is.EqualTo(TradeResult.Success));
            Assert.That(results, Has.All.EqualTo(TradeResult.Busy));
            Assert.That(_market.TradeCount, Is.EqualTo(1));
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(90));
            Assert.That(_profile.Inventory.GetItemCount(_seed), Is.EqualTo(4));
        }

        [Test]
        public void TradedEvent_FiresOncePerSuccess_NotOnFailure()
        {
            int events = 0;
            _market.Traded += _ => events++;
            _market.Buy(_seed, 2);
            _market.Buy(_seed, 50); // not enough coins
            _market.Sell(_fish, 1); // none owned
            Assert.That(events, Is.EqualTo(1));
        }

        // ---------- founder end-to-end example ----------

        [Test]
        public void EndToEnd_Sell3Fruit_1Fish_ThenBuy3Seeds_Gives136()
        {
            _profile.Inventory.AddItem(_fruit, 3);
            _profile.Inventory.AddItem(_fish, 1);
            Assert.That(_market.Sell(_fruit, 3), Is.EqualTo(TradeResult.Success));
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(154));
            Assert.That(_market.Sell(_fish, 1), Is.EqualTo(TradeResult.Success));
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(166));
            Assert.That(_market.Buy(_seed, 3), Is.EqualTo(TradeResult.Success));
            Assert.That(_profile.Wallet.Coins, Is.EqualTo(136));
            Assert.That(_profile.Inventory.GetItemCount(_seed), Is.EqualTo(6));
        }

        // ---------- config validation ----------

        [Test]
        public void ConfigValidation_FlagsMissingPrices_Duplicates_AndTools()
        {
            var bad = MarketConfig.CreateRuntime(new[] { _fruit, _seed, _seed }, new[] { _rod, _wood });
            var problems = new List<string>();
            bad.Validate(problems);
            Assert.That(problems.Exists(p => p.Contains("crop_tropical_fruit") && p.Contains("Buy Price")), Is.True);
            Assert.That(problems.Exists(p => p.Contains("twice")), Is.True);
            Assert.That(problems.Exists(p => p.Contains("tools cannot be sold")), Is.True);
            Object.DestroyImmediate(bad);

            var good = new List<string>();
            _config.Validate(good);
            Assert.That(good, Is.Empty);
        }

        // ---------- NPC facing ----------

        [TestCase(0f, -2f, FacingDirection.Down)]
        [TestCase(0f, 2f, FacingDirection.Up)]
        [TestCase(-2f, 0.5f, FacingDirection.Left)]
        [TestCase(2f, -0.5f, FacingDirection.Right)]
        [TestCase(0f, 0f, FacingDirection.Down)] // same spot: keep resting direction
        public void NpcFacesThePlayer(float dx, float dy, FacingDirection expected)
        {
            Assert.That(NpcView.DirectionTo(Vector2.zero, new Vector2(dx, dy), FacingDirection.Down), Is.EqualTo(expected));
        }
    }
}

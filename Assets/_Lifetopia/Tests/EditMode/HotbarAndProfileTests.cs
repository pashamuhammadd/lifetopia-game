using System.Collections.Generic;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using NUnit.Framework;
using UnityEngine;

namespace Lifetopia.Tests
{
    public sealed class HotbarTests
    {
        private TestItems _items;
        private Inventory _inv;
        private Hotbar _hotbar;

        [SetUp]
        public void SetUp()
        {
            _items = new TestItems();
            _inv = new Inventory();
            _hotbar = new Hotbar(_inv);
        }

        [TearDown]
        public void TearDown()
        {
            _hotbar.Dispose();
            _items.Destroy();
        }

        [Test] public void HasFiveSlots() => Assert.AreEqual(5, _hotbar.Size);

        [Test]
        public void Assign_RequiresItemInInventory()
        {
            Assert.AreEqual(InventoryResult.ItemNotInInventory, _hotbar.Assign(0, _items.Seed));
            _inv.AddItem(_items.Seed, 3);
            Assert.AreEqual(InventoryResult.Success, _hotbar.Assign(0, _items.Seed));
            Assert.AreEqual(InventoryResult.InvalidSlot, _hotbar.Assign(5, _items.Seed));
            Assert.AreEqual(InventoryResult.InvalidItem, _hotbar.Assign(1, null));
        }

        [Test]
        public void Count_IsReadFromInventory_NotDuplicated()
        {
            _inv.AddItem(_items.Seed, 3);
            _hotbar.Assign(3, _items.Seed);
            Assert.AreEqual(3, _hotbar.GetCount(3));
            _inv.AddItem(_items.Seed, 4);
            Assert.AreEqual(7, _hotbar.GetCount(3));
            _inv.RemoveItem(_items.Seed, 2);
            Assert.AreEqual(5, _hotbar.GetCount(3));
            Assert.AreEqual(5, _inv.GetItemCount(_items.Seed), "the hotbar must not add its own copy");
        }

        [Test]
        public void StaysConsistent_WhenItemIsMovedInInventory()
        {
            _inv.AddItem(_items.Hoe, 1);
            _inv.AddItem(_items.Seed, 3);
            _hotbar.Assign(0, _items.Hoe);
            _hotbar.Assign(1, _items.Seed);
            _inv.SwapSlot(0, 1);
            _inv.MoveSlot(0, 8);
            Assert.AreEqual(_items.Hoe, _hotbar.GetItem(0));
            Assert.AreEqual(_items.Seed, _hotbar.GetItem(1));
            Assert.AreEqual(3, _hotbar.GetCount(1));
        }

        [Test]
        public void ClearsSlot_WhenItemRunsOut()
        {
            _inv.AddItem(_items.Seed, 2);
            _hotbar.Assign(2, _items.Seed);
            var changed = new List<int>();
            _hotbar.SlotChanged += i => changed.Add(i);
            _inv.RemoveItem(_items.Seed, 1);
            Assert.AreEqual(_items.Seed, _hotbar.GetItem(2));
            _inv.RemoveItem(_items.Seed, 1);
            Assert.IsNull(_hotbar.GetItem(2));
            Assert.AreEqual(0, _hotbar.GetCount(2));
            CollectionAssert.AreEqual(new[] { 2 }, changed);
        }

        [Test]
        public void Item_IsOnAtMostOneSlot()
        {
            _inv.AddItem(_items.Rod, 1);
            _hotbar.Assign(0, _items.Rod);
            _hotbar.Assign(4, _items.Rod);
            Assert.IsNull(_hotbar.GetItem(0));
            Assert.AreEqual(_items.Rod, _hotbar.GetItem(4));
            Assert.AreEqual(4, _hotbar.IndexOf(_items.Rod));
        }

        [Test]
        public void Select_ChangesSelection_AndRejectsInvalidIndex()
        {
            int raised = -1;
            _hotbar.SelectionChanged += i => raised = i;
            Assert.IsTrue(_hotbar.Select(3));
            Assert.AreEqual(3, _hotbar.SelectedIndex);
            Assert.AreEqual(3, raised);
            Assert.IsFalse(_hotbar.Select(5));
            Assert.IsFalse(_hotbar.Select(-1));
            Assert.AreEqual(3, _hotbar.SelectedIndex);
        }

        [Test]
        public void ClearSlot_RemovesReferenceOnly()
        {
            _inv.AddItem(_items.Can, 1);
            _hotbar.Assign(1, _items.Can);
            Assert.AreEqual(InventoryResult.Success, _hotbar.ClearSlot(1));
            Assert.IsNull(_hotbar.GetItem(1));
            Assert.AreEqual(1, _inv.GetItemCount(_items.Can));
            Assert.AreEqual(InventoryResult.SlotEmpty, _hotbar.ClearSlot(1));
        }
    }

    public sealed class PlayerProfileTests
    {
        private TestItems _items;
        private StarterProfile _kit;

        [SetUp]
        public void SetUp()
        {
            _items = new TestItems();
            _kit = StarterProfile.CreateRuntime(100,
                new StarterProfile.StarterItem { item = _items.Hoe, quantity = 1, hotbarSlot = 0 },
                new StarterProfile.StarterItem { item = _items.Can, quantity = 1, hotbarSlot = 1 },
                new StarterProfile.StarterItem { item = _items.Rod, quantity = 1, hotbarSlot = 2 },
                new StarterProfile.StarterItem { item = _items.Seed, quantity = 3, hotbarSlot = 3 });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_kit);
            _items.Destroy();
        }

        [Test]
        public void StarterKit_Gives100Coins_3Seeds_3Tools_OnHotbar()
        {
            using var profile = new PlayerProfile();
            Assert.IsTrue(profile.GrantStarterKit(_kit));
            Assert.AreEqual(100, profile.Wallet.Coins);
            Assert.AreEqual(3, profile.Inventory.GetItemCount(_items.Seed));
            Assert.AreEqual(1, profile.Inventory.GetItemCount(_items.Hoe));
            Assert.AreEqual(1, profile.Inventory.GetItemCount(_items.Can));
            Assert.AreEqual(1, profile.Inventory.GetItemCount(_items.Rod));
            Assert.AreEqual(_items.Hoe, profile.Hotbar.GetItem(0));
            Assert.AreEqual(_items.Can, profile.Hotbar.GetItem(1));
            Assert.AreEqual(_items.Rod, profile.Hotbar.GetItem(2));
            Assert.AreEqual(_items.Seed, profile.Hotbar.GetItem(3));
            Assert.AreEqual(3, profile.Hotbar.GetCount(3));
            Assert.IsFalse(_items.Hoe.ConsumedOnUse, "tools are never used up");
            Assert.IsTrue(profile.StarterGranted);
        }

        [Test]
        public void StarterKit_IsGrantedOnlyOnce()
        {
            using var profile = new PlayerProfile();
            Assert.IsTrue(profile.GrantStarterKit(_kit));
            Assert.IsFalse(profile.GrantStarterKit(_kit));
            Assert.AreEqual(100, profile.Wallet.Coins);
            Assert.AreEqual(3, profile.Inventory.GetItemCount(_items.Seed));
            Assert.AreEqual(1, profile.Inventory.GetItemCount(_items.Hoe));
        }

        [Test]
        public void StarterKit_NotGrantedAgain_AfterSaveRoundTrip()
        {
            using var profile = new PlayerProfile();
            profile.GrantStarterKit(_kit);
            string json = JsonUtility.ToJson(profile.ToData());
            Assert.IsTrue(PlayerProfile.TryFromData(JsonUtility.FromJson<PlayerProfileData>(json), _items, out PlayerProfile loaded, out string error), error);
            using (loaded)
            {
                Assert.IsFalse(loaded.GrantStarterKit(_kit));
                Assert.AreEqual(100, loaded.Wallet.Coins);
                Assert.AreEqual(3, loaded.Inventory.GetItemCount(_items.Seed));
            }
        }

        [Test]
        public void StarterKit_ThatDoesNotFit_GrantsNothing()
        {
            using var profile = new PlayerProfile();
            profile.Inventory.AddItem(_items.Stool, 11); // one free slot, kit needs four
            Assert.IsFalse(profile.GrantStarterKit(_kit));
            Assert.AreEqual(0, profile.Wallet.Coins);
            Assert.AreEqual(0, profile.Inventory.GetItemCount(_items.Seed));
            Assert.AreEqual(0, profile.Inventory.GetItemCount(_items.Hoe));
            Assert.IsFalse(profile.StarterGranted);
        }

        [Test]
        public void SaveData_RoundTripsThroughJson()
        {
            using var profile = new PlayerProfile();
            profile.GrantStarterKit(_kit);
            profile.Inventory.AddItem(_items.Fish, 120);
            profile.Inventory.SwapSlot(0, 7);
            profile.Hotbar.Select(2);
            string json = JsonUtility.ToJson(profile.ToData());

            Assert.IsTrue(PlayerProfile.TryFromData(JsonUtility.FromJson<PlayerProfileData>(json), _items, out PlayerProfile loaded, out string error), error);
            using (loaded)
            {
                for (int i = 0; i < 12; i++)
                {
                    Assert.AreEqual(profile.Inventory.GetSlot(i).Item, loaded.Inventory.GetSlot(i).Item, $"slot {i}");
                    Assert.AreEqual(profile.Inventory.GetSlot(i).Quantity, loaded.Inventory.GetSlot(i).Quantity, $"slot {i}");
                }
                for (int i = 0; i < 5; i++) Assert.AreEqual(profile.Hotbar.GetItem(i), loaded.Hotbar.GetItem(i));
                Assert.AreEqual(2, loaded.Hotbar.SelectedIndex);
                Assert.AreEqual(100, loaded.Wallet.Coins);
            }
        }

        [Test]
        public void SaveData_WithImpossibleValues_IsRejectedWhole()
        {
            using var profile = new PlayerProfile();
            profile.GrantStarterKit(_kit);

            PlayerProfileData tooMany = profile.ToData();
            tooMany.inventory[3] = new SlotData { itemId = "seed_tropical", quantity = 100 };
            Assert.IsFalse(PlayerProfile.TryFromData(tooMany, _items, out _, out _));

            PlayerProfileData negative = profile.ToData();
            negative.inventory[3] = new SlotData { itemId = "seed_tropical", quantity = -5 };
            Assert.IsFalse(PlayerProfile.TryFromData(negative, _items, out _, out _));

            PlayerProfileData stackedTool = profile.ToData();
            stackedTool.inventory[0] = new SlotData { itemId = "tool_hoe", quantity = 2 };
            Assert.IsFalse(PlayerProfile.TryFromData(stackedTool, _items, out _, out _));

            PlayerProfileData unknown = profile.ToData();
            unknown.inventory[9] = new SlotData { itemId = "gold_bar", quantity = 1 };
            Assert.IsFalse(PlayerProfile.TryFromData(unknown, _items, out _, out _));

            PlayerProfileData duplicateHotbar = profile.ToData();
            duplicateHotbar.hotbar[4] = "tool_hoe";
            Assert.IsFalse(PlayerProfile.TryFromData(duplicateHotbar, _items, out _, out _));

            PlayerProfileData missingHotbarItem = profile.ToData();
            missingHotbarItem.hotbar[4] = "fish_reef";
            Assert.IsFalse(PlayerProfile.TryFromData(missingHotbarItem, _items, out _, out _));

            PlayerProfileData coins = profile.ToData();
            coins.coins = -1;
            Assert.IsFalse(PlayerProfile.TryFromData(coins, _items, out _, out _));

            PlayerProfileData future = profile.ToData();
            future.version = 99;
            Assert.IsFalse(PlayerProfile.TryFromData(future, _items, out _, out _));
        }

        [Test]
        public void Wallet_NeverNegative_NoOverflow()
        {
            var wallet = new Wallet();
            Assert.AreEqual(InventoryResult.Success, wallet.Add(100));
            Assert.AreEqual(InventoryResult.NotEnoughCoins, wallet.Spend(101));
            Assert.AreEqual(100, wallet.Coins);
            Assert.AreEqual(InventoryResult.InvalidQuantity, wallet.Spend(-5));
            Assert.AreEqual(InventoryResult.InvalidQuantity, wallet.Add(0));
            Assert.AreEqual(InventoryResult.CoinLimitReached, wallet.Add(int.MaxValue));
            Assert.AreEqual(100, wallet.Coins);
            Assert.AreEqual(InventoryResult.Success, wallet.Spend(100));
            Assert.AreEqual(0, wallet.Coins);
        }
    }
}

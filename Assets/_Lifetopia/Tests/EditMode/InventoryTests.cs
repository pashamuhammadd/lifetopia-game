using System.Collections.Generic;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using NUnit.Framework;
using UnityEngine;

namespace Lifetopia.Tests
{
    /// <summary>Shared in-memory items for inventory tests.</summary>
    internal sealed class TestItems : IItemCatalog
    {
        public readonly ItemDefinition Seed = ItemDefinition.CreateRuntime("seed_tropical", "Tropical Seeds", ItemCategory.Seed, true, 99, 5, 10);
        public readonly ItemDefinition Fish = ItemDefinition.CreateRuntime("fish_reef", "Reef Fish", ItemCategory.Fish, true, 99, 12);
        public readonly ItemDefinition Hoe = ItemDefinition.CreateRuntime("tool_hoe", "Hoe", ItemCategory.Tool, false);
        public readonly ItemDefinition Can = ItemDefinition.CreateRuntime("tool_watering_can", "Watering Can", ItemCategory.Tool, false);
        public readonly ItemDefinition Rod = ItemDefinition.CreateRuntime("tool_fishing_rod", "Fishing Rod", ItemCategory.Tool, false);
        public readonly ItemDefinition Stool = ItemDefinition.CreateRuntime("furniture_wooden_stool", "Wooden Stool", ItemCategory.Furniture, false);

        public IEnumerable<ItemDefinition> All => new[] { Seed, Fish, Hoe, Can, Rod, Stool };

        public bool TryGet(string itemId, out ItemDefinition item)
        {
            foreach (ItemDefinition def in All)
                if (def.ItemId == itemId) { item = def; return true; }
            item = null;
            return false;
        }

        public void Destroy()
        {
            foreach (ItemDefinition def in All) Object.DestroyImmediate(def);
        }
    }

    public sealed class InventoryTests
    {
        private TestItems _items;
        private Inventory _inv;

        [SetUp] public void SetUp() { _items = new TestItems(); _inv = new Inventory(); }
        [TearDown] public void TearDown() => _items.Destroy();

        private int Total()
        {
            int total = 0;
            for (int i = 0; i < _inv.SlotCount; i++) total += _inv.GetSlot(i).Quantity;
            return total;
        }

        [Test] public void HasTwelveSlotsByDefault() => Assert.AreEqual(12, _inv.SlotCount);

        [Test]
        public void AddAndRemove_UpdateCounts()
        {
            Assert.AreEqual(InventoryResult.Success, _inv.AddItem(_items.Seed, 3));
            Assert.AreEqual(3, _inv.GetItemCount(_items.Seed));
            Assert.IsTrue(_inv.HasItem(_items.Seed, 3));
            Assert.IsFalse(_inv.HasItem(_items.Seed, 4));
            Assert.AreEqual(InventoryResult.Success, _inv.RemoveItem(_items.Seed, 2));
            Assert.AreEqual(1, _inv.GetItemCount(_items.Seed));
            Assert.AreEqual(InventoryResult.Success, _inv.RemoveItem(_items.Seed, 1));
            Assert.IsTrue(_inv.GetSlot(0).IsEmpty);
        }

        [Test]
        public void StackLimit_Is99_AndOverflowGoesToNextSlot()
        {
            Assert.AreEqual(InventoryResult.Success, _inv.AddItem(_items.Seed, 150));
            Assert.AreEqual(99, _inv.GetSlot(0).Quantity);
            Assert.AreEqual(51, _inv.GetSlot(1).Quantity);
            Assert.AreEqual(150, _inv.GetItemCount(_items.Seed));
        }

        [Test]
        public void Add_TopsUpExistingStackBeforeUsingEmptySlot()
        {
            _inv.AddItem(_items.Seed, 98);
            _inv.AddItem(_items.Fish, 1);
            _inv.AddItem(_items.Seed, 3);
            Assert.AreEqual(99, _inv.GetSlot(0).Quantity);
            Assert.AreEqual(_items.Fish, _inv.GetSlot(1).Item);
            Assert.AreEqual(2, _inv.GetSlot(2).Quantity);
        }

        [Test]
        public void FullInventory_RejectsWholeAdd_AndChangesNothing()
        {
            for (int i = 0; i < 11; i++) _inv.AddItem(_items.Stool, 1);
            _inv.AddItem(_items.Seed, 90); // last slot: 90/99 seeds
            int changes = 0;
            _inv.Changed += () => changes++;

            Assert.IsFalse(_inv.CanAddItem(_items.Seed, 10));
            Assert.AreEqual(InventoryResult.InventoryFull, _inv.AddItem(_items.Seed, 10));
            Assert.AreEqual(InventoryResult.InventoryFull, _inv.AddItem(_items.Fish, 1));
            Assert.AreEqual(InventoryResult.InventoryFull, _inv.AddItem(_items.Hoe, 1));
            Assert.AreEqual(90, _inv.GetItemCount(_items.Seed), "partial add must not happen");
            Assert.AreEqual(0, changes);

            Assert.IsTrue(_inv.CanAddItem(_items.Seed, 9));
            Assert.AreEqual(InventoryResult.Success, _inv.AddItem(_items.Seed, 9));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void InvalidQuantity_IsRejected(int quantity)
        {
            _inv.AddItem(_items.Seed, 5);
            Assert.AreEqual(InventoryResult.InvalidQuantity, _inv.AddItem(_items.Seed, quantity));
            Assert.AreEqual(InventoryResult.InvalidQuantity, _inv.RemoveItem(_items.Seed, quantity));
            Assert.AreEqual(InventoryResult.InvalidQuantity, _inv.RemoveFromSlot(0, quantity));
            Assert.IsFalse(_inv.CanAddItem(_items.Seed, quantity));
            Assert.AreEqual(5, _inv.GetItemCount(_items.Seed));
        }

        [Test]
        public void HugeQuantity_DoesNotOverflow()
        {
            Assert.AreEqual(InventoryResult.InventoryFull, _inv.AddItem(_items.Seed, int.MaxValue));
            Assert.AreEqual(0, Total());
            Assert.AreEqual(12 * 99, _inv.GetFreeCapacity(_items.Seed));
        }

        [Test]
        public void NullItem_IsRejected()
        {
            Assert.AreEqual(InventoryResult.InvalidItem, _inv.AddItem(null, 1));
            Assert.AreEqual(InventoryResult.InvalidItem, _inv.RemoveItem(null, 1));
            Assert.AreEqual(0, _inv.GetItemCount(null));
        }

        [Test]
        public void RemoveMoreThanOwned_FailsAndKeepsItems()
        {
            _inv.AddItem(_items.Seed, 3);
            Assert.AreEqual(InventoryResult.NotEnoughItems, _inv.RemoveItem(_items.Seed, 4));
            Assert.AreEqual(3, _inv.GetItemCount(_items.Seed));
            Assert.AreEqual(InventoryResult.NotEnoughItems, _inv.RemoveFromSlot(0, 4));
            Assert.AreEqual(InventoryResult.SlotEmpty, _inv.RemoveFromSlot(5, 1));
        }

        [Test]
        public void RemoveAcrossStacks_TakesExactAmount()
        {
            _inv.AddItem(_items.Seed, 120); // 99 + 21
            Assert.AreEqual(InventoryResult.Success, _inv.RemoveItem(_items.Seed, 30));
            Assert.AreEqual(90, _inv.GetItemCount(_items.Seed));
        }

        [Test]
        public void NonStackable_UsesOneSlotEach()
        {
            Assert.AreEqual(1, _items.Hoe.MaxStack);
            Assert.AreEqual(InventoryResult.Success, _inv.AddItem(_items.Stool, 3));
            for (int i = 0; i < 3; i++) Assert.AreEqual(1, _inv.GetSlot(i).Quantity);
            Assert.AreEqual(3, _inv.GetItemCount(_items.Stool));
            Assert.AreEqual(InventoryResult.NotStackable, _inv.MergeStack(0, 1));
            Assert.AreEqual(InventoryResult.InventoryFull, _inv.AddItem(_items.Stool, 10));
            Assert.AreEqual(3, _inv.GetItemCount(_items.Stool));
        }

        [Test]
        public void MoveSlot_ToEmpty_MovesWholeStack()
        {
            _inv.AddItem(_items.Seed, 7);
            Assert.AreEqual(InventoryResult.Success, _inv.MoveSlot(0, 5));
            Assert.IsTrue(_inv.GetSlot(0).IsEmpty);
            Assert.AreEqual(7, _inv.GetSlot(5).Quantity);
        }

        [Test]
        public void MoveSlot_OntoOtherItem_FailsWithoutChange()
        {
            _inv.AddItem(_items.Seed, 7);
            _inv.AddItem(_items.Fish, 2);
            Assert.AreEqual(InventoryResult.TargetOccupied, _inv.MoveSlot(0, 1));
            Assert.AreEqual(_items.Seed, _inv.GetSlot(0).Item);
            Assert.AreEqual(_items.Fish, _inv.GetSlot(1).Item);
            Assert.AreEqual(InventoryResult.SlotEmpty, _inv.MoveSlot(4, 5));
            Assert.AreEqual(InventoryResult.InvalidSlot, _inv.MoveSlot(0, 12));
            Assert.AreEqual(InventoryResult.InvalidSlot, _inv.MoveSlot(-1, 0));
            Assert.AreEqual(InventoryResult.SameSlot, _inv.MoveSlot(0, 0));
        }

        [Test]
        public void SwapSlot_ExchangesContents()
        {
            _inv.AddItem(_items.Seed, 7);
            _inv.AddItem(_items.Hoe, 1);
            Assert.AreEqual(InventoryResult.Success, _inv.SwapSlot(0, 1));
            Assert.AreEqual(_items.Hoe, _inv.GetSlot(0).Item);
            Assert.AreEqual(7, _inv.GetSlot(1).Quantity);
            Assert.AreEqual(InventoryResult.Success, _inv.SwapSlot(1, 9)); // with empty slot
            Assert.AreEqual(7, _inv.GetSlot(9).Quantity);
            Assert.AreEqual(InventoryResult.SlotEmpty, _inv.SwapSlot(3, 4));
            Assert.AreEqual(InventoryResult.SameSlot, _inv.SwapSlot(0, 0));
        }

        [Test]
        public void MergeStack_MovesWhatFits_KeepsRemainder()
        {
            _inv.AddItem(_items.Seed, 99);
            _inv.AddItem(_items.Seed, 50); // slot1 = 50
            _inv.RemoveFromSlot(0, 9);     // slot0 = 90
            Assert.AreEqual(InventoryResult.Success, _inv.MergeStack(1, 0));
            Assert.AreEqual(99, _inv.GetSlot(0).Quantity);
            Assert.AreEqual(41, _inv.GetSlot(1).Quantity);
            Assert.AreEqual(InventoryResult.StackFull, _inv.MergeStack(1, 0));
            Assert.AreEqual(140, _inv.GetItemCount(_items.Seed));
        }

        [Test]
        public void MergeStack_RejectsDifferentItemsAndEmptyTarget()
        {
            _inv.AddItem(_items.Seed, 5);
            _inv.AddItem(_items.Fish, 5);
            Assert.AreEqual(InventoryResult.DifferentItems, _inv.MergeStack(0, 1));
            Assert.AreEqual(InventoryResult.SlotEmpty, _inv.MergeStack(0, 4));
            Assert.AreEqual(InventoryResult.SameSlot, _inv.MergeStack(0, 0));
        }

        [Test]
        public void MoveSlot_OntoSameItem_MergesOnlyWhenEverythingFits()
        {
            _inv.AddItem(_items.Seed, 99);
            _inv.AddItem(_items.Seed, 10); // slot1 = 10
            Assert.AreEqual(InventoryResult.StackFull, _inv.MoveSlot(1, 0));
            Assert.AreEqual(10, _inv.GetSlot(1).Quantity);
            _inv.RemoveFromSlot(0, 50);   // slot0 = 49
            Assert.AreEqual(InventoryResult.Success, _inv.MoveSlot(1, 0));
            Assert.AreEqual(59, _inv.GetSlot(0).Quantity);
            Assert.IsTrue(_inv.GetSlot(1).IsEmpty);
        }

        [Test]
        public void NoDuplication_AcrossManyMovesSwapsAndMerges()
        {
            _inv.AddItem(_items.Seed, 150);
            _inv.AddItem(_items.Fish, 30);
            _inv.AddItem(_items.Hoe, 1);
            int before = Total();
            var random = new System.Random(1234);
            for (int n = 0; n < 2000; n++)
            {
                int a = random.Next(-1, 13), b = random.Next(-1, 13);
                switch (random.Next(3))
                {
                    case 0: _inv.MoveSlot(a, b); break;
                    case 1: _inv.SwapSlot(a, b); break;
                    default: _inv.MergeStack(a, b); break;
                }
                for (int i = 0; i < _inv.SlotCount; i++)
                {
                    InventorySlot s = _inv.GetSlot(i);
                    Assert.IsTrue(s.IsEmpty || (s.Quantity >= 1 && s.Quantity <= s.Item.MaxStack));
                }
            }
            Assert.AreEqual(before, Total());
            Assert.AreEqual(150, _inv.GetItemCount(_items.Seed));
            Assert.AreEqual(30, _inv.GetItemCount(_items.Fish));
            Assert.AreEqual(1, _inv.GetItemCount(_items.Hoe));
        }

        [Test]
        public void Events_FireOnlyForChangedSlots_AndNotOnFailure()
        {
            var changed = new List<int>();
            int total = 0;
            _inv.SlotChanged += i => changed.Add(i);
            _inv.Changed += () => total++;
            _inv.AddItem(_items.Seed, 120);
            CollectionAssert.AreEqual(new[] { 0, 1 }, changed);
            Assert.AreEqual(1, total);
            changed.Clear();
            _inv.RemoveItem(_items.Seed, 500);
            _inv.SwapSlot(5, 6);
            Assert.IsEmpty(changed);
            Assert.AreEqual(1, total);
        }
    }
}

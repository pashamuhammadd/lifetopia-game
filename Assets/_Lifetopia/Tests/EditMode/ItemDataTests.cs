using System.Collections.Generic;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using Lifetopia.UI;
using NUnit.Framework;
using UnityEditor;

namespace Lifetopia.Tests
{
    /// <summary>Checks the authored item assets in Data/Items (catches typos before they reach a save).</summary>
    public sealed class ItemDataTests
    {
        private const string DatabasePath = "Assets/_Lifetopia/Data/Items/ItemDatabase.asset";
        private const string StarterPath = "Assets/_Lifetopia/Data/Items/StarterProfile.asset";

        [Test]
        public void Database_HasValidUniqueIds()
        {
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
            Assert.IsNotNull(db, "ItemDatabase asset missing");
            var problems = new List<string>();
            db.Validate(problems);
            Assert.IsEmpty(problems, string.Join("\n", problems));
            foreach (string id in new[] { "tool_hoe", "tool_watering_can", "tool_fishing_rod", "seed_tropical" })
                Assert.IsTrue(db.TryGet(id, out _), $"missing {id}");
        }

        [Test]
        public void Database_ContainsEveryItemAsset()
        {
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
            foreach (string guid in AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/_Lifetopia/Data/Items" }))
            {
                var def = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsTrue(db.TryGet(def.ItemId, out ItemDefinition found) && found == def, $"{def.name} not in database");
            }
        }

        [Test]
        public void Tools_AreNonStackable_AndNeverConsumed()
        {
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
            foreach (ItemDefinition def in db.Items)
            {
                Assert.LessOrEqual(def.MaxStack, ItemDefinition.MaxStackLimit);
                if (def.Category != ItemCategory.Tool) continue;
                Assert.IsFalse(def.Stackable, def.ItemId);
                Assert.IsFalse(def.ConsumedOnUse, def.ItemId);
            }
        }

        [Test]
        public void StarterAsset_MatchesGdd()
        {
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(DatabasePath);
            var kit = AssetDatabase.LoadAssetAtPath<StarterProfile>(StarterPath);
            Assert.IsNotNull(kit, "StarterProfile asset missing");
            using var profile = new PlayerProfile();
            Assert.IsTrue(profile.GrantStarterKit(kit));
            db.TryGet("seed_tropical", out ItemDefinition seed);
            Assert.AreEqual(100, profile.Wallet.Coins);
            Assert.AreEqual(3, profile.Inventory.GetItemCount(seed));
            int tools = 0;
            for (int i = 0; i < profile.Hotbar.Size; i++)
                if (profile.Hotbar.GetItem(i) != null && profile.Hotbar.GetItem(i).Category == ItemCategory.Tool) tools++;
            Assert.AreEqual(3, tools, "three starter tools on the hotbar");
        }

        [TestCase("tool_hoe", true)]
        [TestCase("seed_tropical_2", true)]
        [TestCase("", false)]
        [TestCase("Tool_Hoe", false)]
        [TestCase("tool hoe", false)]
        [TestCase("1tool", false)]
        public void ItemIdFormat(string id, bool valid) => Assert.AreEqual(valid, ItemDefinition.IsValidId(id));

        [TestCase("Hoe", "HOE")]
        [TestCase("Watering Can", "WC")]
        [TestCase("Tropical Seeds", "TS")]
        [TestCase("", "?")]
        public void PlaceholderLabel(string name, string expected) => Assert.AreEqual(expected, ItemSlotView.ShortLabel(name));
    }
}

using System;
using Lifetopia.Items;

namespace Lifetopia.InventorySystem
{
    /// <summary>
    /// Runtime state of the local player: inventory (12 slots), hotbar (5 references) and coins.
    /// Created once per new game session; convertible to/from <see cref="PlayerProfileData"/> for saves.
    /// No network, account or real currency involved.
    /// </summary>
    public sealed class PlayerProfile : IDisposable
    {
        public Inventory Inventory { get; }
        public Hotbar Hotbar { get; }
        public Wallet Wallet { get; }

        /// <summary>True once the starter kit has been granted; stored in saves so it is never granted twice.</summary>
        public bool StarterGranted { get; private set; }

        public PlayerProfile(int inventorySlots = Inventory.DefaultSlotCount, int hotbarSlots = Hotbar.DefaultSize)
        {
            Inventory = new Inventory(inventorySlots);
            Hotbar = new Hotbar(Inventory, hotbarSlots);
            Wallet = new Wallet();
        }

        /// <summary>
        /// Gives the starter coins/items exactly once. All-or-nothing: if the kit is invalid or does not
        /// fit, nothing is granted and the flag stays false.
        /// </summary>
        public bool GrantStarterKit(StarterProfile kit)
        {
            if (StarterGranted || kit == null) return false;
            if ((long)Wallet.Coins + kit.StartingCoins > Wallet.MaxCoins) return false;

            // Dry run on a copy so a kit that does not fit changes nothing.
            var trial = new Inventory(Inventory.SlotCount);
            trial.LoadSlots(CopySlots(Inventory));
            foreach (StarterProfile.StarterItem entry in kit.Items)
            {
                if (entry.item == null || entry.quantity <= 0) return false;
                if (!trial.AddItem(entry.item, entry.quantity).Succeeded()) return false;
            }

            foreach (StarterProfile.StarterItem entry in kit.Items) Inventory.AddItem(entry.item, entry.quantity);
            if (kit.StartingCoins > 0) Wallet.Add(kit.StartingCoins);
            foreach (StarterProfile.StarterItem entry in kit.Items)
                if (Hotbar.IsValidSlot(entry.hotbarSlot) && Hotbar.GetItem(entry.hotbarSlot) == null)
                    Hotbar.Assign(entry.hotbarSlot, entry.item);

            StarterGranted = true;
            return true;
        }

        public PlayerProfileData ToData()
        {
            var data = new PlayerProfileData
            {
                coins = Wallet.Coins,
                starterGranted = StarterGranted,
                selectedHotbarSlot = Hotbar.SelectedIndex,
            };
            for (int i = 0; i < Inventory.SlotCount; i++)
            {
                InventorySlot slot = Inventory.GetSlot(i);
                data.inventory.Add(new SlotData { itemId = slot.IsEmpty ? "" : slot.Item.ItemId, quantity = slot.Quantity });
            }
            for (int i = 0; i < Hotbar.Size; i++)
            {
                ItemDefinition item = Hotbar.GetItem(i);
                data.hotbar.Add(item != null ? item.ItemId : "");
            }
            return data;
        }

        /// <summary>
        /// Rebuilds a profile from saved data. Rejects the whole save (returns false) on unknown items,
        /// impossible quantities, wrong slot counts or a newer version, instead of loading it partially.
        /// </summary>
        public static bool TryFromData(PlayerProfileData data, IItemCatalog catalog, out PlayerProfile profile, out string error)
        {
            profile = null;
            error = null;
            if (data == null || catalog == null) { error = "Missing data."; return false; }
            if (data.version != PlayerProfileData.CurrentVersion) { error = $"Unsupported save version {data.version}."; return false; }
            if (data.coins < 0 || data.coins > Wallet.MaxCoins) { error = "Invalid coin balance."; return false; }
            if (data.inventory == null || data.inventory.Count != Inventory.DefaultSlotCount) { error = "Invalid inventory size."; return false; }
            if (data.hotbar == null || data.hotbar.Count != Hotbar.DefaultSize) { error = "Invalid hotbar size."; return false; }

            var slots = new InventorySlot[data.inventory.Count];
            for (int i = 0; i < slots.Length; i++)
            {
                SlotData s = data.inventory[i];
                if (string.IsNullOrEmpty(s.itemId))
                {
                    if (s.quantity != 0) { error = $"Slot {i}: quantity without item."; return false; }
                    continue;
                }
                if (!catalog.TryGet(s.itemId, out ItemDefinition item)) { error = $"Slot {i}: unknown item '{s.itemId}'."; return false; }
                if (s.quantity < 1 || s.quantity > item.MaxStack) { error = $"Slot {i}: invalid quantity {s.quantity}."; return false; }
                slots[i] = new InventorySlot(item, s.quantity);
            }

            var result = new PlayerProfile();
            result.Inventory.LoadSlots(slots);
            for (int i = 0; i < data.hotbar.Count; i++)
            {
                string id = data.hotbar[i];
                if (string.IsNullOrEmpty(id)) continue;
                if (!catalog.TryGet(id, out ItemDefinition item) || result.Hotbar.IndexOf(item) >= 0 ||
                    !result.Hotbar.Assign(i, item).Succeeded())
                {
                    result.Dispose();
                    error = $"Hotbar slot {i}: invalid reference '{id}'.";
                    return false;
                }
            }
            result.Hotbar.Select(result.Hotbar.IsValidSlot(data.selectedHotbarSlot) ? data.selectedHotbarSlot : 0);
            result.Wallet.Load(data.coins);
            result.StarterGranted = data.starterGranted;
            profile = result;
            return true;
        }

        public void Dispose() => Hotbar.Dispose();

        private static InventorySlot[] CopySlots(Inventory source)
        {
            var copy = new InventorySlot[source.SlotCount];
            for (int i = 0; i < copy.Length; i++) copy[i] = source.GetSlot(i);
            return copy;
        }
    }
}

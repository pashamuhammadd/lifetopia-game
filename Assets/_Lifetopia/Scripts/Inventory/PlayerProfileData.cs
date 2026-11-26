using System;
using System.Collections.Generic;

namespace Lifetopia.InventorySystem
{
    /// <summary>
    /// Plain, versioned snapshot of the local player state for saving (JsonUtility-compatible).
    /// Holds only stable item IDs and numbers, never Unity object references.
    /// The save system itself is a later milestone; this is the format it will write.
    /// </summary>
    [Serializable]
    public sealed class PlayerProfileData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int coins;
        public bool starterGranted;
        public List<SlotData> inventory = new List<SlotData>();
        public List<string> hotbar = new List<string>();
        public int selectedHotbarSlot;
    }

    [Serializable]
    public struct SlotData
    {
        public string itemId;
        public int quantity;
    }
}

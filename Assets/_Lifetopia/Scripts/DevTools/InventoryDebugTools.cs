using Lifetopia.InventorySystem;
using Lifetopia.Items;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Lifetopia.DevTools
{
    /// <summary>
    /// DEVELOPMENT ONLY: keyboard shortcuts to add/remove test items and coins. Not a player feature.
    /// The body compiles only in the Editor and Development Builds; in release builds the component
    /// exists (so scenes keep a valid script reference) but does nothing.
    /// Function keys collide with browser shortcuts, so use these in the Editor.
    /// </summary>
    public sealed class InventoryDebugTools : MonoBehaviour
    {
#pragma warning disable 0169, 0414, 0649 // fields are only read in Editor/Development builds
        [SerializeField] private PlayerProfileHost host;
        [SerializeField] private ItemDefinition seed;
        [SerializeField] private ItemDefinition fish;
        [SerializeField] private ItemDefinition material;
        [SerializeField] private ItemDefinition furniture;
        [SerializeField] private bool showHelp = true;
#pragma warning restore 0169, 0414, 0649

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const string Help =
            "DEV  F1 +1 Seeds   F2 -1 Seeds   F3 +1 Fish   F4 +10 Material   F6 +99 Material   " +
            "F7 +1 Furniture   F8 +10 coins   F9 -10 coins   F10 hide";

        private void Update()
        {
            Keyboard k = Keyboard.current;
            if (k == null || host == null) return;
            if (k.shiftKey.isPressed) return; // Shift + F-keys belong to TimeDebugTools
            if (k.f1Key.wasPressedThisFrame) Give(seed, 1);
            if (k.f2Key.wasPressedThisFrame) Take(seed, 1);
            if (k.f3Key.wasPressedThisFrame) Give(fish, 1);
            if (k.f4Key.wasPressedThisFrame) Give(material, 10);
            if (k.f6Key.wasPressedThisFrame) Give(material, 99);
            if (k.f7Key.wasPressedThisFrame) Give(furniture, 1);
            if (k.f8Key.wasPressedThisFrame) Coins(+10);
            if (k.f9Key.wasPressedThisFrame) Coins(-10);
            if (k.f10Key.wasPressedThisFrame) showHelp = !showHelp;
        }

        public void Give(ItemDefinition item, int quantity)
        {
            if (item != null && host.GiveItem(item, quantity).Succeeded()) host.Notify($"DEV: added {quantity} {item.DisplayName}");
        }

        public void Take(ItemDefinition item, int quantity)
        {
            if (item != null && host.TakeItem(item, quantity).Succeeded()) host.Notify($"DEV: removed {quantity} {item.DisplayName}");
        }

        private void Coins(int delta)
        {
            Wallet wallet = host.Profile.Wallet;
            InventoryResult result = delta > 0 ? wallet.Add(delta) : wallet.Spend(-delta);
            host.Notify(result.Succeeded() ? $"DEV: coins {delta:+#;-#}" : InventoryMessages.Describe(result));
        }

        private void OnGUI()
        {
            if (!showHelp) return;
            // Under the keyboard hint; F10 hides it (e.g. for screenshots).
            GUI.Label(new Rect(24, 84, Screen.width - 48, 40), Help);
        }
#endif
    }
}

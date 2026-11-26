using System;
using Lifetopia.Items;
using UnityEngine;

namespace Lifetopia.InventorySystem
{
    /// <summary>
    /// Scene component that owns the local <see cref="PlayerProfile"/> for this play session and is the
    /// entry point for gameplay code that gives or takes items. A new profile (with the starter kit) is
    /// created the first time it is needed; later milestones will load a saved profile instead.
    /// Failures raise <see cref="Notification"/> so the HUD can tell the player instead of losing items silently.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerProfileHost : MonoBehaviour
    {
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private StarterProfile starterProfile;

        private PlayerProfile _profile;

        /// <summary>Short message for the player (English).</summary>
        public event Action<string> Notification;

        public IItemCatalog Catalog => itemDatabase;

        public PlayerProfile Profile
        {
            get
            {
                if (_profile == null) CreateNewSession();
                return _profile;
            }
        }

        private void Awake()
        {
            if (_profile == null) CreateNewSession();
        }

        private void OnDestroy()
        {
            _profile?.Dispose();
            _profile = null;
        }

        public InventoryResult GiveItem(ItemDefinition item, int quantity)
        {
            InventoryResult result = Profile.Inventory.AddItem(item, quantity);
            if (!result.Succeeded()) Notify(InventoryMessages.Describe(result, item));
            return result;
        }

        public InventoryResult TakeItem(ItemDefinition item, int quantity)
        {
            InventoryResult result = Profile.Inventory.RemoveItem(item, quantity);
            if (!result.Succeeded()) Notify(InventoryMessages.Describe(result, item));
            return result;
        }

        public void Notify(string message)
        {
            if (!string.IsNullOrEmpty(message)) Notification?.Invoke(message);
        }

        private void CreateNewSession()
        {
            _profile = new PlayerProfile();
            if (starterProfile == null)
            {
                Debug.LogWarning("[Lifetopia] PlayerProfileHost has no StarterProfile; starting empty.", this);
                return;
            }
            if (!_profile.GrantStarterKit(starterProfile))
                Debug.LogError("[Lifetopia] Starter kit could not be granted (invalid or does not fit).", this);
        }
    }
}

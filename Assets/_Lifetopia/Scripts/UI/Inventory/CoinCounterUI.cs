using System.Globalization;
using Lifetopia.InventorySystem;
using UnityEngine;
using UnityEngine.UI;

namespace Lifetopia.UI
{
    /// <summary>Shows the local coin balance; updates only when the wallet changes.</summary>
    public sealed class CoinCounterUI : MonoBehaviour
    {
        [SerializeField] private PlayerProfileHost host;
        [SerializeField] private Text amountLabel;

        private Wallet _wallet;

        private void Start()
        {
            if (host == null || amountLabel == null)
            {
                Debug.LogError("[Lifetopia] CoinCounterUI is missing references.", this);
                enabled = false;
                return;
            }
            _wallet = host.Profile.Wallet;
            _wallet.Changed += Show;
            Show(_wallet.Coins);
        }

        private void OnDestroy()
        {
            if (_wallet != null) _wallet.Changed -= Show;
        }

        private void Show(int coins) => amountLabel.text = coins.ToString("N0", CultureInfo.InvariantCulture);
    }
}

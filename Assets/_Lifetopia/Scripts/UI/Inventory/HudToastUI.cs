using Lifetopia.InventorySystem;
using UnityEngine;
using UnityEngine.UI;

namespace Lifetopia.UI
{
    /// <summary>
    /// Short message at the top of the screen (e.g. "Your bag is full."). Driven by
    /// <see cref="PlayerProfileHost.Notification"/>; the component only ticks while a message is visible.
    /// </summary>
    public sealed class HudToastUI : MonoBehaviour
    {
        [SerializeField] private PlayerProfileHost host;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Text label;
        [SerializeField, Min(0.5f)] private float visibleSeconds = 2.5f;
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.4f;

        private float _timer;

        /// <summary>Last message shown (for tests).</summary>
        public string LastMessage { get; private set; } = "";

        private void Awake()
        {
            if (group != null) group.alpha = 0f;
            // Subscribe here, not in Start: the component disables itself until a message arrives,
            // and Start never runs on a disabled component.
            if (host != null) host.Notification += Show;
            enabled = false;
        }

        private void OnDestroy()
        {
            if (host != null) host.Notification -= Show;
        }

        public void Show(string message)
        {
            LastMessage = message;
            if (label != null) label.text = message;
            if (group != null) group.alpha = 1f;
            _timer = visibleSeconds + fadeSeconds;
            enabled = true;
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (group != null) group.alpha = Mathf.Clamp01(_timer / fadeSeconds);
            if (_timer <= 0f) enabled = false;
        }
    }
}

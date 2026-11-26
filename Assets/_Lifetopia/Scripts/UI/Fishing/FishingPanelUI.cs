using Lifetopia.Fishing;
using Lifetopia.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Lifetopia.UI
{
    /// <summary>
    /// Fishing mini-game panel (bottom centre): fish icon, English status, a bar with a green target zone
    /// (with dark boundary lines, so it does not rely on colour alone), a moving high-contrast marker, and
    /// big Reel In / Cancel buttons for touch. Objects in <see cref="hideWhileOpen"/> (the hotbar) are hidden
    /// while the panel is up. Holds no game state.
    /// </summary>
    public sealed class FishingPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text hintLabel;
        [SerializeField] private Image fishIcon;
        [SerializeField] private RectTransform bar;
        [SerializeField] private RectTransform zone;
        [SerializeField] private RectTransform marker;
        [SerializeField] private Button reelInButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Text cancelLabel;
        [SerializeField] private GameObject[] hideWhileOpen = new GameObject[0];
        [SerializeField] private TouchControlsVisibility touchVisibility;

        private FishingController _controller;
        private bool[] _wasActive;
        private FishingState _shownState = (FishingState)(-1);

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
            if (reelInButton != null) reelInButton.onClick.AddListener(() => _controller?.Press());
            if (cancelButton != null) cancelButton.onClick.AddListener(() => _controller?.Cancel());
        }

        public void Bind(FishingController controller) => _controller = controller;

        public void Open()
        {
            if (root == null) return;
            _wasActive = new bool[hideWhileOpen.Length];
            for (int i = 0; i < hideWhileOpen.Length; i++)
            {
                if (hideWhileOpen[i] == null) continue;
                _wasActive[i] = hideWhileOpen[i].activeSelf;
                hideWhileOpen[i].SetActive(false);
            }
            _shownState = (FishingState)(-1);
            root.SetActive(true);
        }

        public void Close()
        {
            if (root == null || !root.activeSelf) return;
            root.SetActive(false);
            if (_wasActive == null) return;
            for (int i = 0; i < hideWhileOpen.Length; i++)
                if (hideWhileOpen[i] != null && _wasActive[i]) hideWhileOpen[i].SetActive(true);
        }

        /// <summary>Called every frame while open: moves the marker; texts change only with the state.</summary>
        public void Show(FishingSession session, string status)
        {
            if (session == null || root == null || !root.activeSelf) return;
            FishingSettings s = session.Settings;
            if (zone != null)
            {
                zone.anchorMin = new Vector2(s.ZoneMin, 0f);
                zone.anchorMax = new Vector2(s.ZoneMax, 1f);
            }
            if (marker != null)
            {
                float x = session.State == FishingState.Timing || session.IsFinished ? session.Marker : 0f;
                marker.anchorMin = marker.anchorMax = new Vector2(x, 0.5f);
            }
            if (session.State == _shownState) return;
            _shownState = session.State;
            if (statusLabel != null) statusLabel.text = status;
            bool active = session.IsActive;
            if (reelInButton != null) reelInButton.interactable = active;
            if (cancelLabel != null) cancelLabel.text = active ? "Cancel" : "Close";
            if (hintLabel != null)
            {
                bool touch = touchVisibility != null && touchVisibility.TouchVisible;
                hintLabel.text = !active ? "" : touch ? "Tap Reel In when the marker is in the green zone" : "[E] Reel in when the marker is in the green zone   [Esc] Cancel";
            }
        }

        /// <summary>Sets the fish icon (local art override by item ID, else a placeholder tile).</summary>
        public void SetFish(ItemDefinition fish)
        {
            if (fishIcon == null) return;
            Sprite art = ItemIcons.Get(fish);
            fishIcon.sprite = art != null ? art : RoundedSprites.Get(12);
            fishIcon.type = art != null ? Image.Type.Simple : Image.Type.Sliced;
            fishIcon.preserveAspect = art != null;
            fishIcon.color = art != null ? Color.white : UiPalette.Lagoon;
        }
    }
}

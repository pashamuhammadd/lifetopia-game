using Lifetopia.Core;
using Lifetopia.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace Lifetopia.UI
{
    /// <summary>
    /// Shows "[E] Read Welcome Board" while an interactable is in range and dims the touch
    /// Interact button when there is nothing to use. Hidden while a modal blocks gameplay.
    /// </summary>
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private InteractionDetector detector;
        [SerializeField] private GameplayInputGate gate;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptLabel;

        [Tooltip("Optional: the touch Interact button's CanvasGroup, dimmed when nothing is in range.")]
        [SerializeField] private CanvasGroup touchInteractButton;
        [SerializeField, Range(0f, 1f)] private float dimmedAlpha = 0.45f;

        [Tooltip("Optional: when touch controls are visible the keyboard key hint is left out.")]
        [SerializeField] private TouchControlsVisibility touchVisibility;

        [SerializeField] private string keyHint = "[E]";

        private void OnEnable()
        {
            if (detector != null) detector.CurrentChanged += OnTargetChanged;
            if (gate != null) gate.BlockedChanged += OnBlockedChanged;
            if (touchVisibility != null) touchVisibility.VisibilityChanged += OnBlockedChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (detector != null) detector.CurrentChanged -= OnTargetChanged;
            if (gate != null) gate.BlockedChanged -= OnBlockedChanged;
            if (touchVisibility != null) touchVisibility.VisibilityChanged -= OnBlockedChanged;
        }

        private void OnTargetChanged(IInteractable _) => Refresh();
        private void OnBlockedChanged(bool _) => Refresh();

        private void Refresh()
        {
            IInteractable target = detector != null ? detector.Current : null;
            bool blocked = gate != null && gate.IsBlocked;
            bool show = target != null && !blocked;
            bool touch = touchVisibility != null && touchVisibility.TouchVisible;

            if (promptRoot != null) promptRoot.SetActive(show);
            if (show && promptLabel != null) promptLabel.text = touch ? target.PromptText : keyHint + "  " + target.PromptText;
            if (touchInteractButton != null) touchInteractButton.alpha = target != null || blocked ? 1f : dimmedAlpha;
        }
    }
}

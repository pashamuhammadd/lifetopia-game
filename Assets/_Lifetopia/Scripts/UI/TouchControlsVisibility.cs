using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lifetopia.UI
{
    /// <summary>
    /// Decides whether on-screen touch controls are shown.
    /// Auto: shown on mobile browsers/devices, when the page URL contains "touch=1" (desktop
    /// testing), or from the first real finger press onward. The mere presence of a Touchscreen
    /// device is NOT used: Unity Web registers one even on desktop browsers without touch.
    /// The keyboard hint is shown only while touch controls are hidden.
    /// </summary>
    public sealed class TouchControlsVisibility : MonoBehaviour
    {
        public enum Mode { Auto, AlwaysShow, AlwaysHide }

        [SerializeField] private Mode mode = Mode.Auto;
        [SerializeField] private GameObject touchControls;
        [SerializeField] private GameObject keyboardHint;

        private bool _touchSeen;

        /// <summary>Raised whenever <see cref="TouchVisible"/> changes.</summary>
        public event Action<bool> VisibilityChanged;

        public bool TouchVisible { get; private set; }

        private void OnEnable() => Evaluate();

        private void Update()
        {
            // Only polls while controls are hidden in Auto mode; one bool read per frame, no allocation.
            if (mode != Mode.Auto || TouchVisible) return;
            Touchscreen touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                _touchSeen = true;
                Evaluate();
            }
        }

        public void SetMode(Mode newMode)
        {
            mode = newMode;
            Evaluate();
        }

        private void Evaluate()
        {
            bool show = mode switch
            {
                Mode.AlwaysShow => true,
                Mode.AlwaysHide => false,
                _ => Application.isMobilePlatform || _touchSeen || UrlForcesTouch()
            };

            if (touchControls != null) touchControls.SetActive(show);
            if (keyboardHint != null) keyboardHint.SetActive(!show);
            if (show == TouchVisible) return;
            TouchVisible = show;
            VisibilityChanged?.Invoke(show);
        }

        private static bool UrlForcesTouch()
        {
            string url = Application.absoluteURL;
            return !string.IsNullOrEmpty(url) && url.Contains("touch=1");
        }
    }
}

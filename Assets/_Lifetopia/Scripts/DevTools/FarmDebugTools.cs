using Lifetopia.Farming;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Lifetopia.DevTools
{
    /// <summary>
    /// DEVELOPMENT ONLY: Shift+F9 adds 30 s of growth to every watered crop (the time keys speed up the
    /// clock, but crops grow on active play seconds, so they need their own shortcut).
    /// Compiles to an empty component in release builds. Inventory dev keys (F1 = +1 seeds) still apply.
    /// </summary>
    public sealed class FarmDebugTools : MonoBehaviour
    {
#pragma warning disable 0169, 0414, 0649 // fields are only read in Editor/Development builds
        [SerializeField] private FarmFieldHost farm;
        [SerializeField] private bool showHelp = true;
        [SerializeField, Min(1f)] private float growStepSeconds = 30f;
#pragma warning restore 0169, 0414, 0649

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const string Help = "DEV FARM  Shift+F9 +30 s crop growth   (F1 +1 Seeds)";

        private void Update()
        {
            Keyboard k = Keyboard.current;
            if (k == null || farm == null || !k.shiftKey.isPressed) return;
            if (k.f9Key.wasPressedThisFrame) farm.DebugAdvanceGrowth(growStepSeconds);
        }

        private void OnGUI()
        {
            if (!showHelp || farm == null) return;
            GUI.Label(new Rect(24, 124, Screen.width - 48, 40), Help);
        }
#endif
    }
}

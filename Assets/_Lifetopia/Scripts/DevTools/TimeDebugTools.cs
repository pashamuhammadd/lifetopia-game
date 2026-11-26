using Lifetopia.TimeSystem;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Lifetopia.DevTools
{
    /// <summary>
    /// DEVELOPMENT ONLY: Shift + function keys to jump/speed up game time. Not a player feature.
    /// The body compiles only in the Editor and Development Builds; in release builds this component
    /// is empty. (Inventory dev keys use the same F-keys without Shift.)
    /// </summary>
    public sealed class TimeDebugTools : MonoBehaviour
    {
#pragma warning disable 0169, 0414, 0649 // fields are only read in Editor/Development builds
        [SerializeField] private GameClockHost clockHost;
        [SerializeField] private bool showHelp = true;
#pragma warning restore 0169, 0414, 0649

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const string Help =
            "DEV TIME  Shift+F1 06:00 sunrise   Shift+F2 12:00   Shift+F3 18:00 sunset   Shift+F4 22:00 night   " +
            "Shift+F5 23:58   Shift+F6 +1 hour   Shift+F7 speed x1/x10/x60   Shift+F8 pause";

        private static readonly double[] Speeds = { 1, 10, 60 };
        private int _speedIndex;
        private bool _devPaused;

        private void Update()
        {
            Keyboard k = Keyboard.current;
            if (k == null || clockHost == null || !k.shiftKey.isPressed) return;
            if (k.f1Key.wasPressedThisFrame) Jump(6 * 60);
            if (k.f2Key.wasPressedThisFrame) Jump(12 * 60);
            if (k.f3Key.wasPressedThisFrame) Jump(18 * 60);
            if (k.f4Key.wasPressedThisFrame) Jump(22 * 60);
            if (k.f5Key.wasPressedThisFrame) Jump(23 * 60 + 58);
            if (k.f6Key.wasPressedThisFrame) clockHost.Runner.Clock.AdvanceGameMinutes(60);
            if (k.f7Key.wasPressedThisFrame) CycleSpeed();
            if (k.f8Key.wasPressedThisFrame) TogglePause();
        }

        /// <summary>Sets the time of day on the current day.</summary>
        public void Jump(int minuteOfDay) => clockHost.Runner.Clock.SetTime(clockHost.Clock.Day, minuteOfDay);

        public void CycleSpeed()
        {
            _speedIndex = (_speedIndex + 1) % Speeds.Length;
            clockHost.Runner.Speed = Speeds[_speedIndex];
        }

        public void TogglePause()
        {
            _devPaused = !_devPaused;
            if (_devPaused) clockHost.PauseClock(this);
            else clockHost.ResumeClock(this);
        }

        private void OnGUI()
        {
            if (!showHelp || clockHost == null) return;
            GUI.Label(new Rect(24, 104, Screen.width - 48, 40),
                Help + $"   [speed x{clockHost.Runner.Speed:0}{(clockHost.IsPaused ? ", paused" : "")}]");
        }
#endif
    }
}

using Lifetopia.Controls;
using Lifetopia.Core;
using UnityEngine;

namespace Lifetopia.TimeSystem
{
    /// <summary>
    /// Scene component that owns the game clock for this play session and advances it once per frame
    /// with scaled frame time (so <c>Time.timeScale = 0</c> also stops it).
    /// Time pauses while:
    /// - the browser tab/window or app loses focus (resumes when focus returns),
    /// - a time-pausing modal (inventory, dialogue) is open — <see cref="GameplayInputGate.PausesGameTime"/>.
    ///   Activities that only block movement (fishing) do NOT pause time,
    /// - any other system calls <see cref="PauseClock"/>.
    /// Gameplay code reads time through <see cref="Clock"/> (<see cref="IGameClock"/>).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class GameClockHost : MonoBehaviour
    {
        [SerializeField] private GameClockConfig config;
        [Tooltip("Optional. While a time-pausing modal is open (inventory, dialogue) the clock is paused; movement-only blocks (fishing) keep time running.")]
        [SerializeField] private GameplayInputGate gate;
        [SerializeField] private bool pauseWhileModalOpen = true;
        [SerializeField] private bool pauseOnFocusLoss = true;

        private static readonly object FocusSource = new object();
        private GameTimeRunner _runner;
        private bool _subscribed;

        /// <summary>Read-only clock for gameplay systems.</summary>
        public IGameClock Clock => Runner.Clock;

        /// <summary>Full runner (pause sources, speed). For tools and the future save system.</summary>
        public GameTimeRunner Runner
        {
            get
            {
                if (_runner == null) Create();
                return _runner;
            }
        }

        public bool IsPaused => Runner.Pause.IsPaused;

        private void Awake()
        {
            if (_runner == null) Create();
        }

        private void OnEnable()
        {
            if (_subscribed) return;
            WebFocusBridge.FocusLost += OnFocusLost;
            WebFocusBridge.FocusGained += OnFocusGained;
            if (gate != null)
            {
                gate.TimePauseChanged += OnGateChanged;
                OnGateChanged(gate.PausesGameTime);
            }
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed) return;
            WebFocusBridge.FocusLost -= OnFocusLost;
            WebFocusBridge.FocusGained -= OnFocusGained;
            if (gate != null) gate.TimePauseChanged -= OnGateChanged;
            _subscribed = false;
        }

        private void Update() => Runner.Tick(Time.deltaTime);

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) OnFocusGained();
            else OnFocusLost();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) OnFocusLost();
            else OnFocusGained();
        }

        /// <summary>Pause game time on behalf of <paramref name="source"/> (e.g. a pause menu). Call <see cref="ResumeClock"/> with the same source.</summary>
        public void PauseClock(Object source) => Runner.Pause.Pause(source);

        public void ResumeClock(Object source) => Runner.Pause.Resume(source);

        /// <summary>Snapshot for the future save system.</summary>
        public GameClockData CaptureState() => Runner.Clock.ToData();

        /// <summary>Restores a saved snapshot; returns false (and keeps the current time) if the data is invalid.</summary>
        public bool TryRestoreState(GameClockData data, out string error) => Runner.Clock.TryRestore(data, out error);

        private void Create()
        {
            GameClockSettings settings = config != null ? config.Settings : GameClockSettings.Default;
            _runner = new GameTimeRunner(settings);
            if (config == null) Debug.LogWarning("[Lifetopia] GameClockHost has no GameClockConfig; using defaults.", this);
            else if (!_runner.Clock.SettingsWereValid)
                Debug.LogWarning($"[Lifetopia] Invalid GameClockConfig ({_runner.Clock.SettingsError}); using defaults.", this);
        }

        private void OnFocusLost()
        {
            if (pauseOnFocusLoss) Runner.Pause.Pause(FocusSource);
        }

        private void OnFocusGained() => Runner.Pause.Resume(FocusSource);

        private void OnGateChanged(bool pausesTime)
        {
            if (gate == null) return;
            Runner.Pause.Set(gate, pauseWhileModalOpen && pausesTime);
        }
    }
}

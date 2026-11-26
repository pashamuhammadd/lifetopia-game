using System;
using System.Collections.Generic;

namespace Lifetopia.TimeSystem
{
    /// <summary>
    /// Tracks who is pausing game time (focus loss, open inventory/dialogue, a pause menu, dev tools).
    /// Time runs only when no source is pausing; each source must resume itself.
    /// </summary>
    public sealed class TimePauseState
    {
        private readonly HashSet<object> _sources = new HashSet<object>();

        /// <summary>Raised with true when the first source pauses and false when the last resumes.</summary>
        public event Action<bool> PausedChanged;

        public bool IsPaused => _sources.Count > 0;
        public int SourceCount => _sources.Count;

        public bool IsPausedBy(object source) => source != null && _sources.Contains(source);

        public void Pause(object source)
        {
            if (source == null) return;
            bool was = IsPaused;
            _sources.Add(source);
            if (!was && IsPaused) PausedChanged?.Invoke(true);
        }

        public void Resume(object source)
        {
            if (source == null) return;
            bool was = IsPaused;
            _sources.Remove(source);
            if (was && !IsPaused) PausedChanged?.Invoke(false);
        }

        public void Set(object source, bool paused)
        {
            if (paused) Pause(source);
            else Resume(source);
        }
    }

    /// <summary>
    /// Clock + pause sources + speed: the whole time logic without MonoBehaviour, so it can be unit tested.
    /// <see cref="GameClockHost"/> calls <see cref="Tick"/> once per frame.
    /// </summary>
    public sealed class GameTimeRunner
    {
        private double _speed = 1.0;

        public GameTimeRunner(GameClockSettings settings)
        {
            Clock = new GameClock(settings);
            Pause = new TimePauseState();
        }

        public GameClock Clock { get; }
        public TimePauseState Pause { get; }

        /// <summary>Time multiplier (1 = normal). Development tools only; clamped to 0..1000.</summary>
        public double Speed
        {
            get => _speed;
            set => _speed = double.IsNaN(value) ? 1.0 : Math.Max(0.0, Math.Min(1000.0, value));
        }

        /// <summary>Advances the clock by one frame of real time unless something is pausing it.</summary>
        public void Tick(double realDeltaSeconds)
        {
            if (Pause.IsPaused || _speed <= 0) return;
            Clock.Advance(realDeltaSeconds, _speed);
        }
    }
}

using System;

namespace Lifetopia.Fishing
{
    public enum FishingState
    {
        Idle = 0,
        Casting,
        WaitingForBite,
        Timing,
        Success,
        Failure,
        Cancelled,
    }

    public enum FishingOutcome
    {
        None = 0,
        Caught,
        /// <summary>Pressed before the bite (scared the fish).</summary>
        TooEarly,
        /// <summary>Pressed during the timing window while the marker was outside the zone.</summary>
        Missed,
        /// <summary>No press before the attempt time limit.</summary>
        TooLate,
        Cancelled,
    }

    /// <summary>
    /// One fishing attempt (plain C#, unit-tested): Idle → Casting → WaitingForBite → Timing →
    /// Success / Failure / Cancelled → (Reset) Idle.
    /// - Time only advances through <see cref="Tick"/>; each step is clamped to
    ///   <see cref="FishingSettings.maxStepSeconds"/> so a hitch or a resumed tab cannot "catch up".
    ///   The caller simply does not tick while the game is paused or unfocused.
    /// - The whole attempt is limited to <see cref="FishingSettings.maxAttemptSeconds"/> (GDD 8 s).
    /// - One press decides: before the bite = TooEarly, in the zone = Caught, outside = Missed.
    /// - Every attempt has a new <see cref="AttemptId"/>; a finished attempt never changes again.
    /// </summary>
    public sealed class FishingSession
    {
        private readonly FishingSettings _s;
        private float _timingTime;

        public FishingSession(FishingSettings settings)
        {
            if (!settings.Validate(out string error)) throw new ArgumentException("Invalid fishing settings: " + error);
            _s = settings;
        }

        public FishingSettings Settings => _s;
        public FishingState State { get; private set; } = FishingState.Idle;
        public FishingOutcome Outcome { get; private set; } = FishingOutcome.None;
        /// <summary>Increases with every started attempt (0 = none yet).</summary>
        public int AttemptId { get; private set; }
        /// <summary>Seconds since the attempt started (active, clamped time only).</summary>
        public float Elapsed { get; private set; }
        public float BiteDelay { get; private set; }
        /// <summary>Marker position on the bar, 0..1 (moves only during Timing).</summary>
        public float Marker { get; private set; }

        public bool IsActive => State == FishingState.Casting || State == FishingState.WaitingForBite || State == FishingState.Timing;
        public bool IsFinished => State == FishingState.Success || State == FishingState.Failure || State == FishingState.Cancelled;

        /// <summary>Raised after every state change.</summary>
        public event Action<FishingState> StateChanged;

        /// <summary>
        /// Starts a new attempt. <paramref name="biteDelay"/> is clamped into the configured range
        /// (the caller supplies the random value, which keeps this class deterministic).
        /// Returns false while an attempt is still running (no re-entry).
        /// </summary>
        public bool Begin(float biteDelay)
        {
            if (IsActive) return false;
            AttemptId++;
            Elapsed = 0f;
            _timingTime = 0f;
            Marker = 0f;
            Outcome = FishingOutcome.None;
            BiteDelay = Clamp(biteDelay, _s.biteDelayMin, _s.biteDelayMax);
            SetState(_s.castSeconds > 0f ? FishingState.Casting : FishingState.WaitingForBite);
            return true;
        }

        /// <summary>Advances the attempt by <paramref name="deltaSeconds"/> of active time (clamped).</summary>
        public void Tick(float deltaSeconds)
        {
            if (!IsActive) return;
            if (!(deltaSeconds > 0f) || float.IsInfinity(deltaSeconds)) return;
            float dt = deltaSeconds > _s.maxStepSeconds ? _s.maxStepSeconds : deltaSeconds;
            Elapsed += dt;

            if (State == FishingState.Casting && Elapsed >= _s.castSeconds)
                SetState(FishingState.WaitingForBite);
            if (State == FishingState.WaitingForBite && Elapsed >= _s.castSeconds + BiteDelay)
            {
                _timingTime = Elapsed - (_s.castSeconds + BiteDelay);
                SetState(FishingState.Timing);
            }
            else if (State == FishingState.Timing)
            {
                _timingTime += dt;
            }
            if (State == FishingState.Timing) Marker = PingPong(_timingTime * _s.markerCyclesPerSecond);

            if (Elapsed >= _s.maxAttemptSeconds && IsActive) Finish(FishingState.Failure, FishingOutcome.TooLate);
        }

        /// <summary>The single "reel in" press. Ignored when no attempt is running.</summary>
        public FishingOutcome Press()
        {
            switch (State)
            {
                case FishingState.Casting:
                case FishingState.WaitingForBite:
                    Finish(FishingState.Failure, FishingOutcome.TooEarly);
                    return Outcome;
                case FishingState.Timing:
                    if (InZone(Marker)) Finish(FishingState.Success, FishingOutcome.Caught);
                    else Finish(FishingState.Failure, FishingOutcome.Missed);
                    return Outcome;
                default:
                    return FishingOutcome.None;
            }
        }

        public void Cancel()
        {
            if (IsActive) Finish(FishingState.Cancelled, FishingOutcome.Cancelled);
        }

        /// <summary>Back to Idle after a finished attempt (keeps <see cref="AttemptId"/>).</summary>
        public void Reset()
        {
            if (IsActive) return;
            if (State != FishingState.Idle) SetState(FishingState.Idle);
        }

        public bool InZone(float marker) => marker >= _s.ZoneMin && marker <= _s.ZoneMax;

        /// <summary>0 → 1 → 0 over one cycle (triangle wave).</summary>
        public static float PingPong(float cycles)
        {
            float f = cycles - (float)Math.Floor(cycles);
            return f < 0.5f ? f * 2f : 2f - f * 2f;
        }

        private void Finish(FishingState state, FishingOutcome outcome)
        {
            Outcome = outcome;
            SetState(state);
        }

        private void SetState(FishingState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }

        private static float Clamp(float v, float min, float max) => float.IsNaN(v) ? min : v < min ? min : v > max ? max : v;
    }
}

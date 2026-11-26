using System.Collections.Generic;
using Lifetopia.Controls;
using Lifetopia.Core;
using Lifetopia.Interaction;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using Lifetopia.UI;
using UnityEngine;

namespace Lifetopia.Fishing
{
    /// <summary>
    /// Runs fishing attempts in the scene and connects the testable <see cref="FishingSession"/> to input,
    /// inventory, the modal gate, the world view and the panel.
    /// - Start: rod selected and owned, room for the catch, not busy. Then the gameplay gate is blocked for
    ///   movement + world interaction only: game time (and crop growth) KEEPS RUNNING while fishing
    ///   (founder decision M3.4). Focus loss / pause still stop game time as before.
    /// - Mini-game timer: its own clock driven by <c>Time.deltaTime</c> (stops at timeScale 0), clamped per step,
    ///   and NOT advanced while the app is unfocused/hidden. It is not tied to the global modal pause.
    /// - Reel in: E / touch Interact (arrive as UiSubmit while blocked) or the panel button; ignored on the
    ///   frame the attempt started. Cancel: Escape or the panel button.
    /// - Every exit path (result shown, cancel, disable, destroy) releases the gate.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FishingController : MonoBehaviour
    {
        [SerializeField] private FishingConfig config;
        [SerializeField] private PlayerProfileHost profileHost;
        [SerializeField] private GameplayInputGate gate;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private InteractionDetector detector;
        [SerializeField] private Transform player;
        [SerializeField] private FishingView view;
        [SerializeField] private FishingPanelUI panel;
        [SerializeField] private FishingSpot[] spots = new FishingSpot[0];

        private FishingSession _session;
        private readonly FishingRewards _rewards = new FishingRewards();
        private PlayerProfile _profile;
        private bool _focusLost;
        private int _beginFrame = -1;
        private float _resultTimer = -1f;
        private bool _blocked;
        private bool _started;
        private System.Random _random;

        public bool IsFishing => _blocked;
        public FishingSession Session => _session;
        public FishingRewards Rewards => _rewards;
        /// <summary>Message of the last result for tests/tools (e.g. "Caught a Reef Fish!").</summary>
        public string LastStatus { get; private set; } = "";

        private void Start()
        {
            if (config == null || profileHost == null || gate == null)
            {
                Debug.LogError("[Lifetopia] FishingController is missing references; fishing disabled.", this);
                enabled = false;
                return;
            }
            var problems = new List<string>();
            config.Validate(problems);
            foreach (string p in problems) Debug.LogWarning("[Lifetopia] " + p, this);
            if (!config.Settings.Validate(out _)) { enabled = false; return; }

            _session = new FishingSession(config.Settings);
            _session.StateChanged += OnStateChanged;
            _random = new System.Random(System.Environment.TickCount);
            _profile = profileHost.Profile;
            _profile.Hotbar.SelectionChanged += OnSelectionChanged;
            _profile.Hotbar.SlotChanged += OnSelectionChanged;
            _profile.Inventory.Changed += RaisePrompts;
            if (input != null)
            {
                input.UiSubmitPerformed += OnSubmit;
                input.CancelPerformed += OnCancel;
            }
            if (detector != null) detector.CurrentChanged += OnTargetChanged;
            if (panel != null) panel.Bind(this);
            WebFocusBridge.FocusLost += OnFocusLost;
            WebFocusBridge.FocusGained += OnFocusGained;
            _started = true;
        }

        private void OnDisable()
        {
            if (_session != null && _session.IsActive) _session.Cancel();
            EndAttempt();
        }

        private void OnDestroy()
        {
            if (!_started) return;
            if (_profile != null)
            {
                _profile.Hotbar.SelectionChanged -= OnSelectionChanged;
                _profile.Hotbar.SlotChanged -= OnSelectionChanged;
                _profile.Inventory.Changed -= RaisePrompts;
            }
            if (input != null)
            {
                input.UiSubmitPerformed -= OnSubmit;
                input.CancelPerformed -= OnCancel;
            }
            if (detector != null) detector.CurrentChanged -= OnTargetChanged;
            WebFocusBridge.FocusLost -= OnFocusLost;
            WebFocusBridge.FocusGained -= OnFocusGained;
        }

        public string PromptFor(FishingSpot spot)
        {
            if (_profile == null || config == null) return "";
            ItemDefinition selected = _profile.Hotbar.SelectedAvailableItem;
            return selected != null && selected == config.RodItem ? FishingText.PromptFish : FishingText.PromptNeedRod;
        }

        /// <summary>Starts an attempt at <paramref name="spot"/>; explains in a toast when it cannot.</summary>
        public FishingStartResult TryBegin(FishingSpot spot)
        {
            if (_session == null || spot == null) return FishingStartResult.InvalidConfig;
            if (_blocked) return FishingStartResult.Busy;
            if (gate.IsBlocked) return FishingStartResult.Busy; // another modal is open
            FishingStartResult check = FishingRewards.CanStart(config, _profile.Hotbar.SelectedAvailableItem, _profile.Inventory, _session);
            if (check != FishingStartResult.Ok)
            {
                profileHost.Notify(FishingText.StartMessage(check, config.FishItem));
                return check;
            }
            FishingSettings s = config.Settings;
            float delay = s.biteDelayMin + (float)_random.NextDouble() * (s.biteDelayMax - s.biteDelayMin);
            if (!_session.Begin(delay)) return FishingStartResult.Busy;
            _beginFrame = Time.frameCount;
            _resultTimer = -1f;
            gate.Block(this, pausesGameTime: false); // movement lock only; game time keeps running
            _blocked = true;
            if (view != null) view.Begin(player, spot.BobberAnchor);
            if (panel != null)
            {
                panel.SetFish(config.FishItem);
                panel.Open();
            }
            OnStateChanged(_session.State);
            RaisePrompts();
            return FishingStartResult.Ok;
        }

        /// <summary>The reel-in press (keyboard, touch or panel button).</summary>
        public void Press()
        {
            if (_session == null || !_session.IsActive) return;
            if (Time.frameCount == _beginFrame) return; // the press that opened fishing never reels in
            _session.Press();
        }

        public void Cancel()
        {
            if (_session == null) return;
            if (_session.IsActive) _session.Cancel();
            else if (_session.IsFinished) _resultTimer = 0f; // close the result early
        }

        /// <summary>Test hook: advance the mini game as if <paramref name="seconds"/> of focused play passed.</summary>
        public void DebugTick(float seconds)
        {
            while (seconds > 0f && _session != null && _session.IsActive)
            {
                float step = Mathf.Min(seconds, config.Settings.maxStepSeconds);
                _session.Tick(step);
                seconds -= step;
            }
            Refresh();
        }

        private void Update()
        {
            if (_session == null) return;
            if (_session.IsActive && !_focusLost) _session.Tick(Time.deltaTime);
            Refresh();
            if (_resultTimer >= 0f && !_focusLost)
            {
                _resultTimer -= Time.deltaTime;
                if (_resultTimer <= 0f) EndAttempt();
            }
        }

        private void Refresh()
        {
            if (panel != null && _blocked) panel.Show(_session, LastStatus);
        }

        private void OnStateChanged(FishingState state)
        {
            if (view != null) view.OnState(state);
            string fishName = config.FishItem != null ? config.FishItem.DisplayName : "fish";
            LastStatus = FishingText.Status(state, _session.Outcome, fishName);
            if (state == FishingState.Success)
            {
                FishingAwardResult award = _rewards.TryAward(_session, config, _profile.Inventory);
                if (award == FishingAwardResult.InventoryFull)
                    LastStatus = "Inventory full. The fish got away.";
                if (award == FishingAwardResult.Awarded)
                    profileHost.Notify($"+{config.CatchQuantity} {fishName}");
                else if (award == FishingAwardResult.InventoryFull)
                    profileHost.Notify(LastStatus);
            }
            if (_session.IsFinished) _resultTimer = config.ResultSeconds;
            Refresh();
        }

        private void EndAttempt()
        {
            _resultTimer = -1f;
            if (_session != null && !_session.IsActive) _session.Reset();
            if (view != null) view.HideAll();
            if (panel != null) panel.Close();
            if (_blocked && gate != null) gate.Unblock(this);
            _blocked = false;
            RaisePrompts();
        }

        private void OnSubmit()
        {
            if (_blocked) Press();
        }

        private void OnCancel()
        {
            if (_blocked) Cancel();
        }

        private void OnFocusLost() => _focusLost = true;
        private void OnFocusGained() => _focusLost = false;

        private void OnApplicationFocus(bool hasFocus) => _focusLost = !hasFocus;

        private void OnApplicationPause(bool paused)
        {
            if (paused) _focusLost = true;
        }

        private void OnSelectionChanged(int _) => RaisePrompts();

        private void RaisePrompts()
        {
            foreach (FishingSpot s in spots) if (s != null) s.RaisePromptChanged();
        }

        private void OnTargetChanged(IInteractable target)
        {
            foreach (FishingSpot s in spots) if (s != null) s.SetHighlighted(ReferenceEquals(s, target));
        }
    }
}

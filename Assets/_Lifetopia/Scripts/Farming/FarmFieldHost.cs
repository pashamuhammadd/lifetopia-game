using Lifetopia.Interaction;
using Lifetopia.InventorySystem;
using Lifetopia.Items;
using Lifetopia.TimeSystem;
using UnityEngine;

namespace Lifetopia.Farming
{
    /// <summary>
    /// Scene component that owns the <see cref="FarmField"/> state for this session and connects it to
    /// the player profile (inventory + hotbar), the game clock and the plot views.
    /// - Growth time = <see cref="IGameClock.ActivePlaySeconds"/>: stops whenever the clock is paused
    ///   (bag/dialogue open, focus lost, timeScale 0). No wall-clock, no offline growth.
    /// - Views are refreshed on clock MinuteChanged (event-driven, no per-plot per-frame timers).
    /// - Messages go through <see cref="PlayerProfileHost.Notify"/> (HUD toast).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FarmFieldHost : MonoBehaviour
    {
        [SerializeField] private FarmingConfig config;
        [Tooltip("Optional. Local artwork set; empty = generated placeholder shapes.")]
        [SerializeField] private FarmVisualSet visuals;
        [SerializeField] private GameClockHost clockHost;
        [SerializeField] private PlayerProfileHost profileHost;
        [Tooltip("Optional: highlights the plot the player is targeting.")]
        [SerializeField] private InteractionDetector detector;
        [SerializeField] private FarmPlotView[] plots = new FarmPlotView[FarmField.DefaultPlotCount];

        private FarmField _field;
        private IGameClock _clock;
        private PlayerProfile _profile;
        private int[] _shownKey;
        private bool _started;

        /// <summary>Runtime state (for tests, tools and the future local save).</summary>
        public FarmField Field => _field;
        public FarmVisualSet Visuals => visuals;
        public double Now => _clock != null ? _clock.ActivePlaySeconds : 0;

        private void Start()
        {
            if (config == null || clockHost == null || profileHost == null || plots == null || plots.Length == 0)
            {
                Debug.LogError("[Lifetopia] FarmFieldHost is missing references; farming disabled.", this);
                enabled = false;
                return;
            }
            var problems = new System.Collections.Generic.List<string>();
            config.Validate(problems);
            foreach (string p in problems) Debug.LogWarning("[Lifetopia] " + p, this);

            _field = new FarmField(plots.Length);
            _shownKey = new int[plots.Length];
            _clock = clockHost.Clock;
            _profile = profileHost.Profile;
            for (int i = 0; i < plots.Length; i++)
            {
                if (plots[i] == null) { Debug.LogError($"[Lifetopia] Farm plot {i} is not assigned.", this); continue; }
                plots[i].Bind(this, i);
                _shownKey[i] = int.MinValue;
                FarmPlot plot = _field.Get(i);
                int index = i;
                plot.Changed += () => RefreshPlot(index, true);
            }

            _clock.MinuteChanged += OnMinute;
            _profile.Hotbar.SelectionChanged += OnSelectionChanged;
            _profile.Hotbar.SlotChanged += OnSelectionChanged;
            _profile.Inventory.Changed += OnInventoryChanged;
            if (detector != null) detector.CurrentChanged += OnTargetChanged;
            _started = true;
            RefreshAll(true);
        }

        private void OnDestroy()
        {
            if (!_started) return;
            if (_clock != null) _clock.MinuteChanged -= OnMinute;
            if (_profile != null)
            {
                _profile.Hotbar.SelectionChanged -= OnSelectionChanged;
                _profile.Hotbar.SlotChanged -= OnSelectionChanged;
                _profile.Inventory.Changed -= OnInventoryChanged;
            }
            if (detector != null) detector.CurrentChanged -= OnTargetChanged;
        }

        /// <summary>Prompt for plot <paramref name="index"/> based on the selected hotbar item.</summary>
        public string GetPrompt(int index)
        {
            FarmPlot plot = _field?.Get(index);
            if (plot == null) return "";
            double now = Now;
            ItemDefinition selected = SelectedItem;
            FarmAction action = FarmActions.Resolve(plot, selected, config, now);
            if (action == FarmAction.Plant && !_profile.Inventory.HasItem(selected)) return "No seeds left";
            return FarmText.Prompt(action, plot, plot.GetStage(now), plot.GetSecondsLeft(now));
        }

        /// <summary>Interact on plot <paramref name="index"/> with the selected hotbar item.</summary>
        public FarmResult Act(int index)
        {
            FarmPlot plot = _field?.Get(index);
            if (plot == null) return FarmResult.InvalidPlot;
            ItemDefinition selected = SelectedItem;
            CropDefinition cropBefore = plot.Crop;
            FarmResult result = FarmActions.Perform(plot, selected, config, _profile.Inventory, Now, out FarmAction action);
            if (result == FarmResult.Success)
            {
                plots[index].Pulse();
                if (action == FarmAction.Harvest && cropBefore != null)
                    profileHost.Notify($"+{cropBefore.HarvestQuantity} {cropBefore.HarvestItem.DisplayName}");
            }
            else
            {
                profileHost.Notify(FarmText.Message(result, plot));
            }
            RefreshPlot(index, true);
            return result;
        }

        /// <summary>Save hook for the future local save (not persisted in M3.3).</summary>
        public FarmFieldData CaptureState() => _field?.ToData(Now);

        public bool TryRestoreState(FarmFieldData data, out string error)
        {
            if (_field == null) { error = "Farm not started."; return false; }
            bool ok = _field.TryLoad(data, config, Now, out error);
            if (ok) RefreshAll(true);
            return ok;
        }

        /// <summary>Development tools only: adds <paramref name="seconds"/> of growth to watered crops.</summary>
        public void DebugAdvanceGrowth(double seconds)
        {
            if (_field == null) return;
            for (int i = 0; i < _field.Count; i++) _field.Get(i).ShiftGrowth(seconds);
        }

        /// <summary>
        /// Selected hotbar item. A seed with 0 left is still returned so the player is told "no seeds left"
        /// (planting checks the inventory and consumes nothing); a tool with 0 left counts as nothing selected.
        /// </summary>
        private ItemDefinition SelectedItem
        {
            get
            {
                if (_profile == null) return null;
                ItemDefinition item = _profile.Hotbar.SelectedItem;
                if (item == null || _profile.Hotbar.IsAvailable(_profile.Hotbar.SelectedIndex)) return item;
                return config.CropForSeed(item) != null ? item : null;
            }
        }

        private void OnMinute() => RefreshAll(false);
        private void OnSelectionChanged(int _) => RaiseAllPrompts();
        private void OnInventoryChanged() => RaiseAllPrompts();

        private void OnTargetChanged(IInteractable target)
        {
            for (int i = 0; i < plots.Length; i++)
                if (plots[i] != null) plots[i].SetHighlighted(ReferenceEquals(plots[i], target));
        }

        private void RaiseAllPrompts()
        {
            for (int i = 0; i < plots.Length; i++) plots[i]?.RaisePromptChanged();
        }

        private void RefreshAll(bool force)
        {
            for (int i = 0; i < plots.Length; i++) RefreshPlot(i, force);
        }

        /// <summary>Applies sprites only when the visual state changed; growing plots also update their prompt countdown.</summary>
        private void RefreshPlot(int index, bool force)
        {
            FarmPlotView view = plots[index];
            FarmPlot plot = _field?.Get(index);
            if (view == null || plot == null) return;
            double now = Now;
            FarmPlotStage stage = plot.GetStage(now);
            float youngAt = visuals != null ? visuals.YoungAtFraction : 0.5f;
            int visualIndex = FarmVisualSet.VisualStageIndex(stage, plot.GetGrowthFraction(now), youngAt);
            bool wet = stage == FarmPlotStage.Watered || stage == FarmPlotStage.Ready;
            int key = (plot.Tilled ? 1 : 0) | (wet ? 2 : 0) | ((visualIndex + 1) << 2);

            if (force || key != _shownKey[index])
            {
                _shownKey[index] = key;
                Sprite soil = visuals != null ? visuals.Soil(plot.Tilled, wet) : null;
                if (soil == null) soil = FarmPlaceholderSprites.Soil(plot.Tilled, wet);
                Sprite crop = null;
                float cropScale = 1f;
                if (visualIndex >= 0)
                {
                    float fraction = plot.GetGrowthFraction(now);
                    crop = visuals != null && plot.Crop != null ? visuals.Crop(plot.Crop.CropId, stage, fraction) : null;
                    if (crop != null) cropScale = visuals.CropScale(plot.Crop.CropId, stage, fraction);
                    else crop = FarmPlaceholderSprites.Crop(visualIndex); // placeholder shapes already grow per stage
                }
                Sprite indicator = null;
                if (stage == FarmPlotStage.Planted)
                    indicator = visuals != null && visuals.NeedsWaterIndicator != null ? visuals.NeedsWaterIndicator : FarmPlaceholderSprites.NeedsWater;
                else if (stage == FarmPlotStage.Ready)
                    indicator = visuals != null && visuals.ReadyIndicator != null ? visuals.ReadyIndicator : FarmPlaceholderSprites.Ready;
                float scale = visuals != null && indicator != null && (indicator == visuals.NeedsWaterIndicator || indicator == visuals.ReadyIndicator)
                    ? visuals.IndicatorScale : 0.45f; // placeholder sprites are 64 px (1 unit)
                view.Show(soil, crop, cropScale, indicator, scale);
                view.RaisePromptChanged();
            }
            else if (stage == FarmPlotStage.Watered)
            {
                view.RaisePromptChanged(); // countdown text
            }
        }
    }
}

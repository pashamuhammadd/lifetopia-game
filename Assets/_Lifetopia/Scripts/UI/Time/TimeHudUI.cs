using Lifetopia.TimeSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Lifetopia.UI
{
    /// <summary>
    /// Small HUD panel: period icon, time in 24-hour format ("08:00"), day number and period name.
    /// Updates only on clock events (once per game minute), using cached strings.
    /// Icons: assign one sprite per period (Morning, Daytime, Sunset, Night) to replace the
    /// code-drawn coloured placeholder circle.
    /// </summary>
    public sealed class TimeHudUI : MonoBehaviour
    {
        [SerializeField] private GameClockHost clockHost;
        [SerializeField] private Text timeLabel;
        [SerializeField] private Text dayLabel;
        [SerializeField] private Text periodLabel;
        [SerializeField] private Image periodIcon;
        [Tooltip("Optional sprites in order: Morning, Daytime, Sunset, Night.")]
        [SerializeField] private Sprite[] periodSprites = new Sprite[4];

        private IGameClock _clock;
        private int _shownDay = -1;

        private void Start()
        {
            if (clockHost == null)
            {
                Debug.LogError("[Lifetopia] TimeHudUI needs a GameClockHost.", this);
                enabled = false;
                return;
            }
            _clock = clockHost.Clock;
            _clock.MinuteChanged += ShowTime;
            _clock.PeriodChanged += ShowPeriod;
            ShowTime();
            ShowPeriod(_clock.Period);
        }

        private void OnDestroy()
        {
            if (_clock == null) return;
            _clock.MinuteChanged -= ShowTime;
            _clock.PeriodChanged -= ShowPeriod;
        }

        private void ShowTime()
        {
            if (timeLabel != null) timeLabel.text = ClockText.Time24(_clock.MinuteOfDay);
            if (_clock.Day != _shownDay)
            {
                _shownDay = _clock.Day;
                if (dayLabel != null) dayLabel.text = ClockText.DayLabel(_shownDay);
            }
        }

        private void ShowPeriod(TimeOfDayPeriod period)
        {
            if (periodLabel != null) periodLabel.text = ClockText.PeriodName(period);
            if (periodIcon == null) return;
            int index = (int)period;
            Sprite art = periodSprites != null && index < periodSprites.Length ? periodSprites[index] : null;
            if (art != null)
            {
                periodIcon.sprite = art;
                periodIcon.type = Image.Type.Simple;
                periodIcon.preserveAspect = true;
                periodIcon.color = Color.white;
            }
            else
            {
                periodIcon.sprite = RoundedSprites.Get(16);
                periodIcon.type = Image.Type.Sliced;
                periodIcon.color = PlaceholderColor(period);
            }
        }

        /// <summary>Placeholder icon colours from the Art Bible palette.</summary>
        public static Color PlaceholderColor(TimeOfDayPeriod period)
        {
            switch (period)
            {
                case TimeOfDayPeriod.Morning: return UiPalette.Coral;
                case TimeOfDayPeriod.Daytime: return UiPalette.Sand;
                case TimeOfDayPeriod.Sunset: return UiPalette.Wood;
                default: return UiPalette.Lagoon;
            }
        }
    }
}

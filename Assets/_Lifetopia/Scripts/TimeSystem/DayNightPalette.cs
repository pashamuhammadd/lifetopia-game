using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.TimeSystem
{
    /// <summary>One colour at one minute of the day.</summary>
    [Serializable]
    public struct DayNightKey
    {
        [Range(0, GameClockSettings.MinutesPerDay - 1)] public int minuteOfDay;
        public Color color;

        public DayNightKey(int minuteOfDay, Color color)
        {
            this.minuteOfDay = minuteOfDay;
            this.color = color;
        }
    }

    /// <summary>
    /// Pure colour interpolation over the day. Keys are treated as a loop, so the colour between the
    /// last key (e.g. 23:00) and the first key (e.g. 03:00) blends smoothly across midnight.
    /// </summary>
    public static class DayNightMath
    {
        /// <param name="sortedKeys">Keys sorted by minute, distinct minutes, at least one.</param>
        /// <param name="minuteOfDay">0 ≤ minute &lt; 1440 (fractions allowed).</param>
        public static Color Evaluate(IReadOnlyList<DayNightKey> sortedKeys, double minuteOfDay)
        {
            if (sortedKeys == null || sortedKeys.Count == 0) return Color.white;
            if (sortedKeys.Count == 1) return sortedKeys[0].color;
            const double day = GameClockSettings.MinutesPerDay;
            minuteOfDay = ((minuteOfDay % day) + day) % day;

            int next = 0;
            while (next < sortedKeys.Count && sortedKeys[next].minuteOfDay <= minuteOfDay) next++;
            int prev = next - 1;
            if (prev < 0) prev = sortedKeys.Count - 1;      // before the first key: wrap to the last
            if (next >= sortedKeys.Count) next = 0;          // after the last key: wrap to the first

            double from = sortedKeys[prev].minuteOfDay;
            double to = sortedKeys[next].minuteOfDay;
            double span = ((to - from) % day + day) % day;
            if (span <= 0) span = day;
            double offset = ((minuteOfDay - from) % day + day) % day;
            float t = (float)(offset / span);
            return Color.Lerp(sortedKeys[prev].color, sortedKeys[next].color, Mathf.SmoothStep(0f, 1f, t));
        }

        /// <summary>Blends <paramref name="tint"/> toward white: 0 = no effect, 1 = full tint.</summary>
        public static Color ApplyStrength(Color tint, float strength) => Color.Lerp(Color.white, tint, Mathf.Clamp01(strength));
    }

    /// <summary>
    /// Inspector asset: the world tint over a day. White = no change; darker/bluer = night.
    /// The tint multiplies the world sprites only (UI is never tinted).
    /// Keep night colours bright enough to play (each channel ≥ ~0.5).
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Time/Day Night Palette", fileName = "DayNightPalette")]
    public sealed class DayNightPalette : ScriptableObject
    {
        [SerializeField] private List<DayNightKey> keys = DefaultKeys();
        [Tooltip("0 = no day/night tint, 1 = full tint.")]
        [SerializeField, Range(0f, 1f)] private float strength = 1f;

        private DayNightKey[] _sorted;

        public float Strength => strength;

        public Color Evaluate(double minuteOfDay)
        {
            if (_sorted == null) Sort();
            return DayNightMath.ApplyStrength(DayNightMath.Evaluate(_sorted, minuteOfDay), strength);
        }

        /// <summary>Default Chibi Tropical Cozy tints: warm dawn, neutral day, golden sunset, soft blue night.</summary>
        public static List<DayNightKey> DefaultKeys() => new List<DayNightKey>
        {
            new DayNightKey(3 * 60, new Color(0.60f, 0.64f, 0.86f)),        // deep night
            new DayNightKey(5 * 60, new Color(0.66f, 0.68f, 0.86f)),        // morning begins
            new DayNightKey(6 * 60 + 30, new Color(1.00f, 0.88f, 0.82f)),   // warm sunrise
            new DayNightKey(8 * 60, Color.white),                           // daytime
            new DayNightKey(16 * 60 + 30, Color.white),
            new DayNightKey(17 * 60 + 45, new Color(1.00f, 0.84f, 0.66f)),  // golden sunset
            new DayNightKey(18 * 60 + 45, new Color(0.92f, 0.72f, 0.72f)),  // pink dusk
            new DayNightKey(19 * 60 + 45, new Color(0.64f, 0.66f, 0.88f)),  // night
            new DayNightKey(23 * 60, new Color(0.60f, 0.64f, 0.86f)),
        };

        internal static DayNightPalette CreateRuntime(List<DayNightKey> keys, float strength = 1f)
        {
            var palette = CreateInstance<DayNightPalette>();
            palette.keys = keys;
            palette.strength = strength;
            palette.Sort();
            return palette;
        }

        private void OnEnable() => _sorted = null;
        private void OnValidate() => _sorted = null;

        private void Sort()
        {
            var list = new List<DayNightKey>(keys ?? new List<DayNightKey>());
            list.Sort((a, b) => a.minuteOfDay.CompareTo(b.minuteOfDay));
            // Drop duplicate minutes (keep the first) so interpolation spans are never zero.
            for (int i = list.Count - 1; i > 0; i--)
                if (list[i].minuteOfDay == list[i - 1].minuteOfDay) list.RemoveAt(i);
            _sorted = list.ToArray();
        }
    }
}

using UnityEngine;

namespace Lifetopia.TimeSystem
{
    /// <summary>
    /// Inspector asset holding the clock rules (start time, speed, period boundaries).
    /// Change <c>Real Seconds Per Game Hour</c> to make time faster or slower.
    /// Invalid values are reported and the GDD defaults are used instead.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Time/Game Clock Config", fileName = "GameClockConfig")]
    public sealed class GameClockConfig : ScriptableObject
    {
        [SerializeField] private GameClockSettings settings = GameClockSettings.Default;

        public GameClockSettings Settings => settings;

        private void OnValidate()
        {
            if (!settings.Validate(out string error))
                Debug.LogWarning($"[Lifetopia] {name}: {error} The default clock settings will be used.", this);
        }

        private void Reset() => settings = GameClockSettings.Default;
    }
}

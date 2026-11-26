using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.Core
{
    /// <summary>
    /// Blocks gameplay input (movement, world interaction) while any modal UI is open.
    /// Several sources may block at once; gameplay resumes only when all have released.
    /// Separately, each blocker says whether it also pauses GAME TIME:
    /// - menus such as the bag and dialogues pause time (default, M3.2 behaviour);
    /// - activities such as fishing block movement but let time (and crop growth) continue.
    /// The game clock listens to <see cref="TimePauseChanged"/>, never to <see cref="BlockedChanged"/>.
    /// </summary>
    public sealed class GameplayInputGate : MonoBehaviour
    {
        private readonly HashSet<UnityEngine.Object> _blockers = new HashSet<UnityEngine.Object>();
        private readonly HashSet<UnityEngine.Object> _timePausers = new HashSet<UnityEngine.Object>();

        /// <summary>Raised with true when the first blocker arrives and false when the last leaves.</summary>
        public event Action<bool> BlockedChanged;

        /// <summary>Raised with true when the first time-pausing blocker arrives and false when the last leaves.</summary>
        public event Action<bool> TimePauseChanged;

        public bool IsBlocked => _blockers.Count > 0;

        /// <summary>True while at least one blocker asked to pause game time.</summary>
        public bool PausesGameTime => _timePausers.Count > 0;

        /// <summary>True when something other than <paramref name="source"/> is blocking gameplay.</summary>
        public bool IsBlockedExcept(UnityEngine.Object source) =>
            _blockers.Count > (source != null && _blockers.Contains(source) ? 1 : 0);

        /// <summary>Blocks movement/world interaction and pauses game time (menus, dialogues).</summary>
        public void Block(UnityEngine.Object source) => Block(source, true);

        /// <summary>Blocks movement/world interaction; <paramref name="pausesGameTime"/> false keeps game time running.</summary>
        public void Block(UnityEngine.Object source, bool pausesGameTime)
        {
            if (source == null) return;
            bool wasBlocked = IsBlocked;
            bool wasPausing = PausesGameTime;
            _blockers.Add(source);
            if (pausesGameTime) _timePausers.Add(source);
            else _timePausers.Remove(source);
            if (!wasBlocked && IsBlocked) BlockedChanged?.Invoke(true);
            if (wasPausing != PausesGameTime) TimePauseChanged?.Invoke(PausesGameTime);
        }

        public void Unblock(UnityEngine.Object source)
        {
            if (source == null) return;
            bool wasBlocked = IsBlocked;
            bool wasPausing = PausesGameTime;
            _blockers.Remove(source);
            _timePausers.Remove(source);
            if (wasBlocked && !IsBlocked) BlockedChanged?.Invoke(false);
            if (wasPausing != PausesGameTime) TimePauseChanged?.Invoke(PausesGameTime);
        }
    }
}

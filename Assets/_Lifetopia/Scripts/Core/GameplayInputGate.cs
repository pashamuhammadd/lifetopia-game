using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.Core
{
    /// <summary>
    /// Blocks gameplay input (movement, world interaction) while any modal UI is open.
    /// Several sources may block at once; gameplay resumes only when all have released.
    /// </summary>
    public sealed class GameplayInputGate : MonoBehaviour
    {
        private readonly HashSet<UnityEngine.Object> _blockers = new HashSet<UnityEngine.Object>();

        /// <summary>Raised with true when the first blocker arrives and false when the last leaves.</summary>
        public event Action<bool> BlockedChanged;

        public bool IsBlocked => _blockers.Count > 0;

        /// <summary>True when something other than <paramref name="source"/> is blocking gameplay.</summary>
        public bool IsBlockedExcept(UnityEngine.Object source) =>
            _blockers.Count > (source != null && _blockers.Contains(source) ? 1 : 0);

        public void Block(UnityEngine.Object source)
        {
            if (source == null) return;
            bool wasBlocked = IsBlocked;
            _blockers.Add(source);
            if (!wasBlocked && IsBlocked) BlockedChanged?.Invoke(true);
        }

        public void Unblock(UnityEngine.Object source)
        {
            if (source == null) return;
            bool wasBlocked = IsBlocked;
            _blockers.Remove(source);
            if (wasBlocked && !IsBlocked) BlockedChanged?.Invoke(false);
        }
    }
}

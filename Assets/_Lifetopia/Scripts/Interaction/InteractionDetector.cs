using System;
using System.Collections.Generic;
using Lifetopia.Controls;
using UnityEngine;

namespace Lifetopia.Interaction
{
    /// <summary>
    /// Lives on the player's trigger sensor (CircleCollider2D, Is Trigger). Tracks interactables
    /// in range, selects the nearest usable one as <see cref="Current"/>, and calls it when the
    /// input reader reports Interact (E key or touch button).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;

        private readonly List<IInteractable> _inRange = new List<IInteractable>(4);

        /// <summary>Raised when the selected target changes (null = nothing in range).</summary>
        public event Action<IInteractable> CurrentChanged;

        public IInteractable Current { get; private set; }

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnEnable()
        {
            if (input != null) input.InteractPerformed += HandleInteract;
        }

        private void OnDisable()
        {
            if (input != null) input.InteractPerformed -= HandleInteract;
            _inRange.Clear();
            SetCurrent(null);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var interactable = other.GetComponentInParent<IInteractable>();
            if (interactable != null && !_inRange.Contains(interactable)) _inRange.Add(interactable);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var interactable = other.GetComponentInParent<IInteractable>();
            if (interactable != null) _inRange.Remove(interactable);
        }

        private void Update()
        {
            IInteractable best = null;
            float bestDistance = float.MaxValue;
            Vector2 origin = transform.position;

            for (int i = _inRange.Count - 1; i >= 0; i--)
            {
                IInteractable candidate = _inRange[i];
                // Destroyed objects compare equal to null through UnityEngine.Object.
                if (candidate == null || (candidate is UnityEngine.Object o && o == null))
                {
                    _inRange.RemoveAt(i);
                    continue;
                }
                if (!candidate.CanInteract) continue;

                float d = (candidate.InteractionPoint - origin).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = candidate;
                }
            }

            SetCurrent(best);
        }

        private void HandleInteract()
        {
            if (Current != null && Current.CanInteract) Current.Interact(input.gameObject);
        }

        private void SetCurrent(IInteractable next)
        {
            if (ReferenceEquals(next, Current)) return;
            Current = next;
            CurrentChanged?.Invoke(Current);
        }
    }
}

using System;
using Lifetopia.Interaction;
using UnityEngine;

namespace Lifetopia.Fishing
{
    /// <summary>
    /// A place to fish (end of the dock). Trigger collider for the InteractionDetector; the bobber lands
    /// at <see cref="BobberAnchor"/> in the water. Prompt: "Fish" with the rod selected, otherwise a hint.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class FishingSpot : MonoBehaviour, IInteractable, IPromptChangeNotifier
    {
        [SerializeField] private FishingController controller;
        [SerializeField] private Transform bobberAnchor;
        [SerializeField] private SpriteRenderer highlight;

        public event Action PromptChanged;

        public Vector3 BobberAnchor => (bobberAnchor != null ? bobberAnchor : transform).position;
        public string PromptText => controller != null ? controller.PromptFor(this) : "";
        public bool CanInteract => controller != null && controller.isActiveAndEnabled && !controller.IsFishing;
        public Vector2 InteractionPoint => transform.position;

        public void Interact(GameObject interactor)
        {
            if (CanInteract) controller.TryBegin(this);
        }

        internal void RaisePromptChanged() => PromptChanged?.Invoke();

        public void SetHighlighted(bool on)
        {
            if (highlight != null && highlight.enabled != on) highlight.enabled = on;
        }
    }
}

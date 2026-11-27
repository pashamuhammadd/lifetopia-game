using Lifetopia.Interaction;
using UnityEngine;

namespace Lifetopia.Housing
{
    /// <summary>
    /// Front door of the starter house in the village: walk up to the steps and press Interact
    /// (E / touch button) to go inside. Needs a trigger Collider2D on this object at the door.
    /// The house sprite, position and collider are unchanged.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class HomeDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private HomeTransitionController transition;
        [SerializeField] private string promptText = HousingText.EnterPrompt;
        [SerializeField] private Transform interactionPoint;

        public string PromptText => promptText;
        public bool CanInteract => transition != null && transition.CanEnter;
        public Vector2 InteractionPoint => (interactionPoint != null ? interactionPoint : transform).position;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        public void Interact(GameObject interactor)
        {
            if (CanInteract) transition.RequestEnter();
        }
    }
}

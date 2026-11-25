using Lifetopia.UI;
using UnityEngine;

namespace Lifetopia.Interaction
{
    /// <summary>
    /// Example interactable: shows a few lines of dialogue (signs, simple NPC greetings).
    /// Edit Speaker Name, Prompt Text and Lines in the Inspector. No quests or rewards.
    /// </summary>
    public sealed class DialogueInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private DialogueUI dialogue;
        [SerializeField] private string speakerName = "Welcome Board";
        [SerializeField] private string promptText = "Read Welcome Board";
        [SerializeField, TextArea(2, 4)] private string[] lines =
        {
            "Welcome to Lifetopia Village!",
            "This is a greybox test area. Real art arrives in a later milestone."
        };

        [Tooltip("Where the prompt distance is measured from (defaults to this transform).")]
        [SerializeField] private Transform interactionPoint;

        public string PromptText => promptText;
        public bool CanInteract => dialogue != null && !dialogue.IsOpen && lines != null && lines.Length > 0;
        public Vector2 InteractionPoint => (interactionPoint != null ? interactionPoint : transform).position;

        public void Interact(GameObject interactor)
        {
            if (!CanInteract) return;
            dialogue.Open(speakerName, lines);
        }
    }
}

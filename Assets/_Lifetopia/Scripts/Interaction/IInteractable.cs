using UnityEngine;

namespace Lifetopia.Interaction
{
    /// <summary>
    /// Anything the player can use by walking close and pressing Interact (sign, NPC, door...).
    /// Implementers need a Collider2D so the player's InteractionDetector can find them.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Short English verb phrase for the prompt, e.g. "Read Welcome Board".</summary>
        string PromptText { get; }

        /// <summary>False hides the prompt and ignores the button (e.g. already used).</summary>
        bool CanInteract { get; }

        /// <summary>World position used to pick the nearest target.</summary>
        Vector2 InteractionPoint { get; }

        void Interact(GameObject interactor);
    }
}

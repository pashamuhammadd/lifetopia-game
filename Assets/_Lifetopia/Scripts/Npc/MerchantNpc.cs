using Lifetopia.Economy;
using Lifetopia.Interaction;
using Lifetopia.UI;
using UnityEngine;

namespace Lifetopia.Npc
{
    /// <summary>
    /// A merchant NPC (Rio): Interact (E / touch) nearby turns the NPC toward the player and opens the market.
    /// Uses the existing interaction system; the NPC's solid collider is only its foot footprint.
    /// When the market closes the NPC returns to its resting direction.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MerchantNpc : MonoBehaviour, IInteractable
    {
        [SerializeField] private MarketPanelUI market;
        [SerializeField] private NpcView view;
        [SerializeField] private string promptText = MarketText.PromptTalk;
        [Tooltip("Where the prompt distance is measured from (defaults to this transform).")]
        [SerializeField] private Transform interactionPoint;

        private bool _listening;

        public string PromptText => promptText;
        public bool CanInteract => market != null && market.CanOpen;
        public Vector2 InteractionPoint => (interactionPoint != null ? interactionPoint : transform).position;

        public void Interact(GameObject interactor)
        {
            if (!CanInteract) return;
            if (view != null && interactor != null) view.FaceTowards(interactor.transform.position);
            if (!market.Open()) { if (view != null) view.ResetFacing(); return; }
            if (!_listening) { market.Closed += OnMarketClosed; _listening = true; }
        }

        private void OnMarketClosed()
        {
            if (view != null) view.ResetFacing();
        }

        private void OnDestroy()
        {
            if (_listening && market != null) market.Closed -= OnMarketClosed;
        }
    }
}

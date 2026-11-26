using System;
using Lifetopia.Interaction;
using UnityEngine;

namespace Lifetopia.Farming
{
    /// <summary>
    /// Scene object for one farm plot: soil, crop, status indicator and focus outline renderers, plus a
    /// trigger collider so the player's InteractionDetector can target it. Holds no game state; the
    /// <see cref="FarmFieldHost"/> owns the plot state and tells this view what to show.
    /// Sorting: soil and outline on "GroundDetail"; crop and indicator on "World" (Y-sorted by pivot).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class FarmPlotView : MonoBehaviour, IInteractable, IPromptChangeNotifier
    {
        [SerializeField] private SpriteRenderer soilRenderer;
        [SerializeField] private SpriteRenderer cropRenderer;
        [SerializeField] private SpriteRenderer indicatorRenderer;
        [SerializeField] private SpriteRenderer highlightRenderer;
        [Tooltip("Optional disc behind the status badge (generated if no sprite is set).")]
        [SerializeField] private SpriteRenderer indicatorBackRenderer;
        [Tooltip("Transform scaled briefly as action feedback (soil + crop).")]
        [SerializeField] private Transform feedbackRoot;
        [SerializeField, Min(0.05f)] private float pulseSeconds = 0.18f;

        private FarmFieldHost _host;
        private int _index = -1;
        private float _pulse;

        public event Action PromptChanged;

        public int Index => _index;

        public string PromptText => _host != null ? _host.GetPrompt(_index) : "";
        public bool CanInteract => _host != null && _host.isActiveAndEnabled && _index >= 0;
        public Vector2 InteractionPoint => transform.position;

        public void Interact(GameObject interactor)
        {
            if (CanInteract) _host.Act(_index);
        }

        internal void Bind(FarmFieldHost host, int index)
        {
            _host = host;
            _index = index;
            if (highlightRenderer != null && highlightRenderer.sprite == null)
                highlightRenderer.sprite = FarmPlaceholderSprites.Highlight; // generated outline, no art needed
            if (indicatorBackRenderer != null && indicatorBackRenderer.sprite == null)
                indicatorBackRenderer.sprite = FarmPlaceholderSprites.BadgeBack;
            SetHighlighted(false);
            enabled = false; // Update only runs while a feedback pulse plays.
        }

        internal void RaisePromptChanged() => PromptChanged?.Invoke();

        internal void Show(Sprite soil, Sprite crop, float cropScale, Sprite indicator, float indicatorScale)
        {
            if (soilRenderer != null) soilRenderer.sprite = soil;
            if (cropRenderer != null)
            {
                cropRenderer.sprite = crop;
                cropRenderer.enabled = crop != null;
                // Scales around the sprite pivot (plant base), so the base stays on the plot centre.
                cropRenderer.transform.localScale = new Vector3(cropScale, cropScale, 1f);
            }
            if (indicatorRenderer != null)
            {
                indicatorRenderer.sprite = indicator;
                indicatorRenderer.enabled = indicator != null;
                indicatorRenderer.transform.localScale = new Vector3(indicatorScale, indicatorScale, 1f);
            }
            if (indicatorBackRenderer != null) indicatorBackRenderer.enabled = indicator != null;
        }

        public void SetHighlighted(bool on)
        {
            if (highlightRenderer != null && highlightRenderer.enabled != on) highlightRenderer.enabled = on;
        }

        /// <summary>Short squash-and-stretch as feedback after an action (no new art needed).</summary>
        internal void Pulse()
        {
            if (feedbackRoot == null) return;
            _pulse = pulseSeconds;
            enabled = true;
        }

        private void Update()
        {
            _pulse -= Time.deltaTime;
            float t = Mathf.Clamp01(_pulse / pulseSeconds);
            float s = 1f + 0.12f * Mathf.Sin(t * Mathf.PI);
            if (feedbackRoot != null) feedbackRoot.localScale = new Vector3(s, 2f - s, 1f);
            if (_pulse <= 0f)
            {
                if (feedbackRoot != null) feedbackRoot.localScale = Vector3.one;
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (feedbackRoot != null) feedbackRoot.localScale = Vector3.one;
        }
    }
}

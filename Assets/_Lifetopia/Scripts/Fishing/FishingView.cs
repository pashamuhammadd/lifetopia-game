using UnityEngine;

namespace Lifetopia.Fishing
{
    /// <summary>
    /// World visuals of an attempt: bobber at a fixed anchor, a thin line from the player's rod tip,
    /// looping ripple, and splashes on bite / catch. Two frames per effect animated with scale/fade
    /// (no particle systems, no character animation). Holds no game state.
    /// </summary>
    public sealed class FishingView : MonoBehaviour
    {
        [SerializeField] private FishingVisualSet visuals;
        [SerializeField] private SpriteRenderer bobber;
        [SerializeField] private SpriteRenderer ripple;
        [SerializeField] private SpriteRenderer splash;
        [Tooltip("Thin line drawn with a 1-pixel sprite stretched between rod tip and bobber.")]
        [SerializeField] private SpriteRenderer line;
        [SerializeField, Min(0.005f)] private float lineWidth = 0.035f;
        [Tooltip("Rod tip offset from the player's feet.")]
        [SerializeField] private Vector2 rodTipOffset = new Vector2(0.28f, 0.95f);
        [SerializeField] private Color lineColor = new Color(0.25f, 0.2f, 0.17f, 0.9f);

        private Transform _player;
        private Vector3 _anchor;
        private FishingState _state;
        private float _t, _splashT = -1f;

        private void Awake() => HideAll();

        public void Begin(Transform player, Vector3 anchor)
        {
            _player = player;
            _anchor = anchor;
            _t = 0f;
            _splashT = -1f;
            if (bobber != null) { bobber.sprite = Set().BobberIdle; bobber.enabled = true; }
            if (line != null) { line.sprite = FishingPlaceholderSprites.Pixel; line.color = lineColor; line.enabled = true; }
            if (ripple != null) ripple.enabled = false;
            if (splash != null) splash.enabled = false;
            enabled = true;
        }

        public void OnState(FishingState state)
        {
            _state = state;
            switch (state)
            {
                case FishingState.WaitingForBite:
                    if (ripple != null) ripple.enabled = true;
                    break;
                case FishingState.Timing:
                    if (bobber != null) bobber.sprite = Set().BobberBite;
                    PlaySplash();
                    break;
                case FishingState.Success:
                    PlaySplash();
                    if (bobber != null) bobber.enabled = false;
                    if (line != null) line.enabled = false;
                    break;
                case FishingState.Failure:
                case FishingState.Cancelled:
                    if (bobber != null) bobber.sprite = Set().BobberIdle;
                    break;
                case FishingState.Idle:
                    HideAll();
                    break;
            }
        }

        public void HideAll()
        {
            foreach (SpriteRenderer r in new[] { bobber, ripple, splash, line }) if (r != null) r.enabled = false;
            enabled = false;
        }

        private FishingVisualSet Set()
        {
            if (visuals == null) visuals = ScriptableObject.CreateInstance<FishingVisualSet>(); // placeholders only
            return visuals;
        }

        private void PlaySplash()
        {
            _splashT = 0f;
            if (splash != null) { splash.enabled = true; splash.transform.position = _anchor; }
        }

        private void LateUpdate()
        {
            _t += Time.deltaTime;
            float castT = _state == FishingState.Casting ? Mathf.Clamp01(_t / 0.3f) : 1f;
            Vector3 tip = _player != null ? _player.position + (Vector3)rodTipOffset : _anchor;

            // Bobber: arcs out while casting, bobs gently while waiting, dips on a bite.
            Vector3 pos = Vector3.Lerp(tip, _anchor, castT) + Vector3.up * Mathf.Sin(castT * Mathf.PI) * 0.6f;
            if (_state == FishingState.WaitingForBite) pos.y += Mathf.Sin(_t * 3f) * 0.03f;
            if (_state == FishingState.Timing) pos.y += Mathf.Sin(_t * 18f) * 0.05f - 0.04f;
            if (bobber != null) bobber.transform.position = pos;

            if (line != null && line.enabled)
            {
                Vector3 d = pos - tip;
                line.transform.position = tip;
                line.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                line.transform.localScale = new Vector3(d.magnitude, lineWidth, 1f);
            }

            if (ripple != null && ripple.enabled)
            {
                float cycle = (_t % 1.2f) / 1.2f;
                ripple.sprite = Set().Ripple(cycle < 0.5f ? 0 : 1);
                ripple.transform.position = _anchor;
                float s = 0.8f + cycle * 0.5f;
                ripple.transform.localScale = new Vector3(s, s, 1f);
                ripple.color = new Color(1f, 1f, 1f, 1f - cycle * 0.7f);
            }

            if (splash != null && _splashT >= 0f)
            {
                _splashT += Time.deltaTime;
                float k = _splashT / 0.5f;
                splash.sprite = Set().Splash(k < 0.5f ? 0 : 1);
                splash.transform.localScale = Vector3.one * (0.8f + k * 0.4f);
                splash.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1.2f - k));
                if (k >= 1f) { splash.enabled = false; _splashT = -1f; }
            }
        }
    }
}

using UnityEngine;

namespace Lifetopia.Fishing
{
    /// <summary>
    /// Sprites for the fishing effects (two frames each, animated by scale/fade in code).
    /// The instance with original artwork is LOCAL ONLY; empty fields use generated placeholders.
    /// </summary>
    [CreateAssetMenu(menuName = "Lifetopia/Fishing/Fishing Visual Set", fileName = "FishingVisualSet")]
    public sealed class FishingVisualSet : ScriptableObject
    {
        [SerializeField] private Sprite bobberIdle;
        [SerializeField] private Sprite bobberBite;
        [SerializeField] private Sprite ripple1;
        [SerializeField] private Sprite ripple2;
        [SerializeField] private Sprite splash1;
        [SerializeField] private Sprite splash2;

        public Sprite BobberIdle => bobberIdle != null ? bobberIdle : FishingPlaceholderSprites.Bobber(false);
        public Sprite BobberBite => bobberBite != null ? bobberBite : FishingPlaceholderSprites.Bobber(true);
        // Explicit != null: Unity's "fake null" for unassigned fields is not caught by the ?? operator.
        public Sprite Ripple(int frame)
        {
            Sprite s = frame == 0 ? ripple1 : ripple2;
            return s != null ? s : FishingPlaceholderSprites.Ripple(frame);
        }

        public Sprite Splash(int frame)
        {
            Sprite s = frame == 0 ? splash1 : splash2;
            return s != null ? s : FishingPlaceholderSprites.Splash(frame);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Lifetopia.UI
{
    /// <summary>
    /// Gives an Image a code-generated rounded (or soft-shadow) sprite at runtime when no sprite is
    /// assigned. To use authored UI art instead, assign a 9-sliced sprite to the Image in the Inspector;
    /// this component then leaves it untouched. (In Edit Mode the placeholder Image shows as a square.)
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class RoundedImage : MonoBehaviour
    {
        [SerializeField, Range(2, 64)] private int radius = 16;
        [Tooltip("0 = crisp. Use 6-16 for a soft shadow.")]
        [SerializeField, Range(0, 32)] private int softness;

        private void Awake()
        {
            var image = GetComponent<Image>();
            if (image.sprite != null) return;
            image.sprite = RoundedSprites.Get(radius, softness);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
        }
    }
}

using System;
using Lifetopia.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Lifetopia.UI
{
    /// <summary>
    /// Applies LOCAL-ONLY artwork to shared (tracked) UI and item icons at runtime.
    /// Put this component only in local scenes (e.g. GameplayPrototype). Tracked prefabs and item
    /// assets keep no reference to local art, so the public repository builds with placeholders.
    /// - Item icons: registers an <see cref="ItemIconOverrides"/> asset with <see cref="ItemIcons"/>.
    /// - UI images: sets a sprite on each target Image, optionally showing/hiding objects
    ///   (e.g. show the backpack icon, hide the "Bag" text).
    /// - Time HUD: period icons (Morning, Daytime, Sunset, Night).
    /// Runs early so slots draw with the final icons on their first update.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class LocalArtBindings : MonoBehaviour
    {
        [Serializable]
        public struct ImageBinding
        {
            public Image target;
            public Sprite sprite;
            [Tooltip("Objects activated when the binding is applied (e.g. a hidden Icon child).")]
            public GameObject[] show;
            [Tooltip("Objects deactivated when the binding is applied (e.g. a placeholder text).")]
            public GameObject[] hide;
            [Tooltip("Graphics turned off (not deactivated) when applied, e.g. a placeholder ring on a parent.")]
            public Graphic[] disableGraphics;
        }

        [SerializeField] private ItemIconOverrides itemIcons;
        [SerializeField] private ImageBinding[] images = Array.Empty<ImageBinding>();
        [SerializeField] private TimeHudUI timeHud;
        [Tooltip("Order: Morning, Daytime, Sunset, Night.")]
        [SerializeField] private Sprite[] periodIcons = new Sprite[4];

        private void Awake()
        {
            if (itemIcons != null) ItemIcons.Register(itemIcons);
            foreach (ImageBinding b in images)
            {
                if (b.target == null || b.sprite == null) continue;
                if (b.show != null) foreach (GameObject go in b.show) if (go != null) go.SetActive(true);
                if (b.hide != null) foreach (GameObject go in b.hide) if (go != null) go.SetActive(false);
                if (b.disableGraphics != null) foreach (Graphic g in b.disableGraphics) if (g != null) g.enabled = false;
                b.target.sprite = b.sprite;
                b.target.type = Image.Type.Simple;
                b.target.preserveAspect = true;
                b.target.color = Color.white;
            }
        }

        private void Start()
        {
            // Works in either order: before TimeHudUI.Start the sprites are stored and used for its first
            // draw; after it, SetPeriodSprites redraws the current period.
            if (timeHud != null && periodIcons != null && periodIcons.Length > 0) timeHud.SetPeriodSprites(periodIcons);
        }

        private void OnDestroy()
        {
            if (itemIcons != null) ItemIcons.Unregister(itemIcons);
        }
    }
}

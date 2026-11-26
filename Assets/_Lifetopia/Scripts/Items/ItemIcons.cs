using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.Items
{
    /// <summary>A source of icon sprites keyed by stable item ID (e.g. a local-only art set).</summary>
    public interface IItemIconSource
    {
        /// <summary>Returns the icon for <paramref name="itemId"/>, or null when this source has none.</summary>
        Sprite GetIcon(string itemId);
    }

    /// <summary>
    /// Resolves the icon shown for an item: a registered override (by stable item ID) first, then the
    /// definition's own <see cref="ItemDefinition.Icon"/>, else null (UI shows its placeholder tile).
    /// Why: tracked <see cref="ItemDefinition"/> assets must not reference local-only artwork, so the
    /// local scene registers an <see cref="ItemIconOverrides"/> asset at runtime instead
    /// (see <c>LocalArtBindings</c>). The public scene registers nothing and keeps placeholders.
    /// </summary>
    public static class ItemIcons
    {
        private static readonly List<IItemIconSource> Sources = new List<IItemIconSource>(2);

        /// <summary>Raised when sources are added/removed so visible slots can redraw.</summary>
        public static event System.Action Changed;

        public static void Register(IItemIconSource source)
        {
            if (source == null || Sources.Contains(source)) return;
            Sources.Add(source);
            Changed?.Invoke();
        }

        public static void Unregister(IItemIconSource source)
        {
            if (source != null && Sources.Remove(source)) Changed?.Invoke();
        }

        public static Sprite Get(ItemDefinition item)
        {
            if (item == null) return null;
            for (int i = Sources.Count - 1; i >= 0; i--)
            {
                Sprite s = Sources[i]?.GetIcon(item.ItemId);
                if (s != null) return s;
            }
            return item.Icon;
        }

        /// <summary>Clears registrations (tests; play mode start without domain reload).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Reset()
        {
            Sources.Clear();
            Changed = null;
        }
    }
}

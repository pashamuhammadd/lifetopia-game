using UnityEngine;
using UnityEngine.Rendering;

namespace Lifetopia.Housing
{
    /// <summary>
    /// Scene object for one placed piece (or the Decorating ghost). Built in code from the logical
    /// placement: the transform sits on the placement anchor (front-centre of the footprint for objects,
    /// footprint centre for rugs) so the sprite pivot from the manifest lines up and Y-sorting matches the
    /// player (SortingGroup, same "World" layer, custom Y axis). The collider comes from the logical
    /// footprint, never from the PNG size. Without art a labelled greybox block is drawn.
    /// </summary>
    public sealed class FurnitureView : MonoBehaviour
    {
        /// <summary>Gap so neighbouring colliders never touch and 1-cell corridors stay walkable.</summary>
        public const float ColliderInset = 0.06f;

        private SpriteRenderer _art;
        private SpriteRenderer _body;
        private TextMesh _label;
        private BoxCollider2D _collider;
        private SortingGroup _group;

        public int InstanceId { get; private set; }
        public FurnitureDefinition Definition { get; private set; }
        public bool HasArt => _art != null && _art.sprite != null && _art.enabled;
        public BoxCollider2D Collider => _collider;

        /// <summary>Anchor of a footprint in room space: front-centre (objects) or centre (rugs).</summary>
        public static Vector2 AnchorFor(FurnitureDefinition def, Vector2Int cell, FurnitureOrientation o)
        {
            Vector2Int s = def.Footprint(o);
            float x = cell.x + s.x * 0.5f;
            return def.Layer == FurnitureLayer.Floor ? new Vector2(x, cell.y + s.y * 0.5f) : new Vector2(x, cell.y);
        }

        public static FurnitureView Create(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<FurnitureView>();
            view._group = go.AddComponent<SortingGroup>();
            var art = new GameObject("Art");
            art.transform.SetParent(go.transform, false);
            view._art = art.AddComponent<SpriteRenderer>();
            return view;
        }

        /// <summary>Shows <paramref name="def"/> at <paramref name="cell"/> (room-local coordinates).</summary>
        public void Show(int instanceId, FurnitureDefinition def, Vector2Int cell, FurnitureOrientation o, Sprite sprite,
            bool solidCollider, string sortingLayer, int sortingOrder, float visualScale = 1f)
        {
            InstanceId = instanceId;
            Definition = def;
            Vector2Int size = def.Footprint(o);
            Vector2 anchor = AnchorFor(def, cell, o);
            transform.localPosition = new Vector3(anchor.x, anchor.y, 0f);

            _group.sortingLayerName = sortingLayer;
            _group.sortingOrder = sortingOrder;

            _art.sprite = sprite;
            _art.enabled = sprite != null;
            // Uniform art calibration around the pivot (= anchor): proportions, footprint and collider unchanged.
            float k = visualScale > 0f ? visualScale : 1f;
            _art.transform.localScale = new Vector3(k, k, 1f);
            if (sprite == null) ShowPlaceholder(def, size);
            else if (_body != null) { _body.enabled = false; _label.gameObject.SetActive(false); }

            bool wantCollider = solidCollider && def.BlocksMovement;
            if (wantCollider && _collider == null) _collider = gameObject.AddComponent<BoxCollider2D>();
            if (_collider != null)
            {
                _collider.enabled = wantCollider;
                // Footprint rect relative to the anchor (front-centre): x centred, y from 0 to depth.
                _collider.offset = new Vector2(0f, size.y * 0.5f);
                _collider.size = new Vector2(Mathf.Max(0.1f, size.x - 2f * ColliderInset), Mathf.Max(0.1f, size.y - 2f * ColliderInset));
            }
        }

        public void SetTint(Color tint)
        {
            _art.color = tint;
            if (_body != null) _body.color = new Color(_bodyBase.r * tint.r, _bodyBase.g * tint.g, _bodyBase.b * tint.b, tint.a);
            if (_label != null) _label.color = new Color(0.25f, 0.18f, 0.12f, tint.a);
        }

        private Color _bodyBase = Color.white;

        private void ShowPlaceholder(FurnitureDefinition def, Vector2Int size)
        {
            if (_body == null)
            {
                var b = new GameObject("Placeholder_TEMP");
                b.transform.SetParent(transform, false);
                _body = b.AddComponent<SpriteRenderer>();
                _body.sprite = HousingSprites.WhiteBottom;
                var l = new GameObject("Label_TEMP");
                l.transform.SetParent(transform, false);
                _label = l.AddComponent<TextMesh>();
                _label.anchor = TextAnchor.MiddleCenter;
                _label.alignment = TextAlignment.Center;
                _label.characterSize = 0.07f;
                _label.fontSize = 48;
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null)
                {
                    _label.font = font;
                    l.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                }
            }
            _body.enabled = true;
            _label.gameObject.SetActive(true);
            bool rug = def.Layer == FurnitureLayer.Floor;
            float w = size.x - 0.2f;
            float h = rug ? size.y - 0.2f : size.y * 0.75f + 0.55f;
            _body.transform.localPosition = new Vector3(0f, rug ? -h * 0.5f : 0.04f, 0f);
            _body.transform.localScale = new Vector3(w, h, 1f);
            _bodyBase = PlaceholderColor(def.ItemId, rug);
            _body.color = _bodyBase;
            string name = def.Item != null ? def.Item.DisplayName : def.ItemId;
            _label.text = name + "\n(TEMP)";
            _label.transform.localPosition = new Vector3(0f, rug ? 0f : h * 0.55f, 0f);
            _label.color = new Color(0.25f, 0.18f, 0.12f, 1f);
            // TextMesh renders with its MeshRenderer inside the SortingGroup (above the body).
            var mr = _label.GetComponent<MeshRenderer>();
            mr.sortingOrder = 1;
        }

        private static Color PlaceholderColor(string id, bool rug)
        {
            if (rug) return new Color(0.42f, 0.74f, 0.74f, 1f);
            int h = 0;
            foreach (char c in id) h = h * 31 + c;
            float hue = Mathf.Abs(h % 360) / 360f;
            Color c2 = Color.HSVToRGB(Mathf.Lerp(0.05f, 0.13f, hue), 0.45f, Mathf.Lerp(0.62f, 0.8f, hue));
            return c2;
        }
    }
}

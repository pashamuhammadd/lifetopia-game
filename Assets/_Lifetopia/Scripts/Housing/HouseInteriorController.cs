using System.Collections.Generic;
using UnityEngine;

namespace Lifetopia.Housing
{
    /// <summary>
    /// Root of the HomeInterior scene. Receives the village context from
    /// <see cref="HomeTransitionController"/> (no scene search for services), builds the room, shows the
    /// session's placements (one <see cref="FurnitureView"/> per instance, redrawn on layout changes),
    /// places the player on the safe spawn, starts the adaptive camera and the interior HUD (Living Mode).
    /// Everything it creates is parented under this object, so unloading the scene removes it all.
    /// </summary>
    public sealed class HouseInteriorController : MonoBehaviour
    {
        [SerializeField] private HouseRoomView room;
        [SerializeField] private InteriorCameraFramer framer;
        [SerializeField] private DecoratingController decorating;
        [SerializeField] private HousingHud hud;
        [Tooltip("Focus offset above the feet when the camera follows the player.")]
        [SerializeField] private Vector2 followOffset = new Vector2(0f, 0.75f);

        private readonly Dictionary<int, FurnitureView> _views = new Dictionary<int, FurnitureView>();
        private readonly List<int> _stale = new List<int>();
        private Transform _furnitureRoot;
        private Transform _gridRoot;
        private HouseLayout _layout;

        public HousingContext Context { get; private set; }
        public HouseRoomView Room => room;
        public InteriorCameraFramer Framer => framer;
        public DecoratingController Decorating => decorating;
        public HousingHud Hud => hud;
        public bool IsActive => Context != null;
        public Transform FurnitureRoot => _furnitureRoot;

        /// <summary>World rect of everything that must be visible.</summary>
        public Rect ContentWorld
        {
            get
            {
                Rect r = room.ContentBounds;
                r.position += (Vector2)transform.position;
                return r;
            }
        }

        public void Enter(HousingContext context)
        {
            Context = context;
            _layout = context.Service.Layout;
            room.Build(context.Visuals, _layout.Size);
            if (_furnitureRoot == null)
            {
                _furnitureRoot = new GameObject("Furniture").transform;
                _furnitureRoot.SetParent(transform, false);
            }
            BuildExitZone();
            BuildGrid();
            SyncFurniture();
            _layout.Changed += SyncFurniture;

            if (context.Camera != null && context.Visuals != null) context.Camera.backgroundColor = context.Visuals.background;
            else if (context.Camera != null) context.Camera.backgroundColor = new Color(0.20f, 0.14f, 0.10f, 1f);

            context.Player.Teleport(RoomToWorld(room.SpawnPoint));

            hud.Build(context.Visuals);
            hud.DecorateClicked += OnDecorateClicked;
            hud.ExitClicked += OnExitClicked;
            hud.ShowLiving(true);

            decorating.Bind(context, this);
            framer.SetMode(false, hud.LivingUsable, PlayerFocus);
            framer.Begin(context.Camera, ContentWorld, PlayerFocus);
        }

        public void Leave()
        {
            if (decorating != null) decorating.Close();
            if (_layout != null) _layout.Changed -= SyncFurniture;
            if (hud != null)
            {
                hud.DecorateClicked -= OnDecorateClicked;
                hud.ExitClicked -= OnExitClicked;
                hud.ShowLiving(false);
            }
            if (framer != null) framer.End();
            Context = null;
        }

        private void OnDestroy()
        {
            if (Context != null) Leave();
        }

        public Vector2 RoomToWorld(Vector2 local) => (Vector2)transform.position + local;
        public Vector2 WorldToRoom(Vector2 world) => world - (Vector2)transform.position;
        public Vector2Int WorldToCell(Vector2 world)
        {
            Vector2 l = WorldToRoom(world);
            return new Vector2Int(Mathf.FloorToInt(l.x), Mathf.FloorToInt(l.y));
        }

        public Sprite SpriteFor(FurnitureDefinition def, FurnitureOrientation o) => SpriteFor(def, o, out _);

        public Sprite SpriteFor(FurnitureDefinition def, FurnitureOrientation o, out float visualScale)
        {
            visualScale = 1f;
            return Context != null && Context.Visuals != null && Context.Visuals.TryGetFurniture(def.ItemId, o, out Sprite s, out visualScale)
                ? s : null;
        }

        public bool TryGetView(int instanceId, out FurnitureView view) => _views.TryGetValue(instanceId, out view);

        public void SetGridVisible(bool visible)
        {
            if (_gridRoot != null) _gridRoot.gameObject.SetActive(visible);
        }

        private Vector2 PlayerFocus() => Context != null ? (Vector2)Context.Player.transform.position + followOffset : ContentWorld.center;

        private void OnDecorateClicked() => decorating.Open();

        private void OnExitClicked() => Context?.RequestExit?.Invoke();

        /// <summary>Creates, moves or removes views so they match the layout exactly (one per instance).</summary>
        private void SyncFurniture()
        {
            if (_layout == null) return;
            _stale.Clear();
            foreach (int id in _views.Keys) if (!_layout.TryGet(id, out _)) _stale.Add(id);
            foreach (int id in _stale)
            {
                if (_views[id] != null) Destroy(_views[id].gameObject);
                _views.Remove(id);
            }
            foreach (FurniturePlacement p in _layout.Placements)
            {
                if (!_views.TryGetValue(p.InstanceId, out FurnitureView v) || v == null)
                {
                    v = FurnitureView.Create(_furnitureRoot, $"{p.ItemId}#{p.InstanceId}");
                    _views[p.InstanceId] = v;
                }
                bool rug = p.Definition.Layer == FurnitureLayer.Floor;
                Sprite art = SpriteFor(p.Definition, p.Orientation, out float k);
                v.Show(p.InstanceId, p.Definition, p.Cell, p.Orientation, art, true, rug ? "GroundDetail" : "World", rug ? 5 : 0, k);
                v.SetTint(Color.white);
            }
        }

        private void BuildExitZone()
        {
            if (transform.Find("ExitZone") != null) return;
            var go = new GameObject("ExitZone");
            go.transform.SetParent(transform, false);
            RectInt door = room.DoorCells;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.offset = new Vector2(door.x + door.width * 0.5f, -0.85f);
            box.size = new Vector2(door.width - 0.1f, 0.7f);
            go.AddComponent<HomeExitZone>().Bind(this);
        }

        internal void OnPlayerReachedExit(Collider2D other)
        {
            if (Context == null || other == null || other.isTrigger) return;
            if (other.attachedRigidbody == null || other.attachedRigidbody.gameObject != Context.Player.gameObject) return;
            Context.RequestExit?.Invoke();
        }

        private void BuildGrid()
        {
            if (_gridRoot != null) return;
            _gridRoot = new GameObject("DecorGrid").transform;
            _gridRoot.SetParent(transform, false);
            for (int y = 0; y < _layout.Size.y; y++)
                for (int x = 0; x < _layout.Size.x; x++)
                {
                    var c = new Vector2Int(x, y);
                    var go = new GameObject($"Cell_{x}_{y}");
                    go.transform.SetParent(_gridRoot, false);
                    go.transform.localPosition = new Vector3(x + 0.5f, y + 0.5f, 0f);
                    var r = go.AddComponent<SpriteRenderer>();
                    bool reserved = _layout.IsReserved(c);
                    r.sprite = reserved ? HousingSprites.HatchCell : HousingSprites.GridCell;
                    r.color = reserved ? new Color(0.35f, 0.75f, 0.85f, 0.9f) : Color.white;
                    r.sortingLayerName = "GroundDetail";
                    r.sortingOrder = 20;
                }
            _gridRoot.gameObject.SetActive(false);
        }
    }
}

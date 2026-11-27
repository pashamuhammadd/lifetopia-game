using UnityEngine;

namespace Lifetopia.Housing
{
    /// <summary>
    /// Builds the interior shell in code from the logical room size (default 8 x 8 cells, 1 unit each):
    /// floor tiles, a tall back wall with windows, thin side posts and a low front cutaway with a door
    /// opening, plus the wall colliders. Room space: origin = south-west floor corner.
    /// Uses <see cref="HousingVisualSet"/> art when given, otherwise labelled greybox shapes.
    /// Sorting: floor "Ground"; back/side walls and windows "GroundDetail" (always behind people and
    /// furniture); the low front wall "World" with a pivot sort point (in front of feet at the door).
    /// </summary>
    public sealed class HouseRoomView : MonoBehaviour
    {
        [Tooltip("Door opening in the front wall: first cell column and width in cells.")]
        [SerializeField] private int doorColumn = 3;
        [SerializeField] private int doorWidth = 2;
        [Tooltip("Height of the back wall above the floor in units (art is 96 px = 1.5).")]
        [SerializeField] private float backWallHeight = 1.5f;

        private Transform _visuals;

        public Vector2Int Size { get; private set; } = new Vector2Int(8, 8);

        /// <summary>Everything that must be visible (room-local): side posts, front wall, back wall top.</summary>
        public Rect ContentBounds => new Rect(-0.35f, -0.55f, Size.x + 0.7f, Size.y + backWallHeight + 0.65f);

        /// <summary>Where the player appears inside (room-local), on the reserved cells in front of the door.</summary>
        public Vector2 SpawnPoint => new Vector2(doorColumn + doorWidth * 0.5f, 1.0f);

        public RectInt DoorCells => new RectInt(doorColumn, 0, doorWidth, 1);

        public void Build(HousingVisualSet art, Vector2Int size)
        {
            Size = size;
            if (_visuals != null) Destroy(_visuals.gameObject);
            _visuals = new GameObject("Shell").transform;
            _visuals.SetParent(transform, false);

            BuildFloor(art);
            BuildBackWall(art);
            BuildSides(art);
            BuildFront(art);
            BuildColliders();
        }

        private void BuildFloor(HousingVisualSet art)
        {
            Transform floor = Child("Floor");
            bool hasArt = art != null && art.floorTiles != null && art.floorTiles.Length > 0;
            for (int y = 0; y < Size.y; y++)
                for (int x = 0; x < Size.x; x++)
                {
                    int pattern = (x * 3 + y * 5 + x * y) & 3;
                    Sprite s = hasArt ? art.floorTiles[pattern % art.floorTiles.Length] : HousingSprites.PlaceholderFloor(pattern);
                    if (s == null) s = HousingSprites.PlaceholderFloor(pattern);
                    Sprite(floor, $"Floor_{x}_{y}", s, new Vector2(x + 0.5f, y + 0.5f), "Ground", 0);
                }
        }

        private void BuildBackWall(HousingVisualSet art)
        {
            Transform wall = Child("BackWall");
            if (art != null && art.backWallCenter != null)
            {
                for (int x = 0; x < Size.x; x++)
                {
                    Sprite s = x == 0 && art.backWallLeftCap != null ? art.backWallLeftCap
                             : x == Size.x - 1 && art.backWallRightCap != null ? art.backWallRightCap
                             : art.backWallCenter;
                    Sprite(wall, "Back_" + x, s, new Vector2(x + 0.5f, Size.y), "GroundDetail", 0);
                }
                if (art.windowSingle != null) Sprite(wall, "Window_Single", art.windowSingle, new Vector2(2f, Size.y + 0.45f), "GroundDetail", 1);
                if (art.windowDouble != null) Sprite(wall, "Window_Double", art.windowDouble, new Vector2(5.5f, Size.y + 0.45f), "GroundDetail", 1);
                return;
            }
            Block(wall, "Back_TEMP", new Vector2(Size.x * 0.5f, Size.y), new Vector2(Size.x + 0.5f, backWallHeight), new Color(0.96f, 0.90f, 0.78f), "GroundDetail", 0);
            Block(wall, "BackTrim_TEMP", new Vector2(Size.x * 0.5f, Size.y + backWallHeight - 0.12f), new Vector2(Size.x + 0.5f, 0.14f), new Color(0.62f, 0.42f, 0.26f), "GroundDetail", 1);
            Block(wall, "BackBase_TEMP", new Vector2(Size.x * 0.5f, Size.y), new Vector2(Size.x + 0.5f, 0.1f), new Color(0.62f, 0.42f, 0.26f), "GroundDetail", 1);
            Block(wall, "Window_TEMP", new Vector2(5.5f, Size.y + 0.45f), new Vector2(1.8f, 0.8f), new Color(0.55f, 0.80f, 0.82f), "GroundDetail", 2);
        }

        private void BuildSides(HousingVisualSet art)
        {
            Transform sides = Child("Sides");
            if (art != null && art.sideWallLeft != null && art.sideWallRight != null)
            {
                // Side art is a 17 px strip at the outer edge of a 64 px tile: put the strip just outside the floor.
                for (int y = 0; y < Size.y; y++)
                {
                    Sprite(sides, "Left_" + y, art.sideWallLeft, new Vector2(0.5f - 17f / 64f, y + 0.5f), "GroundDetail", 2);
                    Sprite(sides, "Right_" + y, art.sideWallRight, new Vector2(Size.x - 0.5f + 16f / 64f, y + 0.5f), "GroundDetail", 2);
                }
                return;
            }
            Color post = new Color(0.62f, 0.42f, 0.26f);
            Block(sides, "Left_TEMP", new Vector2(-0.13f, 0f), new Vector2(0.26f, Size.y), post, "GroundDetail", 2);
            Block(sides, "Right_TEMP", new Vector2(Size.x + 0.13f, 0f), new Vector2(0.26f, Size.y), post, "GroundDetail", 2);
        }

        private void BuildFront(HousingVisualSet art)
        {
            Transform front = Child("Front");
            // The front strip is the top 17 px of a 24 px image (pivot bottom): put its top on the floor edge.
            const float pivotY = -24f / 64f + 0.02f;
            for (int x = 0; x < Size.x; x++)
            {
                if (x >= doorColumn && x < doorColumn + doorWidth) continue;
                if (art != null && art.frontWallLow != null)
                {
                    Sprite s = x == doorColumn - 1 && art.frontWallLeftEnd != null ? art.frontWallLeftEnd
                             : x == doorColumn + doorWidth && art.frontWallRightEnd != null ? art.frontWallRightEnd
                             : art.frontWallLow;
                    SpriteRenderer r = Sprite(front, "Front_" + x, s, new Vector2(x + 0.5f, pivotY), "World", 0);
                    r.spriteSortPoint = SpriteSortPoint.Pivot;
                }
                else
                {
                    SpriteRenderer r = Block(front, "Front_TEMP_" + x, new Vector2(x + 0.5f, -0.26f), new Vector2(1f, 0.28f), new Color(0.62f, 0.42f, 0.26f), "World", 0);
                    r.spriteSortPoint = SpriteSortPoint.Pivot;
                }
            }
        }

        private void BuildColliders()
        {
            Transform c = Child("Colliders");
            float w = Size.x, h = Size.y, door0 = doorColumn, door1 = doorColumn + doorWidth;
            Box(c, "WallLeft", new Vector2(-0.3f, h * 0.5f - 0.7f), new Vector2(0.6f, h + 1.4f));
            Box(c, "WallRight", new Vector2(w + 0.3f, h * 0.5f - 0.7f), new Vector2(0.6f, h + 1.4f));
            Box(c, "WallBack", new Vector2(w * 0.5f, h + 0.3f), new Vector2(w + 1.2f, 0.6f));
            // Front wall left/right of the opening; the opening continues as a short corridor to the exit zone.
            Box(c, "FrontLeft", new Vector2(door0 * 0.5f, -0.7f), new Vector2(door0, 1.4f));
            Box(c, "FrontRight", new Vector2((door1 + w) * 0.5f, -0.7f), new Vector2(w - door1, 1.4f));
            Box(c, "DoorStop", new Vector2(w * 0.5f, -1.6f), new Vector2(w + 1.2f, 0.4f));
        }

        private Transform Child(string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(_visuals, false);
            return t;
        }

        private static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Vector2 pos, string layer, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingLayerName = layer;
            r.sortingOrder = order;
            return r;
        }

        private static SpriteRenderer Block(Transform parent, string name, Vector2 bottomCentre, Vector2 size, Color color, string layer, int order)
        {
            SpriteRenderer r = Sprite(parent, name, HousingSprites.WhiteBottom, bottomCentre, layer, order);
            r.transform.localScale = new Vector3(size.x, size.y, 1f);
            r.color = color;
            return r;
        }

        private static void Box(Transform parent, string name, Vector2 centre, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<BoxCollider2D>();
            b.offset = centre;
            b.size = size;
        }
    }
}

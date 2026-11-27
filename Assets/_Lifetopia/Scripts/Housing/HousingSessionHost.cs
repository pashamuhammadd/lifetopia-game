using System.Collections.Generic;
using Lifetopia.InventorySystem;
using UnityEngine;

namespace Lifetopia.Housing
{
    /// <summary>
    /// Owns the house layout for this play session. Lives in the village scene next to
    /// <see cref="PlayerProfileHost"/> (the village scene stays loaded while the interior scene is added on
    /// top), so placements survive entering/leaving the house without a second inventory or wallet.
    /// The free starter furniture is placed once when the session's house is first created.
    /// Reloading the browser starts a new session (no permanent save before M3.7).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HousingSessionHost : MonoBehaviour
    {
        [SerializeField] private PlayerProfileHost profileHost;
        [SerializeField] private FurnitureCatalog catalog;
        [SerializeField] private Vector2Int roomSize = new Vector2Int(8, 8);
        [Tooltip("Cells of the door opening; the path check floods from here.")]
        [SerializeField] private List<Vector2Int> exitCells = new List<Vector2Int> { new Vector2Int(3, 0), new Vector2Int(4, 0) };
        [Tooltip("Extra cells kept free (interior spawn in front of the door).")]
        [SerializeField] private List<Vector2Int> reservedCells = new List<Vector2Int> { new Vector2Int(3, 1), new Vector2Int(4, 1) };
        [Tooltip("Free furniture placed once per new session (never also added to the bag).")]
        [SerializeField] private List<StarterPlacement> starterLayout = new List<StarterPlacement>();

        private HouseLayout _layout;
        private HouseDecorService _service;

        public FurnitureCatalog Catalog => catalog;
        public PlayerProfileHost ProfileHost => profileHost;

        public HouseDecorService Service
        {
            get
            {
                if (_service == null) Create();
                return _service;
            }
        }

        public HouseLayout Layout => Service?.Layout;

        private void Awake()
        {
            if (_service == null) Create();
        }

        private void Create()
        {
            if (profileHost == null || catalog == null)
            {
                Debug.LogError("[Lifetopia] HousingSessionHost needs a PlayerProfileHost and a FurnitureCatalog.", this);
                return;
            }
            var problems = new List<string>();
            catalog.Validate(problems);
            _layout = new HouseLayout(roomSize, exitCells, reservedCells);
            _service = new HouseDecorService(_layout, catalog, profileHost.Profile.Inventory);
            _service.GrantStarterLayout(starterLayout, problems);
            foreach (string p in problems) Debug.LogWarning("[Lifetopia] " + p, this);
        }

        public HouseLayoutData CaptureState() => Layout?.ToData();
    }
}

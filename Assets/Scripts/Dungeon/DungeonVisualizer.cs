using UnityEngine;
using System.Collections.Generic;

namespace DarkFort.Dungeon
{
    /// <summary>
    /// Simple grid-based dungeon visualizer.
    /// Each room is a colored sprite on a grid. No walls or corridors.
    /// </summary>
    public class DungeonVisualizer : MonoBehaviour
    {
        #region Singleton
        public static DungeonVisualizer Instance { get; private set; }
        #endregion

        #region Settings
        [Header("Grid Settings")]
        [SerializeField] private float cellSize = 2f;  // Size of each room sprite
        [SerializeField] private float cellSpacing = 2.5f;  // Distance between room centers

        [Header("Room Colors - Base")]
        [SerializeField] private Color unexploredColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        [SerializeField] private Color exploredColor = new Color(0.4f, 0.4f, 0.45f, 1f);
        [SerializeField] private Color foggedColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        [Header("Room Colors - Encounter Types")]
        [SerializeField] private Color entranceColor = new Color(0.2f, 0.6f, 0.2f, 1f);      // Green
        [SerializeField] private Color emptyRoomColor = new Color(0.4f, 0.4f, 0.45f, 1f);    // Gray (default)
        [SerializeField] private Color pitTrapColor = new Color(0.55f, 0.35f, 0.2f, 1f);     // Brown
        [SerializeField] private Color soothsayerColor = new Color(0.5f, 0.3f, 0.6f, 1f);    // Purple
        [SerializeField] private Color weakMonsterColor = new Color(0.6f, 0.3f, 0.3f, 1f);   // Light Red
        [SerializeField] private Color toughMonsterColor = new Color(0.7f, 0.2f, 0.2f, 1f);  // Dark Red
        [SerializeField] private Color merchantColor = new Color(0.7f, 0.65f, 0.2f, 1f);     // Yellow/Gold

        [Header("Connection Colors")]
        [SerializeField] private Color connectionColor = new Color(0.35f, 0.35f, 0.4f, 1f);
        [SerializeField] private Color foggedConnectionColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        [Header("Sorting")]
        [SerializeField] private int roomSortOrder = 0;
        [SerializeField] private int connectionSortOrder = -1;
        [SerializeField] private int backgroundSortOrder = -100;

        [Header("Background")]
        [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);  // Dark blue-gray
        [SerializeField] private float backgroundSize = 500f;
        #endregion

        #region State
        private DungeonLayout currentLayout;
        private Dictionary<Room, GameObject> roomSprites = new Dictionary<Room, GameObject>();
        private Dictionary<Room, SpriteRenderer> roomRenderers = new Dictionary<Room, SpriteRenderer>();
        private List<GameObject> connectionSprites = new List<GameObject>();
        private Room currentRoom;
        private Sprite squareSprite;
        private Transform roomsParent;
        private Transform connectionsParent;
        private GameObject background;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            squareSprite = CreateSquareSprite();

            // Create background
            CreateBackground();

            roomsParent = new GameObject("Rooms").transform;
            roomsParent.SetParent(transform);

            connectionsParent = new GameObject("Connections").transform;
            connectionsParent.SetParent(transform);
        }

        private void Start()
        {
            if (DungeonGenerator.Instance != null)
            {
                DungeonGenerator.Instance.OnDungeonGenerated += OnDungeonGenerated;
            }

            if (MapController.Instance != null)
            {
                MapController.Instance.OnRoomEntered += OnRoomEntered;
            }
        }

        private void OnDestroy()
        {
            if (DungeonGenerator.Instance != null)
            {
                DungeonGenerator.Instance.OnDungeonGenerated -= OnDungeonGenerated;
            }

            if (MapController.Instance != null)
            {
                MapController.Instance.OnRoomEntered -= OnRoomEntered;
            }

            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Event Handlers
        private void OnDungeonGenerated(DungeonLayout layout)
        {
            RenderDungeon(layout);
        }

        private void OnRoomEntered(Room room, bool firstTime)
        {
            if (room == null) return;

            // Update current room highlight
            Room previousRoom = currentRoom;
            currentRoom = room;

            // Update visuals for previous room
            if (previousRoom != null)
            {
                UpdateRoomVisual(previousRoom);
            }

            // Update visuals for current room and connections
            UpdateRoomVisual(room);

            // Update adjacent rooms (they might now be visible)
            foreach (var adjacent in room.Connections.Values)
            {
                UpdateRoomVisual(adjacent);
            }

            // Update all connections
            UpdateAllConnections();
        }
        #endregion

        #region Rendering
        public void RenderDungeon(DungeonLayout layout)
        {
            currentLayout = layout;
            ClearDungeon();

            // Create room sprites
            foreach (var room in layout.Rooms)
            {
                CreateRoomSprite(room);
            }

            // Create connection sprites
            foreach (var corridor in layout.Corridors)
            {
                CreateConnectionSprite(corridor);
            }

            // Set initial room as current
            if (layout.EntranceRoom != null)
            {
                currentRoom = layout.EntranceRoom;
                UpdateRoomVisual(layout.EntranceRoom);
            }

            Debug.Log($"Visualized dungeon: {layout.Rooms.Count} rooms");
        }

        public void ClearDungeon()
        {
            foreach (var kvp in roomSprites)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }
            roomSprites.Clear();
            roomRenderers.Clear();

            foreach (var conn in connectionSprites)
            {
                if (conn != null) Destroy(conn);
            }
            connectionSprites.Clear();

            currentRoom = null;
        }

        private void CreateRoomSprite(Room room)
        {
            Vector3 position = GridToWorldPosition(room.GridPosition);

            GameObject obj = new GameObject($"Room_{room.Id}");
            obj.transform.SetParent(roomsParent);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(cellSize, cellSize, 1f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
            sr.sortingOrder = roomSortOrder;

            roomSprites[room] = obj;
            roomRenderers[room] = sr;

            UpdateRoomVisual(room);
        }

        private void CreateConnectionSprite(Corridor corridor)
        {
            Vector3 posA = GridToWorldPosition(corridor.RoomA.GridPosition);
            Vector3 posB = GridToWorldPosition(corridor.RoomB.GridPosition);
            Vector3 midPoint = (posA + posB) / 2f;

            GameObject obj = new GameObject($"Connection_{corridor.RoomA.Id}_{corridor.RoomB.Id}");
            obj.transform.SetParent(connectionsParent);
            obj.transform.position = midPoint;

            // Determine size based on direction
            bool isVertical = corridor.DirectionFromA == Direction.North || corridor.DirectionFromA == Direction.South;
            float length = cellSpacing - cellSize * 0.5f;
            float width = cellSize * 0.4f;

            if (isVertical)
            {
                obj.transform.localScale = new Vector3(width, length, 1f);
            }
            else
            {
                obj.transform.localScale = new Vector3(length, width, 1f);
            }

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
            sr.sortingOrder = connectionSortOrder;

            // Store reference to corridor in the gameobject for later updates
            ConnectionData data = obj.AddComponent<ConnectionData>();
            data.corridor = corridor;

            connectionSprites.Add(obj);

            UpdateConnectionVisual(obj, corridor);
        }

        private void UpdateRoomVisual(Room room)
        {
            if (!roomRenderers.TryGetValue(room, out SpriteRenderer sr)) return;

            Color color;

            if (!room.IsVisible)
            {
                // Room not yet discovered - show as fogged
                color = foggedColor;
            }
            else if (!room.IsExplored)
            {
                // Visible but not explored
                color = unexploredColor;
            }
            else
            {
                // Explored room - color based on type/encounter
                color = GetRoomColor(room);
            }

            sr.color = color;
        }

        private Color GetRoomColor(Room room)
        {
            // Entrance room is always green regardless of encounter
            if (room.Type == RoomType.Entrance)
            {
                return entranceColor;
            }

            // Color based on encounter type if rolled
            if (room.Encounter.HasValue)
            {
                return room.Encounter.Value switch
                {
                    // Normal encounters
                    EncounterType.Empty => emptyRoomColor,
                    EncounterType.PitTrap => pitTrapColor,
                    EncounterType.RiddlingSoothsayer => soothsayerColor,
                    EncounterType.WeakMonster => weakMonsterColor,
                    EncounterType.ToughMonster => toughMonsterColor,
                    EncounterType.Merchant => merchantColor,

                    _ => exploredColor
                };
            }

            // Default explored color
            return exploredColor;
        }

        private void UpdateConnectionVisual(GameObject connObj, Corridor corridor)
        {
            SpriteRenderer sr = connObj.GetComponent<SpriteRenderer>();
            if (sr == null) return;

            // Connection is visible only if both rooms are explored
            bool bothExplored = corridor.RoomA.IsExplored && corridor.RoomB.IsExplored;
            bool eitherVisible = corridor.RoomA.IsVisible || corridor.RoomB.IsVisible;

            if (bothExplored)
            {
                sr.color = connectionColor;
            }
            else if (eitherVisible)
            {
                sr.color = foggedConnectionColor;
            }
            else
            {
                sr.color = foggedColor;
            }
        }

        private void UpdateAllConnections()
        {
            foreach (var connObj in connectionSprites)
            {
                if (connObj == null) continue;
                ConnectionData data = connObj.GetComponent<ConnectionData>();
                if (data != null && data.corridor != null)
                {
                    UpdateConnectionVisual(connObj, data.corridor);
                }
            }
        }
        #endregion

        #region Utility
        private void CreateBackground()
        {
            background = new GameObject("Background");
            background.transform.SetParent(transform);
            background.transform.position = Vector3.zero;
            background.transform.localScale = new Vector3(backgroundSize, backgroundSize, 1f);

            SpriteRenderer sr = background.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite ?? CreateSquareSprite();
            sr.color = backgroundColor;
            sr.sortingOrder = backgroundSortOrder;
        }

        private Sprite CreateSquareSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * cellSpacing, gridPos.y * cellSpacing, 0);
        }

        public Vector3 GetRoomWorldPosition(Room room)
        {
            return GridToWorldPosition(room.GridPosition);
        }

        public float CellSpacing => cellSpacing;
        #endregion

        #region Debug
        [ContextMenu("Refresh All Visuals")]
        private void DebugRefreshVisuals()
        {
            if (currentLayout == null) return;

            foreach (var room in currentLayout.Rooms)
            {
                UpdateRoomVisual(room);
            }
            UpdateAllConnections();
        }

        [ContextMenu("Reveal All Rooms")]
        private void DebugRevealAll()
        {
            if (currentLayout == null) return;

            foreach (var room in currentLayout.Rooms)
            {
                room.IsVisible = true;
                room.IsExplored = true;
                UpdateRoomVisual(room);
            }
            UpdateAllConnections();
        }
        #endregion
    }

    /// <summary>
    /// Helper component to store corridor reference on connection GameObjects
    /// </summary>
    public class ConnectionData : MonoBehaviour
    {
        public Corridor corridor;
    }
}
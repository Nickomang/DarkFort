using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace DarkFort.Dungeon
{
    /// <summary>
    /// UI minimap that shows explored dungeon layout
    /// Can be displayed as corner widget or full-screen overlay
    /// </summary>
    public class Minimap : MonoBehaviour
    {
        #region Singleton
        public static Minimap Instance { get; private set; }
        #endregion

        #region References
        [Header("UI References")]
        [SerializeField] private RectTransform minimapContainer;
        [SerializeField] private RectTransform roomsParent;
        [SerializeField] private GameObject roomPrefab;
        [SerializeField] private GameObject corridorPrefab;
        [SerializeField] private GameObject playerMarkerPrefab;

        [Header("Full Map Panel")]
        [SerializeField] private GameObject fullMapPanel;
        [SerializeField] private RectTransform fullMapRoomsParent;
        #endregion

        #region Settings
        [Header("Minimap Settings")]
        [SerializeField] private float roomSize = 20f;
        [SerializeField] private float roomSpacing = 30f;
        [SerializeField] private float corridorWidth = 8f;

        [Header("Colors")]
        [SerializeField] private Color exploredRoomColor = new Color(0.6f, 0.5f, 0.4f);
        [SerializeField] private Color unexploredRoomColor = new Color(0.3f, 0.3f, 0.35f);
        [SerializeField] private Color currentRoomColor = new Color(1f, 0.9f, 0.6f);
        [SerializeField] private Color entranceColor = new Color(0.4f, 0.7f, 0.4f);
        [SerializeField] private Color corridorColor = new Color(0.4f, 0.35f, 0.3f);
        [SerializeField] private Color playerMarkerColor = Color.white;
        // Full map toggle key: M (hardcoded for Input System)
        #endregion

        #region State
        private DungeonLayout currentLayout;
        private Dictionary<Room, RectTransform> roomUIElements = new Dictionary<Room, RectTransform>();
        private Dictionary<Room, Image> roomImages = new Dictionary<Room, Image>();
        private RectTransform playerMarker;
        private RectTransform fullMapPlayerMarker;
        private bool isFullMapOpen = false;
        #endregion

        #region Properties
        public bool IsFullMapOpen => isFullMapOpen;
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
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (DungeonGenerator.Instance != null)
            {
                DungeonGenerator.Instance.OnDungeonGenerated -= OnDungeonGenerated;
            }

            if (MapController.Instance != null)
            {
                MapController.Instance.OnRoomEntered -= OnRoomEntered;
                MapController.Instance.OnRoomRevealed -= OnRoomRevealed;
            }

            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            // Subscribe to events
            if (DungeonGenerator.Instance != null)
            {
                DungeonGenerator.Instance.OnDungeonGenerated += OnDungeonGenerated;
            }

            if (MapController.Instance != null)
            {
                MapController.Instance.OnRoomEntered += OnRoomEntered;
                MapController.Instance.OnRoomRevealed += OnRoomRevealed;
            }

            // Hide full map initially
            if (fullMapPanel != null)
            {
                fullMapPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // Toggle full map (M key)
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.mKey.wasPressedThisFrame)
            {
                ToggleFullMap();
            }
        }
        #endregion

        #region Event Handlers

        private void OnDungeonGenerated(DungeonLayout layout)
        {
            currentLayout = layout;
            BuildMinimap();
        }

        private void OnRoomEntered(Room room, bool firstTime)
        {
            UpdateRoomVisual(room);
            UpdatePlayerMarkerPosition(room);

            // Update adjacent rooms
            foreach (var adjacent in room.Connections.Values)
            {
                UpdateRoomVisual(adjacent);
            }
        }

        private void OnRoomRevealed(Room room)
        {
            UpdateRoomVisual(room);
        }

        #endregion

        #region Minimap Building

        private void BuildMinimap()
        {
            ClearMinimap();

            if (currentLayout == null) return;

            // Create room UI elements
            foreach (var room in currentLayout.Rooms)
            {
                CreateRoomUI(room, roomsParent);

                if (fullMapRoomsParent != null)
                {
                    CreateRoomUI(room, fullMapRoomsParent, true);
                }
            }

            // Create corridor UI elements
            foreach (var corridor in currentLayout.Corridors)
            {
                CreateCorridorUI(corridor, roomsParent);

                if (fullMapRoomsParent != null)
                {
                    CreateCorridorUI(corridor, fullMapRoomsParent, true);
                }
            }

            // Create player marker
            CreatePlayerMarker();

            // Initial visibility update
            foreach (var room in currentLayout.Rooms)
            {
                UpdateRoomVisual(room);
            }
        }

        private void ClearMinimap()
        {
            // Clear minimap
            if (roomsParent != null)
            {
                foreach (Transform child in roomsParent)
                {
                    Destroy(child.gameObject);
                }
            }

            // Clear full map
            if (fullMapRoomsParent != null)
            {
                foreach (Transform child in fullMapRoomsParent)
                {
                    Destroy(child.gameObject);
                }
            }

            roomUIElements.Clear();
            roomImages.Clear();
            playerMarker = null;
            fullMapPlayerMarker = null;
        }

        private void CreateRoomUI(Room room, RectTransform parent, bool isFullMap = false)
        {
            GameObject roomObj;

            if (roomPrefab != null)
            {
                roomObj = Instantiate(roomPrefab, parent);
            }
            else
            {
                // Create default room UI
                roomObj = new GameObject($"Room_{room.Id}");
                roomObj.transform.SetParent(parent, false);

                Image img = roomObj.AddComponent<Image>();
                img.color = unexploredRoomColor;
            }

            RectTransform rt = roomObj.GetComponent<RectTransform>();

            // Position based on grid position
            float scale = isFullMap ? 1.5f : 1f;
            rt.anchoredPosition = new Vector2(
                room.GridPosition.x * roomSpacing * scale,
                room.GridPosition.y * roomSpacing * scale
            );
            rt.sizeDelta = new Vector2(roomSize * scale, roomSize * scale);

            // Store reference (only for main minimap)
            if (!isFullMap)
            {
                roomUIElements[room] = rt;

                Image image = roomObj.GetComponent<Image>();
                if (image != null)
                {
                    roomImages[room] = image;
                }
            }
        }

        private void CreateCorridorUI(Corridor corridor, RectTransform parent, bool isFullMap = false)
        {
            GameObject corridorObj;

            if (corridorPrefab != null)
            {
                corridorObj = Instantiate(corridorPrefab, parent);
            }
            else
            {
                corridorObj = new GameObject($"Corridor_{corridor.RoomA.Id}_{corridor.RoomB.Id}");
                corridorObj.transform.SetParent(parent, false);

                Image img = corridorObj.AddComponent<Image>();
                img.color = corridorColor;
            }

            RectTransform rt = corridorObj.GetComponent<RectTransform>();

            float scale = isFullMap ? 1.5f : 1f;

            // Calculate position and size based on connected rooms
            Vector2 posA = new Vector2(
                corridor.RoomA.GridPosition.x * roomSpacing * scale,
                corridor.RoomA.GridPosition.y * roomSpacing * scale
            );
            Vector2 posB = new Vector2(
                corridor.RoomB.GridPosition.x * roomSpacing * scale,
                corridor.RoomB.GridPosition.y * roomSpacing * scale
            );

            Vector2 midpoint = (posA + posB) / 2f;
            Vector2 diff = posB - posA;
            float length = diff.magnitude - roomSize * scale;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            rt.anchoredPosition = midpoint;
            rt.sizeDelta = new Vector2(length, corridorWidth * scale);
            rt.localRotation = Quaternion.Euler(0, 0, angle);

            // Set sibling index to render behind rooms
            rt.SetAsFirstSibling();
        }

        private void CreatePlayerMarker()
        {
            // Main minimap marker
            if (roomsParent != null)
            {
                GameObject markerObj;

                if (playerMarkerPrefab != null)
                {
                    markerObj = Instantiate(playerMarkerPrefab, roomsParent);
                }
                else
                {
                    markerObj = new GameObject("PlayerMarker");
                    markerObj.transform.SetParent(roomsParent, false);

                    Image img = markerObj.AddComponent<Image>();
                    img.color = playerMarkerColor;
                }

                playerMarker = markerObj.GetComponent<RectTransform>();
                playerMarker.sizeDelta = new Vector2(roomSize * 0.4f, roomSize * 0.4f);

                // Render on top
                playerMarker.SetAsLastSibling();
            }

            // Full map marker
            if (fullMapRoomsParent != null)
            {
                GameObject markerObj;

                if (playerMarkerPrefab != null)
                {
                    markerObj = Instantiate(playerMarkerPrefab, fullMapRoomsParent);
                }
                else
                {
                    markerObj = new GameObject("PlayerMarker_FullMap");
                    markerObj.transform.SetParent(fullMapRoomsParent, false);

                    Image img = markerObj.AddComponent<Image>();
                    img.color = playerMarkerColor;
                }

                fullMapPlayerMarker = markerObj.GetComponent<RectTransform>();
                fullMapPlayerMarker.sizeDelta = new Vector2(roomSize * 0.6f, roomSize * 0.6f);
                fullMapPlayerMarker.SetAsLastSibling();
            }
        }

        #endregion

        #region Visual Updates

        private void UpdateRoomVisual(Room room)
        {
            if (!roomImages.TryGetValue(room, out Image image))
                return;

            // Determine color based on state
            Color targetColor;

            if (!room.IsVisible)
            {
                // Hidden - make invisible or very dark
                targetColor = Color.clear;
            }
            else if (MapController.Instance?.CurrentRoom == room)
            {
                targetColor = currentRoomColor;
            }
            else if (!room.IsExplored)
            {
                targetColor = unexploredRoomColor;
            }
            else
            {
                // Explored - color by room type
                targetColor = room.Type switch
                {
                    RoomType.Entrance => entranceColor,
                    _ => exploredRoomColor
                };
            }

            image.color = targetColor;
        }

        private void UpdatePlayerMarkerPosition(Room room)
        {
            if (room == null) return;

            Vector2 position = new Vector2(
                room.GridPosition.x * roomSpacing,
                room.GridPosition.y * roomSpacing
            );

            if (playerMarker != null)
            {
                playerMarker.anchoredPosition = position;
            }

            if (fullMapPlayerMarker != null)
            {
                fullMapPlayerMarker.anchoredPosition = position * 1.5f; // Full map scale
            }
        }

        /// <summary>
        /// Refresh all room visuals
        /// </summary>
        public void RefreshAllRoomVisuals()
        {
            if (currentLayout == null) return;

            foreach (var room in currentLayout.Rooms)
            {
                UpdateRoomVisual(room);
            }
        }

        #endregion

        #region Full Map Toggle

        /// <summary>
        /// Toggle the full map overlay
        /// </summary>
        public void ToggleFullMap()
        {
            isFullMapOpen = !isFullMapOpen;

            if (fullMapPanel != null)
            {
                fullMapPanel.SetActive(isFullMapOpen);
            }

            // Pause game while map is open (optional)
            // Time.timeScale = isFullMapOpen ? 0f : 1f;

            Debug.Log($"Full map: {(isFullMapOpen ? "Open" : "Closed")}");
        }

        /// <summary>
        /// Open the full map
        /// </summary>
        public void OpenFullMap()
        {
            if (!isFullMapOpen)
                ToggleFullMap();
        }

        /// <summary>
        /// Close the full map
        /// </summary>
        public void CloseFullMap()
        {
            if (isFullMapOpen)
                ToggleFullMap();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Center the minimap on the current room
        /// </summary>
        public void CenterOnCurrentRoom()
        {
            Room currentRoom = MapController.Instance?.CurrentRoom;
            if (currentRoom == null || roomsParent == null) return;

            Vector2 offset = new Vector2(
                -currentRoom.GridPosition.x * roomSpacing,
                -currentRoom.GridPosition.y * roomSpacing
            );

            roomsParent.anchoredPosition = offset;
        }

        /// <summary>
        /// Show/hide the minimap
        /// </summary>
        public void SetMinimapVisible(bool visible)
        {
            if (minimapContainer != null)
            {
                minimapContainer.gameObject.SetActive(visible);
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Toggle Full Map")]
        private void DebugToggleFullMap()
        {
            ToggleFullMap();
        }

        [ContextMenu("Refresh Minimap")]
        private void DebugRefresh()
        {
            RefreshAllRoomVisuals();
        }

        [ContextMenu("Rebuild Minimap")]
        private void DebugRebuild()
        {
            BuildMinimap();
        }

        #endregion
    }
}
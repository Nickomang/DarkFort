using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using DarkFort.Core;
using DarkFort.UI;

namespace DarkFort.Dungeon
{
    /// <summary>
    /// Controls player movement through the dungeon, room transitions, and exploration state
    /// </summary>
    public class MapController : MonoBehaviour
    {
        #region Singleton
        public static MapController Instance { get; private set; }
        #endregion

        #region Events
        public delegate void RoomEnteredHandler(Room room, bool firstTime);
        public event RoomEnteredHandler OnRoomEntered;

        public delegate void RoomExitedHandler(Room room);
        public event RoomExitedHandler OnRoomExited;

        public delegate void PlayerMovedHandler(Room fromRoom, Room toRoom, Direction direction);
        public event PlayerMovedHandler OnPlayerMoved;

        public delegate void RoomRevealedHandler(Room room);
        public event RoomRevealedHandler OnRoomRevealed;
        #endregion

        #region State
        [Header("Current State")]
        [SerializeField] private Room currentRoom;
        [SerializeField] private DungeonLayout dungeonLayout;

        [Header("Player Visual")]
        [SerializeField] private Transform playerMarker;
        [SerializeField] private float moveAnimationDuration = 0.3f;

        [Header("Visibility Settings")]
        [SerializeField] private bool revealAdjacentRooms = true;
        [SerializeField] private int visibilityRadius = 1; // How many rooms away to reveal
        #endregion

        #region Properties
        public Room CurrentRoom => currentRoom;
        public DungeonLayout DungeonLayout => dungeonLayout;
        public bool CanMove => GameManager.Instance?.CurrentGameState == GameState.Exploring
                               && (Inventory.Instance == null || !Inventory.Instance.IsMerchantModeActive);
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
                DungeonGenerator.Instance.OnDungeonGenerated -= HandleDungeonGenerated;
            }

            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            // Subscribe to dungeon generation
            if (DungeonGenerator.Instance != null)
            {
                DungeonGenerator.Instance.OnDungeonGenerated += HandleDungeonGenerated;
            }
        }

        private void Update()
        {
            // Handle movement input
            if (CanMove)
            {
                HandleMovementInput();
            }
        }
        #endregion

        #region Initialization

        private void HandleDungeonGenerated(DungeonLayout layout)
        {
            InitializeDungeon(layout);
        }

        /// <summary>
        /// Initialize the map controller with a dungeon layout
        /// </summary>
        public void InitializeDungeon(DungeonLayout layout)
        {
            // Guard against double initialization
            if (dungeonLayout == layout && currentRoom != null)
            {
                Debug.Log("MapController already initialized with this layout, skipping");
                return;
            }

            dungeonLayout = layout;

            // Start in entrance room - treat as first time entry to trigger encounter
            Room startRoom = layout.EntranceRoom;
            if (startRoom != null)
            {
                EnterRoom(startRoom, true);  // true = first time, triggers encounter roll
            }
            else
            {
                Debug.LogError("No entrance room in dungeon layout!");
            }
        }

        #endregion

        #region Movement

        // Touch/swipe detection
        private Vector2 touchStartPosition;
        private Vector2 touchEndPosition;
        private bool isSwiping = false;
        private const float minSwipeDistance = 50f; // Minimum swipe distance in pixels

        private void HandleMovementInput()
        {
            if (currentRoom == null) return;

            // Handle keyboard input
            HandleKeyboardInput();

            // Handle touch/swipe input
            HandleTouchInput();
        }

        private void HandleKeyboardInput()
        {
            // Get keyboard (null check for safety)
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Keyboard input
            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                TryMove(Direction.North);
            else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                TryMove(Direction.South);
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                TryMove(Direction.East);
            else if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                TryMove(Direction.West);
        }

        /// <summary>
        /// Check if a screen position is over a UI element
        /// </summary>
        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            eventData.position = screenPosition;

            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current?.RaycastAll(eventData, results);

            return results.Count > 0;
        }

        private void HandleTouchInput()
        {
            // Check for touch input (mobile)
            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
            {
                var touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];

                switch (touch.phase)
                {
                    case UnityEngine.InputSystem.TouchPhase.Began:
                        // Don't start swipe if touching UI
                        if (!IsPointerOverUI(touch.screenPosition))
                        {
                            touchStartPosition = touch.screenPosition;
                            isSwiping = true;
                        }
                        else
                        {
                            isSwiping = false;
                        }
                        break;

                    case UnityEngine.InputSystem.TouchPhase.Ended:
                        if (isSwiping)
                        {
                            touchEndPosition = touch.screenPosition;
                            DetectSwipe();
                            isSwiping = false;
                        }
                        break;

                    case UnityEngine.InputSystem.TouchPhase.Canceled:
                        isSwiping = false;
                        break;
                }
            }

            // Also support mouse swipe for testing on desktop
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    // Don't start swipe if clicking on UI
                    if (!IsPointerOverUI(mouse.position.ReadValue()))
                    {
                        touchStartPosition = mouse.position.ReadValue();
                        isSwiping = true;
                    }
                    else
                    {
                        isSwiping = false;
                    }
                }
                else if (mouse.leftButton.wasReleasedThisFrame && isSwiping)
                {
                    touchEndPosition = mouse.position.ReadValue();
                    DetectSwipe();
                    isSwiping = false;
                }
            }
        }

        private void DetectSwipe()
        {
            Vector2 swipeDelta = touchEndPosition - touchStartPosition;
            float swipeDistance = swipeDelta.magnitude;

            // Check if swipe is long enough
            if (swipeDistance < minSwipeDistance)
            {
                return; // Not a swipe, just a tap
            }

            // Determine swipe direction (use the dominant axis)
            float absX = Mathf.Abs(swipeDelta.x);
            float absY = Mathf.Abs(swipeDelta.y);

            if (absX > absY)
            {
                // Horizontal swipe
                if (swipeDelta.x > 0)
                    TryMove(Direction.East);  // Swipe right
                else
                    TryMove(Direction.West);  // Swipe left
            }
            else
            {
                // Vertical swipe
                if (swipeDelta.y > 0)
                    TryMove(Direction.North); // Swipe up
                else
                    TryMove(Direction.South); // Swipe down
            }
        }

        private void OnEnable()
        {
            // Enable enhanced touch support for mobile
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            // Disable enhanced touch support
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
        }

        /// <summary>
        /// Attempt to move in a direction
        /// </summary>
        public bool TryMove(Direction direction)
        {
            if (!CanMove)
            {
                Debug.Log("Cannot move in current game state");
                return false;
            }

            if (currentRoom == null)
            {
                Debug.LogWarning("No current room!");
                return false;
            }

            // Check if there's a connection in this direction
            if (!currentRoom.HasConnection(direction))
            {
                Debug.Log($"No exit to the {direction}");
                UIManager.Instance?.ShowMessage($"No exit to the {direction}");
                return false;
            }

            Room targetRoom = currentRoom.Connections[direction];

            // Perform the move
            MoveToRoom(targetRoom, direction);
            return true;
        }

        /// <summary>
        /// Move directly to a specific room
        /// </summary>
        public void MoveToRoom(Room targetRoom, Direction? fromDirection = null)
        {
            if (targetRoom == null)
            {
                Debug.LogWarning("Cannot move to null room");
                return;
            }

            Room previousRoom = currentRoom;

            // Exit current room
            if (currentRoom != null)
            {
                OnRoomExited?.Invoke(currentRoom);
            }

            // Determine direction for event
            Direction moveDir = fromDirection ?? Direction.North;

            // Enter new room
            bool isFirstVisit = !targetRoom.IsExplored;
            EnterRoom(targetRoom, isFirstVisit);

            // Fire movement event
            OnPlayerMoved?.Invoke(previousRoom, targetRoom, moveDir);

            // Update player visuals
            UpdatePlayerPosition(targetRoom);
        }

        private void EnterRoom(Room room, bool firstTime)
        {
            currentRoom = room;

            // Mark as explored
            if (!room.IsExplored)
            {
                room.IsExplored = true;
                Player.Instance?.IncrementRoomsExplored();
            }

            // Update visibility
            room.IsVisible = true;

            Debug.Log($"Entered {room.Type} room at {room.GridPosition}" + (firstTime ? " (first time)" : ""));

            // Roll exits for this room if not already rolled (creates new connected rooms)
            if (firstTime && !room.ExitsRolled && DungeonGenerator.Instance != null)
            {
                var newRooms = DungeonGenerator.Instance.RollExitsForRoom(room);

                // Update visualization if new rooms were created
                if (newRooms.Count > 0 && DungeonVisualizer.Instance != null)
                {
                    DungeonVisualizer.Instance.RenderDungeon(DungeonGenerator.Instance.CurrentLayout);
                }

                // Check if player is trapped (no exits available)
                CheckIfTrapped();
            }

            // Roll encounter BEFORE firing event so visualizer shows correct color
            if (firstTime)
            {
                RollEncounterForRoom(room);
            }

            // Fire event (visualizer will now see the correct encounter)
            OnRoomEntered?.Invoke(room, firstTime);

            // Resolve the encounter (combat, items, etc.)
            if (firstTime)
            {
                ResolveRoomEncounter(room);
            }
        }

        private void HandleFirstTimeEntry(Room room)
        {
            // HandleFirstTimeEntry is no longer used - logic moved to RollEncounterForRoom and ResolveRoomEncounter
        }

        #endregion

        #region Room Encounters

        // Pending room for False Omen choice
        private Room pendingFalseOmenRoom;

        /// <summary>
        /// Roll the encounter for a room (sets room.Encounter but doesn't resolve it)
        /// </summary>
        private void RollEncounterForRoom(Room room)
        {
            if (room.Encounter.HasValue) return; // Already rolled

            switch (room.Type)
            {
                case RoomType.Entrance:
                    room.Encounter = RoomEncounterSystem.RollForStartRoomEncounter();
                    break;

                case RoomType.Normal:
                    // Check if False Omen room choice is active
                    if (RoomEncounterSystem.ShouldShowRoomChoice())
                    {
                        // Store room for later, UI will handle the choice
                        pendingFalseOmenRoom = room;
                        UI.UIManager.Instance?.ShowFalseOmenRoomChoice(OnFalseOmenRoomChosen);
                        return; // Don't set encounter yet
                    }

                    room.Encounter = RoomEncounterSystem.RollForEncounter();
                    break;
            }
        }

        /// <summary>
        /// Called when player chooses a room encounter type via False Omen
        /// </summary>
        private void OnFalseOmenRoomChosen(int choice)
        {
            if (pendingFalseOmenRoom == null) return;

            EncounterType chosen = RoomEncounterSystem.GetChosenEncounter(choice);
            pendingFalseOmenRoom.Encounter = chosen;

            // Now resolve the encounter
            ResolveRoomEncounter(pendingFalseOmenRoom);

            // Update visualizer to show correct room color
            if (DungeonVisualizer.Instance != null && DungeonGenerator.Instance != null)
            {
                DungeonVisualizer.Instance.RenderDungeon(DungeonGenerator.Instance.CurrentLayout);
            }

            pendingFalseOmenRoom = null;
        }

        /// <summary>
        /// Resolve the encounter that was rolled for a room
        /// </summary>
        private void ResolveRoomEncounter(Room room)
        {
            if (!room.Encounter.HasValue) return;

            EncounterType encounter = room.Encounter.Value;

            // Resolve the encounter
            RoomEncounterSystem.ResolveEncounter(encounter);

            // Mark as cleared if not a combat encounter
            bool isCombat = encounter == EncounterType.WeakMonster ||
                           encounter == EncounterType.ToughMonster ||
                           encounter == EncounterType.StartWeakMonster;

            if (!isCombat)
            {
                room.IsCleared = true;
            }
        }

        #endregion

        #region Visibility

        private void RevealAdjacentRooms(Room centerRoom)
        {
            foreach (var connection in centerRoom.Connections)
            {
                Room adjacentRoom = connection.Value;
                if (!adjacentRoom.IsVisible)
                {
                    adjacentRoom.IsVisible = true;
                    OnRoomRevealed?.Invoke(adjacentRoom);
                }
            }
        }

        /// <summary>
        /// Get all currently visible rooms
        /// </summary>
        public List<Room> GetVisibleRooms()
        {
            if (dungeonLayout == null) return new List<Room>();
            return dungeonLayout.Rooms.FindAll(r => r.IsVisible);
        }

        /// <summary>
        /// Get all explored rooms
        /// </summary>
        public List<Room> GetExploredRooms()
        {
            if (dungeonLayout == null) return new List<Room>();
            return dungeonLayout.Rooms.FindAll(r => r.IsExplored);
        }

        /// <summary>
        /// Get count of unexplored but visible rooms
        /// </summary>
        public int GetUnexploredVisibleRoomCount()
        {
            if (dungeonLayout == null) return 0;
            return dungeonLayout.Rooms.Count(r => r.IsVisible && !r.IsExplored);
        }

        #endregion

        #region Player Visual

        private void UpdatePlayerPosition(Room room)
        {
            if (playerMarker == null) return;

            // Calculate world position from grid position
            // This will be coordinated with DungeonVisualizer
            Vector3 targetPos = GridToWorldPosition(room.GridPosition);

            // Animate movement
            StartCoroutine(AnimatePlayerMovement(targetPos));
        }

        private System.Collections.IEnumerator AnimatePlayerMovement(Vector3 targetPos)
        {
            if (playerMarker == null) yield break;

            Vector3 startPos = playerMarker.position;
            float elapsed = 0f;

            while (elapsed < moveAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / moveAnimationDuration);
                playerMarker.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            playerMarker.position = targetPos;
        }

        /// <summary>
        /// Convert grid position to world position
        /// Override this based on your tilemap/visual setup
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            // Default: 10 units per grid cell (rooms are spaced apart)
            float spacing = 10f;
            return new Vector3(gridPos.x * spacing, gridPos.y * spacing, 0);
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Check if player can move in a direction
        /// </summary>
        public bool CanMoveInDirection(Direction direction)
        {
            return currentRoom != null && currentRoom.Connections != null && currentRoom.HasConnection(direction);
        }

        /// <summary>
        /// Get available movement directions from current room
        /// </summary>
        public List<Direction> GetAvailableDirections()
        {
            if (currentRoom == null) return new List<Direction>();
            return new List<Direction>(currentRoom.Connections.Keys);
        }

        /// <summary>
        /// Check if the player has any unexplored exits available in the entire dungeon
        /// If not, the player is trapped and loses
        /// </summary>
        private void CheckIfTrapped()
        {
            if (dungeonLayout == null) return;

            // Check all visited rooms for any unvisited connections
            foreach (var room in dungeonLayout.Rooms)
            {
                if (!room.IsExplored) continue; // Only check rooms we've been to

                foreach (var connection in room.Connections)
                {
                    Room connectedRoom = connection.Value;
                    if (connectedRoom != null && !connectedRoom.IsExplored)
                    {
                        // Found an unexplored exit - player is not trapped
                        return;
                    }
                }
            }

            // No unexplored exits found - check if we're in the current room with no exits
            if (currentRoom != null && currentRoom.Connections.Count == 0)
            {
                Debug.Log("Player is trapped! No exits available.");
                UIManager.Instance?.ShowMessage("The dungeon has collapsed around you. There is no escape...", MessageType.Danger);
                GameManager.Instance?.EndGame(false);
                return;
            }

            // All reachable rooms have been visited - player is trapped
            bool hasUnvisitedConnection = false;
            if (currentRoom != null)
            {
                foreach (var connection in currentRoom.Connections)
                {
                    if (connection.Value != null && !connection.Value.IsExplored)
                    {
                        hasUnvisitedConnection = true;
                        break;
                    }
                }
            }

            if (!hasUnvisitedConnection && currentRoom != null && currentRoom.ExitsRolled)
            {
                // Check entire dungeon for any unvisited rooms that are reachable
                bool anyUnvisited = dungeonLayout.Rooms.Any(r => !r.IsExplored);
                if (!anyUnvisited || !HasPathToUnvisitedRoom())
                {
                    Debug.Log("Player is trapped! All reachable rooms explored with no new exits.");
                    UIManager.Instance?.ShowMessage("You've explored every path. The dungeon offers no escape...", MessageType.Danger);
                    GameManager.Instance?.EndGame(false);
                }
            }
        }

        /// <summary>
        /// Check if there's a path from current room to any unvisited room
        /// </summary>
        private bool HasPathToUnvisitedRoom()
        {
            if (currentRoom == null || dungeonLayout == null) return false;

            HashSet<Room> visited = new HashSet<Room>();
            Queue<Room> toCheck = new Queue<Room>();
            toCheck.Enqueue(currentRoom);
            visited.Add(currentRoom);

            while (toCheck.Count > 0)
            {
                Room room = toCheck.Dequeue();

                foreach (var connection in room.Connections)
                {
                    Room connected = connection.Value;
                    if (connected == null || visited.Contains(connected)) continue;

                    if (!connected.IsExplored)
                    {
                        return true; // Found an unvisited room we can reach
                    }

                    visited.Add(connected);
                    toCheck.Enqueue(connected);
                }
            }

            return false;
        }

        /// <summary>
        /// Mark current room as cleared (e.g., after winning combat)
        /// </summary>
        public void MarkCurrentRoomCleared()
        {
            if (currentRoom != null)
            {
                currentRoom.IsCleared = true;
                Debug.Log($"Room {currentRoom.Id} marked as cleared");
            }
        }

        /// <summary>
        /// Get room in a direction from current room (or null)
        /// </summary>
        public Room GetRoomInDirection(Direction direction)
        {
            if (currentRoom == null || !currentRoom.HasConnection(direction))
                return null;
            return currentRoom.Connections[direction];
        }

        #endregion

        #region Debug

        [ContextMenu("Print Current Room")]
        private void DebugPrintCurrentRoom()
        {
            if (currentRoom == null)
            {
                Debug.Log("No current room");
                return;
            }

            Debug.Log($"=== CURRENT ROOM ===");
            Debug.Log(currentRoom.ToString());
            Debug.Log($"Explored: {currentRoom.IsExplored}");
            Debug.Log($"Cleared: {currentRoom.IsCleared}");
            Debug.Log($"Encounter: {currentRoom.Encounter?.ToString() ?? "None"}");
        }

        [ContextMenu("Print Visible Rooms")]
        private void DebugPrintVisibleRooms()
        {
            var visible = GetVisibleRooms();
            Debug.Log($"=== VISIBLE ROOMS ({visible.Count}) ===");
            foreach (var room in visible)
            {
                Debug.Log($"  {room}");
            }
        }

        [ContextMenu("Move North")]
        private void DebugMoveNorth() => TryMove(Direction.North);

        [ContextMenu("Move South")]
        private void DebugMoveSouth() => TryMove(Direction.South);

        [ContextMenu("Move East")]
        private void DebugMoveEast() => TryMove(Direction.East);

        [ContextMenu("Move West")]
        private void DebugMoveWest() => TryMove(Direction.West);

        #endregion
    }
}
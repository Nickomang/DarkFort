using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DarkFort.Dungeon
{
    #region Data Structures

    /// <summary>
    /// Cardinal directions for room exits
    /// </summary>
    public enum Direction
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    /// <summary>
    /// Type of room content/purpose
    /// </summary>
    public enum RoomType
    {
        Entrance,       // Starting room - special encounter
        Normal          // Standard room - roll for encounter
    }

    /// <summary>
    /// Represents a single room in the dungeon
    /// </summary>
    [System.Serializable]
    public class Room
    {
        public int Id;
        public Vector2Int GridPosition;
        public RoomType Type;
        public bool IsExplored;
        public bool IsCleared;          // Combat/encounter resolved
        public bool IsVisible;          // Currently visible to player
        public bool ExitsRolled;        // Whether exits have been rolled for this room
        public EncounterType? Encounter; // Rolled encounter (null if not yet rolled)
        public int MaxExits;            // Maximum exits this room can have (rolled when entered)

        // Connections to adjacent rooms (by direction)
        public Dictionary<Direction, Room> Connections = new Dictionary<Direction, Room>();

        // Room dimensions for visualization
        public int Width = 3;   // In tiles
        public int Height = 3;  // In tiles

        public Room(int id, Vector2Int position, RoomType type = RoomType.Normal)
        {
            Id = id;
            GridPosition = position;
            Type = type;
            IsExplored = false;
            IsCleared = false;
            IsVisible = false;
            ExitsRolled = false;
            Encounter = null;
            MaxExits = 0;  // Unknown until rolled
            Connections = new Dictionary<Direction, Room>();
        }

        public bool HasConnection(Direction dir) => Connections != null && Connections.ContainsKey(dir);

        public int ConnectionCount => Connections?.Count ?? 0;

        /// <summary>
        /// Can this room accept more exits? Only valid after exits are rolled.
        /// </summary>
        public bool CanAddExit => ExitsRolled && ConnectionCount < MaxExits;

        public List<Direction> GetAvailableDirections()
        {
            var available = new List<Direction>();
            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                if (Connections == null || !Connections.ContainsKey(dir))
                    available.Add(dir);
            }
            return available;
        }

        public override string ToString()
        {
            string connections = Connections != null ? string.Join(", ", Connections.Keys) : "none";
            string exitInfo = ExitsRolled ? $"Max: {MaxExits}" : "Not rolled";
            return $"Room {Id} at {GridPosition} ({Type}) - Exits: [{connections}] ({exitInfo})";
        }
    }

    /// <summary>
    /// Represents a corridor connecting two rooms
    /// </summary>
    [System.Serializable]
    public class Corridor
    {
        public Room RoomA;
        public Room RoomB;
        public Direction DirectionFromA;
        public bool IsExplored;

        public Corridor(Room a, Room b, Direction dirFromA)
        {
            RoomA = a;
            RoomB = b;
            DirectionFromA = dirFromA;
            IsExplored = false;
        }
    }

    /// <summary>
    /// Complete dungeon layout
    /// </summary>
    [System.Serializable]
    public class DungeonLayout
    {
        public List<Room> Rooms = new List<Room>();
        public List<Corridor> Corridors = new List<Corridor>();
        public Room EntranceRoom;
        public int Seed;

        public Room GetRoomAt(Vector2Int position)
        {
            return Rooms.Find(r => r.GridPosition == position);
        }

        public Room GetRoomById(int id)
        {
            return Rooms.Find(r => r.Id == id);
        }
    }

    #endregion

    /// <summary>
    /// Generates procedural dungeon layouts for Dark Fort.
    /// Exit rolling follows Dark Fort rules:
    /// - 1 in 4 (25%): Dead end (1 exit)
    /// - 1 in 4 (25%): 2 exits  
    /// - 1 in 2 (50%): 3 exits
    /// </summary>
    public class DungeonGenerator : MonoBehaviour
    {
        #region Singleton
        public static DungeonGenerator Instance { get; private set; }
        #endregion

        #region Events
        public delegate void DungeonGeneratedHandler(DungeonLayout layout);
        public event DungeonGeneratedHandler OnDungeonGenerated;
        #endregion

        #region Settings
        [Header("Generation Settings")]
        [SerializeField] private int initialRoomCount = 10;

        [Header("Room Size Settings")]
        [SerializeField] private int minRoomSize = 2;
        [SerializeField] private int maxRoomSize = 4;

        [Header("Current Dungeon")]
        [SerializeField] private DungeonLayout currentLayout;

        // Track occupied positions and next room ID
        private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();
        private int nextRoomId = 0;
        #endregion

        #region Properties
        public DungeonLayout CurrentLayout => currentLayout;
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
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Generate a new dungeon with optional seed
        /// </summary>
        public DungeonLayout GenerateDungeon(int seed = 0)
        {
            // Set seed
            if (seed == 0)
                seed = System.Environment.TickCount;

            Random.InitState(seed);
            Debug.Log($"Generating dungeon with seed: {seed}");

            currentLayout = new DungeonLayout { Seed = seed };
            occupiedPositions.Clear();
            nextRoomId = 0;

            // Create entrance room at origin
            Room entrance = CreateRoom(Vector2Int.zero, RoomType.Entrance);
            entrance.MaxExits = 3; // Entrance always has 3 potential exits
            currentLayout.EntranceRoom = entrance;
            currentLayout.Rooms.Add(entrance);
            occupiedPositions.Add(Vector2Int.zero);
            entrance.IsExplored = true;
            entrance.IsCleared = true;
            entrance.IsVisible = true;

            // Generate initial rooms using proper exit rolling
            GenerateInitialRooms();

            // Assign random sizes to rooms
            AssignRoomSizes();

            Debug.Log($"Dungeon generated: {currentLayout.Rooms.Count} rooms, {currentLayout.Corridors.Count} corridors");

            OnDungeonGenerated?.Invoke(currentLayout);
            return currentLayout;
        }

        /// <summary>
        /// Get the room the player should start in
        /// </summary>
        public Room GetStartingRoom()
        {
            return currentLayout?.EntranceRoom;
        }

        /// <summary>
        /// Roll exits for a room when the player enters it.
        /// Creates new connected rooms based on the roll.
        /// Returns list of newly created rooms.
        /// </summary>
        public List<Room> RollExitsForRoom(Room room)
        {
            List<Room> newRooms = new List<Room>();

            if (room == null || room.ExitsRolled) return newRooms;

            // Check if there are other unexplored rooms (excluding this one)
            bool hasOtherUnexploredRooms = currentLayout.Rooms.Any(r => r != room && !r.IsExplored);

            // Roll for number of exits
            int exitRoll = RollExitCount(allowDeadEnd: hasOtherUnexploredRooms);
            room.MaxExits = exitRoll;
            room.ExitsRolled = true;

            Debug.Log($"Room {room.Id}: Rolled {exitRoll} max exits (currently has {room.ConnectionCount}), other unexplored rooms: {hasOtherUnexploredRooms}");

            // Calculate how many NEW exits we need to create
            // (subtract the entrance we came from)
            int newExitsNeeded = exitRoll - room.ConnectionCount;

            if (newExitsNeeded <= 0)
            {
                Debug.Log($"Room {room.Id}: No new exits needed (dead end or already has enough)");
                return newRooms;
            }

            // Get available directions (walls without connections)
            var availableDirections = room.GetAvailableDirections();

            // Shuffle available directions
            for (int i = availableDirections.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = availableDirections[i];
                availableDirections[i] = availableDirections[j];
                availableDirections[j] = temp;
            }

            // Create new exits
            int exitsCreated = 0;
            foreach (Direction dir in availableDirections)
            {
                if (exitsCreated >= newExitsNeeded) break;

                Vector2Int newPos = GetAdjacentPosition(room.GridPosition, dir);

                // Check if position is occupied
                if (occupiedPositions.Contains(newPos))
                {
                    // Try to connect to existing room
                    Room existingRoom = currentLayout.GetRoomAt(newPos);
                    if (existingRoom != null && !room.HasConnection(dir))
                    {
                        // Connect to existing room (this uses up one of our exits)
                        ConnectRooms(room, existingRoom, dir);
                        existingRoom.IsVisible = true;
                        exitsCreated++;
                        Debug.Log($"Room {room.Id}: Connected to existing Room {existingRoom.Id} via {dir}");
                    }
                    continue;
                }

                // Create new room (exits not yet rolled - will be rolled when player enters)
                Room newRoom = CreateRoom(newPos, RoomType.Normal);
                newRoom.IsVisible = true;  // Visible but not explored
                currentLayout.Rooms.Add(newRoom);
                occupiedPositions.Add(newPos);
                AssignRoomSize(newRoom);

                // Connect rooms
                ConnectRooms(room, newRoom, dir);

                newRooms.Add(newRoom);
                exitsCreated++;

                Debug.Log($"Room {room.Id}: Created new Room {newRoom.Id} at {newPos} via {dir}");
            }

            Debug.Log($"Room {room.Id}: Created {exitsCreated} new exits, total connections: {room.ConnectionCount}");

            return newRooms;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Roll for number of ADDITIONAL exits per Dark Fort rules:
        /// d4 roll determines additional exits beyond the entrance:
        /// - Roll 1 (25%): Dead end - 0 additional exits (only if allowDeadEnd is true)
        /// - Roll 2 (25%): 1 additional exit (2 total)
        /// - Roll 3 (25%): 2 additional exits (3 total)
        /// - Roll 4 (25%): 3 additional exits (4 total)
        /// 
        /// If allowDeadEnd is false and we roll a 1, we re-roll.
        /// MaxExits = entrance + additional exits
        /// </summary>
        private int RollExitCount(bool allowDeadEnd = true)
        {
            int roll = UnityEngine.Random.Range(1, 5); // d4: 1-4

            // If dead ends not allowed and we rolled one, re-roll until we don't
            if (!allowDeadEnd && roll == 1)
            {
                Debug.Log("Dead end not allowed (no other unexplored rooms) - re-rolling");
                roll = UnityEngine.Random.Range(2, 5); // d3: 2-4
            }

            int additionalExits;

            switch (roll)
            {
                case 1:
                    additionalExits = 0; // Dead end - no additional exits (25%)
                    break;
                case 2:
                    additionalExits = 1; // 1 additional exit (25%)
                    break;
                case 3:
                    additionalExits = 2; // 2 additional exits (25%)
                    break;
                default:
                    additionalExits = 3; // 3 additional exits (25%)
                    break;
            }

            // MaxExits = entrance (1) + additional exits
            int maxExits = 1 + additionalExits;

            Debug.Log($"Exit roll: d4={roll} -> {additionalExits} additional exits -> MaxExits={maxExits}");
            return maxExits;
        }

        /// <summary>
        /// Initialize dungeon with just the entrance room.
        /// Other rooms are created as player explores.
        /// </summary>
        private void GenerateInitialRooms()
        {
            // Roll exits for entrance room - don't set ExitsRolled manually,
            // let RollExitsForRoom handle everything
            var entranceRoom = currentLayout.EntranceRoom;

            Debug.Log($"Creating initial exits from entrance room");

            // This will roll exits and create connected rooms
            var newRooms = RollExitsForRoom(entranceRoom);

            Debug.Log($"Initial dungeon: Entrance + {newRooms.Count} connected rooms (total: {currentLayout.Rooms.Count})");
        }

        private Room CreateRoom(Vector2Int position, RoomType type)
        {
            Room room = new Room(nextRoomId++, position, type);
            return room;
        }

        private void ConnectRooms(Room roomA, Room roomB, Direction dirFromA)
        {
            Direction opposite = GetOppositeDirection(dirFromA);

            roomA.Connections[dirFromA] = roomB;
            roomB.Connections[opposite] = roomA;

            Corridor corridor = new Corridor(roomA, roomB, dirFromA);
            currentLayout.Corridors.Add(corridor);
        }

        private void AssignRoomSizes()
        {
            foreach (var room in currentLayout.Rooms)
            {
                AssignRoomSize(room);
            }
        }

        private void AssignRoomSize(Room room)
        {
            // Entrance is medium sized
            if (room.Type == RoomType.Entrance)
            {
                room.Width = 3;
                room.Height = 3;
            }
            // Normal rooms are random
            else
            {
                room.Width = Random.Range(minRoomSize, maxRoomSize + 1);
                room.Height = Random.Range(minRoomSize, maxRoomSize + 1);
            }
        }

        #endregion

        #region Utility Methods

        public static Vector2Int GetAdjacentPosition(Vector2Int pos, Direction dir)
        {
            return dir switch
            {
                Direction.North => pos + Vector2Int.up,
                Direction.East => pos + Vector2Int.right,
                Direction.South => pos + Vector2Int.down,
                Direction.West => pos + Vector2Int.left,
                _ => pos
            };
        }

        public static Direction GetOppositeDirection(Direction dir)
        {
            return dir switch
            {
                Direction.North => Direction.South,
                Direction.East => Direction.West,
                Direction.South => Direction.North,
                Direction.West => Direction.East,
                _ => dir
            };
        }

        public static Vector2Int DirectionToVector(Direction dir)
        {
            return dir switch
            {
                Direction.North => Vector2Int.up,
                Direction.East => Vector2Int.right,
                Direction.South => Vector2Int.down,
                Direction.West => Vector2Int.left,
                _ => Vector2Int.zero
            };
        }

        #endregion

        #region Debug

        [ContextMenu("Generate Test Dungeon")]
        private void DebugGenerateDungeon()
        {
            GenerateDungeon();
        }

        [ContextMenu("Print Dungeon Layout")]
        private void DebugPrintLayout()
        {
            if (currentLayout == null)
            {
                Debug.Log("No dungeon generated");
                return;
            }

            Debug.Log($"=== DUNGEON LAYOUT (Seed: {currentLayout.Seed}) ===");
            Debug.Log($"Total Rooms: {currentLayout.Rooms.Count}");
            Debug.Log($"Total Corridors: {currentLayout.Corridors.Count}");

            // Count exit distributions
            int deadEnds = 0, twoExits = 0, threeExits = 0;
            foreach (var room in currentLayout.Rooms)
            {
                Debug.Log(room.ToString());
                switch (room.MaxExits)
                {
                    case 1: deadEnds++; break;
                    case 2: twoExits++; break;
                    case 3: threeExits++; break;
                }
            }
            Debug.Log($"Exit distribution - Dead ends: {deadEnds}, Two exits: {twoExits}, Three exits: {threeExits}");
        }

        [ContextMenu("Print Exit Statistics")]
        private void DebugPrintExitStats()
        {
            if (currentLayout == null)
            {
                Debug.Log("No dungeon generated");
                return;
            }

            int deadEnds = 0, twoExits = 0, threeExits = 0;
            foreach (var room in currentLayout.Rooms)
            {
                if (room.Type == RoomType.Entrance) continue; // Skip entrance
                switch (room.MaxExits)
                {
                    case 1: deadEnds++; break;
                    case 2: twoExits++; break;
                    case 3: threeExits++; break;
                }
            }

            int total = deadEnds + twoExits + threeExits;
            Debug.Log($"=== EXIT STATISTICS (excluding entrance) ===");
            Debug.Log($"Dead ends (1 exit): {deadEnds} ({100f * deadEnds / total:F1}%) - Expected: 25%");
            Debug.Log($"Two exits: {twoExits} ({100f * twoExits / total:F1}%) - Expected: 25%");
            Debug.Log($"Three exits: {threeExits} ({100f * threeExits / total:F1}%) - Expected: 50%");
        }

        #endregion
    }
}
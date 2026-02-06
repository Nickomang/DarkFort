using UnityEngine;
using DarkFort.Core;
using DarkFort.Dungeon;

namespace DarkFort
{
    /// <summary>
    /// Bootstrap script that initializes the game and generates a dungeon on start.
    /// Attach this to a GameObject in your Gameplay scene.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int dungeonSeed = 0; // 0 = random seed
        [SerializeField] private bool autoGenerateOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool logDebugInfo = true;

        private void Start()
        {
            if (autoGenerateOnStart)
            {
                InitializeGame();
            }
        }

        /// <summary>
        /// Initialize the game systems and generate a dungeon
        /// </summary>
        public void InitializeGame()
        {
            if (logDebugInfo)
                Debug.Log("=== GAME BOOTSTRAP STARTING ===");

            // Step 1: Ensure GameManager is ready
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeGameState(GameState.Exploring);
                if (logDebugInfo)
                    Debug.Log("GameManager ready");
            }
            else
            {
                Debug.LogWarning("GameManager not found!");
            }

            // Step 2: Generate dungeon
            DungeonLayout layout = null;
            if (DungeonGenerator.Instance != null)
            {
                int seed = dungeonSeed > 0 ? dungeonSeed : 0;
                layout = DungeonGenerator.Instance.GenerateDungeon(seed);

                if (logDebugInfo)
                {
                    Debug.Log($"Dungeon generated with {layout.Rooms.Count} rooms");
                    Debug.Log($"Entrance at: {layout.EntranceRoom?.GridPosition}");
                }
            }
            else
            {
                Debug.LogError("DungeonGenerator not found! Make sure it's in the scene.");
                return;
            }

            // Step 3: Initialize MapController with the dungeon
            if (MapController.Instance != null && layout != null)
            {
                MapController.Instance.InitializeDungeon(layout);
                if (logDebugInfo)
                    Debug.Log("MapController initialized with dungeon");
            }
            else
            {
                Debug.LogWarning("MapController not found!");
            }

            // Step 4: Initialize PlayerMarker position
            if (PlayerMarker.Instance != null && layout?.EntranceRoom != null)
            {
                PlayerMarker.Instance.MoveToRoom(layout.EntranceRoom, true);
                if (logDebugInfo)
                    Debug.Log("PlayerMarker placed at entrance");
            }
            else
            {
                Debug.LogWarning("PlayerMarker not found! Create a GameObject with PlayerMarker component.");
            }

            // Step 5: Snap camera to player
            if (DungeonCamera.Instance != null)
            {
                DungeonCamera.Instance.SnapToTarget();
                if (logDebugInfo)
                    Debug.Log("Camera snapped to player");
            }

            if (logDebugInfo)
                Debug.Log("=== GAME BOOTSTRAP COMPLETE ===");
        }

        /// <summary>
        /// Regenerate the dungeon with a new seed
        /// </summary>
        [ContextMenu("Regenerate Dungeon")]
        public void RegenerateDungeon()
        {
            if (DungeonGenerator.Instance != null)
            {
                // Clear existing visualization
                if (DungeonVisualizer.Instance != null)
                {
                    DungeonVisualizer.Instance.ClearDungeon();
                }

                // Generate new dungeon
                DungeonLayout layout = DungeonGenerator.Instance.GenerateDungeon(0);
                Debug.Log($"New dungeon generated with seed: {layout.Seed}");

                // Reinitialize MapController
                if (MapController.Instance != null)
                {
                    MapController.Instance.InitializeDungeon(layout);
                }

                // Move player to entrance
                if (PlayerMarker.Instance != null && layout.EntranceRoom != null)
                {
                    PlayerMarker.Instance.MoveToRoom(layout.EntranceRoom, true);
                }

                // Snap camera
                if (DungeonCamera.Instance != null)
                {
                    DungeonCamera.Instance.SnapToTarget();
                }
            }
        }

        /// <summary>
        /// Regenerate with a specific seed
        /// </summary>
        public void RegenerateDungeonWithSeed(int seed)
        {
            dungeonSeed = seed;

            if (DungeonGenerator.Instance != null)
            {
                if (DungeonVisualizer.Instance != null)
                {
                    DungeonVisualizer.Instance.ClearDungeon();
                }

                DungeonLayout layout = DungeonGenerator.Instance.GenerateDungeon(seed);
                Debug.Log($"Dungeon generated with seed: {seed}");

                if (MapController.Instance != null)
                {
                    MapController.Instance.InitializeDungeon(layout);
                }

                if (PlayerMarker.Instance != null && layout.EntranceRoom != null)
                {
                    PlayerMarker.Instance.MoveToRoom(layout.EntranceRoom, true);
                }

                if (DungeonCamera.Instance != null)
                {
                    DungeonCamera.Instance.SnapToTarget();
                }
            }
        }

        #region Debug Commands

        [ContextMenu("Print Dungeon Info")]
        private void DebugPrintDungeonInfo()
        {
            if (DungeonGenerator.Instance?.CurrentLayout == null)
            {
                Debug.Log("No dungeon generated");
                return;
            }

            var layout = DungeonGenerator.Instance.CurrentLayout;
            Debug.Log($"=== DUNGEON INFO ===");
            Debug.Log($"Seed: {layout.Seed}");
            Debug.Log($"Rooms: {layout.Rooms.Count}");
            Debug.Log($"Corridors: {layout.Corridors.Count}");
            Debug.Log($"Entrance: Room {layout.EntranceRoom?.Id} at {layout.EntranceRoom?.GridPosition}");
        }

        [ContextMenu("Print Current Room")]
        private void DebugPrintCurrentRoom()
        {
            if (MapController.Instance?.CurrentRoom == null)
            {
                Debug.Log("No current room");
                return;
            }

            var room = MapController.Instance.CurrentRoom;
            Debug.Log($"=== CURRENT ROOM ===");
            Debug.Log($"Room {room.Id} ({room.Type})");
            Debug.Log($"Position: {room.GridPosition}");
            Debug.Log($"Explored: {room.IsExplored}, Cleared: {room.IsCleared}");
            Debug.Log($"Connections: {string.Join(", ", room.Connections.Keys)}");
            Debug.Log($"Max Exits: {room.MaxExits}, Current: {room.ConnectionCount}");
        }

        [ContextMenu("Print Player Stats")]
        private void DebugPrintPlayerStats()
        {
            if (Player.Instance == null)
            {
                Debug.Log("No player instance");
                return;
            }

            var p = Player.Instance;
            Debug.Log($"=== PLAYER STATS ===");
            Debug.Log($"Health: {p.CurrentHealth}/{p.MaxHealth}");
            Debug.Log($"Level: {p.CurrentLevel}");
            Debug.Log($"XP: {p.CurrentXP}/{p.XPRequiredForLevelUp}");
            Debug.Log($"Rooms: {p.RoomsExplored}/{p.RoomsRequiredForLevelUp}");
            Debug.Log($"Weapon: {p.EquippedWeapon?.WeaponName ?? "None"}");
        }

        [ContextMenu("Print Player Position")]
        private void DebugPrintPlayerPosition()
        {
            if (PlayerMarker.Instance == null)
            {
                Debug.Log("No PlayerMarker instance");
                return;
            }

            Debug.Log($"=== PLAYER MARKER ===");
            Debug.Log($"Position: {PlayerMarker.Instance.transform.position}");
            Debug.Log($"Target: {PlayerMarker.Instance.TargetPosition}");
            Debug.Log($"Current Room: {PlayerMarker.Instance.CurrentRoom?.Id}");
        }

        #endregion
    }
}
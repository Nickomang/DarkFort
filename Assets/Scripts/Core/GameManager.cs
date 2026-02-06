using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkFort.Core
{
    /// <summary>
    /// Main game manager that persists across scenes and manages overall game state
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Events
        public delegate void GameStateChangedHandler(GameState newState);
        public event GameStateChangedHandler OnGameStateChanged;

        public delegate void PlayerLevelUpHandler(int newLevel);
        public event PlayerLevelUpHandler OnPlayerLevelUp;

        public delegate void GameOverHandler(bool victory);
        public event GameOverHandler OnGameOver;
        #endregion

        #region Properties
        [Header("Game State")]
        [SerializeField] private GameState currentGameState = GameState.MainMenu;

        [Header("Game Settings")]
        [SerializeField] private int targetLevel = 10; // Win condition
        [SerializeField] private int dungeonSeed = 0; // 0 = random seed

        [Header("Runtime Data")]
        [SerializeField] private int currentTurn = 0;
        [SerializeField] private bool isGameActive = false;

        // Public accessors
        public GameState CurrentGameState => currentGameState;
        public int TargetLevel => targetLevel;
        public int CurrentTurn => currentTurn;
        public bool IsGameActive => isGameActive;
        public int DungeonSeed { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Singleton pattern with DontDestroyOnLoad
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeGame();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            // Prevent "objects not cleaned up" warning by clearing instance before quit
            _instance = null;
        }
        #endregion

        #region Initialization
        private void InitializeGame()
        {
            Debug.Log("GameManager initialized");

            // Set random seed if not specified
            if (dungeonSeed == 0)
            {
                DungeonSeed = Random.Range(1, 999999);
            }
            else
            {
                DungeonSeed = dungeonSeed;
            }
        }
        #endregion

        #region Game Flow Methods
        /// <summary>
        /// Start a new game session
        /// </summary>
        public void StartNewGame()
        {
            Debug.Log("Starting new game...");

            currentTurn = 0;
            isGameActive = true;

            // Generate new seed
            DungeonSeed = Random.Range(1, 999999);

            ChangeGameState(GameState.Exploring);

            // Always reload the gameplay scene to ensure clean state
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == "Gameplay" || currentScene == "GameplayScene")
            {
                // Force reload current scene
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            else
            {
                // Load gameplay scene
                SceneManager.LoadScene("Gameplay");
            }
        }

        /// <summary>
        /// Continue from a saved game
        /// </summary>
        public void ContinueGame()
        {
            Debug.Log("Continuing game...");
            isGameActive = true;
            // TODO: Load save data
            ChangeGameState(GameState.Exploring);
        }

        /// <summary>
        /// Pause the current game
        /// </summary>
        public void PauseGame()
        {
            if (isGameActive)
            {
                ChangeGameState(GameState.Paused);
            }
        }

        /// <summary>
        /// Resume from pause
        /// </summary>
        public void ResumeGame()
        {
            if (currentGameState == GameState.Paused)
            {
                ChangeGameState(GameState.Exploring);
            }
        }

        /// <summary>
        /// End the game (victory or defeat)
        /// </summary>
        public void EndGame(bool victory)
        {
            Debug.Log($"Game ended. Victory: {victory}");
            isGameActive = false;
            ChangeGameState(GameState.GameOver);
            OnGameOver?.Invoke(victory);
        }

        /// <summary>
        /// Return to main menu
        /// </summary>
        public void ReturnToMainMenu()
        {
            isGameActive = false;
            ChangeGameState(GameState.MainMenu);
            SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// Quit the application
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("Quitting game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        #endregion

        #region State Management
        /// <summary>
        /// Change the current game state
        /// </summary>
        public void ChangeGameState(GameState newState)
        {
            if (currentGameState == newState) return;

            GameState previousState = currentGameState;
            currentGameState = newState;

            Debug.Log($"Game state changed: {previousState} -> {newState}");
            OnGameStateChanged?.Invoke(newState);

            HandleStateChange(newState);
        }

        private void HandleStateChange(GameState newState)
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;

                case GameState.Exploring:
                    Time.timeScale = 1f;
                    break;

                case GameState.Combat:
                    Time.timeScale = 1f;
                    break;

                case GameState.Merchant:
                    Time.timeScale = 1f;
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;

                case GameState.GameOver:
                    Time.timeScale = 1f;
                    break;
            }
        }
        #endregion

        #region Turn Management
        /// <summary>
        /// Advance to the next turn
        /// </summary>
        public void NextTurn()
        {
            if (!isGameActive) return;

            currentTurn++;
            Debug.Log($"Turn {currentTurn}");
        }
        #endregion

        #region Win Condition
        /// <summary>
        /// Check if player has reached the target level to win
        /// </summary>
        public void CheckWinCondition(int playerLevel)
        {
            if (playerLevel >= targetLevel && isGameActive)
            {
                EndGame(true);
            }
        }

        /// <summary>
        /// Called when player levels up
        /// </summary>
        public void NotifyPlayerLevelUp(int newLevel)
        {
            Debug.Log($"Player reached level {newLevel}");
            OnPlayerLevelUp?.Invoke(newLevel);
            CheckWinCondition(newLevel);
        }
        #endregion

        #region Debug Methods
#if UNITY_EDITOR
        [ContextMenu("Force Victory")]
        private void DebugForceVictory()
        {
            EndGame(true);
        }

        [ContextMenu("Force Defeat")]
        private void DebugForceDefeat()
        {
            EndGame(false);
        }

        [ContextMenu("Print Game State")]
        private void DebugPrintGameState()
        {
            Debug.Log($"=== GAME STATE ===");
            Debug.Log($"State: {currentGameState}");
            Debug.Log($"Turn: {currentTurn}");
            Debug.Log($"Active: {isGameActive}");
            Debug.Log($"Seed: {DungeonSeed}");
            Debug.Log($"Target Level: {targetLevel}");
        }
#endif
        #endregion
    }
}
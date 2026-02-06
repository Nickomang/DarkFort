using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace DarkFort.UI
{
    /// <summary>
    /// Manages the start screen - name entry and scene transition
    /// </summary>
    public class StartScreenManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button playButton;

        [Header("Settings")]
        [SerializeField] private string gameplaySceneName = "GameplayScene";
        [SerializeField] private string defaultPlayerName = "Adventurer";
        [SerializeField] private int maxNameLength = 20;

        // Static so it persists between scenes
        private static string pendingPlayerName;

        /// <summary>
        /// Get the player name entered on the start screen
        /// Call this from the gameplay scene to retrieve the name
        /// </summary>
        public static string GetPendingPlayerName()
        {
            string name = pendingPlayerName;
            pendingPlayerName = null; // Clear after reading
            return name;
        }

        /// <summary>
        /// Set the pending player name (used when restarting to preserve name)
        /// </summary>
        public static void SetPendingPlayerName(string name)
        {
            pendingPlayerName = name;
        }

        /// <summary>
        /// Check if there's a pending player name from the start screen
        /// </summary>
        public static bool HasPendingPlayerName => !string.IsNullOrEmpty(pendingPlayerName);

        private void Start()
        {
            // Set up input field
            if (nameInputField != null)
            {
                nameInputField.characterLimit = maxNameLength;
                nameInputField.onSubmit.AddListener(OnNameSubmit);
            }

            // Set up play button
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayButtonClicked);
            }
        }

        private void OnDestroy()
        {
            // Clean up listeners
            if (nameInputField != null)
            {
                nameInputField.onSubmit.RemoveListener(OnNameSubmit);
            }

            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayButtonClicked);
            }
        }

        /// <summary>
        /// Called when Enter is pressed in the input field
        /// </summary>
        private void OnNameSubmit(string name)
        {
            StartGame();
        }

        /// <summary>
        /// Called when the Play button is clicked
        /// </summary>
        public void OnPlayButtonClicked()
        {
            StartGame();
        }

        /// <summary>
        /// Validate and store the player name, then load the gameplay scene
        /// </summary>
        private void StartGame()
        {
            // Get the name from input field
            string playerName = nameInputField != null ? nameInputField.text : "";

            // Trim whitespace
            playerName = playerName.Trim();

            // Use default if empty
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = defaultPlayerName;
            }

            // Store for retrieval in gameplay scene
            pendingPlayerName = playerName;

            Debug.Log($"Starting game with player name: {playerName}");

            // Load the gameplay scene
            SceneManager.LoadScene(gameplaySceneName);
        }
    }
}
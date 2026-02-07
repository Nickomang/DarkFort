using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DarkFort.Combat;

namespace DarkFort.UI
{
    /// <summary>
    /// Central UI manager for Dark Fort
    /// Handles HUD, messages, combat UI, menus, and game state displays
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        public static UIManager Instance { get; private set; }
        #endregion

        #region Events
        public delegate void UIStateChangedHandler(UIState newState);
        public event UIStateChangedHandler OnUIStateChanged;
        #endregion

        #region UI References - HUD
        [Header("HUD - Player Stats")]
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI xpText;
        [SerializeField] private Slider xpBar;
        [SerializeField] private TextMeshProUGUI roomsExploredText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI weaponText;

        [Header("HUD - Mobile Buttons")]
        [SerializeField] private GameObject levelUpButton;  // Only shown when 40+ silver
        [SerializeField] private GameObject rerollButton;   // Only shown when False Omen reroll is available

        [Header("HUD - Movement")]
        [SerializeField] private GameObject northArrow;
        [SerializeField] private GameObject southArrow;
        [SerializeField] private GameObject eastArrow;
        [SerializeField] private GameObject westArrow;
        #endregion

        #region UI References - Messages
        [Header("Message System")]
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float defaultMessageDuration = 3f;
        [SerializeField] private int maxMessageHistory = 50;

        [Header("Game Log")]
        [SerializeField] private GameObject gameLogPanel;        // Always-visible log panel
        [SerializeField] private Transform gameLogContent;       // Content container for message objects (child of ScrollRect)
        [SerializeField] private ScrollRect gameLogScrollRect;   // ScrollRect for auto-scrolling
        [SerializeField] private int maxGameLogLines = 20;       // Max messages to show in game log
        [SerializeField] private GameObject gameLogMessagePrefab; // Prefab for individual messages (optional, will create if null)
        [SerializeField] private float gameLogFontSize = 14f;    // Font size for log messages

        [Header("Full Message Log (Toggle)")]
        [SerializeField] private GameObject messageLogPanel;     // Full history panel (toggle with L)
        [SerializeField] private TextMeshProUGUI messageLogText;
        [SerializeField] private ScrollRect messageLogScrollRect;
        #endregion

        #region UI References - Combat
        [Header("Combat UI")]
        [SerializeField] private GameObject combatPanel;
        [SerializeField] private TextMeshProUGUI monsterNameText;
        [SerializeField] private TextMeshProUGUI monsterStatsText;
        [SerializeField] private TextMeshProUGUI monsterDescriptionText;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button fleeButton;
        [SerializeField] private Image monsterImage;
        #endregion

        #region UI References - Item Bar
        [Header("Item Bar (HUD)")]
        [SerializeField] private Transform itemBarContainer;        // Parent for item slots (top-left)
        [SerializeField] private GameObject itemSlotPrefab;         // Prefab for each slot
        [SerializeField] private int maxItemBarSlots = 10;

        [Header("Item Bar Appearance")]
        [SerializeField] private Color itemSlotNormalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color itemSlotHoverColor = new Color(0.3f, 0.3f, 0.4f, 0.9f);
        [SerializeField] private Vector2 itemSlotSize = new Vector2(100, 100);

        [Header("Item Icon Colors (auto-generated if no sprite assigned)")]
        [SerializeField] private Color potionColor = new Color(1f, 0.3f, 0.3f);      // Red
        [SerializeField] private Color ropeColor = new Color(0.6f, 0.4f, 0.2f);      // Brown
        [SerializeField] private Color armourColor = new Color(0.5f, 0.5f, 0.6f);    // Silver
        [SerializeField] private Color cloakColor = new Color(0.4f, 0.2f, 0.6f);     // Purple
        [SerializeField] private Color scrollColor = new Color(0.9f, 0.85f, 0.6f);   // Parchment
        [SerializeField] private Color defaultItemColor = new Color(0.5f, 0.5f, 0.5f); // Gray

        // Generated sprites (cached)
        private Dictionary<Core.ItemType, Sprite> itemIconCache = new Dictionary<Core.ItemType, Sprite>();
        private Sprite baseIconSprite;
        #endregion

        #region UI References - Menus
        [Header("Menus")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject victoryPanel;

        [Header("Game Over / Victory")]
        [SerializeField] private TextMeshProUGUI gameOverTitleText;
        [SerializeField] private TextMeshProUGUI gameOverStatsText;
        [SerializeField] private TextMeshProUGUI victoryTitleText;
        [SerializeField] private TextMeshProUGUI victoryStatsText;

        [Header("Choice Panel (Soothsayer, Merchant, etc.)")]
        [SerializeField] private GameObject choicePanelRoot;
        [SerializeField] private TextMeshProUGUI choiceTitleText;
        [SerializeField] private TextMeshProUGUI choiceDescriptionText;
        [SerializeField] private Transform choiceButtonContainer;
        [SerializeField] private GameObject choiceButtonPrefab;
        [SerializeField] private GameObject levelUpPanel;
        [SerializeField] private TextMeshProUGUI levelUpText;
        #endregion

        #region UI References - Tooltips
        [Header("Tooltips")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipText;
        #endregion

        #region Settings
        [Header("Settings")]
        [SerializeField] private float messageAnimationDuration = 0.2f;
        [SerializeField] private Color normalMessageColor = Color.white;
        [SerializeField] private Color warningMessageColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color dangerMessageColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color successMessageColor = new Color(0.3f, 1f, 0.4f);
        [SerializeField] private Color infoMessageColor = new Color(0.5f, 0.8f, 1f);
        #endregion

        #region State
        private UIState currentState = UIState.HUD;
        private Queue<string> messageHistory = new Queue<string>();
        private List<GameObject> gameLogMessages = new List<GameObject>(); // Individual message objects
        private Coroutine currentMessageCoroutine;

        // Item bar state
        private List<ItemBarSlot> itemBarSlots = new List<ItemBarSlot>();
        private ItemBarSlot hoveredItemSlot;

        // Choice panel state
        private List<GameObject> activeChoiceButtons = new List<GameObject>();
        private System.Action<int> currentChoiceCallback;

        // Track if we need to return to merchant after level up choice
        private bool wasInMerchantBeforeLevelUp = false;
        #endregion

        #region Properties
        public UIState CurrentGameState => currentState;
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

        private void Start()
        {
            // Subscribe to game events
            SubscribeToEvents();

            // Initialize UI
            InitializeUI();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                UnsubscribeFromEvents();
                Instance = null;
            }
        }

        private void Update()
        {
            // Handle UI input
            HandleInput();

            // Update tooltip position if showing item tooltip
            if (tooltipPanel != null && tooltipPanel.activeSelf && hoveredItemSlot != null)
            {
                UpdateItemTooltipPosition();
            }
        }
        #endregion

        #region Initialization

        private void InitializeUI()
        {
            // Hide all panels initially
            SetPanelActive(combatPanel, false);
            SetPanelActive(pauseMenuPanel, false);
            SetPanelActive(gameOverPanel, false);
            SetPanelActive(levelUpPanel, false);
            SetPanelActive(tooltipPanel, false);
            SetPanelActive(messageLogPanel, false);
            SetPanelActive(choicePanelRoot, false);

            // Setup button listeners
            if (attackButton != null)
                attackButton.onClick.AddListener(OnAttackButtonClicked);
            if (fleeButton != null)
                fleeButton.onClick.AddListener(OnFleeButtonClicked);

            // Initialize item bar
            InitializeItemBar();

            // Initialize choice panel
            InitializeChoicePanel();

            // Initial HUD update
            UpdateHUD();
        }

        private void SubscribeToEvents()
        {
            // Player events
            if (Core.Player.Instance != null)
            {
                Core.Player.Instance.OnHealthChanged += OnPlayerHealthChanged;
                Core.Player.Instance.OnLevelUp += OnPlayerLevelUp;
                Core.Player.Instance.OnXPChanged += OnPlayerXPChanged;
                Core.Player.Instance.OnRoomsExploredChanged += OnRoomsExploredChanged;
                Core.Player.Instance.OnWeaponChanged += OnWeaponChanged;
                Core.Player.Instance.OnPlayerDeath += OnPlayerDeath;
            }

            // Combat events
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnCombatStarted += OnCombatStarted;
                CombatManager.Instance.OnCombatEnded += OnCombatEnded;
                CombatManager.Instance.OnAttackResult += OnAttackResult;
                CombatManager.Instance.OnMonsterDamaged += OnMonsterDamaged;
                CombatManager.Instance.OnRoundStarted += OnRoundStarted;
            }

            // Inventory events
            if (Core.Inventory.Instance != null)
            {
                Core.Inventory.Instance.OnGoldChanged += OnGoldChanged;
                Core.Inventory.Instance.OnInventoryChanged += OnInventoryChanged;
            }

            // Game state events
            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                Core.GameManager.Instance.OnGameOver += OnGameOver;
            }

            // Dungeon events
            if (Dungeon.MapController.Instance != null)
            {
                Dungeon.MapController.Instance.OnRoomEntered += OnRoomEntered;
            }

            // Level up choice event
            if (Core.Player.Instance != null)
            {
                Core.Player.Instance.OnLevelUpChoice += OnLevelUpChoice;
            }

            // Reroll system events
            if (Core.RerollSystem.Instance != null)
            {
                Core.RerollSystem.Instance.OnRerollAvailableChanged += OnRerollAvailableChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (Core.Player.Instance != null)
            {
                Core.Player.Instance.OnHealthChanged -= OnPlayerHealthChanged;
                Core.Player.Instance.OnLevelUp -= OnPlayerLevelUp;
                Core.Player.Instance.OnXPChanged -= OnPlayerXPChanged;
                Core.Player.Instance.OnRoomsExploredChanged -= OnRoomsExploredChanged;
                Core.Player.Instance.OnWeaponChanged -= OnWeaponChanged;
                Core.Player.Instance.OnPlayerDeath -= OnPlayerDeath;
                Core.Player.Instance.OnLevelUpChoice -= OnLevelUpChoice;
            }

            if (Core.RerollSystem.Instance != null)
            {
                Core.RerollSystem.Instance.OnRerollAvailableChanged -= OnRerollAvailableChanged;
            }

            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnCombatStarted -= OnCombatStarted;
                CombatManager.Instance.OnCombatEnded -= OnCombatEnded;
                CombatManager.Instance.OnAttackResult -= OnAttackResult;
                CombatManager.Instance.OnMonsterDamaged -= OnMonsterDamaged;
                CombatManager.Instance.OnRoundStarted -= OnRoundStarted;
            }

            if (Core.Inventory.Instance != null)
            {
                Core.Inventory.Instance.OnGoldChanged -= OnGoldChanged;
                Core.Inventory.Instance.OnInventoryChanged -= OnInventoryChanged;
            }

            if (Core.GameManager.Instance != null)
            {
                Core.GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
                Core.GameManager.Instance.OnGameOver -= OnGameOver;
            }

            if (Dungeon.MapController.Instance != null)
            {
                Dungeon.MapController.Instance.OnRoomEntered -= OnRoomEntered;
            }
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // Get keyboard (null check for safety)
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Escape key - pause/unpause or close panels
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeKey();
            }

            // U key - level up via silver (when available)
            if (keyboard.uKey.wasPressedThisFrame && currentState == UIState.HUD)
            {
                if (Core.Player.Instance?.CanLevelUpBySilver == true)
                {
                    ShowSilverLevelUpConfirmation();
                }
            }

            // R key - reroll (when False Omen reroll is available)
            if (keyboard.rKey.wasPressedThisFrame)
            {
                if (Core.RerollSystem.Instance?.CanReroll == true)
                {
                    OnRerollButtonClicked();
                }
            }

            // L key - message log toggle
            if (keyboard.lKey.wasPressedThisFrame)
            {
                ToggleMessageLog();
            }
        }

        private void HandleEscapeKey()
        {
            switch (currentState)
            {
                case UIState.HUD:
                    OpenPauseMenu();
                    break;
                case UIState.Paused:
                    ClosePauseMenu();
                    break;
                case UIState.Combat:
                    // Can't escape combat!
                    ShowMessage("You can't escape that easily!", MessageType.Warning);
                    break;
            }
        }

        #endregion

        #region Message System

        /// <summary>
        /// Show a message to the player and add it to the game log
        /// </summary>
        public void ShowMessage(string message, MessageType type = MessageType.Normal, float duration = -1f)
        {
            if (string.IsNullOrEmpty(message)) return;

            // Add to full history (with timestamp)
            string timestampedMessage = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            messageHistory.Enqueue(timestampedMessage);
            while (messageHistory.Count > maxMessageHistory)
            {
                messageHistory.Dequeue();
            }

            // Add to visible game log as individual object
            AddGameLogMessage(message);

            // Update full message log
            UpdateMessageLog();

            // Show message popup (temporary notification)
            if (messagePanel != null && messageText != null)
            {
                // Stop any existing message animation
                if (currentMessageCoroutine != null)
                {
                    StopCoroutine(currentMessageCoroutine);
                }

                // Set message color based on type
                messageText.color = GetMessageColor(type);
                messageText.text = message;

                float displayDuration = duration > 0 ? duration : defaultMessageDuration;
                currentMessageCoroutine = StartCoroutine(ShowMessageCoroutine(displayDuration));
            }

            Debug.Log($"[UI] {message}");
        }

        /// <summary>
        /// Shorthand for ShowMessage
        /// </summary>
        public void ShowMessage(string message)
        {
            ShowMessage(message, MessageType.Normal);
        }

        /// <summary>
        /// Add a message to the log without showing the popup notification
        /// </summary>
        public void LogMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            // Add to full history
            string timestampedMessage = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            messageHistory.Enqueue(timestampedMessage);
            while (messageHistory.Count > maxMessageHistory)
            {
                messageHistory.Dequeue();
            }

            // Add to visible game log as individual object
            AddGameLogMessage(message);
            UpdateMessageLog();
        }

        /// <summary>
        /// Create a new message object in the game log
        /// </summary>
        private void AddGameLogMessage(string message)
        {
            if (gameLogContent == null) return;

            // Create message object
            GameObject msgObj;
            if (gameLogMessagePrefab != null)
            {
                msgObj = Instantiate(gameLogMessagePrefab, gameLogContent);
            }
            else
            {
                msgObj = CreateDefaultMessageObject();
                msgObj.transform.SetParent(gameLogContent, false);
            }

            msgObj.SetActive(true);
            msgObj.name = $"LogMessage_{gameLogMessages.Count}";

            // Set the text
            TextMeshProUGUI tmp = msgObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = message;
                tmp.fontSize = gameLogFontSize;
            }

            gameLogMessages.Add(msgObj);

            // Remove oldest messages if over limit
            while (gameLogMessages.Count > maxGameLogLines)
            {
                GameObject oldest = gameLogMessages[0];
                gameLogMessages.RemoveAt(0);
                Destroy(oldest);
            }

            // Scroll to bottom
            if (gameLogScrollRect != null)
            {
                StartCoroutine(ScrollGameLogToBottom());
            }
        }

        /// <summary>
        /// Create a default message object if no prefab is assigned
        /// </summary>
        private GameObject CreateDefaultMessageObject()
        {
            GameObject msgObj = new GameObject("LogMessage");

            RectTransform rect = msgObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);

            TextMeshProUGUI tmp = msgObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = gameLogFontSize;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.richText = true;
            tmp.alignment = TextAlignmentOptions.TopLeft;

            // Add ContentSizeFitter to auto-size height based on text
            ContentSizeFitter fitter = msgObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return msgObj;
        }

        /// <summary>
        /// Add a colored message to the game log (uses rich text)
        /// </summary>
        public void LogMessageWithColor(string message, Color color)
        {
            if (string.IsNullOrEmpty(message)) return;

            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            string coloredMessage = $"<color=#{colorHex}>{message}</color>";

            LogMessage(coloredMessage);
        }

        /// <summary>
        /// Add a colored message to the game log using MessageType
        /// </summary>
        public void LogMessageWithType(string message, MessageType type)
        {
            LogMessageWithColor(message, GetMessageColor(type));
        }

        private IEnumerator ShowMessageCoroutine(float duration)
        {
            // Fade in
            messagePanel.SetActive(true);
            CanvasGroup canvasGroup = messagePanel.GetComponent<CanvasGroup>();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < messageAnimationDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = elapsed / messageAnimationDuration;
                    yield return null;
                }
                canvasGroup.alpha = 1f;
            }

            // Wait
            yield return new WaitForSeconds(duration);

            // Fade out
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < messageAnimationDuration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = 1f - (elapsed / messageAnimationDuration);
                    yield return null;
                }
                canvasGroup.alpha = 0f;
            }

            messagePanel.SetActive(false);
            currentMessageCoroutine = null;
        }

        private Color GetMessageColor(MessageType type)
        {
            return type switch
            {
                MessageType.Warning => warningMessageColor,
                MessageType.Danger => dangerMessageColor,
                MessageType.Success => successMessageColor,
                MessageType.Info => infoMessageColor,
                _ => normalMessageColor
            };
        }

        private void UpdateMessageLog()
        {
            if (messageLogText == null) return;

            messageLogText.text = string.Join("\n", messageHistory);

            // Force layout rebuild before scrolling
            if (messageLogScrollRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(messageLogText.rectTransform);

                if (messageLogScrollRect.content != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(messageLogScrollRect.content);
                }

                // Scroll to bottom (0 = bottom, 1 = top)
                Canvas.ForceUpdateCanvases();
                messageLogScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void UpdateGameLog()
        {
            // No longer needed - messages are added individually via AddGameLogMessage
            // Kept for compatibility but does nothing
        }

        private IEnumerator ScrollGameLogToBottom()
        {
            // Wait for end of frame to ensure layout is updated
            yield return new WaitForEndOfFrame();

            if (gameLogScrollRect == null) yield break;

            // Force canvas update
            Canvas.ForceUpdateCanvases();

            // Rebuild layout from the content up
            if (gameLogContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(gameLogContent as RectTransform);
            }

            if (gameLogScrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(gameLogScrollRect.content);
            }

            // Wait another frame for layout to settle
            yield return null;

            // Final canvas update
            Canvas.ForceUpdateCanvases();

            // Scroll to bottom (0 = bottom, 1 = top)
            gameLogScrollRect.verticalNormalizedPosition = 0f;
            gameLogScrollRect.velocity = Vector2.zero;
        }

        private IEnumerator ScrollToBottomCoroutine(ScrollRect scrollRect)
        {
            // Wait two frames for layout to fully update
            yield return null;
            yield return null;

            // Force layout update
            Canvas.ForceUpdateCanvases();

            // Scroll to bottom (0 = bottom, 1 = top)
            scrollRect.verticalNormalizedPosition = 0f;

            // Force refresh the scroll rect
            scrollRect.velocity = Vector2.zero;
        }

        public void ToggleMessageLog()
        {
            if (messageLogPanel != null)
            {
                bool isActive = !messageLogPanel.activeSelf;
                messageLogPanel.SetActive(isActive);
            }
        }

        /// <summary>
        /// Clear the game log
        /// </summary>
        public void ClearGameLog()
        {
            // Destroy all message objects
            foreach (var msgObj in gameLogMessages)
            {
                if (msgObj != null)
                    Destroy(msgObj);
            }
            gameLogMessages.Clear();
        }

        #endregion

        #region HUD Updates

        /// <summary>
        /// Update all HUD elements
        /// </summary>
        public void UpdateHUD()
        {
            UpdateHealthDisplay();
            UpdateLevelDisplay();
            UpdateXPDisplay();
            UpdateRoomsDisplay();
            UpdateGoldDisplay();
            UpdateWeaponDisplay();
            UpdateDirectionArrows();
            UpdateLevelUpButtonVisibility();
            UpdateRerollButtonVisibility();
        }

        private void UpdateHealthDisplay()
        {
            if (Core.Player.Instance == null) return;

            int current = Core.Player.Instance.CurrentHealth;
            int max = Core.Player.Instance.MaxHealth;

            if (healthText != null)
                healthText.text = $"{current}/{max}";

            if (healthBar != null)
            {
                healthBar.maxValue = max;
                healthBar.value = current;
            }
        }

        private void UpdateLevelDisplay()
        {
            if (Core.Player.Instance == null) return;

            if (levelText != null)
                levelText.text = $"Level {Core.Player.Instance.CurrentLevel}";
        }

        private void UpdateXPDisplay()
        {
            if (Core.Player.Instance == null) return;

            int current = Core.Player.Instance.CurrentXP;
            int required = Core.Player.Instance.XPRequiredForLevelUp;

            if (xpText != null)
                xpText.text = $"XP: {current}/{required}";

            if (xpBar != null)
            {
                xpBar.maxValue = required;
                xpBar.value = current;
            }
        }

        private void UpdateRoomsDisplay()
        {
            if (Core.Player.Instance == null) return;

            int current = Core.Player.Instance.RoomsExplored;
            int required = Core.Player.Instance.RoomsRequiredForLevelUp;

            if (roomsExploredText != null)
                roomsExploredText.text = $"Rooms: {current}/{required}";
        }

        private void UpdateGoldDisplay()
        {
            if (Core.Inventory.Instance == null) return;

            if (goldText != null)
                goldText.text = $"Silver: {Core.Inventory.Instance.Gold}";
        }

        private void UpdateWeaponDisplay()
        {
            if (Core.Player.Instance?.EquippedWeapon == null) return;

            var weapon = Core.Player.Instance.EquippedWeapon;
            if (weaponText != null)
                weaponText.text = weapon.ToString();
        }

        private void UpdateDirectionArrows()
        {
            if (Dungeon.MapController.Instance == null || Dungeon.MapController.Instance.CurrentRoom == null)
            {
                // No room yet - hide all arrows
                SetArrowActive(northArrow, false);
                SetArrowActive(southArrow, false);
                SetArrowActive(eastArrow, false);
                SetArrowActive(westArrow, false);
                return;
            }

            var controller = Dungeon.MapController.Instance;

            SetArrowActive(northArrow, controller.CanMoveInDirection(Dungeon.Direction.North));
            SetArrowActive(southArrow, controller.CanMoveInDirection(Dungeon.Direction.South));
            SetArrowActive(eastArrow, controller.CanMoveInDirection(Dungeon.Direction.East));
            SetArrowActive(westArrow, controller.CanMoveInDirection(Dungeon.Direction.West));
        }

        private void SetArrowActive(GameObject arrow, bool active)
        {
            if (arrow != null)
                arrow.SetActive(active);
        }

        #endregion

        #region Combat UI

        private void OnCombatStarted(Monster monster)
        {
            SetUIState(UIState.Combat);
            SetPanelActive(combatPanel, true);

            UpdateCombatUI(monster);

            // Log combat start to game log (no special effect display)
            LogMessageWithType($"--- Combat: {monster.MonsterName} ---", MessageType.Danger);
            LogMessageWithColor($"HP: {monster.CurrentHP} | Hit: {monster.HitValue}+ | DMG: {monster.BaseDamage}", infoMessageColor);
        }

        private void UpdateCombatUI(Monster monster)
        {
            if (monster == null) return;

            if (monsterNameText != null)
                monsterNameText.text = monster.MonsterName;

            if (monsterStatsText != null)
            {
                monsterStatsText.text = $"HP: {monster.CurrentHP}/{monster.MaxHP} | Hit: {monster.HitValue}+ | DMG: {monster.BaseDamage}";
            }

            if (monsterDescriptionText != null)
            {
                monsterDescriptionText.text = monster.Description;
            }

            if (monsterImage != null && monster.MonsterSprite != null)
                monsterImage.sprite = monster.MonsterSprite;
        }

        private void OnCombatEnded(bool playerVictory)
        {
            SetPanelActive(combatPanel, false);
            SetUIState(UIState.HUD);

            if (playerVictory)
            {
                LogMessageWithType("Victory!", MessageType.Success);
            }
            else
            {
                LogMessageWithType("Combat ended.", MessageType.Info);
            }
        }

        private void OnMonsterDamaged(Monster monster, int damage, int remainingHP)
        {
            // Update the combat UI to show new HP
            UpdateCombatUI(monster);
        }

        private void OnRoundStarted(int round)
        {
            // Log round marker
            LogMessageWithColor($"-- Round {round} --", new Color(0.7f, 0.7f, 0.7f));

            // Update damage display if monster has alternating damage
            if (CombatManager.Instance?.CurrentMonster != null)
            {
                UpdateCombatUI(CombatManager.Instance.CurrentMonster);
            }
        }

        private void OnAttackResult(int playerRoll, int requiredRoll, bool hit, int damage)
        {
            if (hit)
            {
                Monster monster = CombatManager.Instance?.CurrentMonster;
                LogMessageWithType($"Rolled {playerRoll} vs {requiredRoll}+ - HIT!", MessageType.Success);
                LogMessageWithType($"Dealt {damage} damage!", MessageType.Success);
                if (monster != null)
                {
                    LogMessageWithColor($"{monster.MonsterName}: {monster.CurrentHP}/{monster.MaxHP} HP", infoMessageColor);
                }
            }
            else
            {
                LogMessageWithColor($"Rolled {playerRoll} vs {requiredRoll}+ - MISS!", new Color(1f, 0.5f, 0.3f));
                LogMessageWithType($"Took {damage} damage!", MessageType.Danger);
            }

            // Update combat UI after attack
            if (CombatManager.Instance?.CurrentMonster != null)
            {
                UpdateCombatUI(CombatManager.Instance.CurrentMonster);
            }
        }

        private void OnAttackButtonClicked()
        {
            CombatManager.Instance?.PlayerAttack();
        }

        private void OnFleeButtonClicked()
        {
            CombatManager.Instance?.PlayerFlee();
        }

        #endregion

        #region Choice Panel (Soothsayer, Merchant, etc.)

        private void InitializeChoicePanel()
        {
            if (choicePanelRoot == null)
            {
                CreateDefaultChoicePanel();
            }

            if (choiceButtonPrefab == null)
            {
                CreateDefaultChoiceButtonPrefab();
            }
        }

        private void CreateDefaultChoicePanel()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }
            if (canvas == null)
            {
                Debug.LogError("ChoicePanel: No Canvas found in scene! Creating one...");
                GameObject canvasObj = new GameObject("AutoCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            Debug.Log($"ChoicePanel: Creating panel on canvas '{canvas.name}'");

            // Create panel root
            choicePanelRoot = new GameObject("ChoicePanel");
            choicePanelRoot.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = choicePanelRoot.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400, 300);

            Image panelBg = choicePanelRoot.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

            VerticalLayoutGroup layout = choicePanelRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter panelFitter = choicePanelRoot.AddComponent<ContentSizeFitter>();
            panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(choicePanelRoot.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            choiceTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            choiceTitleText.fontSize = 24;
            choiceTitleText.fontStyle = FontStyles.Bold;
            choiceTitleText.alignment = TextAlignmentOptions.Center;
            choiceTitleText.color = Color.white;
            LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 35;

            // Description
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(choicePanelRoot.transform, false);
            RectTransform descRect = descObj.AddComponent<RectTransform>();
            choiceDescriptionText = descObj.AddComponent<TextMeshProUGUI>();
            choiceDescriptionText.fontSize = 16;
            choiceDescriptionText.alignment = TextAlignmentOptions.Center;
            choiceDescriptionText.color = new Color(0.8f, 0.8f, 0.8f);
            LayoutElement descLayout = descObj.AddComponent<LayoutElement>();
            descLayout.preferredHeight = 60;

            // Button container
            GameObject buttonContainerObj = new GameObject("ButtonContainer");
            buttonContainerObj.transform.SetParent(choicePanelRoot.transform, false);

            RectTransform buttonContainerRect = buttonContainerObj.AddComponent<RectTransform>();

            VerticalLayoutGroup buttonLayout = buttonContainerObj.AddComponent<VerticalLayoutGroup>();
            buttonLayout.spacing = 10;
            buttonLayout.childAlignment = TextAnchor.UpperCenter;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = false;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = false;

            ContentSizeFitter buttonContainerFitter = buttonContainerObj.AddComponent<ContentSizeFitter>();
            buttonContainerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // IMPORTANT: Set the container reference AFTER creating the object
            choiceButtonContainer = buttonContainerObj.transform;

            Debug.Log($"ChoicePanel: Panel created. ButtonContainer={choiceButtonContainer != null}");

            choicePanelRoot.SetActive(false);
        }

        private void CreateDefaultChoiceButtonPrefab()
        {
            choiceButtonPrefab = new GameObject("ChoiceButtonPrefab");
            choiceButtonPrefab.SetActive(false);

            RectTransform rect = choiceButtonPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 40);

            LayoutElement layoutElement = choiceButtonPrefab.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 40;
            layoutElement.preferredWidth = 300;

            Image bg = choiceButtonPrefab.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.3f, 1f);

            Button btn = choiceButtonPrefab.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.25f, 0.25f, 0.3f, 1f);
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.45f, 1f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.25f, 1f);
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(choiceButtonPrefab.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
            btnText.fontSize = 16;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = Color.white;

            choiceButtonPrefab.transform.SetParent(transform, false);
        }

        /// <summary>
        /// Show a choice panel with multiple options
        /// </summary>
        /// <param name="title">Panel title</param>
        /// <param name="description">Description text</param>
        /// <param name="choices">Array of choice text strings</param>
        /// <param name="onChoiceSelected">Callback with index of selected choice</param>
        public void ShowChoicePanel(string title, string description, string[] choices, System.Action<int> onChoiceSelected)
        {
            // Lazy initialization if needed
            if (choicePanelRoot == null)
            {
                CreateDefaultChoicePanel();
            }
            if (choiceButtonPrefab == null)
            {
                CreateDefaultChoiceButtonPrefab();
            }

            if (choicePanelRoot == null)
            {
                Debug.LogError("ChoicePanel: Failed to create panel root!");
                return;
            }

            // Clear existing buttons
            ClearChoiceButtons();

            // Set text
            if (choiceTitleText != null)
                choiceTitleText.text = title;

            if (choiceDescriptionText != null)
                choiceDescriptionText.text = description;

            // Store callback
            currentChoiceCallback = onChoiceSelected;

            // Create buttons
            Debug.Log($"ChoicePanel: Creating {choices.Length} buttons. Prefab={choiceButtonPrefab != null}, Container={choiceButtonContainer != null}");
            for (int i = 0; i < choices.Length; i++)
            {
                CreateChoiceButton(choices[i], i);
            }
            Debug.Log($"ChoicePanel: Created {activeChoiceButtons.Count} buttons");

            // Show panel
            SetPanelActive(choicePanelRoot, true);
            SetUIState(UIState.Dialogue);

            // Force layout rebuild
            Canvas.ForceUpdateCanvases();
            if (choiceButtonContainer != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(choiceButtonContainer.GetComponent<RectTransform>());
            }
            if (choicePanelRoot != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(choicePanelRoot.GetComponent<RectTransform>());
            }
        }

        private void CreateChoiceButton(string text, int index)
        {
            if (choiceButtonPrefab == null)
            {
                Debug.LogError("ChoicePanel: Button prefab is null!");
                return;
            }
            if (choiceButtonContainer == null)
            {
                Debug.LogError("ChoicePanel: Button container is null!");
                return;
            }

            GameObject btnObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            btnObj.SetActive(true);
            btnObj.name = $"Choice_{index}";

            TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = text;
            else
                Debug.LogWarning($"ChoicePanel: No TextMeshProUGUI found on button {index}");

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                int capturedIndex = index; // Capture for closure
                btn.onClick.AddListener(() => OnChoiceButtonClicked(capturedIndex));
            }
            else
            {
                Debug.LogWarning($"ChoicePanel: No Button component found on button {index}");
            }

            activeChoiceButtons.Add(btnObj);
            Debug.Log($"ChoicePanel: Created button '{text}' at index {index}");
        }

        private void OnChoiceButtonClicked(int index)
        {
            System.Action<int> callback = currentChoiceCallback;

            // Close panel first
            CloseChoicePanel();

            // Then invoke callback
            callback?.Invoke(index);
        }

        public void CloseChoicePanel()
        {
            ClearChoiceButtons();
            SetPanelActive(choicePanelRoot, false);
            currentChoiceCallback = null;

            SetUIState(UIState.HUD);
        }

        /// <summary>
        /// Refresh choice panel content without closing it (for merchant)
        /// </summary>
        public void RefreshChoicePanel(string title, string description, string[] choices, System.Action<int> onChoiceSelected)
        {
            if (choicePanelRoot == null || !choicePanelRoot.activeSelf)
            {
                // Panel not open, use normal show
                ShowChoicePanel(title, description, choices, onChoiceSelected);
                return;
            }

            // Clear existing buttons
            ClearChoiceButtons();

            // Set text
            if (choiceTitleText != null)
                choiceTitleText.text = title;

            if (choiceDescriptionText != null)
                choiceDescriptionText.text = description;

            // Store callback
            currentChoiceCallback = onChoiceSelected;

            // Create buttons after a frame delay to ensure old ones are destroyed
            StartCoroutine(CreateButtonsDelayed(choices));
        }

        private System.Collections.IEnumerator CreateButtonsDelayed(string[] choices)
        {
            yield return null; // Wait one frame for Destroy to complete

            for (int i = 0; i < choices.Length; i++)
            {
                CreateChoiceButton(choices[i], i);
            }

            // Force layout rebuild
            Canvas.ForceUpdateCanvases();
            if (choiceButtonContainer != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(choiceButtonContainer.GetComponent<RectTransform>());
            }
        }

        private void ClearChoiceButtons()
        {
            foreach (var btn in activeChoiceButtons)
            {
                if (btn != null)
                {
                    btn.SetActive(false); // Hide immediately
                    Destroy(btn); // Queue for destruction
                }
            }
            activeChoiceButtons.Clear();
        }

        #endregion

        #region Item Bar (HUD)

        private void InitializeItemBar()
        {
            if (itemBarContainer == null)
            {
                CreateDefaultItemBarContainer();
            }

            if (itemSlotPrefab == null)
            {
                CreateDefaultItemSlotPrefab();
            }

            // Initial refresh
            RefreshItemBar();
        }

        private void CreateDefaultItemBarContainer()
        {
            // Find or create canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }
            if (canvas == null) return;

            GameObject containerObj = new GameObject("ItemBarContainer");
            containerObj.transform.SetParent(canvas.transform);

            RectTransform rect = containerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -10);
            rect.sizeDelta = new Vector2(500, 60);

            HorizontalLayoutGroup layout = containerObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 5;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            itemBarContainer = containerObj.transform;
        }

        private void CreateDefaultItemSlotPrefab()
        {
            itemSlotPrefab = new GameObject("ItemSlotPrefab");
            itemSlotPrefab.SetActive(false);

            RectTransform rect = itemSlotPrefab.AddComponent<RectTransform>();
            rect.sizeDelta = itemSlotSize;

            Image bg = itemSlotPrefab.AddComponent<Image>();
            bg.color = itemSlotNormalColor;

            // Icon child
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(itemSlotPrefab.transform);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(8, 8);  // Larger padding for larger icons
            iconRect.offsetMax = new Vector2(-8, -8);

            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.white;

            // Uses text child
            GameObject usesObj = new GameObject("UsesText");
            usesObj.transform.SetParent(itemSlotPrefab.transform);

            RectTransform usesRect = usesObj.AddComponent<RectTransform>();
            usesRect.anchorMin = new Vector2(1, 0);
            usesRect.anchorMax = new Vector2(1, 0);
            usesRect.pivot = new Vector2(1, 0);
            usesRect.anchoredPosition = new Vector2(-2, 2);
            usesRect.sizeDelta = new Vector2(20, 20);

            TextMeshProUGUI usesText = usesObj.AddComponent<TextMeshProUGUI>();
            usesText.fontSize = 12;
            usesText.fontStyle = FontStyles.Bold;
            usesText.alignment = TextAlignmentOptions.BottomRight;
            usesText.color = Color.white;

            itemSlotPrefab.transform.SetParent(transform);
        }

        private void OnInventoryChanged()
        {
            RefreshItemBar();
        }

        public void RefreshItemBar()
        {
            if (Core.Inventory.Instance == null) return;

            var items = Core.Inventory.Instance.Items;
            var weapons = Core.Inventory.Instance.Weapons;
            var equippedWeapon = Core.Player.Instance?.EquippedWeapon;
            bool isUnarmed = Core.Player.Instance?.IsUnarmed ?? true;

            Debug.Log($"RefreshItemBar: Equipped={equippedWeapon?.WeaponName ?? "Unarmed"}, Weapons={weapons.Count}, Items={items.Count}");

            // Always show first slot (equipped weapon or unarmed)
            int totalNeeded = 1 + weapons.Count + items.Count;

            // Create slots as needed
            while (itemBarSlots.Count < totalNeeded && itemBarSlots.Count < maxItemBarSlots)
            {
                CreateItemBarSlot();
            }

            int slotIndex = 0;

            // First slot: Equipped weapon OR Unarmed indicator
            if (slotIndex < itemBarSlots.Count)
            {
                if (equippedWeapon != null)
                {
                    itemBarSlots[slotIndex].SetWeapon(equippedWeapon, GetIconForWeapon(equippedWeapon, true), true);
                }
                else
                {
                    itemBarSlots[slotIndex].SetUnarmed(GetUnarmedIcon());
                }
                itemBarSlots[slotIndex].gameObject.SetActive(true);
                slotIndex++;
            }

            // Next: Inventory weapons
            for (int i = 0; i < weapons.Count && slotIndex < itemBarSlots.Count; i++)
            {
                Debug.Log($"RefreshItemBar: Adding weapon slot {slotIndex} = {weapons[i].WeaponName}");
                itemBarSlots[slotIndex].SetWeapon(weapons[i], GetIconForWeapon(weapons[i], false), false);
                itemBarSlots[slotIndex].gameObject.SetActive(true);
                slotIndex++;
            }

            // Then: Items
            for (int i = 0; i < items.Count && slotIndex < itemBarSlots.Count; i++)
            {
                itemBarSlots[slotIndex].SetItem(items[i], GetIconForItem(items[i]));
                itemBarSlots[slotIndex].gameObject.SetActive(true);
                slotIndex++;
            }

            // Hide unused slots
            for (int i = slotIndex; i < itemBarSlots.Count; i++)
            {
                itemBarSlots[i].ClearItem();
                itemBarSlots[i].gameObject.SetActive(false);
            }
        }

        private void CreateItemBarSlot()
        {
            if (itemSlotPrefab == null || itemBarContainer == null) return;

            GameObject slotObj = Instantiate(itemSlotPrefab, itemBarContainer);
            slotObj.SetActive(true);
            slotObj.name = $"ItemSlot_{itemBarSlots.Count}";

            // Force the slot size from settings
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            if (slotRect != null)
            {
                slotRect.sizeDelta = itemSlotSize;
            }

            ItemBarSlot slot = slotObj.AddComponent<ItemBarSlot>();
            slot.Initialize(this, itemBarSlots.Count, itemSlotNormalColor, itemSlotHoverColor);

            itemBarSlots.Add(slot);
        }

        public void OnItemSlotClicked(ItemBarSlot slot)
        {
            switch (slot.ContentType)
            {
                case SlotContentType.Item:
                    if (slot.CurrentItem != null)
                    {
                        bool used = Core.Inventory.Instance.UseItem(slot.CurrentItem);
                        if (used)
                        {
                            HideItemTooltip();
                        }
                    }
                    break;

                case SlotContentType.Weapon:
                    // Inventory weapon - equip it (swaps with current) or sell if merchant mode
                    if (slot.CurrentWeapon != null)
                    {
                        if (Core.Inventory.Instance.IsMerchantModeActive)
                        {
                            Core.Inventory.Instance.SellWeapon(slot.CurrentWeapon);
                        }
                        else if (!Combat.CombatManager.Instance?.IsInCombat ?? true)
                        {
                            // Find index of this weapon in inventory
                            int weaponIndex = Core.Inventory.Instance.Weapons.IndexOf(slot.CurrentWeapon);
                            if (weaponIndex >= 0)
                            {
                                Core.Inventory.Instance.EquipWeaponAt(weaponIndex);
                                ShowMessage($"Equipped {slot.CurrentWeapon.WeaponName}!", MessageType.Success);
                            }
                        }
                        else
                        {
                            ShowMessage("Cannot change weapons during combat!", MessageType.Warning);
                        }
                        HideItemTooltip();
                    }
                    break;

                case SlotContentType.EquippedWeapon:
                    // Currently equipped weapon - sell if merchant mode active
                    if (Core.Inventory.Instance?.IsMerchantModeActive == true)
                    {
                        Core.Inventory.Instance.SellEquippedWeapon();
                        HideItemTooltip();
                    }
                    else
                    {
                        ShowMessage($"{slot.CurrentWeapon?.WeaponName} is equipped", MessageType.Info);
                    }
                    break;

                case SlotContentType.Unarmed:
                    ShowMessage("You are unarmed. Deal d4-1 damage in combat.", MessageType.Info);
                    break;
            }
        }

        public void OnItemSlotHoverEnter(ItemBarSlot slot)
        {
            hoveredItemSlot = slot;
            slot.SetHighlight(true);

            switch (slot.ContentType)
            {
                case SlotContentType.Item:
                    if (slot.CurrentItem != null)
                    {
                        ShowItemTooltip(slot.CurrentItem);
                    }
                    break;

                case SlotContentType.Weapon:
                case SlotContentType.EquippedWeapon:
                    if (slot.CurrentWeapon != null)
                    {
                        ShowWeaponTooltip(slot.CurrentWeapon, slot.ContentType == SlotContentType.EquippedWeapon);
                    }
                    break;

                case SlotContentType.Unarmed:
                    ShowUnarmedTooltip();
                    break;
            }
        }

        public void OnItemSlotHoverExit(ItemBarSlot slot)
        {
            if (hoveredItemSlot == slot)
            {
                hoveredItemSlot = null;
                HideItemTooltip();
            }
            slot.SetHighlight(false);
        }

        /// <summary>
        /// Check if we're likely on a touch/mobile device
        /// </summary>
        private bool IsTouchDevice()
        {
            // Check for touch support
            return UnityEngine.Input.touchSupported && !UnityEngine.Input.mousePresent;
        }

        /// <summary>
        /// Get the appropriate action text based on platform
        /// </summary>
        private string GetActionText(string clickAction, string longPressAction)
        {
            if (IsTouchDevice())
            {
                return $"\n<color=#AAAAAA>{longPressAction}</color>";
            }
            else
            {
                return $"\n<color=#AAAAAA>{clickAction}</color>";
            }
        }

        private void ShowItemTooltip(Core.Item item)
        {
            if (tooltipPanel == null || tooltipText == null) return;

            string stackStr = item.StackCount > 1 ? $" x{item.StackCount}" : "";
            string usesStr = item.UsesRemaining > 0 ? $" ({item.UsesRemaining} uses)" : "";
            string sellStr = Core.Inventory.Instance?.IsMerchantModeActive == true
                ? $"\n<color=#C0C0C0>Sell 1 for {item.SellPrice} silver</color>"
                : "";
            string actionStr = Core.Inventory.Instance?.IsMerchantModeActive == true
                ? ""
                : GetActionText("Click to use", "Long-press to use");
            tooltipText.text = $"<b>{item.ItemName}</b>{stackStr}{usesStr}\n<size=80%>{item.Description}</size>{sellStr}{actionStr}";

            tooltipPanel.SetActive(true);
            ResizeTooltipToFitText();
            UpdateItemTooltipPosition();
        }

        private void ShowWeaponTooltip(Core.Weapon weapon, bool isEquipped)
        {
            if (tooltipPanel == null || tooltipText == null) return;

            string hitStr = weapon.HitBonus > 0 ? $", +{weapon.HitBonus} hit" : "";
            string statusStr = isEquipped ? " <color=#00FF00>(Equipped)</color>" : "";
            string actionStr;

            if (Core.Inventory.Instance?.IsMerchantModeActive == true)
            {
                actionStr = $"\n<color=#C0C0C0>Sell for {weapon.SellPrice} silver</color>";
            }
            else if (!isEquipped)
            {
                actionStr = GetActionText("Click to equip", "Long-press to equip");
            }
            else
            {
                actionStr = "";
            }

            tooltipText.text = $"<b>{weapon.WeaponName}</b>{statusStr}\n{weapon.DamageString} damage{hitStr}\n<size=80%>{weapon.Description}</size>{actionStr}";

            tooltipPanel.SetActive(true);
            ResizeTooltipToFitText();
            UpdateItemTooltipPosition();
        }

        private void ShowUnarmedTooltip()
        {
            if (tooltipPanel == null || tooltipText == null) return;

            tooltipText.text = "<b>Unarmed</b> <color=#FF6666>(No Weapon)</color>\nd4-1 damage (minimum 1)\n<size=80%>Find or buy a weapon to deal more damage!</size>";

            tooltipPanel.SetActive(true);
            ResizeTooltipToFitText();
            UpdateItemTooltipPosition();
        }

        private void HideItemTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Resize the tooltip panel height to fit the text content
        /// </summary>
        private void ResizeTooltipToFitText()
        {
            if (tooltipPanel == null || tooltipText == null) return;

            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            if (tooltipRect == null) return;

            // Force text mesh to update so we get accurate preferred values
            tooltipText.ForceMeshUpdate();

            // Get the preferred height of the text
            float preferredHeight = tooltipText.preferredHeight;

            // Add padding (adjust as needed for your UI)
            float padding = 20f;

            // Set the tooltip panel height, keeping the width the same
            // With top-left anchor/pivot, the top stays in place and height extends downward
            Vector2 size = tooltipRect.sizeDelta;
            size.y = preferredHeight + padding;
            tooltipRect.sizeDelta = size;
        }

        private void UpdateItemTooltipPosition()
        {
            if (tooltipPanel == null) return;

            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();

            // Force layout update to get correct size after resize
            Canvas.ForceUpdateCanvases();

            Vector2 mousePos = Input.mousePosition;
            Vector2 size = tooltipRect.sizeDelta;

            // With top-left anchor (0,1) and top-left pivot (0,1):
            // anchoredPosition.x = distance from left edge to top-left of tooltip
            // anchoredPosition.y = distance from top edge (negative = below top)

            // Position tooltip's top-left corner to the right of cursor, with top aligned near cursor
            float posX = mousePos.x + 15f;
            float posY = mousePos.y - Screen.height + 15f;  // +15 to position top of tooltip just above cursor

            // Keep on screen - right edge
            if (posX + size.x > Screen.width)
                posX = mousePos.x - size.x - 15f;

            // Keep on screen - left edge
            if (posX < 0)
                posX = 0;

            // Keep on screen - bottom edge
            if (posY - size.y < -Screen.height)
                posY = -Screen.height + size.y;

            // Keep on screen - top edge
            if (posY > 0)
                posY = 0;

            tooltipRect.anchoredPosition = new Vector2(posX, posY);
        }

        private Sprite GetIconForItem(Core.Item item)
        {
            if (item == null) return GetOrCreateIcon(Core.ItemType.Potion, defaultItemColor);

            // Check cache first
            if (itemIconCache.TryGetValue(item.Type, out Sprite cached))
            {
                return cached;
            }

            // Generate and cache icon based on item type
            Color iconColor = item.Type switch
            {
                Core.ItemType.Potion => potionColor,
                Core.ItemType.Rope => ropeColor,
                Core.ItemType.Armour => armourColor,
                Core.ItemType.CloakOfInvisibility => cloakColor,
                Core.ItemType.SummonWeakDaemon or
                Core.ItemType.PalmsOpenSouthernGate or
                Core.ItemType.AegisOfSorrow or
                Core.ItemType.FalseOmen or
                Core.ItemType.RandomScroll => scrollColor,
                _ => defaultItemColor
            };

            return GetOrCreateIcon(item.Type, iconColor);
        }

        private Sprite GetOrCreateIcon(Core.ItemType type, Color color)
        {
            if (itemIconCache.TryGetValue(type, out Sprite cached))
            {
                return cached;
            }

            // Create a simple colored square sprite
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    // Add a simple border
                    bool isBorder = x < 2 || x > 29 || y < 2 || y > 29;
                    pixels[y * 32 + x] = isBorder ? color * 0.6f : color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            itemIconCache[type] = sprite;

            return sprite;
        }

        // Weapon icon cache (separate from items)
        private Dictionary<string, Sprite> weaponIconCache = new Dictionary<string, Sprite>();

        private Sprite GetIconForWeapon(Core.Weapon weapon, bool isEquipped)
        {
            if (weapon == null) return null;

            string cacheKey = weapon.WeaponName + (isEquipped ? "_equipped" : "");

            if (weaponIconCache.TryGetValue(cacheKey, out Sprite cached))
            {
                return cached;
            }

            // Color based on damage tier (using average roll)
            Color baseColor;
            float avgDamage = weapon.DamageDice.AverageRoll;
            if (avgDamage >= 5)
                baseColor = new Color(1f, 0.8f, 0.2f); // Gold for high damage
            else if (avgDamage >= 3)
                baseColor = new Color(0.7f, 0.7f, 0.8f); // Silver for medium
            else
                baseColor = new Color(0.5f, 0.4f, 0.3f); // Bronze for low

            // Equipped weapons get a green tint
            if (isEquipped)
            {
                baseColor = Color.Lerp(baseColor, Color.green, 0.3f);
            }

            // Create sword-like icon
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            Color transparent = new Color(0, 0, 0, 0);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    // Simple diagonal sword shape
                    bool isBlade = (x + y >= 14 && x + y <= 18) && (x >= 8 && y >= 8);
                    bool isHilt = (x >= 12 && x <= 20 && y >= 4 && y <= 8);
                    bool isHandle = (x >= 14 && x <= 18 && y >= 0 && y <= 6);

                    if (isBlade)
                        pixels[y * 32 + x] = baseColor;
                    else if (isHilt)
                        pixels[y * 32 + x] = baseColor * 0.7f;
                    else if (isHandle)
                        pixels[y * 32 + x] = new Color(0.4f, 0.25f, 0.1f);
                    else
                        pixels[y * 32 + x] = transparent;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            weaponIconCache[cacheKey] = sprite;

            return sprite;
        }

        private Sprite unarmedIcon;

        private Sprite GetUnarmedIcon()
        {
            if (unarmedIcon != null) return unarmedIcon;

            // Create a fist/empty hand icon
            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            Color handColor = new Color(0.8f, 0.6f, 0.5f); // Skin tone
            Color outlineColor = new Color(0.4f, 0.3f, 0.25f);
            Color transparent = new Color(0, 0, 0, 0);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    // Simple fist shape
                    bool isFist = (x >= 8 && x <= 24 && y >= 10 && y <= 26);
                    bool isOutline = isFist && (x == 8 || x == 24 || y == 10 || y == 26);

                    if (isOutline)
                        pixels[y * 32 + x] = outlineColor;
                    else if (isFist)
                        pixels[y * 32 + x] = handColor;
                    else
                        pixels[y * 32 + x] = transparent;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Point;

            unarmedIcon = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            return unarmedIcon;
        }

        #endregion

        #region Pause Menu

        public void OpenPauseMenu()
        {
            if (currentState == UIState.Combat) return; // Can't pause during combat

            SetUIState(UIState.Paused);
            SetPanelActive(pauseMenuPanel, true);
            Time.timeScale = 0f;
        }

        public void ClosePauseMenu()
        {
            SetPanelActive(pauseMenuPanel, false);
            SetUIState(UIState.HUD);
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Toggle pause menu (for mobile button)
        /// </summary>
        public void TogglePauseMenu()
        {
            if (currentState == UIState.Paused)
                ClosePauseMenu();
            else if (currentState == UIState.HUD)
                OpenPauseMenu();
        }

        public void OnResumeButtonClicked()
        {
            ClosePauseMenu();
        }

        public void OnMainMenuButtonClicked()
        {
            Time.timeScale = 1f;
            Core.GameManager.Instance?.ReturnToMainMenu();
        }

        public void OnQuitButtonClicked()
        {
            Core.GameManager.Instance?.QuitGame();
        }

        #endregion

        #region Mobile UI Button Methods

        /// <summary>
        /// Called by mobile UI button to try silver level up
        /// </summary>
        public void OnLevelUpButtonClicked()
        {
            if (Core.Player.Instance?.CanLevelUpBySilver == true)
            {
                ShowSilverLevelUpConfirmation();
            }
            else
            {
                ShowMessage("Need 40 silver to level up this way.", MessageType.Warning);
            }
        }

        /// <summary>
        /// Called by mobile UI button to toggle message log
        /// </summary>
        public void OnLogButtonClicked()
        {
            ToggleMessageLog();
        }

        /// <summary>
        /// Called by mobile UI button to toggle pause
        /// </summary>
        public void OnPauseButtonClicked()
        {
            TogglePauseMenu();
        }

        /// <summary>
        /// Called by mobile UI button or keyboard to use False Omen reroll
        /// </summary>
        public void OnRerollButtonClicked()
        {
            if (Core.RerollSystem.Instance != null && Core.RerollSystem.Instance.CanReroll)
            {
                Core.RerollSystem.Instance.TryReroll();
            }
            else
            {
                ShowMessage("No reroll available.", MessageType.Warning);
            }
        }

        /// <summary>
        /// Called when reroll availability changes
        /// </summary>
        private void OnRerollAvailableChanged(bool available)
        {
            UpdateRerollButtonVisibility();
        }

        private void UpdateRerollButtonVisibility()
        {
            if (rerollButton != null)
            {
                bool canReroll = Core.RerollSystem.Instance?.CanReroll == true;
                rerollButton.SetActive(canReroll);
            }
        }

        #endregion

        #region Game Over

        private void OnGameOver(bool victory)
        {
            SetUIState(UIState.GameOver);

            // Get player name (with Sir title if knighted)
            string displayName = "Adventurer";
            if (Core.Player.Instance != null)
            {
                displayName = Core.Player.Instance.DisplayName;
            }

            // Build stats text using lifetime totals
            string statsText = "";
            if (Core.Player.Instance != null)
            {
                var player = Core.Player.Instance;
                statsText = $"Level Reached: {player.CurrentLevel}\n" +
                           $"Rooms Explored: {player.TotalRoomsExplored}\n" +
                           $"Monsters Slain: {player.TotalMonstersKilled}\n" +
                           $"Silver Collected: {player.TotalSilverCollected}";
            }

            if (victory)
            {
                // Show victory panel
                SetPanelActive(victoryPanel, true);
                SetPanelActive(gameOverPanel, false);

                if (victoryTitleText != null)
                {
                    victoryTitleText.text = $"Hail, {displayName}!";
                    victoryTitleText.color = successMessageColor;
                }

                if (victoryStatsText != null)
                {
                    victoryStatsText.text = statsText;
                }
            }
            else
            {
                // Show game over panel
                SetPanelActive(gameOverPanel, true);
                SetPanelActive(victoryPanel, false);

                if (gameOverTitleText != null)
                {
                    gameOverTitleText.text = $"Farewell, {displayName}";
                    gameOverTitleText.color = dangerMessageColor;
                }

                if (gameOverStatsText != null)
                {
                    gameOverStatsText.text = statsText;
                }
            }
        }

        public void OnRestartButtonClicked()
        {
            // Preserve player name for the new game
            if (Core.Player.Instance != null)
            {
                StartScreenManager.SetPendingPlayerName(Core.Player.Instance.PlayerName);
            }

            // Hide game over/victory panels
            SetPanelActive(gameOverPanel, false);
            SetPanelActive(victoryPanel, false);

            // Close any open panels (merchant, choice, etc.)
            SetPanelActive(choicePanelRoot, false);
            Core.Inventory.Instance?.DisableMerchantMode();

            // Clear game log
            ClearGameLog();

            // Reset UI state
            SetUIState(UIState.HUD);

            // Start new game
            Core.GameManager.Instance?.StartNewGame();
        }

        #endregion

        #region Level Up

        private System.Action<int> pendingLevelUpCallback;
        private string selectedWeakMonster;
        private string selectedToughMonster;

        private void OnPlayerLevelUp(int newLevel)
        {
            UpdateLevelDisplay();
            UpdateHealthDisplay(); // Max HP might have changed
            UpdateLevelUpButtonVisibility(); // Silver was spent, hide button
            ShowLevelUpPanel(newLevel);
        }

        private void ShowLevelUpPanel(int newLevel)
        {
            SetPanelActive(levelUpPanel, true);

            if (levelUpText != null)
            {
                levelUpText.text = $"LEVEL UP!\nYou are now level {newLevel}!";
            }

            // Auto-hide after delay
            StartCoroutine(HideLevelUpPanelAfterDelay(3f));
        }

        private IEnumerator HideLevelUpPanelAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetPanelActive(levelUpPanel, false);
        }

        /// <summary>
        /// Called when player rolls a 6 for level up and needs to choose monsters
        /// </summary>
        private void OnLevelUpChoice(int roll, System.Action<int> callback)
        {
            if (roll == 6)
            {
                // Track if we're interrupting merchant mode
                wasInMerchantBeforeLevelUp = Core.Inventory.Instance?.IsMerchantModeActive ?? false;

                pendingLevelUpCallback = callback;
                selectedWeakMonster = null;
                selectedToughMonster = null;
                ShowMonsterChoicePanel();
            }
        }

        private void ShowMonsterChoicePanel()
        {
            // First, show weak monster choices
            var weakMonsters = Combat.MonsterDatabase.GetAllWeakMonsters();
            var choices = new List<string>();

            foreach (var monster in weakMonsters)
            {
                choices.Add($"{monster.MonsterName} (Weak)");
            }

            ShowChoicePanel(
                "Choose Weak Monster",
                "Select a weak monster to halve its damage permanently:",
                choices.ToArray(),
                OnWeakMonsterChosen
            );
        }

        private void OnWeakMonsterChosen(int index)
        {
            var weakMonsters = Combat.MonsterDatabase.GetAllWeakMonsters();
            if (index >= 0 && index < weakMonsters.Length)
            {
                selectedWeakMonster = weakMonsters[index].MonsterName;
                ShowMessage($"Selected {selectedWeakMonster}. Now choose a tough monster.", MessageType.Info);

                // Now show tough monster choices
                var toughMonsters = Combat.MonsterDatabase.GetAllToughMonsters();
                var choices = new List<string>();

                foreach (var monster in toughMonsters)
                {
                    choices.Add($"{monster.MonsterName} (Tough)");
                }

                ShowChoicePanel(
                    "Choose Tough Monster",
                    "Select a tough monster to halve its damage permanently:",
                    choices.ToArray(),
                    OnToughMonsterChosen
                );
            }
        }

        private void OnToughMonsterChosen(int index)
        {
            var toughMonsters = Combat.MonsterDatabase.GetAllToughMonsters();
            if (index >= 0 && index < toughMonsters.Length)
            {
                selectedToughMonster = toughMonsters[index].MonsterName;

                // Apply the choices
                if (!string.IsNullOrEmpty(selectedWeakMonster))
                {
                    Core.Player.Instance?.AddHalvedDamageMonster(selectedWeakMonster);
                }
                if (!string.IsNullOrEmpty(selectedToughMonster))
                {
                    Core.Player.Instance?.AddHalvedDamageMonster(selectedToughMonster);
                }

                ShowMessage($"Damage halved for {selectedWeakMonster} and {selectedToughMonster}!", MessageType.Success);

                // Complete the level up
                pendingLevelUpCallback?.Invoke(6);
                pendingLevelUpCallback = null;

                // Restore merchant if we were in it before
                if (wasInMerchantBeforeLevelUp)
                {
                    wasInMerchantBeforeLevelUp = false;
                    // Re-trigger merchant display
                    Dungeon.RoomEncounterSystem.RefreshMerchantIfActive();
                }
            }
        }

        /// <summary>
        /// Show confirmation for silver level up
        /// </summary>
        public void ShowSilverLevelUpConfirmation()
        {
            if (Core.Player.Instance == null || !Core.Player.Instance.CanLevelUpBySilver) return;

            // Track if we're interrupting merchant mode
            bool wasInMerchant = Core.Inventory.Instance?.IsMerchantModeActive ?? false;

            ShowChoicePanel(
                "Level Up via Silver",
                $"Spend 40 silver to level up?\n(Current silver: {Core.Inventory.Instance?.Gold ?? 0})",
                new string[] { "Yes, level up!", "Not yet" },
                (index) => {
                    if (index == 0)
                    {
                        // Track merchant state for potential roll 6 choice
                        wasInMerchantBeforeLevelUp = wasInMerchant;
                        Core.Player.Instance.TryLevelUpBySilver();

                        // If the roll wasn't 6, restore merchant now
                        // (roll 6 will restore in OnToughMonsterChosen)
                        if (wasInMerchant && !wasInMerchantBeforeLevelUp)
                        {
                            // wasInMerchantBeforeLevelUp was reset, meaning roll wasn't 6
                            Dungeon.RoomEncounterSystem.RefreshMerchantIfActive();
                        }
                        else if (wasInMerchant && pendingLevelUpCallback == null)
                        {
                            // No pending callback means roll wasn't 6
                            wasInMerchantBeforeLevelUp = false;
                            Dungeon.RoomEncounterSystem.RefreshMerchantIfActive();
                        }
                    }
                    else
                    {
                        // Player said "Not yet", restore merchant if needed
                        if (wasInMerchant)
                        {
                            Dungeon.RoomEncounterSystem.RefreshMerchantIfActive();
                        }
                    }
                }
            );
        }

        /// <summary>
        /// Show False Omen choice UI - room choice or reroll
        /// </summary>
        public void ShowFalseOmenChoice(Core.Item falseOmenItem)
        {
            ShowChoicePanel(
                "False Omen",
                "Choose the power of the False Omen:",
                new string[] {
                    "Choose next room type (instead of random)",
                    "Reroll any die result (once)"
                },
                (index) => {
                    if (index == 0)
                    {
                        Core.Player.Instance?.ActivateFalseOmenRoomChoice();
                    }
                    else
                    {
                        Core.Player.Instance?.ActivateFalseOmenReroll();
                    }

                    // Consume the item
                    falseOmenItem.UseCharge();
                    if (!falseOmenItem.HasUsesRemaining())
                    {
                        Core.Inventory.Instance?.RemoveItem(falseOmenItem);
                    }
                    Core.Inventory.Instance?.NotifyInventoryChanged();
                }
            );
        }

        /// <summary>
        /// Show room type choice for False Omen
        /// </summary>
        public void ShowFalseOmenRoomChoice(System.Action<int> onChosen)
        {
            ShowChoicePanel(
                "False Omen - Choose Room",
                "What awaits in this room?",
                new string[] {
                    "Empty Room (safe)",
                    "Pit Trap (danger)",
                    "Riddling Soothsayer (mystery)",
                    "Weak Monster (combat)",
                    "Tough Monster (hard combat)",
                    "Merchant (shop)"
                },
                (index) => {
                    // EncounterType values: Empty=1, PitTrap=2, Soothsayer=3, WeakMonster=4, ToughMonster=5, Merchant=6
                    int encounterValue = index + 1;
                    LogMessage($"<color=#9966FF>False Omen: You chose {(Dungeon.EncounterType)encounterValue}!</color>");
                    onChosen?.Invoke(encounterValue);
                }
            );
        }

        #endregion

        #region Tooltips

        public void ShowTooltip(string text, Vector2 position)
        {
            if (tooltipPanel == null || tooltipText == null) return;

            tooltipText.text = text;
            tooltipPanel.SetActive(true);

            RectTransform rt = tooltipPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.position = position;
            }
        }

        public void HideTooltip()
        {
            SetPanelActive(tooltipPanel, false);
        }

        #endregion

        #region Event Callbacks

        private void OnPlayerHealthChanged(int current, int max)
        {
            UpdateHealthDisplay();

            // Flash health bar red if taking damage
            if (current < max * 0.25f)
            {
                ShowMessage("Health critical!", MessageType.Danger);
            }
        }

        private void OnPlayerXPChanged(int current, int required)
        {
            UpdateXPDisplay();
        }

        private void OnRoomsExploredChanged(int current, int required)
        {
            UpdateRoomsDisplay();
            // Exploration level up is now automatic - no notification needed
        }

        private void OnWeaponChanged()
        {
            UpdateWeaponDisplay();
            RefreshItemBar(); // Update inventory icons to show new equipped weapon
        }

        private void OnPlayerDeath()
        {
            ShowMessage("You have fallen...", MessageType.Danger, 5f);
        }

        private void OnGoldChanged(int newAmount)
        {
            UpdateGoldDisplay();
            UpdateLevelUpButtonVisibility();

            // Check if level up via silver is now available (silver level up is manual)
            if (Core.Player.Instance?.CanLevelUpBySilver == true && Core.GameManager.Instance?.CurrentGameState == Core.GameState.Exploring)
            {
                ShowMessage("You have 40+ silver! Press U to level up.", MessageType.Success);
            }
        }

        private void UpdateLevelUpButtonVisibility()
        {
            if (levelUpButton != null)
            {
                bool canLevelUp = Core.Player.Instance?.CanLevelUpBySilver == true;
                levelUpButton.SetActive(canLevelUp);
            }
        }

        private void OnGameStateChanged(Core.GameState newState)
        {
            // Update UI based on game state
            switch (newState)
            {
                case Core.GameState.Exploring:
                    SetUIState(UIState.HUD);
                    break;
                case Core.GameState.Combat:
                    // Combat UI handled by OnCombatStarted
                    break;
                case Core.GameState.Paused:
                    // Pause UI handled by OpenPauseMenu
                    break;
            }

            UpdateDirectionArrows();
        }

        private void OnRoomEntered(Dungeon.Room room, bool firstTime)
        {
            UpdateDirectionArrows();
        }

        #endregion

        #region UI State Management

        private void SetUIState(UIState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            OnUIStateChanged?.Invoke(newState);

            Debug.Log($"UI State: {newState}");
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }

        #endregion

        #region Debug

        [ContextMenu("Show Test Message")]
        private void DebugShowMessage()
        {
            ShowMessage("This is a test message!", MessageType.Info);
        }

        [ContextMenu("Show Warning Message")]
        private void DebugShowWarning()
        {
            ShowMessage("Warning! Low health!", MessageType.Warning);
        }

        [ContextMenu("Show Danger Message")]
        private void DebugShowDanger()
        {
            ShowMessage("DANGER! You are dying!", MessageType.Danger);
        }

        [ContextMenu("Show Success Message")]
        private void DebugShowSuccess()
        {
            ShowMessage("Victory! Enemy defeated!", MessageType.Success);
        }

        [ContextMenu("Update HUD")]
        private void DebugUpdateHUD()
        {
            UpdateHUD();
        }

        [ContextMenu("Toggle Combat UI")]
        private void DebugToggleCombat()
        {
            if (combatPanel != null)
            {
                bool isActive = !combatPanel.activeSelf;
                SetPanelActive(combatPanel, isActive);
                SetUIState(isActive ? UIState.Combat : UIState.HUD);
            }
        }

        #endregion
    }

    #region Enums

    public enum UIState
    {
        HUD,
        Combat,
        Inventory,
        Paused,
        GameOver,
        Dialogue
    }

    public enum MessageType
    {
        Normal,
        Warning,
        Danger,
        Success,
        Info
    }

    #endregion

    /// <summary>
    /// Type of content in an inventory slot
    /// </summary>
    public enum SlotContentType
    {
        Empty,
        Item,
        Weapon,
        EquippedWeapon,
        Unarmed
    }

    /// <summary>
    /// Individual slot for the inventory bar HUD - can hold items or weapons
    /// Supports desktop (click to use, hover for tooltip) and mobile (tap for tooltip, long-press to use)
    /// </summary>
    public class ItemBarSlot : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler,
                                               UnityEngine.EventSystems.IPointerEnterHandler,
                                               UnityEngine.EventSystems.IPointerExitHandler,
                                               UnityEngine.EventSystems.IPointerDownHandler,
                                               UnityEngine.EventSystems.IPointerUpHandler
    {
        private UIManager uiManager;
        private int slotIndex;
        private Core.Item currentItem;
        private Core.Weapon currentWeapon;
        private SlotContentType contentType = SlotContentType.Empty;
        private Image backgroundImage;
        private Image iconImage;
        private Image equippedIndicator;
        private TextMeshProUGUI usesText;
        private Color normalColor;
        private Color hoverColor;

        // Long press detection
        private bool isPointerDown = false;
        private float pointerDownTime = 0f;
        private const float longPressThreshold = 0.5f; // Half second for long press
        private bool longPressTriggered = false;

        public Core.Item CurrentItem => currentItem;
        public Core.Weapon CurrentWeapon => currentWeapon;
        public SlotContentType ContentType => contentType;

        public void Initialize(UIManager ui, int index, Color normal, Color hover)
        {
            uiManager = ui;
            slotIndex = index;
            normalColor = normal;
            hoverColor = hover;

            backgroundImage = GetComponent<Image>();
            iconImage = transform.Find("Icon")?.GetComponent<Image>();
            usesText = transform.Find("UsesText")?.GetComponent<TextMeshProUGUI>();
            equippedIndicator = transform.Find("EquippedIndicator")?.GetComponent<Image>();

            if (backgroundImage != null)
            {
                backgroundImage.color = normalColor;
            }
        }

        private void Update()
        {
            // Check for long press
            if (isPointerDown && !longPressTriggered)
            {
                if (Time.unscaledTime - pointerDownTime >= longPressThreshold)
                {
                    longPressTriggered = true;
                    OnLongPress();
                }
            }
        }

        public void SetItem(Core.Item item, Sprite icon)
        {
            currentItem = item;
            currentWeapon = null;
            contentType = SlotContentType.Item;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (usesText != null)
            {
                // Show stack count if stacked, otherwise uses remaining
                if (item.StackCount > 1)
                {
                    usesText.text = $"x{item.StackCount}";
                    usesText.enabled = true;
                }
                else if (item.UsesRemaining > 0)
                {
                    usesText.text = item.UsesRemaining.ToString();
                    usesText.enabled = true;
                }
                else
                {
                    usesText.enabled = false;
                }
            }

            if (equippedIndicator != null)
            {
                equippedIndicator.enabled = false;
            }
        }

        public void SetWeapon(Core.Weapon weapon, Sprite icon, bool isEquipped)
        {
            currentItem = null;
            currentWeapon = weapon;
            contentType = isEquipped ? SlotContentType.EquippedWeapon : SlotContentType.Weapon;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (usesText != null)
            {
                // Show damage dice for weapons
                usesText.text = weapon.DamageString;
                usesText.enabled = true;
            }

            if (equippedIndicator != null)
            {
                equippedIndicator.enabled = isEquipped;
            }
        }

        public void SetUnarmed(Sprite icon)
        {
            currentItem = null;
            currentWeapon = null;
            contentType = SlotContentType.Unarmed;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (usesText != null)
            {
                usesText.text = "?";
                usesText.enabled = true;
            }

            if (equippedIndicator != null)
            {
                equippedIndicator.enabled = false;
            }
        }

        public void ClearItem()
        {
            currentItem = null;
            currentWeapon = null;
            contentType = SlotContentType.Empty;

            if (iconImage != null)
                iconImage.enabled = false;

            if (usesText != null)
                usesText.enabled = false;

            if (equippedIndicator != null)
                equippedIndicator.enabled = false;
        }

        public void SetHighlight(bool highlighted)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = highlighted ? hoverColor : normalColor;
            }
        }

        /// <summary>
        /// Check if we're on a touch device (no mouse available or touch is primary)
        /// </summary>
        private bool IsTouchDevice()
        {
            // Check if there are active touches
            return UnityEngine.Input.touchCount > 0 ||
                   (UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled &&
                    UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0);
        }

        // Click handler - works for both desktop and mobile
        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            // If long press was triggered, don't also handle click
            if (longPressTriggered)
            {
                return;
            }

            // Check if this click came from touch
            bool isTouch = eventData.pointerId >= 0 && IsTouchDevice();

            if (isTouch)
            {
                // Mobile tap - show tooltip (toggle)
                uiManager?.OnItemSlotHoverEnter(this);
            }
            else
            {
                // Desktop click - use item
                uiManager?.OnItemSlotClicked(this);
            }
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            isPointerDown = true;
            pointerDownTime = Time.unscaledTime;
            longPressTriggered = false;

            // Show highlight
            SetHighlight(true);
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            isPointerDown = false;
            SetHighlight(false);
        }

        private void OnLongPress()
        {
            // Long press - use/equip item (same as desktop click)
            uiManager?.OnItemSlotClicked(this);

            // Hide tooltip after action
            uiManager?.HideTooltip();
        }

        // Desktop: Hover to show tooltip
        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            // Always show tooltip on hover - works for both mouse and touch drag-over
            uiManager?.OnItemSlotHoverEnter(this);
            SetHighlight(true);
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            uiManager?.OnItemSlotHoverExit(this);
            SetHighlight(false);
        }
    }
}
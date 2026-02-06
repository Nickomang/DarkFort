namespace DarkFort
{
    /// <summary>
    /// Game-wide constants and configuration values
    /// </summary>
    public static class Constants
    {
        #region Scene Names
        public const string SCENE_MAIN_MENU = "MainMenu";
        public const string SCENE_GAMEPLAY = "Gameplay";
        #endregion

        #region Player Defaults
        public const int PLAYER_STARTING_HEALTH = 20;
        public const int PLAYER_STARTING_LEVEL = 1;
        public const int PLAYER_STARTING_GOLD = 50;
        public const int PLAYER_BASE_DICE_COUNT = 2;
        public const int PLAYER_BASE_DICE_SIZE = 6;
        public const int PLAYER_HP_PER_LEVEL = 5;
        public const int PLAYER_DAMAGE_PER_HIT = 5;
        #endregion

        #region Win/Loss Conditions
        public const int TARGET_LEVEL_FOR_VICTORY = 7; // Win at level 7
        public const int XP_REQUIRED_FOR_LEVEL_UP = 15;
        public const int ROOMS_REQUIRED_FOR_LEVEL_UP = 12;
        #endregion

        #region Dungeon Generation
        public const int INITIAL_ROOM_COUNT = 5;
        public const int MIN_EXITS_PER_ROOM = 1;
        public const int MAX_EXITS_PER_ROOM = 3;
        public const float DEAD_END_CHANCE = 0.15f;
        #endregion

        #region Room Type Probabilities
        public const float COMBAT_ROOM_CHANCE = 0.4f;
        public const float TREASURE_ROOM_CHANCE = 0.2f;
        public const float MERCHANT_ROOM_CHANCE = 0.1f;
        // Remaining probability (0.3f) is for empty rooms
        #endregion

        #region Combat
        public const float FLEE_SUCCESS_CHANCE = 0.5f;
        public const float BASE_LOOT_DROP_CHANCE = 0.3f;
        public const float BOSS_LOOT_DROP_CHANCE = 1.0f;
        #endregion

        #region Inventory
        public const int MAX_INVENTORY_SIZE = 20;
        #endregion

        #region Item Rarity Weights
        public const float COMMON_RARITY_WEIGHT = 0.5f;      // 50%
        public const float UNCOMMON_RARITY_WEIGHT = 0.3f;    // 30%
        public const float RARE_RARITY_WEIGHT = 0.15f;       // 15%
        public const float EPIC_RARITY_WEIGHT = 0.04f;       // 4%
        public const float LEGENDARY_RARITY_WEIGHT = 0.01f;  // 1%
        #endregion

        #region UI
        public const float MESSAGE_DISPLAY_TIME = 3f;
        public const int UNEXPLORED_ROOMS_WARNING_THRESHOLD = 3;
        public const int UNEXPLORED_ROOMS_CAUTION_THRESHOLD = 10;
        #endregion

        #region XP Scaling
        /// <summary>
        /// Calculate XP required for a given level
        /// </summary>
        public static int XPForLevel(int level)
        {
            return 10 * level;
        }
        #endregion

        #region Monster Scaling
        /// <summary>
        /// Get the appropriate monster difficulty for a player level
        /// Note: In Dark Fort, encounters are determined by d6 room roll (4=Weak, 5=Tough)
        /// This function is kept for potential future use
        /// </summary>
        public static Combat.MonsterDifficulty GetMonsterDifficultyForLevel(int playerLevel)
        {
            // In Dark Fort, this is typically determined by room encounter roll
            // but could be used for dynamic difficulty scaling
            if (playerLevel <= 3)
            {
                return Combat.MonsterDifficulty.Weak;
            }
            else
            {
                return UnityEngine.Random.value < 0.5f
                    ? Combat.MonsterDifficulty.Weak
                    : Combat.MonsterDifficulty.Tough;
            }
        }
        #endregion

        #region Layer Names (for future use)
        public const string LAYER_DEFAULT = "Default";
        public const string LAYER_UI = "UI";
        public const string LAYER_PLAYER = "Player";
        public const string LAYER_ENEMY = "Enemy";
        #endregion

        #region Tags (for future use)
        public const string TAG_PLAYER = "Player";
        public const string TAG_ENEMY = "Enemy";
        public const string TAG_GAME_MANAGER = "GameManager";
        #endregion
    }
}
using UnityEngine;
using System;
using System.Collections.Generic;
using DarkFort.Combat;
using DarkFort.UI;

namespace DarkFort.Core
{
    /// <summary>
    /// Manages player stats, equipment, and progression
    /// </summary>
    public class Player : MonoBehaviour
    {
        #region Singleton
        public static Player Instance { get; private set; }
        #endregion

        #region Stats
        [Header("Player Identity")]
        [SerializeField] private string playerName = "Adventurer";

        [Header("Core Stats")]
        [SerializeField] private int maxHealth = 15;
        [SerializeField] private int currentHealth = 15;
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentXP = 0;
        [SerializeField] private int xpRequiredForLevelUp = 15;
        [SerializeField] private int roomsExplored = 1; // Start at 1 (entrance room)
        [SerializeField] private int roomsRequiredForLevelUp = 12;
        [SerializeField] private int silverRequiredForLevelUp = 40;

        [Header("Lifetime Stats (for game over/victory)")]
        [SerializeField] private int totalRoomsExplored = 1;
        [SerializeField] private int totalSilverCollected = 0;
        [SerializeField] private int totalMonstersKilled = 0;

        [Header("Equipment")]
        [SerializeField] private Weapon equippedWeapon;

        [Header("Level Up Bonuses")]
        [SerializeField] private bool isKnighted = false;
        [SerializeField] private int bonusToHit = 0;
        [SerializeField] private List<string> halvedDamageMonsters = new List<string>();

        // Track which level up results have been used (1-6)
        private List<int> usedLevelUpResults = new List<int>();

        [Header("Active Scroll Effects")]
        [SerializeField] private int daemonFightsRemaining = 0;  // Summon Weak Daemon
        [SerializeField] private int aegisChargesRemaining = 0;  // Aegis of Sorrow damage prevention
        [SerializeField] private bool falseOmenRoomChoice = false; // Can choose next room type
        [SerializeField] private bool falseOmenReroll = false;    // Can reroll a die
        [SerializeField] private int cloakInvisibilityRemaining = 0; // Cloak of Invisibility bypasses
        #endregion

        #region Lifetime Stats Properties
        public int TotalRoomsExplored => totalRoomsExplored;
        public int TotalSilverCollected => totalSilverCollected;
        public int TotalMonstersKilled => totalMonstersKilled;

        public void AddSilverCollected(int amount)
        {
            if (amount > 0)
                totalSilverCollected += amount;
        }

        public void IncrementMonstersKilled()
        {
            totalMonstersKilled++;
        }
        #endregion

        #region Events
        public delegate void HealthChangedHandler(int current, int max);
        public event HealthChangedHandler OnHealthChanged;

        public delegate void LevelUpHandler(int newLevel);
        public event LevelUpHandler OnLevelUp;

        public delegate void LevelUpBuffAppliedHandler(int rollResult);
        public event LevelUpBuffAppliedHandler OnLevelUpBuffApplied;

        public delegate void PlayerDeathHandler();
        public event PlayerDeathHandler OnPlayerDeath;

        public delegate void XPChangedHandler(int current, int required);
        public event XPChangedHandler OnXPChanged;

        public delegate void RoomsExploredChangedHandler(int current, int required);
        public event RoomsExploredChangedHandler OnRoomsExploredChanged;

        public delegate void WeaponChangedHandler();
        public event WeaponChangedHandler OnWeaponChanged;

        public delegate void LevelUpChoiceHandler(int roll, System.Action<int> onChoiceMade);
        public event LevelUpChoiceHandler OnLevelUpChoice;

        public delegate void ScrollEffectChangedHandler();
        public event ScrollEffectChangedHandler OnScrollEffectChanged;
        #endregion

        #region Properties
        public string PlayerName => playerName;
        public string DisplayName => isKnighted ? $"Sir {playerName}" : playerName;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public int CurrentLevel => currentLevel;
        public int CurrentXP => currentXP;
        public int XPRequiredForLevelUp => xpRequiredForLevelUp;
        public int RoomsExplored => roomsExplored;
        public int RoomsRequiredForLevelUp => roomsRequiredForLevelUp;
        public int SilverRequiredForLevelUp => silverRequiredForLevelUp;
        public bool IsAlive => currentHealth > 0;
        public Weapon EquippedWeapon => equippedWeapon;
        public bool IsUnarmed => equippedWeapon == null;
        public bool IsKnighted => isKnighted;
        public int BonusToHit => bonusToHit;
        public IReadOnlyList<string> HalvedDamageMonsters => halvedDamageMonsters;
        public int HalvedDamageMonsterCount => halvedDamageMonsters.Count;
        public IReadOnlyList<int> UsedLevelUpResults => usedLevelUpResults;

        /// <summary>
        /// Set the player's name (called from start screen)
        /// </summary>
        public void SetPlayerName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                playerName = name;
            }
        }

        /// <summary>
        /// Check if a specific level up result has been used (1-6)
        /// </summary>
        public bool HasUsedLevelUpResult(int result)
        {
            return usedLevelUpResults.Contains(result);
        }

        /// <summary>
        /// Get list of monsters with halved damage for display
        /// </summary>
        public List<string> GetHalvedDamageMonsters()
        {
            return new List<string>(halvedDamageMonsters);
        }

        // Scroll effect properties
        public int DaemonFightsRemaining => daemonFightsRemaining;
        public bool HasDaemon => daemonFightsRemaining > 0;
        public int AegisChargesRemaining => aegisChargesRemaining;
        public bool HasAegis => aegisChargesRemaining > 0;
        public bool HasFalseOmenRoomChoice => falseOmenRoomChoice;
        public bool HasFalseOmenReroll => falseOmenReroll;
        public int CloakInvisibilityRemaining => cloakInvisibilityRemaining;
        public bool HasCloakInvisibility => cloakInvisibilityRemaining > 0;

        public bool CanLevelUpByExploration => roomsExplored >= roomsRequiredForLevelUp && currentXP >= xpRequiredForLevelUp;
        public bool CanLevelUpBySilver => Inventory.Instance != null && Inventory.Instance.Gold >= silverRequiredForLevelUp;
        public bool CanLevelUp => CanLevelUpByExploration || CanLevelUpBySilver;
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
            // Check for pending player name from start screen
            if (StartScreenManager.HasPendingPlayerName)
            {
                string name = StartScreenManager.GetPendingPlayerName();
                SetPlayerName(name);
                Debug.Log($"Player name set from start screen: {playerName}");
            }

            // Roll for starting weapon and item
            RollStartingEquipment();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region Initialization
        private void RollStartingEquipment()
        {
            // Roll starting weapon (d4: Warhammer, Dagger, Sword, Flail)
            equippedWeapon = WeaponDatabase.RollStartingWeapon();
            OnWeaponChanged?.Invoke();

            // Roll starting item (d4: Armour, Potion, Summon Weak Daemon, Cloak of Invisibility)
            Item startingItem = ItemDatabase.RollStartingItem();
            Inventory.Instance?.AddItem(startingItem);
        }
        #endregion

        #region Health Management
        public void TakeDamage(int damage)
        {
            if (!IsAlive) return;

            // Try to prevent damage with Aegis of Sorrow
            int prevented = TryPreventDamageWithAegis(damage);
            int actualDamage = damage - prevented;

            if (actualDamage <= 0)
            {
                Debug.Log($"All {damage} damage prevented by Aegis!");
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - actualDamage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log($"Player took {actualDamage} damage (Aegis prevented {prevented}). Health: {currentHealth}/{maxHealth}");

            if (!IsAlive)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log($"Player healed {amount}. Health: {currentHealth}/{maxHealth}");
        }

        public void IncreaseMaxHealth(int amount)
        {
            maxHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            Debug.Log($"Max health increased by {amount}. New max: {maxHealth}");
        }

        public void SetMaxHealth(int newMax)
        {
            maxHealth = newMax;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Set current health directly (used for reroll system)
        /// </summary>
        public void SetHealth(int health)
        {
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            Debug.Log("Player has died!");
            OnPlayerDeath?.Invoke();
            GameManager.Instance.EndGame(false); // Defeat
        }
        #endregion

        #region Experience & Leveling
        public void GainXP(int amount)
        {
            currentXP += amount;
            Debug.Log($"Gained {amount} XP. Current: {currentXP}/{xpRequiredForLevelUp}");
            OnXPChanged?.Invoke(currentXP, xpRequiredForLevelUp);

            if (CanLevelUp)
            {
                Debug.Log("Ready to level up! (12 rooms explored and 15+ XP)");
            }
        }

        public void IncrementRoomsExplored()
        {
            roomsExplored++;
            totalRoomsExplored++;
            Debug.Log($"Rooms explored: {roomsExplored}/{roomsRequiredForLevelUp} (Total: {totalRoomsExplored})");
            OnRoomsExploredChanged?.Invoke(roomsExplored, roomsRequiredForLevelUp);

            // Auto level up when exploration requirements are met
            if (CanLevelUpByExploration)
            {
                Debug.Log("Auto level up triggered (12 rooms + 15 XP)!");
                TryLevelUpByExploration();
            }
        }

        /// <summary>
        /// Decrement rooms explored (used when fleeing combat - room doesn't count)
        /// Note: Does NOT decrement totalRoomsExplored since you still visited the room
        /// </summary>
        public void DecrementRoomsExplored()
        {
            if (roomsExplored > 0)
            {
                roomsExplored--;
                Debug.Log($"Room uncounted (fled). Rooms explored: {roomsExplored}/{roomsRequiredForLevelUp}");
                OnRoomsExploredChanged?.Invoke(roomsExplored, roomsRequiredForLevelUp);
            }
        }

        /// <summary>
        /// Attempt to level up via exploration (12 rooms + 15 XP)
        /// </summary>
        public void TryLevelUpByExploration()
        {
            if (!CanLevelUpByExploration)
            {
                Debug.LogWarning("Cannot level up by exploration yet!");
                return;
            }

            // Reset XP and rooms
            currentXP = 0;
            roomsExplored = 0;

            OnXPChanged?.Invoke(currentXP, xpRequiredForLevelUp);
            OnRoomsExploredChanged?.Invoke(roomsExplored, roomsRequiredForLevelUp);

            PerformLevelUp("exploration");
        }

        /// <summary>
        /// Attempt to level up via silver (40 silver)
        /// </summary>
        public void TryLevelUpBySilver()
        {
            if (!CanLevelUpBySilver)
            {
                Debug.LogWarning("Cannot level up by silver yet!");
                return;
            }

            // Spend 40 silver
            Inventory.Instance.SpendGold(silverRequiredForLevelUp);

            PerformLevelUp("silver");
        }

        /// <summary>
        /// Core level up logic - rolls d6 and applies result
        /// </summary>
        private void PerformLevelUp(string method)
        {
            currentLevel++;
            Debug.Log($"LEVEL UP via {method}! Now level {currentLevel}.");

            // Log to game log
            UI.UIManager.Instance?.LogMessage($"<color=#FFD700>=== LEVEL UP! (Level {currentLevel}) ===</color>");
            UI.UIManager.Instance?.LogMessage($"<color=#AAAAAA>Method: {method}</color>");

            // Check win condition first
            if (currentLevel >= 7)
            {
                Debug.Log("Reached level 7! VICTORY!");
                UI.UIManager.Instance?.LogMessage("<color=#00FF00>You have reached level 7! VICTORY!</color>");
                OnLevelUp?.Invoke(currentLevel);
                GameManager.Instance.EndGame(true);
                return;
            }

            // Roll for level up bonus
            int roll = RollLevelUpBonus();

            OnLevelUp?.Invoke(currentLevel);

            // If roll is 6 (monster halving), we need UI to let player choose
            if (roll == 6)
            {
                OnLevelUpChoice?.Invoke(roll, ApplyLevelUpResult);
            }
            else
            {
                ApplyLevelUpResult(roll);
            }
        }

        /// <summary>
        /// Roll d6 for level up bonus, rerolling if result already used
        /// </summary>
        private int RollLevelUpBonus()
        {
            // If all results used, just return 0 (no bonus)
            if (usedLevelUpResults.Count >= 6)
            {
                Debug.Log("All level up bonuses already claimed!");
                return 0;
            }

            int roll;
            int attempts = 0;
            do
            {
                roll = UnityEngine.Random.Range(1, 7);
                attempts++;
            } while (usedLevelUpResults.Contains(roll) && attempts < 100);

            Debug.Log($"Level up roll: {roll} (attempt {attempts})");
            return roll;
        }

        /// <summary>
        /// Apply a level up result (called directly or after player choice for roll 6)
        /// </summary>
        public void ApplyLevelUpResult(int roll)
        {
            if (roll <= 0 || usedLevelUpResults.Contains(roll))
            {
                Debug.Log($"Level up result {roll} already used or invalid");
                return;
            }

            usedLevelUpResults.Add(roll);

            // Log the roll to the game log
            UI.UIManager.Instance?.LogMessage($"<color=#FFD700>Level up roll: {roll}</color>");

            switch (roll)
            {
                case 1: // Knighted
                    isKnighted = true;
                    UI.UIManager.Instance?.LogMessage("<color=#00FF00>You have been Knighted! You are now 'Sir'.</color>");
                    Debug.Log("Level up: Knighted!");
                    break;

                case 2: // +1 to hit
                    bonusToHit += 1;
                    UI.UIManager.Instance?.LogMessage($"<color=#00FF00>Combat training! +1 to all attack rolls. (Total: +{bonusToHit})</color>");
                    Debug.Log("Level up: +1 to hit!");
                    break;

                case 3: // +5 max HP
                    maxHealth += 5;
                    currentHealth += 5; // Also heal the bonus amount
                    OnHealthChanged?.Invoke(currentHealth, maxHealth);
                    UI.UIManager.Instance?.LogMessage($"<color=#00FF00>Fortitude increased! +5 maximum HP. (Now {maxHealth} max)</color>");
                    Debug.Log("Level up: +5 max HP!");
                    break;

                case 4: // 5 potions
                    for (int i = 0; i < 5; i++)
                    {
                        Inventory.Instance?.AddItem(ItemDatabase.Potion.Clone());
                    }
                    UI.UIManager.Instance?.LogMessage("<color=#00FF00>A not very occult herbmaster salutes you and gives you 5 potions!</color>");
                    Debug.Log("Level up: 5 potions!");
                    break;

                case 5: // Zweihander
                    Weapon zweihander = new Weapon("Zweihander", new DiceRoll(1, 6, 2), 0, "A mighty two-handed sword.", 25, 25);
                    if (IsUnarmed)
                    {
                        EquipWeapon(zweihander);
                        UI.UIManager.Instance?.LogMessage("<color=#00FF00>You find a mighty Zweihander and equip it! (d6+2 damage)</color>");
                    }
                    else
                    {
                        Inventory.Instance?.AddWeapon(zweihander);
                        UI.UIManager.Instance?.LogMessage("<color=#00FF00>You find a mighty Zweihander! (d6+2 damage)</color>");
                    }
                    Debug.Log("Level up: Zweihander!");
                    break;

                case 6: // Halve monster damage - UI will handle the choice
                    UI.UIManager.Instance?.LogMessage("<color=#00FF00>Monster Slayer! Choose monsters to halve their damage...</color>");
                    Debug.Log("Level up: Monster damage halving (awaiting choice)");
                    break;
            }

            // Notify listeners that a buff was applied
            OnLevelUpBuffApplied?.Invoke(roll);
        }

        /// <summary>
        /// Add a monster to the halved damage list
        /// </summary>
        public void AddHalvedDamageMonster(string monsterName)
        {
            if (!halvedDamageMonsters.Contains(monsterName))
            {
                halvedDamageMonsters.Add(monsterName);
                UI.UIManager.Instance?.LogMessage($"<color=#00FF00>{monsterName} damage now halved!</color>");
                Debug.Log($"Monster damage halved: {monsterName}");
            }
        }

        /// <summary>
        /// Check if a monster's damage should be halved
        /// </summary>
        public bool IsMonsterDamageHalved(string monsterName)
        {
            return halvedDamageMonsters.Contains(monsterName);
        }

        /// <summary>
        /// Force level up (bypasses room/XP/silver requirements, used by special effects like Ruin Basilisk)
        /// Resets XP to zero
        /// </summary>
        public void ForceLevelUp()
        {
            currentXP = 0;
            OnXPChanged?.Invoke(currentXP, xpRequiredForLevelUp);

            PerformLevelUp("ancient power");
        }

        /// <summary>
        /// Legacy method - use TryLevelUpByExploration or TryLevelUpBySilver instead
        /// </summary>
        public int LevelUp()
        {
            if (CanLevelUpByExploration)
            {
                TryLevelUpByExploration();
            }
            else if (CanLevelUpBySilver)
            {
                TryLevelUpBySilver();
            }
            return currentLevel;
        }
        #endregion

        #region Weapon Management
        public void EquipWeapon(Weapon weapon)
        {
            equippedWeapon = weapon; // Can be null (unarmed)

            if (weapon != null)
                Debug.Log($"Equipped {weapon.WeaponName}");
            else
                Debug.Log("Now unarmed");

            OnWeaponChanged?.Invoke();
        }

        /// <summary>
        /// Unequip current weapon, returning it (or null if already unarmed)
        /// </summary>
        public Weapon UnequipWeapon()
        {
            Weapon oldWeapon = equippedWeapon;
            equippedWeapon = null;
            Debug.Log("Unequipped weapon - now unarmed");
            OnWeaponChanged?.Invoke();
            return oldWeapon;
        }

        public Weapon GetEquippedWeapon()
        {
            return equippedWeapon;
        }
        #endregion

        #region Combat
        /// <summary>
        /// Roll attack against a target hit value
        /// Returns the total roll (d6 + weapon hit bonus + level up bonus)
        /// </summary>
        public int RollAttack()
        {
            int diceRoll = UnityEngine.Random.Range(1, 7); // d6
            int weaponHitBonus = equippedWeapon?.HitBonus ?? 0;
            int totalRoll = diceRoll + weaponHitBonus + bonusToHit;

            string bonusStr = "";
            if (weaponHitBonus > 0) bonusStr += $" + {weaponHitBonus} weapon";
            if (bonusToHit > 0) bonusStr += $" + {bonusToHit} level";

            Debug.Log($"Attack: d6({diceRoll}){bonusStr} = {totalRoll}");
            return totalRoll;
        }

        /// <summary>
        /// Get the damage value from equipped weapon (or unarmed d4-1)
        /// </summary>
        public int GetWeaponDamage()
        {
            if (equippedWeapon != null)
            {
                return equippedWeapon.RollDamage();
            }
            else
            {
                // Unarmed: d4-1, minimum 1
                int unarmedDamage = UnityEngine.Random.Range(1, 5) - 1; // d4-1
                return Mathf.Max(1, unarmedDamage);
            }
        }

        /// <summary>
        /// Get damage display string for UI
        /// </summary>
        public string GetWeaponDamageString()
        {
            if (equippedWeapon != null)
            {
                return equippedWeapon.DamageString;
            }
            else
            {
                return "d4-1";
            }
        }
        #endregion

        #region Scroll Effects
        /// <summary>
        /// Activate Summon Weak Daemon scroll
        /// </summary>
        public void ActivateDaemon()
        {
            int fights = UnityEngine.Random.Range(1, 5); // d4
            daemonFightsRemaining = fights;
            Debug.Log($"Daemon summoned for {fights} fights!");
            UI.UIManager.Instance?.ShowMessage($"A weak daemon appears! It will help for {fights} fights.", UI.MessageType.Success);
            OnScrollEffectChanged?.Invoke();
        }

        /// <summary>
        /// Called when daemon attacks in combat, returns damage dealt
        /// </summary>
        public int DaemonAttack()
        {
            if (!HasDaemon) return 0;
            int damage = UnityEngine.Random.Range(1, 5); // d4
            Debug.Log($"Daemon deals {damage} damage!");
            return damage;
        }

        /// <summary>
        /// Called when a fight ends to decrement daemon counter
        /// </summary>
        public void OnFightEnded()
        {
            if (daemonFightsRemaining > 0)
            {
                daemonFightsRemaining--;
                Debug.Log($"Daemon fights remaining: {daemonFightsRemaining}");
                if (daemonFightsRemaining == 0)
                {
                    UI.UIManager.Instance?.ShowMessage("The daemon fades away...", UI.MessageType.Info);
                }
                OnScrollEffectChanged?.Invoke();
            }
        }

        /// <summary>
        /// Activate Aegis of Sorrow scroll
        /// </summary>
        public void ActivateAegis()
        {
            int charges = UnityEngine.Random.Range(1, 5); // d4
            aegisChargesRemaining += charges;
            Debug.Log($"Aegis activated with {charges} charges! Total: {aegisChargesRemaining}");
            UI.UIManager.Instance?.ShowMessage($"Aegis of Sorrow activated! {charges} damage will be prevented.", UI.MessageType.Success);
            OnScrollEffectChanged?.Invoke();
        }

        /// <summary>
        /// Try to prevent damage with Aegis, returns amount actually prevented
        /// </summary>
        public int TryPreventDamageWithAegis(int incomingDamage)
        {
            if (!HasAegis) return 0;

            int prevented = Mathf.Min(aegisChargesRemaining, incomingDamage);
            aegisChargesRemaining -= prevented;

            if (prevented > 0)
            {
                Debug.Log($"Aegis prevented {prevented} damage! Charges remaining: {aegisChargesRemaining}");
                UI.UIManager.Instance?.ShowMessage($"Aegis absorbs {prevented} damage!", UI.MessageType.Info);

                if (aegisChargesRemaining == 0)
                {
                    UI.UIManager.Instance?.ShowMessage("Aegis of Sorrow fades...", UI.MessageType.Info);
                }
                OnScrollEffectChanged?.Invoke();
            }

            return prevented;
        }

        /// <summary>
        /// Activate False Omen scroll - choose room OR reroll
        /// </summary>
        public void ActivateFalseOmenRoomChoice()
        {
            falseOmenRoomChoice = true;
            Debug.Log("False Omen (Room Choice) activated!");
            UI.UIManager.Instance?.ShowMessage("False Omen: You may choose the next room type.", UI.MessageType.Success);
            OnScrollEffectChanged?.Invoke();
        }

        public void ActivateFalseOmenReroll()
        {
            falseOmenReroll = true;
            Debug.Log("False Omen (Reroll) activated!");
            UI.UIManager.Instance?.ShowMessage("False Omen: You may reroll any die result. Press R when ready.", UI.MessageType.Success);
            OnScrollEffectChanged?.Invoke();

            // Notify RerollSystem that reroll is now available
            RerollSystem.Instance?.RefreshAvailability();
        }

        /// <summary>
        /// Use False Omen room choice ability
        /// </summary>
        public void UseFalseOmenRoomChoice()
        {
            if (falseOmenRoomChoice)
            {
                falseOmenRoomChoice = false;
                Debug.Log("False Omen (Room Choice) consumed!");
                OnScrollEffectChanged?.Invoke();
            }
        }

        /// <summary>
        /// Use False Omen reroll ability
        /// </summary>
        public void UseFalseOmenReroll()
        {
            if (falseOmenReroll)
            {
                falseOmenReroll = false;
                Debug.Log("False Omen (Reroll) consumed!");
                OnScrollEffectChanged?.Invoke();
            }
        }

        /// <summary>
        /// Use Palms of the Southern Gate scroll - deals d6+1 damage
        /// </summary>
        public int UsePalmsDamage()
        {
            int damage = UnityEngine.Random.Range(1, 7) + 1; // d6+1
            Debug.Log($"Palms of the Southern Gate deals {damage} damage!");
            UI.UIManager.Instance?.ShowMessage($"Palms of the Southern Gate deals {damage} damage!", UI.MessageType.Success);
            return damage;
        }

        /// <summary>
        /// Activate Cloak of Invisibility - bypass d4 monster encounters
        /// </summary>
        public void ActivateCloakInvisibility()
        {
            int bypasses = UnityEngine.Random.Range(1, 5); // d4
            cloakInvisibilityRemaining = bypasses;
            Debug.Log($"Cloak of Invisibility activated for {bypasses} encounters!");
            UI.UIManager.Instance?.LogMessage($"<color=#9966FF>Cloak of Invisibility activated! You can bypass {bypasses} monster encounters.</color>");
            OnScrollEffectChanged?.Invoke();
        }

        /// <summary>
        /// Try to use cloak invisibility to bypass a monster encounter
        /// Returns true if successfully bypassed
        /// </summary>
        public bool TryUseCloakInvisibility(Combat.Monster monster)
        {
            if (!HasCloakInvisibility) return false;

            cloakInvisibilityRemaining--;
            Debug.Log($"Cloak invisibility used! Remaining: {cloakInvisibilityRemaining}");

            // Log the bypass
            UI.UIManager.Instance?.LogMessage($"<color=#9966FF>You use your invisibility to sneak past the {monster.MonsterName} silently.</color>");

            // Grant XP
            GainXP(monster.XPReward);
            UI.UIManager.Instance?.LogMessage($"<color=#00FF00>Victory! +{monster.XPReward} XP (no loot)</color>");

            if (cloakInvisibilityRemaining == 0)
            {
                UI.UIManager.Instance?.LogMessage("<color=#9966FF>Your cloak's magic fades...</color>");
            }

            OnScrollEffectChanged?.Invoke();
            return true;
        }
        #endregion

        #region Reset
        public void ResetPlayer()
        {
            currentHealth = 15;
            maxHealth = 15;
            currentLevel = 1;
            currentXP = 0;
            roomsExplored = 1;

            // Reset lifetime stats
            totalRoomsExplored = 1;
            totalSilverCollected = 0;
            totalMonstersKilled = 0;

            // Reset level up bonuses
            isKnighted = false;
            bonusToHit = 0;
            halvedDamageMonsters.Clear();
            usedLevelUpResults.Clear();

            // Reset scroll effects
            daemonFightsRemaining = 0;
            aegisChargesRemaining = 0;
            falseOmenRoomChoice = false;
            falseOmenReroll = false;
            cloakInvisibilityRemaining = 0;

            RollStartingEquipment();

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnXPChanged?.Invoke(currentXP, xpRequiredForLevelUp);
            OnRoomsExploredChanged?.Invoke(roomsExplored, roomsRequiredForLevelUp);
        }
        #endregion

        #region Debug
        [ContextMenu("Take 5 Damage")]
        private void DebugTakeDamage()
        {
            TakeDamage(5);
        }

        [ContextMenu("Heal 5 HP")]
        private void DebugHeal()
        {
            Heal(5);
        }

        [ContextMenu("Gain 5 XP")]
        private void DebugGainXP()
        {
            GainXP(5);
        }

        [ContextMenu("Explore Room")]
        private void DebugExploreRoom()
        {
            IncrementRoomsExplored();
        }

        [ContextMenu("Force Level Up")]
        private void DebugForceLevelUp()
        {
            currentXP = 15;
            roomsExplored = 12;
            int roll = LevelUp();
            Debug.Log($"Level up roll: {roll}");
        }

        [ContextMenu("Unequip Weapon")]
        private void DebugUnequipWeapon()
        {
            Weapon old = UnequipWeapon();
            if (old != null)
            {
                Inventory.Instance?.AddWeapon(old);
            }
        }

        [ContextMenu("Print Player Stats")]
        private void DebugPrintStats()
        {
            Debug.Log($"=== PLAYER STATS ===");
            Debug.Log($"Level: {currentLevel}");
            Debug.Log($"Health: {currentHealth}/{maxHealth}");
            Debug.Log($"XP: {currentXP}/{xpRequiredForLevelUp}");
            Debug.Log($"Rooms: {roomsExplored}/{roomsRequiredForLevelUp}");
            Debug.Log($"Can Level Up: {CanLevelUp}");
            Debug.Log($"Weapon: {(equippedWeapon != null ? equippedWeapon.WeaponName : "Unarmed (d4-1)")}");
            if (equippedWeapon != null)
            {
                Debug.Log($"  Damage: {equippedWeapon.DamageString}, Hit Bonus: {equippedWeapon.HitBonus}");
            }
        }
        #endregion
    }
}
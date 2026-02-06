using UnityEngine;
using DarkFort.Core;
using DarkFort.UI;

namespace DarkFort.Combat
{
    /// <summary>
    /// Manages combat encounters between player and monsters
    /// Combat rules: 
    /// - Player rolls d6 + hit bonus. If >= monster hit value, deal weapon damage to monster HP.
    /// - If player misses, monster rolls damage dice and deals damage to player.
    /// - Combat continues until monster HP reaches 0 or player dies/flees.
    /// - Monster effects trigger at appropriate times during combat.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        #region Singleton
        public static CombatManager Instance { get; private set; }
        #endregion

        #region Events
        public delegate void CombatStartedHandler(Monster monster);
        public event CombatStartedHandler OnCombatStarted;

        public delegate void CombatEndedHandler(bool playerVictory);
        public event CombatEndedHandler OnCombatEnded;

        public delegate void AttackResultHandler(int playerRoll, int requiredRoll, bool hit, int damage);
        public event AttackResultHandler OnAttackResult;

        public delegate void MonsterDamagedHandler(Monster monster, int damage, int remainingHP);
        public event MonsterDamagedHandler OnMonsterDamaged;

        public delegate void RoundStartedHandler(int round);
        public event RoundStartedHandler OnRoundStarted;

        public delegate void RoundEndedHandler(int round);
        public event RoundEndedHandler OnRoundEnded;

        public delegate void PlayerFledHandler();
        public event PlayerFledHandler OnPlayerFled;
        #endregion

        #region State
        [Header("Current Combat")]
        [SerializeField] private Monster currentMonster;
        [SerializeField] private bool isInCombat = false;
        [SerializeField] private int combatRound = 0;
        #endregion

        #region Properties
        public Monster CurrentMonster => currentMonster;
        public bool IsInCombat => isInCombat;
        public int CombatRound => combatRound;
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
            {
                Instance = null;
            }
        }
        #endregion

        #region Combat Flow
        /// <summary>
        /// Start combat with a monster
        /// </summary>
        public void StartCombat(Monster monster)
        {
            if (isInCombat)
            {
                Debug.LogWarning("Already in combat!");
                return;
            }

            currentMonster = monster;
            isInCombat = true;
            combatRound = 0;

            Debug.Log($"Combat started with {monster.MonsterName}!");
            Debug.Log(monster.ToString());

            // Trigger monster's combat start effects
            currentMonster.TriggerOnCombatStart();

            OnCombatStarted?.Invoke(monster);
            GameManager.Instance?.ChangeGameState(GameState.Combat);
        }

        /// <summary>
        /// End the current combat
        /// </summary>
        private void EndCombat(bool playerVictory)
        {
            if (!isInCombat)
            {
                Debug.LogWarning("Not in combat!");
                return;
            }

            Debug.Log($"Combat ended after {combatRound} rounds. Player victory: {playerVictory}");

            if (playerVictory)
            {
                HandleVictory();
            }

            OnCombatEnded?.Invoke(playerVictory);

            isInCombat = false;
            currentMonster = null;
            combatRound = 0;

            // Clear any pending reroll action
            RerollSystem.Instance?.ClearLastAction();

            // Return to exploring state if player survived
            if (Player.Instance != null && Player.Instance.IsAlive)
            {
                GameManager.Instance?.ChangeGameState(GameState.Exploring);
            }
        }
        #endregion

        #region Player Actions

        // State snapshot for reroll support
        private int savedPlayerHP;
        private int savedMonsterHP;
        private int savedCombatRound;

        /// <summary>
        /// Player attacks the monster
        /// </summary>
        public void PlayerAttack()
        {
            if (!isInCombat || currentMonster == null || Player.Instance == null)
            {
                Debug.LogWarning("Cannot attack - invalid combat state");
                return;
            }

            // Save state before attack for potential reroll
            savedPlayerHP = Player.Instance.CurrentHealth;
            savedMonsterHP = currentMonster.CurrentHP;
            savedCombatRound = combatRound;

            // Start new round
            combatRound++;
            currentMonster.TriggerOnRoundStart(combatRound);
            OnRoundStarted?.Invoke(combatRound);

            // Roll attack and process
            ExecuteAttackRoll();
        }

        /// <summary>
        /// Execute the attack roll (can be called again for reroll)
        /// </summary>
        private void ExecuteAttackRoll()
        {
            // Roll attack
            int attackRoll = Player.Instance.RollAttack();
            int requiredRoll = currentMonster.HitValue;

            // Record this roll for potential reroll
            if (RerollSystem.Instance != null && Player.Instance.HasFalseOmenReroll)
            {
                var action = new RerollableAction($"Attack vs {currentMonster.MonsterName}", attackRoll);

                // Capture current monster reference for closures
                var monster = currentMonster;

                // Set up undo action - restore HP values
                action.UndoAction = () => {
                    Player.Instance.SetHealth(savedPlayerHP);
                    monster.CurrentHP = savedMonsterHP;
                    combatRound = savedCombatRound;
                };

                // Set up reroll action
                action.RerollAction = () => {
                    combatRound++;
                    monster.TriggerOnRoundStart(combatRound);
                    OnRoundStarted?.Invoke(combatRound);
                    ExecuteAttackRoll();
                };

                RerollSystem.Instance.RecordAction(action);
            }

            if (attackRoll >= requiredRoll)
            {
                // HIT! Deal weapon damage to monster
                int damage = Player.Instance.GetWeaponDamage();

                // Add daemon damage if active
                if (Player.Instance.HasDaemon)
                {
                    int daemonDamage = Player.Instance.DaemonAttack();
                    damage += daemonDamage;
                    UIManager.Instance?.LogMessage($"<color=#AA66FF>Daemon deals {daemonDamage} additional damage!</color>");
                }

                OnAttackResult?.Invoke(attackRoll, requiredRoll, true, damage);

                bool monsterDied = currentMonster.TakeDamage(damage);
                OnMonsterDamaged?.Invoke(currentMonster, damage, currentMonster.CurrentHP);

                // Check if monster is dead
                if (monsterDied)
                {
                    EndCombat(true);
                    return;
                }
            }
            else
            {
                // MISS! Monster attacks back
                int monsterDamage = MonsterAttacks(attackRoll, requiredRoll);

                OnAttackResult?.Invoke(attackRoll, requiredRoll, false, monsterDamage);

                // Check if player is dead
                if (!Player.Instance.IsAlive)
                {
                    EndCombat(false);
                    return;
                }
            }

            // End round
            currentMonster.TriggerOnRoundEnd(combatRound);
            OnRoundEnded?.Invoke(combatRound);

            // Advance turn
            GameManager.Instance?.NextTurn();
        }

        /// <summary>
        /// Monster attacks the player. Returns damage dealt.
        /// </summary>
        private int MonsterAttacks(int playerRoll = 0, int requiredRoll = 0)
        {
            // Roll monster damage with current round (for effects like alternating damage)
            DiceRoll damageDice = currentMonster.GetCurrentDamageDice(combatRound);
            int monsterDamageRoll = damageDice.Roll();
            int finalDamage = CalculateDamageAfterArmor(monsterDamageRoll);

            Player.Instance.TakeDamage(finalDamage);

            // Trigger monster's damage dealt effects
            currentMonster.TriggerOnDamageDealt(finalDamage);

            return finalDamage;
        }

        /// <summary>
        /// Calculate damage after armor reduction (rolls d4)
        /// </summary>
        private int CalculateDamageAfterArmor(int baseDamage)
        {
            bool hasArmor = Inventory.Instance?.HasItem(ItemType.Armour) ?? false;

            if (hasArmor)
            {
                int armorReduction = UnityEngine.Random.Range(1, 5); // d4
                int finalDamage = Mathf.Max(0, baseDamage - armorReduction);
                return finalDamage;
            }

            return baseDamage;
        }

        /// <summary>
        /// Player flees from combat. Always succeeds but takes d4 damage.
        /// The monster is defeated (cannot be encountered again) but the room
        /// doesn't count towards level up progress.
        /// </summary>
        public void PlayerFlee()
        {
            if (!isInCombat)
            {
                Debug.LogWarning("Not in combat!");
                return;
            }

            // Take d4 damage for fleeing
            int fleeDamage = UnityEngine.Random.Range(1, 5); // d4
            Player.Instance?.TakeDamage(fleeDamage);

            UIManager.Instance?.ShowMessage($"You flee! Took {fleeDamage} damage escaping.", MessageType.Warning);

            // Check if player died from flee damage
            if (Player.Instance != null && !Player.Instance.IsAlive)
            {
                EndCombat(false);
                return;
            }

            // Room doesn't count towards level up when fleeing
            Player.Instance?.DecrementRoomsExplored();

            // End combat - fled successfully
            isInCombat = false;
            currentMonster = null;
            combatRound = 0;

            OnCombatEnded?.Invoke(false); // Not a victory
            OnPlayerFled?.Invoke(); // Signal that player fled

            GameManager.Instance?.ChangeGameState(GameState.Exploring);
            GameManager.Instance?.NextTurn();
        }
        #endregion

        #region Victory
        private void HandleVictory()
        {
            // Award XP first
            if (Player.Instance != null)
            {
                Player.Instance.GainXP(currentMonster.XPReward);
                Player.Instance.IncrementMonstersKilled();
                UIManager.Instance?.ShowMessage($"Victory! {currentMonster.MonsterName} defeated! +{currentMonster.XPReward} XP", MessageType.Success);

                // Decrement daemon fight counter
                Player.Instance.OnFightEnded();
            }

            // Then trigger death effects (loot drops, curses, level ups, etc.)
            currentMonster.TriggerOnDeath();
        }
        #endregion

        #region Public Helpers
        /// <summary>
        /// Get current monster HP percentage (0-1)
        /// </summary>
        public float GetMonsterHPPercent()
        {
            if (currentMonster == null || currentMonster.MaxHP <= 0)
                return 0;
            return (float)currentMonster.CurrentHP / currentMonster.MaxHP;
        }

        /// <summary>
        /// Get monster's current damage dice string for display
        /// </summary>
        public string GetMonsterDamageString()
        {
            if (currentMonster == null)
                return "";
            return currentMonster.GetCurrentDamageDice(combatRound + 1).ToString();
        }

        /// <summary>
        /// Deal direct damage to the current monster (from items/scrolls)
        /// </summary>
        public void DealDirectDamageToMonster(int damage)
        {
            if (!isInCombat || currentMonster == null) return;

            bool monsterDied = currentMonster.TakeDamage(damage);
            OnMonsterDamaged?.Invoke(currentMonster, damage, currentMonster.CurrentHP);
            UIManager.Instance?.LogMessage($"<color=#FFD700>Dealt {damage} direct damage! ({currentMonster.CurrentHP}/{currentMonster.MaxHP} HP)</color>");

            if (monsterDied)
            {
                EndCombat(true);
            }
        }
        #endregion

        #region Debug
        [ContextMenu("Start Weak Monster Combat")]
        private void DebugStartWeakCombat()
        {
            Monster monster = MonsterDatabase.RollWeakMonster();
            StartCombat(monster);
        }

        [ContextMenu("Start Tough Monster Combat")]
        private void DebugStartToughCombat()
        {
            Monster monster = MonsterDatabase.RollToughMonster();
            StartCombat(monster);
        }

        [ContextMenu("Player Attack")]
        private void DebugPlayerAttack()
        {
            PlayerAttack();
        }

        [ContextMenu("Player Flee")]
        private void DebugPlayerFlee()
        {
            PlayerFlee();
        }

        [ContextMenu("Print Combat State")]
        private void DebugPrintCombatState()
        {
            Debug.Log($"=== COMBAT STATE ===");
            Debug.Log($"In Combat: {isInCombat}");
            Debug.Log($"Combat Round: {combatRound}");
            if (currentMonster != null)
            {
                Debug.Log($"Monster: {currentMonster}");
                Debug.Log($"Monster HP: {currentMonster.CurrentHP}/{currentMonster.MaxHP}");
                Debug.Log($"Current Damage Dice: {currentMonster.GetCurrentDamageDice(combatRound + 1)}");
                Debug.Log($"Effects: {currentMonster.Effects.Count}");
            }
        }
        #endregion
    }
}
using UnityEngine;
using System.Collections.Generic;

namespace DarkFort.Combat
{
    #region Dice System
    /// <summary>
    /// Represents a dice roll like "2d6+3" or "1d4-1"
    /// </summary>
    [System.Serializable]
    public class DiceRoll
    {
        public int Count;      // Number of dice (e.g., 2 in "2d6")
        public int Sides;      // Sides per die (e.g., 6 in "2d6")
        public int Modifier;   // Flat modifier (e.g., +3 in "2d6+3")

        public DiceRoll(int count, int sides, int modifier = 0)
        {
            Count = count;
            Sides = sides;
            Modifier = modifier;
        }

        /// <summary>
        /// Roll the dice and return total
        /// </summary>
        public int Roll()
        {
            int total = 0;
            for (int i = 0; i < Count; i++)
            {
                total += UnityEngine.Random.Range(1, Sides + 1);
            }
            total += Modifier;
            return Mathf.Max(1, total); // Minimum 1 damage
        }

        /// <summary>
        /// Get minimum possible roll
        /// </summary>
        public int MinRoll => Mathf.Max(1, Count + Modifier);

        /// <summary>
        /// Get maximum possible roll
        /// </summary>
        public int MaxRoll => Count * Sides + Modifier;

        /// <summary>
        /// Get average roll
        /// </summary>
        public float AverageRoll => Count * ((Sides + 1f) / 2f) + Modifier;

        /// <summary>
        /// Get string representation (e.g., "2d6+3", "1d4", "1d6-1")
        /// </summary>
        public override string ToString()
        {
            string diceStr = $"{Count}d{Sides}";
            if (Modifier > 0)
                return $"{diceStr}+{Modifier}";
            else if (Modifier < 0)
                return $"{diceStr}{Modifier}";
            return diceStr;
        }

        /// <summary>
        /// Create a copy of this dice roll
        /// </summary>
        public DiceRoll Clone()
        {
            return new DiceRoll(Count, Sides, Modifier);
        }

        // Common dice presets
        public static DiceRoll D4 => new DiceRoll(1, 4);
        public static DiceRoll D6 => new DiceRoll(1, 6);
        public static DiceRoll D8 => new DiceRoll(1, 8);
        public static DiceRoll D10 => new DiceRoll(1, 10);
        public static DiceRoll D12 => new DiceRoll(1, 12);
        public static DiceRoll D20 => new DiceRoll(1, 20);
    }
    #endregion

    #region Monster Effects
    /// <summary>
    /// Base class for monster special effects
    /// </summary>
    public abstract class MonsterEffect
    {
        public string EffectName;
        public string Description;

        /// <summary>
        /// Called when combat starts
        /// </summary>
        public virtual void OnCombatStart(Monster monster) { }

        /// <summary>
        /// Called at the start of each combat round
        /// </summary>
        public virtual void OnRoundStart(Monster monster, int round) { }

        /// <summary>
        /// Allows modification of damage dice before rolling
        /// </summary>
        public virtual DiceRoll ModifyDamageDice(Monster monster, DiceRoll baseDice, int round)
        {
            return baseDice;
        }

        /// <summary>
        /// Called after monster deals damage
        /// </summary>
        public virtual void OnDamageDealt(Monster monster, int damage) { }

        /// <summary>
        /// Called after monster takes damage
        /// </summary>
        public virtual void OnDamageTaken(Monster monster, int damage) { }

        /// <summary>
        /// Called when monster dies
        /// </summary>
        public virtual void OnDeath(Monster monster) { }

        /// <summary>
        /// Called at end of each round
        /// </summary>
        public virtual void OnRoundEnd(Monster monster, int round) { }
    }

    /// <summary>
    /// Alternates damage dice between two types each turn
    /// </summary>
    public class AlternatingDamageEffect : MonsterEffect
    {
        private DiceRoll dice1;
        private DiceRoll dice2;

        public AlternatingDamageEffect(DiceRoll firstDice, DiceRoll secondDice)
        {
            EffectName = "Alternating Attack";
            Description = $"Alternates between {firstDice} and {secondDice} damage";
            dice1 = firstDice;
            dice2 = secondDice;
        }

        public override DiceRoll ModifyDamageDice(Monster monster, DiceRoll baseDice, int round)
        {
            // Odd rounds use dice1, even rounds use dice2
            return (round % 2 == 1) ? dice1.Clone() : dice2.Clone();
        }
    }

    /// <summary>
    /// Monster regenerates HP each round
    /// </summary>
    public class RegenerationEffect : MonsterEffect
    {
        private int regenAmount;

        public RegenerationEffect(int amount)
        {
            regenAmount = amount;
            EffectName = "Regeneration";
            Description = $"Regenerates {amount} HP each round";
        }

        public override void OnRoundEnd(Monster monster, int round)
        {
            if (monster.IsAlive && monster.CurrentHP < monster.MaxHP)
            {
                int healed = Mathf.Min(regenAmount, monster.MaxHP - monster.CurrentHP);
                monster.CurrentHP += healed;
                Debug.Log($"{monster.MonsterName} regenerates {healed} HP. HP: {monster.CurrentHP}/{monster.MaxHP}");
            }
        }
    }

    /// <summary>
    /// Applies poison to player on hit
    /// </summary>
    public class PoisonEffect : MonsterEffect
    {
        private int poisonDamage;

        public PoisonEffect(int damage = 1)
        {
            poisonDamage = damage;
            EffectName = "Poison";
            Description = $"Poisons for {damage} damage per round";
        }

        public override void OnDamageDealt(Monster monster, int damage)
        {
            // TODO: Apply poison status to player
            Debug.Log($"{monster.MonsterName}'s poison will deal {poisonDamage} damage per round!");
        }
    }

    /// <summary>
    /// Steals gold from player on hit
    /// </summary>
    public class StealGoldEffect : MonsterEffect
    {
        private DiceRoll stealDice;

        public StealGoldEffect(int diceCount = 1, int diceSides = 6)
        {
            stealDice = new DiceRoll(diceCount, diceSides);
            EffectName = "Steals Gold";
            Description = $"Steals {stealDice} gold on hit";
        }

        public override void OnDamageDealt(Monster monster, int damage)
        {
            int stolen = stealDice.Roll();
            if (Core.Inventory.Instance != null && Core.Inventory.Instance.Gold > 0)
            {
                int actualStolen = Mathf.Min(stolen, Core.Inventory.Instance.Gold);
                Core.Inventory.Instance.SpendGold(actualStolen);
                Debug.Log($"{monster.MonsterName} steals {actualStolen} gold!");
                UI.UIManager.Instance?.ShowMessage($"{monster.MonsterName} steals {actualStolen} gold!");
            }
        }
    }

    /// <summary>
    /// Life drain - heals monster for portion of damage dealt
    /// </summary>
    public class LifeDrainEffect : MonsterEffect
    {
        private float drainPercent;

        public LifeDrainEffect(float percent = 0.5f)
        {
            drainPercent = percent;
            EffectName = "Life Drain";
            Description = $"Heals for {percent * 100}% of damage dealt";
        }

        public override void OnDamageDealt(Monster monster, int damage)
        {
            int healed = Mathf.RoundToInt(damage * drainPercent);
            if (healed > 0 && monster.CurrentHP < monster.MaxHP)
            {
                monster.CurrentHP = Mathf.Min(monster.MaxHP, monster.CurrentHP + healed);
                Debug.Log($"{monster.MonsterName} drains {healed} life! HP: {monster.CurrentHP}/{monster.MaxHP}");
                UI.UIManager.Instance?.ShowMessage($"{monster.MonsterName} drains {healed} life!");
            }
        }
    }

    /// <summary>
    /// Explodes on death, dealing damage to player
    /// </summary>
    public class ExplodeOnDeathEffect : MonsterEffect
    {
        private DiceRoll explosionDice;

        public ExplodeOnDeathEffect(int diceCount = 1, int diceSides = 6, int modifier = 0)
        {
            explosionDice = new DiceRoll(diceCount, diceSides, modifier);
            EffectName = "Explosive";
            Description = $"Explodes for {explosionDice} damage on death";
        }

        public override void OnDeath(Monster monster)
        {
            int damage = explosionDice.Roll();
            Debug.Log($"{monster.MonsterName} explodes for {damage} damage!");
            UI.UIManager.Instance?.ShowMessage($"{monster.MonsterName} explodes! {damage} damage!");
            Core.Player.Instance?.TakeDamage(damage);
        }
    }

    /// <summary>
    /// Enrages when below certain HP, increasing damage
    /// </summary>
    public class EnrageEffect : MonsterEffect
    {
        private float hpThreshold;
        private int bonusDamage;
        private bool isEnraged = false;

        public EnrageEffect(float threshold = 0.5f, int bonus = 2)
        {
            hpThreshold = threshold;
            bonusDamage = bonus;
            EffectName = "Enrage";
            Description = $"+{bonus} damage when below {threshold * 100}% HP";
        }

        public override DiceRoll ModifyDamageDice(Monster monster, DiceRoll baseDice, int round)
        {
            float hpPercent = (float)monster.CurrentHP / monster.MaxHP;
            if (hpPercent <= hpThreshold)
            {
                if (!isEnraged)
                {
                    isEnraged = true;
                    Debug.Log($"{monster.MonsterName} becomes enraged!");
                    UI.UIManager.Instance?.ShowMessage($"{monster.MonsterName} becomes enraged!");
                }
                return new DiceRoll(baseDice.Count, baseDice.Sides, baseDice.Modifier + bonusDamage);
            }
            return baseDice;
        }
    }

    /// <summary>
    /// Chance to drop an item on death
    /// </summary>
    public class DropItemOnDeathEffect : MonsterEffect
    {
        public enum DropType { Weapon, Item }

        private DropType dropType;
        private System.Func<Core.Item> getItem;
        private System.Func<Core.Weapon> getWeapon;
        private int dropChanceDie;    // Die to roll (e.g., 6 for d6)
        private int dropThreshold;    // Drop if roll <= this (e.g., 2 means drop on 1-2)

        public DropItemOnDeathEffect(System.Func<Core.Item> itemGetter, int dieSides = 6, int threshold = 2)
        {
            dropType = DropType.Item;
            getItem = itemGetter;
            dropChanceDie = dieSides;
            dropThreshold = threshold;
            EffectName = "Drops Loot";
            Description = $"May drop item on death";
        }

        public DropItemOnDeathEffect(System.Func<Core.Weapon> weaponGetter, int dieSides = 6, int threshold = 2)
        {
            dropType = DropType.Weapon;
            getWeapon = weaponGetter;
            dropChanceDie = dieSides;
            dropThreshold = threshold;
            EffectName = "Drops Loot";
            Description = $"May drop weapon on death";
        }

        public override void OnDeath(Monster monster)
        {
            int roll = UnityEngine.Random.Range(1, dropChanceDie + 1);

            if (roll <= dropThreshold)
            {
                if (dropType == DropType.Item && getItem != null)
                {
                    var item = getItem();
                    if (Core.Inventory.Instance != null && item != null)
                    {
                        Core.Inventory.Instance.AddItem(item);
                        UI.UIManager.Instance?.ShowMessage($"Found {item.ItemName}!", UI.MessageType.Success);
                        Debug.Log($"{monster.MonsterName} dropped {item.ItemName} (rolled {roll})");
                    }
                }
                else if (dropType == DropType.Weapon && getWeapon != null)
                {
                    var weapon = getWeapon();
                    if (Core.Inventory.Instance != null && weapon != null)
                    {
                        Core.Inventory.Instance.AddWeapon(weapon);
                        UI.UIManager.Instance?.ShowMessage($"Found {weapon.WeaponName}!", UI.MessageType.Success);
                        Debug.Log($"{monster.MonsterName} dropped {weapon.WeaponName} (rolled {roll})");
                    }
                }
            }
            else
            {
                Debug.Log($"{monster.MonsterName} dropped nothing (rolled {roll}, needed <= {dropThreshold})");
            }
        }
    }

    /// <summary>
    /// Drops silver on death
    /// </summary>
    public class DropSilverOnDeathEffect : MonsterEffect
    {
        private DiceRoll silverDice;

        public DropSilverOnDeathEffect(int diceCount, int diceSides, int modifier = 0)
        {
            silverDice = new DiceRoll(diceCount, diceSides, modifier);
            EffectName = "Drops Silver";
            Description = $"Drops {silverDice} silver on death";
        }

        public override void OnDeath(Monster monster)
        {
            int silver = silverDice.Roll();
            Core.Inventory.Instance?.AddGold(silver, playSound: true);
            UI.UIManager.Instance?.ShowMessage($"Found {silver} silver!", UI.MessageType.Success);
            Debug.Log($"{monster.MonsterName} dropped {silver} silver");
        }
    }

    /// <summary>
    /// Drops silver calculated as dA x dB (e.g., d4 x d6)
    /// </summary>
    public class DropMultipliedSilverOnDeathEffect : MonsterEffect
    {
        private int diceASides;
        private int diceBSides;

        public DropMultipliedSilverOnDeathEffect(int sidesA, int sidesB)
        {
            diceASides = sidesA;
            diceBSides = sidesB;
            EffectName = "Drops Silver";
            Description = $"Drops d{sidesA}×d{sidesB} silver on death";
        }

        public override void OnDeath(Monster monster)
        {
            int rollA = UnityEngine.Random.Range(1, diceASides + 1);
            int rollB = UnityEngine.Random.Range(1, diceBSides + 1);
            int silver = rollA * rollB;
            Core.Inventory.Instance?.AddGold(silver, playSound: true);
            UI.UIManager.Instance?.ShowMessage($"Found {silver} silver! ({rollA}×{rollB})", UI.MessageType.Success);
            Debug.Log($"{monster.MonsterName} dropped {silver} silver ({rollA}×{rollB})");
        }
    }

    /// <summary>
    /// Chance to instantly kill the player on death (transformation/petrification)
    /// </summary>
    public class DeathCurseEffect : MonsterEffect
    {
        private int dieSides;
        private int deathThreshold;
        private string curseDescription;

        public DeathCurseEffect(int sides, int threshold, string description)
        {
            dieSides = sides;
            deathThreshold = threshold;
            curseDescription = description;
            EffectName = "Death Curse";
            Description = $"On death: {description} (1-{threshold} on d{sides})";
        }

        public override void OnDeath(Monster monster)
        {
            int roll = UnityEngine.Random.Range(1, dieSides + 1);

            if (roll <= deathThreshold)
            {
                UI.UIManager.Instance?.ShowMessage($"{curseDescription}! (rolled {roll})", UI.MessageType.Danger);
                Debug.Log($"Death curse triggered! {curseDescription} (rolled {roll})");

                // Kill the player
                Core.Player.Instance?.TakeDamage(9999);
            }
            else
            {
                Debug.Log($"Death curse avoided (rolled {roll}, needed <= {deathThreshold})");
            }
        }
    }

    /// <summary>
    /// Chance to instantly level up on death
    /// </summary>
    public class LevelUpOnDeathEffect : MonsterEffect
    {
        private int dieSides;
        private int levelUpThreshold;

        public LevelUpOnDeathEffect(int sides, int threshold)
        {
            dieSides = sides;
            levelUpThreshold = threshold;
            EffectName = "Level Up Chance";
            Description = $"May grant level up on death (1-{threshold} on d{sides})";
        }

        public override void OnDeath(Monster monster)
        {
            int roll = UnityEngine.Random.Range(1, dieSides + 1);

            if (roll <= levelUpThreshold)
            {
                UI.UIManager.Instance?.ShowMessage($"Ancient power flows through you! LEVEL UP! (rolled {roll})", UI.MessageType.Success);
                Debug.Log($"Level up triggered! (rolled {roll})");

                if (Core.Player.Instance != null)
                {
                    // Force level up (bypassing normal requirements)
                    Core.Player.Instance.ForceLevelUp();
                }
            }
            else
            {
                Debug.Log($"Level up chance missed (rolled {roll}, needed <= {levelUpThreshold})");
            }
        }
    }
    #endregion

    #region Monster Class
    /// <summary>
    /// Represents a monster encounter
    /// </summary>
    [System.Serializable]
    public class Monster
    {
        #region Properties
        public string MonsterName;
        public string Description;
        public MonsterDifficulty Difficulty;

        public int HitValue;           // Player must roll >= this to hit
        public int MaxHP;              // Monster's maximum hit points
        public int CurrentHP;          // Monster's current hit points

        public DiceRoll BaseDamage;    // Base damage dice

        public int XPReward;
        public string EffectDescription;  // Text description for display

        public Sprite MonsterSprite;   // Visual representation

        // Effects system
        private List<MonsterEffect> effects = new List<MonsterEffect>();

        public bool IsAlive => CurrentHP > 0;
        public IReadOnlyList<MonsterEffect> Effects => effects;
        #endregion

        #region Constructor
        public Monster(string name, MonsterDifficulty difficulty, int hit, int maxHP, DiceRoll damage, int xp, string effectDesc = "", string description = "")
        {
            MonsterName = name;
            Difficulty = difficulty;
            HitValue = hit;
            MaxHP = maxHP;
            CurrentHP = maxHP;
            BaseDamage = damage;
            XPReward = xp;
            EffectDescription = effectDesc;
            Description = description;
        }
        #endregion

        #region Effect Management
        public Monster AddEffect(MonsterEffect effect)
        {
            effects.Add(effect);
            return this;
        }

        public void TriggerOnCombatStart()
        {
            foreach (var effect in effects)
                effect.OnCombatStart(this);
        }

        public void TriggerOnRoundStart(int round)
        {
            foreach (var effect in effects)
                effect.OnRoundStart(this, round);
        }

        public void TriggerOnRoundEnd(int round)
        {
            foreach (var effect in effects)
                effect.OnRoundEnd(this, round);
        }

        public void TriggerOnDamageDealt(int damage)
        {
            foreach (var effect in effects)
                effect.OnDamageDealt(this, damage);
        }

        public void TriggerOnDamageTaken(int damage)
        {
            foreach (var effect in effects)
                effect.OnDamageTaken(this, damage);
        }

        public void TriggerOnDeath()
        {
            foreach (var effect in effects)
                effect.OnDeath(this);
        }
        #endregion

        #region Methods
        public Monster Clone()
        {
            var clone = new Monster(MonsterName, Difficulty, HitValue, MaxHP, BaseDamage.Clone(), XPReward, EffectDescription, Description)
            {
                CurrentHP = this.MaxHP,
                MonsterSprite = this.MonsterSprite
            };

            // Clone effects
            foreach (var effect in effects)
            {
                clone.effects.Add(effect); // Effects are typically stateless, but could deep clone if needed
            }

            return clone;
        }

        /// <summary>
        /// Roll damage for this monster, applying any effect modifications
        /// </summary>
        public int RollDamage(int round)
        {
            DiceRoll currentDice = BaseDamage.Clone();

            // Let effects modify the dice
            foreach (var effect in effects)
            {
                currentDice = effect.ModifyDamageDice(this, currentDice, round);
            }

            int damage = currentDice.Roll();

            // Check if player has halved this monster's damage
            if (Core.Player.Instance != null && Core.Player.Instance.IsMonsterDamageHalved(MonsterName))
            {
                damage = Mathf.Max(1, damage / 2);
                Debug.Log($"{MonsterName} damage halved: {damage}");
            }

            return damage;
        }

        /// <summary>
        /// Get current damage dice (with effect modifications for display)
        /// </summary>
        public DiceRoll GetCurrentDamageDice(int round)
        {
            DiceRoll currentDice = BaseDamage.Clone();
            foreach (var effect in effects)
            {
                currentDice = effect.ModifyDamageDice(this, currentDice, round);
            }
            return currentDice;
        }

        /// <summary>
        /// Take damage, returns true if monster dies
        /// </summary>
        /// <summary>
        /// Take damage and return true if the monster dies
        /// Note: OnDeath effects should be triggered separately by CombatManager after victory handling
        /// </summary>
        public bool TakeDamage(int damage)
        {
            CurrentHP -= damage;
            if (CurrentHP < 0) CurrentHP = 0;

            TriggerOnDamageTaken(damage);

            return !IsAlive;
        }

        public override string ToString()
        {
            string effectStr = !string.IsNullOrEmpty(EffectDescription) ? $" | Effect: {EffectDescription}" : "";
            return $"{MonsterName} ({Difficulty}) | Hit: {HitValue} | HP: {CurrentHP}/{MaxHP} | DMG: {BaseDamage} | XP: {XPReward}{effectStr}";
        }
        #endregion
    }
    #endregion

    #region Enums
    public enum MonsterDifficulty
    {
        Weak,
        Tough
    }
    #endregion

    #region Monster Database
    /// <summary>
    /// Static database of monster tables
    /// </summary>
    public static class MonsterDatabase
    {
        #region Weak Monsters (d4 table)
        public static Monster BloodDrenchedSkeleton => new Monster(
            name: "Blood-Drenched Skeleton",
            difficulty: MonsterDifficulty.Weak,
            hit: 3,
            maxHP: 6,
            damage: new DiceRoll(1, 4),  // d4
            xp: 3,
            effectDesc: "May drop Dagger",
            description: "A skeleton soaked in ancient blood, clutching a rusty blade."
        ).AddEffect(new DropItemOnDeathEffect(() => Core.WeaponDatabase.Dagger.Clone(), 6, 2));

        public static Monster CatacombCultist => new Monster(
            name: "Catacomb Cultist",
            difficulty: MonsterDifficulty.Weak,
            hit: 3,
            maxHP: 6,
            damage: new DiceRoll(1, 4),  // d4
            xp: 3,
            effectDesc: "May drop Scroll",
            description: "A robed figure muttering forbidden prayers."
        ).AddEffect(new DropItemOnDeathEffect(() => Core.ItemDatabase.ResolveRandomScroll(), 6, 2));

        public static Monster Goblin => new Monster(
            name: "Goblin",
            difficulty: MonsterDifficulty.Weak,
            hit: 3,
            maxHP: 5,
            damage: new DiceRoll(1, 4),  // d4
            xp: 3,
            effectDesc: "May drop Rope",
            description: "A small, vicious creature with sharp teeth."
        ).AddEffect(new DropItemOnDeathEffect(() => Core.ItemDatabase.Rope.Clone(), 6, 2));

        public static Monster UndeadHound => new Monster(
            name: "Undead Hound",
            difficulty: MonsterDifficulty.Weak,
            hit: 4,
            maxHP: 6,
            damage: new DiceRoll(1, 4),  // d4
            xp: 4,
            description: "A skeletal dog with glowing red eyes."
        );
        #endregion

        #region Tough Monsters (d4 table)
        public static Monster NecroSorcerer => new Monster(
            name: "Necro-Sorcerer",
            difficulty: MonsterDifficulty.Tough,
            hit: 4,
            maxHP: 8,
            damage: new DiceRoll(1, 4),  // Base d4, alternates with d6
            xp: 4,
            effectDesc: "Alternating damage, drops silver, death curse",
            description: "A dark mage wreathed in necrotic energy."
        ).AddEffect(new AlternatingDamageEffect(
            new DiceRoll(1, 4),  // First hit: d4
            new DiceRoll(1, 6)   // Second hit: d6
        )).AddEffect(new DropSilverOnDeathEffect(3, 6))  // 3d6 silver
          .AddEffect(new DeathCurseEffect(6, 1, "You are transformed into a maggot"));

        public static Monster SmallStoneTroll => new Monster(
            name: "Small Stone Troll",
            difficulty: MonsterDifficulty.Tough,
            hit: 5,
            maxHP: 9,
            damage: new DiceRoll(1, 6, 1),  // d6+1
            xp: 7,
            description: "A squat troll with rocky grey skin."
        );

        public static Monster Medusa => new Monster(
            name: "Medusa",
            difficulty: MonsterDifficulty.Tough,
            hit: 4,
            maxHP: 10,
            damage: new DiceRoll(1, 6),  // d6
            xp: 4,
            effectDesc: "Drops silver, petrification curse",
            description: "A serpent-haired horror with a deadly gaze."
        ).AddEffect(new DropMultipliedSilverOnDeathEffect(4, 6))  // d4 x d6 silver
          .AddEffect(new DeathCurseEffect(6, 1, "You are petrified"));

        public static Monster RuinBasilisk => new Monster(
            name: "Ruin Basilisk",
            difficulty: MonsterDifficulty.Tough,
            hit: 4,
            maxHP: 11,
            damage: new DiceRoll(1, 6),  // d6
            xp: 4,
            effectDesc: "May grant level up on death",
            description: "An ancient serpent whose death releases powerful magic."
        ).AddEffect(new LevelUpOnDeathEffect(6, 2));  // Level up on 1-2
        #endregion

        #region Monster Table Rolling
        public static Monster RollWeakMonster()
        {
            int roll = UnityEngine.Random.Range(1, 5); // d4
            Monster template;

            switch (roll)
            {
                case 1: template = BloodDrenchedSkeleton; break;
                case 2: template = CatacombCultist; break;
                case 3: template = Goblin; break;
                case 4: template = UndeadHound; break;
                default: template = Goblin; break;
            }

            Debug.Log($"Rolled weak monster (d4={roll}): {template.MonsterName}");
            return template.Clone();
        }

        public static Monster RollToughMonster()
        {
            int roll = UnityEngine.Random.Range(1, 5); // d4
            Monster template;

            switch (roll)
            {
                case 1: template = NecroSorcerer; break;
                case 2: template = SmallStoneTroll; break;
                case 3: template = Medusa; break;
                case 4: template = RuinBasilisk; break;
                default: template = NecroSorcerer; break;
            }

            Debug.Log($"Rolled tough monster (d4={roll}): {template.MonsterName}");
            return template.Clone();
        }

        public static Monster GetMonsterByDifficulty(MonsterDifficulty difficulty)
        {
            return difficulty == MonsterDifficulty.Weak ? RollWeakMonster() : RollToughMonster();
        }
        #endregion

        #region Helper Methods
        public static Monster[] GetAllWeakMonsters()
        {
            return new Monster[] { BloodDrenchedSkeleton, CatacombCultist, Goblin, UndeadHound };
        }

        public static Monster[] GetAllToughMonsters()
        {
            return new Monster[] { NecroSorcerer, SmallStoneTroll, Medusa, RuinBasilisk };
        }
        #endregion
    }
    #endregion
}
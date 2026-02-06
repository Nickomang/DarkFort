using UnityEngine;
using DarkFort.Combat;

namespace DarkFort.Core
{
    /// <summary>
    /// Represents a weapon with damage dice and hit bonus
    /// </summary>
    [System.Serializable]
    public class Weapon
    {
        public string WeaponName;
        public DiceRoll DamageDice;
        public int HitBonus;
        public string Description;
        public int BuyPrice;
        public int SellPrice;

        public Weapon(string name, DiceRoll damageDice, int hitBonus = 0, string description = "", int buyPrice = 0, int sellPrice = 0)
        {
            WeaponName = name;
            DamageDice = damageDice;
            HitBonus = hitBonus;
            Description = description;
            // Auto-calculate prices based on average damage if not specified
            int avgDamage = Mathf.RoundToInt(damageDice.AverageRoll);
            BuyPrice = buyPrice > 0 ? buyPrice : (avgDamage * 5) + (hitBonus * 3);
            SellPrice = sellPrice > 0 ? sellPrice : Mathf.Max(1, BuyPrice / 2);
        }

        // Legacy constructor for flat damage (converts to d1 * damage)
        public Weapon(string name, int damage, int hitBonus = 0, string description = "", int buyPrice = 0, int sellPrice = 0)
            : this(name, new DiceRoll(damage, 1, 0), hitBonus, description, buyPrice, sellPrice)
        {
        }

        public Weapon Clone()
        {
            return new Weapon(WeaponName, DamageDice.Clone(), HitBonus, Description, BuyPrice, SellPrice);
        }

        /// <summary>
        /// Roll damage for this weapon
        /// </summary>
        public int RollDamage()
        {
            return DamageDice.Roll();
        }

        /// <summary>
        /// Get damage as display string (e.g., "d6", "d6+1")
        /// </summary>
        public string DamageString => DamageDice.ToString();

        public override string ToString()
        {
            string bonus = HitBonus > 0 ? $", +{HitBonus} to hit" : "";
            return $"{WeaponName} ({DamageString} damage{bonus})";
        }
    }

    /// <summary>
    /// Database of all weapons in the game
    /// </summary>
    public static class WeaponDatabase
    {
        #region Starting Weapons (d4 roll)
        public static Weapon Warhammer => new Weapon(
            "Warhammer",
            new DiceRoll(1, 6, 0), // d6
            hitBonus: 0,
            description: "A heavy hammer that crushes with brutal force.",
            buyPrice: 15,
            sellPrice: 6
        );

        public static Weapon Dagger => new Weapon(
            "Dagger",
            new DiceRoll(1, 4, 0), // d4
            hitBonus: 1,
            description: "A quick blade, easy to strike true.",
            buyPrice: 8,
            sellPrice: 3
        );

        public static Weapon Sword => new Weapon(
            "Sword",
            new DiceRoll(1, 6, 0), // d6
            hitBonus: 1,
            description: "A balanced blade with good reach.",
            buyPrice: 20,
            sellPrice: 8
        );

        public static Weapon Flail => new Weapon(
            "Flail",
            new DiceRoll(1, 6, 1), // d6+1
            hitBonus: 0,
            description: "A spiked ball on a chain, devastating but unwieldy.",
            buyPrice: 18,
            sellPrice: 7
        );
        #endregion

        #region Additional Weapons (found in dungeon)
        public static Weapon Shortsword => new Weapon(
            "Shortsword",
            new DiceRoll(1, 6, 0), // d6
            hitBonus: 0,
            description: "A simple blade for close combat.",
            buyPrice: 12,
            sellPrice: 5
        );

        public static Weapon Longsword => new Weapon(
            "Longsword",
            new DiceRoll(1, 8, 0), // d8
            hitBonus: 0,
            description: "A reliable sword of good length.",
            buyPrice: 25,
            sellPrice: 10
        );

        public static Weapon BattleAxe => new Weapon(
            "Battle Axe",
            new DiceRoll(1, 8, 0), // d8
            hitBonus: 0,
            description: "A heavy axe that cleaves with force.",
            buyPrice: 25,
            sellPrice: 10
        );

        public static Weapon Spear => new Weapon(
            "Spear",
            new DiceRoll(1, 6, 0), // d6
            hitBonus: 1,
            description: "A long spear with excellent reach.",
            buyPrice: 18,
            sellPrice: 7
        );

        public static Weapon Greatsword => new Weapon(
            "Greatsword",
            new DiceRoll(2, 6, 0), // 2d6
            hitBonus: 0,
            description: "A massive two-handed blade.",
            buyPrice: 35,
            sellPrice: 14
        );

        public static Weapon MagicSword => new Weapon(
            "Magic Sword",
            new DiceRoll(1, 8, 0), // d8
            hitBonus: 2,
            description: "An enchanted blade that strikes true.",
            buyPrice: 50,
            sellPrice: 20
        );

        public static Weapon FlamingSword => new Weapon(
            "Flaming Sword",
            new DiceRoll(1, 8, 2), // d8+2
            hitBonus: 1,
            description: "A blade wreathed in eternal flames.",
            buyPrice: 60,
            sellPrice: 24
        );
        #endregion

        #region Weapon Selection Methods
        /// <summary>
        /// Get starting weapon based on d4 roll (1-4)
        /// </summary>
        public static Weapon GetStartingWeaponByRoll(int roll)
        {
            switch (roll)
            {
                case 1: return Warhammer.Clone();
                case 2: return Dagger.Clone();
                case 3: return Sword.Clone();
                case 4: return Flail.Clone();
                default: return Sword.Clone();
            }
        }

        /// <summary>
        /// Roll d4 and get a random starting weapon
        /// </summary>
        public static Weapon RollStartingWeapon()
        {
            int roll = UnityEngine.Random.Range(1, 5); // d4
            Weapon weapon = GetStartingWeaponByRoll(roll);
            Debug.Log($"Starting weapon (d4={roll}): {weapon.WeaponName}");
            return weapon;
        }

        /// <summary>
        /// Get a random weapon that can be found in the dungeon
        /// </summary>
        public static Weapon GetRandomDungeonWeapon()
        {
            int roll = UnityEngine.Random.Range(0, 8);

            switch (roll)
            {
                case 0: return Dagger.Clone();
                case 1: return Shortsword.Clone();
                case 2: return Sword.Clone();
                case 3: return Longsword.Clone();
                case 4: return BattleAxe.Clone();
                case 5: return Spear.Clone();
                case 6: return Warhammer.Clone();
                case 7: return Flail.Clone();
                default: return Sword.Clone();
            }
        }

        /// <summary>
        /// Get a powerful weapon (for special rewards)
        /// </summary>
        public static Weapon GetPowerfulWeapon()
        {
            int roll = UnityEngine.Random.Range(0, 3);
            switch (roll)
            {
                case 0: return Greatsword.Clone();
                case 1: return MagicSword.Clone();
                case 2: return FlamingSword.Clone();
                default: return Greatsword.Clone();
            }
        }

        /// <summary>
        /// Get weapons available for merchant sale
        /// </summary>
        public static Weapon[] GetMerchantWeapons()
        {
            return new Weapon[]
            {
                Dagger, Shortsword, Sword, Longsword, Spear, Warhammer, Flail
            };
        }
        #endregion
    }
}
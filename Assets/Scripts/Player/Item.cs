using UnityEngine;

namespace DarkFort.Core
{
    /// <summary>
    /// Represents an item in Dark Fort
    /// </summary>
    [System.Serializable]
    public class Item
    {
        public string ItemName;
        public string Description;
        public ItemType Type;
        public int UsesRemaining; // For items with limited uses (-1 = unlimited)
        public int BuyPrice;      // Cost to purchase from merchant
        public int SellPrice;     // Gold received when selling
        public int StackCount;    // Number of items in this stack (1 = single item)

        public Item(string name, ItemType type, string description = "", int uses = -1, int buyPrice = 5, int sellPrice = 2)
        {
            ItemName = name;
            Type = type;
            Description = description;
            UsesRemaining = uses;
            BuyPrice = buyPrice;
            SellPrice = sellPrice;
            StackCount = 1;
        }

        public Item Clone()
        {
            return new Item(ItemName, Type, Description, UsesRemaining, BuyPrice, SellPrice)
            {
                StackCount = 1 // Cloned items start with stack of 1
            };
        }

        public bool HasUsesRemaining()
        {
            return UsesRemaining == -1 || UsesRemaining > 0;
        }

        public void UseCharge()
        {
            if (UsesRemaining > 0)
            {
                UsesRemaining--;
            }
        }

        /// <summary>
        /// Check if this item can stack with another item
        /// </summary>
        public bool CanStackWith(Item other)
        {
            if (other == null) return false;

            // Must be same type and name
            if (Type != other.Type || ItemName != other.ItemName) return false;

            // Items with unlimited uses (-1) always stack with same type
            if (UsesRemaining == -1 && other.UsesRemaining == -1) return true;

            // Single-use items (uses = 1) stack together
            if (UsesRemaining == 1 && other.UsesRemaining == 1) return true;

            // Multi-use items only stack if they have same uses remaining
            if (UsesRemaining > 1 && UsesRemaining == other.UsesRemaining) return true;

            // Don't stack items with different use counts (e.g., a 3-use scroll with a 2-use scroll)
            return false;
        }

        /// <summary>
        /// Add to this stack, returns true if successful
        /// </summary>
        public bool AddToStack(int count = 1)
        {
            StackCount += count;
            return true;
        }

        /// <summary>
        /// Remove from stack, returns true if stack still has items
        /// </summary>
        public bool RemoveFromStack(int count = 1)
        {
            StackCount -= count;
            return StackCount > 0;
        }

        public override string ToString()
        {
            string stackStr = StackCount > 1 ? $" x{StackCount}" : "";
            string uses = UsesRemaining > 0 ? $" ({UsesRemaining} uses)" : "";
            return $"{ItemName}{stackStr}{uses}";
        }
    }

    #region Enums
    public enum ItemType
    {
        Potion,              // Heal d6 HP
        Rope,                // +1 on pit trap roll
        RandomScroll,        // Roll d4 for scroll type
        Armour,              // Reduce damage by d4
        CloakOfInvisibility, // Avoid d4 fights, gain XP

        // Scrolls
        SummonWeakDaemon,    // Daemon helps for d4 fights, deals d4 damage
        PalmsOpenSouthernGate, // d6+1 damage, d4 uses
        AegisOfSorrow,       // Prevent next damage up to 4, d4 uses
        FalseOmen            // Choose next room encounter OR reroll any die
    }
    #endregion

    #region Item Database
    public static class ItemDatabase
    {
        #region Basic Items
        public static Item Potion => new Item(
            "Potion",
            ItemType.Potion,
            "Heals d6 HP when consumed.",
            uses: 1,
            buyPrice: 5,
            sellPrice: 2
        );

        public static Item Rope => new Item(
            "Rope",
            ItemType.Rope,
            "Grants +1 on pit trap encounter rolls.",
            uses: -1,
            buyPrice: 8,
            sellPrice: 3
        );

        public static Item RandomScroll => new Item(
            "Random Scroll",
            ItemType.RandomScroll,
            "Roll d4 to determine which scroll this is.",
            uses: 1,
            buyPrice: 10,
            sellPrice: 4
        );

        public static Item Armour => new Item(
            "Armour",
            ItemType.Armour,
            "Reduces incoming damage by d4.",
            uses: -1,
            buyPrice: 15,
            sellPrice: 6
        );

        public static Item CloakOfInvisibility => new Item(
            "Cloak of Invisibility",
            ItemType.CloakOfInvisibility,
            "Avoid d4 fights while acquiring all monster XP.",
            uses: 1,
            buyPrice: 20,
            sellPrice: 8
        );
        #endregion

        #region Scrolls
        public static Item SummonWeakDaemon => new Item(
            "Summon Weak Daemon",
            ItemType.SummonWeakDaemon,
            "Daemon helps for the next d4 fights, dealing d4 damage.",
            uses: 1,
            buyPrice: 12,
            sellPrice: 5
        );

        public static Item PalmsOpenSouthernGate => new Item(
            "Palms Open the Southern Gate",
            ItemType.PalmsOpenSouthernGate,
            "Deals d6+1 damage in combat.",
            uses: UnityEngine.Random.Range(1, 5),
            buyPrice: 15,
            sellPrice: 6
        );

        public static Item AegisOfSorrow => new Item(
            "Aegis of Sorrow",
            ItemType.AegisOfSorrow,
            "Prevent the next damage instance of up to 4.",
            uses: UnityEngine.Random.Range(1, 5),
            buyPrice: 15,
            sellPrice: 6
        );

        public static Item FalseOmen => new Item(
            "False Omen",
            ItemType.FalseOmen,
            "Choose the next room encounter type OR reroll any die.",
            uses: 1,
            buyPrice: 18,
            sellPrice: 7
        );
        #endregion

        #region Helper Methods
        /// <summary>
        /// Get starting item based on d4 roll (1-4)
        /// Starting items: Armour, Potion, Scroll of Summon Weak Daemon, Cloak of Invisibility
        /// </summary>
        public static Item GetStartingItemByRoll(int roll)
        {
            switch (roll)
            {
                case 1: return Armour.Clone();
                case 2: return Potion.Clone();
                case 3: return SummonWeakDaemon.Clone();
                case 4: return CloakOfInvisibility.Clone();
                default: return Potion.Clone();
            }
        }

        /// <summary>
        /// Roll d4 and get a random starting item
        /// </summary>
        public static Item RollStartingItem()
        {
            int roll = UnityEngine.Random.Range(1, 5); // d4
            Item item = GetStartingItemByRoll(roll);
            Debug.Log($"Starting item (d4={roll}): {item.ItemName}");
            return item;
        }

        /// <summary>
        /// Roll d4 to determine which scroll the Random Scroll becomes
        /// </summary>
        public static Item ResolveRandomScroll()
        {
            int roll = UnityEngine.Random.Range(1, 5); // d4
            Item scroll;

            switch (roll)
            {
                case 1: scroll = SummonWeakDaemon.Clone(); break;
                case 2: scroll = PalmsOpenSouthernGate.Clone(); break;
                case 3: scroll = AegisOfSorrow.Clone(); break;
                case 4: scroll = FalseOmen.Clone(); break;
                default: scroll = SummonWeakDaemon.Clone(); break;
            }

            Debug.Log($"Random Scroll (d4={roll}): {scroll.ItemName}");
            return scroll;
        }

        /// <summary>
        /// Get a random basic item (no scrolls)
        /// </summary>
        public static Item GetRandomBasicItem()
        {
            int roll = UnityEngine.Random.Range(0, 5);

            switch (roll)
            {
                case 0: return Potion.Clone();
                case 1: return Rope.Clone();
                case 2: return RandomScroll.Clone();
                case 3: return Armour.Clone();
                case 4: return CloakOfInvisibility.Clone();
                default: return Potion.Clone();
            }
        }

        /// <summary>
        /// Get a random treasure item (better distribution)
        /// </summary>
        public static Item GetTreasureItem()
        {
            float roll = UnityEngine.Random.value;

            if (roll < 0.3f) return Potion.Clone();
            else if (roll < 0.5f) return Rope.Clone();
            else if (roll < 0.7f) return RandomScroll.Clone();
            else if (roll < 0.85f) return Armour.Clone();
            else return CloakOfInvisibility.Clone();
        }
        #endregion
    }
    #endregion
}
using UnityEngine;
using System.Collections.Generic;

namespace DarkFort.Combat
{
    /// <summary>
    /// ScriptableObject database for monster sprites.
    /// Create an instance via Assets > Create > Dark Fort > Monster Sprite Database
    /// and assign sprites for each monster in the inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "MonsterSpriteDatabase", menuName = "Dark Fort/Monster Sprite Database")]
    public class MonsterSpriteDatabase : ScriptableObject
    {
        [System.Serializable]
        public class MonsterSpriteEntry
        {
            public string MonsterName;
            public Sprite Sprite;
        }

        [Header("Weak Monsters")]
        public Sprite BloodDrenchedSkeleton;
        public Sprite CatacombCultist;
        public Sprite Goblin;
        public Sprite UndeadHound;

        [Header("Tough Monsters")]
        public Sprite NecroSorcerer;
        public Sprite SmallStoneTroll;
        public Sprite Medusa;
        public Sprite RuinBasilisk;

        [Header("Custom Monsters (by name)")]
        [Tooltip("For any additional monsters not listed above")]
        public List<MonsterSpriteEntry> CustomMonsters = new List<MonsterSpriteEntry>();

        // Singleton instance
        private static MonsterSpriteDatabase _instance;
        public static MonsterSpriteDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<MonsterSpriteDatabase>("MonsterSpriteDatabase");
                    if (_instance == null)
                    {
                        Debug.LogWarning("MonsterSpriteDatabase not found in Resources folder!");
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Get the sprite for a monster by name
        /// </summary>
        public Sprite GetSprite(string monsterName)
        {
            // Check built-in monsters first
            switch (monsterName)
            {
                case "Blood-Drenched Skeleton": return BloodDrenchedSkeleton;
                case "Catacomb Cultist": return CatacombCultist;
                case "Goblin": return Goblin;
                case "Undead Hound": return UndeadHound;
                case "Necro-Sorcerer": return NecroSorcerer;
                case "Small Stone Troll": return SmallStoneTroll;
                case "Medusa": return Medusa;
                case "Ruin Basilisk": return RuinBasilisk;
            }

            // Check custom monsters
            foreach (var entry in CustomMonsters)
            {
                if (entry.MonsterName == monsterName)
                    return entry.Sprite;
            }

            Debug.LogWarning($"No sprite found for monster: {monsterName}");
            return null;
        }

        /// <summary>
        /// Apply sprite to a monster instance
        /// </summary>
        public void ApplySprite(Monster monster)
        {
            if (monster != null)
            {
                monster.MonsterSprite = GetSprite(monster.MonsterName);
            }
        }
    }
}
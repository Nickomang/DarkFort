using UnityEngine;
using DarkFort.Audio;
using DarkFort.Combat;
using DarkFort.Core;
using DarkFort.UI;

namespace DarkFort.Dungeon
{
    /// <summary>
    /// Types of encounters when entering a room
    /// </summary>
    public enum EncounterType
    {
        // Normal room encounters (d6)
        Empty = 1,              // Nothing happens
        PitTrap = 2,            // Take damage
        RiddlingSoothsayer = 3, // Special event
        WeakMonster = 4,        // Combat with weak monster
        ToughMonster = 5,       // Combat with tough monster
        Merchant = 6,           // Shop/trading

        // Start room encounters (d4) - values 10+ to distinguish
        StartItem = 10,         // Find a random item
        StartWeakMonster = 11,  // Weak monster encounter
        StartScroll = 12,       // Random scroll
        StartEmpty = 13         // Empty room
    }

    /// <summary>
    /// Handles rolling for and resolving room encounters
    /// </summary>
    public static class RoomEncounterSystem
    {
        #region Events
        public delegate void EncounterRolledHandler(int roll, EncounterType encounter);
        public static event EncounterRolledHandler OnEncounterRolled;

        public delegate void PitTrapHandler(int damage);
        public static event PitTrapHandler OnPitTrap;

        public delegate void RiddlingSoothsayerHandler();
        public static event RiddlingSoothsayerHandler OnRiddlingSoothsayer;

        public delegate void MerchantHandler();
        public static event MerchantHandler OnMerchant;

        public delegate void StartItemHandler();
        public static event StartItemHandler OnStartItem;

        public delegate void StartScrollHandler();
        public static event StartScrollHandler OnStartScroll;
        #endregion

        /// <summary>
        /// Roll d6 to determine encounter type for normal rooms
        /// If False Omen room choice is active, player chooses instead
        /// </summary>
        public static EncounterType RollForEncounter()
        {
            // Check if False Omen room choice is active
            if (Player.Instance != null && Player.Instance.HasFalseOmenRoomChoice)
            {
                // Player chooses - return a placeholder, UI will handle the actual choice
                Debug.Log("False Omen: Player will choose room encounter!");
                return EncounterType.Empty; // Will be overridden by UI choice
            }

            int roll = Random.Range(1, 7); // d6
            EncounterType encounter = (EncounterType)roll;

            Debug.Log($"Encounter roll: d6={roll} = {encounter}");
            OnEncounterRolled?.Invoke(roll, encounter);

            return encounter;
        }

        /// <summary>
        /// Check if False Omen room choice should be triggered
        /// </summary>
        public static bool ShouldShowRoomChoice()
        {
            return Player.Instance != null && Player.Instance.HasFalseOmenRoomChoice;
        }

        /// <summary>
        /// Get a chosen encounter type (consumes False Omen)
        /// </summary>
        public static EncounterType GetChosenEncounter(int choice)
        {
            // Consume the False Omen
            Player.Instance?.UseFalseOmenRoomChoice();

            EncounterType encounter = (EncounterType)choice;
            Debug.Log($"False Omen: Player chose {encounter}");
            OnEncounterRolled?.Invoke(choice, encounter);

            return encounter;
        }

        /// <summary>
        /// Roll d4 to determine encounter type for start room
        /// 1 = Random Item, 2 = Weak Monster, 3 = Random Scroll, 4 = Empty
        /// </summary>
        public static EncounterType RollForStartRoomEncounter()
        {
            int roll = Random.Range(1, 5); // d4
            EncounterType encounter = roll switch
            {
                1 => EncounterType.StartItem,
                2 => EncounterType.StartWeakMonster,
                3 => EncounterType.StartScroll,
                4 => EncounterType.StartEmpty,
                _ => EncounterType.StartEmpty
            };

            Debug.Log($"Start room encounter roll: d4={roll} = {encounter}");
            OnEncounterRolled?.Invoke(roll, encounter);

            return encounter;
        }

        /// <summary>
        /// Resolve the encounter that was rolled
        /// </summary>
        public static void ResolveEncounter(EncounterType encounter)
        {
            switch (encounter)
            {
                case EncounterType.Empty:
                case EncounterType.StartEmpty:
                    ResolveEmpty();
                    break;

                case EncounterType.PitTrap:
                    ResolvePitTrap();
                    break;

                case EncounterType.RiddlingSoothsayer:
                    ResolveRiddlingSoothsayer();
                    break;

                case EncounterType.WeakMonster:
                case EncounterType.StartWeakMonster:
                    ResolveWeakMonster();
                    break;

                case EncounterType.ToughMonster:
                    ResolveToughMonster();
                    break;

                case EncounterType.Merchant:
                    ResolveMerchant();
                    break;
                    
                case EncounterType.StartItem:
                    ResolveStartItem();
                    break;
                    
                case EncounterType.StartScroll:
                    ResolveStartScroll();
                    break;
            }
        }

        #region Encounter Resolution
        private static void ResolveEmpty()
        {
            Debug.Log("Empty room - nothing happens.");
            UIManager.Instance?.ShowMessage("The room is empty.");
        }

        private static void ResolvePitTrap()
        {
            // Check if player has rope (gives +1 to roll)
            bool hasRope = Inventory.Instance?.HasItem(ItemType.Rope) ?? false;
            int ropeBonus = hasRope ? 1 : 0;

            // Roll d6 for pit trap damage (with rope bonus)
            int roll = Random.Range(1, 7); // d6
            int adjustedRoll = roll + ropeBonus;

            // Damage is based on roll (typically 1-3, rope can reduce it)
            int damage = Mathf.Max(0, 4 - adjustedRoll); // Higher roll = less damage

            if (hasRope)
            {
                Debug.Log($"Pit trap! Rolled {roll}+1 (rope) = {adjustedRoll}. Taking {damage} damage.");
                UIManager.Instance?.ShowMessage($"Pit trap! Rope helps! {damage} damage!");
            }
            else
            {
                Debug.Log($"Pit trap! Rolled {roll}. Taking {damage} damage.");
                UIManager.Instance?.ShowMessage($"You fall into a pit trap! {damage} damage!");
            }

            if (damage > 0)
            {
                Player.Instance?.TakeDamage(damage);
            }
            else
            {
                UIManager.Instance?.ShowMessage("You avoid the pit trap!");
            }

            OnPitTrap?.Invoke(damage);
        }

        private static void ResolveRiddlingSoothsayer()
        {
            UIManager.Instance?.ShowMessage("A mysterious soothsayer appears and poses a riddle...");
            OnRiddlingSoothsayer?.Invoke();

            // Roll to determine success (odd = success, even = failure)
            bool solvedRiddle = (UnityEngine.Random.Range(0, 2) == 1);

            if (solvedRiddle)
            {
                // Success! Player chooses reward
                UIManager.Instance?.ShowChoicePanel(
                    "Riddle Solved!",
                    "The soothsayer nods approvingly.\n\"Your wisdom serves you well. Choose your reward...\"",
                    new string[] { "Take 10 Silver", "Take 3 XP" },
                    OnSoothsayerRewardChosen
                );
            }
            else
            {
                // Failure - take d4 damage (ignores armor)
                int damage = UnityEngine.Random.Range(1, 5); // d4
                
                UIManager.Instance?.ShowMessage($"You fail the riddle! A mind-shattering shockwave deals {damage} damage!", MessageType.Danger);
                
                // Deal damage directly, bypassing armor
                Player.Instance?.TakeDamage(damage);
            }
        }

        private static void OnSoothsayerRewardChosen(int choiceIndex)
        {
            switch (choiceIndex)
            {
                case 0: // 10 Silver
                    Inventory.Instance?.AddGold(10, playSound: true);
                    UIManager.Instance?.ShowMessage("You receive 10 silver coins!", MessageType.Success);
                    break;
                    
                case 1: // 3 XP
                    Player.Instance?.GainXP(3);
                    UIManager.Instance?.ShowMessage("You gain 3 XP from the mystical experience!", MessageType.Success);
                    break;
            }
        }

        private static void ResolveWeakMonster()
        {
            Debug.Log("Weak monster encounter!");

            Monster monster = MonsterDatabase.RollWeakMonster();
            
            // Apply sprite from database
            MonsterSpriteDatabase.Instance?.ApplySprite(monster);
            
            // Check for Cloak of Invisibility
            if (Player.Instance != null && Player.Instance.HasCloakInvisibility)
            {
                if (Player.Instance.TryUseCloakInvisibility(monster))
                {
                    // Monster bypassed, room still counts as explored
                    return;
                }
            }
            
            UIManager.Instance?.ShowMessage($"A {monster.MonsterName} attacks!");

            // Start combat
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.StartCombat(monster);
            }
        }

        private static void ResolveToughMonster()
        {
            Debug.Log("Tough monster encounter!");

            Monster monster = MonsterDatabase.RollToughMonster();
            
            // Apply sprite from database
            MonsterSpriteDatabase.Instance?.ApplySprite(monster);
            
            // Check for Cloak of Invisibility
            if (Player.Instance != null && Player.Instance.HasCloakInvisibility)
            {
                if (Player.Instance.TryUseCloakInvisibility(monster))
                {
                    // Monster bypassed, room still counts as explored
                    return;
                }
            }
            
            UIManager.Instance?.ShowMessage($"A {monster.MonsterName} attacks!");

            // Start combat
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.StartCombat(monster);
            }
        }

        private static void ResolveMerchant()
        {
            UIManager.Instance?.ShowMessage("You encounter a traveling merchant.");
            OnMerchant?.Invoke();

            // Enable merchant mode so clicking items sells them
            Inventory.Instance?.EnableMerchantMode();

            // Build merchant stock
            var merchantOptions = new System.Collections.Generic.List<string>();
            
            // Items for sale
            merchantOptions.Add($"Potion ({ItemDatabase.Potion.BuyPrice}s) - Heals d6 HP");
            merchantOptions.Add($"Rope ({ItemDatabase.Rope.BuyPrice}s) - +1 pit trap rolls");
            merchantOptions.Add($"Armour ({ItemDatabase.Armour.BuyPrice}s) - Reduces damage by d4");
            merchantOptions.Add($"Cloak of Invisibility ({ItemDatabase.CloakOfInvisibility.BuyPrice}s) - Avoid d4 fights");
            merchantOptions.Add($"Random Scroll ({ItemDatabase.RandomScroll.BuyPrice}s) - Mystery scroll");
            
            // Weapons for sale
            merchantOptions.Add($"Dagger ({WeaponDatabase.Dagger.BuyPrice}s) - {WeaponDatabase.Dagger.DamageString}, +{WeaponDatabase.Dagger.HitBonus} hit");
            merchantOptions.Add($"Sword ({WeaponDatabase.Sword.BuyPrice}s) - {WeaponDatabase.Sword.DamageString}, +{WeaponDatabase.Sword.HitBonus} hit");
            merchantOptions.Add($"Warhammer ({WeaponDatabase.Warhammer.BuyPrice}s) - {WeaponDatabase.Warhammer.DamageString}");
            merchantOptions.Add($"Flail ({WeaponDatabase.Flail.BuyPrice}s) - {WeaponDatabase.Flail.DamageString}");
            
            // Leave option
            merchantOptions.Add("Leave Shop");

            UIManager.Instance?.ShowChoicePanel(
                "Traveling Merchant",
                $"\"Welcome, traveler!\"\n<color=#C0C0C0>Your silver: {Inventory.Instance?.Gold ?? 0}</color>\n<size=80%>Click your items to sell them</size>",
                merchantOptions.ToArray(),
                OnMerchantChoiceSelected
            );
        }
        
        // Store the current merchant's weapon for purchase
        private static Weapon currentMerchantWeapon;

        private static void OnMerchantChoiceSelected(int choiceIndex)
        {
            switch (choiceIndex)
            {
                case 0: // Potion
                    TryPurchaseItem(ItemDatabase.Potion);
                    break;
                    
                case 1: // Rope
                    TryPurchaseItem(ItemDatabase.Rope);
                    break;
                    
                case 2: // Armour
                    TryPurchaseItem(ItemDatabase.Armour);
                    break;
                    
                case 3: // Cloak
                    TryPurchaseItem(ItemDatabase.CloakOfInvisibility);
                    break;
                    
                case 4: // Random Scroll
                    TryPurchaseItem(ItemDatabase.RandomScroll);
                    break;
                    
                case 5: // Dagger
                    TryPurchaseWeapon(WeaponDatabase.Dagger);
                    break;
                    
                case 6: // Sword
                    TryPurchaseWeapon(WeaponDatabase.Sword);
                    break;
                    
                case 7: // Warhammer
                    TryPurchaseWeapon(WeaponDatabase.Warhammer);
                    break;
                    
                case 8: // Flail
                    TryPurchaseWeapon(WeaponDatabase.Flail);
                    break;
                    
                case 9: // Leave
                default:
                    UIManager.Instance?.ShowMessage("The merchant waves goodbye.");
                    Inventory.Instance?.DisableMerchantMode();
                    return; // Don't refresh - panel already closed by OnChoiceButtonClicked
            }
            
            // Refresh merchant panel with updated silver
            RefreshMerchantPanel();
        }
        
        /// <summary>
        /// Refresh merchant panel if merchant mode is active (called after level up interruption)
        /// </summary>
        public static void RefreshMerchantIfActive()
        {
            if (Inventory.Instance?.IsMerchantModeActive == true)
            {
                RefreshMerchantPanel();
            }
        }
        
        private static void RefreshMerchantPanel()
        {
            // Re-enable merchant mode (in case it was disabled)
            Inventory.Instance?.EnableMerchantMode();
            
            // Build merchant stock
            var merchantOptions = new System.Collections.Generic.List<string>();
            
            // Items for sale
            merchantOptions.Add($"Potion ({ItemDatabase.Potion.BuyPrice}s) - Heals d6 HP");
            merchantOptions.Add($"Rope ({ItemDatabase.Rope.BuyPrice}s) - +1 pit trap rolls");
            merchantOptions.Add($"Armour ({ItemDatabase.Armour.BuyPrice}s) - Reduces damage by d4");
            merchantOptions.Add($"Cloak of Invisibility ({ItemDatabase.CloakOfInvisibility.BuyPrice}s) - Avoid d4 fights");
            merchantOptions.Add($"Random Scroll ({ItemDatabase.RandomScroll.BuyPrice}s) - Mystery scroll");
            
            // Weapons for sale
            merchantOptions.Add($"Dagger ({WeaponDatabase.Dagger.BuyPrice}s) - {WeaponDatabase.Dagger.DamageString}, +{WeaponDatabase.Dagger.HitBonus} hit");
            merchantOptions.Add($"Sword ({WeaponDatabase.Sword.BuyPrice}s) - {WeaponDatabase.Sword.DamageString}, +{WeaponDatabase.Sword.HitBonus} hit");
            merchantOptions.Add($"Warhammer ({WeaponDatabase.Warhammer.BuyPrice}s) - {WeaponDatabase.Warhammer.DamageString}");
            merchantOptions.Add($"Flail ({WeaponDatabase.Flail.BuyPrice}s) - {WeaponDatabase.Flail.DamageString}");
            
            // Leave option
            merchantOptions.Add("Leave Shop");

            UIManager.Instance?.RefreshChoicePanel(
                "Traveling Merchant",
                $"\"Welcome, traveler!\"\n<color=#C0C0C0>Your silver: {Inventory.Instance?.Gold ?? 0}</color>\n<size=80%>Click your items to sell them</size>",
                merchantOptions.ToArray(),
                OnMerchantChoiceSelected
            );
        }
        
        private static bool TryPurchaseItem(Item item)
        {
            if (Inventory.Instance == null) return false;
            
            if (!Inventory.Instance.CanAfford(item.BuyPrice))
            {
                UIManager.Instance?.ShowMessage($"Not enough silver! Need {item.BuyPrice}.", MessageType.Warning);
                return false;
            }
            
            if (Inventory.Instance.IsItemsFull)
            {
                UIManager.Instance?.ShowMessage("Inventory is full!", MessageType.Warning);
                return false;
            }
            
            Inventory.Instance.SpendGold(item.BuyPrice);
            Inventory.Instance.AddItem(item.Clone());
            AudioManager.Instance?.PlayItemBuy();
            UIManager.Instance?.ShowMessage($"Purchased {item.ItemName} for {item.BuyPrice} silver!", MessageType.Success);
            return true;
        }
        
        private static bool TryPurchaseWeapon(Weapon weapon)
        {
            if (Inventory.Instance == null) return false;
            
            if (!Inventory.Instance.CanAfford(weapon.BuyPrice))
            {
                UIManager.Instance?.ShowMessage($"Not enough silver! Need {weapon.BuyPrice}.", MessageType.Warning);
                return false;
            }
            
            // If unarmed, equip directly instead of adding to inventory
            if (Player.Instance != null && Player.Instance.IsUnarmed)
            {
                Inventory.Instance.SpendGold(weapon.BuyPrice);
                Player.Instance.EquipWeapon(weapon.Clone());
                AudioManager.Instance?.PlayItemBuy();
                UIManager.Instance?.ShowMessage($"Purchased and equipped {weapon.WeaponName} for {weapon.BuyPrice} silver!", MessageType.Success);
                return true;
            }
            
            if (Inventory.Instance.IsWeaponsFull)
            {
                UIManager.Instance?.ShowMessage("Weapon inventory is full!", MessageType.Warning);
                return false;
            }
            
            Inventory.Instance.SpendGold(weapon.BuyPrice);
            Inventory.Instance.AddWeapon(weapon.Clone());
            AudioManager.Instance?.PlayItemBuy();
            UIManager.Instance?.ShowMessage($"Purchased {weapon.WeaponName} for {weapon.BuyPrice} silver!", MessageType.Success);
            return true;
        }
        
        private static void ResolveStartItem()
        {
            Debug.Log("Start room: Found a random item!");
            
            Item item = ItemDatabase.GetRandomBasicItem();
            if (Inventory.Instance != null && item != null)
            {
                Inventory.Instance.AddItem(item);
                UIManager.Instance?.ShowMessage($"You found a {item.ItemName}!");
            }
            else
            {
                UIManager.Instance?.ShowMessage("You found an item!");
            }
            
            OnStartItem?.Invoke();
        }
        
        private static void ResolveStartScroll()
        {
            Debug.Log("Start room: Found a random scroll!");
            
            Item scroll = ItemDatabase.ResolveRandomScroll();
            if (Inventory.Instance != null && scroll != null)
            {
                Inventory.Instance.AddItem(scroll);
                UIManager.Instance?.ShowMessage($"You found a {scroll.ItemName}!");
            }
            else
            {
                UIManager.Instance?.ShowMessage("You found a scroll!");
            }
            
            OnStartScroll?.Invoke();
        }
        #endregion

        /// <summary>
        /// Roll and immediately resolve an encounter for normal rooms
        /// </summary>
        public static EncounterType RollAndResolve()
        {
            EncounterType encounter = RollForEncounter();
            ResolveEncounter(encounter);
            return encounter;
        }
        
        /// <summary>
        /// Roll and immediately resolve an encounter for the start room
        /// </summary>
        public static EncounterType RollAndResolveStartRoom()
        {
            EncounterType encounter = RollForStartRoomEncounter();
            ResolveEncounter(encounter);
            return encounter;
        }
    }
}
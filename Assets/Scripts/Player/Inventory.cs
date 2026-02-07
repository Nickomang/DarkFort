using UnityEngine;
using System.Collections.Generic;
using DarkFort.UI;
using DarkFort.Audio;

namespace DarkFort.Core
{
    /// <summary>
    /// Manages the player's inventory - weapons, items, and gold
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        #region Singleton
        public static Inventory Instance { get; private set; }
        #endregion

        #region Events
        public delegate void InventoryChangedHandler();
        public event InventoryChangedHandler OnInventoryChanged;

        public delegate void WeaponAddedHandler(Weapon weapon);
        public event WeaponAddedHandler OnWeaponAdded;

        public delegate void ItemAddedHandler(Item item);
        public event ItemAddedHandler OnItemAdded;

        public delegate void GoldChangedHandler(int newAmount);
        public event GoldChangedHandler OnGoldChanged;

        public delegate void ItemSoldHandler(Item item, int price);
        public event ItemSoldHandler OnItemSold;

        public delegate void WeaponSoldHandler(Weapon weapon, int price);
        public event WeaponSoldHandler OnWeaponSold;
        #endregion

        #region Fields
        [Header("Inventory")]
        [SerializeField] private List<Weapon> weapons = new List<Weapon>();
        [SerializeField] private List<Item> items = new List<Item>();
        [SerializeField] private int gold = 0;

        [Header("Settings")]
        [SerializeField] private int maxWeapons = 10;
        [SerializeField] private int maxItems = 15;

        // Merchant mode - when true, clicking items sells them
        private bool merchantModeActive = false;
        #endregion

        #region Properties
        public List<Weapon> Weapons => weapons;
        public List<Item> Items => items;
        public int Gold => gold;
        public int WeaponCount => weapons.Count;
        public int ItemCount => items.Count;
        public int MaxWeapons => maxWeapons;
        public int MaxItems => maxItems;
        public bool IsWeaponsFull => weapons.Count >= maxWeapons;
        public bool IsItemsFull => items.Count >= maxItems;
        public bool IsMerchantModeActive => merchantModeActive;
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
            gold = 0;
            OnGoldChanged?.Invoke(gold);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region Merchant Mode
        /// <summary>
        /// Enable merchant mode - clicking items will sell them
        /// </summary>
        public void EnableMerchantMode()
        {
            merchantModeActive = true;
            Debug.Log("Merchant mode enabled - click items to sell");
        }

        /// <summary>
        /// Disable merchant mode - return to normal item usage
        /// </summary>
        public void DisableMerchantMode()
        {
            merchantModeActive = false;
            Debug.Log("Merchant mode disabled");
        }
        #endregion

        #region Weapon Management
        public bool AddWeapon(Weapon weapon)
        {
            if (weapon == null)
            {
                Debug.LogWarning("Attempted to add null weapon");
                return false;
            }

            if (IsWeaponsFull)
            {
                Debug.LogWarning("Weapon inventory is full!");
                return false;
            }

            weapons.Add(weapon.Clone());
            Debug.Log($"Added {weapon.WeaponName} to inventory");

            OnWeaponAdded?.Invoke(weapon);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveWeapon(Weapon weapon)
        {
            if (weapon == null || !weapons.Contains(weapon))
            {
                return false;
            }

            weapons.Remove(weapon);
            Debug.Log($"Removed {weapon.WeaponName} from inventory");

            OnInventoryChanged?.Invoke();
            return true;
        }

        public Weapon GetWeapon(int index)
        {
            if (index < 0 || index >= weapons.Count)
            {
                return null;
            }
            return weapons[index];
        }

        /// <summary>
        /// Equip a weapon from inventory
        /// </summary>
        public bool EquipWeaponAt(int index)
        {
            Weapon weapon = GetWeapon(index);
            if (weapon == null)
            {
                return false;
            }

            // Get currently equipped weapon
            Weapon currentWeapon = Player.Instance?.GetEquippedWeapon();

            // Equip the new weapon
            Player.Instance?.EquipWeapon(weapon);

            // Remove newly equipped weapon from inventory
            RemoveWeapon(weapon);

            // Add old weapon back to inventory if there was one
            if (currentWeapon != null)
            {
                AddWeapon(currentWeapon);
            }

            Debug.Log($"Equipped {weapon.WeaponName}");
            return true;
        }

        /// <summary>
        /// Sell a weapon for gold
        /// </summary>
        public bool SellWeapon(Weapon weapon)
        {
            if (weapon == null || !weapons.Contains(weapon))
            {
                return false;
            }

            int sellValue = weapon.SellPrice;

            RemoveWeapon(weapon);
            AddGold(sellValue);

            AudioManager.Instance?.PlayItemSell();
            UIManager.Instance?.ShowMessage($"Sold {weapon.WeaponName} for {sellValue} silver", MessageType.Success);
            OnWeaponSold?.Invoke(weapon, sellValue);

            return true;
        }

        public bool SellWeaponAt(int index)
        {
            Weapon weapon = GetWeapon(index);
            return SellWeapon(weapon);
        }

        /// <summary>
        /// Sell the currently equipped weapon
        /// </summary>
        public bool SellEquippedWeapon()
        {
            if (Player.Instance == null || Player.Instance.EquippedWeapon == null)
            {
                return false;
            }

            Weapon weapon = Player.Instance.UnequipWeapon();
            if (weapon == null) return false;

            int sellValue = weapon.SellPrice;
            AddGold(sellValue);

            AudioManager.Instance?.PlayItemSell();
            UIManager.Instance?.ShowMessage($"Sold {weapon.WeaponName} for {sellValue} silver. You are now unarmed!", MessageType.Warning);
            OnWeaponSold?.Invoke(weapon, sellValue);

            return true;
        }
        #endregion

        #region Item Management
        public bool AddItem(Item item)
        {
            if (item == null)
            {
                Debug.LogWarning("Attempted to add null item");
                return false;
            }

            // Handle Random Scroll - roll to determine what it is
            if (item.Type == ItemType.RandomScroll)
            {
                item = ItemDatabase.ResolveRandomScroll();
            }

            // Try to stack with existing item
            foreach (var existingItem in items)
            {
                Debug.Log($"Checking stack: existing '{existingItem.ItemName}' (uses={existingItem.UsesRemaining}) vs new '{item.ItemName}' (uses={item.UsesRemaining})");
                if (existingItem.CanStackWith(item))
                {
                    existingItem.AddToStack(item.StackCount);
                    Debug.Log($"Stacked {item.ItemName} (now x{existingItem.StackCount})");
                    OnItemAdded?.Invoke(item);
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            // No stackable item found, add as new (if space)
            if (IsItemsFull)
            {
                Debug.LogWarning("Item inventory is full!");
                return false;
            }

            items.Add(item.Clone());
            Debug.Log($"Added NEW {item.ItemName} to inventory (uses={item.UsesRemaining}, stack={item.StackCount})");

            OnItemAdded?.Invoke(item);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItem(Item item)
        {
            if (item == null || !items.Contains(item))
            {
                return false;
            }

            items.Remove(item);
            Debug.Log($"Removed {item.ItemName} from inventory");

            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Remove one item from a stack, or remove the item entirely if stack is 1
        /// </summary>
        public bool RemoveOneFromStack(Item item)
        {
            if (item == null || !items.Contains(item))
            {
                return false;
            }

            if (item.StackCount > 1)
            {
                item.RemoveFromStack(1);
                Debug.Log($"Removed 1 from {item.ItemName} stack (now x{item.StackCount})");
            }
            else
            {
                items.Remove(item);
                Debug.Log($"Removed last {item.ItemName} from inventory");
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public Item GetItem(int index)
        {
            if (index < 0 || index >= items.Count)
            {
                return null;
            }
            return items[index];
        }

        public bool HasItem(ItemType itemType)
        {
            return items.Exists(i => i.Type == itemType);
        }

        public Item GetFirstItemOfType(ItemType itemType)
        {
            return items.Find(i => i.Type == itemType);
        }

        public int CountItemsOfType(ItemType itemType)
        {
            int total = 0;
            foreach (var item in items)
            {
                if (item.Type == itemType)
                    total += item.StackCount;
            }
            return total;
        }

        /// <summary>
        /// Sell one item from a stack for gold
        /// </summary>
        public bool SellItem(Item item)
        {
            if (item == null || !items.Contains(item))
            {
                return false;
            }

            int sellValue = item.SellPrice;

            // Remove only one from the stack
            RemoveOneFromStack(item);
            AddGold(sellValue);

            AudioManager.Instance?.PlayItemSell();
            string stackInfo = item.StackCount > 0 ? $" ({item.StackCount} remaining)" : "";
            UIManager.Instance?.ShowMessage($"Sold {item.ItemName} for {sellValue} silver{stackInfo}", MessageType.Success);
            OnItemSold?.Invoke(item, sellValue);

            return true;
        }

        public bool SellItemAt(int index)
        {
            Item item = GetItem(index);
            return SellItem(item);
        }
        #endregion

        #region Item Usage
        /// <summary>
        /// Use an item from inventory (or sell if merchant mode is active)
        /// </summary>
        public bool UseItem(Item item)
        {
            // If merchant mode is active, sell instead of use
            if (merchantModeActive)
            {
                return SellItem(item);
            }

            if (item == null || !items.Contains(item))
            {
                return false;
            }

            if (!item.HasUsesRemaining())
            {
                Debug.LogWarning($"{item.ItemName} has no uses remaining!");
                return false;
            }

            // Apply item effect based on type
            bool effectApplied = ApplyItemEffect(item);

            if (effectApplied)
            {
                // Handle consumption based on item type
                if (item.UsesRemaining == 1)
                {
                    // Single-use item (like potions) - remove from stack or remove entirely
                    if (item.StackCount > 1)
                    {
                        item.RemoveFromStack(1);
                        Debug.Log($"Used 1 {item.ItemName} (stack now x{item.StackCount})");
                    }
                    else
                    {
                        // Last one in stack, remove the item
                        RemoveItem(item);
                        Debug.Log($"Used last {item.ItemName}");
                    }
                }
                else
                {
                    // Multi-use item (like scrolls with d4 uses) - consume a charge
                    item.UseCharge();

                    if (!item.HasUsesRemaining())
                    {
                        RemoveItem(item);
                        Debug.Log($"Used last charge of {item.ItemName}");
                    }
                    else
                    {
                        Debug.Log($"Used {item.ItemName} ({item.UsesRemaining} uses left)");
                    }
                }

                OnInventoryChanged?.Invoke();
                return true;
            }

            return false;
        }

        public bool UseItemAt(int index)
        {
            Item item = GetItem(index);
            return UseItem(item);
        }

        private bool ApplyItemEffect(Item item)
        {
            switch (item.Type)
            {
                case ItemType.Potion:
                    int healing = UnityEngine.Random.Range(1, 7); // d6
                    Player.Instance?.Heal(healing);
                    UIManager.Instance?.ShowMessage($"Healed {healing} HP!", MessageType.Success);
                    return true;

                case ItemType.Rope:
                    UIManager.Instance?.ShowMessage("Rope is a passive item (helps with pit traps)");
                    return false;

                case ItemType.Armour:
                    UIManager.Instance?.ShowMessage("Armour is a passive item (reduces damage)");
                    return false;

                case ItemType.CloakOfInvisibility:
                    Player.Instance?.ActivateCloakInvisibility();
                    return true;

                case ItemType.SummonWeakDaemon:
                    Player.Instance?.ActivateDaemon();
                    return true;

                case ItemType.PalmsOpenSouthernGate:
                    // Can only use in combat
                    if (Combat.CombatManager.Instance?.IsInCombat == true)
                    {
                        int damage = Player.Instance?.UsePalmsDamage() ?? 0;
                        Combat.CombatManager.Instance?.DealDirectDamageToMonster(damage);
                        return true;
                    }
                    else
                    {
                        UIManager.Instance?.ShowMessage("Can only use Palms in combat!", MessageType.Warning);
                        return false;
                    }

                case ItemType.AegisOfSorrow:
                    Player.Instance?.ActivateAegis();
                    return true;

                case ItemType.FalseOmen:
                    // Show choice UI for room choice or reroll
                    UIManager.Instance?.ShowFalseOmenChoice(item);
                    return false; // Don't consume yet - UI will handle it

                default:
                    return false;
            }
        }
        #endregion

        #region Gold Management
        public void AddGold(int amount, bool playSound = false)
        {
            gold += amount;

            // Track total silver collected for stats
            if (amount > 0)
            {
                Player.Instance?.AddSilverCollected(amount);

                // Play coin sound for loot pickups
                if (playSound)
                {
                    AudioManager.Instance?.PlayCoin();
                }
            }

            OnGoldChanged?.Invoke(gold);
            OnInventoryChanged?.Invoke();
        }

        public bool SpendGold(int amount)
        {
            if (gold < amount)
            {
                UIManager.Instance?.ShowMessage($"Not enough silver! Need {amount}, have {gold}", MessageType.Warning);
                return false;
            }

            gold -= amount;
            OnGoldChanged?.Invoke(gold);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool CanAfford(int cost)
        {
            return gold >= cost;
        }

        /// <summary>
        /// Manually trigger inventory changed event
        /// </summary>
        public void NotifyInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }
        #endregion

        #region Reset
        public void ResetInventory()
        {
            weapons.Clear();
            items.Clear();
            gold = 0;
            merchantModeActive = false;
            OnGoldChanged?.Invoke(gold);
            OnInventoryChanged?.Invoke();
        }
        #endregion

        #region Debug
        [ContextMenu("Print Inventory")]
        private void DebugPrintInventory()
        {
            Debug.Log($"=== INVENTORY ===");
            Debug.Log($"Gold: {gold}");
            Debug.Log($"Merchant Mode: {merchantModeActive}");

            Debug.Log($"Weapons ({weapons.Count}/{maxWeapons}):");
            for (int i = 0; i < weapons.Count; i++)
            {
                Debug.Log($"  {i}: {weapons[i]}");
            }

            Debug.Log($"Items ({items.Count}/{maxItems}):");
            for (int i = 0; i < items.Count; i++)
            {
                Debug.Log($"  {i}: {items[i]} (Sell: {items[i].SellPrice}g)");
            }

            if (Player.Instance != null && Player.Instance.EquippedWeapon != null)
            {
                Debug.Log($"Equipped: {Player.Instance.EquippedWeapon}");
            }
        }

        [ContextMenu("Add 50 Gold")]
        private void DebugAddGold()
        {
            AddGold(50);
        }

        [ContextMenu("Toggle Merchant Mode")]
        private void DebugToggleMerchantMode()
        {
            if (merchantModeActive)
                DisableMerchantMode();
            else
                EnableMerchantMode();
        }
        #endregion
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DarkFort.Core;

namespace DarkFort.UI
{
    /// <summary>
    /// Displays level up buff icons. Each icon starts transparent and becomes visible when earned.
    /// Hover over an icon to see tooltip with buff details.
    /// </summary>
    public class LevelUpBuffDisplay : MonoBehaviour
    {
        [Header("Buff Icons (in order 1-6)")]
        [SerializeField] private Image knightedIcon;        // Roll 1: Knighted
        [SerializeField] private Image combatTrainingIcon;  // Roll 2: +1 to hit
        [SerializeField] private Image fortitudeIcon;       // Roll 3: +5 max HP
        [SerializeField] private Image potionsIcon;         // Roll 4: 5 potions
        [SerializeField] private Image zweihanderIcon;      // Roll 5: Zweihander
        [SerializeField] private Image monsterSlayerIcon;   // Roll 6: Halve monster damage

        [Header("Tooltip")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private TextMeshProUGUI tooltipText;

        [Header("Visibility Settings")]
        [SerializeField] private float hiddenAlpha = 0f;
        [SerializeField] private float visibleAlpha = 1f;

        // Track which buffs are active
        private bool[] buffActive = new bool[6];

        private void Start()
        {
            // Initialize all icons as hidden
            SetIconAlpha(knightedIcon, hiddenAlpha);
            SetIconAlpha(combatTrainingIcon, hiddenAlpha);
            SetIconAlpha(fortitudeIcon, hiddenAlpha);
            SetIconAlpha(potionsIcon, hiddenAlpha);
            SetIconAlpha(zweihanderIcon, hiddenAlpha);
            SetIconAlpha(monsterSlayerIcon, hiddenAlpha);

            // Add hover handlers to each icon
            AddHoverHandler(knightedIcon, 0);
            AddHoverHandler(combatTrainingIcon, 1);
            AddHoverHandler(fortitudeIcon, 2);
            AddHoverHandler(potionsIcon, 3);
            AddHoverHandler(zweihanderIcon, 4);
            AddHoverHandler(monsterSlayerIcon, 5);

            // Hide tooltip initially
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);

            // Subscribe to buff applied event (fires AFTER buff is applied)
            if (Player.Instance != null)
            {
                Player.Instance.OnLevelUpBuffApplied += OnLevelUpBuffApplied;
            }

            // Initial refresh in case we're loading a saved game
            RefreshBuffDisplay();
        }

        private void OnDestroy()
        {
            if (Player.Instance != null)
            {
                Player.Instance.OnLevelUpBuffApplied -= OnLevelUpBuffApplied;
            }
        }

        private void OnLevelUpBuffApplied(int rollResult)
        {
            // Directly activate the buff icon for the roll result
            ActivateBuff(rollResult);
        }

        /// <summary>
        /// Call this to refresh which buffs are shown (e.g., after loading a save)
        /// </summary>
        public void RefreshBuffDisplay()
        {
            if (Player.Instance == null) return;

            // Check each buff status from Player
            // Roll 1: Knighted
            bool isKnighted = Player.Instance.IsKnighted;
            SetBuffActive(0, isKnighted);

            // Roll 2: Combat Training (+1 to hit) - check if bonus > 0
            bool hasCombatTraining = Player.Instance.BonusToHit > 0;
            SetBuffActive(1, hasCombatTraining);

            // Roll 3: Fortitude (+5 HP) - check if max HP > 15 (base)
            bool hasFortitude = Player.Instance.MaxHealth > 15;
            SetBuffActive(2, hasFortitude);

            // Roll 4: Potions - this is a one-time reward, track via used results
            bool hadPotions = Player.Instance.HasUsedLevelUpResult(4);
            SetBuffActive(3, hadPotions);

            // Roll 5: Zweihander - check if equipped or in inventory
            bool hasZweihander = Player.Instance.HasUsedLevelUpResult(5);
            SetBuffActive(4, hasZweihander);

            // Roll 6: Monster Slayer - check if any monsters have halved damage
            bool hasMonsterSlayer = Player.Instance.HalvedDamageMonsterCount > 0;
            SetBuffActive(5, hasMonsterSlayer);
        }

        private void SetBuffActive(int index, bool active)
        {
            if (index < 0 || index >= 6) return;

            buffActive[index] = active;
            Image icon = GetIconByIndex(index);
            SetIconAlpha(icon, active ? visibleAlpha : hiddenAlpha);
        }

        private Image GetIconByIndex(int index)
        {
            switch (index)
            {
                case 0: return knightedIcon;
                case 1: return combatTrainingIcon;
                case 2: return fortitudeIcon;
                case 3: return potionsIcon;
                case 4: return zweihanderIcon;
                case 5: return monsterSlayerIcon;
                default: return null;
            }
        }

        private void SetIconAlpha(Image icon, float alpha)
        {
            if (icon == null) return;
            Color c = icon.color;
            c.a = alpha;
            icon.color = c;
        }

        private void AddHoverHandler(Image icon, int buffIndex)
        {
            if (icon == null) return;

            // Add EventTrigger component if not present
            EventTrigger trigger = icon.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = icon.gameObject.AddComponent<EventTrigger>();

            // Pointer Enter
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => OnBuffHoverEnter(buffIndex, icon.rectTransform));
            trigger.triggers.Add(enterEntry);

            // Pointer Exit
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => OnBuffHoverExit());
            trigger.triggers.Add(exitEntry);
        }

        private void OnBuffHoverEnter(int buffIndex, RectTransform iconRect)
        {
            // Only show tooltip if buff is active
            if (!buffActive[buffIndex]) return;

            if (tooltipPanel == null || tooltipText == null) return;

            // Set tooltip text based on buff
            tooltipText.text = GetBuffTooltip(buffIndex);
            tooltipPanel.SetActive(true);
        }

        private void OnBuffHoverExit()
        {
            if (tooltipPanel != null)
                tooltipPanel.SetActive(false);
        }

        private string GetBuffTooltip(int buffIndex)
        {
            switch (buffIndex)
            {
                case 0: // Knighted
                    return "<b>Knighted</b>\n<size=80%>You have been granted the title of 'Sir' by the realm.</size>";

                case 1: // Combat Training
                    int hitBonus = Player.Instance?.BonusToHit ?? 0;
                    return $"<b>Combat Training</b>\n<size=80%>+{hitBonus} to all attack rolls.</size>";

                case 2: // Fortitude
                    int maxHP = Player.Instance?.MaxHealth ?? 15;
                    int bonusHP = maxHP - 15;
                    return $"<b>Fortitude</b>\n<size=80%>+{bonusHP} maximum HP (now {maxHP}).</size>";

                case 3: // Herbmaster's Gift
                    return "<b>Herbmaster's Gift</b>\n<size=80%>You received 5 potions from a friendly herbmaster.</size>";

                case 4: // Zweihander
                    return "<b>Zweihander</b>\n<size=80%>You found a mighty Zweihander (d6+2 damage).</size>";

                case 5: // Monster Slayer
                    string monsters = "";
                    if (Player.Instance != null)
                    {
                        var halvedMonsters = Player.Instance.GetHalvedDamageMonsters();
                        if (halvedMonsters != null && halvedMonsters.Count > 0)
                        {
                            monsters = string.Join(", ", halvedMonsters);
                        }
                    }
                    return $"<b>Monster Slayer</b>\n<size=80%>Damage halved from: {monsters}</size>";

                default:
                    return "";
            }
        }

        /// <summary>
        /// Manually activate a buff icon (called from Player when buff is gained)
        /// </summary>
        public void ActivateBuff(int rollResult)
        {
            int index = rollResult - 1; // Convert 1-6 to 0-5
            SetBuffActive(index, true);
        }
    }
}
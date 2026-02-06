using UnityEngine;
using System;

namespace DarkFort.Core
{
    /// <summary>
    /// Tracks the last dice roll action for False Omen reroll functionality.
    /// Stores a snapshot of game state before the roll so it can be undone.
    /// </summary>
    public class RerollSystem : MonoBehaviour
    {
        #region Singleton
        public static RerollSystem Instance { get; private set; }
        #endregion

        #region Events
        public delegate void RerollAvailableChangedHandler(bool available);
        public event RerollAvailableChangedHandler OnRerollAvailableChanged;
        #endregion

        #region State
        private RerollableAction lastAction;

        public bool HasRerollableAction => lastAction != null && !lastAction.WasRerolled;
        public bool CanReroll => HasRerollableAction && Player.Instance?.HasFalseOmenReroll == true;
        public string LastActionDescription => lastAction?.Description ?? "";
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
                Instance = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Record an action that can be rerolled
        /// </summary>
        public void RecordAction(RerollableAction action)
        {
            lastAction = action;
            Debug.Log($"[RerollSystem] Recorded: {action.Description}");
            NotifyRerollAvailabilityChanged();
        }

        /// <summary>
        /// Clear the last action (e.g., when moving to a new room or combat ends)
        /// </summary>
        public void ClearLastAction()
        {
            lastAction = null;
            NotifyRerollAvailabilityChanged();
        }

        /// <summary>
        /// Attempt to reroll the last action using False Omen
        /// </summary>
        public bool TryReroll()
        {
            if (!CanReroll)
            {
                Debug.Log("[RerollSystem] Cannot reroll - no action or no False Omen");
                return false;
            }

            // Consume the False Omen
            Player.Instance.UseFalseOmenReroll();

            // Log the reroll attempt
            UI.UIManager.Instance?.LogMessage($"<color=#9966FF>False Omen! Rerolling {lastAction.Description}...</color>");

            // Undo the previous action
            Debug.Log($"[RerollSystem] Undoing: {lastAction.Description}");
            lastAction.UndoAction?.Invoke();

            // Re-execute the action
            Debug.Log($"[RerollSystem] Re-executing: {lastAction.Description}");
            lastAction.RerollAction?.Invoke();

            // Mark as rerolled
            lastAction.WasRerolled = true;

            NotifyRerollAvailabilityChanged();
            return true;
        }

        /// <summary>
        /// Refresh availability (call when False Omen state changes)
        /// </summary>
        public void RefreshAvailability()
        {
            NotifyRerollAvailabilityChanged();
        }

        private void NotifyRerollAvailabilityChanged()
        {
            OnRerollAvailableChanged?.Invoke(CanReroll);
        }
        #endregion
    }

    /// <summary>
    /// Represents an action that can be rerolled
    /// </summary>
    public class RerollableAction
    {
        public string Description;      // e.g., "Attack Roll vs Goblin"
        public int OriginalRoll;        // The value that was rolled
        public bool WasRerolled;        // Can only reroll once

        /// <summary>
        /// Action to undo the effects of this roll
        /// </summary>
        public Action UndoAction;

        /// <summary>
        /// Action to re-execute the roll
        /// </summary>
        public Action RerollAction;

        public RerollableAction(string description, int originalRoll)
        {
            Description = description;
            OriginalRoll = originalRoll;
            WasRerolled = false;
        }
    }
}
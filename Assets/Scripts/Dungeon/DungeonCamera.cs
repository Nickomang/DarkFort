using UnityEngine;
using UnityEngine.InputSystem;

namespace DarkFort.Dungeon
{
    /// <summary>
    /// Simple camera controller that follows the PlayerMarker.
    /// </summary>
    public class DungeonCamera : MonoBehaviour
    {
        #region Singleton
        public static DungeonCamera Instance { get; private set; }
        #endregion

        #region Settings
        [Header("Target")]
        [SerializeField] private Transform target;  // Usually PlayerMarker
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

        [Header("Follow Settings")]
        [SerializeField] private float followSpeed = 8f;
        [SerializeField] private bool instantFollow = false;

        [Header("Zoom Settings")]
        [SerializeField] private float defaultZoom = 5f;
        [SerializeField] private float minZoom = 2f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private float zoomSpeed = 2f;
        #endregion

        #region State
        private Camera cam;
        private float currentZoom;
        private float targetZoom;
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

            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
            }

            currentZoom = defaultZoom;
            targetZoom = defaultZoom;

            if (cam != null)
            {
                cam.orthographicSize = currentZoom;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            // Auto-find PlayerMarker if not assigned
            if (target == null && PlayerMarker.Instance != null)
            {
                target = PlayerMarker.Instance.transform;
                Debug.Log("DungeonCamera auto-assigned PlayerMarker as target");
            }

            // Snap to target immediately on start
            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }

        private void LateUpdate()
        {
            // Try to find target if we don't have one
            if (target == null && PlayerMarker.Instance != null)
            {
                target = PlayerMarker.Instance.transform;
            }

            HandleInput();
            FollowTarget();
            UpdateZoom();
        }
        #endregion

        #region Following
        private void FollowTarget()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;

            if (instantFollow)
            {
                transform.position = desiredPosition;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);
            }
        }
        #endregion

        #region Zoom
        private void HandleInput()
        {
            var keyboard = Keyboard.current;

            // Keyboard zoom only (scroll wheel disabled)
            if (keyboard != null)
            {
                if (keyboard.equalsKey.isPressed || keyboard.numpadPlusKey.isPressed)
                {
                    targetZoom -= zoomSpeed * Time.deltaTime * 5f;
                    targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
                }
                if (keyboard.minusKey.isPressed || keyboard.numpadMinusKey.isPressed)
                {
                    targetZoom += zoomSpeed * Time.deltaTime * 5f;
                    targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
                }

                // Reset zoom
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    targetZoom = defaultZoom;
                }
            }
        }

        private void UpdateZoom()
        {
            if (cam == null || !cam.orthographic) return;

            currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * 5f);
            cam.orthographicSize = currentZoom;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set zoom level directly
        /// </summary>
        public void SetZoom(float zoom)
        {
            targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        }

        /// <summary>
        /// Snap camera to target immediately
        /// </summary>
        public void SnapToTarget()
        {
            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }

        /// <summary>
        /// Set a new target to follow
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
        #endregion

        #region Debug
        [ContextMenu("Snap To Target")]
        private void DebugSnapToTarget()
        {
            SnapToTarget();
        }

        [ContextMenu("Reset Zoom")]
        private void DebugResetZoom()
        {
            targetZoom = defaultZoom;
        }

        [ContextMenu("Find PlayerMarker")]
        private void DebugFindPlayerMarker()
        {
            if (PlayerMarker.Instance != null)
            {
                target = PlayerMarker.Instance.transform;
                Debug.Log("Found and assigned PlayerMarker");
            }
            else
            {
                Debug.LogWarning("PlayerMarker.Instance is null");
            }
        }
        #endregion
    }
}
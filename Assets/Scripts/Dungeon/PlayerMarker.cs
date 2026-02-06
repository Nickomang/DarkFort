using UnityEngine;

namespace DarkFort.Dungeon
{
    /// <summary>
    /// Visual representation of the player on the dungeon map.
    /// Moves smoothly between rooms and provides a target for the camera.
    /// </summary>
    public class PlayerMarker : MonoBehaviour
    {
        #region Singleton
        public static PlayerMarker Instance { get; private set; }
        #endregion

        #region Settings
        [Header("Appearance")]
        [SerializeField] private Color markerColor = Color.white;
        [SerializeField] private float markerSize = 1f;
        [SerializeField] private int sortingOrder = 10;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private bool instantMove = false;  // Set true to snap instead of lerp
        #endregion

        #region State
        private Vector3 targetPosition;
        private SpriteRenderer spriteRenderer;
        private Room currentRoom;
        private bool isInitialized = false;
        #endregion

        #region Properties
        public Room CurrentRoom => currentRoom;
        public Vector3 TargetPosition => targetPosition;
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

            CreateVisual();
        }

        private void Start()
        {
            // Subscribe to room changes
            if (MapController.Instance != null)
            {
                MapController.Instance.OnRoomEntered += OnRoomEntered;
            }

            // Initialize position if dungeon already exists
            if (MapController.Instance != null && MapController.Instance.CurrentRoom != null)
            {
                MoveToRoom(MapController.Instance.CurrentRoom, true);
            }
        }

        private void OnDestroy()
        {
            if (MapController.Instance != null)
            {
                MapController.Instance.OnRoomEntered -= OnRoomEntered;
            }

            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Smooth movement towards target
            if (!instantMove && transform.position != targetPosition)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);

                // Snap if very close
                if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
                {
                    transform.position = targetPosition;
                }
            }
        }
        #endregion

        #region Initialization
        private void CreateVisual()
        {
            // Create sprite renderer if not already present
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            // Create a circle sprite for the player marker
            spriteRenderer.sprite = CreateCircleSprite();
            spriteRenderer.color = markerColor;
            spriteRenderer.sortingOrder = sortingOrder;

            transform.localScale = new Vector3(markerSize, markerSize, 1f);
        }

        private Sprite CreateCircleSprite()
        {
            // Create a simple circle texture
            int size = 32;
            Texture2D texture = new Texture2D(size, size);
            float radius = size / 2f;
            Vector2 center = new Vector2(radius, radius);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist < radius - 1)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                    else if (dist < radius)
                    {
                        // Anti-aliased edge
                        float alpha = radius - dist;
                        texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
        #endregion

        #region Event Handlers
        private void OnRoomEntered(Room room, bool firstTime)
        {
            MoveToRoom(room, false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Move the player marker to a room
        /// </summary>
        public void MoveToRoom(Room room, bool instant = false)
        {
            if (room == null) return;

            currentRoom = room;

            // Get world position from visualizer
            if (DungeonVisualizer.Instance != null)
            {
                targetPosition = DungeonVisualizer.Instance.GetRoomWorldPosition(room);
            }
            else
            {
                // Fallback if visualizer not ready
                targetPosition = new Vector3(room.GridPosition.x * 2.5f, room.GridPosition.y * 2.5f, 0);
            }

            if (instant || instantMove)
            {
                transform.position = targetPosition;
            }

            isInitialized = true;

            Debug.Log($"PlayerMarker moving to Room {room.Id} at {targetPosition}");
        }

        /// <summary>
        /// Snap immediately to current target position
        /// </summary>
        public void SnapToTarget()
        {
            transform.position = targetPosition;
        }
        #endregion

        #region Debug
        [ContextMenu("Snap To Current Room")]
        private void DebugSnapToRoom()
        {
            if (MapController.Instance?.CurrentRoom != null)
            {
                MoveToRoom(MapController.Instance.CurrentRoom, true);
            }
        }
        #endregion
    }
}
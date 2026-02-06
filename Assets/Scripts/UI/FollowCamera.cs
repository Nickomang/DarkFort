using UnityEngine;

namespace DarkFort.UI
{
    /// <summary>
    /// Makes this object follow the camera's X/Y position.
    /// Attach to background or other objects that should stay centered on camera.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Camera targetCamera;

        [Header("Settings")]
        [SerializeField] private bool followX = true;
        [SerializeField] private bool followY = true;
        [SerializeField] private bool followZ = false;

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;

            Vector3 camPos = targetCamera.transform.position;
            Vector3 newPos = transform.position;

            if (followX) newPos.x = camPos.x;
            if (followY) newPos.y = camPos.y;
            if (followZ) newPos.z = camPos.z;

            transform.position = newPos;
        }
    }
}
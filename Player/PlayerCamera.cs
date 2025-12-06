using UnityEngine;

namespace DigDigDiner
{
    /// <summary>
    /// Camera that smoothly follows the player with boundary constraints.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Player player;

        [Header("Settings")]
        [SerializeField] private float smoothTime = SharedConstants.PLAYER_CAMERA_SMOOTH_TIME;
        [SerializeField] private float boundPadding = SharedConstants.PLAYER_CAMERA_BOUND_PADDING;
        [SerializeField] private float cameraOffsetZ = -10f;
        [SerializeField] private bool constrainToBounds = true;

        private DualGridSystem gridSystem;
        private Camera cam;
        private Vector3 velocity = Vector3.zero;

        // Camera bounds (calculated based on grid size)
        private float minX, maxX, minY, maxY;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            // 1. Find Player if missing
            if (player == null)
            {
                player = FindFirstObjectByType<Player>();
                if (player == null)
                {
                    Debug.LogError("PlayerCamera: No Player found in scene!");
                    enabled = false;
                    return;
                }
            }

            gridSystem = player.GridSystem;

            if (gridSystem == null)
            {
                gridSystem = FindFirstObjectByType<DualGridSystem>();
                
                if (gridSystem == null)
                {
                    Debug.LogError("PlayerCamera: No DualGridSystem found! Camera disabled.");
                    enabled = false;
                    return;
                }
            }

            CalculateCameraBounds();

            Vector3 targetPosition = GetTargetPosition();
            transform.position = targetPosition;
            
            Debug.Log($"PlayerCamera: Initialized. Tracking player at {player.transform.position}");
        }

        private void LateUpdate()
        {
            if (player == null) return;

            // Get target position (player position + camera offset)
            Vector3 targetPosition = GetTargetPosition();

            // Smoothly move camera towards target
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime
            );
        }

        private Vector3 GetTargetPosition()
        {
            if (player == null) return transform.position;

            Vector3 targetPosition = player.transform.position;
            targetPosition.z = cameraOffsetZ;

            // Constrain to bounds if enabled
            if (constrainToBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
                targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
            }

            return targetPosition;
        }

        private void CalculateCameraBounds()
        {
            if (gridSystem == null || cam == null) return;

            // Get camera dimensions in world units
            float cameraHeight = cam.orthographicSize * 2f;
            float cameraWidth = cameraHeight * cam.aspect;

            float halfWidth = cameraWidth / 2f;
            float halfHeight = cameraHeight / 2f;

            // Calculate bounds based on grid size
            minX = halfWidth - boundPadding;
            maxX = gridSystem.Width - halfWidth + boundPadding;

            minY = halfHeight - boundPadding;
            maxY = gridSystem.Height - halfHeight + boundPadding;

            // Ensure min doesn't exceed max (for small grids)
            if (minX > maxX)
            {
                float centerX = (minX + maxX) / 2f;
                minX = maxX = centerX;
            }

            if (minY > maxY)
            {
                float centerY = (minY + maxY) / 2f;
                minY = maxY = centerY;
            }

            Debug.Log($"PlayerCamera: Bounds calculated - X:[{minX:F2}, {maxX:F2}], Y:[{minY:F2}, {maxY:F2}]");
        }

        public void RecalculateBounds()
        {
            CalculateCameraBounds();
        }

        public void SetTarget(Player newPlayer)
        {
            player = newPlayer;
            if (player != null)
            {
                gridSystem = player.GridSystem;
                CalculateCameraBounds();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !constrainToBounds) return;

            Gizmos.color = Color.cyan;
            Vector3 bottomLeft = new Vector3(minX, minY, 0);
            Vector3 bottomRight = new Vector3(maxX, minY, 0);
            Vector3 topLeft = new Vector3(minX, maxY, 0);
            Vector3 topRight = new Vector3(maxX, maxY, 0);

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
#endif
    }
}
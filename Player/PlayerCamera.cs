using UnityEngine;

namespace DigDigDiner
{
    /// <summary>
    /// Camera that follows the player with boundary constraints.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Player player;

        [Header("Settings")]
        [SerializeField] private float smoothTime = SharedConstants.PLAYER_CAMERA_SMOOTH_TIME;
        [SerializeField] private float boundPadding = SharedConstants.PLAYER_CAMERA_BOUND_PADDING;
        
        [SerializeField] private bool constrainToBounds = true;

        private DualGridSystem gridSystem;
        private Camera cam;
        private Vector3 velocity = Vector3.zero;
        private float initialZOffset; 

        // Camera bounds
        private float minX, maxX, minY, maxY;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            // Find Player if missing
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

            initialZOffset = transform.position.z - player.transform.position.z;

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

            // Snap immediately to target on start 
            Vector3 targetPosition = GetTargetPosition();
            transform.position = targetPosition;
            
            Debug.Log($"PlayerCamera: Initialized. Tracking player at {player.transform.position}");
        }

        private void LateUpdate()
        {
            if (player == null) return;

            Vector3 targetPosition = GetTargetPosition();

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

            // Target is Player Position + the initial Z gap
            Vector3 targetPosition = player.transform.position;
            targetPosition.z += initialZOffset; 

            if (constrainToBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
                targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
            }

            return targetPosition;
        }

        private void CalculateCameraBounds()
        {
            if (gridSystem == null || cam == null || player == null) return;

            float halfHeight, halfWidth;

            if (cam.orthographic)
            {
                halfHeight = cam.orthographicSize;
                halfWidth = halfHeight * cam.aspect;
            }
            else // *** NEW PERSPECTIVE LOGIC ***
            {
                // Calculate distance from Camera to Player's Plane (MG Layer)
                // We use Abs to ensure positive distance regardless of which way Z points
                float distanceToPlayer = Mathf.Abs(transform.position.z - player.transform.position.z);
                
                // Calculate the visible height at that specific distance
                // h = 2 * distance * tan(FOV/2)
                // Note: FOV is vertical in Unity
                float frustumHeight = 2.0f * distanceToPlayer * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                
                halfHeight = frustumHeight / 2f;
                halfWidth = halfHeight * cam.aspect;
            }

            // Calculate bounds based on grid size
            // We use gridSystem.Width to ensure we stop exactly at the map edge
            minX = halfWidth - boundPadding;
            maxX = gridSystem.Width - halfWidth + boundPadding;

            minY = halfHeight - boundPadding;
            maxY = gridSystem.Height - halfHeight + boundPadding;

            // Safety check for small maps
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
                // Update Z offset for new player if needed
                initialZOffset = transform.position.z - player.transform.position.z;
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


using UnityEngine;
using DigDigDiner;

[RequireComponent(typeof(Camera))]
public class PlayerCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerTarget;
    
    // Kept for inspector visibility, but we primarily use playerTarget now
    [SerializeField] private PlayerMovement playerMovement; 

    [Header("Settings")]
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private float boundPadding = 1f;
    [SerializeField] private bool constrainToBounds = true;

    private DualGridSystem gridSystem;
    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private float initialZOffset;
    private float minX, maxX, minY, maxY;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        // 1. Find the Grid 
        if (gridSystem == null)
        {
            gridSystem = FindFirstObjectByType<DualGridSystem>();
            if (gridSystem != null)
            {
                CalculateCameraBounds();
            }
        }

        // 2. Find the player, or wait
        if (playerTarget == null)
        {
            if (playerMovement == null) 
                playerMovement = FindFirstObjectByType<PlayerMovement>();
            
            if (playerMovement != null)
            {
                playerTarget = playerMovement.transform;
                InitializeTarget();
            }
            else
            {
                Debug.Log("PlayerCamera: No player found in scene. Waiting for Spawner...");
            }
        }
        else
        {
            InitializeTarget();
        }
    }

    private void InitializeTarget()
    {
        initialZOffset = transform.position.z - playerTarget.position.z;
        Vector3 targetPosition = GetTargetPosition();
        transform.position = targetPosition;
        Debug.Log($"PlayerCamera: Linked to {playerTarget.name}");
    }

    private void LateUpdate()
    {
        // Standby Check: If we have no target, do nothing.
        if (playerTarget == null) return;

        Vector3 targetPosition = GetTargetPosition();

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );
    }

    public void SetTarget(Transform newTarget)
    {
        playerTarget = newTarget;
        
        enabled = true; 

        if (playerTarget != null)
        {
            if (gridSystem == null) 
                gridSystem = FindFirstObjectByType<DualGridSystem>();

            CalculateCameraBounds();
            InitializeTarget();
        }
    }

    private Vector3 GetTargetPosition()
    {
        if (playerTarget == null) return transform.position;

        // Target is Player Position + the initial Z gap
        Vector3 targetPosition = playerTarget.position;
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
        if (gridSystem == null || cam == null || playerTarget == null) return;

        float halfHeight, halfWidth;

        if (cam.orthographic)
        {
            halfHeight = cam.orthographicSize;
            halfWidth = halfHeight * cam.aspect;
        }
        else
        {
            // Calculate distance from Camera to Player's Plane
            float distanceToPlayer = Mathf.Abs(transform.position.z - playerTarget.position.z);

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

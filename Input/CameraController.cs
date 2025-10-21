using UnityEngine;
using UnityEngine.InputSystem;

namespace DigDigDiner
{
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = SharedConstants.DEFAULT_CAMERA_MOVE_SPEED;
        [SerializeField] private float smoothTime = SharedConstants.DEFAULT_CAMERA_SMOOTH_TIME;

        [Header("Bounds")]
        [SerializeField] private Vector2 minBounds;
        [SerializeField] private Vector2 maxBounds;

        [Header("Manual Control")]
        [SerializeField] private bool enabledByDefault = false;
        [SerializeField] private Camera playerCamera; // Reference to PlayerCamera

        private Vector3 velocity = Vector3.zero;
        private Vector3 targetPosition;

        private TileEditorInputs inputActions;
        private Vector2 moveInput;
        private bool manualControlEnabled;
        private Camera thisCamera;
        
        private void Awake()
        {
            inputActions = new TileEditorInputs();
            thisCamera = GetComponent<Camera>();

            // Auto-find PlayerCamera if not assigned
            if (playerCamera == null)
            {
                var playerCameraScript = Object.FindFirstObjectByType<PlayerCamera>();
                if (playerCameraScript != null)
                {
                    playerCamera = playerCameraScript.GetComponent<Camera>();
                }
            }
        }
        
        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("CameraController: inputActions is null in OnEnable!");
                return;
            }

            inputActions.Enable();
            inputActions.CameraMap.ResetCamera.performed += OnResetCamera;
            inputActions.CameraMap.ToggleManualControl.performed += OnToggleManualControl;
        }

        private void OnDisable()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("CameraController: inputActions is null in OnDisable!");
                return;
            }

            inputActions.CameraMap.ResetCamera.performed -= OnResetCamera;
            inputActions.CameraMap.ToggleManualControl.performed -= OnToggleManualControl;
            inputActions.Disable();
        }
        
        private void Start()
        {
            var gridSystem = Object.FindFirstObjectByType<DualGridSystem>();
            if (gridSystem != null)
            {
                minBounds = new Vector2(-SharedConstants.CAMERA_BOUND_PADDING, -SharedConstants.CAMERA_BOUND_PADDING);
                maxBounds = new Vector2(
                    gridSystem.Width + SharedConstants.CAMERA_BOUND_PADDING,
                    gridSystem.Height + SharedConstants.CAMERA_BOUND_PADDING
                );
            }
            else
            {
                minBounds = new Vector2(-SharedConstants.CAMERA_BOUND_PADDING, -SharedConstants.CAMERA_BOUND_PADDING);
                maxBounds = new Vector2(
                    SharedConstants.GRID_WIDTH + SharedConstants.CAMERA_BOUND_PADDING,
                    SharedConstants.GRID_HEIGHT + SharedConstants.CAMERA_BOUND_PADDING
                );
            }

            targetPosition = transform.position;
            manualControlEnabled = enabledByDefault;

            // Set initial camera states
            SetCameraActive(manualControlEnabled);

            Debug.Log($"CameraController: Manual control {(manualControlEnabled ? "ENABLED" : "DISABLED")}. Press F1 to toggle.");
        }

        private void SetCameraActive(bool active)
        {
            if (thisCamera != null)
            {
                thisCamera.enabled = active;
            }

            if (playerCamera != null)
            {
                playerCamera.enabled = !active;
            }
        }
        
        private void Update()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("CameraController: inputActions is null!");
                return;
            }

            // Only process movement if manual control is enabled
            if (manualControlEnabled)
            {
                moveInput = inputActions.CameraMap.Move.ReadValue<Vector2>();

                if (moveInput != Vector2.zero)
                {
                    Vector3 moveDirection = new Vector3(moveInput.x, moveInput.y, 0);
                    targetPosition += moveDirection * moveSpeed * Time.deltaTime;

                    targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
                    targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
                    targetPosition.z = transform.position.z;
                }

                UpdatePosition();
            }
        }

        private void OnToggleManualControl(InputAction.CallbackContext context)
        {
            manualControlEnabled = !manualControlEnabled;

            if (manualControlEnabled)
            {
                // Switching TO manual control
                // Snap camera to player's current position before enabling
                if (playerCamera != null)
                {
                    Vector3 playerPos = playerCamera.transform.position;
                    transform.position = playerPos;
                    targetPosition = playerPos;
                }
            }

            // Toggle camera states
            SetCameraActive(manualControlEnabled);

            Debug.Log($"CameraController: Manual control {(manualControlEnabled ? "ENABLED" : "DISABLED")}");
        }
        
        private void UpdatePosition()
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, 
                targetPosition, 
                ref velocity, 
                smoothTime
            );
        }
        
        private void OnResetCamera(InputAction.CallbackContext context)
        {
            targetPosition = new Vector3(
                SharedConstants.GRID_WIDTH * 0.5f, 
                SharedConstants.GRID_HEIGHT * 0.5f, 
                transform.position.z
            );
        }
    }
}
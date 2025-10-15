using UnityEngine;
using UnityEngine.InputSystem;

namespace MiningGame
{
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = SharedConstants.DEFAULT_CAMERA_MOVE_SPEED;
        [SerializeField] private float smoothTime = SharedConstants.DEFAULT_CAMERA_SMOOTH_TIME;
        
        [Header("Bounds")]
        [SerializeField] private Vector2 minBounds;
        [SerializeField] private Vector2 maxBounds;
        
        private Vector3 velocity = Vector3.zero;
        private Vector3 targetPosition;
        
        private TileEditorInputs inputActions;
        private Vector2 moveInput;
        
        private void Awake()
        {
            inputActions = new TileEditorInputs();
        }
        
        private void OnEnable()
        {
            inputActions.Enable();
            inputActions.CameraMap.ResetCamera.performed += OnResetCamera;
        }
        
        private void OnDisable()
        {
            inputActions.CameraMap.ResetCamera.performed -= OnResetCamera;
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
        }
        
        private void Update()
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
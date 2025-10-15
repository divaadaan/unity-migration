using UnityEngine;
using UnityEngine.InputSystem;

namespace MiningGame
{
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float smoothTime = 0.1f;
        
        [Header("Bounds")]
        [SerializeField] private Vector2 minBounds = new Vector2(-5, -5);
        [SerializeField] private Vector2 maxBounds = new Vector2(25, 25);
        
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
            // Calculate bounds from grid system
            var gridSystem = Object.FindFirstObjectByType<DualGridSystem>();
            if (gridSystem != null)
            {
                minBounds = new Vector2(-2, -2);
                maxBounds = new Vector2(gridSystem.Width + 2, gridSystem.Height + 2);
            }
            targetPosition = transform.position;
        }
        
        private void Update()
        {
            // Read movement input
            moveInput = inputActions.CameraMap.Move.ReadValue<Vector2>();
            
            if (moveInput != Vector2.zero)
            {
                Vector3 moveDirection = new Vector3(moveInput.x, moveInput.y, 0);
                targetPosition += moveDirection * moveSpeed * Time.deltaTime;
                
                // Clamp to bounds
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
            targetPosition = new Vector3(10, 10, transform.position.z);
        }
    }
}
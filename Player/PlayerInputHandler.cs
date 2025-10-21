using UnityEngine;
using UnityEngine.InputSystem;

namespace DigDigDiner
{
    /// <summary>
    /// Handles keyboard input for player movement and digging.
    /// Uses Input System with Arrow Keys for movement and Space for digging.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        private Player player;
        private float lastMoveTime;
        private TileEditorInputs inputActions;

        public void Initialize(Player playerController)
        {
            player = playerController;
            lastMoveTime = -SharedConstants.PLAYER_MOVE_COOLDOWN; // Allow immediate first move
        }

        private void Awake()
        {
            inputActions = new TileEditorInputs();
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("PlayerInputHandler: inputActions is null in OnEnable!");
                return;
            }

            inputActions.Enable();
            inputActions.GameplayMap.Dig.performed += OnDigPerformed;
        }

        private void OnDisable()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("PlayerInputHandler: inputActions is null in OnDisable!");
                return;
            }

            inputActions.GameplayMap.Dig.performed -= OnDigPerformed;
            inputActions.Disable();
        }

        private void Update()
        {
            if (player == null) return;

            // Handle movement input (Arrow Keys via Input System)
            HandleMovementInput();
        }

        private void HandleMovementInput()
        {
            // Check if enough time has passed since last move (for grid-based movement feel)
            if (Time.time - lastMoveTime < SharedConstants.PLAYER_MOVE_COOLDOWN)
                return;

            if (inputActions == null)
                return;

            // Read movement input from Input System
            Vector2 moveInput = inputActions.GameplayMap.Move.ReadValue<Vector2>();

            // Convert to discrete directions (only one direction at a time)
            Vector2Int moveDirection = Vector2Int.zero;

            if (Mathf.Abs(moveInput.y) > Mathf.Abs(moveInput.x))
            {
                // Vertical movement takes priority
                if (moveInput.y > 0.5f)
                    moveDirection = Vector2Int.up;
                else if (moveInput.y < -0.5f)
                    moveDirection = Vector2Int.down;
            }
            else
            {
                // Horizontal movement
                if (moveInput.x > 0.5f)
                    moveDirection = Vector2Int.right;
                else if (moveInput.x < -0.5f)
                    moveDirection = Vector2Int.left;
            }

            // If movement was attempted, try to move
            if (moveDirection != Vector2Int.zero)
            {
                player.TryMove(moveDirection);
                lastMoveTime = Time.time;
            }
        }

        private void OnDigPerformed(InputAction.CallbackContext context)
        {
            if (player != null)
            {
                player.TryDig();
            }
        }
    }
}

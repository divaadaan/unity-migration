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
            lastMoveTime = -SharedConstants.PLAYER_MOVE_COOLDOWN; 
            Debug.Log("PlayerInputHandler: Initialized and ready.");
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
            Debug.Log("PlayerInputHandler: Input Actions Enabled.");
        }

        private void OnDisable()
        {
            if (inputActions == null) return;

            inputActions.GameplayMap.Dig.performed -= OnDigPerformed;
            inputActions.Disable();
        }

        private void Update()
        {
            if (player == null) return;

            // Debug log to prove Update is running
            // (Uncomment if completely stuck, but might span console)
            // Debug.Log("PlayerInputHandler: Update running");

            HandleMovementInput();
        }

        private void HandleMovementInput()
        {
            if (inputActions == null) return;

            // 1. Read Raw Input
            Vector2 moveInput = inputActions.GameplayMap.Move.ReadValue<Vector2>();

            // Debug input if non-zero
            if (moveInput != Vector2.zero)
            {
                // Debug.Log($"PlayerInputHandler: Raw Input {moveInput}");
            }

            // Check cooldown
            if (Time.time - lastMoveTime < SharedConstants.PLAYER_MOVE_COOLDOWN)
                return;

            Vector2Int moveDirection = Vector2Int.zero;

            if (Mathf.Abs(moveInput.y) > Mathf.Abs(moveInput.x))
            {
                if (moveInput.y > 0.5f) moveDirection = Vector2Int.up;
                else if (moveInput.y < -0.5f) moveDirection = Vector2Int.down;
            }
            else
            {
                if (moveInput.x > 0.5f) moveDirection = Vector2Int.right;
                else if (moveInput.x < -0.5f) moveDirection = Vector2Int.left;
            }

            if (moveDirection != Vector2Int.zero)
            {
                Debug.Log($"PlayerInputHandler: Attempting move {moveDirection}");
                player.TryMove(moveDirection);
                lastMoveTime = Time.time;
            }
        }

        private void OnDigPerformed(InputAction.CallbackContext context)
        {
            Debug.Log("PlayerInputHandler: Dig action performed");
            if (player != null)
            {
                player.TryDig();
            }
        }
    }
}
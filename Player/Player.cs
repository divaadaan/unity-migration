using UnityEngine;

namespace DigDigDiner
{
    /// <summary>
    /// Main player controller that orchestrates all player components.
    /// Manages grid position and coordinates between input, movement, digging, and rendering.
    /// </summary>
    public class Player : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DualGridSystem gridSystem;

        [Header("Spawn Settings")]
        [SerializeField] private Vector2Int spawnPosition = new Vector2Int(5, 5);
        [SerializeField] private bool autoFindSpawn = true;

        // Components
        private PlayerInputHandler inputHandler;
        private PlayerMovement movement;
        private PlayerDigging digging;
        private PlayerRenderer playerRenderer;

        // State
        private Vector2Int gridPosition;
        private Vector2Int facingDirection = Vector2Int.down; // Start facing down

        public Vector2Int GridPosition => gridPosition;
        public Vector2Int FacingDirection => facingDirection;
        public DualGridSystem GridSystem => gridSystem;

        private void Awake()
        {
            // Find grid system if not assigned
            if (gridSystem == null)
            {
                gridSystem = FindFirstObjectByType<DualGridSystem>();
                if (gridSystem == null)
                {
                    Debug.LogError("Player: No DualGridSystem found in scene!");
                    enabled = false;
                    return;
                }
            }

            // Get or add components
            inputHandler = GetComponent<PlayerInputHandler>();
            if (inputHandler == null)
                inputHandler = gameObject.AddComponent<PlayerInputHandler>();

            movement = GetComponent<PlayerMovement>();
            if (movement == null)
                movement = gameObject.AddComponent<PlayerMovement>();

            digging = GetComponent<PlayerDigging>();
            if (digging == null)
                digging = gameObject.AddComponent<PlayerDigging>();

            playerRenderer = GetComponent<PlayerRenderer>();
            if (playerRenderer == null)
                playerRenderer = gameObject.AddComponent<PlayerRenderer>();

            // Initialize components
            inputHandler.Initialize(this);
            movement.Initialize(this);
            digging.Initialize(this);
            playerRenderer.Initialize(this);
        }

        private void Start()
        {
            // Wait one frame to ensure map generation completes
            StartCoroutine(SpawnAfterMapGeneration());
        }

        private System.Collections.IEnumerator SpawnAfterMapGeneration()
        {
            // Wait for map generation to complete
            yield return new WaitForEndOfFrame();

            Debug.Log($"Player: Starting spawn process. Grid size: {gridSystem.Width}x{gridSystem.Height}");

            // Find valid spawn position
            if (autoFindSpawn)
            {
                gridPosition = FindValidSpawnPosition();
                Debug.Log($"Player: Auto-found spawn position: {gridPosition}");
            }
            else
            {
                gridPosition = spawnPosition;
                Debug.Log($"Player: Using manual spawn position: {gridPosition}");
            }

            // Validate spawn position
            var tile = gridSystem.GetTileAt(gridPosition.x, gridPosition.y);
            Debug.Log($"Player: Tile at spawn {gridPosition} is {(tile != null ? tile.terrainType.ToString() : "NULL")}");

            if (!IsValidPosition(gridPosition))
            {
                Debug.LogError($"Player: Invalid spawn position {gridPosition}! Tile type: {(tile != null ? tile.terrainType.ToString() : "NULL")}");

                for (int y = gridSystem.Height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < gridSystem.Width; x++)
                    {
                        if (IsValidPosition(new Vector2Int(x, y)))
                        {
                            gridPosition = new Vector2Int(x, y);
                            Debug.LogWarning($"Player: Found fallback spawn at {gridPosition}");
                            goto FoundSpawn;
                        }
                    }
                }
                gridPosition = new Vector2Int(gridSystem.Width / 2, gridSystem.Height / 2);
                Debug.LogError($"Player: No valid spawn found anywhere! Using center: {gridPosition}");
                FoundSpawn:;
            }

            UpdateWorldPosition();

            Debug.Log($"Player spawned at grid position: {gridPosition}, world position: {transform.position}");
        }

        private void Update()
        {
            // Input handler will call TryMove and TryDig based on input
        }

        public void TryMove(Vector2Int direction)
        {
            // Always update facing direction (even if movement is blocked)
            if (direction != Vector2Int.zero)
            {
                facingDirection = direction;
            }

            // Attempt movement
            Vector2Int newPosition = gridPosition + direction;

            if (movement.CanMoveTo(newPosition))
            {
                gridPosition = newPosition;
                UpdateWorldPosition();
                Debug.Log($"Player moved to {gridPosition}, facing {facingDirection}");
            }
            else
            {
                Debug.Log($"Player movement blocked at {newPosition}");
            }
        }

        public void TryDig()
        {
            Vector2Int digTarget = gridPosition + facingDirection;

            if (digging.CanDigAt(digTarget))
            {
                digging.DigAt(digTarget);
                Debug.Log($"Player dug tile at {digTarget}");
            }
            else
            {
                Debug.Log($"Cannot dig at {digTarget}");
            }
        }

        public bool IsValidPosition(Vector2Int pos)
        {
            // Check bounds
            if (pos.x < 0 || pos.x >= gridSystem.Width ||
                pos.y < 0 || pos.y >= gridSystem.Height)
            {
                return false;
            }

            // Check terrain type
            var tile = gridSystem.GetTileAt(pos.x, pos.y);
            return tile != null && tile.terrainType == TerrainType.Empty;
        }

        private void UpdateWorldPosition()
        {
            // Convert grid position to world position
            // Grid positions are at integer coordinates, center the player on them
            transform.position = (Vector2)gridPosition;
        }

        /// <summary>
        /// Finds the first valid Empty tile for spawning.
        /// Prioritizes the entrance area at the top of the map.
        /// </summary>
        private Vector2Int FindValidSpawnPosition()
        {
            int centerX = gridSystem.Width / 2;

            // Search from top of map downward (entrance area)
            // Start from near top and work down
            for (int y = gridSystem.Height - 1; y >= 0; y--)
            {
                // Check center column first (where entrance typically is)
                for (int xOffset = 0; xOffset < 3; xOffset++)
                {
                    int x = centerX + (xOffset % 2 == 0 ? xOffset / 2 : -(xOffset / 2 + 1));

                    if (x >= 0 && x < gridSystem.Width)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (IsValidPosition(pos))
                        {
                            Debug.Log($"Player: Found spawn at entrance area: {pos}");
                            return pos;
                        }
                    }
                }
            }

            // Fallback: search entire grid from top to bottom
            for (int y = gridSystem.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < gridSystem.Width; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (IsValidPosition(pos))
                        return pos;
                }
            }

            Debug.LogWarning("Player: No valid spawn position found! Using fallback");
            return new Vector2Int(centerX, gridSystem.Height - 2);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Draw current grid position
            Gizmos.color = Color.green;
            Vector3 gridWorldPos = new Vector3(gridPosition.x, gridPosition.y, 0);
            Gizmos.DrawWireCube(gridWorldPos, Vector3.one * 0.8f);

            // Draw facing direction
            Gizmos.color = Color.yellow;
            Vector3 facingEnd = gridWorldPos + new Vector3(facingDirection.x, facingDirection.y, 0) * 0.5f;
            Gizmos.DrawLine(gridWorldPos, facingEnd);

            // Draw dig target
            Vector2Int digTarget = gridPosition + facingDirection;
            if (digging != null && digging.CanDigAt(digTarget))
            {
                Gizmos.color = Color.cyan;
                Vector3 digWorldPos = new Vector3(digTarget.x, digTarget.y, 0);
                Gizmos.DrawWireCube(digWorldPos, Vector3.one * 0.9f);
            }
        }
#endif
    }
}

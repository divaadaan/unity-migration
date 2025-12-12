using UnityEngine;
using DigDigDiner;

/// <summary>
/// Handles player spawn positioning for the Character Scripts player system.
/// Finds a valid Empty tile in the DualGridSystem and positions the player there.
/// Waits for map generation to complete before spawning.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DualGridSystem gridSystem;

    [Header("Spawn Settings")]
    [SerializeField] private Vector2Int manualSpawnPosition = new Vector2Int(5, 5);
    [SerializeField] private bool autoFindSpawn = true;
    [Tooltip("Search from top of map downward to find entrance area")]
    [SerializeField] private bool prioritizeTopOfMap = true;

    [Header("Debug")]
    [SerializeField] private bool logSpawnProcess = true;

    private void Awake()
    {
        // Auto-find grid system if not assigned
        if (gridSystem == null)
        {
            gridSystem = FindFirstObjectByType<DualGridSystem>();
            if (gridSystem == null)
            {
                Debug.LogError("PlayerSpawner: No DualGridSystem found in scene!");
                enabled = false;
                return;
            }
        }
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

        if (logSpawnProcess)
            Debug.Log($"PlayerSpawner: Starting spawn process. Grid size: {gridSystem.Width}x{gridSystem.Height}");

        // Find valid spawn position
        Vector2Int gridPosition;
        if (autoFindSpawn)
        {
            gridPosition = FindValidSpawnPosition();
            if (logSpawnProcess)
                Debug.Log($"PlayerSpawner: Auto-found spawn position: {gridPosition}");
        }
        else
        {
            gridPosition = manualSpawnPosition;
            if (logSpawnProcess)
                Debug.Log($"PlayerSpawner: Using manual spawn position: {gridPosition}");
        }

        // Validate and fallback if needed
        if (!IsValidSpawnPosition(gridPosition))
        {
            Debug.LogWarning($"PlayerSpawner: Invalid spawn position {gridPosition}, searching for fallback...");
            gridPosition = FindAnyValidPosition();
        }

        // Position player at spawn (grid coordinates = world coordinates)
        Vector3 spawnWorldPos = new Vector3(gridPosition.x, gridPosition.y, 0);
        transform.position = spawnWorldPos;

        if (logSpawnProcess)
            Debug.Log($"PlayerSpawner: Player spawned at grid {gridPosition}, world position {spawnWorldPos}");
    }

    /// <summary>
    /// Finds the first valid Empty tile for spawning.
    /// Prioritizes the entrance area at the top of the map.
    /// </summary>
    private Vector2Int FindValidSpawnPosition()
    {
        if (prioritizeTopOfMap)
        {
            int centerX = gridSystem.Width / 2;

            // Search from top of map downward (entrance area)
            for (int y = gridSystem.Height - 1; y >= 0; y--)
            {
                // Check center column first (where entrance typically is)
                // Expands outward: center, center+1, center-1
                for (int xOffset = 0; xOffset < 3; xOffset++)
                {
                    int x = centerX + (xOffset % 2 == 0 ? xOffset / 2 : -(xOffset / 2 + 1));

                    if (x >= 0 && x < gridSystem.Width)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (IsValidSpawnPosition(pos))
                        {
                            if (logSpawnProcess)
                                Debug.Log($"PlayerSpawner: Found spawn at entrance area: {pos}");
                            return pos;
                        }
                    }
                }
            }
        }

        // Fallback: search entire grid
        return FindAnyValidPosition();
    }

    /// <summary>
    /// Searches entire grid from top to bottom for any valid spawn position.
    /// </summary>
    private Vector2Int FindAnyValidPosition()
    {
        for (int y = gridSystem.Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < gridSystem.Width; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (IsValidSpawnPosition(pos))
                {
                    if (logSpawnProcess)
                        Debug.Log($"PlayerSpawner: Found fallback spawn at {pos}");
                    return pos;
                }
            }
        }

        // Last resort: use center of map
        int centerX = gridSystem.Width / 2;
        int centerY = gridSystem.Height / 2;
        Debug.LogWarning($"PlayerSpawner: No valid spawn found! Using center: ({centerX}, {centerY})");
        return new Vector2Int(centerX, centerY);
    }

    /// <summary>
    /// Checks if a position is valid for spawning (must be Empty terrain).
    /// </summary>
    private bool IsValidSpawnPosition(Vector2Int pos)
    {
        // Check bounds
        if (pos.x < 0 || pos.x >= gridSystem.Width ||
            pos.y < 0 || pos.y >= gridSystem.Height)
        {
            return false;
        }

        // Check terrain type - can only spawn on Empty tiles
        var tile = gridSystem.GetTileAt(pos.x, pos.y);
        return tile != null && tile.terrainType == TerrainType.Empty;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (gridSystem == null || !Application.isPlaying)
            return;

        // Draw manual spawn position
        if (!autoFindSpawn)
        {
            Gizmos.color = Color.green;
            Vector3 spawnPos = new Vector3(manualSpawnPosition.x, manualSpawnPosition.y, 0);
            Gizmos.DrawWireSphere(spawnPos, 0.5f);
            Gizmos.DrawWireCube(spawnPos, Vector3.one * 0.8f);
        }

        // Draw current player position on grid
        Vector2Int currentGridPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y)
        );
        Gizmos.color = Color.cyan;
        Vector3 gridWorldPos = new Vector3(currentGridPos.x, currentGridPos.y, 0);
        Gizmos.DrawWireCube(gridWorldPos, Vector3.one * 0.9f);
    }
#endif
}

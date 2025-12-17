using UnityEngine;
using DigDigDiner;

public class PlayerSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DualGridSystem gridSystem;
    [SerializeField] private PlayerCamera playerCamera; 

    [Header("Assets")]
    [SerializeField] private GameObject playerPrefab; 

    [Header("Spawn Settings")]
    [SerializeField] private Vector2Int manualSpawnPosition = new Vector2Int(5, 5);
    [SerializeField] private bool autoFindSpawn = true;
    [SerializeField] private bool prioritizeTopOfMap = true;

    [Header("Debug")]
    [SerializeField] private bool logSpawnProcess = true;

    private void Awake()
    {
        if (gridSystem == null)
            gridSystem = FindFirstObjectByType<DualGridSystem>();
            
        // Auto-find camera if missed
        if (playerCamera == null)
            playerCamera = FindFirstObjectByType<PlayerCamera>();
    }

    private void Start()
    {
        StartCoroutine(SpawnAfterMapGeneration());
    }

    private System.Collections.IEnumerator SpawnAfterMapGeneration()
    {
        yield return new WaitForEndOfFrame();

        if (logSpawnProcess) Debug.Log($"PlayerSpawner: Finding spawn point...");

        // 1. Find Position
        Vector2Int gridPosition;
        if (autoFindSpawn)
        {
            gridPosition = FindValidSpawnPosition();
        }
        else
        {
            gridPosition = manualSpawnPosition;
        }

        // 2. Instantiate Prefab
        Vector3 spawnWorldPos = gridSystem.BaseGridToWorld(gridPosition);
        GameObject newPlayer = Instantiate(playerPrefab, spawnWorldPos, Quaternion.identity);
        newPlayer.name = "Player_Active";

        if (logSpawnProcess) Debug.Log($"PlayerSpawner: Spawned {newPlayer.name} at {gridPosition}");

        // 3. Link to Camera
        if (playerCamera != null)
        {
            PlayerMovement mover = newPlayer.GetComponentInChildren<PlayerMovement>();

            if (mover != null)
            {
                playerCamera.SetTarget(mover.transform); 
                Debug.Log($"PlayerSpawner: Linked Camera to {mover.name}");
            }
            else
            {
                // Fallback if script is missing (tracks root)
                playerCamera.SetTarget(newPlayer.transform);
                Debug.LogWarning("PlayerSpawner: Could not find PlayerMovement script on prefab!");
            }
        }
        else
        {
            Debug.LogError("PlayerSpawner: No PlayerCamera assigned!");
        }
    }

    private Vector2Int FindValidSpawnPosition()
    {
        if (prioritizeTopOfMap)
        {
            int centerX = gridSystem.Width / 2;
            for (int y = gridSystem.Height - 1; y >= 0; y--)
            {
                for (int xOffset = 0; xOffset < 3; xOffset++)
                {
                    int x = centerX + (xOffset % 2 == 0 ? xOffset / 2 : -(xOffset / 2 + 1));
                    if (x >= 0 && x < gridSystem.Width && IsValidSpawnPosition(new Vector2Int(x, y)))
                        return new Vector2Int(x, y);
                }
            }
        }
        return FindAnyValidPosition();
    }

    private Vector2Int FindAnyValidPosition()
    {
        for (int y = gridSystem.Height - 1; y >= 0; y--)
        {
            for (int x = 0; x < gridSystem.Width; x++)
            {
                if (IsValidSpawnPosition(new Vector2Int(x, y))) return new Vector2Int(x, y);
            }
        }
        return new Vector2Int(gridSystem.Width / 2, gridSystem.Height / 2);
    }

    private bool IsValidSpawnPosition(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= gridSystem.Width || pos.y < 0 || pos.y >= gridSystem.Height) return false;
        var tile = gridSystem.GetTileAt(pos.x, pos.y);
        return tile != null && tile.terrainType == TerrainType.Empty;
    }
}
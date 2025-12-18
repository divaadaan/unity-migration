using UnityEngine;
using DigDigDiner;

/// <summary>
/// Handles beat-synced tile digging for the Character Scripts player system.
/// Bridges between PlayerMovement's physics-based tool detection and DualGridSystem's tile data.
/// Applies damage to tiles on each music beat (130 BPM) when drilling.
/// </summary>
public class BeatDigging : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private DualGridSystem gridSystem;
    [SerializeField] private Transform toolCheckPos;
    [SerializeField] private LayerMask groundLayer;

    [Header("Beat Timing")]
    private const float BPM = 130f;
    private float beatInterval; // Calculated from BPM
    private float nextBeatTime = 0f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool logDigEvents = false;

    private void Awake()
    {
        // 1. Find PlayerMovement 
        {
            playerMovement = GetComponentInChildren<PlayerMovement>();
            if (playerMovement == null) 
                Debug.LogError("BeatDigging: Could not find PlayerMovement script on this object or children!");
        }

        // 2. Find the Mining Grid
        if (gridSystem == null)
        {
            GameObject miningObject = GameObject.Find("MiningGameSystem");
            if (miningObject != null)
            {
                gridSystem = miningObject.GetComponent<DualGridSystem>();
            }
            else
            {
                // Fallback 
                Debug.LogError("BeatDigging: Could not find 'MiningGameSystem'. searching for ANY grid...");
                gridSystem = FindFirstObjectByType<DualGridSystem>();
            }

            if (gridSystem == null)
                Debug.LogError("BeatDigging: Critical Error - No Grid System found!");
        }

        // 3. Find Tool Position (Copy from PlayerMovement if we lost the reference)
        if (toolCheckPos == null && playerMovement != null)
        {
            toolCheckPos = playerMovement.toolCheckPos;
        }

        // 4. Find Ground Layer (Copy from PlayerMovement)
        if (groundLayer == 0 && playerMovement != null)
        {
            groundLayer = playerMovement.groundLayer;
        }

        // Calculate beat interval
        beatInterval = 60f / BPM;
        nextBeatTime = Time.time + beatInterval;
    }

    private void Update()
    {
        if (playerMovement == null || gridSystem == null)
            return;

        // Only process if player is drilling and tool is hitting something
        if (!playerMovement.isDrilling || !ToolCheck())
            return;

        // Check if it's time for the next beat
        if (Time.time >= nextBeatTime)
        {
            ApplyDigDamage();
            nextBeatTime = Time.time + beatInterval;
        }
    }

    /// <summary>
    /// Applies damage to the tile at the tool check position.
    /// </summary>
    private void ApplyDigDamage()
        {
            // 1. Convert tool world position to grid coordinates
            Vector2Int gridPos = new Vector2Int(
                Mathf.RoundToInt(toolCheckPos.position.x), 
                Mathf.RoundToInt(toolCheckPos.position.y)
            );

            // 2. Validate grid position 
            if (gridPos.x < 0 || gridPos.x >= gridSystem.Width ||
                gridPos.y < 0 || gridPos.y >= gridSystem.Height)
            {
                if (logDigEvents)
                    Debug.LogWarning($"BeatDigging: Tool position {toolCheckPos.position} is outside grid bounds");
                return;
            }

            Tile tile = gridSystem.GetTileAt(gridPos.x, gridPos.y);
            if (tile == null || tile.terrainType != TerrainType.Diggable)
                return;

            bool tileDestroyed = tile.TakeDamage(1);

            if (logDigEvents)
            {
                Debug.Log($"BeatDigging: Hit tile at {gridPos}, HP: {tile.currentHitPoints}/{tile.maxHitPoints}");
            }

            if (tileDestroyed)
            {
                gridSystem.SetTileAt(gridPos.x, gridPos.y, new Tile(TerrainType.Empty));

                if (logDigEvents)
                    Debug.Log($"BeatDigging: Destroyed tile at {gridPos}");
            }
        }

    /// <summary>
    /// Check if tool is colliding with ground layer.
    /// </summary>
    private bool ToolCheck()
    {
        if (toolCheckPos == null) return false;
        return Physics2D.OverlapBox(toolCheckPos.position, new Vector2(0.25f, 0.25f), 0, groundLayer);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || toolCheckPos == null) return;

        Gizmos.color = playerMovement != null && playerMovement.isDrilling ? Color.red : Color.yellow;
        Gizmos.DrawWireCube(toolCheckPos.position, new Vector3(0.25f, 0.25f, 1f));

        if (gridSystem != null && playerMovement != null && playerMovement.isDrilling && ToolCheck())
        {
            Vector2Int gridPos = gridSystem.WorldToBaseGrid(toolCheckPos.position);
            Vector3 targetWorldPos = gridSystem.BaseGridToWorld(gridPos);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(targetWorldPos, Vector3.one * 0.9f);
        }
    }
#endif
}

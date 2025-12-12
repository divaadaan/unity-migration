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
        // Auto-find references if not assigned
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (gridSystem == null)
            gridSystem = FindFirstObjectByType<DualGridSystem>();

        if (toolCheckPos == null && playerMovement != null)
            toolCheckPos = playerMovement.toolCheckPos;

        if (groundLayer == 0 && playerMovement != null)
            groundLayer = playerMovement.groundLayer;

        // Calculate beat interval: 60 seconds / 130 BPM = 0.4615 seconds per beat
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
        // Convert tool world position to grid coordinates
        Vector2Int gridPos = WorldToGridPosition(toolCheckPos.position);

        // Validate grid position
        if (gridPos.x < 0 || gridPos.x >= gridSystem.Width ||
            gridPos.y < 0 || gridPos.y >= gridSystem.Height)
        {
            if (logDigEvents)
                Debug.LogWarning($"BeatDigging: Tool position {toolCheckPos.position} is outside grid bounds");
            return;
        }

        // Get the tile
        Tile tile = gridSystem.GetTileAt(gridPos.x, gridPos.y);

        if (tile == null || tile.terrainType != TerrainType.Diggable)
            return;

        // Apply damage
        bool tileDestroyed = tile.TakeDamage(1);

        if (logDigEvents)
        {
            Debug.Log($"BeatDigging: Hit tile at {gridPos}, HP: {tile.currentHitPoints}/{tile.maxHitPoints}");
        }

        // If tile destroyed, convert to Empty
        if (tileDestroyed)
        {
            Tile emptyTile = new Tile(TerrainType.Empty);
            gridSystem.SetTileAt(gridPos.x, gridPos.y, emptyTile);

            if (logDigEvents)
                Debug.Log($"BeatDigging: Destroyed tile at {gridPos}");
        }
    }

    /// <summary>
    /// Converts world position to grid coordinates (rounds to nearest integer).
    /// </summary>
    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
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

        // Draw tool check area
        Gizmos.color = playerMovement != null && playerMovement.isDrilling ? Color.red : Color.yellow;
        Gizmos.DrawWireCube(toolCheckPos.position, new Vector3(0.25f, 0.25f, 1f));

        // Draw grid position being targeted
        if (playerMovement != null && playerMovement.isDrilling && ToolCheck())
        {
            Vector2Int gridPos = WorldToGridPosition(toolCheckPos.position);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector3(gridPos.x, gridPos.y, 0), Vector3.one * 0.9f);
        }
    }
#endif
}

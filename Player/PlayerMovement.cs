using UnityEngine;

namespace DigDigDiner
{
    /// <summary>
    /// Handles player movement logic and collision detection with the grid.
    /// Player can only move through Empty tiles.
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        private Player player;
        private DualGridSystem gridSystem;

        public void Initialize(Player playerController)
        {
            player = playerController;
            gridSystem = player.GridSystem;
        }

        /// <summary>
        /// Checks if the player can move to the specified grid position.
        /// </summary>
        public bool CanMoveTo(Vector2Int targetPosition)
        {
            if (gridSystem == null)
            {
                Debug.LogWarning("PlayerMovement: GridSystem is null!");
                return false;
            }

            // Check grid boundaries
            if (targetPosition.x < 0 || targetPosition.x >= gridSystem.Width ||
                targetPosition.y < 0 || targetPosition.y >= gridSystem.Height)
            {
                return false; // Out of bounds
            }

            // Check terrain type - can only move into Empty tiles
            var tile = gridSystem.GetTileAt(targetPosition.x, targetPosition.y);

            if (tile == null)
            {
                Debug.LogWarning($"PlayerMovement: Tile at {targetPosition} is null!");
                return false;
            }

            // Only Empty tiles are walkable
            return tile.terrainType == TerrainType.Empty;
        }

        /// <summary>
        /// Gets the terrain type at the specified position.
        /// </summary>
        public TerrainType GetTerrainAt(Vector2Int position)
        {
            if (gridSystem == null) return TerrainType.Undiggable;

            // Check bounds
            if (position.x < 0 || position.x >= gridSystem.Width ||
                position.y < 0 || position.y >= gridSystem.Height)
            {
                return TerrainType.Undiggable; // Treat out-of-bounds as undiggable
            }

            var tile = gridSystem.GetTileAt(position.x, position.y);
            return tile != null ? tile.terrainType : TerrainType.Undiggable;
        }
    }
}

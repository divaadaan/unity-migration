using UnityEngine;

namespace DigDigDiner
{
    /// <summary>
    /// Handles player digging mechanics.
    /// Player can dig Diggable tiles in the facing direction, converting them to Empty.
    /// </summary>
    public class PlayerDigging : MonoBehaviour
    {
        private Player player;
        private DualGridSystem gridSystem;

        public void Initialize(Player playerController)
        {
            player = playerController;
            gridSystem = player.GridSystem;
        }

        /// <summary>
        /// Checks if the player can dig at the specified grid position.
        /// </summary>
        public bool CanDigAt(Vector2Int targetPosition)
        {
            if (gridSystem == null)
            {
                Debug.LogWarning("PlayerDigging: GridSystem is null!");
                return false;
            }

            // Check grid boundaries
            if (targetPosition.x < 0 || targetPosition.x >= gridSystem.Width ||
                targetPosition.y < 0 || targetPosition.y >= gridSystem.Height)
            {
                return false; // Out of bounds
            }

            // Check terrain type - can only dig Diggable tiles
            var tile = gridSystem.GetTileAt(targetPosition.x, targetPosition.y);

            if (tile == null)
            {
                Debug.LogWarning($"PlayerDigging: Tile at {targetPosition} is null!");
                return false;
            }

            // Only Diggable tiles can be dug
            return tile.terrainType == TerrainType.Diggable;
        }

        /// <summary>
        /// Digs the tile at the specified position, converting it from Diggable to Empty.
        /// </summary>
        public void DigAt(Vector2Int targetPosition)
        {
            if (!CanDigAt(targetPosition))
            {
                Debug.LogWarning($"PlayerDigging: Cannot dig at {targetPosition}");
                return;
            }

            // Convert Diggable tile to Empty
            Tile newTile = new Tile(TerrainType.Empty);
            gridSystem.SetTileAt(targetPosition.x, targetPosition.y, newTile);

        }

        /// <summary>
        /// Gets the current dig target position based on player position and facing direction.
        /// </summary>
        public Vector2Int GetDigTarget()
        {
            return player.GridPosition + player.FacingDirection;
        }
    }
}

using UnityEngine;

namespace DigDigDiner
{
    [System.Serializable]
    public class Tile
    {
        [Header("Core Properties")]
        public int stateIndex;

        public TerrainType terrainType
        {
            get => (TerrainType)stateIndex;
            set => stateIndex = (int)value;
        }

        [Header("Hitpoints (Beat-Synced)")]
        public int maxHitPoints = 3; // Default: 3 beats to destroy
        public int currentHitPoints;

        public Tile(int state)
        {
            stateIndex = state;
            InitializeHitPoints();
        }

        public Tile(TerrainType type)
        {
            stateIndex = (int)type;
            InitializeHitPoints();
        }

        private void InitializeHitPoints()
        {
            // Set hitpoints based on terrain type
            switch (terrainType)
            {
                case TerrainType.Diggable:
                    maxHitPoints = 3; // 3 beats to dig through
                    currentHitPoints = maxHitPoints;
                    break;
                case TerrainType.Empty:
                case TerrainType.Undiggable:
                default:
                    maxHitPoints = 0;
                    currentHitPoints = 0;
                    break;
            }
        }

        /// <summary>
        /// Apply damage to this tile (called on each beat when drilling)
        /// </summary>
        /// <returns>True if tile is destroyed (should become Empty)</returns>
        public bool TakeDamage(int damage = 1)
        {
            if (terrainType != TerrainType.Diggable)
                return false;

            currentHitPoints -= damage;

            if (currentHitPoints <= 0)
            {
                // Tile destroyed - caller should convert to Empty via DualGridSystem.SetTileAt()
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get normalized health (0.0 to 1.0) for visual feedback
        /// </summary>
        public float GetHealthPercent()
        {
            if (maxHitPoints == 0) return 0f;
            return (float)currentHitPoints / maxHitPoints;
        }

        /// <summary>
        /// Reset tile to full health (used when re-digging)
        /// </summary>
        public void ResetHealth()
        {
            InitializeHitPoints();
        }
    }
}
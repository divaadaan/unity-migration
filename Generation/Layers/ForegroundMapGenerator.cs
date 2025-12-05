using UnityEngine;

namespace DigDigDiner
{
    /// <summary>
    /// Generates foreground occlusion tiles based on the layout of a source grid (Mining Layer).
    /// Maps Undiggable mining tiles to Active foreground tiles with configurable coverage.
    /// </summary>
    public class ForegroundMapGenerator : MonoBehaviour
    {
        [Header("Grid References")]
        [Tooltip("The source grid to read from (The Mining Layer)")]
        [SerializeField] private DualGridSystem sourceGrid;
        
        [Tooltip("The target grid to write to (The Foreground Layer)")]
        [SerializeField] private DualGridSystem targetGrid;

        [Header("Generation Rules")]
        [Tooltip("The terrain type in the source grid that triggers an FG tile.")]
        [SerializeField] private TerrainType triggerType = TerrainType.Undiggable;

        [Tooltip("The state index to set on the FG grid when triggered (usually 1).")]
        [SerializeField] private int activeStateIndex = 1;
        
        [Tooltip("The state index to set on the FG grid when NOT triggered (usually 0).")]
        [SerializeField] private int emptyStateIndex = 0;

        [Header("Variation")]
        [Tooltip("Chance (0-1) that an FG tile spawns on a valid trigger. < 1.0 creates a 'worn' look.")]
        [Range(0f, 1f)]
        [SerializeField] private float coverageRatio = 1.0f;
        
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool useDirectorSeed = true;

        /// <summary>
        /// Generates the foreground layer.
        /// </summary>
        /// <param name="externalSeed">Optional seed from the Director for consistent world generation.</param>
        public void Generate(int? externalSeed = null)
        {
            if (sourceGrid == null || targetGrid == null)
            {
                Debug.LogError("ForegroundMapGenerator: Missing grid references!");
                return;
            }

            // Use external seed if provided and enabled, otherwise use local seed
            int currentSeed = (useDirectorSeed && externalSeed.HasValue) ? externalSeed.Value : seed;
            System.Random prng = new System.Random(currentSeed);

            Debug.Log($"ForegroundMapGenerator: Generating FG layer (Seed: {currentSeed})...");

            // Iterate over the grid dimensions
            for (int x = 0; x < targetGrid.Width; x++)
            {
                for (int y = 0; y < targetGrid.Height; y++)
                {
                    int targetState = emptyStateIndex;

                    if (x < sourceGrid.Width && y < sourceGrid.Height)
                    {
                        Tile sourceTile = sourceGrid.GetTileAt(x, y);

                        if (sourceTile != null && sourceTile.terrainType == triggerType)
                        {
                            if (prng.NextDouble() <= coverageRatio)
                            {
                                targetState = activeStateIndex;
                            }
                        }
                    }

                    targetGrid.SetTileAtSilent(x, y, new Tile(targetState));
                }
            }

            targetGrid.RefreshAllVisualTiles();
            Debug.Log("ForegroundMapGenerator: Generation Complete.");
        }
    }
}
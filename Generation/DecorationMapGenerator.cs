using UnityEngine;

namespace DigDigDiner
{
    /// <summary>
    /// Generates decoration tiles that overlay specific terrain types in the mining layer.
    /// </summary>
    public class DecorationMapGenerator : MonoBehaviour
    {
        [Header("Grid References")]
        [Tooltip("The source grid to read from (MiningGameSystem)")]
        [SerializeField] private DualGridSystem sourceGrid;
        
        [Tooltip("The target grid to write to (Decoration)")]
        [SerializeField] private DualGridSystem targetGrid;

        [Header("Generation Rules")]
        [Tooltip("Decoration tiles will spawn on top of these terrain types.")]
        [SerializeField] private bool overlayDiggable = true;
        [SerializeField] private bool overlayUndiggable = true;

        [Header("Settings")]
        [Tooltip("State index for the decoration tile (1 for Active).")]
        [SerializeField] private int activeStateIndex = 1;
        
        [Tooltip("State index for empty space (usually 0).")]
        [SerializeField] private int emptyStateIndex = 0;
       
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool useDirectorSeed = true;

        /// <summary>
        /// Generates the decoration layer based on the source grid layout.
        /// </summary>
        public void Generate(int? externalSeed = null)
        {
            if (sourceGrid == null || targetGrid == null)
            {
                Debug.LogError($"DecorationMapGenerator ({name}): Missing grid references!");
                return;
            }

            int currentSeed = (useDirectorSeed && externalSeed.HasValue) ? externalSeed.Value : seed;

            Debug.Log($"DecorationMapGenerator: Generating layer (Seed: {currentSeed})...");

            for (int x = 0; x < targetGrid.Width; x++)
            {
                for (int y = 0; y < targetGrid.Height; y++)
                {
                    int targetState = emptyStateIndex;

                    // Ensure we are within bounds of the source grid
                    if (x < sourceGrid.Width && y < sourceGrid.Height)
                    {
                        Tile sourceTile = sourceGrid.GetTileAt(x, y);

                        if (sourceTile != null)
                        {
                            bool isValidTerrain = 
                                (overlayDiggable && sourceTile.terrainType == TerrainType.Diggable) ||
                                (overlayUndiggable && sourceTile.terrainType == TerrainType.Undiggable);

                            if (isValidTerrain)
                            {
                                targetState = activeStateIndex;
                            }
                        }
                    }

                    targetGrid.SetTileAtSilent(x, y, new Tile(targetState));
                }
            }

            targetGrid.RefreshAllVisualTiles();
            Debug.Log($"DecorationMapGenerator: Generation Complete for {name}.");
        }
    }
}
using UnityEngine;

namespace DigDigDiner
{
    /// <summary>
    /// Fills the entire target grid with decoration tiles, using the source grid only for dimensions.
    /// </summary>
    public class DistantDecorationMapGenerator : MonoBehaviour
    {
        [Header("Grid References")]
        [Tooltip("Used only to determine grid dimensions.")]
        [SerializeField] private DualGridSystem sourceGrid;
        
        [Tooltip("The target grid to fill (Distant Decoration).")]
        [SerializeField] private DualGridSystem targetGrid;

        [Header("Settings")]
        [Tooltip("The tile state to apply to every cell (default 1 for Active).")]
        [SerializeField] private int decorationState = 1;

        public void Generate(int? externalSeed = null)
        {
            if (sourceGrid == null || targetGrid == null)
            {
                Debug.LogError("DistantDecorationMapGenerator: Missing grid references!");
                return;
            }

            Debug.Log($"DistantDecorationMapGenerator: Filling {targetGrid.name} with state {decorationState}...");

            // Loop through the entire grid dimensions
            for (int x = 0; x < sourceGrid.Width; x++)
            {
                for (int y = 0; y < sourceGrid.Height; y++)
                {
                    targetGrid.SetTileAtSilent(x, y, new Tile(decorationState));
                }
            }

            targetGrid.RefreshAllVisualTiles();
            Debug.Log("DistantDecorationMapGenerator: Generation Complete.");
        }
    }
}
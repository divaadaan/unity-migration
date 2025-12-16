using UnityEngine;
using UnityEngine.Tilemaps;

namespace DigDigDiner
{
    /// <summary>
    /// Generates decoration tiles based on the VERTEX state of the mining layer.
    /// Places tiles directly on the integer coordinates, aligning with the Debug Grid.
    /// </summary>
    public class DecorationMapGenerator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The source grid to read from (MiningGameSystem)")]
        [SerializeField] private DualGridSystem sourceGrid;
        
        [Tooltip("The standard Unity Tilemap to write to (Remove DualGridSystem from this object)")]
        [SerializeField] private Tilemap targetTilemap;

        [Header("Assets")]
        [Tooltip("The visual tile asset to place at active locations.")]
        [SerializeField] private TileBase decorationTile;

        [Header("Generation Rules")]
        [Tooltip("Decoration tiles will spawn on top of these terrain types.")]
        [SerializeField] private bool overlayDiggable = true;
        [SerializeField] private bool overlayUndiggable = true;
       
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool useDirectorSeed = true;

        public void Generate(int? externalSeed = null)
        {
            if (sourceGrid == null || targetTilemap == null || decorationTile == null)
            {
                Debug.LogError($"DecorationMapGenerator ({name}): Missing references!");
                return;
            }

            int currentSeed = (useDirectorSeed && externalSeed.HasValue) ? externalSeed.Value : seed;
            Debug.Log($"DecorationMapGenerator: Generating Vertex Layer (Seed: {currentSeed})...");

            // Clear the existing standard tilemap
            targetTilemap.ClearAllTiles();

            for (int x = 0; x < sourceGrid.Width; x++)
            {
                for (int y = 0; y < sourceGrid.Height; y++)
                {
                    // Check the vertex state from the Source Grid
                    Tile sourceTile = sourceGrid.GetTileAt(x, y);

                    if (sourceTile != null)
                    {
                        bool shouldPlace = 
                            (overlayDiggable && sourceTile.terrainType == TerrainType.Diggable) ||
                            (overlayUndiggable && sourceTile.terrainType == TerrainType.Undiggable);

                        if (shouldPlace)
                        {
                            // Place the tile directly at the integer coordinate (Vertex)
                            // This matches the behavior of the Debug Tilemap
                            targetTilemap.SetTile(new Vector3Int(x, y, 0), decorationTile);
                        }
                    }
                }
            }

            Debug.Log($"DecorationMapGenerator: Generation Complete for {name}.");
        }
    }
}
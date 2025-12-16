using UnityEngine;
using UnityEngine.Tilemaps;

namespace DigDigDiner
{
    /// <summary>
    /// Links a Standard Tilemap to a governing DualGridSystem.
    /// </summary>
    public class GridDependency : MonoBehaviour
    {
        [Header("Linkage")]
        [Tooltip("The governing grid (Mining Layer)")]
        [SerializeField] private DualGridSystem sourceGrid;

        [Tooltip("The dependent standard tilemap (Decoration Layer)")]
        [SerializeField] private Tilemap targetTilemap;

        [Header("Rules")]
        [Tooltip("If the Source tile becomes Empty, delete the decoration at that location.")]
        [SerializeField] private bool clearOnSourceEmpty = true;

        private void OnEnable()
        {
            if (sourceGrid != null)
            {
                sourceGrid.OnTileChanged += OnSourceTileChanged;
            }
        }

        private void OnDisable()
        {
            if (sourceGrid != null)
            {
                sourceGrid.OnTileChanged -= OnSourceTileChanged;
            }
        }

        private void OnSourceTileChanged(int x, int y, Tile newSourceTile)
        {
            if (targetTilemap == null) return;

            // Rule: Clear target if source becomes Empty
            if (clearOnSourceEmpty && newSourceTile.terrainType == TerrainType.Empty)
            {
                // Remove the tile from the standard Tilemap
                targetTilemap.SetTile(new Vector3Int(x, y, 0), null);
            }
        }
    }
}
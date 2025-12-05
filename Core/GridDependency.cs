using UnityEngine;

namespace DigDigDiner
{
    /// <summary>
    /// Links a dependent grid (Target) to a governing grid (Source).
    /// When the Source grid updates, the Target grid reacts accordingly.
    /// </summary>
    public class GridDependency : MonoBehaviour
    {
        [Header("Linkage")]
        [Tooltip("The governing grid")]
        [SerializeField] private DualGridSystem sourceGrid;

        [Tooltip("The dependent grid")]
        [SerializeField] private DualGridSystem targetGrid;

        [Header("Rules")]
        [Tooltip("If the Source tile becomes Empty, clear the Target tile.")]
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
            if (targetGrid == null) return;

            // Rule: Clear target if source becomes Empty
            if (clearOnSourceEmpty && newSourceTile.terrainType == TerrainType.Empty)
            {
                // We set the target to Empty. 
                targetGrid.SetTileAt(x, y, new Tile(TerrainType.Empty));
            }
        }
    }
}
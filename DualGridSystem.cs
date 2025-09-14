using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace MiningGame
{
    /// <summary>
    /// Manages the dual-grid system with base (logic) and draw (visual) layers
    /// </summary>
    public class DualGridSystem : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private int gridWidth = 20;
        [SerializeField] private int gridHeight = 24;
        
        [Header("Tilemap References")]
        [Tooltip("Visual tilemap for color tiles (offset by 0.5, 0.5)")]
        [SerializeField] private Tilemap colorTilemap;
        
        [Tooltip("Visual tilemap for normal map tiles (offset by 0.5, 0.5)")]
        [SerializeField] private Tilemap normalTilemap;
        
        [Tooltip("Optional debug tilemap to show base grid")]
        [SerializeField] private Tilemap debugBaseTilemap;
        
        [Header("Tile System")]
        [SerializeField] private TilePatternSystem tilePatternSystem;
        
        [Header("Visual Settings")]
        [SerializeField] private Vector2 visualOffset = new Vector2(0.5f, 0.5f);
        [SerializeField] private bool showDebugGrid = false;
        
        // Base grid data (logic layer)
        private TerrainType[,] baseGrid;
        
        // Properties
        public int Width => gridWidth;
        public int Height => gridHeight;
        public TerrainType[,] BaseGrid => baseGrid;
        
        private void Awake()
        {
            InitializeGrid();
            
            if (tilePatternSystem != null)
            {
                tilePatternSystem.InitializeLookup();
            }
        }
        
        /// <summary>
        /// Initialize the base grid with empty terrain
        /// </summary>
        private void InitializeGrid()
        {
            baseGrid = new TerrainType[gridHeight, gridWidth];
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    baseGrid[y, x] = TerrainType.Empty;
                }
            }
        }
        
        /// <summary>
        /// Set the entire base grid from external data
        /// </summary>
        public void SetBaseGrid(TerrainType[,] newGrid)
        {
            if (newGrid == null) return;
            
            gridHeight = newGrid.GetLength(0);
            gridWidth = newGrid.GetLength(1);
            baseGrid = newGrid;
            
            RefreshAllVisualTiles();
        }
        
        /// <summary>
        /// Get terrain type at base grid position
        /// </summary>
        public TerrainType GetTerrainAt(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return TerrainType.Undiggable; // Out of bounds = undiggable
            }
            return baseGrid[y, x];
        }
        
        /// <summary>
        /// Set terrain type at base grid position
        /// </summary>
        public void SetTerrainAt(int x, int y, TerrainType terrainType)
        {
            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            {
                baseGrid[y, x] = terrainType;
                UpdateAffectedVisualTiles(x, y);
            }
        }
        
        /// <summary>
        /// Dig at specified position (convert diggable to empty)
        /// </summary>
        public bool DigAt(int x, int y)
        {
            if (GetTerrainAt(x, y) == TerrainType.Diggable)
            {
                SetTerrainAt(x, y, TerrainType.Empty);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Get the pattern for a visual tile based on its 4 corner samples
        /// </summary>
        private (TerrainType tl, TerrainType tr, TerrainType bl, TerrainType br) GetVisualTilePattern(int visualX, int visualY)
        {
            // Visual tile at (x,y) samples from base grid at:
            // (x,y), (x+1,y), (x,y+1), (x+1,y+1)
            var tl = GetTerrainAt(visualX, visualY);
            var tr = GetTerrainAt(visualX + 1, visualY);
            var bl = GetTerrainAt(visualX, visualY + 1);
            var br = GetTerrainAt(visualX + 1, visualY + 1);
            
            return (tl, tr, bl, br);
        }
        
        /// <summary>
        /// Update a single visual tile
        /// </summary>
        private void UpdateVisualTile(int visualX, int visualY)
        {
            if (tilePatternSystem == null) return;
            
            var pattern = GetVisualTilePattern(visualX, visualY);
            var tiles = tilePatternSystem.GetTiles(pattern.tl, pattern.tr, pattern.bl, pattern.br);
            
            // Position in tilemap (accounting for Unity's coordinate system)
            // Unity tilemaps use bottom-left origin, our grid uses top-left
            Vector3Int tilePos = new Vector3Int(visualX, gridHeight - 1 - visualY, 0);
            
            if (colorTilemap != null)
            {
                colorTilemap.SetTile(tilePos, tiles.colorTile);
            }
            
            if (normalTilemap != null)
            {
                normalTilemap.SetTile(tilePos, tiles.normalTile);
            }
        }
        
        /// <summary>
        /// Update visual tiles affected by a base grid change
        /// </summary>
        private void UpdateAffectedVisualTiles(int baseX, int baseY)
        {
            // A change at base position (x,y) affects up to 4 visual tiles:
            // (x-1,y-1), (x,y-1), (x-1,y), (x,y)
            
            var affectedPositions = new List<Vector2Int>
            {
                new Vector2Int(baseX - 1, baseY - 1),
                new Vector2Int(baseX, baseY - 1),
                new Vector2Int(baseX - 1, baseY),
                new Vector2Int(baseX, baseY)
            };
            
            foreach (var pos in affectedPositions)
            {
                // Only update if within visual grid bounds
                if (pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight)
                {
                    UpdateVisualTile(pos.x, pos.y);
                }
            }
        }
        
        /// <summary>
        /// Refresh all visual tiles
        /// </summary>
        public void RefreshAllVisualTiles()
        {
            if (tilePatternSystem == null)
            {
                Debug.LogWarning("TilePatternSystem not assigned!");
                return;
            }
            
            // Clear tilemaps first
            if (colorTilemap != null) colorTilemap.ClearAllTiles();
            if (normalTilemap != null) normalTilemap.ClearAllTiles();
            
            // Update all visual tiles
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    UpdateVisualTile(x, y);
                }
            }
            
            Debug.Log($"Refreshed {gridWidth}x{gridHeight} visual tiles");
        }
        
        /// <summary>
        /// Convert world position to base grid coordinates
        /// </summary>
        public Vector2Int WorldToBaseGrid(Vector3 worldPos)
        {
            // Account for tilemap's transform
            Vector3 localPos = colorTilemap.transform.InverseTransformPoint(worldPos);
            
            // Convert to grid coordinates (accounting for visual offset)
            int x = Mathf.FloorToInt(localPos.x + visualOffset.x);
            int y = gridHeight - 1 - Mathf.FloorToInt(localPos.y + visualOffset.y);
            
            return new Vector2Int(x, y);
        }
        
        /// <summary>
        /// Convert base grid coordinates to world position
        /// </summary>
        public Vector3 BaseGridToWorld(int x, int y)
        {
            // Convert to tilemap position
            Vector3 localPos = new Vector3(x - visualOffset.x, gridHeight - 1 - y - visualOffset.y, 0);
            
            // Convert to world position
            return colorTilemap.transform.TransformPoint(localPos);
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebugGrid || baseGrid == null) return;
            
            // Draw base grid debug visualization
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Vector3 worldPos = BaseGridToWorld(x, y);
                    
                    // Color based on terrain type
                    switch (baseGrid[y, x])
                    {
                        case TerrainType.Empty:
                            Gizmos.color = new Color(1, 1, 1, 0.3f);
                            break;
                        case TerrainType.Diggable:
                            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                            break;
                        case TerrainType.Undiggable:
                            Gizmos.color = new Color(0, 0, 0, 0.7f);
                            break;
                    }
                    
                    Gizmos.DrawCube(worldPos, Vector3.one * 0.9f);
                }
            }
        }
    }
}

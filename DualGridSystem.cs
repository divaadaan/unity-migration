using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace MiningGame
{
    public class DualGridSystem : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private int gridWidth = 100;
        [SerializeField] private int gridHeight = 100;
        
        [Header("Tilemap References")]
        [SerializeField] private Tilemap colorTilemap;
        [SerializeField] private Tilemap normalTilemap;
        [SerializeField] private Tilemap debugTilemap;
        
        [Header("Tile Assets")]
        [SerializeField] private TileMapping tileMapping;
        [SerializeField] private TileBase[] artistColorTiles; 
        [SerializeField] private TileBase[] artistNormalTiles;
        [SerializeField] private TileBase emptyTile; 
        
        [Header("Visual Settings")]
        [SerializeField] private Vector2 visualOffset = new Vector2(-0.5f, -0.5f);
        [SerializeField] private bool showDebugGrid = false;
        
        // Base grid data (logic layer)
        private Tile[,] baseGrid;
        
        // Properties
        public int Width => gridWidth;
        public int Height => gridHeight;
        
        private void Awake()
        {
            InitializeGrid();
            if (tileMapping != null)
            {
                tileMapping.Initialize();
            }
        }
        
        private void Start()
        {
            RefreshAllVisualTiles();
        }
        
        private void InitializeGrid()
        {
            baseGrid = new Tile[gridHeight, gridWidth];
            
            // Initialize with a simple test pattern
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    TerrainType type = TerrainType.Empty;
                    
                    // Create some test patterns
                    if (x == 0 || x == gridWidth - 1 || y == 0 || y == gridHeight - 1)
                        type = TerrainType.Undiggable; // Border
                    else if (Random.Range(0f, 1f) < 0.3f)
                        type = TerrainType.Diggable;
                    else if (Random.Range(0f, 1f) < 0.1f)
                        type = TerrainType.Undiggable;
                    
                    baseGrid[y, x] = new Tile(type);
                }
            }
        }
        
        public Tile GetTileAt(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return new Tile(TerrainType.Undiggable);
            }
            return baseGrid[y, x];
        }
        
        public void SetTileAt(int x, int y, Tile tile)
        {
            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            {
                baseGrid[y, x] = tile;
                UpdateAffectedVisualTiles(x, y);
            }
        }
        
        public void CycleTileAt(int x, int y)
        {
            var tile = GetTileAt(x, y);
            if (tile == null) return;
            
            // Cycle: Empty -> Diggable -> Undiggable -> Empty
            TerrainType newType = tile.terrainType switch
            {
                TerrainType.Empty => TerrainType.Diggable,
                TerrainType.Diggable => TerrainType.Undiggable,
                TerrainType.Undiggable => TerrainType.Empty,
                _ => TerrainType.Empty
            };
            
            SetTileAt(x, y, new Tile(newType));
            Debug.Log($"Tile ({x},{y}): {tile.terrainType} -> {newType}");
        }
        
        private void UpdateVisualTile(int visualX, int visualY)
        {
            if (tileMapping == null || artistColorTiles == null || artistColorTiles.Length == 0)
                return;
            
            // Get the four corners
            var tl = GetTileAt(visualX, visualY);
            var tr = GetTileAt(visualX + 1, visualY);
            var bl = GetTileAt(visualX, visualY + 1);
            var br = GetTileAt(visualX + 1, visualY + 1);
            
            // Get artist tile position
            var artistPos = tileMapping.GetArtistPosition(
                tl.terrainType, tr.terrainType, bl.terrainType, br.terrainType
            );
            
            // Calculate tile index (row 0 is at bottom in Unity)
            int tileIndex = artistPos.y * 8 + artistPos.x;
            
            // Handle all-empty special case
            TileBase colorTile = null;
            TileBase normalTile = null;
            
            if (tl.terrainType == TerrainType.Empty && tr.terrainType == TerrainType.Empty &&
                bl.terrainType == TerrainType.Empty && br.terrainType == TerrainType.Empty)
            {
                colorTile = emptyTile;
            }
            else if (tileIndex >= 0 && tileIndex < artistColorTiles.Length)
            {
                colorTile = artistColorTiles[tileIndex];
                if (artistNormalTiles != null && tileIndex < artistNormalTiles.Length)
                    normalTile = artistNormalTiles[tileIndex];
            }
            
            // Set tiles in tilemaps (with visual offset)
            Vector3Int tilePos = new Vector3Int(visualX, visualY, 0);
            
            if (colorTilemap != null && colorTile != null)
                colorTilemap.SetTile(tilePos, colorTile);
            
            if (normalTilemap != null && normalTile != null)
                normalTilemap.SetTile(tilePos, normalTile);
        }
        
        private void UpdateAffectedVisualTiles(int baseX, int baseY)
        {
            // A change at base position affects up to 4 visual tiles
            var positions = new Vector2Int[]
            {
                new Vector2Int(baseX - 1, baseY - 1),
                new Vector2Int(baseX, baseY - 1),
                new Vector2Int(baseX - 1, baseY),
                new Vector2Int(baseX, baseY)
            };
            
            foreach (var pos in positions)
            {
                if (pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight)
                {
                    UpdateVisualTile(pos.x, pos.y);
                }
            }
        }
        
        public void RefreshAllVisualTiles()
        {
            if (colorTilemap != null) colorTilemap.ClearAllTiles();
            if (normalTilemap != null) normalTilemap.ClearAllTiles();
            
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    UpdateVisualTile(x, y);
                }
            }
            
            Debug.Log($"Refreshed {gridWidth}x{gridHeight} visual tiles");
        }
        
        public Vector2Int WorldToBaseGrid(Vector3 worldPos)
        {
            // Convert world position to grid coordinates
            var gridPos = colorTilemap.WorldToCell(worldPos);
            return new Vector2Int(gridPos.x, gridPos.y);
        }
    }
}
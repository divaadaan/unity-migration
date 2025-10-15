using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

namespace MiningGame
{
    public class DualGridSystem : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private int gridWidth = SharedConstants.GRID_WIDTH;
        [SerializeField] private int gridHeight = SharedConstants.GRID_HEIGHT;
        
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
        [SerializeField] private Vector2 visualOffset = SharedConstants.VISUAL_OFFSET;
        
        [Header("Debug Settings")]
        [SerializeField] private bool showDebugOverlay = false;
        [SerializeField] private Color debugEmptyColor = SharedConstants.DEBUG_EMPTY_COLOR;
        [SerializeField] private Color debugDiggableColor = SharedConstants.DEBUG_DIGGABLE_COLOR;
        [SerializeField] private Color debugUndiggableColor = SharedConstants.DEBUG_UNDIGGABLE_COLOR;

        // Class member variables
        private Tile[,] baseGrid;
        private TileEditorInputs inputActions;
        private Camera mainCamera;
        private static readonly Tile OUT_OF_BOUNDS_TILE = new Tile(TerrainType.Undiggable);      
        
        public int Width => gridWidth;
        public int Height => gridHeight;
        
        private void Awake()
        {
            InitializeGrid();
            
            if (tileMapping == null)
            {
                Debug.LogError("DualGridSystem: tileMapping is NULL!");
                return;
            }
            
            Debug.Log($"DualGridSystem: Initializing TileMapping at {Time.frameCount}");
            tileMapping.Initialize();
            Debug.Log($"DualGridSystem: TileMapping initialized, checking dictionary...");
            
            // Test immediate access
            var testPos = tileMapping.GetArtistPosition(
                TerrainType.Diggable, TerrainType.Diggable, 
                TerrainType.Diggable, TerrainType.Diggable
            );
            Debug.Log($"DualGridSystem: Test pattern (1,1,1,1) returned position {testPos}");
            
            inputActions = new TileEditorInputs();
            mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            inputActions.Enable();
            inputActions.GameplayMap.ToggleDebug.performed += ToggleDebugOverlay;
            inputActions.GameplayMap.TileEdit.performed += OnTileEdit;
        }
        
        private void OnDisable()
        {
            inputActions.GameplayMap.ToggleDebug.performed -= ToggleDebugOverlay;
            inputActions.GameplayMap.TileEdit.performed -= OnTileEdit;
            inputActions.Disable();
        }

        private void Start()
        {
            RefreshAllVisualTiles();
        }

        private void OnTileEdit(InputAction.CallbackContext context)
        {
            HandleRuntimeTileEdit();
        }
        
        private void HandleRuntimeTileEdit()
        {
            Vector2 mousePos = inputActions.GameplayMap.MousePosition.ReadValue<Vector2>();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            worldPos.z = 0;
            
            Vector2Int gridPos = WorldToBaseGrid(worldPos);
            
            if (gridPos.x >= 0 && gridPos.x < gridWidth && 
                gridPos.y >= 0 && gridPos.y < gridHeight)
            {
                CycleTileAt(gridPos.x, gridPos.y);
                Debug.Log($"Cycled tile at ({gridPos.x}, {gridPos.y})");
            }
        }
        
        public void ToggleDebugOverlay(InputAction.CallbackContext context)
        {
            showDebugOverlay = !showDebugOverlay;
            UpdateDebugOverlay();
            Debug.Log($"Debug overlay: {(showDebugOverlay ? "ON" : "OFF")}");
        }
        
        private void UpdateDebugOverlay()
        {
            if (debugTilemap == null) return;
            
            debugTilemap.ClearAllTiles();
            
            if (!showDebugOverlay) return;
            
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    var tile = GetTileAt(x, y);
                    if (tile != null)
                    {
                        Vector3Int pos = new Vector3Int(x, y, 0);
                        var debugTile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                        
                        var texture = new Texture2D(1, 1);
                        Color color = tile.terrainType switch
                        {
                            TerrainType.Empty => debugEmptyColor,
                            TerrainType.Diggable => debugDiggableColor,
                            TerrainType.Undiggable => debugUndiggableColor,
                            _ => Color.clear
                        };
                        texture.SetPixel(0, 0, color);
                        texture.Apply();
                        
                        debugTile.sprite = Sprite.Create(
                            texture, 
                            new Rect(0, 0, 1, 1), 
                            Vector2.one * 0.5f, 
                            SharedConstants.RENDER_TILE_SIZE
                        );
                        debugTilemap.SetTile(pos, debugTile);
                    }
                }
            }
        }
        
        private void InitializeGrid()
        {
            baseGrid = new Tile[gridHeight, gridWidth];
            
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    TerrainType type = TerrainType.Empty;
                    
                    if (x == 0 || x == gridWidth - 1 || y == 0 || y == gridHeight - 1)
                        type = TerrainType.Undiggable;
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
                return OUT_OF_BOUNDS_TILE;
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
            
            // Get artist tile position (returns null for all-empty pattern)
            var artistPos = tileMapping.GetArtistPosition(
                tl.terrainType, tr.terrainType, bl.terrainType, br.terrainType
            );
            
            TileBase colorTile = null;
            TileBase normalTile = null;
            
            // Handle all-empty pattern (null) vs mapped patterns
            if (!artistPos.HasValue)
            {
                colorTile = emptyTile;
            }
            else
            {
                int tileIndex = (SharedConstants.TILEMAP_ROWS - 1 - artistPos.Value.y) * SharedConstants.TILEMAP_COLUMNS + artistPos.Value.x;
                
                if (tileIndex >= 0 && tileIndex < artistColorTiles.Length)
                {
                    colorTile = artistColorTiles[tileIndex];
                    if (artistNormalTiles != null && tileIndex < artistNormalTiles.Length)
                        normalTile = artistNormalTiles[tileIndex];
                }
            }
            
            // Set tiles in tilemaps
            Vector3Int tilePos = new Vector3Int(visualX, visualY, 0);
            
            if (colorTilemap != null && colorTile != null)
                colorTilemap.SetTile(tilePos, colorTile);
            
            if (normalTilemap != null && normalTile != null)
                normalTilemap.SetTile(tilePos, normalTile);
        }
        
        private void UpdateAffectedVisualTiles(int baseX, int baseY)
        {
            for (int vx = baseX - 1; vx <= baseX; vx++)
            {
                for (int vy = baseY - 1; vy <= baseY; vy++)
                {
                    if (vx >= 0 && vx < gridWidth && vy >= 0 && vy < gridHeight)
                    {
                        UpdateVisualTile(vx, vy);
                    }
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
            Vector3Int visualCell = colorTilemap.WorldToCell(worldPos);

            Vector3 cellWorldPos = colorTilemap.CellToWorld(visualCell);
            float localX = (worldPos.x - cellWorldPos.x) / colorTilemap.cellSize.x;
            float localY = (worldPos.y - cellWorldPos.y) / colorTilemap.cellSize.y;

            int baseX, baseY;

            if (localX < 0.5f && localY < 0.5f)
            {
                baseX = visualCell.x;
                baseY = visualCell.y;
            }
            else if (localX >= 0.5f && localY < 0.5f)
            {
                baseX = visualCell.x + 1;
                baseY = visualCell.y;
            }
            else if (localX < 0.5f && localY >= 0.5f)
            {
                baseX = visualCell.x;
                baseY = visualCell.y + 1;
            }
            else
            {
                baseX = visualCell.x + 1;
                baseY = visualCell.y + 1;
            }

            return new Vector2Int(baseX, baseY);
        }
        
        [ContextMenu("Debug Bottom Row")]
        public void DebugBottomRow()
        {
            Debug.Log("=== BOTTOM ROW DEBUG ===");
            
            // Check base grid bottom row (y=0 in array, bottom of map)
            for (int x = 0; x < gridWidth; x++)
            {
                var tile = GetTileAt(x, 0);
                Debug.Log($"Base[{x},0] = {tile.terrainType}");
            }
            
            // Check what visual tiles see for bottom row
            for (int vx = 0; vx < gridWidth; vx++)
            {
                var tl = GetTileAt(vx, 0);
                var tr = GetTileAt(vx + 1, 0);
                var bl = GetTileAt(vx, 1);
                var br = GetTileAt(vx + 1, 1);

                var artistPos = tileMapping.GetArtistPosition(
                    tl.terrainType, tr.terrainType, bl.terrainType, br.terrainType
                );
                
                if (!artistPos.HasValue)
                {
                    Debug.Log($"Visual[{vx},0]: TL={tl.terrainType} TR={tr.terrainType} BL={bl.terrainType} BR={br.terrainType} -> Artist(Empty)");
                    continue;
                }
                else
                {
                    int tileIndex = artistPos.Value.y * SharedConstants.TILEMAP_COLUMNS + artistPos.Value.x;
                    Debug.Log($"Visual[{vx},0]: TL={tl.terrainType} TR={tr.terrainType} BL={bl.terrainType} BR={br.terrainType} -> Artist({artistPos.Value.x},{artistPos.Value.y}) -> Index {tileIndex}");
                }                
            }
        }
    }
}
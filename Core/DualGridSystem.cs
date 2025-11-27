using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

namespace DigDigDiner
{
    public class DualGridSystem : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private int gridWidth = SharedConstants.GRID_WIDTH;
        [SerializeField] private int gridHeight = SharedConstants.GRID_HEIGHT;
        
        [Header("Tilemap References")]
        [SerializeField] private Tilemap colorTilemap;
        [SerializeField] private Tilemap debugTilemap;
        
        [Header("Tile Assets")]
        [SerializeField] private TileMapping tileMapping;
        [SerializeField] private TileBase[] artistColorTiles; 
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
        public bool IsInitialized { get; private set; } = false;
        
        private void Awake()
        {
            // Initialize the base grid array (MapGenerator will populate it)
            baseGrid = new Tile[gridHeight, gridWidth];

            if (tileMapping == null)
            {
                Debug.LogError("DualGridSystem: tileMapping is NULL!");
                return;
            }

            Debug.Log($"DualGridSystem: Initializing TileMapping at {Time.frameCount}");
            tileMapping.Initialize();
            Debug.Log($"DualGridSystem: TileMapping initialized, checking dictionary...");

            inputActions = new TileEditorInputs();
            AlignGrid();
            mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("DualGridSystem: inputActions is null in OnEnable!");
                return;
            }

            inputActions.Enable();
            inputActions.GameplayMap.ToggleDebug.performed += ToggleDebugOverlay;
            inputActions.GameplayMap.TileEdit.performed += OnTileEdit;
        }

        private void OnDisable()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("DualGridSystem: inputActions is null in OnDisable!");
                return;
            }

            inputActions.GameplayMap.ToggleDebug.performed -= ToggleDebugOverlay;
            inputActions.GameplayMap.TileEdit.performed -= OnTileEdit;
            inputActions.Disable();
        }

        private void Start()
        {
        }

private void AlignGrid()
    {
        if (colorTilemap != null && colorTilemap.layoutGrid != null)
        {
            Grid grid = colorTilemap.layoutGrid;
            Vector3 currentPos = grid.transform.position;
            grid.transform.position = new Vector3(visualOffset.x, visualOffset.y, currentPos.z);
            
            Debug.Log($"DualGridSystem: Aligned Grid to {grid.transform.position} using offset {visualOffset}");
        }
    }

        public void CompleteInitialization()
        {
            IsInitialized = true;
            RefreshAllVisualTiles();
            Debug.Log("DualGridSystem: Initialization Finalized.");
        }

        private void OnTileEdit(InputAction.CallbackContext context)
        {
            HandleRuntimeTileEdit();
        }
        
        private void HandleRuntimeTileEdit()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("DualGridSystem: inputActions is null!");
                return;
            }

            if (mainCamera == null)
            {
                Debug.LogWarning("DualGridSystem: mainCamera is null!");
                return;
            }

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
        
        
        public Tile GetTileAt(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return OUT_OF_BOUNDS_TILE;
            }
            return baseGrid[y, x] ?? new Tile(TerrainType.Empty);
        }
        
        public void SetTileAt(int x, int y, Tile tile)
        {
            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            {
                baseGrid[y, x] = tile;
                UpdateAffectedVisualTiles(x, y);
            }
        }

        /// <summary>
        /// Sets a tile without triggering visual updates.
        /// Use this for batch operations like map generation.
        /// </summary>
        public void SetTileAtSilent(int x, int y, Tile tile)
        {
            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            {
                baseGrid[y, x] = tile;
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

            // Get the four corners (Y increases upward in Unity, so visualY+1 is above visualY)
            var tl = GetTileAt(visualX, visualY + 1);
            var tr = GetTileAt(visualX + 1, visualY + 1);
            var bl = GetTileAt(visualX, visualY);
            var br = GetTileAt(visualX + 1, visualY);
            
            // Get artist tile position (returns null for all-empty pattern)
            var artistPos = tileMapping.GetArtistPosition(
                tl.terrainType, tr.terrainType, bl.terrainType, br.terrainType
            );
            
            TileBase colorTile = null;
            
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
                }
            }

            // Set tiles in tilemaps (null clears the tile)
            Vector3Int tilePos = new Vector3Int(visualX, visualY, 0);

            if (colorTilemap != null)
                colorTilemap.SetTile(tilePos, colorTile); // null or emptyTile clears it
        }
        
        private void UpdateAffectedVisualTiles(int baseX, int baseY)
        {
            for (int vx = baseX - 1; vx <= baseX; vx++)
            {
                for (int vy = baseY - 1; vy <= baseY; vy++)
                {
                    // Only update visual tiles that won't sample out-of-bounds
                    if (vx >= 0 && vx < gridWidth - 1 && vy >= 0 && vy < gridHeight - 1)
                    {
                        UpdateVisualTile(vx, vy);
                    }
                }
            }
        }   
        
        public void RefreshAllVisualTiles()
        {
            if (colorTilemap != null) colorTilemap.ClearAllTiles();

            // Only draw visual tiles up to gridWidth-1 and gridHeight-1
            // to avoid sampling out-of-bounds at top and right edges
            for (int y = 0; y < gridHeight - 1; y++)
            {
                for (int x = 0; x < gridWidth - 1; x++)
                {
                    UpdateVisualTile(x, y);
                }
            }

            Debug.Log($"Refreshed {(gridWidth-1)}x{(gridHeight-1)} visual tiles");
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
                var tl = GetTileAt(vx, 1);
                var tr = GetTileAt(vx + 1, 1);
                var bl = GetTileAt(vx, 0);
                var br = GetTileAt(vx + 1, 0);

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
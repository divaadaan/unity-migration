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
              
        [Header("Debug Settings")]
        [SerializeField] private bool showDebugOverlay = false;
        [SerializeField] private Color debugEmptyColor = SharedConstants.DEBUG_EMPTY_COLOR;
        [SerializeField] private Color debugDiggableColor = SharedConstants.DEBUG_DIGGABLE_COLOR;
        [SerializeField] private Color debugUndiggableColor = SharedConstants.DEBUG_UNDIGGABLE_COLOR;

        private Tile[,] baseGrid;
        private TileEditorInputs inputActions;
        private Camera mainCamera;
        private static readonly Tile OUT_OF_BOUNDS_TILE = new Tile(TerrainType.Undiggable);      
        
        public int Width => gridWidth;
        public int Height => gridHeight;
        public bool IsInitialized { get; private set; } = false;
        
        private void Awake()
        {
            baseGrid = new Tile[gridHeight, gridWidth];

            if (tileMapping == null)
            {
                Debug.LogError($"DualGridSystem ({name}): tileMapping is NULL!");
                return;
            }

            Debug.Log($"DualGridSystem ({name}): Initializing TileMapping at {Time.frameCount}");
            tileMapping.Initialize();

            inputActions = new TileEditorInputs();
            mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (inputActions == null) return;
            inputActions.Enable();
            inputActions.GameplayMap.ToggleDebug.performed += ToggleDebugOverlay;
            inputActions.GameplayMap.TileEdit.performed += OnTileEdit;
        }

        private void OnDisable()
        {
            if (inputActions == null) return;
            inputActions.GameplayMap.ToggleDebug.performed -= ToggleDebugOverlay;
            inputActions.GameplayMap.TileEdit.performed -= OnTileEdit;
            inputActions.Disable();
        }

        public void CompleteInitialization()
        {
            IsInitialized = true;
            RefreshAllVisualTiles();
            Debug.Log($"DualGridSystem ({name}): Initialization Finalized.");
        }

        private void OnTileEdit(InputAction.CallbackContext context)
        {
            HandleRuntimeTileEdit();
        }
        
        private void HandleRuntimeTileEdit()
        {
            if (inputActions == null || mainCamera == null) return;

            Vector2 mousePos = inputActions.GameplayMap.MousePosition.ReadValue<Vector2>();
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            worldPos.z = 0;

            Vector2Int gridPos = WorldToBaseGrid(worldPos);

            if (gridPos.x >= 0 && gridPos.x < gridWidth &&
                gridPos.y >= 0 && gridPos.y < gridHeight)
            {
                CycleTileAt(gridPos.x, gridPos.y);
            }
        }
        
        public void ToggleDebugOverlay(InputAction.CallbackContext context)
        {
            showDebugOverlay = !showDebugOverlay;
            UpdateDebugOverlay();
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
            if (baseGrid == null) return OUT_OF_BOUNDS_TILE;

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
        }
        
        private void UpdateVisualTile(int visualX, int visualY)
        {
            if (tileMapping == null) return;

            // Get the four corners
            var tl = GetTileAt(visualX, visualY + 1);
            var tr = GetTileAt(visualX + 1, visualY + 1);
            var bl = GetTileAt(visualX, visualY);
            var br = GetTileAt(visualX + 1, visualY);
            
            TileBase colorTile = tileMapping.GetTile(
                tl.stateIndex, tr.stateIndex, bl.stateIndex, br.stateIndex
            );

            Vector3Int tilePos = new Vector3Int(visualX, visualY, 0);

            if (colorTilemap != null)
                colorTilemap.SetTile(tilePos, colorTile);
            
        }
        
        private void UpdateAffectedVisualTiles(int baseX, int baseY)
        {
            for (int vx = baseX - 1; vx <= baseX; vx++)
            {
                for (int vy = baseY - 1; vy <= baseY; vy++)
                {
                    if (vx >= 0 && vx < gridWidth - 1 && vy >= 0 && vy < gridHeight - 1)
                    {
                        UpdateVisualTile(vx, vy);
                    }
                }
            }
        }   
        
        public void RefreshAllVisualTiles()
        {
            if (baseGrid == null) return;

            if (colorTilemap != null) colorTilemap.ClearAllTiles();

            for (int y = 0; y < gridHeight - 1; y++)
            {
                for (int x = 0; x < gridWidth - 1; x++)
                {
                    UpdateVisualTile(x, y);
                }
            }
            
            Debug.Log($"Refreshed {(gridWidth-1)}x{(gridHeight-1)} visual tiles on {name}");
        }

        public Vector2Int WorldToBaseGrid(Vector3 worldPos)
        {
            if (colorTilemap == null) return Vector2Int.zero;

            Vector3Int visualCell = colorTilemap.WorldToCell(worldPos);
            Vector3 cellWorldPos = colorTilemap.CellToWorld(visualCell);
            
            float localX = (worldPos.x - cellWorldPos.x) / colorTilemap.cellSize.x;
            float localY = (worldPos.y - cellWorldPos.y) / colorTilemap.cellSize.y;

            int baseX = (localX >= 0.0f) ? visualCell.x + 1 : visualCell.x;
            int baseY = (localY >= 0.0f) ? visualCell.y + 1 : visualCell.y;

            return new Vector2Int(baseX, baseY);
        }
    }
}
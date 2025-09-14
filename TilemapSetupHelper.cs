using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiningGame
{
    /// <summary>
    /// Helper component to set up tilemaps and import artist tiles
    /// </summary>
    public class TilemapSetupHelper : MonoBehaviour
    {
        [Header("Tilemap Structure")]
        [SerializeField] private GameObject tilemapRoot;
        [SerializeField] private Grid gridComponent;
        
        [Header("Created Tilemaps")]
        [SerializeField] private Tilemap colorTilemap;
        [SerializeField] private Tilemap normalTilemap;
        [SerializeField] private Tilemap debugTilemap;
        
        [Header("Tilemap Settings")]
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 visualOffset = new Vector2(0.5f, 0.5f);
        
        /// <summary>
        /// Create the tilemap hierarchy
        /// </summary>
        [ContextMenu("Create Tilemap Structure")]
        public void CreateTilemapStructure()
        {
            // Create root if it doesn't exist
            if (tilemapRoot == null)
            {
                tilemapRoot = new GameObject("DualGridTilemaps");
                tilemapRoot.transform.SetParent(transform);
                tilemapRoot.transform.localPosition = Vector3.zero;
            }
            
            // Add Grid component if needed
            if (gridComponent == null)
            {
                gridComponent = tilemapRoot.GetComponent<Grid>();
                if (gridComponent == null)
                {
                    gridComponent = tilemapRoot.AddComponent<Grid>();
                }
                gridComponent.cellSize = new Vector3(cellSize, cellSize, 0);
            }
            
            // Create Color Tilemap
            if (colorTilemap == null)
            {
                GameObject colorObj = new GameObject("ColorTilemap");
                colorObj.transform.SetParent(tilemapRoot.transform);
                colorObj.transform.localPosition = new Vector3(-visualOffset.x, -visualOffset.y, 0);
                colorTilemap = colorObj.AddComponent<Tilemap>();
                var colorRenderer = colorObj.AddComponent<TilemapRenderer>();
                colorRenderer.sortingOrder = 0;
            }
            
            // Create Normal Tilemap
            if (normalTilemap == null)
            {
                GameObject normalObj = new GameObject("NormalTilemap");
                normalObj.transform.SetParent(tilemapRoot.transform);
                normalObj.transform.localPosition = new Vector3(-visualOffset.x, -visualOffset.y, 0);
                normalTilemap = normalObj.AddComponent<Tilemap>();
                var normalRenderer = normalObj.AddComponent<TilemapRenderer>();
                normalRenderer.sortingOrder = 1;
            }
            
            // Create Debug Tilemap
            if (debugTilemap == null)
            {
                GameObject debugObj = new GameObject("DebugTilemap");
                debugObj.transform.SetParent(tilemapRoot.transform);
                debugObj.transform.localPosition = Vector3.zero;
                debugTilemap = debugObj.AddComponent<Tilemap>();
                var debugRenderer = debugObj.AddComponent<TilemapRenderer>();
                debugRenderer.sortingOrder = -1;
                debugObj.SetActive(false); // Hidden by default
            }
            
            Debug.Log("Tilemap structure created successfully!");
        }
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// Editor tool to help slice and import artist tilemaps
    /// </summary>
    public class TilemapImporter : EditorWindow
    {
        private Texture2D colorTilemap;
        private Texture2D normalTilemap;
        private int tileSize = 100;
        private int columns = 10;
        private int rows = 8;
        private string outputFolder = "Assets/Tiles/Generated";
        private TilePatternSystem targetPatternSystem;
        
        [MenuItem("Mining Game/Tilemap Importer")]
        public static void ShowWindow()
        {
            GetWindow<TilemapImporter>("Tilemap Importer");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Artist Tilemap Importer", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            colorTilemap = EditorGUILayout.ObjectField("Color Tilemap", colorTilemap, typeof(Texture2D), false) as Texture2D;
            normalTilemap = EditorGUILayout.ObjectField("Normal Tilemap", normalTilemap, typeof(Texture2D), false) as Texture2D;
            
            EditorGUILayout.Space();
            
            tileSize = EditorGUILayout.IntField("Tile Size (pixels)", tileSize);
            columns = EditorGUILayout.IntField("Columns", columns);
            rows = EditorGUILayout.IntField("Rows", rows);
            
            EditorGUILayout.Space();
            
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            targetPatternSystem = EditorGUILayout.ObjectField("Target Pattern System", targetPatternSystem, typeof(TilePatternSystem), false) as TilePatternSystem;
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Slice and Create Tiles"))
            {
                SliceAndCreateTiles();
            }
            
            if (GUILayout.Button("Auto-Assign to Pattern System"))
            {
                AutoAssignToPatternSystem();
            }
        }
        
        private void SliceAndCreateTiles()
        {
            if (colorTilemap == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a color tilemap texture", "OK");
                return;
            }
            
            // Ensure output folder exists
            if (!AssetDatabase.IsValidFolder(outputFolder))
            {
                System.IO.Directory.CreateDirectory(outputFolder);
                AssetDatabase.Refresh();
            }
            
            // Slice color tilemap
            var colorSprites = SliceTilemap(colorTilemap, "Color");
            
            // Slice normal tilemap if provided
            var normalSprites = normalTilemap != null ? SliceTilemap(normalTilemap, "Normal") : null;
            
            // Create TileBase assets from sprites
            CreateTileAssets(colorSprites, normalSprites);
            
            EditorUtility.DisplayDialog("Success", "Tiles created successfully!", "OK");
        }
        
        private Sprite[] SliceTilemap(Texture2D texture, string suffix)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer == null) return null;
            
            // Set texture import settings
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = tileSize;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            
            // Create sprite metadata
            var spriteMetaData = new SpriteMetaData[columns * rows];
            int index = 0;
            
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    spriteMetaData[index] = new SpriteMetaData
                    {
                        name = $"{texture.name}_{suffix}_{index}",
                        rect = new Rect(x * tileSize, texture.height - (y + 1) * tileSize, tileSize, tileSize),
                        pivot = new Vector2(0.5f, 0.5f),
                        alignment = (int)SpriteAlignment.Center
                    };
                    index++;
                }
            }
            
            importer.spritesheet = spriteMetaData;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            
            // Load the sprites
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        }
        
        private void CreateTileAssets(Sprite[] colorSprites, Sprite[] normalSprites)
        {
            if (colorSprites == null) return;
            
            for (int i = 0; i < colorSprites.Length; i++)
            {
                // Create color tile
                var colorTile = ScriptableObject.CreateInstance<Tile>();
                colorTile.sprite = colorSprites[i];
                
                string colorPath = $"{outputFolder}/Tile_Color_{i}.asset";
                AssetDatabase.CreateAsset(colorTile, colorPath);
                
                // Create normal tile if available
                if (normalSprites != null && i < normalSprites.Length)
                {
                    var normalTile = ScriptableObject.CreateInstance<Tile>();
                    normalTile.sprite = normalSprites[i];
                    
                    string normalPath = $"{outputFolder}/Tile_Normal_{i}.asset";
                    AssetDatabase.CreateAsset(normalTile, normalPath);
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        private void AutoAssignToPatternSystem()
        {
            if (targetPatternSystem == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a target pattern system", "OK");
                return;
            }
            
            // Load all tile assets from output folder
            string[] colorTilePaths = AssetDatabase.FindAssets("t:Tile", new[] { outputFolder })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => path.Contains("Color"))
                .OrderBy(path => path)
                .ToArray();
            
            string[] normalTilePaths = AssetDatabase.FindAssets("t:Tile", new[] { outputFolder })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => path.Contains("Normal"))
                .OrderBy(path => path)
                .ToArray();
            
            // Auto-assign to pattern system
            var patterns = targetPatternSystem.GetType()

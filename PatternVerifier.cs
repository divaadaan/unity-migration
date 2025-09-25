using UnityEngine;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiningGame
{
#if UNITY_EDITOR
    public class PatternVerifier : EditorWindow
    {
        [Header("References")]
        public PatternMapper patternMapper;
        public Texture2D artistTilemap;
        
        [Header("Generation Settings")]
        public int tileSize = 100;
        public bool generateComparisonImage = true;
        public bool showPatternLabels = true;
        public bool highlightMismatches = true;
        
        [Header("Color Scheme")]
        public Color emptyColor = new Color(0.9f, 0.95f, 1f, 1f);      // Light blue-white
        public Color diggableColor = new Color(0.54f, 0.45f, 0.33f, 1f); // Brown earth
        public Color undiggableColor = new Color(0.17f, 0.17f, 0.17f, 1f); // Dark gray
        
        private Vector2 scrollPosition;
        
        [MenuItem("Mining Game/Pattern Verifier")]
        public static void ShowWindow()
        {
            GetWindow<PatternVerifier>("Pattern Verifier");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Pattern Mapping Verifier", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Generate systematic tiles to compare with artist tilemap", MessageType.Info);
            
            EditorGUILayout.Space();
            
            patternMapper = EditorGUILayout.ObjectField("Pattern Mapper", patternMapper, typeof(PatternMapper), false) as PatternMapper;
            artistTilemap = EditorGUILayout.ObjectField("Artist Tilemap", artistTilemap, typeof(Texture2D), false) as Texture2D;
            
            EditorGUILayout.Space();
            
            tileSize = EditorGUILayout.IntField("Tile Size", tileSize);
            generateComparisonImage = EditorGUILayout.Toggle("Generate Comparison Image", generateComparisonImage);
            showPatternLabels = EditorGUILayout.Toggle("Show Pattern Labels", showPatternLabels);
            highlightMismatches = EditorGUILayout.Toggle("Highlight Mismatches", highlightMismatches);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Colors:", EditorStyles.boldLabel);
            emptyColor = EditorGUILayout.ColorField("Empty", emptyColor);
            diggableColor = EditorGUILayout.ColorField("Diggable", diggableColor);
            undiggableColor = EditorGUILayout.ColorField("Undiggable", undiggableColor);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Generate Systematic Tilemap"))
            {
                GenerateSystematicTilemap();
            }
            
            if (GUILayout.Button("Generate Comparison Grid"))
            {
                GenerateComparisonGrid();
            }
            
            if (GUILayout.Button("Validate All Mappings"))
            {
                ValidateAllMappings();
            }
            
            EditorGUILayout.Space();
            
            if (patternMapper != null)
            {
                EditorGUILayout.LabelField($"Patterns: {GetPatternCount()}", EditorStyles.miniLabel);
            }
        }
        
        private int GetPatternCount()
        {
            if (patternMapper == null) return 0;
            var mappingsField = typeof(PatternMapper).GetField("mappings", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (mappingsField?.GetValue(patternMapper) is System.Collections.IList mappings)
            {
                return mappings.Count;
            }
            return 0;
        }
        
        private void GenerateSystematicTilemap()
        {
            if (patternMapper == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Pattern Mapper", "OK");
                return;
            }
            
            // Generate all 80 patterns systematically
            var systematicTexture = CreateSystematicTilemap();
            SaveTexture(systematicTexture, "SystematicTilemap");
            
            Debug.Log("Systematic tilemap generated! Compare with artist tilemap to verify mappings.");
        }
        
        private void GenerateComparisonGrid()
        {
            if (patternMapper == null || artistTilemap == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both Pattern Mapper and Artist Tilemap", "OK");
                return;
            }
            
            var comparisonTexture = CreateComparisonGrid();
            SaveTexture(comparisonTexture, "ComparisonGrid");
            
            Debug.Log("Comparison grid generated! Side-by-side systematic vs artist tiles.");
        }
        
        private Texture2D CreateSystematicTilemap()
        {
            // Create 8x10 tilemap (80 tiles)
            var texture = new Texture2D(8 * tileSize, 10 * tileSize, TextureFormat.RGBA32, false);
            
            // Generate all 80 patterns
            var patterns = GenerateAllPatterns();
            
            for (int i = 0; i < patterns.Count && i < 80; i++)
            {
                int col = i % 8;
                int row = i / 8;
                
                DrawPatternTile(texture, patterns[i], col * tileSize, (9 - row) * tileSize); // Flip Y
            }
            
            texture.Apply();
            return texture;
        }
        
        private Texture2D CreateComparisonGrid()
        {
            // Create side-by-side comparison: systematic | artist
            var texture = new Texture2D(16 * tileSize, 10 * tileSize, TextureFormat.RGBA32, false);
            
            // Left side: systematic
            var systematicTexture = CreateSystematicTilemap();
            var systematicPixels = systematicTexture.GetPixels();
            
            for (int y = 0; y < 10 * tileSize; y++)
            {
                for (int x = 0; x < 8 * tileSize; x++)
                {
                    texture.SetPixel(x, y, systematicPixels[y * 8 * tileSize + x]);
                }
            }
            
            // Right side: artist tilemap (if available)
            if (artistTilemap != null)
            {
                var artistPixels = artistTilemap.GetPixels();
                int artistWidth = artistTilemap.width;
                int artistHeight = artistTilemap.height;
                
                for (int y = 0; y < Mathf.Min(10 * tileSize, artistHeight); y++)
                {
                    for (int x = 0; x < Mathf.Min(8 * tileSize, artistWidth); x++)
                    {
                        if (x < artistWidth && y < artistHeight)
                        {
                            Color artistColor = artistPixels[y * artistWidth + x];
                            texture.SetPixel(8 * tileSize + x, y, artistColor);
                        }
                    }
                }
            }
            
            // Draw dividing line
            for (int y = 0; y < 10 * tileSize; y++)
            {
                texture.SetPixel(8 * tileSize - 1, y, Color.red);
                texture.SetPixel(8 * tileSize, y, Color.red);
                texture.SetPixel(8 * tileSize + 1, y, Color.red);
            }
            
            texture.Apply();
            DestroyImmediate(systematicTexture);
            return texture;
        }
        
        private void DrawPatternTile(Texture2D texture, PatternDefinition pattern, int startX, int startY)
        {
            int halfSize = tileSize / 2;
            
            // Draw each quadrant
            FillQuadrant(texture, startX, startY, halfSize, halfSize, GetColorForTerrain(pattern.topLeft));
            FillQuadrant(texture, startX + halfSize, startY, halfSize, halfSize, GetColorForTerrain(pattern.topRight));
            FillQuadrant(texture, startX, startY + halfSize, halfSize, halfSize, GetColorForTerrain(pattern.bottomLeft));
            FillQuadrant(texture, startX + halfSize, startY + halfSize, halfSize, halfSize, GetColorForTerrain(pattern.bottomRight));
            
            // Draw quadrant dividers
            DrawLine(texture, startX + halfSize, startY, startX + halfSize, startY + tileSize, Color.black);
            DrawLine(texture, startX, startY + halfSize, startX + tileSize, startY + halfSize, Color.black);
            
            // Draw tile border
            DrawRect(texture, startX, startY, tileSize, tileSize, Color.gray);
            
            // Draw pattern label if enabled
            if (showPatternLabels)
            {
                DrawPatternLabel(texture, pattern, startX + 2, startY + 2);
            }
        }
        
        private void FillQuadrant(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    if (x + dx < texture.width && y + dy < texture.height)
                    {
                        texture.SetPixel(x + dx, y + dy, color);
                    }
                }
            }
        }
        
        private void DrawLine(Texture2D texture, int x1, int y1, int x2, int y2, Color color)
        {
            // Simple line drawing (vertical and horizontal only)
            if (x1 == x2) // Vertical line
            {
                for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
                {
                    if (x1 < texture.width && y < texture.height)
                        texture.SetPixel(x1, y, color);
                }
            }
            else if (y1 == y2) // Horizontal line
            {
                for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
                {
                    if (x < texture.width && y1 < texture.height)
                        texture.SetPixel(x, y1, color);
                }
            }
        }
        
        private void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            // Draw border only
            DrawLine(texture, x, y, x + width - 1, y, color); // Top
            DrawLine(texture, x, y + height - 1, x + width - 1, y + height - 1, color); // Bottom
            DrawLine(texture, x, y, x, y + height - 1, color); // Left
            DrawLine(texture, x + width - 1, y, x + width - 1, y + height - 1, color); // Right
        }
        
        private void DrawPatternLabel(Texture2D texture, PatternDefinition pattern, int x, int y)
        {
            // Simple text rendering - just show pattern as numbers
            string label = $"{(int)pattern.topLeft}{(int)pattern.topRight}{(int)pattern.bottomLeft}{(int)pattern.bottomRight}";
            
            // This would need a more sophisticated text rendering system
            // For now, just draw a small colored square to indicate the pattern
            Color labelColor = Color.Lerp(Color.black, Color.white, 0.7f);
            for (int dy = 0; dy < 8; dy++)
            {
                for (int dx = 0; dx < 20; dx++)
                {
                    if (x + dx < texture.width && y + dy < texture.height)
                    {
                        texture.SetPixel(x + dx, y + dy, labelColor);
                    }
                }
            }
        }
        
        private Color GetColorForTerrain(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Empty: return emptyColor;
                case TerrainType.Diggable: return diggableColor;
                case TerrainType.Undiggable: return undiggableColor;
                default: return Color.magenta; // Error color
            }
        }
        
        private List<PatternDefinition> GenerateAllPatterns()
        {
            var patterns = new List<PatternDefinition>();
            
            // Generate all 80 non-empty patterns in the same order as JavaScript
            for (int tl = 0; tl <= 2; tl++)
            {
                for (int tr = 0; tr <= 2; tr++)
                {
                    for (int bl = 0; bl <= 2; bl++)
                    {
                        for (int br = 0; br <= 2; br++)
                        {
                            // Skip all-empty pattern
                            if (tl == 0 && tr == 0 && bl == 0 && br == 0) continue;
                            
                            patterns.Add(new PatternDefinition
                            {
                                topLeft = (TerrainType)tl,
                                topRight = (TerrainType)tr,
                                bottomLeft = (TerrainType)bl,
                                bottomRight = (TerrainType)br
                            });
                        }
                    }
                }
            }
            
            return patterns;
        }
        
        private void SaveTexture(Texture2D texture, string filename)
        {
            byte[] bytes = texture.EncodeToPNG();
            string path = Path.Combine(Application.dataPath, $"{filename}.png");
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            
            Debug.Log($"Texture saved to: {path}");
        }
        
        private void ValidateAllMappings()
        {
            if (patternMapper == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Pattern Mapper", "OK");
                return;
            }
            
            patternMapper.ValidateAllMappings();
            
            // Additional validation: check if we have all 80 patterns
            var patterns = GenerateAllPatterns();
            Debug.Log($"Expected patterns: {patterns.Count}, Mapped patterns: {GetPatternCount()}");
            
            if (patterns.Count != GetPatternCount())
            {
                Debug.LogWarning($"Pattern count mismatch! Expected 80, found {GetPatternCount()}");
            }
        }
        
        private struct PatternDefinition
        {
            public TerrainType topLeft;
            public TerrainType topRight;
            public TerrainType bottomLeft;
            public TerrainType bottomRight;
        }
    }
#endif
}

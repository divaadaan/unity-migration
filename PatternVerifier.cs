using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public int tileSize = 200; // Updated to match your 200px tiles
        public int artistColumns = 8; // Artist tilemap columns
        public int artistRows = 10; // Artist tilemap rows
        public bool generateComparisonImage = true;
        public bool showPatternLabels = true;
        public bool highlightMismatches = true;
        public bool flipYAxis = false; // Option to flip Y coordinate
        
        [Header("Color Scheme")]
        public Color emptyColor = new Color(0.9f, 0.95f, 1f, 1f);
        public Color diggableColor = new Color(0.54f, 0.45f, 0.33f, 1f);
        public Color undiggableColor = new Color(0.17f, 0.17f, 0.17f, 1f);
        
        private Vector2 scrollPosition;
        private List<PatternMismatch> mismatches = new List<PatternMismatch>();
        
        private struct PatternMismatch
        {
            public int index;
            public string systematicPattern;
            public string artistPattern;
            public int col;
            public int row;
        }
        
        [MenuItem("Mining Game/Pattern Verifier")]
        public static void ShowWindow()
        {
            GetWindow<PatternVerifier>("Pattern Verifier");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Pattern Mapping Verifier", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Compares systematic tile generation with artist tilemap", MessageType.Info);
            
            EditorGUILayout.Space();
            
            patternMapper = EditorGUILayout.ObjectField("Pattern Mapper", patternMapper, typeof(PatternMapper), false) as PatternMapper;
            artistTilemap = EditorGUILayout.ObjectField("Artist Tilemap", artistTilemap, typeof(Texture2D), false) as Texture2D;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Artist Tilemap Layout", EditorStyles.boldLabel);
            artistColumns = EditorGUILayout.IntField("Columns", artistColumns);
            artistRows = EditorGUILayout.IntField("Rows", artistRows);
            tileSize = EditorGUILayout.IntField("Tile Size (pixels)", tileSize);
            
            EditorGUILayout.Space();
            flipYAxis = EditorGUILayout.Toggle("Flip Y Axis", flipYAxis);
            generateComparisonImage = EditorGUILayout.Toggle("Generate Comparison", generateComparisonImage);
            showPatternLabels = EditorGUILayout.Toggle("Show Pattern Labels", showPatternLabels);
            highlightMismatches = EditorGUILayout.Toggle("Highlight Mismatches", highlightMismatches);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Generate Comparison Grid"))
            {
                GenerateComparisonGrid();
            }
            
            if (GUILayout.Button("Export Pattern Array (JavaScript)"))
            {
                ExportPatternArray();
            }
            
            if (GUILayout.Button("Analyze Artist Tilemap"))
            {
                AnalyzeArtistTilemap();
            }
            
            // Show mismatches if any
            if (mismatches.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Found {mismatches.Count} mismatches:", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                foreach (var mismatch in mismatches)
                {
                    EditorGUILayout.LabelField($"Index {mismatch.index} ({mismatch.col},{mismatch.row}): " +
                        $"Expected {mismatch.systematicPattern} but got {mismatch.artistPattern}");
                }
                EditorGUILayout.EndScrollView();
            }
        }
        
        private void GenerateComparisonGrid()
        {
            if (artistTilemap == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign Artist Tilemap", "OK");
                return;
            }
            
            mismatches.Clear();
            
            // Create side-by-side comparison
            var texture = new Texture2D(artistColumns * tileSize * 2 + 10, artistRows * tileSize, TextureFormat.RGBA32, false);
            
            // Fill with background
            var bgColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, bgColor);
                }
            }
            
            // Generate all patterns
            var patterns = GenerateAllPatterns();
            
            // Draw systematic tiles (left side)
            for (int i = 0; i < patterns.Count && i < 80; i++)
            {
                int col = i % artistColumns;
                int row = i / artistColumns;
                
                if (flipYAxis)
                {
                    row = (artistRows - 1) - row;
                }
                
                DrawPatternTile(texture, patterns[i], col * tileSize, row * tileSize, i);
            }
            
            // Draw divider
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    texture.SetPixel(artistColumns * tileSize + x, y, Color.red);
                }
            }
            
            // Copy artist tilemap (right side) and analyze
            if (artistTilemap != null)
            {
                var artistPixels = artistTilemap.GetPixels();
                int artistWidth = artistTilemap.width;
                int artistHeight = artistTilemap.height;
                
                for (int i = 0; i < 80; i++)
                {
                    int col = i % artistColumns;
                    int row = i / artistColumns;
                    
                    if (flipYAxis)
                    {
                        row = (artistRows - 1) - row;
                    }
                    
                    // Copy tile from artist tilemap
                    for (int ty = 0; ty < tileSize; ty++)
                    {
                        for (int tx = 0; tx < tileSize; tx++)
                        {
                            int srcX = col * tileSize + tx;
                            int srcY = row * tileSize + ty;
                            int dstX = artistColumns * tileSize + 10 + col * tileSize + tx;
                            int dstY = row * tileSize + ty;
                            
                            if (srcX < artistWidth && srcY < artistHeight)
                            {
                                Color artistColor = artistPixels[srcY * artistWidth + srcX];
                                texture.SetPixel(dstX, dstY, artistColor);
                                
                                // Analyze for mismatches
                                if (highlightMismatches && tx == tileSize/2 && ty == tileSize/2)
                                {
                                    var expectedPattern = patterns[i];
                                    var actualPattern = AnalyzeTilePattern(artistTilemap, col, row);
                                    
                                    if (!PatternsMatch(expectedPattern, actualPattern))
                                    {
                                        mismatches.Add(new PatternMismatch
                                        {
                                            index = i,
                                            systematicPattern = PatternToString(expectedPattern),
                                            artistPattern = PatternToString(actualPattern),
                                            col = col,
                                            row = row
                                        });
                                        
                                        // Draw red border on mismatched tiles
                                        DrawBorder(texture, dstX - tileSize/2, dstY - tileSize/2, tileSize, Color.red, 3);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            texture.Apply();
            SaveTexture(texture, "ComparisonGrid_Fixed");
            
            if (mismatches.Count > 0)
            {
                Debug.LogWarning($"Found {mismatches.Count} pattern mismatches!");
            }
            else
            {
                Debug.Log("All patterns match correctly!");
            }
        }
        
        private PatternDefinition AnalyzeTilePattern(Texture2D tilemap, int tileCol, int tileRow)
        {
            // Sample the four quadrants of a tile to determine its pattern
            var pixels = tilemap.GetPixels(
                tileCol * tileSize, 
                tileRow * tileSize, 
                tileSize, 
                tileSize
            );
            
            int quarter = tileSize / 4;
            int half = tileSize / 2;
            
            // Sample center of each quadrant
            var tl = ClassifyPixel(pixels[quarter * tileSize + quarter]);
            var tr = ClassifyPixel(pixels[quarter * tileSize + (half + quarter)]);
            var bl = ClassifyPixel(pixels[(half + quarter) * tileSize + quarter]);
            var br = ClassifyPixel(pixels[(half + quarter) * tileSize + (half + quarter)]);
            
            return new PatternDefinition { topLeft = tl, topRight = tr, bottomLeft = bl, bottomRight = br };
        }
        
        private TerrainType ClassifyPixel(Color pixel)
        {
            // Classify based on your artist's color scheme
            // White/light = empty
            if (pixel.r > 0.8f && pixel.g > 0.8f && pixel.b > 0.8f)
                return TerrainType.Empty;
            
            // Blue = diggable
            if (pixel.b > pixel.r && pixel.b > pixel.g)
                return TerrainType.Diggable;
            
            // Black/red = undiggable
            return TerrainType.Undiggable;
        }
        
        private bool PatternsMatch(PatternDefinition a, PatternDefinition b)
        {
            return a.topLeft == b.topLeft && 
                   a.topRight == b.topRight && 
                   a.bottomLeft == b.bottomLeft && 
                   a.bottomRight == b.bottomRight;
        }
        
        private string PatternToString(PatternDefinition p)
        {
            return $"({(int)p.topLeft},{(int)p.topRight},{(int)p.bottomLeft},{(int)p.bottomRight})";
        }
        
        private void DrawBorder(Texture2D texture, int x, int y, int size, Color color, int thickness)
        {
            for (int t = 0; t < thickness; t++)
            {
                // Top and bottom
                for (int i = 0; i < size; i++)
                {
                    texture.SetPixel(x + i, y + t, color);
                    texture.SetPixel(x + i, y + size - 1 - t, color);
                }
                // Left and right
                for (int i = 0; i < size; i++)
                {
                    texture.SetPixel(x + t, y + i, color);
                    texture.SetPixel(x + size - 1 - t, y + i, color);
                }
            }
        }
        
        private void ExportPatternArray()
        {
            if (patternMapper == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Pattern Mapper", "OK");
                return;
            }
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("// Auto-generated pattern array for 80 tiles (8x10 layout)");
            sb.AppendLine("// Each index maps to artist tilemap position");
            sb.AppendLine("const ARTIST_TILE_PATTERNS = [");
            
            var patterns = GenerateAllPatterns();
            for (int i = 0; i < patterns.Count && i < 80; i++)
            {
                var p = patterns[i];
                int col = i % artistColumns;
                int row = i / artistColumns;
                
                sb.AppendLine($"  {{ index: {i}, col: {col}, row: {row}, " +
                    $"pattern: [{(int)p.topLeft}, {(int)p.topRight}, {(int)p.bottomLeft}, {(int)p.bottomRight}] }},");
            }
            
            sb.AppendLine("];");
            
            string path = Path.Combine(Application.dataPath, "ArtistPatternArray.js");
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
            
            Debug.Log($"Pattern array exported to: {path}");
            EditorUtility.DisplayDialog("Success", $"Pattern array exported to:\n{path}", "OK");
        }
        
        private void AnalyzeArtistTilemap()
        {
            if (artistTilemap == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign Artist Tilemap", "OK");
                return;
            }
            
            Debug.Log($"Analyzing artist tilemap: {artistTilemap.width}x{artistTilemap.height}px");
            Debug.Log($"Expected: {artistColumns * tileSize}x{artistRows * tileSize}px");
            Debug.Log($"Tiles: {artistColumns}x{artistRows} = {artistColumns * artistRows} tiles");
            
            // Analyze each tile
            for (int i = 0; i < 80; i++)
            {
                int col = i % artistColumns;
                int row = i / artistColumns;
                var pattern = AnalyzeTilePattern(artistTilemap, col, row);
                Debug.Log($"Tile {i} ({col},{row}): {PatternToString(pattern)}");
            }
        }
        
        // Rest of the helper methods remain the same...
        private void DrawPatternTile(Texture2D texture, PatternDefinition pattern, int startX, int startY, int index)
        {
            int halfSize = tileSize / 2;
            
            // Draw quadrants
            FillQuadrant(texture, startX, startY, halfSize, halfSize, GetColorForTerrain(pattern.topLeft));
            FillQuadrant(texture, startX + halfSize, startY, halfSize, halfSize, GetColorForTerrain(pattern.topRight));
            FillQuadrant(texture, startX, startY + halfSize, halfSize, halfSize, GetColorForTerrain(pattern.bottomLeft));
            FillQuadrant(texture, startX + halfSize, startY + halfSize, halfSize, halfSize, GetColorForTerrain(pattern.bottomRight));
            
            // Draw dividers
            DrawLine(texture, startX + halfSize, startY, startX + halfSize, startY + tileSize, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            DrawLine(texture, startX, startY + halfSize, startX + tileSize, startY + halfSize, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            
            // Draw index label
            if (showPatternLabels)
            {
                // Would need proper text rendering here
                // For now, just a colored indicator
                for (int y = 0; y < 20; y++)
                {
                    for (int x = 0; x < 30; x++)
                    {
                        if (startX + x < texture.width && startY + y < texture.height)
                        {
                            texture.SetPixel(startX + x + 5, startY + y + 5, new Color(1, 1, 1, 0.8f));
                        }
                    }
                }
            }
        }
        
        private List<PatternDefinition> GenerateAllPatterns()
        {
            var patterns = new List<PatternDefinition>();
            
            for (int tl = 0; tl <= 2; tl++)
            {
                for (int tr = 0; tr <= 2; tr++)
                {
                    for (int bl = 0; bl <= 2; bl++)
                    {
                        for (int br = 0; br <= 2; br++)
                        {
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
        
        // Other helper methods remain the same...
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
            if (x1 == x2) // Vertical
            {
                for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
                {
                    if (x1 < texture.width && y < texture.height)
                        texture.SetPixel(x1, y, color);
                }
            }
            else if (y1 == y2) // Horizontal
            {
                for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
                {
                    if (x < texture.width && y1 < texture.height)
                        texture.SetPixel(x, y1, color);
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
                default: return Color.magenta;
            }
        }
        
        private void SaveTexture(Texture2D texture, string filename)
        {
            byte[] bytes = texture.EncodeToPNG();
            string path = Path.Combine(Application.dataPath, $"{filename}.png");
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            Debug.Log($"Texture saved to: {path}");
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
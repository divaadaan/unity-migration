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
        public int tileSize = 200; 
        public int artistColumns = 8; // Artist tilemap columns
        public int artistRows = 10; // Artist tilemap rows
        public bool generateComparisonImage = true;
        public bool showPatternLabels = true;
        public bool highlightMismatches = true;
        
        [Header("Color Scheme")]
        public Color emptyColor = new Color(0.9f, 0.95f, 1f, 1f);
        public Color diggableColor = new Color(0.54f, 0.45f, 0.33f, 1f);
        public Color undiggableColor = new Color(0.17f, 0.17f, 0.17f, 1f);
        
        private Vector2 scrollPosition;
        private List<PatternMismatch> mismatches = new List<PatternMismatch>();
        private string importPath = "";
        
        [System.Serializable]
        private class PatternData
        {
            public int index;
            public int col;
            public int row;
            public int[] pattern;
        }
        
        [System.Serializable]
        private class PatternArrayData
        {
            public List<PatternData> patterns;
        }
        
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
            generateComparisonImage = EditorGUILayout.Toggle("Generate Comparison", generateComparisonImage);
            showPatternLabels = EditorGUILayout.Toggle("Show Pattern Labels", showPatternLabels);
            highlightMismatches = EditorGUILayout.Toggle("Highlight Mismatches", highlightMismatches);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Generate Comparison Grid"))
            {
                GenerateComparisonGrid();
            }
            
            if (GUILayout.Button("Analyze Artist Tilemap"))
            {
                AnalyzeArtistTilemap();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pattern Array Import/Export", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Export Pattern Array (JSON)"))
            {
                ExportPatternArrayJSON();
            }
            
            EditorGUILayout.BeginHorizontal();
            importPath = EditorGUILayout.TextField("Import File:", importPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("Select Pattern Array JSON", Application.dataPath, "json");
                if (!string.IsNullOrEmpty(path))
                {
                    importPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Import Pattern Array (JSON)"))
            {
                ImportPatternArrayJSON();
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
            var texture = new Texture2D(
                artistColumns * tileSize * 2 + 10,
                artistRows * tileSize,
                TextureFormat.RGBA32,
                false
            );

            // Fill background
            var bgColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            texture.SetPixels(Enumerable.Repeat(bgColor, texture.width * texture.height).ToArray());

            // Generate all patterns
            var patterns = GenerateAllPatterns();

            // ---- Left side: systematic tiles ----
            for (int i = 0; i < patterns.Count && i < 80; i++)
            {
                int col = i % artistColumns;
                int logicalRow = i / artistColumns;
                int drawRow = artistRows - 1 - logicalRow;

                DrawPatternTile(texture, patterns[i], col * tileSize, drawRow * tileSize, i);
            }

            // ---- Divider ----
            int dividerX = artistColumns * tileSize;
            Color[] dividerBlock = Enumerable.Repeat(Color.red, 10 * texture.height).ToArray();
            texture.SetPixels(dividerX, 0, 10, texture.height, dividerBlock);

            // ---- Right side: copy artist tiles ----
            int artistWidth = artistTilemap.width;
            int artistHeight = artistTilemap.height;

            for (int i = 0; i < 80; i++)
            {
                int col = i % artistColumns;
                int logicalRow = i / artistColumns;
                int artistTextureRow = artistRows - 1 - logicalRow;

                int srcX = col * tileSize;
                int srcY = artistTextureRow * tileSize;
                int dstX = artistColumns * tileSize + 10 + col * tileSize;
                int dstY = artistTextureRow * tileSize;

                if (srcX + tileSize <= artistWidth && srcY + tileSize <= artistHeight)
                {
                    // Copy tile block in one call
                    Color[] block = artistTilemap.GetPixels(srcX, srcY, tileSize, tileSize);
                    texture.SetPixels(dstX, dstY, tileSize, tileSize, block);

                    if (highlightMismatches)
                    {
                        var expectedPattern = patterns[i];
                        var actualPattern = AnalyzeTilePattern(artistTilemap, col, artistTextureRow);

                        if (!PatternsMatch(expectedPattern, actualPattern))
                        {
                            mismatches.Add(new PatternMismatch
                            {
                                index = i,
                                systematicPattern = PatternToString(expectedPattern),
                                artistPattern = PatternToString(actualPattern),
                                col = col,
                                row = logicalRow
                            });

                            // Fast border draw (still batched)
                            DrawBorder(texture, dstX, dstY, tileSize, Color.red, 3);
                        }
                    }
                }
            }

            // ---- Single Apply here ----
            texture.Apply();

            SaveTexture(texture, "ComparisonGrid");

            if (mismatches.Count > 0)
                Debug.LogWarning($"Found {mismatches.Count} pattern mismatches!");
            else
                Debug.Log("All patterns match correctly!");
        }


        
        private void ExportPatternArrayJSON()
        {
            var patterns = GenerateAllPatterns();
            var patternList = new List<PatternData>();
            
            for (int i = 0; i < patterns.Count && i < 80; i++)
            {
                var p = patterns[i];
                patternList.Add(new PatternData
                {
                    index = i,
                    col = i % artistColumns,
                    row = i / artistColumns,
                    pattern = new int[] { (int)p.topLeft, (int)p.topRight, (int)p.bottomLeft, (int)p.bottomRight }
                });
            }
            
            var data = new PatternArrayData { patterns = patternList };
            string json = JsonUtility.ToJson(data, true);
            
            string path = EditorUtility.SaveFilePanel(
                "Save Pattern Array JSON",
                Application.dataPath,
                "ArtistPatternArray",
                "json"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, json);
                AssetDatabase.Refresh();
                Debug.Log($"Pattern array (JSON) exported to: {path}");
                EditorUtility.DisplayDialog("Success", $"Pattern array exported successfully", "OK");
            }
        }
        
        private void ImportPatternArrayJSON()
        {
            if (string.IsNullOrEmpty(importPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a JSON file to import", "OK");
                return;
            }
            
            if (!File.Exists(importPath))
            {
                EditorUtility.DisplayDialog("Error", "File does not exist", "OK");
                return;
            }
            
            if (patternMapper == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Pattern Mapper to import into", "OK");
                return;
            }
            
            try
            {
                string jsonContent = File.ReadAllText(importPath);
                var data = JsonUtility.FromJson<PatternArrayData>(jsonContent);
                
                if (data == null || data.patterns == null || data.patterns.Count == 0)
                {
                    EditorUtility.DisplayDialog("Error", "No patterns found in JSON file", "OK");
                    return;
                }
                               
                var newMappings = new List<PatternMapper.PatternMapping>();
                foreach (var pattern in data.patterns)
                {
                    if (pattern.pattern != null && pattern.pattern.Length == 4)
                    {
                        var mapping = new PatternMapper.PatternMapping
                        {
                            topLeft = (TerrainType)pattern.pattern[0],
                            topRight = (TerrainType)pattern.pattern[1],
                            bottomLeft = (TerrainType)pattern.pattern[2],
                            bottomRight = (TerrainType)pattern.pattern[3],
                            artistColumn = pattern.col,
                            artistRow = pattern.row
                        };
                        mapping.UpdateCalculatedValues();
                        newMappings.Add(mapping);
                    }
                }

                int appliedCount = patternMapper.SetMappings(newMappings);

                Debug.Log($"Imported {appliedCount} pattern mappings from JSON into {patternMapper.name}");
                EditorUtility.DisplayDialog("Success", $"Imported {appliedCount} patterns successfully", "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to import JSON: {e.Message}", "OK");
                Debug.LogError(e);
            }
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
            int width = texture.width;
            int height = texture.height;

            int clampedX = Mathf.Clamp(x, 0, width - 1);
            int clampedY = Mathf.Clamp(y, 0, height - 1);
            int clampedSize = Mathf.Min(size, width - clampedX, height - clampedY);

            var horiz = Enumerable.Repeat(color, clampedSize * thickness).ToArray();
            var vert = Enumerable.Repeat(color, clampedSize * thickness).ToArray();
            
            //top
            texture.SetPixels(clampedX, clampedY + clampedSize - thickness, clampedSize, thickness, horiz);

            //bottom
            texture.SetPixels(clampedX, clampedY, clampedSize, thickness, horiz);

            // Left strip
            texture.SetPixels(clampedX, clampedY, thickness, clampedSize, vert);

            // Right strip
            texture.SetPixels(clampedX + clampedSize - thickness, clampedY, thickness, clampedSize, vert);
        }

        
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
                // Simple index indicator
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
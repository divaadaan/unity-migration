using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MiningGame
{
#if UNITY_EDITOR
    public class PatternVerifier : EditorWindow
    {
        // Core references
        private PatternMapper patternMapper;
        private Texture2D artistTilemap;
        
        // Settings
        private int tileSize = 200;
        private int columns = 8;
        private int rows = 10;
        
        // Results
        private Texture2D comparisonTexture;
        private List<string> errors = new List<string>();
        
        [MenuItem("Mining Game/Pattern Verifier")]
        public static void ShowWindow()
        {
            var window = GetWindow<PatternVerifier>("Pattern Verifier");
            window.minSize = new Vector2(400, 500);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Tile Pattern Verifier", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Input fields
            patternMapper = EditorGUILayout.ObjectField("Pattern Mapper", patternMapper, typeof(PatternMapper), false) as PatternMapper;
            artistTilemap = EditorGUILayout.ObjectField("Artist Tilemap", artistTilemap, typeof(Texture2D), false) as Texture2D;
            
            EditorGUILayout.Space();
            tileSize = EditorGUILayout.IntField("Tile Size (px)", tileSize);
            columns = EditorGUILayout.IntField("Columns", columns);
            rows = EditorGUILayout.IntField("Rows", rows);
            
            EditorGUILayout.Space();
            
            // Action buttons
            if (GUILayout.Button("Generate Comparison", GUILayout.Height(30)))
            {
                GenerateComparison();
            }
            
            if (comparisonTexture != null)
            {
                if (GUILayout.Button("Save Comparison Image"))
                {
                    SaveTexture(comparisonTexture);
                }
            }
            
            // Display errors
            if (errors.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Issues Found: {errors.Count}", EditorStyles.boldLabel);
                
                var scrollRect = GUILayoutUtility.GetRect(0, 150, GUILayout.ExpandWidth(true));
                using (var scrollView = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(150)))
                {
                    foreach (var error in errors)
                    {
                        EditorGUILayout.LabelField(error, EditorStyles.wordWrappedLabel);
                    }
                }
            }
            
            // Display preview
            if (comparisonTexture != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Preview:", EditorStyles.boldLabel);
                
                var rect = GUILayoutUtility.GetRect(380, 200);
                GUI.DrawTexture(rect, comparisonTexture, ScaleMode.ScaleToFit);
            }
        }
        
        private void GenerateComparison()
        {
            errors.Clear();
            
            // Validation
            if (patternMapper == null)
            {
                errors.Add("ERROR: Pattern Mapper not assigned");
                return;
            }
            
            if (artistTilemap == null)
            {
                errors.Add("ERROR: Artist Tilemap not assigned");
                return;
            }
            
            Debug.Log($"Starting comparison generation...");
            Debug.Log($"Artist tilemap: {artistTilemap.width}x{artistTilemap.height}px");
            Debug.Log($"Expected: {columns * tileSize}x{rows * tileSize}px");
            
            // Initialize pattern mapper
            patternMapper.InitializeLookups();
            
            // Create texture (side by side with gap)
            int gap = 20;
            int width = columns * tileSize * 2 + gap;
            int height = rows * tileSize;
            
            comparisonTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            comparisonTexture.filterMode = FilterMode.Point;
            
            // Fill background
            var bgColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = bgColor;
            comparisonTexture.SetPixels(pixels);
            
            // Generate all 80 patterns (excluding all-empty)
            var patterns = GenerateAllPatterns();
            
            Debug.Log($"Generated {patterns.Count} patterns to verify");
            
            // Draw systematic tiles on the left
            DrawSystematicTiles(patterns);
            
            // Draw corresponding artist tiles on the right
            DrawArtistTiles(patterns);
            
            // Draw divider
            DrawDivider(gap);
            
            // Apply changes
            comparisonTexture.Apply();
            
            Debug.Log($"Comparison complete. {errors.Count} issues found.");
        }
        
        private List<Pattern> GenerateAllPatterns()
        {
            var patterns = new List<Pattern>();
            int index = 0;
            
            for (int tl = 0; tl <= 2; tl++)
            {
                for (int tr = 0; tr <= 2; tr++)
                {
                    for (int bl = 0; bl <= 2; bl++)
                    {
                        for (int br = 0; br <= 2; br++)
                        {
                            // Skip all-empty
                            if (tl == 0 && tr == 0 && bl == 0 && br == 0)
                                continue;
                            
                            patterns.Add(new Pattern
                            {
                                index = index++,
                                tl = (TerrainType)tl,
                                tr = (TerrainType)tr,
                                bl = (TerrainType)bl,
                                br = (TerrainType)br
                            });
                        }
                    }
                }
            }
            
            return patterns;
        }
        
        private void DrawSystematicTiles(List<Pattern> patterns)
        {
            Debug.Log("Drawing systematic tiles...");
    
            for (int i = 0; i < patterns.Count && i < 80; i++)
            {
                var pattern = patterns[i];
                int col = i % columns;
                int row = i / columns;
        
                int x = col * tileSize;
                int y = row * tileSize;  // Row 0 at bottom, increasing upward
        
                DrawSystematicTile(x, y, pattern);
            }
        }
        
        private void DrawSystematicTile(int x, int y, Pattern pattern)
        {
            int half = tileSize / 2;
            
            // Colors for each terrain type
            var colors = new Dictionary<TerrainType, Color>
            {
                { TerrainType.Empty, new Color(0.95f, 0.95f, 0.95f, 1f) },
                { TerrainType.Diggable, new Color(0.4f, 0.6f, 1f, 1f) },  // Blue
                { TerrainType.Undiggable, new Color(0.8f, 0.2f, 0.2f, 1f) } // Red
            };
            
            // Draw quadrants
            DrawQuadrant(x, y + half, half, colors[pattern.tl]); // Top-left
            DrawQuadrant(x + half, y + half, half, colors[pattern.tr]); // Top-right
            DrawQuadrant(x, y, half, colors[pattern.bl]); // Bottom-left
            DrawQuadrant(x + half, y, half, colors[pattern.br]); // Bottom-right
            
            // Draw grid lines
            DrawLine(x + half, y, x + half, y + tileSize, Color.black, 2);
            DrawLine(x, y + half, x + tileSize, y + half, Color.black, 2);
            DrawBorder(x, y, tileSize, Color.black, 2);
            
            // Add index label
            DrawLabel(x + 5, y + tileSize - 20, pattern.index.ToString());
        }
        
        private void DrawArtistTiles(List<Pattern> patterns)
        {
            Debug.Log("Drawing artist tiles...");
    
            int xOffset = columns * tileSize + 20; // Right side offset
    
            for (int i = 0; i < patterns.Count && i < 80; i++)
            {
                var pattern = patterns[i];
        
                // Get artist tile position from mapper (this is our source of truth)
                var (artistCol, artistRow) = patternMapper.GetArtistPosition(
                    pattern.tl, pattern.tr, pattern.bl, pattern.br
                );
        
                // Display position (where it should appear in grid)
                int displayCol = i % columns;
                int displayRow = i / columns;
                int displayX = xOffset + displayCol * tileSize;
                int displayY = displayRow * tileSize;
        
                // Source position in artist tilemap
                int sourceX = artistCol * tileSize;
                int sourceY = artistRow * tileSize;
        
                // Validate source position is within bounds
                if (sourceX < 0 || sourceY < 0 || 
                    sourceX + tileSize > artistTilemap.width || 
                    sourceY + tileSize > artistTilemap.height)
                {
                    errors.Add($"Pattern {i} ({pattern.tl},{pattern.tr},{pattern.bl},{pattern.br}): " +
                              $"Artist position ({artistCol},{artistRow}) is out of bounds");
                    DrawErrorTile(displayX, displayY);
                    continue;
                }
        
                // Copy artist tile - no verification needed, we trust the mapping
                try
                {
                    var tilePixels = artistTilemap.GetPixels(sourceX, sourceY, tileSize, tileSize);
                    comparisonTexture.SetPixels(displayX, displayY, tileSize, tileSize, tilePixels);
            
                    // Optional: Add subtle border to match left side
                    DrawBorder(displayX, displayY, tileSize, new Color(0.3f, 0.3f, 0.3f, 1f), 1);
                }
                catch (System.Exception e)
                {
                    errors.Add($"Pattern {i}: Failed to copy tile - {e.Message}");
                    DrawErrorTile(displayX, displayY);
                }
            }
        }
        
        private void DrawQuadrant(int x, int y, int size, Color color)
        {
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            comparisonTexture.SetPixels(x, y, size, size, pixels);
        }
        
        private void DrawErrorTile(int x, int y)
        {
            var pixels = new Color[tileSize * tileSize];
            var errorColor = new Color(1f, 0f, 1f, 1f); // Magenta
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = errorColor;
            comparisonTexture.SetPixels(x, y, tileSize, tileSize, pixels);
        }
        
        private void DrawLine(int x1, int y1, int x2, int y2, Color color, int thickness)
        {
            // Simple line drawing (vertical or horizontal only)
            if (x1 == x2) // Vertical
            {
                for (int t = 0; t < thickness; t++)
                {
                    for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
                    {
                        if (x1 + t - thickness/2 >= 0 && x1 + t - thickness/2 < comparisonTexture.width && 
                            y >= 0 && y < comparisonTexture.height)
                            comparisonTexture.SetPixel(x1 + t - thickness/2, y, color);
                    }
                }
            }
            else if (y1 == y2) // Horizontal
            {
                for (int t = 0; t < thickness; t++)
                {
                    for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
                    {
                        if (x >= 0 && x < comparisonTexture.width && 
                            y1 + t - thickness/2 >= 0 && y1 + t - thickness/2 < comparisonTexture.height)
                            comparisonTexture.SetPixel(x, y1 + t - thickness/2, color);
                    }
                }
            }
        }
        
        private void DrawBorder(int x, int y, int size, Color color, int thickness)
        {
            DrawLine(x, y, x + size, y, color, thickness);
            DrawLine(x, y + size, x + size, y + size, color, thickness);
            DrawLine(x, y, x, y + size, color, thickness);
            DrawLine(x + size, y, x + size, y + size, color, thickness);
        }
        
        private void DrawDivider(int gap)
        {
            int x = columns * tileSize;
            var dividerColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            
            for (int dx = 0; dx < gap; dx++)
            {
                for (int y = 0; y < comparisonTexture.height; y++)
                {
                    comparisonTexture.SetPixel(x + dx, y, dividerColor);
                }
            }
        }
        
        private void DrawLabel(int x, int y, string text)
        {
            // Simple text placeholder - draws a small white rectangle
            var labelColor = Color.white;
            for (int dx = 0; dx < 20; dx++)
            {
                for (int dy = 0; dy < 15; dy++)
                {
                    if (x + dx < comparisonTexture.width && y + dy < comparisonTexture.height)
                        comparisonTexture.SetPixel(x + dx, y + dy, labelColor);
                }
            }
        }
        
        private void SaveTexture(Texture2D texture)
        {
            string path = EditorUtility.SaveFilePanel(
                "Save Comparison Image",
                Application.dataPath,
                "TileComparison",
                "png"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                Debug.Log($"Saved comparison to: {path}");
                AssetDatabase.Refresh();
            }
        }
        
        private class Pattern
        {
            public int index;
            public TerrainType tl, tr, bl, br;
        }
    }
#endif
}
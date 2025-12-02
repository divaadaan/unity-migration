using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace DigDigDiner
{
#if UNITY_EDITOR
    public class PatternVerifier : EditorWindow
    {
        private TileMapping tileMapping;
        private Texture2D artistTilemap;
        
        private int tileSize = SharedConstants.SOURCE_TILE_SIZE;
        private int columns = SharedConstants.TILEMAP_COLUMNS;
        private int rows = SharedConstants.TILEMAP_ROWS;
        
        private Texture2D comparisonTexture;
        private List<string> errors = new List<string>();
        
        private const int DIVIDER_GAP = 20;
        private const int MIN_WINDOW_WIDTH = 400;
        private const int MIN_WINDOW_HEIGHT = 500;
        private const int GENERATE_BUTTON_HEIGHT = 30;
        private const int ERROR_SCROLL_HEIGHT = 150;
        private const int PREVIEW_HEIGHT = 200;
        private const int PREVIEW_WIDTH = 380;
        
        private const int QUADRANT_DIVISIONS = 2;
        private const int LINE_THICKNESS_HEAVY = 2;
        private const int LINE_THICKNESS_LIGHT = 1;
        private const int LABEL_WIDTH = 20;
        private const int LABEL_HEIGHT = 15;
        private const int LABEL_OFFSET_X = 5;
        private const int LABEL_OFFSET_Y = 20;
        
        private static readonly Color BACKGROUND_COLOR = new Color(0.2f, 0.2f, 0.2f, 1f);
        private static readonly Color SYSTEMATIC_STATE_0 = new Color(0.95f, 0.95f, 0.95f, 1f); 
        private static readonly Color SYSTEMATIC_STATE_1 = new Color(0.4f, 0.6f, 1f, 1f);
        private static readonly Color SYSTEMATIC_STATE_2 = new Color(0.8f, 0.2f, 0.2f, 1f);
        private static readonly Color SYSTEMATIC_STATE_OTHER = Color.gray;

        private static readonly Color BORDER_COLOR_DARK = Color.black;
        private static readonly Color BORDER_COLOR_LIGHT = new Color(0.3f, 0.3f, 0.3f, 1f);
        private static readonly Color DIVIDER_COLOR = new Color(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color LABEL_COLOR = Color.white;
        private static readonly Color ERROR_TILE_COLOR = new Color(1f, 0f, 1f, 1f);
        
        [MenuItem("Mining Game/Pattern Verifier")]
        public static void ShowWindow()
        {
            var window = GetWindow<PatternVerifier>("Pattern Verifier");
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Tile Pattern Verifier", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            tileMapping = EditorGUILayout.ObjectField("Tile Mapper", tileMapping, typeof(TileMapping), false) as TileMapping;
            artistTilemap = EditorGUILayout.ObjectField("Artist Tilemap", artistTilemap, typeof(Texture2D), false) as Texture2D;
            
            EditorGUILayout.Space();
            tileSize = EditorGUILayout.IntField("Tile Size (px)", tileSize);
            columns = EditorGUILayout.IntField("Columns", columns);
            rows = EditorGUILayout.IntField("Rows", rows);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Generate Comparison", GUILayout.Height(GENERATE_BUTTON_HEIGHT)))
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
            
            if (errors.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Issues Found: {errors.Count}", EditorStyles.boldLabel);
                
                using (var scrollView = new EditorGUILayout.ScrollViewScope(Vector2.zero, GUILayout.Height(ERROR_SCROLL_HEIGHT)))
                {
                    foreach (var error in errors)
                    {
                        EditorGUILayout.LabelField(error, EditorStyles.wordWrappedLabel);
                    }
                }
            }
            
            if (comparisonTexture != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Preview:", EditorStyles.boldLabel);
                
                var rect = GUILayoutUtility.GetRect(PREVIEW_WIDTH, PREVIEW_HEIGHT);
                GUI.DrawTexture(rect, comparisonTexture, ScaleMode.ScaleToFit);
            }
        }
        
        private void GenerateComparison()
        {
            errors.Clear();
            
            if (tileMapping == null)
            {
                errors.Add("ERROR: Pattern Mapper not assigned");
                return;
            }
            
            if (artistTilemap == null)
            {
                errors.Add("ERROR: Artist Tilemap not assigned");
                return;
            }
            
            tileMapping.Initialize();
            
            Debug.Log($"Starting comparison generation...");
            Debug.Log($"Artist tilemap: {artistTilemap.width}x{artistTilemap.height}px");
            
            var patterns = GenerateAllPatterns();
            Debug.Log($"Generated {patterns.Count} patterns to verify (State Count: {tileMapping.StateCount})");

            int width = columns * tileSize * QUADRANT_DIVISIONS + DIVIDER_GAP;
            int requiredRows = Mathf.CeilToInt((float)patterns.Count / columns);
            int height = Mathf.Max(rows, requiredRows) * tileSize;
            
            comparisonTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            comparisonTexture.filterMode = FilterMode.Point;

            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = BACKGROUND_COLOR;
            comparisonTexture.SetPixels(pixels);
            
            DrawSystematicTiles(patterns);
            DrawArtistTiles(patterns);            
            DrawDivider();
            
            comparisonTexture.Apply();
            
            Debug.Log($"Comparison complete. {errors.Count} issues found.");
        }
        
        private List<Pattern> GenerateAllPatterns()
        {
            var patterns = new List<Pattern>();
            int index = 0;
            
            int maxState = tileMapping != null ? tileMapping.StateCount : SharedConstants.TERRAIN_TYPE_COUNT;

            for (int tl = 0; tl < maxState; tl++)
            {
                for (int tr = 0; tr < maxState; tr++)
                {
                    for (int bl = 0; bl < maxState; bl++)
                    {
                        for (int br = 0; br < maxState; br++)
                        {
                            patterns.Add(new Pattern
                            {
                                index = index++,
                                tl = tl,
                                tr = tr,
                                bl = bl,
                                br = br
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
    
            for (int i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];
                int col = i % columns;
                int row = i / columns;
        
                int x = col * tileSize;
                int y = row * tileSize;
        
                DrawSystematicTile(x, y, pattern);
            }
        }
        
        private void DrawSystematicTile(int x, int y, Pattern pattern)
        {
            int half = tileSize / QUADRANT_DIVISIONS;
            
            DrawQuadrant(x, y + half, half, GetColorForState(pattern.tl));
            DrawQuadrant(x + half, y + half, half, GetColorForState(pattern.tr));
            DrawQuadrant(x, y, half, GetColorForState(pattern.bl));
            DrawQuadrant(x + half, y, half, GetColorForState(pattern.br));
            
            DrawLine(x + half, y, x + half, y + tileSize, BORDER_COLOR_DARK, LINE_THICKNESS_HEAVY);
            DrawLine(x, y + half, x + tileSize, y + half, BORDER_COLOR_DARK, LINE_THICKNESS_HEAVY);
            
            DrawBorder(x, y, tileSize, BORDER_COLOR_DARK, LINE_THICKNESS_HEAVY);
            DrawLabel(x + LABEL_OFFSET_X, y + tileSize - LABEL_OFFSET_Y, pattern.index.ToString());
        }

        private Color GetColorForState(int state)
        {
            switch (state)
            {
                case 0: return SYSTEMATIC_STATE_0;
                case 1: return SYSTEMATIC_STATE_1;
                case 2: return SYSTEMATIC_STATE_2;
                default: return SYSTEMATIC_STATE_OTHER;
            }
        }
        
        private void DrawArtistTiles(List<Pattern> patterns)
        {
            Debug.Log("Drawing artist tiles...");
    
            int xOffset = columns * tileSize + DIVIDER_GAP;
    
            for (int i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];
        
                var (artistCol, artistRow) = tileMapping.GetArtistPositionTuple(
                    pattern.tl, pattern.tr, pattern.bl, pattern.br
                );
                
                if (artistCol < 0 || artistRow < 0)
                {
                     continue;
                }
        
                int displayCol = i % columns;
                int displayRow = i / columns;
                int displayX = xOffset + displayCol * tileSize;
                int displayY = displayRow * tileSize;
        
                int sourceX = artistCol * tileSize;
                int sourceY = artistRow * tileSize;
        
                if (sourceX < 0 || sourceY < 0 || 
                    sourceX + tileSize > artistTilemap.width || 
                    sourceY + tileSize > artistTilemap.height)
                {
                    errors.Add($"Pattern {i} ({pattern.tl},{pattern.tr},{pattern.bl},{pattern.br}): " +
                              $"Artist position ({artistCol},{artistRow}) is out of bounds");
                    DrawErrorTile(displayX, displayY);
                    continue;
                }
        
                try
                {
                    var tilePixels = artistTilemap.GetPixels(sourceX, sourceY, tileSize, tileSize);
                    comparisonTexture.SetPixels(displayX, displayY, tileSize, tileSize, tilePixels);
            
                    DrawBorder(displayX, displayY, tileSize, BORDER_COLOR_LIGHT, LINE_THICKNESS_LIGHT);
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
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = ERROR_TILE_COLOR;
            comparisonTexture.SetPixels(x, y, tileSize, tileSize, pixels);
        }
        
        private void DrawLine(int x1, int y1, int x2, int y2, Color color, int thickness)
        {
            int halfThickness = thickness / 2;
            
            if (x1 == x2) 
            {
                for (int t = 0; t < thickness; t++)
                {
                    for (int y = Mathf.Min(y1, y2); y <= Mathf.Max(y1, y2); y++)
                    {
                        int xPos = x1 + t - halfThickness;
                        if (xPos >= 0 && xPos < comparisonTexture.width && 
                            y >= 0 && y < comparisonTexture.height)
                            comparisonTexture.SetPixel(xPos, y, color);
                    }
                }
            }
            else if (y1 == y2)
            {
                for (int t = 0; t < thickness; t++)
                {
                    for (int x = Mathf.Min(x1, x2); x <= Mathf.Max(x1, x2); x++)
                    {
                        int yPos = y1 + t - halfThickness;
                        if (x >= 0 && x < comparisonTexture.width && 
                            yPos >= 0 && yPos < comparisonTexture.height)
                            comparisonTexture.SetPixel(x, yPos, color);
                    }
                }
            }
        }
        
        private void DrawBorder(int x, int y, int size, Color color, int thickness)
        {
            // Top
            DrawLine(x, y + size, x + size, y + size, color, thickness);
            // Bottom
            DrawLine(x, y, x + size, y, color, thickness);
            // Left
            DrawLine(x, y, x, y + size, color, thickness);
            // Right
            DrawLine(x + size, y, x + size, y + size, color, thickness);
        }
        
        private void DrawDivider()
        {
            int x = columns * tileSize;
            
            for (int dx = 0; dx < DIVIDER_GAP; dx++)
            {
                for (int y = 0; y < comparisonTexture.height; y++)
                {
                    comparisonTexture.SetPixel(x + dx, y, DIVIDER_COLOR);
                }
            }
        }
        
        private void DrawLabel(int x, int y, string text)
        {
            for (int dx = 0; dx < LABEL_WIDTH; dx++)
            {
                for (int dy = 0; dy < LABEL_HEIGHT; dy++)
                {
                    if (x + dx < comparisonTexture.width && y + dy < comparisonTexture.height)
                        comparisonTexture.SetPixel(x + dx, y + dy, LABEL_COLOR);
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
            public int tl, tr, bl, br;
        }
    }
#endif
}
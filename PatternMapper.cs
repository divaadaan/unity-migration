using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiningGame
{
    /// <summary>
    /// Maps between artist tilemap layout and pattern-based system
    /// </summary>
    [CreateAssetMenu(fileName = "PatternMapper", menuName = "Mining Game/Pattern Mapper")]
    public class PatternMapper : ScriptableObject
    {
        [System.Serializable]
        public class PatternMapping {
            [Header("Pattern Definition")]
            public TerrainType topLeft;
            public TerrainType topRight;
            public TerrainType bottomLeft;
            public TerrainType bottomRight;
            
            [Header("Artist Tilemap Position")]
            [Tooltip("Column in artist's 10x8 tilemap (0-9)")]
            public int artistColumn;
            [Tooltip("Row in artist's 10x8 tilemap (0-7)")]
            public int artistRow;
            
            [Header("Generated Info")]
            [Tooltip("Calculated pattern index for JavaScript compatibility")]
            [SerializeField] private int calculatedPatternIndex;
            [Tooltip("Calculated artist index (row * 10 + col)")]
            [SerializeField] private int calculatedArtistIndex;
            
            public string GetPatternKey()
            {
                return $"{(int)topLeft},{(int)topRight},{(int)bottomLeft},{(int)bottomRight}";
            }
            
            public int GetPatternIndex()
            {
                // Calculate the same way as JavaScript: skip (0,0,0,0), then enumerate all combinations
                if (topLeft == TerrainType.Empty && topRight == TerrainType.Empty && 
                    bottomLeft == TerrainType.Empty && bottomRight == TerrainType.Empty)
                {
                    return -1; // Special case for all-empty
                }
                
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
                                if (tl == 0 && tr == 0 && bl == 0 && br == 0) continue;
                                
                                // Check if this matches our pattern
                                if (tl == (int)topLeft && tr == (int)topRight && 
                                    bl == (int)bottomLeft && br == (int)bottomRight)
                                {
                                    return index;
                                }
                                index++;
                            }
                        }
                    }
                }
                return -1;
            }
            
            public int GetArtistIndex()
            {
                return artistRow * 10 + artistColumn;
            }
            
            public void UpdateCalculatedValues()
            {
                calculatedPatternIndex = GetPatternIndex();
                calculatedArtistIndex = GetArtistIndex();
            }
        }
        
        [Header("Pattern Mappings")]
        [SerializeField] private List<PatternMapping> mappings = new List<PatternMapping>();

        
        [Header("Reference Tilemaps")]
        [Tooltip("Artist's tilemap for visual reference")]
        public Texture2D artistTilemap;
        
        // Runtime lookup dictionaries
        private Dictionary<string, int> patternToArtistIndex;
        private Dictionary<int, string> artistToPatternKey;
        
        public void InitializeLookups()
        {
            patternToArtistIndex = new Dictionary<string, int>();
            artistToPatternKey = new Dictionary<int, string>();
            
            foreach (var mapping in mappings)
            {
                string key = mapping.GetPatternKey();
                int artistIndex = mapping.GetArtistIndex();
                
                patternToArtistIndex[key] = artistIndex;
                artistToPatternKey[artistIndex] = key;
            }
            
            Debug.Log($"Initialized pattern mapping: {mappings.Count} patterns mapped");
        }
        
        /// <summary>
        /// Get artist tilemap index from pattern corner values
        /// </summary>
        public int GetArtistIndex(TerrainType tl, TerrainType tr, TerrainType bl, TerrainType br)
        {
            if (patternToArtistIndex == null) InitializeLookups();
            
            string key = $"{(int)tl},{(int)tr},{(int)bl},{(int)br}";
            
            if (patternToArtistIndex.TryGetValue(key, out int artistIndex))
            {
                return artistIndex;
            }
            
            Debug.LogWarning($"No artist mapping found for pattern: {key}");
            return 0; // Fallback to first tile
        }
        
        /// <summary>
        /// Get artist tilemap position (col, row) from pattern
        /// </summary>
        public (int col, int row) GetArtistPosition(TerrainType tl, TerrainType tr, TerrainType bl, TerrainType br)
        {
            int artistIndex = GetArtistIndex(tl, tr, bl, br);
            return (artistIndex % 10, artistIndex / 10);
        }
        
        /// <summary>
        /// Export mapping data for JavaScript system
        /// </summary>
        public string ExportForJavaScript()
        {
            var jsMapping = new System.Text.StringBuilder();
            jsMapping.AppendLine("// Pattern to Artist Index Mapping");
            jsMapping.AppendLine("const PATTERN_TO_ARTIST_MAPPING = new Map([");
            
            foreach (var mapping in mappings)
            {
                string patternKey = mapping.GetPatternKey();
                int artistIndex = mapping.GetArtistIndex();
                jsMapping.AppendLine($"  ['{patternKey}', {artistIndex}],");
            }
            
            jsMapping.AppendLine("]);");
            jsMapping.AppendLine();
            jsMapping.AppendLine("// Updated getPatternIndex function");
            jsMapping.AppendLine("function getArtistTileIndex(tl, tr, bl, br) {");
            jsMapping.AppendLine("  const key = `${tl},${tr},${bl},${br}`;");
            jsMapping.AppendLine("  return PATTERN_TO_ARTIST_MAPPING.get(key) || 0;");
            jsMapping.AppendLine("}");
            
            return jsMapping.ToString();
        }
        
#if UNITY_EDITOR
        [ContextMenu("Generate All Patterns")]
        public void GenerateAllPatterns()
        {
            mappings.Clear();
            
            // Generate all 80 non-empty patterns
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
                            
                            var mapping = new PatternMapping
                            {
                                topLeft = (TerrainType)tl,
                                topRight = (TerrainType)tr,
                                bottomLeft = (TerrainType)bl,
                                bottomRight = (TerrainType)br,
                                // Default to sequential layout - you'll need to manually adjust these
                                artistColumn = mappings.Count % 10,
                                artistRow = mappings.Count / 10
                            };
                            
                            mapping.UpdateCalculatedValues();
                            mappings.Add(mapping);
                        }
                    }
                }
            }
            
            EditorUtility.SetDirty(this);
            Debug.Log($"Generated {mappings.Count} pattern mappings. Please manually assign correct artist positions.");
        }
        
        [ContextMenu("Update Calculated Values")]
        public void UpdateAllCalculatedValues()
        {
            foreach (var mapping in mappings)
            {
                mapping.UpdateCalculatedValues();
            }
            EditorUtility.SetDirty(this);
        }
        
        [ContextMenu("Export JavaScript Mapping")]
        public void ExportJavaScriptMapping()
        {
            string jsCode = ExportForJavaScript();
            string path = Application.dataPath + "/Scripts/GeneratedPatternMapping.js";
            System.IO.File.WriteAllText(path, jsCode);
            AssetDatabase.Refresh();
            Debug.Log($"JavaScript mapping exported to: {path}");
        }
        
                [ContextMenu("Validate All Mappings")]
        public void ValidateAllMappings()
        {
            var usedArtistPositions = new HashSet<int>();
            var missingPatterns = new List<string>();
            
            foreach (var mapping in mappings)
            {
                int artistIndex = mapping.GetArtistIndex();
                
                if (usedArtistPositions.Contains(artistIndex))
                {
                    Debug.LogError($"Duplicate artist position: ({mapping.artistColumn}, {mapping.artistRow})");
                }
                usedArtistPositions.Add(artistIndex);
                
                if (artistIndex < 0 || artistIndex >= 80)
                {
                    Debug.LogError($"Invalid artist position: ({mapping.artistColumn}, {mapping.artistRow})");
                }
            }
            
            Debug.Log($"Validation complete. {mappings.Count} mappings checked.");
        }
#endif

#if UNITY_EDITOR
        public int SetMappings(List<PatternMapping> newMappings)
        {
            mappings = newMappings ?? new List<PatternMapping>();
            UpdateAllCalculatedValues();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            return mappings.Count;
        }
#endif
    } 

}

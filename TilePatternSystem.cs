using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

namespace MiningGame
{
    /// <summary>
    /// Manages tile pattern mappings between corner combinations and actual tile assets
    /// Supports custom artist tilemap layouts
    /// </summary>
    [CreateAssetMenu(fileName = "TilePatternSystem", menuName = "Mining Game/Tile Pattern System")]
    public class TilePatternSystem : ScriptableObject
    {
        [System.Serializable]
        public class TilePattern
        {
            [Header("Corner Configuration (TL, TR, BL, BR)")]
            public TerrainType topLeft;
            public TerrainType topRight;
            public TerrainType bottomLeft;
            public TerrainType bottomRight;
            
            [Header("Tile Assets")]
            public TileBase colorTile;
            public TileBase normalTile;
            
            [Header("Metadata")]
            public string patternName;
            public int artistTilemapIndex = -1; // Position in artist's tilemap
            public Vector2Int artistGridPosition; // Grid position in artist's layout
            
            public string GetKey()
            {
                return $"{(int)topLeft},{(int)topRight},{(int)bottomLeft},{(int)bottomRight}";
            }
            
            public bool MatchesPattern(TerrainType tl, TerrainType tr, TerrainType bl, TerrainType br)
            {
                return topLeft == tl && topRight == tr && bottomLeft == bl && bottomRight == br;
            }
        }
        
        [Header("Tile Pattern Configuration")]
        [SerializeField] private List<TilePattern> patterns = new List<TilePattern>();
        
        [Header("Artist Tilemap Settings")]
        [Tooltip("The artist's tilemap sprite sheet for color tiles")]
        public Texture2D artistColorTilemap;
        
        [Tooltip("The artist's tilemap sprite sheet for normal maps")]
        public Texture2D artistNormalTilemap;
        
        [Tooltip("Tile size in pixels in the artist's tilemap")]
        public int tilePixelSize = 100;
        
        [Tooltip("Number of columns in the artist's tilemap")]
        public int tilemapColumns = 10;
        
        [Tooltip("Number of rows in the artist's tilemap")]
        public int tilemapRows = 8;
        
        [Header("Fallback Tiles")]
        [Tooltip("Tile to use when no pattern matches")]
        public TileBase fallbackColorTile;
        public TileBase fallbackNormalTile;
        
        [Tooltip("Special tile for all-empty pattern (0,0,0,0)")]
        public TileBase emptyColorTile;
        public TileBase emptyNormalTile;
        
        // Runtime lookup dictionary
        private Dictionary<string, TilePattern> patternLookup;
        
        /// <summary>
        /// Initialize the pattern lookup dictionary for fast runtime access
        /// </summary>
        public void InitializeLookup()
        {
            patternLookup = new Dictionary<string, TilePattern>();
            foreach (var pattern in patterns)
            {
                var key = pattern.GetKey();
                if (!patternLookup.ContainsKey(key))
                {
                    patternLookup[key] = pattern;
                }
                else
                {
                    Debug.LogWarning($"Duplicate pattern found: {key}");
                }
            }
            Debug.Log($"Initialized {patternLookup.Count} tile patterns");
        }
        
        /// <summary>
        /// Get the tile pattern for a specific corner combination
        /// </summary>
        public TilePattern GetPattern(TerrainType tl, TerrainType tr, TerrainType bl, TerrainType br)
        {
            if (patternLookup == null)
            {
                InitializeLookup();
            }
            
            var key = $"{(int)tl},{(int)tr},{(int)bl},{(int)br}";
            
            if (patternLookup.TryGetValue(key, out var pattern))
            {
                return pattern;
            }
            
            // Special case for all-empty
            if (tl == TerrainType.Empty && tr == TerrainType.Empty && 
                bl == TerrainType.Empty && br == TerrainType.Empty)
            {
                return new TilePattern
                {
                    topLeft = TerrainType.Empty,
                    topRight = TerrainType.Empty,
                    bottomLeft = TerrainType.Empty,
                    bottomRight = TerrainType.Empty,
                    colorTile = emptyColorTile,
                    normalTile = emptyNormalTile,
                    patternName = "All Empty"
                };
            }
            
            Debug.LogWarning($"No pattern found for combination: {key}");
            return null;
        }
        
        /// <summary>
        /// Get color and normal tiles for a pattern
        /// </summary>
        public (TileBase colorTile, TileBase normalTile) GetTiles(TerrainType tl, TerrainType tr, TerrainType bl, TerrainType br)
        {
            var pattern = GetPattern(tl, tr, bl, br);
            
            if (pattern != null)
            {
                return (pattern.colorTile ?? fallbackColorTile, 
                        pattern.normalTile ?? fallbackNormalTile);
            }
            
            return (fallbackColorTile, fallbackNormalTile);
        }
        
        /// <summary>
        /// Generate all 81 possible patterns (including all-empty)
        /// </summary>
        [ContextMenu("Generate All Patterns")]
        public void GenerateAllPatterns()
        {
            patterns.Clear();
            int index = 0;
            
            for (int tl = 0; tl <= 2; tl++)
            {
                for (int tr = 0; tr <= 2; tr++)
                {
                    for (int bl = 0; bl <= 2; bl++)
                    {
                        for (int br = 0; br <= 2; br++)
                        {
                            var pattern = new TilePattern
                            {
                                topLeft = (TerrainType)tl,
                                topRight = (TerrainType)tr,
                                bottomLeft = (TerrainType)bl,
                                bottomRight = (TerrainType)br,
                                patternName = $"{tl}{tr}{bl}{br}",
                                artistTilemapIndex = (tl == 0 && tr == 0 && bl == 0 && br == 0) ? -1 : index,
                                artistGridPosition = new Vector2Int(index % tilemapColumns, index / tilemapColumns)
                            };
                            
                            patterns.Add(pattern);
                            
                            // Skip incrementing index for all-empty pattern
                            if (!(tl == 0 && tr == 0 && bl == 0 && br == 0))
                            {
                                index++;
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"Generated {patterns.Count} patterns (80 tiles + 1 all-empty)");
        }
        
        /// <summary>
        /// Validate that all patterns have assigned tiles
        /// </summary>
        [ContextMenu("Validate Pattern Assignments")]
        public void ValidatePatterns()
        {
            int missingColor = 0;
            int missingNormal = 0;
            
            foreach (var pattern in patterns)
            {
                if (pattern.colorTile == null)
                {
                    missingColor++;
                    Debug.LogWarning($"Pattern {pattern.patternName} missing color tile");
                }
                if (pattern.normalTile == null)
                {
                    missingNormal++;
                    Debug.LogWarning($"Pattern {pattern.patternName} missing normal tile");
                }
            }
            
            if (missingColor == 0 && missingNormal == 0)
            {
                Debug.Log($"All {patterns.Count} patterns have tiles assigned!");
            }
            else
            {
                Debug.LogWarning($"Missing tiles - Color: {missingColor}, Normal: {missingNormal}");
            }
        }
    }
}

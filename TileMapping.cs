using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MiningGame
{
    /// <summary>
    /// Represents a tile's data in the mining game
    /// </summary>
    [System.Serializable]
    public class Tile
    {
        [Header("Core Properties")]
        public TerrainType terrainType;
        
        // Ready for additional properties like:
        // [Header("Resources")]
        // public ResourceType embeddedResource;
        // public int resourceAmount;
        
        // [Header("Mining")]
        // public int durability;
        // public float miningDifficulty;
        
        // [Header("Visual")]
        // public int damageLevel;
        // public string biomeVariant;
        
        public Tile(TerrainType type)
        {
            terrainType = type;
        }
    }
    
    [CreateAssetMenu(fileName = "TileMapping", menuName = "Mining Game/Tile Mapping")]
    public class TileMapping : ScriptableObject
    {
        [System.Serializable]
        private class PatternEntry
        {
            public int index;
            public int col;
            public int row;
            public int[] pattern; // [tl, tr, bl, br]
        }
        
        [System.Serializable]
        private class PatternList
        {
            public PatternEntry[] patterns;
        }
        
        [Header("Configuration")]
        [SerializeField] private TextAsset mappingJson;
        [SerializeField] private int tilemapColumns = 8;
        [SerializeField] private int tilemapRows = 10;
        
        // Runtime lookup
        private Dictionary<string, Vector2Int> patternToPosition;
        private bool isInitialized = false;
        
        public void Initialize()
        {
            if (isInitialized) return;
            
            if (mappingJson == null)
            {
                Debug.LogError("TileMapping: No JSON file assigned!");
                return;
            }
            
            LoadMappingFromJson();
            isInitialized = true;
        }
        
        private void LoadMappingFromJson()
        {
            patternToPosition = new Dictionary<string, Vector2Int>();
            
            try
            {
                // Simply parse the JSON as-is
                var data = JsonUtility.FromJson<PatternList>(mappingJson.text);
                
                if (data.patterns == null)
                {
                    Debug.LogError("TileMapping: JSON parsed but patterns array is null");
                    return;
                }
                
                foreach (var entry in data.patterns)
                {
                    string key = $"{entry.pattern[0]},{entry.pattern[1]},{entry.pattern[2]},{entry.pattern[3]}";
                    patternToPosition[key] = new Vector2Int(entry.col, entry.row);
                }
                
                Debug.Log($"TileMapping: Loaded {patternToPosition.Count} pattern mappings from JSON");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TileMapping: Failed to parse JSON: {e.Message}");
                Debug.LogError($"JSON content preview: {mappingJson.text.Substring(0, Mathf.Min(200, mappingJson.text.Length))}");
            }
        }
        
        /// <summary>
        /// Get artist tilemap position for a given corner pattern
        /// </summary>
        public Vector2Int GetArtistPosition(TerrainType tl, TerrainType tr, TerrainType bl, TerrainType br)
        {
            if (!isInitialized) Initialize();
            
            string key = $"{(int)tl},{(int)tr},{(int)bl},{(int)br}";
            
            if (patternToPosition != null && patternToPosition.TryGetValue(key, out Vector2Int pos))
            {
                return pos;
            }
            
            Debug.LogWarning($"TileMapping: No mapping found for pattern {key}");
            return Vector2Int.zero;
        }
        
        /// <summary>
        /// Get artist position for a visual tile based on surrounding tiles
        /// </summary>
        public Vector2Int GetArtistPositionFromTiles(Tile topLeft, Tile topRight, Tile bottomLeft, Tile bottomRight)
        {
            return GetArtistPosition(
                topLeft?.terrainType ?? TerrainType.Undiggable,
                topRight?.terrainType ?? TerrainType.Undiggable,
                bottomLeft?.terrainType ?? TerrainType.Undiggable,
                bottomRight?.terrainType ?? TerrainType.Undiggable
            );
        }
        
        /// <summary>
        /// For compatibility with existing code that expects (int, int) tuple
        /// </summary>
        public (int col, int row) GetArtistPositionTuple(TerrainType tl, TerrainType tr, TerrainType bl, TerrainType br)
        {
            var pos = GetArtistPosition(tl, tr, bl, br);
            return (pos.x, pos.y);
        }
        
        /// <summary>
        /// Create a Tile with a specific terrain type
        /// </summary>
        public static Tile CreateTile(TerrainType terrainType)
        {
            return new Tile(terrainType);
        }
        
        // Editor validation
        [ContextMenu("Validate JSON Mapping")]
        private void ValidateMapping()
        {
            Initialize();
            
            if (patternToPosition == null || patternToPosition.Count == 0)
            {
                Debug.LogError("No mappings loaded!");
                return;
            }
            
            // Check for all 80 non-empty patterns
            int found = 0;
            for (int tl = 0; tl <= 2; tl++)
            {
                for (int tr = 0; tr <= 2; tr++)
                {
                    for (int bl = 0; bl <= 2; bl++)
                    {
                        for (int br = 0; br <= 2; br++)
                        {
                            if (tl == 0 && tr == 0 && bl == 0 && br == 0) continue;
                            
                            string key = $"{tl},{tr},{bl},{br}";
                            if (patternToPosition.ContainsKey(key))
                                found++;
                            else
                                Debug.LogWarning($"Missing pattern: {key}");
                        }
                    }
                }
            }
            
            Debug.Log($"Validation complete: {found}/80 patterns mapped");
        }
    }
}
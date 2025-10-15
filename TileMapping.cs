using UnityEngine;
using System.Collections.Generic;

namespace MiningGame
{
    [System.Serializable]
    public class Tile
    {
        [Header("Core Properties")]
        public TerrainType terrainType;
        
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
            public int[] pattern;
        }
        
        [System.Serializable]
        private class PatternList
        {
            public PatternEntry[] patterns;
        }
        
        [Header("Configuration")]
        [SerializeField] private TextAsset mappingJson;

        private Dictionary<string, Vector2Int> patternToPosition;
        
        [System.NonSerialized]
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
            Debug.Log("TileMapping: Initialization complete");
        }
        
        private void LoadMappingFromJson()
        {
            patternToPosition = new Dictionary<string, Vector2Int>();
            
            try
            {
                if (mappingJson == null)
                {
                    Debug.LogError("TileMapping: mappingJson is NULL!");
                    return;
                }
                
                Debug.Log($"TileMapping: JSON text length: {mappingJson.text.Length}");
                Debug.Log($"TileMapping: First 100 chars: {mappingJson.text.Substring(0, Mathf.Min(100, mappingJson.text.Length))}");
                
                var data = JsonUtility.FromJson<PatternList>(mappingJson.text);
                
                if (data == null)
                {
                    Debug.LogError("TileMapping: JsonUtility returned NULL!");
                    return;
                }
                
                if (data.patterns == null)
                {
                    Debug.LogError("TileMapping: Patterns array is NULL after parsing!");
                    Debug.LogError($"Full JSON: {mappingJson.text}");
                    return;
                }
                
                Debug.Log($"TileMapping: Parsed {data.patterns.Length} pattern entries");
                
                const int PATTERN_ARRAY_SIZE = 4;
                foreach (var entry in data.patterns)
                {
                    if (entry.pattern == null || entry.pattern.Length != PATTERN_ARRAY_SIZE)
                    {
                        Debug.LogWarning($"TileMapping: Invalid pattern at index {entry.index}");
                        continue;
                    }
                    
                    string key = $"{entry.pattern[0]},{entry.pattern[1]},{entry.pattern[2]},{entry.pattern[3]}";
                    patternToPosition[key] = new Vector2Int(entry.col, entry.row);
                }
                
                Debug.Log($"TileMapping: Successfully loaded {patternToPosition.Count} pattern mappings");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TileMapping: Exception during JSON parsing: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }
        
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
        
        public Vector2Int GetArtistPositionFromTiles(Tile topLeft, Tile topRight, Tile bottomLeft, Tile bottomRight)
        {
            return GetArtistPosition(
                topLeft?.terrainType ?? TerrainType.Undiggable,
                topRight?.terrainType ?? TerrainType.Undiggable,
                bottomLeft?.terrainType ?? TerrainType.Undiggable,
                bottomRight?.terrainType ?? TerrainType.Undiggable
            );
        }
        
        public (int col, int row) GetArtistPositionTuple(TerrainType tl, TerrainType tr, TerrainType bl, TerrainType br)
        {
            var pos = GetArtistPosition(tl, tr, bl, br);
            return (pos.x, pos.y);
        }
        
        public static Tile CreateTile(TerrainType terrainType)
        {
            return new Tile(terrainType);
        }
        
        [ContextMenu("Validate JSON Mapping")]
        private void ValidateMapping()
        {
            Initialize();
            
            if (patternToPosition == null || patternToPosition.Count == 0)
            {
                Debug.LogError("No mappings loaded!");
                return;
            }
            
            int found = 0;
            for (int tl = 0; tl < SharedConstants.TERRAIN_TYPE_COUNT; tl++)
            {
                for (int tr = 0; tr < SharedConstants.TERRAIN_TYPE_COUNT; tr++)
                {
                    for (int bl = 0; bl < SharedConstants.TERRAIN_TYPE_COUNT; bl++)
                    {
                        for (int br = 0; br < SharedConstants.TERRAIN_TYPE_COUNT; br++)
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
            
            Debug.Log($"Validation complete: {found}/{SharedConstants.TOTAL_PATTERNS} patterns mapped");
        }
    }
}
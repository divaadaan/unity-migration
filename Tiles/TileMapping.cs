using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
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

        [Tooltip("2 for Backgrounds (Default/Active), 3 for Mining (Empty/Diggable/Undiggable)")]
        [SerializeField] private int stateCount = 3; 

        [Tooltip("If FALSE, the 0,0,0,0 pattern will always return NULL (transparent tile).")]
        [SerializeField] private bool allowZeroPattern = false;

        public int StateCount => stateCount;

        private Dictionary<string, Vector2Int> patternToPosition;
        
        [System.NonSerialized]
        private bool isInitialized = false;
        
        public void Initialize()
        {
            if (isInitialized) return;
            
            if (mappingJson == null)
            {
                Debug.LogError($"TileMapping ({name}): No JSON file assigned!");
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
                var data = JsonUtility.FromJson<PatternList>(mappingJson.text);
                
                if (data == null || data.patterns == null)
                {
                    Debug.LogError($"TileMapping ({name}): Failed to parse pattern list!");
                    return;
                }
                
                foreach (var entry in data.patterns)
                {
                    if (entry.pattern == null || entry.pattern.Length != 4)
                        continue;
                    
                    string key = $"{entry.pattern[0]},{entry.pattern[1]},{entry.pattern[2]},{entry.pattern[3]}";
                    patternToPosition[key] = new Vector2Int(entry.col, entry.row);
                }
                
                Debug.Log($"TileMapping ({name}): Loaded {patternToPosition.Count} patterns. Mode: {stateCount}-State. Zero Pattern Allowed: {allowZeroPattern}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TileMapping ({name}): JSON Parse Error: {e.Message}");
            }
        }
        
        public Vector2Int? GetArtistPosition(int tl, int tr, int bl, int br)
        {
            if (!isInitialized) Initialize();

            // 1. Check for Zero Pattern Override
            // If all corners are 0, and we disallow zero patterns, return null immediately.
            if (!allowZeroPattern && tl == 0 && tr == 0 && bl == 0 && br == 0)
            {
                return null;
            }

            // Otherwise Standard Lookup
            string key = $"{tl},{tr},{bl},{br}";
            
            if (patternToPosition != null && patternToPosition.TryGetValue(key, out Vector2Int pos))
            {
                return pos;
            }
            
            return null;
        }
        
        public Vector2Int? GetArtistPosition(TerrainType tl, TerrainType tr, TerrainType bl, TerrainType br)
        {
            return GetArtistPosition((int)tl, (int)tr, (int)bl, (int)br);
        }

        public (int col, int row) GetArtistPositionTuple(int tl, int tr, int bl, int br)
        {
            var pos = GetArtistPosition(tl, tr, bl, br);
            return pos.HasValue ? (pos.Value.x, pos.Value.y) : (-1, -1);
        }
    }
}
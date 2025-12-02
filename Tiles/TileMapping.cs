using UnityEngine;
using UnityEngine.Tilemaps;
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
            public int[] pattern;
        }
        
        [System.Serializable]
        private class PatternList
        {
            public PatternEntry[] patterns;
        }
        
        [Header("Configuration")]
        [SerializeField] private TextAsset mappingJson;
        
        [Header("Visual Assets")]
        [Tooltip("Assign the Tile assets here. Array index must match JSON index.")]
        [SerializeField] private TileBase[] tileAssets; 

        [Header("Settings")]
        [Tooltip("2 for Backgrounds, 3 for Mining")]
        [SerializeField] private int stateCount = 3; 
        [SerializeField] private bool allowZeroPattern = false;

        public int StateCount => stateCount;

        private Dictionary<string, Vector2Int> patternToPosition;
        private Dictionary<string, int> patternToIndex; 
        
        [System.NonSerialized]
        private bool isInitialized = false;
        
        public void Initialize()
        {
            if (isInitialized) return;
            if (mappingJson == null) { Debug.LogError($"TileMapping ({name}): No JSON!"); return; }
            LoadMappingFromJson();
            isInitialized = true;
        }
        
        private void LoadMappingFromJson()
        {
            patternToPosition = new Dictionary<string, Vector2Int>();
            patternToIndex = new Dictionary<string, int>(); // New Dictionary
            
            try
            {
                var data = JsonUtility.FromJson<PatternList>(mappingJson.text);
                if (data == null || data.patterns == null) return;
                
                foreach (var entry in data.patterns)
                {
                    if (entry.pattern == null || entry.pattern.Length != 4) continue;
                    
                    string key = $"{entry.pattern[0]},{entry.pattern[1]},{entry.pattern[2]},{entry.pattern[3]}";
                    
                    patternToPosition[key] = new Vector2Int(entry.col, entry.row);
                    patternToIndex[key] = entry.index;
                }
            }
            catch (System.Exception e) { Debug.LogError(e.Message); }
        }
        
        /// <summary>
        /// Returns the actual TileBase asset for the given pattern.
        /// </summary>
        public TileBase GetTile(int tl, int tr, int bl, int br)
        {
            if (!isInitialized) Initialize();

            if (!allowZeroPattern && tl == 0 && tr == 0 && bl == 0 && br == 0)
                return null;

            string key = $"{tl},{tr},{bl},{br}";

            if (patternToIndex != null && patternToIndex.TryGetValue(key, out int index))
            {
                if (tileAssets != null && index >= 0 && index < tileAssets.Length)
                {
                    return tileAssets[index];
                }
            }
            
            return null;
        }

        public Vector2Int? GetArtistPosition(int tl, int tr, int bl, int br)
        {
            if (!isInitialized) Initialize();
            string key = $"{tl},{tr},{bl},{br}";
            if (patternToPosition != null && patternToPosition.TryGetValue(key, out Vector2Int pos)) return pos;
            return null;
        }

        public TileBase GetTile(TerrainType tl, TerrainType tr, TerrainType bl, TerrainType br)
        {
            return GetTile((int)tl, (int)tr, (int)bl, (int)br);
        }
        
        public (int col, int row) GetArtistPositionTuple(int tl, int tr, int bl, int br)
        {
             var pos = GetArtistPosition(tl, tr, bl, br);
             return pos.HasValue ? (pos.Value.x, pos.Value.y) : (-1, -1);
        }
    }
}
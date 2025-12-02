using UnityEngine;

namespace DigDigDiner
{
    [System.Serializable]
    public class Tile
    {
        [Header("Core Properties")]
        public int stateIndex; 

        public TerrainType terrainType 
        {
            get => (TerrainType)stateIndex;
            set => stateIndex = (int)value;
        }

        public Tile(int state)
        {
            stateIndex = state;
        }

        public Tile(TerrainType type)
        {
            stateIndex = (int)type;
        }
    }
}
using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    public class BackgroundMapGenerator : MonoBehaviour
    {
        public enum AlgoType { CellularAutomata, PerlinNoise, RandomWalk, Clusters }

        [Header("References")]
        [SerializeField] private DualGridSystem gridSystem;
        
        [Header("Configuration")]
        [SerializeField] private AlgoType algorithm = AlgoType.CellularAutomata;
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool randomizeSeedOnStart = true;

        [Header("Algorithm Settings")]
        [Tooltip("Fill % for CA, Threshold for Perlin")]
        [Range(0f, 1f)] public float densityParam = 0.45f;
        [Tooltip("Steps for Walk, Radius for Clusters")]
        public int sizeParam = 5;
        [Tooltip("Noise Scale for Perlin (Lower = Zoomed In, Higher = Noisier)")]
            public float noiseScale = 0.1f;
        private void Start()
        {
            if (randomizeSeedOnStart) seed = Random.Range(0, 100000);
            Generate();
        }

        [ContextMenu("Regenerate")]
        public void Generate()
        {
            if (gridSystem == null) return;

            IBackgroundGenerator generator = algorithm switch
            {
                AlgoType.CellularAutomata => new CellularAutomataGenerator(5, densityParam),
                AlgoType.PerlinNoise => new PerlinNoiseGenerator(noiseScale, densityParam),
                AlgoType.RandomWalk => new RandomWalkGenerator(5, sizeParam * 10),
                AlgoType.Clusters => new ClusterGenerator(10, 2, sizeParam),
                _ => new ClusterGenerator()
            };

            Debug.Log($"BG Generator: Running {generator.Name}...");

            HashSet<Vector2Int> activeTiles = generator.Generate(gridSystem.Width, gridSystem.Height, seed);
          
            for (int x = 0; x < gridSystem.Width; x++)
            {
                for (int y = 0; y < gridSystem.Height; y++)
                {
                    bool isActive = activeTiles.Contains(new Vector2Int(x, y));
                    TerrainType type = isActive ? TerrainType.Diggable : TerrainType.Empty;
                    
                    gridSystem.SetTileAtSilent(x, y, new Tile(type));
                }
            }

            gridSystem.RefreshAllVisualTiles();
        }
    }
}
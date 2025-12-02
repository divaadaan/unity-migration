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

        private void Start()
        {
            if (randomizeSeedOnStart) seed = Random.Range(0, 100000);
            Generate();
        }

        [ContextMenu("Regenerate")]
        public void Generate()
        {
            if (gridSystem == null) return;

            // 1. Resolve Strategy
            IBackgroundGenerator generator = algorithm switch
            {
                AlgoType.CellularAutomata => new CellularAutomataGenerator(5, densityParam),
                AlgoType.PerlinNoise => new PerlinNoiseGenerator(0.1f, densityParam),
                AlgoType.RandomWalk => new RandomWalkGenerator(5, sizeParam * 10),
                AlgoType.Clusters => new ClusterGenerator(10, 2, sizeParam),
                _ => new ClusterGenerator()
            };

            Debug.Log($"BG Generator: Running {generator.Name}...");

            // 2. Generate Data (Active Tiles)
            HashSet<Vector2Int> activeTiles = generator.Generate(gridSystem.Width, gridSystem.Height, seed);

            // 3. Apply to Grid
            // Assuming DualGridSystem can accept TerrainType.Diggable as "Active" (1) and Empty as "Default" (0)
            // Or cast integers if you refactored DualGridSystem to use ints.
            
            for (int x = 0; x < gridSystem.Width; x++)
            {
                for (int y = 0; y < gridSystem.Height; y++)
                {
                    bool isActive = activeTiles.Contains(new Vector2Int(x, y));
                    // Map Active -> 1 (Diggable enum value is 1)
                    // Map Default -> 0 (Empty enum value is 0)
                    TerrainType type = isActive ? TerrainType.Diggable : TerrainType.Empty;
                    
                    gridSystem.SetTileAtSilent(x, y, new Tile(type));
                }
            }

            gridSystem.RefreshAllVisualTiles();
        }
    }
}
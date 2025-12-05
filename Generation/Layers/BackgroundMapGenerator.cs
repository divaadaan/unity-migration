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

        [Header("Algorithm Settings")]
        [Range(0f, 1f)] public float densityParam = 0.45f;
        public int sizeParam = 5;
        public float noiseScale = 0.1f;

        /// <summary>
        /// Generates the background layer using the configured algorithm.
        /// </summary>
        /// <param name="externalSeed">Optional seed.</param>
        public void Generate(int? externalSeed = null)
        {
            if (gridSystem == null) return;

            if (externalSeed.HasValue)
            {
                //this makes sure the BG and DistantBG layers don't have the same seed by accident
                seed = externalSeed.Value + (int)algorithm + (name.GetHashCode() % 100);
            }

            IBackgroundGenerator generator = algorithm switch
            {
                AlgoType.CellularAutomata => new CellularAutomataGenerator(5, densityParam),
                AlgoType.PerlinNoise => new PerlinNoiseGenerator(noiseScale, densityParam),
                AlgoType.RandomWalk => new RandomWalkGenerator(5, sizeParam * 10),
                AlgoType.Clusters => new ClusterGenerator(10, 2, sizeParam),
                _ => new ClusterGenerator()
            };

            Debug.Log($"BG Generator ({name}): Running {generator.Name} with seed {seed}...");

            HashSet<Vector2Int> activeTiles = generator.Generate(gridSystem.Width, gridSystem.Height, seed);
          
            // Batch update using Silent set
            for (int x = 0; x < gridSystem.Width; x++)
            {
                for (int y = 0; y < gridSystem.Height; y++)
                {
                    bool isActive = activeTiles.Contains(new Vector2Int(x, y));
                    int stateIndex = isActive ? 1 : 0;
                    gridSystem.SetTileAtSilent(x, y, new Tile(stateIndex));
                }
            }

            gridSystem.RefreshAllVisualTiles();
        }
    }
}
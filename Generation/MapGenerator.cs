using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    [RequireComponent(typeof(DualGridSystem))]
    public class MapGenerator : MonoBehaviour
    {
        [Header("Entrance Settings")]
        [SerializeField] private int entranceNeckWidth = 2;
        [SerializeField] private int entranceNeckLength = 3;
        [SerializeField] private int spawnAreaHeight = 2;

        [Header("Biome Configuration")]
        [SerializeField] private List<BlobSpawner.BlobSpawnConfig> emptyPocketConfigs = new List<BlobSpawner.BlobSpawnConfig>();
        [SerializeField] private List<BlobSpawner.BlobSpawnConfig> undiggablePocketConfigs = new List<BlobSpawner.BlobSpawnConfig>();

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private DualGridSystem gridSystem;

        private void Awake()
        {
            gridSystem = GetComponent<DualGridSystem>();
            
            // Initialize defaults if list is empty
            if (emptyPocketConfigs.Count == 0) InitializeDefaultConfigs();
        }

        public void GenerateMap(int? externalSeed = null)
        {
            int seed = externalSeed ?? 12345;
            if (showDebugLogs) Debug.Log($"MapGenerator: Starting Pipeline (Seed: {seed})...");

            // --- THE PIPELINE ---
            List<IMapStrategy> pipeline = new List<IMapStrategy>();
            pipeline.Add(new FillMapStrategy(TerrainType.Diggable));
            pipeline.Add(new BlobGenerationStrategy(emptyPocketConfigs));
            pipeline.Add(new BlobGenerationStrategy(undiggablePocketConfigs));
            pipeline.Add(new BorderMapStrategy());
            pipeline.Add(new EntranceMapStrategy(entranceNeckWidth, entranceNeckLength, spawnAreaHeight));

            foreach (var strategy in pipeline)
            {
                strategy.Execute(gridSystem, seed);
            }

            gridSystem.CompleteInitialization();
            
            if (showDebugLogs) Debug.Log("MapGenerator: Pipeline Complete.");
        }

        private void InitializeDefaultConfigs()
        {
            // Fallback config just in case inspector is empty
            emptyPocketConfigs.Add(new BlobSpawner.BlobSpawnConfig
            {
                configName = "Default Caverns",
                terrainType = TerrainType.Empty,
                minCount = 3, maxCount = 5,
                spawnProbability = 1.0f
            });
        }

        [ContextMenu("Regenerate Map")]
        public void RegenerateMap()
        {
            GenerateMap();
        }
    }
}
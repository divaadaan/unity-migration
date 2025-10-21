using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    /// <summary>
    /// Modular map generator that uses blob-based generation.
    /// Creates a playable map filled with Diggable tiles and pockets of Empty/Undiggable terrain.
    /// Supports multiple generation strategies through configurable blob spawners.
    /// </summary>
    [RequireComponent(typeof(DualGridSystem))]
    public class MapGenerator : MonoBehaviour
    {
        [Header("Generation Strategy")]
        [SerializeField] private GenerationMode generationMode = GenerationMode.BlobMap;

        [Header("Entrance Settings")]
        [SerializeField] private int entranceNeckWidth = 2;
        [SerializeField] private int entranceNeckLength = 3;
        [SerializeField] private int spawnAreaHeight = 2;

        [Header("Biome Settings")]
        [SerializeField] private int biomeRegionCount = 4;
        [SerializeField] private float biomeMinSpacing = 8f;

        [Header("Blob Spawn Configurations")]
        [SerializeField] private List<BlobSpawner.BlobSpawnConfig> emptyPocketConfigs = new List<BlobSpawner.BlobSpawnConfig>();
        [SerializeField] private List<BlobSpawner.BlobSpawnConfig> undiggablePocketConfigs = new List<BlobSpawner.BlobSpawnConfig>();

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showBiomeGizmos = false;

        private DualGridSystem gridSystem;
        private BlobSpawner blobSpawner;
        private BiomeManager biomeManager;
        private System.Random random;

        public enum GenerationMode
        {
            BlobMap,      // New blob-based generation
            CavernMap     // Old cavern-based generation (moved to Old folder)
        }

        private void Awake()
        {
            gridSystem = GetComponent<DualGridSystem>();
            random = new System.Random();

            // Initialize default configurations if empty
            if (emptyPocketConfigs.Count == 0)
            {
                InitializeDefaultEmptyConfigs();
            }

            if (undiggablePocketConfigs.Count == 0)
            {
                InitializeDefaultUndiggableConfigs();
            }
        }

        private void Start()
        {
            GenerateMap();
        }

        /// <summary>
        /// Main map generation entry point.
        /// </summary>
        public void GenerateMap()
        {
            if (showDebugLogs)
                Debug.Log($"MapGenerator: Starting {generationMode} generation...");

            switch (generationMode)
            {
                case GenerationMode.BlobMap:
                    GenerateBlobMap();
                    break;

                case GenerationMode.CavernMap:
                    Debug.LogWarning("MapGenerator: CavernMap generation has been moved to Generation/Old/. Please use BlobMap mode.");
                    GenerateBlobMap(); // Fallback to blob map
                    break;
            }

            if (showDebugLogs)
                Debug.Log("MapGenerator: Generation complete!");
        }

        /// <summary>
        /// Generates a blob-based map with configurable pockets of terrain.
        /// </summary>
        private void GenerateBlobMap()
        {
            // Step 1: Fill with Diggable terrain as base
            FillWithDiggableTerrain();

            // Step 2: Create border (Undiggable)
            GenerateBorder();

            // Step 3: Generate entrance with spawn area
            GenerateEntranceNeck();

            // Step 4: Initialize blob spawner
            blobSpawner = new BlobSpawner(gridSystem, random);

            // Step 5: Spawn Empty pockets (rooms, chambers, open areas)
            foreach (var config in emptyPocketConfigs)
            {
                blobSpawner.SpawnBlobs(config, showDebugLogs);
            }

            // Step 6: Spawn Undiggable pockets (obstacles, pillars, walls)
            foreach (var config in undiggablePocketConfigs)
            {
                blobSpawner.SpawnBlobs(config, showDebugLogs);
            }

            // Step 7: Initialize biome manager and assign regional biomes
            biomeManager = new BiomeManager(gridSystem, random);
            biomeManager.GenerateBiomeRegions(biomeRegionCount, biomeMinSpacing);

            // Step 8: Refresh visual tiles
            gridSystem.RefreshAllVisualTiles();

            if (showDebugLogs)
            {
                Debug.Log($"MapGenerator: Blob map complete with {emptyPocketConfigs.Count} empty configs and {undiggablePocketConfigs.Count} obstacle configs");
            }
        }

        /// <summary>
        /// Fills the entire map with diggable terrain as the base layer.
        /// </summary>
        private void FillWithDiggableTerrain()
        {
            for (int y = 0; y < gridSystem.Height; y++)
            {
                for (int x = 0; x < gridSystem.Width; x++)
                {
                    gridSystem.SetTileAtSilent(x, y, new Tile(TerrainType.Diggable));
                }
            }

            if (showDebugLogs)
                Debug.Log("MapGenerator: Filled map with Diggable terrain");
        }

        /// <summary>
        /// Creates an undiggable border around the map perimeter.
        /// </summary>
        private void GenerateBorder()
        {
            for (int x = 0; x < gridSystem.Width; x++)
            {
                gridSystem.SetTileAtSilent(x, 0, new Tile(TerrainType.Undiggable));
                gridSystem.SetTileAtSilent(x, gridSystem.Height - 1, new Tile(TerrainType.Undiggable));
            }

            for (int y = 0; y < gridSystem.Height; y++)
            {
                gridSystem.SetTileAtSilent(0, y, new Tile(TerrainType.Undiggable));
                gridSystem.SetTileAtSilent(gridSystem.Width - 1, y, new Tile(TerrainType.Undiggable));
            }

            if (showDebugLogs)
                Debug.Log("MapGenerator: Created undiggable border");
        }

        /// <summary>
        /// Generates entrance with spawn area at top and diggable neck leading down.
        /// Returns the bottom position of the entrance.
        /// </summary>
        private Vector2Int GenerateEntranceNeck()
        {
            int centerX = gridSystem.Width / 2;
            int topY = gridSystem.Height - 1;
            int neckStartY = topY - spawnAreaHeight;
            int neckEndY = neckStartY - entranceNeckLength;

            if (showDebugLogs)
            {
                Debug.Log($"MapGenerator: Entrance generation - CenterX:{centerX}, TopY:{topY}, NeckStartY:{neckStartY}, NeckEndY:{neckEndY}");
                Debug.Log($"MapGenerator: Entrance width: {entranceNeckWidth}, spawn height: {spawnAreaHeight}, neck length: {entranceNeckLength}");
            }

            // Create empty spawn area at the top (player spawns here)
            int emptyTilesCreated = 0;
            for (int y = topY; y > neckStartY && y >= 0; y--)
            {
                for (int dx = -entranceNeckWidth / 2; dx <= entranceNeckWidth / 2; dx++)
                {
                    int x = centerX + dx;
                    if (x >= 0 && x < gridSystem.Width)
                    {
                        gridSystem.SetTileAtSilent(x, y, new Tile(TerrainType.Empty));
                        emptyTilesCreated++;
                        if (showDebugLogs)
                            Debug.Log($"MapGenerator: Created Empty tile at ({x}, {y})");
                    }
                }
            }

            if (showDebugLogs)
                Debug.Log($"MapGenerator: Created {emptyTilesCreated} Empty spawn tiles");

            // Create diggable neck passage (player may dig through this to access map)
            for (int y = neckStartY; y > neckEndY && y > 0; y--)
            {
                for (int dx = -entranceNeckWidth / 2; dx <= entranceNeckWidth / 2; dx++)
                {
                    int x = centerX + dx;
                    if (x > 0 && x < gridSystem.Width - 1)
                    {
                        gridSystem.SetTileAtSilent(x, y, new Tile(TerrainType.Diggable));
                    }
                }
            }

            Vector2Int entranceBottom = new Vector2Int(centerX, neckEndY);
            Vector2Int spawnPosition = new Vector2Int(centerX, topY - 1);

            if (showDebugLogs)
                Debug.Log($"MapGenerator: Spawn area at {spawnPosition}, diggable neck from {neckStartY} to {neckEndY}");

            return entranceBottom;
        }

        /// <summary>
        /// Initializes default Empty pocket configurations.
        /// </summary>
        private void InitializeDefaultEmptyConfigs()
        {
            // Large open chambers
            emptyPocketConfigs.Add(new BlobSpawner.BlobSpawnConfig
            {
                configName = "Large Chambers",
                terrainType = TerrainType.Empty,
                minCount = 3,
                maxCount = 5,
                minSpacing = 6,
                spawnProbability = 1.0f,
                largeBlobWeight = 0.8f,
                snakeBlobWeight = 0.2f
            });

            // Snake-like tunnels
            emptyPocketConfigs.Add(new BlobSpawner.BlobSpawnConfig
            {
                configName = "Winding Tunnels",
                terrainType = TerrainType.Empty,
                minCount = 2,
                maxCount = 4,
                minSpacing = 4,
                spawnProbability = 0.8f,
                largeBlobWeight = 0.2f,
                snakeBlobWeight = 0.8f
            });
        }

        /// <summary>
        /// Initializes default Undiggable pocket configurations.
        /// </summary>
        private void InitializeDefaultUndiggableConfigs()
        {
            // Large obstacles/pillars
            undiggablePocketConfigs.Add(new BlobSpawner.BlobSpawnConfig
            {
                configName = "Stone Pillars",
                terrainType = TerrainType.Undiggable,
                minCount = 2,
                maxCount = 4,
                minSpacing = 5,
                spawnProbability = 0.7f,
                largeBlobWeight = 0.9f,
                snakeBlobWeight = 0.1f
            });

            // Narrow undiggable veins
            undiggablePocketConfigs.Add(new BlobSpawner.BlobSpawnConfig
            {
                configName = "Rock Veins",
                terrainType = TerrainType.Undiggable,
                minCount = 1,
                maxCount = 3,
                minSpacing = 4,
                spawnProbability = 0.5f,
                largeBlobWeight = 0.1f,
                snakeBlobWeight = 0.9f
            });
        }

        /// <summary>
        /// Gets the biome at a specific grid position.
        /// </summary>
        public Biome GetBiomeAt(int x, int y)
        {
            if (biomeManager != null)
                return biomeManager.GetBiomeAt(x, y);

            return Biome.AllBiomes[0]; // Default fallback
        }

        /// <summary>
        /// Gets the biome manager for external access.
        /// </summary>
        public BiomeManager GetBiomeManager()
        {
            return biomeManager;
        }

        /// <summary>
        /// Context menu command to regenerate the map.
        /// </summary>
        [ContextMenu("Regenerate Map")]
        public void RegenerateMap()
        {
            GenerateMap();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showBiomeGizmos) return;

            if (biomeManager != null)
            {
                biomeManager.DrawGizmos();
            }
        }
#endif
    }
}

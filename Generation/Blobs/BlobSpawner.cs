using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    /// <summary>
    /// Manages spawning of blob pockets throughout the map.
    /// Works with IBlobGenerator strategies to create varied terrain features.
    /// </summary>
    [System.Serializable]
    public class BlobSpawner
    {
        [System.Serializable]
        public class BlobSpawnConfig
        {
            public string configName = "Unnamed Config";
            public TerrainType terrainType = TerrainType.Empty;
            public int minCount = 3;
            public int maxCount = 6;
            public int minSpacing = 4;
            [Range(0f, 1f)]
            public float spawnProbability = 1.0f;

            [Header("Generator Mix")]
            [Range(0f, 1f)]
            public float largeBlobWeight = 0.5f;
            [Range(0f, 1f)]
            public float snakeBlobWeight = 0.5f;
        }

        private DualGridSystem gridSystem;
        private System.Random random;
        private List<Vector2Int> occupiedPositions = new List<Vector2Int>();

        // Available blob generators
        private Dictionary<string, IBlobGenerator> generators = new Dictionary<string, IBlobGenerator>();

        public BlobSpawner(DualGridSystem grid, System.Random rng)
        {
            gridSystem = grid;
            random = rng;

            // Register available generators
            RegisterGenerator("large", new LargeBlobGenerator());
            RegisterGenerator("snake", new SnakeBlobGenerator());
        }

        /// <summary>
        /// Registers a blob generator for use in spawning.
        /// </summary>
        public void RegisterGenerator(string key, IBlobGenerator generator)
        {
            generators[key] = generator;
        }

        /// <summary>
        /// Spawns blobs according to the given configuration.
        /// </summary>
        public void SpawnBlobs(BlobSpawnConfig config, bool showDebugLogs = false)
        {
            if (random.NextDouble() > config.spawnProbability)
            {
                if (showDebugLogs)
                    Debug.Log($"BlobSpawner: Skipping '{config.configName}' (probability check)");
                return;
            }

            int blobCount = random.Next(config.minCount, config.maxCount + 1);

            if (showDebugLogs)
                Debug.Log($"BlobSpawner: Spawning {blobCount} blobs for '{config.configName}' ({config.terrainType})");

            for (int i = 0; i < blobCount; i++)
            {
                if (!TrySpawnBlob(config, out var blobPositions))
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"BlobSpawner: Failed to spawn blob {i + 1}/{blobCount} for '{config.configName}'");
                    continue;
                }

                // Mark positions as occupied
                occupiedPositions.AddRange(blobPositions);

                if (showDebugLogs)
                    Debug.Log($"BlobSpawner: Spawned blob {i + 1}/{blobCount} with {blobPositions.Count} tiles");
            }
        }

        /// <summary>
        /// Attempts to spawn a single blob at a valid location.
        /// </summary>
        private bool TrySpawnBlob(BlobSpawnConfig config, out List<Vector2Int> blobPositions)
        {
            blobPositions = new List<Vector2Int>();

            const int maxAttempts = 50;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Pick random position (avoid edges)
                int x = random.Next(2, gridSystem.Width - 2);
                int y = random.Next(2, gridSystem.Height - 2);
                Vector2Int startPos = new Vector2Int(x, y);

                // Check spacing from other blobs
                if (!IsValidSpawnPosition(startPos, config.minSpacing))
                    continue;

                // Select generator based on weights
                IBlobGenerator generator = SelectGenerator(config);
                if (generator == null)
                {
                    Debug.LogError("BlobSpawner: No generator selected!");
                    return false;
                }

                // Generate the blob
                blobPositions = generator.GenerateBlob(
                    startPos,
                    config.terrainType,
                    gridSystem.Width,
                    gridSystem.Height,
                    random
                );

                if (blobPositions.Count > 0)
                {
                    // Apply blob to grid
                    ApplyBlobToGrid(blobPositions, config.terrainType);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Selects a generator based on configured weights.
        /// </summary>
        private IBlobGenerator SelectGenerator(BlobSpawnConfig config)
        {
            float totalWeight = config.largeBlobWeight + config.snakeBlobWeight;
            if (totalWeight <= 0)
            {
                // Default to large blob if no weights set
                return generators["large"];
            }

            float roll = (float)random.NextDouble() * totalWeight;

            if (roll < config.largeBlobWeight)
                return generators["large"];
            else
                return generators["snake"];
        }

        /// <summary>
        /// Checks if a position is valid for spawning (respects spacing).
        /// </summary>
        private bool IsValidSpawnPosition(Vector2Int position, int minSpacing)
        {
            foreach (var occupied in occupiedPositions)
            {
                float distance = Vector2Int.Distance(position, occupied);
                if (distance < minSpacing)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Applies the blob positions to the grid.
        /// </summary>
        private void ApplyBlobToGrid(List<Vector2Int> positions, TerrainType terrainType)
        {
            foreach (var pos in positions)
            {
                if (pos.x > 0 && pos.x < gridSystem.Width - 1 &&
                    pos.y > 0 && pos.y < gridSystem.Height - 1)
                {
                    gridSystem.SetTileAtSilent(pos.x, pos.y, new Tile(terrainType));
                }
            }
        }

        /// <summary>
        /// Clears the list of occupied positions (call between different generation phases).
        /// </summary>
        public void ClearOccupiedPositions()
        {
            occupiedPositions.Clear();
        }

        /// <summary>
        /// Gets all occupied positions for external use (e.g., biome assignment).
        /// </summary>
        public List<Vector2Int> GetOccupiedPositions()
        {
            return new List<Vector2Int>(occupiedPositions);
        }
    }
}

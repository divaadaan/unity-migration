using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    /// <summary>
    /// Manages biome assignment across the map.
    /// Assigns biomes to regions using Voronoi diagram approach.
    /// </summary>
    public class BiomeManager
    {
        [System.Serializable]
        public class BiomeRegion
        {
            public Vector2Int center;
            public Biome biome;
            public float influenceRadius;

            public BiomeRegion(Vector2Int centerPos, Biome biomeType, float radius)
            {
                center = centerPos;
                biome = biomeType;
                influenceRadius = radius;
            }
        }

        private List<BiomeRegion> regions = new List<BiomeRegion>();
        private DualGridSystem gridSystem;
        private System.Random random;

        // Biome data storage (position -> biome)
        private Dictionary<Vector2Int, Biome> biomeMap = new Dictionary<Vector2Int, Biome>();

        public BiomeManager(DualGridSystem grid, System.Random rng)
        {
            gridSystem = grid;
            random = rng;
        }

        /// <summary>
        /// Creates biome regions across the map.
        /// </summary>
        public void GenerateBiomeRegions(int regionCount, float minSpacing = 8f)
        {
            regions.Clear();
            biomeMap.Clear();

            List<Vector2Int> placedCenters = new List<Vector2Int>();

            for (int i = 0; i < regionCount; i++)
            {
                Vector2Int center = FindValidRegionCenter(placedCenters, minSpacing);

                if (center == Vector2Int.zero && placedCenters.Count > 0)
                {
                    Debug.LogWarning($"BiomeManager: Could only place {i}/{regionCount} biome regions");
                    break;
                }

                // Select random biome
                Biome biome = Biome.AllBiomes[random.Next(Biome.AllBiomes.Length)];

                // Random influence radius
                float influenceRadius = Random.Range(6f, 12f);

                BiomeRegion region = new BiomeRegion(center, biome, influenceRadius);
                regions.Add(region);
                placedCenters.Add(center);

                Debug.Log($"BiomeManager: Placed {biome.Name} region at {center} (radius: {influenceRadius:F1})");
            }

            // Assign biomes to all grid positions using Voronoi
            AssignBiomesToGrid();
        }

        /// <summary>
        /// Finds a valid center point for a new biome region.
        /// </summary>
        private Vector2Int FindValidRegionCenter(List<Vector2Int> existingCenters, float minSpacing)
        {
            const int maxAttempts = 100;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int x = random.Next(5, gridSystem.Width - 5);
                int y = random.Next(5, gridSystem.Height - 5);
                Vector2Int candidate = new Vector2Int(x, y);

                // Check spacing from existing centers
                bool valid = true;
                foreach (var existing in existingCenters)
                {
                    if (Vector2Int.Distance(candidate, existing) < minSpacing)
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                    return candidate;
            }

            return Vector2Int.zero; // Failed to find valid position
        }

        /// <summary>
        /// Assigns biomes to all grid positions using Voronoi diagram (closest region).
        /// </summary>
        private void AssignBiomesToGrid()
        {
            for (int y = 0; y < gridSystem.Height; y++)
            {
                for (int x = 0; x < gridSystem.Width; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    Biome closestBiome = FindClosestBiome(pos);

                    if (closestBiome != null)
                    {
                        biomeMap[pos] = closestBiome;
                    }
                }
            }

            Debug.Log($"BiomeManager: Assigned biomes to {biomeMap.Count} grid positions");
        }

        /// <summary>
        /// Finds the biome that influences a given position most (closest region center).
        /// </summary>
        private Biome FindClosestBiome(Vector2Int position)
        {
            if (regions.Count == 0)
                return Biome.AllBiomes[0]; // Default to first biome

            BiomeRegion closest = regions[0];
            float minDistance = Vector2Int.Distance(position, regions[0].center);

            foreach (var region in regions)
            {
                float distance = Vector2Int.Distance(position, region.center);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = region;
                }
            }

            return closest.biome;
        }

        /// <summary>
        /// Gets the biome at a specific grid position.
        /// </summary>
        public Biome GetBiomeAt(Vector2Int position)
        {
            if (biomeMap.TryGetValue(position, out Biome biome))
                return biome;

            return Biome.AllBiomes[0]; // Default fallback
        }

        /// <summary>
        /// Gets the biome at a specific grid coordinate.
        /// </summary>
        public Biome GetBiomeAt(int x, int y)
        {
            return GetBiomeAt(new Vector2Int(x, y));
        }

        /// <summary>
        /// Gets all biome regions for visualization/debugging.
        /// </summary>
        public List<BiomeRegion> GetRegions()
        {
            return new List<BiomeRegion>(regions);
        }

        /// <summary>
        /// Visualizes biome regions in the Unity Editor.
        /// </summary>
        public void DrawGizmos()
        {
            if (regions == null || regions.Count == 0) return;

            foreach (var region in regions)
            {
                // Set color based on biome ID (for debug visualization only)
                Gizmos.color = GetDebugColorForBiomeID(region.biome.BiomeID);

                // Draw sphere at center
                Vector3 centerWorld = new Vector3(region.center.x, region.center.y, 0);
                Gizmos.DrawWireSphere(centerWorld, region.influenceRadius);

                // Draw label would go here in a custom editor
            }
        }

        /// <summary>
        /// Gets a debug color for visualizing biomes (development only).
        /// Artists will define actual colors in shaders.
        /// </summary>
        private Color GetDebugColorForBiomeID(int biomeID)
        {
            // Simple debug colors based on ID
            return biomeID switch
            {
                0 => new Color(1f, 0.2f, 0.2f, 0.5f),   // Red (Apple)
                1 => new Color(1f, 0.6f, 0f, 0.5f),     // Orange (Orange)
                2 => new Color(1f, 1f, 0f, 0.5f),       // Yellow (Banana)
                3 => new Color(0.6f, 0f, 1f, 0.5f),     // Purple (Grape)
                _ => Color.white
            };
        }
    }
}

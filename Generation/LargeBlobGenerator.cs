using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    /// <summary>
    /// Generates large, blob-shaped pockets using cellular automata-like growth.
    /// Creates organic, rounded shapes similar to caverns.
    /// </summary>
    public class LargeBlobGenerator : IBlobGenerator
    {
        [System.Serializable]
        public class BlobSettings
        {
            public int minRadius = 3;
            public int maxRadius = 6;
            public float fillRatio = 0.7f; // How "filled" the blob is (0-1)
            public int smoothingIterations = 2;
        }

        private BlobSettings settings;

        public LargeBlobGenerator(BlobSettings customSettings = null)
        {
            settings = customSettings ?? new BlobSettings();
        }

        public string GetGeneratorName() => "LargeBlobGenerator";

        public List<Vector2Int> GenerateBlob(
            Vector2Int startPosition,
            TerrainType terrainType,
            int gridWidth,
            int gridHeight,
            System.Random random)
        {
            List<Vector2Int> blobPositions = new List<Vector2Int>();

            // Random radius for this blob
            int radius = random.Next(settings.minRadius, settings.maxRadius + 1);

            // Create circular area
            HashSet<Vector2Int> potentialPositions = new HashSet<Vector2Int>();

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    Vector2Int pos = new Vector2Int(startPosition.x + dx, startPosition.y + dy);

                    // Check bounds
                    if (pos.x <= 0 || pos.x >= gridWidth - 1 || pos.y <= 0 || pos.y >= gridHeight - 1)
                        continue;

                    // Check if within circular radius
                    float distance = Vector2.Distance(Vector2.zero, new Vector2(dx, dy));
                    if (distance <= radius)
                    {
                        potentialPositions.Add(pos);
                    }
                }
            }

            // Randomly fill based on fillRatio and distance from center
            foreach (var pos in potentialPositions)
            {
                float distanceFromCenter = Vector2.Distance(startPosition, pos);
                float normalizedDistance = distanceFromCenter / radius;

                // Center is more likely to be filled, edges less likely
                float fillChance = settings.fillRatio * (1.2f - normalizedDistance);
                fillChance = Mathf.Clamp01(fillChance);

                if (random.NextDouble() < fillChance)
                {
                    blobPositions.Add(pos);
                }
            }

            // Smooth the blob to make it more organic
            blobPositions = SmoothBlob(blobPositions, startPosition, radius, gridWidth, gridHeight);

            return blobPositions;
        }

        /// <summary>
        /// Smooths the blob using cellular automata rules to create organic shapes.
        /// </summary>
        private List<Vector2Int> SmoothBlob(
            List<Vector2Int> positions,
            Vector2Int center,
            int radius,
            int gridWidth,
            int gridHeight)
        {
            HashSet<Vector2Int> blobSet = new HashSet<Vector2Int>(positions);

            for (int iteration = 0; iteration < settings.smoothingIterations; iteration++)
            {
                HashSet<Vector2Int> newBlobSet = new HashSet<Vector2Int>();

                // Check all positions in the blob area
                for (int dy = -radius - 1; dy <= radius + 1; dy++)
                {
                    for (int dx = -radius - 1; dx <= radius + 1; dx++)
                    {
                        Vector2Int pos = new Vector2Int(center.x + dx, center.y + dy);

                        // Check bounds
                        if (pos.x <= 0 || pos.x >= gridWidth - 1 || pos.y <= 0 || pos.y >= gridHeight - 1)
                            continue;

                        // Count filled neighbors
                        int filledNeighbors = CountFilledNeighbors(pos, blobSet);

                        // Cellular automata rule: keep tile if it has 5+ neighbors, or if it's already filled and has 4+ neighbors
                        bool wasFilled = blobSet.Contains(pos);
                        bool shouldBeFilled = filledNeighbors >= 5 || (wasFilled && filledNeighbors >= 4);

                        if (shouldBeFilled)
                        {
                            newBlobSet.Add(pos);
                        }
                    }
                }

                blobSet = newBlobSet;
            }

            return new List<Vector2Int>(blobSet);
        }

        /// <summary>
        /// Counts how many neighbors (including diagonals) are filled.
        /// </summary>
        private int CountFilledNeighbors(Vector2Int position, HashSet<Vector2Int> filledSet)
        {
            int count = 0;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue; // Skip center

                    Vector2Int neighbor = new Vector2Int(position.x + dx, position.y + dy);
                    if (filledSet.Contains(neighbor))
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}

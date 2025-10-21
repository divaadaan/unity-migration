using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    /// <summary>
    /// Generates narrow, snake-like pockets using random walk algorithm.
    /// Creates winding, tunnel-like features.
    /// </summary>
    public class SnakeBlobGenerator : IBlobGenerator
    {
        [System.Serializable]
        public class SnakeSettings
        {
            public int minLength = 8;
            public int maxLength = 16;
            public int width = 1; // Width of the snake (0 = single tile, 1 = 3 tiles wide, etc.)
            public float directionChangeChance = 0.3f; // Probability of changing direction
            public float branchChance = 0.1f; // Probability of creating a branch
            public int maxBranches = 2;
        }

        private SnakeSettings settings;

        public SnakeBlobGenerator(SnakeSettings customSettings = null)
        {
            settings = customSettings ?? new SnakeSettings();
        }

        public string GetGeneratorName() => "SnakeBlobGenerator";

        public List<Vector2Int> GenerateBlob(
            Vector2Int startPosition,
            TerrainType terrainType,
            int gridWidth,
            int gridHeight,
            System.Random random)
        {
            HashSet<Vector2Int> blobPositions = new HashSet<Vector2Int>();

            // Generate main snake body
            int length = random.Next(settings.minLength, settings.maxLength + 1);
            GenerateSnakePath(startPosition, length, blobPositions, gridWidth, gridHeight, random);

            // Generate branches
            List<Vector2Int> branchStarts = new List<Vector2Int>(blobPositions);
            int branchCount = random.Next(0, settings.maxBranches + 1);

            for (int i = 0; i < branchCount && i < branchStarts.Count; i++)
            {
                Vector2Int branchStart = branchStarts[random.Next(branchStarts.Count)];
                int branchLength = length / 2; // Branches are shorter
                GenerateSnakePath(branchStart, branchLength, blobPositions, gridWidth, gridHeight, random);
            }

            return new List<Vector2Int>(blobPositions);
        }

        /// <summary>
        /// Generates a single snake path using random walk.
        /// </summary>
        private void GenerateSnakePath(
            Vector2Int startPosition,
            int length,
            HashSet<Vector2Int> positions,
            int gridWidth,
            int gridHeight,
            System.Random random)
        {
            Vector2Int currentPos = startPosition;
            Vector2Int currentDirection = GetRandomDirection(random);

            for (int step = 0; step < length; step++)
            {
                // Add current position and surrounding tiles (for width)
                AddTileWithWidth(currentPos, positions, gridWidth, gridHeight);

                // Decide if we should change direction
                if (random.NextDouble() < settings.directionChangeChance)
                {
                    currentDirection = GetRandomDirection(random);
                }

                // Move in current direction
                Vector2Int nextPos = currentPos + currentDirection;

                // Check if next position is valid
                if (IsValidPosition(nextPos, gridWidth, gridHeight))
                {
                    currentPos = nextPos;
                }
                else
                {
                    // Hit boundary, try a different direction
                    currentDirection = GetRandomDirection(random);
                    nextPos = currentPos + currentDirection;

                    if (IsValidPosition(nextPos, gridWidth, gridHeight))
                    {
                        currentPos = nextPos;
                    }
                    else
                    {
                        // Stuck, end this path
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a tile and its surrounding tiles based on snake width.
        /// </summary>
        private void AddTileWithWidth(
            Vector2Int center,
            HashSet<Vector2Int> positions,
            int gridWidth,
            int gridHeight)
        {
            for (int dy = -settings.width; dy <= settings.width; dy++)
            {
                for (int dx = -settings.width; dx <= settings.width; dx++)
                {
                    Vector2Int pos = new Vector2Int(center.x + dx, center.y + dy);

                    if (IsValidPosition(pos, gridWidth, gridHeight))
                    {
                        positions.Add(pos);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a random cardinal direction (up, down, left, right).
        /// </summary>
        private Vector2Int GetRandomDirection(System.Random random)
        {
            int choice = random.Next(4);
            return choice switch
            {
                0 => Vector2Int.up,
                1 => Vector2Int.down,
                2 => Vector2Int.left,
                3 => Vector2Int.right,
                _ => Vector2Int.up
            };
        }

        /// <summary>
        /// Checks if a position is within valid bounds.
        /// </summary>
        private bool IsValidPosition(Vector2Int pos, int gridWidth, int gridHeight)
        {
            return pos.x > 0 && pos.x < gridWidth - 1 &&
                   pos.y > 0 && pos.y < gridHeight - 1;
        }
    }
}

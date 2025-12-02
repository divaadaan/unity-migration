using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    // 1. Cellular Automata (Organic Blobs)
    public class CellularAutomataGenerator : IBackgroundGenerator
    {
        public string Name => "Cellular Automata";
        private int iterations;
        private float fillPercent;

        public CellularAutomataGenerator(int iterations = 5, float fillPercent = 0.45f)
        {
            this.iterations = iterations;
            this.fillPercent = fillPercent;
        }

        public HashSet<Vector2Int> Generate(int width, int height, int seed)
        {
            System.Random prng = new System.Random(seed);
            int[,] map = new int[width, height];

            // Random Fill
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    map[x, y] = (prng.Next(0, 100) < fillPercent * 100) ? 1 : 0;

            // Smoothing
            for (int i = 0; i < iterations; i++)
            {
                int[,] newMap = new int[width, height];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int neighbors = GetNeighborCount(map, x, y, width, height);
                        if (neighbors > 4) newMap[x, y] = 1;
                        else if (neighbors < 4) newMap[x, y] = 0;
                        else newMap[x, y] = map[x, y];
                    }
                }
                map = newMap;
            }

            return ConvertToSet(map, width, height);
        }

        private int GetNeighborCount(int[,] map, int x, int y, int w, int h)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (x+dx >= 0 && x+dx < w && y+dy >= 0 && y+dy < h && !(dx==0&&dy==0))
                        count += map[x+dx, y+dy];
            return count;
        }

        private HashSet<Vector2Int> ConvertToSet(int[,] map, int w, int h)
        {
            var set = new HashSet<Vector2Int>();
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    if (map[x, y] == 1) set.Add(new Vector2Int(x, y));
            return set;
        }
    }

    // 2. Perlin Noise (Clouds / Large Regions)
    public class PerlinNoiseGenerator : IBackgroundGenerator
    {
        public string Name => "Perlin Noise";
        private float scale;
        private float threshold;

        public PerlinNoiseGenerator(float scale = 0.1f, float threshold = 0.5f)
        {
            this.scale = scale;
            this.threshold = threshold;
        }

        public HashSet<Vector2Int> Generate(int width, int height, int seed)
        {
            var set = new HashSet<Vector2Int>();
            Vector2 offset = new Vector2(seed % 100, seed % 100);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float val = Mathf.PerlinNoise((x * scale) + offset.x, (y * scale) + offset.y);
                    if (val > threshold) set.Add(new Vector2Int(x, y));
                }
            }
            return set;
        }
    }

    // 3. Random Walk (Snake/Veins)
    public class RandomWalkGenerator : IBackgroundGenerator
    {
        public string Name => "Random Walk";
        private int walkerCount;
        private int steps;

        public RandomWalkGenerator(int walkerCount = 5, int steps = 50)
        {
            this.walkerCount = walkerCount;
            this.steps = steps;
        }

        public HashSet<Vector2Int> Generate(int width, int height, int seed)
        {
            var set = new HashSet<Vector2Int>();
            System.Random prng = new System.Random(seed);

            for (int i = 0; i < walkerCount; i++)
            {
                Vector2Int pos = new Vector2Int(prng.Next(width), prng.Next(height));
                for (int s = 0; s < steps; s++)
                {
                    set.Add(pos);
                    int dir = prng.Next(0, 4);
                    Vector2Int next = pos;
                    if (dir == 0) next.y++;
                    else if (dir == 1) next.y--;
                    else if (dir == 2) next.x--;
                    else if (dir == 3) next.x++;

                    if (next.x >= 0 && next.x < width && next.y >= 0 && next.y < height)
                        pos = next;
                }
            }
            return set;
        }
    }

    // 4. Custom Cluster Spawner (Scattered Groups)
    public class ClusterGenerator : IBackgroundGenerator
    {
        public string Name => "Cluster Spawner";
        private int clusterCount;
        private int minRadius;
        private int maxRadius;

        public ClusterGenerator(int clusterCount = 10, int minRadius = 2, int maxRadius = 4)
        {
            this.clusterCount = clusterCount;
            this.minRadius = minRadius;
            this.maxRadius = maxRadius;
        }

        public HashSet<Vector2Int> Generate(int width, int height, int seed)
        {
            var set = new HashSet<Vector2Int>();
            System.Random prng = new System.Random(seed);

            for (int i = 0; i < clusterCount; i++)
            {
                Vector2Int center = new Vector2Int(prng.Next(width), prng.Next(height));
                int r = prng.Next(minRadius, maxRadius + 1);
                
                // Draw circle
                for (int x = center.x - r; x <= center.x + r; x++)
                {
                    for (int y = center.y - r; y <= center.y + r; y++)
                    {
                        if (x < 0 || x >= width || y < 0 || y >= height) continue;
                        if (Vector2Int.Distance(center, new Vector2Int(x, y)) <= r)
                        {
                            set.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }
            return set;
        }
    }
}
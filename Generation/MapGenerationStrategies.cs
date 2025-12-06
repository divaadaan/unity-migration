using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    /// <summary>
    /// Interface for any algorithm that modifies the gameplay map.
    /// </summary>
    public interface IMapStrategy
    {
        void Execute(DualGridSystem grid, int seed);
    }

    /// <summary>
    /// 1. FILL: Resets the entire map to a base terrain (usually Diggable Dirt).
    /// </summary>
    public class FillMapStrategy : IMapStrategy
    {
        private TerrainType fillType;
        public FillMapStrategy(TerrainType type) { fillType = type; }

        public void Execute(DualGridSystem grid, int seed)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    grid.SetTileAtSilent(x, y, new Tile(fillType));
                }
            }
        }
    }

    /// <summary>
    /// 2. BLOBS: Runs the BlobSpawner to create pockets of Empty or Undiggable terrain.
    /// </summary>
    public class BlobGenerationStrategy : IMapStrategy
    {
        private List<BlobSpawner.BlobSpawnConfig> configs;

        public BlobGenerationStrategy(List<BlobSpawner.BlobSpawnConfig> configs)
        {
            this.configs = configs;
        }

        public void Execute(DualGridSystem grid, int seed)
        {
            System.Random prng = new System.Random(seed);
            BlobSpawner spawner = new BlobSpawner(grid, prng);

            foreach (var config in configs)
            {
                spawner.SpawnBlobs(config, false); // 'false' to silence debug logs per blob
            }
        }
    }

    /// <summary>
    /// 3. BORDER: Forces the outer edges to be Undiggable (Bedrock).
    /// This ensures the map is sealed, overwriting any blobs that hit the edge.
    /// </summary>
    public class BorderMapStrategy : IMapStrategy
    {
        public void Execute(DualGridSystem grid, int seed)
        {
            // Top and Bottom
            for (int x = 0; x < grid.Width; x++)
            {
                grid.SetTileAtSilent(x, 0, new Tile(TerrainType.Undiggable));
                grid.SetTileAtSilent(x, grid.Height - 1, new Tile(TerrainType.Undiggable));
            }

            // Left and Right
            for (int y = 0; y < grid.Height; y++)
            {
                grid.SetTileAtSilent(0, y, new Tile(TerrainType.Undiggable));
                grid.SetTileAtSilent(grid.Width - 1, y, new Tile(TerrainType.Undiggable));
            }
        }
    }

    /// <summary>
    /// 4. ENTRANCE: Clears the 'Neck' and Spawn Area.
    /// Running this LAST guarantees no obstacles block the player's entry.
    /// </summary>
    public class EntranceMapStrategy : IMapStrategy
    {
        private int neckWidth;
        private int neckLength;
        private int spawnHeight;

        public EntranceMapStrategy(int width, int length, int height)
        {
            this.neckWidth = width;
            this.neckLength = length;
            this.spawnHeight = height;
        }

        public void Execute(DualGridSystem grid, int seed)
        {
            int centerX = grid.Width / 2;
            int topY = grid.Height - 1;
            int neckStartY = topY - spawnHeight;
            int neckEndY = neckStartY - neckLength;

            // 1. Clear the Spawn Area (Top)
            for (int y = topY; y > neckStartY && y >= 0; y--)
            {
                for (int dx = -neckWidth / 2; dx <= neckWidth / 2; dx++)
                {
                    int x = centerX + dx;
                    if (x >= 0 && x < grid.Width)
                    {
                        grid.SetTileAtSilent(x, y, new Tile(TerrainType.Empty));
                    }
                }
            }

            // 2. Clear the Neck (Passage down) - Sets to DIGGABLE so it looks filled but soft
            for (int y = neckStartY; y > neckEndY && y > 0; y--)
            {
                for (int dx = -neckWidth / 2; dx <= neckWidth / 2; dx++)
                {
                    int x = centerX + dx;
                    if (x > 0 && x < grid.Width - 1)
                    {
                        grid.SetTileAtSilent(x, y, new Tile(TerrainType.Diggable));
                    }
                }
            }
        }
    }
}
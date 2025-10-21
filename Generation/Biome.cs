using UnityEngine;

namespace DigDigDiner
{
    /// <summary>
    /// Represents a biome type for map regions.
    /// Each biome defines unique characteristics that can be used by artists for shader customization.
    /// Biomes identify regions of the map but do NOT specify colors - that's left to the artist.
    /// </summary>
    public class Biome
    {
        public string Name { get; private set; }
        public int BiomeID { get; private set; } // Unique ID for shader lookup

        // Cavern size properties (legacy, kept for potential future use)
        public int MinRadius { get; set; }
        public int MaxRadius { get; set; }

        // Tile composition ratios (legacy, kept for potential future use)
        public float EmptyTileRatio { get; set; }
        public float DiggableTileRatio { get; set; }
        public float UndiggableTileRatio { get; set; }

        // Tunnel properties (legacy, kept for potential future use)
        public int TunnelWidth { get; set; }
        public float TunnelDiggableRatio { get; set; }

        public Biome(string name,
                     int biomeID,
                     int minRadius = 2,
                     int maxRadius = 4,
                     float emptyRatio = 0.6f,
                     float diggableRatio = 0.3f,
                     float undiggableRatio = 0.1f,
                     int tunnelWidth = 1,
                     float tunnelDiggableRatio = 0.5f)
        {
            Name = name;
            BiomeID = biomeID;
            MinRadius = minRadius;
            MaxRadius = maxRadius;
            EmptyTileRatio = emptyRatio;
            DiggableTileRatio = diggableRatio;
            UndiggableTileRatio = undiggableRatio;
            TunnelWidth = tunnelWidth;
            TunnelDiggableRatio = tunnelDiggableRatio;
        }

        // Predefined biome types
        // BiomeID is used by shaders to identify regions - artists map IDs to colors
        public static readonly Biome Apple = new Biome(
            name: "Apple",
            biomeID: 0,
            minRadius: 2,
            maxRadius: 4,
            emptyRatio: 0.7f,
            diggableRatio: 0.2f,
            undiggableRatio: 0.1f,
            tunnelWidth: 1,
            tunnelDiggableRatio: 0.6f
        );

        public static readonly Biome Orange = new Biome(
            name: "Orange",
            biomeID: 1,
            minRadius: 3,
            maxRadius: 5,
            emptyRatio: 0.5f,
            diggableRatio: 0.4f,
            undiggableRatio: 0.1f,
            tunnelWidth: 1,
            tunnelDiggableRatio: 0.5f
        );

        public static readonly Biome Banana = new Biome(
            name: "Banana",
            biomeID: 2,
            minRadius: 2,
            maxRadius: 3,
            emptyRatio: 0.4f,
            diggableRatio: 0.4f,
            undiggableRatio: 0.2f,
            tunnelWidth: 1,
            tunnelDiggableRatio: 0.4f
        );

        public static readonly Biome Grape = new Biome(
            name: "Grape",
            biomeID: 3,
            minRadius: 4,
            maxRadius: 6,
            emptyRatio: 0.6f,
            diggableRatio: 0.3f,
            undiggableRatio: 0.1f,
            tunnelWidth: 2,
            tunnelDiggableRatio: 0.7f
        );

        // Array of all biomes for easy iteration
        public static readonly Biome[] AllBiomes = { Apple, Orange, Banana, Grape };

        public override string ToString()
        {
            return $"{Name} (ID:{BiomeID})";
        }
    }
}

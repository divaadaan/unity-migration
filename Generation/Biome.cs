namespace DigDigDiner
{
    /// <summary>
    /// Represents a biome type for cavern chambers.
    /// Each biome defines unique generation parameters and gameplay characteristics.
    /// </summary>
    public class Biome
    {
        public string Name { get; private set; }

        // Cavern size properties
        public int MinRadius { get; set; }
        public int MaxRadius { get; set; }

        // Tile composition ratios (should sum to ~1.0)
        public float EmptyTileRatio { get; set; }
        public float DiggableTileRatio { get; set; }
        public float UndiggableTileRatio { get; set; }

        // Tunnel properties
        public int TunnelWidth { get; set; }
        public float TunnelDiggableRatio { get; set; } // Ratio of diggable vs empty in tunnels

        public Biome(string name,
                     int minRadius = 2,
                     int maxRadius = 4,
                     float emptyRatio = 0.6f,
                     float diggableRatio = 0.3f,
                     float undiggableRatio = 0.1f,
                     int tunnelWidth = 1,
                     float tunnelDiggableRatio = 0.5f)
        {
            Name = name;
            MinRadius = minRadius;
            MaxRadius = maxRadius;
            EmptyTileRatio = emptyRatio;
            DiggableTileRatio = diggableRatio;
            UndiggableTileRatio = undiggableRatio;
            TunnelWidth = tunnelWidth;
            TunnelDiggableRatio = tunnelDiggableRatio;
        }

        // Predefined biome types with distinct characteristics
        public static readonly Biome Apple = new Biome(
            name: "Apple",
            minRadius: 2,
            maxRadius: 4,
            emptyRatio: 0.7f,      // Open and spacious
            diggableRatio: 0.2f,
            undiggableRatio: 0.1f,
            tunnelWidth: 1,
            tunnelDiggableRatio: 0.6f
        );

        public static readonly Biome Orange = new Biome(
            name: "Orange",
            minRadius: 3,
            maxRadius: 5,
            emptyRatio: 0.5f,      // Balanced
            diggableRatio: 0.4f,
            undiggableRatio: 0.1f,
            tunnelWidth: 1,
            tunnelDiggableRatio: 0.5f
        );

        public static readonly Biome Banana = new Biome(
            name: "Banana",
            minRadius: 2,
            maxRadius: 3,
            emptyRatio: 0.4f,      // More obstacles
            diggableRatio: 0.4f,
            undiggableRatio: 0.2f,
            tunnelWidth: 1,
            tunnelDiggableRatio: 0.4f
        );

        public static readonly Biome Grape = new Biome(
            name: "Grape",
            minRadius: 4,
            maxRadius: 6,
            emptyRatio: 0.6f,      // Large and open
            diggableRatio: 0.3f,
            undiggableRatio: 0.1f,
            tunnelWidth: 2,         // Wider tunnels
            tunnelDiggableRatio: 0.7f
        );

        // Array of all biomes for easy iteration
        public static readonly Biome[] AllBiomes = { Apple, Orange, Banana, Grape };

        public override string ToString()
        {
            return $"{Name} (R:{MinRadius}-{MaxRadius}, E:{EmptyTileRatio:P0} D:{DiggableTileRatio:P0} U:{UndiggableTileRatio:P0})";
        }
    }
}

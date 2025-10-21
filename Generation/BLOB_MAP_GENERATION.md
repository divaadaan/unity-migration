# Blob-Based Map Generation System

## Overview

The new modular blob-based map generation system creates dynamic, varied maps by spawning pockets of different terrain types throughout a Diggable base layer. This replaces the old cavern+tunnel system with a more flexible, configurable approach.

## Architecture

### Core Components

1. **MapGenerator.cs** - Main orchestrator
   - Configurable blob spawn settings
   - Biome region management
   - Generation mode selection

2. **IBlobGenerator** - Interface for blob generation strategies
   - Defines contract for blob generators
   - Allows easy addition of new generation algorithms

3. **BlobSpawner.cs** - Manages spawning of terrain pockets
   - Handles spawn configurations
   - Manages spacing and placement
   - Applies generated blobs to grid

4. **BiomeManager.cs** - Regional biome assignment
   - Voronoi-based biome regions
   - Per-tile biome lookup
   - Shader color assignment support

5. **Biome.cs** - Biome definitions with shader properties
   - Visual characteristics (colors, brightness)
   - Legacy generation properties (kept for compatibility)

### Blob Generators

#### LargeBlobGenerator
- Creates organic, blob-shaped pockets
- Uses cellular automata for smoothing
- Configurable size and fill ratio
- Ideal for: chambers, rooms, large open areas

#### SnakeBlobGenerator
- Creates narrow, winding paths
- Uses random walk algorithm
- Configurable length and width
- Optional branching
- Ideal for: tunnels, corridors, veins

## Generation Process

### Step-by-Step Flow

1. **Fill with Diggable** - Entire map starts as Diggable terrain
2. **Create Border** - Undiggable perimeter for boundaries
3. **Generate Entrance** - Empty spawn area + diggable neck
4. **Spawn Empty Pockets** - Create chambers/tunnels (walkable areas)
5. **Spawn Undiggable Pockets** - Create obstacles/pillars
6. **Assign Biomes** - Regional biome assignment for shader coloring
7. **Refresh Visuals** - Update tilemap rendering

### Configuration

#### Blob Spawn Config
```csharp
BlobSpawnConfig
{
    configName:         Descriptive name
    terrainType:        Empty or Undiggable
    minCount:           Minimum number of blobs
    maxCount:           Maximum number of blobs
    minSpacing:         Minimum distance between blobs
    spawnProbability:   0-1 chance to spawn this config
    largeBlobWeight:    0-1 weight for large blobs
    snakeBlobWeight:    0-1 weight for snake blobs
}
```

#### Default Configurations

**Empty Pockets:**
- "Large Chambers" - 3-5 large blob-shaped rooms
- "Winding Tunnels" - 2-4 snake-like corridors

**Undiggable Pockets:**
- "Stone Pillars" - 2-4 large obstacles
- "Rock Veins" - 1-3 narrow rock formations

## Biome System

### Regional Assignment

Biomes are assigned using a Voronoi diagram approach:
1. Random biome centers are placed across the map
2. Each tile is assigned to the closest biome center
3. Creates natural-looking biome regions

### Shader Integration

**Biomes do NOT provide colors** - that's left entirely to the artist!

Each biome provides:
- **BiomeID** - Unique identifier (0-3) for shader lookup
- **Name** - Human-readable name for debugging

Artists define colors in shaders or scripts. See `SHADER_INTEGRATION.md` for detailed instructions.

### Predefined Biomes

| Biome ID | Name   | Description |
|----------|--------|-------------|
| 0        | Apple  | First biome region |
| 1        | Orange | Second biome region |
| 2        | Banana | Third biome region |
| 3        | Grape  | Fourth biome region |

## Usage in Unity

### Basic Setup

1. Add `MapGenerator` component to GameObject with `DualGridSystem`
2. Set Generation Mode to "BlobMap"
3. Configure entrance settings
4. Configure biome settings
5. Add/modify blob spawn configurations

### Inspector Configuration

**Entrance Settings:**
- Entrance Neck Width: Width of spawn area (default: 2)
- Entrance Neck Length: Length of diggable entrance (default: 3)
- Spawn Area Height: Height of empty spawn area (default: 2)

**Biome Settings:**
- Biome Region Count: Number of biome regions (default: 4)
- Biome Min Spacing: Minimum distance between biome centers (default: 8)

**Blob Configurations:**
- Empty Pocket Configs: List of configurations for Empty terrain
- Undiggable Pocket Configs: List of configurations for Undiggable terrain

### Runtime Access

```csharp
// Get biome at position
Biome biome = mapGenerator.GetBiomeAt(x, y);

// Use biome data
int id = biome.BiomeID;       // 0-3 (for shader lookup)
string name = biome.Name;     // "Apple", "Orange", etc.

// Access biome manager
BiomeManager biomeManager = mapGenerator.GetBiomeManager();
```

**For shader integration and color assignment, see `SHADER_INTEGRATION.md`**

## Extending the System

### Adding New Blob Generators

1. Create class implementing `IBlobGenerator`
2. Implement `GenerateBlob()` method
3. Register in `BlobSpawner` constructor

Example:
```csharp
public class CircularBlobGenerator : IBlobGenerator
{
    public string GetGeneratorName() => "CircularBlobGenerator";

    public List<Vector2Int> GenerateBlob(
        Vector2Int startPosition,
        TerrainType terrainType,
        int gridWidth,
        int gridHeight,
        System.Random random)
    {
        // Your generation logic here
        return blobPositions;
    }
}
```

### Adding New Biomes

In `Biome.cs`:
```csharp
public static readonly Biome MyBiome = new Biome(
    name: "MyBiome",
    primaryColor: new Color(r, g, b),
    secondaryColor: new Color(r, g, b),
    colorVariation: 0.2f,
    brightness: 1.0f
);

// Add to AllBiomes array
public static readonly Biome[] AllBiomes = {
    Apple, Orange, Banana, Grape, MyBiome
};
```

### Custom Blob Configurations

Add new configurations in `MapGenerator.InitializeDefaultEmptyConfigs()` or `InitializeDefaultUndiggableConfigs()`, or add them directly in the Unity Inspector.

## Comparison with Old System

### Old Cavern System (Moved to Generation/Old/)
- ✓ Guaranteed connectivity via MST
- ✓ Structured rooms and corridors
- ✗ Less variety in shapes
- ✗ Fixed room-tunnel paradigm
- ✗ More complex graph management

### New Blob System
- ✓ Highly modular and extensible
- ✓ Varied organic shapes
- ✓ Easy to configure without code
- ✓ Simpler architecture
- ✓ Better shader integration via biomes
- ✗ No guaranteed connectivity (player must dig)
- ✗ Requires balancing blob counts/spacing

## Future Enhancements

### Potential Additions
1. **Connectivity Blob Generator** - Ensure connected paths between Empty pockets
2. **Themed Blob Sets** - Biome-specific blob configurations
3. **Layered Generation** - Multi-pass generation with different priorities
4. **Procedural Biome Colors** - Generate biome colors procedurally
5. **Biome Transition Zones** - Gradual color blending between biomes
6. **Special Features** - Resource veins, treasure rooms, etc.

### Shader Integration
- Artists define biome colors in shaders or scripts
- Pass BiomeID to shaders for color lookup
- Apply tinting based on biome regions
- See `SHADER_INTEGRATION.md` for implementation examples

## Debug Tools

### Inspector Options
- **Show Debug Logs** - Enable/disable generation logging
- **Show Biome Gizmos** - Visualize biome regions in Scene view

### Context Menu
- **Regenerate Map** - Right-click MapGenerator component

### Gizmos
- Biome regions shown as colored spheres in Scene view
- Radius indicates biome influence area

## Performance Considerations

- **Blob Generation**: O(n) where n = blob size
- **Biome Assignment**: O(w × h × r) where w,h = grid dimensions, r = region count
- **Total Generation**: Typically <100ms for 100×100 grid

## Migration from Old System

The old cavern-based system has been moved to `Generation/Old/`:
- `MapGenerator_Cavern.cs` (renamed from MapGenerator.cs)
- `CavernNode.cs`
- `MapGraph.cs`
- `TunnelEdge.cs`

The new system is **not backwards compatible** but retains similar entrance generation and biome concepts.

## Credits

- Cellular automata smoothing inspired by roguelike cave generation
- Voronoi biome regions inspired by Minecraft biome system
- Modular architecture designed for rapid iteration and experimentation

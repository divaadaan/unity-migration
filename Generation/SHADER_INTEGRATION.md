# Biome-Based Shader Integration Guide

## For Artists

This document explains how to integrate the biome system with custom shaders to create beautiful, region-based tile coloring.

## Overview

The map generation system divides the map into **biome regions** using a Voronoi diagram approach. Each tile knows which biome it belongs to, but **the code does NOT specify colors** - that's entirely up to you as the artist!

## Biome System

### Available Biomes

| Biome ID | Name   | Description |
|----------|--------|-------------|
| 0        | Apple  | First biome region |
| 1        | Orange | Second biome region |
| 2        | Banana | Third biome region |
| 3        | Grape  | Fourth biome region |

**Note:** The names are just identifiers. You decide what colors/themes each biome represents!

### How Biomes Work

1. MapGenerator creates 4 random regions across the map
2. Each tile is assigned to the closest biome region center (Voronoi)
3. You can query `BiomeID` for any tile position
4. Use BiomeID in your shader to determine tile appearance

## Integration Methods

### Method 1: Tilemap Material Property Blocks (Recommended)

This method allows you to pass biome data to your tilemap shaders dynamically.

#### Step 1: Create a BiomeColorProvider Script

```csharp
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DigDigDiner
{
    [RequireComponent(typeof(Tilemap))]
    public class BiomeColorProvider : MonoBehaviour
    {
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private Tilemap targetTilemap;

        [Header("Biome Colors (Artist Defined)")]
        [SerializeField] private Color appleBiomeColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color orangeBiomeColor = new Color(1.0f, 0.6f, 0.1f);
        [SerializeField] private Color bananaBiomeColor = new Color(1.0f, 0.95f, 0.3f);
        [SerializeField] private Color grapeBiomeColor = new Color(0.6f, 0.2f, 0.9f);

        private MaterialPropertyBlock propBlock;

        void Start()
        {
            if (targetTilemap == null)
                targetTilemap = GetComponent<Tilemap>();

            propBlock = new MaterialPropertyBlock();

            // Apply biome colors to tilemap
            ApplyBiomeColorsToTilemap();
        }

        void ApplyBiomeColorsToTilemap()
        {
            if (mapGenerator == null || targetTilemap == null) return;

            var biomeManager = mapGenerator.GetBiomeManager();
            if (biomeManager == null) return;

            // Get tilemap bounds
            BoundsInt bounds = targetTilemap.cellBounds;

            foreach (var pos in bounds.allPositionsWithin)
            {
                // Get biome at this tile position
                Biome biome = biomeManager.GetBiomeAt(pos.x, pos.y);

                // Get color based on biome ID
                Color tileColor = GetColorForBiome(biome.BiomeID);

                // Apply to tile (this is a simplified example)
                // In practice, you'd pass this to your shader via a texture or custom data
                targetTilemap.SetTileFlags(pos, TileFlags.None);
                targetTilemap.SetColor(pos, tileColor);
            }
        }

        Color GetColorForBiome(int biomeID)
        {
            return biomeID switch
            {
                0 => appleBiomeColor,
                1 => orangeBiomeColor,
                2 => bananaBiomeColor,
                3 => grapeBiomeColor,
                _ => Color.white
            };
        }
    }
}
```

#### Step 2: Assign Colors in Inspector

1. Add `BiomeColorProvider` to your ColorTilemap GameObject
2. Assign the MapGenerator reference
3. **Customize the biome colors to your liking!**
4. Colors will automatically apply to tiles based on their biome region

### Method 2: Custom Shader with Biome Texture

For more advanced control, create a texture that stores biome IDs and sample it in your shader.

#### Step 1: Create Biome Data Texture

```csharp
using UnityEngine;

namespace DigDigDiner
{
    public class BiomeTextureGenerator : MonoBehaviour
    {
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private Material targetMaterial;
        [SerializeField] private string texturePropertyName = "_BiomeMap";

        void Start()
        {
            GenerateBiomeTexture();
        }

        void GenerateBiomeTexture()
        {
            var biomeManager = mapGenerator.GetBiomeManager();
            var gridSystem = mapGenerator.GetComponent<DualGridSystem>();

            if (biomeManager == null || gridSystem == null) return;

            // Create texture matching grid size
            Texture2D biomeTexture = new Texture2D(
                gridSystem.Width,
                gridSystem.Height,
                TextureFormat.RGBA32,
                false
            );
            biomeTexture.filterMode = FilterMode.Point; // No interpolation

            // Fill texture with biome IDs
            for (int y = 0; y < gridSystem.Height; y++)
            {
                for (int x = 0; x < gridSystem.Width; x++)
                {
                    Biome biome = biomeManager.GetBiomeAt(x, y);

                    // Encode biome ID in red channel (0-255)
                    float biomeValue = biome.BiomeID / 255f;
                    Color pixelColor = new Color(biomeValue, 0, 0, 1);

                    biomeTexture.SetPixel(x, y, pixelColor);
                }
            }

            biomeTexture.Apply();

            // Assign to material
            if (targetMaterial != null)
            {
                targetMaterial.SetTexture(texturePropertyName, biomeTexture);
            }
        }
    }
}
```

#### Step 2: Sample in Shader

```hlsl
// In your shader properties:
_BiomeMap ("Biome Map", 2D) = "white" {}
_BiomeColors ("Biome Colors", 2DArray) = "" {}

// Or define colors directly:
_AppleColor ("Apple Biome Color", Color) = (0.9, 0.2, 0.2, 1)
_OrangeColor ("Orange Biome Color", Color) = (1.0, 0.6, 0.1, 1)
_BananaColor ("Banana Biome Color", Color) = (1.0, 0.95, 0.3, 1)
_GrapeColor ("Grape Biome Color", Color) = (0.6, 0.2, 0.9, 1)

// In your fragment shader:
float4 frag(v2f i) : SV_Target
{
    // Sample biome map
    float biomeID = tex2D(_BiomeMap, i.uv).r * 255;

    // Get base color from your tile texture
    float4 baseColor = tex2D(_MainTex, i.uv);

    // Select biome color
    float4 biomeColor;
    if (biomeID < 0.5) // Apple (ID 0)
        biomeColor = _AppleColor;
    else if (biomeID < 1.5) // Orange (ID 1)
        biomeColor = _OrangeColor;
    else if (biomeID < 2.5) // Banana (ID 2)
        biomeColor = _BananaColor;
    else // Grape (ID 3)
        biomeColor = _GrapeColor;

    // Blend or multiply with base color
    float4 finalColor = baseColor * biomeColor;

    return finalColor;
}
```

### Method 3: Scriptable Render Pipeline (URP/HDRP)

For SRP, you can pass biome data via a custom render pass or compute buffer.

```csharp
// Example: Pass biome data as a compute buffer
ComputeBuffer biomeBuffer = new ComputeBuffer(width * height, sizeof(int));
int[] biomeData = new int[width * height];

for (int y = 0; y < height; y++)
{
    for (int x = 0; x < width; x++)
    {
        Biome biome = biomeManager.GetBiomeAt(x, y);
        biomeData[y * width + x] = biome.BiomeID;
    }
}

biomeBuffer.SetData(biomeData);
material.SetBuffer("_BiomeData", biomeBuffer);
```

## Creative Possibilities

### Biome Color Themes

You have complete freedom! Here are some ideas:

**Earthy Theme:**
- Apple → Brown/Dirt tones
- Orange → Sandy/Desert tones
- Banana → Grassy/Green tones
- Grape → Rocky/Gray tones

**Elemental Theme:**
- Apple → Fire (reds, oranges)
- Orange → Water (blues, cyans)
- Banana → Earth (browns, greens)
- Grape → Air (whites, light blues)

**Time-of-Day Theme:**
- Apple → Dawn (pinks, light oranges)
- Orange → Midday (bright yellows)
- Banana → Sunset (deep oranges, purples)
- Grape → Night (dark blues, purples)

### Advanced Effects

**Gradient Transitions:**
- Calculate distance to biome center
- Blend between primary and secondary colors
- Create smooth transitions between regions

**Noise Variation:**
- Add Perlin noise based on BiomeID
- Vary brightness/saturation within regions
- Create organic, non-uniform coloring

**Seasonal Themes:**
- Swap biome color palettes based on game state
- Animate color transitions
- Create dynamic, evolving environments

## Code Access

### Getting Biome Data at Runtime

```csharp
// Get MapGenerator reference
MapGenerator mapGen = FindObjectOfType<MapGenerator>();

// Get BiomeManager
BiomeManager biomeManager = mapGen.GetBiomeManager();

// Get biome at specific position
Biome biome = biomeManager.GetBiomeAt(x, y);

// Use biome ID
int id = biome.BiomeID;      // 0-3
string name = biome.Name;     // "Apple", "Orange", etc.
```

### Extending Biome Count

Want more than 4 biomes? Easy:

1. Add new biomes to `Biome.cs`:
```csharp
public static readonly Biome Mango = new Biome(
    name: "Mango",
    biomeID: 4,  // New ID
    // ... other params
);
```

2. Update `AllBiomes` array:
```csharp
public static readonly Biome[] AllBiomes = {
    Apple, Orange, Banana, Grape, Mango
};
```

3. Update `MapGenerator` settings:
```csharp
[SerializeField] private int biomeRegionCount = 5; // Increase count
```

4. Add color handling in your shader/script

## Performance Considerations

- **Biome lookup is O(1)**: Dictionary-based, very fast
- **Texture method**: Best for static maps
- **Property blocks**: Good for dynamic updates
- **Compute buffers**: Best for large maps with URP/HDRP

## Debugging

### Visualizing Biomes

In the MapGenerator Inspector:
- Enable "Show Biome Gizmos"
- Biome regions show as colored wireframe spheres in Scene view
- Colors are for debug only - they don't affect actual rendering

### Console Logs

Enable "Show Debug Logs" in MapGenerator to see:
- How many biome regions were placed
- Biome assignment counts
- Center positions and influence radii

## Summary

**The system provides:**
- BiomeID (0-3) for each tile
- Fast lookup via `BiomeManager.GetBiomeAt(x, y)`
- Voronoi-based regional distribution

**You (the artist) provide:**
- Color palettes for each biome
- Shader logic for applying colors
- Visual themes and variations

**No code changes needed** - just assign colors in Inspector or shader properties!

Have fun creating beautiful biome-based environments! 🎨

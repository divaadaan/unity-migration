using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    /// <summary>
    /// Generates a playable cavern map using graph-based approach with A* pathfinding.
    /// Creates random caverns connected by tunnels with biome-specific characteristics.
    /// </summary>
    [RequireComponent(typeof(DualGridSystem))]
    public class MapGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [SerializeField] private int numberOfCaverns = 4;
        [SerializeField] private int minCavernSpacing = 6;
        [SerializeField] private int entranceNeckWidth = 2;
        [SerializeField] private int entranceNeckLength = 3;
        [SerializeField] private int maxPlacementAttempts = 100;
        [SerializeField] private int extraConnections = 1;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showGraphDebug = false;

        private DualGridSystem gridSystem;
        private MapGraph mapGraph;
        private AStarPathfinder pathfinder;
        private System.Random random;

        private void Awake()
        {
            gridSystem = GetComponent<DualGridSystem>();
            mapGraph = new MapGraph();
            random = new System.Random();
        }

        private void Start()
        {
            GenerateMap();
        }

        /// <summary>
        /// Main map generation entry point using graph-based approach.
        /// </summary>
        public void GenerateMap()
        {
            if (showDebugLogs)
                Debug.Log("MapGenerator: Starting graph-based map generation...");

            // Initialize pathfinder
            pathfinder = new AStarPathfinder(gridSystem.Width, gridSystem.Height);

            // Clear previous generation
            mapGraph.Clear();

            // Step 1: Fill with undiggable terrain
            FillWithUndiggableTerrain();

            // Step 2: Create border
            GenerateBorder();

            // Step 3: Generate entrance
            Vector2Int entrancePos = GenerateEntranceNeck();
            mapGraph.EntrancePosition = entrancePos;

            // Step 4: Randomly place caverns
            PlaceCavernsRandomly();

            // Step 5: Build graph connections (MST + extra connections)
            BuildGraphConnections();

            // Step 6: Connect entrance to nearest cavern
            ConnectEntranceToCaverns(entrancePos);

            // Step 7: Generate tunnels using A* pathfinding
            GenerateTunnelsWithPathfinding();

            // Step 8: Generate caverns with randomized tiles
            GenerateCavernsWithRandomTiles();

            // Step 9: Validate connectivity
            ValidateConnectivity();

            // Step 10: Refresh visuals
            gridSystem.RefreshAllVisualTiles();

            if (showDebugLogs)
            {
                Debug.Log($"MapGenerator: Generation complete!");
                Debug.Log(mapGraph.GetDebugInfo());
            }

            if (showGraphDebug)
            {
                LogGraphStructure();
            }
        }

        /// <summary>
        /// Fills the entire map with undiggable terrain as a base.
        /// </summary>
        private void FillWithUndiggableTerrain()
        {
            for (int y = 0; y < gridSystem.Height; y++)
            {
                for (int x = 0; x < gridSystem.Width; x++)
                {
                    gridSystem.SetTileAtSilent(x, y, new Tile(TerrainType.Undiggable));
                }
            }

            if (showDebugLogs)
                Debug.Log("MapGenerator: Filled map with undiggable terrain");
        }

        /// <summary>
        /// Creates an undiggable border around the map perimeter.
        /// </summary>
        private void GenerateBorder()
        {
            for (int x = 0; x < gridSystem.Width; x++)
            {
                gridSystem.SetTileAtSilent(x, 0, new Tile(TerrainType.Undiggable));
                gridSystem.SetTileAtSilent(x, gridSystem.Height - 1, new Tile(TerrainType.Undiggable));
            }

            for (int y = 0; y < gridSystem.Height; y++)
            {
                gridSystem.SetTileAtSilent(0, y, new Tile(TerrainType.Undiggable));
                gridSystem.SetTileAtSilent(gridSystem.Width - 1, y, new Tile(TerrainType.Undiggable));
            }
        }

        /// <summary>
        /// Generates entrance neck and returns the bottom position.
        /// </summary>
        private Vector2Int GenerateEntranceNeck()
        {
            int centerX = gridSystem.Width / 2;
            int startY = gridSystem.Height - 2;
            int endY = startY - entranceNeckLength;

            for (int y = startY; y > endY && y > 0; y--)
            {
                for (int dx = -entranceNeckWidth / 2; dx <= entranceNeckWidth / 2; dx++)
                {
                    int x = centerX + dx;
                    if (x > 0 && x < gridSystem.Width - 1)
                    {
                        gridSystem.SetTileAtSilent(x, y, new Tile(TerrainType.Diggable));
                    }
                }
            }

            Vector2Int entranceBottom = new Vector2Int(centerX, endY);

            if (showDebugLogs)
                Debug.Log($"MapGenerator: Entrance at {entranceBottom}");

            return entranceBottom;
        }

        /// <summary>
        /// Randomly places caverns with minimum spacing constraint.
        /// </summary>
        private void PlaceCavernsRandomly()
        {
            int placedCount = 0;
            int attempts = 0;

            while (placedCount < numberOfCaverns && attempts < maxPlacementAttempts)
            {
                attempts++;

                // Random position (avoid edges)
                int x = random.Next(minCavernSpacing, gridSystem.Width - minCavernSpacing);
                int y = random.Next(minCavernSpacing, gridSystem.Height - minCavernSpacing - entranceNeckLength);

                Vector2Int center = new Vector2Int(x, y);

                // Check spacing from existing caverns
                if (!IsValidCavernPlacement(center))
                    continue;

                // Select random biome
                Biome biome = Biome.AllBiomes[random.Next(Biome.AllBiomes.Length)];

                // Random radius from biome range
                int radius = random.Next(biome.MinRadius, biome.MaxRadius + 1);

                // Add to graph
                mapGraph.AddCavern(center, radius, biome);
                placedCount++;

                if (showDebugLogs)
                    Debug.Log($"MapGenerator: Placed cavern {placedCount} at {center} ({biome.Name}, R={radius})");
            }

            if (placedCount < numberOfCaverns)
            {
                Debug.LogWarning($"MapGenerator: Only placed {placedCount}/{numberOfCaverns} caverns after {attempts} attempts");
            }
        }

        /// <summary>
        /// Checks if a cavern can be placed at the given position.
        /// </summary>
        private bool IsValidCavernPlacement(Vector2Int position)
        {
            foreach (var node in mapGraph.Nodes)
            {
                float distance = Vector2Int.Distance(position, node.Center);
                if (distance < minCavernSpacing)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Builds graph connections using MST and adds extra random connections.
        /// </summary>
        private void BuildGraphConnections()
        {
            if (mapGraph.Nodes.Count < 2)
                return;

            // Create minimum spanning tree for guaranteed connectivity
            mapGraph.CreateMinimumSpanningTree();

            // Add extra connections for loops
            mapGraph.AddRandomConnections(extraConnections);

            if (showDebugLogs)
                Debug.Log($"MapGenerator: Created {mapGraph.Edges.Count} tunnel connections");
        }

        /// <summary>
        /// Connects the entrance to the nearest cavern.
        /// </summary>
        private void ConnectEntranceToCaverns(Vector2Int entrancePos)
        {
            var nearestCavern = mapGraph.FindNearestCavern(entrancePos);
            if (nearestCavern == null)
            {
                Debug.LogError("MapGenerator: No caverns to connect to entrance!");
                return;
            }

            // Find path from entrance to cavern center
            var path = pathfinder.FindPath(entrancePos, nearestCavern.Center, pos => true);

            if (path != null)
            {
                // Carve entrance tunnel
                foreach (var pos in path)
                {
                    CarveDiggableTunnel(pos.x, pos.y, 1);
                }

                if (showDebugLogs)
                    Debug.Log($"MapGenerator: Connected entrance to cavern {nearestCavern.NodeId}");
            }
            else
            {
                Debug.LogError("MapGenerator: Failed to find path from entrance to cavern!");
            }
        }

        /// <summary>
        /// Generates all tunnels using A* pathfinding.
        /// </summary>
        private void GenerateTunnelsWithPathfinding()
        {
            foreach (var edge in mapGraph.Edges)
            {
                // Find path from one cavern center to another
                var path = pathfinder.FindPath(edge.StartNode.Center, edge.EndNode.Center, pos => true);

                if (path != null)
                {
                    edge.SetPath(path);

                    // Use biome tunnel width (average of both caverns)
                    int width = (edge.StartNode.Biome.TunnelWidth + edge.EndNode.Biome.TunnelWidth) / 2;

                    // Carve tunnel with randomized tiles
                    foreach (var pos in path)
                    {
                        CarveRandomizedTunnel(pos.x, pos.y, width, edge.StartNode.Biome);
                    }

                    if (showDebugLogs)
                        Debug.Log($"MapGenerator: Tunnel {edge.StartNode.NodeId}->{edge.EndNode.NodeId}: {path.Count} tiles");
                }
                else
                {
                    Debug.LogWarning($"MapGenerator: Failed to find path between caverns {edge.StartNode.NodeId} and {edge.EndNode.NodeId}");
                }
            }
        }

        /// <summary>
        /// Generates caverns with randomized tile composition based on biome.
        /// </summary>
        private void GenerateCavernsWithRandomTiles()
        {
            foreach (var cavern in mapGraph.Nodes)
            {
                int centerX = cavern.Center.x;
                int centerY = cavern.Center.y;
                int radius = cavern.Radius;
                Biome biome = cavern.Biome;

                // Create cavern area
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    for (int x = centerX - radius; x <= centerX + radius; x++)
                    {
                        if (x <= 0 || x >= gridSystem.Width - 1 || y <= 0 || y >= gridSystem.Height - 1)
                            continue;

                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));

                        if (distance <= radius)
                        {
                            // Inside cavern - use randomized tiles based on biome
                            TerrainType tileType = GetRandomCavernTile(biome, distance, radius);
                            gridSystem.SetTileAtSilent(x, y, new Tile(tileType));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets a random tile type for cavern based on biome and distance from center.
        /// </summary>
        private TerrainType GetRandomCavernTile(Biome biome, float distanceFromCenter, float radius)
        {
            // Center area: more empty tiles
            // Edge area: more obstacles

            float normalizedDistance = distanceFromCenter / radius;
            float emptyWeight = biome.EmptyTileRatio * (1.5f - normalizedDistance);
            float diggableWeight = biome.DiggableTileRatio;
            float undiggableWeight = biome.UndiggableTileRatio * normalizedDistance;

            float totalWeight = emptyWeight + diggableWeight + undiggableWeight;
            float roll = (float)random.NextDouble() * totalWeight;

            if (roll < emptyWeight)
                return TerrainType.Empty;
            else if (roll < emptyWeight + diggableWeight)
                return TerrainType.Diggable;
            else
                return TerrainType.Undiggable;
        }

        /// <summary>
        /// Carves a tunnel segment with randomized tiles.
        /// </summary>
        private void CarveRandomizedTunnel(int centerX, int centerY, int width, Biome biome)
        {
            for (int dy = -width; dy <= width; dy++)
            {
                for (int dx = -width; dx <= width; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x <= 0 || x >= gridSystem.Width - 1 || y <= 0 || y >= gridSystem.Height - 1)
                        continue;

                    // Randomize between empty and diggable based on biome
                    float roll = (float)random.NextDouble();
                    TerrainType tileType = roll < biome.TunnelDiggableRatio
                        ? TerrainType.Diggable
                        : TerrainType.Empty;

                    gridSystem.SetTileAtSilent(x, y, new Tile(tileType));
                }
            }
        }

        /// <summary>
        /// Carves a simple diggable tunnel (used for entrance connection).
        /// </summary>
        private void CarveDiggableTunnel(int centerX, int centerY, int width)
        {
            for (int dy = -width; dy <= width; dy++)
            {
                for (int dx = -width; dx <= width; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x <= 0 || x >= gridSystem.Width - 1 || y <= 0 || y >= gridSystem.Height - 1)
                        continue;

                    gridSystem.SetTileAtSilent(x, y, new Tile(TerrainType.Diggable));
                }
            }
        }

        /// <summary>
        /// Validates that all parts of the map are connected.
        /// </summary>
        private void ValidateConnectivity()
        {
            bool graphConnected = mapGraph.IsFullyConnected();

            if (showDebugLogs)
            {
                if (graphConnected)
                    Debug.Log("MapGenerator: Graph is fully connected!");
                else
                    Debug.LogWarning("MapGenerator: Graph has disconnected components!");
            }
        }

        /// <summary>
        /// Logs detailed graph structure for debugging.
        /// </summary>
        private void LogGraphStructure()
        {
            Debug.Log("=== MAP GRAPH STRUCTURE ===");
            foreach (var node in mapGraph.Nodes)
            {
                Debug.Log(node.ToString());
            }
            foreach (var edge in mapGraph.Edges)
            {
                Debug.Log(edge.ToString());
            }
        }

        /// <summary>
        /// Context menu command to regenerate the map.
        /// </summary>
        [ContextMenu("Regenerate Map")]
        public void RegenerateMap()
        {
            GenerateMap();
        }
    }
}

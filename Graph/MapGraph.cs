using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DigDigDiner
{
    /// <summary>
    /// Manages the graph structure of the map, including caverns and tunnels.
    /// Provides methods for graph construction and connectivity analysis.
    /// </summary>
    public class MapGraph
    {
        public List<CavernNode> Nodes { get; private set; }
        public List<TunnelEdge> Edges { get; private set; }
        public Vector2Int EntrancePosition { get; set; }

        private int nextNodeId = 0;

        public MapGraph()
        {
            Nodes = new List<CavernNode>();
            Edges = new List<TunnelEdge>();
        }

        /// <summary>
        /// Adds a new cavern node to the graph.
        /// </summary>
        public CavernNode AddCavern(Vector2Int center, int radius, Biome biome)
        {
            var node = new CavernNode(nextNodeId++, center, radius, biome);
            Nodes.Add(node);
            return node;
        }

        /// <summary>
        /// Adds a tunnel edge connecting two cavern nodes.
        /// </summary>
        public TunnelEdge AddTunnel(CavernNode start, CavernNode end, int width = 1)
        {
            // Check if connection already exists
            var existing = Edges.FirstOrDefault(e => e.ConnectsNodes(start, end));
            if (existing != null)
            {
                Debug.LogWarning($"Tunnel already exists between caverns {start.NodeId} and {end.NodeId}");
                return existing;
            }

            var edge = new TunnelEdge(start, end, width);
            Edges.Add(edge);

            // Update node connections (bidirectional)
            start.ConnectTo(end);
            end.ConnectTo(start);

            return edge;
        }

        /// <summary>
        /// Finds the nearest cavern to a given position.
        /// </summary>
        public CavernNode FindNearestCavern(Vector2Int position)
        {
            if (Nodes.Count == 0) return null;

            CavernNode nearest = Nodes[0];
            float minDistance = Vector2Int.Distance(position, nearest.Center);

            foreach (var node in Nodes)
            {
                float distance = Vector2Int.Distance(position, node.Center);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = node;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Creates a minimum spanning tree to ensure all caverns are connected.
        /// Uses Prim's algorithm.
        /// </summary>
        public void CreateMinimumSpanningTree()
        {
            if (Nodes.Count < 2) return;

            HashSet<CavernNode> inTree = new HashSet<CavernNode>();
            inTree.Add(Nodes[0]);

            while (inTree.Count < Nodes.Count)
            {
                CavernNode closestInTree = null;
                CavernNode closestOutTree = null;
                float minDistance = float.MaxValue;

                // Find the shortest edge connecting a node in the tree to a node outside
                foreach (var nodeIn in inTree)
                {
                    foreach (var nodeOut in Nodes)
                    {
                        if (!inTree.Contains(nodeOut))
                        {
                            float distance = nodeIn.DistanceTo(nodeOut);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                closestInTree = nodeIn;
                                closestOutTree = nodeOut;
                            }
                        }
                    }
                }

                if (closestOutTree != null)
                {
                    AddTunnel(closestInTree, closestOutTree);
                    inTree.Add(closestOutTree);
                }
            }
        }

        /// <summary>
        /// Adds extra random connections to make the graph more interesting.
        /// </summary>
        public void AddRandomConnections(int count, float maxDistance = float.MaxValue)
        {
            System.Random random = new System.Random();
            int attempts = 0;
            int maxAttempts = count * 10;

            while (Edges.Count - (Nodes.Count - 1) < count && attempts < maxAttempts)
            {
                attempts++;

                // Pick two random nodes
                var nodeA = Nodes[random.Next(Nodes.Count)];
                var nodeB = Nodes[random.Next(Nodes.Count)];

                // Skip if same node or already connected
                if (nodeA == nodeB || nodeA.IsConnectedTo(nodeB))
                    continue;

                // Skip if too far apart
                if (nodeA.DistanceTo(nodeB) > maxDistance)
                    continue;

                AddTunnel(nodeA, nodeB);
            }
        }

        /// <summary>
        /// Checks if all caverns are reachable from a starting position using BFS.
        /// Returns true if fully connected.
        /// </summary>
        public bool IsFullyConnected(CavernNode startNode = null)
        {
            if (Nodes.Count == 0) return true;

            startNode = startNode ?? Nodes[0];

            HashSet<CavernNode> visited = new HashSet<CavernNode>();
            Queue<CavernNode> queue = new Queue<CavernNode>();

            queue.Enqueue(startNode);
            visited.Add(startNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var neighbor in current.ConnectedNodes)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return visited.Count == Nodes.Count;
        }

        /// <summary>
        /// Gets debug info about the graph structure.
        /// </summary>
        public string GetDebugInfo()
        {
            return $"MapGraph: {Nodes.Count} caverns, {Edges.Count} tunnels, " +
                   $"Connected: {IsFullyConnected()}";
        }

        public void Clear()
        {
            Nodes.Clear();
            Edges.Clear();
            nextNodeId = 0;
        }
    }
}

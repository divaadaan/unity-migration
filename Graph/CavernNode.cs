using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    /// <summary>
    /// Represents a cavern chamber in the map graph.
    /// Each node stores its position, size, biome, and connections to other caverns.
    /// </summary>
    public class CavernNode
    {
        public Vector2Int Center { get; private set; }
        public int Radius { get; set; }
        public Biome Biome { get; set; }
        public List<CavernNode> ConnectedNodes { get; private set; }
        public int NodeId { get; private set; }

        public CavernNode(int id, Vector2Int center, int radius, Biome biome)
        {
            NodeId = id;
            Center = center;
            Radius = radius;
            Biome = biome;
            ConnectedNodes = new List<CavernNode>();
        }

        /// <summary>
        /// Adds a connection to another cavern node.
        /// </summary>
        public void ConnectTo(CavernNode other)
        {
            if (!ConnectedNodes.Contains(other))
            {
                ConnectedNodes.Add(other);
            }
        }

        /// <summary>
        /// Checks if this cavern is connected to another cavern.
        /// </summary>
        public bool IsConnectedTo(CavernNode other)
        {
            return ConnectedNodes.Contains(other);
        }

        /// <summary>
        /// Gets the distance from this cavern's center to another cavern's center.
        /// </summary>
        public float DistanceTo(CavernNode other)
        {
            return Vector2Int.Distance(Center, other.Center);
        }

        /// <summary>
        /// Checks if a position is within this cavern's radius.
        /// </summary>
        public bool ContainsPosition(Vector2Int position)
        {
            return Vector2Int.Distance(Center, position) <= Radius;
        }

        public override string ToString()
        {
            return $"Cavern {NodeId} ({Biome.Name}) at {Center}, radius {Radius}, {ConnectedNodes.Count} connections";
        }
    }
}

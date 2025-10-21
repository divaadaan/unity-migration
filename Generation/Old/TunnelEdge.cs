using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    /// <summary>
    /// Represents a tunnel connection between two cavern nodes.
    /// Stores the path waypoints that connect the caverns.
    /// </summary>
    public class TunnelEdge
    {
        public CavernNode StartNode { get; private set; }
        public CavernNode EndNode { get; private set; }
        public List<Vector2Int> PathWaypoints { get; private set; }
        public int TunnelWidth { get; set; }

        public TunnelEdge(CavernNode start, CavernNode end, int width = 1)
        {
            StartNode = start;
            EndNode = end;
            TunnelWidth = width;
            PathWaypoints = new List<Vector2Int>();
        }

        /// <summary>
        /// Sets the path for this tunnel using waypoints from pathfinding.
        /// </summary>
        public void SetPath(List<Vector2Int> waypoints)
        {
            PathWaypoints = new List<Vector2Int>(waypoints);
        }

        /// <summary>
        /// Checks if this edge connects the given two nodes (bidirectional).
        /// </summary>
        public bool ConnectsNodes(CavernNode nodeA, CavernNode nodeB)
        {
            return (StartNode == nodeA && EndNode == nodeB) ||
                   (StartNode == nodeB && EndNode == nodeA);
        }

        /// <summary>
        /// Gets the length of the tunnel path.
        /// </summary>
        public int PathLength
        {
            get { return PathWaypoints.Count; }
        }

        public override string ToString()
        {
            return $"Tunnel: Cavern {StartNode.NodeId} -> Cavern {EndNode.NodeId} ({PathLength} waypoints, width {TunnelWidth})";
        }
    }
}

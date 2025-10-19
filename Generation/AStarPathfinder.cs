using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DigDigDiner
{
    /// <summary>
    /// A* pathfinding implementation for finding paths through the grid.
    /// Used to connect caverns with tunnels.
    /// </summary>
    public class AStarPathfinder
    {
        private class PathNode
        {
            public Vector2Int Position { get; set; }
            public float GCost { get; set; } // Distance from start
            public float HCost { get; set; } // Heuristic distance to end
            public float FCost => GCost + HCost;
            public PathNode Parent { get; set; }

            public PathNode(Vector2Int position)
            {
                Position = position;
            }
        }

        private int gridWidth;
        private int gridHeight;

        public AStarPathfinder(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;
        }

        /// <summary>
        /// Finds a path from start to end using A* algorithm.
        /// Returns null if no path exists.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, System.Func<Vector2Int, bool> isWalkable)
        {
            // Validate start and end positions
            if (!IsInBounds(start) || !IsInBounds(end))
            {
                Debug.LogWarning($"AStarPathfinder: Start {start} or end {end} out of bounds");
                return null;
            }

            List<PathNode> openSet = new List<PathNode>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

            PathNode startNode = new PathNode(start) { GCost = 0, HCost = GetHeuristic(start, end) };
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                // Get node with lowest F cost
                PathNode current = openSet.OrderBy(n => n.FCost).ThenBy(n => n.HCost).First();

                // Check if we reached the goal
                if (current.Position == end)
                {
                    return ReconstructPath(current);
                }

                openSet.Remove(current);
                closedSet.Add(current.Position);

                // Check all neighbors
                foreach (var neighborPos in GetNeighbors(current.Position))
                {
                    if (closedSet.Contains(neighborPos))
                        continue;

                    // Skip unwalkable tiles (but allow the end position even if unwalkable)
                    if (neighborPos != end && !isWalkable(neighborPos))
                        continue;

                    float tentativeGCost = current.GCost + GetMoveCost(current.Position, neighborPos);

                    PathNode neighborNode = openSet.FirstOrDefault(n => n.Position == neighborPos);

                    if (neighborNode == null)
                    {
                        neighborNode = new PathNode(neighborPos)
                        {
                            GCost = tentativeGCost,
                            HCost = GetHeuristic(neighborPos, end),
                            Parent = current
                        };
                        openSet.Add(neighborNode);
                    }
                    else if (tentativeGCost < neighborNode.GCost)
                    {
                        neighborNode.GCost = tentativeGCost;
                        neighborNode.Parent = current;
                    }
                }
            }

            // No path found
            return null;
        }

        /// <summary>
        /// Gets the 4-directional neighbors of a position.
        /// </summary>
        private List<Vector2Int> GetNeighbors(Vector2Int pos)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>
            {
                new Vector2Int(pos.x + 1, pos.y),
                new Vector2Int(pos.x - 1, pos.y),
                new Vector2Int(pos.x, pos.y + 1),
                new Vector2Int(pos.x, pos.y - 1)
            };

            return neighbors.Where(n => IsInBounds(n)).ToList();
        }

        /// <summary>
        /// Checks if a position is within the grid bounds.
        /// </summary>
        private bool IsInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
        }

        /// <summary>
        /// Calculates the heuristic distance (Manhattan distance).
        /// </summary>
        private float GetHeuristic(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }

        /// <summary>
        /// Gets the cost of moving between two adjacent positions.
        /// </summary>
        private float GetMoveCost(Vector2Int from, Vector2Int to)
        {
            // Simple: all moves cost 1
            return 1.0f;
        }

        /// <summary>
        /// Reconstructs the path by following parent pointers.
        /// </summary>
        private List<Vector2Int> ReconstructPath(PathNode endNode)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            PathNode current = endNode;

            while (current != null)
            {
                path.Add(current.Position);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }
    }
}

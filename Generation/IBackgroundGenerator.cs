using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    public interface IBackgroundGenerator
    {
        string Name { get; }
        /// <summary>
        /// Returns a set of positions that should be ACTIVE (1). All others are DEFAULT (0).
        /// </summary>
        HashSet<Vector2Int> Generate(int width, int height, int seed);
    }
}
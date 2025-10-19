using UnityEngine;

namespace DigDigDiner
{
    /// <summary>
    /// Defines the three terrain types used in the mining game
    /// </summary>
    public enum TerrainType
    {
        Empty = 0,      // Can walk through
        Diggable = 1,   // Can be dug to become empty
        Undiggable = 2  // Cannot be modified (walls/barriers)
    }
}
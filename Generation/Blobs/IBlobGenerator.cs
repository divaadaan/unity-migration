using UnityEngine;
using System.Collections.Generic;

namespace DigDigDiner
{
    /// <summary>
    /// Interface for blob generation strategies.
    /// Blob generators create pockets of Empty or Undiggable tiles with different shapes.
    /// </summary>
    public interface IBlobGenerator
    {
        /// <summary>
        /// Generates a blob starting from the given position.
        /// Returns a list of grid positions that make up the blob.
        /// </summary>
        /// <param name="startPosition">Starting position for the blob</param>
        /// <param name="terrainType">Type of terrain to generate (Empty or Undiggable)</param>
        /// <param name="gridWidth">Grid width boundary</param>
        /// <param name="gridHeight">Grid height boundary</param>
        /// <param name="random">Random number generator for consistency</param>
        /// <returns>List of positions that make up the blob</returns>
        List<Vector2Int> GenerateBlob(
            Vector2Int startPosition,
            TerrainType terrainType,
            int gridWidth,
            int gridHeight,
            System.Random random
        );

        /// <summary>
        /// Gets the name of this blob generator for debugging/logging.
        /// </summary>
        string GetGeneratorName();
    }
}

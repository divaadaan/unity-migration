using UnityEngine;

namespace MiningGame
{
    public static class SharedConstants
    {
        // Grid dimensions
        public const int GRID_WIDTH = 10;
        public const int GRID_HEIGHT = 10;
        
        // Tilemap layout
        public const int TILEMAP_COLUMNS = 8;
        public const int TILEMAP_ROWS = 10;
        public const int TOTAL_PATTERNS = 80;
        
        // Tile dimensions
        public const int SOURCE_TILE_SIZE = 200;  // Artist tilemap in pixels
        public const float RENDER_TILE_SIZE = 1.0f;  // Unity world units per tile
        
        // Visual offset (draw grid offset from base grid)
        public static readonly Vector2 VISUAL_OFFSET = new Vector2(-0.5f, -0.5f);
        
        // Terrain type count
        public const int TERRAIN_TYPE_COUNT = 3; // Empty, Diggable, Undiggable
        
        // Pattern calculation
        public const int TOTAL_POSSIBLE_PATTERNS = 81; // 3^4 including all-empty
        
        // Debug colors
        public static readonly Color DEBUG_EMPTY_COLOR = new Color(1f, 1f, 1f, 0.3f);
        public static readonly Color DEBUG_DIGGABLE_COLOR = new Color(0.5f, 0.5f, 1f, 0.3f);
        public static readonly Color DEBUG_UNDIGGABLE_COLOR = new Color(1f, 0f, 0f, 0.3f);
        
        // Camera defaults
        public const float DEFAULT_CAMERA_MOVE_SPEED = 10f;
        public const float DEFAULT_CAMERA_SMOOTH_TIME = 0.1f;
        public const float CAMERA_BOUND_PADDING = 2f;
    }
}
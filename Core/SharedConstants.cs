using UnityEngine;

namespace DigDigDiner
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

        // Player Movement
        public const float PLAYER_MOVE_SPEED = 5f;
        public const float PLAYER_MOVE_COOLDOWN = 0.15f; // Delay between moves for grid-based movement

        // Player Rendering
        public const float PLAYER_BODY_RADIUS = 0.05f;  // Reduced from 0.3f (half size)
        public const float PLAYER_BOB_SPEED = 3f;
        public const float PLAYER_BOB_AMOUNT = 0.05f;   // Reduced from 0.1f (proportional to size)
        public const float PLAYER_SHADOW_ALPHA = 0.4f;
        public const float PLAYER_SHADOW_OFFSET_Y = -0.1f;  // Reduced from -0.2f (proportional to size)
        public const float PLAYER_DIRECTION_INDICATOR_OFFSET = 0.18f;  // Reduced from 0.35f (proportional to size)

        // Player Dig Preview
        public const float DIG_PREVIEW_ALPHA = 0.5f;
        public static readonly Color DIG_PREVIEW_COLOR = new Color(1f, 1f, 0f, 0.5f); // Yellow highlight

        // Player Camera
        public const float PLAYER_CAMERA_SMOOTH_TIME = 0.15f;
        public const float PLAYER_CAMERA_OFFSET_Z = -10f;
        public const float PLAYER_CAMERA_BOUND_PADDING = 1f;
    }
}
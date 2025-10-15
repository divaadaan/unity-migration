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
        public const int SOURCE_TILE_SIZE = 200;  // Artist tilemap
        public const float RENDER_TILE_SIZE = 1.0f;  // Unity world units
        
        // Visual offset
        public static readonly Vector2 VISUAL_OFFSET = new Vector2(-0.5f, -0.5f);
    }
}
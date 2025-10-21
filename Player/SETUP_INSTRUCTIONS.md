# Player System Setup Instructions

## Overview
This document provides step-by-step instructions for setting up the Player system in Unity 6.2.

## Scripts Created
All scripts are located in `Assets/Scripts/MiningGame/Player/`:
- `Player.cs` - Main player controller
- `PlayerInputHandler.cs` - Keyboard input handling
- `PlayerMovement.cs` - Movement logic and collision
- `PlayerDigging.cs` - Digging mechanics
- `PlayerRenderer.cs` - Visual rendering
- `PlayerCamera.cs` - Camera following system

## Unity Editor Setup

### Step 1: Create Player GameObject

1. In the Unity Hierarchy, create a new empty GameObject
2. Name it "Player"
3. Set Position to (0, 0, 0)
4. Add the `Player.cs` script component
   - The script will automatically add the other required components

### Step 2: Configure Player Component

In the Inspector for the Player GameObject:

**References:**
- `Grid System`: Drag the DualGridSystem GameObject from the Hierarchy

**Spawn Settings:**
- `Spawn Position`: Default is (5, 5) - only used if Auto Find Spawn is disabled
- `Auto Find Spawn`: Leave checked (will find first valid Empty tile)

### Step 3: Create Player Camera

1. In the Hierarchy, create a new Camera
2. Name it "PlayerCamera"
3. Position it at (0, 0, -10)
4. Add the `PlayerCamera.cs` script component

**Configure PlayerCamera:**
- `Player`: Drag the Player GameObject from the Hierarchy
- `Smooth Time`: 0.15 (default, adjust for slower/faster camera)
- `Bound Padding`: 1.0 (default)
- `Constrain To Bounds`: Checked

**Camera Settings:**
- Set `Projection` to Orthographic
- Set `Size` to 5 (or adjust for your desired zoom level)
- Set `Clear Flags` to Solid Color
- Set `Background` to desired color (e.g., black or dark blue)

**Audio Listener:**
- PlayerCamera automatically has an Audio Listener component
- This is the active audio listener for gameplay

### Step 4: Configure Main Camera (Optional)

If you want to keep the existing CameraController for manual debugging:

1. Select the Main Camera in the Hierarchy
2. **Remove the Audio Listener component** (only one Audio Listener allowed in scene)
3. In the CameraController component:
   - Set `Enabled By Default` to **false** (F1 will enable it)
4. You can now toggle between PlayerCamera (automatic) and manual camera control (F1)

Alternatively, you can disable/remove the Main Camera GameObject entirely if you don't need it.

### Step 5: Verify DualGridSystem Setup

Make sure your scene has:
1. A GameObject with the `DualGridSystem` component
2. DualGridSystem configured with:
   - Color Tilemap reference
   - Normal Tilemap reference
   - Tile Mapping asset
   - Artist Color Tiles array
   - (Optional) Artist Normal Tiles array

The MapGenerator should run and populate the grid with Empty/Diggable/Undiggable tiles.

## Controls

**Player Movement:**
- Arrow Keys: Move Up/Down/Left/Right
- Space: Dig tile in facing direction

**Camera:**
- Automatic: PlayerCamera follows player automatically
- Manual (if enabled): Press F1 to toggle manual camera control

**Debug:**
- Toggle Debug Overlay: (Check DualGridSystem input mapping)

## Testing Checklist

### Movement Tests
- [ ] Player spawns in a valid Empty tile
- [ ] Player moves up/down/left/right with arrow keys
- [ ] Player cannot move into Diggable tiles
- [ ] Player cannot move into Undiggable tiles
- [ ] Player cannot move outside grid boundaries
- [ ] Player facing direction updates based on movement input
- [ ] Player facing direction updates even when blocked

### Digging Tests
- [ ] Dig preview highlights adjacent Diggable tile
- [ ] Dig preview does NOT show for Empty tiles
- [ ] Dig preview does NOT show for Undiggable tiles
- [ ] Dig preview does NOT show for out-of-bounds positions
- [ ] Pressing Space digs the highlighted tile (Diggable → Empty)
- [ ] Visual tiles update immediately after digging
- [ ] Player can move into newly dug tile

### Visual Tests
- [ ] Player body renders as colored circle
- [ ] Direction indicator points in facing direction
- [ ] Shadow renders below player
- [ ] Bobbing animation plays smoothly
- [ ] Dig preview appears when facing Diggable tile

### Camera Tests
- [ ] Camera follows player smoothly
- [ ] Camera stays within map boundaries
- [ ] Camera doesn't show out-of-bounds areas
- [ ] (Optional) F1 toggles manual camera control

## Troubleshooting

### Player doesn't appear
- Check that PlayerRenderer is attached and initialized
- Check Console for errors
- Verify Player GameObject is not at z = -10 (behind camera)

### Player can't move
- Verify grid has Empty tiles (check debug overlay)
- Check that Player spawned in valid position
- Look for errors in Console related to DualGridSystem

### Dig doesn't work
- Ensure you're facing a Diggable tile (dig preview should show)
- Check Console for digging-related errors
- Verify DualGridSystem SetTileAt is working

### Camera doesn't follow
- Check PlayerCamera has Player reference assigned
- Verify PlayerCamera z-position is negative (e.g., -10)
- Check that PlayerCamera component is enabled

### Visual artifacts
- Check sorting layers and order in layer values
- Ensure Player is on appropriate layer
- Verify Camera culling mask includes Player layer

## Customization

### Change Player Color
In PlayerRenderer.cs, modify the `playerColor` field in the Inspector or code:
```csharp
[SerializeField] private Color playerColor = new Color(0.2f, 0.8f, 0.3f);
```

### Adjust Movement Speed
In SharedConstants.cs:
```csharp
public const float PLAYER_MOVE_COOLDOWN = 0.15f; // Lower = faster
```

### Adjust Camera Follow Speed
In SharedConstants.cs:
```csharp
public const float PLAYER_CAMERA_SMOOTH_TIME = 0.15f; // Lower = faster
```

### Adjust Bobbing Animation
In SharedConstants.cs:
```csharp
public const float PLAYER_BOB_SPEED = 3f; // Higher = faster bobbing
public const float PLAYER_BOB_AMOUNT = 0.1f; // Higher = more vertical movement
```

## Next Steps (Future Enhancements)

1. **Replace placeholder graphics with artist sprites:**
   - Create sprite assets for player body, direction indicator, shadow
   - Assign in PlayerRenderer component

2. **Add animations:**
   - Idle animation
   - Walking animation (4 directions)
   - Digging animation

3. **Add audio:**
   - Movement sound effects
   - Digging sound effects
   - Background music

4. **Add particle effects:**
   - Dust particles when moving
   - Dirt/debris when digging

5. **Add game mechanics:**
   - Collectibles
   - Enemies
   - Resources
   - Health system

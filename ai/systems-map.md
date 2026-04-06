# Systems Map

Last updated: 2026-04-06

Purpose: map the current active systems and note which areas are high impact.

## Active Systems

### Scene bootstrap and world setup

- Files: `Assets/Scripts/TransportPrototypeBootstrap.cs`, `Assets/Scenes/SampleScene.unity`
- Includes:
  - camera framing
  - directional light setup
  - ground plane
  - visible grid lines
  - location spawning

### Road placement and grid occupancy

- Files: `Assets/Scripts/TransportPrototypeBootstrap.cs`
- Includes:
  - click-to-place roads
  - grid snapping
  - occupation blocking on location cells
  - simple road tile stretching toward neighbors

### Truck movement and resource flow

- Files: `Assets/Scripts/TransportPrototypeBootstrap.cs`
- Includes:
  - BFS pathfinding over road cells and anchors
  - smooth cell-to-cell truck motion
  - automatic `wood` production at Forest
  - automatic delivery chain `Forest -> Warehouse -> Town`

### Minimal runtime UI

- Files: `Assets/Scripts/TransportPrototypeBootstrap.cs`
- Includes:
  - current mode display
  - per-location wood counts
  - basic controls hint

## Impact Hints

- Change to road placement or path validity:
  read the placement and `FindPath()` flow first.
- Change to truck behavior:
  read `DetermineNextTask()`, `StartMoveTo()`, and `UpdateTruckMovement()` together.
- Change to scene readability:
  read camera, lighting, grid, and location setup together.

## Current Cleanup Reality

- The whole vertical slice is centralized in one script.
- The first likely future seam is splitting setup, grid/pathfinding, and transport simulation into separate files.

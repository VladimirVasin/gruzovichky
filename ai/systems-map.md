# Systems Map

Last updated: 2026-04-06

Purpose: map the current active systems and note which areas are high impact.

## Active Systems

### Scene bootstrap and world setup

- Files: `Assets/Scripts/Runtime/Core/GameBootstrap.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.World.cs`, `Assets/Scenes/SampleScene.unity`
- Includes:
  - camera framing
  - directional light setup
  - ground plane
  - visible grid lines
  - location spawning

### Road placement and grid occupancy

- Files: `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.cs`, `Assets/Scripts/Runtime/Transport/Services/GridPathService.cs`, `Assets/Scripts/Runtime/Transport/Services/RoadLanternPlanner.cs`
- Includes:
  - click-to-place roads
  - grid snapping
  - occupation blocking on location cells
  - simple road tile stretching toward neighbors

### Truck movement and resource flow

- Files: `Assets/Scripts/Runtime/Actors/GameBootstrap.Actors.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.TruckState.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.RouteRuntime.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Orders.cs`, `Assets/Scripts/Runtime/Transport/Services/GridPathService.cs`, `Assets/Scripts/Runtime/Transport/Services/TruckAutoPlanner.cs`, `Assets/Scripts/Runtime/Transport/Services/TripRewardCalculator.cs`
- Includes:
  - BFS pathfinding over road cells and anchors
  - smooth cell-to-cell truck motion
  - multi-truck route and refuel flow
  - automatic `wood` production at Forest
  - automatic delivery chain `Forest -> Warehouse -> Town`

### Minimal runtime UI

- Files: `Assets/Scripts/Runtime/UI/GameBootstrap.UI.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Selection.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Fleet.cs`, `Assets/Scripts/Runtime/UI/SelectionVisualService.cs`
- Includes:
  - fleet list and truck details
  - building info cards
  - money / time / speed panels
  - route assignment and command feedback

## Impact Hints

- Change to road placement or path validity:
  read `GameBootstrap.Input` and `GameBootstrap.Transport` together.
- Change to truck behavior:
  read `GameBootstrap.Actors`, `GameBootstrap.Transport`, and `GameBootstrap.Fleet` together.
- Change to scene readability:
  read `GameBootstrap`, `GameBootstrap.World`, `GameBootstrap.Selection`, and `GameBootstrap.UI` together.

## Current Cleanup Reality

- The vertical slice still runs through one `GameBootstrap` class.
- The code is now split into partial scripts by concern under `Assets/Scripts/Runtime/`.

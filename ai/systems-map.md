# Systems Map

Last updated: 2026-04-25

Purpose: map the current active systems and note which areas are high impact.

## Active Systems

### Scene bootstrap and world setup

- Files: `Assets/Scripts/Runtime/Core/GameBootstrap.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.World.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.World.MiscDecorations.cs`, `Assets/Scripts/Runtime/World/BuildingPlacementService.cs`, `Assets/Scripts/Runtime/World/MiscDecorationSpawnService.cs`, `Assets/Scripts/Runtime/World/WorldLayoutGenerator.cs`, `Assets/Scripts/Runtime/World/WorldLayoutRoadValidator.cs`, `Assets/Scenes/SampleScene.unity`
- Includes:
  - camera framing
  - directional light setup
  - ground plane
  - visible grid lines
  - location spawning
  - misc tree/bush/flower primitive decoration visuals
  - generic rotated building placement and preview math
  - generated layout road-access validation
  - testable misc decoration spawn bucket selection

### Road placement and grid occupancy

- Files: `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.BuildRoad.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.Road*.cs`, `Assets/Scripts/Runtime/Transport/Services/GridPathService.cs`, `Assets/Scripts/Runtime/Transport/Services/RoadBuildPlacementService.cs`, `Assets/Scripts/Runtime/Transport/Services/RoadLanternPlanner.cs`, `Assets/Scripts/Runtime/Transport/Services/RoadMarkingPlanner.cs`, `Assets/Scripts/Runtime/Transport/Services/TwoLaneRoadGeometry.cs`
- Includes:
  - click-to-place roads
  - grid snapping
  - occupation blocking on location cells
  - simple road tile stretching toward neighbors
  - two-lane road footprints and right-hand lane geometry
  - build-mode road footprint placement validation
  - testable two-lane road marking/axis decisions

### Truck movement and resource flow

- Files: `Assets/Scripts/Runtime/Actors/GameBootstrap.Actors.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.TruckState.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.RouteRuntime.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.LocalBus.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Orders.cs`, `Assets/Scripts/Runtime/Transport/Services/GridPathService.cs`, `Assets/Scripts/Runtime/Transport/Services/BusStopOrderingService.cs`, `Assets/Scripts/Runtime/Transport/Services/LocalBusRoutePlanner.cs`, `Assets/Scripts/Runtime/Transport/Services/LocalBusRuntimeService.cs`, `Assets/Scripts/Runtime/Transport/Services/LocalBusPassengerService.cs`, `Assets/Scripts/Runtime/Transport/Services/TruckAutoPlanner.cs`, `Assets/Scripts/Runtime/Transport/Services/TruckRuntimeGuardService.cs`, `Assets/Scripts/Runtime/Transport/Services/TruckTripRuntimeService.cs`, `Assets/Scripts/Runtime/Transport/Services/TruckRefuelRuntimeService.cs`, `Assets/Scripts/Runtime/Transport/Services/TripRewardCalculator.cs`, `Assets/Scripts/Runtime/Core/Services/TradeAutoDispatchService.cs`, `Assets/Scripts/Runtime/Core/Services/TradeDispatchPreconditionService.cs`, `Assets/Scripts/Runtime/Core/Services/TradeOrderQueueService.cs`, `Assets/Scripts/Runtime/Core/Services/TradeRunRuntimeService.cs`
- Includes:
  - BFS pathfinding over road cells and anchors
  - smooth cell-to-cell truck motion
  - multi-truck route and refuel flow
  - automatic `wood` production at Forest
  - automatic delivery chain `Forest -> Warehouse -> Town`
  - local bus route stop-order decisions
  - deterministic local bus-stop ordering
  - local bus dwell/movement stepping seams
  - local bus passenger fare/capacity/fallback decisions
  - truck runtime guard seams for trip/refuel phases
  - regular-trip/refuel phase-step decision services
  - trade auto-dispatch retry/weekend gating seam
  - trade dispatch precondition reason seam
  - trade order queue lifecycle seam
  - trade-run phase helper for parking/highway/off-map decisions

### Minimal runtime UI

- Files: `Assets/Scripts/Runtime/UI/GameBootstrap.UI.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Selection.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Fleet.cs`, `Assets/Scripts/Runtime/UI/SelectionVisualService.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/FleetCanvasUiFactory.cs`, `Assets/Scripts/Runtime/UI/Localization/LocalizedStringTable.cs`, `Assets/Scripts/Runtime/UI/Localization/LocalizationJsonLoader.cs`, `Assets/Resources/Localization/ui.ru.json`
- Includes:
  - fleet list and truck details
  - building info cards
  - money / time / speed panels
  - route assignment and command feedback
  - shared FleetCanvas primitive/section-card/tab-row/scroll-panel factory seam
  - table-style localization lookup with JSON override seed for MainMenu/PatchNotes/Economy/Trade/Workers/Roles labels

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

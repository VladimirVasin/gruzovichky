# Project Overview

Last updated: 2026-04-06

## Purpose

Stable high-level map of the current playable slice.

## Project Shape

- Unity 3D (Core) prototype
- Current active slice is a simple grid-based transport sandbox:
  - visible placement grid
  - three fixed logistics locations
  - player-built roads
  - one truck moving on connected road cells
  - one resource (`wood`) transferred automatically

## Main Gameplay Locations

```text
Assets/
  Scenes/
  Scripts/
  Settings/
```

## Script Layout

- `Assets/Scripts/Runtime/Core/GameBootstrap.cs`
  Main runtime bootstrap/orchestrator for the playable scene.
- `Assets/Scripts/Runtime/World/GameBootstrap.World.cs`
  Thin world adapter between scene state and generation services.
- `Assets/Scripts/Runtime/World/WorldLayoutGenerator.cs`
  Random placement generator for `Parking`, `Gas Station`, `Forest`, `Warehouse`, and `Town`.
- `Assets/Scripts/Runtime/World/TerrainHeightGenerator.cs`
  Standalone terrain heightmap generation with flat pads under buildings.
- `Assets/Scripts/Runtime/World/MiscTreePlanner.cs`
  Standalone planner for misc tree spawn cells on free tiles.
- `Assets/Scripts/Runtime/Actors/GameBootstrap.Actors.cs`
  Truck, driver, lights, and actor visuals/setup.
- `Assets/Scripts/Runtime/Actors/GameBootstrap.TruckState.cs`
  Truck state sync, fleet lookup helpers, and parking-slot helpers.
- `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.cs`
  Hotkeys, camera controls, road placement/removal, and selection input.
- `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.cs`
  Pathfinding, road/lantern rebuilding, movement simulation, and transport helpers.
- `Assets/Scripts/Runtime/Transport/GameBootstrap.RouteRuntime.cs`
  Active trip/refuel runtime state machine.
- `Assets/Scripts/Runtime/Transport/Services/GridPathService.cs`
  Shared BFS/grid path service for trucks, road building, and driver rescue walking.
- `Assets/Scripts/Runtime/Transport/Services/RoadLanternPlanner.cs`
  Pure planner for deciding where road lanterns can spawn.
- `Assets/Scripts/Runtime/UI/GameBootstrap.UI.cs`
  Primary HUD rendering.
- `Assets/Scripts/Runtime/UI/GameBootstrap.Selection.cs`
  Building selection flow and selected-location visual state.
- `Assets/Scripts/Runtime/UI/GameBootstrap.Fleet.cs`
  Fleet HUD rendering and truck detail presentation.
- `Assets/Scripts/Runtime/UI/GameBootstrap.Orders.cs`
  Truck auto-mode, order assignment, and trip reward wiring.
- `Assets/Scripts/Runtime/UI/SelectionVisualService.cs`
  Low-level creation/update helpers for selection highlights and world labels.
- `Assets/Scripts/Runtime/Audio/GameBootstrap.Audio.cs`
  Runtime audio update and procedural clip generation.
- `Assets/Scripts/Runtime/Transport/GameBootstrap.Interaction.cs`
  Truck cargo/refuel interaction runtime and completion flow.
- `Assets/Scripts/Runtime/Transport/Services/ServiceSlotCoordinator.cs`
  Shared single-slot service-bay coordinator for loading/unloading/refuel points.
- `Assets/Scripts/Runtime/Transport/Services/TruckAutoPlanner.cs`
  Small pure decision helper for auto-mode behavior.
- `Assets/Scripts/Runtime/Transport/Services/TripRewardCalculator.cs`
  Pure reward calculation helper for route payouts.

## Main Runtime Flow

- `SampleScene` hosts a bootstrap object.
- On startup `GameBootstrap` creates the ground, visible grid, directional light, low-poly locations, and truck.
- During play:
  - player clicks grid cells to place roads
  - forest produces wood automatically
  - truck checks for connected paths
  - truck transfers wood from `Forest -> Warehouse -> Town`

## Large Areas To Treat Carefully

- `Assets/Scripts/Runtime/Core/GameBootstrap.cs`
- `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.cs`
- `Assets/Scenes/SampleScene.unity`

## Important Reality Check

- The prototype is intentionally compact and scene-local.
- Core gameplay still lives in one partial `GameBootstrap` class, but it is now split by runtime concern across several scripts.

# Architecture Notes

Last updated: 2026-04-06

Purpose: describe the real implemented architecture and current hotspots.

## Current Architecture

- The project currently uses a scene-local bootstrap architecture.
- One partial `MonoBehaviour` (`GameBootstrap`) constructs and runs nearly the full prototype:
  - environment setup
  - low-poly placeholder visuals
  - grid and road state
  - input handling
  - pathfinding
  - truck simulation
  - UI and audio

## Practical Consequences

- This is still fast for prototype iteration.
- It still couples presentation and simulation, but the runtime is now split into concern-based partial scripts under `Assets/Scripts/Runtime/`.
- Small changes are easy; larger feature growth will increase risk quickly.

## Current Hotspots

### `Assets/Scripts/Runtime/Core/GameBootstrap.cs`

- Central runtime entrypoint and owner of shared state.

### `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.cs`

- Highest-risk gameplay file after the refactor.
- Owns pathfinding, road connectivity, and truck movement.

### `Assets/Scripts/Runtime/Transport/GameBootstrap.RouteRuntime.cs`

- Owns the active trip/refuel state machine.

### `Assets/Scripts/Runtime/UI/GameBootstrap.Orders.cs`

- Owns truck order assignment and auto-mode wiring.

### `Assets/Scripts/Runtime/Actors/GameBootstrap.TruckState.cs`

- Owns `TruckAgent` <-> active-runtime state synchronization.
- Also owns fleet lookup and parking-slot helpers.

### `Assets/Scripts/Runtime/Transport/Services/GridPathService.cs`

- Shared grid BFS helper.
- Used by truck routing, starter road generation, and driver rescue walking.

### `Assets/Scripts/Runtime/Transport/GameBootstrap.Interaction.cs`

- Owns truck cargo/refuel interaction state and completion flow.

### `Assets/Scripts/Runtime/UI/GameBootstrap.Selection.cs`

- Owns selected-building highlight and label presentation orchestration.

### `Assets/Scripts/Runtime/World/WorldLayoutGenerator.cs`

- Owns random map placement rules.

### `Assets/Scripts/Runtime/World/TerrainHeightGenerator.cs`

- Owns terrain height generation and flat building pads.

### `Assets/Scenes/SampleScene.unity`

- Entry scene because it hosts the bootstrap object.

## Important Refactor Seams

- `WorldSetup`
  camera, light, ground, grid, and low-poly locations
- `RoadGrid`
  occupancy, placement rules, and road visuals
- `TransportSimulation`
  production, task selection, pathfinding, and truck movement

## Cleanup Reality

- The project is now in an intermediate refactor state: one runtime owner, several partial scripts.
- The next healthy seam after this is moving trucks, world generation, and HUD into fully separate classes/services rather than only partial-class slices.

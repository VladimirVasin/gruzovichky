# Architecture Notes

Last updated: 2026-04-06

Purpose: describe the real implemented architecture and current hotspots.

## Current Architecture

- The project currently uses a scene-local bootstrap architecture.
- One `MonoBehaviour` constructs and runs nearly the full prototype:
  - environment setup
  - low-poly placeholder visuals
  - grid and road state
  - input handling
  - pathfinding
  - truck simulation
  - minimal UI

## Practical Consequences

- This is fast for prototype iteration.
- It also tightly couples presentation and simulation in one place.
- Small changes are easy; larger feature growth will increase risk quickly.

## Current Hotspots

### `Assets/Scripts/TransportPrototypeBootstrap.cs`

- Central gameplay script.
- Owns most runtime behavior and scene construction.

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

- The current architecture is appropriate for a first playable prototype.
- If more vehicles, resources, or UI are added, the bootstrap should be split early.

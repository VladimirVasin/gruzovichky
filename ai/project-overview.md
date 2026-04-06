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

- `Assets/Scripts/TransportPrototypeBootstrap.cs`
  Single bootstrap/controller for scene setup, input, pathfinding, truck movement, and UI.

## Main Runtime Flow

- `SampleScene` hosts a bootstrap object.
- On startup the bootstrap creates the ground, visible grid, directional light, low-poly locations, and truck.
- During play:
  - player clicks grid cells to place roads
  - forest produces wood automatically
  - truck checks for connected paths
  - truck transfers wood from `Forest -> Warehouse -> Town`

## Large Areas To Treat Carefully

- `Assets/Scripts/TransportPrototypeBootstrap.cs`
- `Assets/Scenes/SampleScene.unity`

## Important Reality Check

- The prototype is intentionally compact and scene-local.
- Core gameplay currently lives in one script for speed of iteration.

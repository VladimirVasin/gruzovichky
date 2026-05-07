# Project Overview

Last updated: 2026-05-07

## Purpose

Stable high-level map of the current playable prototype. Code remains the source of truth; use this file only to orient before reading `ai/systems-map.md` and the relevant code.

## Project Shape

- Unity 3D prototype for a low-poly logistics/town simulation.
- Runtime is still scene-local and generated from `Assets/Scenes/SampleScene.unity`.
- The main playable slice now combines:
  - buildable roads, service buildings, production buildings, and local bus stops
  - workers with portraits, education, needs, perks, money, owned-item inventory, jobs, shifts, and life routines
  - trucks, local buses, warehouse storage/logistics, and intercity trade
  - taxes, building banks, event feed, regional map, tutorial/onboarding, and racing mode
  - procedural terrain, natural zones, water, ambient life, weather/lighting, low-poly visuals, and runtime audio/options

## Main Project Areas

```text
Assets/
  Editor/Tests/        Unity editor smoke tests
  Resources/           Localization, game-data JSON, and runtime-loaded assets
  Scenes/              SampleScene entry point
  Scripts/Runtime/     Game runtime partials and extracted services
  Settings/            Unity project settings/assets
tools/                 Project sanity and line-count checks
ai/                    Shared AI memory
```

## Runtime Layout

- `Assets/Scripts/Runtime/Core/GameBootstrap.cs`
  Central scene bootstrap and shared runtime state owner.
- `Assets/Scripts/Runtime/Core/GameBootstrap.*.cs`
  Runtime loop, trade/economy, worker needs/perks, ambient life, world visuals, water, and telemetry partials.
- `Assets/Scripts/Runtime/Data/`
  Small JSON-backed content/config loaders for data that is being separated from large C# UI files.
- `Assets/Scripts/Runtime/World/`
  World generation, terrain height, natural zones, build placement, service decorations, misc decorations, and layout validation.
- `Assets/Scripts/Runtime/Transport/`
  Road input/building, road visuals, pathing, trucks, route/refuel runtime, local bus, racing entry hooks, and transport interactions.
- `Assets/Scripts/Runtime/Transport/Services/`
  Testable transport helpers for grid pathing, two-lane road geometry, road placement, markings, bus routing/passengers, truck runtime phases, and rewards.
- `Assets/Scripts/Runtime/Actors/`
  Trucks, buses, worker/driver visuals, worker life cycle, hiring, shifts, and truck-state synchronization.
- `Assets/Scripts/Runtime/Audio/`
  Runtime audio helpers, generated/curated clip catalog, music, ambience, footsteps, and sound-option volume controls.
- `Assets/Scripts/Runtime/UI/`
  HUDs, quick HUDs, main menu, localization, tutorial, fleet/management screens, map, debug service panel, event feed, and money popups.
- `Assets/Scripts/Runtime/UI/FleetCanvas/`
  Main management screens for Build, Workers, Vacancies/Roles/Shifts, Resources, Economy/Trade, Regional Map, and tutorial helpers.
- `Assets/Resources/GameData/`
  Runtime-loaded JSON catalogs such as Patch Notes and Build menu definitions.
- `Assets/Scripts/Runtime/Racing/`
  Separate racing-mode controls, track/world setup, vehicle behavior, HUD, and atmosphere.
- `Assets/Editor/Tests/`
  Smoke tests for world generation, road build, transport/trade, vacancies, tutorial goals, and related service seams.

## Main Runtime Flow

- `SampleScene` hosts `GameBootstrap`.
- Startup creates the generated world, terrain, highway access, lighting, ambience, UI, and initial mode-specific state.
- The main menu has two player-facing starts: Tutorial and New Game.
- Tutorial and New Game share the same build-first empty-town start; Tutorial runs onboarding, while New Game applies the tutorial-skip state immediately and unlocks the tools/vacancies that skipping tutorial would unlock.
- During play:
  - the player builds roads/buildings/stops and manages workers through HUD panels
  - workers resolve shifts and needs through services, production, transit, walking, and fallback activities
  - trucks and buses use grid roads while regional land/river trade uses built routes, Warehouse/Docks, and off-map merchant flows
  - production, warehouse delivery, taxes, service banks, event feed, and regional trade evolve over time
  - racing can be launched from eligible trade-truck flow

## High-Impact Areas

- `Assets/Scripts/Runtime/Core/GameBootstrap.cs`
- `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport*.cs`
- `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.BuildRoad.cs`
- `Assets/Scripts/Runtime/Transport/GameBootstrap.RouteRuntime.cs`
- `Assets/Scripts/Runtime/Actors/GameBootstrap.Drivers*.cs`
- `Assets/Scripts/Runtime/UI/FleetCanvas/`
- `Assets/Scripts/Runtime/UI/GameBootstrap.Tutorial*.cs`
- `Assets/Scripts/Runtime/World/GameBootstrap.World*.cs`
- `Assets/Scenes/SampleScene.unity`

## Reality Check

- The project is beyond the original small transport sandbox, but it is still prototype-shaped.
- One partial `GameBootstrap` remains the runtime owner; many systems are split by concern and supported by small pure services.
- Use `ai/systems-map.md` -> `System Owner Map` as the first navigation pass before broad code searches.

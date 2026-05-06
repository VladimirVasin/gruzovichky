# Architecture Notes

Last updated: 2026-05-06

Purpose: describe the real implemented architecture and current hotspots.

## Current Architecture

- The project currently uses a scene-local bootstrap architecture.
- One partial `MonoBehaviour` (`GameBootstrap`) constructs and runs nearly the full prototype:
  - environment setup
  - low-poly placeholder visuals
  - grid and road state
  - input handling
  - pathfinding
  - truck, bus, worker, and trade simulation
  - worker needs/perks/life-cycle logic
  - tutorial, HUD, main menu, regional map, racing, UI, and audio

## Practical Consequences

- This is still fast for prototype iteration.
- It still couples presentation and simulation, but the runtime is now split into concern-based partial scripts under `Assets/Scripts/Runtime/`.
- Runtime C# files are kept under the 900-line cleanup target by splitting feature clusters into focused partials such as `Runtime/Racing/`, `Runtime/UI/FleetCanvas/`, `GameBootstrap.Types*.cs`, `GameBootstrap.WorldVisuals.*.cs`, `GameBootstrap.MainMenuHud*.cs`, and `GameBootstrap.Drivers.*.cs`.
- Numeric worker skills and temporary worker effects have been removed; `GameBootstrap.WorkerPerks*` owns perk assignment/display, while `GameBootstrap.WorkerEducation.cs` keeps the small Basic/Vocational/Higher gate needed by Labor Exchange staffing.
- Building work schedules now route through `GameBootstrap.RuntimeSchedules.cs`; use that partial before editing staff slot counts, service shifts, higher-education office hours, or transport weekend behavior.
- Runtime audio has a dedicated partial/catalog layer for generated SFX, curated ambience, music, worker footsteps, and Main Menu Sound options.
- Navigation now starts from `ai/systems-map.md` -> `System Owner Map`; owner cards are maintained when paths, ownership, or responsibilities change.
- `tools/check-all.ps1` is the preferred project sanity runner, with `-SkipSmokeTests` for fast checks while Unity is already open.
- `tools/check-line-count.ps1` and CI default to the 900-line limit.
- Small changes are easy; larger feature growth will increase risk quickly.

## Current Hotspots

### `Assets/Scripts/Runtime/Core/GameBootstrap.cs`

- Central runtime entrypoint and owner of shared state.
- Nested runtime data/state types are split into `GameBootstrap.Types.cs`, `GameBootstrap.Types.AmbientWater.cs`, `GameBootstrap.LocationInstances.cs`, `GameBootstrap.RuntimeSchedules.cs`, and `GameBootstrap.RegionalMap.cs`.

### `Assets/Scripts/Runtime/Core/GameBootstrap.RuntimeSchedules.cs`

- Central owner for building slot counts, staff work-hour checks, service shift presets, higher-education office slots, and UI schedule labels.
- Transport shifts intentionally run daily, while production and higher-education office work remain weekday `08:00-18:00`.

### `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.cs`

- Highest-risk gameplay file after the refactor.
- Owns pathfinding, road connectivity, and truck movement.

### `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.BuildRoad.cs`

- Coordinates build-mode road workflow; preview visuals live in `GameBootstrap.Input.BuildRoad.Preview.cs`, and footprint/place-road flow lives in `GameBootstrap.Input.BuildRoad.Placement.cs`.
- Two-way roads now use a click-start/click-finish segment workflow, with `GameBootstrap.Input.RoadSegments.cs` holding segment-specific state helpers.
- Keep two-lane road invariants here when changing road build tools.
- Footprint offset and structural placement checks now delegate to `RoadBuildPlacementService`; the build-road partial family still owns preview visuals, actual `AddRoad()` calls, turn-fill logging, and input state.

### `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.BuildCursorAssist.cs`

- Owns night-only build cursor lighting/glow for building and road previews.
- Keep this visual assist separate from placement rules; blocked/valid state should be driven by existing build validation results.

### `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.Road*.cs`

- Regular road logic is split into focused partials:
  - `GameBootstrap.Transport.Roads.cs`: core path/road occupancy helpers and add/remove entrypoints.
  - `GameBootstrap.Transport.RoadGeneration.cs`: starter road generation and validation logging.
  - `GameBootstrap.Transport.RoadVisuals.cs`: unified road mesh/cap visual generation.
  - `GameBootstrap.Transport.RoadMarkings.cs`: primitive road markings and corner dashes.
  - `GameBootstrap.Transport.RoadsideProps.cs`: lanterns, edge-highway lanterns, and roadside benches.

### `Assets/Scripts/Runtime/Racing/`

- Racing mode is split by concern: runtime loop, controls, track generation, vehicle state, HUD, world setup, and atmosphere.
- `GameBootstrap.Racing.cs` remains the race-mode coordinator and shared state owner.

### `Assets/Scripts/Runtime/UI/FleetCanvas/`

- Large management screens are split by panel: Workers, Shifts setup/runtime, Build, Resources, Economy, World Map, and tutorial helpers, with large runtime/catalog/assignment blocks moved into focused companion partials.
- Shared UI refs and helper types remain in `GameBootstrap.FleetCanvas.ManagementScreens.cs`.

### `Assets/Scripts/Runtime/UI/GameBootstrap.MainMenuHud*.cs`

- Main Menu HUD is now split into focused partials for the core menu, loading/world-build progress, graphics options, and sound options.
- Patch Notes content still lives behind the JSON-backed catalog path first, with the old hardcoded fallback kept in the core main-menu partial.

### `Assets/Scripts/Runtime/Audio/GameBootstrap.Audio*.cs`

- Runtime audio is split across generated/curated clip loading, music playback, ambience, worker footsteps, and sound-option volume application.
- `GameBootstrap.AudioCatalog.cs` loads the kept generated SFX plus curated nature/footstep assets from `Resources`; stale or removed generated SFX should not be reintroduced without updating the Sound options catalog.

### `Assets/Scripts/Runtime/UI/GameBootstrap.BuildingDemolition.cs`

- Owns selectable-building demolition, confirmation modal behavior, repeated-service-instance targeting, and cleanup for building-owned runtime objects.
- Core/non-removable buildings should stay protected here when expanding demolition behavior.

### `Assets/Scripts/Runtime/UI/GameBootstrap.EventFeed.cs`

- Owns compact top-right event toasts and important player-facing warnings.
- Keep noisy diagnostics in `SessionDebugLogger`; event feed entries should remain short and actionable.

### `Assets/Scripts/Runtime/Core/GameBootstrap.WorldVisuals*.cs`

- World visual setup is split into focused partials for material/texture helpers, ground/diorama meshes, water cells/effects/fish, atmosphere/weather/post-processing, and graphics-option application.
- Road meshes and road-side visuals still live under the transport road partials.

### `Assets/Scripts/Runtime/Core/GameBootstrap.AmbientLife*.cs`

- Ambient life is split by family: clouds/air/birds in the core partial, cats in `GameBootstrap.AmbientLife.Cats.cs`, squirrels in `GameBootstrap.AmbientLife.Squirrels.cs`, bees in `GameBootstrap.AmbientLife.Bees.cs`, frogs in `GameBootstrap.AmbientLife.Frogs.cs`, moths/leaves in `GameBootstrap.AmbientLife.MothsLeaves.cs`, and particle activity schedules in `GameBootstrap.AmbientLife.ParticleSchedules.cs`.
- `GameBootstrap.AmbientLife.Particles.cs` keeps shared particle setup plus smaller firefly/exhaust clusters.

### `Assets/Scripts/Runtime/Actors/GameBootstrap.Drivers.*.cs`

- Driver/worker behavior is split into general movement/logistics, warehouse delivery, life cycle and needs, idle wander, and hiring/shift orchestration.

### `Assets/Scripts/Runtime/Transport/GameBootstrap.RouteRuntime.cs`

- Owns the active trip/refuel state machine.
- Runtime guards are delegated to `TruckRuntimeGuardService`.
- Trip and refuel phase-step decisions are delegated to `TruckTripRuntimeService` and `TruckRefuelRuntimeService`; the partial still owns Unity-side effects such as movement commands, interactions, audio, salary/rest handoff, and feed/debug output.

### `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.Infrastructure.cs`

- Owns Parking-provided truck and bus slot capacity plus automatic vehicle provisioning helpers.
- Vacancies, Labor Exchange, tutorial freight setup, and bus-shift assignment should ask this partial for available/provisionable vehicles instead of creating or buying trucks/buses directly.

### `Assets/Scripts/Runtime/Core/GameBootstrap.RegionalMap.cs`

- Owns generated regional state for the current town, visible external cities, route type/availability, resource buy/sell tables, and built-route state.
- UI lives in the FleetCanvas world-map partials; land/river trade runtime consumes the generated route data.

### `Assets/Scripts/Runtime/Core/GameBootstrap.RegionalTrade.Runtime.cs`

- Owns external merchant-truck land-route visits to Warehouse and bridges active Trade policies with generated built regional routes.
- River trading is handled through Docks runtime and uses the same active-policy/built-route requirement.

### `Assets/Scripts/Runtime/Core/GameBootstrap.Trade.Resources.cs`

- Runtime bridge between private `LocationData` storage and extracted trade resource helpers.
- Warehouse stock and Docks export/import stock mutations should go through this partial before touching `GameBootstrap.Trade.cs`, `GameBootstrap.Docks.Runtime.cs`, or `GameBootstrap.RegionalTrade.Runtime.cs`.

### `Assets/Scripts/Runtime/Transport/Services/TruckRuntimeGuardService.cs`

- Pure guard seam for deciding whether assigned-trip and refuel update loops should run.
- Keeps conflict-state checks testable while the actual truck phase transitions still live in `GameBootstrap.RouteRuntime.cs`.

### `Assets/Scripts/Runtime/Transport/Services/TruckTripRuntimeService.cs`

- Pure phase-step service for regular assigned truck trips.
- Decides whether to move to pickup/dropoff/parking, start load/unload, wait, or complete a trip.

### `Assets/Scripts/Runtime/Transport/Services/TruckRefuelRuntimeService.cs`

- Pure phase-step service for truck refuel orders.
- Decides whether to move to Gas Station/Parking, start refueling, wait, or complete the refuel order.

### `Assets/Scripts/Runtime/Core/Services/TradeAutoDispatchService.cs`

- Pure timing/gating seam for trade auto-dispatch retry intervals and weekend blocking.
- `GameBootstrap.Trade.cs` still owns concrete order lookup and dispatch execution.

### `Assets/Scripts/Runtime/Core/Services/TradeSimulation.cs`

- First trade tick coordinator seam.
- `GameBootstrap.RuntimeLoop.cs` now calls `UpdateTradeSimulation()`, which keeps Unity-side active trade runs, Docks ship runtime, and regional merchant trucks in `GameBootstrap` while delegating auto-dispatch timer decisions through this service.

### `Assets/Scripts/Runtime/Core/Services/TradeDispatchPreconditionService.cs`

- Pure precondition helper for trade dispatch blocking reasons.
- `GameBootstrap.Trade.cs` gathers live driver/truck/world/resource facts, then delegates the final dispatch/no-dispatch decision to this service.

### `Assets/Scripts/Runtime/Core/Services/TradeOrderQueueService.cs`

- Pure helper for active trade-order creation, queue peek, first-order completion, and cancel-by-id removal.
- Economy and World Map still own UI and dispatch side effects, but queue lifecycle rules are now covered by editor smoke tests.

### `Assets/Scripts/Runtime/Core/Services/TradeResourceLedger.cs`

- Shared stock snapshot and mutation helper for Warehouse and Docks resource amounts.
- Keeps resource `Get/Add/TryConsume` arithmetic testable while `GameBootstrap.Trade.Resources.cs` owns applying results back to private runtime locations.

### `Assets/Scripts/Runtime/Core/Services/DocksTradePolicyRuntime.cs`

- Pure river-trade policy seam for Docks ship buy/sell quantities and skip reasons.
- Docks runtime still owns ship movement, money ledger entries, feed events, and concrete Docks/Warehouse resource mutation.

### `Assets/Scripts/Runtime/Core/Services/TradeScreenModel.cs`

- View-model seam for Trade policy rows.
- `GameBootstrap.FleetCanvas.TradeScreen.cs` builds row models from runtime facts, then only applies text/button states to Unity UI controls.

### `Assets/Scripts/Runtime/Core/Services/TradeRunRuntimeService.cs`

- Pure trade-run phase helper for driver-to-parking, truck target arrival/movement, highway departure, out-of-map timing, and buy/sell return routing.
- `GameBootstrap.Trade.cs` still owns concrete path building, object visibility, cargo mutation, money ledger, event feed, and driver rest handoff.

### `Assets/Scripts/Runtime/UI/GameBootstrap.Orders.cs`

- Owns truck order assignment and auto-mode wiring.

### `Assets/Scripts/Runtime/Actors/GameBootstrap.TruckState.cs`

- Owns `TruckAgent` <-> active-runtime state synchronization.
- Also owns fleet lookup and parking-slot helpers.

### `Assets/Scripts/Runtime/Transport/Services/GridPathService.cs`

- Shared grid BFS helper.
- Used by truck routing, starter road generation, and driver rescue walking.

### `Assets/Scripts/Runtime/Transport/Services/TwoLaneRoadGeometry.cs`

- Pure helper for two-lane road direction normalization, right-lane offsets, footprints, and turn fill bounds.
- Covered by editor smoke tests because road geometry regressions quickly break transport.

### `Assets/Scripts/Runtime/Transport/Services/RoadBuildPlacementService.cs`

- Pure build-mode road placement helper for resolving the second lane offset, blocking buildings/highway/misc cells, and detecting third parallel lane attempts.
- Used by `GameBootstrap.Input.BuildRoad.cs` and covered by editor smoke tests.

### `Assets/Scripts/Runtime/Transport/Services/RoadSegmentBuildService.cs`

- Pure helper for Cities-like road segment shape decisions.
- Keeps click-start/click-finish road segment generation testable while runtime input owns cursor state, previews, and actual road mutation.

### `Assets/Scripts/Runtime/Transport/Services/BusStopOrderingService.cs`

- Pure helper for deterministic local bus-stop ordering by stop number and anchor coordinates.
- Runtime still owns `LocationData`, but ordering decisions are now testable outside `GameBootstrap`.

### `Assets/Scripts/Runtime/Transport/Services/RoadMarkingPlanner.cs`

- Pure helper for regular two-lane road visual-axis and center-dash decisions.
- `GameBootstrap.Transport.Roads.cs` still owns actual road mesh/primitive creation, but lane-marking rules now have a testable seam.

### `Assets/Scripts/Runtime/Transport/Services/LocalBusRoutePlanner.cs`

- First extracted local-bus route-state helper.
- Owns pure stop-index/direction decisions for the local route bus out-and-back cycle while `GameBootstrap.LocalBus.cs` still owns runtime objects, passengers, and movement.

### `Assets/Scripts/Runtime/Transport/GameBootstrap.LocalBus.RouteSkipping.cs`

- Runtime helper partial for reachable-stop search and skipped-stop tracking when local bus networks are partially disconnected.
- Keeps bus routes from collapsing entirely when one stop is unreachable from Parking or from the current route segment.

### `Assets/Scripts/Runtime/Transport/Services/LocalBusRuntimeService.cs`

- Pure-ish local bus runtime seam for dwell countdown and waypoint movement stepping.
- Passenger ownership, bus-driver shift lifecycle, and object wiring still remain in `GameBootstrap.LocalBus.cs`.

### `Assets/Scripts/Runtime/Transport/Services/LocalBusPassengerService.cs`

- Pure helper for local-bus passenger boarding decisions.
- Encodes capacity, fare exemption, paid fare, and fallback-to-walking rules while `GameBootstrap.LocalBus.cs` keeps concrete worker/object mutations.

### `Assets/Scripts/Runtime/World/WorldLayoutRoadValidator.cs`

- Pure helper for validating whether required generated-world road-access pairs can support a wide two-lane road.
- `GameBootstrap.Transport.Roads.cs` delegates its layout road feasibility check here, while runtime road creation still owns actual cell mutation/visual rebuilding.
- Also exposes a smoke-test path-appending helper so tests can build a generated starter road network and verify actual connectivity across required destinations.
- Editor smoke tests use this service to validate generated world-layout route chains across deterministic seeds.

### `Assets/Scripts/Runtime/World/BuildingPlacementService.cs`

- Pure helper for rotated building footprints, footprint blockage checks, preview footprint cells, and preview scale/center calculations.
- `GameBootstrap.World.cs` still owns concrete building creation and CityPark-specific implementation, but generic placement/preview math is now testable outside the partial.

### `Assets/Scripts/Runtime/World/MiscDecorationSpawnService.cs`

- Pure helper for choosing misc decoration buckets (`FlowerPatch`, `BerryBush`, `Tree`) from spawn chances.
- Keeps part of `GameBootstrap.World.cs` world-decoration behavior testable while concrete low-poly prop construction remains in the world partial.

### `Assets/Scripts/Runtime/World/GameBootstrap.World.MiscDecorations.cs`

- Partial world-decoration split for misc trees, berry bushes, flower patches, tree sway, perch points, and tree primitive variants.
- Keeps `GameBootstrap.World.cs` focused more on placement/service/city-park construction while preserving existing runtime behavior.

### `Assets/Scripts/Runtime/World/ServiceDecorationStyleService.cs`

- Shared style helper for service-building light identity (`Bar`, `Canteen`, `GamblingHall`).
- Concrete primitive constructors still live in `GameBootstrap.World.cs`, but color/intensity/range choices are now centralized and smoke-tested.

### `Assets/Scripts/Runtime/World/GameBootstrap.BuildingModel*.cs`

- Shared low-poly building-model helper layer and enhancement pass.
- `GameBootstrap.BuildingModelHelpers.cs` wraps primitive box/cylinder/sphere creation, cardinal anchor-facing roots, window rows, crate stacks, and collider disabling for decorative pieces.
- `GameBootstrap.BuildingModelEnhancements.cs` adds per-building detail overlays for all buildable/location types while existing placement, runtime location state, and base decoration methods stay in the transport/world decoration partials.

### `Assets/Scripts/Runtime/UI/FleetCanvas/FleetCanvasUiFactory.cs`

- First extracted UI factory service for FleetCanvas primitive UI creation.
- `GameBootstrap.FleetCanvas.cs` still exposes compatibility wrappers, but low-level object/text/button/section-card/tab-row/layout-panel/badge/scroll-panel/scroll-list/scrollbar/spacer creation now has a real service seam.

### `Assets/Scripts/Runtime/Data/`

- First JSON-backed content/config loader layer.
- `PatchNotesCatalog.cs` loads `Assets/Resources/GameData/patch-notes.json` for Main Menu Patch Notes, with the old C# content path kept as fallback.
- `BuildCatalog.cs` loads `Assets/Resources/GameData/build-catalog.json` for Build menu categories, titles, colors, and descriptions, with existing build placement/validation logic still in C#.
- `LocalizedContentData.cs` is the shared EN/RU text wrapper for these content catalogs.

### `Assets/Scripts/Runtime/UI/FleetCanvas/VacancyFlowRulesService.cs`

- Small decision seam for vacancy/tutorial assignment flow invariants.
- Used by the vacancy HUD step logic and covered by focused smoke tests so future UI reshaping does not silently break assignment progression.

### `Assets/Scripts/Runtime/UI/Localization/LocalizedStringTable.cs`

- First table-style localization seam.
- Existing Russian strings still live in `GameBootstrap.Localization.cs`, but lookup/reverse lookup/common-fragment translation now goes through a reusable table object.
- `Assets/Resources/Localization/ui.ru.json` exists as the first external localization-table seed; the built-in dictionary remains the authoritative fallback until migration is completed.

### `Assets/Scripts/Runtime/UI/Localization/LocalizationJsonLoader.cs`

- Loads simple flat JSON localization tables from `Resources`.
- `GameBootstrap.Localization.cs` merges the external Russian JSON table over the built-in dictionary so migration can happen incrementally without breaking fallback coverage.

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
  occupancy, placement rules, road segment building, and road visuals
- `TransportSimulation`
  production, task selection, pathfinding, truck movement, local bus routing, and intercity trade
- `WorkerSimulation`
  needs, shifts, service visits, fallback activities, and life-cycle decisions
- `ManagementUI`
  FleetCanvas screens, vacancies, tutorial goals, localization, regional map, event feed, and main-menu options

## Cleanup Reality

- The project is now in an intermediate refactor state: one runtime owner, several partial scripts.
- The next healthy seam after this is moving trucks, world generation, and HUD into fully separate classes/services rather than only partial-class slices.
- `tools/check-all.ps1`, `tools/check-line-count.ps1`, and `.github/workflows/project-sanity.yml` guard builds, line count, whitespace, mojibake, and smoke-test coverage.
- `Assets/Editor/Tests/WorldGenerationSmokeTests.cs` covers world layout placement validity, two-lane geometry, and generated road-access chain feasibility for historical starter-road layouts.
- `Assets/Editor/Tests/RoadBuildSmokeTests.cs`, `TransportTradeSmokeTests.cs`, and `VacancyTutorialSmokeTests.cs` cover newer road segment, transport/trade, vacancy, and tutorial-goal seams.

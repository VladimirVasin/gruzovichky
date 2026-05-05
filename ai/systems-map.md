# Systems Map

Last updated: 2026-05-05

Purpose: map the current active systems and note which areas are high impact.

## System Owner Map

Use this before broad code search. Owner cards are navigation starting points, not exclusive ownership. If code and this map disagree, trust code and update this map after the change. When changing files listed here, update this section if paths, ownership, or responsibilities shift.

- Core bootstrap state/types: `Assets/Scripts/Runtime/Core/GameBootstrap.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.Types.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.Types.AmbientWater.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.LocationInstances.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.RegionalMap.cs`.
- Tutorial / onboarding: `Assets/Scripts/Runtime/UI/GameBootstrap.Tutorial.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Tutorial.Window.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Tutorial.CameraFocus.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Tutorial.PanelActions.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Tutorial.Hiring.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.TutorialGoalsHud.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.Tutorial.Cinematics.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.Tutorial.FleetAndOutlines.cs`.
- Road build / build preview: `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.BuildRoad.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.BuildRoad.Preview.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.BuildRoad.Placement.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.BuildRoad.Debug.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.BuildCursorAssist.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.RoadSegments.cs`, `Assets/Scripts/Runtime/Transport/Services/RoadBuildPlacementService.cs`, `Assets/Scripts/Runtime/Transport/Services/RoadSegmentBuildService.cs`, `Assets/Scripts/Runtime/Transport/Services/TwoLaneRoadGeometry.cs`.
- Road runtime / visuals / props: `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.Roads.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.RoadGeneration.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.RoadVisuals.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.RoadMarkings.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.RoadsideProps.cs`, `Assets/Scripts/Runtime/Transport/Services/RoadMarkingPlanner.cs`, `Assets/Scripts/Runtime/Transport/Services/RoadLanternPlanner.cs`.
- Vacancies / assignments: `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.ShiftsSetup.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.ShiftsSetup.DriverRows.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.VacanciesRuntime.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.VacanciesRuntime.Assignment.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.ShiftsRuntime.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.ShiftsRuntime.Logistics.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/VacancyFlowRulesService.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.FleetCanvas.ManagementScreens.cs`.
- Workers / needs / perks / education / life simulation: `Assets/Scripts/Runtime/Actors/GameBootstrap.Drivers.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.Drivers.HiringAndShifts.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.WorkerMigration.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.WorkerContracts.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.WorkerProfession.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.Drivers.LifeCycle.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.Drivers.LifeCycle.Cars.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.Drivers.LifeCycle.Housing.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.Drivers.LifeCycle.Meals.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.Drivers.LifeCycle.Services.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.Drivers.LifeCycle.Idle.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.LaborExchange.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.Actors.Cars.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.Actors.PersonalCars.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.WorkerEducation.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.WorkerNeeds.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.WorkerPerks.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.WorkerPerks.Tooltips.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.DriverQuickHud.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.QuickHud.WorkerFocus.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.WorkersScreen.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.WorkersScreen.Runtime.cs`.
- Trucks / freight / trade runtime: `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.Runtime.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.DriverWalk.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.Infrastructure.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.RouteRuntime.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.Trade.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.Trade.Runtime.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.Trade.Policies.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.Docks.Runtime.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.RegionalTrade.Runtime.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.TruckState.cs`, `Assets/Scripts/Runtime/Transport/Services/TruckAutoPlanner.cs`, `Assets/Scripts/Runtime/Transport/Services/TruckTripRuntimeService.cs`, `Assets/Scripts/Runtime/Transport/Services/TruckRefuelRuntimeService.cs`, `Assets/Scripts/Runtime/Core/Services/TradeAutoDispatchService.cs`, `Assets/Scripts/Runtime/Core/Services/TradeDispatchPreconditionService.cs`, `Assets/Scripts/Runtime/Core/Services/TradeOrderQueueService.cs`, `Assets/Scripts/Runtime/Core/Services/TradeRunRuntimeService.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.TruckQuickHud.cs`.
- Local bus / transit: `Assets/Scripts/Runtime/Transport/GameBootstrap.LocalBus.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.BusPurchaseArrival.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.Infrastructure.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.BusState.cs`, `Assets/Scripts/Runtime/Transport/Services/LocalBusRuntimeService.cs`, `Assets/Scripts/Runtime/Transport/Services/LocalBusRoutePlanner.cs`, `Assets/Scripts/Runtime/Transport/Services/LocalBusPassengerService.cs`, `Assets/Scripts/Runtime/Transport/Services/BusStopOrderingService.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.LocalBusQuickHud.cs`.
- Economy / taxes / trade HUD: `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.EconomyScreen.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.TradeScreen.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.Trade.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.Trade.Runtime.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.Trade.Policies.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.EventFeed.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.MoneyPopups.cs`.
- Build menu / building placement: `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.BuildScreen.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.BuildScreen.Catalog.cs`, `Assets/Scripts/Runtime/Data/BuildCatalog.cs`, `Assets/Resources/GameData/build-catalog.json`, `Assets/Scripts/Runtime/World/GameBootstrap.World.BuildPlacement.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.World.LaborExchange.cs`, `Assets/Scripts/Runtime/World/BuildingPlacementService.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.BuildRoad.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.BuildRoad.Placement.cs`.
- World generation / terrain / natural zones: `Assets/Scripts/Runtime/World/GameBootstrap.World.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.World.NaturalZones.cs`, `Assets/Scripts/Runtime/World/WorldLayoutGenerator.cs`, `Assets/Scripts/Runtime/World/WorldLayoutRoadValidator.cs`, `Assets/Scripts/Runtime/World/GeneratedWorldLayout.cs`, `Assets/Scripts/Runtime/World/TerrainHeightGenerator.cs`, `Assets/Scripts/Runtime/World/NaturalZoneData.cs`.
- Buildings / decorations / service visuals: `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.Locations.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.Locations.Decorations.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.BuildingModelHelpers.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.BuildingModelEnhancements.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.BuildingModelEnhancements.LaborExchange.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.World.MiscDecorations.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.World.ServiceDecorations.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.World.LaborExchange.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.World.Docks.cs`, `Assets/Scripts/Runtime/World/ServiceDecorationStyleService.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.BuildingQuickHud.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.BuildingQuickHud.Workers.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.BuildingQuickHud.Status.cs`.
- Regional map: `Assets/Scripts/Runtime/Core/GameBootstrap.RegionalMap.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.WorldMapScreen.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.WorldMapScreen.Preview.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas.WorldMapScreen.PixelMap.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.FleetCanvas.UtilityScreens.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.FleetCanvas.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.FleetCanvas.DriverAssignments.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.FleetCanvas.UiHelpers.cs`.
- Ambient life / particles / weather / water: `Assets/Scripts/Runtime/Core/GameBootstrap.AmbientLife.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.AmbientLife.Cats.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.AmbientLife.Particles.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.AmbientLife.Bees.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.AmbientLife.Frogs.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.AmbientLife.MothsLeaves.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.AmbientLife.ParticleSchedules.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.AmbientLife.Squirrels.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.EdgeHighway.RiverBoats.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.WorldVisuals.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.WorldVisuals.Atmosphere.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.WorldVisuals.GraphicsOptions.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.WorldVisuals.Ground.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.WorldVisuals.Water.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.Water.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.WeatherHud.cs`.
- Racing mode: `Assets/Scripts/Runtime/GameBootstrap.Racing.cs`, `Assets/Scripts/Runtime/Racing/GameBootstrap.Racing.Runtime.cs`, `Assets/Scripts/Runtime/Racing/GameBootstrap.Racing.Controls.cs`, `Assets/Scripts/Runtime/Racing/GameBootstrap.Racing.Hud.cs`, `Assets/Scripts/Runtime/Racing/GameBootstrap.Racing.Track.cs`, `Assets/Scripts/Runtime/Racing/GameBootstrap.Racing.Vehicle.cs`, `Assets/Scripts/Runtime/Racing/GameBootstrap.Racing.World.cs`, `Assets/Scripts/Runtime/Racing/GameBootstrap.Racing.Atmosphere.cs`.
- Main menu / options / patch notes / localization: `Assets/Scripts/Runtime/UI/GameBootstrap.MainMenuHud.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.MainMenuHud.Loading.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.MainMenuHud.GraphicsOptions.cs`, `Assets/Scripts/Runtime/Data/PatchNotesCatalog.cs`, `Assets/Scripts/Runtime/Data/LocalizedContentData.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Localization.cs`, `Assets/Scripts/Runtime/UI/Localization/LocalizationJsonLoader.cs`, `Assets/Scripts/Runtime/UI/Localization/LocalizedStringTable.cs`, `Assets/Resources/GameData/patch-notes.json`, `Assets/Resources/Localization/ui.ru.json`, `ai/release-notes.md`.
- Debug / telemetry / project checks: `Assets/Scripts/Runtime/UI/GameBootstrap.DebugServicePanel.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.DebugServicePanel.AutoAssign.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.DebugTelemetry.cs`, `Assets/Scripts/Runtime/Diagnostics/SessionDebugLogger.cs`, `tools/check-all.ps1`, `tools/check-line-count.ps1`.
- Smoke tests / editor safety: `Assets/Editor/Tests/WorldGenerationSmokeTests.cs`, `Assets/Editor/Tests/RoadBuildSmokeTests.cs`, `Assets/Editor/Tests/TransportTradeSmokeTests.cs`, `Assets/Editor/Tests/VacancyTutorialSmokeTests.cs`, `Assets/Editor/PlayModeSceneGuard.cs`, `.github/workflows/project-sanity.yml`.

## Active Systems

### Scene bootstrap and world setup

- Files: `Assets/Scripts/Runtime/Core/GameBootstrap.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.Types*.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.World.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.World.MiscDecorations.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.World.ServiceDecorations.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.BuildingModelHelpers.cs`, `Assets/Scripts/Runtime/World/GameBootstrap.BuildingModelEnhancements.cs`, `Assets/Scripts/Runtime/World/BuildingPlacementService.cs`, `Assets/Scripts/Runtime/World/MiscDecorationSpawnService.cs`, `Assets/Scripts/Runtime/World/WorldLayoutGenerator.cs`, `Assets/Scripts/Runtime/World/WorldLayoutRoadValidator.cs`, `Assets/Scenes/SampleScene.unity`
- Includes:
  - camera framing
  - directional light setup
  - ground plane
  - visible grid lines
  - location spawning
  - misc tree/bush/flower primitive decoration visuals
  - shared low-poly building-model helper/enhancement pass
  - generic rotated building placement and preview math
  - generated layout road-access validation
  - testable misc decoration spawn bucket selection

### Road placement and grid occupancy

- Files: `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Input.BuildRoad*.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.Road*.cs`, `Assets/Scripts/Runtime/Transport/Services/GridPathService.cs`, `Assets/Scripts/Runtime/Transport/Services/RoadBuildPlacementService.cs`, `Assets/Scripts/Runtime/Transport/Services/RoadLanternPlanner.cs`, `Assets/Scripts/Runtime/Transport/Services/RoadMarkingPlanner.cs`, `Assets/Scripts/Runtime/Transport/Services/TwoLaneRoadGeometry.cs`
- Includes:
  - click-to-place roads
  - grid snapping
  - occupation blocking on location cells
  - simple road tile stretching toward neighbors
  - two-lane road footprints and right-hand lane geometry
  - build-mode road footprint placement validation
  - testable two-lane road marking/axis decisions

### Truck movement and resource flow

- Files: `Assets/Scripts/Runtime/Actors/GameBootstrap.Actors*.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.Drivers*.cs`, `Assets/Scripts/Runtime/Actors/GameBootstrap.TruckState.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.Runtime.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.DriverWalk.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.Transport.Infrastructure.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.RouteRuntime.cs`, `Assets/Scripts/Runtime/Transport/GameBootstrap.LocalBus.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Orders.cs`, `Assets/Scripts/Runtime/Transport/Services/GridPathService.cs`, `Assets/Scripts/Runtime/Transport/Services/BusStopOrderingService.cs`, `Assets/Scripts/Runtime/Transport/Services/LocalBusRoutePlanner.cs`, `Assets/Scripts/Runtime/Transport/Services/LocalBusRuntimeService.cs`, `Assets/Scripts/Runtime/Transport/Services/LocalBusPassengerService.cs`, `Assets/Scripts/Runtime/Transport/Services/TruckAutoPlanner.cs`, `Assets/Scripts/Runtime/Transport/Services/TruckRuntimeGuardService.cs`, `Assets/Scripts/Runtime/Transport/Services/TruckTripRuntimeService.cs`, `Assets/Scripts/Runtime/Transport/Services/TruckRefuelRuntimeService.cs`, `Assets/Scripts/Runtime/Transport/Services/TripRewardCalculator.cs`, `Assets/Scripts/Runtime/Core/GameBootstrap.Trade*.cs`, `Assets/Scripts/Runtime/Core/Services/TradeAutoDispatchService.cs`, `Assets/Scripts/Runtime/Core/Services/TradeDispatchPreconditionService.cs`, `Assets/Scripts/Runtime/Core/Services/TradeOrderQueueService.cs`, `Assets/Scripts/Runtime/Core/Services/TradeRunRuntimeService.cs`
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
  - Parking-provided truck and bus slot capacity plus automatic vehicle provisioning

### Minimal runtime UI

- Files: `Assets/Scripts/Runtime/UI/GameBootstrap.UI.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.UI.Hud.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Selection.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.Fleet.cs`, `Assets/Scripts/Runtime/UI/GameBootstrap.FleetCanvas*.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/GameBootstrap.FleetCanvas*.cs`, `Assets/Scripts/Runtime/UI/SelectionVisualService.cs`, `Assets/Scripts/Runtime/UI/FleetCanvas/FleetCanvasUiFactory.cs`, `Assets/Scripts/Runtime/Data/BuildCatalog.cs`, `Assets/Scripts/Runtime/Data/PatchNotesCatalog.cs`, `Assets/Scripts/Runtime/Data/LocalizedContentData.cs`, `Assets/Scripts/Runtime/UI/Localization/LocalizedStringTable.cs`, `Assets/Scripts/Runtime/UI/Localization/LocalizationJsonLoader.cs`, `Assets/Resources/GameData/build-catalog.json`, `Assets/Resources/GameData/patch-notes.json`, `Assets/Resources/Localization/ui.ru.json`
- Includes:
  - fleet list and truck details
  - building info cards
  - money / time / speed panels
  - route assignment and command feedback
  - Stats reference screen for worker perks, professionalism, needs, and statuses
  - shared FleetCanvas primitive/section-card/tab-row/layout-panel/badge/scroll-list factory seam
  - table-style localization lookup with JSON override seed for MainMenu/PatchNotes/Economy/Trade/Workers/Roles labels
  - JSON-backed Patch Notes and Build menu catalog data

## Impact Hints

- Change to road placement or path validity:
  read `GameBootstrap.Input` and `GameBootstrap.Transport` together.
- Change to truck behavior:
  read `GameBootstrap.Actors`, `GameBootstrap.Transport`, and `GameBootstrap.Fleet` together.
- Change to scene readability:
  read `GameBootstrap`, `GameBootstrap.World`, `GameBootstrap.Selection`, and `GameBootstrap.UI` together.

## Current Cleanup Reality

- The vertical slice still runs through one `GameBootstrap` class.
- Runtime C# files are kept under the 900-line cleanup limit by splitting feature clusters into focused partial scripts under `Assets/Scripts/Runtime/`.

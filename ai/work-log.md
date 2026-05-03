# Work Log

Last updated: 2026-05-04

Purpose: compact active memory for recent work. Older detailed history was intentionally collapsed on 2026-04-20 and again on 2026-05-03 to keep agent startup light. Use git history for exact old implementation details.

## Recent Work

- 2026-05-04: Tightened road-build diagnostics and junction handling after reviewing `debug.log`. Road segment logs now distinguish `requestedEnd` from `resolvedEnd` and report axis-lock state, turn-fill no-op messages moved to verbose logging, and player road junction turn-fill/preview now only uses perpendicular directions that are actually connected to existing road cells. Added smoke coverage for connected perpendicular junction direction selection. Verified `./tools/check-all.ps1 -SkipSmokeTests`.

- 2026-05-03: Completed the broad 900-line runtime partial cleanup. Split the remaining oversized `GameBootstrap` partials into focused files for core nested types, world/service decorations, transport location decorations, build-road preview/placement, driver idle/walk/warehouse delivery, trade runtime, ambient bees/frogs/moths/leaves/river boats, racing runtime, HUD/status helpers, FleetCanvas map/build/workers/shifts/vacancies helpers, tutorial window UI, and debug auto-assign flow. Updated owner map and architecture notes for the new paths, and set project line-count tooling/CI to the 900-line default. Verified `./tools/check-all.ps1 -SkipSmokeTests`, `git diff --check`, and an explicit scan confirming no `Assets/Scripts` C# files exceed 900 lines.

- 2026-05-03: Continued the gradual 900-line partial-file cleanup. Split ambient cats into `GameBootstrap.AmbientLife.Cats.cs`, moved squirrel setup/runtime into `GameBootstrap.AmbientLife.Squirrels.cs`, and moved bee/moth active-hour helpers into `GameBootstrap.AmbientLife.ParticleSchedules.cs`; the original `GameBootstrap.AmbientLife.cs` is now below 900 lines. Verified `dotnet build Assembly-CSharp.csproj -v:minimal` and `./tools/check-all.ps1 -SkipSmokeTests`.

- 2026-05-03: Continued the gradual 900-line partial-file cleanup. Split `GameBootstrap.WorldVisuals.cs` into `GameBootstrap.WorldVisuals.Ground.cs`, `GameBootstrap.WorldVisuals.Water.cs`, and `GameBootstrap.WorldVisuals.Atmosphere.cs`; the original partial now keeps material/texture and visual tint helpers. All `WorldVisuals` partials are now below 900 lines. Verified `dotnet build Assembly-CSharp.csproj -v:minimal` and `./tools/check-all.ps1 -SkipSmokeTests`.

- 2026-05-03: Started the gradual 900-line partial-file cleanup. Split `GameBootstrap.MainMenuHud.cs` into `GameBootstrap.MainMenuHud.Loading.cs` and `GameBootstrap.MainMenuHud.GraphicsOptions.cs`; the original Main Menu HUD partial is now under 900 lines while loading/world-build flow and graphics options behavior remain in the same `GameBootstrap` partial class. Verified runtime/editor builds, line-count, diff whitespace, and mojibake scan with `./tools/check-all.ps1 -SkipSmokeTests`.

- 2026-05-03: Expanded `FleetCanvasUiFactory` to reduce hand-built UI layout code. Added shared vertical stack, horizontal layout panel, simple vertical scroll-list, and badge helpers; Build, Resources, and Fleet list setup now use these helpers for card rows, category headers, status badges, and scroll content. Verified `./tools/check-all.ps1 -SkipSmokeTests`.

- 2026-05-03: Started separating data/text from large UI C# files. Added `Assets/Scripts/Runtime/Data/` JSON loaders plus `Assets/Resources/GameData/patch-notes.json` and `build-catalog.json`; Main Menu Patch Notes and Build menu categories/titles/descriptions now load these catalogs first, while old C# content remains as fallback. Verified `dotnet build Assembly-CSharp.csproj -v:minimal`, `dotnet build Assembly-CSharp-Editor.csproj -v:minimal`, JSON parsing, line-count, `git diff --check`, and `./tools/check-all.ps1 -SkipSmokeTests`.

- 2026-05-03: Compacted and refreshed AI memory after reviewing `ai/`. Updated `project-overview.md` from the old one-truck/wood-loop slice to the current town-sim prototype, filled `release-notes.md` with a concise `v.0.0.3` player-facing baseline, refreshed `architecture-notes.md` for the road-segment/build-cursor/test reality, and reduced this work log back to active memory plus summaries.

- 2026-05-03: Added focused smoke-test coverage for road build, transport/trade, vacancies, and tutorial goals. New editor tests cover road segment shape/blocked placement/path blockers, local bus route/passenger/trade queue decisions, and vacancy/tutorial flow invariants. Added `VacancyFlowRulesService` as a small testable seam used by vacancy HUD step logic. Fixed `tools/check-all.ps1` mojibake scanning for untracked UTF-8 files so new C# test files are not falsely flagged. Verified `./tools/check-all.ps1 -SkipSmokeTests`; full Unity smoke-test mode was blocked by an already-open Unity project instance.

- 2026-05-03: Tightened owner-map maintenance rules. Agents must now update `ai/systems-map.md` when work in listed owner paths changes file paths, ownership, or responsibilities, with `AGENTS.md`, `ai/README.md`, and the owner-map intro all carrying the rule.

- 2026-05-03: Added a System Owner Map to `ai/systems-map.md` so agents can quickly find likely owner files for Tutorial, Road Build, Vacancies, Workers, Trucks/Trade, Local Bus, Economy, World, Regional Map, Ambient, Racing, Localization, Debug, and tests. Added explicit owner-map usage rules to `AGENTS.md` and `ai/README.md`. Verified `./tools/check-all.ps1 -SkipSmokeTests`.

- 2026-05-03: Restored `tools/check-all.ps1` as the one-command project sanity runner. It runs runtime/editor `dotnet build`, line-count, staged/unstaged `git diff --check`, added-line/untracked-file mojibake scanning, and Unity EditMode smoke tests when Unity is available; use `-SkipSmokeTests` for fast local checks while the project is open in Unity. Added the usage rule to `ai/README.md`. Verified `./tools/check-all.ps1 -SkipSmokeTests`; full smoke-test mode correctly fails when another Unity instance has the project open.

- 2026-05-03: Added night build-mode cursor assistance. Build previews now spawn a warm point light plus a soft ground glow around the current building/road footprint only when the scene is dark, using red tint for blocked placement; the helper lives in `GameBootstrap.Input.BuildCursorAssist.cs` so `GameBootstrap.Input.BuildRoad.cs` stays under the line-count limit. Verified runtime/editor `dotnet build`, line-count check, and `git diff --check`.

- 2026-05-03: Made building driveway/access cells become real road cells automatically when a location is created. `CreateLocation()` now ensures each `RoadAccess` cell is added through the normal road visual/connectivity refresh path while still refusing water, beach, highway, or footprint-overlap cells; misc props on the driveway are removed first. Verified runtime/editor `dotnet build`, line-count check, and `git diff --check`.

- 2026-05-02: Adjusted the Regional Map interaction model so opening Map shows only the large map by default. No region is selected on open, the right-side region/trade panel stays hidden until the player clicks a city marker, and regional trade route controls now live inside that right panel instead of as a separate bottom console. Verified `dotnet build Assembly-CSharp.csproj -nologo`, line-count check, and `git diff --check`.

- 2026-05-02: Reworked the Regional Map toward a Pharaoh-like fullscreen parchment map. The map now uses a generated parchment/sea/river/forest/mountain texture, compact settlement-style city markers, warmer selected/current region styling, route lines from the current town, and the Map HUD opens fullscreen while pausing simulation until closed. Verified `dotnet build Assembly-CSharp.csproj -nologo`, line-count check, and `git diff --check`.

- 2026-05-02: Improved the Regional Map HUD from text-heavy placeholders into schematic known-region previews. Known neighbors (`River Port`, `Cotton & Textile Belt`, `Dry South`) now render distinct mini-map layouts in both the 3x3 grid and detail panel, dynamic map labels/descriptions use Russian display strings when selected, unknown regions show a localized survey placeholder, and duplicate regional trade UI logging with special symbols was removed. Verified `dotnet build Assembly-CSharp.csproj -nologo`, line-count check, `git diff --check`, and touched-file mojibake scan.

- 2026-05-02: Tuned worker needs balance after multi-day `debug.log` review. New workers now spawn with random `$50-$100` personal balance, active need-resolution fallbacks are no longer interruptible as low-priority idle activities, critical needs clear daily helper flags, and Warehouse service delivery scoring prefers filling service buildings toward full capacity when stock space exists. Verified `dotnet build Assembly-CSharp.csproj -nologo`, line-count check, and `git diff --check`.

- 2026-05-02: Generalized the no-money worker fallback debuff. The former trash-meal-only effect is now a shared `money_fallback` effect shown as `I Have Fallen` / `Ya opustilsya`; workers receive it when eating from trash cans or when they cannot afford Motel sleep and use a bench sleep fallback. Motel arrival now also retries bench fallback if the worker loses enough money before check-in. Verified `dotnet build Assembly-CSharp.csproj -nologo`, line-count check, `git diff --check`, and touched-file mojibake scan.

- 2026-05-02: Started the road-builder shift toward a Cities-like segment workflow. Two-way roads now use a click-start/click-finish segment flow instead of immediate single-cell placement, with Shift constraining the segment to one dominant axis; the existing `roadCells` backend remains intact for pathfinding. Added `RoadSegmentBuildService` plus `GameBootstrap.Input.RoadSegments.cs`, and updated the Build menu description. Verified runtime/editor `dotnet build` and line-count check.

- 2026-05-01: Improved service-needs balance and service-resource logistics. Warehouse/local service buffers were increased, warehouse loaders carry multiple units by Logistics skill, delivery target scoring now considers demand/urgency/fill level, retry cooldowns distinguish money blocks from empty stock, F9 can adjust Warehouse Fuel/Alcohol/Food, and hourly `NEEDS_ECON` telemetry includes stock snapshots.

- 2026-05-01: Added a Canteen money fallback for worker meals. Workers who need food but cannot afford Canteen service can path to a registered building trash can, perform a short trash-meal idle activity, satisfy the meal need, and receive the temporary `I Have Fallen` effect. Canteen arrival rechecks money before charging.

- 2026-05-01: Improved worker life-simulation need handling. Due needs are selected by urgency, failed paid-service attempts no longer satisfy daily needs, critical needs can interrupt low-priority idle work, and free/off-shift workers may resolve due needs before 18:00 when not blocked by active work or transport.

- 2026-05-01: Fixed local-bus and worker/vacancy edge cases. Local bus routes require at least two local stops, one-stop networks show warning guidance, passenger planning treats one-stop service as unavailable, worker walking no longer falls back across water, Bus Driver vacancy assignment no longer requires an already-parked bus, and Warehouse loader vacancies are grouped into one vacancy with three internal shifts.

- 2026-05-01: Added first smooth-road terrain pass and follow-up fixes. Logical road roots remain grid-based, while visible road surfaces/markings sample road height so smoothed strips and per-cell fallback roads stay above terrain and markings no longer cut through slopes.

- 2026-05-01: Expanded debug telemetry while adding verbose gating. Worker/lumber/road/economy logs now capture useful aggregate state, while noisy per-waypoint/per-cell/lane/HUD/bus-spawn traces are verbose-only through `GRUZOVICHKY_DEBUG_VERBOSE` or category-specific env vars.

- 2026-05-01: Added F9 debug worker-wave tool. The debug panel can summon ten free workers through the existing intercity arrival-bus flow, and arrival buses release passengers with a short stagger instead of dumping large waves at once.

- 2026-04-30: Folded trade dispatch into the regular Truck Driver shift pool. Economy/Trade now uses an available on-shift Truck Driver and auto-reserves a parked truck from Parking instead of requiring a separate Intercity vacancy; tutorial and F9 auto-assign paths were updated for the new model.

- 2026-04-30: Expanded Main Menu graphics settings into a live post-processing control window with `0..100` controls, explicit toggles, reset-to-defaults, fresh `v2` prefs, and localized labels. Defaults now sit near the middle of the range instead of maxing all effects.

- 2026-04-30: Added `Assets/Editor/PlayModeSceneGuard.cs` to prevent silent empty Play Mode starts when Unity leaves the active scene on `Temp/__Backupscenes/...`; it repairs backup scenes and adds `Tools/Lo-Fi Delivery/Open Main Scene`.

- 2026-04-30: Bumped the manual main-menu version label to `Lo-fi Delivery Co. v.0.0.3` and added the in-development `v.0.0.3` tracking section to `ai/release-notes.md`.

## Recent Summary

- 2026-04-29: Removed superseded legacy tutorial mode and completed the rebuilt User tutorial chain through workers, production, freight, service buildings, local bus, economy/taxes, trade, Join-the-Race, and final Demo Complete. Tutorial camera focus, panel actions, goals HUD, copy, and gating were repeatedly tuned and split into focused partials.

- 2026-04-29: Reworked Gas Station/User refuel safety and tutorial service-building objectives. Gas Station became buildable in the User tutorial service step, and refuel orders now guard against missing Gas Station/Parking instead of throwing.

- 2026-04-28 to 2026-04-29: Stabilized road-building UX and two-lane behavior. Preview drift, shift-drag continuation offsets, turn-fill preview cells, truck lane targeting, and build-road debug logging were corrected; one-lane road remains available as its own tool.

- 2026-04-27 to 2026-04-28: Reopened `New Game User` as a lean build-first start. User mode now starts without the old central starter-road/building setup, expects the player to build core logistics through tutorial goals, and uses a retuned city-builder camera/zoom curve for the larger map.

- 2026-04-26 to 2026-04-27: Expanded the world to `128x128`, added natural zones, tuned generated roads/building access for two-lane roads, increased vegetation density, and added far-zoom visual LOD while preserving gameplay simulation and important night lights.

- 2026-04-26: Added first-pass `Car Market` and personal worker cars. Workers with enough money can buy cars, worker/car info appears in HUDs, and Car Market has a procedural lot/office/display-car visual.

- 2026-04-24 to 2026-04-26: Performed graphics/atmosphere passes: lighting/post-processing, richer surfaces, water/shore refinement, material smoothness presets, service/building night identity, warmer practical lights, landmark readability, falling leaves, and general building trash/perimeter glow.

- 2026-04-24 to 2026-04-26: Hardened economy/trade/workers systems. Added daily building tax MVP, Economy Taxes/Trade tabs, improved Event Feed placement, F9 auto-assign, worker telemetry, service-resource deliveries, City Park leisure, Lumberjack Camp build entry, and free-time need handling.

- 2026-04-25 to 2026-04-26: Extracted many pure/testable seams from `GameBootstrap`: road geometry/build placement/markings, truck trip/refuel/trade run phase helpers, trade queue/preconditions/auto-dispatch, local bus routing/runtime/passengers, warehouse bus-delivery decisions, building placement, world layout validation, service-decoration style, FleetCanvas UI factory, localization table/json loader, and misc decoration spawn buckets.

- 2026-04-23 to 2026-04-25: Local bus and vacancy systems were rebuilt into clearer shift/vacancy flows, grouped bus-driver shifts, passenger counts, route-end behavior, richer debug logs, and step-by-step assignment UI. Several mojibake/player-facing localization regressions were cleaned.

- 2026-04-22: Forest production became a Lumberyard MVP with real harvestable trees, worker chopping, falling/felled trees, log carry-back, sapling replanting, growth stages, and shift-end recovery.

- 2026-04-16 to 2026-04-21: User-mode tutorial grew into a multi-step onboarding chain; buildable services/production buildings, build previews, local stops, production assignments, Workers/Roles/Resources/Fleet HUDs, localization, regional map, intercity trade, ambient buses, racing mode, water/ambient life, and debug quick HUDs were added or iterated.

- 2026-04-06 to 2026-04-15: Project began as a runtime-generated grid transport prototype in `SampleScene`, then expanded into partial `GameBootstrap` systems with Fleet Canvas, drivers/workers, shifts, generated world layout, broader economy/resources, edge highway, bus stop, ambience, and racing minigame.

## Active Notes / Watchouts

- Keep `ai/work-log.md` short. If it grows beyond roughly 120-160 lines, collapse older completed items into this summary format again.

- Code remains source of truth. The project has many partial `GameBootstrap.*.cs` files; memory is only a navigation aid.

- Use `ai/systems-map.md` -> `System Owner Map` as the first navigation pass for implementation, bugfix, refactor, and investigation tasks.

- When changing files listed in an owner card, update `ai/systems-map.md` only if paths, ownership, or responsibilities changed.

- Prefer `./tools/check-all.ps1` before commits or after risky code edits. Use `-SkipSmokeTests` for a fast local pass when Unity is already open or unavailable.

- For memory-only edits, a text/diff check is enough; no Unity build is needed unless code changed.

- Avoid unsafe shell rewrites for localized files. Prefer `apply_patch`, keep text UTF-8, and scan touched localized UI/HUD/tutorial files for mojibake markers after edits.

- Avoid broad Canvas localization passes immediately after dynamic HUD redraws unless necessary; several HUDs intentionally use direct localized strings to avoid mixed-language output.

- Tutorial flow is sensitive to timing, overlays, highlights, goals, and camera state. When changing tutorial steps, verify OK click behavior, skip behavior, menu highlight cleanup, and whether regular HUDs should be hidden or preserved.

- Quick HUDs should not open automatically after building placement and should close when larger HUD windows open. Fleet details are an exception: preserve Fleet's internal selected-truck state while Fleet is open.

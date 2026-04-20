# Work Log

Last updated: 2026-04-20

Purpose: compact active memory for recent work. Older detailed history was intentionally collapsed on 2026-04-20 to keep agent startup light. Use git history for exact old implementation details.

## Recent Detailed Work

- 2026-04-20: Updated Racing controls HUD and steering input. Racing steering is now mouse-wheel-drag only; keyboard `A/D` and left/right arrow steering fallback were removed while `W`/up, `S`/down, and `ESC` remain active. Added a readable top-right Russian control legend covering mouse steering, throttle, brake/reverse, and exit. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors; one pre-existing legacy `DriverCardUi.DriverId` warning remains.

- 2026-04-20: Removed the old worker/driver Energy attribute after the needs-system rewrite. DriverAgent no longer stores Energy/SleepStartEnergy/rest-queued state, runtime no longer drains/restores energy during shifts or motel sleep, Fleet/Workers/Truck/Driver quick HUDs no longer display energy rows, and Stamina tooltip copy now refers to tiredness/needs instead of energy. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors; one pre-existing legacy `DriverCardUi.DriverId` warning remains.

- 2026-04-20: Tightened Patch Notes scroll content spacing. Removed premature fixed preferred heights from paragraph rows, added per-text `ContentSizeFitter`, reduced content spacing, and kept compact section header minimum heights so the changelog no longer creates huge gaps between sections. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors; one pre-existing legacy `DriverCardUi.DriverId` warning remains.

- 2026-04-20: Added `ai/release-notes.md` as stable release memory for in-game Patch Notes. It documents the player-facing contents of `Lo-fi Delivery Co. v.0.0.1` and the workflow for comparing that baseline against current code/work-log when preparing future patch notes. `ai/README.md` now points agents to this file for version/changelog tasks.

- 2026-04-20: Improved the main-menu Patch Notes window. The changelog body is now a scrollable `ScrollRect`, section headers are rendered as separate accent-colored bold rows, and the content/title localizes between English and Russian using UTF-safe escaped strings. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors; one pre-existing legacy `DriverCardUi.DriverId` warning remains.

- 2026-04-20: Added a main-menu `Patch Notes` button and modal changelog window. The window currently documents `Lo-fi Delivery Co. v.0.0.1` with version-grouped sections for the core loop, workers, production/resources, logistics/trade, and world ambience, creating an in-game place for future patch notes. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors; one pre-existing legacy `DriverCardUi.DriverId` warning remains.

- 2026-04-20: Added a manual main-menu version label. `GameBootstrap.MainMenuHud.cs` now has `MainMenuVersionLabel = "Lo-fi Delivery Co. v.0.0.1"` and renders it in the bottom-right corner of the main menu. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors; one pre-existing legacy `DriverCardUi.DriverId` warning remains.

- 2026-04-20: Expanded worker needs diagnostics in `debug.log`. Needs threshold changes, satisfied needs, lifecycle day-rollover queue/reset, skipped services, service entry, sleep completion, and unemployed work-skip now include timer/status snapshots like `Meal=7.2h/Ok, Sleep=18.0h/Warning, Leisure=3.1h/Ok` plus service failure reasons where relevant. `GameBootstrap.WorkerNeeds.cs` was rewritten with UTF-safe Russian UI strings. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors; one pre-existing legacy `DriverCardUi.DriverId` warning remains.

- 2026-04-20: Redesigned Resources HUD with two tabs. "Warehouse" tab (На складе) shows the 8 existing resource rows sourced from Warehouse only. "Production" tab (Производство) shows per-building sections (Forest → Logs, Sawmill → Logs+Boards, FurnitureFactory → Boards+Textile+Furniture, GasStation → Fuel); sections for unbuilt buildings show a "(not built)" label. Window widened from 400→480px. All labels set directly via IsRussianLanguage(). Verified 0 errors; one pre-existing legacy warning remains.

- 2026-04-20: Added MVP worker needs timers. Workers now track game-hours since Meal, Sleep, and Leisure; Canteen/Bar/fallback leisure/Motel completion resets the relevant timer, need status thresholds are logged once on change, and Workers HUD shows a compact Needs block with localized status and hours. The existing life-chain now uses need timers for seeking service goals and only resets its daily helper flags at a safe idle moment after day rollover. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-20: Temporarily disabled the main-menu User mode entry. `New Game User` now renders as a non-interactive `Work in progress` button with disabled visuals while Debug mode remains available. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors; one pre-existing legacy `DriverCardUi.DriverId` warning remains.

- 2026-04-20: Added a first pass worker daily life-cycle. After a completed work block, workers now proceed through goal-based Eat/Canteen, Leisure/Bar or idle fallback, Sleep/Motel, then Idle; unemployed workers skip Work and start the same evening chain. Canteen/Bar/Motel goals are skipped for the day when the needed building/resource/money is unavailable, Bar/Canteen are no longer picked by random idle wandering, and lifestyle durations now use game-hour based timing (1h canteen, 2h leisure, 1-3h idle fallback, 8h sleep). Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors; one pre-existing legacy `DriverCardUi.DriverId` warning remains.

- 2026-04-20: Added hover explanations for Workers HUD skills. Driving, Stamina, Production, and Logistics now render as accent-colored interactive rows in the selected worker card, and hovering each row opens a compact tooltip describing what that skill affects in English/Russian. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors; one pre-existing legacy `DriverCardUi.DriverId` warning remains.

- 2026-04-20: Added MVP worker base stats: Driving, Stamina, Production, and Logistics. Stats are generated deterministically on worker creation, stored on `DriverAgent`, and displayed beside the portrait in the selected worker's Workers HUD detail card; they are informational only for now and do not affect gameplay yet. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors; one pre-existing legacy `DriverCardUi.DriverId` warning remains.

- 2026-04-20: Added procedural low-poly worker portraits to the Workers HUD detail panel. Each `DriverAgent` now gets deterministic portrait traits on creation, and the selected worker card renders a UI-primitive portrait directly under the worker name in the right-side detail view. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors; one pre-existing legacy `DriverCardUi.DriverId` warning remains.

- 2026-04-20: Fixed tutorial/HUD polish issues from the latest playtest. Tutorial step 9 now clears Forest/building and debug-cell selections before showing, does not open quick HUDs, and continues simulation while blocking player input behind the tutorial window. Tutorial step 15 no longer highlights or auto-picks the first driver; the player must choose any free driver, and any valid truck assignment completes the tutorial. Shifts HUD now labels the roster as Workers, shows each worker's profession on the right side of the row, and marks truck-assigned workers as Logistics with Truck Driver profession. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-20: Fixed two tutorial/assignment regressions. Tutorial step 9 (`ForestWorkerStarted`) no longer pauses the simulation while its HUD is open; other tutorial windows keep the existing pause behavior. Production assignment from Shifts > Productions now auto-unassigns a worker from their truck roster before switching them to building work, and blocks the production assignment if the truck is actively using that driver and cannot safely release them. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-20: Rewrote all active tutorial HUD copy to be concise and gameplay-informative in both English and Russian. Updated the 17-step User tutorial flow, both OrbitHUD messages, removed the hidden Forest text override, and cleaned old literary tutorial strings out of localization so Russian mode uses the new instructional copy. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-20: Compressed this work log. Kept recent onboarding/Fleet/Trade/quick-HUD notes in useful detail and collapsed older multi-session history into thematic summaries. Stable memory files were not changed.

- 2026-04-19: Fixed Fleet tutorial step 13 Buy Truck panel hiding. The Buy Truck panel is now hidden every visible Fleet update during `FleetSelectTruck`, even if the list is not rebuilt that frame. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-19: Added typewriter reveal to every regular tutorial HUD. `ShowTutorialWindow` stores localized body text separately and `UpdateTutorialUi` reveals it at the same default speed as orbit tutorial HUDs. Bee/flower easter egg popups use the same path. Verified build clean.

- 2026-04-19: Added a day-only bee/flower easter egg in both Debug and User modes. Clicking a flower patch cell during Day opens a tutorial-style popup saying `Дурачок, не мешай пчёлкам`; it takes priority over debug cell inspection. Verified build clean.

- 2026-04-19: Extended the User tutorial after Fleet driver assignment. Workers HUD dynamic labels are localized, Fleet step 13 hides the Buy Truck panel, assigning a worker to Truck 1 closes Fleet and queues step 16, step 16 sends the player to assign someone to Sawmill, and assigning a Sawmill worker queues step 17 placeholder. Current tutorial total: 17. Verified build clean.

- 2026-04-19: Fixed tutorial step 15 side-card placement so the Fleet driver-picker card remains visible left of the Fleet HUD instead of drifting off-screen. Verified build clean.

- 2026-04-19: Fixed OrbitHUD profession labels. Step 12 explicitly renders `(for now) unemployed` / `(пока ещё) безработный`; generic fallback no longer calls unassigned local workers drivers unless they have truck assignment. Verified build clean.

- 2026-04-19: Fixed tutorial step 8 Forest assignment regression. The forced tutorial commute now moves time into production hours and clears stale rest/walk state so the assigned worker reaches Forest and starts working instead of immediately going to Motel/sleep. Verified build clean.

- 2026-04-19: Polished orbit/typewriter tutorial identity and gating. Forest worker-start step 9 waits until step 8 is dismissed, OrbitHUD speaker text is highlighted separately from typewritten body, and descriptor/profession localization was added for assignable jobs/services/truck/intercity/default workers. Verified build clean.

- 2026-04-19: Added OrbitHUD detach behavior. When the attached character reaches their destination or leaves the walk phase, the HUD stops orbiting, smoothly returns to the screen center, keeps typing, and triggers smooth camera return to the default diorama view. Verified build clean.

- 2026-04-19: Hardened standalone Build And Run `Join the race` clicks by adding a direct mouse-rect fallback after showing the Canvas button. Verified build clean.

- 2026-04-19: Rewrote User tutorial step 12 as first-person worker thought text with matching Russian localization. Verified build clean.

- 2026-04-19: Fixed compile break in `GameBootstrap.FleetCanvas.UtilityScreens.cs` caused by smart quotes in C# code and an accessibility mismatch on `UnlockBuildTool(BuildTool tool)`. Verified build clean.

- 2026-04-17: Fixed truck microHUD layout for long trade/status text. Truck quick HUD is wider/taller, uses one-column resource/route rows, avoids broad post-update localization, and uses compact active trade phrases. Verified build clean.

- 2026-04-17: Cleaned worker quick HUD readability and trade cargo display. Worker quick HUD uses wider one-column rows and direct localization. Truck cargo formatting is shared as `amount/capacity (Resource)` across quick HUD, Fleet list/details, and legacy HUD. Buy trade runs load purchased cargo before the truck reappears at the return edge. Verified build clean.

- 2026-04-17: Fixed a Fleet regression from quick-HUD cleanup. Fleet internal selected-truck state is preserved while Fleet is open; only compact quick-HUD canvases are hidden. Verified build clean.

- 2026-04-17: Added automatic quick-HUD cleanup when larger HUD windows or build mode are open. Truck, Driver, Building, and Cell quick HUD selections/canvases plus debug-cell highlights are cleared before large HUD updates. Verified build clean.

- 2026-04-17: Rebuilt Trade HUD into an explicit order-creation panel. Trade now has Resource dropdown, Action dropdown, amount stepper, PLACE ORDER, Active Orders list, BUY/SELL tags, and remove buttons. Resources HUD Buy/Sell threshold controls and hidden threshold auto-dispatch were removed/disabled. Verified build clean.

- 2026-04-17: Fixed Fleet driver assignment filtering after Intercity assignment. Local and Intercity drivers/workers can be truck candidates if otherwise free; production-assigned, already truck-assigned, arriving, active trade-run, resting, and busy walking workers are excluded. Verified build clean.

- 2026-04-17: Simplified Warehouse quick HUD by removing inline stock counts; detailed inventory remains in Resources HUD. Verified build clean.

- 2026-04-17: Fixed Shifts HUD tab/layout presentation. Logistics and Productions now enforce fixed panel dimensions; Productions active tab text receives accent styling. Diagnostics log `SHIFTS_HUD` state for future UI debugging. Verified build clean.

## Older Summary

- 2026-04-16 to 2026-04-17: User-mode tutorial system grew from a welcome popup into a multi-step onboarding chain covering Motel construction, Workers panel, hiring, Forest production assignment, Sawmill construction, worker arrival cinematic, Fleet selection, truck driver assignment, and follow-up Sawmill assignment. It includes localized Russian/English text, side-card tutorial windows, red UI highlights, OrbitHUD/world-space typewriter panels, camera focus/zoom, skip tutorial, and cleanup of tutorial overlays/highlights.

- 2026-04-16: Added separate New Game Debug/User modes. Debug keeps fuller starter generation; User starts leaner and expects Motel/Sawmill to be built manually. Restarting either mode from the Escape menu reloads the scene and starts the selected mode.

- 2026-04-16: Added buildable service/production buildings and build-preview improvements. Canteen was added as a service building with fee/inside counters, Motel/Sawmill became buildable in User mode, Furniture Factory produces Furniture from Boards + Textile, and build mode now previews full footprints, driveway cells, rotation with R, and post-placement road connection flow.

- 2026-04-16: Production assignments were split from logistics shifts. Logistics still uses driver shifts/intercity role; Productions assigns workers directly to production buildings with fixed 08:00-18:00 working hours.

- 2026-04-16: Localization system was introduced with Eng/Rus main-menu selector and runtime label helpers. There was a past mojibake incident in `GameBootstrap.Localization.cs`; avoid non-UTF-safe file writes and prefer `apply_patch`.

- 2026-04-12: Regional Map was added and iterated into a clickable 3x3 region HUD. The current region has a schematic preview with river/highway/town/forest cues; neighboring regions are placeholders with known resource roles like Textile/Cotton/route.

- 2026-04-11: Intercity trade highway routing and ambient bus lane behavior were heavily debugged. Current intent: trade trucks use explicit edge-highway lane paths, disappear/reappear at the highway edge, and bus spawning is suppressed only on relevant active trade lanes to reduce overlap. Debug logs include `TRADE_PHASE`, `TRADE_PATH`, and `BUS_SPAWN`.

- 2026-04-10 to 2026-04-11: Edge-highway lane mapping, bus direction rows, highway lantern suppression near local-road junctions, and trade-truck lane usage were repeatedly corrected. Treat current code as source of truth, not old notes.

- 2026-04-09 to 2026-04-14: World polish added/iterated edge highway, bus stop, ambient buses, water/river visuals, beach/water height, fish, bees, birds, cats, benches, flowers, roadside/forest particles, dust, night moth swarms, and audio toggles. Some ambient sounds were fully disabled by request; truck loop audio should only run when a truck is active/occupied.

- 2026-04-08 to 2026-04-09: Major runtime/gameplay split from the early prototype into partial `GameBootstrap` systems. Added Fleet Canvas, driver extraction from truck, early rest handling, shifts, manual routes, building quick HUDs, debug cell HUD, generated world layout, and broader resource/economy UI.

- 2026-04-06: Project began as a runtime-generated transport prototype in `SampleScene`: grid, road placement, Parking/Forest/Warehouse/Town, truck pathing, cargo service, basic resource loop, fuel/gas station, diorama presentation, procedural audio, and AI memory files.

- 2026-04-15: Intercity racing minigame was added and polished. Trade trucks can expose Join the Race, launch a separate road scene with truck controls, dashboard objects, finish sequence, music/SFX, skybox, collisions, road extension, and payout bonus.

## Active Notes / Watchouts

- Keep `ai/work-log.md` short. If it grows beyond roughly 120-160 lines, collapse older completed items into this summary format again.

- Code remains source of truth. The project has many partial `GameBootstrap.*.cs` files; memory is only a navigation aid.

- Prefer `dotnet build Assembly-CSharp.csproj -nologo` after code edits when practical. For memory-only edits, a text/diff check is enough.

- Avoid broad Canvas localization passes immediately after dynamic HUD redraws unless necessary; prior fixes moved several HUDs toward direct localized strings to avoid mixed-language/mojibake-looking output.

- Tutorial flow is sensitive to timing, overlays, highlights, and camera state. When changing tutorial steps, verify OK click behavior, skip behavior, menu highlight cleanup, and whether regular HUDs should be hidden or preserved.

- Quick HUDs should not open automatically after building placement and should close when larger HUD windows open. Fleet details are an exception: preserve Fleet's internal selected-truck state while Fleet is open.

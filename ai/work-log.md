# Work Log

Last updated: 2026-04-23

Purpose: compact active memory for recent work. Older detailed history was intentionally collapsed on 2026-04-20 to keep agent startup light. Use git history for exact old implementation details.

## Recent Detailed Work

- 2026-04-23: Implemented the first working local-bus-driver gameplay loop. A worker assigned to the `Bus Driver / Водитель автобуса` slot now keeps their normal logistics shift, walks to `Parking` before the shift, boards a dedicated local route bus, and drives between buildable local stops in stop-number order (`1..N`, then back down) using real road pathfinding between stop anchors. The bus waits 5 in-game minutes at each stop, returns to `Parking` at shift end, and the worker disembarks into the normal after-work life cycle. The route bus reuses the same low-poly bus model/headlights style as the highway decorative buses. Added a dedicated `GameBootstrap.LocalBus.cs` runtime partial plus UI/status integration and included the new file in `Assembly-CSharp.csproj`. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-23: Fixed a Russian HUD mojibake regression in shift time ranges. `GetShiftRangeLabel()` in `GameBootstrap.cs` had a broken literal separator (`вЂ“`) inside the returned string, which rendered as garbage in `Assignments / Logistics` headers like `Утро 06:00 вЂ“ 14:00`. Replaced it with a UTF-safe escaped en dash (`\u2013`) so the range now renders correctly regardless of file encoding. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Added a placeholder `Bus Driver` / `Водитель автобуса` role to `Assignments -> Logistics`. This is intentionally only a reservation slot for upcoming local bus-route gameplay: one worker can now be assigned/removed as the future city bus driver, the profession appears correctly in Workers/Assignments/quick HUDs, and truck roster assignment now excludes that reserved worker until they are reassigned. Normal logistics/production/intercity reassignments clear the placeholder bus-driver reservation safely. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Tightened local bus-stop numbering so stop numbers now always live in the compact range `1..N`, where `N` is the current number of buildable local stops. Added normalization for legacy/skewed numbers and changed the quick-HUD `- / +` logic to swap positions within that bounded range instead of letting arbitrary values like `4` survive when only one stop exists. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Fixed the buildable local `Bus Stop` system so stops are no longer blocked after the first placement. Local stops were moved off the singleton `locations[LocationType.Stop]` path into their own `localStops` list, while the highway-generated `Intercity Stop` remains a separate special singleton. World placement, terrain application, occupancy checks, selection highlights/labels, Build placement rules, and building quick HUD stop-number editing were updated to work with multiple local stops safely. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Fixed the stop-number microHUD controls after the first pass rendered `- / +` almost non-clickable. The stop-number buttons now get explicit `LayoutElement` width/height and the center label also has a stable height, so the row has a real hit area instead of collapsing to text-sized controls. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Added a first-pass buildable `Stop` / `Остановка` MVP using the same visual language as the existing highway `Bus Stop`. Build mode now includes a `Stop` infrastructure card with rotated 2x1 placement, the new stop is treated as a service building, and both `Bus Stop` and `Stop` now carry an auto-assigned stop number in `LocationData`. Building quick HUD gained a compact stop-number row with `- / +` controls so the number can be edited after placement. Current architecture still keeps stops singleton-per-type (`Bus Stop` and one buildable `Stop`) rather than a true multi-instance stop network. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Exposed worker gender in the Workers HUD detail card. The right-side profile block now appends localized gender (`Male/Female`, `Мужчина/Женщина`) to the occupation line, so the existing `DriverAgent.Gender` field is visible in normal gameplay UI without expanding the left worker list. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Updated the `v.0.0.2` patch notes wording to include a separate all-caps release bullet `ДОБАВЛЕНЫ ЖЕНЩИНЫ!!!` in the new-version section. Mirrored the same line into `ai/release-notes.md` so future patch-note updates keep the documented `0.0.2` release text consistent.

- 2026-04-22: Fixed a Lumberyard field-work regression where a Forest worker teleported straight back inside the building immediately after planting a sapling. Planting now transitions into a dedicated `LumberReturnToBuilding` walk phase, so the worker visibly walks back to the Lumberyard entrance before being hidden inside again. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Bumped the documented game version from `Lo-fi Delivery Co. v.0.0.1` to `Lo-fi Delivery Co. v.0.0.2`. Updated the main-menu version label, rewrote the in-game Patch Notes to add a new `v.0.0.2` section describing the delta since `v.0.0.1`, and updated `ai/release-notes.md` so future patch-note work has a stable `0.0.2` baseline. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Extended the `F9` debug resource controls beyond service buildings. The same panel now has a `Production / storage` block for `Forest`, `Sawmill`, `Warehouse`, and `Furniture Factory`, with `-1 / +1` controls for `Logs`, `Boards`, `Textile`, and `Furniture`. Runtime-cap storage uses existing building limits where available; loose debug-only storage rows use a safe debug cap of `99`. All adjustments are logged to `debug.log`. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Added a distinct dense tree/grass zone to the generated town. Ground visuals now force a separate forest hotspot with a broken organic edge, and misc-tree planning can prioritize cells inside that hotspot, so one part of the map reads as a noticeably denser wooded patch instead of grass and trees being spread with the same uniform noise everywhere. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Increased the initial workforce from 1 to 3 workers on world start. Startup now uses a shared `InitialWorkerCount` constant and spawns three unassigned idle workers during the normal Motel-based bootstrap instead of only one. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Rebalanced ambient misc world dressing for the larger town map. `MiscTreePlanner` no longer uses the old tiny hard cap; it now scales misc-prop target count with map area and candidate density, so larger generated maps receive noticeably more trees/bushes/flower patches. All world `misc` trees were also doubled in overall scale during spawn, making the decorative tree line read much larger against the expanded map. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Repeated the `F9` debug service-panel sizing fix after the previous pass still felt cramped in use. The panel itself is now larger (`380x820`), the worker list got a bit more vertical room, and the service-resource scroll area was expanded again so the `Canteen` row and its `+ / -` controls are fully visible during gameplay testing. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Followed up the new `F9` debug resource controls by enlarging the debug service window and increasing the service-resource scroll area height, so the full service list (including `Canteen`) is visible without the last control row being clipped. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Expanded the `F9` debug service panel for `Debug` mode with direct service-resource controls. The panel now includes `-1 / +1` test buttons for `Gas Station` fuel, `Bar` alcohol, and `Canteen` food, each clamped to the real in-game storage caps and logged to `debug.log`. This gives a quick gameplay-testing path for service supply states without touching normal player HUD flows. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Fixed the last major `Assignments / Назначения` overflow bug by making the tab work area height derive from the actual free space inside the right column instead of an outdated fixed constant. This keeps the `Intercity` block inside the panel bounds and makes scrolling responsible for overflow instead of letting the content extend below the window. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Rebuilt scrolling in `Assignments / Назначения` so the right-side tab content is finally contained by a real viewport instead of spilling past the window bounds. Both `Logistics` and `Production` now use explicit masked viewports plus visible vertical scrollbars, and the selected-worker summary card was restored to show the worker name/status above the tabs instead of being effectively collapsed by earlier fixes. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Simplified the `Assignments / Назначения` selected-worker summary card to stop text collisions for good. The card now uses only two active lines inside its info block (`name` + one status line), the extra middle line was disabled, and both active texts were switched to `VerticalWrapMode.Truncate` with fixed heights so they cannot overlap even when localized strings change. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Applied a systemic HUD upscale pass to give the main management windows more breathing room. `Fleet`, `Workers`, `Assignments`, `Build`, `Resources`, `Trade`, `States`, and the main-menu Patch Notes window were all enlarged, with matching panel-height updates where those screens relied on fixed content areas. This was done as a coordinated UI-capacity increase rather than a one-off fix so growing Russian text and denser cards have more space across the whole interface. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Simplified `Assignments / Назначения` production cards by removing the extra `Role:` line entirely. The building already communicates the purpose of the assignment, so this row only added visual noise. Production and warehouse cards were shortened accordingly, reducing vertical clutter in the right panel. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Fixed an `Assignments / Назначения` layout regression introduced during the HUD redesign. The selected-worker summary card no longer tries to act as a long explanatory block: its no-selection state was shortened, hint copy was reduced, dynamic colors were corrected, and the internal row heights were rebalanced so wrapped Russian text no longer visually collides. Warehouse assignment rows also stopped trying to render the selected worker name inside a narrow button; the warehouse action button now uses a short explicit label (`Assign` / `Назначить`) with a wider fixed width so the row remains readable. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Continued the `Assignments / Назначения` HUD redesign with a structure pass focused on tab clarity. Both tabs now start with their own short explanatory card so the screen reads as two distinct assignment modes: `Logistics` explains local shifts plus intercity reservation, while `Production` explains direct building assignment on the 08:00-18:00 schedule. The intercity card now has its own summary/context line, and shift summaries were tightened to read like local delivery staffing status rather than abstract slots. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Continued HUD redesign for `Assignments / Назначения` with a second readability pass. The right panel now starts with a selected-worker summary card (`name`, `profession`, `current status`, `context hint`) so the screen reads from the chosen worker outward instead of looking like two unrelated tables. Logistics shift cards gained a short per-shift summary, and Production cards were clarified with explicit role context plus cleaner assignment wording (`Assigned: Name`, clearer warehouse slot labels, localized building titles). Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Reframed the old `Shifts` screen as `Assignments` on the player-facing HUD. The top menu button now opens `Assignments / Назначения` while internal runtime state still uses the existing `Shifts` plumbing for safety. The canvas title was renamed, worker row statuses were clarified so production/logistics/intercity read more consistently, and several assignment/intercity button labels were localized/cleaned up to better match the current gameplay model where this screen is about assigning workers, not only scheduling time slots. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Continued HUD redesign with pass 1 for `Fleet` / `Автопарк`. The Fleet right panel was restructured from one dense truck overview into clearer grouped sections: truck profile, crew, resources, and route. Active driver and crew-slot summary were moved into the top profile card, roster management stayed functional in a dedicated crew section, and lower cards now read more like operational summaries instead of debug leftovers. Runtime text setup was also normalized for Russian/English labels in the refreshed layout. To avoid breaking the tutorial chain, compatibility refs for `InfoCardLayout` and `AssignDriverPickerLayout` were preserved even though the visual composition changed. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-22: Started HUD redesign pass 1 for `Workers`. The right-side worker detail view was split into clearer cards: `Profile`, `Skills`, `Effects`, `Needs`, `Perks`, `Work`, and `Contract`, replacing the previous dense mixed layout. The old needs/perks shared-card artifact path was removed by separating them into two proper panels, worker list status badges were widened, and status wording was unified between the left list and the selected worker card (`Idle`, `Production`, `Logistics`, `Intercity`, `Trade Run`, etc.). This pass also restored build stability after perk cleanup by keeping the old perk/effect reference data needed by `States`/gambling, while worker runtime perk assignment was reduced back to MVP behavior: only random `Alcoholism` is currently assigned. Verified with `dotnet build Assembly-CSharp.csproj -nologo` (0 errors, 0 warnings).

- 2026-04-20 (session 2): Added new HUD menu button "States" (Состояния). New file `GameBootstrap.FleetCanvas.StatesScreen.cs` creates a 700×580 scrollable reference panel listing all 4 Skills (Driving/Stamina/Production/Logistics with descriptions), all 17 Effects grouped as Activities (Rested, Well Fed, Forest Air, Sawdust, Warehouse Flow, Craft Focus, Worked Hard, Drunk, Road Focus, Road Fatigue, Race Rush) and Needs (Hungry, Starving, Sleep Deprived, Exhausted, Bored, Burned Out) with colored modifier tags (+N green / −N red) and source/cause hints, plus 1 Perk (Alcoholism). Panel localizes to Russian/English on language change. `isStatesPanelOpen` added to `GameBootstrap.cs`; button added to DrawMenuBar in `Fleet.cs`; close logic wired in ToggleMenuPanel, Input.cs (Escape/CloseAllMenus/scroll-block), BuildingQuickHud blocking check, and Localization.cs dirty flag. Build not verified in this session.

- 2026-04-20 (session 2): Fixed Workers HUD detail panel — Перки section. `needsCardLayout.childForceExpandWidth` was `true`, causing Unity to expand the 2px divider to ~1/3 of the card width (gray-blue "square" artefact) and squish the perks column to near-zero width (hiding Алкоголизм text). Changed to `false` so the divider stays at `minWidth = 2f` and both columns share the remaining space equally. One-line fix in `ManagementScreens.cs:589`.

- 2026-04-20: Fixed standalone Build And Run freeze when pressing `Join the race`. `Player.log` showed `ArgumentNullException: shader` in `CreateRacingHeadlight`; racing headlights/skydome now use packaged `ShaderRefs` fallbacks instead of `Shader.Find("Standard")`, and racing bootstrap is wrapped in exception logging plus cleanup so future startup errors restore the city instead of leaving `Time.timeScale = 0`. `Time.fixedDeltaTime` is also kept valid during racing pause. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors; one pre-existing legacy `DriverCardUi.DriverId` warning remains.

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

- 2026-04-21: Simplified the Workers HUD Effects panel. The visible list now shows only each active effect name plus remaining time, while descriptions and skill modifiers moved into the shared hover tooltip; the tooltip expands for Effects and returns to the compact size for Skills. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-21: Expanded worker effects into the current gameplay loop. Motel sleep now applies `Rested`; need warning/critical states apply and clear live debuffs for hunger, sleep deprivation, and leisure burnout; completed work applies `Worked Hard` plus building-specific effects for Forest/Sawmill/Warehouse/Furniture Factory; normal route completion applies `Road Focus`; intercity trade completion applies `Road Fatigue`; race bonuses apply `Race Rush`. Effect refresh logging was adjusted to avoid per-frame spam. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-21: Added the second active worker effect, `Well Fed` / `Сытость`, after successful Canteen visits. Consuming Food now applies/refreshed a 6-game-hour effect with Stamina +2, Production +1, and Logistics +1; the existing Effects panel and effective skill display pick it up automatically. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-21: Added the first active worker effect, `Drunk` / `Опьянение`, after successful Bar visits. Consuming Alcohol now applies/refreshed a 4-game-hour effect with Driving -5, Production +1, and Logistics +1; the Workers HUD skill lines show effective values with base-plus-delta notation while the Effects panel lists the timed modifier. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-20: Added a worker Effects foundation beside Skills in the Workers HUD. Workers now have active temporary effect state with duration and skill modifier deltas, effect timers tick with game time, debug logs record effect activation/expiry, and the selected worker card shows a separated localized Effects panel with an empty state. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-21: Added the first worker perk system. Workers now roll random perks during stat generation; the first available perk is Alcoholism, internally typed as a negative perk. Workers HUD now shows a Perks column beside Needs with a divider, localized labels, and per-perk hover details, but does not print perk type labels in the HUD. Alcoholism strengthens and extends the Bar/Drunk effect instead of being only a label. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-21: Changed Workers HUD effects hover from a whole-table tooltip to per-effect rows. Each visible effect row now has its own hover target and shows only that effect's remaining time, modifiers, and description. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

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

- 2026-04-23: Added explicit encoding-safety rules to both `AGENTS.md` and `ai/README.md`. The repo workflow now formally treats project text files as UTF-8, prefers `apply_patch` for localized edits, forbids unsafe shell rewrites without explicit UTF-8, and requires quick mojibake scans after UI/localization changes.

- 2026-04-23: Fixed visible mojibake/encoding regressions in several player-facing HUDs. Cleaned broken Russian strings in the top time/day HUD, the shared building quick HUD (including Motel/service activity labels and gambling slot text), and the States screen. Also restored the correct Russian display name for `Forest -> Лесозаготовка` in shared location-label helpers. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Expanded the local-bus quick HUD with richer route context. The selected city bus card is now taller and shows not only driver / passenger count / current stop / route leg, but also the next stop target and a compact passenger manifest (`Name -> Stop #N`, capped to a few visible entries with overflow summary). Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Added local-bus runtime selection and worker trip status polish. The active city bus is now a selectable world object with its own quick HUD (driver, phase, passenger count, stop, route leg), and Worker quick HUD now shows bus-trip states such as walking to a stop, waiting at a stop, and riding to a destination stop. `Assembly-CSharp.csproj` was updated to include the new `Assets/Scripts/Runtime/UI/GameBootstrap.LocalBusQuickHud.cs` partial for external `dotnet build` validation. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Added worker local-bus passenger MVP for long trips. Local workers can now choose the city bus for sufficiently distant commutes to production buildings, service visits, and Motel sleep: they walk to an origin stop, wait there, board during the bus stop dwell, ride invisibly inside the existing local route bus, exit at the chosen destination stop, and then resume the original final walk phase. Passenger transitions are logged under `BUS_PASSENGER`. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Fixed premature idle/life-cycle release for local bus drivers during shift handoff. While `NeedsShiftEndReturn` is set, bus drivers are now excluded from idle wandering and idle conversations, so they stay logically busy until the bus actually completes `ReturningToParking` and `CompleteBusDriverShiftReturn()` starts post-work life-cycle. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Fixed a local-bus route startup race. Bus drivers no longer re-enter `ToParkingForShift` after already boarding the local bus: `UpdateDriverShiftPreparation()` now skips workers already controlling an active local bus route, and `StartBusDriverShiftCommute()` has a guard/log line to prevent accidental driver-object reactivation over an active bus. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Fixed bus-driver shift assignment availability. The grouped Bus Driver card now allows separate workers to be assigned to all three shift rows; UI availability no longer incorrectly blocks candidates just because they are idle-walking/resting, while a worker already assigned to one bus slot is not offered for another slot until removed. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Reworked the bus-driver assignment UI in Shifts/Assignments to match the Warehouse-style grouped layout. Bus Driver now renders as one logistics card with a shared title/summary and three compact shift rows (Morning / Evening / Night), each with its own assign/remove controls, while keeping the existing per-shift runtime logic intact. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Expanded bus action telemetry in `debug.log` for easier route debugging. Added `BUS_SHIFT` transition logs for local-bus boarding blocks (with reason and anti-spam dedup), successful boarding/awaiting shift window, route-cycle start from Parking, next-stop transitions (ascending/descending leg), and route-start failures due to missing stops/Parking. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Added local bus passenger attribute to runtime data (`PassengerCount` with max capacity `5`). The value is now surfaced only in bus micro HUD context (driver quick HUD while `On Bus Route`: `Passengers X/5`) and not shown in the Shifts assignment panels. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Fixed local bus visual orientation. Local-route bus rotation now uses model-axis-aware facing (`+X` as bus nose) via `Quaternion.FromToRotation(Vector3.right, direction)` instead of generic `LookRotation`, so buses move along the road instead of appearing sideways. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Reworked bus-driver assignments to match the intended 24h model. The old single `Bus Driver` slot was replaced by three dedicated bus-driver slots tied directly to Morning / Evening / Night, each slot auto-assigns its matching shift hour, and bus drivers are no longer mixed into the normal local-delivery shift cards. Runtime role tracking now uses three bus-driver slot ids instead of a single `busDriverId`. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Added clearer bus-driver UX for the reserved-but-no-shift case. Assigning a worker to the Bus Driver slot now writes an explicit `BUS_SHIFT` explanation to `debug.log` if no shift is assigned yet, the Bus Driver slot HUD tells the player that Morning/Evening/Night still must be chosen, and worker quick HUD shows `Bus Driver: no shift` / `Водитель автобуса: смена не назначена`. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Expanded local bus debug logging for route-end analysis. `debug.log` now records `BUS_SHIFT` entries when a bus driver is marked for finish-cycle return, when a stop performs the finish-cycle decision, when the route cycle is considered complete, and when the bus starts the actual return-to-parking segment. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Refined local bus shift-end behavior. When a bus driver's shift ends mid-route, the bus no longer aborts toward Parking from the next stop; it now finishes the current full route cycle, then returns to Parking and completes after-work flow. For the current MVP this means the bus parks only after reaching Stop #1 on the descending leg, or after the single stop if only one local stop exists. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-23: Fixed a bus-driver shift assignment regression. Assigning a logistics shift to the worker reserved in the Bus Driver slot no longer clears `busDriverId`, and if that shift is assigned during an active shift window the worker now starts the bus-specific commute to Parking immediately instead of falling back to normal truck commute logic or staying idle. Verified `dotnet build Assembly-CSharp.csproj -nologo` with 0 errors and 0 warnings.

- 2026-04-22: Forest production was reworked into an early Lumberyard MVP. Ambient fake forest workers were disabled; the Forest building now renders as a simple lumberyard/depot; dense-forest misc trees are registered as harvestable runtime trees with daily growth stages 0..5; assigned Forest workers now leave the building, walk to a real misc tree, perform a 5-hit chop loop, trigger a fall, spawn 1..3 world logs on the ground, carry logs back one by one into Lumberyard storage, then replant a sapling that grows over following days. Shift-end logic now also pulls an out-in-the-field lumber worker back into the normal after-work life cycle.

- 2026-04-22: `Assembly-CSharp.csproj` was updated to include the new `Assets/Scripts/Runtime/World/GameBootstrap.Lumberyard.cs` partial so `dotnet build Assembly-CSharp.csproj -nologo` continues to validate the repo outside Unity after adding the new runtime file.

- Keep `ai/work-log.md` short. If it grows beyond roughly 120-160 lines, collapse older completed items into this summary format again.

- Code remains source of truth. The project has many partial `GameBootstrap.*.cs` files; memory is only a navigation aid.

- Prefer `dotnet build Assembly-CSharp.csproj -nologo` after code edits when practical. For memory-only edits, a text/diff check is enough.

- Avoid broad Canvas localization passes immediately after dynamic HUD redraws unless necessary; prior fixes moved several HUDs toward direct localized strings to avoid mixed-language/mojibake-looking output.

- Tutorial flow is sensitive to timing, overlays, highlights, and camera state. When changing tutorial steps, verify OK click behavior, skip behavior, menu highlight cleanup, and whether regular HUDs should be hidden or preserved.

- Quick HUDs should not open automatically after building placement and should close when larger HUD windows open. Fleet details are an exception: preserve Fleet's internal selected-truck state while Fleet is open.

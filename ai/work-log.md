# Work Log

Last updated: 2026-05-08

Purpose: compact active memory for recent work. Older detailed history was intentionally collapsed on 2026-04-20, 2026-05-03, and 2026-05-08 to keep agent startup light. Use git history for exact old implementation details.

## Recent Work

- 2026-05-08: Reworked the Motel building microHUD content. Motel now has a dedicated compact Russian summary for sleep/rest service, fee, staff on shift, and bank; the duplicated "workers inside" counter was removed from the summary while the separate people-in-building list remains with an empty state, and the Motel quickHUD height now shrinks when the inside list is empty. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `git diff --check`, and added-line mojibake scan.

- 2026-05-08: Added a second mandatory New Game build-unlock layer. New Game still starts with one-lane road, Warehouse, Motel, and City Hall; after the core trio, only Parking, Labor Exchange, Canteen, Forest, Gas Station, Sawmill, and Bar unlock. The remaining optional layer, including two-way road, bus stop, Docks, advanced production, leisure, housing, Kindergarten, and Car Market, unlocks only after all second-layer buildings exist. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, line-count check, `git diff --check`, and targeted mojibake scan.

- 2026-05-08: Tightened New Game build progression order. New Game still starts with only the one-lane road, Warehouse, Motel, and City Hall, but no longer unlocks partial groups from each core building. All remaining build tools now unlock together only after Warehouse, Motel, and City Hall all exist, preserving the new-tool pulse/feed feedback at the final core building. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, line-count check, `git diff --check`, and targeted mojibake scan.

- 2026-05-08: Fixed another idle-worker overlap found at the end of `debug.log`. The fresh trace showed Lilit Renne and Raul Pemmick both entering `IdleSmoking` on cell `(47,8)` after an idle conversation/wander sequence. Idle wander now refuses to step into a cell already occupied by a visible worker, and stationary idle actions such as smoking/phone calls redirect to a free wander point when the current spot is occupied. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, line-count check, and `git diff --check`.

- 2026-05-08: Tuned idle and Bar interior dialogue audio/readability. World idle dialogue lines now hold slightly longer and use a stronger spatial vocalizer at the conversation midpoint. Bar interior patrons now have local 3D vocalizer syllables during speech bubbles plus a separate room ambience loop. Entering the Bar interior switches the active listener to the interior camera and pauses outside city audio while leaving UI/interior sources audible; exiting restores the previous world audio/listener state. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, line-count check, and `git diff --check`.

- 2026-05-08: Added separate New Game build-unlock progression. New Game starts with only the working road tool, Warehouse, Motel, and City Hall; later tightened so all remaining build tools unlock only after the full core trio exists. Build menu categories/items pulse with a warm glow when new tools appear, and feed events list newly unlocked buildings. Tutorial skip no longer unlocks all build tools for New Game. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, line-count check, and `git diff --check`.

- 2026-05-08: Made the Build dock's upper tray and lower category bar containers fully transparent and non-raycast while keeping individual category/tool button backplates and borders visible/clickable. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, targeted line-count check, and targeted `git diff --check`.

- 2026-05-08: Added Build dock category unfocus behavior. Clicking the already-selected bottom category now clears the selected category and lets the upper building tray hide through its existing animation, while keeping any active build tool untouched. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, targeted line-count check, and targeted `git diff --check`.

- 2026-05-08: Fixed river ambience positioning. The river loop now uses a dedicated 3D nearest-point proxy instead of the shared 2D nature ambience source, so panning inland fades the river by map distance while moving along the river keeps it audible. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`.

- 2026-05-08: Fixed Main Menu Graphics/Sound options action buttons. Graphics settings now has a real close `X`, the Graphics panel is taller so its reset button stays inside the panel layout, and Graphics/Sound close/reset buttons are stored as refs and revalidated as interactable raycast targets whenever an options panel opens or refreshes. Reset actions also play UI feedback so clicks are visible even when values were already default. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, line-count check, `git diff --check`, and targeted added-line mojibake scan.

- 2026-05-08: Added smooth outside-click closing for the bottom Build dock. When the Build menu is open and no build tool is active, a normal world click closes the dock with a downward slide/fade animation; clicks during active placement remain reserved for building. The Build canvas now stays alive during its close animation through a root `CanvasGroup`. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, line-count check, and `git diff --check`.

- 2026-05-08: Tightened the bottom Build dock behavior after visual review. The upper building/tool tray no longer auto-opens when the Build menu opens; it stays hidden until the player clicks a bottom category, then animates in. Removed the default `Available` badge from available build tools so the icon art remains unobscured, while non-default statuses still appear. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, line-count check, and `git diff --check`.

- 2026-05-08: Reworked the Build menu into a Cities-style bottom dock. The old centered Build window is now a bottom-centered category bar with custom drawn category icons; selecting a category animates a centered building/tool tray above it, individual tools keep icon + label + status states, and both categories/tools get hover scale/lift animation. Tutorial copy and `ai/tutorial-scenario.md` now refer to the bottom Build dock / `B` instead of the top-HUD build window. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, and targeted mojibake scan for changed tutorial/build text.

- 2026-05-08: Slowed Bar interior conversation bubble pacing. Decorative patron dialogue now uses a longer 13-second conversation cycle with a shorter 3.2-second visible window, making speech bubbles appear less frequently while leaving other ambient animations intact. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `git diff --check`, line-count check, and targeted mojibake scan.

- 2026-05-08: Removed the large animated `BAR` wall letters from the Bar interior scene. The back wall now relies on shelves, bottles, frames, and static menu decor instead of a glowing oversized text sign. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `git diff --check`, line-count check, and targeted mojibake scan.

- 2026-05-08: Fixed solid yellow rectangles in Bar interior text signs. `CreateBarInteriorTextSign` no longer replaces the `TextMesh` font material with a generic unlit material, preserving the font atlas alpha while coloring via `TextMesh.color`. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `git diff --check`, line-count check, and targeted mojibake scan.

- 2026-05-08: Fixed mirrored Bar interior `TextMesh` rendering. Conversation bubbles now billboard with the same front-facing convention as worker idle dialogue text, and back-wall text signs no longer get a 180-degree flip that exposed their reverse side. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `git diff --check`, line-count check, and targeted mojibake scan.

- 2026-05-08: Added ambient animation and coziness to the Bar interior scene. The room now keeps refs for decorative patrons, adds a companion `GameBootstrap.BarInteriorScene.Animation.cs` partial, animates idle breathing, table/counter conversations with typed world-space bubbles, drink-sipping poses, dance-floor sways, bartender wiping, candle/sconce/sign flicker, and extra warm decor. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, line-count check, `git diff --check`, targeted whitespace check, and targeted mojibake scan.

- 2026-05-08: Disabled Unity editor cloud diagnostics/analytics settings after `Editor.log` showed Unity Connect DNS failures followed by the internal `CrashReporter::GetInsightsSignedUrlAsync should only be called from the main thread` warning. `UnityConnectSettings` now disables Connect, engine diagnostics, and editor exception capture; `ProjectSettings` no longer submits analytics. This is an editor/services noise fix, not a gameplay-threading fix.

- 2026-05-08: Added a large fullscreen Bar interior scene from the Bar quick HUD. The Bar context button now opens `Enter Inside` / `Войти внутрь`, pauses the main simulation, fades from the dimmed town into a separate RenderTexture room, shows a large roofless low-poly interior with counter, tables, warm lights, and decorative character figures, then exits through a fade-out button while restoring the previous speed or paused state. Main file: `GameBootstrap.BarInteriorScene.cs`. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, line-count check, `git diff --check`, and targeted mojibake scan.

- 2026-05-08: Fixed a second idle-worker overlap source from the end of `debug.log`. After Motel construction, starter workers and the Motel arrival wave could reserve duplicate future motel idle cells because idle-slot selection only checked currently active positions. Motel idle placement now treats other workers' current positions, assigned `MotelIdlePosition`, active walk target, and final walk waypoint as reserved, and both setup and starter relocation pass the current driver into the reservation check. Idle wander also rejects same-cell zero-step targets. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, line-count check, targeted `git diff --check`, and targeted mojibake scan.

- 2026-05-08: Fixed early idle-conversation worker overlap found from `debug.log`. Two starter workers could choose the same idle-wander destination cell near the intercity stop, then start an idle conversation while visually stacked. Idle wander target selection now treats other workers' current cells and already planned idle-wander targets as reserved, idle conversations do not start/continue inside the same grid cell, and idle visual separation uses the full idle personal-space radius. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, line-count check, targeted `git diff --check`, and targeted mojibake scan.

- 2026-05-08: Tightened `ai/tutorial-scenario.md` after comparing it with current Tutorial code. The scenario now notes that the Workers goal completes on opening the Workers panel, local bus stops complete at `>= 2` despite text asking for two, paid service banks exclude free/leisure exceptions like City Park, and the contextual Staffing top-menu button also stays visible while the staffing screen is open.

- 2026-05-08: Added a dedicated Tutorial-mode scenario memory rule. `ai/tutorial-scenario.md` now records the current `Обучение` / `GameStartMode.Tutorial` player path, with maintenance rules requiring serious gameplay/HUD/staffing/economy/transport/trade/building/worker changes to compare against it and update Tutorial/scenario together when the taught flow changes. `AGENTS.md`, `ai/README.md`, `ai/prompt-templates.md`, and `ai/systems-map.md` now reference this rule.

- 2026-05-08: Moved staffing out of the permanent top HUD. Normal play no longer shows a top-level Vacancies tab; early Tutorial keeps a contextual Staffing button only while manual assignment is still being taught. Labor Exchange quick HUD now opens the staffing overview, the screen title/counts/hints frame vacancies as Labor Exchange automation with manual override, tutorial/goals/localization text was refreshed, new UI helper partials were added and synced in `Assembly-CSharp.csproj`, and release notes / owner map / architecture notes were updated. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, line-count check, JSON parse, `git diff --check`, stale Vacancies text scan, and mojibake scan.

- 2026-05-08: Compacted AI memory after reviewing `ai/`. `work-log.md` was collapsed from 441 lines to a short system-grouped summary, and `release-notes.md` `v.0.0.3` was refreshed for City Hall/Trust, Residents HUD, Social Graph, worker inventory/Kiosk, families/Kindergarten, and social-introduction/idle-dialogue player-facing changes. Verification for this memory-only edit: line count, `git diff --check`, and mojibake scan; no Unity build needed.

- 2026-05-08: Added and polished the special City Hall social-introduction request. City Hall now creates a normal `SocialIntroduction` request first, the fullscreen scene starts after `Принять`, asks for a non-empty player topic, reveals both residents, plays word-typed Russian dialogue, and resolves with a 70% success / 30% failure result card. The scene now has a 10-variant dialogue bank, topic-aware success/failure copy, procedural per-word voice syllables, success/failure pacing, relationship consequences, and an F9 `SOCIAL REQUEST` debug trigger. Main files: `GameBootstrap.CitySocialRequests.cs`, `GameBootstrap.CitySocialRequestSceneHud.cs`, `GameBootstrap.CitySocialRequestInput.cs`, `GameBootstrap.CitySocialRequestDialogue.cs`, `GameBootstrap.CitySocialRequestVoice.cs`.

- 2026-05-08: Implemented in-world idle conversation snippets. Idle conversations now spawn a small non-UI `TextMesh` bubble above the current speaker, type three short Russian lines word by word, play existing procedural voice syllables from a spatial `AudioSource` centered between the two workers, and give the active speaker a stronger conversational gesture pose. Main file: `GameBootstrap.WorkerIdleDialogue.cs`.

- 2026-05-08: Adjusted City Hall request and Residents inventory HUD behavior. City Hall lists only active Open/Accepted requests, keeps accepted requests visible until their goal resolves/expires, and fades rejected rows out instead of keeping history. The Residents Inventory tab hides zero-quantity Snack/Coffee cards and shows a compact empty state when no auto-consumables are available.

- 2026-05-07: Added the City Hall request/trust loop. City Hall is a buildable civic building with request UI, unread marker, `Trust` / `Доверие` score, accept/reject actions, 24h service-building goals, immediate completion when the target building exists, +25 trust success reward, and -25 trust rejection/expiry penalty. Service-building requests now wait 8 in-game hours after City Hall activation and at least 12 in-game hours between new requests.

- 2026-05-07: Reworked the Residents HUD. The old Workers screen became a near-fullscreen Residents HUD with larger portrait rows, clearer dossier/skills/work cards, compact left-list money/status layout, redesigned Needs/Inventory/Thoughts/Social Links tabs, embedded focused social graph, animated node/edge motion, and a polished right inspector panel.

- 2026-05-07: Added worker thoughts, life opinions, and player-facing mental-state UI. Workers now maintain bounded recent thoughts, active current-priority thoughts, and Work/City/Money/Housing/Social opinion snapshots. The Thoughts tab shows `Сейчас важно`, recent thought rows, opinion chips, and generated icons; thought/debug logging was made readable without verbose spam.

- 2026-05-07: Added and tuned the citizen social graph. Workers now form hidden/visible social memories from arrivals, co-presence, coworker shifts, and idle conversations; visible links require enough familiarity, weak hidden links decay, Socialite improves relationship growth, and the standalone `Social` / `Связи` HUD supports whole-city and focused views with filters, semantic layout, hover details, and animations.

- 2026-05-07: Added worker-owned inventory and the Kiosk consumable loop. Workers carry item stacks loaded from `worker-items.json`; Kiosk became the single Snack/Coffee vendor after Coffee Shop was removed; Snack/Coffee auto-use now triggers before the critical threshold, and service prices were reduced to Motel/Bar/Canteen `$8` and Kiosk consumables `$4`.

- 2026-05-07: Tuned worker needs and service complaints. Need Warning became internal behavior pressure only; only Critical needs create player-facing problem states or City Hall need complaints. Critical Meal/Sleep workers can route to Kiosk for Snack/Coffee before falling back to harsher options.

- 2026-05-07: Improved worker migration, worker focus, vacancy text, and build navigation. Migration now accumulates bounded vacancy pressure and checks more persistently; hidden-worker focus can resolve targets through buses, trucks, cars, homes, Motel, and assigned/service buildings; Vacancies mojibake in Russian subtitles was fixed; Build menu categories were reorganized into five intent-based groups.

- 2026-05-06: Added worker families, children, household economy, and Kindergarten. Personal House families form from strong social links or delayed fallback matching; families share houses and money pressure, can have children, show household happiness/upkeep/child-care coverage in HUDs, and Kindergarten provides staffed child-care slots that affect family happiness and migration satisfaction.

- 2026-05-06: Added worker social links and profile visibility. Workers store bounded relationship memories with familiarity/relationship/context/decay, the Workers HUD gained Social Links and Profile tabs, left worker rows show personal money, and worker quick HUD `Open in Workers` now selects and scrolls to the correct resident.

- 2026-05-06: Reworked sound/audio and Event Feed/demolition feedback. Main Menu Sound options expose music, ambience, footsteps, and kept generated SFX with sliders/previews; curated nature ambience and grass footsteps are loaded from `Resources`; relaxed generated SFX were narrowed/retuned; demolition/build feedback sounds were added; Event Feed became compact top-right toasts; Delete demolition now supports non-core buildings with confirmation and runtime cleanup.

- 2026-05-06: Continued trade/runtime extraction and schedule cleanup. Trade state, policies, resource ledger, Docks policy runtime, Trade screen models, and first `TradeSimulation` tick coordination were extracted into services. `GameBootstrap.RuntimeSchedules.cs` centralizes building slot counts, staff work-hour checks, service shift presets, higher-education office hours, and UI schedule labels.

- 2026-05-06: Updated early-game balance and workflow rules. Starting treasury is `$5000`; building Motel queues a one-time 10-worker arrival bus through the normal migration flow; development rules now require `.csproj` sync for added/moved/deleted C# files and forbid overlapping build/test/check commands.

## Recent Summary

- 2026-05-08: Retuned the worker microHUD condition readout. The compact resident popup now derives its condition score from current need fullness, so a freshly spawned worker with full needs shows `100` instead of inheriting the longer-term migration satisfaction score, and the warning-styled `Критично сейчас` section became neutral `Нужды` with `Все нужды в норме` as the empty state. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` and `git diff --check`.

- 2026-05-08: Reworked the City Hall building microHUD into a compact dedicated layout. The quick HUD now switches City Hall to a worker-sized dark card headed `Ратуша (Сервис)`, keeps the active-request count only in the `Обращения` status row, keeps short Russian effect/status rows, and preserves close/open actions plus map tether behavior. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` and `git diff --check`.

- 2026-05-08: Added map tether lines to building quick HUDs. Selected building, bus stop, and personal house microHUDs now draw a non-raycast amber UI line from the HUD edge to the start corner of the selected grid footprint, and hide the line when the HUD closes or a blocking HUD opens. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` and `git diff --check`.

- 2026-05-08: Moved City Trust out of the City Hall quick HUD into the top information bar next to Treasury. City Hall quick HUD now explains that citizen requests become 24h city goals, shows only total active requests, the accepted city goal, and the current focus request, without separate new/urgent counters. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `git diff --check`, and targeted mojibake marker scan.

- 2026-05-08: Retuned the compact Event Feed after visual review. Toast rows now keep readable 13px message text, drop the time column, use a stable readable width again, clamp each row to a compact one-line 28px height with clipped overflow instead of tall wrapping, and stay below other HUD layers. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`.

- 2026-05-08: Added a location tether to the compact worker microHUD. The resident popup now draws a non-raycast amber UI line to the same selected worker highlight target used by selection visuals, including workers riding vehicles or being inside buildings, and hides the tether whenever the microHUD is hidden. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal` and `git diff --check`.

- 2026-05-08: Extended City Hall citizen build requests to cover the staged build progression. Requests now use core/second/third construction layers, honor current build unlocks, keep later layers blocked until the current unlocked layer is built, support required counts such as two bus stops, and resolve against actual built-location counts when construction completes.

- 2026-05-08: Fixed Bar interior teardown audio cleanup. Destroyed Unity `AudioSource` references are now stopped through an explicit Unity-null guard instead of C# null-conditional calls, preventing MissingReferenceException during `OnDestroy`/scene release.

- 2026-05-08: Reworked the worker microHUD into a compact Russian status popup. It now shows only resident name, occupation, current activity, overall condition with a readable bar, up to three attention needs with percent bars, money, close, and open-profile actions; profile-style metadata moved back to the full Residents profile.

- 2026-05-08: Compacted the Event Feed into a lower-priority HUD layer. Feed toasts now use smaller rows, fewer visible entries, no separate time column, and `sortingOrder` below quick HUDs and full-screen management windows so other HUDs cover it when they overlap.

- 2026-05-05: Rebuilt the active Tutorial around current systems: Labor Exchange, automatic arrivals, services, warehouse loaders, local buses, economy/taxes, Regional Map route building, Docks staffing, and Trade policy setup.

- 2026-05-05: Built the generated regional route/trade flow. Regional Map now generates the player's town plus visible external cities, route state, resource buy/sell tables, pixel-art map presentation, post-route `Open Trade`, land merchant trucks, and river Docks ships that obey built-route policies.

- 2026-05-05: Moved Trade into a dedicated screen with per-resource `No trade`, `Buy up to`, and `Sell surplus` policies. Trade/Docks policy leaks were fixed, per-unit prices are visible, old intercity-driver trade was runtime-blocked in favor of built generated routes, and Trade screen click reliability was hardened.

- 2026-05-05: Improved service staffing, Labor Exchange priorities, transport throughput, and economy pressure. Service buildings expose multiple shift slots, transport bottlenecks raise vacancy priority, truck cargo capacity and speeds were increased, and treasury can go negative with red HUD presentation.

- 2026-05-05: Simplified player starts and HUD presentation. Main Menu now offers only `Tutorial` and `New Game`; both use the empty build-first baseline, New Game applies tutorial skip state, top HUD shows population, and Regional Map/Docks/Trade presentation was tightened.

- 2026-05-04: Added Docks as a shore building with pier/crane visuals, staffing, Docks quick HUD, Warehouse/Docks cargo movement, river ship phases, shoreline placement fixes, and generated-route integration.

- 2026-05-04: Added Labor Exchange, service-building staff vacancies, automatic worker migration, dynamic salaries, fixed-term contracts, and three-level worker professionalism. Direct paid hiring was removed from player-facing UI.

- 2026-05-04: Added personal worker cars and home-meal behavior. Personal House owners can buy/use cars for services, Labor Exchange, departure, home sleep, and commutes; house residents route home for Meal and Sleep.

- 2026-05-04: Removed Fuel/Food/Alcohol as town storage/trade resources. Services now depend on buildings, worker needs, money, and truck refuel behavior instead of Warehouse service-resource buffers; remaining trade resources are Logs, Boards, Cotton, Textile, and Furniture.

- 2026-05-04: Hardened workers, roads, and UI from `debug.log` review. Worker walking has safer rescue/path failure handling, City Park no longer traps workers, service interiors hide/respawn workers correctly, road segment turn-fill and roadside decoration cases were fixed, and Workers/quick HUD scrolling/click targets improved.

- 2026-05-03: Completed broad 900-line runtime partial cleanup. Oversized `GameBootstrap` partials were split by concern, owner map and architecture notes were refreshed, line-count tooling/CI default to 900 lines, and new editor smoke tests cover road build, transport/trade, vacancies, and tutorial goals.

- 2026-05-03: Started moving data/text out of large UI C# files. `Assets/Scripts/Runtime/Data/` loaders and JSON catalogs now back Patch Notes and Build menu content first, with C# fallbacks kept.

- 2026-05-02 to 2026-05-01: Tuned worker need balance, no-money fallback effects, road segment workflow, local-bus edge cases, smooth road terrain visuals, debug telemetry, and the F9 worker-wave tool.

- 2026-04-30 to 2026-04-06: The project evolved from a runtime-generated grid logistics prototype into the current town-sim slice with tutorial, workers/needs, production, local buses, regional trade, racing, water/ambient life, audio/options, and many extracted service seams.

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

- After adding or changing HUD/Canvas buttons, explicitly verify clickability: Canvas has an EventSystem/GraphicRaycaster, every `Button` has a `targetGraphic` and listener path, text/decorative images do not block raycasts unless they are the button target, overlay close buttons are last-sibling but small, and the new panel is included in `CloseAllMenus`/blocking-HUD state.

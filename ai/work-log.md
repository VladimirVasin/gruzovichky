# Work Log

Last updated: 2026-05-08

Purpose: compact active memory for recent work. Older detailed history was intentionally collapsed on 2026-04-20, 2026-05-03, and 2026-05-08 to keep agent startup light. Use git history for exact old implementation details.

## Recent Work

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

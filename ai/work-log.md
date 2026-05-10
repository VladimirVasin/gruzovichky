# Work Log

Last updated: 2026-05-09

Purpose: compact active memory for recent work. Older detailed history was intentionally collapsed on 2026-04-20, 2026-05-03, 2026-05-08, and 2026-05-09 to keep agent startup light. Use git history for exact old implementation details.

## Recent Work

- 2026-05-10: Moved the Noosphere animation inside the Noosphere window. The window is back to its wide layout and now combines a left-side Canvas animation panel with the right-side knowledge log; the animation shows the core, resident nodes, canon links, recent event arcs, and travelling pulses without spawning world objects over City Hall. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted project-file scan, and targeted mojibake scan.

- 2026-05-09: Added citywide Noosphere canon knowledge. When enough active residents have formed the same building or conversation-topic knowledge, it becomes permanent city knowledge, records a canonized Noosphere event, applies to all residents through duplicate checks/thought context/dialogue/UI without filling every personal memory list, and shows as `forever` in Residents Knowledge and Noosphere. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted conflict-marker scan, and targeted mojibake scan.

- 2026-05-09: Removed player framing controls from the first City Hall social-introduction topic scene. The scene now asks only for a conversation topic again; the first rumor/knowledge iteration is recorded with neutral source framing, while rumor connotation is left to the conversation result and later resident-to-resident iterations. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted trailing-whitespace scan, and targeted mojibake scan.

- 2026-05-09: Added rumor-state tracking for resident conversation knowledge. Conversation-topic memories now keep the original iteration-1 topic separately from the current rumor text, track distortion percentage, connotation score/confidence, and carry a stable rumor root through pending knowledge, resident sharing, opinions, the Residents Knowledge tab, and Noosphere logs. Each new resident-to-resident iteration drifts distortion and connotation, with a deterministic chance to polarize to 100% positive or 100% negative. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted trailing-whitespace scan, and targeted mojibake scan.

- 2026-05-09: Added player framing for City Hall conversation rumors. The social-introduction scene now lets the player frame a topic as supportive, neutral, or a warning; that framing travels through social memory, pending knowledge, resident opinion formation, copied knowledge, the Residents Knowledge tab, and Noosphere logs separately from the conversation success/failure result. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted trailing-whitespace scan, and targeted mojibake scan.

- 2026-05-09: Added staged resident knowledge formation. New facts now enter `PendingKnowledge`, move through heard/compare/judge stages, then become personal memory only after an opinion tone, score, confidence, and reason are formed. Building discovery, City Hall conversation topics, resident sharing, Residents Knowledge UI, and Noosphere logs now use the formed opinion path. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted trailing-whitespace scan, and targeted mojibake scan.

- 2026-05-09: Added delayed resident thought formation. Active warnings and knowledge reactions now create pending thought candidates first, mature over in-game time, cancel if the cause disappears, and enrich final text with relevant active knowledge such as known service/work locations or newly learned conversation/building knowledge. The Workers Thoughts HUD now shows a `Раздумывает` / `Осмысляет знание` current state while a thought is still forming. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted trailing-whitespace scan, and targeted mojibake scan.

- 2026-05-09: Refreshed AI memory after the resident knowledge/Noosphere work. `project-overview.md` now treats knowledge sharing and Noosphere as stable project areas, `architecture-notes.md` documents the `GameBootstrap.WorkerKnowledge*` seam, and `release-notes.md` tracks the player-facing Knowledge tab, spreading memories, and Noosphere HUD for `v.0.0.4`. Verification: `git diff --check` and targeted mojibake scan.

- 2026-05-09: Made residents with active conversation-topic knowledge more likely to initiate idle dialogue themselves. Idle conversation start chance now gets a source-side bonus when the initiating worker holds a non-expired `ConversationTopic` memory; building-existence knowledge does not affect initiation. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted trailing-whitespace scan, and targeted mojibake scan.

- 2026-05-09: Fixed City Hall topic knowledge being hidden behind building transfers. Player-prompted City Hall conversations now suppress the generic pre-topic knowledge share, create the requester topic memory first, then transfer that exact topic to the target as iteration 2 with the City Hall interaction reason. General resident knowledge sharing now prefers conversation-topic candidates before building-existence memories. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted trailing-whitespace scan, and targeted mojibake scan.

- 2026-05-09: Highlighted knowledge subjects inside live dialogue bubbles. Idle knowledge-share bubbles now render City Hall topics and building-existence memories as bright rich text, with tag-safe word reveal/wrapping. The Bar interior scene now prefers active resident knowledge lines with highlighted topics/buildings and falls back to its old ambient phrases when no active knowledge is available. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted trailing-whitespace scan, and targeted mojibake scan.

- 2026-05-09: Added per-memory knowledge iterations. Direct building knowledge starts at iteration 1, City Hall player-topic memories assign iteration 1 to the requester and 2 to the target, and every resident-to-resident transfer creates a separate copied memory at source iteration + 1 without changing duplicate matching. Residents Knowledge and Noosphere now display the stored iteration snapshot. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted trailing-whitespace scan, targeted mojibake scan, and targeted iteration wiring scan.

- 2026-05-09: Extended resident knowledge sharing beyond idle bubbles. Service co-presence, coworker shifts, City Hall social introductions, and family formation can now transfer one active non-duplicate knowledge item, with source reasons that name the interaction type for resident Knowledge and Noosphere logs. Worker arrival bus waves no longer create social memories, and the old `ArrivalWave` social kind was removed. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted trailing-whitespace scan, targeted mojibake scan, and targeted sharing/arrival wiring scan.

- 2026-05-09: Added idle knowledge sharing between residents. Idle conversations now first try to transfer one active, non-expired knowledge item the partner does not already know, using building/topic placeholder bubble templates; existing idle bubbles remain the fallback when no shareable knowledge exists. Shared memories get a fresh 48h timer, resident Knowledge/Noosphere source reasons, and duplicate knowledge does not refresh timers. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted trailing-whitespace scan, targeted mojibake scan, and targeted sharing wiring scan.

- 2026-05-09: Added a visible Noosphere knowledge timer indicator. Each knowledge-log row with a timed memory now shows a right-side TTL label, shrinking color bar, and remaining hours; burned/expired rows show a depleted red state. The indicator is decorative and does not intercept scroll/close clicks. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted trailing-whitespace scan, and targeted mojibake scan.

- 2026-05-09: Added resident building-existence knowledge. First use of a concrete building records a 48h `BuildingExistence` memory with source reason, shows it in the resident Knowledge tab and Noosphere, and does not refresh the timer on repeat visits. Sources include service interiors, truck stops, work shifts, kiosks, parks, homes, bus stops, and City Hall requests/complaints. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted diff mojibake scan, and targeted no-refresh wiring scan.

- 2026-05-09: Added the global Noosphere HUD as a citywide knowledge event log. The top menu now opens a Canvas list of received and burned resident knowledge events with names, topics, event time, outcome, and reason; knowledge creation, expiry, and memory-cap trimming feed the log. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted mojibake scan, and targeted clickability wiring scan.

- 2026-05-09: Added 48 in-game-hour expiry timers for Residents `Knowledge` conversation-topic memories. New topic memories store `ExpiresWorldHour`, existing topic memories derive expiry from their creation hour, runtime prunes expired memories, and the Knowledge tab shows remaining hours plus a shrinking yellow-to-red timer bar. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted mojibake scan, and targeted clickability/timer wiring scan.

- 2026-05-09: Highlighted player-entered City Hall social-conversation topics in bright rich text both inside the Residents `Knowledge` tab and in the City Hall dialogue/result scene. The input field stays plain text, while dialogue/body/result renderers explicitly support rich text. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, targeted mojibake scan, and targeted clickability/rich-text wiring scan.

- 2026-05-09: Added a dedicated `Knowledge` / `Знания` tab to the Residents detail HUD. It reads persistent `DriverAgent.Memories`, currently showing City Hall social-introduction conversation topics with participant, outcome, and day/time rows. The new tab is wired into the existing Residents tab click-target validation and the new partial/project file/owner map were synced. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, targeted clickability wiring scan, `git diff --check`, and targeted mojibake scan.

- 2026-05-09: Compacted AI memory after reviewing `ai/`. `work-log.md` now keeps only fresh details plus grouped summaries; `release-notes.md` and in-game Patch Notes mention persistent City Hall social-conversation memory; `systems-map.md` metadata matches the latest map touch. Verification: JSON parse, `git diff --check`, and targeted mojibake scan.

- 2026-05-09: Reused the existing F9 `SOCIAL REQUEST` debug command for City Hall social-introduction requests. The command now requires City Hall, blocks while another social scene/request is active, files a normal City Hall complaint, and no longer launches the dialogue scene directly; the scene still starts through the regular accept flow. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, and `git diff --check`.

- 2026-05-09: Implemented persistent resident memory for player-prompted City Hall social conversations. The sanitized topic entered in the special dialogue scene is now stored in both participants' personal memory lists, mirrored into their social-memory context, and shown through a new `Я узнал что-то новое` resident thought instead of the generic pleasant-talk thought. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, and targeted mojibake scan.

- 2026-05-09: Bumped the project version to `v.0.0.4`: Main Menu shows `Lo-fi Delivery Co. v.0.0.4`, in-game Patch Notes/release-notes use `v.0.0.4`, and Unity `PlayerSettings.bundleVersion` is `0.0.4`. Verification: JSON parse, old/new version search, targeted mojibake scan, `git diff --check`, and `dotnet build Assembly-CSharp.csproj -v:minimal`.

- 2026-05-09: Audited `v.0.0.3` Patch Notes against recent implemented work and refreshed them as `v.0.0.4` player-facing notes. The in-game notes now cover staged New Game building unlocks, City Hall construction requests, resident life systems, service/trade progression, compact/tethered microHUDs, Motel guest submenu, Bar interior/audio, regional map, Tutorial flow, and atmosphere fixes while leaving out CI/editor/debug-only details. Verification: JSON parse, `git diff --check`, and targeted mojibake scan.

- 2026-05-09: Reworked the Warehouse building microHUD into a dedicated compact logistics layout. The quick HUD now heads the panel as `Склад`, shows a short Russian storage/logistics description, status/loaders/logistics rows, five warehouse resource counters, keeps the worker section as `Грузчики`, and preserves the existing `Открыть ресурсы` action. Added `GameBootstrap.BuildingQuickHud.Warehouse.cs` and updated the owner map/project file for the new partial. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, and targeted mojibake scan.

- 2026-05-09: Fixed idle life-cycle restart spam and follow-up idle overlap cases from `debug.log`. Due-need idle fallback now respects overlap pauses, advances wander targets instead of rebuilding the same path every frame, and allows proposed idle steps that increase separation when workers are already visually overlapping. Verification: `dotnet build Assembly-CSharp.csproj -v:minimal`, `tools/check-line-count.ps1`, `git diff --check`, and targeted mojibake scan.

## Recent Summary

- 2026-05-08: Reworked compact building and resident microHUDs. City Hall, Motel, Warehouse, worker quick HUDs, guest submenu overlays, map tether lines, Event Feed layering, scroll/click behavior, and close/focus handling were tightened for the current UI style.

- 2026-05-08: Rebuilt the Build experience around the bottom dock. The Build menu now uses category icons and an animated tray, supports outside-click closing and category unfocus, keeps tray/category containers transparent where needed, and New Game unlocks buildings in staged layers instead of exposing everything after tutorial skip.

- 2026-05-08: Added and polished the large Bar interior scene. The Bar quick HUD can enter a separate paused interior with fade transitions, room ambience, patron idle/talk/drink/dance animation, readable TextMesh signs/bubbles, spatial vocalizer audio, and safer teardown/audio listener handling.

- 2026-05-08: Hardened worker idle placement and dialogue from `debug.log` review. Idle wandering, conversations, Motel idle slots, stationary idle actions, and overlap checks now reserve planned/current positions more carefully and avoid same-cell stacking.

- 2026-05-08: Updated Tutorial and staffing memory around the current player path. Normal play no longer has a permanent Vacancies top-HUD entry; Tutorial uses contextual Staffing/Labor Exchange flow, and `ai/tutorial-scenario.md` records the current `Обучение` sequence.

- 2026-05-08 to 2026-05-06: Expanded resident simulation. City Hall requests/trust, social-introduction requests, idle dialogue, Residents HUD, thoughts/opinions, social graph, inventory/Kiosk, families/children, Kindergarten, worker migration, contracts, needs, money, and worker focus were added or tuned.

- 2026-05-06 to 2026-05-04: Continued transport/economy/system work. Trade services, schedule helpers, Docks/regional trade, Labor Exchange automation, service staffing, personal cars, resource simplification, audio/options, Event Feed, demolition, and runtime workflow rules were implemented or hardened.

- 2026-05-03: Completed broad 900-line runtime partial cleanup. Oversized `GameBootstrap` partials were split by concern, owner map and architecture notes were refreshed, line-count tooling/CI default to 900 lines, and editor smoke tests cover road build, transport/trade, vacancies, and tutorial goals.

- 2026-05-03: Started moving data/text out of large UI C# files. `Assets/Scripts/Runtime/Data/` loaders and JSON catalogs now back Patch Notes and Build menu content first, with C# fallbacks kept.

- 2026-05-02 to 2026-04-06: The project evolved from a runtime-generated grid logistics prototype into the current town-sim slice with tutorial, workers/needs, production, local buses, regional trade, racing, water/ambient life, audio/options, and many extracted service seams.

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

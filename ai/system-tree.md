# System Tree

Last updated: 2026-05-14

Purpose: stable informational tree of the project systems, subsystems, feature leaves, and the most important cross-system links. Code remains the source of truth; use this file to orient before reading `ai/systems-map.md` and scanning code.

## How To Use

- Start here when a task is broad, architectural, or touches several gameplay areas.
- Use the system id in brackets, such as `[BUILD]`, to name the affected area in plans and work logs.
- Use `ai/systems-map.md` after this file to find concrete owner files.
- Keep descriptions short: this file is a map, not a design document.

## Top-Level Tree

### [CORE] Bootstrap, Runtime State, And Time

- [CORE.Startup] Startup and mode selection
  - Builds the generated world, initial roads, locations, UI canvases, audio, and starting state.
  - Feature leaves: Main Menu start, Tutorial start, New Game start, loading overlay, day-1 black-screen scene/audio fade, scene bootstrap completion.
- [CORE.Loop] Main runtime loop
  - Ticks world simulation, actors, transport, trade, UI, audio, and visual runtime updates.
  - Feature leaves: day/night clock, game speed, daily rollover, start-of-day title, day-start Noosphere snapshot trigger, update ordering.
- [CORE.State] Shared runtime state
  - Scene-local state still owned by `GameBootstrap` partials.
  - Feature leaves: money, city trust, locations, roads, workers, trucks, buses, world roots, UI dirty flags.
- [CORE.Schedules] Runtime schedules
  - Centralizes staff slot counts, shift windows, service hours, and schedule labels.
  - Feature leaves: production hours, transport shifts, higher-education office hours, worker slot limits.
- [CORE.Diagnostics] Debug and safety tools
  - Keeps logs, smoke-test seams, sanity scripts, and debug panels.
  - Feature leaves: `debug.log`, `SessionDebugLogger`, debug service panel, project sanity checks.

### [WORLD] World, Terrain, Environment, And Places

- [WORLD.Layout] Generated town layout
  - Places major areas and validates generated road-access chains.
  - Feature leaves: layout seed, required destinations, generated road-access validation, map anchors.
- [WORLD.Terrain] Terrain and ground
  - Generates heights, flat pads, ground materials, water-adjacent flattening, and diorama presentation.
  - Feature leaves: terrain height sampling, building pads, water cells, beaches, ground visual layers.
- [WORLD.Nature] Natural zones and ambience
  - Owns forests, hills, lakes, misc vegetation, and ambient animals/particles.
  - Feature leaves: generated/imported/fallback trees, berries, flowers, birds, cats, bees, frogs, moths, leaves, fireflies, river/lake fish.
- [WORLD.Weather] Atmosphere and weather
  - Controls sky, lighting feel, rain/night visuals, water visuals, and graphics-option presentation.
  - Feature leaves: night stars, rain, clouds, water LOD, post-processing, graphics settings, internal cell lighting map.
- [WORLD.Locations] Buildable and generated places
  - Holds `LocationData`, footprints, anchors, storage, service state, and place-specific runtime data.
  - Feature leaves: Parking, Warehouse, Forest, Sawmill, Motel, service buildings, City Hall, Docks, houses, schools.
- [WORLD.Placement] Building placement and occupancy
  - Resolves footprints, walk buffers, access cells, previews, blockers, and demolition cleanup.
  - Feature leaves: rotated footprints, placement preview, non-walkable buffers, entrance exceptions, demolition.
- [WORLD.Decor] Building and service visuals
  - Adds low-poly details and imported/fallback service-building visuals.
  - Feature leaves: building boxes/cylinders, lighting profiles, imported Bar/Gambling Hall interactions, construction pop-ins.
- [WORLD.Footpaths] Pedestrian ground traces
  - Tracks visible footpath wear and preferred pedestrian walking surfaces.
  - Feature leaves: footpath cells, wear, clear-on-building, path preference.
- [WORLD.Litter] Street litter and cleanup
  - Grows visible litter from crowds and lets cleaners remove it.
  - Feature leaves: litter pressure, visible stages, explicit litter sources, litter props, cleaner target reservation, cleaning depot radius.

### [BUILD] Build Mode, Construction, And Unlocks

- [BUILD.Catalog] Build catalog and build menu
  - Loads build categories, titles, descriptions, colors, and costs.
  - Feature leaves: category cards, item cards, cost badges, no-funds states, JSON fallback.
- [BUILD.Unlocks] Build progression
  - Controls which buildings are available in New Game/Tutorial progression.
  - Feature leaves: starter tools, second layer unlocks, third layer unlocks, child-stage school unlocks, unlock feedback.
- [BUILD.Roads] Player road construction
  - Handles click-start/click-finish road segments, previews, validation, and placement.
  - Feature leaves: single-lane road, two-way road, preview footprint, blocked cells, construction spending.
- [BUILD.Buildings] Player building construction
  - Places buildings, charges treasury, clears litter/footpaths, creates `LocationData`, and starts visuals.
  - Feature leaves: placement cost, money popup, service locations, production locations, residential/decorative places.
- [BUILD.Animation] Construction visual feedback
  - Shows road waves and building pop-in animations.
  - Feature leaves: road construction wave, road reveal timing, Bar/Gambling Hall construction animation, spend float.
- [BUILD.Demolition] Removing buildings
  - Lets removable buildings be selected and demolished through confirmation UI.
  - Feature leaves: protected core buildings, repeated service instance targeting, cleanup of building-owned objects.

### [TRANSPORT] Roads, Trucks, Buses, Routes, And Trade Movement

- [TRANSPORT.RoadGrid] Road cells and connectivity
  - Owns road occupancy, road visuals, road markings, access warnings, and route graph facts.
  - Feature leaves: road cell sets, road visuals, markings, roadside props, disconnected-building warnings.
- [TRANSPORT.Pathing] Grid and pedestrian pathfinding
  - Provides road BFS and weighted pedestrian pathing.
  - Feature leaves: road path, safe walk cells, rescue paths, footpath preference, building buffer avoidance.
- [TRANSPORT.Trucks] Truck runtime
  - Moves trucks through assigned trips, parking, loading/unloading, refuel, and route completion.
  - Feature leaves: truck agents, active path, cargo state, trip phases, refuel phases, parking return.
- [TRANSPORT.Infrastructure] Parking and vehicle capacity
  - Provides truck/bus slots and automatic vehicle provisioning.
  - Feature leaves: parking slots, bus slots, provisionable vehicles, assignment capacity.
- [TRANSPORT.LocalBus] Local city bus
  - Runs local stop ordering, route movement, boarding, dwell, fares, and disconnected-stop skipping.
  - Feature leaves: bus stops, stop numbers, route planner, passengers, fare exemption, fallback walking.
- [TRANSPORT.Regional] Regional routes and off-map movement
  - Connects the town to generated external cities by land/river routes.
  - Feature leaves: regional map, built routes, route availability, merchant trucks, river ships.
- [TRANSPORT.Racing] Racing mode
  - Separate racing mode launched from eligible trade-truck flow.
  - Feature leaves: race track, race vehicle, race HUD, controls, atmosphere, race bonus.

### [ECONOMY] Money, Resources, Taxes, And City Upgrades

- [ECONOMY.Treasury] Treasury and money ledger
  - Tracks city money changes and player-facing money feedback.
  - Feature leaves: starting treasury, typed ledger entries, account owner/reason fields, top HUD, money popup, spend/feed events.
- [ECONOMY.BuildingBanks] Building-local money
  - Lets service/production buildings accumulate and expose earnings/taxable balances.
  - Feature leaves: service payments, building bank display, taxable totals.
- [ECONOMY.Resources] Stock and cargo resources
  - Owns Warehouse/Docks stock snapshots and mutations.
  - Feature leaves: logs, boards, cotton, textile, furniture, resource consume/add helpers.
- [ECONOMY.Production] Production chains
  - Turns resources into goods through staffed or automatic production locations.
  - Feature leaves: Forest logs, Sawmill boards, Furniture Factory output, Warehouse delivery chain.
- [ECONOMY.Trade] Trade policies and orders
  - Handles buy/sell orders, auto-dispatch, Docks policy, land/river trade, and trade preconditions.
  - Feature leaves: trade screen rows, order queue, dispatch blocking reasons, auto-dispatch retry, Docks buy/sell.
- [ECONOMY.Taxes] Tax controls
  - Presents and applies city tax/economy controls in management UI.
  - Feature leaves: tax rate UI, taxable bank total, economy summary.
- [ECONOMY.Upgrades] City upgrades
  - Trust-gated purchasable upgrades that affect other systems.
  - Feature leaves: upgrade tree cards, problem relevance highlights, treasury spending, litter gain reduction, cleaner radius, construction discount, longer requests, softer trust penalties.

### [WORKERS] Residents, Jobs, Needs, Life, And Social Simulation

- [WORKERS.Identity] Worker identity and presentation
  - Stores names, visual race, cultural heritage, portraits, education, profession, visuals, and focus state.
  - Feature leaves: generated names, stable race/heritage assignment, heritage HUD badge/catalog, race/gender texture-backed portrait UI with state-aware cartoon layered animation and procedural fallback, education label, citizen id, worker focus, driver/worker object.
- [WORKERS.Personality] Character traits, weaknesses, and temporary affects
  - Separates stable worker traits from at most one steady weakness and temporary emotional states.
  - Feature leaves: 3 generated non-conflicting traits, optional weakness, legacy perk migration, affect lifetime/intensity/source/reason, affect-created thoughts, affect knowledge bias, Workers/F9 personality UI.
- [WORKERS.Hiring] Hiring and migration
  - Brings workers into town and assigns contracts.
  - Feature leaves: migration arrivals, hire flow, worker contracts, salary, arrival bus/truck handoff.
- [WORKERS.Professions] Jobs and professions
  - Maps worker capabilities to building roles and duties.
  - Feature leaves: citizen profession kind, driver, lumberjack, clerk, cleaner, teacher, service staff, logistics worker.
- [WORKERS.Shifts] Shifts, vacancies, and staffing
  - Assigns workers to building slots, transport, logistics, and vacancy flows.
  - Feature leaves: shift screen, vacancy options, assign/remove worker, on-shift counts, Labor Exchange postings.
- [WORKERS.Needs] Needs and services
  - Drives hunger, rest, leisure, family stress, service visits, and fallback activities.
  - Feature leaves: Motel, Canteen, Bar, Kiosk, Gambling Hall, City Park, Kindergarten, Primary School, Secondary School, Gas Station service.
- [WORKERS.Movement] Worker walking and commute
  - Moves residents between work, services, home, parking, bus stops, and idle places.
  - Feature leaves: safe walk target, idle wander, commute path, local bus choice, rescue from unsafe cells.
- [WORKERS.Inventory] Owned items and money
  - Lets workers carry items and spend money.
  - Feature leaves: Snack, Coffee, item catalog, worker balance, service purchase.
- [WORKERS.Family] Housing, families, and children
  - Forms households and affects family happiness/upkeep.
  - Feature leaves: personal houses, spouse pairing, family child slots, next-child readiness, child stages/growth, child education coverage, child visuals, family stress, household upkeep.
- [WORKERS.SocialSignals] Structured social signal layer
  - Records important resident/city events as topic, tone, strength, source, place, reason, and day facts.
  - Feature leaves: thought signals, litter signals, daily-experience signals, topic-opinion signals, City Hall complaint/decision signals, Noosphere insight aggregation, current social-leadership score/status from city links.
- [WORKERS.Thoughts] Active thoughts
  - Records current worries/reactions and priority life thoughts, while emitting non-duplicating social signals.
  - Feature leaves: need thoughts, litter thoughts, work thoughts, service thoughts, family thoughts, affect thoughts.
- [WORKERS.Experience] End-of-day lived experience
  - Summarizes each resident's daily factors and social-signal factors into final positive/negative verdicts, then emits daily raw-material social signals.
  - Feature leaves: score, confidence, accumulated litter exposure, dominant reason, counterpoint, "Пережитый опыт" card.
- [WORKERS.Knowledge] Personal knowledge and memories
  - Creates, expires, burns, and displays resident memory entries while separating building facts from rumors/opinions/experiences.
  - Feature leaves: factual building memories, rumor memories, source reason, expiry, displayability, affect-biased building judgement, knowledge tab.
- [WORKERS.Rumors] Rumors and knowledge sharing
  - Copies and mutates knowledge through resident interactions.
  - Feature leaves: idle dialogue, coworker sharing, service co-presence, family sharing, rumor iterations.
- [WORKERS.ConversationTopics] Player conversation topic registry
  - Stores arbitrary player topics separately from buildings and resident memories.
  - Feature leaves: topic key, display text, mention counts, opinion counts, first association aliases.
- [WORKERS.TopicOpinions] Arbitrary topic opinions
  - Forms positive/negative resident opinions about player-provided conversation topics.
  - Feature leaves: topic stance, source trust, contradiction, daily mood influence, associated social signals, topic registry link, opinion metadata.
- [WORKERS.Complaints] City Hall requests
  - Groups resident complaints and turns accepted requests into city goals.
  - Feature leaves: complaint signals, negative signal clusters, public concerns, signer groups, acceptance, rejection, public promises, expiry, trust reward/penalty, request goal HUD.
- [WORKERS.SocialRequests] Player-guided social introductions
  - Lets City Hall request a conversation topic for two residents.
  - Feature leaves: topic input, social scene, voice/feedback, relationship introduction.

### [NOOSPHERE] Knowledge UI, City Memory, And Meaning Space

- [NOOSPHERE.Screen] Noosphere HUD
  - Shows citywide knowledge/memory events, social signal topic summaries, and summary panels.
  - Feature leaves: event log rows, knowledge received/burned/canonized states, city experience summary, daily social signal topics, tone split, recurring reasons, tension sources.
- [NOOSPHERE.Canon] Citywide knowledge canon
  - Promotes repeated resident knowledge into permanent city-level knowledge.
  - Feature leaves: canon threshold, permanent entry, source aggregation, Noosphere event.
- [NOOSPHERE.CityExperience] Citywide lived experience
  - Aggregates all residents' daily experiences into city-level mood.
  - Feature leaves: score, confidence, consensus, tension, dominant reason, counterpoint.
- [NOOSPHERE.Dive] Fullscreen 3D Noosphere dive
  - Shows a deeper animated 3D meaning space from the same knowledge/opinion/social-signal data.
  - Feature leaves: fade transition, render texture, orbit rings, knowledge words, social signal words, close/escape return.
- [NOOSPHERE.Vision] Fullscreen city meaning view
  - Shows prioritized city insights from education pressure, family readiness, strong affect states, shared experience, social signals, and canon knowledge.
  - Feature leaves: slow-time entry, insight cards, source dots, affect cause-state-thought-topic chain, resident heritage marker in clarity overlay, journal bridge, close/escape return.
- [NOOSPHERE.Visuals] Noosphere animation
  - Draws pulsing nodes, labels, connections, and state transitions.
  - Feature leaves: node animation, edge animation, received/burned/canonized color states.
- [NOOSPHERE.Snapshots] Internal day-start memory archive
  - Silently stores copied Noosphere state at game start/day 1 and each later day start.
  - Feature leaves: knowledge events, social signals, city experience, canon, resident cognition, resident heritage/race/traits/weaknesses/affects, dive meanings, vision insights, visual-node state.

### [GOVERNANCE] Trust, City Hall, Complaints, And Decisions

- [GOVERNANCE.CityHall] City Hall building and UI
  - Player-facing hub for resident requests and city upgrades.
  - Feature leaves: quick HUD button, request list, request detail, accept/reject buttons, upgrade tab.
- [GOVERNANCE.Trust] City trust score
  - Tracks resident confidence in city decisions.
  - Feature leaves: trust HUD, trust labels, social-signal formula, promise success/failure effects, request penalties, completion rewards, upgrade gates.
- [GOVERNANCE.Commands] City Hall runtime commands
  - Keeps City Hall and upgrade decision logic outside UI screens.
  - Feature leaves: accept request command, reject request command, purchase upgrade command.
- [GOVERNANCE.Goals] Accepted city goals
  - Turns accepted requests into visible timed goals.
  - Feature leaves: goal marker, goal timer, success/failure feedback, city hall attention marker.
- [GOVERNANCE.Upgrades] Governance bridge to economy
  - Uses trust as a prerequisite for city upgrade purchases.
  - Feature leaves: required trust, purchased upgrade state, locked/available/purchased statuses.

### [UI] HUD, Management Screens, Menus, And Localization

- [UI.TopHud] Always-visible HUD
  - Shows money, population, trust, time/speed, weather/day titles, and quick overlays.
  - Feature leaves: money panel, trust panel, population panel, speed controls, day title, game-start title fade.
- [UI.QuickHud] Context quick HUDs
  - Shows focused data for selected trucks, buses, buildings, workers, and cells.
  - Feature leaves: truck quick HUD, local bus quick HUD, driver quick HUD, building quick HUD, cell quick HUD.
- [UI.FleetCanvas] Management screens
  - Full-screen/overlay management UI for fleet, workers, shifts, build, resources, economy, map, social graph, City Hall, Noosphere.
  - Feature leaves: tab rows, cards, scroll lists, buttons, selection rows, shared UI factory, leader-centered city social graph.
- [UI.BuildScreen] Build screen
  - Player build catalog and placement command UI.
  - Feature leaves: category tray, build cards, costs, unlock pulse, hotkeys.
- [UI.WorkersScreen] Workers screen
  - Resident list and detail tabs.
  - Feature leaves: profile race/personality card, resident list race badge, social links, thoughts, knowledge, inventory, daily opinion card.
- [UI.ShiftsScreen] Staffing screens
  - Shift assignment and vacancy management.
  - Feature leaves: transport/logistics tabs, building slots, worker options, vacancy flow.
- [UI.EconomyScreen] Economy and trade screens
  - Treasury, taxes, trade policies, resource order controls.
  - Feature leaves: taxes panel, trade rows, resource/action dropdowns, order placement.
- [UI.MapScreen] Regional map screen
  - Shows external cities, route states, trade route building, and preview.
  - Feature leaves: pixel map, route preview, city selection, build route button.
- [UI.MainMenu] Main menu, options, and patch notes
  - Launches modes and exposes graphics/sound/patch-note panels.
  - Feature leaves: new game/tutorial buttons, loading bar, graphics options, sound options, patch notes.
- [UI.Localization] Text localization
  - Resolves Russian/English UI text with JSON override support.
  - Feature leaves: built-in dictionary, JSON table, reverse lookup, localized content data.

### [TUTORIAL] Tutorial And New Game Progression

- [TUTORIAL.Flow] Tutorial windows and goals
  - Guides the player through the current Tutorial-mode path.
  - Feature leaves: tutorial windows, goal HUD, camera focus, panel actions, goal completion.
- [TUTORIAL.Building] Build-first tutorial path
  - Teaches required buildings and roads in sequence.
  - Feature leaves: road building, Warehouse/Motel/Parking core, Lumberjack Camp, service buildings, local bus stops, Docks, build unlocks.
- [TUTORIAL.Hiring] Worker hiring and assignments
  - Teaches hiring/shift assignment/vacancy controls.
  - Feature leaves: hire worker, assign worker, transport/logistics flow.
- [TUTORIAL.NewGameProgression] Non-tutorial staged unlocks
  - Keeps New Game from unlocking all build tools at once.
  - Feature leaves: starter tools, later building layers, unlock feedback.

### [AUDIO] Audio, Music, Ambience, And Sound Options

- [AUDIO.Catalog] Audio catalog
  - Loads generated and curated clips from Resources.
  - Feature leaves: generated SFX, nature ambience, footsteps, clip lookup.
- [AUDIO.Runtime] Runtime sound playback
  - Plays UI, truck, worker, environment, and music sounds.
  - Feature leaves: UI clicks, construction sounds, footsteps, ambience, music.
- [AUDIO.Options] Sound options
  - Lets player adjust categories.
  - Feature leaves: volume rows, sliders, mute/apply state, main-menu sound panel.

### [TESTS] Tests, Tools, And Safety Nets

- [TESTS.Editor] Unity editor smoke tests
  - Guards high-risk systems with focused smoke tests.
  - Feature leaves: world generation, road build, transport/trade, vacancy/tutorial checks.
- [TESTS.Tools] Local scripts and CI checks
  - Runs project sanity checks outside the editor where possible.
  - Feature leaves: `tools/check-all.ps1`, `tools/check-line-count.ps1`, diff whitespace, mojibake scans.
- [TESTS.CI] GitHub workflow
  - Runs project sanity in CI.
  - Feature leaves: `.github/workflows/project-sanity.yml`.

## Cross-System Links

### High-Impact Links

- [CORE.Loop] ticks nearly everything: [WORLD], [TRANSPORT], [WORKERS], [ECONOMY], [GOVERNANCE], [NOOSPHERE], [UI], [AUDIO].
- [WORLD.Placement] constrains [BUILD.Buildings], [TRANSPORT.Pathing], [WORKERS.Movement], [WORLD.Footpaths], and [WORLD.Litter].
- [TRANSPORT.RoadGrid] feeds [TRANSPORT.Trucks], [TRANSPORT.LocalBus], [BUILD.Roads], [WORLD.Locations], and disconnected-building UI warnings.
- [WORKERS.Shifts] links [WORKERS.Professions], [TRANSPORT.Trucks], [TRANSPORT.LocalBus], [WORLD.Locations], [ECONOMY.Production], and [WORLD.Litter].
- [WORKERS.Personality] feeds [WORKERS.Needs] leisure choice, [WORKERS.Thoughts] affect thoughts, [WORKERS.Knowledge] opinion bias, [NOOSPHERE.Vision], and [NOOSPHERE.Snapshots].
- [ECONOMY.Treasury] is touched by [BUILD.Buildings], [BUILD.Roads], [ECONOMY.Trade], [WORKERS.Hiring], [WORKERS.Needs], and [ECONOMY.Upgrades].
- [GOVERNANCE.Trust] gates [ECONOMY.Upgrades] and is changed by [WORKERS.Complaints], public promises, and daily [WORKERS.SocialSignals].
- [WORLD.Litter] is produced by [WORKERS.Movement]/crowds, perceived by [WORKERS.Thoughts]/[WORKERS.SocialSignals]/[WORKERS.Experience], cleaned by [WORKERS.Professions]/Cleaner, and tuned by [ECONOMY.Upgrades].
- [WORKERS.SocialSignals] is written by [WORKERS.Thoughts], [WORLD.Litter], [WORKERS.Experience], [WORKERS.TopicOpinions], and [WORKERS.Complaints]; it feeds [WORKERS.TopicOpinions], [GOVERNANCE.Trust], [WORKERS.Complaints], [ECONOMY.Upgrades], [NOOSPHERE.Screen], and [NOOSPHERE.Dive].
- [WORKERS.Knowledge], [WORKERS.Rumors], [WORKERS.ConversationTopics], [WORKERS.TopicOpinions], [WORKERS.SocialSignals], [WORKERS.Personality], and [WORKERS.Experience] feed [NOOSPHERE.Screen], [NOOSPHERE.Canon], [NOOSPHERE.Vision], and [NOOSPHERE.Dive].
- [NOOSPHERE.Snapshots] is triggered by [CORE.Loop] and reads [WORKERS] cognition/personality/social state plus [NOOSPHERE] derived layers without showing player-facing output.
- [TUTORIAL.Flow] depends on [BUILD], [WORKERS.Shifts], [TRANSPORT], [ECONOMY], and [UI]; any serious gameplay change should be checked against `ai/tutorial-scenario.md`.
- [UI.FleetCanvas] displays and mutates many systems, but should not own simulation rules when a pure service or runtime partial already exists.

### Dependency Matrix

| From | Depends On | Why |
| --- | --- | --- |
| [BUILD.Roads] | [TRANSPORT.RoadGrid], [WORLD.Placement], [ECONOMY.Treasury], [UI.BuildScreen] | Validates road cells, spends money, and updates build UI. |
| [BUILD.Buildings] | [WORLD.Placement], [WORLD.Locations], [ECONOMY.Treasury], [UI.BuildScreen] | Places locations, blocks cells, charges construction cost. |
| [TRANSPORT.Trucks] | [TRANSPORT.RoadGrid], [TRANSPORT.Pathing], [ECONOMY.Resources], [WORKERS.Shifts] | Trucks need roads, cargo, drivers, and phase logic. |
| [TRANSPORT.LocalBus] | [TRANSPORT.RoadGrid], [WORKERS.Movement], [WORKERS.Shifts], [ECONOMY.Treasury] | Bus service moves workers and may collect fares. |
| [ECONOMY.Trade] | [TRANSPORT.Regional], [TRANSPORT.Trucks], [ECONOMY.Resources], [UI.EconomyScreen] | Trade consumes stock, route state, vehicles, and UI orders. |
| [WORKERS.Needs] | [WORLD.Locations], [WORKERS.Movement], [ECONOMY.Treasury], [WORKERS.Thoughts] | Needs select services, move workers, spend money, create thoughts. |
| [WORKERS.Personality] | [WORKERS.Needs], [WORKERS.Thoughts], [WORKERS.Knowledge], [NOOSPHERE.Vision], [UI.FleetCanvas] | Character traits, weaknesses, and affects steer interpretation, service choice, thoughts, knowledge bias, and resident state display. |
| [WORKERS.SocialSignals] | [WORKERS.Thoughts], [WORLD.Litter], [WORKERS.Experience], [WORKERS.TopicOpinions], [WORKERS.Complaints], [GOVERNANCE.Trust], [ECONOMY.Upgrades], [NOOSPHERE] | Important resident/city events become shared topic/tone/strength facts. |
| [WORKERS.Complaints] | [WORKERS.Thoughts], [WORKERS.SocialSignals], [GOVERNANCE.CityHall], [GOVERNANCE.Trust], [UI.FleetCanvas] | Complaints arise from resident state and negative signal clusters, then become city decisions. |
| [NOOSPHERE.Canon] | [WORKERS.Knowledge], [WORKERS.Rumors], [WORKERS.ConversationTopics], [WORKERS.TopicOpinions] | City knowledge is aggregated from resident facts/rumors/opinions. |
| [ECONOMY.Upgrades] | [GOVERNANCE.Trust], [ECONOMY.Treasury], [WORLD.Litter], [BUILD.Catalog], [WORKERS.Complaints], [WORKERS.SocialSignals] | Upgrades are bought with money/trust, can be highlighted by live problems, and modify gameplay helpers. |
| [TUTORIAL.Flow] | [BUILD], [TRANSPORT], [WORKERS], [ECONOMY], [UI] | Tutorial is a guided path through real gameplay systems. |

## Update Rules

- Add a new top-level system only when it has its own lifecycle, UI surface, or major data ownership.
- Add a subsystem when multiple files/features cluster around a stable responsibility.
- Add feature leaves as one-line facts, not implementation essays.
- When a new feature bridges systems, update both the tree leaf and `Cross-System Links`.
- Keep concrete file ownership in `ai/systems-map.md`; keep this file conceptual and navigational.

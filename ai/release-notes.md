# Release Notes Memory

Purpose: stable baseline for in-game Patch Notes. Use this file to compare the last documented public version with current implemented code when the user asks to update patch notes.

Rules:

- Keep entries grouped by version.
- Record shipped/implemented player-facing features, not every internal fix.
- When preparing a new version, compare this baseline against current code and recent `ai/work-log.md`.
- Update the in-game Patch Notes text in `Assets/Resources/GameData/patch-notes.json`.
- `Assets/Scripts/Runtime/UI/GameBootstrap.MainMenuHud.cs` keeps a hardcoded fallback only for missing/invalid JSON.

## Lo-fi Delivery Co. v.0.0.4

Status: in development. Main Menu version label has been bumped to `Lo-fi Delivery Co. v.0.0.4`.

### Release Tracking

- Use `v.0.0.2` as the previous public baseline.
- When the user asks to update Patch Notes for `v.0.0.4`, compare current code and recent `ai/work-log.md` entries against the `v.0.0.2` section below.
- Record only player-facing mechanics, content, UX, and visual changes here. Do not include internal refactors, line-count work, temporary failed attempts, or debug-only implementation details unless they matter to players.

### New Since v.0.0.2

#### Build And Progression

- The Build menu now lives as a bottom dock with five icon categories and an animated tool tray.
- New Game starts with only the one-cell road, Warehouse, Motel, and City Hall, then unlocks later buildings in clear construction layers.
- Newly unlocked buildings pulse in the Build dock and appear in the event feed.
- Road construction now uses a start/end segment flow with an animated construction wave instead of instant single-click road placement.
- Buildable buildings now have Treasury costs, price badges, no-funds states, money popups, and ledger/feed feedback when construction succeeds.
- City Hall construction requests now follow the same staged order and can eventually suggest every available building.
- City Hall now includes trust-gated city upgrades that can reduce litter growth, improve cleaner coverage, discount construction, extend request deadlines, or soften trust penalties.
- Build previews, road-access warnings, road visuals, and road markings are easier to read on uneven terrain and at night.

#### Residents And City Life

- Residents now have personal money, education, profession levels, dynamic vacancy salaries, and fixed-term contracts.
- Food, Sleep, and Leisure are handled by urgency during free time, with fallback activities when a resident cannot afford a normal service.
- Kiosk Snack and Coffee can be carried in personal inventory and auto-used before needs become critical.
- Residents now have thoughts, opinions, social links, families, children, child-care pressure, school pressure, and lived-experience summaries in the Residents screen.
- Children now progress through life stages, and Kindergarten, Primary School, and Secondary School capacity affects family happiness and city complaints.
- City Hall conversation topics and first visits to buildings now create time-limited personal knowledge shown in the Residents Knowledge tab.
- Knowledge can spread between residents through idle chats, shared services, coworker shifts, City Hall introductions, and family formation, with highlighted subjects in dialogue bubbles.
- The Noosphere HUD records citywide knowledge events, social-signal topic summaries, citywide lived experience, received/expired/burned memories, and deeper 3D meaning-space visuals.
- Idle residents can chat in the world with readable bubbles and spatial vocalizer sounds.
- Idle movement is safer: residents should spread out more reliably instead of stacking on one tile.

#### Services, City Hall, And Trade

- Labor Exchange now owns vacancy posting, applicants, and automatic staffing, so the permanent Vacancies top-HUD entry is gone in normal play.
- City Hall accepts citizen requests, turns accepted requests into 24-hour city goals, and changes city Trust on success, rejection, or expiry.
- City Hall can create a social-introduction request where the player chooses a topic for two residents.
- Public concerns can now grow out of repeated resident problems, and upgrade cards can highlight problems they help solve.
- Cleaning Depot and the Cleaner profession add staffed street-litter cleanup, with coverage previews and visible cleanup work in town.
- Parking automatically provides truck and bus slots; local buses need at least two stops and skip unreachable route targets.
- Trade moved to policy-based resource controls, while Docks and regional routes drive river and land trade.
- Motel, Bar, Canteen, Kiosk, Kindergarten, Primary School, and Secondary School are tied more directly into resident needs, fees, families, child care, and education.

#### HUD And Interaction

- Resident, City Hall, Motel, and Warehouse microHUDs were rebuilt as compact status cards with only the most useful actions and numbers.
- Worker and building microHUDs now draw a line to the selected person or building tile, making the map target easier to find.
- Motel has an animated guest submenu with its own scroll, and clicking a guest opens that resident in the Residents screen.
- Opening a different microHUD closes the current one, and the Event Feed now sits below other HUD panels.
- The top HUD now shows population and city Trust near Treasury, while compact event messages stay readable without covering the play area.
- Building demolition now asks for confirmation before removing a structure.
- Build controls now support clearer keyboard-number selection and right-click/Escape cancellation through menu/tool layers.

#### Atmosphere, Audio, And Map

- The Bar can now be entered as a separate large interior scene that pauses the town and returns with a fade.
- Bar patrons idle, talk, drink, dance, and use their own room ambience and vocalizer sounds.
- Entering the Bar interior mutes the outside city soundscape, and the river ambience is now positioned on the map instead of playing everywhere equally.
- Bar and Gambling Hall world buildings can use imported models with visible seating, animated doors, night lighting, and staged construction pop-ins.
- Regional Map is a fullscreen pixel-art screen that pauses simulation, shows external cities, and links built routes into Trade.
- Tutorial text and flow now follow the current build-first start, bottom Build dock, Labor Exchange, services, buses, Docks, and trade policies.
- Town presentation is warmer and busier: imported ground/water/tree textures, generated low-poly tree assets, better lighting, smoother roads, richer service details, new relaxed UI/build/truck sounds, and more small life in the scene.

## Lo-fi Delivery Co. v.0.0.2

Status: previous documented prototype version.

### New Since v.0.0.1

- WOMEN ADDED!!!

### Workers

- Workers now have portraits, gender, needs, money, jobs, and perks.
- Food, sleep, and leisure are now part of the worker life cycle.
- The Alcoholism and Gambler perks steer leisure behavior without temporary modifiers.

### Production

- Lumberyard is now an active production chain: workers chop trees, carry logs, and plant new saplings.
- Trees in forest zones grow over days, creating a renewable wood source.

### Transport

- Added a local bus system with stops, bus drivers, routes, and worker passenger trips.
- Buses charge fares, store route revenue, and transfer it into Parking.
- Trucks move resources between buildings and can run intercity trade routes.

### Economy And Trade

- Economy is split into Taxes and Trade tabs.
- Daily taxes now collect money from building banks into the town Treasury.
- Trade orders let the town buy and sell resources through intercity runs.

### Town Services

- Bar, Canteen, Gambling Hall, Gas Station, and City Park now have clearer roles in worker and vehicle life.
- Service buildings are more visually distinct, especially at night.
- Added an Event Feed for important town events.

### World And Atmosphere

- The map is larger and denser, with more trees, bushes, forest zones, water, hills, and ambient life.
- Lighting, fog, water, roads, lanterns, clouds, and ambient details were improved.
- Added a rebuilt Tutorial flow plus a regular New Game start that skips tutorial gates.

### Main Menu And Patch Notes

- Main Menu version label now shows `Lo-fi Delivery Co. v.0.0.2`.
- In-game Patch Notes now include a dedicated `v.0.0.2` section focused on changes since `v.0.0.1`.

## Lo-fi Delivery Co. v.0.0.1

Status: previous documented prototype baseline.

### Core Loop

- Runtime-generated logistics town on a grid.
- Main Menu now uses player-facing Tutorial and New Game starts; legacy debug/clear start modes are no longer player-facing.
- Top HUD tabs provide access to Fleet, Workers, Shifts, Resources, Economy/Trade, Build, and Regional Map.
- Debug tooling includes clickable cell/building/truck/worker quick HUDs and `debug.log` diagnostics.

### Build And World Setup

- Build menu supports roads and buildable structures including Motel, Sawmill, Canteen, Furniture Factory, and related service/production buildings.
- Build preview shows occupied footprint cells, driveway cell, and supports rotation with `R`.
- Generated world includes roads, buildings, river/water, beach/shoreline, edge highway, bus stop, terrain variation, and decorative props.

### Workers

- Workers can be hired from the Workers HUD.
- New hires arrive by bus before checking in.
- Workers have generated low-poly portrait UI, perks, salary, balance, and focus controls.
- Workers have need timers for Food, Sleep, and Leisure shown in the Workers HUD.
- Worker daily/life behavior can route them through work, Canteen, Bar/leisure, Motel sleep, and idle.
- Need changes and lifecycle decisions are logged to `debug.log`.

### Production And Resources

- Resource set includes Logs, Boards, Cotton, Textile, and Furniture.
- Forest produces Logs.
- Sawmill converts Logs into Boards.
- Furniture Factory converts Boards plus Textile into Furniture.
- Warehouse stores finished resources and supports the resource loop.
- Resources HUD has Warehouse and Production tabs.
- Production workers are assigned by building rather than by logistics shifts and normally work 08:00-18:00.

### Logistics, Fleet, And Trade

- Fleet HUD supports buying trucks, selecting trucks, assigning available workers/drivers, and viewing truck state.
- Trucks display fuel, cargo amount/resource name, route/status, and can be followed by camera.
- Trade HUD supports explicit order creation with Resource, Buy/Sell action, amount stepper, Place Order, and Active Orders list.
- Intercity trade runs use the edge highway; trucks leave/return at highway edges and can carry bought cargo back.
- Trade/highway diagnostics include lane/path and bus/truck interaction logs.

### Racing

- Trade trucks can expose `Join the race`.
- Racing minigame has separate road scene behavior, truck controls, collisions, finish sequence, SFX/music hooks, and payout/bonus handling.

### Regional Map

- Regional Map opens from HUD/main controls.
- 3x3 conceptual region map includes current town and placeholder neighboring regions such as Textile/Cotton/trade route regions.
- Current region preview sketches the town context with river/highway/town/forest cues.

### World Ambience

- Ambient buses travel along the edge highway.
- Decorative/environment systems include benches, flowers, berry bushes, bees, birds, cats, fish, river/water effects, particles, roadside dust, lantern moths, and misc trees.
- Night/day gates exist for several ambient systems.
- Some ambient audio was intentionally disabled; truck loop audio should only play when a truck is active/occupied.

### Main Menu And Patch Notes

- Main Menu shows manual version label `Lo-fi Delivery Co. v.0.0.1` in the bottom-right corner.
- Main Menu has a `Patch Notes` button.
- Patch Notes window is scrollable, localized in English/Russian, and uses version-grouped sections.

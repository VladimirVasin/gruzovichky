# Release Notes Memory

Purpose: stable baseline for in-game Patch Notes. Use this file to compare the last documented public version with current implemented code when the user asks to update patch notes.

Rules:

- Keep entries grouped by version.
- Record shipped/implemented player-facing features, not every internal fix.
- When preparing a new version, compare this baseline against current code and recent `ai/work-log.md`.
- Update the in-game Patch Notes text in `Assets/Resources/GameData/patch-notes.json`.
- `Assets/Scripts/Runtime/UI/GameBootstrap.MainMenuHud.cs` keeps a hardcoded fallback only for missing/invalid JSON.

## Lo-fi Delivery Co. v.0.0.3

Status: in development. Main Menu version label has been bumped to `Lo-fi Delivery Co. v.0.0.3`.

### Release Tracking

- Use `v.0.0.2` as the previous public baseline.
- When the user asks to update Patch Notes for `v.0.0.3`, compare current code and recent `ai/work-log.md` entries against the `v.0.0.2` section below.
- Record only player-facing mechanics, content, UX, and visual changes here. Do not include internal refactors, line-count work, temporary failed attempts, or debug-only implementation details unless they matter to players.

### New Since v.0.0.2

#### Build And Roads

- The Build menu is now organized into intent-based categories: Roads & Transport, Logistics & Trade, Production, Housing & Civic, and Services & Leisure.
- The regular one-cell road remains the active player road tool, while the two-way road segment tool is visible as temporarily under rework.
- Two-way road segment work still exists behind the disabled tool: click-start/click-finish, Shift axis constraint, lane footprints, and center markings.
- Build-mode previews are clearer at night thanks to a warm cursor light with blocked-placement tint.
- New buildings automatically create valid driveway/access road cells when possible, reducing manual cleanup around entrances.
- Road visuals and markings now follow uneven terrain more smoothly instead of appearing buried, floating, or stair-stepped.

#### Workers And Needs

- New workers start with a random personal balance, making early need/service behavior less uniform.
- Workers resolve Food, Sleep, and Leisure by urgency and can handle due needs during free/off-shift time instead of waiting for evening.
- Workers who cannot afford Canteen food or Motel sleep can use fallback activities such as trash meals or bench sleep and receive the temporary `I Have Fallen` effect.
- Idle workers choose a wider range of city interest points, so the town feels less clustered around one building.
- Workers now have Basic/Vocational/Higher education, profession levels, dynamic vacancy salaries, and fixed-term contracts.
- Labor Exchange can post vacancies, receive applicants, and auto-staff its higher-education clerk slot when possible.
- The permanent top-HUD Vacancies entry has been removed from normal play; staffing is now accessed through Labor Exchange, with manual assignment retained as an override/tutorial path.
- Workers now have bounded recent thoughts, current-priority concerns, and life-opinion summaries shown in the Residents HUD.
- Workers can own simple inventory items. Kiosk-sold Snack and Coffee are stored on the worker and auto-used before Food/Sleep needs become critical.
- Worker social relationships now have visible memories, relationship strength, decay, and a Socialite perk effect; idle and coworker contact can grow friendships over time.
- Workers can form families in Personal Houses, share household pressure, have children, and care about child-care coverage.
- Automatic worker migration is more persistent and reacts to open-vacancy pressure while still respecting caps and satisfaction pressure.

#### Transport And Services

- Trade dispatch now uses the regular Truck Driver shift pool and automatically reserves an available parked truck from Parking.
- Local bus service now requires at least two local stops before it becomes active; one-stop networks show clearer warnings and no longer trap passengers in dead-wait loops.
- Parking now provides truck and bus slots automatically; separate vehicle purchases are no longer required for the current fleet flow.
- Local buses now tolerate disconnected stops by starting from reachable stops and skipping unreachable route targets.
- Trade moved to a dedicated Trade screen with per-resource `No trade`, `Buy up to`, and `Sell surplus` policies.
- Docks and generated regional routes now drive river/land trading; ships and merchant trucks only act on active policies with built routes.
- Fuel, Food, and Alcohol were removed as trade/storage resources; services now operate directly through worker needs, fees, and truck refuel orders.
- Service prices were lowered for the current economy balance: Motel, Bar, and Canteen charge `$8`, while Kiosk Snack/Coffee costs `$4`.
- Kindergarten is now a buildable service that provides staffed child-care slots for worker families.
- City Hall is now a buildable civic building with citizen requests, accept/reject choices, visible goal timers, completion rewards, expiry/rejection penalties, and a city-wide Trust score.
- City Hall can generate a special social-introduction request where the player suggests a topic and watches a short resident conversation resolve into a relationship outcome.

#### Regional Map

- The Regional Map is now a borderless fullscreen pixel-art map that pauses simulation while open.
- The map currently shows the player's town plus two visible external cities with resource buy/sell tables.
- Trade-route lines appear only after the player builds a route, and built city cards can open the Trade screen directly.
- Regional labels/descriptions use localized display strings where available.

#### User Experience And Presentation

- Main Menu graphics settings now expose live `0..100` controls, effect toggles, and a reset-to-defaults action for post-processing.
- Main Menu sound options expose music, curated ambience, worker footsteps, and the kept generated SFX with preview/reset/volume controls.
- The rebuilt Tutorial now follows the current empty-town flow through roads, core buildings, Labor Exchange, services, warehouse loaders, local buses, taxes, Regional Map, Docks, and Trade policy setup.
- Service/town visuals received more atmosphere: smoother roads, building/perimeter night lighting, warmer lights, richer regional map presentation, and small city-life details.
- Building demolition now has a confirmation modal, and Event Feed notifications render as compact top-right toasts.
- The HUD now includes current population beside time and treasury.
- The Workers screen has been rebuilt as a Residents HUD with profile, needs, thoughts, inventory, work, and social-link views.
- A dedicated `Social` / `Связи` HUD shows citizen relationship graphs with focused views, filters, animated nodes, and hover details.
- Residents can now show small in-world idle conversation bubbles with synchronized procedural voice syllables.
- Debug/User tooling is less intrusive for players: noisy debug traces are verbose-only, while important F9 tools remain available for testing.

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

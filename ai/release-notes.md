# Release Notes Memory

Purpose: stable baseline for in-game Patch Notes. Use this file to compare the last documented public version with current implemented code when the user asks to update patch notes.

Rules:

- Keep entries grouped by version.
- Record shipped/implemented player-facing features, not every internal fix.
- When preparing a new version, compare this baseline against current code and recent `ai/work-log.md`.
- Update the in-game Patch Notes text in `Assets/Scripts/Runtime/UI/GameBootstrap.MainMenuHud.cs`.

## Lo-fi Delivery Co. v.0.0.3

Status: in development. Main Menu version label has been bumped to `Lo-fi Delivery Co. v.0.0.3`.

### Release Tracking

- Use `v.0.0.2` as the previous public baseline.
- When the user asks to update Patch Notes for `v.0.0.3`, compare current code and recent `ai/work-log.md` entries against the `v.0.0.2` section below.
- Record only player-facing mechanics, content, UX, and visual changes here. Do not include internal refactors, line-count work, temporary failed attempts, or debug-only implementation details unless they matter to players.

### New Since v.0.0.2

#### Build And Roads

- Two-way road building now uses a click-start/click-finish segment flow, with Shift constraining segments to one dominant axis.
- Build-mode previews are clearer at night thanks to a warm cursor light and ground glow; blocked placement is tinted red.
- New buildings automatically create valid driveway/access road cells when possible, reducing manual cleanup around entrances.
- Road visuals and markings now follow uneven terrain more smoothly instead of appearing buried, floating, or stair-stepped.

#### Workers And Needs

- New workers start with a random personal balance, making early need/service behavior less uniform.
- Workers resolve Food, Sleep, and Leisure by urgency and can handle due needs during free/off-shift time instead of waiting for evening.
- Workers who cannot afford Canteen food or Motel sleep can use fallback activities such as trash meals or bench sleep and receive the temporary `I Have Fallen` effect.
- Idle workers choose a wider range of city interest points, so the town feels less clustered around one building.

#### Transport And Services

- Trade dispatch now uses the regular Truck Driver shift pool and automatically reserves an available parked truck from Parking.
- Local bus service now requires at least two local stops before it becomes active; one-stop networks show clearer warnings and no longer trap passengers in dead-wait loops.
- Bought city buses now arrive from the edge highway and drive into Parking before becoming operational.
- Warehouse/service logistics were rebalanced with larger buffers and smarter delivery target selection for Fuel, Food, and Alcohol.

#### Regional Map

- The Regional Map is now a fullscreen parchment-style map that pauses simulation while open.
- Known regions use schematic mini-map previews, settlement-style markers, route lines, and a hidden detail panel that appears after selecting a city.
- Regional labels/descriptions use localized display strings where available.

#### User Experience And Presentation

- Main Menu graphics settings now expose live `0..100` controls, effect toggles, and a reset-to-defaults action for post-processing.
- The rebuilt User tutorial has been tightened around freight, services, buses, taxes, trade, racing, and a final Demo Complete step.
- Service/town visuals received more atmosphere: smoother roads, stronger building/perimeter night glow, warmer lights, richer regional map presentation, and small city-life details.
- Debug/User tooling is less intrusive for players: noisy debug traces are verbose-only, while important F9 tools remain available for testing.

## Lo-fi Delivery Co. v.0.0.2

Status: previous documented prototype version.

### New Since v.0.0.1

- WOMEN ADDED!!!

### Workers

- Workers now have portraits, gender, skills, needs, effects, and perks.
- Food, sleep, and leisure are now part of the worker life cycle.
- The Alcoholism perk now has distinct Drunk and Hangover behavior.

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
- Added a rebuilt User tutorial flow and the free New Game Clear mode.

### Main Menu And Patch Notes

- Main Menu version label now shows `Lo-fi Delivery Co. v.0.0.2`.
- In-game Patch Notes now include a dedicated `v.0.0.2` section focused on changes since `v.0.0.1`.

## Lo-fi Delivery Co. v.0.0.1

Status: previous documented prototype baseline.

### Core Loop

- Runtime-generated logistics town on a grid.
- Main Menu includes `New Game Debug`; `New Game User` is temporarily disabled and shown as `Work in progress`.
- Top HUD tabs provide access to Fleet, Workers, Shifts, Resources, Economy/Trade, Build, and Regional Map.
- Debug tooling includes clickable cell/building/truck/worker quick HUDs and `debug.log` diagnostics.

### Build And World Setup

- Build menu supports roads and buildable structures including Motel, Sawmill, Canteen, Furniture Factory, and related service/production buildings.
- Build preview shows occupied footprint cells, driveway cell, and supports rotation with `R`.
- Generated world includes roads, buildings, river/water, beach/shoreline, edge highway, bus stop, terrain variation, and decorative props.

### Workers

- Workers can be hired from the Workers HUD.
- New hires arrive by bus before checking in.
- Workers have generated low-poly portrait UI, base stats (`Driving`, `Stamina`, `Production`, `Logistics`), skill hover descriptions, salary, balance, and focus controls.
- Workers have need timers for Food, Sleep, and Leisure shown in the Workers HUD.
- Worker daily/life behavior can route them through work, Canteen, Bar/leisure, Motel sleep, and idle.
- Need changes and lifecycle decisions are logged to `debug.log`.

### Production And Resources

- Resource set includes Logs, Boards, Cotton, Textile, Furniture, Fuel, Food, and Alcohol.
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

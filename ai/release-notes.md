# Release Notes Memory

Purpose: stable baseline for in-game Patch Notes. Use this file to compare the last documented public version with current implemented code when the user asks to update patch notes.

Rules:

- Keep entries grouped by version.
- Record shipped/implemented player-facing features, not every internal fix.
- When preparing a new version, compare this baseline against current code and recent `ai/work-log.md`.
- Update the in-game Patch Notes text in `Assets/Scripts/Runtime/UI/GameBootstrap.MainMenuHud.cs`.

## Lo-fi Delivery Co. v.0.0.2

Status: current documented prototype version.

### New Since v.0.0.1

- WOMEN ADDED!!!
- Forest was reworked into an early Lumberyard gameplay loop. Forest workers can now leave the building, go to real misc trees in the dense forest zone, chop them down, spawn world logs, carry those logs back to storage, and replant saplings.
- Dense-forest trees now support day-based regrowth from sapling to mature tree, creating the start of a renewable forestry loop instead of one static decorative forest patch.
- Worker simulation expanded significantly: portraits, base skills, timed needs, timed effects, perks, and the separate `States` reference HUD are now all part of the current playable build.
- Workers now also have randomized perk loadouts, more distinct female portraits, and a deeper `Alcoholism` perk tied to `Drunk` / `Hangover` behavior.
- Main management HUDs were redesigned and enlarged, especially Workers, Fleet, and Assignments / Roles, to better support the denser worker information model.
- The town now has a real local bus loop: buildable local stops, a dedicated bus-driver role, worker passenger trips, and bus-assisted Warehouse deliveries.
- Local buses charge a `$1` fare, keep their own onboard bank, and transfer that revenue into `Parking` at shift end.
- A right-side Event Feed HUD was added for important gameplay notifications.
- Economy was expanded with a separate `Taxes / Trade` tabbed window and a daily `10%` tax transfer from all building banks into global Treasury.
- `Bar`, `Canteen`, and `Gambling Hall` were visually separated with stronger silhouettes, larger distinct footprints, and different night-light identities.
- `F9` debug tooling was expanded so both service resources and production/storage resources can be manipulated directly during testing.
- World bootstrap was rebalanced for the larger map: the game now starts with three workers, uses a dedicated dense forest hotspot, and scales misc props/trees more aggressively for the bigger town.

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

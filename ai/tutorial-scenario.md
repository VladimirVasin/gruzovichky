# Tutorial Scenario

Last updated: 2026-05-08

Purpose: keep a plain-text scenario for the current `Tutorial` game mode so future gameplay, HUD, economy, staffing, transport, trade, or building changes can be checked against the actual teaching flow.

Scope: `Tutorial` means only the player-facing `Обучение` mode (`GameStartMode.Tutorial`). It does not mean New Game onboarding, generic hints, patch notes, debug panels, or non-tutorial HUD copy unless those directly affect the Tutorial mode path.

Source of truth: code remains authoritative. This file is the cross-session checklist that should be compared with code before and after serious changes.

## Maintenance Rule

- Before changing a system taught by Tutorial, read this file and the Tutorial owner files in `ai/systems-map.md`.
- Serious changes must update both the implementation and this scenario in the same task when they alter player-facing Tutorial flow, prerequisites, unlock order, HUD entry points, required buildings, required resources, automation/manual-control balance, or goal text.
- If this file disagrees with code, trust code for behavior, then update this file or fix Tutorial code so the player path is coherent.
- Keep this file short. Do not paste full localized Tutorial text; record the playable sequence and important invariants.

## Current Scenario

1. Start Tutorial mode with an almost empty town, highway/intercity access, starter workers, and locked progression. Show welcome, focus the start area, then require camera controls: zoom in, zoom out, pan, and rotate.
2. Teach road building. Player opens the bottom-centered Build dock or presses `B`, selects a road tool from the category tray, places one road cell, then uses Shift-drag to place a longer road segment. Roads must connect the town to the highway.
3. Unlock and build the core: Warehouse, Motel, and Parking. Each built core building can show its own explanation. The core goal completes when all three exist; tutorial text warns that road access is required for buildings to work properly.
4. Unlock and build Lumberjack Camp near trees. Tutorial temporarily exposes the top-menu `Staffing` entry for early manual assignment. Player opens Staffing, selects the Lumberjack Camp vacancy, and assigns a lumberjack.
5. Explain production shifts and wages. Then teach freight setup: player assigns a Truck Driver shift through Staffing, and Parking automatically provides the truck from fleet capacity. No separate truck purchase is taught.
6. Show the first freight run. A staffed truck looks for useful work and can move Logs from Lumberjack Camp to Warehouse.
7. Unlock and build Labor Exchange. The Tutorial goal requires the building and one staffed clerk. From this point, staffing is framed as Labor Exchange automation; manual assignment remains an override for tutorial pacing.
8. Explain worker arrivals. Player opens Workers; the copy encourages inspecting a worker card, while the goal completes on opening the Workers panel. The tutorial starts or observes the 10-worker arrival bus. New workers arrive at the intercity stop and walk to Motel. After the arrival explanation, all tutorial vacancies unlock.
9. Explain worker life after shifts. Unlock and build service buildings: Bar, Gambling Hall, Canteen, Gas Station, and City Park. These buildings satisfy needs, support trucks, or provide leisure; paid services accumulate building-bank money.
10. Open Workers overview again, then introduce Warehouse resources/loaders. Player reviews Warehouse slots through Labor Exchange; Labor Exchange can fill them automatically, and manual override remains available if Tutorial needs a faster handoff. The active goal is three Warehouse loaders.
11. Unlock local bus stops. Tutorial text asks for two local stops; the goal completes when the town has at least two. Player also reviews bus-driver staffing through Labor Exchange. Buses visit numbered stops in order and back again; rides cost `$1`, and route bank goes to Parking.
12. Teach Economy and Taxes. Service buildings hold money in building banks; at `00:00`, the town collects the selected percent into Treasury. Player opens Economy -> Taxes and sets the rate to `15%`.
13. Teach Regional Map. Player opens Map and builds the river trade route to the known city that sells Textile.
14. Unlock and build Docks on the riverbank, then review/staff the Docks worker slot through Labor Exchange. Docks receive imported goods from ships and export Warehouse cargo; local trucks move goods between Docks and Warehouse.
15. Teach Trade policy. Player opens Trade, selects Textile, and sets policy to `Buy up to`; river ships can then bring Textile and trucks move it to Warehouse.
16. Show Demo Complete. Tutorial ends; New Game is the non-tutorial mode with tutorial windows skipped, but its build tools follow the separate New Game staged unlock progression rather than Tutorial skip unlocking every building.

## Current Invariants

- All Tutorial-only progression must be guarded by `selectedGameStartMode == GameStartMode.Tutorial` and `!isTutorialSkipped`.
- New Game build progression is not Tutorial: tutorial skip state may suppress Tutorial windows, but it must not unlock all build tools for a fresh New Game.
- Normal play does not show a permanent top-HUD Vacancies tab. The top-menu `Staffing` button appears during current Tutorial staffing-goal/manual-override moments and also remains available while the staffing screen is already open.
- Labor Exchange quick HUD opens the staffing overview. The screen may still have internal `Vacancies` method names, but player-facing Tutorial copy should talk about `Staffing`, `Кадры`, or `Биржа труда` according to context.
- Tutorial should not teach removed resources or old direct-hiring flow. Current resource/trade path is Logs/Boards/Cotton/Textile/Furniture, with Textile import taught through river trade and Docks.
- Parking provides tutorial freight vehicles from capacity; Tutorial should not tell the player to buy a truck separately.

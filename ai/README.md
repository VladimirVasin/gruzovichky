# AI Memory System

Purpose: shared project memory for AI agents working in this repository.

This folder is intentionally small. Code remains the source of truth. These files exist to reduce repeated rescans, improve planning, and keep cross-session context consistent.

## Files

- `project-overview.md`
  Stable high-level map of the project: folders, key modules, and main runtime areas.
- `system-tree.md`
  Hierarchical informational tree of project systems, subsystems, feature leaves, and cross-system links.
- `systems-map.md`
  Active systems and their main files, plus impact hints for future changes.
- `architecture-notes.md`
  Actual implemented architecture, complexity hotspots, and likely refactor seams.
- `work-log.md`
  Active and recently completed work. Use this for task state and short implementation notes.
- `release-notes.md`
  Stable baseline for in-game Patch Notes by version. Use it to compare documented releases against current code when updating patch notes.
- `tutorial-scenario.md`
  Plain-text scenario for the current `Обучение` / `GameStartMode.Tutorial` flow. Use it to keep Tutorial aligned with serious gameplay, HUD, staffing, economy, transport, trade, and building changes.

Design maps:

- `Design/worker-thought-tree.md`
  Current map of the worker thought system: cause -> affect/status -> thought -> knowledge/opinion -> social signal -> Noosphere/UI. Read it before adding or restructuring worker thoughts, affects, weakness-driven thoughts, WorkerKnowledge/Opinion links, Noosphere thought/state chains, or thought UI cases. Update it in the same task when that chain changes.
- `Design/worker-thought-influence-matrix.md`
  Design matrix for explicit thought influence rules: source thought -> WorkerOpinion -> target thought bias. Read it before changing `WorkerOpinion` feedback loops, source/target thought links, influence windows/caps, weakness/trait modifiers, or human-readable influence reasons. Update it in the same task when those rules change.

## Memory Types

Stable memory:

- `project-overview.md`
- `system-tree.md`
- `systems-map.md`
- `architecture-notes.md`

Release memory:

- `release-notes.md`

Volatile memory:

- `work-log.md`

Rule:

- stable memory is updated rarely
- release memory is updated when version labels, changelogs, release contents, or Patch Notes change
- volatile memory is updated often
- do not edit stable memory unless project reality actually changed

## Read Order For Agents

1. Read this file.
2. Read `project-overview.md`.
3. Read `system-tree.md` for broad, architectural, cross-system, or unclear tasks.
4. Read `systems-map.md`, especially `System Owner Map`, before broad code search.
5. Read `architecture-notes.md`.
6. Read `work-log.md`.
7. Read `release-notes.md` when the task involves version labels, changelogs, release contents, or Patch Notes.
8. Read `tutorial-scenario.md` when the task touches Tutorial mode or a system currently taught by Tutorial.
9. Read `Design/worker-thought-tree.md` when the task touches worker thoughts, active/pending thought formation, affect states, worker weaknesses/traits as thought inputs, WorkerKnowledge/Opinion links from thoughts, social signals from thoughts, Noosphere thought/state display, or Workers/F9 thought UI.
10. Read `Design/worker-thought-influence-matrix.md` when the task touches explicit thought influence rules, `WorkerOpinion` feedback loops, source-thought -> target-thought bias, influence windows/caps, or human-logic links between thoughts.
11. Scan only the code relevant to the requested change.

## Workflow Contract

### Before coding

- Read the memory files in the order above.
- Treat memory as a guide, not as authority over code.
- If memory and code disagree, trust code and update memory after finishing.
- Identify the affected systems before editing.
- For broad, architectural, cross-system, or unclear tasks, use `ai/system-tree.md` to understand the conceptual system tree and cross-system links before choosing owner files.
- Use `ai/systems-map.md` -> `System Owner Map` to pick the first files to inspect.
- If the affected system is taught by `Обучение` / `GameStartMode.Tutorial`, compare the change against `ai/tutorial-scenario.md` before editing.
- If the affected system changes worker cognition chain behavior or display, compare the change against `ai/Design/worker-thought-tree.md` before editing.
- If the affected system changes explicit thought influence behavior, compare the change against `ai/Design/worker-thought-influence-matrix.md` before editing.
- Write a short plan before changing code.
- Do not start editing before the plan is stated.

### Planning

- Build a short plan using system boundaries, not file-by-file trivia.
- Note likely affected files.
- For risky changes, note dependencies and verification steps before editing.
- For larger or riskier tasks, pause after the plan if the current working mode expects confirmation.

### During implementation

- Keep notes short.
- Do not paste code into memory files.
- Do not document speculative designs as implemented facts.

### Encoding Safety

- Treat all source and project text files as `UTF-8`.
- Prefer `apply_patch` for code/text edits, especially in files with Russian localization.
- Avoid whole-file rewrite scripts unless the write path is explicitly `UTF-8`.
- In PowerShell, never write project files without an explicit UTF-8 encoding.
- After localized UI/tutorial/HUD text edits, run a quick scan for mojibake markers such as `вЂ`, `Р`, `С`, or `�`.
- If encoding corruption appears in diff output, treat it as a bug to fix immediately.
- Avoid combining large refactors with mass localization edits unless necessary.

### After implementation

- Update `work-log.md` first.
- Update `system-tree.md` when system hierarchy, subsystem responsibilities, feature leaves, or cross-system dependencies changed.
- Update `tutorial-scenario.md` when serious changes alter the Tutorial-mode player path, prerequisites, unlock order, HUD entry points, required buildings/resources, automation/manual-control balance, or goal text.
- Update `Design/worker-thought-tree.md` when adding, removing, renaming, reclassifying, or substantially changing worker thought keys, affect-driven thoughts, weakness-driven interpretation, knowledge/opinion effects from thoughts, Noosphere thought/state chains, or Workers/F9 thought display cases.
- Update `Design/worker-thought-influence-matrix.md` when adding, removing, retuning, or substantially changing explicit thought influence rules, source/target thought links, influence strength, influence windows, safety caps, weakness/trait modifiers, or example wording for thought feedback.
- Update `project-overview.md` only if visible project structure or key responsibilities changed.
- Update `systems-map.md` if system ownership, file involvement, owner-map paths, or owner-map responsibilities changed.
- Update `architecture-notes.md` only if the real architecture changed or a new hotspot/refactor seam appeared.
- Prefer `./tools/check-all.ps1` before commits or after risky code edits. Use `-SkipSmokeTests` for a fast local pass when Unity is already open or unavailable; otherwise the script runs runtime/editor builds, line-count, diff whitespace, mojibake scan, and Unity EditMode smoke tests.

### System Tree Maintenance

- Keep `ai/system-tree.md` up to date as the project grows.
- Update it when adding, removing, renaming, or substantially changing a system, subsystem, player-facing feature, simulation feature, UI surface, or cross-system dependency.
- Keep file ownership and concrete paths in `ai/systems-map.md`; keep `ai/system-tree.md` conceptual and navigational.
- If a feature crosses systems, update both the relevant tree leaves and the cross-system links section.

## Writing Rules

- Use concise factual bullets.
- Prefer summaries over narratives.
- Avoid duplicate information across files.
- Record implemented reality, not intention unless clearly marked as planned.
- Keep entries scannable for the next agent.
- Date entries when they describe a task or change.

## Anti-Bloat Rules

- Do not copy full prompts.
- Do not log tiny cosmetic changes unless they affect behaviour or workflow.
- Do not repeat the same mechanic in multiple files.
- Do not keep stale "in progress" items; move them to `Done` or remove them.
- If `work-log.md` gets large, keep only active items and a short recent history.
- In `work-log.md`, keep `Done` limited to recent meaningful changes.
- Periodically collapse older completed items into a short summary.

## Quick Template For Future Prompts

Use this workflow:

1. Read `ai/README.md` and the other AI memory files.
2. Use memory to identify relevant systems.
3. Scan only the necessary code.
4. Make a short plan before editing.
5. Implement the change.
6. Update AI memory files to match the new code.

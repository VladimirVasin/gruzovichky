# AI Memory System

Purpose: shared project memory for AI agents working in this repository.

This folder is intentionally small. Code remains the source of truth. These files exist to reduce repeated rescans, improve planning, and keep cross-session context consistent.

## Files

- `project-overview.md`
  Stable high-level map of the project: folders, key modules, and main runtime areas.
- `systems-map.md`
  Active systems and their main files, plus impact hints for future changes.
- `architecture-notes.md`
  Actual implemented architecture, complexity hotspots, and likely refactor seams.
- `work-log.md`
  Active and recently completed work. Use this for task state and short implementation notes.
- `release-notes.md`
  Stable baseline for in-game Patch Notes by version. Use it to compare documented releases against current code when updating patch notes.

## Memory Types

Stable memory:

- `project-overview.md`
- `systems-map.md`
- `architecture-notes.md`

Volatile memory:

- `work-log.md`
- `release-notes.md`

Rule:

- stable memory is updated rarely
- volatile memory is updated often
- do not edit stable memory unless project reality actually changed

## Read Order For Agents

1. Read this file.
2. Read `project-overview.md`.
3. Read `systems-map.md`.
4. Read `architecture-notes.md`.
5. Read `work-log.md`.
6. Read `release-notes.md` when the task involves version labels, changelogs, release contents, or Patch Notes.
7. Scan only the code relevant to the requested change.

## Workflow Contract

### Before coding

- Read the memory files in the order above.
- Treat memory as a guide, not as authority over code.
- If memory and code disagree, trust code and update memory after finishing.
- Identify the affected systems before editing.
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
- Update `project-overview.md` only if visible project structure or key responsibilities changed.
- Update `systems-map.md` only if system ownership or file involvement changed.
- Update `architecture-notes.md` only if the real architecture changed or a new hotspot/refactor seam appeared.

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

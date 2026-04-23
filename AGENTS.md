# AGENTS

This project uses shared AI memory in:

- `ai/`

Main memory files:

- `ai/README.md`
- `ai/project-overview.md`
- `ai/systems-map.md`
- `ai/architecture-notes.md`
- `ai/work-log.md`

Reusable prompt templates live in:

- `ai/prompt-templates.md`

## Template Selection Logic

Primary rule:

- If the user explicitly writes `Use TEMPLATE_NAME`, the agent must load that template from `ai/prompt-templates.md` and follow it.
- At the start of every final response, the agent must state which template was used and whether it was explicitly requested by the user or inferred by the agent.

Fallback rule:

- If no template is specified, the agent should infer the most appropriate template from the request.

Inference rules:

- Use `FULL_TEMPLATE` for normal implementation tasks.
- Use `PLAN_TEMPLATE` for large, ambiguous, risky, or cross-system tasks.
- Use `BUGFIX_TEMPLATE` for bug investigation or regression fixes.
- Use `REFACTOR_TEMPLATE` for structural code changes, file splitting, or architectural cleanup.

Safety rule:

- If the correct template is unclear, default to `PLAN_TEMPLATE`.

Response rule:

- If the user explicitly specified a template, the agent must say that the template was explicitly requested by the user.
- If the user did not specify a template, the agent must say that the template was inferred by the agent.

## Template Lookup Rules

If the user says any of the following:

- `Use FULL_TEMPLATE`
- `Use PLAN_TEMPLATE`
- `Use BUGFIX_TEMPLATE`
- `Use REFACTOR_TEMPLATE`

the agent must:

1. open `ai/prompt-templates.md`
2. load the matching template block
3. follow that workflow for the task

## Default Agent Behaviour

- Read `ai/README.md` first.
- Read only the AI memory files relevant to the task.
- Treat code as source of truth if memory and code disagree.
- Identify affected systems before editing.
- Scan only the necessary code.
- State a short plan before making code changes.
- Avoid unrelated refactors.
- Update `ai/work-log.md` after implementation.
- Update other AI memory files only if they truly need changes.

## Memory Rules

- Keep memory concise.
- Avoid bloat and duplicate notes.
- Do not paste code into memory files.
- `ai/work-log.md` is the frequently updated file.
- Stable memory files should only be updated when project reality actually changes.

## Encoding Safety Rules

- Treat all `.cs`, `.md`, `.json`, `.txt`, and other project text files as `UTF-8`.
- Prefer `apply_patch` for manual edits, especially in files containing Russian UI/localization text.
- Do not rewrite project files through shell commands unless encoding is explicitly forced to UTF-8.
- After editing localized UI/HUD/tutorial/state text, run a quick scan for mojibake markers such as `вЂ`, `Р`, `С`, or `�`.
- If a diff shows broken Cyrillic or mojibake, stop and fix it before moving on.

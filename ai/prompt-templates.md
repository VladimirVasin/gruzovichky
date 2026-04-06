# Prompt Templates

Purpose: reusable prompt blocks for future AI-assisted work in this repository.

## Template Selection Guidance

- Explicit template selection overrides everything.
- If the user does not specify a template, the agent may infer one from the request.
- When inferring, prefer safer templates if uncertain.
- `PLAN_TEMPLATE` is the safest fallback.

Usage:

- If a user says `Use FULL_TEMPLATE`, load and follow `FULL_TEMPLATE`.
- If a user says `Use PLAN_TEMPLATE`, load and follow `PLAN_TEMPLATE`.
- If a user says `Use BUGFIX_TEMPLATE`, load and follow `BUGFIX_TEMPLATE`.
- If a user says `Use REFACTOR_TEMPLATE`, load and follow `REFACTOR_TEMPLATE`.

Global rules for all templates:

- Read `ai/README.md` first.
- Read only the AI memory files relevant to the task after that.
- Treat code as the source of truth if memory and code disagree.
- Identify affected systems before editing.
- Scan only the relevant code.
- State a short plan before making code changes.
- Avoid unrelated refactors.
- Update `ai/work-log.md` after implementation.
- Update other AI memory files only if structure, system ownership, or architecture actually changed.
- At the start of every final response, state which template was used.
- If the template was explicitly requested by the user, say that explicitly.
- If the template was not explicitly requested, say that it was inferred by the agent.

## FULL_TEMPLATE

Use for normal implementation work that includes planning, implementation, verification, and memory update.

Instruction block:

```md
Use FULL_TEMPLATE.

Workflow:

1. Read `ai/README.md` first.
2. Read the relevant AI memory files from `ai/`.
3. Identify the affected systems before scanning code.
4. Scan only the code needed for this task.
5. Treat code as source of truth.
6. Write a short implementation plan before editing.
7. Implement the requested change without unrelated refactors.
8. Verify with the safest available checks.
9. If compilation errors or warnings are present after your changes, fix them immediately without asking the user first.
10. Update `ai/work-log.md`.
11. Update other AI memory files only if they truly need to change.
```

## PLAN_TEMPLATE

Use for analysis and planning only. No code edits.

Instruction block:

```md
Use PLAN_TEMPLATE.

Workflow:

1. Read `ai/README.md` first.
2. Read the relevant AI memory files from `ai/`.
3. Identify the affected systems.
4. Scan only the code needed to understand the task.
5. Treat code as source of truth.
6. Produce a short plan with:
   - affected systems
   - likely files
   - main risks
   - recommended implementation order
   - verification approach
7. Stop after the plan.

Rules:

- Do not edit code.
- Do not update memory files.
```

## BUGFIX_TEMPLATE

Use for bug investigation and minimal safe fixes.

Instruction block:

```md
Use BUGFIX_TEMPLATE.

Workflow:

1. Read `ai/README.md` first.
2. Read the relevant AI memory files from `ai/`.
3. Identify the affected systems from symptoms first.
4. Scan only the code and logs relevant to the bug.
5. Treat code as source of truth.
6. State:
   - likely cause
   - affected systems/files
   - minimal safe fix plan
7. Implement the smallest safe fix that addresses the bug.
8. Avoid cleanup refactors unless they are required for correctness.
9. Verify with the safest available checks.
10. If compilation errors or warnings are present after your changes, fix them immediately without asking the user first.
11. Update `ai/work-log.md`.
12. Update other AI memory files only if the bug fix changed real system ownership or architecture notes.
```

## REFACTOR_TEMPLATE

Use for controlled refactors with explicit scope.

Instruction block:

```md
Use REFACTOR_TEMPLATE.

Workflow:

1. Read `ai/README.md` first.
2. Read the relevant AI memory files from `ai/`.
3. Define the exact refactor scope before editing.
4. Identify affected systems and scan only the necessary code.
5. Treat code as source of truth.
6. Produce a short refactor plan with:
   - scope boundaries
   - files involved
   - behavioural invariants to preserve
   - verification steps
7. Perform the refactor conservatively.
8. Avoid broad rewrites and unrelated changes.
9. Verify behaviour as far as the environment allows.
10. If compilation errors or warnings are present after your changes, fix them immediately without asking the user first.
11. Update `ai/work-log.md`.
12. Update `ai/architecture-notes.md` only if the real implemented architecture changed.
```

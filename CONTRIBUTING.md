# Contributing

This project is a Unity prototype, so the main goal is to keep iteration fast while preserving clean scene and asset history.

## Branching

- Use `main` for the current stable working state.
- Create short-lived feature branches for focused work.
- Suggested branch naming:
  - `feature/<topic>`
  - `bugfix/<topic>`
  - `chore/<topic>`

Examples:

- `feature/fuel-rescue-driver`
- `bugfix/road-visual-connection`

## Commit Style

Keep commits small and task-focused.

Recommended commit format:

- `feat: add manual fuel rescue driver loop`
- `fix: restore starter road connection near gas station`
- `chore: update unity git attributes`

## Unity-Specific Rules

- Commit `.meta` files together with the assets they belong to.
- Do not commit generated folders such as `Library`, `Temp`, `Logs`, or `UserSettings`.
- Prefer text serialization for Unity assets so diffs stay reviewable.
- Keep scene edits intentional and avoid unrelated re-saves when possible.

## Suggested Daily Workflow

1. Pull latest changes from `main`.
2. Create a new branch for the task.
3. Make the smallest useful change set.
4. Run a quick validation:
   - open the scene in Unity
   - verify the target interaction manually
   - run `dotnet build Assembly-CSharp.csproj -nologo`
5. Commit with a focused message.
6. Push the branch and open a PR if needed.

## Before Pushing

Check:

- no generated Unity folders are staged
- `.meta` files match changed assets
- scene still opens
- no obvious broken references in inspector
- build/test command used for this repository still passes

## Notes For This Repository

- The main runtime prototype logic currently lives in `Assets/Scripts/TransportPrototypeBootstrap.cs`.
- This is still a prototype-first repository, so simple and readable changes are preferred over heavy abstraction.

# The Race for Space

Prototype scaffold for **The Race for Space**, a Kerbal Space Program mod.

This repository currently contains the planned project structure only. Gameplay systems will be implemented incrementally during prototype development.

## Repository layout

- `src/TheRaceForSpace/` — mod source code, separated by gameplay/system responsibility.
- `GameData/TheRaceForSpace/` — KSP-ready mod distribution layout.
- `tests/TheRaceForSpace.Tests/` — automated tests for logic that can be isolated from KSP.
- `docs/` — design, prototype scope, architecture notes and decisions.
- `tools/` — development/build helper tooling.

## Prototype architecture

The initial source structure separates:

- Core lifecycle and shared domain state
- Space-program / competitor state
- Race progression and milestone evaluation
- Scoring and victory/progress calculations
- Save/load persistence
- KSP integration adapters and event handling
- User interface / presentation
- Debug and prototype-only utilities

No gameplay implementation has been added yet.

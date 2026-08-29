# The Race for Space

Prototype development repository for **The Race for Space**, a Kerbal Space Program mod.

The current vertical slice implements the first satellite funding race prototype with two simulated rival agencies, three funding programmes and player satellite tracking around Kerbin, Mun and Minmus.

## Repository layout

- `src/TheRaceForSpace/` — mod source code, separated by gameplay/system responsibility.
- `GameData/TheRaceForSpace/` — KSP-ready mod distribution layout.
- `tests/TheRaceForSpace.Tests/` — automated tests for logic that can be isolated from KSP.
- `docs/` — design, prototype scope, architecture notes and decisions.
- `tools/` — development/build helper tooling.

## Build

The mod project is `src/TheRaceForSpace/TheRaceForSpace.csproj`. It builds against the assemblies from a local KSP 1.12.x installation; KSP and Unity DLLs are not stored in this repository.

See `docs/BUILDING.md` for prerequisites, `KSP_ROOT` setup, build commands and optional deployment into `GameData/TheRaceForSpace/Plugins/`.

## Prototype architecture

The source structure separates:

- Core lifecycle and shared domain state
- Space-program / competitor state
- Race progression and milestone evaluation
- Scoring and victory/progress calculations
- Save/load persistence
- KSP integration adapters and event handling
- User interface / presentation
- Debug and prototype-only utilities

The first gameplay implementation is intentionally narrow so the competitive satellite loop can be verified before persistence, stock Career funding integration and broader programme types are added.

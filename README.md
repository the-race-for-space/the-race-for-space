# The Race for Space

Prototype development repository for **The Race for Space**, a Kerbal Space Program mod.

The current 0.3 vertical slice implements two competitive Kerbin orbit contracts, unlockable satellite-network funding around Kerbin, Mun and Minmus, two simulated rival agencies, scheduled funding, and persistent player/rival race progression.

## Repository layout

- `src/TheRaceForSpace/` — mod source code, separated by gameplay/system responsibility.
- `GameData/TheRaceForSpace/` — KSP-ready mod distribution layout.
- `tests/TheRaceForSpace.Tests/` — automated tests for logic that can be isolated from KSP.
- `docs/` — design, prototype scope, architecture notes and decisions.
- `tools/` — development/build helper tooling.

## Build

The mod project is `src/TheRaceForSpace/TheRaceForSpace.csproj`. It builds against the assemblies from a local KSP 1.12.x installation; KSP and Unity DLLs are not stored in this repository.

See `docs/BUILDING.md` for prerequisites, `KSP_ROOT` setup, build commands and optional deployment into `GameData/TheRaceForSpace/Plugins/`.

## Interface target

The primary interface target is a Steam Machine or conventional desktop PC using a normal PC-sized display. Prototype UI layouts should use the available desktop screen space rather than assume handheld-screen constraints.

Version 0.3 keeps one Command Center window with Overview, Funding Targets, Rival Agencies and Space Race views. Funding Targets shows only contracts currently in play, while Space Race shows the complete progression and player-guide information, including locked objectives.

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

The gameplay implementation remains intentionally narrow so the progression, competitive achievement funding, rival mission simulation and satellite funding loop can be verified before broader programme types or celestial-body expansion are added.

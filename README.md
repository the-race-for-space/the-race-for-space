# The Race for Space

Prototype development repository for **The Race for Space**, a Kerbal Space Program mod.

The current **0.4 alpha** baseline implements competitive Kerbin, Mun, Minmus and Duna orbit achievements, unlockable satellite-network funding around Kerbin, Mun, Minmus and Duna, two simulated rival agencies, scheduled funding, and persistent player/rival race progression.

Version 0.4 begins with a cleanup pass over the working 0.3 prototype. The immediate goal is to make the existing systems clearer and safer before larger structural or gameplay expansion. Duna support that was added late in the 0.3 development branch is treated as part of the 0.4 baseline.

## Repository layout

- `src/TheRaceForSpace/` — mod source code, separated by gameplay/system responsibility.
- `GameData/TheRaceForSpace/` — KSP-ready mod distribution layout.
- `tests/TheRaceForSpace.Tests/` — automated tests for logic that can be isolated from KSP.
- `docs/` — design, prototype scope, architecture notes and decisions.
- `tools/` — development/build helper tooling.

## Build

The mod project is `src/TheRaceForSpace/TheRaceForSpace.csproj`. It builds against the assemblies from a local KSP 1.12.x installation; KSP and Unity DLLs are not stored in this repository.

See `docs/BUILDING.md` for prerequisites, `KSP_ROOT` setup, build commands and optional deployment into `GameData/TheRaceForSpace/Plugins/`.

## Balance configuration

`GameData/TheRaceForSpace/Config/RaceSettings.cfg` contains the user-editable gameplay balance values. It exposes the Kerbin, Kerbin-moon, interplanetary-planet and interplanetary-moon funding/cost tiers, plus the funding interval, rival starting funds, rival progress chance and number of rivals.

The file is read through KSP's GameDatabase before the race controller is first created. Restart KSP after editing the file. Missing or invalid values retain the built-in defaults, and no in-game settings UI is required.

## Interface target

The primary interface target is a Steam Machine or conventional desktop PC using a normal PC-sized display. Prototype UI layouts should use the available desktop screen space rather than assume handheld-screen constraints.

Version 0.4 keeps one Command Center window with Overview, Funding Targets, Rival Agencies and Space Race views. Funding Targets shows only contracts currently in play, while Space Race shows the complete progression and player-guide information, including locked objectives.

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

The gameplay implementation remains intentionally narrow. Version 0.4 should first stabilize and simplify the existing progression, competitive achievement funding, rival mission simulation and satellite funding loop before broader programme types, additional rival agencies, or larger celestial-body expansion are added.

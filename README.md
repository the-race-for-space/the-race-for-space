# The Race for Space

Prototype development repository for **The Race for Space**, a Kerbal Space Program mod.

The current **0.5 alpha** adds a pre-orbit Kerbin contract phase to the competitive space-race prototype. Four special starter programme lines - Directed Power, Mass, Control and Biome - each contain five milestones with independent progression, rival participation and declining achievement funding. Completing Level V in any one line unlocks the first Probe Orbit objective for the race.

Version 0.5 builds on the cleaned 0.4 competitive funding/rival foundation rather than replacing it. Existing orbital milestones, satellite-network funding, scheduled sponsor reviews, rival simulation and persistent campaign progression remain in place after the starter phase.

## Repository layout

- `src/TheRaceForSpace/` — mod source code, separated by gameplay/system responsibility.
- `GameData/TheRaceForSpace/` — KSP-ready mod distribution layout.
- `tests/TheRaceForSpace.Tests/` — automated tests for logic that can be isolated from KSP.
- `docs/` — design, prototype scope, architecture notes and decisions.
- `tools/` — development/build helper tooling.

## Build

The mod project is `src/TheRaceForSpace/TheRaceForSpace.csproj`. It builds against the assemblies from a local KSP 1.12.x installation; KSP and Unity DLLs are not stored in this repository.

See `docs/BUILDING.md` for prerequisites, `KSP_ROOT` setup, build commands and optional deployment into `GameData/TheRaceForSpace/Plugins/`.

For the v0.5 starter-contract in-game acceptance pass, see `docs/KERBAL_CONTRACTS_V0_5_TESTING.md`.

## Balance configuration

`GameData/TheRaceForSpace/Config/RaceSettings.cfg` contains the user-editable gameplay balance values. It exposes the Kerbin, Kerbin-moon, interplanetary-planet and interplanetary-moon funding/cost tiers, plus the funding interval, rival starting funds, rival progress chance and number of rivals.

The mod reads this file once before the first race controller is created. Restart KSP after editing the file. Missing or invalid values retain the built-in defaults, and no in-game settings UI is required.

The twenty v0.5 starter milestones currently use code-defined fixed contract values: Level I-V rewards of 10,000 / 20,000 / 30,000 / 40,000 / 50,000 and rival successful 10% progress costs of 2,000 / 3,000 / 4,000 / 5,000 / 6,000.

## Interface target

The primary interface target is a Steam Machine or conventional desktop PC using a normal PC-sized display. Prototype UI layouts should use the available desktop screen space rather than assume handheld-screen constraints.

Version 0.5 keeps one Command Center window with Overview, Funding Targets, Rival Agencies and Space Race views. Space Race now includes a dedicated four-card Starter Programmes panel with current line objectives and live flight progress, while the normal funding catalogue continues to present the orbital/satellite progression. Funding Targets remains the financial view for all offered and paying contracts, including starter achievements after completion.

## Prototype architecture

The source structure separates:

- Core lifecycle and shared domain state
- Space-program / competitor state
- Race progression and milestone evaluation
- KSP-independent starter-flight tracking
- Scoring and victory/progress calculations
- Save/load persistence
- KSP integration adapters and event handling
- User interface / presentation
- Debug and prototype-only utilities

Version 0.5 keeps KSP vessel access at the integration boundary and evaluates starter contracts from project-owned flight snapshots. The active vessel uses a lightweight one-second observation path, while the existing heavier loaded/unloaded orbital scan remains on its separate slower cadence. Starter attempts are persisted independently so flight history needed for fair contract evaluation survives save/load without creating a parallel funding or rival system.

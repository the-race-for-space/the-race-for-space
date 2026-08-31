# Prototype Structure

This scaffold mirrors the current design direction for **The Race for Space** without committing to gameplay implementation yet.

## Source modules

- `Core/` — mod lifecycle, shared state and cross-module coordination. `RaceRuntime` owns the current race controller and advances progression on the controlled five-second refresh cadence independently of the UI.
- `Programs/` — player and rival space-program models/state.
- `Tracking/` — KSP-independent vessel classification and body-presence tracking from project-owned vessel snapshots.
- `Milestones/` — achievement/goal definitions and evaluation.
- `Competition/` — first-to-achieve and comparative-coverage race logic.
- `Funding/` — nation/corporation sponsors, offers and awards.
- `Simulation/` — lightweight rival-program simulation for prototype use.
- `Scoring/` — progress/coverage calculations kept separate from funding rules.
- `Persistence/` — simple save-state models for Race for Space values that must survive KSP save/load cycles.
- `KspIntegration/` — KSP API adapters, game events, ScenarioModule persistence hooks, and vessel discovery. `KspVesselDiscovery` resolves loaded and unloaded KSP vessel state into project-owned tracking snapshots before tracking rules consume it.
- `UI/` — race/funding presentation, Command Center visibility, and stock launcher interaction. UI code reads race state but does not own or advance gameplay progression.
- `Debug/` — prototype diagnostics and developer-only helpers.

## Distribution layout

`GameData/TheRaceForSpace/` is reserved for the installable KSP package. `Plugins/`, `Config/`, `Assets/` and `Localization/` are present as placeholders only.

## Tests

`tests/TheRaceForSpace.Tests/` mirrors logic-heavy modules so milestone, competition, tracking and scoring rules can be tested without requiring a live KSP scene where possible.

## Prototype rule

Keep the first implementation narrow: prove we can read persistent/unloaded vessel data, classify vessel presence around/on celestial bodies, maintain per-program state, evaluate a small milestone set, and award a simple funding outcome. Rival simulation, UI polish and richer sponsor behaviour should remain replaceable layers.

# Prototype Structure

This scaffold mirrors the current design direction for **The Race for Space** without committing to gameplay implementation yet.

## Source modules

- `Core/` — mod lifecycle, shared state and cross-module coordination. `RaceRuntime` owns the current race controller and advances progression on the controlled five-second refresh cadence independently of the UI.
- `Programs/` — player and rival space-program models/state.
- `Tracking/` — KSP-independent vessel classification and body-presence tracking from project-owned vessel snapshots. Probe/Relay counts are maintained for observed celestial bodies independently of milestone definitions. Milestone recording evaluates each definition's `UnlockRule` against the full race-program collection at the vessel observation time, repeating when a new player achievement can satisfy another rule in the same snapshot.
- `Milestones/` — achievement/goal definitions and evaluation. `PrototypeMilestones` is the code-defined catalogue of current prototype achievement targets. Flexible unlock definitions use immutable OR-of-AND rules with achievement scope/count and universal-time conditions. `UnlockRuleEvaluator` is the single KSP-independent interpretation of those rules for tracking, funding availability, and rival target selection.
- `Competition/` — first-to-achieve and comparative-coverage race logic. `SatelliteRaceController` consumes programme collections, does not construct or expose individual funding targets, and caches per-programme projected payouts when its controlled refresh evaluates funding. Unlock rules are evaluated at explicit current, vessel-observation, projected-funding, or historical funding-boundary times as appropriate.
- `Funding/` — nation/corporation sponsors, offers and awards. `PrototypeFundingCatalogue` owns the code-defined achievement rewards and satellite-network programme bootstrap for the current prototype. Achievement and satellite programmes store `UnlockRuleDefinition`; availability itself is evaluated outside Funding by the shared milestone evaluator.
- `Simulation/` — lightweight rival-program simulation for prototype use. Rival progression consumes program and target collections, stable mission target IDs are authoritative, and target availability uses `UnlockRuleEvaluator` at the simulation time. Display names are presentation only.
- `Scoring/` — progress/coverage calculations kept separate from funding rules.
- `Persistence/` — simple save-state models for Race for Space values that must survive KSP save/load cycles. Rival programs are persisted as a collection keyed by stable program ID, with stable mission target IDs rather than presentation-name migration.
- `KspIntegration/` — KSP API adapters, game events, ScenarioModule persistence hooks, and vessel discovery. `KspVesselDiscovery` resolves loaded and unloaded KSP vessel state into project-owned tracking snapshots before tracking rules consume it.
- `UI/` — race/funding presentation, Command Center visibility, and stock launcher interaction. UI code reads race state but does not own or advance gameplay progression; recurring IMGUI scratch buffers, layout options, and the window callback are reused rather than recreated for every draw event.
- `Debug/` — prototype diagnostics and developer-only helpers.

## Distribution layout

`GameData/TheRaceForSpace/` is reserved for the installable KSP package. `Plugins/`, `Config/`, `Assets/` and `Localization/` are present as placeholders only.

## Tests

- `tests/TheRaceForSpace.Tests/` mirrors logic-heavy modules so milestone, simulation, tracking, funding, and persistence rules can be tested without requiring a live KSP scene. It compiles both the unlock definition model and evaluator and includes consumer-level tracking/rival regressions.
- `tests/TheRaceForSpace.ControllerTests/` compiles the real `SatelliteRaceController` against small test-only stand-ins for its existing KSP integration boundaries. It covers cross-module controller ordering without adding test seams to production code or requiring a KSP installation. The runner also hosts focused unlock-rule evaluator and rival-consumer regressions.
- `tools/run-logic-tests.sh` runs both standalone suites.
- `.github/workflows/logic-tests.yml` runs the same script on GitHub Actions for pushes and pull requests using .NET 8 on Ubuntu.

Live KSP API behavior such as loaded/unloaded vessel discovery and actual Career-funds integration still requires the documented in-game verification path.

## Prototype rule

Keep the first implementation narrow: prove we can read persistent/unloaded vessel data, classify vessel presence around/on celestial bodies, maintain per-program state, evaluate a small milestone set, and award a simple funding outcome. Rival simulation, UI polish and richer sponsor behaviour should remain replaceable layers.

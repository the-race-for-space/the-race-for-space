# Version 0.4 Alpha Scope

Version 0.4 starts from the working Funding Targets 0.3 prototype and treats the late Duna additions on that branch as part of the new baseline.

The first 0.4 branch is **Alpha/Cleanup-0.4**. Its purpose is maintenance and consolidation before larger gameplay expansion.

## 0.4 Baseline

The 0.4 alpha baseline currently contains:

- Probe Orbit and Crewed Orbit achievements around Kerbin.
- Probe Orbit and Crewed Orbit achievements around Mun, Minmus, and Duna.
- Satellite-network funding around Kerbin, Mun, Minmus, and Duna.
- Player plus Aster and Cobalt space-program state.
- Shared 90 Kerbin-day funding dates.
- Declining-interest achievement funding.
- Lightweight rival mission simulation.
- Persistent race, achievement, funding, satellite, and rival mission state.
- The four-view Command Center interface: Overview, Funding Targets, Rival Agencies, and Space Race.

## Alpha Cleanup Goals

The initial 0.4 cleanup pass was intentionally narrow:

1. Align version numbers and documentation with the code that actually exists.
2. Keep rival cost and ETA queries free of hidden mission-state mutation.
3. Add small defensive checks where invalid state can otherwise cause avoidable failures.

The initial cleanup did not introduce a general configuration/rule framework.

## Approved Structural Work

### Core runtime ownership

Race progression ownership has now been moved out of `UI/RaceWindow` with explicit approval under the structural-change gate in `AGENTS.md`.

- `Core/RaceRuntime` owns the current `SatelliteRaceController`.
- `RaceRuntime` advances the controller on the existing five-second real-time refresh cadence.
- The controller remains shared across KSP scene changes inside the same game.
- A different `HighLogic.CurrentGame` receives a fresh controller so state is not carried between saves.
- `RaceWindow` keeps only a non-owning reference for presentation and no longer constructs or refreshes the controller.
- No save-data format, funding rule, milestone rule, rival rule, or tracking rule changed as part of this move.

The purpose of this change is to ensure race progression continues independently of whether the Command Center window is visible or whether the UI implementation changes later.

### KSP vessel discovery boundary

Raw KSP vessel discovery has now been moved out of `Tracking/SatelliteTracker` with explicit approval under the structural-change gate in `AGENTS.md`.

- `KspIntegration/KspVesselDiscovery` owns access to `HighLogic`, `ProtoVessel`, live `Vessel` state, `FlightGlobals`, and KSP universal time for vessel observations.
- Loaded vessels continue to use live situation, body, type, and crew state so newly reached orbits are detected immediately.
- Unloaded vessels continue to use persistent `ProtoVessel` and orbit snapshot state.
- KSP vessel types are converted into the project-owned `TrackedVesselType` representation before tracking rules consume them.
- `Tracking/SatelliteTracker` consumes `VesselTrackingSnapshot` values and contains no direct KSP vessel API access.
- `Competition/SatelliteRaceController` coordinates discovery and tracking during the existing refresh path.
- Snapshot-based tracking logic is included in the standalone logic-test project.
- No save-data format, funding rule, milestone definition, rival rule, or refresh cadence changed as part of this move.

### Milestone-independent satellite body tracking

Satellite body-count selection has now been separated from milestone definitions with explicit approval under the structural-change gate in `AGENTS.md`.

- `SatelliteTracker` counts every observed Probe or Relay by its celestial body name, including bodies that have no current milestone definition.
- Milestone definitions are now used only when evaluating milestone achievements, not when deciding which bodies can have satellite counts.
- A successful vessel snapshot refresh is authoritative for player satellite presence, so stale counts are cleared before the newly observed body counts are stored.
- If KSP vessel discovery is not ready and returns no valid snapshot refresh, the tracker is not called and the last known counts remain untouched.
- Existing Kerbin, Mun, Minmus, and Duna funding behaviour is unchanged.
- The persistence schema remains unchanged; its existing collection-based satellite storage can already represent arbitrary celestial-body names.

This removes the hidden requirement that a celestial body must first appear in the milestone catalogue before tracking can represent satellites around it.

### Collection-driven rival persistence

Rival persistence has now been changed from fixed Aster/Cobalt save slots to a collection-driven format with explicit approval under the structural-change and save-compatibility gates in `AGENTS.md`.

- `Persistence/RivalProgramsSaveState` stores any number of rival program states keyed by each program's stable ID.
- Each `RIVAL` entry stores its own `programId` together with funds, achievements, satellite counts, mission target ID, mission progress, and next progress-check time.
- `KspIntegration/RacePersistenceScenario` now saves one `RIVALS` collection instead of dedicated `ASTER` and `COBALT` nodes.
- Restore matches runtime rivals by stable program ID, so save-node order and display-name changes do not affect identity.
- A rival that exists in the runtime but has no matching saved entry keeps its constructor defaults. This allows future rivals to be introduced without inventing state for them or extending the save schema.
- Current Aster and Cobalt gameplay rules are unchanged; only how their state is stored and restored has changed.

This is intentionally a save-format break for rival state. The older fixed `ASTER` and `COBALT` ScenarioModule nodes are no longer imported by the new collection path.

### Centralized prototype funding catalogue

The code-defined funding target bootstrap has now been moved out of `Competition/SatelliteRaceController` with explicit approval under the structural-change gate in `AGENTS.md`.

- `Milestones/PrototypeMilestones` remains the catalogue of achievement definitions and their stable IDs, bodies, vessel requirements, descriptions, and prerequisite milestones.
- `Funding/PrototypeFundingCatalogue` now owns achievement reward amounts and the satellite-network definitions for the current prototype.
- Achievement funding programmes are created from the milestone catalogue, so names, objective text, and prerequisite IDs are not repeated in the controller.
- `SatelliteRaceController` consumes the two programme collections and no longer constructs Kerbin, Mun, Minmus, or Duna funding targets individually.
- The standalone logic suite verifies the current eight achievement programmes, four satellite programmes, rewards, prerequisites, and fresh campaign-state creation.
- No funding calculation, unlock rule, rival rule, tracking rule, or save format changed as part of this move.

This makes target expansion a catalogue change instead of a controller-constructor change while deliberately stopping short of a configuration framework or general rule engine.

### Collection-first compatibility API cleanup

The remaining 0.3 rival-simulation compatibility surface has now been removed with explicit approval for public API and legacy-save cleanup.

- `RivalSimulation` now exposes one collection-driven progression path instead of the old three-program and boolean-state overloads.
- Display-name lookup, launch-progress cost, and ETA calculations use the current collection-aware APIs rather than fixed Kerbin/Mun/Minmus compatibility overloads.
- `SpaceProgramState.NextMissionTargetId` is the authoritative rival mission identity. `NextLaunchBodyName` remains a presentation mirror and is never translated back into gameplay identity.
- The fixed public rival target-name and three-network compatibility constants have been removed from `RivalSimulation`.
- `SatelliteRaceController` no longer exposes named achievement/network programme properties; callers use `AchievementFundingProgrammes` and `FundingProgrammes`.
- `RivalProgramSaveState` stores and restores the current stable `nextMissionTargetId` directly and no longer imports old display-name mission fields, fixed rival achievement flags, or fixed Kerbin/Mun/Minmus satellite fields.
- Current collection-driven `RIVALS` save nodes and their stable fields remain the active 0.4 format; this cleanup does not change that current schema.
- Regression tests now exercise the collection APIs directly and verify that presentation text alone cannot define a rival mission target.

This removes duplicate prototype-era entry points before more rivals and targets are added, so new gameplay has one supported identity and collection model.

### Controller regression coverage and CI

Controller-level regression coverage and continuous integration have now been added without introducing production test seams.

- `tests/TheRaceForSpace.ControllerTests/` compiles the real `SatelliteRaceController` together with its KSP-independent production dependencies.
- Small test-only stand-ins implement the controller's existing KSP-facing boundaries for universal time, vessel discovery, ScenarioModule readiness/capture, and Career funding awards.
- Controller regressions cover the player Probe observation flowing through tracking into achievement/funding unlocks, the exact first shared funding-boundary payout for existing eligible state, and the rule that a vessel first observed on a crossed funding boundary is not paid retroactively for that boundary.
- The original `tests/TheRaceForSpace.Tests/` domain suite remains independent and unchanged in responsibility.
- `tools/run-logic-tests.sh` runs both the domain suite and controller suite in Release configuration.
- `.github/workflows/logic-tests.yml` runs that same script on Ubuntu with .NET 8 for pushes and pull requests.
- CI deliberately does not build the KSP-targeted mod assembly because KSP/Unity assemblies are not stored in the repository. Loaded/unloaded vessel API behavior and real Career-funds integration remain manual in-game checks.

This gives the current controller orchestration an automated regression boundary while keeping KSP-specific runtime validation separate.

### Funding and Command Center performance cleanup

The two low-priority prototype performance items have now been completed together without changing save data or gameplay rules.

- `SatelliteRaceController` owns fixed-size per-program/per-programme payout arrays for the controller lifetime.
- `EvaluateFundingProgrammes()` rebuilds satellite and achievement projected payouts once per controlled refresh and uses those same values to assemble each program's `NextPayoutFunds`.
- `GetSatelliteCurrentPayout()` and `GetAchievementCurrentPayout()` serve the completed refresh snapshot to repeated UI queries instead of recalculating cross-agency shares on every IMGUI event.
- Historical crossed-funding replay deliberately bypasses the projection cache and calculates directly from the state at each funding boundary, preserving payment ordering and amounts.
- The controller regression suite verifies that projected payout values are rebuilt when a later vessel snapshot changes the player satellite state.
- `RaceWindow` reuses funding-card payout/name buffers, a `StringBuilder` for agency summaries, the `GUILayout.Window` callback delegate, and common `GUILayoutOption[]` arrays.
- Scratch payout/name arrays are only reallocated when the number of programs changes, rather than once per funding card per IMGUI event.
- Dynamic label strings still exist where their displayed values actually change; this cleanup targets the obvious recurring collection, delegate, and layout-option allocations without complicating the UI code.

No persistence field, save node, unlock condition, reward amount, funding cadence, rival rule, tracking rule, or Command Center layout was changed by this performance pass.

## Compatibility

Player race progress, funding-programme state, achievement-contract state, and Command Center visibility keep their existing persistence paths.

Rival state uses the `RIVALS` collection format introduced during Item 7. Saves written before that collection change do not restore the old fixed Aster/Cobalt rival state.

Within rival state, current 0.4 `programId`, `nextMissionTargetId`, achievement collection, and satellite collection fields remain supported. Older rival migration fields and the old public rival-simulation compatibility APIs are no longer supported; this additional compatibility cleanup was explicitly accepted for Item 9.

## Next Decisions

Items 1 through 13 of the original 0.4 cleanup roadmap are now complete. The remaining numbered roadmap item is the larger future gameplay/design decision: flexible multi-condition unlock rules.

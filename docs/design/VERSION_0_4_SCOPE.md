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
2. Keep rival cost and ETA queries free of hidden mission-state mutation; legacy target migration remains part of simulation refresh/load behaviour.
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

## Compatibility

Player race progress, funding-programme state, achievement-contract state, and Command Center visibility keep their existing persistence paths.

Rival state now uses the new `RIVALS` collection format. Saves written before this Item 7 change will not restore the old fixed Aster/Cobalt rival state; those rivals will start from their constructor defaults when such a save is loaded. This compatibility break was explicitly accepted for the 0.4 structural work.

## Next Decisions

Larger 0.4 work can continue to be selected separately. The main remaining expansion-preparation decision is centralising the currently code-defined target/programme bootstrap so new celestial bodies and race targets do not require edits across the controller constructor. That work remains outside the current implementation unless separately approved where required by `AGENTS.md`.

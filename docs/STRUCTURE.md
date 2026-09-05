# Project Structure

This document defines **which module owns which responsibility** in the current 0.5 codebase.

For a shorter introduction, read [`CODE_OVERVIEW.md`](CODE_OVERVIEW.md).

## Architecture at a glance

```text
KSP / Unity
    |
    v
KspIntegration
    |
    +--> live active-vessel snapshot
    |        |
    |        v
    |    FlightContractTracker
    |
    +--> slower loaded/unloaded vessel snapshot
             |
             v
         OrbitalVesselTracker

Tracking results
    |
    v
CampaignController
    |
    +--> Agencies
    +--> Objectives
    +--> Funding
    +--> Rivals
    |
    +--> Persistence
    +--> UI reads state

ModRuntime schedules the work above.
```

The important rule is that **raw KSP objects stay at the integration boundary**. Gameplay logic should work with project-owned state and snapshots.

## Source modules

### `Core/`

Owns runtime scheduling and campaign-wide settings.

Main classes:

- `ModRuntime` — owns the live `CampaignController` for the current KSP game and schedules recurring work.
- `CampaignSettings` — reads and exposes campaign balance settings.

Current runtime cadences:

- active Flight Contract telemetry: about once per second;
- normal campaign/controller refresh: about every five seconds;
- heavier orbital vessel scan: about every twenty seconds.

The UI does not own these timers.

### `Campaign/`

Owns high-level campaign coordination.

Main class:

- `CampaignController`

It coordinates:

- agencies;
- objective funding contracts;
- satellite-network funding contracts;
- sponsor reviews;
- shared funding dates;
- objective availability;
- rival progress;
- the cached set of currently active Flight Contracts.

It does **not** query raw KSP vessels directly.

### `Agencies/`

Owns player and rival agency state.

Main class:

- `AgencyState`

An agency stores mutable campaign information such as:

- completed objective timestamps;
- funds;
- qualifying satellite counts;
- current rival mission state where applicable.

Stable agency IDs are gameplay identity. Display names are presentation.

### `Objectives/`

Owns objective definitions and unlock rules.

Main classes:

- `ObjectiveDefinition`
- `ObjectiveCatalogue`
- `UnlockRuleDefinition`
- `UnlockRuleEvaluator`

`ObjectiveCatalogue` contains:

- the twenty current Pre-Orbit objectives;
- orbital probe and crewed objectives for supported celestial bodies.

Unlock rules use one shared evaluator. UI, campaign progression, rivals, and tracking should not invent separate interpretations of the same rule.

### `Funding/`

Owns funding-contract definitions and funding lifecycle state.

Main classes:

- `ObjectiveFundingContract`
- `SatelliteNetworkFundingContract`
- `FundingContractCatalogue`

Two funding types exist:

1. **Objective Funding Contracts** — one-off objectives with the declining ten-payment sequence.
2. **Satellite Network Funding Contracts** — continuing network funding based on qualifying satellites.

The controller coordinates these contracts, but the contract types own their own funding state and calculations.

### `Rivals/`

Owns simulated rival mission behaviour.

Main class:

- `RivalSimulation`

It handles:

- selecting valid offered targets;
- mission progress;
- rival spending;
- completing simulated rival objectives;
- creating simulated satellite-network progress when the mission type calls for it.

Rival targets use stable IDs. Presentation text must never become gameplay identity.

### `Tracking/`

Owns KSP-independent vessel evaluation.

There are two separate paths.

#### Fast path: Flight Contracts

Main classes:

- `FlightContractTracker`
- `FlightTelemetryPlan`
- `ActiveVesselSnapshot`
- `SurfaceImpactEvaluator`

This path uses frequent telemetry from the actively controlled vessel.

The current users are the four Pre-Orbit lines:

- Directed Power;
- Mass;
- Control;
- Biome.

The infrastructure is intentionally generic. Future active-vessel contracts on Mun, Minmus, or other bodies should reuse this path rather than creating another tracker.

#### Slow path: Orbital Vessel Tracking

Main classes:

- `OrbitalVesselTracker`
- `OrbitingVesselSnapshot`

This path inspects loaded and unloaded vessels and is used for:

- orbital objective completion;
- qualifying satellite counts by celestial body.

### `Persistence/`

Owns KSP-independent save-state models.

Main classes:

- `CampaignFundingSaveState`
- `RivalAgenciesSaveState`
- `FlightContractProgressSaveState`

The persistence models store mutable project-owned state. They do not query KSP and they should not calculate gameplay progression.

Current top-level ScenarioModule sections are:

```text
CAMPAIGN_FUNDING
RIVAL_AGENCIES
FLIGHT_CONTRACT_PROGRESS
```

Command Center visibility is stored separately as a value on the ScenarioModule node.

### `KspIntegration/`

Owns direct KSP and Unity interaction.

Main classes include:

- `KspVesselMonitor`
- `ModPersistenceScenario`
- config and launcher/event integration classes.

Responsibilities include:

- reading active and persistent vessel state;
- handling loaded and unloaded vessels;
- listening for KSP destruction events used by Directed Power;
- converting KSP data into project-owned snapshots;
- ScenarioModule save/load hooks;
- loading `CampaignSettings.cfg`;
- Career-funds integration.

Raw `Vessel`, `ProtoVessel`, `HighLogic`, `FlightGlobals`, and similar KSP types should remain here where practical.

### `UI/`

Owns Command Center presentation.

Main class:

- `CommandCenterWindow`

Current views:

- Overview;
- Funding Targets;
- Rival Agencies;
- Contract Catalogue.

The UI reads state. It must not complete objectives, advance rivals, process funding, or sample KSP vessels directly.

## Current Pre-Orbit progression

Each line has five levels.

```text
Level I -> Level II -> Level III -> Level IV -> Level V
                                      |
                       any line Level V complete
                                      |
                                      v
                                Probe Orbit offered
```

Rules:

- Directed Power I, Mass I, Control I, and Biome I are offered at campaign start.
- A later level unlocks when any agency completes the previous level in that line.
- Unlocked Pre-Orbit contracts wait for the next sponsor review before becoming offered.
- All unlocked Pre-Orbit contracts can be offered together; they do not consume the normal one-off objective offer limit.
- Probe Orbit is the exception: completing Level V in any Pre-Orbit line offers Probe Orbit immediately.

## How a Pre-Orbit contract is evaluated

1. `ObjectiveCatalogue` defines the objective and its criteria.
2. `FundingContractCatalogue` creates the related `ObjectiveFundingContract`.
3. `CampaignController` decides whether that contract is offered.
4. Offered, unfinished Pre-Orbit objectives become active Flight Contracts.
5. `FlightTelemetryPlan` determines which live vessel values are needed.
6. `KspVesselMonitor` captures only those required values.
7. `FlightContractTracker` evaluates the snapshot against every active contract independently.
8. `AgencyState` records each completed objective.
9. `CampaignController` settles unlocks, offers, funding, and rival state on its normal refresh.
10. Persistence stores the changed campaign state and the Command Center displays it.

Multiple offered contracts may complete from the same flight if their own criteria are independently satisfied.

## Pre-Orbit criteria

### Directed Power

- reach the contract's required surface speed;
- never exceed 70 km during the attempt;
- do not enter orbit;
- impact Kerbin to complete.

### Mass

- retain the required final vessel mass;
- travel the required great-circle distance from the tracked launch origin;
- finish `LANDED` on Kerbin;
- `SPLASHED` does not count.

### Control

- have crew aboard;
- remain continuously inside the contract's altitude band for the required time;
- after qualification, land safely on Kerbin with crew;
- each offered Control contract keeps independent hold state.

### Biome

- reach the target Kerbin biome;
- finish `LANDED` in that biome;
- flying over a biome or splashing down does not count.

## Tracking and performance rules

The active-vessel path is requirement-gated.

Examples:

- mass is queried only while an active Mass contract needs it;
- biome is queried only while an active Biome contract needs it;
- crew count is queried only while Control needs it;
- Directed Power destruction tracking is enabled only while Directed Power requires impact telemetry.

When there are no active Flight Contracts, the fast path should avoid unnecessary active-vessel discovery and evaluation.

The slower orbital scan remains separate because it must consider loaded and unloaded vessels.

## Save-state ownership

`CAMPAIGN_FUNDING` stores:

- player objective-completion timestamps;
- objective funding lifecycle state;
- satellite-network funding lifecycle state;
- next shared funding time.

`RIVAL_AGENCIES` stores each rival by stable agency ID.

`FLIGHT_CONTRACT_PROGRESS` stores temporary active-flight state needed for fair continuation after save/load, including:

- tracked flight identity and launch origin;
- Directed Power maximum speed/altitude and orbit invalidation;
- independent `CONTROL_STATE` entries.

Instantaneous live telemetry such as current altitude, current mass, current biome, and crew count is rebuilt from the next active-vessel sample instead of being treated as authoritative saved state.

## Tests

Two KSP-independent suites are run by:

```bash
bash tools/run-logic-tests.sh
```

- `tests/TheRaceForSpace.Tests/` — domain, tracking, funding, rivals, and persistence.
- `tests/TheRaceForSpace.ControllerTests/` — real `CampaignController` orchestration against test-only KSP boundary stubs.

`.github/workflows/logic-tests.yml` runs the same script in CI.

Direct KSP API behaviour still requires an in-game test. See [`KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md).

## Where common changes belong

| Change | Primary location |
| --- | --- |
| Add or change an objective | `Objectives/ObjectiveCatalogue.cs` |
| Change unlock logic | `Objectives/UnlockRuleEvaluator.cs` |
| Change active-vessel contract evaluation | `Tracking/FlightContractTracker.cs` |
| Change live vessel values collected | `KspIntegration/KspVesselMonitor.cs` |
| Change orbital vessel evaluation | `Tracking/OrbitalVesselTracker.cs` |
| Change sponsor reviews or campaign coordination | `Campaign/CampaignController.cs` |
| Change one-off funding | `Funding/ObjectiveFundingContract.cs` |
| Change satellite-network funding | `Funding/SatelliteNetworkFundingContract.cs` |
| Change rival behaviour | `Rivals/RivalSimulation.cs` |
| Change save-state models | `Persistence/` |
| Change KSP save hooks | `KspIntegration/ModPersistenceScenario.cs` |
| Change Command Center presentation | `UI/CommandCenterWindow.cs` |

## Structure rule

Extend the existing modules before creating new ones. If a feature genuinely requires moving ownership, adding a major module, changing a public API, or breaking save/config compatibility, follow the structural-change gate in [`../AGENTS.md`](../AGENTS.md).

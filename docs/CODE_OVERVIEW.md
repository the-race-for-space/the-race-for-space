# Code Overview

This is the quickest map of how the mod is put together.

## Overall flow

```text
KSP / Unity
    |
    v
ModRuntime
    |
    +--> KspIntegration reads KSP state
    |
    +--> FlightContractTracker checks contracts that need live active-vessel data
    +--> OrbitalVesselTracker checks orbital objectives and satellite-network counts
    |
    v
CampaignController
    |
    +--> Agencies stores player and rival state
    +--> Objectives defines what can be completed and how it unlocks
    +--> Funding stores funding contracts and payouts
    +--> Rivals advances rival agencies
    |
    +--> Persistence saves campaign and flight-progress state
    +--> UI reads and displays the state
```

`ModRuntime` drives when work happens. `CampaignController` coordinates campaign state. The UI only reads state and never advances progression.

## Source folders

| Folder | Purpose |
| --- | --- |
| `Core/` | Runtime scheduling and campaign-wide settings. |
| `Campaign/` | Main campaign coordination: offers, unlocks, funding reviews and rival updates. |
| `Agencies/` | State for the player agency and rival agencies. |
| `Objectives/` | Objective definitions and unlock-rule evaluation. |
| `Funding/` | One-off objective funding and recurring satellite-network funding. |
| `Rivals/` | Rival mission selection, spending and progress. |
| `Tracking/` | KSP-independent flight-contract and orbital-vessel evaluation. |
| `Persistence/` | Saveable project-owned state. |
| `KspIntegration/` | Direct KSP API access, vessel capture, events, config loading and ScenarioModule save hooks. |
| `UI/` | Command Center presentation. |

## Main classes

### `ModRuntime`
Runs the mod's timed work. It samples active-vessel flight contracts every second, refreshes campaign progression on the regular controller cadence, and performs the slower orbital vessel scan separately.

### `CampaignController`
Coordinates the campaign. It owns the player/rival agency collections, funding-contract state, unlock progression, sponsor reviews, rival updates and the cached set of active flight contracts.

### `AgencyState`
Stores the state of one player or rival agency: completed objectives, funds, qualifying satellite counts and rival mission progress.

### `ObjectiveDefinition`
Defines one objective and the requirements for completing it. Examples include Directed Power I, Control III and Probe Orbit.

### `ObjectiveCatalogue`
Creates the current objective definitions and their stable IDs. The four current Kerbin lines are identified as pre-orbit objectives.

### `FlightContractTracker`
Evaluates contracts that require frequent data from the currently controlled vessel. The current users are the four pre-orbit Kerbin lines, but the tracking path is intentionally generic so future Mun, Minmus or other active-flight contracts can use the same one-second system.

### `OrbitalVesselTracker`
Uses the slower vessel scan to record orbital objectives and update qualifying satellite-network counts.

### `FundingContractCatalogue`
Builds the funding-contract state associated with objectives and satellite networks.

### `ObjectiveFundingContract`
Funding lifecycle for a one-off objective.

### `SatelliteNetworkFundingContract`
Funding lifecycle for a repeatable satellite-network target.

### `RivalSimulation`
Chooses valid rival missions, spends rival funds and advances mission progress.

### `ModPersistenceScenario`
Connects KSP save/load to `CampaignFundingSaveState`, `RivalAgenciesSaveState` and `FlightContractProgressSaveState`.

### `CommandCenterWindow`
Draws the Command Center and reads current campaign/flight state for display.

## Two vessel-checking paths

### Flight contracts - fast path
Used when a contract depends on the currently controlled vessel and needs frequent telemetry. `KspVesselMonitor` captures only the fields requested by `FlightTelemetryPlan`, then `FlightContractTracker` evaluates them.

The current pre-orbit Directed Power, Mass, Control and Biome contracts use this path. Future active-flight contracts on Mun, Minmus or other bodies can use it too.

### Orbital tracking - slower path
Used to inspect loaded and unloaded vessels for orbital objectives and satellite-network counts. `KspVesselMonitor` creates `OrbitingVesselSnapshot` values and `OrbitalVesselTracker` evaluates them.

## How a current pre-orbit contract completes

1. `ObjectiveCatalogue` defines the objective and its measurable requirements.
2. `FundingContractCatalogue` creates its `ObjectiveFundingContract`.
3. `CampaignController` decides whether the funding contract is currently offered.
4. The offered objective becomes part of `ActiveFlightContracts`.
5. `ModRuntime` asks `KspVesselMonitor` for the telemetry needed by those active contracts.
6. `FlightContractTracker` evaluates the active vessel against the objective.
7. `AgencyState.RecordObjectiveCompletion()` records completion for the player.
8. `CampaignController` updates unlocks, funding and rival state on its normal refresh.
9. Persistence saves the changed state and the Command Center displays it.

## Where to make common changes

- Add/change an objective: `Objectives/ObjectiveCatalogue.cs`.
- Change a current active-flight completion rule: `Tracking/FlightContractTracker.cs`.
- Change which live vessel values are collected: `KspIntegration/KspVesselMonitor.cs`.
- Change unlock logic: `Objectives/UnlockRuleEvaluator.cs`.
- Change offers, sponsor reviews or campaign coordination: `Campaign/CampaignController.cs`.
- Change one-off objective funding: `Funding/ObjectiveFundingContract.cs`.
- Change satellite-network funding: `Funding/SatelliteNetworkFundingContract.cs` and `FundingContractCatalogue.cs`.
- Change rival behaviour: `Rivals/RivalSimulation.cs`.
- Change the Command Center: `UI/CommandCenterWindow.cs`.
- Change save/restore behaviour: `Persistence/` and `KspIntegration/ModPersistenceScenario.cs`.

## Boundaries to keep

- Direct KSP API access stays in `KspIntegration/` where practical.
- Tracking consumes project-owned snapshots rather than raw KSP vessel objects.
- `CampaignController` coordinates gameplay state but does not query KSP vessels directly.
- UI displays state but does not advance gameplay.
- Flight-contract tracking is generic; `PreOrbit` names are reserved for the current Kerbin contract family.

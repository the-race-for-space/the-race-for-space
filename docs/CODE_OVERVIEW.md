# Code Overview

This is the quickest guide to the current codebase.

Use this when you want to understand **what happens where** without reading every class.

For ownership rules, see [`STRUCTURE.md`](STRUCTURE.md).

## Core flow

```text
KSP / Unity
    |
    v
ModRuntime
    |
    +--> KspVesselMonitor
    |       |
    |       +--> FlightContractTracker
    |       +--> OrbitalVesselTracker
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
    +--> CommandCenterWindow reads the result
```

`ModRuntime` decides **when** work happens.

`KspVesselMonitor` reads **KSP state**.

The trackers decide **what the vessel state means**.

`CampaignController` coordinates **campaign progression**.

The UI only **displays** the current state.

## Main terms

### Campaign

The complete progression state for one KSP save.

### Agency

The player or a simulated rival.

### Objective

A gameplay goal that an agency can complete.

Examples:

- Directed Power I
- Control III
- Probe Orbit
- Mun Crewed Orbit

### Objective Funding Contract

One-off funding attached to an objective. It uses the declining ten-payment funding sequence.

### Satellite Network Funding Contract

Continuing funding based on qualifying satellites around a celestial body.

### Pre-Orbit Contract

One of the current twenty Kerbin contracts in the Directed Power, Mass, Control, or Biome lines.

### Flight Contract

Generic active-vessel contract infrastructure. The current Pre-Orbit contracts use it, but future Mun, Minmus, or other active-vessel contracts can use it too.

## Main classes

### `ModRuntime`

Location: `Core/ModRuntime.cs`

Owns the live campaign runtime for the current KSP game.

It schedules:

- frequent active-vessel Flight Contract samples;
- normal campaign refreshes;
- slower orbital vessel scans.

### `CampaignController`

Location: `Campaign/CampaignController.cs`

Coordinates the campaign.

It manages:

- agencies;
- funding-contract state;
- unlock progression;
- sponsor reviews;
- shared funding dates;
- rival updates;
- the active Flight Contract list.

### `AgencyState`

Location: `Agencies/AgencyState.cs`

Stores mutable state for one agency.

Examples:

- completed objective times;
- funds;
- satellite counts;
- rival mission progress.

### `ObjectiveDefinition`

Location: `Objectives/ObjectiveDefinition.cs`

Defines one objective and its requirements.

### `ObjectiveCatalogue`

Location: `Objectives/ObjectiveCatalogue.cs`

Creates the current objective set and stable IDs.

It includes the twenty Pre-Orbit objectives and the orbital objectives used by the wider campaign.

### `UnlockRuleEvaluator`

Location: `Objectives/UnlockRuleEvaluator.cs`

Interprets objective unlock rules.

The same rule logic is reused by campaign progression, rivals, tracking, and read-only UI progress.

### `ObjectiveFundingContract`

Location: `Funding/ObjectiveFundingContract.cs`

Stores the lifecycle of one one-off objective funding contract.

### `SatelliteNetworkFundingContract`

Location: `Funding/SatelliteNetworkFundingContract.cs`

Stores recurring network funding for one celestial body.

### `FundingContractCatalogue`

Location: `Funding/FundingContractCatalogue.cs`

Creates the code-owned funding contract set.

### `FlightContractTracker`

Location: `Tracking/FlightContractTracker.cs`

Evaluates frequent active-vessel telemetry.

Current Pre-Orbit uses:

- Directed Power — speed, altitude, and impact state;
- Mass — final mass and distance;
- Control — altitude-band hold, crew, and safe landing;
- Biome — current biome and landing state.

It can evaluate several offered contracts independently from the same flight.

### `OrbitalVesselTracker`

Location: `Tracking/OrbitalVesselTracker.cs`

Uses the slower vessel scan for:

- orbital objective completion;
- satellite-network counts.

It works with both loaded and unloaded vessel snapshots.

### `KspVesselMonitor`

Location: `KspIntegration/KspVesselMonitor.cs`

Reads KSP vessel state and converts it into project-owned snapshots.

This is where raw KSP vessel access belongs.

### `ModPersistenceScenario`

Location: `KspIntegration/ModPersistenceScenario.cs`

Connects the project-owned save-state classes to KSP's `ScenarioModule` save/load system.

### `RivalSimulation`

Location: `Rivals/RivalSimulation.cs`

Selects rival targets, spends rival funds, advances mission progress, and records simulated completions.

### `CommandCenterWindow`

Location: `UI/CommandCenterWindow.cs`

Draws the Command Center.

It reads campaign and Flight Contract state but does not advance gameplay.

## The two vessel paths

### 1. Flight Contract path

Use this when a contract needs frequent information from the actively controlled vessel.

```text
Active KSP vessel
    |
    v
KspVesselMonitor
    |
    v
ActiveVesselSnapshot
    |
    v
FlightContractTracker
```

The telemetry request is requirement-gated. If only Mass is active, there is no reason to query biome or enable Directed Power impact callbacks.

### 2. Orbital Vessel path

Use this when the mod needs to inspect the broader vessel population, including unloaded vessels.

```text
Loaded + unloaded KSP vessels
    |
    v
KspVesselMonitor
    |
    v
OrbitingVesselSnapshot list
    |
    v
OrbitalVesselTracker
```

This is slower and runs less often.

## Example: completing Mass II

1. `ObjectiveCatalogue` defines Mass II.
2. `FundingContractCatalogue` creates its objective funding contract.
3. `CampaignController` offers it after the correct progression and sponsor-review rules are met.
4. It becomes part of `ActiveFlightContracts`.
5. `FlightTelemetryPlan` requests Mass telemetry.
6. `KspVesselMonitor` captures active-vessel mass, launch position, current position, and situation.
7. `FlightContractTracker` checks the Mass II requirements.
8. On a valid Kerbin landing, `AgencyState.RecordObjectiveCompletion()` records the result.
9. `CampaignController` updates unlocks and funding state.
10. Persistence saves the change and the Command Center shows the updated state.

## Example: completing Probe Orbit

1. Probe Orbit is offered after any Pre-Orbit line reaches Level V.
2. A qualifying uncrewed Probe or Relay enters Kerbin orbit.
3. The slower vessel scan captures that vessel.
4. `OrbitalVesselTracker` evaluates the orbital objective.
5. The player's agency records Probe Orbit completion.
6. Campaign progression and funding update on the normal controller path.

## Where to make changes

| You want to change... | Start here |
| --- | --- |
| Objective definitions or thresholds | `Objectives/ObjectiveCatalogue.cs` |
| Unlock rules | `Objectives/UnlockRuleEvaluator.cs` |
| Active-vessel contract behaviour | `Tracking/FlightContractTracker.cs` |
| KSP telemetry collection | `KspIntegration/KspVesselMonitor.cs` |
| Orbital tracking | `Tracking/OrbitalVesselTracker.cs` |
| Sponsor review or campaign progression | `Campaign/CampaignController.cs` |
| Funding calculations | `Funding/` |
| Rival behaviour | `Rivals/RivalSimulation.cs` |
| Save-state models | `Persistence/` |
| KSP save/load hooks | `KspIntegration/ModPersistenceScenario.cs` |
| Command Center | `UI/CommandCenterWindow.cs` |

## Three rules to remember

1. **KSP API access stays in `KspIntegration` where practical.**
2. **Gameplay logic should use project-owned state and snapshots.**
3. **The UI displays state; it does not advance the campaign.**

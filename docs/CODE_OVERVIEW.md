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
    |       +--> ActiveVesselSnapshot + persistent part IDs
    |       |       |
    |       |       +--> FlightContractTracker
    |       |               +--> remembered FlightAttemptState records
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
    +--> CommandCenterWindow reads campaign/funding state
    +--> FlightActiveUI reads offered/live Flight Contract state
    +--> FundingNotificationUI publishes new player completions through KSP MessageSystem
```

`ModRuntime` decides **when** work happens.

`KspVesselMonitor` reads **KSP state**.

The trackers decide **what the vessel state means**.

`CampaignController` coordinates **campaign progression**.

The UI only **displays or reports** the current state.

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

### Flight Attempt

One remembered set of active-vessel Flight Contract history, such as launch origin, maximum speed/altitude, orbit state, and Control progress. Step 2 can keep several unrelated attempts in memory during the current session. Step 3 now supplies stable KSP part persistent IDs on each active-vessel snapshot; Step 4 will use that lineage for attempt selection instead of relying on temporary vessel ID changes.

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

A genuinely new gameplay completion recorded through `RecordObjectiveCompletion()` raises an internal completion signal. Persistence uses `RestoreObjectiveCompletion()` instead, which restores the same saved state without raising that signal. This is what prevents old achievements from being announced again when a save is loaded.

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

Evaluates frequent active-vessel telemetry and selects the remembered attempt associated with the currently sampled craft.

Current Pre-Orbit uses:

- Directed Power — speed, altitude, and impact state;
- Mass — final mass, distance, and landed/splashed state;
- Control — altitude-band hold, crew, and safe landed/splashed recovery;
- Biome — current biome and landed/splashed state.

It can evaluate several offered contracts independently from the same flight. It also retains independent in-memory attempt histories for unrelated vessel IDs, so A -> B -> A restores A's previous maxima/history during the same session instead of starting over.

Step 3 does not yet change the tracker selection rule: vessel-ID lookup and the same-launch fallback remain active. The tracker can now receive persistent part lineage through `ActiveVesselSnapshot`, and Step 4 will use that data to replace the temporary staging/switching identity rule.

### `FlightAttemptState`

Location: `Tracking/FlightAttemptState.cs`

Stores the mutable state for one remembered Flight Attempt. It contains the attempt's current KSP vessel/body identity, launch origin/time, historical maxima, orbit state, current presentation telemetry, and independent Control-contract state. It does not evaluate contract rules itself.

### `ActiveVesselSnapshot`

Location: `Tracking/ActiveVesselSnapshot.cs`

Carries KSP-independent active-vessel values into the tracker. Alongside telemetry and launch context it now includes a read-only list of KSP part persistent IDs represented only as primitive `uint` values. The constructor copies that list so later KSP-side collection changes cannot mutate an already captured snapshot.

### `OrbitalVesselTracker`

Location: `Tracking/OrbitalVesselTracker.cs`

Uses the slower vessel scan for:

- orbital objective completion;
- satellite-network counts.

It works with both loaded and unloaded vessel snapshots.

### `KspVesselMonitor`

Location: `KspIntegration/KspVesselMonitor.cs`

Reads KSP vessel state and converts it into project-owned snapshots.

For the Flight Contract path it now walks only the active loaded vessel's existing `parts` list and copies every non-zero `Part.persistentId` into `ActiveVesselSnapshot`. Raw KSP `Part` objects remain inside `KspIntegration`; the tracking layer sees only primitive IDs. The deduplicated telemetry status line includes the number of persistent part IDs captured, which gives an in-game diagnostic for this boundary.

This is where raw KSP vessel access belongs.

### `ModPersistenceScenario`

Location: `KspIntegration/ModPersistenceScenario.cs`

Connects the project-owned save-state classes to KSP's `ScenarioModule` save/load system.

The current `FLIGHT_CONTRACT_PROGRESS` format still serializes only the currently active Flight Attempt. Inactive attempts remembered by Step 2 and the new Step 3 part-lineage IDs are session-only until the planned multi-attempt persistence step.

### `RivalSimulation`

Location: `Rivals/RivalSimulation.cs`

Selects rival targets, spends rival funds, advances mission progress, and records simulated completions.

### `CommandCenterWindow`

Location: `UI/CommandCenterWindow.cs`

Draws the full Command Center with Overview, Funding Targets, Rival Agencies, and Contract Catalogue views.

It reads campaign and funding state but does not advance gameplay. Live Flight Contract requirement telemetry is deliberately left to `FlightActiveUI`.

### `FlightActiveUI`

Location: `UI/FlightActiveUI.cs`

Draws the compact Flight-only **Offered Contracts** window.

It has its own Flight-scene launcher button and reads the same controller/tracker state already owned by `ModRuntime`. Player-uncompleted Offered objective contracts appear first and can be expanded independently by stable contract ID. Offered contracts already completed by the player sit at the bottom marked `Complete` with no expand/collapse control.

Expanded Pre-Orbit contracts show the live requirements already tracked for Directed Power, Mass, Control, and Biome. Other objective types use their normal objective description. The window does not query KSP vessels or create a second telemetry loop.

### `FundingNotificationUI`

Location: `UI/FundingNotificationUI.cs`

Publishes the stock KSP inbox notice for a newly completed player Objective Funding Contract.

The class subscribes once for the KSP session to `AgencyState.ObjectiveCompletionRecorded`. Player completions are queued by stable objective ID until KSP's `MessageSystem` is ready, then resolved back to the current `ObjectiveFundingContract`. A message is sent only when the matching funding target is currently Offered and unexpired. Rival completions are ignored.

The approved notification format is:

```text
Funding Target Completed — Control II
Control II has been achieved. Your agency is now eligible for a share of the remaining contract funding.
```

The message uses KSP's green message styling and normal message icon. It does not complete objectives, calculate funding, or add another polling/telemetry loop. Loading saved objective completions is silent, so historical achievements do not generate duplicate inbox entries.

## The two vessel paths

### 1. Flight Contract path

Use this when a contract needs frequent information from the actively controlled vessel.

```text
Active KSP vessel
    |
    v
KspVesselMonitor
    |
    +--> condition telemetry
    +--> Part.persistentId values
    |
    v
ActiveVesselSnapshot
    |
    v
FlightContractTracker
    |
    +--> select/create current FlightAttemptState
    +--> campaign completion evaluation
    +--> read-only FlightActiveUI presentation
```

The telemetry request is requirement-gated. If only Mass is active, there is no reason to query biome or enable Directed Power impact callbacks. Persistent part IDs are common attempt identity context, so they are captured whenever this already-active snapshot path runs.

Only the active vessel is sampled at the normal fast-path cadence. Remembered inactive attempts are passive state; switching back to one reselects it from memory and resumes its historical maxima and qualified state. An unfinished continuous Control hold still resets when the observation gap is too long, because the tracker cannot prove continuity while the vessel was not observed.

Step 3 adds one pass over the active vessel's loaded part list per captured telemetry sample. It does not scan other vessels or add a new timer.

UI visibility does not change the telemetry cadence; the tracker continues to be updated by `ModRuntime` whether the compact window is open or closed.

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
6. `KspVesselMonitor` captures active-vessel mass, launch position, current position, situation, and persistent part IDs.
7. `FlightContractTracker` selects the active craft's remembered Flight Attempt and checks the Mass II requirements.
8. On a valid Kerbin landing or splashdown, `AgencyState.RecordObjectiveCompletion()` records the result and raises the new-completion signal.
9. `FundingNotificationUI` posts the stock funding-target completion message for Mass II.
10. `CampaignController` updates unlocks and funding state.
11. Persistence saves the change; the Command Center and FlightActiveUI read the resulting state for presentation.

## Example: completing Probe Orbit

1. Probe Orbit is offered after any Pre-Orbit line reaches Level V.
2. A qualifying uncrewed Probe or Relay enters Kerbin orbit.
3. The slower vessel scan captures that vessel.
4. `OrbitalVesselTracker` evaluates the orbital objective.
5. The player's agency records Probe Orbit completion and emits the same completion signal.
6. `FundingNotificationUI` posts the stock completion notice if Probe Orbit is still an Offered, unexpired funding target.
7. Campaign progression and funding update on the normal controller path.

## Where to make changes

| You want to change... | Start here |
| --- | --- |
| Objective definitions or thresholds | `Objectives/ObjectiveCatalogue.cs` |
| Unlock rules | `Objectives/UnlockRuleEvaluator.cs` |
| Active-vessel contract behaviour | `Tracking/FlightContractTracker.cs` |
| Flight Attempt state | `Tracking/FlightAttemptState.cs` |
| Active-vessel snapshot data | `Tracking/ActiveVesselSnapshot.cs` |
| KSP telemetry or lineage collection | `KspIntegration/KspVesselMonitor.cs` |
| Orbital tracking | `Tracking/OrbitalVesselTracker.cs` |
| Sponsor review or campaign progression | `Campaign/CampaignController.cs` |
| Funding calculations | `Funding/` |
| Rival behaviour | `Rivals/RivalSimulation.cs` |
| Save-state models | `Persistence/` |
| KSP save/load hooks | `KspIntegration/ModPersistenceScenario.cs` |
| Full Command Center | `UI/CommandCenterWindow.cs` |
| Compact Flight contract window | `UI/FlightActiveUI.cs` |
| Funding completion inbox messages | `UI/FundingNotificationUI.cs` |

## Three rules to remember

1. **KSP API access stays in `KspIntegration` where practical.** Presentation classes may use KSP's stock UI APIs for their own display responsibility.
2. **Gameplay logic should use project-owned state and snapshots.**
3. **The UI displays or reports state; it does not advance the campaign.**

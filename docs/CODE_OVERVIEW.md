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
    |       +--> ActiveVesselSnapshot
    |       |       +--> persistent part IDs
    |       |       +--> reference/control part persistent ID
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

One remembered set of active-vessel Flight Contract history, such as launch origin, maximum speed/altitude, orbit state, and Control progress. Several attempts can remain in memory during the current session. Each attempt remembers the persistent-part lineage that owns that history, while KSP vessel ID is retained only as a fast lookup cache.

A docked KSP vessel may contain several Flight Attempt lineages at once. Their histories stay separate. The lineage containing KSP's current reference/control part is the one shown/evaluated as the active attempt; `Control From Here` can move that selection to another remembered lineage without combining their historical progress.

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

It can evaluate several offered contracts independently from the same flight and retains independent in-memory histories for several craft.

Normal unchanged-vessel samples use the direct vessel-ID dictionary when its cached attempt still owns the current reference/control part. If the vessel ID changes, conflicts with remembered lineage, or a docked vessel's reference part belongs to another remembered attempt, the tracker resolves the correct attempt from persistent-part lineage instead.

When staging or another split removes parts, the continued attempt keeps only the persistent IDs still present on the actively controlled branch. A detached branch therefore cannot later inherit the parent's historical maxima or Control qualification merely because it has the same launch time. The previous launch-time/body fallback is used only for lineage-less attempts, mainly the current single-attempt restore path before its first normal snapshot.

Docking never merges remembered histories or automatically absorbs the other craft's parts into the selected attempt. A combined vessel may contain several remembered lineages. The lineage containing `ActiveVesselSnapshot.ReferencePartPersistentId` receives live telemetry. If the player uses KSP's `Control From Here` on the other docked craft, that other attempt becomes active while retaining its own earlier maxima/origin/state. After undocking, each branch is matched back to its own persistent-part lineage.

If KSP temporarily has no usable reference-part ID, the tracker keeps the current matching attempt when possible and otherwise uses the strongest existing persistent-part overlap so it does not invent one merged history from several docked craft.

Step 5 is intentionally about identity and history separation only. Step 7 will define contract-specific topology rules, especially how Mass should treat attached mass from another lineage and whether docking should invalidate an unfinished continuous Control hold.

### `FlightAttemptState`

Location: `Tracking/FlightAttemptState.cs`

Stores the mutable state for one remembered Flight Attempt. It contains the attempt's current KSP vessel/body identity, launch origin/time, historical maxima, orbit state, current presentation telemetry, independent Control-contract state, and its current set of remembered persistent-part IDs. It does not evaluate contract rules itself.

The lineage set is seeded from the attempt's first snapshot and is narrowed when parts leave the actively continued branch. Newly attached parts are not automatically added, so docking cannot silently merge independent histories.

### `ActiveVesselSnapshot`

Location: `Tracking/ActiveVesselSnapshot.cs`

Carries KSP-independent active-vessel values into the tracker. Alongside telemetry and launch context it includes a read-only list of KSP part persistent IDs represented only as primitive `uint` values plus `ReferencePartPersistentId`, the persistent ID of KSP's current reference/control part. The constructor copies the part-ID list so later KSP-side collection changes cannot mutate an already captured snapshot.

### `OrbitalVesselTracker`

Location: `Tracking/OrbitalVesselTracker.cs`

Uses the slower vessel scan for:

- orbital objective completion;
- satellite-network counts.

It works with both loaded and unloaded vessel snapshots.

### `KspVesselMonitor`

Location: `KspIntegration/KspVesselMonitor.cs`

Reads KSP vessel state and converts it into project-owned snapshots.

For the Flight Contract path it walks only the active loaded vessel's existing `parts` list and copies every non-zero `Part.persistentId` into `ActiveVesselSnapshot`. It also reads KSP's current reference transform part through `GetReferenceTransformPart()`, falling back to the vessel root part during transient topology changes, and passes only that part's persistent ID across the integration boundary. Raw KSP `Part` objects remain inside `KspIntegration`.

The deduplicated telemetry status line includes both the number of persistent part IDs and the current reference-part ID, which gives an in-game diagnostic for staging/docking lineage selection.

This is where raw KSP vessel access belongs.

### `ModPersistenceScenario`

Location: `KspIntegration/ModPersistenceScenario.cs`

Connects the project-owned save-state classes to KSP's `ScenarioModule` save/load system.

The current `FLIGHT_CONTRACT_PROGRESS` format still serializes only the currently active Flight Attempt. Inactive attempts and their Step 5 lineage relationships remain session-only until the planned multi-attempt persistence step.

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
    +--> reference/control Part.persistentId
    |
    v
ActiveVesselSnapshot
    |
    v
FlightContractTracker
    |
    +--> vessel-ID cache when reference lineage matches
    +--> reference-part ownership for docked assemblies
    +--> persistent-part overlap when identity changes
    +--> select/create current FlightAttemptState
    +--> campaign completion evaluation
    +--> read-only FlightActiveUI presentation
```

The telemetry request is requirement-gated. If only Mass is active, there is no reason to query biome or enable Directed Power impact callbacks. Persistent part IDs and the reference-part ID are common attempt identity context, so they are captured whenever this already-active snapshot path runs.

Only the active vessel is sampled at the normal fast-path cadence. Remembered inactive attempts are passive state. Most samples use the O(1) vessel-ID cache plus one reference-part membership check. The tracker scans the small in-memory attempt collection only when the current reference part belongs to a different history, vessel identity changes/conflicts, or a fallback overlap match is required.

When a split/staged branch continues the attempt, its lineage is narrowed to the persistent parts still on that branch. When several lineages are docked together they remain separate; only the selected reference lineage receives the current sample. An unfinished continuous Control hold still resets when the observation gap is too long, because the tracker cannot prove continuity while that attempt was not observed.

The persistent-part capture adds one pass over the active vessel's loaded part list per captured telemetry sample and one reference-part lookup. It does not scan other KSP vessels or add a new timer.

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
6. `KspVesselMonitor` captures active-vessel mass, launch position, current position, situation, persistent part IDs, and the reference/control part ID.
7. `FlightContractTracker` resolves the active craft's remembered Flight Attempt without merging any other docked lineage and checks the Mass II requirements.
8. On a valid Kerbin landing or splashdown, `AgencyState.RecordObjectiveCompletion()` records the result and raises the new-completion signal.
9. `FundingNotificationUI` posts the stock funding-target completion message for Mass II.
10. `CampaignController` updates unlocks and funding state.
11. Persistence saves the change; the Command Center and FlightActiveUI read the resulting state for presentation.

The current Mass evaluator still sees KSP's total active-vessel mass. Preventing unrelated docked mass from satisfying another attempt is explicitly reserved for Step 7.

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

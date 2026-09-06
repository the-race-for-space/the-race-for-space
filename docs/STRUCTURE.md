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
    |        +--> persistent part IDs
    |        +--> reference/control part persistent ID
    |        |
    |        v
    |    FlightContractTracker
    |        |
    |        +--> remembered FlightAttemptState records
    |                    |
    |                    v
    |            FLIGHT_CONTRACT_PROGRESS
    |              repeated ATTEMPT nodes
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
    +--> UI reads state / publishes stock KSP notifications

ModRuntime schedules the work above.
```

The important rule is that **raw KSP objects stay at the integration boundary**. Gameplay logic should work with project-owned state and snapshots.

## Source modules

### `Core/`

Owns runtime scheduling and campaign-wide settings.

Main classes:

- `ModRuntime` — persistent KSP-session scheduler that owns the live `CampaignController` and `FlightContractTracker` for the current KSP save.
- `CampaignSettings` — reads and exposes campaign balance settings.

`ModRuntime` starts once for the KSP session and survives normal scene changes. KSP can replace `HighLogic.CurrentGame` during those transitions, so campaign ownership uses `HighLogic.SaveFolder` as the stable save identity instead of `Game` object reference equality. Loading a different save folder replaces the controller/tracker and active-vessel callback state; returning to the main menu releases current-save state so reloading the same folder later restores it cleanly.

Current runtime cadences:

- active Flight Contract telemetry: about once per second;
- normal campaign/controller refresh: about every five seconds;
- broad loaded/unloaded vessel refresh: about every twenty seconds.

The broad refresh serves orbital tracking and now also supplies the persistent-part population used for conservative Flight Attempt lifecycle pruning. The UI does not own these timers.

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

`AgencyState.RecordObjectiveCompletion` raises an internal completion signal only when gameplay records a genuinely new objective. Persistence uses the separate silent restore path so historical achievements do not look like new completions after loading a save. UI code may observe that signal, but it must not record objectives itself.

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
2. **Satellite Network Funding Contracts** — continuing network funding based on qualifying satellites around a body.

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
- `FlightAttemptState`
- `FlightTelemetryPlan`
- `ActiveVesselSnapshot`
- `SurfaceImpactEvaluator`

This path uses frequent telemetry from the actively controlled vessel.

`FlightContractTracker` retains multiple `FlightAttemptState` records in memory. Switching from unrelated Craft A to Craft B and later back to A reselects A's previous history instead of deleting it. Only the active vessel is sampled; inactive attempt records are passive state and do not create extra vessel scans or evaluation loops.

`ActiveVesselSnapshot` carries the non-zero KSP `Part.persistentId` values present on the active loaded vessel plus the persistent ID of KSP's current reference/control part. `KspVesselMonitor` converts these to project-owned primitive `uint` values before they leave `KspIntegration`, so lineage matching does not need raw KSP `Part` objects.

`FlightContractTracker` treats the remembered-attempt list as authoritative and keeps KSP vessel ID only as a fast cache for normal unchanged-vessel samples. When a previously unseen or conflicting vessel ID appears with persistent parts that overlap a remembered attempt, that attempt is rebound to the new vessel ID instead of starting over.

During staging/splitting, the continued attempt narrows its remembered lineage to the persistent parts still present on the actively controlled branch. Parts that separated away are removed from that attempt's lineage, so switching later to the detached branch does not clone the parent's maximum speed, altitude, orbit state, or Control history even when both branches share the same original launch time. The launch-time/body fallback remains only for attempts that genuinely have no persistent-part lineage.

Docking does **not** merge Flight Attempt histories. A combined KSP vessel may contain parts belonging to several remembered attempts at once. The lineage containing KSP's current reference/control part is the attempt selected for live telemetry. Using KSP's `Control From Here` to move the reference part to another remembered lineage selects that lineage instead. Newly attached parts are not absorbed into either history, and after undocking each branch can recover its own attempt from its surviving persistent parts. If KSP temporarily cannot provide a usable reference-part identity, the tracker prefers the currently active matching attempt and otherwise uses the strongest persistent-part overlap rather than inventing a merged attempt.

Contract-specific topology rules sit in this same tracking layer. For Mass, a selected attempt cannot complete while the current KSP vessel contains persistent parts outside that attempt's own lineage; the unrelated docked mass is therefore never accepted as qualifying delivery mass. After those external parts are detached, normal stock vessel mass is used again. For Control, `FlightAttemptState` remembers the current set of externally attached persistent parts. Adding or removing those external parts resets only unfinished continuous holds; a Control objective that already qualified keeps its qualification. Ordinary staging of the attempt's own lineage is reconciled before this comparison and does not count as an external attachment change.

Lifecycle pruning is deliberately based on **part existence, not time or inactivity**. After a successful broad loaded/unloaded vessel refresh, `FlightContractTracker` compares each remembered lineage with the persistent-part population reported by `KspVesselMonitor`. An attempt is removed only when none of its remembered lineage parts exist anywhere in the current save. Parked, docked, unloaded, or long-running craft therefore keep their histories indefinitely while at least one lineage part survives. Lineage-less attempts are retained because absence cannot be proven safely.

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

The same successful broad refresh also provides the persistent-part population used for Flight Attempt pruning. Loaded vessels contribute live `Part.persistentId` values because their `ProtoVessel` can lag after topology changes; unloaded vessels contribute `ProtoPartSnapshot.persistentId` values.

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

`FLIGHT_CONTRACT_PROGRESS` now contains repeated `ATTEMPT` children rather than one set of root attempt values. Each `ATTEMPT` stores its last KSP vessel/body identity, launch time/origin, last sample time, historical Directed Power maxima/orbit state, zero or more `PART_LINEAGE` persistent IDs, and zero or more per-objective `CONTROL_STATE` children. At most one attempt is marked `selected = true`; it is the history that was selected by the tracker when the save snapshot was captured.

Different attempts may legitimately have the same last KSP vessel ID after docking. Persistence therefore does not use vessel ID as unique history identity. Persistent-part ownership is serialized separately, and duplicate part ownership across saved attempts is treated as malformed progress rather than guessing which history owns the part.

This Step 6 format intentionally does **not** load the previous single-attempt root layout. Compatibility with earlier development builds was explicitly not required for this change.

The transient set of parts currently attached from outside an attempt's lineage is not another persisted field. After load, the first usable active-vessel snapshot establishes that topology baseline from the saved lineage plus the live KSP part IDs. This prevents a restored partial Control hold from being reset merely because the runtime itself was recreated; later attachment changes are then detected normally.

When lifecycle pruning removes obsolete attempts, `ModRuntime` immediately refreshes the captured Flight Contract persistence state. The next normal KSP save therefore omits those dead/recovered histories without changing the `FLIGHT_CONTRACT_PROGRESS` schema.

Command Center visibility is stored separately as a value on the ScenarioModule node. Funding-completion notifications do not add another save section; restored objective completions are applied silently and only new gameplay completions generate notifications.

### `KspIntegration/`

Owns direct KSP and Unity interaction.

Main classes include:

- `KspVesselMonitor`
- `ModPersistenceScenario`
- config and launcher/event integration classes.

Responsibilities include:

- reading active and persistent vessel state;
- handling loaded and unloaded vessels;
- capturing active-vessel KSP part persistent IDs and the current reference/control-part persistent ID for Flight Attempt lineage;
- capturing the persistent-part population during the existing broad loaded/unloaded vessel refresh for lifecycle pruning;
- listening for KSP destruction events used by Directed Power;
- converting KSP data into project-owned snapshots;
- ScenarioModule save/load hooks;
- loading `CampaignSettings.cfg`;
- Career-funds integration.

Raw `Vessel`, `Part`, `ProtoVessel`, `HighLogic`, `FlightGlobals`, and similar KSP types should remain here where practical.

### `UI/`

Owns presentation for the full Command Center, the compact Flight-only contract tracker, and stock KSP completion notices.

Main classes:

- `CommandCenterWindow` — the full campaign interface with Overview, Funding Targets, Rival Agencies, and Contract Catalogue views.
- `FlightActiveUI` — a separate Flight-scene window for quickly checking Offered objective requirements while controlling a vessel.
- `FundingNotificationUI` — a session-level presentation subscriber that publishes stock KSP inbox messages for newly completed player funding targets.

`FlightActiveUI` owns its own Flight-only stock launcher button and window visibility. It lists player-uncompleted Offered objective contracts first, allows each unfinished contract to expand independently by stable contract ID, and places Offered contracts already completed by the player at the bottom marked `Complete` with no expansion control. Expanded Pre-Orbit contracts display the current requirement state from `ModRuntime.FlightContractTrackingState`; other objective types fall back to their normal objective description.

`FundingNotificationUI` listens to the internal `AgencyState` completion signal, queues the stable objective ID until KSP's `MessageSystem` is available, resolves the matching `ObjectiveFundingContract`, and sends a green stock message only when that contract is currently Offered and unexpired. Rival completions are ignored. The approved message format is:

```text
Funding Target Completed — Control II
Control II has been achieved. Your agency is now eligible for a share of the remaining contract funding.
```

All three UI classes are presentation-only consumers. They must not complete objectives, advance rivals, process funding, sample KSP vessels, or create another telemetry cadence. `CommandCenterWindow` keeps Funding Targets focused on funding and contract-lifecycle information, `FlightActiveUI` is the dedicated live Flight Contract presentation, and `FundingNotificationUI` reports new player completions through the stock inbox. Active-vessel sampling remains the single `ModRuntime` path.

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
6. `KspVesselMonitor` captures only those required condition values plus common attempt context, including the active vessel's persistent part IDs and current reference/control-part ID.
7. `FlightContractTracker` resolves the correct remembered `FlightAttemptState` from vessel cache, reference-part ownership, or persistent-part overlap without merging independent docked histories, applies the topology rules for that selected attempt, then evaluates the snapshot against every active contract independently.
8. `AgencyState` records each completed objective and emits the new-completion signal.
9. `FundingNotificationUI` may publish the stock funding-completion notice for the player.
10. `CampaignController` settles unlocks, offers, funding, and rival state on its normal refresh.
11. On a successful broad vessel refresh, `FlightContractTracker` prunes remembered attempts whose complete persistent-part lineage has disappeared from the save.
12. `FlightContractProgressSaveState` captures the remaining remembered Flight Attempts for the normal KSP save path; the UI reads the controller and tracker state for presentation.

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
- finish either `LANDED` or `SPLASHED` on Kerbin;
- have no persistent parts attached from outside the selected Flight Attempt lineage when completion is checked.

### Control

- have crew aboard;
- remain continuously inside the contract's altitude band for the required time;
- adding or removing externally attached parts resets an unfinished continuous hold;
- ordinary staging of the attempt's own lineage does not count as that external topology change;
- after qualification, finish either landed or splashed safely on Kerbin with crew;
- once qualified, later docking/undocking does not erase the completed hold;
- each offered Control contract keeps independent hold state.

### Biome

- reach the target Kerbin biome;
- finish either `LANDED` or `SPLASHED` while KSP still reports that target biome;
- flying over a biome without finishing there does not count.

## Tracking and performance rules

The active-vessel path is requirement-gated.

Examples:

- mass is queried only while an active Mass contract needs it;
- biome is queried only while an active Biome contract needs it;
- crew count is queried only while Control needs it;
- Directed Power destruction tracking is enabled only while Directed Power requires impact telemetry.

When there are no active Flight Contracts, the fast path should avoid unnecessary active-vessel discovery and evaluation.

Remembered inactive Flight Attempts are not sampled or scanned through KSP on the one-second path. Normal unchanged-vessel samples still use the direct vessel-ID cache and a constant-time lineage/reference check. The small in-memory attempt collection is scanned only when the current reference part belongs to a different remembered lineage, vessel identity changes/conflicts, or a fallback overlap match is required after a topology change.

Persistent part IDs and the current reference-part ID are captured only while the existing active-vessel snapshot is already being built. This adds one pass over the **active vessel's** loaded part list per telemetry sample plus one KSP reference-part lookup; it does not scan inactive vessels or ProtoVessels. Mass anti-combination and Control topology detection reuse those already-captured persistent IDs and do not make another KSP call or vessel scan.

Lifecycle pruning adds **no new recurring scan or timer**. The existing approximately twenty-second loaded/unloaded vessel refresh already walks the broad KSP vessel population for orbital tracking. That same pass now collects each surviving persistent part ID, then the tracker compares its small remembered-attempt collection against that primitive set. Pruning happens only after a successful refresh and only after Flight Contract persistence has been restored for the current save.

The persistence capture also copies remembered attempt state into project-owned save-state objects after the normal Flight Contract sample. This is in-memory work only; it does not trigger another KSP vessel scan. When pruning removes one or more attempts, the captured persistence state is refreshed immediately so obsolete histories are not written by a later save.

UI visibility does not own or change the telemetry sampling frequency. `FlightActiveUI` reads the existing tracker state maintained by `ModRuntime`; `CommandCenterWindow` no longer draws live Flight Contract requirement telemetry. `FundingNotificationUI` is event-driven and does not add another vessel or contract-evaluation loop.

The slower broad vessel scan remains separate because orbital tracking and lifecycle pruning must consider loaded and unloaded vessels.

## Save-state ownership

`CAMPAIGN_FUNDING` stores:

- player objective-completion timestamps;
- objective funding lifecycle state;
- satellite-network funding lifecycle state;
- next shared funding time.

`RIVAL_AGENCIES` stores each rival by stable agency ID.

`FLIGHT_CONTRACT_PROGRESS` stores **all remembered Flight Attempts**, each with:

- last KSP vessel/body identity and launch origin/time;
- last sample time;
- Directed Power maximum speed/altitude and orbit invalidation;
- persistent-part lineage through repeated `PART_LINEAGE` nodes;
- independent per-objective `CONTROL_STATE` entries;
- whether that attempt was selected when persistence was captured.

Because lineage is saved for every attempt, saving while Craft B is active no longer discards Craft A. After reload, switching back to A, staging, docking, or undocking can recover A's existing historical progress from its surviving persistent parts. Two docked attempts can even share the same last KSP vessel ID in the save without being merged.

Once a successful broad KSP vessel refresh proves that **none** of one attempt's saved lineage parts still exist, that attempt is removed before the next persistence capture. No age or inactivity field is needed. A parked or unloaded craft remains saved as long as at least one remembered lineage part still exists.

Instantaneous live telemetry such as current altitude, current mass, current biome, crew count, current reference part, and the current external-attachment topology is rebuilt from the next active-vessel sample instead of being treated as authoritative saved state.

## Tests

Two KSP-independent suites are run by:

```bash
bash tools/run-logic-tests.sh
```

- `tests/TheRaceForSpace.Tests/` — domain, tracking, funding, rivals, and persistence.
- `tests/TheRaceForSpace.ControllerTests/` — real `CampaignController` orchestration against test-only KSP boundary stubs.

`.github/workflows/logic-tests.yml` runs the same script in CI.

The Flight Contract regression suite includes lineage-specific checks that the actively continued stage retains parent history, a detached same-launch branch does not receive a cloned copy, a docked assembly keeps two remembered histories separate even if KSP reuses one craft's vessel ID, `Control From Here` can select the other lineage, both histories are recovered after undocking, both histories still survive a save/load round trip while docked, unrelated docked parts cannot supply Mass completion, docking/undocking resets unfinished Control holds without erasing qualified Control state, and an attempt whose entire lineage disappears is pruned without deleting another surviving craft or reappearing in persistence.

Direct KSP API behaviour still requires an in-game test. `KspVesselMonitor` includes the captured persistent-part count and reference-part ID in its deduplicated Flight telemetry status line, and Step 8 additionally depends on live `Part.persistentId` plus unloaded `ProtoPartSnapshot.persistentId` values during the broad vessel refresh. See [`KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md).

## Where common changes belong

| Change | Primary location |
| --- | --- |
| Add or change an objective | `Objectives/ObjectiveCatalogue.cs` |
| Change unlock logic | `Objectives/UnlockRuleEvaluator.cs` |
| Change active-vessel contract evaluation | `Tracking/FlightContractTracker.cs` |
| Change Flight Attempt state | `Tracking/FlightAttemptState.cs` |
| Change live vessel values or lineage IDs collected | `KspIntegration/KspVesselMonitor.cs` |
| Change orbital vessel evaluation | `Tracking/OrbitalVesselTracker.cs` |
| Change sponsor reviews or campaign coordination | `Campaign/CampaignController.cs` |
| Change one-off funding | `Funding/ObjectiveFundingContract.cs` |
| Change satellite-network funding | `Funding/SatelliteNetworkFundingContract.cs` |
| Change rival behaviour | `Rivals/RivalSimulation.cs` |
| Change save-state models | `Persistence/` |
| Change KSP save hooks | `KspIntegration/ModPersistenceScenario.cs` |
| Change Command Center presentation | `UI/CommandCenterWindow.cs` |
| Change compact Flight contract presentation | `UI/FlightActiveUI.cs` |
| Change stock funding-completion notifications | `UI/FundingNotificationUI.cs` |

## Structure rule

Extend the existing modules before creating new ones. If a feature genuinely requires moving ownership, adding a major module, changing a public API, or breaking save/config compatibility, follow the structural-change gate in [`../AGENTS.md`](../AGENTS.md).
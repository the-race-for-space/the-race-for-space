# Code Overview

This is a practical walkthrough of the current **Content v0.6** codebase. Read [`STRUCTURE.md`](STRUCTURE.md) for stricter ownership rules and `AGENTS.md` before making changes.

## The big picture

The mod has three broad kinds of work:

1. **Read KSP** through `KspIntegration`.
2. **Evaluate/advance project-owned gameplay state** through Tracking, Rivals, Funding, Objectives, Agencies, and Campaign.
3. **Present the result** through UI.

`ModRuntime` supplies the existing runtime heartbeat. `CampaignController` coordinates campaign-wide ordering. No UI component and no rival specialist creates an independent realtime simulation loop.

```text
KSP APIs
  |
  +--> KspVesselMonitor --------> Tracking ------------------+
  |                                                         |
  +--> KspScienceAdapter -------> Rival Science ------------+--> CampaignController
  |                                                         |       |
  +--> CareerFundingAdapter / persistence / config ---------+       +--> Persistence
                                                                  |
                                                                  +--> UI reads state
```

---

# Core concepts

## Campaign

One KSP save's overall progression, funding, agencies, offered contracts, and shared timing.

Main coordinator: `Campaign/CampaignController.cs`.

## Agency

The player or one simulated rival.

Common state lives in `Agencies/AgencyState.cs`. Rival-only programme state is composed through `Agencies/RivalProgramState.cs`.

## Objective

A code-defined gameplay achievement such as Directed Power I, Probe Orbit, or Mun Crewed Orbit.

Definitions live in `Objectives/ObjectiveCatalogue.cs` / `ObjectiveDefinition.cs`.

## Funding Contract

The sponsorship attached to campaign goals.

- `ObjectiveFundingContract` — one-off objective funding with declining payouts.
- `SatelliteNetworkFundingContract` — repeatable network funding based on qualifying satellites.

## Launch Progress

Preparation progress for a rival mission. Reaching 100% means **ready to launch**, not “objective completed”.

Normal Contract preparation fields remain on `AgencyState`. Science preparation lives in `RivalProgramState`.

## Live Mission

A launched rival Contract or Science mission stored until its completion universal time. Outcome, crew assignment, Difficulty, success chance, location, duration, and seed are snapshotted at launch.

## Science Expedition

A rival mission for one stock Science subject. Rival gameplay uses `ScienceSubjectKey`; raw KSP Science subjects never leave `KspIntegration/KspScienceAdapter.cs`.

---

# Runtime and campaign coordination

## `Core/ModRuntime.cs`

Think of `ModRuntime` as the clock and runtime owner.

It:

- owns the current `CampaignController`;
- samples active-vessel Flight Contract telemetry on the faster cadence;
- runs broader campaign refreshes on the existing slower cadence;
- connects player Flight Contract completion back into campaign state;
- exposes current tracking state to UI.

It should not gain rival facility formulas, funding lifecycle rules, or direct UI logic.

## `Campaign/CampaignController.cs`

Think of `CampaignController` as the campaign traffic controller.

It owns the cross-system sequence, not every rule used in that sequence.

Important responsibilities:

- create current agencies and funding contracts;
- restore save state before gameplay advances;
- refresh player orbital state;
- invoke rival chronology with Science callbacks;
- evaluate unlocks and sponsor reviews;
- start/advance funding lifecycles;
- calculate player/rival payouts;
- complete/start rival research and construction at funding boundaries;
- persist updated state;
- maintain the current active Pre-Orbit Flight Contract plan.

### Funding boundary sequence

A funding day is deliberately ordered:

```text
catch up rival events
  -> finish due research/construction
  -> update eligibility/start funding
  -> calculate/apply income, payroll, insurance
  -> advance active one-off payouts
  -> start new research
  -> start new construction
  -> sponsor review
```

If multiple funding boundaries were crossed during timewarp, the controller processes them one at a time.

---

# Agencies and persistent rival state

## `Agencies/AgencyState.cs`

Contains state used by both player and rivals:

- stable ID/name;
- player/rival identity;
- Funds and `NextPayoutFunds`;
- objective completion timestamps;
- satellite counts;
- rival normal Contract target / Launch Progress / progress-check UT / ready UT.

A new objective completion emits the live completion signal used by notifications. Persistence restoration uses `RestoreObjectiveCompletion` instead so loading a save stays silent.

## `Agencies/RivalProgramState.cs`

Contains the rest of the v0.6 rival programme:

- Stored Science;
- Science preparation;
- live missions;
- completed Science subjects;
- researched tech IDs;
- current research;
- nine facility levels;
- facility construction collection;
- employed Kerbals;
- pending insurance.

This file also defines lightweight project-owned state classes. These are intentionally serializable/testable without KSP types.

---

# Objective and funding definitions

## `Objectives/ObjectiveDefinition.cs`

Holds immutable objective metadata.

For rivals, every supported Contract objective now also has:

- `Difficulty`;
- `RequiredKerbalCount`.

Those values are used when a live rival Contract mission launches.

Pre-Orbit definitions also contain the active-flight rule data used by `FlightContractTracker`.

## `Objectives/ObjectiveCatalogue.cs`

Owns stable objective IDs and the code-defined campaign objective list.

If you add a new objective, its stable ID and immutable gameplay definition normally start here.

## `Objectives/UnlockRuleDefinition.cs` and `UnlockRuleEvaluator.cs`

Represent and evaluate prerequisites without embedding special cases in UI.

Current conditions can depend on objective completions, agency scope/count, satellite counts, and universal time.

## `Funding/FundingContractCatalogue.cs`

Creates fresh funding-contract state for the objective catalogue and satellite networks using current `CampaignSettings` balance.

The catalogue defines content relationships; current offer/start/expiry state belongs to each funding-contract instance.

---

# Rival programme simulation

The v0.6 rival code is intentionally split by responsibility.

## `Rivals/RivalSimulation.cs`

This is the chronological coordinator.

It:

- selects only valid Offered targets;
- advances normal Contract Launch Progress at stored check times;
- asks `RivalScienceSimulation` to maintain/advance Science preparation;
- launches ready Contract or Science missions when crew/capacity gates allow;
- persists ready UT and uses it for Contract-vs-Science crew arbitration;
- gives Contract priority on an exact ready-time tie;
- finds the earliest stored event across rivals and processes events in chronological order;
- uses stable agency ordering for exact-time shared-Science ties;
- clears/replaces invalid preparations rather than letting bad state block progression forever.

It does not query raw KSP Science or own facility/research formulas.

## `Rivals/RivalScienceSimulation.cs`

Owns Science preparation rules.

It starts from experiment IDs unlocked by `RivalTechCatalogue` plus facility-only EVA Report / Surface Sample gates, then filters KSP-supplied subject candidates by:

- current body/situation access;
- Tracking Station range;
- campaign progression;
- facility requirements;
- completed subjects;
- live/preparing duplicates;
- positive Science remaining at selection/revalidation time.

It chooses targets, processes configured daily progress checks, stores ready time, and calculates presentation ETA/range.

Current design notes:

- all normal Kerbin surface/KSC biome access is available from campaign start when the experiment permits it;
- Probe Orbit opens Kerbin Low/High Space Science access;
- EVA Report requires Astronaut Complex Level 2;
- Surface Sample requires R&D Level 2;
- the Sun is excluded from rival Science content;
- current Science expeditions require one Kerbal.

## `Rivals/RivalLiveMissionSimulation.cs`

Turns ready preparations into launched mission snapshots and later resolves them.

It owns:

- Contract/Science mission type;
- location and duration;
- Difficulty and success chance (`95% - 5% * Difficulty` for Difficulty 1-10);
- deterministic `OutcomeSeed`;
- assigned crew;
- satellite-producing mission reservations;
- objective/satellite results on successful Contracts;
- shared-pool Science award on successful Science;
- casualties and pending insurance on failed crewed missions.

A Science success calls back to the KSP Science boundary at completion time so it receives only what is still unclaimed then.

## `Rivals/RivalDevelopmentSimulation.cs`

Owns facilities and development.

Facility effects:

- **Administration** — base rival income;
- **Astronaut Complex** — Kerbal roster limit; Level 2 also enables EVA Report through Science rules;
- **Mission Control** — total satellite capacity;
- **Research and Development** — rival tech Science-cost ceiling; Level 2 also enables Surface Sample through Science rules;
- **VAB + Launch Pad** — normal Contract Launch Progress chance;
- **SPH + Runway** — Science Launch Progress chance;
- **Tracking Station** — destination/expedition range.

It also owns:

- crew availability and launch-time hiring;
- payroll;
- facility construction selection/completion;
- research selection/completion;
- `RivalFundingBreakdown` including signed net payout.

`CampaignController` decides when these functions run at a funding boundary.

## `Rivals/RivalTechCatalogue.cs`

Fixed project mirror of the KSP 1.12 tech nodes needed by rivals.

Contains stable IDs, Science costs, prerequisite semantics, and experiment unlocks. Tech does not gate rival Contract destinations in v0.6.

---

# Stock Science boundary

## `KspIntegration/KspScienceAdapter.cs`

This is the only rival-Science code allowed to manipulate raw stock Science objects.

It:

- finds stock experiments and celestial bodies;
- maps supported situations/biomes into `ScienceSubjectKey` identities;
- calculates currently remaining Science;
- avoids registering every untouched candidate into the player's archives merely by inspecting it;
- builds `RivalScienceSubjectCandidate` snapshots;
- consumes the matching stock subject when a rival wins it.

The important boundary is:

```text
KSP ScienceSubject / ScienceExperiment
        stay inside KspIntegration
                 |
                 v
ScienceSubjectKey + primitive snapshot data
                 |
                 v
            Rivals/
```

If future rival Science code needs more stock data, extend the project-owned snapshot or adapter method instead of passing the KSP object outward.

---

# Player vessel tracking

Version 0.6 retains the v0.5 tracking architecture.

## `KspIntegration/KspVesselMonitor.cs`

Direct KSP reader.

Creates project-owned snapshots from loaded/unloaded vessels, active control lineage, persistent parts, body/biome/situation data, and relevant destruction/lifecycle events.

## `Tracking/FlightContractTracker.cs`

Evaluates the actively controlled vessel for Pre-Orbit Flight Contracts.

Four current lines:

- Directed Power;
- Mass;
- Control;
- Biome.

Tracking history belongs to remembered `FlightAttemptState` objects, not to the UI or live KSP vessel object.

## `Tracking/FlightAttemptState.cs`

Keeps histories independent across vessel switching and robust through staging, docking/undocking, save/load, and vessel-ID reuse by using remembered persistent-part lineage.

Important conservative rules include:

- unrelated docked lineage cannot provide qualifying Mass;
- Control topology changes can reset an unfinished continuous hold;
- a newly built unrelated craft does not inherit old history merely because KSP reused a vessel ID;
- broad refresh evidence is required before stale attempt histories are pruned.

## `Tracking/OrbitalVesselTracker.cs`

Uses the slower broad vessel snapshots for orbital objectives and satellite counts.

---

# Persistence

## `KspIntegration/ModPersistenceScenario.cs`

KSP-facing ScenarioModule. It is the storage bridge, not the gameplay model.

## `Persistence/CampaignFundingSaveState.cs`

Transforms player/campaign funding state and next funding time.

## `Persistence/FlightContractProgressSaveState.cs`

Transforms remembered Flight Attempt state.

## `Persistence/RivalAgenciesSaveState.cs`

Collection wrapper for current rivals.

## `Persistence/RivalProgramSaveState.cs`

Transforms the full v0.6 rival programme:

- signed Funds;
- objectives and satellites;
- normal Contract preparation;
- Science preparation;
- live missions/outcome seed;
- Stored Science and completed Science subjects;
- crew and pending insurance;
- facilities/construction;
- researched tech and current research.

Persistence should normalize malformed values and silently restore historical completions. It should not run funding, missions, or notifications.

---

# UI

## `UI/CommandCenterWindow.cs`

Main window shell and general views:

- Overview;
- Funding Targets;
- Contract Catalogue;
- Help / Player Guide.

It also owns common UI helpers used by the rival partial.

Current v0.6 polish includes player-uncompleted Funding Targets first and the funding-sharing help example.

## `UI/CommandCenterWindow.Rivals.cs`

Rival Agencies presentation partial.

Each rival card reads authoritative state in this order:

```text
Programme Status
Live Mission Progress
Current Launch Programme | Launch Science Expedition
Construction
Facilities
Tech Tree / Research
Funding
```

The UI does not simulate a progress check, roll an outcome, spend Funds, award Science, complete construction, or choose research.

## `UI/FlightActiveUI.cs`

Small Flight-only Offered Contracts window.

It displays:

- unfinished Offered objective contracts first;
- the base reward beside each contract;
- player-completed Offered objective contracts at the bottom;
- expand/collapse controls for unfinished items;
- current Pre-Orbit telemetry inside expanded rows when fresh telemetry is available.

It does not own a separate active-vessel sampler or a current Flight Attempt identity header.

## `UI/FundingNotificationUI.cs`

Observes campaign signals and publishes stock KSP messages for:

- eligible player objective completion;
- rival objective completion;
- sponsor reviews with new offers;
- positive player campaign funding payouts.

It maintains only presentation-side queue/baseline state and resets that state when the current save changes.

---

# Configuration

## `GameData/TheRaceForSpace/Config/CampaignSettings.cfg`

Current high-level sections include:

```text
THE_RACE_FOR_SPACE_SETTINGS
  RIVAL_PROGRAMME
    KERBALS
    CONSTRUCTION
    RESEARCH
    FACILITIES
    LAUNCH_PROGRESS
    MISSION_LOCATIONS
  PRE_ORBIT
  body/network balance sections
```

The legacy single `rivalProgressChancePercent` setting is gone.

Normal rival Contract chance comes from VAB + Launch Pad. Science chance comes from SPH + Runway.

`KspIntegration/CampaignSettingsLoader.cs` owns parsing and validation. `Core/CampaignSettings.cs` owns the resulting values used by gameplay.

---

# Tests

Run:

```bash
bash tools/run-logic-tests.sh
```

This runs both KSP-independent suites.

Useful test areas now include:

- objective/funding/unlock rules;
- Flight Contract tracking and save transforms;
- CampaignController offer/funding order;
- rival state/persistence;
- tech catalogue;
- Science filtering/preparation;
- live mission results/casualties/satellite reservations;
- crew contention;
- large time jumps;
- research/construction;
- signed funding-boundary integration.

The automated suite cannot verify actual KSP/Unity APIs or IMGUI rendering. Use [`CONTENT_V0_6_TESTING.md`](CONTENT_V0_6_TESTING.md) for current live acceptance and [`KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md) for the detailed retained Pre-Orbit cases.

---

# Where should I make this change?

| Goal | Start here |
| --- | --- |
| Change runtime frequency | `Core/ModRuntime.cs` |
| Add/change balance setting | `Core/CampaignSettings.cs`, loader, cfg |
| Change sponsor/funding order | `Campaign/CampaignController.cs` |
| Add objective | `Objectives/ObjectiveCatalogue.cs` / `ObjectiveDefinition.cs` |
| Change unlock rule | `Objectives/UnlockRuleDefinition.cs` / evaluator |
| Change funding lifecycle | `Funding/` |
| Change rival target/event chronology | `Rivals/RivalSimulation.cs` |
| Change rival Science rules | `Rivals/RivalScienceSimulation.cs` |
| Change mission outcome/casualty/reservation | `Rivals/RivalLiveMissionSimulation.cs` |
| Change rival facility/crew/research/construction | `Rivals/RivalDevelopmentSimulation.cs` |
| Change rival tech | `Rivals/RivalTechCatalogue.cs` |
| Read/change stock Science integration | `KspIntegration/KspScienceAdapter.cs` |
| Change active-vessel snapshot acquisition | `KspIntegration/KspVesselMonitor.cs` |
| Change Pre-Orbit evaluation/history | `Tracking/FlightContractTracker.cs` / `FlightAttemptState.cs` |
| Change rival save format | `Persistence/RivalProgramSaveState.cs` |
| Change Rival Agencies layout | `UI/CommandCenterWindow.Rivals.cs` |
| Change Funding Targets / catalogue / help | `UI/CommandCenterWindow.cs` |
| Change compact Flight presentation | `UI/FlightActiveUI.cs` |
| Change stock inbox messages | `UI/FundingNotificationUI.cs` |

Three practical rules prevent most architectural mistakes:

1. Do not let raw KSP objects escape `KspIntegration`.
2. Do not make UI authoritative for gameplay.
3. When several systems need ordering, coordinate them in `CampaignController` or `ModRuntime` rather than duplicating the rule in each subsystem.

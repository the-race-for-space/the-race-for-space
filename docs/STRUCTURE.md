# Code Structure and Ownership

This document defines **which module owns which responsibility** in the current **Content v0.6** codebase on `Alpha/Content-v0.6`.

The main rule is simple:

> Keep KSP API access at the integration boundary, keep gameplay rules in domain/simulation modules, let `CampaignController` coordinate cross-system order, and keep UI presentation-only.

## Runtime flow at a glance

```text
KSP / Unity
   |
   +--> KspIntegration/KspVesselMonitor.cs
   |       |
   |       +--> project-owned active-vessel snapshots
   |       |       --> Tracking/FlightContractTracker.cs
   |       |
   |       +--> project-owned orbital-vessel snapshots
   |               --> Tracking/OrbitalVesselTracker.cs
   |
   +--> KspIntegration/KspScienceAdapter.cs
   |       |
   |       +--> ScienceSubjectKey / rival Science candidates
   |               --> Rivals/RivalScienceSimulation.cs
   |
   +--> KspIntegration/ModPersistenceScenario.cs
   +--> KspIntegration/CareerFundingAdapter.cs
   +--> KspIntegration/CampaignSettingsLoader.cs

Core/ModRuntime.cs
   |
   +--> schedules active-flight sampling and broader campaign refreshes
   |
   +--> Campaign/CampaignController.cs
           |
           +--> Agencies/
           +--> Objectives/
           +--> Funding/
           +--> Rivals/RivalSimulation.cs
           |       +--> RivalScienceSimulation.cs
           |       +--> RivalLiveMissionSimulation.cs
           |
           +--> funding boundaries
                   +--> RivalDevelopmentSimulation.cs

Persistence/ stores project-owned mutable state.
UI/ reads the resulting state and never advances gameplay.
```

There is **no separate rival realtime scheduler**. `ModRuntime` remains the runtime heartbeat; `RivalSimulation` processes stored universal-time events chronologically whenever the campaign is refreshed.

---

# 1. `Core/`

## `ModRuntime.cs`

Owns runtime lifetime and scheduling.

Responsibilities:

- creates/owns the current `CampaignController`;
- schedules the faster active-vessel Flight Contract sampling cadence;
- schedules the slower broad campaign/orbital/rival refresh cadence;
- forwards player Flight Contract completions into campaign state;
- exposes read-only/current tracking state used by UI;
- does not contain rival mission rules or stock Science logic.

If code decides **when** work is sampled/refreshed, it normally belongs here.

## `CampaignSettings.cs`

Owns in-memory balance/configuration values after startup loading.

Version 0.6 settings include:

- funding interval, rival count, and starting Funds;
- objective/network and Pre-Orbit balance;
- rival Kerbal hiring, payroll, casualties, and insurance;
- construction/research durations and costs;
- facility capability levels;
- normal Contract Launch Progress and Science Launch Progress values;
- rival mission location/duration/difficulty settings.

It contains project-owned configuration only. Reading KSP `ConfigNode` data belongs in `KspIntegration/CampaignSettingsLoader.cs`.

---

# 2. `Campaign/`

## `CampaignController.cs`

Owns campaign-wide coordination.

Responsibilities:

- creates player/rival agency collections and funding-contract collections;
- restores project-owned persistent state before gameplay advances;
- coordinates player orbital tracking and rival refreshes;
- evaluates unlock availability and sponsor offers;
- starts objective funding lifecycles after qualifying completions;
- calculates/caches projected payouts for UI;
- processes crossed funding boundaries in chronological order;
- persists updated campaign/rival state after refresh;
- supplies presentation helpers such as current payout and rival ETA.

It **coordinates** systems but should not absorb specialist rival rules, KSP vessel reads, raw Science logic, or UI formatting.

## Funding-boundary order

For every crossed funding boundary, `CampaignController.ProcessDueFunding()` uses the locked v0.6 order:

1. catch up rival stored events through the boundary UT;
2. complete due rival research and facility construction;
3. update funding eligibility, reached-network state, and newly started one-off funding;
4. calculate and apply player/rival funding using pre-advance Contract lifecycle values;
5. apply rival Administration income, payroll, and insurance as the signed `RivalFundingBreakdown` result;
6. advance active one-off funding lifecycles once;
7. start eligible new rival research;
8. start eligible new rival facility construction;
9. run the sponsor review last;
10. advance to the next boundary and repeat if timewarp crossed more than one.

This order is intentionally controller-owned because it coordinates funding, rival development, objective progression, and sponsor state.

---

# 3. `Agencies/`

## `AgencyState.cs`

Owns mutable state shared by player and rivals:

- stable agency identity and display name;
- Funds / projected next payout;
- objective completion times;
- satellite counts by body;
- normal rival Contract preparation fields (`NextMissionTargetId`, Launch Progress, next check UT, ready UT).

Rival-only programme state is composed through `RivalProgramState` rather than by adding every rival subsystem directly to the common agency type.

Objective completion restoration uses a silent path so save loading does not emit live completion notifications.

## `RivalProgramState.cs`

Owns persistent **rival-only mutable programme state**:

- Stored Science;
- Science launch preparation;
- launched live missions;
- completed Science subject identities;
- researched tech IDs;
- current research project;
- facility levels;
- active facility construction collection;
- employed Kerbals;
- pending insurance Funds.

Also defines project-owned rival state/value types such as:

- `ScienceSubjectKey`;
- `ScienceLaunchPreparationState`;
- `RivalLiveMissionState`;
- `RivalResearchProjectState`;
- `RivalFacilityConstructionState`;
- `RivalFacilityType` and `RivalMissionType`.

These types deliberately contain no raw KSP objects, which keeps persistence and KSP-independent testing straightforward.

---

# 4. `Objectives/`

## `ObjectiveDefinition.cs`

Owns the immutable definition of one gameplay objective.

In addition to player evaluation metadata, v0.6 objective definitions carry rival mission metadata including:

- `Difficulty`;
- `RequiredKerbalCount`.

Pre-Orbit definitions also contain their active-flight requirement data.

## `ObjectiveCatalogue.cs`

Owns the code-defined objective catalogue and stable IDs.

Content belongs here when it is an **objective definition**, not mutable campaign state.

## `UnlockRuleDefinition.cs` / `UnlockRuleEvaluator.cs`

Own structured unlock conditions and their evaluation against project-owned campaign state.

Unlock rules can reference:

- objective completion by player/rivals/any agency;
- required agency counts;
- satellite counts;
- universal time.

UI may ask the evaluator for progress/display data, but UI does not decide unlock state itself.

---

# 5. `Funding/`

## `FundingContractCatalogue.cs`

Creates fresh campaign funding-contract state from code-defined content and loaded balance settings.

## `ObjectiveFundingContract.cs`

Owns one-off objective funding lifecycle state:

- offered/start/expired state;
- base reward;
- payments remaining;
- declining interest/current payout;
- unlock rule reference.

## `SatelliteNetworkFundingContract.cs`

Owns repeatable satellite-network funding state:

- target body;
- required satellite count;
- reward;
- availability/offer/reached-target state;
- unlock rule;
- rival mission Difficulty and required crew metadata.

Funding classes calculate contract-local values. Cross-agency payout timing/order belongs to `CampaignController`.

---

# 6. `Rivals/`

Version 0.6 splits rival behaviour into specialist simulations with `RivalSimulation` as the chronological coordinator.

## `RivalSimulation.cs`

Owns chronological rival event coordination.

Responsibilities:

- chooses valid **Offered** Contract targets;
- maintains normal Contract Launch Progress and ready time;
- advances Science preparations through the Science simulation;
- arbitrates ready Contract versus Science launches when crew is contested;
- gives Contract priority on an exact ready-time tie;
- blocks invalid/duplicate targets;
- enforces Tracking Station destination access for Contract selection;
- rechecks satellite capacity and crew at launch;
- launches live missions instead of directly completing objectives;
- processes the earliest due stored event repeatedly until the target UT is reached;
- sorts exact-time rival processing by stable agency ID for deterministic shared-Science races;
- delegates raw Science capture/consumption through callbacks supplied by `CampaignController`/`KspScienceAdapter`.

It does **not** own another Unity timer or polling loop.

## `RivalScienceSimulation.cs`

Owns KSP-independent **Launch Science Expedition** preparation rules:

- derives currently unlocked experiment IDs from rival tech/facilities;
- adds EVA Report at Astronaut Complex Level 2;
- adds Surface Sample at R&D Level 2;
- applies campaign progression and Tracking Station destination access;
- excludes the Sun from current rival Science content;
- filters already-completed/live duplicate subjects;
- chooses/revalidates preparation targets;
- advances daily Science Launch Progress;
- derives Science launch ETA and expedition range.

It receives project-owned candidate snapshots. It never reads `ScienceSubject`, `ResearchAndDevelopment`, or other raw KSP Science objects.

## `RivalLiveMissionSimulation.cs`

Owns creation/resolution of launched rival missions:

- Contract and Science mission snapshots;
- location and configured duration;
- Difficulty to success-chance mapping;
- deterministic mission outcome seed;
- assigned Kerbal count;
- one-off duplicate blocking;
- satellite-capacity reservations for live satellite-producing missions;
- success/failure resolution;
- deterministic crew casualty rolls on failed crewed missions;
- pending insurance accumulation and the exact insurance liability generated by each failed mission;
- Contract objective/satellite results;
- Science award through the shared-pool callback;
- an observer-only `LiveMissionResolved` signal emitted after valid mission state has been finalized, carrying the authoritative resolution snapshot for presentation.

A launched Contract remains valid if its sponsor funding later expires; sponsor lifecycle and mission outcome are separate concerns.

## `RivalDevelopmentSimulation.cs`

Owns KSP-independent rival facilities, crew economy, research, construction, and funding calculations.

Responsibilities include:

- Administration base income;
- Astronaut Complex roster limit;
- Mission Control satellite capacity;
- R&D Science-cost ceiling;
- Tracking Station level;
- VAB + Launch Pad normal Launch Progress chance;
- SPH + Runway Science Launch Progress chance;
- available/on-mission Kerbal counts;
- launch-time missing-crew hiring;
- payroll;
- `RivalFundingBreakdown`;
- facility upgrade cost/duration and completion;
- research eligibility, selection, cost, and completion;
- observer-only `ResearchCompleted` and `FacilityConstructionCompleted` signals emitted after genuine new development state has been finalized.

Malformed/duplicate development records may still be cleared defensively, but that cleanup does not emit a completion signal. It does not decide **when** a funding boundary occurs; `CampaignController` calls it at the correct point in the boundary sequence.

## `RivalTechCatalogue.cs`

Owns the fixed project mirror of the KSP 1.12 tech tree needed by rival Science.

It stores stable tech IDs, display names, Science costs, prerequisite rules, and experiment unlock IDs. Rival tech currently gates Science experiments only; Contract launch destination access comes from the Tracking Station/campaign rules.

---

# 7. `Tracking/`

Tracking owns **KSP-independent evaluation of project-owned vessel snapshots**.

## `ActiveVesselSnapshot.cs`

Project-owned snapshot used by active Flight Contract evaluation.

No live `Vessel`, `Part`, or Unity object crosses into Tracking.

## `FlightAttemptState.cs`

Stores one remembered active-flight lineage and its accumulated Pre-Orbit history.

Important behaviour retained from v0.5:

- independent histories survive vessel switching;
- staging/splits retain compatible lineage history;
- docking/undocking is reconciled conservatively from persistent-part identity;
- unrelated craft must not satisfy Mass by attaching extra lineage mass;
- Control hold state reacts to topology changes according to its rule;
- save/load preserves remembered attempts;
- dead/recovered histories are pruned only when a successful broad vessel refresh proves their remembered persistent parts no longer exist.

## `FlightContractTracker.cs`

Owns evaluation and accumulated state for Pre-Orbit active-vessel contracts.

It is updated by `ModRuntime` from `KspVesselMonitor` snapshots. UI only reads it.

## `OrbitingVesselSnapshot.cs` / `OrbitalVesselTracker.cs`

Own the slower loaded/unloaded vessel scan model and orbital objective/satellite evaluation.

## `SurfaceImpactEvaluator.cs`

Owns the project-side decision about whether a destruction/removal event qualifies as a real surface impact for Directed Power.

---

# 8. `Persistence/`

Persistence classes transform project-owned mutable state to/from `ConfigNode` data. They do not advance gameplay.

## Main classes

- `CampaignFundingSaveState.cs` — player/campaign funding-contract state and shared funding date data.
- `FlightContractProgressSaveState.cs` — remembered Flight Attempts and active Pre-Orbit tracking state.
- `RivalAgenciesSaveState.cs` — collection wrapper for rival programme save nodes.
- `RivalProgramSaveState.cs` — full v0.6 state for one rival programme.

`RivalProgramSaveState` persists:

- signed rival Funds;
- objective completion timestamps;
- satellite counts;
- normal Contract preparation target/progress/check/ready UT;
- Stored Science;
- Science preparation;
- live missions including deterministic outcome seed;
- employed Kerbals and pending insurance;
- all facility levels;
- active facility construction;
- researched tech IDs and current research;
- completed Science subjects.

Old/malformed data is normalized defensively. Objective completions are restored silently so persistence cannot replay live notifications. Restored facility levels and researched-tech IDs likewise do not emit development-completion signals; only an active project that actually completes during funding-boundary processing can do so.

---

# 9. `KspIntegration/`

This is the boundary for direct KSP/Unity gameplay APIs.

## `KspVesselMonitor.cs`

Reads live KSP vessels/parts/proto snapshots and converts them into project-owned tracking snapshots.

It owns KSP-specific details such as:

- active vessel selection;
- persistent part IDs;
- control/reference lineage capture;
- biome/body/situation reads;
- loaded/unloaded broad vessel discovery;
- destruction/lifecycle hooks needed by Flight Contract tracking.

## `KspScienceAdapter.cs`

The **only** component that works directly with stock KSP Science subjects for rival Science Expeditions.

Responsibilities:

- resolves stock experiments, bodies, situations, and biomes;
- enumerates stock-valid rival Science candidates;
- returns project-owned `ScienceSubjectKey`/candidate snapshots;
- reads the shared remaining Science for a subject;
- consumes/exhausts the exact stock subject when a rival successfully wins that Science;
- keeps raw `ScienceSubject`, `ScienceExperiment`, R&D, and celestial-body objects inside `KspIntegration`.

## `CampaignSettingsLoader.cs`

Reads `GameData/TheRaceForSpace/Config/CampaignSettings.cfg` once at startup and populates `CampaignSettings`.

## `CareerFundingAdapter.cs`

Applies positive player campaign payouts to stock Career Funds and emits the live payout signal observed by `FundingNotificationUI`.

## `ModPersistenceScenario.cs`

KSP `ScenarioModule` bridge that calls project-owned persistence transforms and stores UI visibility/campaign save nodes.

---

# 10. `UI/`

UI is presentation-only. It may call read-only/calculation helpers, but it must not simulate progress, spend Funds, resolve missions, award Science, or mutate campaign state to make the display look current.

## `CommandCenterWindow.cs`

Owns the full Command Center shell and the Overview, Funding Targets, Contract Catalogue, and Help views.

Notable v0.6 presentation behaviour:

- Funding Targets shows Offered one-off objectives not yet completed by the player first;
- recurring satellite funding follows;
- player-completed Offered one-off objectives are moved to the bottom;
- Help includes Pre-Orbit guidance and a worked funding-sharing example.

## `CommandCenterWindow.Rivals.cs`

Partial class containing the Rival Agencies dashboard only.

The page shows up to four active-rival selector buttons and renders the full detail card for only the selected rival. The selected button uses white text while unselected active rivals use grey text; inactive rival slots do not produce buttons.

Card order is:

1. Programme Status
2. Live Mission Progress
3. Current Launch Programme / Launch Science Expedition
4. Funding
5. Construction
6. Facilities
7. Tech Tree / Research

It displays authoritative values from `AgencyState`, `RivalProgramState`, `CampaignController`, and specialist rival simulation helpers. Expansion state for locked technologies and rival selection are UI-only and are not persisted as gameplay state.

## `FlightActiveUI.cs`

Owns the compact Flight-only **Offered Contracts** window.

Responsibilities:

- separate Flight launcher button/lifecycle;
- unfinished Offered objectives before player-completed ones;
- base funding reward beside each row;
- independent expansion controls;
- expanded Pre-Orbit requirement telemetry from the current `FlightContractTracker`.

It does **not** show or own a separate Flight Attempt identity header in the current v0.6 implementation. Fresh telemetry gating happens inside expanded requirement rows.

## `FundingNotificationUI.cs`

Session-level observer for stock KSP inbox messages.

It reports:

- player completion of an Offered, unexpired Objective Funding Contract;
- valid rival Live Mission results, including success/failure, Science gained, crew survival/casualties, and the exact insurance liability created by that mission;
- rival research completion, including newly unlocked Science experiments where applicable;
- rival facility construction completion, including a `Bonus:` line derived from the finalized facility capability/configuration;
- sponsor reviews that create newly Offered targets;
- positive player campaign funding payouts.

Rival objective completion alone is no longer a separate notification path; a successful rival Contract is reported once as its Live Mission result. Development notifications observe the authoritative completion signals and do not complete projects or alter facility/research state themselves.

It establishes an offer baseline silently when a controller/save is first observed so loading a save does not replay historical sponsor-review messages. Persistence restoration also uses silent objective/development state restoration and therefore cannot fabricate a Live Mission, research, or facility-completion notification.

---

# 11. Common flows

## Player Pre-Orbit Flight Contract

```text
KspVesselMonitor
  -> ActiveVesselSnapshot
  -> ModRuntime active-flight cadence
  -> FlightContractTracker
  -> AgencyState objective completion
  -> CampaignController unlock/funding state on refresh
  -> FlightActiveUI / CommandCenterWindow presentation
```

## Player orbital objective / satellite count

```text
KspVesselMonitor broad capture
  -> OrbitingVesselSnapshot list
  -> OrbitalVesselTracker
  -> AgencyState objective/satellite state
  -> CampaignController funding/unlock evaluation
```

## Rival Science race

```text
CampaignController.RefreshRivals
  -> KspScienceAdapter captures stock-valid candidates
  -> project-owned candidate list
  -> RivalScienceSimulation chooses/prepares target
  -> RivalSimulation reaches ready event
  -> RivalLiveMissionSimulation launches Science mission
  -> chronological completion event
  -> KspScienceAdapter consumes exact remaining stock Science
  -> rival StoredScience / completed subject updated
  -> LiveMissionResolved observer signal
  -> FundingNotificationUI presentation
```

## Rival Contract mission

```text
Offered funding target
  -> RivalSimulation selects valid destination
  -> Launch Progress checks
  -> 100% ready time persisted
  -> crew/capacity gates
  -> RivalLiveMissionSimulation launches mission
  -> chronological completion
  -> success records objective/satellite result, or failure resolves casualties/insurance
  -> LiveMissionResolved observer signal
  -> FundingNotificationUI presentation
  -> CampaignController later processes funding consequences
```

## Rival development completion

```text
CampaignController funding boundary
  -> RivalDevelopmentSimulation completes due research/construction
  -> finalized researched-tech/facility state
  -> ResearchCompleted / FacilityConstructionCompleted observer signal
  -> FundingNotificationUI presentation
  -> remaining funding-boundary processing continues
```

---

# 12. Tests and acceptance

Run KSP-independent logic and controller regressions with:

```bash
bash tools/run-logic-tests.sh
```

The tests cover domain rules including:

- funding and unlock rules;
- Pre-Orbit tracking/persistence;
- rival programme state and persistence;
- rival technology;
- Science target/progress rules;
- live mission outcomes, outcome observer signals, casualties, insurance liability, and satellite reservations;
- crew contention and ready-time ordering;
- large time jumps / chronological rival events;
- negative rival funding;
- research/construction rules and genuine-completion observer signals;
- funding-boundary integration order.

They cannot prove raw KSP APIs or on-screen IMGUI behaviour. Use:

- `docs/CONTENT_V0_6_TESTING.md` for the current v0.6 live-KSP acceptance pass;
- `docs/KERBAL_CONTRACTS_V0_5_TESTING.md` for the detailed retained Pre-Orbit checks.

---

# 13. Where common changes belong

| Change | Primary owner |
| --- | --- |
| Runtime cadence / when work is sampled | `Core/ModRuntime.cs` |
| User-editable balance property | `Core/CampaignSettings.cs` + config loader/cfg |
| Sponsor/funding-boundary order | `Campaign/CampaignController.cs` |
| Common agency objective/satellite state | `Agencies/AgencyState.cs` |
| Rival-only mutable programme state | `Agencies/RivalProgramState.cs` |
| Objective content / rival Difficulty / crew metadata | `Objectives/` |
| Funding lifecycle/calculation local to a contract | `Funding/` |
| Rival event ordering / target arbitration | `Rivals/RivalSimulation.cs` |
| Rival Science preparation/access | `Rivals/RivalScienceSimulation.cs` |
| Live rival mission creation/outcomes | `Rivals/RivalLiveMissionSimulation.cs` |
| Rival facilities/crew/research/construction/funding | `Rivals/RivalDevelopmentSimulation.cs` |
| Rival tech/experiment unlock mirror | `Rivals/RivalTechCatalogue.cs` |
| Active Flight Contract rule evaluation | `Tracking/FlightContractTracker.cs` |
| Orbital/satellite snapshot evaluation | `Tracking/OrbitalVesselTracker.cs` |
| Raw KSP vessel/part/proto access | `KspIntegration/KspVesselMonitor.cs` |
| Raw stock Science access/consumption | `KspIntegration/KspScienceAdapter.cs` |
| Save transform for rival programme | `Persistence/RivalProgramSaveState.cs` |
| Command Center general views/help | `UI/CommandCenterWindow.cs` |
| Rival dashboard presentation | `UI/CommandCenterWindow.Rivals.cs` |
| Compact Flight contract presentation | `UI/FlightActiveUI.cs` |
| Stock campaign inbox messages | `UI/FundingNotificationUI.cs` |

When a change spans several rows, keep the rule in its natural owner and let `CampaignController` or `ModRuntime` coordinate it rather than duplicating the rule across modules.

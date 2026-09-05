# Code Overview

This document is the quickest way to understand how the mod is put together.

Some class and folder names are left over from earlier versions of the project and are broader or narrower than what the code now does. The descriptions below explain the current role of the code, not just what the name suggests.

## Overall flow

The main flow is:

```text
KSP / Unity
    |
    v
RaceRuntime
    |
    +--> KspIntegration reads KSP state
    |
    +--> Tracking evaluates vessels and active flights
    |
    v
SatelliteRaceController
    |
    +--> Programs stores player/rival state
    +--> Funding stores contract and payout state
    +--> RivalSimulation advances rivals
    +--> Unlock rules decide what becomes available
    |
    +--> Persistence saves the state
    +--> UI reads and displays the state
```

`RaceRuntime` drives the system. `SatelliteRaceController` is the central gameplay coordinator. The UI does not advance gameplay; it only reads current state.

## Main parts

| Folder | What it actually does |
| --- | --- |
| `Core/` | Runs the mod while KSP is open. `RaceRuntime` owns the main controller and decides when different work should run. |
| `KspIntegration/` | Talks directly to KSP. Reads vessels, active-flight data, game time and config, handles KSP events, and connects project save data to KSP saves. |
| `Tracking/` | Takes vessel data and decides whether gameplay conditions have been met. This includes starter-contract flight checks, orbital objective checks and qualifying satellite counts. |
| `Competition/` | Contains the main campaign coordinator. Despite the name, this is not just "race" logic. `SatelliteRaceController` controls offers, unlocks, funding reviews, rival updates and the main campaign state. |
| `Milestones/` | Defines one-off objectives and their requirements. A `MilestoneDefinition` is effectively an objective definition: what must be achieved, where, with what crew/state, and what unlock rule applies. |
| `Funding/` | Defines funding contracts and their payout lifecycle. It also builds the initial catalogue of one-off achievement funding and satellite-network funding. |
| `Programs/` | Stores the current state of each space agency: player or rival. This includes achievements, funds, satellite counts and rival mission state. |
| `Simulation/` | Advances rival agencies. Chooses valid missions, spends rival funds and increases rival mission progress. |
| `Persistence/` | Converts the mod's runtime state into saveable data and restores it later. |
| `UI/` | Draws the Command Center. Reads gameplay state from the runtime/controller but should not contain progression logic. |

## Important classes

### `RaceRuntime`

The mod's runtime driver.

It owns the current `SatelliteRaceController` and schedules the different update speeds:

- frequent active-flight checks for starter contracts;
- regular gameplay/controller refreshes;
- slower full vessel scans for orbital and satellite state.

It is not the place where contract rules are defined.

### `SatelliteRaceController`

The main campaign coordinator.

The name is now misleading because it does much more than satellites. It currently handles:

- player and rival programme collections;
- which contracts are Offered, Unlocked, Locked or Expired;
- funding review dates and payouts;
- starter-contract availability;
- Probe Orbit unlocking;
- rival updates;
- the current list of starter contracts that should be checked during flight.

It coordinates existing systems rather than reading KSP vessels directly.

### `SpaceProgramState`

The state of one agency.

Used for both the player and rivals. It stores things such as:

- completed objective IDs and completion times;
- funds;
- qualifying satellite counts by body;
- rival mission target and progress.

### `MilestoneDefinition`

A one-off objective definition.

Examples include Directed Power I, Control III, Probe Orbit and later orbital objectives.

It stores the actual requirements used to decide whether that objective has been achieved. For starter contracts this includes values such as required speed, mass, distance, altitude band, hold time or biome.

### `PrototypeMilestones`

The code-owned catalogue of one-off objectives.

This is where the current starter objectives and orbital objectives are created and given stable IDs, requirements, unlock rules and rewards/costs.

### `UnlockRuleDefinition` / `UnlockRuleEvaluator`

Defines and checks when something is allowed to unlock.

Rules can depend on things such as:

- an objective being completed by the player, a rival or any agency;
- a number of agencies completing an objective;
- satellite counts;
- campaign time.

### `StarterFlightTracker`

Checks the currently active player flight against Offered starter contracts.

It keeps the state that must survive between one-second samples, such as:

- maximum speed;
- maximum altitude;
- launch position;
- current mass/distance/biome/crew values;
- whether orbit was entered;
- independent Control hold progress for each active Control contract.

This is where Directed Power, Mass, Control and Biome completion rules are evaluated.

### `SatelliteTracker`

This class really is satellite/orbit focused.

It uses vessel snapshots from KSP to:

- count qualifying probes/relays around bodies;
- record orbital one-off objectives when their conditions are met.

### `PrototypeFundingCatalogue`

Builds the funding-contract objects used by a new controller.

It creates:

- `AchievementFundingProgramme` objects for one-off objectives;
- `FundingProgramme` objects for recurring satellite-network funding.

### `AchievementFundingProgramme`

Funding state for a one-off objective such as Directed Power I or Probe Orbit.

It tracks whether the contract is offered, whether it has started paying out, how many payouts have happened and when it expires.

### `FundingProgramme`

Funding state for a satellite-network target.

This is different from `AchievementFundingProgramme`: it represents ongoing satellite funding rather than a one-time achievement.

### `RivalSimulation`

Controls rival mission progression.

It chooses from currently valid offered targets, checks whether a rival can afford progress, spends funds and advances that rival's mission until completion.

### `RacePersistenceScenario`

The bridge between KSP saves and the project's own persistence classes.

The actual save data is split into:

- funding/player achievement state;
- rival state;
- temporary active-contract flight progress.

### `RaceWindow`

The Command Center UI.

It displays information from the controller and active flight tracker. It should not decide whether contracts complete, unlock or pay out.

## How a starter contract completes

1. A starter objective exists in `PrototypeMilestones`.
2. `PrototypeFundingCatalogue` creates its funding contract.
3. `SatelliteRaceController` decides whether that contract is currently Offered.
4. `RaceRuntime` asks `KspVesselDiscovery` for only the live KSP values needed by the currently Offered starter contracts.
5. `StarterFlightTracker` checks those values against each active contract.
6. When a contract is completed, the player's `SpaceProgramState` records the objective ID and completion time.
7. `SatelliteRaceController` then updates unlocks, offers, funding and rival state on its normal refresh.
8. Persistence saves the changed state and the UI displays it.

## How orbital and satellite checks differ

Starter contracts use the active vessel and are checked frequently during flight.

Orbital objectives and satellite counts use the slower vessel scan. `KspVesselDiscovery` creates vessel snapshots and `SatelliteTracker` uses those snapshots to update orbital achievements and body satellite counts.

This is why some older code uses the word `Satellite` even though the overall campaign now contains many non-satellite objectives.

## Legacy names to mentally translate

| Current name | Better mental meaning |
| --- | --- |
| `RaceRuntime` | Main mod runtime / scheduler |
| `SatelliteRaceController` | Main campaign controller |
| `Competition/` | Campaign coordination |
| `Milestone` | One-off objective / achievement definition |
| `AchievementFundingProgramme` | Funding contract for a one-off objective |
| `FundingProgramme` | Satellite-network funding contract |

These names have not been changed because renaming public concepts can create unnecessary code churn and save/API risk. The important point is to use their current function when reading the code.

## Where to make common changes

- Change or add a one-off objective: `Milestones/PrototypeMilestones.cs`.
- Change how a starter flight is judged: `Tracking/StarterFlightTracker.cs`.
- Change what KSP vessel data is collected: `KspIntegration/KspVesselDiscovery.cs`.
- Change unlock rule behaviour: `Milestones/UnlockRuleEvaluator.cs`.
- Change offers, funding review behaviour or campaign coordination: `Competition/SatelliteRaceController.cs`.
- Change one-off funding lifecycle: `Funding/AchievementFundingProgramme.cs`.
- Change satellite-network funding: `Funding/FundingProgramme.cs` and `PrototypeFundingCatalogue.cs`.
- Change rival behaviour: `Simulation/RivalSimulation.cs`.
- Change the Command Center: `UI/RaceWindow.cs`.
- Change save format or restore behaviour: `Persistence/` plus `KspIntegration/RacePersistenceScenario.cs`.

## Design rules worth remembering

- KSP API access should stay in `KspIntegration/` where practical.
- Tracking/evaluation code should work from project-owned snapshots rather than raw KSP objects.
- `SatelliteRaceController` coordinates gameplay state; it should not become a second KSP integration layer.
- UI reads state; it should not advance progression.
- Stable IDs and save fields should not be renamed casually.

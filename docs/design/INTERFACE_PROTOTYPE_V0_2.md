# Interface Prototype 0.2 - Historical Design Record

> **Historical document.** This describes the 0.2 interface prototype. It is not the current 0.5 UI or funding model. Current terminology is used where possible.

## Purpose

Version 0.2 focused on presenting the early satellite prototype through one coherent Command Center while adding scheduled funding and persistent rival state.

It did not add the current Pre-Orbit phase or the later objective catalogue.

## Target display

The interface was designed for a Steam Machine or normal desktop PC display rather than a handheld-sized layout.

The Command Center was draggable and used KSP's normal opaque IMGUI window background.

## Command Center layout

Version 0.2 used one window with four views:

- Overview;
- Funding Targets;
- Rival Agencies;
- a progression/tracking view that later evolved into today's **Contract Catalogue**.

Only one view was visible at a time. Switching views changed the content inside the same window rather than opening extra windows.

## Overview

The Overview displayed:

- next scheduled funding date as Kerbin Year and Day;
- projected player payout;
- player satellite counts around Kerbin, Mun, and Minmus.

The design deliberately removed winner-takes-all summary text because funding was moving toward proportional payouts.

## Funding Targets

Version 0.2 used these satellite-network targets:

| Target | Requirement | Total available payout |
| --- | --- | ---: |
| Kerbin Orbital Network | 10 satellites | 200,000 |
| Mun Survey Network | 5 satellites | 300,000 |
| Minmus Relay Initiative | 5 satellites | 300,000 |

Cards used two columns:

- target, requirement, and available payout on the left;
- player/Aster/Cobalt progress and current payout on the right.

## Historical proportional funding rule

Before the combined satellite count reached the requirement:

```text
Current Payout = Total Available Payout × (Agency Satellites / Required Satellites)
```

After the target became saturated:

```text
Current Payout = Total Available Payout × (Agency Satellites / Total Satellites)
```

The total payout pool never exceeded the fixed amount assigned to the target.

This was an early funding model and should not be treated as the current objective-funding design.

## Shared funding dates

Funding was scheduled every **90 Kerbin days**.

At each funding date:

- the player's projected amount was added to KSP Career funds;
- rival projected amounts were added to simulated rival funds;
- the next funding date advanced by another 90 Kerbin days.

Sandbox and Science modes did not receive Career-funds changes.

The idea of one shared funding calendar continued into later versions.

## Rival Agencies

The prototype used:

- Aster Aerospace Directorate;
- Cobalt Orbital Bureau.

Each rival began with **200,000 funds** and an initial Kerbin target.

The UI showed:

- funds;
- next payout;
- planned mission;
- mission progress;
- progress-step cost;
- estimated time to completion;
- Kerbin/Mun/Minmus satellite counts.

## Historical rival simulation

Every **5 Kerbin days** each rival received one progress check.

- success chance: **50%**;
- progress gained on success: **10%**;
- Kerbin step cost: **20,000**;
- Mun/Minmus step cost: **30,000**.

Ten successful steps completed one mission.

The first target was always Kerbin. Later targets were chosen from Kerbin, Mun, and Minmus.

## Historical ETA rule

At 50% success chance with one check every five Kerbin days, one successful 10% step took about ten Kerbin days on average.

The ETA also considered whether the rival had enough funds for the remaining steps. If current and projected funding could not finance the mission, the UI displayed an awaiting-funding state.

## Rival persistence

Version 0.2 introduced persistent rival state through a KSP `ScenarioModule`.

The saved state included:

- rival funds;
- Kerbin/Mun/Minmus satellite counts;
- planned mission body;
- mission progress;
- next progress-check time.

Derived values such as projected payout and step cost were recalculated rather than duplicated in save data.

The modern project still follows this general principle: persist mutable project-owned state, derive what can be safely rebuilt.

## Window behaviour

The interface rules established in this version included:

- no Command Center on loading screens or the main menu;
- one Command Center instance per loaded game;
- F8 hide/show support;
- normal scene changes should not duplicate the window;
- loading a different KSP save should not carry campaign state across saves.

These remain important behaviour checks today.

## Historical verification focus

The main 0.2 acceptance questions were:

1. Does one Command Center survive normal scene changes without duplication?
2. Do proportional payouts update correctly?
3. Do shared 90-day funding dates work?
4. Do rival funds and mission progress persist?
5. Do rival missions resume with the same planned target after reload?
6. Does F8 hide/show the interface only while a game is loaded?

## Current equivalent

The current 0.5 UI is `CommandCenterWindow` with:

- Overview;
- Funding Targets;
- Rival Agencies;
- Contract Catalogue.

Current funding is split into `ObjectiveFundingContract` and `SatelliteNetworkFundingContract`, and current rival state is stored through `RivalAgenciesSaveState`.

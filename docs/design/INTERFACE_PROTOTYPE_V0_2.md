# Interface Prototype 0.2

Version 0.2 focuses on presenting the satellite-race prototype as a coherent command-center interface while refining the first funding and rival-program simulation. It does not add new funding targets, rival agencies, vessel rules, or save persistence.

## Target display

The primary target is a Steam Machine or conventional desktop PC connected to a normal PC-sized display. The interface therefore prioritizes a larger command-center layout rather than handheld-screen constraints.

The interface remains draggable so players can position it appropriately for their own display size and KSP layout.

## Interface structure

The interface uses one command-center window with four switchable views:

- Overview;
- Funding Targets;
- Rival Agencies;
- Space Race.

A navigation row remains visible at the top of the command center. Selecting a button replaces the current content inside the same window, so only one view is visible at a time and no detail views appear as separate pop-out windows.

The command-center uses the normal opaque KSP IMGUI window background so scene content does not show through the main interface panel.

### Overview

The Overview shows:

- next scheduled funding date as Kerbin Year and Day;
- the player's projected next payout;
- number of funding programmes already decided;
- qualifying satellite counts around Kerbin, Mun and Minmus.

The previous Race Summary section is removed from the 0.2 interface.

## Funding Targets

The Funding Targets view uses the expanded 0.2 test requirements and payout pools:

- Kerbin Orbital Network — 10 qualifying satellites, 200,000 Total Available Payout;
- Mun Survey Network — 5 qualifying satellites, 300,000 Total Available Payout;
- Minmus Relay Initiative — 5 qualifying satellites, 300,000 Total Available Payout.

Each target displays its data in this order:

1. requirement;
2. Total Available Payout;
3. Player Progress;
4. Player Current Payout;
5. Aster Progress;
6. Aster Current Payout;
7. Cobalt Progress;
8. Cobalt Current Payout.

Progress displays the current satellite count only, for example `Player Progress: 1 Satellite`, because the requirement is already shown separately.

The target body and first-to-requirement winner remain visible as contextual race information. Open targets do not display a redundant `Status: OPEN` line.

## Funding payout rule

Each funding target has one fixed **Total Available Payout**. The combined current payouts across all agencies must never exceed that amount.

Before the combined satellite count reaches the requirement, each agency receives a share based on its completion percentage:

`Current Payout = Total Available Payout × (Agency Satellites / Required Satellites)`

When the combined player and rival satellite count exceeds the requirement, the target is saturated. The complete payout pool is distributed according to ownership of all qualifying satellites:

`Current Payout = Total Available Payout × (Agency Satellites / Total Satellites)`

At exactly the required combined satellite count, the completion-percentage and ownership-share calculations produce the same result. Once saturated, adding more satellites changes the ratio but never increases the combined payout above the fixed total available amount.

First-to-requirement remains a separate competitive achievement. Winning that race milestone does not grant exclusive ownership of the funding pool.

## Scheduled funding

Funding is paid on a prototype cycle of **90 Kerbin days**.

- Funding dates align to 90-day Kerbin calendar boundaries.
- The Overview shows the next funding date as `Year X, Day Y`.
- `Next Payout` is recalculated from current satellite ownership on the normal five-second tracking refresh.
- When the funding date is reached, the player's projected payout is added to the real KSP Career funds balance.
- Rival payouts are added to their simulated spendable funds balances.
- The next funding date then advances by another 90 Kerbin days.
- In Sandbox or Science modes the mod does not modify KSP's funds because the Career funding system is unavailable.

The 90-day interval is a prototype tuning value represented as a named constant.

## Rival Agencies

The Rival Agencies view presents the existing two simulated agencies:

- Aster Aerospace Directorate;
- Cobalt Orbital Bureau.

Each rival begins the prototype session with **50,000 funds** and displays:

- Funds;
- Next Payout;
- Next Launch Planned;
- Launch Progress %;
- tracked satellite counts around Kerbin, Mun and Minmus.

Race Points and the previous explanatory subtitle are not displayed on the Rival Agencies view.

## Rival launch simulation

The rival launch loop uses the following prototype rules:

- Each rival begins with a randomly selected next launch target: Kerbin, Mun, or Minmus.
- Every **5 Kerbin days**, each rival receives one launch-progress check.
- Each check has a **50% chance** of increasing Launch Progress by **10 percentage points**.
- Each successful 10% progress step deducts **20,000 funds** immediately.
- If a rival has less than 20,000 funds at a progress check, that check cannot successfully increase progress.
- Ten successful progress steps are required for a complete launch, so the current total simulated launch cost is **200,000 funds**.
- At 100% progress, the satellite is added immediately because all development costs have already been paid during progress.
- After the launch, progress returns to 0% and the next body is selected randomly from Kerbin, Mun, and Minmus.

The 20,000 development payment, five-day progress interval, 50% progress chance, and 10-point progress increment are prototype tuning values.

## Space Race

The Space Race view presents the existing player satellite milestones. For each body it shows:

- current progress against the required satellite count;
- whether the player's milestone is complete.

The previous explanatory subtitle, associated-programme line, and race-status line are removed from this view.

The view still documents the prototype tracking rules in-game: Probe and Relay vessel types, ORBITING situation, loaded and unloaded ProtoVessel records, and the five-second refresh interval.

## Window behaviour

- F8 shows or hides the complete Race for Space interface.
- Only one command-center window is created.
- Overview, Funding Targets, Rival Agencies, and Space Race are navigation views inside that window.
- Only one view is visible at a time.
- Switching views does not create or destroy separate windows.
- The navigation row remains available while moving between views.
- The command-center window remains draggable through KSP/Unity IMGUI behaviour.
- The window uses the normal opaque KSP background.
- A defensive single-instance guard prevents KSP editor scene transitions from rendering duplicate command-center windows.

## Versioning

The project metadata for this prototype is `0.2.0` and the interface identifies itself as Prototype 0.2.

Version 0.2 remains session-only for Race for Space state. Rival balances, planned launches, progress, funding dates, and race state are not yet persisted into the KSP save file.

## Verification focus

For this 0.2 test:

1. Build and deploy the `prototype/interface-v0.2` branch.
2. Confirm only one opaque Command Center window appears through Space Center, editor, and flight scene changes.
3. Confirm the Overview no longer shows the Race Summary section.
4. Confirm the Funding Targets and Rival Agencies views no longer show their explanatory subtitles.
5. Confirm Funding Target progress lines show only the current satellite count rather than `current/required`.
6. Confirm the Space Race view no longer shows its explanatory subtitle, associated programme, or race status.
7. Confirm projected payouts still follow completion percentage below saturation and ownership ratio after saturation.
8. Confirm the next funding date advances in 90-Kerbin-day intervals.
9. Confirm both rivals start with 50,000 funds.
10. Advance across five-day Kerbin boundaries and verify each rival has a 50% chance to gain 10% progress.
11. Confirm every successful 10% progress step deducts 20,000 rival funds immediately.
12. Confirm a rival with less than 20,000 funds cannot make a successful progress step.
13. Confirm reaching 100% adds one satellite without a second launch-cost deduction, resets progress, and selects another body.
14. Reach a scheduled funding date and confirm rival balances increase by their Next Payout amounts.
15. In Career mode, confirm the player's Next Payout is added to the real KSP funds balance on the scheduled funding date.
16. Confirm F8 hides and restores the complete interface.

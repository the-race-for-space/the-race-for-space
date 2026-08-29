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

The Overview is the player's home screen for the race. It shows:

- prototype version;
- next scheduled funding date as Kerbin Year and Day;
- the player's current projected next payout;
- number of funding programmes already decided;
- qualifying satellite counts around Kerbin, Mun and Minmus;
- a compact status line for each current funding programme.

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

Funding is no longer deposited continuously. Version 0.2 uses a prototype funding cycle of **30 Kerbin days**.

- Funding dates align to 30-day Kerbin calendar boundaries.
- The Overview shows the next funding date as `Year X, Day Y`.
- `Next Payout` is recalculated from the current satellite ownership on the normal five-second tracking refresh.
- When the funding date is reached, the player's projected payout is added to the real KSP Career funds balance.
- Rival payouts are added to their simulated spendable funds balances.
- The next funding date then advances by another 30 Kerbin days.
- In Sandbox or Science modes the mod does not modify KSP's funds because the Career funding system is unavailable.

The 30-day interval is a prototype tuning value and is intentionally represented as a named constant rather than embedded throughout the code.

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

Race Points are no longer displayed on the Rival Agencies view.

## Rival launch simulation

The old fixed timetable has been replaced by a simple funded launch loop.

- Each rival begins with a randomly selected next launch target: Kerbin, Mun, or Minmus.
- Every **5 Kerbin days**, each rival receives one launch-progress check.
- Each check has a **25% chance** of increasing Launch Progress by **25 percentage points**.
- Launch Progress therefore moves through 0%, 25%, 50%, 75%, and 100%.
- A completed launch currently costs **50,000 funds**.
- At 100% progress, a rival launches as soon as it has at least 50,000 funds.
- The launch deducts 50,000 from the rival's simulated funds and adds one satellite at the planned body.
- After the launch, progress returns to 0% and the next body is selected randomly from Kerbin, Mun, and Minmus.
- If progress reaches 100% while the rival cannot afford the launch, it remains ready until a later funding payout supplies enough funds.

The 50,000 launch cost, five-day progress interval, 25% progress chance, and 25-point progress increment are prototype tuning values.

## Space Race

The Space Race view presents the existing satellite milestones and tracking information. For each existing programme it shows:

- associated celestial body;
- required and current satellite count;
- whether the player requirement is complete;
- whether the competitive programme is still open or already claimed.

The view also documents the prototype tracking rules in-game: Probe and Relay vessel types, ORBITING situation, loaded and unloaded ProtoVessel records, and the five-second refresh interval.

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
3. Confirm the Overview shows Next Funding Date and Next Payout rather than Race Points.
4. Confirm the Funding Targets show Kerbin 10 / 200,000, Mun 5 / 300,000, and Minmus 5 / 300,000.
5. Confirm projected payouts still follow completion percentage below saturation and ownership ratio after saturation.
6. Confirm both rivals start with 50,000 funds.
7. Confirm each rival shows Next Payout, Next Launch Planned, and Launch Progress %.
8. Advance across five-day Kerbin boundaries and confirm rival launch progress only changes on those checks, with stochastic 25-point increases.
9. Confirm a rival at 100% spends 50,000, adds one satellite at its planned body, resets progress, and chooses another body.
10. Confirm a rival at 100% without enough funds waits instead of launching for free.
11. Reach a scheduled funding date and confirm rival balances increase by their Next Payout amounts.
12. In Career mode, confirm the player's Next Payout is added to the real KSP funds balance on the scheduled funding date.
13. Confirm the next funding date advances by another 30 Kerbin days after payment.
14. Confirm F8 hides and restores the complete interface.

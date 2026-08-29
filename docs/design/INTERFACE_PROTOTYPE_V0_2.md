# Interface Prototype 0.2

Version 0.2 focuses on presenting the satellite-race prototype as a coherent command-center interface while refining the first funding, rival-program simulation, and rival save persistence. It does not add new funding targets, rival agencies, or vessel rules.

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
- qualifying satellite counts around Kerbin, Mun and Minmus.

The previous Race Summary and Funding Programmes Decided lines are removed because funding is now proportional rather than presented as a winner-takes-all programme outcome.

## Funding Targets

The Funding Targets view uses the expanded 0.2 test requirements and payout pools:

- Kerbin Orbital Network — 10 qualifying satellites, 200,000 Total Available Payout;
- Mun Survey Network — 5 qualifying satellites, 300,000 Total Available Payout;
- Minmus Relay Initiative — 5 qualifying satellites, 300,000 Total Available Payout.

Each funding-target card now uses two columns to make better use of the desktop-width command center:

- left column — Target, Requirement, and Total Available Payout;
- right column — Player, Aster, and Cobalt Progress and Current Payout values.

Progress displays the current satellite count only, for example `Player Progress: 1 Satellite`, because the requirement is already shown separately.

The target body remains visible as context. Winner/claimed status is not displayed in the Funding Targets view because the proportional payout model is the relevant funding information.

## Funding payout rule

Each funding target has one fixed **Total Available Payout**. The combined current payouts across all agencies must never exceed that amount.

Before the combined satellite count reaches the requirement, each agency receives a share based on its completion percentage:

`Current Payout = Total Available Payout × (Agency Satellites / Required Satellites)`

When the combined player and rival satellite count exceeds the requirement, the target is saturated. The complete payout pool is distributed according to ownership of all qualifying satellites:

`Current Payout = Total Available Payout × (Agency Satellites / Total Satellites)`

At exactly the required combined satellite count, the completion-percentage and ownership-share calculations produce the same result. Once saturated, adding more satellites changes the ratio but never increases the combined payout above the fixed total available amount.

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

On a save with no existing Race for Space rival data, each rival begins with **200,000 funds** and its first planned launch is always **Kerbin**. Each rival card uses two columns:

- left column — Funds, Next Payout, Next Launch Planned, Launch Progress, Launch Progress Cost, and ETA till Launch;
- right column — Kerbin, Mun, and Minmus satellite counts.

`Launch Progress Cost` shows the cost of the rival's next successful 10% development step for its currently planned body.

Race Points and the previous explanatory subtitle are not displayed on the Rival Agencies view.

## Rival launch simulation

The rival launch loop uses the following prototype rules:

- On a save with no persisted rival state, each rival's first launch target is always **Kerbin** so the 200,000 starting balance can finance one complete first satellite.
- After the first launch completes, each new rival launch target is selected randomly from Kerbin, Mun, or Minmus.
- Every **5 Kerbin days**, each rival receives one launch-progress check.
- Each check has a **50% chance** of increasing Launch Progress by **10 percentage points**.
- Kerbin uses the base **20,000 funds** cost for each successful 10% progress step, representing 100% of the base cost.
- Mun and Minmus cost **50% more than Kerbin**, so they use **150% of the base cost**, or **30,000 funds** for each successful 10% progress step.
- If a rival has less than the body-adjusted Launch Progress Cost at a progress check, that check cannot successfully increase progress.
- Ten successful progress steps are required for a complete launch. A full Kerbin development cycle therefore costs **200,000 funds**, while a full Mun or Minmus development cycle costs **300,000 funds**.
- At 100% progress, the satellite is added immediately because all development costs have already been paid during progress.
- After the launch, progress returns to 0% and the next body is selected randomly from Kerbin, Mun, and Minmus. The Launch Progress Cost immediately follows that new planned body.

The 20,000 base development payment, Mun/Minmus 150%-of-base modifier, five-day progress interval, 50% progress chance, and 10-point progress increment are prototype tuning values.

### Launch ETA

`ETA till Launch` is an expected value rather than a guaranteed countdown.

- At a 50% success chance and one roll every 5 Kerbin days, one successful 10% development step takes 10 Kerbin days on average.
- For example, a rival at 50% progress has five successful steps remaining, so the base expected ETA is 50 Kerbin days when funds are already available.
- The estimate also checks the rival's current funds using the same body-adjusted Launch Progress Cost displayed in the interface: 20,000 per remaining step for Kerbin and 30,000 per remaining step for Mun or Minmus.
- If current funds are insufficient, the estimate projects forward using the rival's current `Next Payout` and the 90-day funding cycle.
- The current `Next Payout` is treated as the projected amount for future funding cycles. Because payouts can change with satellite ownership, the ETA is recalculated on the normal refresh cycle rather than being permanently fixed.
- If the rival cannot finance all remaining steps with its current balance and projected payout is zero, the interface displays `ETA till Launch: Awaiting Funding` instead of a numeric estimate.

The guaranteed first Kerbin target means the 200,000 starting balance is exactly enough for each rival to finance its first complete satellite. Later Mun/Minmus targets still require 300,000 for a complete 0–100% development cycle and may therefore depend on scheduled funding.

## Rival save persistence

Rival programme progress is stored inside the KSP save through a `ScenarioModule` rather than a separate external file.

For both Aster and Cobalt the save stores:

- current simulated Funds;
- Kerbin satellite count;
- Mun satellite count;
- Minmus satellite count;
- Next Launch Planned body;
- Launch Progress percentage;
- universal time of the next five-day launch-progress check.

The next planned body and next progress-check time are persisted alongside the percentage so a partially completed launch resumes against the same destination and cadence after loading. Launch Progress Cost is derived from the persisted planned body and therefore does not require an additional save field.

Values that can be derived safely are not duplicated in the save:

- `Next Payout` is recalculated from current satellite ownership;
- `Launch Progress Cost` is recalculated from the planned launch body;
- player satellite counts continue to come from the actual KSP vessel tracker;
- player Career funds remain owned by KSP.

Save values are validated when loaded. Negative funds and satellite counts are clamped to zero, launch progress is clamped to 0–100%, and an invalid saved launch body is discarded so the existing rival simulation can choose a valid Kerbin/Mun/Minmus target.

Older saves that do not yet contain Race for Space scenario data remain valid. They receive the 200,000 rival starting balances, zero simulated rival satellites/progress, and Kerbin as the first planned rival launch until that state is first saved.

The command center keeps one controller across ordinary scene changes inside the same KSP game, but binds that controller to the current KSP `Game` object so loading a different save in the same KSP process does not carry rival state across saves.

## Space Race

The Space Race view presents the existing player satellite milestones. For each body it shows:

- current progress against the required satellite count;
- whether the player's milestone is complete.

The previous explanatory subtitle, associated-programme line, and race-status line are removed from this view.

The view still documents the prototype tracking rules in-game: Probe and Relay vessel types, ORBITING situation, loaded and unloaded ProtoVessel records, and the five-second refresh interval.

## Window behaviour

- The command center does not initialize or render during KSP loading screens or on the main menu.
- The command center becomes available only after KSP has entered an actual loaded-game scene.
- Returning from a save to the main menu suppresses the interface immediately during the scene transition.
- F8 shows or hides the complete Race for Space interface while in a loaded game.
- Only one command-center window is created.
- Overview, Funding Targets, Rival Agencies, and Space Race are navigation views inside that window.
- Only one view is visible at a time.
- Switching views does not create or destroy separate windows.
- The navigation row remains available while moving between views.
- The command-center window remains draggable through KSP/Unity IMGUI behaviour.
- The window uses the normal opaque KSP background.
- A defensive single-instance guard prevents KSP editor scene transitions from rendering duplicate command-center windows.

## Versioning

The project metadata for this prototype remains `0.2.0`.

Version 0.2 now persists the rival values listed above. Other Race for Space state remains prototype/session-derived unless explicitly covered by the persistence list.

## Verification focus

For this 0.2 test:

1. Build and deploy the `prototype/interface-v0.2` branch.
2. Start KSP and confirm no Command Center is shown during loading or on the main menu.
3. Load a save and confirm one Command Center appears in the loaded game.
4. Confirm the window remains available through Space Center, editor, and flight scene changes without duplicates.
5. Return to the main menu and confirm the Command Center disappears.
6. Confirm the Overview no longer shows Race Summary or Funding Programmes Decided.
7. Confirm Funding Targets no longer display Winner/claimed status.
8. Confirm Funding Target cards use target/requirement/pool information on the left and Player/Aster/Cobalt progress and payout information on the right.
9. Confirm Rival Agency cards use funding/launch information on the left and Kerbin/Mun/Minmus satellite counts on the right.
10. Confirm Funding Target progress lines show only the current satellite count rather than `current/required`.
11. Confirm the Space Race view no longer shows its explanatory subtitle, associated programme, or race status.
12. Confirm projected payouts still follow completion percentage below saturation and ownership ratio after saturation.
13. Confirm the next funding date advances in 90-Kerbin-day intervals.
14. On an older/new save without persisted Race for Space rival data, confirm both rivals start with 200,000 funds and `Next Launch Planned: Kerbin`.
15. Confirm an already-persisted rival launch reloads with its saved planned body rather than being forced back to Kerbin.
16. Advance across five-day Kerbin boundaries and verify each rival has a 50% chance to gain 10% progress.
17. With Next Launch Planned set to Kerbin, confirm `Launch Progress Cost: 20,000` and every successful +10% step deducts 20,000.
18. With Next Launch Planned set to Mun or Minmus, confirm `Launch Progress Cost: 30,000` and every successful +10% step deducts 30,000.
19. Confirm a rival cannot make a successful progress step when its funds are below the currently displayed Launch Progress Cost.
20. Confirm `ETA till Launch` uses 10 expected Kerbin days per remaining 10% step when funds are available; for example, 50% progress should show approximately 50 days.
21. Confirm a cash-limited rival's ETA uses the body-adjusted cost and includes waits for projected 90-day funding payments.
22. Confirm reaching 100% adds one Kerbin satellite for the initial launch without a second launch-cost deduction, resets progress, randomly selects the next body, and updates Launch Progress Cost for that body.
23. Change rival funds/progress/satellite counts, save the game, leave or restart KSP, reload the save, and confirm those rival values return unchanged.
24. Specifically confirm a partially completed launch reloads with the same Next Launch Planned body, Launch Progress percentage, and derived Launch Progress Cost.
25. After reloading, cross the saved next five-day progress-check boundary and confirm the launch cadence resumes rather than resetting from the load time.
26. Load a different KSP save in the same process and confirm rival funds/satellites/progress come from that save rather than the previously open save.
27. Reach a scheduled funding date and confirm rival balances increase by their Next Payout amounts.
28. In Career mode, confirm the player's Next Payout is added to the real KSP funds balance on the scheduled funding date.
29. Confirm F8 hides and restores the complete interface only while a game is loaded.

# Interface Prototype 0.2

Version 0.2 focuses on presenting the existing satellite-race prototype as a coherent command-center interface while refining the first funding model. It deliberately does not add new funding targets, rival agencies, vessel rules, persistence, or Career-mode funding integration.

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
- player race points;
- current prototype payout;
- number of funding programmes already decided;
- qualifying satellite counts around Kerbin, Mun and Minmus;
- a compact status line for each current funding programme.

### Funding Targets

The Funding Targets view presents the same three prototype programmes already present in version 0.1:

- Kerbin Orbital Network;
- Mun Survey Network;
- Minmus Relay Initiative.

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

No additional contracts or funding target types are introduced in 0.2.

## Funding payout rule

Each funding target has one fixed **Total Available Payout**. The combined current payouts across all agencies must never exceed that amount.

Before the combined satellite count reaches the requirement, each agency receives a share based on its completion percentage:

`Current Payout = Total Available Payout × (Agency Satellites / Required Satellites)`

For example, if a target requires 10 satellites and the player has 3 while no other qualifying satellites exist, the player's current payout is 30% of the available pool.

When the combined player and rival satellite count exceeds the requirement, the target is saturated. The complete payout pool is then distributed according to ownership of all qualifying satellites:

`Current Payout = Total Available Payout × (Agency Satellites / Total Satellites)`

For example, if the target requires 10 satellites but 12 qualifying satellites exist and the player owns 8, the player receives 8/12 of the total payout while the remaining 4/12 is shared by the rivals according to their own satellite counts.

At exactly the required combined satellite count, the completion-percentage and ownership-share calculations produce the same result. Once saturated, adding more satellites changes the ratio but never increases the combined payout above the fixed total available amount.

First-to-requirement remains a separate competitive achievement and still awards the race point. Winning that race milestone does not grant exclusive ownership of the funding pool.

### Rival Agencies

The Rival Agencies view presents the existing two simulated agencies:

- Aster Aerospace Directorate;
- Cobalt Orbital Bureau.

For each rival it shows race points and tracked satellite counts around Kerbin, Mun and Minmus. A compact comparison area shows the player and both rivals together.

The rival simulation rules and timing remain unchanged from version 0.1.

### Space Race

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

## Versioning

The project metadata for this prototype is `0.2.0` and the interface identifies itself as Prototype 0.2.

Version 0.2 remains a prototype. Broader gameplay expansion stays out of scope while the interface and initial shared funding model are tested in KSP.

## Verification focus

For this 0.2 test:

1. Build and deploy the `prototype/interface-v0.2` branch.
2. Confirm one opaque Command Center window appears and remains draggable.
3. Confirm the four navigation buttons are named Overview, Funding Targets, Rival Agencies, and Space Race.
4. Confirm selecting a navigation button replaces the content in the same window.
5. Confirm only one view is visible at a time and no additional windows appear.
6. Confirm each Funding Target uses the requested data order and `Total Available Payout` wording.
7. Confirm a programme below saturation pays each agency according to its percentage of the required satellite count.
8. Confirm an oversaturated programme splits the fixed pool according to each agency's share of all qualifying satellites.
9. Confirm the combined displayed current payouts never exceed the Total Available Payout.
10. Confirm first-to-requirement still records the winner/race point separately from the shared payout.
11. Confirm F8 hides and restores the complete interface.
12. Confirm existing satellite tracking and rival simulation still refresh correctly.

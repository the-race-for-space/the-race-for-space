# Interface Prototype 0.2

Version 0.2 focuses on presenting the existing satellite-race prototype as a coherent command-center interface. It deliberately does not add new funding programmes, rival agencies, vessel rules, persistence, or Career-mode funding integration.

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

The command-center background is semi-transparent so the KSP scene remains visible behind the interface while preserving enough contrast for text and controls.

### Overview

The Overview is the player's home screen for the race. It shows:

- prototype version;
- player race points;
- prototype funding won;
- number of funding programmes already decided;
- qualifying satellite counts around Kerbin, Mun and Minmus;
- a compact status line for each current funding programme.

### Funding Targets

The Funding Targets view presents the same three prototype programmes already present in version 0.1:

- Kerbin Orbital Network;
- Mun Survey Network;
- Minmus Relay Initiative.

Each target shows its target body, satellite requirement, player progress, prototype reward, and both rival progress values.

Open targets do not display a redundant `Status: OPEN` line. Once a target has been claimed, the view identifies the winning programme.

No additional contracts or funding programme types are introduced in 0.2.

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
- The window background is semi-transparent rather than fully opaque.

## Versioning

The project metadata for this prototype is `0.2.0` and the interface identifies itself as Prototype 0.2.

Version 0.2 is intentionally an interface prototype. Gameplay expansion remains out of scope until this layout has been tested in KSP.

## Verification focus

For this 0.2 interface test:

1. Build and deploy the `prototype/interface-v0.2` branch.
2. Confirm one semi-transparent Command Center window appears and remains draggable.
3. Confirm the four navigation buttons are named Overview, Funding Targets, Rival Agencies, and Space Race.
4. Confirm selecting a navigation button replaces the content in the same window.
5. Confirm only one view is visible at a time and no additional windows appear.
6. Confirm open funding targets do not display `Status: OPEN`.
7. Confirm claimed funding targets still identify their winner.
8. Confirm F8 hides and restores the complete interface.
9. Confirm existing satellite counts, rival progress and programme winners display the same underlying values as version 0.1.
10. Confirm the semi-transparent background remains readable against normal KSP scenes.

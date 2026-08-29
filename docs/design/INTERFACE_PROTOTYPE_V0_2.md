# Interface Prototype 0.2

Version 0.2 focuses on presenting the existing satellite-race prototype as a coherent command-center interface. It deliberately does not add new funding programmes, rival agencies, vessel rules, persistence, or Career-mode funding integration.

## Target display

The primary target is a Steam Machine or conventional desktop PC connected to a normal PC-sized display. The interface therefore prioritizes a larger command-center layout rather than handheld-screen constraints.

The default window positions are intended to work well on a typical desktop resolution while remaining draggable so players can arrange the interface for their own display size and KSP layout.

## Interface structure

The interface is organized around one always-available overview window and three independently managed detail windows.

### Command Center Overview

The overview is the player's home screen for the race. It shows:

- prototype version;
- player race points;
- prototype funding won;
- number of funding programmes already decided;
- qualifying satellite counts around Kerbin, Mun and Minmus;
- a compact status line for each current funding programme;
- controls for opening or hiding each detailed race view.

The overview remains visible while any combination of the three detail windows is open.

### Funding Programmes

The funding window presents the same three prototype programmes already present in version 0.1:

- Kerbin Orbital Network;
- Mun Survey Network;
- Minmus Relay Initiative.

Each programme shows its current status, target body, satellite requirement, player progress, prototype reward, and both rival progress values.

No additional contracts or funding programme types are introduced in 0.2.

### Rival Agencies

The rival window presents the existing two simulated agencies:

- Aster Aerospace Directorate;
- Cobalt Orbital Bureau.

For each rival it shows race points and tracked satellite counts around Kerbin, Mun and Minmus. A compact comparison area shows the player and both rivals together.

The rival simulation rules and timing remain unchanged from version 0.1.

### Milestones & Satellite Tracking

The milestones window turns the current vessel counts into a clearer progress view. For each existing programme it shows:

- associated celestial body;
- required and current satellite count;
- whether the player requirement is complete;
- whether the competitive programme is still open or already claimed.

The window also documents the prototype tracking rules in-game: Probe and Relay vessel types, ORBITING situation, loaded and unloaded ProtoVessel records, and the five-second refresh interval.

## Window behaviour

- F8 shows or hides the complete Race for Space interface.
- The Command Center overview remains the primary window.
- Funding, Rivals, and Milestones are opened or hidden independently from the overview.
- Multiple detail windows may be open at the same time.
- Every detail window can be closed independently.
- All windows remain draggable through KSP/Unity IMGUI behaviour.
- Default window sizes are larger than version 0.1 to make better use of normal desktop resolutions.

## Versioning

The project metadata for this prototype is `0.2.0` and the interface identifies itself as Prototype 0.2.

Version 0.2 is intentionally an interface prototype. Gameplay expansion remains out of scope until this layout has been tested in KSP.

## Verification focus

For the first 0.2 desktop test:

1. Build and deploy the `prototype/interface-v0.2` branch.
2. Confirm the Command Center overview appears and remains draggable.
3. Open each of the three detail windows from the overview.
4. Confirm two or all three detail windows can remain open simultaneously.
5. Confirm each Close button hides only its own window.
6. Confirm the overview buttons correctly show and hide each detail window.
7. Confirm F8 hides and restores the complete interface.
8. Confirm existing satellite counts, rival progress and programme winners display the same underlying values as version 0.1.
9. Confirm the default window sizes and positions are comfortable on the target desktop resolution.
10. Rearrange the windows and identify any preferred desktop layout before further visual polish.

# Satellite Race Prototype v1

This vertical slice proves the first competitive gameplay loop without expanding the project architecture.

## Included

- Two simulated rival agencies: Aster Aerospace Directorate and Cobalt Orbital Bureau.
- Three first-to-achieve satellite funding programmes around Kerbin, Mun and Minmus.
- Player satellite counts read from `ProtoVessel` data, including unloaded vessels.
- Deterministic rival progress so test runs are reproducible.
- A basic command-center window with Overview, Funding Programmes, Rival Agencies, Satellite Tracking and Milestones sections.
- F8 toggles the prototype window.

## Funding programmes

| Programme | Requirement | Prototype award |
| --- | --- | ---: |
| Kerbin Orbital Network | 2 satellites orbiting Kerbin | 25,000 |
| Mun Survey Network | 1 satellite orbiting Mun | 40,000 |
| Minmus Relay Initiative | 1 satellite orbiting Minmus | 50,000 |

The first program observed meeting a requirement claims it. Player awards are tracked inside the prototype state for now; stock Career-mode funds integration is intentionally deferred until the base loop is verified.

## Rival schedule

Aster reaches Kerbin after 3 Kerbin days, Mun after 20 days and Minmus after 45 days. Cobalt reaches Kerbin after 5 days, Mun after 15 days and Minmus after 35 days.

## Manual verification

1. Build the mod against a KSP 1.12.x install and place the assembly in `GameData/TheRaceForSpace/Plugins/`.
2. Start or load a game and confirm the command-center window appears; press F8 twice to verify hide/show.
3. Launch a craft whose vessel type is Probe or Relay into Kerbin orbit.
4. Return to another scene or leave the vessel unloaded and wait for the five-second refresh.
5. Confirm Satellite Tracking still counts the unloaded vessel.
6. Put a second satellite around Kerbin and confirm the Kerbin programme is awarded to the player if a rival has not already reached it.
7. Repeat with Mun and Minmus.
8. Time-warp through rival thresholds and confirm rival counts and programme winners update without per-frame scanning.

## Known prototype limits

- Race state is session-only; save persistence is the next implementation step.
- Player funding awards are displayed by the mod but are not yet written into KSP Career funds.
- Satellite classification is deliberately narrow: only orbiting `Probe` and `Relay` vessel types count.
- Rival progress is deterministic and abstract rather than represented by physical vessels.

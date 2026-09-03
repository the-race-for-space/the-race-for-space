# Kerbal Contracts v0.5 - Live KSP Acceptance Checklist

This checklist covers behavior that the standalone logic/controller suites cannot prove without a real Kerbal Space Program 1.12.x installation. Run it against `Alpha/KerbalContracts-v0.5` after building/deploying the 0.5.0 assembly with `KSP_ROOT` pointing at the test installation.

## Build and baseline

1. Build the production project with the local KSP assemblies and confirm there are no C# or KSP API compile errors.
2. Deploy the resulting assembly to a clean test copy of `GameData/TheRaceForSpace/Plugins` together with the current RaceSettings config.
3. Start a fresh Career save and confirm the KSP log contains no repeated Race for Space exceptions or per-frame starter-contract spam.
4. Open the Command Center with F8 and the stock launcher button and confirm both controls still share the same persisted visibility state.

## Fresh-campaign offers and UI

1. Confirm the initial starter choices are Directed Power I, Mass I, Control I, and Biome I.
2. Confirm those four special choices do not consume the normal maximum of two random achievement-offer slots.
3. In Space Race, confirm `STARTER PROGRAMMES` appears above the normal funding catalogue with four cards in one usable row at common resolutions.
4. Confirm each card shows `0 / 5`, its Level I objective, and a 10,000 100% payout.
5. Confirm the normal Space Race Offered/Unlocked/Locked/Expired catalogue does not duplicate the twenty starter contracts.
6. Confirm Funding Targets still shows offered starter contracts and, after completion, their continuing payout state.
7. Confirm Overview shows only the current starter objective from each line rather than every previously completed starter contract that is still paying.
8. Confirm the Help guide explains the four starter lines and the Level V to Probe Orbit convergence.

## Directed Power line

Repeat the relevant threshold for each level: 600, 1,100, 1,400, 1,700, and 2,000 m/s.

1. Launch a Kerbin vehicle, reach the required surface speed while remaining at or below 70,000 m, and confirm the Space Race card updates maximum speed and maximum altitude approximately once per second.
2. Land or recover a qualifying-speed vehicle without destroying it and confirm Directed Power does not complete.
3. Destroy the qualifying tracked vehicle in a genuine Kerbin surface impact and confirm the current Directed Power level completes.
4. Exceed 70,000 m at any point, later descend below 70,000 m and impact at sufficient speed, and confirm the attempt remains invalid.
5. Enter `ORBITING`, then return and impact, and confirm the starter attempt remains invalid.
6. Stage during a qualifying flight, keep control of the continuing stage, and confirm peak speed/altitude history follows the same launch.
7. Save after exceeding the 70 km ceiling, reload, then impact; confirm the saved ceiling violation is not lost.
8. Confirm ordinary vessel destruction away from the surface does not falsely complete Directed Power.
9. Confirm a low-speed destruction/cleanup near the ground does not incorrectly count as the required high-energy surface impact.

## Mass line

Required pairs are 1 t / 25 km, 2.5 t / 75 km, 5 t / 150 km, 10 t / 300 km, and 20 t / 600 km.

1. Confirm the card displays current remaining vessel mass and distance from the launch origin during flight.
2. Reach the required distance with insufficient remaining mass and confirm no completion.
3. Meet the required remaining mass while still short of the required distance and confirm no completion.
4. Meet both requirements simultaneously and confirm the current Mass level completes immediately.
5. Continue the same flight far enough/heavy enough for the next level and confirm only one Mass level can complete per launch.
6. Start a new launch and confirm the newly unlocked next Mass level can complete.
7. Confirm entering orbit invalidates the starter attempt.
8. Note the current v0.5 rule: distance is measured from the tracked launch/start position. A normal KSC LaunchPad/Runway mission therefore behaves as distance from the Space Centre, while an alternate launch site uses that alternate origin. Record this explicitly if alternate launch sites are part of balance testing.

## Control line

Required bands/times are 2-5 km / 30 s, 8-12 km / 45 s, 15-25 km / 60 s, 30-40 km / 75 s, and 50-65 km / 90 s.

1. Launch with at least one Kerbal and enter the required band. Confirm altitude, hold time, and crew count update in the live card.
2. Remain continuously inside the band for the required time and confirm the UI changes to the qualified state instructing the player to land safely.
3. Land on Kerbin with crew aboard and confirm completion.
4. Leave the altitude band before qualification and confirm the continuous timer resets.
5. Remove/lose all crew before the hold completes and confirm no qualification.
6. Complete the hold but splash down instead of entering `LANDED` and confirm the contract does not complete.
7. Complete the hold but enter orbit before landing and confirm the starter attempt is invalid.
8. Save halfway through a valid hold, reload, continue the remaining hold time and land; confirm accumulated hold time survives correctly when observations resume normally.
9. Save after the hold has qualified but before landing, reload, land safely, and confirm completion still works.
10. While an unqualified hold is in progress, create a long interval in which the active vessel is not observed (for example by leaving the flight scene and advancing time), then return to the same vessel. Confirm the missing interval is not credited and the unqualified continuous hold restarts rather than jumping forward.

## Biome line

Targets are Grasslands, Highlands, Mountains, Deserts, and Ice Caps.

1. Confirm the live card reports the stock Kerbin biome of the active vessel and the current target biome.
2. Enter the required biome without orbiting and confirm the current Biome level completes immediately.
3. Continue into the next required biome during the same launch and confirm a second Biome level does not complete from that launch.
4. Start a fresh launch and confirm the next unlocked Biome level can complete.
5. Confirm biome reporting remains stable across low flight, landed flight, and scene transitions where the active vessel remains available.
6. Confirm no Biome completion occurs away from Kerbin.

## Cross-line behavior

1. Use one launch that legitimately satisfies conditions in two different lines, such as Mass and Biome, and confirm both lines may advance once on that launch.
2. Confirm each individual line can advance no more than one level per launch.
3. Confirm staging does not accidentally create a new attempt when launch time/body match the ongoing vehicle.
4. Confirm switching to an unrelated vessel/launch begins a new tracked attempt rather than inheriting the previous vehicle's maxima or Control timer.

## Probe Orbit convergence

Test each Level V route independently in a disposable save or by restoring a backup.

1. Complete Directed Power V and confirm Probe Orbit is offered immediately without waiting for a funding review.
2. Repeat with Mass V, Control V, and Biome V and confirm each one independently satisfies the OR gate.
3. Allow a rival to complete a starter Level V first and confirm Probe Orbit becomes available to the whole race.
4. Confirm Probe Orbit itself remains a normal uncrewed orbital milestone and a qualifying player Probe/Relay can complete it through the existing orbital vessel tracker.
5. Confirm completing a starter contract by a rival does not create a fake Kerbin satellite; only an actual uncrewed orbit milestone may do so.

## Funding and rival balance

1. Confirm Level I-V starter base payouts are 10,000 / 20,000 / 30,000 / 40,000 / 50,000.
2. After first completion of a starter achievement, confirm its funding contract starts and follows the normal ten-payment sequence: 100%, 90%, 80%, 70%, 60%, 50%, 40%, 30%, 20%, 10%.
3. Confirm multiple agencies completing the same starter achievement split later payouts using the same existing achievement rules.
4. Confirm each successful rival progress check on a starter contract advances launch progress by 20%, following 0% -> 20% -> 40% -> 60% -> 80% -> 100%.
5. Confirm the corresponding Level I-V starter progress-step costs are 4,000 / 6,000 / 8,000 / 10,000 / 12,000. Five successful steps should therefore preserve the previous full-development costs of 20,000 / 30,000 / 40,000 / 50,000 / 60,000.
6. Confirm normal orbital and satellite rival missions still advance by 10% per successful progress check and retain their existing costs.
7. Confirm the Rival Agencies ETA reflects five successful development steps from 0% for a starter mission rather than ten, while normal orbit/satellite ETA calculation still uses ten 10% steps.
8. Confirm current starter-line offers remain available independently of the normal two unfinished achievement offers throughout this progression.

## Persistence and scene changes

1. Save and reload with no active flight and confirm no starter attempt is invented.
2. Save and reload during a valid active attempt and confirm only the intended historical state persists: maxima, launch origin, Control state, orbit invalidation, and line-completion flags.
3. Confirm current telemetry such as instantaneous mass/altitude/biome is repopulated by the next live vessel sample rather than stale saved values.
4. Move Flight -> Space Center -> Tracking Station -> Flight and confirm the controller, offers, completed achievements, and saved starter state remain consistent.
5. Load a different KSP save in the same process and confirm no active-flight state or destruction callback leaks from the previous save.
6. Inspect the save file for a `STARTER_FLIGHT` node only when Race for Space has captured starter state and confirm older saves without it load normally.

## Regression and release acceptance

The v0.5 candidate is ready for merge/release only when:

- the GitHub `Logic Tests` workflow is green and its log explicitly includes `PASS: Starter flight contracts and persistence`;
- the production assembly builds against the target KSP 1.12.x installation;
- all four starter lines can be completed end-to-end in KSP;
- at least one Level V route has been followed through a real Probe Orbit completion;
- no starter line can be advanced twice by one launch;
- saving/loading cannot erase Directed Power invalidation or manufacture Control progress;
- Space Race, Overview, Funding Targets, and Rival Agencies remain usable without layout-breaking overlap or exceptions;
- no repeated exceptions or obvious performance/log-spam regressions appear during normal flight.

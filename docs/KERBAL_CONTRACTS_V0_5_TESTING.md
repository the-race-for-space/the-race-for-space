# Kerbal Contracts v0.5 - Live KSP Acceptance Checklist

This checklist covers behavior that the standalone logic/controller suites cannot prove without a real Kerbal Space Program 1.12.x installation. Run it against `Alpha/KerbalContracts-v0.5` after building/deploying the 0.5.0 assembly with `KSP_ROOT` pointing at the test installation.

## Build and baseline

1. Build the production project with the local KSP assemblies and confirm there are no C# or KSP API compile errors.
2. Deploy the resulting assembly to a clean test copy of `GameData/TheRaceForSpace/Plugins` together with the current RaceSettings config.
3. Start a fresh Career save and confirm the KSP log contains no repeated Race for Space exceptions or per-frame starter-contract spam.
4. Open the Command Center with F8 and the stock launcher button and confirm both controls still share the same persisted visibility state.

## Fresh-campaign offers and UI

1. Confirm the initial starter offers are Directed Power I, Mass I, Control I, and Biome I.
2. In Space Race, confirm those four contracts appear in the normal `Offered` section.
3. Confirm Directed Power II-V, Mass II-V, Control II-V, and Biome II-V appear in the normal `Locked` section at campaign start: sixteen locked starter contracts in total.
4. Confirm there is no separate `STARTER PROGRAMMES` four-card panel taking space above the catalogue.
5. Open each starter contract from the normal Space Race catalogue and confirm its objective, payout, state, and unlock requirements use the same presentation as other achievement contracts.
6. Complete one Level I contract with the player and confirm its Level II successor moves from `Locked` to `Unlocked` rather than immediately becoming `Offered`.
7. In a disposable save, allow a rival to complete one Level I starter contract first and confirm the same Level II successor also moves from `Locked` to `Unlocked`; starter prerequisites are `Any Agency`.
8. Unlock more than two starter successors before the same funding boundary and confirm **every unlocked starter contract becomes `Offered` at that funding review**. In particular, if all four Level II contracts are unlocked, all four must be offered together; starter contracts do not consume the normal two unfinished-achievement slots.
9. Confirm the normal two-offer limit still applies to Probe Orbit and later one-off achievement contracts, and that satellite programmes still have their own independent two-unfulfilled-offer limit.
10. Confirm Funding Targets shows live starter telemetry inside **every offered, unfinished starter contract card** during an active flight, so multiple offered starter thresholds can be compared against the same craft at once.
11. Confirm Space Race itself does not duplicate that live-flight panel; it remains focused on `Offered`, `Unlocked`, `Locked`, and `Expired` catalogue state. Confirm its `Current Funding Info` area is visibly 25% taller than the previous 220 px layout (275 px).
12. Confirm Overview treats offered starter contracts the same way as other offered one-off achievements.
13. Confirm the Help guide explains the four opening starter offers, same-line progression, Any Agency unlocks, and the Level V to Probe Orbit convergence.

## Directed Power line

Repeat the relevant threshold for each level: 600, 1,100, 1,400, 1,700, and 2,000 m/s.

1. Launch a Kerbin vehicle with the relevant starter contract offered, reach the required surface speed while remaining at or below 70,000 m, and confirm its Funding Targets card updates maximum speed and maximum altitude approximately once per second.
2. Land or recover a qualifying-speed vehicle without destroying it and confirm Directed Power does not complete.
3. Crash the qualifying tracked vehicle into Kerbin and confirm the current Directed Power level completes even if KSP briefly reports the dying vessel as `LANDED` or zero surface speed during breakup.
4. Repeat a qualifying crash at terrain rather than sea level and confirm the destruction is still recognised as a surface impact.
5. Exceed 70,000 m at any point, later descend below 70,000 m and impact at sufficient speed, and confirm the attempt remains invalid.
6. Enter `ORBITING`, then return and impact, and confirm the starter attempt remains invalid.
7. Stage during a qualifying flight, keep control of the continuing stage, and confirm peak speed/altitude history follows the same launch and the final controlled stage may complete the impact.
8. Save after exceeding the 70 km ceiling, reload, then impact; confirm the saved ceiling violation is not lost.
9. Confirm ordinary vessel destruction well away from the surface does not falsely complete Directed Power.
10. Confirm a low-speed vessel deletion/recovery near the ground does not incorrectly count as the required high-energy surface impact.
11. Inspect `KSP.log` after the crash and confirm there are no repeated callback exceptions from either the vessel-local destruction callback or the global vessel-will-destroy fallback.

## Mass line

Required pairs are 1 t / 25 km, 2.5 t / 75 km, 5 t / 150 km, 10 t / 300 km, and 20 t / 600 km.

1. Confirm every offered unfinished Mass contract card in Funding Targets displays current remaining vessel mass, distance from the launch origin, and live `Landed: YES/NO` state during flight.
2. Fly beyond the required distance while retaining enough mass and confirm the contract does **not** complete while the craft is still flying.
3. Reach the required distance but land with less than the required final vessel mass and confirm no completion.
4. Keep sufficient mass but land short of the required distance and confirm no completion.
5. Land on Kerbin beyond the required distance with the finished landed craft still retaining at least the required mass and confirm the current Mass level completes.
6. Continue or move the same landed craft so that it could satisfy the next level and confirm only one Mass level can complete per launch attempt.
7. Start a new launch and confirm the newly unlocked/offered next Mass level can complete after another qualifying landing.
8. Confirm `SPLASHED` does not count as `LANDED` for the Mass delivery requirement.
9. Confirm entering orbit invalidates the starter attempt.
10. Note the current v0.5 rule: distance is measured from the tracked launch/start position. A normal KSC LaunchPad/Runway mission therefore behaves as distance from the Space Centre, while an alternate launch site uses that alternate origin. Record this explicitly if alternate launch sites are part of balance testing.

## Control line

Required bands/times are 2-5 km / 30 s, 8-12 km / 45 s, 15-25 km / 60 s, 30-40 km / 75 s, and 50-65 km / 90 s.

1. Launch with at least one Kerbal and enter the required band. Confirm altitude, hold time, and crew count update in each offered unfinished Control contract card in Funding Targets against that contract's own thresholds.
2. Remain continuously inside the band for the required time and confirm the Funding Targets live status changes to the qualified state instructing the player to land safely.
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

1. Confirm every offered unfinished Biome contract card in Funding Targets reports the active vessel's stock Kerbin biome, that contract's target biome, and live `Landed: YES/NO` state.
2. Fly over or through the required biome without landing and confirm the Biome contract does **not** complete.
3. Land the craft in the required Kerbin biome and confirm the current Biome level completes only once the vessel situation is `LANDED`.
4. Splash down in or beside the target biome and confirm `SPLASHED` does not count as the required landing.
5. Continue or move the same craft into the next target biome and confirm a second Biome level does not complete from that launch attempt.
6. Start a fresh launch and confirm the next unlocked/offered Biome level can complete after landing in its target biome.
7. Confirm biome reporting remains stable during low flight and after touchdown so the landed sample reports the correct stock biome.
8. Confirm no Biome completion occurs away from Kerbin.

## Cross-line behavior

1. Use one launch that legitimately satisfies conditions in two different lines, such as landing a sufficiently heavy craft in the required Biome at the required Mass distance, and confirm both lines may advance once on that landing.
2. Confirm each individual line can advance no more than one level per launch.
3. Confirm staging does not accidentally create a new attempt when launch time/body match the ongoing vehicle.
4. Confirm switching to an unrelated vessel/launch begins a new tracked attempt rather than inheriting the previous vehicle's maxima or Control timer.

## Probe Orbit convergence

Test each Level V route independently in a disposable save or by restoring a backup.

1. Complete Directed Power V and confirm Probe Orbit is offered immediately without waiting for a funding review.
2. Repeat with Mass V, Control V, and Biome V and confirm each one independently satisfies the OR gate.
3. Allow a rival to complete a starter Level V first and confirm Probe Orbit is offered immediately to the whole race.
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
8. Confirm Levels II-V of the starter lines wait in `Unlocked` after their predecessor is completed by any agency, then **all currently unlocked starter contracts become `Offered` at the next funding review** with no starter offer limit.
9. With several starter contracts still unfinished and offered, unlock at least three normal Probe-or-later achievement candidates and confirm only two normal achievement contracts are offered; starter offers must not consume those two normal slots. Confirm satellite offers remain governed by their separate two-unfulfilled-offer limit.

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
- a fresh campaign shows exactly the four Level I starter offers and the other sixteen starter contracts locked in the normal Space Race catalogue;
- player and rival starter completions both unlock the correct next same-line contract;
- every unlocked starter contract is offered at the next funding review without consuming the normal two unfinished-achievement slots;
- the normal Probe-and-later achievement pool still caps unfinished offers at two, independently of the satellite pool's own two-offer cap;
- every offered unfinished starter contract shows its own live criteria values in Funding Targets without a separate Space Race starter panel blocking the catalogue;
- Space Race keeps a usable 275 px Current Funding Info area above the catalogue;
- Biome only completes on a landed craft in the target biome;
- Mass only completes on a landed finished craft that still meets both final mass and distance requirements;
- a real qualifying Directed Power surface crash reliably produces completion without turning normal recovery into a false crash;
- all four starter lines can be completed end-to-end in KSP;
- at least one Level V route has been followed through a real Probe Orbit completion;
- no starter line can be advanced twice by one launch;
- saving/loading cannot erase Directed Power invalidation or manufacture Control progress;
- Space Race, Overview, Funding Targets, and Rival Agencies remain usable without layout-breaking overlap or exceptions;
- no repeated exceptions or obvious performance/log-spam regressions appear during normal flight.

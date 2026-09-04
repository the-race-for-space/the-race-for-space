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
13. Confirm the Help guide explains the four opening starter offers, same-line progression, Any Agency unlocks, that every Offered unfinished starter contract is active independently, and the Level V to Probe Orbit convergence.

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
12. Repeat a qualifying destructive impact while using physics warp (2x and 4x where practical) and confirm Directed Power still completes. The impact detector should use the actual universal-time gap since the last in-flight sample rather than assuming a fixed two-second travel interval.
13. Create a state where Directed Power I and II are both `Offered` and unfinished for the player. Reach at least 1,100 m/s without exceeding 70 km and impact Kerbin; confirm **both** contracts complete from that one impact, while Directed Power III remains untouched if it has not separately become `Offered`.

## Mass line

Required pairs are 1 t / 25 km, 2.5 t / 75 km, 5 t / 150 km, 10 t / 300 km, and 20 t / 600 km.

1. Confirm every offered unfinished Mass contract card in Funding Targets displays current remaining vessel mass, distance from the launch origin, and live `Landed: YES/NO` state during flight.
2. Fly beyond the required distance while retaining enough mass and confirm the contract does **not** complete while the craft is still flying.
3. Reach the required distance but land with less than the required final vessel mass and confirm no completion.
4. Keep sufficient mass but land short of the required distance and confirm no completion.
5. Land on Kerbin beyond the required distance with the finished landed craft still retaining at least the required mass and confirm each active Mass contract whose own mass/distance requirements are met completes.
6. Create a state where Mass I and Mass II are both `Offered` and unfinished. Land one craft with at least 2.5 t more than 75 km from launch and confirm **both Mass I and Mass II complete from the same landing**.
7. In the same test, make the craft strong enough to satisfy Mass III as well but leave Mass III merely `Unlocked` or `Locked`; confirm Mass III does **not** complete until it is separately `Offered`.
8. Confirm `SPLASHED` does not count as `LANDED` for the Mass delivery requirement.
9. Confirm entering orbit invalidates the starter attempt.
10. Note the current v0.5 rule: distance is measured from the tracked launch/start position. A normal KSC LaunchPad/Runway mission therefore behaves as distance from the Space Centre, while an alternate launch site uses that alternate origin. Record this explicitly if alternate launch sites are part of balance testing.

## Control line

Required bands/times are 2-5 km / 30 s, 8-12 km / 45 s, 15-25 km / 60 s, 30-40 km / 75 s, and 50-65 km / 90 s.

1. Launch with at least one Kerbal and enter the required band. Confirm altitude, hold time, and crew count update in each offered unfinished Control contract card in Funding Targets against that contract's own thresholds. When more than one Control level is Offered, each card must show its own contract-specific hold/qualification state rather than a shared timer.
2. Remain continuously inside the band for the required time and confirm the Funding Targets live status changes to the qualified state instructing the player to land safely.
3. Land on Kerbin with crew aboard and confirm completion.
4. Leave the altitude band before qualification and confirm the continuous timer resets.
5. Remove/lose all crew before the hold completes and confirm no qualification.
6. Complete the hold but splash down instead of entering `LANDED` and confirm the contract does not complete.
7. Complete the hold but enter orbit before landing and confirm the starter attempt is invalid.
8. Save halfway through a valid hold, reload, continue the remaining hold time and land; confirm accumulated hold time survives correctly when observations resume normally.
9. Save after the hold has qualified but before landing, reload, land safely, and confirm completion still works.
10. While an unqualified hold is in progress, create a long interval in which the active vessel is not observed (for example by leaving the flight scene and advancing time), then return to the same vessel. Confirm the missing interval is not credited and the unqualified continuous hold restarts rather than jumping forward.
11. Create a state where Control I and Control II are both `Offered`. Qualify Control I at 2-5 km, then begin Control II at 8-12 km and save before its 45-second hold is complete. Before saving, confirm Funding Targets shows Control I as qualified while Control II shows its own partial hold rather than hiding Control II or asking for a new launch. Reload and confirm Control I remains qualified while Control II resumes from its own saved partial hold; finish Control II and land safely, then confirm **both contracts complete on the same landing**.
12. In that multi-Control save, inspect `ACTIVE_CONTRACT_PROGRESS` and confirm each tracked Control contract has its own `CONTROL_STATE` child containing `milestoneId`, `holdSeconds`, `wasSampleInBand`, and `qualified`.

## Biome line

Targets are Grasslands, Highlands, Mountains, Deserts, and Ice Caps.

1. Confirm every offered unfinished Biome contract card in Funding Targets reports the active vessel's stock Kerbin biome, that contract's target biome, and live `Landed: YES/NO` state.
2. Fly over or through the required biome without landing and confirm the Biome contract does **not** complete.
3. Land the craft in the required Kerbin biome and confirm that offered Biome contract completes only once the vessel situation is `LANDED`.
4. Splash down in or beside the target biome and confirm `SPLASHED` does not count as the required landing.
5. Create a state where Biome I and Biome II are both `Offered`. Land in Grasslands to complete Biome I, then continue the **same launch attempt** and later land in Highlands; confirm Biome II can complete independently without requiring a new launch.
6. Confirm a later Biome level that is not `Offered` does not complete even if the same mission eventually visits its target biome.
7. Confirm biome reporting remains stable during low flight and after touchdown so the landed sample reports the correct stock biome.
8. Confirm no Biome completion occurs away from Kerbin.

## Active telemetry gating and idle path

1. With the four opening Level I contracts active, confirm Directed Power, Mass, Control, and Biome live telemetry all continue to update correctly; together those four lines require the full starter telemetry mask.
2. In a disposable state, complete or expire Directed Power, Control, and Biome while leaving only a Mass contract Offered and unfinished. Confirm Mass telemetry and completion still work normally, and confirm no Directed Power destruction callback exceptions appear while no Directed Power contract is active.
3. Repeat with only a Biome contract active and confirm biome/landing behavior still works without requiring a Mass or Control contract to remain active.
4. Repeat with only a Control contract active and confirm altitude, crew, independent hold state, and landing completion still work normally.
5. Reach a state with **zero Offered unfinished starter contracts** (for example after completing the currently Offered set before the next sponsor review), remain in flight for several one-second intervals, and confirm there are no starter-tracking exceptions, impact callback errors, or unexpected contract completions. The runtime should skip active-vessel starter discovery/evaluation until a later offer replaces the cached active set.
6. While still in the same save, allow the next sponsor review to Offer a new starter contract and confirm its required live telemetry resumes on the next one-second observation without restarting the controller or reloading the save.
7. If profiling the mod, compare a Mass-only, Biome-only, Control-only, and zero-active state. `GetTotalMass()` should only be present for Mass, `ScienceUtil.GetExperimentBiome()` only for Biome, crew collection only for Control, and Directed Power destruction tracking only while Directed Power is active.

## Cross-line behavior

1. Use one launch that legitimately satisfies conditions in two different lines, such as landing a sufficiently heavy craft in the required Biome at the required Mass distance, and confirm both active contracts may complete on that landing.
2. Confirm Mass, Biome, Directed Power, and Control evaluate each **Offered, unfinished contract independently** rather than choosing one highest unlocked level. A qualifying flight may complete multiple Offered levels in the same line, but must not complete a merely Locked/Unlocked level that is absent from the active set.
3. Confirm multiple Offered Control contracts retain separate hold/qualification state throughout the same launch and can complete together on one qualifying crewed landing.
4. Confirm staging does not accidentally create a new attempt when launch time/body match the ongoing vehicle.
5. Confirm switching to an unrelated vessel/launch begins a new tracked attempt rather than inheriting the previous vehicle's maxima or Control timers.

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

1. Inspect a current v0.5 Race for Space scenario node and confirm persistence is divided into exactly three gameplay sections: `FUNDING_CONTRACTS`, `RIVALS`, and `ACTIVE_CONTRACT_PROGRESS` (plus the separate command-center visibility value).
2. Inspect `FUNDING_CONTRACTS` and confirm player completion timestamps use `PLAYER_ACHIEVEMENT`, achievement funding contracts use `ACHIEVEMENT_CONTRACT`, and satellite funding contracts use `SATELLITE_CONTRACT`, all identified by stable `id` values. Confirm locked/unoffered contracts are represented explicitly rather than disappearing from the saved contract set.
3. Save and reload with no active flight and confirm no active contract attempt is invented.
4. Save and reload during a valid active attempt and confirm `ACTIVE_CONTRACT_PROGRESS` preserves the intended temporary condition state: maxima, launch origin, every per-contract Control hold/qualification state, and orbit invalidation.
5. Confirm current telemetry such as instantaneous mass/altitude/biome is repopulated by the next live vessel sample rather than stale saved values.
6. Move Flight -> Space Center -> Tracking Station -> Flight and confirm the controller, offers, completed achievements, rival state, and saved active-contract progress remain consistent.
7. Load a different KSP save in the same process and confirm no funding-contract, rival, active-flight, or destruction-callback state leaks from the previous save.
8. Inspect `ACTIVE_CONTRACT_PROGRESS` and confirm Control progress is represented by repeated `CONTROL_STATE` children. Confirm obsolete single-Control values such as `controlHoldMilestoneId` and per-line completion flags are no longer written.
9. Corrupt one current-format `CONTROL_STATE` entry in a disposable save and confirm the malformed active attempt fails closed rather than creating invented hold progress.
10. Confirm earlier development-only persistence nodes such as `RACE_PROGRESS` and `STARTER_FLIGHT` are not written by the current v0.5 format; compatibility with those pre-cleanup development saves is intentionally not required.

## Regression and release acceptance

The v0.5 candidate is ready for merge/release only when:

- the GitHub `Logic Tests` workflow is green and its log explicitly includes `PASS: Starter flight contracts and persistence`;
- the production assembly builds against the target KSP 1.12.x installation;
- a fresh campaign shows exactly the four Level I starter offers and the other sixteen starter contracts locked in the normal Space Race catalogue;
- player and rival starter completions both unlock the correct next same-line contract;
- every unlocked starter contract is offered at the next funding review without consuming the normal two unfinished-achievement slots;
- the normal Probe-and-later achievement pool still caps unfinished offers at two, independently of the satellite pool's own two-offer cap;
- every offered unfinished starter contract shows its own live criteria values in Funding Targets without a separate Space Race starter panel blocking the catalogue;
- simultaneous Control cards show their own hold/qualification state, and completing one offered Control level does not suppress another offered level's live progress;
- Space Race keeps a usable 275 px Current Funding Info area above the catalogue;
- Biome only completes on a landed craft in the target biome;
- Mass only completes on a landed finished craft that still meets both final mass and distance requirements;
- simultaneously Offered Mass/Biome/Directed Power/Control levels are evaluated independently, while an unoffered higher level is not checked even when the same flight would satisfy it;
- simultaneous Control hold/qualification states survive current-format save/load independently;
- persistence writes the three current sections `FUNDING_CONTRACTS`, `RIVALS`, and `ACTIVE_CONTRACT_PROGRESS`, with all funding contracts stored explicitly by stable ID and temporary condition progress kept separate from contract lifecycle state;
- the active starter telemetry plan requests only the condition families currently represented by Offered unfinished starter contracts, and a zero-active plan bypasses active-vessel starter discovery/evaluation;
- a real qualifying Directed Power surface crash reliably produces completion without turning normal recovery into a false crash;
- all four starter lines can be completed end-to-end in KSP;
- at least one Level V route has been followed through a real Probe Orbit completion;
- saving/loading cannot erase Directed Power invalidation or manufacture Control progress;
- Space Race, Overview, Funding Targets, and Rival Agencies remain usable without layout-breaking overlap or exceptions;
- no repeated exceptions or obvious performance/log-spam regressions appear during normal flight.

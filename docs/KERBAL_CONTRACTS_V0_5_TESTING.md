# Version 0.5 - Live KSP Acceptance Checklist

This checklist covers behaviour that the standalone automated tests cannot fully prove without a real Kerbal Space Program 1.12.x installation.

Run it against:

```text
Alpha/KerbalContracts-v0.5
```

Use a disposable Career save where possible.

## Before testing

1. Run the automated tests:

   ```bash
   bash tools/run-logic-tests.sh
   ```

2. Build and deploy the real mod against KSP 1.12.x.
3. Deploy both:

   ```text
   GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll
   GameData/TheRaceForSpace/Config/CampaignSettings.cfg
   ```

4. Start KSP and confirm there are no repeated mod exceptions in `KSP.log`.

## Quick release smoke test

Before the detailed checks, verify the basic campaign loop:

1. Create a fresh Career save.
2. Open the Command Center with F8 and with the stock launcher button.
3. Open **Contract Catalogue**.
4. Confirm these four Level I Pre-Orbit contracts are `Offered`:
   - Directed Power I
   - Mass I
   - Control I
   - Biome I
5. Confirm Levels II-V of all four lines are `Locked`.
6. Launch a vessel and open **Funding Targets**.
7. Confirm live Flight Contract telemetry appears for every offered, unfinished Pre-Orbit contract.
8. Complete one Level I objective.
9. Confirm the next level in that line becomes `Unlocked`.
10. Cross a sponsor-review funding boundary and confirm the unlocked level becomes `Offered`.
11. Save and reload once and confirm campaign state remains correct.

If this basic loop fails, fix it before running the longer checklist.

---

# 1. Command Center and campaign state

## Fresh campaign

Confirm:

- **Overview**, **Funding Targets**, **Rival Agencies**, and **Contract Catalogue** are available.
- Contract Catalogue uses `Offered`, `Unlocked`, `Locked`, and `Expired` sections.
- there is no separate Pre-Orbit-only panel above the normal catalogue.
- Directed Power I, Mass I, Control I, and Biome I are `Offered`.
- the remaining sixteen Pre-Orbit contracts are `Locked`.
- offered Pre-Orbit contracts appear in Overview like other offered one-off objective funding contracts.

## Any Agency progression

Use the player in one save and a rival in another if practical.

Confirm:

- completing a Pre-Orbit level unlocks the next level in that same line;
- either the player or a rival can satisfy that prerequisite;
- the newly unlocked level waits in `Unlocked` until the next sponsor review;
- all unlocked Pre-Orbit contracts are offered at that review, even if more than two are waiting;
- Pre-Orbit offers do not consume the normal two unfinished one-off objective offer slots;
- satellite-network funding contracts keep their separate offer rules.

---

# 2. Live Flight Contract telemetry

During flight, Funding Targets should show live values about once per second.

## Directed Power card

Expect:

- **Current Speed**
- **Max Speed** compared with the required speed
- **Max Altitude** compared with the 70 km ceiling
- Kerbin impact readiness/status

## Mass card

Expect:

- current remaining vessel mass
- distance from the tracked launch origin
- landed state

## Control card

Expect:

- current altitude and target altitude band
- continuous hold progress
- crew count
- safe-landing readiness after qualification

Each offered Control contract must display its own independent hold/qualification state.

## Biome card

Expect:

- current biome
- target biome
- biome match state
- landed state

## Telemetry failure messages

While in Flight with a normal active vessel, the UI should not remain stuck on:

```text
Waiting for active vessel telemetry...
```

If that message appears continuously, investigate the runtime/tracking path rather than only the UI layout.

The Contract Catalogue should remain focused on contract state and should not duplicate the live telemetry panel.

---

# 3. Directed Power

All levels use a **70 km maximum altitude** and require a Kerbin surface impact.

| Level | Required surface speed |
| --- | ---: |
| I | 600 m/s |
| II | 1,100 m/s |
| III | 1,400 m/s |
| IV | 1,700 m/s |
| V | 2,000 m/s |

For each level being tested:

1. Reach the required speed without exceeding 70,000 m.
2. Confirm Current Speed and Max Speed update in Funding Targets.
3. Confirm normal landing or recovery does **not** complete the objective.
4. Impact Kerbin with a qualifying vessel and confirm completion.
5. Repeat against normal terrain, not only sea level.
6. Exceed 70,000 m first, descend, then impact at sufficient speed. Confirm the attempt remains invalid.
7. Enter orbit, return, and impact. Confirm the attempt remains invalid.
8. Stage during flight and confirm the continuing controlled stage keeps the same attempt history.
9. Save after violating the altitude ceiling, reload, then impact. Confirm the violation survives save/load.
10. Destroy a vessel well above the surface and confirm that does not count as the required surface impact.
11. Confirm low-speed deletion/recovery near the ground does not create a false completion.
12. If practical, repeat a qualifying impact at 2x and 4x physics warp.
13. Inspect `KSP.log` and confirm there are no repeated destruction-callback exceptions.

## Multiple offered Directed Power levels

Create a state where Directed Power I and II are both offered and unfinished.

Reach at least 1,100 m/s, remain below 70 km, and impact Kerbin.

Confirm:

- Directed Power I completes;
- Directed Power II completes;
- a higher level that is only `Locked` or `Unlocked` does not complete.

---

# 4. Mass

| Level | Required final mass | Required distance |
| --- | ---: | ---: |
| I | 1 t | 25 km |
| II | 2.5 t | 75 km |
| III | 5 t | 150 km |
| IV | 10 t | 300 km |
| V | 20 t | 600 km |

For each level:

1. Confirm the Funding Targets card shows current mass, distance, and landed state.
2. Fly beyond the required distance with enough mass. Confirm no completion while still flying.
3. Land beyond the required distance with too little final mass. Confirm no completion.
4. Land with enough mass but short of the required distance. Confirm no completion.
5. Land on Kerbin beyond the required distance with enough final mass. Confirm completion.
6. Confirm `SPLASHED` does not count as `LANDED`.
7. Enter orbit first and confirm the Pre-Orbit attempt is invalid.

Distance is measured from the tracked launch origin. A KSC launch therefore behaves like distance from the Space Centre, while an alternate launch site uses that alternate origin.

## Multiple offered Mass levels

Create a state where Mass I and II are both offered.

Land one craft at least 75 km from launch with at least 2.5 t remaining.

Confirm both complete. A higher Mass level that is not offered must remain incomplete even if the craft also satisfies its numbers.

---

# 5. Control

| Level | Altitude band | Required continuous hold |
| --- | --- | ---: |
| I | 2-5 km | 30 s |
| II | 8-12 km | 45 s |
| III | 15-25 km | 60 s |
| IV | 30-40 km | 75 s |
| V | 50-65 km | 90 s |

Every Control objective requires crew and a safe Kerbin landing after qualification.

For each level:

1. Launch with at least one Kerbal.
2. Enter the required altitude band.
3. Confirm hold time increases only while the vessel remains continuously in band with crew.
4. Leave the band before qualification and confirm the timer resets.
5. Lose/remove all crew before qualification and confirm the hold cannot qualify.
6. Complete the required hold and confirm the card changes to a qualified/landing-ready state.
7. Land on Kerbin with crew and confirm completion.
8. Splash down instead and confirm no completion.
9. Enter orbit before landing and confirm the Pre-Orbit attempt is invalid.

## Save/load Control state

Test both cases:

- save partway through a valid hold, reload, then continue;
- save after qualification but before landing, reload, then land.

Confirm state resumes correctly.

If the vessel is not observed for a long interval before qualification, that missing time must not be credited as continuous hold time.

## Multiple offered Control levels

Create a state where Control I and II are both offered.

1. Qualify Control I at 2-5 km.
2. Move to the Control II band.
3. Begin the Control II hold.
4. Confirm Control I remains qualified while Control II shows separate progress.
5. Save and reload during Control II progress.
6. Finish Control II.
7. Land safely with crew.

Confirm both objectives complete on that same landing.

---

# 6. Biome

| Level | Target biome |
| --- | --- |
| I | Grasslands |
| II | Highlands |
| III | Mountains |
| IV | Deserts |
| V | Ice Caps |

For each level:

1. Confirm Funding Targets displays the stock Kerbin biome and the contract target.
2. Fly over the target biome. Confirm no completion.
3. Land in the target biome and confirm completion only after KSP reports `LANDED`.
4. Confirm `SPLASHED` does not count.
5. Confirm no Biome completion occurs away from Kerbin.
6. Confirm biome reporting remains correct at low altitude and after touchdown.

## Multiple offered Biome levels

Create a state where Biome I and II are both offered.

Land in Grasslands, continue the same launch attempt, then later land in Highlands.

Confirm each offered objective can complete independently. A higher level that is not offered must not complete.

---

# 7. Cross-line behaviour

Use one launch that can satisfy more than one active contract.

Examples:

- a heavy craft lands at the required Mass distance inside the required Biome;
- more than one offered threshold in the same line is satisfied;
- more than one Control hold is qualified before one final landing.

Confirm:

- every offered, unfinished active contract is evaluated independently;
- one valid landing or impact may complete several offered contracts;
- `Locked` or merely `Unlocked` contracts are not evaluated as active Flight Contracts;
- staging does not create a new attempt when the continuing vessel belongs to the same launch;
- switching to an unrelated vessel does not inherit old maxima, origin, or Control timers.

---

# 8. Telemetry gating and idle behaviour

The fast Flight Contract path should only collect data needed by active contracts.

Test, if practical:

- all four opening lines active;
- Mass only;
- Control only;
- Biome only;
- Directed Power only;
- zero offered unfinished Pre-Orbit contracts.

Confirm the remaining active contract still works in each reduced state.

When no active Flight Contracts exist, confirm there are no unexpected completions, impact callback errors, or repeated tracking exceptions.

After a later sponsor review offers a new Pre-Orbit contract, live telemetry should resume without restarting the save or controller.

---

# 9. Probe Orbit convergence

Test each route independently where practical:

- Directed Power V -> Probe Orbit
- Mass V -> Probe Orbit
- Control V -> Probe Orbit
- Biome V -> Probe Orbit

Confirm:

1. completing any one Level V offers Probe Orbit immediately;
2. the offer does not wait for the next sponsor review;
3. a rival Level V completion can also trigger the campaign-wide offer;
4. Probe Orbit remains a normal orbital objective;
5. a qualifying uncrewed Probe or Relay can complete Probe Orbit through `OrbitalVesselTracker`;
6. completing a Pre-Orbit objective by a rival does not invent a satellite.

---

# 10. Funding and rivals

## Pre-Orbit reward values

| Level | Base reward |
| --- | ---: |
| I | 10,000 |
| II | 20,000 |
| III | 30,000 |
| IV | 40,000 |
| V | 50,000 |

After first completion of an objective funding contract, confirm the normal payment sequence is:

```text
100%, 90%, 80%, 70%, 60%, 50%, 40%, 30%, 20%, 10%
```

Confirm later qualifying agencies share later payments according to the normal funding rules and receive no retroactive share of earlier payments.

## Rival Pre-Orbit progress

A successful rival Pre-Orbit progress check advances by 20%:

```text
0 -> 20 -> 40 -> 60 -> 80 -> 100
```

| Level | Cost per successful 20% step | Full development cost |
| --- | ---: | ---: |
| I | 4,000 | 20,000 |
| II | 6,000 | 30,000 |
| III | 8,000 | 40,000 |
| IV | 10,000 | 50,000 |
| V | 12,000 | 60,000 |

Confirm normal orbital and satellite-network rival missions still use 10% progress steps.

Confirm the Rival Agencies ETA reflects five successful Pre-Orbit steps from 0%, not ten.

---

# 11. Persistence

Current top-level ScenarioModule sections are:

```text
CAMPAIGN_FUNDING
RIVAL_AGENCIES
FLIGHT_CONTRACT_PROGRESS
```

Command Center visibility is stored separately.

## `CAMPAIGN_FUNDING`

Confirm current saves use:

```text
PLAYER_OBJECTIVE_COMPLETION
OBJECTIVE_FUNDING_CONTRACT
SATELLITE_NETWORK_FUNDING_CONTRACT
```

Each entry is identified by a stable `id` where applicable.

Confirm locked/unoffered funding contracts are represented explicitly rather than disappearing from saved campaign state.

## `RIVAL_AGENCIES`

Confirm rivals are stored as repeated:

```text
RIVAL
```

entries keyed by stable agency identity.

## `FLIGHT_CONTRACT_PROGRESS`

Confirm it stores only temporary active-flight evaluation state, including:

- active attempt identity;
- launch time and origin;
- maximum Directed Power altitude and speed history;
- orbit invalidation;
- repeated `CONTROL_STATE` children.

Each `CONTROL_STATE` should contain:

```text
objectiveId
holdSeconds
wasSampleInBand
qualified
```

Instantaneous telemetry such as current altitude, current mass, current biome, and current crew should be rebuilt from the next live sample after load.

## Save/reload checks

1. Save with no active attempt and confirm reload does not invent one.
2. Save during a Directed Power attempt after exceeding 70 km; reload and confirm the invalidation remains.
3. Save during a Control hold; reload and confirm valid saved progress resumes.
4. Save after Control qualification but before landing; reload and confirm landing can still complete it.
5. Move Flight -> Space Center -> Tracking Station -> Flight and confirm campaign state remains consistent.
6. Load a different KSP save in the same process and confirm campaign, rival, tracking, and callback state does not leak across saves.
7. Corrupt a current-format `CONTROL_STATE` in a disposable save and confirm malformed progress fails closed rather than inventing valid progress.

---

# 12. Orbital vessel tracking

Use Probe/Relay and crewed vessels around Kerbin. Repeat around Mun, Minmus, or another supported body if relevant to the change being tested.

Confirm:

1. a newly orbiting loaded vessel is recognised;
2. after leaving Flight, the unloaded/persistent vessel remains counted;
3. Probe and Relay vessel types both count as qualifying satellites where appropriate;
4. a crewed vessel can satisfy a crewed orbital objective;
5. a crewed Probe can still count toward satellite-network presence while not satisfying an uncrewed Probe Orbit requirement by itself;
6. scene changes and save/reload do not lose valid orbital vessel state.

This verifies the boundary between `KspVesselMonitor` and `OrbitalVesselTracker`.

---

# 13. Runtime ownership

Hide the Command Center with F8 and continue playing or time-warping.

Confirm:

- rival progress continues;
- sponsor/funding timing continues;
- active Flight Contract tracking continues during Flight;
- reopening the UI shows current state rather than restarting progression;
- moving between normal KSP scenes does not recreate campaign state for the same save.

This verifies that `ModRuntime` owns progression and `CommandCenterWindow` is presentation-only.

---

# 14. Log check

After a test session:

```bash
grep -i "Race for Space\|TheRaceForSpace\|Exception" "$KSP_ROOT/KSP.log" | tail -n 100
```

Look for:

- repeated exceptions;
- vessel-destruction callback errors;
- save/load errors;
- excessive per-frame logging.

---

# Release-candidate sign-off

Before calling a 0.5 build ready for broader testing, confirm all of the following:

- [ ] Real KSP build succeeds.
- [ ] Automated logic suites pass.
- [ ] Four opening Pre-Orbit offers are correct.
- [ ] Live telemetry displays correctly.
- [ ] Directed Power impact behaviour works.
- [ ] Mass landing behaviour works.
- [ ] Control hold + safe landing works.
- [ ] Biome landing behaviour works.
- [ ] Multiple simultaneously offered contracts evaluate independently.
- [ ] Any Agency progression works.
- [ ] Sponsor reviews offer all unlocked Pre-Orbit contracts.
- [ ] Any Level V offers Probe Orbit immediately.
- [ ] Objective funding and rival progress values are correct.
- [ ] Save/reload and scene changes preserve the correct state.
- [ ] Loaded and unloaded orbital vessels are handled correctly.
- [ ] No repeated KSP log errors are present.

# Version 0.5 - Live KSP Acceptance Checklist

This checklist covers behaviour that the standalone automated tests cannot fully prove without a real Kerbal Space Program 1.12.x installation.

Run it against:

```text
Alpha/KerbalContracts-v0.5
```

Use a disposable Career save where possible. The current `FLIGHT_CONTRACT_PROGRESS` format intentionally does not migrate the earlier development single-attempt layout, so use a save created or rewritten by the current build for Flight Attempt persistence checks.

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
6. Launch a vessel and open the Flight-only **Offered Contracts** window.
7. Confirm live Flight Contract telemetry appears for every offered, unfinished Pre-Orbit contract you expand.
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

During flight, the Flight-only **Offered Contracts** window should show live values about once per second for expanded Pre-Orbit contracts.

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
- landed/splashed state

## Control card

Expect:

- current altitude and target altitude band
- continuous hold progress
- crew count
- safe-recovery readiness after qualification

Each offered Control contract must display its own independent hold/qualification state.

## Biome card

Expect:

- current biome
- target biome
- biome match state
- landed/splashed state

## Telemetry failure messages

While in Flight with a normal active vessel, the UI should not remain stuck on:

```text
Waiting for vessel telemetry...
```

If that message appears continuously, investigate the runtime/tracking path rather than only the UI layout.

Funding Targets and Contract Catalogue should remain focused on funding/contract state and should not duplicate the live telemetry panel.

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
2. Confirm Current Speed and Max Speed update in **Offered Contracts**.
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

1. Confirm **Offered Contracts** shows current mass, distance, and landed/splashed state.
2. Fly beyond the required distance with enough mass. Confirm no completion while still flying.
3. Finish landed or splashed beyond the required distance with too little final mass. Confirm no completion.
4. Finish landed or splashed with enough mass but short of the required distance. Confirm no completion.
5. Land on Kerbin beyond the required distance with enough final mass and no outside-lineage parts attached. Confirm completion.
6. Splash down on Kerbin beyond the required distance with enough final mass and no outside-lineage parts attached. Confirm completion.
7. Enter orbit first and confirm the Pre-Orbit attempt is invalid.

Distance is measured from the tracked launch origin. A KSC launch therefore behaves like distance from the Space Centre, while an alternate launch site uses that alternate origin.

## Docked Mass anti-combination

Use Craft A and unrelated Craft B after both have already been sampled as separate Flight Attempts.

1. Make Craft A travel beyond the active Mass distance while retaining enough mass on its own.
2. Dock B to A and keep A selected using **Control From Here** on an A part.
3. Land or splash down the combined vessel while B's persistent parts are still attached.
4. Confirm the Mass contract does **not** complete even though the combined KSP vessel mass is high enough.
5. Detach B so A is again made only from A's remembered lineage.
6. With A still satisfying the normal mass, distance, and landed/splashed requirements, confirm Mass can now complete.

The intended rule is conservative: the tracker uses stock vessel mass only when no persistent parts from outside the selected attempt lineage are attached. It does not maintain a second approximate per-part mass calculation.

## Multiple offered Mass levels

Create a state where Mass I and II are both offered.

Land or splash down one single-lineage craft at least 75 km from launch with at least 2.5 t remaining.

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

Every Control objective requires crew and a safe Kerbin landing or splashdown after qualification.

For each level:

1. Launch with at least one Kerbal.
2. Enter the required altitude band.
3. Confirm hold time increases only while the vessel remains continuously in band with crew.
4. Leave the band before qualification and confirm the timer resets.
5. Lose/remove all crew before qualification and confirm the hold cannot qualify.
6. Complete the required hold and confirm the card changes to a qualified/recovery-ready state.
7. Land on Kerbin with crew and confirm completion.
8. Splash down on Kerbin with crew and confirm completion.
9. Enter orbit before recovery and confirm the Pre-Orbit attempt is invalid.

## Docking and undocking during Control

Use Craft A with an active Control objective and another separately remembered craft B.

1. Begin an unqualified Control hold on A and accumulate several seconds.
2. Dock B to A while A remains the selected lineage.
3. Confirm A's unfinished Control hold resets when B's external persistent parts appear.
4. Keep the docked topology unchanged and confirm a new continuous hold can begin normally.
5. Undock B before that new hold qualifies.
6. Confirm the unfinished hold resets again when B's external parts disappear.
7. Complete a fresh uninterrupted hold until Control is qualified.
8. Dock or undock another lineage again.
9. Confirm the already-qualified Control state remains qualified and can still finish with the normal crewed Kerbin landing or splashdown.

Ordinary staging of the selected attempt's own continuing lineage should not count as this external attachment change and should not reset a Control hold solely because own-lineage parts were staged away.

## Save/load Control state

Test both cases:

- save partway through a valid hold, reload, then continue;
- save after qualification but before recovery, reload, then land or splash down.

Confirm state resumes correctly.

The transient external-attachment set is rebuilt from the first usable live snapshot after load. That first observation establishes the current topology baseline and should not itself reset valid saved partial Control progress; a later real docking or undocking change should reset an unfinished hold normally.

If the vessel is not observed for a long interval before qualification, that missing time must not be credited as continuous hold time.

## Multiple offered Control levels

Create a state where Control I and II are both offered.

1. Qualify Control I at 2-5 km.
2. Move to the Control II band.
3. Begin the Control II hold.
4. Confirm Control I remains qualified while Control II shows separate progress.
5. Save and reload during Control II progress.
6. Finish Control II.
7. Land or splash down safely with crew.

Confirm both objectives complete on that same recovery.

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

1. Confirm **Offered Contracts** displays the stock Kerbin biome and the contract target.
2. Fly over the target biome. Confirm no completion.
3. Land in the target biome and confirm completion after KSP reports `LANDED`.
4. Where KSP can report the same target biome while `SPLASHED`, splash down there and confirm completion as well.
5. Confirm no Biome completion occurs away from Kerbin.
6. Confirm biome reporting remains correct at low altitude and after touchdown/splashdown.

## Multiple offered Biome levels

Create a state where Biome I and II are both offered.

Finish landed or splashed in Grasslands, continue the same launch attempt, then later finish landed or splashed in Highlands where practical.

Confirm each offered objective can complete independently. A higher level that is not offered must not complete.

---

# 7. Cross-line and Flight Attempt behaviour

Use one launch that can satisfy more than one active contract.

Examples:

- a heavy craft finishes landed or splashed at the required Mass distance inside the required Biome;
- more than one offered threshold in the same line is satisfied;
- more than one Control hold is qualified before one final recovery.

Confirm:

- every offered, unfinished active contract is evaluated independently;
- one valid landed/splashed recovery or impact may complete several offered contracts;
- `Locked` or merely `Unlocked` contracts are not evaluated as active Flight Contracts;
- staging does not create a new attempt when the continuing branch retains the remembered lineage;
- a detached same-launch branch does not clone the continuing branch's historical progress;
- switching from Craft A to unrelated Craft B does not transfer A's maxima, origin, or Control state to B;
- returning to Craft A restores A's previous remembered history;
- docking A and B keeps both histories separate;
- KSP's **Control From Here** can select the remembered lineage belonging to the new reference/control part;
- Mass cannot use a second attached lineage to satisfy its final mass requirement;
- docking or undocking an external lineage resets an unfinished Control hold but preserves qualified Control state;
- after undocking, each branch recovers its own history from its surviving persistent parts.

## Flight Attempt lifecycle pruning

Use two remembered craft with clearly distinguishable histories.

1. Leave Craft A intact somewhere in the current save. It may be parked or unloaded and does not need to remain active.
2. Recover, terminate, or fully destroy Craft B so none of B's remembered persistent parts remain in any KSP vessel.
3. Allow the normal broad loaded/unloaded vessel refresh to succeed. It normally runs about every 20 seconds.
4. Confirm A's remembered history remains available when A is selected again.
5. Save after the cleanup refresh and inspect a disposable save if practical: A's `ATTEMPT` should remain in `FLIGHT_CONTRACT_PROGRESS`, while B's obsolete `ATTEMPT` should be absent.
6. Leave A inactive across additional broad refreshes and confirm inactivity alone does **not** prune it.
7. Dock and undock living remembered craft and confirm their histories are not pruned while at least one persistent part from each lineage still exists.
8. Check `KSP.log`; a genuine cleanup may emit one line such as:

   ```text
   [TheRaceForSpace] Pruned 1 obsolete Flight Attempt(s) after vessel population refresh.
   ```

The intended rule has no timeout. An attempt is pruned only when a successful broad refresh proves that **none** of its remembered lineage parts exist. Lineage-less fallback attempts are retained conservatively because part-population absence cannot prove they are gone.

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

Mass anti-combination and Control topology detection must reuse the persistent IDs already captured by the normal active-vessel snapshot; they should not introduce another vessel scan or faster telemetry cadence. Lifecycle pruning must likewise reuse the existing slower broad loaded/unloaded vessel refresh rather than add another global scan.

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

Confirm the current format contains repeated:

```text
ATTEMPT
```

nodes rather than one active attempt stored directly on the root.

Each `ATTEMPT` should contain:

```text
selected
vesselId
body
launchUniversalTime
startLatitude
startLongitude
lastSampleUniversalTime
maximumAltitudeMeters
maximumSurfaceSpeedMetersPerSecond
enteredOrbit
```

Each remembered persistent part should appear as a nested:

```text
PART_LINEAGE
{
    partPersistentId = ...
}
```

and each saved Control objective should remain nested under its owning attempt as:

```text
CONTROL_STATE
{
    objectiveId = ...
    holdSeconds = ...
    wasSampleInBand = ...
    qualified = ...
}
```

Confirm:

- every remembered attempt whose lineage still exists is written, not only the currently selected craft;
- a pruned dead/recovered attempt is no longer written after the lifecycle refresh;
- at most one `ATTEMPT` is marked `selected = true`;
- two histories that were docked may legitimately contain the same last `vesselId` without being merged;
- their `PART_LINEAGE` IDs remain independent;
- the old root-level `active`, `vesselId`, and root-level `CONTROL_STATE` layout is not written by the current build.

Instantaneous telemetry such as current altitude, current mass, current biome, current crew, current reference part, and the transient external-attachment topology should be rebuilt from the next live sample after load.

## Multi-attempt save/reload check

Use two controllable craft with clearly different Directed Power maximum speeds because that history is easy to see in FlightActiveUI:

1. Fly Craft A and record a recognizable maximum speed.
2. Switch to Craft B and record a different maximum speed.
3. Return to A and confirm its old maximum is still present.
4. Optionally dock A and B, then use **Control From Here** on B so both lineages share one KSP vessel while B is selected.
5. Save the game while B is selected.
6. Reload the save and confirm B's saved maximum is restored immediately.
7. Switch to A, or undock and switch to A if the save was made docked.
8. Confirm A's previous independent maximum is restored by its saved persistent-part lineage rather than starting at the post-load live value.
9. Switch back to B and confirm B retains its own history.
10. If practical, save again after undocking, reload, and reconfirm both histories.

The key result is **A -> B -> save while B -> reload -> A still remembers A**.

## Other save/reload checks

1. Save with no remembered attempt and confirm reload does not invent one.
2. Save during a Directed Power attempt after exceeding 70 km; reload and confirm the invalidation remains.
3. Save during a Control hold; reload and confirm valid saved progress resumes.
4. After reloading a partial Control hold, confirm the first live topology observation does not by itself reset that progress; then perform a real docking/undocking change and confirm the unfinished hold does reset.
5. Save after Control qualification but before recovery; reload and confirm either a landing or splashdown can still complete it.
6. Recover or terminate a remembered craft, allow a successful broad vessel refresh, save, reload, and confirm the pruned attempt does not return.
7. Move Flight -> Space Center -> Tracking Station -> Flight and confirm campaign state remains consistent.
8. Load a different KSP save in the same process and confirm campaign, rival, tracking, pruning, and callback state does not leak across saves.
9. Corrupt a current-format `CONTROL_STATE` inside one `ATTEMPT` in a disposable save and confirm malformed Flight Contract progress fails closed rather than inventing valid progress.
10. If practical, duplicate one `partPersistentId` across two `ATTEMPT` nodes in a disposable save and confirm the ambiguous Flight Contract progress is rejected rather than merging histories.

The current format intentionally does not migrate the earlier development single-attempt root layout.

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

This verifies the boundary between `KspVesselMonitor` and `OrbitalVesselTracker`. The same successful broad vessel refresh now also copies persistent IDs from loaded live `Part` objects and unloaded `ProtoPartSnapshot` objects for Flight Attempt lifecycle pruning.

---

# 13. Runtime ownership

Hide the Command Center with F8 and continue playing or time-warping.

Confirm:

- rival progress continues;
- sponsor/funding timing continues;
- active Flight Contract tracking continues during Flight;
- lifecycle pruning can still occur after the normal broad vessel refresh even while the Flight UI is hidden;
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
- Flight Attempt save/load errors or histories unexpectedly resetting after reload;
- repeated pruning of the same already-removed attempt;
- excessive per-frame logging.

A real dead/recovered lineage may produce one `Pruned <n> obsolete Flight Attempt(s) after vessel population refresh.` message. It should not repeat on later refreshes once that attempt has been removed.

---

# Release-candidate sign-off

Before calling a 0.5 build ready for broader testing, confirm all of the following:

- [ ] Real KSP build succeeds.
- [ ] Automated logic suites pass.
- [ ] Four opening Pre-Orbit offers are correct.
- [ ] Live telemetry displays correctly in FlightActiveUI.
- [ ] Directed Power impact behaviour works.
- [ ] Mass landed/splashed recovery and docked-lineage rejection work.
- [ ] Control hold, topology resets, qualification preservation, and safe landing/splashdown work.
- [ ] Biome landed/splashed completion behaviour works where the target biome is reported.
- [ ] Multiple simultaneously offered contracts evaluate independently.
- [ ] Staging, vessel switching, docking, and undocking preserve independent Flight Attempt histories.
- [ ] Unrelated docked parts cannot be combined to satisfy Mass.
- [ ] Docking/undocking resets unfinished Control holds but does not erase qualified Control state.
- [ ] Multiple Flight Attempts and their part lineages survive save/reload.
- [ ] Dead/recovered Flight Attempts are pruned only after their lineage disappears, while inactive surviving craft are retained.
- [ ] Pruned Flight Attempts do not reappear in later saves.
- [ ] Any Agency progression works.
- [ ] Sponsor reviews offer all unlocked Pre-Orbit contracts.
- [ ] Any Level V offers Probe Orbit immediately.
- [ ] Objective funding and rival progress values are correct.
- [ ] Save/reload and scene changes preserve the correct state.
- [ ] Loaded and unloaded orbital vessels are handled correctly.
- [ ] No repeated KSP log errors are present.
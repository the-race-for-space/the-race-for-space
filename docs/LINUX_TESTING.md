# Linux / Steam Deck Testing and Deployment

This guide is for testing and deploying **The Race for Space 0.5.0** from Konsole on the Linux / Steam Deck development machine.

The current development branch is:

```text
Alpha/KerbalContracts-v0.5
```

For the complete in-game acceptance pass for the four pre-orbit contract lines, Probe Orbit convergence, funding, rivals, persistence, and UI behavior, also use [`docs/KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md).

## Normal command sequence

For most test sessions, these are the commands you need:

```bash
cd /home/deck/Projects/the-race-for-space/
export KSP_ROOT="/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program"

git fetch origin
git switch Alpha/KerbalContracts-v0.5
git pull --ff-only

bash tools/run-logic-tests.sh
bash tools/test-prototype.sh Alpha/KerbalContracts-v0.5
```

Local paths used by this project:

```text
Repository: /home/deck/Projects/the-race-for-space/
KSP root:   /home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program
GameData:   /home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program/GameData/
```

> `KSP_ROOT` must point to the KSP installation folder, not directly to `GameData`.

---

# Part 1 - Deploy and Test When Setup Is Already Complete

Use this section for the normal development/test cycle.

## 1. Open the project

Open Konsole and run:

```bash
cd /home/deck/Projects/the-race-for-space/
export KSP_ROOT="/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program"
```

## 2. Update the 0.5 branch

```bash
git fetch origin
git switch Alpha/KerbalContracts-v0.5
git pull --ff-only
```

Check that the repository is clean:

```bash
git status --short
```

No output means there are no uncommitted changes.

Confirm the branch before testing:

```bash
git branch --show-current
```

Expected output:

```text
Alpha/KerbalContracts-v0.5
```

## 3. Run the automated tests

```bash
bash tools/run-logic-tests.sh
```

A successful 0.5 run should include:

```text
PASS: Starter flight contracts and persistence
All prototype logic tests passed.
All controller and unlock rule regression tests passed.
```

The automated suites cover the KSP-independent starter-contract rules, including landed-only Mass and Biome completion, persistence, four Level I starter offers plus sixteen initially locked successors, Any Agency rival/player line unlocking, all unlocked pre-orbit contracts being offered together at the next funding review without consuming the normal two achievement slots, rival participation, the separate two-offer satellite cap, and the four-way Level V to Probe Orbit unlock rule.

The live KSP API paths still require the in-game checks below. In particular, automated tests cannot prove the active-vessel destruction callbacks/global vessel-will-destroy fallback, stock biome reporting, actual Command Center layout, or loaded/unloaded vessel discovery inside KSP.

Do not deploy a build if the automated tests fail.

## 4. Build and deploy to KSP

```bash
bash tools/test-prototype.sh Alpha/KerbalContracts-v0.5
```

The helper fetches and fast-forwards the requested branch, builds against the local KSP assemblies, and deploys both the assembly and the current race settings config.

Successful output should end with something similar to:

```text
Prototype ready to test.
Branch: Alpha/KerbalContracts-v0.5
```

The deployed files are:

```text
/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll
/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program/GameData/TheRaceForSpace/Config/CampaignSettings.cfg
```

Confirm both exist with:

```bash
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll"
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Config/CampaignSettings.cfg"
```

The test helper performs these checks automatically and stops if either deployed file is missing.

Because Directed Power now uses KSP destruction events in addition to the vessel-local callback, this local build is an important compile gate: do not proceed to gameplay testing if `GameEvents.onVesselWillDestroy` or its callback signature fails against the installed KSP 1.12.x assemblies.

## 5. Start KSP and perform the 0.5 smoke test

1. Start KSP 1.12.x through Steam.
2. Load a disposable Career test save. A fresh save is preferred when verifying opening offers.
3. Confirm the Race for Space Command Center opens from the stock launcher button and with `F8`.
4. Confirm `F8` hides and reopens the same window without resetting race state.
5. Open **Contract Catalogue** and confirm there is no separate `STARTER PROGRAMMES` four-card panel above the normal catalogue.
6. Expand **Offered** and confirm **Directed Power I**, **Mass I**, **Control I**, and **Biome I** are present.
7. Expand **Locked** and confirm Levels II-V of all four starter lines are present: sixteen locked pre-orbit contracts in total.
8. Open several pre-orbit contracts and confirm their objective, payout, state, and unlock requirements use the same contract details view as the existing achievement targets.
9. Complete one Level I pre-orbit contract and confirm its Level II successor moves to **Unlocked**, not immediately to **Offered**.
10. Before one funding boundary, unlock starter successors in several lines. Cross the funding boundary and confirm **every unlocked pre-orbit contract becomes Offered**, even when more than two are unlocked. Starter offers must not consume the normal two unfinished achievement slots.
11. When Probe-or-later normal achievement candidates are available, confirm no more than two unfinished normal achievement offers are active from that pool, independently of the starter offers. Confirm satellite programmes still have their own separate two-unfulfilled-offer limit.
12. Open **Funding Targets** during an active starter flight and confirm every offered unfinished starter funding card shows its own live flight values approximately once per second.
13. Confirm **Overview** treats offered pre-orbit contracts like the other offered one-off achievements.
14. Save and reload once before ending the smoke test and confirm the race state and Command Center visibility remain consistent.

For a release candidate, continue with the full checklist in [`KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md).

### 0.5 starter-flight runtime verification

Use these checks after changes to `Core/ModRuntime`, `Tracking/FlightContractTracker`, `KspIntegration/KspVesselMonitor`, starter persistence, or the Funding Targets UI.

1. Launch a vessel on Kerbin with one of the currently offered pre-orbit contracts and keep **Funding Targets** open on its funding card.
2. Confirm live values update approximately once per second rather than every frame:
   - Directed Power: maximum surface speed and maximum altitude;
   - Mass: current remaining mass and distance from launch;
   - Control: current altitude, continuous hold time, and crew count;
   - Biome: current stock Kerbin biome.
3. Confirm the Contract Catalogue catalogue does not show a second live-flight panel; it should remain focused on Offered/Unlocked/Locked/Expired state.
4. Stage once during flight and confirm the continuing controlled stage retains the same launch history rather than starting a fresh attempt.
5. Enter orbit during a starter attempt and confirm the attempt is treated as invalid for starter completion.
6. Switch to an unrelated vessel or launch a new vehicle and confirm old maxima, distance, and Control hold state are not inherited.
7. Hide the Command Center with `F8`, continue flying for several seconds, reopen it, and confirm gameplay tracking continued while the UI was hidden.

These checks verify that `ModRuntime` owns the one-second starter observation cadence and that `CommandCenterWindow` only reads tracker state for presentation.

### 0.5 quick contract checks

These are short smoke tests. The full thresholds and edge cases are in the dedicated 0.5 acceptance checklist.

#### Directed Power

1. Reach the current required speed while staying at or below 70 km.
2. Confirm landing or recovering normally does **not** complete the contract.
3. Crash the qualifying craft into Kerbin and confirm the contract completes even if KSP momentarily changes the dying vessel to `LANDED` or zero surface speed during breakup.
4. Repeat against ordinary terrain, not just sea level, to confirm the surface-impact fallback works with terrain clearance.
5. Exceed 70 km first, then descend and impact, and confirm the attempt stays invalid.
6. Destroy a moving vessel well above the surface and confirm that does not falsely complete Directed Power.

#### Mass

1. Confirm the offered Mass card in Funding Targets shows both current remaining mass and distance from the tracked launch origin.
2. Fly beyond the required distance with enough mass and confirm the contract does **not** complete while still flying.
3. Land beyond the required distance with too little final vessel mass and confirm no completion.
4. Land short of the required distance with enough mass and confirm no completion.
5. Land beyond the required distance with the finished craft still at or above the required mass and confirm completion.
6. Confirm a splashdown does not count as the required landing.
7. Continue the same launch and confirm the same line cannot advance twice during one launch attempt.

#### Control

1. Fly a crewed vessel into the current required altitude band.
2. Confirm the offered Control card in Funding Targets shows hold time increasing only while continuously inside the band with crew aboard.
3. Leave the band before qualification and confirm the timer resets.
4. After qualification, land safely on Kerbin with crew aboard and confirm completion.
5. If there is a long unobserved gap before qualification, such as leaving Flight and later returning, confirm the missing interval is not credited as continuous hold time.

#### Biome

1. Confirm the offered Biome card in Funding Targets displays the active vessel's stock Kerbin biome and the current target biome.
2. Fly over the target biome and confirm the contract does **not** complete.
3. Land in the target biome and confirm the level completes only after KSP reports `LANDED`.
4. Confirm splashdown does not count as a Biome landing.
5. Continue to another target biome in the same launch attempt and confirm the Biome line does not advance twice.

### 0.5 persistence verification

Use these checks after changes to starter save/load behavior:

1. Save with no active starter flight, reload, and confirm no attempt is invented.
2. Save during a Directed Power attempt after exceeding its altitude ceiling, reload, then impact and confirm the disqualification survives.
3. Save partway through a valid Control hold, reload, continue the hold and confirm the saved accumulated time resumes correctly.
4. Save after a Control hold has qualified but before landing, reload, land safely, and confirm completion still works.
5. Confirm instantaneous values such as current altitude, mass, distance, crew count, and biome are rebuilt from the next live vessel observation instead of displaying stale saved telemetry.
6. Move Flight -> Space Center -> Tracking Station -> Flight and confirm starter/race persistence remains consistent.

### 0.5 Probe Orbit convergence verification

At least one end-to-end route should be completed before treating the build as a release candidate.

1. Complete Level V in one starter line.
2. Confirm **Probe Orbit** is offered immediately without waiting for the next funding review.
3. Confirm Probe Orbit appears under the normal **Offered** section in Contract Catalogue.
4. Launch a qualifying uncrewed Probe or Relay into stable Kerbin orbit and confirm the existing orbital tracker completes Probe Orbit.
5. If practical, repeat in disposable saves using the other starter Level V routes to confirm Directed Power V, Mass V, Control V, and Biome V each independently satisfy the OR gate.
6. If practical, let a rival complete a Level V pre-orbit contract first and confirm that also offers Probe Orbit to the whole race.

### 0.5 core runtime verification

Use these checks after changes to race lifecycle or runtime ownership:

1. Open the Command Center and note the current rival funds, mission progress, pre-orbit contract states, and next funding date.
2. Hide the Command Center with `F8` and continue playing or time-warping for more than one five-second runtime refresh.
3. Reopen the Command Center and confirm the race state is still current.
4. Move between normal KSP game scenes, for example Space Center and Flight or Tracking Station, and confirm rival funds/progress do not reset to new-game values.
5. When practical, cross one shared funding boundary and confirm it is processed once and the next funding date advances normally.
6. Save, return to the menu, reload the save, and confirm the persisted race state is restored.

These checks verify that `Core/ModRuntime` owns progression while `UI/CommandCenterWindow` only displays the current controller and starter-flight state.

### 0.5 vessel discovery verification

Use these checks after changes to vessel discovery or satellite tracking:

1. Put a Probe or Relay vessel into Kerbin orbit and, while it is the loaded active vessel, confirm the Kerbin satellite count updates within the twenty-second player vessel observation interval.
2. Reach orbit without saving or changing scenes first. Confirm the active vessel is recognised within the twenty-second observation interval; this verifies the live `Vessel` path rather than a stale `ProtoVessel` snapshot.
3. Return to the Space Center or Tracking Station so the vessel becomes unloaded, then confirm the same satellite is still counted. This verifies the persistent `ProtoVessel` path.
4. Repeat with a Relay vessel and confirm it counts as a satellite in the same way as a Probe vessel.
5. Put a crewed vessel into Kerbin orbit and confirm Crewed Orbit is recognised. If the craft is a crewed Probe, it should still count toward the satellite network but should not qualify as the uncrewed Probe Orbit achievement by itself.
6. If testing Mun, Minmus, or Duna, confirm an unloaded orbiting Probe or Relay remains counted after scene changes and save/reload.

These checks verify the boundary between `KspIntegration/KspVesselMonitor` and `Tracking/OrbitalVesselTracker` while preserving loaded and unloaded vessel behaviour.

### Logs

After a test session, check KSP's log for Race for Space exceptions or excessive repeated output:

```bash
grep -i "Race for Space\|TheRaceForSpace\|Exception" "$KSP_ROOT/KSP.log" | tail -n 100
```

A normal test flight should not generate repeated per-frame starter-contract logging.

---

# Part 2 - Set Up From Scratch

Use this section when setting up a new Linux / Steam Deck development environment or recreating the local project folder.

## 1. Check KSP is installed

The expected KSP installation is:

```text
/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program
```

Confirm the KSP assembly needed by the build exists:

```bash
test -f "/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program/KSP_Data/Managed/Assembly-CSharp.dll" && echo "KSP references found"
```

Expected output:

```text
KSP references found
```

## 2. Check the development tools

Confirm Git is available:

```bash
git --version
```

Confirm the .NET SDK is available:

```bash
dotnet --version
```

The standalone logic/controller tests use .NET 8. The KSP mod itself targets .NET Framework 4.7.2 and, on Linux, may also require compatible Mono 4.7.2 reference assemblies.

## 3. Create the Projects folder

```bash
mkdir -p /home/deck/Projects
cd /home/deck/Projects
```

## 4. Clone the repository

If the repository is not already present:

```bash
git clone https://github.com/the-race-for-space/the-race-for-space.git
cd /home/deck/Projects/the-race-for-space/
```

## 5. Switch to the current 0.5 development branch

For a fresh clone:

```bash
git fetch origin
git switch --track origin/Alpha/KerbalContracts-v0.5
```

If the local branch already exists, use:

```bash
git switch Alpha/KerbalContracts-v0.5
git pull --ff-only
```

Confirm it:

```bash
git branch --show-current
```

Expected output:

```text
Alpha/KerbalContracts-v0.5
```

## 6. Set KSP_ROOT

For the current Konsole session:

```bash
export KSP_ROOT="/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program"
```

Confirm it is correct:

```bash
echo "$KSP_ROOT"
test -f "$KSP_ROOT/KSP_Data/Managed/Assembly-CSharp.dll" && echo "KSP references found"
```

Optional: make `KSP_ROOT` available automatically in future Bash sessions:

```bash
echo 'export KSP_ROOT="/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program"' >> ~/.bashrc
source ~/.bashrc
```

The deploy helper also checks the standard Linux Steam locations if `KSP_ROOT` has not been set, but setting it explicitly keeps the test target unambiguous.

## 7. Run the tests

```bash
cd /home/deck/Projects/the-race-for-space/
bash tools/run-logic-tests.sh
```

Expected success markers include:

```text
PASS: Starter flight contracts and persistence
All prototype logic tests passed.
All controller and unlock rule regression tests passed.
```

## 8. Build and deploy the first 0.5 test build

```bash
bash tools/test-prototype.sh Alpha/KerbalContracts-v0.5
```

Verify both deployment files:

```bash
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll"
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Config/CampaignSettings.cfg"
```

After this succeeds, start KSP and perform the Part 1 smoke test. Future testing should normally use **Part 1** only.

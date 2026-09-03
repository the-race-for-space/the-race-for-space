# Linux / Steam Deck Testing and Deployment

This guide is for testing and deploying **The Race for Space 0.5.0** from Konsole on the Linux / Steam Deck development machine.

The current development branch is:

```text
Alpha/KerbalContracts-v0.5
```

For the complete in-game acceptance pass for the four starter contract lines, Probe Orbit convergence, funding, rivals, persistence, and UI behavior, also use [`docs/KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md).

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

The automated suites cover the KSP-independent starter-contract rules, persistence, four independent starter offers, immediate next-level offers, rival participation, and the four-way Level V to Probe Orbit unlock rule.

The live KSP API paths still require the in-game checks below. In particular, automated tests cannot prove the active-vessel destruction callback, stock biome reporting, actual Command Center layout, or loaded/unloaded vessel discovery inside KSP.

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
/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program/GameData/TheRaceForSpace/Config/RaceSettings.cfg
```

Confirm both exist with:

```bash
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll"
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Config/RaceSettings.cfg"
```

The test helper performs these checks automatically and stops if either deployed file is missing.

## 5. Start KSP and perform the 0.5 smoke test

1. Start KSP 1.12.x through Steam.
2. Load a disposable Career test save. A fresh save is preferred when verifying opening offers.
3. Confirm the Race for Space Command Center opens from the stock launcher button and with `F8`.
4. Confirm `F8` hides and reopens the same window without resetting race state.
5. Open **Space Race** and confirm the `STARTER PROGRAMMES` section appears above the normal funding catalogue.
6. Confirm the four cards are **Directed Power**, **Mass**, **Control**, and **Biome**.
7. On a fresh campaign, confirm each card shows `0 / 5`, its Level I objective, and a 10,000 100% payout.
8. Confirm the twenty starter milestones are not duplicated in the normal Space Race Offered/Unlocked/Locked/Expired catalogue.
9. Confirm **Funding Targets** still contains the offered starter contracts.
10. Confirm **Overview** shows only the current starter objective from each line rather than every completed starter contract that is still paying.
11. Save and reload once before ending the smoke test and confirm the race state and Command Center visibility remain consistent.

For a release candidate, continue with the full checklist in [`KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md).

### 0.5 starter-flight runtime verification

Use these checks after changes to `Core/RaceRuntime`, `Tracking/StarterFlightTracker`, `KspIntegration/KspVesselDiscovery`, starter persistence, or the Space Race UI.

1. Launch a vessel on Kerbin and keep the Space Race starter cards visible.
2. Confirm live values update approximately once per second rather than every frame:
   - Directed Power: maximum surface speed and maximum altitude;
   - Mass: current remaining mass and distance from launch;
   - Control: current altitude, continuous hold time, and crew count;
   - Biome: current stock Kerbin biome.
3. Stage once during flight and confirm the continuing controlled stage retains the same launch history rather than starting a fresh attempt.
4. Enter orbit during a starter attempt and confirm the attempt is treated as invalid for starter completion.
5. Switch to an unrelated vessel or launch a new vehicle and confirm old maxima, distance, and Control hold state are not inherited.
6. Hide the Command Center with `F8`, continue flying for several seconds, reopen it, and confirm gameplay tracking continued while the UI was hidden.

These checks verify that `RaceRuntime` owns the one-second starter observation cadence and that `RaceWindow` only reads tracker state for presentation.

### 0.5 quick contract checks

These are short smoke tests. The full thresholds and edge cases are in the dedicated 0.5 acceptance checklist.

#### Directed Power

1. Reach the current required speed while staying at or below 70 km.
2. Confirm landing or recovering normally does **not** complete the contract.
3. Complete it only with a qualifying Kerbin surface impact.
4. Exceed 70 km first, then descend and impact, and confirm the attempt stays invalid.

#### Mass

1. Confirm both current remaining mass and distance from the tracked launch origin are shown.
2. Meet only one requirement and confirm no completion.
3. Meet both requirements simultaneously and confirm the current level completes.
4. Continue the same launch far enough for the next level and confirm the same line cannot advance twice during one launch.

#### Control

1. Fly a crewed vessel into the current required altitude band.
2. Confirm hold time increases only while continuously inside the band with crew aboard.
3. Leave the band before qualification and confirm the timer resets.
4. After qualification, land safely on Kerbin with crew aboard and confirm completion.
5. If there is a long unobserved gap before qualification, such as leaving Flight and later returning, confirm the missing interval is not credited as continuous hold time.

#### Biome

1. Confirm the card displays the active vessel's stock Kerbin biome and the current target biome.
2. Enter the target biome without orbiting and confirm the level completes immediately.
3. Continue to another target biome in the same launch and confirm the Biome line does not advance twice during that launch.

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
3. Confirm the starter panel reports that Probe Orbit is unlocked.
4. Launch a qualifying uncrewed Probe or Relay into stable Kerbin orbit and confirm the existing orbital tracker completes Probe Orbit.
5. If practical, repeat in disposable saves using the other starter Level V routes to confirm Directed Power V, Mass V, Control V, and Biome V each independently satisfy the OR gate.

### 0.5 core runtime verification

Use these checks after changes to race lifecycle or runtime ownership:

1. Open the Command Center and note the current rival funds, mission progress, starter progress, and next funding date.
2. Hide the Command Center with `F8` and continue playing or time-warping for more than one five-second runtime refresh.
3. Reopen the Command Center and confirm the race state is still current.
4. Move between normal KSP game scenes, for example Space Center and Flight or Tracking Station, and confirm rival funds/progress do not reset to new-game values.
5. When practical, cross one shared funding boundary and confirm it is processed once and the next funding date advances normally.
6. Save, return to the menu, reload the save, and confirm the persisted race state is restored.

These checks verify that `Core/RaceRuntime` owns progression while `UI/RaceWindow` only displays the current controller and starter-flight state.

### 0.5 vessel discovery verification

Use these checks after changes to vessel discovery or satellite tracking:

1. Put a Probe or Relay vessel into Kerbin orbit and, while it is the loaded active vessel, confirm the Kerbin satellite count updates within the twenty-second player vessel observation interval.
2. Reach orbit without saving or changing scenes first. Confirm the active vessel is recognised within the twenty-second observation interval; this verifies the live `Vessel` path rather than a stale `ProtoVessel` snapshot.
3. Return to the Space Center or Tracking Station so the vessel becomes unloaded, then confirm the same satellite is still counted. This verifies the persistent `ProtoVessel` path.
4. Repeat with a Relay vessel and confirm it counts as a satellite in the same way as a Probe vessel.
5. Put a crewed vessel into Kerbin orbit and confirm Crewed Orbit is recognised. If the craft is a crewed Probe, it should still count toward the satellite network but should not qualify as the uncrewed Probe Orbit achievement by itself.
6. If testing Mun, Minmus, or Duna, confirm an unloaded orbiting Probe or Relay remains counted after scene changes and save/reload.

These checks verify the boundary between `KspIntegration/KspVesselDiscovery` and `Tracking/SatelliteTracker` while preserving loaded and unloaded vessel behaviour.

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
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Config/RaceSettings.cfg"
```

After this succeeds, start KSP and perform the Part 1 smoke test. Future testing should normally use **Part 1** only.

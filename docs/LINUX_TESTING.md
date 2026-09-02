# Linux / Steam Deck Testing and Deployment

This guide is for testing and deploying **The Race for Space** from Konsole on the Linux / Steam Deck development machine.

## Normal command sequence

For most test sessions, these are the commands you need:

```bash
cd /home/deck/Projects/the-race-for-space/
export KSP_ROOT="/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program"

git fetch origin
git switch Alpha/Cleanup-0.4
git pull --ff-only

bash tools/run-logic-tests.sh
bash tools/test-prototype.sh Alpha/Cleanup-0.4
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

## 2. Update the 0.4 branch

```bash
git fetch origin
git switch Alpha/Cleanup-0.4
git pull --ff-only
```

Check that the repository is clean:

```bash
git status --short
```

No output means there are no uncommitted changes.

## 3. Run the automated tests

```bash
bash tools/run-logic-tests.sh
```

A successful run should finish with:

```text
All prototype logic tests passed.
```

The logic suite includes KSP-independent vessel snapshot tracking tests. Live KSP vessel discovery still requires the in-game checks below.

Do not deploy a build if the automated tests fail.

## 4. Build and deploy to KSP

```bash
bash tools/test-prototype.sh Alpha/Cleanup-0.4
```

The deploy copies both the compiled assembly and the current user-editable race settings config to KSP:

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

## 5. Test in KSP

1. Start KSP 1.12.x through Steam.
2. Load a disposable Career test save.
3. Confirm the Race for Space Command Center appears.
4. Press `F8` and confirm the window hides and reopens.
5. Test the feature or change being worked on.
6. Save and reload when testing persistence-related behaviour.

When testing balance config changes, edit the deployed `GameData/TheRaceForSpace/Config/RaceSettings.cfg` and restart KSP before checking the changed values.

### 0.4 Core runtime verification

Use these checks after changes to race lifecycle or runtime ownership:

1. Open the Command Center and note the current rival funds, mission progress, and next funding date.
2. Hide the Command Center with `F8` and continue playing or time-warping for more than one five-second runtime refresh.
3. Reopen the Command Center and confirm the race state is still current.
4. Move between normal KSP game scenes, for example Space Center and Flight or Tracking Station, and confirm rival funds/progress do not reset to new-game values.
5. When practical, cross one shared funding boundary and confirm it is processed once and the next funding date advances normally.
6. Save, return to the menu, reload the save, and confirm the persisted race state is restored.

These checks verify that `Core/RaceRuntime` owns progression while `UI/RaceWindow` only displays the current controller state.

### 0.4 vessel discovery verification

Use these checks after changes to vessel discovery or satellite tracking:

1. Put a Probe or Relay vessel into Kerbin orbit and, while it is the loaded active vessel, confirm the Kerbin satellite count updates within the normal five-second refresh.
2. Reach orbit without saving or changing scenes first. Confirm the active vessel is recognised immediately; this verifies the live `Vessel` path rather than a stale `ProtoVessel` snapshot.
3. Return to the Space Center or Tracking Station so the vessel becomes unloaded, then confirm the same satellite is still counted. This verifies the persistent `ProtoVessel` path.
4. Repeat with a Relay vessel and confirm it counts as a satellite in the same way as a Probe vessel.
5. Put a crewed vessel into Kerbin orbit and confirm Crewed Orbit is recognised. If the craft is a crewed Probe, it should still count toward the satellite network but should not qualify as the uncrewed Probe Orbit achievement by itself.
6. If testing Mun, Minmus, or Duna, confirm an unloaded orbiting Probe or Relay remains counted after scene changes and save/reload.

These checks verify the boundary between `KspIntegration/KspVesselDiscovery` and `Tracking/SatelliteTracker` while preserving loaded and unloaded vessel behaviour.

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

The standalone logic tests target .NET 8. The KSP mod itself targets .NET Framework 4.7.2 and, on Linux, may also require compatible Mono 4.7.2 reference assemblies.

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

## 5. Switch to the current 0.4 development branch

```bash
git fetch origin
git switch --track origin/Alpha/Cleanup-0.4
```

If the local branch already exists, use:

```bash
git switch Alpha/Cleanup-0.4
git pull --ff-only
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

## 7. Run the tests

```bash
cd /home/deck/Projects/the-race-for-space/
bash tools/run-logic-tests.sh
```

Expected final message:

```text
All prototype logic tests passed.
```

## 8. Build and deploy the first test build

```bash
bash tools/test-prototype.sh Alpha/Cleanup-0.4
```

Verify both deployment files:

```bash
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll"
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Config/RaceSettings.cfg"
```

After this succeeds, the machine is set up. Future testing should use **Part 1** only.

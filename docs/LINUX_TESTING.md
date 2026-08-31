# Linux / Steam Deck Testing and Deployment

This guide describes the normal local test cycle for **The Race for Space** on the Linux PC / Steam Deck development machine using **Konsole**.

## Local KSP installation

The installed KSP `GameData` folder is:

```text
/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program/GameData/
```

The build uses `KSP_ROOT`, which must point to the **KSP installation folder**, not directly to `GameData`:

```bash
export KSP_ROOT="/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program"
```

You can confirm the KSP managed assemblies are available with:

```bash
test -f "$KSP_ROOT/KSP_Data/Managed/Assembly-CSharp.dll" && echo "KSP references found"
```

Expected output:

```text
KSP references found
```

## Open the repository in Konsole

Open Konsole and change to the local repository folder:

```bash
cd /path/to/the-race-for-space
```

Replace `/path/to/the-race-for-space` with the actual local clone location.

For the current 0.4 cleanup build, switch to:

```bash
git fetch origin
git switch Alpha/Cleanup-0.4
git pull --ff-only
```

If the branch does not yet exist locally, use:

```bash
git fetch origin
git switch --track origin/Alpha/Cleanup-0.4
```

## Run the automated logic tests

From the repository root:

```bash
bash tools/run-logic-tests.sh
```

The logic tests do not require KSP to be running. They test the KSP-independent funding, milestone, persistence, and rival-simulation logic.

A successful run should finish with:

```text
All prototype logic tests passed.
```

If the test runner reports a failure, do not deploy the build until the failure has been investigated.

## Recommended build and deploy command

The easiest way to update the selected prototype branch, build it, and copy the DLL into KSP is:

```bash
export KSP_ROOT="/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program"
bash tools/test-prototype.sh Alpha/Cleanup-0.4
```

The helper refuses to change branches when the repository contains uncommitted changes, fetches the latest branch, builds the mod, and deploys the DLL to:

```text
/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll
```

## Manual build and deployment

If you want to build the currently checked-out code without using the helper:

```bash
export KSP_ROOT="/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program"
dotnet build ./src/TheRaceForSpace/TheRaceForSpace.csproj -c Debug -p:DeployToKsp=true
```

The build target creates the mod plugin directory when necessary and copies the compiled DLL into KSP.

Verify the deployed DLL with:

```bash
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll"
```

## Normal Konsole test cycle

For most development changes, use this sequence from the repository root:

```bash
export KSP_ROOT="/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program"

git switch Alpha/Cleanup-0.4
git pull --ff-only

bash tools/run-logic-tests.sh
bash tools/test-prototype.sh Alpha/Cleanup-0.4
```

Then start KSP through Steam and load a disposable test save.

## In-game verification

After deployment:

1. Start KSP 1.12.x.
2. Load a disposable Career test save.
3. Confirm the Race for Space Command Center appears.
4. Press `F8` to confirm the window hides and reopens correctly.
5. Verify the current funding targets, rival agencies, achievements, and Duna progression relevant to the change being tested.
6. Save and reload when testing persistence-related behaviour.

## Useful checks

Check the installed .NET SDK:

```bash
dotnet --version
```

Check the current Git branch:

```bash
git branch --show-current
```

Check whether the repository has uncommitted changes:

```bash
git status --short
```

Check the deployed DLL timestamp:

```bash
stat "$KSP_ROOT/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll"
```

## Important path note

Use:

```text
KSP_ROOT=/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program
```

Do **not** set `KSP_ROOT` to:

```text
/home/deck/.local/share/Steam/steamapps/common/Kerbal Space Program/GameData/
```

`GameData` is the deployment destination inside the KSP installation. The project needs the installation root so it can also locate KSP's managed assemblies under `KSP_Data/Managed/`.

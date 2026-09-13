# Building The Race for Space

The current development version is **Content v0.6 alpha** for **Kerbal Space Program 1.12.x** and **.NET Framework 4.7.2**.

Current branch:

```text
Alpha/Content-v0.6
```

Do not create a new branch unless it has been explicitly approved.

## Project

```text
src/TheRaceForSpace/TheRaceForSpace.csproj
```

KSP and Unity assemblies are read from your local KSP installation. They are not stored in this repository.

## Prerequisites

You need:

- Kerbal Space Program 1.12.x installed locally;
- Git;
- a .NET SDK capable of building SDK-style projects;
- `KSP_ROOT` pointing at the KSP installation folder containing the KSP executable and `GameData`.

Do not copy KSP, Unity, or .NET Framework DLLs into this repository.

## Automated tests

From the repository root:

```bash
bash tools/run-logic-tests.sh
```

This runs both KSP-independent suites:

- domain/tracking/funding/rival/persistence tests;
- `CampaignController` and unlock/integration regression tests.

The Content v0.6 suite includes rival Science, live missions, casualties, crew contention, satellite reservations, facilities, research/construction, persistence, large time jumps, signed rival funding, and funding-boundary ordering.

These tests do **not** build against a real KSP installation in CI, so they cannot prove stock KSP Science APIs, scene lifecycle, IMGUI presentation, or direct Career integration.

## Linux / Steam Deck quick cycle

A common Steam installation is:

```text
~/.local/share/Steam/steamapps/common/Kerbal Space Program
```

From a new terminal:

```bash
cd /home/deck/Projects/the-race-for-space/
export KSP_ROOT="$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"

git fetch origin
git switch Alpha/Content-v0.6
git pull --ff-only

bash tools/run-logic-tests.sh
bash tools/test-prototype.sh Alpha/Content-v0.6
```

The first `cd` matters. Git commands and `tools/...` helpers must run from inside the repository.

See [`LINUX_TESTING.md`](LINUX_TESTING.md) for the fuller Steam Deck cycle.

## Linux build

Set `KSP_ROOT` and verify the managed assembly:

```bash
cd /home/deck/Projects/the-race-for-space/
export KSP_ROOT="$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"
test -f "$KSP_ROOT/KSP_Data/Managed/Assembly-CSharp.dll" && echo "KSP references found"
```

Build:

```bash
dotnet build ./src/TheRaceForSpace/TheRaceForSpace.csproj -c Debug
```

Typical output:

```text
src/TheRaceForSpace/bin/Debug/net472/TheRaceForSpace.dll
```

The project can use compatible Mono 4.7.2 reference assemblies on Linux when they are installed. Do not solve missing framework references by committing copied framework DLLs.

## Windows build

Example PowerShell:

```powershell
$env:KSP_ROOT = "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program"
dotnet build .\src\TheRaceForSpace\TheRaceForSpace.csproj -c Debug
```

The project reads KSP assemblies from:

```text
<KSP_ROOT>\KSP_x64_Data\Managed\
```

Install the .NET Framework 4.7.2 Developer Pack / targeting pack if the build machine does not already provide those reference assemblies.

## Build and deploy into KSP

Deployment is opt-in.

Linux:

```bash
cd /home/deck/Projects/the-race-for-space/
dotnet build ./src/TheRaceForSpace/TheRaceForSpace.csproj -c Debug -p:DeployToKsp=true
```

Windows PowerShell:

```powershell
dotnet build .\src\TheRaceForSpace\TheRaceForSpace.csproj -c Debug -p:DeployToKsp=true
```

This copies:

```text
<KSP_ROOT>/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll
<KSP_ROOT>/GameData/TheRaceForSpace/Config/CampaignSettings.cfg
```

An ordinary build without `DeployToKsp=true` never modifies the KSP installation.

## Linux helper

The normal helper is:

```bash
cd /home/deck/Projects/the-race-for-space/
bash tools/test-prototype.sh Alpha/Content-v0.6
```

The helper:

1. refuses to switch branches when the working tree has uncommitted changes;
2. fetches `origin`;
3. switches to the requested **existing** branch;
4. pulls with fast-forward-only behaviour;
5. finds `KSP_ROOT` when possible;
6. builds with `DeployToKsp=true`;
7. verifies that the DLL and `CampaignSettings.cfg` were deployed.

If you are already on the correct branch:

```bash
bash tools/test-prototype.sh
```

## Campaign settings

Editable balance lives in:

```text
GameData/TheRaceForSpace/Config/CampaignSettings.cfg
```

Content v0.6 adds rival-programme settings for crew, payroll/insurance, construction, research, facility capabilities, normal/Science Launch Progress, and mission locations/durations/difficulties.

Restart KSP after changing the config. Settings are loaded during startup rather than continuously reloaded.

## Troubleshooting

### `fatal: not a git repository`

Move into the clone first:

```bash
cd /home/deck/Projects/the-race-for-space/
git status --short
```

### `tools/...: No such file or directory`

The helper paths are repository-relative:

```bash
cd /home/deck/Projects/the-race-for-space/
ls tools
```

### `KSP_ROOT is not set`

Point it at the KSP installation folder, not `GameData` or `Managed`.

Expected shape:

```text
<KSP_ROOT>/GameData/
<KSP_ROOT>/KSP_Data/Managed/        Linux
<KSP_ROOT>/KSP_x64_Data/Managed/    Windows
```

### `Could not find KSP managed assemblies`

Confirm:

Linux:

```text
<KSP_ROOT>/KSP_Data/Managed/Assembly-CSharp.dll
```

Windows:

```text
<KSP_ROOT>\KSP_x64_Data\Managed\Assembly-CSharp.dll
```

and confirm the installation is KSP 1.12.x.

## After a successful build

Use a disposable Career save and perform the v0.6 smoke/acceptance checks in [`CONTENT_V0_6_TESTING.md`](CONTENT_V0_6_TESTING.md).

At minimum verify:

1. Command Center and Flight-only launcher lifecycle;
2. four opening Pre-Orbit offers and normal sponsor progression;
3. Rival Agencies dashboard with separate Contract/Science preparation;
4. live rival mission progression through timewarp;
5. stock shared Science interaction;
6. funding/research/construction across a boundary;
7. save/reload of expanded rival programme state;
8. sponsor-review, rival-completion, and player-payout notifications;
9. no repeated The Race for Space exceptions in `KSP.log`.

For the detailed retained Pre-Orbit vessel-tracking cases, also use [`KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md).

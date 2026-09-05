# Building The Race for Space

The mod targets **Kerbal Space Program 1.12.x** and **.NET Framework 4.7.2**.

The project file is:

```text
src/TheRaceForSpace/TheRaceForSpace.csproj
```

KSP and Unity assemblies are read from your local KSP installation. They are not stored in this repository.

## Prerequisites

You need:

- Kerbal Space Program 1.12.x installed locally;
- Git;
- a .NET SDK capable of building SDK-style projects;
- `KSP_ROOT` pointing to the KSP installation folder containing the KSP executable and `GameData`.

Do not copy KSP, Unity, or .NET Framework DLLs into this repository.

## Current development branch

The current 0.5 development branch is:

```text
Alpha/KerbalContracts-v0.5
```

Do not create a new branch unless it has been explicitly approved.

## Run the automated logic tests first

From the repository root:

```bash
bash tools/run-logic-tests.sh
```

This runs both KSP-independent suites:

- domain/tracking/funding/rival/persistence tests;
- `CampaignController` and unlock-rule regression tests.

These tests do not build the real KSP assembly because the CI environment does not contain KSP or Unity DLLs.

## Linux build

A common Steam installation is:

```text
~/.local/share/Steam/steamapps/common/Kerbal Space Program
```

Another common location is:

```text
~/.steam/steam/steamapps/common/Kerbal Space Program
```

Set `KSP_ROOT` to the installation that exists on your machine:

```bash
export KSP_ROOT="$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"
```

Confirm the KSP assembly is available:

```bash
test -f "$KSP_ROOT/KSP_Data/Managed/Assembly-CSharp.dll" && echo "KSP references found"
```

Build:

```bash
dotnet build ./src/TheRaceForSpace/TheRaceForSpace.csproj -c Debug
```

The output DLL is normally:

```text
src/TheRaceForSpace/bin/Debug/net472/TheRaceForSpace.dll
```

## Windows build

Example PowerShell setup:

```powershell
$env:KSP_ROOT = "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program"
dotnet build .\src\TheRaceForSpace\TheRaceForSpace.csproj -c Debug
```

The project reads KSP assemblies from:

```text
<KSP_ROOT>\KSP_x64_Data\Managed\
```

## Build and deploy into KSP

Deployment is opt-in.

### Linux

```bash
dotnet build ./src/TheRaceForSpace/TheRaceForSpace.csproj -c Debug -p:DeployToKsp=true
```

### Windows PowerShell

```powershell
dotnet build .\src\TheRaceForSpace\TheRaceForSpace.csproj -c Debug -p:DeployToKsp=true
```

This copies:

```text
<KSP_ROOT>/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll
<KSP_ROOT>/GameData/TheRaceForSpace/Config/CampaignSettings.cfg
```

An ordinary build without `DeployToKsp=true` never modifies your KSP installation.

## Linux helper

For the normal Linux / Steam Deck development cycle, use:

```bash
bash tools/test-prototype.sh Alpha/KerbalContracts-v0.5
```

The helper:

1. refuses to switch branches if the working tree has uncommitted changes;
2. fetches `origin`;
3. switches to the requested existing branch;
4. pulls with fast-forward-only behaviour;
5. finds `KSP_ROOT` when possible;
6. builds with `DeployToKsp=true`;
7. verifies that the DLL and `CampaignSettings.cfg` were deployed.

If you are already on the correct branch:

```bash
bash tools/test-prototype.sh
```

See [`LINUX_TESTING.md`](LINUX_TESTING.md) for the full Linux test cycle.

## Campaign settings

The editable config is:

```text
GameData/TheRaceForSpace/Config/CampaignSettings.cfg
```

The deploy target copies this file next to the built DLL in the KSP installation.

Restart KSP after changing the config. The current implementation reads the campaign settings during startup rather than continuously reloading them.

## Troubleshooting

### `KSP_ROOT is not set`

Point `KSP_ROOT` at the KSP installation folder, not at `GameData` or the `Managed` folder.

Correct shape:

```text
<KSP_ROOT>/KSP executable
<KSP_ROOT>/GameData/
<KSP_ROOT>/KSP_Data/Managed/        Linux
<KSP_ROOT>/KSP_x64_Data/Managed/    Windows
```

### `Could not find KSP managed assemblies`

Linux:

```text
<KSP_ROOT>/KSP_Data/Managed/Assembly-CSharp.dll
```

Windows:

```text
<KSP_ROOT>\KSP_x64_Data\Managed\Assembly-CSharp.dll
```

Confirm that file exists and that the KSP installation is 1.12.x.

### Missing .NET Framework reference assemblies on Linux

The project can use compatible Mono 4.7.2 reference assemblies when available.

If the build reports missing .NET Framework 4.7.2 references, install/configure compatible Mono reference assemblies and retry. Do not solve this by copying framework DLLs into the repository.

### Missing .NET Framework reference assemblies on Windows

Install the .NET Framework 4.7.2 Developer Pack / targeting pack, or build from a Visual Studio installation that includes it.

## After a successful build

Use a disposable KSP Career save and check:

1. the Command Center opens with F8 and the stock launcher button;
2. the four opening Pre-Orbit contracts are offered;
3. Funding Targets shows live Flight Contract telemetry during flight;
4. rival and funding state continue while the Command Center is hidden;
5. save/reload restores campaign state.

For the complete 0.5 in-game checklist, use [`KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md).

# Building The Race for Space

The prototype is built as a .NET Framework 4.7.2 class library against the assemblies shipped with Kerbal Space Program 1.12.x.

## Prerequisites

- Kerbal Space Program 1.12.x installed locally.
- A .NET SDK capable of building SDK-style projects.
- The environment variable `KSP_ROOT` set to the KSP installation folder containing the KSP executable and `GameData`, or a KSP installation in one of the standard Linux Steam locations detected by the test helper.

Do not copy KSP or Unity DLLs into this repository. The project references them directly from the local KSP installation.

## Quick Linux prototype test cycle

For repeated prototype testing, use `tools/test-prototype.sh` instead of manually fetching, switching, pulling, building and deploying each version.

From anywhere inside the repository, run:

```bash
bash tools/test-prototype.sh Alpha/Cleanup-0.4
```

Replace the branch name with whichever prototype version you want to test.

The helper:

1. refuses to switch versions if the working tree has uncommitted changes;
2. fetches the latest branches from `origin`;
3. creates a local tracking branch when the requested branch only exists on GitHub;
4. switches to the requested branch and pulls it with fast-forward-only behaviour;
5. uses `KSP_ROOT` when already set, otherwise checks the standard Linux Steam KSP locations;
6. builds the current prototype with `DeployToKsp=true`;
7. verifies that `TheRaceForSpace.dll` exists in the KSP plugin folder.

If no branch is supplied, the helper updates, builds and deploys the currently checked-out branch:

```bash
bash tools/test-prototype.sh
```

## Linux manual build

A typical Steam installation is found at one of these locations:

```text
~/.local/share/Steam/steamapps/common/Kerbal Space Program
~/.steam/steam/steamapps/common/Kerbal Space Program
```

Set `KSP_ROOT` to the location that exists on your machine:

```bash
export KSP_ROOT="$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"
```

Confirm the KSP assembly required by the build exists:

```bash
test -f "$KSP_ROOT/KSP_Data/Managed/Assembly-CSharp.dll" && echo "KSP references found"
```

You should see:

```text
KSP references found
```

Check the .NET SDK is installed:

```bash
dotnet --version
```

Build the prototype from the repository root:

```bash
dotnet build ./src/TheRaceForSpace/TheRaceForSpace.csproj -c Debug
```

The compiled mod assembly will be written under:

```text
src/TheRaceForSpace/bin/Debug/net472/TheRaceForSpace.dll
```

### Linux build and deploy into KSP

For local testing, build and copy the DLL directly into the KSP plugin folder:

```bash
dotnet build ./src/TheRaceForSpace/TheRaceForSpace.csproj -c Debug -p:DeployToKsp=true
```

This copies the assembly to:

```text
$KSP_ROOT/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll
```

Confirm deployment with:

```bash
test -f "$KSP_ROOT/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll" && echo "Prototype deployed"
```

## Windows PowerShell

```powershell
$env:KSP_ROOT = "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program"
dotnet build .\src\TheRaceForSpace\TheRaceForSpace.csproj -c Debug
```

The compiled mod assembly will be written under:

```text
src/TheRaceForSpace/bin/Debug/net472/TheRaceForSpace.dll
```

## Windows build and deploy into KSP

```powershell
$env:KSP_ROOT = "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program"
dotnet build .\src\TheRaceForSpace\TheRaceForSpace.csproj -c Debug -p:DeployToKsp=true
```

This copies `TheRaceForSpace.dll` to:

```text
<KSP_ROOT>\GameData\TheRaceForSpace\Plugins\
```

The deploy action is opt-in so an ordinary build never modifies a KSP installation.

## Troubleshooting

### `KSP_ROOT is not set`

Set `KSP_ROOT` to the folder containing the KSP executable and `GameData`, not the `GameData` or `Managed` subfolder.

### `Could not find KSP managed assemblies`

On Linux, confirm this file exists:

```text
<KSP_ROOT>/KSP_Data/Managed/Assembly-CSharp.dll
```

On Windows, confirm this file exists:

```text
<KSP_ROOT>\KSP_x64_Data\Managed\Assembly-CSharp.dll
```

### Missing .NET Framework reference assemblies on Linux

If `dotnet build` reports missing .NET Framework 4.7.2 reference assemblies on a Linux installation, install or configure compatible reference assemblies for the SDK and retry the build. Do not copy framework or KSP DLLs into the repository as a workaround.

### Missing .NET Framework reference assemblies on Windows

Install the .NET Framework 4.7.2 Developer Pack / targeting pack, or build the project with an appropriate Visual Studio installation.

## Prototype verification

After deploying the DLL:

1. Start KSP 1.12.x.
2. Load a disposable test save.
3. Confirm the prototype window appears.
4. Press F8 to hide/show it.
5. Follow `docs/design/VERSION_0_4_SCOPE.md` for the current alpha baseline.

Automated unit-test project setup is intentionally separate from this build/deploy helper. The existing `tests/TheRaceForSpace.Tests/` structure remains reserved for logic that can be tested without a live KSP installation.

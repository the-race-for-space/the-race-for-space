# Building The Race for Space

The prototype is built as a .NET Framework 4.7.2 class library against the assemblies shipped with Kerbal Space Program 1.12.x.

## Prerequisites

- Kerbal Space Program 1.12.x installed locally.
- A .NET SDK capable of building SDK-style projects.
- On Linux, Mono reference assemblies compatible with .NET Framework 4.7.2.
- The environment variable `KSP_ROOT` set to the KSP installation folder containing the KSP executable and `GameData`.

Do not copy KSP or Unity DLLs into this repository. The project references them directly from the local KSP installation.

## Linux

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

Check the .NET SDK and Mono are installed:

```bash
dotnet --version
mono --version
```

The project automatically uses `/usr/lib/mono/4.7.2-api` when that standard Mono reference-assembly path exists. Confirm it with:

```bash
test -f /usr/lib/mono/4.7.2-api/mscorlib.dll && echo "Mono net472 references found"
```

If your distribution installs Mono somewhere else, locate the 4.7.2 reference folder and set `FrameworkPathOverride` before building, for example:

```bash
export FrameworkPathOverride=/path/to/mono/4.7.2-api
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

KSP uses the .NET Framework-compatible Mono runtime rather than modern .NET. The project remains targeted at `net472`, so Linux builds need Mono's .NET Framework reference assemblies.

The project detects this common path automatically:

```text
/usr/lib/mono/4.7.2-api
```

If your distribution stores them elsewhere, set `FrameworkPathOverride` to the directory containing `mscorlib.dll` before running `dotnet build`.

### Missing .NET Framework reference assemblies on Windows

Install the .NET Framework 4.7.2 Developer Pack / targeting pack, or build the project with an appropriate Visual Studio installation.

## Prototype verification

After deploying the DLL:

1. Start KSP 1.12.x.
2. Load a disposable test save.
3. Confirm the prototype window appears.
4. Press F8 to hide/show it.
5. Follow `docs/design/SATELLITE_PROTOTYPE_V1.md` for the satellite-race verification steps.

Automated unit-test project setup is intentionally separate from this first build harness. The existing `tests/TheRaceForSpace.Tests/` structure remains reserved for logic that can be tested without a live KSP installation.

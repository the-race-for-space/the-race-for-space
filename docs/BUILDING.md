# Building The Race for Space

The prototype is built as a .NET Framework 4.7.2 class library against the assemblies shipped with Kerbal Space Program 1.12.x.

## Prerequisites

- Kerbal Space Program 1.12.x installed locally.
- A .NET SDK capable of building `net472` projects on your platform, or Visual Studio/MSBuild with the .NET Framework 4.7.2 targeting pack.
- The environment variable `KSP_ROOT` set to the KSP installation folder containing `KSP_x64.exe` and `GameData`.

Do not copy KSP or Unity DLLs into this repository. The project references them directly from the local KSP installation.

## Windows PowerShell

```powershell
$env:KSP_ROOT = "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program"
dotnet build .\src\TheRaceForSpace\TheRaceForSpace.csproj -c Debug
```

The compiled mod assembly will be written under:

```text
src/TheRaceForSpace/bin/Debug/net472/TheRaceForSpace.dll
```

## Build and deploy into KSP

For local testing, the project can copy the built DLL directly into the mod's KSP plugin folder:

```powershell
$env:KSP_ROOT = "C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program"
dotnet build .\src\TheRaceForSpace\TheRaceForSpace.csproj -c Debug -p:DeployToKsp=true
```

This copies `TheRaceForSpace.dll` to:

```text
<KSP_ROOT>\GameData\TheRaceForSpace\Plugins\
```

The deploy action is opt-in so an ordinary build never modifies a KSP installation.

## Command Prompt

```cmd
set KSP_ROOT=C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program
dotnet build src\TheRaceForSpace\TheRaceForSpace.csproj -c Debug
```

## Troubleshooting

### `KSP_ROOT is not set`

Set `KSP_ROOT` to the folder containing `KSP_x64.exe`, not the `GameData` or `Managed` subfolder.

### `Could not find KSP managed assemblies`

Confirm this file exists:

```text
<KSP_ROOT>\KSP_x64_Data\Managed\Assembly-CSharp.dll
```

### Missing .NET Framework reference assemblies

If `dotnet build` reports that the .NET Framework 4.7.2 reference assemblies are unavailable, install the .NET Framework 4.7.2 Developer Pack / targeting pack, or build the project with an appropriate Visual Studio installation.

## Prototype verification

After deploying the DLL:

1. Start KSP 1.12.x.
2. Load a save.
3. Confirm the prototype window appears.
4. Press F8 to hide/show it.
5. Follow `docs/design/SATELLITE_PROTOTYPE_V1.md` for the satellite-race verification steps.

Automated unit-test project setup is intentionally separate from this first build harness. The existing `tests/TheRaceForSpace.Tests/` structure remains reserved for logic that can be tested without a live KSP installation.

# The Race for Space

**The Race for Space** is a Kerbal Space Program 1.12.x mod prototype about competing space agencies, shared objectives, funding, and rival progress.

The current development version is **0.5 alpha** on:

```text
Alpha/KerbalContracts-v0.5
```

## What version 0.5 adds

Version 0.5 adds a **Pre-Orbit** phase on Kerbin before Probe Orbit.

There are four Pre-Orbit contract lines, with five levels in each line:

- **Directed Power** — reach a required surface speed below a 70 km ceiling, then impact Kerbin.
- **Mass** — land on Kerbin with enough remaining vessel mass after travelling far enough from the launch point.
- **Control** — hold a crewed vessel inside an altitude band for a required time, then land safely on Kerbin with crew.
- **Biome** — land in progressively harder Kerbin biomes.

At campaign start, Level I of all four lines is offered. Levels II-V unlock in sequence when the previous level in that line is completed by **any agency**. Completing Level V in any one line offers **Probe Orbit** immediately.

The existing orbital objectives, satellite-network funding, sponsor reviews, rival simulation, and persistent campaign state remain part of the same campaign.

## Project terminology

Use these names consistently in code and documentation:

| Term | Meaning |
| --- | --- |
| **Campaign** | The overall progression state for the current KSP save. |
| **Agency** | The player or a simulated rival organisation. |
| **Objective** | A gameplay goal that an agency can complete. |
| **Objective Funding Contract** | One-off funding tied to completing an objective. |
| **Satellite Network Funding Contract** | Repeatable funding based on qualifying satellites around a body. |
| **Pre-Orbit Contract** | One of the current Directed Power, Mass, Control, or Biome contracts on Kerbin. |
| **Flight Contract** | Generic infrastructure for contracts evaluated from the actively controlled vessel. |
| **Orbital Vessel Tracking** | The slower loaded/unloaded vessel scan used for orbital objectives and satellite counts. |

**Pre-Orbit** describes the current Kerbin contract family. **Flight Contract** is deliberately generic so future active-vessel contracts on Mun, Minmus, or other bodies can use the same system.

## Repository layout

```text
src/TheRaceForSpace/          Production mod source
GameData/TheRaceForSpace/     Installable KSP package layout
tests/                        KSP-independent automated tests
docs/                         Current guides and historical design records
tools/                        Build and test helpers
```

The main source modules are:

- `Core/` — runtime scheduling and campaign settings.
- `Campaign/` — campaign coordination, offers, funding reviews, and progression.
- `Agencies/` — player and rival agency state.
- `Objectives/` — objective definitions and unlock rules.
- `Funding/` — objective and satellite-network funding contracts.
- `Rivals/` — rival mission selection and progress.
- `Tracking/` — KSP-independent flight-contract and orbital-vessel evaluation.
- `Persistence/` — project-owned save state.
- `KspIntegration/` — direct KSP API access and ScenarioModule integration.
- `UI/` — Command Center presentation only.

See [`docs/STRUCTURE.md`](docs/STRUCTURE.md) for the ownership rules and [`docs/CODE_OVERVIEW.md`](docs/CODE_OVERVIEW.md) for a simple walkthrough.

## Build and test

The mod project is:

```text
src/TheRaceForSpace/TheRaceForSpace.csproj
```

It targets .NET Framework 4.7.2 and builds against the assemblies from a local KSP 1.12.x installation. KSP and Unity DLLs are not stored in this repository.

For setup and build commands, see [`docs/BUILDING.md`](docs/BUILDING.md).

Run the KSP-independent logic suites with:

```bash
bash tools/run-logic-tests.sh
```

For Linux / Steam Deck deployment, see [`docs/LINUX_TESTING.md`](docs/LINUX_TESTING.md).

For the full in-game v0.5 acceptance pass, see [`docs/KERBAL_CONTRACTS_V0_5_TESTING.md`](docs/KERBAL_CONTRACTS_V0_5_TESTING.md).

## Balance configuration

User-editable campaign balance is stored in:

```text
GameData/TheRaceForSpace/Config/CampaignSettings.cfg
```

The config controls body-tier funding values, network sizes, funding interval, rival starting funds, rival progress chance, and rival count.

The current twenty Pre-Orbit objectives use code-defined values:

- Level I-V rewards: **10,000 / 20,000 / 30,000 / 40,000 / 50,000** funds.
- Rival progress per successful check: **20%**.
- Level I-V rival step costs: **4,000 / 6,000 / 8,000 / 10,000 / 12,000** funds.

Normal orbital and satellite-network rival missions continue to use 10% progress steps.

Restart KSP after changing `CampaignSettings.cfg`.

## Command Center

The mod uses one Command Center window with four main views:

- **Overview**
- **Funding Targets**
- **Rival Agencies**
- **Contract Catalogue**

The Contract Catalogue shows `Offered`, `Unlocked`, `Locked`, and `Expired` objective funding contracts.

Funding Targets shows detailed funding information. During flight it also shows live telemetry for every offered, unfinished Pre-Orbit contract that applies to the active vessel.

The UI reads campaign and tracking state. It does not advance gameplay.

## Architecture in one sentence

`ModRuntime` schedules work, `KspIntegration` reads KSP, `Tracking` evaluates project-owned snapshots, `CampaignController` coordinates campaign state, and the Command Center displays the result.

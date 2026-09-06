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
- **Mass** — land or splash down on Kerbin with enough remaining vessel mass after travelling far enough from the launch point; parts from another docked Flight Attempt cannot supply the qualifying mass.
- **Control** — hold a crewed vessel inside an altitude band for a required time, then land or splash down safely on Kerbin with crew. Docking or undocking another lineage resets an unfinished continuous hold but does not erase a hold that has already qualified.
- **Biome** — reach progressively harder Kerbin biomes and finish landed or splashed in the target biome.

At campaign start, Level I of all four lines is offered. Levels II-V unlock in sequence when the previous level in that line is completed by **any agency**. Completing Level V in any one line unlocks **Probe Orbit**, which then waits for the next sponsor review before becoming `Offered`.

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
- `UI/` — full Command Center, compact Flight-only contract presentation, and stock funding-completion notifications.

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

The config controls body-tier funding values, network sizes, funding interval, rival starting funds, rival progress chance, rival count, and the Level I-V Pre-Orbit rewards and rival progress costs.

The default Pre-Orbit values are:

- Level I-V rewards: **10,000 / 20,000 / 30,000 / 40,000 / 50,000** funds.
- Rival progress per successful Pre-Orbit check: **20%**.
- Level I-V rival step costs: **4,000 / 6,000 / 8,000 / 10,000 / 12,000** funds.

These values are configured in the `PRE_ORBIT` section. Each rival progress cost is charged for one successful 20% Pre-Orbit development step. Normal orbital and satellite-network rival missions continue to use 10% progress steps.

Restart KSP after changing `CampaignSettings.cfg`.

## Interfaces

The mod uses one full Command Center window with four main views:

- **Overview**
- **Funding Targets**
- **Rival Agencies**
- **Contract Catalogue**

The Contract Catalogue groups objective and satellite-network funding contracts into `Offered`, `Unlocked`, `Locked`, and `Expired` sections. Each section heading includes the current number of contracts in that state, for example `Offered (4)` or `Unlocked (2)`, and the count remains visible when the section is collapsed.

Overview shows only Offered objectives the player has not yet achieved under `Your Objectives`; completed objectives are hidden, and empty objective or satellite-network summaries show `None`. Completed one-off contracts that are still paying out appear under Funding Information with the player's projected next payout on the first line and the number of scheduled payments remaining on the second line. When more than one agency is eligible for that next payout, the second line also reports the split, for example `3 payments remaining (split between 2 agencies)`.

Funding Targets shows detailed funding, payout, completion, and contract-lifecycle information. It does not draw live active-vessel requirement telemetry; the compact Flight interface is the dedicated real-time flight display.

Flight mode also has a separate compact **Offered Contracts** window implemented by `FlightActiveUI`. It has its own Flight-only launcher button so the player can track contract requirements without opening the full Command Center. Unfinished Offered objective contracts appear first and can be expanded independently. Offered contracts already completed by the player sit at the bottom marked `Complete` with no expansion control.

After save/load or a Flight scene transition, expanded Pre-Orbit requirements wait for a fresh active-vessel sample rather than briefly presenting stale restored telemetry as current. Flight Attempt identity details such as vessel name and launch UT are intentionally not shown in the compact player-facing panel.

Expanded Pre-Orbit contracts show the current Directed Power, Mass, Control, or Biome requirement state from the existing `FlightContractTracker`. The Flight window does not sample vessels or advance gameplay; it reads the same runtime state already maintained by `ModRuntime`.

Persistent Flight Attempts keep independent history through vessel switching, staging, docking/undocking, and save/load. A newly constructed or replacement craft with a reused KSP vessel ID starts fresh when its persistent-part lineage does not overlap the old craft, while surviving remembered lineages remain independently recoverable. Dead or recovered histories are pruned only after a successful broad vessel refresh proves that none of their remembered persistent parts still exist.

`FundingNotificationUI` listens for newly recorded player objective completions and posts one green message through KSP's stock message system when the matching Objective Funding Contract is currently Offered and unexpired. The notification title is `Funding Target Completed — <Contract Name>` and the body explains that the agency is now eligible for a share of the remaining contract funding. Rival completions do not generate player notifications.

Saved objective completions are restored silently, so loading or reloading a save does not replay historical funding-completion messages. No notification-specific save data is added.

The UI reads campaign and tracking state. It does not advance gameplay.

## Architecture in one sentence

`ModRuntime` schedules work, `KspIntegration` reads KSP, `Tracking` evaluates project-owned snapshots, `CampaignController` coordinates campaign state, and the Command Center, `FlightActiveUI`, and `FundingNotificationUI` present the result.

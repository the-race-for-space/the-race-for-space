# The Race for Space

**The Race for Space** is a Kerbal Space Program 1.12.x mod prototype about competing space agencies, shared objectives, funding, Science races, and simulated rival programmes.

The current development version is **0.6 alpha** on:

```text
Alpha/Content-v0.6
```

## What version 0.6 adds

Version 0.6 keeps the complete version 0.5 Pre-Orbit campaign and expands the rival agencies into persistent programmes with their own development, Science, crew, facilities, and live missions.

The main v0.6 additions are:

- **Rival programme state** — each rival has stored Science, employed Kerbals, facility levels, research, construction, Contract preparation, Science preparation, and live missions.
- **Live rival missions** — reaching 100% Launch Progress launches a mission instead of instantly completing its objective. Missions have configured locations and durations, Difficulty-based success chances, deterministic stored outcome seeds, and crew assignments.
- **Rival Science Expeditions** — rivals select valid stock Science subjects, prepare expeditions independently of Contract launches, and compete with the player and other rivals for the same remaining stock Science.
- **Shared Science pool** — `KspScienceAdapter` is the only boundary that handles raw KSP Science objects. Rival code uses project-owned `ScienceSubjectKey` values and receives only the Science still available when a successful expedition completes.
- **Rival technology** — a fixed KSP 1.12 technology catalogue gates rival Science experiments. Contract destinations are not tech-gated.
- **Facilities and development** — all nine Space Centre facilities are represented. Facility levels affect rival income, roster limits, satellite capacity, research limits, launch progress, Science progress, and destination access.
- **Research and construction** — rivals can start research and one facility construction project at funding boundaries. Completed upgrades become active only at the appropriate later funding boundary.
- **Crew economy** — crewed missions use employed Kerbals, missing crew may be hired at launch when allowed, payroll is charged at funding boundaries, failed crewed missions can cause casualties, and insurance is deducted later.
- **Signed rival funding** — rival Funds can become negative. Administration income, Contract income, satellite income, payroll, and pending insurance are applied as one authoritative funding-boundary result.
- **Expanded Rival Agencies dashboard** — the Command Center shows programme status, live missions, Contract and Science preparation, construction, facilities, tech/research, and funding from authoritative simulation state.
- **UI and notifications polish** — Funding Targets prioritises player-uncompleted objectives, Flight shows each offered objective's base reward, and stock KSP messages report sponsor-review offers, rival objective completions, and player campaign payouts.
- **Player help** — the Command Center help view explains the Pre-Orbit progression and includes a worked funding-sharing example.

The v0.6 rival-programme implementation is covered by KSP-independent logic/controller tests. The remaining real-KSP acceptance checks are tracked in [`docs/CONTENT_V0_6_TESTING.md`](docs/CONTENT_V0_6_TESTING.md).

The planned Desert/Polar and orbital/ground base progression is **not implemented yet**. It remains active work in [`docs/TODO.md`](docs/TODO.md).

## Pre-Orbit campaign

There are four Kerbin Pre-Orbit contract lines with five levels in each line:

- **Directed Power** — reach a required surface speed below a 70 km ceiling, then impact Kerbin.
- **Mass** — land or splash down on Kerbin with enough remaining vessel mass after travelling far enough from the launch point; parts from another docked Flight Attempt cannot supply the qualifying mass.
- **Control** — hold a crewed vessel inside an altitude band for a required time, then land or splash down safely on Kerbin with crew.
- **Biome** — reach progressively harder Kerbin biomes and finish landed or splashed in the target biome.

At campaign start, Level I of all four lines is offered. Levels II-V unlock in sequence when the previous level in that line is completed by **any agency**. Completing Level V in any one line unlocks **Probe Orbit**, which then waits for a sponsor review before becoming `Offered`.

## Project terminology

Use these names consistently in code and documentation:

| Term | Meaning |
| --- | --- |
| **Campaign** | Overall progression state for the current KSP save. |
| **Agency** | The player or a simulated rival organisation. |
| **Objective** | A gameplay goal that an agency can complete. |
| **Objective Funding Contract** | One-off funding tied to completing an objective. |
| **Satellite Network Funding Contract** | Repeatable funding based on qualifying satellites around a body. |
| **Pre-Orbit Contract** | A Directed Power, Mass, Control, or Biome objective on Kerbin. |
| **Flight Contract** | Generic infrastructure for objectives evaluated from the actively controlled vessel. |
| **Rival Programme** | Persistent rival-only Science, crew, facilities, research, construction, preparations, and live missions. |
| **Launch Progress** | Preparation progress toward launching a rival Contract or Science mission. It is not objective completion. |
| **Live Mission** | A launched rival Contract or Science mission waiting for its stored completion UT and outcome. |
| **Science Expedition** | Rival mission targeting one stock Science subject through a project-owned subject identity. |
| **Orbital Vessel Tracking** | Slower loaded/unloaded vessel scan used for orbital objectives and satellite counts. |

**Pre-Orbit** describes the current Kerbin contract family. **Flight Contract** remains generic so future active-vessel contracts can reuse the same tracking system.

## Repository layout

```text
src/TheRaceForSpace/          Production mod source
GameData/TheRaceForSpace/     Installable KSP package layout
tests/                        KSP-independent automated tests
docs/                         Current guides plus historical design records
tools/                        Build and test helpers
```

The main source modules are:

- `Core/` — runtime scheduling and campaign settings.
- `Campaign/` — campaign coordination, offers, funding boundaries, and progression.
- `Agencies/` — common agency state plus persistent rival-programme state.
- `Objectives/` — objective definitions, unlock rules, Difficulty, and rival crew metadata.
- `Funding/` — objective and satellite-network funding contracts.
- `Rivals/` — chronological rival coordination, Science preparation, live missions, development, and rival tech.
- `Tracking/` — KSP-independent active-flight and orbital-vessel evaluation.
- `Persistence/` — project-owned campaign, flight-attempt, and rival-programme save state.
- `KspIntegration/` — direct KSP API access, stock Science boundary, configuration loading, funding adapter, and ScenarioModule integration.
- `UI/` — Command Center, rival dashboard, compact Flight contracts, player help, and stock campaign notifications.

See [`docs/STRUCTURE.md`](docs/STRUCTURE.md) for ownership rules and [`docs/CODE_OVERVIEW.md`](docs/CODE_OVERVIEW.md) for the code walkthrough.

## Build and test

The mod project is:

```text
src/TheRaceForSpace/TheRaceForSpace.csproj
```

It targets .NET Framework 4.7.2 and builds against assemblies from a local KSP 1.12.x installation. KSP and Unity DLLs are not stored in this repository.

For setup and build commands, see [`docs/BUILDING.md`](docs/BUILDING.md).

Run the KSP-independent logic and controller suites with:

```bash
bash tools/run-logic-tests.sh
```

For Linux / Steam Deck deployment, see [`docs/LINUX_TESTING.md`](docs/LINUX_TESTING.md).

For live KSP acceptance:

- [`docs/CONTENT_V0_6_TESTING.md`](docs/CONTENT_V0_6_TESTING.md) — v0.6 rival programme, Science, development, funding, UI, timewarp, and persistence checks.
- [`docs/KERBAL_CONTRACTS_V0_5_TESTING.md`](docs/KERBAL_CONTRACTS_V0_5_TESTING.md) — detailed Pre-Orbit Flight Contract checks retained from v0.5.

## Balance configuration

User-editable campaign balance is stored in:

```text
GameData/TheRaceForSpace/Config/CampaignSettings.cfg
```

The config currently controls:

- funding interval, rival starting Funds, and rival count;
- body-tier objective/network funding values and network sizes;
- Pre-Orbit rewards and rival progress costs;
- rival Kerbal hire cost, payroll, insurance, and casualty chance;
- research duration;
- facility construction costs and durations;
- facility capability values for Administration, Astronaut Complex, Mission Control, R&D, and Tracking Station;
- normal Contract Launch Progress checks and VAB/Launch Pad chance contributions;
- Science Launch Progress checks and SPH/Runway chance contributions;
- rival mission locations, durations, and Science difficulties.

The retired global `rivalProgressChancePercent` setting is no longer used. Normal Contract launch chance is derived from the rival's current **VAB + Launch Pad** levels, while Science preparation chance is derived from **SPH + Runway** levels.

Default Pre-Orbit values remain:

- Level I-V rewards: **10,000 / 20,000 / 30,000 / 40,000 / 50,000** Funds.
- Rival Pre-Orbit Launch Progress per successful check: **20%**.
- Level I-V rival step costs: **4,000 / 6,000 / 8,000 / 10,000 / 12,000** Funds.

Restart KSP after changing `CampaignSettings.cfg`.

## Interfaces

The full Command Center has four main views:

- **Overview**
- **Funding Targets**
- **Rival Agencies**
- **Contract Catalogue**

A `?` button opens the player guide.

**Funding Targets** draws currently offered objectives the player has not completed first, then satellite funding, then offered objectives already completed by the player. It presents funding state only; live active-vessel telemetry belongs in the Flight interface.

The Flight-only **Offered Contracts** window is implemented by `FlightActiveUI`. It has a separate Flight launcher button, lists unfinished Offered objectives before completed ones, displays each objective's base funding reward, and lets unfinished objectives be expanded independently. Expanded Pre-Orbit rows read the current `FlightContractTracker` state. The Flight UI does not sample vessels or advance gameplay.

**Rival Agencies** is implemented across `CommandCenterWindow.cs` and `CommandCenterWindow.Rivals.cs`. It presents programme status, live missions, separate Contract/Science preparations, construction, facilities, technology/research, and funding. It derives display text from authoritative campaign/rival state and does not run the simulation.

`FundingNotificationUI` observes live campaign signals and posts stock KSP inbox messages for:

- a player completing an Offered, unexpired Objective Funding Contract;
- a rival recording an objective completion;
- a sponsor review producing newly Offered funding targets;
- a positive campaign funding payout to the player.

Persistence restoration is intentionally silent, so loading historical objective completions does not replay completion notifications.

## Funding-boundary order

When timewarp crosses one or more funding boundaries, `CampaignController` processes each boundary chronologically:

1. catch up rival scheduled events to that boundary;
2. complete due rival research and facility construction;
3. update funding eligibility/start state;
4. calculate and apply player/rival funding, including rival payroll and insurance;
5. advance active one-off funding lifecycles;
6. start eligible new rival research;
7. start eligible new facility construction;
8. run the sponsor review last.

This ordering is covered by controller integration tests and prevents upgrades from retroactively affecting earlier rival events.

## Version 0.6 development record

The live TODO now contains only unfinished work. The large working TODO used throughout v0.6 implementation is preserved unchanged as [`docs/CONTENT_V0_6_TODO_ARCHIVE.md`](docs/CONTENT_V0_6_TODO_ARCHIVE.md) so the design decisions and completed task plan remain available as a historical record.

## Architecture in one sentence

`ModRuntime` schedules work, `KspIntegration` owns raw KSP boundaries, `Tracking` evaluates player snapshots, `CampaignController` coordinates campaign/funding state, `Rivals` advances persistent rival programmes chronologically, `Persistence` serializes project-owned state, and `UI` presents the result.

#!/usr/bin/env python3
from pathlib import Path
import re
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
BRANCH = "Alpha/KerbalContracts-v0.5"
CODE_ROOTS = [ROOT / "src", ROOT / "tests"]
ACTIVE_DOCS = [
    ROOT / "README.md",
    ROOT / "docs" / "BUILDING.md",
    ROOT / "docs" / "CODE_OVERVIEW.md",
    ROOT / "docs" / "KERBAL_CONTRACTS_V0_5_TESTING.md",
    ROOT / "docs" / "LINUX_TESTING.md",
    ROOT / "docs" / "STRUCTURE.md",
]
TEXT_SUFFIXES = {".cs", ".csproj", ".md", ".cfg", ".sh", ".yml"}


def run(*args):
    subprocess.run(args, cwd=ROOT, check=True)


def read_text(path):
    return path.read_text(encoding="utf-8")


def write_text(path, text):
    path.write_text(text, encoding="utf-8")


def text_files(roots, suffixes=TEXT_SUFFIXES):
    files = []
    for root in roots:
        if root.is_file():
            files.append(root)
            continue
        if not root.exists():
            continue
        for path in root.rglob("*"):
            if path.is_file() and path.suffix in suffixes:
                files.append(path)
    return files


def replace_text(roots, literal_replacements, regex_replacements=()):
    for path in text_files(roots):
        original = read_text(path)
        updated = original
        for old, new in literal_replacements:
            updated = updated.replace(old, new)
        for pattern, replacement in regex_replacements:
            updated = re.sub(pattern, replacement, updated)
        if updated != original:
            write_text(path, updated)


def move_path(old_relative, new_relative):
    old_path = ROOT / old_relative
    new_path = ROOT / new_relative
    if not old_path.exists():
        return
    if new_path.exists():
        raise RuntimeError(f"Cannot rename {old_relative}: {new_relative} already exists")
    new_path.parent.mkdir(parents=True, exist_ok=True)
    shutil.move(str(old_path), str(new_path))


def rename_files_by_tokens(roots, replacements):
    for root in roots:
        if not root.exists():
            continue
        files = sorted((p for p in root.rglob("*") if p.is_file()), key=lambda p: len(p.parts), reverse=True)
        for path in files:
            new_name = path.name
            for old, new in replacements:
                new_name = new_name.replace(old, new)
            if new_name == path.name:
                continue
            target = path.with_name(new_name)
            if target.exists():
                raise RuntimeError(f"Cannot rename {path}: {target} already exists")
            path.rename(target)


def assert_no_tokens(roots, tokens):
    failures = []
    for path in text_files(roots, {".cs", ".csproj"}):
        text = read_text(path)
        for token in tokens:
            if token in text:
                failures.append(f"{path.relative_to(ROOT)} still contains {token!r}")
    if failures:
        raise RuntimeError("Old naming remains:\n" + "\n".join(failures[:80]))


def verify_and_commit(message):
    run("git", "diff", "--check")
    run("bash", "tools/run-logic-tests.sh")
    run("git", "add", "-A")
    status = subprocess.run(
        ["git", "diff", "--cached", "--quiet"],
        cwd=ROOT,
        check=False,
    )
    if status.returncode == 0:
        print(f"No changes for checkpoint: {message}")
        return
    run("git", "commit", "-m", message)
    run("git", "push", "origin", f"HEAD:{BRANCH}")


def stage_domain_names():
    # Module names now describe the gameplay concepts they contain.
    move_path("src/TheRaceForSpace/Competition", "src/TheRaceForSpace/Campaign")
    move_path("src/TheRaceForSpace/Milestones", "src/TheRaceForSpace/Objectives")
    move_path("src/TheRaceForSpace/Programs", "src/TheRaceForSpace/Agencies")
    move_path("src/TheRaceForSpace/Simulation", "src/TheRaceForSpace/Rivals")
    move_path("tests/TheRaceForSpace.Tests/Competition", "tests/TheRaceForSpace.Tests/Campaign")
    move_path("tests/TheRaceForSpace.Tests/Milestones", "tests/TheRaceForSpace.Tests/Objectives")
    move_path("tests/TheRaceForSpace.Tests/Simulation", "tests/TheRaceForSpace.Tests/Rivals")

    file_names = [
        ("AchievementFundingProgramme", "ObjectiveFundingContract"),
        ("PrototypeFundingCatalogue", "FundingContractCatalogue"),
        ("SatelliteRaceController", "CampaignController"),
        ("MilestoneDefinition", "ObjectiveDefinition"),
        ("PrototypeMilestones", "ObjectiveCatalogue"),
        ("SpaceProgramState", "AgencyState"),
        ("RivalProgramsSaveState", "RivalAgenciesSaveState"),
        ("FundingProgramme", "SatelliteNetworkFundingContract"),
        ("RaceSettings", "CampaignSettings"),
        ("Milestone", "Objective"),
    ]
    rename_files_by_tokens(CODE_ROOTS, file_names)

    replacements = [
        ("TheRaceForSpace.Competition", "TheRaceForSpace.Campaign"),
        ("TheRaceForSpace.Milestones", "TheRaceForSpace.Objectives"),
        ("TheRaceForSpace.Programs", "TheRaceForSpace.Agencies"),
        ("TheRaceForSpace.Simulation", "TheRaceForSpace.Rivals"),
        (".Competition", ".Campaign"),
        (".Milestones", ".Objectives"),
        (".Programs", ".Agencies"),
        (".Simulation", ".Rivals"),
        ("/Competition/", "/Campaign/"),
        ("/Milestones/", "/Objectives/"),
        ("/Programs/", "/Agencies/"),
        ("/Simulation/", "/Rivals/"),
        ("AchievementFundingProgramme", "ObjectiveFundingContract"),
        ("PrototypeFundingCatalogue", "FundingContractCatalogue"),
        ("SatelliteRaceController", "CampaignController"),
        ("RaceBodySettings", "BodyBalanceSettings"),
        ("RaceSettings", "CampaignSettings"),
        ("MilestoneVesselObservation", "OrbitalObjectiveObservation"),
        ("MilestoneCrewRequirement", "ObjectiveCrewRequirement"),
        ("MilestoneSituation", "ObjectiveSituation"),
        ("MilestoneObjectiveType", "ObjectiveType"),
        ("MilestoneDefinition", "ObjectiveDefinition"),
        ("PrototypeMilestones", "ObjectiveCatalogue"),
        ("SpaceProgramState", "AgencyState"),
        ("RivalProgramsSaveState", "RivalAgenciesSaveState"),
        ("FundingProgramme", "SatelliteNetworkFundingContract"),
        ("StarterContractCriteria", "PreOrbitContractCriteria"),
        ("StarterContractLine", "PreOrbitContractLine"),
        ("StarterDefinitions", "PreOrbitDefinitions"),
        ("CreateStarterMilestone", "CreatePreOrbitObjective"),
        ("StarterRewardFundsPerLevel", "PreOrbitRewardFundsPerLevel"),
        ("StarterRivalProgressCostFundsPerLevel", "PreOrbitRivalProgressCostFundsPerLevel"),
        ("StarterLine", "PreOrbitLine"),
        ("StarterLevel", "PreOrbitLevel"),
        ("IsStarterContract", "IsPreOrbitContract"),
        ("AchievementFundingProgrammes", "ObjectiveFundingContracts"),
        ("achievementFundingProgrammes", "objectiveFundingContracts"),
        ("FundingProgrammes", "SatelliteNetworkFundingContracts"),
        ("fundingProgrammes", "satelliteNetworkFundingContracts"),
        ("UnlockProgramScope", "UnlockAgencyScope"),
        ("ProgramScope", "AgencyScope"),
        ("programScope", "agencyScope"),
        ("RequiredProgramCount", "RequiredAgencyCount"),
        ("requiredProgramCount", "requiredAgencyCount"),
        ("AnyAgencyAchievement", "AnyAgencyObjectiveCompletion"),
        ("MilestoneId", "ObjectiveId"),
        ("milestoneId", "objectiveId"),
        ("RecordedAchievements", "ObjectiveCompletionTimes"),
        ("ClearRecordedAchievements", "ClearObjectiveCompletions"),
        ("_achievementTimesById", "_objectiveCompletionTimesById"),
        ("HasAchievement", "HasCompletedObjective"),
        ("GetAchievementUniversalTime", "GetObjectiveCompletionTime"),
        ("RecordAchievement", "RecordObjectiveCompletion"),
        ("PlayerProgram", "PlayerAgency"),
        ("playerProgram", "playerAgency"),
        ("RivalPrograms", "RivalAgencies"),
        ("rivalPrograms", "rivalAgencies"),
        ("FindProgramById", "FindAgencyById"),
        ("LaunchProgressPercent", "MissionProgressPercent"),
        ("NextLaunchProgressCheckUniversalTime", "NextMissionProgressCheckUniversalTime"),
        ("GetRivalLaunchProgressCost", "GetRivalMissionProgressCost"),
        ("GetEstimatedRivalLaunchDays", "GetEstimatedRivalMissionDays"),
        ("UnlockConditionDefinition.Achievement", "UnlockConditionDefinition.ObjectiveCompletion"),
        ("UnlockConditionType.Achievement", "UnlockConditionType.ObjectiveCompletion"),
    ]
    regexes = [
        (r"\bMilestones\b", "Objectives"),
        (r"\bMilestone\b", "Objective"),
        (r"\bmilestones\b", "objectives"),
        (r"\bmilestone\b", "objective"),
        (r"\bAchievement\b", "ObjectiveCompletion"),
        (r"\bachievement\b", "objective completion"),
        (r"\bprograms\b", "agencies"),
        (r"\bprogram\b", "agency"),
    ]
    replace_text(CODE_ROOTS, replacements, regexes)

    assert_no_tokens(CODE_ROOTS, [
        "TheRaceForSpace.Competition",
        "TheRaceForSpace.Milestones",
        "TheRaceForSpace.Programs",
        "TheRaceForSpace.Simulation",
        "SatelliteRaceController",
        "MilestoneDefinition",
        "PrototypeMilestones",
        "SpaceProgramState",
        "AchievementFundingProgramme",
        "FundingProgramme",
        "RaceSettings",
        "StarterContractLine",
        "StarterContractCriteria",
        "HasAchievement",
        "RecordAchievement",
        "GetAchievementUniversalTime",
    ])
    verify_and_commit("refactor: clarify campaign domain naming")


def stage_tracking_names():
    file_names = [
        ("ActiveStarterContractPlan", "ActiveFlightContractPlan"),
        ("ActiveStarterEvaluation", "ActiveFlightContractEvaluation"),
        ("StarterTelemetryPlan", "FlightTelemetryPlan"),
        ("StarterFlightTracker", "FlightContractTracker"),
        ("ActiveVesselTrackingSnapshot", "ActiveVesselSnapshot"),
        ("VesselTrackingSnapshot", "OrbitingVesselSnapshot"),
        ("SatelliteTracker", "OrbitalVesselTracker"),
        ("KspVesselDiscovery", "KspVesselMonitor"),
        ("RaceRuntime", "ModRuntime"),
    ]
    rename_files_by_tokens(CODE_ROOTS, file_names)

    replacements = [
        ("ActiveStarterContractPlan", "ActiveFlightContractPlan"),
        ("activeStarterContractPlan", "activeFlightContractPlan"),
        ("ActiveStarterContracts", "ActiveFlightContracts"),
        ("activeStarterContracts", "activeFlightContracts"),
        ("StarterFlightTracker", "FlightContractTracker"),
        ("starterFlightTracker", "flightContractTracker"),
        ("StarterTelemetryRequirement", "FlightTelemetryRequirement"),
        ("starterTelemetryRequirement", "flightTelemetryRequirement"),
        ("StarterTelemetryPlan", "FlightTelemetryPlan"),
        ("starterTelemetryPlan", "flightTelemetryPlan"),
        ("StarterFlightState", "FlightContractTrackingState"),
        ("starterFlightState", "flightContractTrackingState"),
        ("RefreshStarterFlightState", "RefreshFlightContractState"),
        ("ActiveVesselTrackingSnapshot", "ActiveVesselSnapshot"),
        ("TrackedFlightSituation", "FlightSituation"),
        ("VesselTrackingSnapshot", "OrbitingVesselSnapshot"),
        ("TrackedVesselType", "OrbitalVesselType"),
        ("SatelliteTracker", "OrbitalVesselTracker"),
        ("KspVesselDiscovery", "KspVesselMonitor"),
        ("RaceRuntime", "ModRuntime"),
        ("raceRuntime", "modRuntime"),
        ("TryCaptureActiveVessel", "TryCaptureActiveVesselSnapshot"),
        ("TryCaptureOrbitingVessels", "TryCaptureOrbitingVesselSnapshots"),
        ("RefreshPlayerSatelliteCounts", "RefreshOrbitalProgress"),
        ("starterTelemetryPlanSource", "flightTelemetryPlanSource"),
        ("starterTelemetryRequirements", "flightTelemetryRequirements"),
        ("starterTelemetry", "flightTelemetry"),
    ]
    replace_text(CODE_ROOTS, replacements)

    # Any remaining Starter identifiers describe the current Kerbin pre-orbit family,
    # not the generic 1-second active-vessel tracking infrastructure.
    replace_text(
        CODE_ROOTS,
        [("Starter", "PreOrbit")],
        [
            (r"\bstarter contracts\b", "pre-orbit contracts"),
            (r"\bstarter contract\b", "pre-orbit contract"),
            (r"\bstarter objectives\b", "pre-orbit objectives"),
            (r"\bstarter objective\b", "pre-orbit objective"),
        ],
    )

    # The generic flight-contract names above must stay generic after the broad PreOrbit cleanup.
    replace_text(CODE_ROOTS, [
        ("PreOrbitFlightContractTracker", "FlightContractTracker"),
        ("PreOrbitFlightTelemetryRequirement", "FlightTelemetryRequirement"),
        ("PreOrbitFlightTelemetryPlan", "FlightTelemetryPlan"),
        ("PreOrbitActiveFlightContracts", "ActiveFlightContracts"),
        ("PreOrbitActiveFlightContractPlan", "ActiveFlightContractPlan"),
        ("PreOrbitFlightContractTrackingState", "FlightContractTrackingState"),
    ])

    assert_no_tokens(CODE_ROOTS, [
        "RaceRuntime",
        "StarterFlightTracker",
        "StarterTelemetryRequirement",
        "StarterTelemetryPlan",
        "ActiveVesselTrackingSnapshot",
        "TrackedFlightSituation",
        "VesselTrackingSnapshot",
        "TrackedVesselType",
        "SatelliteTracker",
        "KspVesselDiscovery",
        "ActiveStarterContracts",
    ])
    verify_and_commit("refactor: generalize flight and orbital tracking names")


def stage_persistence_ui_config_names():
    file_names = [
        ("ActiveContractProgressSaveState", "FlightContractProgressSaveState"),
        ("FundingContractsSaveState", "CampaignFundingSaveState"),
        ("RivalProgramsSaveState", "RivalAgenciesSaveState"),
        ("RacePersistenceScenario", "ModPersistenceScenario"),
        ("RaceSettingsLoader", "CampaignSettingsLoader"),
        ("RaceWindow", "CommandCenterWindow"),
    ]
    rename_files_by_tokens(CODE_ROOTS, file_names)
    move_path(
        "GameData/TheRaceForSpace/Config/RaceSettings.cfg",
        "GameData/TheRaceForSpace/Config/CampaignSettings.cfg",
    )

    replacements = [
        ("ActiveContractProgressSaveState", "FlightContractProgressSaveState"),
        ("activeContractProgressSaveState", "flightContractProgressSaveState"),
        ("FundingContractsSaveState", "CampaignFundingSaveState"),
        ("fundingContractsSaveState", "campaignFundingSaveState"),
        ("RivalProgramsSaveState", "RivalAgenciesSaveState"),
        ("rivalProgramsSaveState", "rivalAgenciesSaveState"),
        ("RacePersistenceScenario", "ModPersistenceScenario"),
        ("racePersistenceScenario", "modPersistenceScenario"),
        ("RaceSettingsLoader", "CampaignSettingsLoader"),
        ("raceSettingsLoader", "campaignSettingsLoader"),
        ("RaceWindow", "CommandCenterWindow"),
        ("raceWindow", "commandCenterWindow"),
        ("SpaceRace", "ContractCatalogue"),
        ("spaceRace", "contractCatalogue"),
        ("Space Race", "Contract Catalogue"),
        ("DrawPreOrbitFundingLiveProgress", "DrawFlightContractLiveProgress"),
        ("DrawPreOrbitLiveProgress", "DrawFlightContractProgress"),
        ("CaptureActiveContractProgress", "CaptureFlightContractProgress"),
        ("TryRestoreActiveContractProgress", "TryRestoreFlightContractProgress"),
        ("CaptureRivalState", "CaptureRivalAgencyState"),
        ("TryRestoreRivalState", "TryRestoreRivalAgencyState"),
        ("raceController", "campaignController"),
        ("RaceController", "CampaignController"),
        ("RaceSettingsConfigPath", "CampaignSettingsConfigPath"),
        ("RaceSettings.cfg", "CampaignSettings.cfg"),
        ("FUNDING_CONTRACTS", "CAMPAIGN_FUNDING"),
        ("ACTIVE_CONTRACT_PROGRESS", "FLIGHT_CONTRACT_PROGRESS"),
        ("PLAYER_ACHIEVEMENT", "PLAYER_OBJECTIVE_COMPLETION"),
        ("ACHIEVEMENT_CONTRACT", "OBJECTIVE_FUNDING_CONTRACT"),
        ("SATELLITE_CONTRACT", "SATELLITE_NETWORK_FUNDING_CONTRACT"),
        ("RIVALS", "RIVAL_AGENCIES"),
        ("MILESTONE_ID", "OBJECTIVE_ID"),
        ("PROGRAM_ID", "AGENCY_ID"),
        ("LAUNCH_PROGRESS_PERCENT", "MISSION_PROGRESS_PERCENT"),
        ("NEXT_LAUNCH_PROGRESS_CHECK_UT", "NEXT_MISSION_PROGRESS_CHECK_UT"),
    ]
    replace_text(CODE_ROOTS + [ROOT / "src/TheRaceForSpace/TheRaceForSpace.csproj"], replacements)
    replace_text([ROOT / "GameData/TheRaceForSpace/Config/CampaignSettings.cfg"], replacements)

    assert_no_tokens(CODE_ROOTS, [
        "ActiveContractProgressSaveState",
        "FundingContractsSaveState",
        "RivalProgramsSaveState",
        "RacePersistenceScenario",
        "RaceSettingsLoader",
        "RaceWindow",
        "SpaceRace",
        "RaceSettings.cfg",
        "ACTIVE_CONTRACT_PROGRESS",
        "PLAYER_ACHIEVEMENT",
        "ACHIEVEMENT_CONTRACT",
    ])
    verify_and_commit("refactor: clarify persistence ui and config naming")


def stage_docs():
    replacements = [
        ("Competition/", "Campaign/"),
        ("Milestones/", "Objectives/"),
        ("Programs/", "Agencies/"),
        ("Simulation/", "Rivals/"),
        ("SatelliteRaceController", "CampaignController"),
        ("RaceRuntime", "ModRuntime"),
        ("RaceSettings", "CampaignSettings"),
        ("RaceBodySettings", "BodyBalanceSettings"),
        ("SpaceProgramState", "AgencyState"),
        ("MilestoneDefinition", "ObjectiveDefinition"),
        ("PrototypeMilestones", "ObjectiveCatalogue"),
        ("AchievementFundingProgramme", "ObjectiveFundingContract"),
        ("FundingProgramme", "SatelliteNetworkFundingContract"),
        ("PrototypeFundingCatalogue", "FundingContractCatalogue"),
        ("StarterFlightTracker", "FlightContractTracker"),
        ("StarterTelemetryRequirement", "FlightTelemetryRequirement"),
        ("StarterTelemetryPlan", "FlightTelemetryPlan"),
        ("ActiveVesselTrackingSnapshot", "ActiveVesselSnapshot"),
        ("VesselTrackingSnapshot", "OrbitingVesselSnapshot"),
        ("SatelliteTracker", "OrbitalVesselTracker"),
        ("KspVesselDiscovery", "KspVesselMonitor"),
        ("ActiveContractProgressSaveState", "FlightContractProgressSaveState"),
        ("FundingContractsSaveState", "CampaignFundingSaveState"),
        ("RivalProgramsSaveState", "RivalAgenciesSaveState"),
        ("RacePersistenceScenario", "ModPersistenceScenario"),
        ("RaceSettingsLoader", "CampaignSettingsLoader"),
        ("RaceWindow", "CommandCenterWindow"),
        ("RaceSettings.cfg", "CampaignSettings.cfg"),
        ("Space Race", "Contract Catalogue"),
        ("starter contracts", "pre-orbit contracts"),
        ("starter contract", "pre-orbit contract"),
        ("starter milestones", "pre-orbit objectives"),
        ("starter milestone", "pre-orbit objective"),
        ("milestones", "objectives"),
        ("milestone", "objective"),
        ("space programs", "agencies"),
        ("space program", "agency"),
    ]
    replace_text(ACTIVE_DOCS, replacements)

    overview = ROOT / "docs" / "CODE_OVERVIEW.md"
    write_text(overview, """# Code Overview

This is the quickest map of how the mod is put together.

## Overall flow

```text
KSP / Unity
    |
    v
ModRuntime
    |
    +--> KspIntegration reads KSP state
    |
    +--> FlightContractTracker checks contracts that need live active-vessel data
    +--> OrbitalVesselTracker checks orbital objectives and satellite-network counts
    |
    v
CampaignController
    |
    +--> Agencies stores player and rival state
    +--> Objectives defines what can be completed and how it unlocks
    +--> Funding stores funding contracts and payouts
    +--> Rivals advances rival agencies
    |
    +--> Persistence saves campaign and flight-progress state
    +--> UI reads and displays the state
```

`ModRuntime` drives when work happens. `CampaignController` coordinates campaign state. The UI only reads state and never advances progression.

## Source folders

| Folder | Purpose |
| --- | --- |
| `Core/` | Runtime scheduling and campaign-wide settings. |
| `Campaign/` | Main campaign coordination: offers, unlocks, funding reviews and rival updates. |
| `Agencies/` | State for the player agency and rival agencies. |
| `Objectives/` | Objective definitions and unlock-rule evaluation. |
| `Funding/` | One-off objective funding and recurring satellite-network funding. |
| `Rivals/` | Rival mission selection, spending and progress. |
| `Tracking/` | KSP-independent flight-contract and orbital-vessel evaluation. |
| `Persistence/` | Saveable project-owned state. |
| `KspIntegration/` | Direct KSP API access, vessel capture, events, config loading and ScenarioModule save hooks. |
| `UI/` | Command Center presentation. |

## Main classes

### `ModRuntime`
Runs the mod's timed work. It samples active-vessel flight contracts every second, refreshes campaign progression on the regular controller cadence, and performs the slower orbital vessel scan separately.

### `CampaignController`
Coordinates the campaign. It owns the player/rival agency collections, funding-contract state, unlock progression, sponsor reviews, rival updates and the cached set of active flight contracts.

### `AgencyState`
Stores the state of one player or rival agency: completed objectives, funds, qualifying satellite counts and rival mission progress.

### `ObjectiveDefinition`
Defines one objective and the requirements for completing it. Examples include Directed Power I, Control III and Probe Orbit.

### `ObjectiveCatalogue`
Creates the current objective definitions and their stable IDs. The four current Kerbin lines are identified as pre-orbit objectives.

### `FlightContractTracker`
Evaluates contracts that require frequent data from the currently controlled vessel. The current users are the four pre-orbit Kerbin lines, but the tracking path is intentionally generic so future Mun, Minmus or other active-flight contracts can use the same one-second system.

### `OrbitalVesselTracker`
Uses the slower vessel scan to record orbital objectives and update qualifying satellite-network counts.

### `FundingContractCatalogue`
Builds the funding-contract state associated with objectives and satellite networks.

### `ObjectiveFundingContract`
Funding lifecycle for a one-off objective.

### `SatelliteNetworkFundingContract`
Funding lifecycle for a repeatable satellite-network target.

### `RivalSimulation`
Chooses valid rival missions, spends rival funds and advances mission progress.

### `ModPersistenceScenario`
Connects KSP save/load to `CampaignFundingSaveState`, `RivalAgenciesSaveState` and `FlightContractProgressSaveState`.

### `CommandCenterWindow`
Draws the Command Center and reads current campaign/flight state for display.

## Two vessel-checking paths

### Flight contracts - fast path
Used when a contract depends on the currently controlled vessel and needs frequent telemetry. `KspVesselMonitor` captures only the fields requested by `FlightTelemetryPlan`, then `FlightContractTracker` evaluates them.

The current pre-orbit Directed Power, Mass, Control and Biome contracts use this path. Future active-flight contracts on Mun, Minmus or other bodies can use it too.

### Orbital tracking - slower path
Used to inspect loaded and unloaded vessels for orbital objectives and satellite-network counts. `KspVesselMonitor` creates `OrbitingVesselSnapshot` values and `OrbitalVesselTracker` evaluates them.

## How a current pre-orbit contract completes

1. `ObjectiveCatalogue` defines the objective and its measurable requirements.
2. `FundingContractCatalogue` creates its `ObjectiveFundingContract`.
3. `CampaignController` decides whether the funding contract is currently offered.
4. The offered objective becomes part of `ActiveFlightContracts`.
5. `ModRuntime` asks `KspVesselMonitor` for the telemetry needed by those active contracts.
6. `FlightContractTracker` evaluates the active vessel against the objective.
7. `AgencyState.RecordObjectiveCompletion()` records completion for the player.
8. `CampaignController` updates unlocks, funding and rival state on its normal refresh.
9. Persistence saves the changed state and the Command Center displays it.

## Where to make common changes

- Add/change an objective: `Objectives/ObjectiveCatalogue.cs`.
- Change a current active-flight completion rule: `Tracking/FlightContractTracker.cs`.
- Change which live vessel values are collected: `KspIntegration/KspVesselMonitor.cs`.
- Change unlock logic: `Objectives/UnlockRuleEvaluator.cs`.
- Change offers, sponsor reviews or campaign coordination: `Campaign/CampaignController.cs`.
- Change one-off objective funding: `Funding/ObjectiveFundingContract.cs`.
- Change satellite-network funding: `Funding/SatelliteNetworkFundingContract.cs` and `FundingContractCatalogue.cs`.
- Change rival behaviour: `Rivals/RivalSimulation.cs`.
- Change the Command Center: `UI/CommandCenterWindow.cs`.
- Change save/restore behaviour: `Persistence/` and `KspIntegration/ModPersistenceScenario.cs`.

## Boundaries to keep

- Direct KSP API access stays in `KspIntegration/` where practical.
- Tracking consumes project-owned snapshots rather than raw KSP vessel objects.
- `CampaignController` coordinates gameplay state but does not query KSP vessels directly.
- UI displays state but does not advance gameplay.
- Flight-contract tracking is generic; `PreOrbit` names are reserved for the current Kerbin contract family.
""")

    run("git", "diff", "--check")
    run("git", "add", "-A")
    status = subprocess.run(["git", "diff", "--cached", "--quiet"], cwd=ROOT, check=False)
    if status.returncode != 0:
        run("git", "commit", "-m", "docs: align architecture guide with clearer naming")
        run("git", "push", "origin", f"HEAD:{BRANCH}")


def main():
    run("git", "config", "user.name", "github-actions[bot]")
    run("git", "config", "user.email", "41898282+github-actions[bot]@users.noreply.github.com")
    stage_domain_names()
    stage_tracking_names()
    stage_persistence_ui_config_names()
    stage_docs()
    print("Naming refactor completed successfully.")


if __name__ == "__main__":
    main()

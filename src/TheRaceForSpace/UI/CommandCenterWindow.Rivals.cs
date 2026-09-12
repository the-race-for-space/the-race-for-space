using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Rivals;
using UnityEngine;

namespace TheRaceForSpace.UI
{
    /// <summary>
    /// Rival-programme presentation for the single Command Center window. Gameplay ownership remains
    /// in CampaignController/Rivals; this partial class only derives display text from authoritative state.
    /// </summary>
    public sealed partial class CommandCenterWindow
    {
        private static readonly GUILayoutOption[] RivalStatusColumnOptions = { GUILayout.Width(405.0f) };
        private static readonly GUILayoutOption[] RivalLiveItemOptions = { GUILayout.Width(155.0f) };
        private static readonly GUILayoutOption[] RivalLiveLocationOptions = { GUILayout.Width(145.0f) };
        private static readonly GUILayoutOption[] RivalLiveTypeOptions = { GUILayout.Width(75.0f) };
        private static readonly GUILayoutOption[] RivalLiveDurationOptions = { GUILayout.Width(70.0f) };
        private static readonly GUILayoutOption[] RivalLiveProgressOptions = { GUILayout.Width(105.0f) };
        private static readonly GUILayoutOption[] RivalLiveEtaOptions = { GUILayout.Width(70.0f) };
        private static readonly GUILayoutOption[] RivalLiveChanceOptions = { GUILayout.Width(95.0f) };
        private static readonly GUILayoutOption[] RivalFacilityNameOptions = { GUILayout.Width(190.0f) };
        private static readonly GUILayoutOption[] RivalFundingLabelOptions = { GUILayout.Width(320.0f) };
        private static readonly GUILayoutOption[] RivalFundingAmountOptions = { GUILayout.Width(125.0f) };
        private static readonly GUILayoutOption[] RivalTechTreeButtonOptions =
            { GUILayout.Width(180.0f), GUILayout.Height(26.0f) };

        private readonly Dictionary<string, bool> _rivalFullTechTreeExpandedByAgencyId =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private void DrawRivalAgencies()
        {
            GUILayout.Label("Rival Agencies - " + FormatNextFundingDate());
            GUILayout.Space(8.0f);

            _rivalsScrollPosition = GUILayout.BeginScrollView(_rivalsScrollPosition);

            if (_campaignController.RivalAgencies.Count == 0)
            {
                GUILayout.Label("No rival agencies are configured.");
            }

            double currentUniversalTime = GetRivalUiCurrentUniversalTime();
            for (int agencyIndex = 0; agencyIndex < _campaignController.RivalAgencies.Count; agencyIndex++)
            {
                AgencyState agency = _campaignController.RivalAgencies[agencyIndex];
                if (agencyIndex > 0)
                {
                    GUILayout.Space(12.0f);
                }

                DrawRivalProgramCard(agency, currentUniversalTime);
            }

            GUILayout.EndScrollView();
        }

        private void DrawRivalProgramCard(AgencyState agency, double currentUniversalTime)
        {
            GUILayout.BeginVertical("box");
            DrawCenteredCardTitle(agency == null ? "Unknown Rival Agency" : agency.Name);

            if (agency == null || agency.RivalProgram == null)
            {
                GUILayout.Label("Programme state unavailable.");
                GUILayout.EndVertical();
                return;
            }

            DrawRivalProgrammeStatus(agency);
            GUILayout.Space(12.0f);
            DrawRivalLiveMissionProgress(agency, currentUniversalTime);
            GUILayout.Space(12.0f);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(RivalStatusColumnOptions);
            DrawRivalContractPreparation(agency);
            GUILayout.EndVertical();

            GUILayout.Space(12.0f);
            GUILayout.BeginVertical(RivalStatusColumnOptions);
            DrawRivalSciencePreparation(agency);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(12.0f);
            DrawRivalConstruction(agency, currentUniversalTime);
            GUILayout.Space(12.0f);
            DrawRivalFacilities(agency);
            GUILayout.Space(12.0f);
            DrawRivalTechAndResearch(agency, currentUniversalTime);
            GUILayout.Space(12.0f);
            DrawRivalFunding(agency);

            GUILayout.EndVertical();
        }

        private void DrawRivalProgrammeStatus(AgencyState agency)
        {
            RivalProgramState programme = agency.RivalProgram;
            int rosterLimit = RivalDevelopmentSimulation.GetKerbalRosterLimit(agency);
            int kerbalsOnMission = RivalDevelopmentSimulation.GetKerbalsOnMission(agency);
            int kerbalsAvailable = RivalDevelopmentSimulation.GetKerbalsAvailable(agency);
            int satelliteCapacityUsed = RivalLiveMissionSimulation.GetSatelliteCapacityUsed(
                agency,
                _campaignController.SatelliteNetworkFundingContracts);
            int satelliteLimit = RivalDevelopmentSimulation.GetSatelliteLimit(agency);

            GUILayout.Label("Programme Status", _boldLabelStyle);
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(RivalStatusColumnOptions);
            GUILayout.Label("Funds: " + agency.Funds.ToString("N0"));
            GUILayout.Label("Stored Science: " + programme.StoredScience.ToString("N1"));
            GUILayout.Label(
                "Kerbals Employed: "
                + Math.Max(0, programme.KerbalsEmployed)
                + " / "
                + FormatLimit(rosterLimit));
            GUILayout.Label("Kerbals On Mission: " + kerbalsOnMission);
            GUILayout.EndVertical();

            GUILayout.Space(12.0f);
            GUILayout.BeginVertical(RivalStatusColumnOptions);
            GUILayout.Label(
                "Satellites: "
                + satelliteCapacityUsed
                + " / "
                + FormatLimit(satelliteLimit));
            GUILayout.Label("Kerbals Available: " + kerbalsAvailable);
            GUILayout.Label(
                "Next Kerbal Payroll: "
                + RivalDevelopmentSimulation.GetKerbalPayrollFunds(agency).ToString("N0")
                + " Funds");
            GUILayout.Label("Total Next Payout: " + agency.NextPayoutFunds.ToString("N0"));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawRivalLiveMissionProgress(AgencyState agency, double currentUniversalTime)
        {
            IList<RivalLiveMissionState> liveMissions = agency.RivalProgram.LiveMissions;

            GUILayout.Label("Live Mission Progress", _boldLabelStyle);
            if (liveMissions.Count == 0)
            {
                GUILayout.Label("None");
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Item", _boldLabelStyle, RivalLiveItemOptions);
            GUILayout.Label("Location", _boldLabelStyle, RivalLiveLocationOptions);
            GUILayout.Label("Mission Type", _boldLabelStyle, RivalLiveTypeOptions);
            GUILayout.Label("Duration", _boldLabelStyle, RivalLiveDurationOptions);
            GUILayout.Label("Progress", _boldLabelStyle, RivalLiveProgressOptions);
            GUILayout.Label("ETA", _boldLabelStyle, RivalLiveEtaOptions);
            GUILayout.Label("Success Chance", _boldLabelStyle, RivalLiveChanceOptions);
            GUILayout.EndHorizontal();

            for (int missionIndex = 0; missionIndex < liveMissions.Count; missionIndex++)
            {
                RivalLiveMissionState mission = liveMissions[missionIndex];
                if (mission == null)
                {
                    continue;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(GetRivalLiveMissionItem(mission), RivalLiveItemOptions);
                GUILayout.Label(GetRivalLiveMissionLocation(mission), RivalLiveLocationOptions);
                GUILayout.Label(mission.MissionType.ToString(), RivalLiveTypeOptions);
                GUILayout.Label(FormatDays(mission.DurationDays), RivalLiveDurationOptions);
                GUILayout.Label(
                    FormatLiveMissionProgress(mission, currentUniversalTime),
                    RivalLiveProgressOptions);
                GUILayout.Label(
                    FormatRemainingDays(mission.CompletionUniversalTime, currentUniversalTime),
                    RivalLiveEtaOptions);
                GUILayout.Label(
                    mission.SuccessChancePercent.ToString("0.#") + "%",
                    RivalLiveChanceOptions);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawRivalContractPreparation(AgencyState agency)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Current Launch Programme", _boldLabelStyle);

            string targetName = RivalSimulation.GetMissionTargetDisplayName(
                agency.NextMissionTargetId,
                _campaignController.ObjectiveFundingContracts,
                _campaignController.SatelliteNetworkFundingContracts);
            if (string.IsNullOrEmpty(targetName))
            {
                targetName = string.IsNullOrEmpty(agency.NextMissionDisplayName)
                    ? "Planning"
                    : agency.NextMissionDisplayName;
            }

            int progressPercent = Math.Max(0, Math.Min(100, agency.MissionProgressPercent));
            int requiredKerbals = GetRivalContractRequiredKerbals(agency.NextMissionTargetId);
            double progressChance = RivalDevelopmentSimulation.GetNormalLaunchProgressChance(agency) * 100.0;
            double progressCost = _campaignController.GetRivalMissionProgressCost(agency);
            int? estimatedLaunchDays = _campaignController.GetEstimatedRivalMissionDays(agency);

            GUILayout.Label("Next Mission: " + targetName);
            GUILayout.Label("Launch Progress: " + progressPercent + "%");
            GUILayout.Label(
                "Progress Chance - each "
                + FormatConfiguredInterval(CampaignSettings.RivalNormalLaunchProgressCheckIntervalDays)
                + ": "
                + progressChance.ToString("0.#")
                + "%");
            GUILayout.Label("Progress Cost: " + progressCost.ToString("N0") + " Funds");
            GUILayout.Label("Estimated Launch: " + FormatEstimatedLaunchDays(estimatedLaunchDays));
            GUILayout.Label("Kerbals Required: " + requiredKerbals);
            DrawRivalPreparationCrewState(agency, requiredKerbals, progressPercent);

            GUILayout.EndVertical();
        }

        private void DrawRivalSciencePreparation(AgencyState agency)
        {
            RivalProgramState programme = agency.RivalProgram;
            ScienceLaunchPreparationState preparation = programme.ScienceLaunchPreparation;

            GUILayout.BeginVertical("box");
            GUILayout.Label("Launch Science Expedition", _boldLabelStyle);

            if (preparation == null || preparation.Subject == null)
            {
                GUILayout.Label("Current Target: Planning");
                GUILayout.EndVertical();
                return;
            }

            int progressPercent = Math.Max(0, Math.Min(100, preparation.LaunchProgressPercent));
            int requiredKerbals = Math.Max(0, preparation.RequiredKerbals);
            int availableKerbals = RivalDevelopmentSimulation.GetKerbalsAvailable(agency);
            string unavailableSuffix = progressPercent >= 100
                && requiredKerbals > 0
                && availableKerbals <= 0
                    ? " - none available"
                    : string.Empty;
            double progressChance = RivalDevelopmentSimulation.GetScienceLaunchProgressChance(agency) * 100.0;
            double? estimatedLaunchDays = RivalScienceSimulation.CalculateEstimatedLaunchDays(agency);

            GUILayout.Label(
                "Experiment: "
                + GetScienceExperimentDisplayName(preparation.Subject.ExperimentId)
                + ", "
                + EmptyAsNone(preparation.Subject.BodyName));
            GUILayout.Label(
                "Situation: "
                + FormatScienceSituation(preparation.Subject.Situation)
                + (string.IsNullOrEmpty(preparation.Subject.BiomeName)
                    ? string.Empty
                    : ", " + preparation.Subject.BiomeName));
            GUILayout.Label("Launch Progress: " + progressPercent + "%");
            GUILayout.Label(
                "Progress Chance - daily: "
                + progressChance.ToString("0.#")
                + "%");
            GUILayout.Label("Estimated Launch: " + FormatEstimatedLaunchDays(estimatedLaunchDays));
            GUILayout.Label("Science Reward: " + preparation.PlannedScienceReward.ToString("0.#"));
            GUILayout.Label(
                "Kerbal Assigned: "
                + availableKerbals
                + " available / "
                + requiredKerbals
                + " required"
                + unavailableSuffix);

            GUILayout.EndVertical();
        }

        private void DrawRivalPreparationCrewState(
            AgencyState agency,
            int requiredKerbals,
            int progressPercent)
        {
            int availableKerbals = RivalDevelopmentSimulation.GetKerbalsAvailable(agency);
            if (progressPercent < 100)
            {
                GUILayout.Label(
                    "Crew Readiness: "
                    + availableKerbals
                    + " available / "
                    + Math.Max(0, requiredKerbals)
                    + " required");
                return;
            }

            string readiness;
            if (requiredKerbals <= 0 || availableKerbals >= requiredKerbals)
            {
                readiness = "Ready";
            }
            else if (RivalDevelopmentSimulation.CanHireMissingKerbals(agency, requiredKerbals))
            {
                readiness = "Hire At Launch";
            }
            else
            {
                readiness = "Waiting For Kerbal";
            }

            GUILayout.Label("Crew Status: " + readiness, _boldLabelStyle);
        }

        private void DrawRivalConstruction(AgencyState agency, double currentUniversalTime)
        {
            IList<RivalFacilityConstructionState> constructionProjects =
                agency.RivalProgram.FacilityConstruction;

            GUILayout.Label("Construction", _boldLabelStyle);
            if (constructionProjects.Count == 0)
            {
                GUILayout.Label("No construction underway.");
                return;
            }

            for (int projectIndex = 0; projectIndex < constructionProjects.Count; projectIndex++)
            {
                RivalFacilityConstructionState construction = constructionProjects[projectIndex];
                if (construction == null)
                {
                    continue;
                }

                double totalDays = Math.Max(
                    0.0,
                    (construction.CompletionUniversalTime - construction.StartUniversalTime) / KerbinDaySeconds);
                double elapsedDays = currentUniversalTime < 0.0
                    ? 0.0
                    : Math.Max(
                        0.0,
                        Math.Min(
                            totalDays,
                            (currentUniversalTime - construction.StartUniversalTime) / KerbinDaySeconds));
                double remainingDays = Math.Max(0.0, totalDays - elapsedDays);

                GUILayout.BeginVertical("box");
                GUILayout.Label(
                    GetRivalFacilityDisplayName(construction.Facility)
                    + ": Level "
                    + construction.SourceLevel
                    + " -> Level "
                    + construction.TargetLevel,
                    _boldLabelStyle);
                GUILayout.Label("Elapsed: " + FormatDays(elapsedDays));
                GUILayout.Label(
                    "Remaining: "
                    + FormatDays(remainingDays)
                    + " - ETA "
                    + FormatKerbinDate(construction.CompletionUniversalTime));
                GUILayout.Label("Paid: " + construction.CostPaidFunds.ToString("N0") + " Funds");
                GUILayout.EndVertical();
            }
        }

        private void DrawRivalFacilities(AgencyState agency)
        {
            GUILayout.Label("Facilities", _boldLabelStyle);

            RivalFacilityType[] facilities =
            {
                RivalFacilityType.Administration,
                RivalFacilityType.AstronautComplex,
                RivalFacilityType.MissionControl,
                RivalFacilityType.ResearchAndDevelopment,
                RivalFacilityType.VehicleAssemblyBuilding,
                RivalFacilityType.LaunchPad,
                RivalFacilityType.SpaceplaneHangar,
                RivalFacilityType.Runway,
                RivalFacilityType.TrackingStation
            };

            for (int facilityIndex = 0; facilityIndex < facilities.Length; facilityIndex++)
            {
                RivalFacilityType facility = facilities[facilityIndex];
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    GetRivalFacilityDisplayName(facility)
                    + " - Level "
                    + RivalDevelopmentSimulation.GetFacilityLevel(agency, facility),
                    _boldLabelStyle,
                    RivalFacilityNameOptions);
                GUILayout.Label(GetRivalFacilityCapabilityText(agency, facility));
                GUILayout.EndHorizontal();
            }
        }

        private void DrawRivalTechAndResearch(AgencyState agency, double currentUniversalTime)
        {
            RivalProgramState programme = agency.RivalProgram;

            GUILayout.Label("Tech Tree / Research", _boldLabelStyle);
            GUILayout.Label("Stored Science: " + programme.StoredScience.ToString("N1"));

            RivalResearchProjectState research = programme.CurrentResearch;
            if (research == null)
            {
                GUILayout.Label("Next Research Project: None");
            }
            else
            {
                RivalTechNodeDefinition definition = RivalTechCatalogue.GetById(research.TechId);
                string displayName = definition == null || string.IsNullOrEmpty(definition.DisplayName)
                    ? research.TechId
                    : definition.DisplayName;
                string researchStatus = currentUniversalTime >= 0.0
                    && currentUniversalTime >= research.ResearchReadyUniversalTime
                    ? "Ready - completes at funding boundary"
                    : "Researching";

                GUILayout.BeginVertical("box");
                GUILayout.Label("Next Research Project: " + displayName, _boldLabelStyle);
                GUILayout.Label("Science Cost: " + research.ScienceCostPaid.ToString("0.#") + " - Paid");
                GUILayout.Label("Status: " + researchStatus);
                GUILayout.Label("Started: " + FormatKerbinDate(research.StartUniversalTime));
                GUILayout.Label(
                    "Eligible Completion: "
                    + FormatKerbinDate(research.EligibleCompletionFundingUniversalTime)
                    + " ("
                    + FormatRemainingDays(research.EligibleCompletionFundingUniversalTime, currentUniversalTime)
                    + ")");
                GUILayout.EndVertical();
            }

            GUILayout.Space(6.0f);
            GUILayout.Label("Researched Tech", _boldLabelStyle);
            bool hasResearchedTech = false;
            for (int techIndex = 0; techIndex < RivalTechCatalogue.All.Count; techIndex++)
            {
                RivalTechNodeDefinition tech = RivalTechCatalogue.All[techIndex];
                if (tech == null
                    || !RivalTechCatalogue.ContainsTechId(programme.ResearchedTechIds, tech.Id))
                {
                    continue;
                }

                DrawRivalTechNode(tech, true);
                hasResearchedTech = true;
            }

            if (!hasResearchedTech)
            {
                GUILayout.Label("None");
            }

            GUILayout.Space(6.0f);
            bool fullTreeExpanded = IsRivalFullTechTreeExpanded(agency.Id);
            if (GUILayout.Button(
                fullTreeExpanded ? "Hide Full Tech Tree" : "Show Full Tech Tree",
                RivalTechTreeButtonOptions))
            {
                _rivalFullTechTreeExpandedByAgencyId[agency.Id] = !fullTreeExpanded;
                fullTreeExpanded = !fullTreeExpanded;
            }

            if (!fullTreeExpanded)
            {
                return;
            }

            GUILayout.Space(4.0f);
            for (int techIndex = 0; techIndex < RivalTechCatalogue.All.Count; techIndex++)
            {
                RivalTechNodeDefinition tech = RivalTechCatalogue.All[techIndex];
                if (tech != null)
                {
                    DrawRivalTechNode(
                        tech,
                        RivalTechCatalogue.ContainsTechId(programme.ResearchedTechIds, tech.Id));
                }
            }
        }

        private void DrawRivalTechNode(RivalTechNodeDefinition tech, bool researched)
        {
            _listTextBuilder.Length = 0;
            _listTextBuilder.Append(researched ? "[x] " : "[ ] ");
            _listTextBuilder.Append(tech.DisplayName);
            _listTextBuilder.Append(" - ");
            _listTextBuilder.Append(tech.ScienceCost.ToString("0.#"));
            _listTextBuilder.Append(" Science");
            GUILayout.Label(_listTextBuilder.ToString());

            if (tech.UnlockedExperimentIds.Count > 0)
            {
                _listTextBuilder.Length = 0;
                _listTextBuilder.Append("    Experiments: ");
                for (int experimentIndex = 0;
                    experimentIndex < tech.UnlockedExperimentIds.Count;
                    experimentIndex++)
                {
                    if (experimentIndex > 0)
                    {
                        _listTextBuilder.Append(", ");
                    }

                    _listTextBuilder.Append(
                        GetScienceExperimentDisplayName(tech.UnlockedExperimentIds[experimentIndex]));
                }
                GUILayout.Label(_listTextBuilder.ToString());
            }

            if (!researched && tech.PrerequisiteTechIds.Count > 0)
            {
                _listTextBuilder.Length = 0;
                _listTextBuilder.Append("    Requires: ");
                for (int prerequisiteIndex = 0;
                    prerequisiteIndex < tech.PrerequisiteTechIds.Count;
                    prerequisiteIndex++)
                {
                    if (prerequisiteIndex > 0)
                    {
                        _listTextBuilder.Append(tech.AnyPrerequisiteUnlocks ? " or " : ", ");
                    }

                    RivalTechNodeDefinition prerequisite =
                        RivalTechCatalogue.GetById(tech.PrerequisiteTechIds[prerequisiteIndex]);
                    _listTextBuilder.Append(
                        prerequisite == null
                            ? tech.PrerequisiteTechIds[prerequisiteIndex]
                            : prerequisite.DisplayName);
                }
                GUILayout.Label(_listTextBuilder.ToString());
            }
        }

        private void DrawRivalFunding(AgencyState agency)
        {
            double objectiveIncome = 0.0;
            double satelliteIncome = 0.0;

            GUILayout.Label("Funding", _boldLabelStyle);

            for (int contractIndex = 0;
                contractIndex < _campaignController.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract =
                    _campaignController.ObjectiveFundingContracts[contractIndex];
                double payout = _campaignController.GetObjectiveCurrentPayout(agency, contract);
                if (payout <= 0.0)
                {
                    continue;
                }

                objectiveIncome += payout;
                DrawRivalFundingRow(contract.Name + ": Completed", payout, false);
            }

            for (int contractIndex = 0;
                contractIndex < _campaignController.SatelliteNetworkFundingContracts.Count;
                contractIndex++)
            {
                SatelliteNetworkFundingContract contract =
                    _campaignController.SatelliteNetworkFundingContracts[contractIndex];
                double payout = _campaignController.GetSatelliteCurrentPayout(agency, contract);
                if (payout <= 0.0)
                {
                    continue;
                }

                satelliteIncome += payout;
                DrawRivalFundingRow(
                    contract.CelestialBodyName
                    + " Satellites: "
                    + agency.GetSatelliteCount(contract.CelestialBodyName),
                    payout,
                    false);
            }

            RivalFundingBreakdown fundingBreakdown =
                RivalDevelopmentSimulation.CalculateFundingBreakdown(
                    agency,
                    objectiveIncome,
                    satelliteIncome);

            GUILayout.Space(4.0f);
            DrawRivalFundingRow("Base Income", fundingBreakdown.BaseIncome, false);
            DrawRivalFundingRow("Gross Next Income", fundingBreakdown.GrossIncome, true);
            DrawRivalFundingRow("Kerbal Payroll", -fundingBreakdown.KerbalPayroll, false);
            DrawRivalFundingRow("Pending Insurance", -fundingBreakdown.InsuranceDeduction, false);
            DrawRivalFundingRow("Total Next Payout", fundingBreakdown.NetPayout, true);
        }

        private void DrawRivalFundingRow(string label, double amount, bool bold)
        {
            GUILayout.BeginHorizontal();
            if (bold)
            {
                GUILayout.Label(label, _boldLabelStyle, RivalFundingLabelOptions);
                GUILayout.Label(amount.ToString("N0"), _boldLabelStyle, RivalFundingAmountOptions);
            }
            else
            {
                GUILayout.Label(label, RivalFundingLabelOptions);
                GUILayout.Label(amount.ToString("N0"), RivalFundingAmountOptions);
            }
            GUILayout.EndHorizontal();
        }

        private int GetRivalContractRequiredKerbals(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                return 0;
            }

            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(targetId);
            if (objective != null)
            {
                return Math.Max(0, objective.RequiredKerbalCount);
            }

            for (int contractIndex = 0;
                contractIndex < _campaignController.SatelliteNetworkFundingContracts.Count;
                contractIndex++)
            {
                SatelliteNetworkFundingContract contract =
                    _campaignController.SatelliteNetworkFundingContracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    return Math.Max(0, contract.RequiredKerbalCount);
                }
            }

            return 0;
        }

        private string GetRivalLiveMissionItem(RivalLiveMissionState mission)
        {
            if (mission.MissionType == RivalMissionType.Science)
            {
                return mission.ScienceSubject == null
                    ? "Science"
                    : GetScienceExperimentDisplayName(mission.ScienceSubject.ExperimentId);
            }

            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(mission.ContractId);
            if (objective != null)
            {
                return objective.Name;
            }

            for (int contractIndex = 0;
                contractIndex < _campaignController.SatelliteNetworkFundingContracts.Count;
                contractIndex++)
            {
                SatelliteNetworkFundingContract contract =
                    _campaignController.SatelliteNetworkFundingContracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, mission.ContractId, StringComparison.OrdinalIgnoreCase))
                {
                    return contract.Name;
                }
            }

            return string.IsNullOrEmpty(mission.ContractId) ? "Contract" : mission.ContractId;
        }

        private static string GetRivalLiveMissionLocation(RivalLiveMissionState mission)
        {
            if (mission.MissionType == RivalMissionType.Science && mission.ScienceSubject != null)
            {
                if (!string.IsNullOrEmpty(mission.ScienceSubject.BiomeName))
                {
                    return mission.ScienceSubject.BodyName + " / " + mission.ScienceSubject.BiomeName;
                }

                if (!string.IsNullOrEmpty(mission.ScienceSubject.BodyName))
                {
                    return mission.ScienceSubject.BodyName;
                }
            }

            return FormatRivalLocationId(mission.LocationId);
        }

        private static string FormatLiveMissionProgress(
            RivalLiveMissionState mission,
            double currentUniversalTime)
        {
            if (currentUniversalTime < 0.0
                || mission.DurationDays <= 0.0
                || mission.LaunchUniversalTime < 0.0)
            {
                return "Pending";
            }

            double elapsedDays = Math.Max(
                0.0,
                Math.Min(
                    mission.DurationDays,
                    (currentUniversalTime - mission.LaunchUniversalTime) / KerbinDaySeconds));
            return elapsedDays.ToString("0.#") + " / " + mission.DurationDays.ToString("0.#") + " days";
        }

        private string GetRivalFacilityCapabilityText(
            AgencyState agency,
            RivalFacilityType facility)
        {
            switch (facility)
            {
                case RivalFacilityType.Administration:
                    return "Base funding: "
                        + RivalDevelopmentSimulation.GetAdministrationBaseIncomeFunds(agency).ToString("N0")
                        + " Funds";

                case RivalFacilityType.AstronautComplex:
                    return "Kerbal roster limit: "
                        + FormatLimit(RivalDevelopmentSimulation.GetKerbalRosterLimit(agency));

                case RivalFacilityType.MissionControl:
                    return "Satellite limit: "
                        + FormatLimit(RivalDevelopmentSimulation.GetSatelliteLimit(agency));

                case RivalFacilityType.ResearchAndDevelopment:
                    double scienceLimit = RivalDevelopmentSimulation.GetResearchScienceCostLimit(agency);
                    return scienceLimit <= 0.0
                        ? "No tech Science-cost ceiling"
                        : "Tech Science-cost ceiling: " + scienceLimit.ToString("0.#");

                case RivalFacilityType.VehicleAssemblyBuilding:
                case RivalFacilityType.LaunchPad:
                    return "Normal Launch Progress Chance total: "
                        + (RivalDevelopmentSimulation.GetNormalLaunchProgressChance(agency) * 100.0)
                            .ToString("0.#")
                        + "%";

                case RivalFacilityType.SpaceplaneHangar:
                case RivalFacilityType.Runway:
                    return "Science Launch Progress Chance total: "
                        + (RivalDevelopmentSimulation.GetScienceLaunchProgressChance(agency) * 100.0)
                            .ToString("0.#")
                        + "%";

                case RivalFacilityType.TrackingStation:
                    return "Destination access: " + RivalScienceSimulation.GetExpeditionRange(agency);

                default:
                    return string.Empty;
            }
        }

        private bool IsRivalFullTechTreeExpanded(string agencyId)
        {
            bool expanded;
            return !string.IsNullOrEmpty(agencyId)
                && _rivalFullTechTreeExpandedByAgencyId.TryGetValue(agencyId, out expanded)
                && expanded;
        }

        private static double GetRivalUiCurrentUniversalTime()
        {
            if (Planetarium.fetch == null)
            {
                return -1.0;
            }

            double universalTime = Planetarium.GetUniversalTime();
            return double.IsNaN(universalTime) || double.IsInfinity(universalTime) || universalTime < 0.0
                ? -1.0
                : universalTime;
        }

        private static string FormatLimit(int limit)
        {
            return limit <= 0 ? "Unlimited" : limit.ToString();
        }

        private static string FormatEstimatedLaunchDays(int? estimatedDays)
        {
            if (!estimatedDays.HasValue)
            {
                return "Awaiting Funding";
            }

            return estimatedDays.Value <= 0
                ? "Ready"
                : estimatedDays.Value + (estimatedDays.Value == 1 ? " day" : " days");
        }

        private static string FormatEstimatedLaunchDays(double? estimatedDays)
        {
            if (!estimatedDays.HasValue)
            {
                return "Unavailable";
            }

            double normalizedDays = Math.Max(0.0, estimatedDays.Value);
            return normalizedDays <= 0.0
                ? "Ready"
                : normalizedDays.ToString("0.#") + (normalizedDays == 1.0 ? " day" : " days");
        }

        private static string FormatConfiguredInterval(double intervalDays)
        {
            double normalizedDays = Math.Max(0.0, intervalDays);
            return normalizedDays.ToString("0.#") + (normalizedDays == 1.0 ? " day" : " days");
        }

        private static string FormatDays(double days)
        {
            double normalizedDays = Math.Max(0.0, days);
            return normalizedDays.ToString("0.#") + (normalizedDays == 1.0 ? " day" : " days");
        }

        private static string FormatRemainingDays(double targetUniversalTime, double currentUniversalTime)
        {
            if (targetUniversalTime < 0.0 || currentUniversalTime < 0.0)
            {
                return "Pending";
            }

            int remainingDays = (int)Math.Ceiling(
                Math.Max(0.0, targetUniversalTime - currentUniversalTime) / KerbinDaySeconds);
            return remainingDays + (remainingDays == 1 ? " day" : " days");
        }

        private static string GetScienceExperimentDisplayName(string experimentId)
        {
            switch (experimentId)
            {
                case RivalTechCatalogue.CrewReportExperimentId:
                    return "Crew Report";
                case RivalTechCatalogue.MysteryGooExperimentId:
                    return "Mystery Goo Observation";
                case RivalTechCatalogue.TemperatureScanExperimentId:
                    return "Temperature Scan";
                case RivalTechCatalogue.AtmosphericPressureScanExperimentId:
                    return "Atmospheric Pressure Scan";
                case RivalTechCatalogue.MaterialsStudyExperimentId:
                    return "Materials Study";
                case RivalTechCatalogue.EvaScienceExperimentId:
                    return "EVA Science";
                case RivalTechCatalogue.AtmosphereAnalysisExperimentId:
                    return "Atmosphere Analysis";
                case RivalTechCatalogue.InfraredTelescopeExperimentId:
                    return "SENTINEL Infrared Telescope";
                case RivalTechCatalogue.SeismicScanExperimentId:
                    return "Seismic Scan";
                case RivalTechCatalogue.MagnetometerReportExperimentId:
                    return "Magnetometer Report";
                case RivalTechCatalogue.GravityScanExperimentId:
                    return "Gravity Scan";
                case RivalScienceSimulation.EvaReportExperimentId:
                    return "EVA Report";
                case RivalScienceSimulation.SurfaceSampleExperimentId:
                    return "Surface Sample";
                default:
                    return string.IsNullOrEmpty(experimentId) ? "Unknown Experiment" : experimentId;
            }
        }

        private static string FormatScienceSituation(string situation)
        {
            switch (situation)
            {
                case "Landed":
                    return "Landed";
                case "Splashed":
                    return "Splashed";
                case "FlyingLow":
                    return "Flying Low";
                case "FlyingHigh":
                    return "Flying High";
                case "LowSpace":
                    return "Low Space";
                case "HighSpace":
                    return "High Space";
                default:
                    return EmptyAsNone(situation);
            }
        }

        private static string EmptyAsNone(string value)
        {
            return string.IsNullOrEmpty(value) ? "None" : value;
        }

        private static string GetRivalFacilityDisplayName(RivalFacilityType facility)
        {
            switch (facility)
            {
                case RivalFacilityType.Administration:
                    return "Administration Building";
                case RivalFacilityType.AstronautComplex:
                    return "Astronaut Complex";
                case RivalFacilityType.MissionControl:
                    return "Mission Control";
                case RivalFacilityType.ResearchAndDevelopment:
                    return "Research and Development";
                case RivalFacilityType.VehicleAssemblyBuilding:
                    return "VAB";
                case RivalFacilityType.LaunchPad:
                    return "Launch Pad";
                case RivalFacilityType.SpaceplaneHangar:
                    return "SPH";
                case RivalFacilityType.Runway:
                    return "Runway";
                case RivalFacilityType.TrackingStation:
                    return "Tracking Station";
                default:
                    return facility.ToString();
            }
        }

        private static string FormatRivalLocationId(string locationId)
        {
            if (string.IsNullOrEmpty(locationId))
            {
                return "Unknown";
            }

            if (string.Equals(
                locationId,
                CampaignSettings.RivalPreOrbitLocalLocationId,
                StringComparison.OrdinalIgnoreCase))
            {
                return "Kerbin / Local";
            }

            if (string.Equals(
                locationId,
                CampaignSettings.RivalKerbinOrbitLocationId,
                StringComparison.OrdinalIgnoreCase))
            {
                return "Kerbin Orbit";
            }

            const string BodyPrefix = "body:";
            const string KerbinPrefix = "kerbin:";
            const string KscPrefix = "ksc:";
            if (locationId.StartsWith(BodyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return locationId.Substring(BodyPrefix.Length);
            }
            if (locationId.StartsWith(KerbinPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return "Kerbin / " + FormatStableLocationWords(locationId.Substring(KerbinPrefix.Length));
            }
            if (locationId.StartsWith(KscPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return "KSC / " + FormatStableLocationWords(locationId.Substring(KscPrefix.Length));
            }

            return locationId;
        }

        private static string FormatStableLocationWords(string stableIdPart)
        {
            if (string.IsNullOrEmpty(stableIdPart))
            {
                return "Unknown";
            }

            if (string.Equals(stableIdPart, "r-and-d", StringComparison.OrdinalIgnoreCase))
            {
                return "R&D";
            }
            if (string.Equals(stableIdPart, "sph", StringComparison.OrdinalIgnoreCase))
            {
                return "SPH";
            }
            if (string.Equals(stableIdPart, "vab", StringComparison.OrdinalIgnoreCase))
            {
                return "VAB";
            }

            char[] characters = stableIdPart.Replace('-', ' ').Replace('_', ' ').ToCharArray();
            bool capitalizeNext = true;
            for (int characterIndex = 0; characterIndex < characters.Length; characterIndex++)
            {
                if (characters[characterIndex] == ' ')
                {
                    capitalizeNext = true;
                    continue;
                }

                if (capitalizeNext)
                {
                    characters[characterIndex] = char.ToUpperInvariant(characters[characterIndex]);
                    capitalizeNext = false;
                }
            }

            return new string(characters);
        }
    }
}

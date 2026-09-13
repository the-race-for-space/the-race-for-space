using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Campaign;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Rivals;
using UnityEngine;

namespace TheRaceForSpace.UI
{
    /// <summary>
    /// Publishes stock KSP inbox messages for important campaign events. Gameplay state remains owned
    /// by the campaign, rival, funding, and KSP-integration systems; this addon only observes live signals.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public sealed class FundingNotificationUI : MonoBehaviour
    {
        private const string PlayerCompletionTitlePrefix = "Funding Target Completed - ";
        private const string PlayerCompletionBodySuffix =
            " has been achieved. Your agency is now eligible for a share of the remaining contract funding.";
        private const string SponsorReviewTitle = "Sponsor Review Complete";
        private const string FundingPayoutTitle = "Campaign Funding Received";

        private sealed class PendingNotification
        {
            public PendingNotification(string title, string body)
            {
                Title = title;
                Body = body;
            }

            public string Title { get; private set; }
            public string Body { get; private set; }
        }

        private static FundingNotificationUI _activeInstance;

        private readonly Queue<PendingNotification> _pendingNotifications =
            new Queue<PendingNotification>();
        private readonly HashSet<string> _knownOfferedTargetKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string _activeSaveFolder;
        private CampaignController _offerSnapshotController;
        private bool _isDuplicateInstance;

        public void Awake()
        {
            // Keep one session-level subscriber so campaign events are observed regardless of which
            // game scene is active when a completion, funding boundary, or sponsor review occurs.
            if (_activeInstance != null && _activeInstance != this)
            {
                _isDuplicateInstance = true;
                Destroy(this);
                return;
            }

            _activeInstance = this;
            AgencyState.ObjectiveCompletionRecorded += OnObjectiveCompletionRecorded;
            RivalLiveMissionSimulation.LiveMissionResolved += OnRivalLiveMissionResolved;
            RivalDevelopmentSimulation.ResearchCompleted += OnRivalResearchCompleted;
            RivalDevelopmentSimulation.FacilityConstructionCompleted += OnRivalFacilityConstructionCompleted;
            CareerFundingAdapter.CampaignFundsAdded += OnCampaignFundsAdded;
        }

        public void Start()
        {
            if (_activeInstance != this)
            {
                return;
            }

            DontDestroyOnLoad(this);
        }

        public void OnDestroy()
        {
            if (_activeInstance != this)
            {
                return;
            }

            AgencyState.ObjectiveCompletionRecorded -= OnObjectiveCompletionRecorded;
            RivalLiveMissionSimulation.LiveMissionResolved -= OnRivalLiveMissionResolved;
            RivalDevelopmentSimulation.ResearchCompleted -= OnRivalResearchCompleted;
            RivalDevelopmentSimulation.FacilityConstructionCompleted -= OnRivalFacilityConstructionCompleted;
            CareerFundingAdapter.CampaignFundsAdded -= OnCampaignFundsAdded;
            ResetCurrentSave();
            _activeInstance = null;
        }

        public void Update()
        {
            if (_isDuplicateInstance || _activeInstance != this)
            {
                return;
            }

            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                ResetCurrentSave();
                return;
            }

            if (!HighLogic.LoadedSceneIsGame
                || HighLogic.CurrentGame == null
                || !EnsureCurrentSaveFolder())
            {
                return;
            }

            CampaignController campaignController = ModRuntime.Controller;
            if (campaignController != null && campaignController.NextFundingUniversalTime >= 0.0)
            {
                CaptureNewSponsorOffers(campaignController);
            }

            TryPublishNextNotification();
        }

        private void OnObjectiveCompletionRecorded(AgencyState agency, string objectiveId)
        {
            // Rival completions are announced from the authoritative Live Mission result instead so
            // failures, Science missions, crew survival, casualties, and insurance all use one message path.
            if (agency == null
                || !agency.IsPlayer
                || string.IsNullOrEmpty(objectiveId)
                || !EnsureCurrentSaveFolder())
            {
                return;
            }

            CampaignController campaignController = ModRuntime.Controller;
            if (campaignController == null)
            {
                return;
            }

            ObjectiveFundingContract contract = FindObjectiveFundingContract(
                campaignController,
                objectiveId);
            if (contract == null)
            {
                Debug.LogWarning(
                    "[TheRaceForSpace] Funding notification skipped unknown objective '"
                    + objectiveId
                    + "'.");
                return;
            }

            // Preserve the existing rule: only an Offered, unexpired target is announced as a
            // player funding completion. Persistence restoration uses the silent restore path.
            if (!contract.IsOffered || contract.IsExpired)
            {
                return;
            }

            EnqueueNotification(
                PlayerCompletionTitlePrefix + contract.Name,
                contract.Name + PlayerCompletionBodySuffix);
        }

        private void OnRivalLiveMissionResolved(
            AgencyState agency,
            RivalLiveMissionResolution resolution)
        {
            if (agency == null
                || agency.IsPlayer
                || resolution == null
                || !resolution.WasValid
                || resolution.Mission == null
                || !EnsureCurrentSaveFolder())
            {
                return;
            }

            string title = GetRivalShortName(agency)
                + " Live Mission - "
                + (resolution.Succeeded ? "Success" : "Failed");
            string body = resolution.Succeeded
                ? BuildRivalMissionSuccessBody(agency, resolution)
                : BuildRivalMissionFailureBody(agency, resolution);
            EnqueueNotification(title, body);
        }

        private void OnRivalResearchCompleted(
            AgencyState agency,
            RivalTechNodeDefinition techNode)
        {
            if (agency == null
                || agency.IsPlayer
                || techNode == null
                || string.IsNullOrEmpty(techNode.DisplayName)
                || !EnsureCurrentSaveFolder())
            {
                return;
            }

            string body = agency.Name
                + " has completed research on "
                + techNode.DisplayName
                + ".";
            if (techNode.UnlockedExperimentIds.Count > 0)
            {
                var experimentNames = new string[techNode.UnlockedExperimentIds.Count];
                for (int experimentIndex = 0;
                    experimentIndex < techNode.UnlockedExperimentIds.Count;
                    experimentIndex++)
                {
                    experimentNames[experimentIndex] = GetScienceExperimentDisplayName(
                        techNode.UnlockedExperimentIds[experimentIndex]);
                }

                body += techNode.UnlockedExperimentIds.Count == 1
                    ? "\nNew Science experiment unlocked: "
                    : "\nNew Science experiments unlocked: ";
                body += string.Join(", ", experimentNames) + ".";
            }

            EnqueueNotification(
                GetRivalShortName(agency) + " Research - Complete",
                body);
        }

        private void OnRivalFacilityConstructionCompleted(
            AgencyState agency,
            RivalFacilityConstructionState construction)
        {
            if (agency == null
                || agency.IsPlayer
                || construction == null
                || !Enum.IsDefined(typeof(RivalFacilityType), construction.Facility)
                || construction.TargetLevel < 2
                || construction.TargetLevel > 3
                || !EnsureCurrentSaveFolder())
            {
                return;
            }

            string body = agency.Name
                + " has completed construction of "
                + GetRivalFacilityDisplayName(construction.Facility)
                + " Level "
                + construction.TargetLevel
                + ".";
            string bonusDescription = GetRivalFacilityCompletionBonus(agency, construction);
            if (!string.IsNullOrEmpty(bonusDescription))
            {
                body += "\nBonus: " + bonusDescription;
            }

            EnqueueNotification(
                GetRivalShortName(agency) + " Facility Upgrade - Complete",
                body);
        }

        private void OnCampaignFundsAdded(double amount)
        {
            if (double.IsNaN(amount)
                || double.IsInfinity(amount)
                || amount <= 0.0
                || !EnsureCurrentSaveFolder())
            {
                return;
            }

            EnqueueNotification(
                FundingPayoutTitle,
                "Your agency received "
                + amount.ToString("N0")
                + " Funds from the campaign funding system.");
        }

        private string BuildRivalMissionSuccessBody(
            AgencyState agency,
            RivalLiveMissionResolution resolution)
        {
            RivalLiveMissionState mission = resolution.Mission;
            if (mission.MissionType == RivalMissionType.Science)
            {
                string body = agency.Name
                    + " has successfully completed a "
                    + GetScienceMissionDescription(mission)
                    + ".";
                return resolution.ScienceAwarded > 0.0
                    ? body + "\nScience gained: " + resolution.ScienceAwarded.ToString("0.#") + "."
                    : body + "\nNo Science remained to collect.";
            }

            return agency.Name
                + " has successfully completed "
                + GetContractMissionDisplayName(mission)
                + ".";
        }

        private string BuildRivalMissionFailureBody(
            AgencyState agency,
            RivalLiveMissionResolution resolution)
        {
            RivalLiveMissionState mission = resolution.Mission;
            string body = mission.MissionType == RivalMissionType.Science
                ? agency.Name + "'s " + GetScienceMissionDescription(mission) + " has failed."
                : agency.Name + "'s mission for " + GetContractMissionDisplayName(mission) + " has failed.";

            int assignedKerbals = Math.Max(0, mission.AssignedKerbalCount);
            if (assignedKerbals <= 0)
            {
                return body;
            }

            int lostKerbals = Math.Max(0, Math.Min(assignedKerbals, resolution.LostKerbalCount));
            int survivingKerbals = assignedKerbals - lostKerbals;
            if (lostKerbals == 0)
            {
                return assignedKerbals == 1
                    ? body + "\nThe assigned Kerbal escaped and survived."
                    : body
                        + "\nAll "
                        + assignedKerbals
                        + " assigned Kerbals escaped and survived.";
            }

            if (assignedKerbals == 1)
            {
                body += "\nThe assigned Kerbal was lost.";
            }
            else if (lostKerbals == assignedKerbals)
            {
                body += "\nAll " + assignedKerbals + " assigned Kerbals were lost.";
            }
            else
            {
                body += "\n"
                    + lostKerbals
                    + " of "
                    + assignedKerbals
                    + " assigned Kerbals "
                    + (lostKerbals == 1 ? "was" : "were")
                    + " lost. "
                    + survivingKerbals
                    + " escaped and survived.";
            }

            if (resolution.InsurancePenaltyFunds > 0.0)
            {
                body += "\nInsurance Penalty to Pay: "
                    + resolution.InsurancePenaltyFunds.ToString("N0")
                    + " Funds.";
            }

            return body;
        }

        private string GetContractMissionDisplayName(RivalLiveMissionState mission)
        {
            if (mission == null || string.IsNullOrEmpty(mission.ContractId))
            {
                return "Contract mission";
            }

            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(mission.ContractId);
            if (objective != null && !string.IsNullOrEmpty(objective.Name))
            {
                return objective.Name;
            }

            CampaignController campaignController = ModRuntime.Controller;
            if (campaignController != null)
            {
                for (int contractIndex = 0;
                    contractIndex < campaignController.SatelliteNetworkFundingContracts.Count;
                    contractIndex++)
                {
                    SatelliteNetworkFundingContract contract =
                        campaignController.SatelliteNetworkFundingContracts[contractIndex];
                    if (contract != null
                        && string.Equals(
                            contract.Id,
                            mission.ContractId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return contract.Name;
                    }
                }
            }

            return mission.ContractId;
        }

        private static string GetRivalFacilityCompletionBonus(
            AgencyState agency,
            RivalFacilityConstructionState construction)
        {
            switch (construction.Facility)
            {
                case RivalFacilityType.Administration:
                    return "Base funding increased to "
                        + RivalDevelopmentSimulation.GetAdministrationBaseIncomeFunds(agency).ToString("N0")
                        + " Funds per funding period.";

                case RivalFacilityType.AstronautComplex:
                    int rosterLimit = RivalDevelopmentSimulation.GetKerbalRosterLimit(agency);
                    string rosterBonus = rosterLimit <= 0
                        ? "Kerbal roster limit is now unlimited."
                        : "Kerbal roster limit increased to " + rosterLimit + ".";
                    if (construction.SourceLevel < 2 && construction.TargetLevel >= 2)
                    {
                        rosterBonus = rosterBonus.TrimEnd('.') + " and EVA Report access unlocked.";
                    }
                    return rosterBonus;

                case RivalFacilityType.MissionControl:
                    int satelliteLimit = RivalDevelopmentSimulation.GetSatelliteLimit(agency);
                    return satelliteLimit <= 0
                        ? "Satellite capacity is now unlimited."
                        : "Satellite capacity increased to " + satelliteLimit + ".";

                case RivalFacilityType.ResearchAndDevelopment:
                    double scienceCostLimit = RivalDevelopmentSimulation.GetResearchScienceCostLimit(agency);
                    string researchBonus = scienceCostLimit <= 0.0
                        ? "Research Science-cost limit removed."
                        : "Research Science-cost limit increased to "
                            + scienceCostLimit.ToString("0.#")
                            + ".";
                    if (construction.SourceLevel < 2 && construction.TargetLevel >= 2)
                    {
                        researchBonus = researchBonus.TrimEnd('.') + " and Surface Sample access unlocked.";
                    }
                    return researchBonus;

                case RivalFacilityType.VehicleAssemblyBuilding:
                case RivalFacilityType.LaunchPad:
                    double normalLaunchBonus = construction.TargetLevel >= 3
                        ? CampaignSettings.RivalNormalLaunchFacilityLevel3BonusChance
                        : CampaignSettings.RivalNormalLaunchFacilityLevel2BonusChance;
                    return "Normal Launch Progress Chance increased by "
                        + FormatPercentagePoints(normalLaunchBonus)
                        + ".";

                case RivalFacilityType.SpaceplaneHangar:
                case RivalFacilityType.Runway:
                    double scienceLaunchBonus = construction.TargetLevel >= 3
                        ? CampaignSettings.RivalScienceLaunchFacilityLevel3BonusChance
                        : CampaignSettings.RivalScienceLaunchFacilityLevel2BonusChance;
                    return "Science Launch Progress Chance increased by "
                        + FormatPercentagePoints(scienceLaunchBonus)
                        + ".";

                case RivalFacilityType.TrackingStation:
                    int interplanetaryLevel = Math.Max(
                        1,
                        CampaignSettings.RivalTrackingStationInterplanetaryLevel);
                    int kerbinMoonsLevel = Math.Max(
                        1,
                        CampaignSettings.RivalTrackingStationKerbinMoonsLevel);
                    if (construction.TargetLevel >= interplanetaryLevel)
                    {
                        return "Destination access expanded to other supported planets and moons.";
                    }
                    if (construction.TargetLevel >= kerbinMoonsLevel)
                    {
                        return "Destination access expanded to the Mun and Minmus.";
                    }
                    return "Destination access expanded to Kerbin.";

                default:
                    return string.Empty;
            }
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
                    return "Vehicle Assembly Building";
                case RivalFacilityType.LaunchPad:
                    return "Launch Pad";
                case RivalFacilityType.SpaceplaneHangar:
                    return "Spaceplane Hangar";
                case RivalFacilityType.Runway:
                    return "Runway";
                case RivalFacilityType.TrackingStation:
                    return "Tracking Station";
                default:
                    return facility.ToString();
            }
        }

        private static string FormatPercentagePoints(double chance)
        {
            double percentagePoints = Math.Max(0.0, chance) * 100.0;
            return percentagePoints.ToString("0.#")
                + (Math.Abs(percentagePoints - 1.0) < 0.000001
                    ? " percentage point"
                    : " percentage points");
        }

        private static string GetScienceMissionDescription(RivalLiveMissionState mission)
        {
            ScienceSubjectKey subject = mission == null ? null : mission.ScienceSubject;
            if (subject == null)
            {
                return "Science Expedition";
            }

            string description = "Science Expedition: "
                + GetScienceExperimentDisplayName(subject.ExperimentId)
                + ", "
                + EmptyAsUnknown(subject.BodyName)
                + " - "
                + FormatScienceSituation(subject.Situation);
            if (!string.IsNullOrEmpty(subject.BiomeName))
            {
                description += ", " + subject.BiomeName;
            }

            return description;
        }

        private static string GetRivalShortName(AgencyState agency)
        {
            if (agency == null || string.IsNullOrWhiteSpace(agency.Name))
            {
                return "Rival";
            }

            string trimmedName = agency.Name.Trim();
            const string GenericRivalPrefix = "Rival Agency ";
            if (trimmedName.StartsWith(GenericRivalPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string rivalNumber = trimmedName.Substring(GenericRivalPrefix.Length).Trim();
                return string.IsNullOrEmpty(rivalNumber) ? "Rival" : "Rival " + rivalNumber;
            }

            int firstSpaceIndex = trimmedName.IndexOf(' ');
            return firstSpaceIndex > 0
                ? trimmedName.Substring(0, firstSpaceIndex)
                : trimmedName;
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
                    return EmptyAsUnknown(situation);
            }
        }

        private static string EmptyAsUnknown(string value)
        {
            return string.IsNullOrEmpty(value) ? "Unknown" : value;
        }

        private void CaptureNewSponsorOffers(CampaignController campaignController)
        {
            if (campaignController == null)
            {
                return;
            }

            if (!ReferenceEquals(_offerSnapshotController, campaignController))
            {
                // A freshly created/restored controller may already contain historical offer state.
                // Establish that state silently so loading or quickloading cannot replay old reviews.
                _knownOfferedTargetKeys.Clear();
                CaptureCurrentOffers(campaignController, null);
                _offerSnapshotController = campaignController;
                return;
            }

            var newlyOfferedTargetNames = new List<string>();
            CaptureCurrentOffers(campaignController, newlyOfferedTargetNames);
            if (newlyOfferedTargetNames.Count == 0)
            {
                return;
            }

            string body = newlyOfferedTargetNames.Count == 1
                ? "New funding target offered: " + newlyOfferedTargetNames[0] + "."
                : "New funding targets offered: "
                    + string.Join(", ", newlyOfferedTargetNames.ToArray())
                    + ".";
            EnqueueNotification(SponsorReviewTitle, body);
        }

        private void CaptureCurrentOffers(
            CampaignController campaignController,
            IList<string> newlyOfferedTargetNames)
        {
            for (int contractIndex = 0;
                contractIndex < campaignController.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract =
                    campaignController.ObjectiveFundingContracts[contractIndex];
                if (contract == null || !contract.IsOffered)
                {
                    continue;
                }

                string offerKey = "objective:" + contract.Id;
                if (_knownOfferedTargetKeys.Add(offerKey) && newlyOfferedTargetNames != null)
                {
                    newlyOfferedTargetNames.Add(contract.Name);
                }
            }

            for (int contractIndex = 0;
                contractIndex < campaignController.SatelliteNetworkFundingContracts.Count;
                contractIndex++)
            {
                SatelliteNetworkFundingContract contract =
                    campaignController.SatelliteNetworkFundingContracts[contractIndex];
                if (contract == null || !contract.IsOffered)
                {
                    continue;
                }

                string offerKey = "satellite:" + contract.Id;
                if (_knownOfferedTargetKeys.Add(offerKey) && newlyOfferedTargetNames != null)
                {
                    newlyOfferedTargetNames.Add(contract.Name);
                }
            }
        }

        private void EnqueueNotification(string title, string body)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(body))
            {
                return;
            }

            _pendingNotifications.Enqueue(new PendingNotification(title, body));
        }

        private bool EnsureCurrentSaveFolder()
        {
            string currentSaveFolder = HighLogic.SaveFolder;
            if (string.IsNullOrEmpty(currentSaveFolder))
            {
                return false;
            }

            if (string.Equals(_activeSaveFolder, currentSaveFolder, StringComparison.Ordinal))
            {
                return true;
            }

            // A session-level addon can survive loading another save. Never carry an unsent message
            // or offer baseline across that boundary because stable IDs may legitimately exist in both.
            _pendingNotifications.Clear();
            _knownOfferedTargetKeys.Clear();
            _offerSnapshotController = null;
            _activeSaveFolder = currentSaveFolder;
            return true;
        }

        private void ResetCurrentSave()
        {
            _pendingNotifications.Clear();
            _knownOfferedTargetKeys.Clear();
            _offerSnapshotController = null;
            _activeSaveFolder = null;
        }

        private void TryPublishNextNotification()
        {
            if (_pendingNotifications.Count == 0 || MessageSystem.Instance == null)
            {
                return;
            }

            PendingNotification notification = _pendingNotifications.Dequeue();
            MessageSystem.Instance.AddMessage(new MessageSystem.Message(
                notification.Title,
                notification.Body,
                MessageSystemButton.MessageButtonColor.GREEN,
                MessageSystemButton.ButtonIcons.MESSAGE));

            Debug.Log(
                "[TheRaceForSpace] Campaign notification sent: '"
                + notification.Title
                + "'.");
        }

        private static ObjectiveFundingContract FindObjectiveFundingContract(
            CampaignController campaignController,
            string objectiveId)
        {
            for (int contractIndex = 0;
                contractIndex < campaignController.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract =
                    campaignController.ObjectiveFundingContracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, objectiveId, StringComparison.OrdinalIgnoreCase))
                {
                    return contract;
                }
            }

            return null;
        }
    }
}

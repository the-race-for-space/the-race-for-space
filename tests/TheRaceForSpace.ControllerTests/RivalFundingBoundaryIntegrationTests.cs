using System;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Campaign;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Objectives;

namespace TheRaceForSpace.ControllerTests
{
    internal static class RivalFundingBoundaryIntegrationTests
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double FundingIntervalSeconds = 90.0 * KerbinDaySeconds;

        public static void DueDevelopmentCompletesBeforeSignedFundingIsApplied()
        {
            ResetEnvironment();
            CampaignSettings.RivalNormalLaunchFacilityLevel1Chance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.Refresh(false);

            AgencyState aster = controller.FindAgencyById(CampaignController.AsterAgencyId);
            Require(aster != null && aster.RivalProgram != null,
                "The configured Aster rival programme should exist for the funding-boundary regression.");

            // Seed both development systems so they become eligible exactly at the first funding boundary.
            // Their costs were already paid in the earlier period, so only their completion is under test here.
            aster.Funds = 0.0;
            aster.RivalProgram.StoredScience = 0.0;
            aster.RivalProgram.PendingInsuranceFunds = 50000.0;
            aster.RivalProgram.FacilityConstruction.Clear();
            aster.RivalProgram.FacilityConstruction.Add(new RivalFacilityConstructionState
            {
                Facility = RivalFacilityType.Administration,
                SourceLevel = 1,
                TargetLevel = 2,
                StartUniversalTime = 0.0,
                CompletionUniversalTime = FundingIntervalSeconds,
                CostPaidFunds = CampaignSettings.RivalFacilityLevel1To2CostFunds
            });
            aster.RivalProgram.ResearchedTechIds.Remove("basicRocketry");
            aster.RivalProgram.CurrentResearch = new RivalResearchProjectState
            {
                TechId = "basicRocketry",
                ScienceCostPaid = 5.0,
                StartUniversalTime = 0.0,
                ResearchReadyUniversalTime = FundingIntervalSeconds,
                EligibleCompletionFundingUniversalTime = FundingIntervalSeconds
            };

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            Equal(2, aster.RivalProgram.FacilityLevels[RivalFacilityType.Administration]);
            Equal(0, aster.RivalProgram.FacilityConstruction.Count);
            Require(aster.RivalProgram.CurrentResearch == null,
                "Research due at the funding boundary should complete before new research selection.");
            Require(aster.RivalProgram.ResearchedTechIds.Contains("basicRocketry"),
                "The due Basic Rocketry project should become researched at the boundary.");

            // Administration Level 2 supplies 20,000 Funds. One employed Kerbal costs 10,000 payroll
            // and the pending 50,000 insurance bill is settled in full, for a signed net payout of -40,000.
            // This also proves the Administration upgrade completed before the boundary income was calculated.
            Equal(-40000.0, aster.Funds);
            Equal(0.0, aster.RivalProgram.PendingInsuranceFunds);

            // No Science or Funds remain for a replacement project/construction. The next projection therefore
            // contains only Level-2 Administration income minus the one-Kerbal payroll.
            Require(aster.RivalProgram.CurrentResearch == null,
                "No replacement research should start without stored Science.");
            Equal(0, aster.RivalProgram.FacilityConstruction.Count);
            Equal(10000.0, aster.NextPayoutFunds);
            Equal(FundingIntervalSeconds * 2.0, controller.NextFundingUniversalTime);
        }

        public static void ReadyCrewRecruitmentUsesFundingBeforeFacilityConstruction()
        {
            ResetEnvironment();
            CampaignSettings.NumberOfRivals = 1;
            CampaignSettings.RivalAdministrationLevel1BaseIncomeFunds = 110000.0;
            CampaignSettings.RivalKerbalPayrollFundsPerFundingBoundary = 10000.0;
            CampaignSettings.RivalKerbalHireCostFunds = 100000.0;
            CampaignSettings.RivalFacilityLevel1To2CostFunds = 100000.0;
            CampaignSettings.RivalNormalLaunchFacilityLevel1Chance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.Refresh(false);

            AgencyState aster = controller.FindAgencyById(CampaignController.AsterAgencyId);
            Require(aster != null && aster.RivalProgram != null,
                "Aster should exist for the recruitment-priority regression.");
            OfferObjective(controller, ObjectiveCatalogue.Control1Id);

            aster.Funds = 0.0;
            aster.RivalProgram.KerbalsEmployed = 1;
            aster.RivalProgram.StoredScience = 0.0;
            aster.RivalProgram.FacilityConstruction.Clear();
            aster.RivalProgram.LiveMissions.Clear();
            aster.RivalProgram.LiveMissions.Add(CreateCrewOccupyingMission(1));
            aster.NextMissionTargetId = ObjectiveCatalogue.Control1Id;
            aster.NextMissionDisplayName = "Control I";
            aster.MissionProgressPercent = 100;
            aster.NextMissionProgressCheckUniversalTime = 0.0;
            aster.NextMissionReadyUniversalTime = 1.0;

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            Equal(2, aster.RivalProgram.KerbalsEmployed);
            Equal(0.0, aster.Funds);
            Equal(0, aster.RivalProgram.FacilityConstruction.Count);
            Require(HasLiveContractMission(
                    aster,
                    ObjectiveCatalogue.Control1Id,
                    FundingIntervalSeconds),
                "The ready Control I mission should recruit at the funding boundary and launch before construction can spend the same Funds.");
        }

        public static void RosterBlockedRecruitmentDoesNotReserveConstructionFunds()
        {
            ResetEnvironment();
            CampaignSettings.NumberOfRivals = 1;
            CampaignSettings.RivalAdministrationLevel1BaseIncomeFunds = 130000.0;
            CampaignSettings.RivalKerbalPayrollFundsPerFundingBoundary = 10000.0;
            CampaignSettings.RivalKerbalHireCostFunds = 100000.0;
            CampaignSettings.RivalFacilityLevel1To2CostFunds = 100000.0;
            CampaignSettings.RivalNormalLaunchFacilityLevel1Chance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.Refresh(false);

            AgencyState aster = controller.FindAgencyById(CampaignController.AsterAgencyId);
            Require(aster != null && aster.RivalProgram != null,
                "Aster should exist for the roster-cap regression.");
            OfferObjective(controller, ObjectiveCatalogue.Control1Id);

            aster.Funds = 0.0;
            aster.RivalProgram.KerbalsEmployed = 3;
            aster.RivalProgram.StoredScience = 0.0;
            aster.RivalProgram.FacilityConstruction.Clear();
            aster.RivalProgram.LiveMissions.Clear();
            aster.RivalProgram.LiveMissions.Add(CreateCrewOccupyingMission(3));
            aster.NextMissionTargetId = ObjectiveCatalogue.Control1Id;
            aster.NextMissionDisplayName = "Control I";
            aster.MissionProgressPercent = 100;
            aster.NextMissionProgressCheckUniversalTime = 0.0;
            aster.NextMissionReadyUniversalTime = 1.0;

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            Equal(3, aster.RivalProgram.KerbalsEmployed);
            Equal(0.0, aster.Funds);
            Equal(1, aster.RivalProgram.FacilityConstruction.Count);
            Require(!HasLiveContractMission(
                    aster,
                    ObjectiveCatalogue.Control1Id,
                    FundingIntervalSeconds),
                "A roster-cap-blocked mission must remain waiting instead of hiring beyond the Astronaut Complex limit.");
        }

        private static RivalLiveMissionState CreateCrewOccupyingMission(int assignedKerbals)
        {
            return new RivalLiveMissionState
            {
                MissionSequence = 1,
                MissionType = RivalMissionType.Contract,
                ContractId = ObjectiveCatalogue.DirectedPower1Id,
                LocationId = CampaignSettings.RivalPreOrbitLocalLocationId,
                LaunchUniversalTime = 0.0,
                DurationDays = 180.0,
                CompletionUniversalTime = FundingIntervalSeconds * 2.0,
                Difficulty = 1,
                SuccessChancePercent = 90.0,
                AssignedKerbalCount = assignedKerbals,
                OutcomeSeed = 1
            };
        }

        private static void OfferObjective(CampaignController controller, string objectiveId)
        {
            for (int contractIndex = 0;
                contractIndex < controller.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract = controller.ObjectiveFundingContracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, objectiveId, StringComparison.OrdinalIgnoreCase))
                {
                    contract.Offer();
                    return;
                }
            }

            throw new InvalidOperationException(
                "Could not find objective funding contract '" + objectiveId + "'.");
        }

        private static bool HasLiveContractMission(
            AgencyState agency,
            string contractId,
            double launchUniversalTime)
        {
            if (agency == null || agency.RivalProgram == null)
            {
                return false;
            }

            for (int missionIndex = 0;
                missionIndex < agency.RivalProgram.LiveMissions.Count;
                missionIndex++)
            {
                RivalLiveMissionState mission = agency.RivalProgram.LiveMissions[missionIndex];
                if (mission != null
                    && mission.MissionType == RivalMissionType.Contract
                    && string.Equals(mission.ContractId, contractId, StringComparison.OrdinalIgnoreCase)
                    && Math.Abs(mission.LaunchUniversalTime - launchUniversalTime) < 0.000001)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ResetEnvironment()
        {
            CampaignSettings.ResetToDefaults();
            Planetarium.Reset();
            CareerFundingAdapter.Reset();
            KspVesselMonitor.Reset();
            ModPersistenceScenario.Reset();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    "Expected '" + expected + "' but got '" + actual + "'.");
            }
        }
    }
}

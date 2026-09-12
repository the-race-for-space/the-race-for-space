using System;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Campaign;
using TheRaceForSpace.Core;
using TheRaceForSpace.KspIntegration;

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

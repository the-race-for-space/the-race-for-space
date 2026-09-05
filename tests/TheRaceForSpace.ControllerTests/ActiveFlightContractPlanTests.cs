using System;
using System.Collections.Generic;
using TheRaceForSpace.Campaign;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Objectives;

namespace TheRaceForSpace.ControllerTests
{
    internal static class ActiveFlightContractPlanTests
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double FundingIntervalSeconds = 90.0 * KerbinDaySeconds;

        public static void OpeningOffersBuildInitialPlanAndStableRefreshReusesIt()
        {
            ResetEnvironment();
            CampaignSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.Refresh();

            IList<ObjectiveDefinition> initialPlan = controller.ActiveFlightContracts;
            Equal(4, initialPlan.Count);
            Require(Contains(initialPlan, ObjectiveCatalogue.DirectedPower1Id),
                "Directed Power I should be active while its opening offer is unfinished.");
            Require(Contains(initialPlan, ObjectiveCatalogue.Mass1Id),
                "Mass I should be active while its opening offer is unfinished.");
            Require(Contains(initialPlan, ObjectiveCatalogue.Control1Id),
                "Control I should be active while its opening offer is unfinished.");
            Require(Contains(initialPlan, ObjectiveCatalogue.Biome1Id),
                "Biome I should be active while its opening offer is unfinished.");

            Planetarium.CurrentUniversalTime = 20.0;
            controller.Refresh(false);

            Require(object.ReferenceEquals(initialPlan, controller.ActiveFlightContracts),
                "A controller refresh with no relevant contract-state change should reuse the cached active starter plan.");
        }

        public static void RivalUnlockDoesNotChangePlanUntilSponsorOffersContract()
        {
            ResetEnvironment();
            CampaignSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.Refresh();
            IList<ObjectiveDefinition> openingPlan = controller.ActiveFlightContracts;

            controller.FindAgencyById(CampaignController.AsterAgencyId)
                .RecordObjectiveCompletion(ObjectiveCatalogue.Mass1Id, 10.0);
            Planetarium.CurrentUniversalTime = 20.0;
            controller.Refresh(false);

            ObjectiveFundingContract mass2 = FindObjectiveFundingContract(
                controller,
                ObjectiveCatalogue.Mass2Id);
            Require(controller.IsObjectiveFundingContractAvailable(mass2),
                "A rival Mass I completion should unlock Mass II.");
            Require(!mass2.IsOffered,
                "Mass II should remain merely unlocked before the sponsor review.");
            Require(object.ReferenceEquals(openingPlan, controller.ActiveFlightContracts),
                "Unlocking a contract without offering it must not rebuild the active starter plan.");
            Require(!Contains(controller.ActiveFlightContracts, ObjectiveCatalogue.Mass2Id),
                "An unlocked-but-not-offered pre-orbit contract must not enter the active plan.");

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            Require(mass2.IsOffered,
                "The funding review should offer the unlocked Mass II contract.");
            Require(!object.ReferenceEquals(openingPlan, controller.ActiveFlightContracts),
                "Offering Mass II should rebuild the active starter plan once the controller refresh settles.");
            Equal(5, controller.ActiveFlightContracts.Count);
            Require(Contains(controller.ActiveFlightContracts, ObjectiveCatalogue.Mass1Id),
                "Rival completion must not remove the player's unfinished Mass I offer from the active plan.");
            Require(Contains(controller.ActiveFlightContracts, ObjectiveCatalogue.Mass2Id),
                "An offered Mass II contract should join the active plan independently of Mass I.");
        }

        public static void PlayerCompletionInvalidatesAndRemovesOnlyCompletedContract()
        {
            ResetEnvironment();
            CampaignSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.Refresh();
            IList<ObjectiveDefinition> openingPlan = controller.ActiveFlightContracts;

            Require(controller.PlayerAgency.RecordObjectiveCompletion(ObjectiveCatalogue.Mass1Id, 10.0),
                "Test setup should record the player's Mass I objectiveCompletion once.");
            controller.NotifyPlayerPreOrbitObjectiveCompleted();
            Planetarium.CurrentUniversalTime = 20.0;
            controller.Refresh(false);

            Require(!object.ReferenceEquals(openingPlan, controller.ActiveFlightContracts),
                "A player starter completion should invalidate the cached active plan.");
            Equal(3, controller.ActiveFlightContracts.Count);
            Require(!Contains(controller.ActiveFlightContracts, ObjectiveCatalogue.Mass1Id),
                "A pre-orbit contract should leave the active plan after the player completes it.");
            Require(Contains(controller.ActiveFlightContracts, ObjectiveCatalogue.DirectedPower1Id),
                "Other unfinished offered pre-orbit contracts should remain active.");
            Require(!Contains(controller.ActiveFlightContracts, ObjectiveCatalogue.Mass2Id),
                "Mass II should not become active until its later sponsor offer.");
        }

        public static void PreOrbitExpiryInvalidatesPlanWithoutAnotherPreOrbitOffer()
        {
            ResetEnvironment();
            CampaignSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.Refresh();

            controller.FindAgencyById(CampaignController.AsterAgencyId)
                .RecordObjectiveCompletion(ObjectiveCatalogue.Mass1Id, 10.0);
            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            ObjectiveFundingContract mass1 = FindObjectiveFundingContract(
                controller,
                ObjectiveCatalogue.Mass1Id);
            ObjectiveFundingContract mass2 = FindObjectiveFundingContract(
                controller,
                ObjectiveCatalogue.Mass2Id);
            Require(mass2.IsOffered,
                "Mass II should already be offered before the expiry-only refresh is tested.");

            mass1.RestoreState(true, 9);
            IList<ObjectiveDefinition> beforeExpiry = controller.ActiveFlightContracts;
            Require(Contains(beforeExpiry, ObjectiveCatalogue.Mass1Id),
                "Mass I should remain active before its final payout expires the contract.");
            Require(Contains(beforeExpiry, ObjectiveCatalogue.Mass2Id),
                "Mass II should already be independently active before Mass I expires.");

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds * 2.0;
            controller.Refresh(false);

            Require(mass1.IsExpired,
                "The tenth Mass I payout should expire the contract.");
            Require(!object.ReferenceEquals(beforeExpiry, controller.ActiveFlightContracts),
                "PreOrbit expiry should invalidate the active plan even when no new starter offer is added.");
            Require(!Contains(controller.ActiveFlightContracts, ObjectiveCatalogue.Mass1Id),
                "Expired Mass I should leave the active starter plan.");
            Require(Contains(controller.ActiveFlightContracts, ObjectiveCatalogue.Mass2Id),
                "Mass II should remain active after Mass I expires.");
        }

        private static bool Contains(IList<ObjectiveDefinition> objectives, string objectiveId)
        {
            for (int objectiveIndex = 0; objectiveIndex < objectives.Count; objectiveIndex++)
            {
                ObjectiveDefinition objective = objectives[objectiveIndex];
                if (objective != null
                    && string.Equals(objective.Id, objectiveId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static ObjectiveFundingContract FindObjectiveFundingContract(
            CampaignController controller,
            string contractId)
        {
            for (int contractIndex = 0;
                contractIndex < controller.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract =
                    controller.ObjectiveFundingContracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, contractId, StringComparison.OrdinalIgnoreCase))
                {
                    return contract;
                }
            }

            throw new InvalidOperationException("Missing objectiveCompletion contract '" + contractId + "'.");
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

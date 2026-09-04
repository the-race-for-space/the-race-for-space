using System;
using System.Collections.Generic;
using TheRaceForSpace.Competition;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Milestones;

namespace TheRaceForSpace.ControllerTests
{
    internal static class ActiveStarterContractPlanTests
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double FundingIntervalSeconds = 90.0 * KerbinDaySeconds;

        public static void OpeningOffersBuildInitialPlanAndStableRefreshReusesIt()
        {
            ResetEnvironment();
            RaceSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.Refresh();

            IList<MilestoneDefinition> initialPlan = controller.ActiveStarterContracts;
            Equal(4, initialPlan.Count);
            Require(Contains(initialPlan, PrototypeMilestones.DirectedPower1Id),
                "Directed Power I should be active while its opening offer is unfinished.");
            Require(Contains(initialPlan, PrototypeMilestones.Mass1Id),
                "Mass I should be active while its opening offer is unfinished.");
            Require(Contains(initialPlan, PrototypeMilestones.Control1Id),
                "Control I should be active while its opening offer is unfinished.");
            Require(Contains(initialPlan, PrototypeMilestones.Biome1Id),
                "Biome I should be active while its opening offer is unfinished.");

            Planetarium.CurrentUniversalTime = 20.0;
            controller.Refresh(false);

            Require(object.ReferenceEquals(initialPlan, controller.ActiveStarterContracts),
                "A controller refresh with no relevant contract-state change should reuse the cached active starter plan.");
        }

        public static void RivalUnlockDoesNotChangePlanUntilSponsorOffersContract()
        {
            ResetEnvironment();
            RaceSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.Refresh();
            IList<MilestoneDefinition> openingPlan = controller.ActiveStarterContracts;

            controller.AsterProgram.RecordAchievement(PrototypeMilestones.Mass1Id, 10.0);
            Planetarium.CurrentUniversalTime = 20.0;
            controller.Refresh(false);

            AchievementFundingProgramme mass2 = FindAchievement(
                controller,
                PrototypeMilestones.Mass2Id);
            Require(controller.IsAchievementProgrammeAvailable(mass2),
                "A rival Mass I completion should unlock Mass II.");
            Require(!mass2.IsOffered,
                "Mass II should remain merely unlocked before the sponsor review.");
            Require(object.ReferenceEquals(openingPlan, controller.ActiveStarterContracts),
                "Unlocking a contract without offering it must not rebuild the active starter plan.");
            Require(!Contains(controller.ActiveStarterContracts, PrototypeMilestones.Mass2Id),
                "An unlocked-but-not-offered starter contract must not enter the active plan.");

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            Require(mass2.IsOffered,
                "The funding review should offer the unlocked Mass II contract.");
            Require(!object.ReferenceEquals(openingPlan, controller.ActiveStarterContracts),
                "Offering Mass II should rebuild the active starter plan once the controller refresh settles.");
            Equal(5, controller.ActiveStarterContracts.Count);
            Require(Contains(controller.ActiveStarterContracts, PrototypeMilestones.Mass1Id),
                "Rival completion must not remove the player's unfinished Mass I offer from the active plan.");
            Require(Contains(controller.ActiveStarterContracts, PrototypeMilestones.Mass2Id),
                "An offered Mass II contract should join the active plan independently of Mass I.");
        }

        public static void PlayerCompletionInvalidatesAndRemovesOnlyCompletedContract()
        {
            ResetEnvironment();
            RaceSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.Refresh();
            IList<MilestoneDefinition> openingPlan = controller.ActiveStarterContracts;

            Require(controller.PlayerProgram.RecordAchievement(PrototypeMilestones.Mass1Id, 10.0),
                "Test setup should record the player's Mass I achievement once.");
            controller.NotifyPlayerStarterAchievementRecorded();
            Planetarium.CurrentUniversalTime = 20.0;
            controller.Refresh(false);

            Require(!object.ReferenceEquals(openingPlan, controller.ActiveStarterContracts),
                "A player starter completion should invalidate the cached active plan.");
            Equal(3, controller.ActiveStarterContracts.Count);
            Require(!Contains(controller.ActiveStarterContracts, PrototypeMilestones.Mass1Id),
                "A starter contract should leave the active plan after the player completes it.");
            Require(Contains(controller.ActiveStarterContracts, PrototypeMilestones.DirectedPower1Id),
                "Other unfinished offered starter contracts should remain active.");
            Require(!Contains(controller.ActiveStarterContracts, PrototypeMilestones.Mass2Id),
                "Mass II should not become active until its later sponsor offer.");
        }

        public static void StarterExpiryInvalidatesPlanWithoutAnotherStarterOffer()
        {
            ResetEnvironment();
            RaceSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.Refresh();

            controller.AsterProgram.RecordAchievement(PrototypeMilestones.Mass1Id, 10.0);
            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            AchievementFundingProgramme mass1 = FindAchievement(
                controller,
                PrototypeMilestones.Mass1Id);
            AchievementFundingProgramme mass2 = FindAchievement(
                controller,
                PrototypeMilestones.Mass2Id);
            Require(mass2.IsOffered,
                "Mass II should already be offered before the expiry-only refresh is tested.");

            mass1.RestoreState(true, 9);
            IList<MilestoneDefinition> beforeExpiry = controller.ActiveStarterContracts;
            Require(Contains(beforeExpiry, PrototypeMilestones.Mass1Id),
                "Mass I should remain active before its final payout expires the contract.");
            Require(Contains(beforeExpiry, PrototypeMilestones.Mass2Id),
                "Mass II should already be independently active before Mass I expires.");

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds * 2.0;
            controller.Refresh(false);

            Require(mass1.IsExpired,
                "The tenth Mass I payout should expire the contract.");
            Require(!object.ReferenceEquals(beforeExpiry, controller.ActiveStarterContracts),
                "Starter expiry should invalidate the active plan even when no new starter offer is added.");
            Require(!Contains(controller.ActiveStarterContracts, PrototypeMilestones.Mass1Id),
                "Expired Mass I should leave the active starter plan.");
            Require(Contains(controller.ActiveStarterContracts, PrototypeMilestones.Mass2Id),
                "Mass II should remain active after Mass I expires.");
        }

        private static bool Contains(IList<MilestoneDefinition> milestones, string milestoneId)
        {
            for (int milestoneIndex = 0; milestoneIndex < milestones.Count; milestoneIndex++)
            {
                MilestoneDefinition milestone = milestones[milestoneIndex];
                if (milestone != null
                    && string.Equals(milestone.Id, milestoneId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static AchievementFundingProgramme FindAchievement(
            SatelliteRaceController controller,
            string programmeId)
        {
            for (int programmeIndex = 0;
                programmeIndex < controller.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    controller.AchievementFundingProgrammes[programmeIndex];
                if (programme != null
                    && string.Equals(programme.Id, programmeId, StringComparison.OrdinalIgnoreCase))
                {
                    return programme;
                }
            }

            throw new InvalidOperationException("Missing achievement programme '" + programmeId + "'.");
        }

        private static void ResetEnvironment()
        {
            RaceSettings.ResetToDefaults();
            Planetarium.Reset();
            CareerFundingAdapter.Reset();
            KspVesselDiscovery.Reset();
            RacePersistenceScenario.Reset();
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

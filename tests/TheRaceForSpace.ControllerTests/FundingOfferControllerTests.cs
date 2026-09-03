using System;
using TheRaceForSpace.Competition;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Milestones;

namespace TheRaceForSpace.ControllerTests
{
    internal static class FundingOfferControllerTests
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double FundingIntervalSeconds = 90.0 * KerbinDaySeconds;

        public static void StarterContractsOpenFourInitialOffersAndLockRemaining()
        {
            ResetEnvironment();
            RaceSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.Refresh();

            Require(FindAchievement(controller, PrototypeMilestones.DirectedPower1Id).IsOffered,
                "Directed Power I should be offered at campaign start.");
            Require(FindAchievement(controller, PrototypeMilestones.Mass1Id).IsOffered,
                "Mass I should be offered at campaign start.");
            Require(FindAchievement(controller, PrototypeMilestones.Control1Id).IsOffered,
                "Control I should be offered at campaign start.");
            Require(FindAchievement(controller, PrototypeMilestones.Biome1Id).IsOffered,
                "Biome I should be offered at campaign start.");
            Require(!FindAchievement(controller, PrototypeMilestones.ProbeOrbitId).IsOffered,
                "Probe Orbit should be locked at campaign start.");
            Equal(4, CountOfferedStarterAchievements(controller));
            Equal(16, CountLockedStarterAchievements(controller));
            Equal(0, CountOfferedNormalAchievements(controller));
        }

        public static void RivalStarterCompletionUnlocksNextLevelForSponsorReview()
        {
            ResetEnvironment();
            RaceSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.Refresh();

            controller.AsterProgram.RecordAchievement(PrototypeMilestones.DirectedPower1Id, 10.0);
            Planetarium.CurrentUniversalTime = 20.0;
            controller.Refresh(false);

            AchievementFundingProgramme first = FindAchievement(
                controller,
                PrototypeMilestones.DirectedPower1Id);
            AchievementFundingProgramme second = FindAchievement(
                controller,
                PrototypeMilestones.DirectedPower2Id);
            AchievementFundingProgramme third = FindAchievement(
                controller,
                PrototypeMilestones.DirectedPower3Id);

            Require(first.HasStarted,
                "A rival completing Directed Power I should start its shared payout lifecycle.");
            Require(!second.IsOffered,
                "Directed Power II should remain Unlocked until a sponsor review offers it.");
            Require(controller.IsAchievementProgrammeAvailable(second),
                "A rival completion should globally unlock Directed Power II.");
            Require(!controller.IsAchievementProgrammeAvailable(third),
                "Directed Power III should remain locked until any agency completes level two.");
        }

        public static void UnlockedStarterLevelsJoinSponsorReview()
        {
            ResetEnvironment();
            RaceSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.Refresh();

            string[] openingMilestoneIds =
            {
                PrototypeMilestones.DirectedPower1Id,
                PrototypeMilestones.Mass1Id,
                PrototypeMilestones.Control1Id,
                PrototypeMilestones.Biome1Id
            };
            string[] secondLevelIds =
            {
                PrototypeMilestones.DirectedPower2Id,
                PrototypeMilestones.Mass2Id,
                PrototypeMilestones.Control2Id,
                PrototypeMilestones.Biome2Id
            };

            for (int milestoneIndex = 0; milestoneIndex < openingMilestoneIds.Length; milestoneIndex++)
            {
                controller.AsterProgram.RecordAchievement(openingMilestoneIds[milestoneIndex], 10.0);
            }

            Planetarium.CurrentUniversalTime = 20.0;
            controller.Refresh(false);

            for (int milestoneIndex = 0; milestoneIndex < secondLevelIds.Length; milestoneIndex++)
            {
                AchievementFundingProgramme programme = FindAchievement(controller, secondLevelIds[milestoneIndex]);
                Require(controller.IsAchievementProgrammeAvailable(programme),
                    "Each second-level starter contract should unlock after its predecessor completes.");
                Require(!programme.IsOffered,
                    "Unlocked starter contracts should wait for the sponsor review.");
            }

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            int offeredSecondLevelCount = 0;
            for (int milestoneIndex = 0; milestoneIndex < secondLevelIds.Length; milestoneIndex++)
            {
                if (FindAchievement(controller, secondLevelIds[milestoneIndex]).IsOffered)
                {
                    offeredSecondLevelCount++;
                }
            }

            Equal(2, offeredSecondLevelCount);
        }

        public static void AnyStarterLevelFiveOffersProbeOrbit()
        {
            string[] levelFiveIds =
            {
                PrototypeMilestones.DirectedPower5Id,
                PrototypeMilestones.Mass5Id,
                PrototypeMilestones.Control5Id,
                PrototypeMilestones.Biome5Id
            };

            for (int testIndex = 0; testIndex < levelFiveIds.Length; testIndex++)
            {
                ResetEnvironment();
                RaceSettings.RivalProgressChance = 0.0;
                Planetarium.CurrentUniversalTime = 0.0;
                KspVesselDiscovery.SetUnavailable();

                var controller = new SatelliteRaceController();
                controller.Refresh();

                controller.AsterProgram.RecordAchievement(levelFiveIds[testIndex], 10.0);
                Planetarium.CurrentUniversalTime = 20.0;
                controller.Refresh(false);

                AchievementFundingProgramme probeOrbit = FindAchievement(
                    controller,
                    PrototypeMilestones.ProbeOrbitId);
                Require(probeOrbit.IsOffered,
                    "Any agency completing any starter level-five achievement should offer Probe Orbit immediately.");
            }
        }

        public static void UnlockedFundingWaitsForFundingReview()
        {
            ResetEnvironment();
            RaceSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.AsterProgram.Funds = 0.0;
            controller.CobaltProgram.Funds = 0.0;
            controller.Refresh();
            OfferAndCompleteAllStarterAchievements(controller, 1.0);

            AchievementFundingProgramme probeOrbit = FindAchievement(
                controller,
                PrototypeMilestones.ProbeOrbitId);
            AchievementFundingProgramme crewedOrbit = FindAchievement(
                controller,
                PrototypeMilestones.CrewedOrbitId);
            AchievementFundingProgramme munProbeOrbit = FindAchievement(
                controller,
                PrototypeMilestones.MunProbeOrbitId);
            AchievementFundingProgramme minmusProbeOrbit = FindAchievement(
                controller,
                PrototypeMilestones.MinmusProbeOrbitId);
            FundingProgramme kerbinNetwork = FindFunding(
                controller,
                PrototypeFundingCatalogue.KerbinNetworkId);

            // This regression begins at the post-Probe-Orbit state. Offer the contract explicitly
            // rather than depending on the starter Level V immediate-offer path.
            probeOrbit.Offer();
            controller.PlayerProgram.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 1.0);
            controller.PlayerProgram.SetSatelliteCount("Kerbin", 1);

            Planetarium.CurrentUniversalTime = 1000.0;
            controller.Refresh(false);

            Require(probeOrbit.HasStarted, "The Probe Orbit offer should start after completion.");
            Require(controller.IsAchievementProgrammeAvailable(crewedOrbit), "Crewed Orbit should unlock after Probe Orbit.");
            Require(controller.IsAchievementProgrammeAvailable(munProbeOrbit), "Mun Probe Orbit should be unlocked.");
            Require(controller.IsAchievementProgrammeAvailable(minmusProbeOrbit), "Minmus Probe Orbit should be unlocked.");
            Require(kerbinNetwork.IsAvailable, "Kerbin network funding should be unlocked.");
            Require(!crewedOrbit.IsOffered && !munProbeOrbit.IsOffered && !minmusProbeOrbit.IsOffered,
                "Unlocked normal achievement funding must wait for a funding review.");
            Require(!kerbinNetwork.IsOffered,
                "Unlocked satellite funding must wait for a funding review.");
            Equal(0.0, controller.GetSatelliteCurrentPayout(controller.PlayerProgram, kerbinNetwork));

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            int newAchievementOffers = (crewedOrbit.IsOffered ? 1 : 0)
                + (munProbeOrbit.IsOffered ? 1 : 0)
                + (minmusProbeOrbit.IsOffered ? 1 : 0);
            Equal(2, newAchievementOffers);
            Require(kerbinNetwork.IsOffered,
                "The funding review should offer the only unlocked satellite candidate.");

            Equal(75000.0, CareerFundingAdapter.TotalAddedFunds);
            Equal(1, CareerFundingAdapter.AddFundsCalls);
        }

        public static void FundingReviewDoesNotCascadeCompletedOffers()
        {
            ResetEnvironment();
            RaceSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.AsterProgram.Funds = 0.0;
            controller.CobaltProgram.Funds = 0.0;
            controller.Refresh();
            OfferAndCompleteAllStarterAchievements(controller, 1.0);

            FindAchievement(controller, PrototypeMilestones.ProbeOrbitId).Offer();

            // Record every normal uncrewed orbital milestone before it is sponsored. Starter
            // contracts are pre-completed in the setup so they do not compete for these review slots.
            for (int programmeIndex = 0;
                programmeIndex < controller.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    controller.AchievementFundingProgrammes[programmeIndex];
                MilestoneDefinition milestone = PrototypeMilestones.FindById(programme.Id);
                if (milestone != null
                    && milestone.ObjectiveType == MilestoneObjectiveType.Orbit
                    && milestone.CrewRequirement == MilestoneCrewRequirement.UncrewedProbe)
                {
                    controller.PlayerProgram.RecordAchievement(programme.Id, 1.0);
                }
            }

            FindAchievement(controller, PrototypeMilestones.CrewedOrbitId).Offer();

            Planetarium.CurrentUniversalTime = 1000.0;
            controller.Refresh(false);

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            int laterOfferedCount = 0;
            AchievementFundingProgramme laterOfferedProgramme = null;
            for (int programmeIndex = 0;
                programmeIndex < controller.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    controller.AchievementFundingProgrammes[programmeIndex];
                if (!programme.IsOffered
                    || IsStarterAchievement(programme)
                    || string.Equals(programme.Id, PrototypeMilestones.ProbeOrbitId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(programme.Id, PrototypeMilestones.CrewedOrbitId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                laterOfferedCount++;
                laterOfferedProgramme = programme;
            }

            Equal(1, laterOfferedCount);
            Require(laterOfferedProgramme != null && laterOfferedProgramme.HasStarted,
                "The selected pre-completed contract should start after being offered.");
            Equal(3, CountOfferedNormalAchievements(controller));
        }

        public static void SatelliteFulfilmentWaitsForFundingReview()
        {
            ResetEnvironment();
            RaceSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.AsterProgram.Funds = 0.0;
            controller.CobaltProgram.Funds = 0.0;
            controller.Refresh();

            FundingProgramme kerbin = FindFunding(controller, PrototypeFundingCatalogue.KerbinNetworkId);
            FundingProgramme mun = FindFunding(controller, PrototypeFundingCatalogue.MunNetworkId);
            FundingProgramme minmus = FindFunding(controller, PrototypeFundingCatalogue.MinmusNetworkId);

            kerbin.Unlock();
            kerbin.Offer();
            mun.Unlock();
            mun.Offer();
            minmus.Unlock();

            controller.PlayerProgram.SetSatelliteCount("Kerbin", 10);
            controller.PlayerProgram.SetSatelliteCount("Mun", 1);

            Planetarium.CurrentUniversalTime = 1000.0;
            controller.Refresh(false);

            Require(kerbin.HasReachedSatelliteTarget,
                "Reaching 100% should permanently fulfil the Kerbin offer slot.");
            Require(!mun.HasReachedSatelliteTarget,
                "A partial Mun network should still consume an unfinished satellite slot.");
            Require(!minmus.IsOffered,
                "The newly vacant satellite slot must remain empty before funding day.");

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            Require(minmus.IsOffered,
                "The next funding review should fill the vacancy created by the fulfilled Kerbin network.");
        }

        public static void SatelliteReviewCapsUnfulfilledOffersAtTwo()
        {
            ResetEnvironment();
            RaceSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.Refresh();

            FundingProgramme kerbin = FindFunding(controller, PrototypeFundingCatalogue.KerbinNetworkId);
            FundingProgramme mun = FindFunding(controller, PrototypeFundingCatalogue.MunNetworkId);
            FundingProgramme minmus = FindFunding(controller, PrototypeFundingCatalogue.MinmusNetworkId);
            kerbin.Unlock();
            mun.Unlock();
            minmus.Unlock();

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            int offeredCount = (kerbin.IsOffered ? 1 : 0)
                + (mun.IsOffered ? 1 : 0)
                + (minmus.IsOffered ? 1 : 0);
            Equal(2, offeredCount);
        }

        public static void CrossedFundingBoundariesEachRunSponsorReview()
        {
            ResetEnvironment();
            RaceSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.AsterProgram.Funds = 0.0;
            controller.CobaltProgram.Funds = 0.0;
            controller.Refresh();
            OfferAndCompleteAllStarterAchievements(controller, 1.0);

            FindAchievement(controller, PrototypeMilestones.ProbeOrbitId).Offer();

            for (int programmeIndex = 0;
                programmeIndex < controller.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    controller.AchievementFundingProgrammes[programmeIndex];
                MilestoneDefinition milestone = PrototypeMilestones.FindById(programme.Id);
                if (milestone != null
                    && milestone.ObjectiveType == MilestoneObjectiveType.Orbit
                    && milestone.CrewRequirement == MilestoneCrewRequirement.UncrewedProbe)
                {
                    controller.PlayerProgram.RecordAchievement(programme.Id, 1.0);
                }
            }

            FindAchievement(controller, PrototypeMilestones.CrewedOrbitId).Offer();

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds * 2.0;
            controller.Refresh(false);

            int laterOfferedCount = 0;
            for (int programmeIndex = 0;
                programmeIndex < controller.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    controller.AchievementFundingProgrammes[programmeIndex];
                if (programme.IsOffered
                    && !IsStarterAchievement(programme)
                    && !string.Equals(programme.Id, PrototypeMilestones.ProbeOrbitId, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(programme.Id, PrototypeMilestones.CrewedOrbitId, StringComparison.OrdinalIgnoreCase))
                {
                    laterOfferedCount++;
                }
            }

            Equal(2, laterOfferedCount);
            Equal(FundingIntervalSeconds * 3.0, controller.NextFundingUniversalTime);
        }

        private static int CountOfferedStarterAchievements(SatelliteRaceController controller)
        {
            int count = 0;
            for (int programmeIndex = 0;
                programmeIndex < controller.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme = controller.AchievementFundingProgrammes[programmeIndex];
                if (programme.IsOffered && IsStarterAchievement(programme))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountLockedStarterAchievements(SatelliteRaceController controller)
        {
            int count = 0;
            for (int programmeIndex = 0;
                programmeIndex < controller.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme = controller.AchievementFundingProgrammes[programmeIndex];
                if (IsStarterAchievement(programme)
                    && !programme.IsOffered
                    && !controller.IsAchievementProgrammeAvailable(programme))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountOfferedNormalAchievements(SatelliteRaceController controller)
        {
            int count = 0;
            for (int programmeIndex = 0;
                programmeIndex < controller.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme = controller.AchievementFundingProgrammes[programmeIndex];
                if (programme.IsOffered && !IsStarterAchievement(programme))
                {
                    count++;
                }
            }

            return count;
        }

        private static void OfferAndCompleteAllStarterAchievements(
            SatelliteRaceController controller,
            double achievementUniversalTime)
        {
            for (int programmeIndex = 0;
                programmeIndex < controller.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme = controller.AchievementFundingProgrammes[programmeIndex];
                if (!IsStarterAchievement(programme))
                {
                    continue;
                }

                programme.Offer();
                controller.AsterProgram.RecordAchievement(programme.Id, achievementUniversalTime);
            }
        }

        private static bool IsStarterAchievement(AchievementFundingProgramme programme)
        {
            MilestoneDefinition milestone = programme == null
                ? null
                : PrototypeMilestones.FindById(programme.Id);
            return milestone != null && milestone.IsStarterContract;
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

        private static FundingProgramme FindFunding(
            SatelliteRaceController controller,
            string programmeId)
        {
            for (int programmeIndex = 0; programmeIndex < controller.FundingProgrammes.Count; programmeIndex++)
            {
                FundingProgramme programme = controller.FundingProgrammes[programmeIndex];
                if (programme != null
                    && string.Equals(programme.Id, programmeId, StringComparison.OrdinalIgnoreCase))
                {
                    return programme;
                }
            }

            throw new InvalidOperationException("Missing satellite programme '" + programmeId + "'.");
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

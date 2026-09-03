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

            AchievementFundingProgramme probeOrbit = FindAchievement(
                controller,
                PrototypeMilestones.ProbeOrbitId);
            AchievementFundingProgramme munProbeOrbit = FindAchievement(
                controller,
                PrototypeMilestones.MunProbeOrbitId);
            AchievementFundingProgramme minmusProbeOrbit = FindAchievement(
                controller,
                PrototypeMilestones.MinmusProbeOrbitId);
            FundingProgramme kerbinNetwork = FindFunding(
                controller,
                PrototypeFundingCatalogue.KerbinNetworkId);

            controller.PlayerProgram.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 1.0);
            controller.PlayerProgram.SetSatelliteCount("Kerbin", 1);

            Planetarium.CurrentUniversalTime = 1000.0;
            controller.Refresh(false);

            Require(probeOrbit.HasStarted, "The opening Probe Orbit offer should start after completion.");
            Require(controller.IsAchievementProgrammeAvailable(munProbeOrbit), "Mun Probe Orbit should be unlocked.");
            Require(controller.IsAchievementProgrammeAvailable(minmusProbeOrbit), "Minmus Probe Orbit should be unlocked.");
            Require(kerbinNetwork.IsAvailable, "Kerbin network funding should be unlocked.");
            Require(!munProbeOrbit.IsOffered && !minmusProbeOrbit.IsOffered,
                "Unlocked achievement funding must wait for a funding review.");
            Require(!kerbinNetwork.IsOffered,
                "Unlocked satellite funding must wait for a funding review.");
            Equal(0.0, controller.GetSatelliteCurrentPayout(controller.PlayerProgram, kerbinNetwork));

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);

            int newAchievementOffers = (munProbeOrbit.IsOffered ? 1 : 0)
                + (minmusProbeOrbit.IsOffered ? 1 : 0);
            Equal(1, newAchievementOffers);
            Require(kerbinNetwork.IsOffered,
                "The funding review should offer the only unlocked satellite candidate.");

            // Newly selected offers happen after this boundary's payout, so the existing Kerbin
            // satellite cannot receive the payment that triggered its sponsor review.
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

            // Record every probe milestone before it is sponsored. They remain real race
            // achievements, but only the opening Probe Orbit contract is currently Offered.
            for (int programmeIndex = 0;
                programmeIndex < controller.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    controller.AchievementFundingProgrammes[programmeIndex];
                MilestoneDefinition milestone = PrototypeMilestones.FindById(programme.Id);
                if (milestone != null
                    && milestone.CrewRequirement == MilestoneCrewRequirement.UncrewedProbe)
                {
                    controller.PlayerProgram.RecordAchievement(programme.Id, 1.0);
                }
            }

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

            // Starting it frees the unfinished slot immediately, but the review already consumed
            // its one vacancy snapshot, so another target must wait until the next funding date.
            Equal(3, CountOfferedAchievements(controller));
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

            for (int programmeIndex = 0;
                programmeIndex < controller.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    controller.AchievementFundingProgrammes[programmeIndex];
                MilestoneDefinition milestone = PrototypeMilestones.FindById(programme.Id);
                if (milestone != null
                    && milestone.CrewRequirement == MilestoneCrewRequirement.UncrewedProbe)
                {
                    controller.PlayerProgram.RecordAchievement(programme.Id, 1.0);
                }
            }

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
                    && !string.Equals(programme.Id, PrototypeMilestones.ProbeOrbitId, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(programme.Id, PrototypeMilestones.CrewedOrbitId, StringComparison.OrdinalIgnoreCase))
                {
                    laterOfferedCount++;
                }
            }

            // The first crossed boundary may issue one replacement because Crewed Orbit still
            // occupies the other unfinished slot. That pre-completed replacement is recognized
            // as complete before the second boundary, allowing exactly one more offer there.
            Equal(2, laterOfferedCount);
            Equal(FundingIntervalSeconds * 3.0, controller.NextFundingUniversalTime);
        }

        private static int CountOfferedAchievements(SatelliteRaceController controller)
        {
            int count = 0;
            for (int programmeIndex = 0;
                programmeIndex < controller.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                if (controller.AchievementFundingProgrammes[programmeIndex].IsOffered)
                {
                    count++;
                }
            }

            return count;
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

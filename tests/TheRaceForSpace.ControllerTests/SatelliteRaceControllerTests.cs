using System;
using System.Collections.Generic;
using TheRaceForSpace.Competition;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.ControllerTests
{
    internal static class SatelliteRaceControllerTests
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double FundingIntervalSeconds = 90.0 * KerbinDaySeconds;

        public static void ProbeObservationUnlocksFundingFlow()
        {
            ResetEnvironment();
            const double observationUniversalTime = 1234.0;
            Planetarium.CurrentUniversalTime = observationUniversalTime;
            KspVesselDiscovery.SetSnapshots(
                new List<VesselTrackingSnapshot>
                {
                    new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 0)
                },
                observationUniversalTime);

            var controller = new SatelliteRaceController();
            controller.AsterProgram.Funds = 0.0;
            controller.CobaltProgram.Funds = 0.0;

            controller.Refresh();

            AchievementFundingProgramme probeOrbit = FindAchievementProgramme(
                controller,
                PrototypeMilestones.ProbeOrbitId);
            AchievementFundingProgramme munProbeOrbit = FindAchievementProgramme(
                controller,
                PrototypeMilestones.MunProbeOrbitId);
            FundingProgramme kerbinNetwork = FindFundingProgramme(
                controller,
                PrototypeFundingCatalogue.KerbinNetworkId);
            FundingProgramme munNetwork = FindFundingProgramme(
                controller,
                PrototypeFundingCatalogue.MunNetworkId);

            Require(
                controller.PlayerProgram.HasAchievement(PrototypeMilestones.ProbeOrbitId),
                "A player Probe snapshot should flow through controller tracking into Probe Orbit.");
            Equal(
                observationUniversalTime,
                controller.PlayerProgram.GetAchievementUniversalTime(PrototypeMilestones.ProbeOrbitId));
            Require(kerbinNetwork.IsAvailable, "Probe Orbit should unlock the Kerbin satellite network.");
            Require(probeOrbit.HasStarted, "The achieved Probe Orbit contract should start immediately.");
            Require(
                controller.IsAchievementProgrammeAvailable(munProbeOrbit),
                "Probe Orbit should make the downstream Mun Probe Orbit contract available.");
            Require(!munNetwork.IsAvailable, "Mun satellite funding should remain locked before Mun Probe Orbit.");
            Equal(120000.0, controller.PlayerProgram.NextPayoutFunds);
            Equal(1, RacePersistenceScenario.RivalCaptureCalls);
            Equal(1, RacePersistenceScenario.RaceProgressCaptureCalls);
        }

        public static void ExistingStatePaysAtSharedFundingBoundary()
        {
            ResetEnvironment();
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.AsterProgram.Funds = 0.0;
            controller.CobaltProgram.Funds = 0.0;
            controller.Refresh();

            controller.PlayerProgram.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 1.0);
            controller.PlayerProgram.SetSatelliteCount("Kerbin", 1);

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh();

            AchievementFundingProgramme probeOrbit = FindAchievementProgramme(
                controller,
                PrototypeMilestones.ProbeOrbitId);

            // The one existing Kerbin satellite earns 20k of the 200k/10 network pool and
            // the sole Probe Orbit achiever receives the first 100k achievement payment.
            Equal(120000.0, CareerFundingAdapter.TotalAddedFunds);
            Equal(2, CareerFundingAdapter.AddFundsCalls);
            Equal(1, probeOrbit.PaymentsProcessed);
            Equal(90, probeOrbit.CurrentInterestPercent);
            Equal(FundingIntervalSeconds * 2.0, controller.NextFundingUniversalTime);
            Equal(110000.0, controller.PlayerProgram.NextPayoutFunds);
        }

        public static void RestoredOverdueFundingBoundaryIsProcessed()
        {
            ResetEnvironment();
            Planetarium.CurrentUniversalTime = FundingIntervalSeconds + 1.0;
            RacePersistenceScenario.RestoredNextFundingUniversalTime = FundingIntervalSeconds;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.AsterProgram.Funds = 0.0;
            controller.CobaltProgram.Funds = 0.0;

            controller.Refresh();

            // Restoring the overdue boundary must replay it instead of recomputing the next date
            // from the current UT and silently skipping the 90-day rival base payment.
            Equal(20000.0, controller.AsterProgram.Funds);
            Equal(20000.0, controller.CobaltProgram.Funds);
            Equal(FundingIntervalSeconds * 2.0, controller.NextFundingUniversalTime);
            Equal(
                FundingIntervalSeconds * 2.0,
                RacePersistenceScenario.LastCapturedNextFundingUniversalTime);
        }

        public static void BoundaryObservationIsNotPaidRetroactively()
        {
            ResetEnvironment();
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetSnapshots(new List<VesselTrackingSnapshot>(), 0.0);

            var controller = new SatelliteRaceController();
            controller.AsterProgram.Funds = 0.0;
            controller.CobaltProgram.Funds = 0.0;
            controller.Refresh();

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            KspVesselDiscovery.SetSnapshots(
                new List<VesselTrackingSnapshot>
                {
                    new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 0)
                },
                FundingIntervalSeconds);

            controller.Refresh();

            AchievementFundingProgramme probeOrbit = FindAchievementProgramme(
                controller,
                PrototypeMilestones.ProbeOrbitId);
            FundingProgramme kerbinNetwork = FindFundingProgramme(
                controller,
                PrototypeFundingCatalogue.KerbinNetworkId);

            // Due funding is replayed before current KSP vessel discovery. A craft first observed
            // on the boundary therefore becomes eligible for the next date, not the date just paid.
            Equal(0.0, CareerFundingAdapter.TotalAddedFunds);
            Equal(0, probeOrbit.PaymentsProcessed);
            Require(
                controller.PlayerProgram.HasAchievement(PrototypeMilestones.ProbeOrbitId),
                "The boundary snapshot should still be recorded after due funding is processed.");
            Require(kerbinNetwork.IsAvailable, "The newly observed Probe Orbit should unlock future Kerbin funding.");
            Equal(120000.0, controller.PlayerProgram.NextPayoutFunds);
        }

        public static void ProjectedPayoutCacheRebuildsOnRefresh()
        {
            ResetEnvironment();
            Planetarium.CurrentUniversalTime = 1000.0;
            KspVesselDiscovery.SetSnapshots(
                new List<VesselTrackingSnapshot>
                {
                    new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 0)
                },
                1000.0);

            var controller = new SatelliteRaceController();
            controller.AsterProgram.Funds = 0.0;
            controller.CobaltProgram.Funds = 0.0;
            controller.Refresh();

            AchievementFundingProgramme probeOrbit = FindAchievementProgramme(
                controller,
                PrototypeMilestones.ProbeOrbitId);
            FundingProgramme kerbinNetwork = FindFundingProgramme(
                controller,
                PrototypeFundingCatalogue.KerbinNetworkId);

            Equal(
                20000.0,
                controller.GetSatelliteCurrentPayout(controller.PlayerProgram, kerbinNetwork));
            Equal(
                100000.0,
                controller.GetAchievementCurrentPayout(controller.PlayerProgram, probeOrbit));

            Planetarium.CurrentUniversalTime = 2000.0;
            KspVesselDiscovery.SetSnapshots(new List<VesselTrackingSnapshot>(), 2000.0);
            controller.Refresh();

            Equal(
                0.0,
                controller.GetSatelliteCurrentPayout(controller.PlayerProgram, kerbinNetwork));
            Equal(
                100000.0,
                controller.GetAchievementCurrentPayout(controller.PlayerProgram, probeOrbit));
            Equal(100000.0, controller.PlayerProgram.NextPayoutFunds);
        }

        private static AchievementFundingProgramme FindAchievementProgramme(
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

            throw new InvalidOperationException(
                "Missing achievement funding programme '" + programmeId + "'.");
        }

        private static FundingProgramme FindFundingProgramme(
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

            throw new InvalidOperationException(
                "Missing satellite funding programme '" + programmeId + "'.");
        }

        private static void ResetEnvironment()
        {
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

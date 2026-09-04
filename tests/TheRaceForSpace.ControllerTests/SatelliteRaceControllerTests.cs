using System;
using System.Collections.Generic;
using TheRaceForSpace.Competition;
using TheRaceForSpace.Core;
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

        public static void ConfiguredRivalCountAndStartingFundsAreUsed()
        {
            ResetEnvironment();
            RaceSettings.NumberOfRivals = 3;
            RaceSettings.RivalStartingFunds = 123456.0;

            var controller = new SatelliteRaceController();

            Equal(3, controller.RivalPrograms.Count);
            Equal(4, controller.Programs.Count);
            Equal(123456.0, controller.RivalPrograms[0].Funds);
            Equal(123456.0, controller.RivalPrograms[1].Funds);
            Equal(123456.0, controller.RivalPrograms[2].Funds);
            Equal(SatelliteRaceController.AsterProgramId, controller.RivalPrograms[0].Id);
            Equal(SatelliteRaceController.CobaltProgramId, controller.RivalPrograms[1].Id);
            Equal("rival-3", controller.RivalPrograms[2].Id);
            Require(
                object.ReferenceEquals(
                    controller.RivalPrograms[0],
                    controller.FindProgramById(SatelliteRaceController.AsterProgramId)),
                "Stable Aster lookup should resolve the first configured rival.");
            Require(
                object.ReferenceEquals(
                    controller.RivalPrograms[1],
                    controller.FindProgramById(SatelliteRaceController.CobaltProgramId)),
                "Stable Cobalt lookup should resolve the second configured rival.");
        }

        public static void ConfiguredFundingIntervalSetsNextBoundary()
        {
            ResetEnvironment();
            RaceSettings.FundingIntervalDays = 30.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.Refresh();

            Equal(30.0 * KerbinDaySeconds, controller.NextFundingUniversalTime);
        }

        public static void ScheduledRefreshCanSkipPlayerVesselObservation()
        {
            ResetEnvironment();
            const double observationUniversalTime = 1000.0;
            Planetarium.CurrentUniversalTime = observationUniversalTime;
            KspVesselDiscovery.SetSnapshots(
                new List<VesselTrackingSnapshot>
                {
                    new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 0)
                },
                observationUniversalTime);

            var controller = new SatelliteRaceController();
            controller.FindProgramById(SatelliteRaceController.AsterProgramId).Funds = 0.0;
            controller.FindProgramById(SatelliteRaceController.CobaltProgramId).Funds = 0.0;
            QualifyForProbeOrbit(controller, observationUniversalTime - 1.0);

            bool skippedObservation = controller.Refresh(false);

            Require(!skippedObservation, "A scheduled non-vessel refresh should report no player observation.");
            Equal(0, KspVesselDiscovery.CaptureCalls);
            Require(
                !controller.PlayerProgram.HasAchievement(PrototypeMilestones.ProbeOrbitId),
                "Skipping the vessel observation must leave player Probe Orbit state unchanged.");
            Equal(1, RacePersistenceScenario.RivalCaptureCalls);
            Equal(1, RacePersistenceScenario.RaceProgressCaptureCalls);

            bool completedObservation = controller.Refresh(true);

            Require(completedObservation, "An available scheduled vessel refresh should report success.");
            Equal(1, KspVesselDiscovery.CaptureCalls);
            Require(
                controller.PlayerProgram.HasAchievement(PrototypeMilestones.ProbeOrbitId),
                "The next scheduled vessel observation should record the qualified Probe Orbit.");
        }

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
            controller.FindProgramById(SatelliteRaceController.AsterProgramId).Funds = 0.0;
            controller.FindProgramById(SatelliteRaceController.CobaltProgramId).Funds = 0.0;
            QualifyForProbeOrbit(controller, observationUniversalTime - 1.0);

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
                "A qualified player Probe snapshot should flow through controller tracking into Probe Orbit.");
            Equal(
                observationUniversalTime,
                controller.PlayerProgram.GetAchievementUniversalTime(PrototypeMilestones.ProbeOrbitId));
            Require(kerbinNetwork.IsAvailable, "Probe Orbit should unlock the Kerbin satellite network.");
            Require(
                !kerbinNetwork.IsOffered,
                "An unlocked satellite programme should wait for a funding-day sponsor review.");
            Require(probeOrbit.HasStarted, "The achieved Probe Orbit offer should start immediately.");
            Require(
                controller.IsAchievementProgrammeAvailable(munProbeOrbit),
                "Probe Orbit should make the downstream Mun Probe Orbit contract available.");
            Require(
                !munProbeOrbit.IsOffered,
                "An unlocked one-off achievement should wait for a funding-day sponsor review.");
            Require(
                !munNetwork.IsAvailable,
                "Mun satellite funding should remain locked until Mun Probe Orbit and Kerbin network progress are complete.");
            Equal(75000.0, controller.PlayerProgram.NextPayoutFunds);
            Equal(1, RacePersistenceScenario.RivalCaptureCalls);
            Equal(1, RacePersistenceScenario.RaceProgressCaptureCalls);
        }

        public static void KerbinNetworkProgressUnlocksMoonFunding()
        {
            ResetEnvironment();
            const double observationUniversalTime = 2000.0;
            Planetarium.CurrentUniversalTime = observationUniversalTime;
            KspVesselDiscovery.SetSnapshots(
                new List<VesselTrackingSnapshot>
                {
                    new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 0),
                    new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 0),
                    new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 0),
                    new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 0),
                    new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 0),
                    new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 0)
                },
                observationUniversalTime);

            var controller = new SatelliteRaceController();
            controller.FindProgramById(SatelliteRaceController.AsterProgramId).Funds = 0.0;
            controller.FindProgramById(SatelliteRaceController.CobaltProgramId).Funds = 0.0;
            QualifyForProbeOrbit(controller, observationUniversalTime - 1.0);

            controller.Refresh();

            FundingProgramme munNetwork = FindFundingProgramme(
                controller,
                PrototypeFundingCatalogue.MunNetworkId);
            FundingProgramme minmusNetwork = FindFundingProgramme(
                controller,
                PrototypeFundingCatalogue.MinmusNetworkId);

            Equal(6, controller.PlayerProgram.GetSatelliteCount("Kerbin"));
            Require(
                controller.PlayerProgram.HasAchievement(PrototypeMilestones.ProbeOrbitId),
                "Six Kerbin probe satellites should record Probe Orbit once a starter line qualifies the programme.");
            Require(
                !controller.PlayerProgram.HasAchievement(PrototypeMilestones.MunProbeOrbitId),
                "Mun Probe Orbit should still be incomplete before a Mun probe achievement is recorded.");
            Require(
                !controller.PlayerProgram.HasAchievement(PrototypeMilestones.MinmusProbeOrbitId),
                "Minmus Probe Orbit should still be incomplete before a Minmus probe achievement is recorded.");
            Require(
                !munNetwork.IsAvailable,
                "Six collective Kerbin satellites alone must not unlock Mun satellite funding without Mun Probe Orbit.");
            Require(
                !minmusNetwork.IsAvailable,
                "Six collective Kerbin satellites alone must not unlock Minmus satellite funding without Minmus Probe Orbit.");

            controller.PlayerProgram.RecordAchievement(
                PrototypeMilestones.MunProbeOrbitId,
                observationUniversalTime);
            controller.Refresh(false);

            Require(
                munNetwork.IsAvailable,
                "Mun Probe Orbit plus six collective Kerbin satellites should unlock Mun satellite funding.");
            Require(
                !minmusNetwork.IsAvailable,
                "Mun Probe Orbit must not satisfy Minmus satellite funding's Minmus Probe Orbit requirement.");

            controller.PlayerProgram.RecordAchievement(
                PrototypeMilestones.MinmusProbeOrbitId,
                observationUniversalTime);
            controller.Refresh(false);

            Require(
                minmusNetwork.IsAvailable,
                "Minmus Probe Orbit plus six collective Kerbin satellites should unlock Minmus satellite funding.");
            Require(
                !munNetwork.IsOffered && !minmusNetwork.IsOffered,
                "Unlocked moon networks should remain unoffered until the next funding review.");
        }

        public static void ExistingStatePaysAtSharedFundingBoundary()
        {
            ResetEnvironment();
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.FindProgramById(SatelliteRaceController.AsterProgramId).Funds = 0.0;
            controller.FindProgramById(SatelliteRaceController.CobaltProgramId).Funds = 0.0;
            QualifyForProbeOrbit(controller, 0.0);
            controller.Refresh();

            controller.PlayerProgram.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 1.0);
            controller.PlayerProgram.SetSatelliteCount("Kerbin", 1);

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh();

            AchievementFundingProgramme probeOrbit = FindAchievementProgramme(
                controller,
                PrototypeMilestones.ProbeOrbitId);
            FundingProgramme kerbinNetwork = FindFundingProgramme(
                controller,
                PrototypeFundingCatalogue.KerbinNetworkId);

            Equal(75000.0, CareerFundingAdapter.TotalAddedFunds);
            Equal(1, CareerFundingAdapter.AddFundsCalls);
            Require(kerbinNetwork.IsOffered, "The funding review should offer the unlocked Kerbin network.");
            Equal(1, probeOrbit.PaymentsProcessed);
            Equal(90, probeOrbit.CurrentInterestPercent);
            Equal(FundingIntervalSeconds * 2.0, controller.NextFundingUniversalTime);
            Equal(87500.0, controller.PlayerProgram.NextPayoutFunds);
        }

        public static void RestoredOverdueFundingBoundaryIsProcessed()
        {
            ResetEnvironment();
            Planetarium.CurrentUniversalTime = FundingIntervalSeconds + 1.0;
            RacePersistenceScenario.RestoredNextFundingUniversalTime = FundingIntervalSeconds;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.FindProgramById(SatelliteRaceController.AsterProgramId).Funds = 0.0;
            controller.FindProgramById(SatelliteRaceController.CobaltProgramId).Funds = 0.0;

            controller.Refresh();

            Equal(
                20000.0,
                controller.FindProgramById(SatelliteRaceController.AsterProgramId).Funds);
            Equal(
                20000.0,
                controller.FindProgramById(SatelliteRaceController.CobaltProgramId).Funds);
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
            controller.FindProgramById(SatelliteRaceController.AsterProgramId).Funds = 0.0;
            controller.FindProgramById(SatelliteRaceController.CobaltProgramId).Funds = 0.0;
            controller.Refresh();

            // Qualifying after the campaign starts makes Probe Orbit available at the crossed
            // funding boundary, but the actual orbital observation still occurs after that payout.
            QualifyForProbeOrbit(controller, 1.0);
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

            Equal(0.0, CareerFundingAdapter.TotalAddedFunds);
            Equal(0, probeOrbit.PaymentsProcessed);
            Require(
                controller.PlayerProgram.HasAchievement(PrototypeMilestones.ProbeOrbitId),
                "The boundary snapshot should still be recorded after due funding is processed.");
            Require(kerbinNetwork.IsAvailable, "The newly observed Probe Orbit should unlock future Kerbin funding.");
            Require(
                !kerbinNetwork.IsOffered,
                "A contract unlocked after the funding review must wait for the next funding date.");
            Equal(75000.0, controller.PlayerProgram.NextPayoutFunds);
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
            controller.FindProgramById(SatelliteRaceController.AsterProgramId).Funds = 0.0;
            controller.FindProgramById(SatelliteRaceController.CobaltProgramId).Funds = 0.0;
            QualifyForProbeOrbit(controller, 999.0);
            controller.Refresh();

            AchievementFundingProgramme probeOrbit = FindAchievementProgramme(
                controller,
                PrototypeMilestones.ProbeOrbitId);
            FundingProgramme kerbinNetwork = FindFundingProgramme(
                controller,
                PrototypeFundingCatalogue.KerbinNetworkId);

            kerbinNetwork.Offer();
            controller.Refresh(false);

            Equal(
                20000.0,
                controller.GetSatelliteCurrentPayout(controller.PlayerProgram, kerbinNetwork));
            Equal(
                75000.0,
                controller.GetAchievementCurrentPayout(controller.PlayerProgram, probeOrbit));

            Planetarium.CurrentUniversalTime = 2000.0;
            KspVesselDiscovery.SetSnapshots(new List<VesselTrackingSnapshot>(), 2000.0);
            controller.Refresh();

            Equal(
                0.0,
                controller.GetSatelliteCurrentPayout(controller.PlayerProgram, kerbinNetwork));
            Equal(
                75000.0,
                controller.GetAchievementCurrentPayout(controller.PlayerProgram, probeOrbit));
            Equal(75000.0, controller.PlayerProgram.NextPayoutFunds);
        }

        private static void QualifyForProbeOrbit(
            SatelliteRaceController controller,
            double achievementUniversalTime)
        {
            controller.PlayerProgram.RecordAchievement(
                PrototypeMilestones.DirectedPower5Id,
                achievementUniversalTime);
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

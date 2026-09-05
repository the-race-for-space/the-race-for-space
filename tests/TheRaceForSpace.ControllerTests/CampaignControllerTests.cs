using System;
using System.Collections.Generic;
using TheRaceForSpace.Campaign;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.ControllerTests
{
    internal static class CampaignControllerTests
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double FundingIntervalSeconds = 90.0 * KerbinDaySeconds;

        public static void ConfiguredRivalCountAndStartingFundsAreUsed()
        {
            ResetEnvironment();
            CampaignSettings.NumberOfRivals = 3;
            CampaignSettings.RivalStartingFunds = 123456.0;

            var controller = new CampaignController();

            Equal(3, controller.RivalAgencies.Count);
            Equal(4, controller.Agencies.Count);
            Equal(123456.0, controller.RivalAgencies[0].Funds);
            Equal(123456.0, controller.RivalAgencies[1].Funds);
            Equal(123456.0, controller.RivalAgencies[2].Funds);
            Equal(CampaignController.AsterAgencyId, controller.RivalAgencies[0].Id);
            Equal(CampaignController.CobaltAgencyId, controller.RivalAgencies[1].Id);
            Equal("rival-3", controller.RivalAgencies[2].Id);
            Require(
                object.ReferenceEquals(
                    controller.RivalAgencies[0],
                    controller.FindAgencyById(CampaignController.AsterAgencyId)),
                "Stable Aster lookup should resolve the first configured rival.");
            Require(
                object.ReferenceEquals(
                    controller.RivalAgencies[1],
                    controller.FindAgencyById(CampaignController.CobaltAgencyId)),
                "Stable Cobalt lookup should resolve the second configured rival.");
        }

        public static void ConfiguredFundingIntervalSetsNextBoundary()
        {
            ResetEnvironment();
            CampaignSettings.FundingIntervalDays = 30.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.Refresh();

            Equal(30.0 * KerbinDaySeconds, controller.NextFundingUniversalTime);
        }

        public static void ScheduledRefreshCanSkipPlayerVesselObservation()
        {
            ResetEnvironment();
            const double observationUniversalTime = 1000.0;
            Planetarium.CurrentUniversalTime = observationUniversalTime;
            KspVesselMonitor.SetSnapshots(
                new List<OrbitingVesselSnapshot>
                {
                    new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, 0)
                },
                observationUniversalTime);

            var controller = new CampaignController();
            controller.FindAgencyById(CampaignController.AsterAgencyId).Funds = 0.0;
            controller.FindAgencyById(CampaignController.CobaltAgencyId).Funds = 0.0;
            QualifyForProbeOrbit(controller, observationUniversalTime - 1.0);

            bool skippedObservation = controller.Refresh(false);

            Require(!skippedObservation, "A scheduled non-vessel refresh should report no player observation.");
            Equal(0, KspVesselMonitor.CaptureCalls);
            Require(
                !controller.PlayerAgency.HasCompletedObjective(ObjectiveCatalogue.ProbeOrbitId),
                "Skipping the vessel observation must leave player Probe Orbit state unchanged.");
            Equal(1, RacePersistenceScenario.RivalCaptureCalls);
            Equal(1, RacePersistenceScenario.RaceProgressCaptureCalls);

            bool completedObservation = controller.Refresh(true);

            Require(completedObservation, "An available scheduled vessel refresh should report success.");
            Equal(1, KspVesselMonitor.CaptureCalls);
            Require(
                controller.PlayerAgency.HasCompletedObjective(ObjectiveCatalogue.ProbeOrbitId),
                "The next scheduled vessel observation should record the qualified Probe Orbit.");
        }

        public static void ProbeObservationUnlocksFundingFlow()
        {
            ResetEnvironment();
            const double observationUniversalTime = 1234.0;
            Planetarium.CurrentUniversalTime = observationUniversalTime;
            KspVesselMonitor.SetSnapshots(
                new List<OrbitingVesselSnapshot>
                {
                    new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, 0)
                },
                observationUniversalTime);

            var controller = new CampaignController();
            controller.FindAgencyById(CampaignController.AsterAgencyId).Funds = 0.0;
            controller.FindAgencyById(CampaignController.CobaltAgencyId).Funds = 0.0;
            QualifyForProbeOrbit(controller, observationUniversalTime - 1.0);

            controller.Refresh();

            ObjectiveFundingContract probeOrbit = FindAchievementProgramme(
                controller,
                ObjectiveCatalogue.ProbeOrbitId);
            ObjectiveFundingContract munProbeOrbit = FindAchievementProgramme(
                controller,
                ObjectiveCatalogue.MunProbeOrbitId);
            SatelliteNetworkFundingContract kerbinNetwork = FindSatelliteNetworkFundingContract(
                controller,
                FundingContractCatalogue.KerbinNetworkId);
            SatelliteNetworkFundingContract munNetwork = FindSatelliteNetworkFundingContract(
                controller,
                FundingContractCatalogue.MunNetworkId);

            Require(
                controller.PlayerAgency.HasCompletedObjective(ObjectiveCatalogue.ProbeOrbitId),
                "A qualified player Probe snapshot should flow through controller tracking into Probe Orbit.");
            Equal(
                observationUniversalTime,
                controller.PlayerAgency.GetObjectiveCompletionTime(ObjectiveCatalogue.ProbeOrbitId));
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
                "An unlocked one-off objectiveCompletion should wait for a funding-day sponsor review.");
            Require(
                !munNetwork.IsAvailable,
                "Mun satellite funding should remain locked until Mun Probe Orbit and Kerbin network progress are complete.");
            Equal(75000.0, controller.PlayerAgency.NextPayoutFunds);
            Equal(1, RacePersistenceScenario.RivalCaptureCalls);
            Equal(1, RacePersistenceScenario.RaceProgressCaptureCalls);
        }

        public static void KerbinNetworkProgressUnlocksMoonFunding()
        {
            ResetEnvironment();
            const double observationUniversalTime = 2000.0;
            Planetarium.CurrentUniversalTime = observationUniversalTime;
            KspVesselMonitor.SetSnapshots(
                new List<OrbitingVesselSnapshot>
                {
                    new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, 0),
                    new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, 0),
                    new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, 0),
                    new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, 0),
                    new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, 0),
                    new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, 0)
                },
                observationUniversalTime);

            var controller = new CampaignController();
            controller.FindAgencyById(CampaignController.AsterAgencyId).Funds = 0.0;
            controller.FindAgencyById(CampaignController.CobaltAgencyId).Funds = 0.0;
            QualifyForProbeOrbit(controller, observationUniversalTime - 1.0);

            controller.Refresh();

            SatelliteNetworkFundingContract munNetwork = FindSatelliteNetworkFundingContract(
                controller,
                FundingContractCatalogue.MunNetworkId);
            SatelliteNetworkFundingContract minmusNetwork = FindSatelliteNetworkFundingContract(
                controller,
                FundingContractCatalogue.MinmusNetworkId);

            Equal(6, controller.PlayerAgency.GetSatelliteCount("Kerbin"));
            Require(
                controller.PlayerAgency.HasCompletedObjective(ObjectiveCatalogue.ProbeOrbitId),
                "Six Kerbin probe satellites should record Probe Orbit once a starter line qualifies the programme.");
            Require(
                !controller.PlayerAgency.HasCompletedObjective(ObjectiveCatalogue.MunProbeOrbitId),
                "Mun Probe Orbit should still be incomplete before a Mun probe objectiveCompletion is recorded.");
            Require(
                !controller.PlayerAgency.HasCompletedObjective(ObjectiveCatalogue.MinmusProbeOrbitId),
                "Minmus Probe Orbit should still be incomplete before a Minmus probe objectiveCompletion is recorded.");
            Require(
                !munNetwork.IsAvailable,
                "Six collective Kerbin satellites alone must not unlock Mun satellite funding without Mun Probe Orbit.");
            Require(
                !minmusNetwork.IsAvailable,
                "Six collective Kerbin satellites alone must not unlock Minmus satellite funding without Minmus Probe Orbit.");

            controller.PlayerAgency.RecordObjectiveCompletion(
                ObjectiveCatalogue.MunProbeOrbitId,
                observationUniversalTime);
            controller.Refresh(false);

            Require(
                munNetwork.IsAvailable,
                "Mun Probe Orbit plus six collective Kerbin satellites should unlock Mun satellite funding.");
            Require(
                !minmusNetwork.IsAvailable,
                "Mun Probe Orbit must not satisfy Minmus satellite funding's Minmus Probe Orbit requirement.");

            controller.PlayerAgency.RecordObjectiveCompletion(
                ObjectiveCatalogue.MinmusProbeOrbitId,
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
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.FindAgencyById(CampaignController.AsterAgencyId).Funds = 0.0;
            controller.FindAgencyById(CampaignController.CobaltAgencyId).Funds = 0.0;
            QualifyForProbeOrbit(controller, 0.0);
            controller.Refresh();

            controller.PlayerAgency.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 1.0);
            controller.PlayerAgency.SetSatelliteCount("Kerbin", 1);

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh();

            ObjectiveFundingContract probeOrbit = FindAchievementProgramme(
                controller,
                ObjectiveCatalogue.ProbeOrbitId);
            SatelliteNetworkFundingContract kerbinNetwork = FindSatelliteNetworkFundingContract(
                controller,
                FundingContractCatalogue.KerbinNetworkId);

            Equal(75000.0, CareerFundingAdapter.TotalAddedFunds);
            Equal(1, CareerFundingAdapter.AddFundsCalls);
            Require(kerbinNetwork.IsOffered, "The funding review should offer the unlocked Kerbin network.");
            Equal(1, probeOrbit.PaymentsProcessed);
            Equal(90, probeOrbit.CurrentInterestPercent);
            Equal(FundingIntervalSeconds * 2.0, controller.NextFundingUniversalTime);
            Equal(87500.0, controller.PlayerAgency.NextPayoutFunds);
        }

        public static void RestoredOverdueFundingBoundaryIsProcessed()
        {
            ResetEnvironment();
            Planetarium.CurrentUniversalTime = FundingIntervalSeconds + 1.0;
            RacePersistenceScenario.RestoredNextFundingUniversalTime = FundingIntervalSeconds;
            KspVesselMonitor.SetUnavailable();

            var controller = new CampaignController();
            controller.FindAgencyById(CampaignController.AsterAgencyId).Funds = 0.0;
            controller.FindAgencyById(CampaignController.CobaltAgencyId).Funds = 0.0;

            controller.Refresh();

            Equal(
                20000.0,
                controller.FindAgencyById(CampaignController.AsterAgencyId).Funds);
            Equal(
                20000.0,
                controller.FindAgencyById(CampaignController.CobaltAgencyId).Funds);
            Equal(FundingIntervalSeconds * 2.0, controller.NextFundingUniversalTime);
            Equal(
                FundingIntervalSeconds * 2.0,
                RacePersistenceScenario.LastCapturedNextFundingUniversalTime);
        }

        public static void BoundaryObservationIsNotPaidRetroactively()
        {
            ResetEnvironment();
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselMonitor.SetSnapshots(new List<OrbitingVesselSnapshot>(), 0.0);

            var controller = new CampaignController();
            controller.FindAgencyById(CampaignController.AsterAgencyId).Funds = 0.0;
            controller.FindAgencyById(CampaignController.CobaltAgencyId).Funds = 0.0;
            controller.Refresh();

            // Qualifying after the campaign starts makes Probe Orbit available at the crossed
            // funding boundary, but the actual orbital observation still occurs after that payout.
            QualifyForProbeOrbit(controller, 1.0);
            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            KspVesselMonitor.SetSnapshots(
                new List<OrbitingVesselSnapshot>
                {
                    new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, 0)
                },
                FundingIntervalSeconds);

            controller.Refresh();

            ObjectiveFundingContract probeOrbit = FindAchievementProgramme(
                controller,
                ObjectiveCatalogue.ProbeOrbitId);
            SatelliteNetworkFundingContract kerbinNetwork = FindSatelliteNetworkFundingContract(
                controller,
                FundingContractCatalogue.KerbinNetworkId);

            Equal(0.0, CareerFundingAdapter.TotalAddedFunds);
            Equal(0, probeOrbit.PaymentsProcessed);
            Require(
                controller.PlayerAgency.HasCompletedObjective(ObjectiveCatalogue.ProbeOrbitId),
                "The boundary snapshot should still be recorded after due funding is processed.");
            Require(kerbinNetwork.IsAvailable, "The newly observed Probe Orbit should unlock future Kerbin funding.");
            Require(
                !kerbinNetwork.IsOffered,
                "A contract unlocked after the funding review must wait for the next funding date.");
            Equal(75000.0, controller.PlayerAgency.NextPayoutFunds);
        }

        public static void ProjectedPayoutCacheRebuildsOnRefresh()
        {
            ResetEnvironment();
            Planetarium.CurrentUniversalTime = 1000.0;
            KspVesselMonitor.SetSnapshots(
                new List<OrbitingVesselSnapshot>
                {
                    new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, 0)
                },
                1000.0);

            var controller = new CampaignController();
            controller.FindAgencyById(CampaignController.AsterAgencyId).Funds = 0.0;
            controller.FindAgencyById(CampaignController.CobaltAgencyId).Funds = 0.0;
            QualifyForProbeOrbit(controller, 999.0);
            controller.Refresh();

            ObjectiveFundingContract probeOrbit = FindAchievementProgramme(
                controller,
                ObjectiveCatalogue.ProbeOrbitId);
            SatelliteNetworkFundingContract kerbinNetwork = FindSatelliteNetworkFundingContract(
                controller,
                FundingContractCatalogue.KerbinNetworkId);

            kerbinNetwork.Offer();
            controller.Refresh(false);

            Equal(
                20000.0,
                controller.GetSatelliteCurrentPayout(controller.PlayerAgency, kerbinNetwork));
            Equal(
                75000.0,
                controller.GetAchievementCurrentPayout(controller.PlayerAgency, probeOrbit));

            Planetarium.CurrentUniversalTime = 2000.0;
            KspVesselMonitor.SetSnapshots(new List<OrbitingVesselSnapshot>(), 2000.0);
            controller.Refresh();

            Equal(
                0.0,
                controller.GetSatelliteCurrentPayout(controller.PlayerAgency, kerbinNetwork));
            Equal(
                75000.0,
                controller.GetAchievementCurrentPayout(controller.PlayerAgency, probeOrbit));
            Equal(75000.0, controller.PlayerAgency.NextPayoutFunds);
        }

        private static void QualifyForProbeOrbit(
            CampaignController controller,
            double achievementUniversalTime)
        {
            controller.PlayerAgency.RecordObjectiveCompletion(
                ObjectiveCatalogue.DirectedPower5Id,
                achievementUniversalTime);
        }

        private static ObjectiveFundingContract FindAchievementProgramme(
            CampaignController controller,
            string programmeId)
        {
            for (int programmeIndex = 0;
                programmeIndex < controller.ObjectiveFundingContracts.Count;
                programmeIndex++)
            {
                ObjectiveFundingContract programme =
                    controller.ObjectiveFundingContracts[programmeIndex];
                if (programme != null
                    && string.Equals(programme.Id, programmeId, StringComparison.OrdinalIgnoreCase))
                {
                    return programme;
                }
            }

            throw new InvalidOperationException(
                "Missing objectiveCompletion funding programme '" + programmeId + "'.");
        }

        private static SatelliteNetworkFundingContract FindSatelliteNetworkFundingContract(
            CampaignController controller,
            string programmeId)
        {
            for (int programmeIndex = 0; programmeIndex < controller.SatelliteNetworkFundingContracts.Count; programmeIndex++)
            {
                SatelliteNetworkFundingContract programme = controller.SatelliteNetworkFundingContracts[programmeIndex];
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
            CampaignSettings.ResetToDefaults();
            Planetarium.Reset();
            CareerFundingAdapter.Reset();
            KspVesselMonitor.Reset();
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

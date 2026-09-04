using System;
using TheRaceForSpace.Competition;
using TheRaceForSpace.Core;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.ControllerTests
{
    internal static class ActiveStarterEvaluationTests
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double FundingIntervalSeconds = 90.0 * KerbinDaySeconds;

        public static void OfferedMassLevelsCompleteIndependently()
        {
            SatelliteRaceController controller = CreateControllerWithSecondLevelOffered(
                PrototypeMilestones.Mass1Id);
            var tracker = new StarterFlightTracker();

            Require(Contains(controller, PrototypeMilestones.Mass1Id),
                "Mass I should remain active for the player after a rival unlocks Mass II.");
            Require(Contains(controller, PrototypeMilestones.Mass2Id),
                "Mass II should join the active set after sponsor review.");
            Require(!Contains(controller, PrototypeMilestones.Mass3Id),
                "Mass III must remain inactive until it is separately offered.");

            tracker.RefreshPlayerMilestones(
                controller.PlayerProgram,
                controller.Programs,
                controller.ActiveStarterContracts,
                Snapshot("mass-independent", 100.0, 100.0, 100.0, 0.0, 6.0, 0, null,
                    TrackedFlightSituation.Prelaunch, 0.0));

            bool recorded = tracker.RefreshPlayerMilestones(
                controller.PlayerProgram,
                controller.Programs,
                controller.ActiveStarterContracts,
                Snapshot("mass-independent", 100.0, 110.0, 0.0, 0.0, 6.0, 0, null,
                    TrackedFlightSituation.Landed, 20.0));

            Require(recorded,
                "One landing satisfying both offered Mass contracts should record achievements.");
            Require(controller.PlayerProgram.HasAchievement(PrototypeMilestones.Mass1Id),
                "Mass I should complete from the shared qualifying landing.");
            Require(controller.PlayerProgram.HasAchievement(PrototypeMilestones.Mass2Id),
                "Mass II should complete independently from the same qualifying landing.");
            Require(!controller.PlayerProgram.HasAchievement(PrototypeMilestones.Mass3Id),
                "Mass III must not be checked merely because the landing also satisfies its criteria.");
        }

        public static void OfferedDirectedPowerLevelsCompleteIndependently()
        {
            SatelliteRaceController controller = CreateControllerWithSecondLevelOffered(
                PrototypeMilestones.DirectedPower1Id);
            var tracker = new StarterFlightTracker();

            tracker.RefreshPlayerMilestones(
                controller.PlayerProgram,
                controller.Programs,
                controller.ActiveStarterContracts,
                Snapshot("power-independent", 200.0, 200.0, 100.0, 0.0, 1.0, 0, null,
                    TrackedFlightSituation.Prelaunch));
            tracker.RefreshPlayerMilestones(
                controller.PlayerProgram,
                controller.Programs,
                controller.ActiveStarterContracts,
                Snapshot("power-independent", 200.0, 210.0, 50000.0, 1200.0, 1.0, 0, null,
                    TrackedFlightSituation.Flying));

            bool recorded = tracker.RecordSurfaceImpact(
                controller.PlayerProgram,
                controller.Programs,
                controller.ActiveStarterContracts,
                "power-independent",
                "Kerbin",
                211.0);

            Require(recorded,
                "One qualifying impact should record every active Directed Power contract it satisfies.");
            Require(controller.PlayerProgram.HasAchievement(PrototypeMilestones.DirectedPower1Id),
                "Directed Power I should complete from the qualifying impact.");
            Require(controller.PlayerProgram.HasAchievement(PrototypeMilestones.DirectedPower2Id),
                "Directed Power II should complete independently from the same impact.");
            Require(!controller.PlayerProgram.HasAchievement(PrototypeMilestones.DirectedPower3Id),
                "Directed Power III must remain untouched while it is not offered.");
        }

        public static void OfferedBiomeLevelsRemainIndependentWithinOneLaunch()
        {
            SatelliteRaceController controller = CreateControllerWithSecondLevelOffered(
                PrototypeMilestones.Biome1Id);
            var tracker = new StarterFlightTracker();

            tracker.RefreshPlayerMilestones(
                controller.PlayerProgram,
                controller.Programs,
                controller.ActiveStarterContracts,
                Snapshot("biome-independent", 300.0, 300.0, 100.0, 0.0, 1.0, 0, "Shores",
                    TrackedFlightSituation.Prelaunch));

            Require(tracker.RefreshPlayerMilestones(
                    controller.PlayerProgram,
                    controller.Programs,
                    controller.ActiveStarterContracts,
                    Snapshot("biome-independent", 300.0, 310.0, 0.0, 0.0, 1.0, 0, "Grasslands",
                        TrackedFlightSituation.Landed)),
                "Landing in Grasslands should complete the active Biome I contract.");
            Require(controller.PlayerProgram.HasAchievement(PrototypeMilestones.Biome1Id),
                "Biome I should be recorded.");
            Require(!controller.PlayerProgram.HasAchievement(PrototypeMilestones.Biome2Id),
                "Biome II should remain incomplete until the craft lands in Highlands.");

            // Match the live runtime: completion invalidates the controller cache immediately, but
            // the same vessel attempt remains alive and can continue toward another offered contract.
            controller.NotifyPlayerStarterAchievementRecorded();
            Planetarium.CurrentUniversalTime = FundingIntervalSeconds + 20.0;
            controller.Refresh(false);

            Require(!Contains(controller, PrototypeMilestones.Biome1Id),
                "Completed Biome I should leave the active set after the controller refresh.");
            Require(Contains(controller, PrototypeMilestones.Biome2Id),
                "Unfinished offered Biome II should remain active.");

            tracker.RefreshPlayerMilestones(
                controller.PlayerProgram,
                controller.Programs,
                controller.ActiveStarterContracts,
                Snapshot("biome-independent", 300.0, 320.0, 1000.0, 100.0, 1.0, 0, "Highlands",
                    TrackedFlightSituation.Flying));
            Require(tracker.RefreshPlayerMilestones(
                    controller.PlayerProgram,
                    controller.Programs,
                    controller.ActiveStarterContracts,
                    Snapshot("biome-independent", 300.0, 321.0, 0.0, 0.0, 1.0, 0, "Highlands",
                        TrackedFlightSituation.Landed)),
                "The same launch should be able to complete independently offered Biome II later.");
            Require(controller.PlayerProgram.HasAchievement(PrototypeMilestones.Biome2Id),
                "Biome II should complete independently from Biome I.");
            Require(!controller.PlayerProgram.HasAchievement(PrototypeMilestones.Biome3Id),
                "Biome III must remain inactive until separately offered.");
        }

        public static void OfferedControlLevelsTrackAndCompleteIndependently()
        {
            SatelliteRaceController controller = CreateControllerWithSecondLevelOffered(
                PrototypeMilestones.Control1Id);
            var tracker = new StarterFlightTracker();

            Require(Contains(controller, PrototypeMilestones.Control1Id),
                "Control I should remain active after a rival unlocks Control II.");
            Require(Contains(controller, PrototypeMilestones.Control2Id),
                "Control II should join the active set after sponsor review.");
            Require(!Contains(controller, PrototypeMilestones.Control3Id),
                "Control III must remain inactive until separately offered.");

            for (int elapsedSeconds = 0; elapsedSeconds <= 30; elapsedSeconds += 5)
            {
                tracker.RefreshPlayerMilestones(
                    controller.PlayerProgram,
                    controller.Programs,
                    controller.ActiveStarterContracts,
                    Snapshot(
                        "control-independent",
                        400.0,
                        400.0 + elapsedSeconds,
                        3000.0,
                        150.0,
                        1.0,
                        1,
                        null,
                        TrackedFlightSituation.Flying));
            }

            Require(tracker.IsControlMilestoneQualified(PrototypeMilestones.Control1Id),
                "Control I should retain its own qualified hold state.");
            Require(!tracker.IsControlMilestoneQualified(PrototypeMilestones.Control2Id),
                "Control II should remain unqualified until its own altitude hold is completed.");

            for (int observationUniversalTime = 435; observationUniversalTime <= 480;
                observationUniversalTime += 5)
            {
                tracker.RefreshPlayerMilestones(
                    controller.PlayerProgram,
                    controller.Programs,
                    controller.ActiveStarterContracts,
                    Snapshot(
                        "control-independent",
                        400.0,
                        observationUniversalTime,
                        10000.0,
                        150.0,
                        1.0,
                        1,
                        null,
                        TrackedFlightSituation.Flying));
            }

            Require(tracker.IsControlMilestoneQualified(PrototypeMilestones.Control1Id),
                "Leaving Control I's band after qualification must not erase its completed hold.");
            Require(tracker.IsControlMilestoneQualified(PrototypeMilestones.Control2Id),
                "Control II should qualify independently after its own 45-second hold.");
            Require(tracker.GetControlHoldSeconds(PrototypeMilestones.Control1Id) >= 30.0,
                "Control I should retain its own accumulated hold duration.");
            Require(tracker.GetControlHoldSeconds(PrototypeMilestones.Control2Id) >= 45.0,
                "Control II should retain its own accumulated hold duration.");

            bool recorded = tracker.RefreshPlayerMilestones(
                controller.PlayerProgram,
                controller.Programs,
                controller.ActiveStarterContracts,
                Snapshot(
                    "control-independent",
                    400.0,
                    481.0,
                    80.0,
                    0.0,
                    1.0,
                    1,
                    null,
                    TrackedFlightSituation.Landed));

            Require(recorded,
                "One safe landing should complete every active Control contract whose own hold qualified.");
            Require(controller.PlayerProgram.HasAchievement(PrototypeMilestones.Control1Id),
                "Control I should complete from the shared safe landing.");
            Require(controller.PlayerProgram.HasAchievement(PrototypeMilestones.Control2Id),
                "Control II should complete independently from the same safe landing.");
            Require(!controller.PlayerProgram.HasAchievement(PrototypeMilestones.Control3Id),
                "Control III must remain untouched while it is not offered.");
        }

        private static SatelliteRaceController CreateControllerWithSecondLevelOffered(
            string firstLevelMilestoneId)
        {
            ResetEnvironment();
            RaceSettings.RivalProgressChance = 0.0;
            Planetarium.CurrentUniversalTime = 0.0;
            KspVesselDiscovery.SetUnavailable();

            var controller = new SatelliteRaceController();
            controller.Refresh();
            controller.FindProgramById(SatelliteRaceController.AsterProgramId)
                .RecordAchievement(firstLevelMilestoneId, 10.0);

            Planetarium.CurrentUniversalTime = FundingIntervalSeconds;
            controller.Refresh(false);
            return controller;
        }

        private static bool Contains(SatelliteRaceController controller, string milestoneId)
        {
            for (int milestoneIndex = 0; milestoneIndex < controller.ActiveStarterContracts.Count; milestoneIndex++)
            {
                MilestoneDefinition milestone = controller.ActiveStarterContracts[milestoneIndex];
                if (milestone != null
                    && string.Equals(milestone.Id, milestoneId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static ActiveVesselTrackingSnapshot Snapshot(
            string vesselId,
            double launchUniversalTime,
            double observationUniversalTime,
            double altitudeMeters,
            double surfaceSpeedMetersPerSecond,
            double massTonnes,
            int crewCount,
            string biomeName,
            TrackedFlightSituation situation,
            double longitudeDegrees = 0.0)
        {
            return new ActiveVesselTrackingSnapshot(
                vesselId,
                "Kerbin",
                situation,
                altitudeMeters,
                surfaceSpeedMetersPerSecond,
                massTonnes,
                0.0,
                longitudeDegrees,
                600000.0,
                biomeName,
                crewCount,
                launchUniversalTime,
                observationUniversalTime);
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
    }
}

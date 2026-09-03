using System;
using System.Collections.Generic;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.Tests.Tracking
{
    internal static class SatelliteTrackerTests
    {
        public static void NormalizedSnapshotsUpdateCountsAndMilestones()
        {
            var playerProgram = new SpaceProgramState("player", "Player", true);
            playerProgram.RecordAchievement(PrototypeMilestones.DirectedPower5Id, 1200.0);
            var programs = new List<SpaceProgramState> { playerProgram };
            var vesselSnapshots = new List<VesselTrackingSnapshot>
            {
                new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 0),
                new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Other, 1),
                new VesselTrackingSnapshot("Mun", TrackedVesselType.Relay, 0),
                new VesselTrackingSnapshot("Eve", TrackedVesselType.Probe, 0)
            };

            SatelliteTracker.RefreshPlayerSatelliteCounts(
                playerProgram,
                programs,
                PrototypeMilestones.All,
                vesselSnapshots,
                1234.0);

            RequireEqual(1, playerProgram.GetSatelliteCount("Kerbin"), "Probe should count as one Kerbin satellite.");
            RequireEqual(1, playerProgram.GetSatelliteCount("Mun"), "Relay should count as one Mun satellite.");
            RequireEqual(1, playerProgram.GetSatelliteCount("Eve"), "A body outside the milestone catalogue should still receive a satellite count.");
            Require(playerProgram.HasAchievement(PrototypeMilestones.ProbeOrbitId), "Qualified uncrewed probe should satisfy Probe Orbit.");
            Require(playerProgram.HasAchievement(PrototypeMilestones.CrewedOrbitId), "Crewed vessel should satisfy Crewed Orbit after Probe Orbit unlocks it in the same stable refresh.");
            Require(playerProgram.HasAchievement(PrototypeMilestones.MunProbeOrbitId), "Mun relay should satisfy Mun Probe Orbit after its unlock rule becomes satisfied.");
            RequireEqual(
                1234.0,
                playerProgram.GetAchievementUniversalTime(PrototypeMilestones.MunProbeOrbitId),
                "Milestones recorded from one snapshot refresh should use the supplied observation time.");
        }

        public static void EmptySnapshotsResetTrackedBodyCounts()
        {
            var playerProgram = new SpaceProgramState("player", "Player", true);
            playerProgram.SetSatelliteCount("Kerbin", 3);
            playerProgram.SetSatelliteCount("Mun", 2);
            playerProgram.SetSatelliteCount("Eve", 1);

            SatelliteTracker.RefreshPlayerSatelliteCounts(
                playerProgram,
                null,
                null,
                new List<VesselTrackingSnapshot>(),
                2000.0);

            RequireEqual(0, playerProgram.GetSatelliteCount("Kerbin"), "Missing Kerbin snapshots should clear the previous count.");
            RequireEqual(0, playerProgram.GetSatelliteCount("Mun"), "Missing Mun snapshots should clear the previous count.");
            RequireEqual(0, playerProgram.GetSatelliteCount("Eve"), "Missing arbitrary-body snapshots should clear the previous count without milestone definitions.");
        }

        public static void CrewedProbeCountsAsSatelliteButNotProbeMilestone()
        {
            var playerProgram = new SpaceProgramState("player", "Player", true);
            var programs = new List<SpaceProgramState> { playerProgram };
            var vesselSnapshots = new List<VesselTrackingSnapshot>
            {
                new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 1)
            };

            SatelliteTracker.RefreshPlayerSatelliteCounts(
                playerProgram,
                programs,
                PrototypeMilestones.All,
                vesselSnapshots,
                3000.0);

            RequireEqual(1, playerProgram.GetSatelliteCount("Kerbin"), "A crewed probe remains a qualifying satellite for network counts.");
            Require(!playerProgram.HasAchievement(PrototypeMilestones.ProbeOrbitId), "A crewed probe should not satisfy the uncrewed Probe Orbit milestone.");
            Require(
                !playerProgram.HasAchievement(PrototypeMilestones.CrewedOrbitId),
                "Crewed Orbit must stay locked when no agency has achieved Probe Orbit.");

            playerProgram.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 3001.0);
            SatelliteTracker.RefreshPlayerSatelliteCounts(
                playerProgram,
                programs,
                PrototypeMilestones.All,
                vesselSnapshots,
                3002.0);

            Require(
                playerProgram.HasAchievement(PrototypeMilestones.CrewedOrbitId),
                "The same crewed-orbit observation should become eligible after Probe Orbit is achieved.");
            RequireEqual(
                3002.0,
                playerProgram.GetAchievementUniversalTime(PrototypeMilestones.CrewedOrbitId),
                "Crewed Orbit should record the later observation time after its Probe Orbit prerequisite is met.");
        }

        public static void FlexibleUnlockRuleUsesRaceStateAndTime()
        {
            var playerProgram = new SpaceProgramState("player", "Player", true);
            var rivalProgram = new SpaceProgramState("rival", "Rival", false);
            rivalProgram.RecordAchievement("rival-breakthrough", 500.0);
            var programs = new List<SpaceProgramState> { playerProgram, rivalProgram };
            var milestones = new List<MilestoneDefinition>
            {
                new MilestoneDefinition(
                    "eve-response",
                    "Eve Response",
                    "Eve",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Orbit Eve after the rival breakthrough and campaign time gate.",
                    new UnlockRuleDefinition(
                        new UnlockPathDefinition(
                            UnlockConditionDefinition.Achievement(
                                "rival-breakthrough",
                                UnlockProgramScope.AnyRival),
                            UnlockConditionDefinition.AfterUniversalTime(600.0))))
            };
            var vesselSnapshots = new List<VesselTrackingSnapshot>
            {
                new VesselTrackingSnapshot("Eve", TrackedVesselType.Probe, 0)
            };

            SatelliteTracker.RefreshPlayerSatelliteCounts(
                playerProgram,
                programs,
                milestones,
                vesselSnapshots,
                599.0);
            Require(
                !playerProgram.HasAchievement("eve-response"),
                "The tracker should keep a milestone locked until every condition in its path is satisfied.");

            SatelliteTracker.RefreshPlayerSatelliteCounts(
                playerProgram,
                programs,
                milestones,
                vesselSnapshots,
                600.0);
            Require(
                playerProgram.HasAchievement("eve-response"),
                "The tracker should use rival state and exact universal time through the shared evaluator.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void RequireEqual(int expected, int actual, string message)
        {
            if (expected != actual)
            {
                throw new InvalidOperationException(message + " Expected " + expected + ", got " + actual + ".");
            }
        }

        private static void RequireEqual(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 0.000001)
            {
                throw new InvalidOperationException(message + " Expected " + expected + ", got " + actual + ".");
            }
        }
    }
}

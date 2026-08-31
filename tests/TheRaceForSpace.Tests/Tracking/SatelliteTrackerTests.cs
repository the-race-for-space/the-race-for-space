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
            var availableMilestoneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PrototypeMilestones.ProbeOrbitId,
                PrototypeMilestones.CrewedOrbitId
            };
            var vesselSnapshots = new List<VesselTrackingSnapshot>
            {
                new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 0),
                new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Other, 1),
                new VesselTrackingSnapshot("Mun", TrackedVesselType.Relay, 0)
            };

            SatelliteTracker.RefreshPlayerSatelliteCounts(
                playerProgram,
                PrototypeMilestones.All,
                availableMilestoneIds,
                vesselSnapshots,
                1234.0);

            RequireEqual(1, playerProgram.GetSatelliteCount("Kerbin"), "Probe should count as one Kerbin satellite.");
            RequireEqual(1, playerProgram.GetSatelliteCount("Mun"), "Relay should count as one Mun satellite.");
            Require(playerProgram.HasAchievement(PrototypeMilestones.ProbeOrbitId), "Uncrewed probe should satisfy Probe Orbit.");
            Require(playerProgram.HasAchievement(PrototypeMilestones.CrewedOrbitId), "Crewed vessel should satisfy Crewed Orbit.");
            Require(playerProgram.HasAchievement(PrototypeMilestones.MunProbeOrbitId), "Mun relay should satisfy Mun Probe Orbit after its prerequisite is recorded.");
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

            SatelliteTracker.RefreshPlayerSatelliteCounts(
                playerProgram,
                PrototypeMilestones.All,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new List<VesselTrackingSnapshot>(),
                2000.0);

            RequireEqual(0, playerProgram.GetSatelliteCount("Kerbin"), "Missing Kerbin snapshots should clear the previous count.");
            RequireEqual(0, playerProgram.GetSatelliteCount("Mun"), "Missing Mun snapshots should clear the previous count.");
        }

        public static void CrewedProbeCountsAsSatelliteButNotProbeMilestone()
        {
            var playerProgram = new SpaceProgramState("player", "Player", true);
            var availableMilestoneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PrototypeMilestones.ProbeOrbitId,
                PrototypeMilestones.CrewedOrbitId
            };
            var vesselSnapshots = new List<VesselTrackingSnapshot>
            {
                new VesselTrackingSnapshot("Kerbin", TrackedVesselType.Probe, 1)
            };

            SatelliteTracker.RefreshPlayerSatelliteCounts(
                playerProgram,
                PrototypeMilestones.All,
                availableMilestoneIds,
                vesselSnapshots,
                3000.0);

            RequireEqual(1, playerProgram.GetSatelliteCount("Kerbin"), "A crewed probe remains a qualifying satellite for network counts.");
            Require(!playerProgram.HasAchievement(PrototypeMilestones.ProbeOrbitId), "A crewed probe should not satisfy the uncrewed Probe Orbit milestone.");
            Require(playerProgram.HasAchievement(PrototypeMilestones.CrewedOrbitId), "A crewed probe should satisfy the Crewed Orbit milestone.");
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

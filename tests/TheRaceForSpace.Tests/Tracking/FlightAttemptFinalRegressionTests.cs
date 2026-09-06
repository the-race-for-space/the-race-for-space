using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.Tests.Tracking
{
    /// <summary>
    /// Final Persistent Flight Attempt regressions that close lifecycle gaps not already covered by
    /// FlightContractTrackerTests. KSP-facing presentation still requires the documented live test.
    /// </summary>
    internal static class FlightAttemptFinalRegressionTests
    {
        internal static void RunAll()
        {
            ConstructedCraftWithReusedVesselIdStartsFreshAttempt();
            DestructionRemovesOnlyImpactedAttempt();
            ActiveVesselSnapshotCarriesPresentationName();
            PrelaunchSurfaceSpeedStartsAtZero();
        }

        private static void ConstructedCraftWithReusedVesselIdStartsFreshAttempt()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();
            var noActiveContracts = new List<ObjectiveDefinition>();

            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "reused-vessel-id",
                    3000.0,
                    3000.0,
                    20000.0,
                    700.0,
                    new uint[] { 10001u, 10002u },
                    10001u,
                    "Original Craft"));

            // Construction mods and KSP topology changes can produce a craft whose vessel ID is not
            // useful as permanent identity. Completely different persistent parts must win over the
            // reused ID and start a fresh attempt instead of inheriting the old craft's history.
            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "reused-vessel-id",
                    3000.0,
                    3010.0,
                    5000.0,
                    150.0,
                    new uint[] { 11001u, 11002u },
                    11001u,
                    "Constructed Craft"));

            RequireNear(
                150.0,
                tracker.MaximumSurfaceSpeedMetersPerSecond,
                "A constructed/replacement craft with a reused vessel ID must start with its own history when none of the original lineage parts remain.");

            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "original-craft-returned",
                    3000.0,
                    3020.0,
                    21000.0,
                    400.0,
                    new uint[] { 10001u, 10002u },
                    10001u,
                    "Original Craft"));

            RequireNear(
                700.0,
                tracker.MaximumSurfaceSpeedMetersPerSecond,
                "The original surviving lineage must remain independently recoverable after another craft reused its old vessel ID.");
        }

        private static void DestructionRemovesOnlyImpactedAttempt()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();
            var noActiveContracts = new List<ObjectiveDefinition>();

            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "destruction-a",
                    3100.0,
                    3100.0,
                    20000.0,
                    650.0,
                    new uint[] { 12001u, 12002u },
                    12001u,
                    "Craft A"));
            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "destruction-b",
                    3200.0,
                    3200.0,
                    10000.0,
                    300.0,
                    new uint[] { 13001u, 13002u },
                    13001u,
                    "Craft B"));

            tracker.RecordSurfaceImpact(
                player,
                noActiveContracts,
                "destruction-b",
                "Kerbin",
                3201.0);

            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "destruction-a",
                    3100.0,
                    3210.0,
                    22000.0,
                    400.0,
                    new uint[] { 12001u, 12002u },
                    12001u,
                    "Craft A"));

            RequireNear(
                650.0,
                tracker.MaximumSurfaceSpeedMetersPerSecond,
                "Destroying one remembered craft must not erase another surviving craft's Flight Attempt history.");
        }

        private static void ActiveVesselSnapshotCarriesPresentationName()
        {
            ActiveVesselSnapshot snapshot = Snapshot(
                "presentation",
                3300.0,
                3300.0,
                100.0,
                0.0,
                new uint[] { 14001u },
                14001u,
                "Explorer One");

            Require(
                string.Equals(snapshot.VesselName, "Explorer One", StringComparison.Ordinal),
                "The KSP-independent active snapshot should carry the vessel display name used by FlightActiveUI presentation.");
        }

        private static void PrelaunchSurfaceSpeedStartsAtZero()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();
            var noActiveContracts = new List<ObjectiveDefinition>();

            var prelaunchSnapshot = new ActiveVesselSnapshot(
                "prelaunch-speed",
                "Kerbin",
                FlightSituation.Prelaunch,
                70.0,
                175.0,
                1.0,
                0.0,
                0.0,
                600000.0,
                null,
                0,
                3400.0,
                3400.0,
                new uint[] { 15001u },
                15001u,
                "Launchpad Craft");

            RequireNear(
                0.0,
                prelaunchSnapshot.SurfaceSpeedMetersPerSecond,
                "PRELAUNCH telemetry must normalize transient body-rotation speed to zero.");

            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                prelaunchSnapshot);

            RequireNear(
                0.0,
                tracker.MaximumSurfaceSpeedMetersPerSecond,
                "A new Flight Attempt must not start with a false prelaunch maximum speed.");

            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                new ActiveVesselSnapshot(
                    "prelaunch-speed",
                    "Kerbin",
                    FlightSituation.Flying,
                    500.0,
                    220.0,
                    1.0,
                    0.0,
                    0.0,
                    600000.0,
                    null,
                    0,
                    3400.0,
                    3401.0,
                    new uint[] { 15001u },
                    15001u,
                    "Launchpad Craft"));

            RequireNear(
                220.0,
                tracker.MaximumSurfaceSpeedMetersPerSecond,
                "Normal surface speed sampling must resume after the craft leaves PRELAUNCH.");
        }

        private static ActiveVesselSnapshot Snapshot(
            string vesselId,
            double launchUniversalTime,
            double observationUniversalTime,
            double altitudeMeters,
            double surfaceSpeedMetersPerSecond,
            IList<uint> partPersistentIds,
            uint referencePartPersistentId,
            string vesselName)
        {
            return new ActiveVesselSnapshot(
                vesselId,
                "Kerbin",
                FlightSituation.Flying,
                altitudeMeters,
                surfaceSpeedMetersPerSecond,
                1.0,
                0.0,
                0.0,
                600000.0,
                null,
                0,
                launchUniversalTime,
                observationUniversalTime,
                partPersistentIds,
                referencePartPersistentId,
                vesselName);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void RequireNear(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 0.000001)
            {
                throw new InvalidOperationException(
                    message + " Expected " + expected + ", got " + actual + ".");
            }
        }
    }
}

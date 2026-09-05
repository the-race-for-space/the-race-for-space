using System;
using System.Collections.Generic;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.Tests.Tracking
{
    internal static class OrbitalVesselTrackerTests
    {
        public static void NormalizedSnapshotsUpdateCountsAndObjectives()
        {
            FlightContractTrackerTests.RunAll();

            var playerAgency = new AgencyState("player", "Player", true);
            playerAgency.RecordObjectiveCompletion(ObjectiveCatalogue.DirectedPower5Id, 1200.0);
            var agencies = new List<AgencyState> { playerAgency };
            var vesselSnapshots = new List<OrbitingVesselSnapshot>
            {
                new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, 0),
                new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Other, 1),
                new OrbitingVesselSnapshot("Mun", OrbitalVesselType.Relay, 0),
                new OrbitingVesselSnapshot("Eve", OrbitalVesselType.Probe, 0)
            };

            OrbitalVesselTracker.RefreshOrbitalProgress(
                playerAgency,
                agencies,
                ObjectiveCatalogue.All,
                vesselSnapshots,
                1234.0);

            RequireEqual(1, playerAgency.GetSatelliteCount("Kerbin"), "Probe should count as one Kerbin satellite.");
            RequireEqual(1, playerAgency.GetSatelliteCount("Mun"), "Relay should count as one Mun satellite.");
            RequireEqual(1, playerAgency.GetSatelliteCount("Eve"), "A body outside the objective catalogue should still receive a satellite count.");
            Require(playerAgency.HasCompletedObjective(ObjectiveCatalogue.ProbeOrbitId), "Qualified uncrewed probe should satisfy Probe Orbit.");
            Require(playerAgency.HasCompletedObjective(ObjectiveCatalogue.CrewedOrbitId), "Crewed vessel should satisfy Crewed Orbit after Probe Orbit unlocks it in the same stable refresh.");
            Require(playerAgency.HasCompletedObjective(ObjectiveCatalogue.MunProbeOrbitId), "Mun relay should satisfy Mun Probe Orbit after its unlock rule becomes satisfied.");
            RequireEqual(
                1234.0,
                playerAgency.GetObjectiveCompletionTime(ObjectiveCatalogue.MunProbeOrbitId),
                "Objectives recorded from one snapshot refresh should use the supplied observation time.");

            CurrentSnapshotSatelliteCountUnlocksObjective();
        }

        public static void EmptySnapshotsResetTrackedBodyCounts()
        {
            var playerAgency = new AgencyState("player", "Player", true);
            playerAgency.SetSatelliteCount("Kerbin", 3);
            playerAgency.SetSatelliteCount("Mun", 2);
            playerAgency.SetSatelliteCount("Eve", 1);

            OrbitalVesselTracker.RefreshOrbitalProgress(
                playerAgency,
                null,
                null,
                new List<OrbitingVesselSnapshot>(),
                2000.0);

            RequireEqual(0, playerAgency.GetSatelliteCount("Kerbin"), "Missing Kerbin snapshots should clear the previous count.");
            RequireEqual(0, playerAgency.GetSatelliteCount("Mun"), "Missing Mun snapshots should clear the previous count.");
            RequireEqual(0, playerAgency.GetSatelliteCount("Eve"), "Missing arbitrary-body snapshots should clear the previous count without objective definitions.");
        }

        public static void CrewedProbeCountsAsSatelliteButNotProbeObjective()
        {
            var playerAgency = new AgencyState("player", "Player", true);
            var agencies = new List<AgencyState> { playerAgency };
            var vesselSnapshots = new List<OrbitingVesselSnapshot>
            {
                new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, 1)
            };

            OrbitalVesselTracker.RefreshOrbitalProgress(
                playerAgency,
                agencies,
                ObjectiveCatalogue.All,
                vesselSnapshots,
                3000.0);

            RequireEqual(1, playerAgency.GetSatelliteCount("Kerbin"), "A crewed probe remains a qualifying satellite for network counts.");
            Require(!playerAgency.HasCompletedObjective(ObjectiveCatalogue.ProbeOrbitId), "A crewed probe should not satisfy the uncrewed Probe Orbit objective.");
            Require(
                !playerAgency.HasCompletedObjective(ObjectiveCatalogue.CrewedOrbitId),
                "Crewed Orbit must stay locked when no agency has achieved Probe Orbit.");

            playerAgency.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 3001.0);
            OrbitalVesselTracker.RefreshOrbitalProgress(
                playerAgency,
                agencies,
                ObjectiveCatalogue.All,
                vesselSnapshots,
                3002.0);

            Require(
                playerAgency.HasCompletedObjective(ObjectiveCatalogue.CrewedOrbitId),
                "The same crewed-orbit observation should become eligible after Probe Orbit is achieved.");
            RequireEqual(
                3002.0,
                playerAgency.GetObjectiveCompletionTime(ObjectiveCatalogue.CrewedOrbitId),
                "Crewed Orbit should record the later observation time after its Probe Orbit prerequisite is met.");

            NegativeCrewCountDoesNotQualifyAsUncrewed();
        }

        public static void FlexibleUnlockRuleUsesRaceStateAndTime()
        {
            var playerAgency = new AgencyState("player", "Player", true);
            var rivalProgram = new AgencyState("rival", "Rival", false);
            rivalProgram.RecordObjectiveCompletion("rival-breakthrough", 500.0);
            var agencies = new List<AgencyState> { playerAgency, rivalProgram };
            var objectives = new List<ObjectiveDefinition>
            {
                new ObjectiveDefinition(
                    "eve-response",
                    "Eve Response",
                    "Eve",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Orbit Eve after the rival breakthrough and campaign time gate.",
                    new UnlockRuleDefinition(
                        new UnlockPathDefinition(
                            UnlockConditionDefinition.ObjectiveCompletion(
                                "rival-breakthrough",
                                UnlockAgencyScope.AnyRival),
                            UnlockConditionDefinition.AfterUniversalTime(600.0))))
            };
            var vesselSnapshots = new List<OrbitingVesselSnapshot>
            {
                new OrbitingVesselSnapshot("Eve", OrbitalVesselType.Probe, 0)
            };

            OrbitalVesselTracker.RefreshOrbitalProgress(
                playerAgency,
                agencies,
                objectives,
                vesselSnapshots,
                599.0);
            Require(
                !playerAgency.HasCompletedObjective("eve-response"),
                "The tracker should keep a objective locked until every condition in its path is satisfied.");

            OrbitalVesselTracker.RefreshOrbitalProgress(
                playerAgency,
                agencies,
                objectives,
                vesselSnapshots,
                600.0);
            Require(
                playerAgency.HasCompletedObjective("eve-response"),
                "The tracker should use rival state and exact universal time through the shared evaluator.");
        }

        private static void CurrentSnapshotSatelliteCountUnlocksObjective()
        {
            var playerAgency = new AgencyState("player", "Player", true);
            var agencies = new List<AgencyState> { playerAgency };
            var objectives = new List<ObjectiveDefinition>
            {
                new ObjectiveDefinition(
                    "kerbin-satellite-gate",
                    "Kerbin Satellite Gate",
                    "Kerbin",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Orbit an uncrewed probe once one Kerbin satellite is present.",
                    new UnlockRuleDefinition(
                        new UnlockPathDefinition(
                            UnlockConditionDefinition.SatelliteCount("Kerbin", 1))))
            };
            var vesselSnapshots = new List<OrbitingVesselSnapshot>
            {
                new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, 0)
            };

            OrbitalVesselTracker.RefreshOrbitalProgress(
                playerAgency,
                agencies,
                objectives,
                vesselSnapshots,
                1500.0);

            RequireEqual(1, playerAgency.GetSatelliteCount("Kerbin"),
                "The current scan should apply its Kerbin satellite count before unlock evaluation.");
            Require(playerAgency.HasCompletedObjective("kerbin-satellite-gate"),
                "A satellite-count prerequisite satisfied by the current scan should unlock in that same scan.");
        }

        private static void NegativeCrewCountDoesNotQualifyAsUncrewed()
        {
            var playerAgency = new AgencyState("player", "Player", true);
            var agencies = new List<AgencyState> { playerAgency };
            var objectives = new List<ObjectiveDefinition>
            {
                new ObjectiveDefinition(
                    "invalid-crew-probe",
                    "Invalid Crew Probe",
                    "Kerbin",
                    ObjectiveSituation.Orbit,
                    ObjectiveCrewRequirement.UncrewedProbe,
                    "Orbit an uncrewed probe.",
                    null)
            };
            var vesselSnapshots = new List<OrbitingVesselSnapshot>
            {
                new OrbitingVesselSnapshot("Kerbin", OrbitalVesselType.Probe, -1)
            };

            OrbitalVesselTracker.RefreshOrbitalProgress(
                playerAgency,
                agencies,
                objectives,
                vesselSnapshots,
                3100.0);

            RequireEqual(1, playerAgency.GetSatelliteCount("Kerbin"),
                "Crew metadata should not affect whether a Probe counts toward the satellite network.");
            Require(!playerAgency.HasCompletedObjective("invalid-crew-probe"),
                "A malformed negative crew count must not be interpreted as a verified uncrewed probe.");
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

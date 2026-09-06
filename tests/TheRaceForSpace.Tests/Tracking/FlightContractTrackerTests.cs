using System;
using System.Collections.Generic;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Persistence;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.Tests.Tracking
{
    internal static class FlightContractTrackerTests
    {
        public static void RunAll()
        {
            DirectedPowerRequiresImpactBelowCeiling();
            MassUsesRemainingMassAndLaunchDistance();
            DockedMassRequiresOriginalAttemptToBeSeparate();
            ControlRequiresContinuousCrewedHoldAndLanding();
            UnobservedControlGapResetsHold();
            ControlTopologyChangesResetUnqualifiedHold();
            BiomeAllowsOnlyOneLineObjectivePerLaunch();
            StagingPreservesDirectedPowerAttempt();
            DetachedStageDoesNotCloneAttemptHistory();
            DockingKeepsAttemptHistoriesSeparate();
            SwitchingCraftPreservesIndependentAttempts();
            MultipleFlightAttemptsSurviveSaveLoad();
            PartialControlHoldSurvivesSaveLoad();
            MultipleControlStatesSurviveSaveLoad();
            DirectedPowerDisqualificationSurvivesSaveLoad();
            LiveProgressReflectsLatestSample();
            MalformedActiveSaveIsDiscarded();
        }

        private static void DirectedPowerRequiresImpactBelowCeiling()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("power-a", 0.0, 0.0, 100.0, 0.0, 0.1, 0, null, FlightSituation.Prelaunch));
            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("power-a", 0.0, 10.0, 50000.0, 650.0, 0.1, 0, null, FlightSituation.Flying));
            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("power-a", 0.0, 10.5, 0.0, 0.0, 0.1, 0, null, FlightSituation.Landed));

            Require(
                !player.HasCompletedObjective(ObjectiveCatalogue.DirectedPower1Id),
                "Directed Power must not complete before the expendable impact occurs.");
            Require(
                tracker.RecordSurfaceImpact(
                    player,
                    ObjectiveCatalogue.PreOrbitContracts,
                    "power-a",
                    "Kerbin",
                    11.0),
                "A qualifying flight history should still complete when KSP reports the crash after the vessel briefly enters a landed state.");

            AgencyState overCeilingPlayer = new AgencyState("over", "Over", true);
            var overCeilingTracker = new FlightContractTracker();
            overCeilingTracker.EvaluateActiveFlightContracts(
                overCeilingPlayer,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("power-b", 20.0, 20.0, 70001.0, 700.0, 0.1, 0, null, FlightSituation.SubOrbital));

            Require(
                !overCeilingTracker.RecordSurfaceImpact(
                    overCeilingPlayer,
                    ObjectiveCatalogue.PreOrbitContracts,
                    "power-b",
                    "Kerbin",
                    21.0),
                "Ever exceeding 70 km must invalidate the Directed Power attempt.");
        }

        private static void MassUsesRemainingMassAndLaunchDistance()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-a", 100.0, 100.0, 100.0, 0.0, 2.0, 0, null, FlightSituation.Prelaunch, 0.0));
            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-a", 100.0, 110.0, 1000.0, 300.0, 1.1, 0, null, FlightSituation.Flying, 3.0));

            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Mass1Id),
                "Flying more than 25 km with enough mass must not complete Mass I before landing.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-a", 100.0, 111.0, 0.0, 0.0, 1.1, 0, null, FlightSituation.Landed, 3.0));
            Require(player.HasCompletedObjective(ObjectiveCatalogue.Mass1Id),
                "A finished landed craft retaining at least 1 t more than 25 km from launch should complete Mass I.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-a", 100.0, 120.0, 1000.0, 400.0, 3.0, 0, null, FlightSituation.Flying, 10.0));
            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Mass2Id),
                "One launch must not cascade through multiple Mass objectives.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-b", 200.0, 200.0, 100.0, 0.0, 3.0, 0, null, FlightSituation.Prelaunch, 0.0));
            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-b", 200.0, 210.0, 1000.0, 400.0, 2.6, 0, null, FlightSituation.Flying, 8.0));
            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Mass2Id),
                "Mass II must wait for the qualifying 2.5 t craft to finish beyond 75 km.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-b", 200.0, 211.0, 0.0, 0.0, 2.6, 0, null, FlightSituation.Splashed, 8.0));
            Require(player.HasCompletedObjective(ObjectiveCatalogue.Mass2Id),
                "A fresh splashed craft retaining at least 2.5 t beyond 75 km should complete Mass II.");
        }

        private static void DockedMassRequiresOriginalAttemptToBeSeparate()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();
            var massContracts = new List<ObjectiveDefinition>
            {
                ObjectiveCatalogue.FindById(ObjectiveCatalogue.Mass1Id)
            };

            tracker.EvaluateActiveFlightContracts(
                player,
                massContracts,
                Snapshot(
                    "mass-topology-a",
                    2200.0,
                    2200.0,
                    100.0,
                    0.0,
                    1.2,
                    0,
                    null,
                    FlightSituation.Prelaunch,
                    partPersistentIds: new uint[] { 1001u, 1002u },
                    referencePartPersistentId: 1001u));
            tracker.EvaluateActiveFlightContracts(
                player,
                massContracts,
                Snapshot(
                    "mass-topology-b",
                    2300.0,
                    2300.0,
                    100.0,
                    0.0,
                    2.0,
                    0,
                    null,
                    FlightSituation.Prelaunch,
                    partPersistentIds: new uint[] { 2001u, 2002u },
                    referencePartPersistentId: 2001u));
            tracker.EvaluateActiveFlightContracts(
                player,
                massContracts,
                Snapshot(
                    "mass-topology-a",
                    2200.0,
                    2301.0,
                    1000.0,
                    200.0,
                    1.2,
                    0,
                    null,
                    FlightSituation.Flying,
                    3.0,
                    new uint[] { 1001u, 1002u },
                    1001u));

            tracker.EvaluateActiveFlightContracts(
                player,
                massContracts,
                Snapshot(
                    "mass-topology-docked",
                    2200.0,
                    2302.0,
                    0.0,
                    0.0,
                    3.2,
                    0,
                    null,
                    FlightSituation.Landed,
                    3.0,
                    new uint[] { 1001u, 1002u, 2001u, 2002u },
                    1001u));

            Require(
                !player.HasCompletedObjective(ObjectiveCatalogue.Mass1Id),
                "A landed docked assembly must not use mass from parts outside the selected Flight Attempt lineage.");

            tracker.EvaluateActiveFlightContracts(
                player,
                massContracts,
                Snapshot(
                    "mass-topology-a-undocked",
                    2200.0,
                    2303.0,
                    0.0,
                    0.0,
                    1.2,
                    0,
                    null,
                    FlightSituation.Landed,
                    3.0,
                    new uint[] { 1001u, 1002u },
                    1001u));

            Require(
                player.HasCompletedObjective(ObjectiveCatalogue.Mass1Id),
                "After the unrelated lineage is detached, the original craft's own qualifying mass and distance should complete Mass I normally.");
        }

        private static void ControlRequiresContinuousCrewedHoldAndLanding()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();

            for (int elapsedSeconds = 0; elapsedSeconds <= 30; elapsedSeconds += 5)
            {
                tracker.EvaluateActiveFlightContracts(
                    player,
                    ObjectiveCatalogue.PreOrbitContracts,
                    Snapshot(
                        "control-a",
                        300.0,
                        300.0 + elapsedSeconds,
                        3000.0,
                        150.0,
                        1.0,
                        1,
                        null,
                        FlightSituation.Flying));
            }

            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Control1Id),
                "Completing the altitude hold alone must not award Control I before recovery.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("control-a", 300.0, 331.0, 80.0, 0.0, 1.0, 1, null, FlightSituation.Splashed));
            Require(player.HasCompletedObjective(ObjectiveCatalogue.Control1Id),
                "A qualified Control hold followed by a crewed Kerbin splashdown should complete the objective.");
        }

        private static void UnobservedControlGapResetsHold()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("control-gap", 350.0, 350.0, 3000.0, 150.0, 1.0, 1, null, FlightSituation.Flying));
            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("control-gap", 350.0, 354.0, 3000.0, 150.0, 1.0, 1, null, FlightSituation.Flying));
            RequireNear(
                4.0,
                tracker.GetControlHoldSeconds(ObjectiveCatalogue.Control1Id),
                "Closely spaced observed samples should accumulate Control hold time.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("control-gap", 350.0, 370.0, 3000.0, 150.0, 1.0, 1, null, FlightSituation.Flying));

            RequireNear(
                0.0,
                tracker.GetControlHoldSeconds(ObjectiveCatalogue.Control1Id),
                "A long unobserved gap must reset an unqualified continuous Control hold.");
            Require(
                !tracker.IsControlObjectiveQualified(ObjectiveCatalogue.Control1Id),
                "A long unobserved gap must not qualify Control from missing flight time.");
            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Control1Id),
                "An unobserved gap must not complete Control I.");
        }

        private static void ControlTopologyChangesResetUnqualifiedHold()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();
            var controlContracts = new List<ObjectiveDefinition>
            {
                ObjectiveCatalogue.FindById(ObjectiveCatalogue.Control1Id)
            };

            tracker.EvaluateActiveFlightContracts(
                player,
                controlContracts,
                Snapshot(
                    "control-topology-a",
                    2400.0,
                    2400.0,
                    3000.0,
                    150.0,
                    1.0,
                    1,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 3001u, 3002u },
                    referencePartPersistentId: 3001u));
            tracker.EvaluateActiveFlightContracts(
                player,
                controlContracts,
                Snapshot(
                    "control-topology-a",
                    2400.0,
                    2404.0,
                    3000.0,
                    150.0,
                    1.0,
                    1,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 3001u, 3002u },
                    referencePartPersistentId: 3001u));
            RequireNear(
                4.0,
                tracker.GetControlHoldSeconds(ObjectiveCatalogue.Control1Id),
                "Control should accumulate normally before a topology change.");

            tracker.EvaluateActiveFlightContracts(
                player,
                controlContracts,
                Snapshot(
                    "control-topology-docked",
                    2400.0,
                    2405.0,
                    3000.0,
                    150.0,
                    2.0,
                    1,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 3001u, 3002u, 4001u },
                    referencePartPersistentId: 3001u));
            RequireNear(
                0.0,
                tracker.GetControlHoldSeconds(ObjectiveCatalogue.Control1Id),
                "Docking an external lineage must reset an unfinished continuous Control hold.");

            tracker.EvaluateActiveFlightContracts(
                player,
                controlContracts,
                Snapshot(
                    "control-topology-docked",
                    2400.0,
                    2409.0,
                    3000.0,
                    150.0,
                    2.0,
                    1,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 3001u, 3002u, 4001u },
                    referencePartPersistentId: 3001u));
            RequireNear(
                4.0,
                tracker.GetControlHoldSeconds(ObjectiveCatalogue.Control1Id),
                "An unchanged docked topology may begin a new continuous Control hold.");

            tracker.EvaluateActiveFlightContracts(
                player,
                controlContracts,
                Snapshot(
                    "control-topology-undocked",
                    2400.0,
                    2410.0,
                    3000.0,
                    150.0,
                    1.0,
                    1,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 3001u, 3002u },
                    referencePartPersistentId: 3001u));
            RequireNear(
                0.0,
                tracker.GetControlHoldSeconds(ObjectiveCatalogue.Control1Id),
                "Undocking an external lineage must also reset an unfinished continuous Control hold.");

            int[] laterSampleTimes = { 2414, 2418, 2422, 2426, 2430, 2434, 2438, 2440 };
            for (int sampleIndex = 0; sampleIndex < laterSampleTimes.Length; sampleIndex++)
            {
                tracker.EvaluateActiveFlightContracts(
                    player,
                    controlContracts,
                    Snapshot(
                        "control-topology-undocked",
                        2400.0,
                        laterSampleTimes[sampleIndex],
                        3000.0,
                        150.0,
                        1.0,
                        1,
                        null,
                        FlightSituation.Flying,
                        partPersistentIds: new uint[] { 3001u, 3002u },
                        referencePartPersistentId: 3001u));
            }

            Require(
                tracker.IsControlObjectiveQualified(ObjectiveCatalogue.Control1Id),
                "Control should qualify normally after a full uninterrupted post-undocking hold.");

            tracker.EvaluateActiveFlightContracts(
                player,
                controlContracts,
                Snapshot(
                    "control-topology-docked-again",
                    2400.0,
                    2441.0,
                    3000.0,
                    150.0,
                    2.0,
                    1,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 3001u, 3002u, 4001u },
                    referencePartPersistentId: 3001u));

            Require(
                tracker.IsControlObjectiveQualified(ObjectiveCatalogue.Control1Id),
                "A topology change after Control qualification must not erase the completed hold.");
        }

        private static void BiomeAllowsOnlyOneLineObjectivePerLaunch()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-a", 400.0, 400.0, 500.0, 50.0, 1.0, 0, "Shores", FlightSituation.Flying));
            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-a", 400.0, 410.0, 500.0, 50.0, 1.0, 0, "Grasslands", FlightSituation.Flying));

            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Biome1Id),
                "Flying over Grasslands must not complete Biome I.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-a", 400.0, 411.0, 0.0, 0.0, 1.0, 0, "Grasslands", FlightSituation.Landed));
            Require(player.HasCompletedObjective(ObjectiveCatalogue.Biome1Id),
                "Landing in Grasslands should complete Biome I.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-a", 400.0, 420.0, 500.0, 50.0, 1.0, 0, "Highlands", FlightSituation.Flying));
            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Biome2Id),
                "A single exploration launch must not complete two Biome line levels.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-b", 500.0, 500.0, 100.0, 0.0, 1.0, 0, "Shores", FlightSituation.Prelaunch));
            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-b", 500.0, 510.0, 1000.0, 100.0, 1.0, 0, "Highlands", FlightSituation.Flying));
            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Biome2Id),
                "Flying over Highlands must not complete Biome II.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-b", 500.0, 511.0, 0.0, 0.0, 1.0, 0, "Highlands", FlightSituation.Splashed));
            Require(player.HasCompletedObjective(ObjectiveCatalogue.Biome2Id),
                "A later launch splashed in Highlands may complete the next unlocked Biome objective.");
        }

        private static void StagingPreservesDirectedPowerAttempt()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot(
                    "stage-a",
                    600.0,
                    600.0,
                    20000.0,
                    650.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 101u, 102u, 103u }));
            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot(
                    "stage-b",
                    600.0,
                    605.0,
                    30000.0,
                    500.0,
                    0.5,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 102u, 103u }));

            RequireNear(650.0, tracker.MaximumSurfaceSpeedMetersPerSecond,
                "The controlled stage should retain the parent attempt's earlier maximum speed through shared part lineage.");
            Require(
                tracker.RecordSurfaceImpact(
                    player,
                    ObjectiveCatalogue.PreOrbitContracts,
                    "stage-b",
                    "Kerbin",
                    606.0),
                "A stage retaining the parent lineage should complete Directed Power from the continuing attempt history.");
        }

        private static void DetachedStageDoesNotCloneAttemptHistory()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot(
                    "split-parent",
                    650.0,
                    650.0,
                    20000.0,
                    650.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 201u, 202u, 203u }));

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot(
                    "split-upper",
                    650.0,
                    655.0,
                    25000.0,
                    500.0,
                    0.6,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 202u, 203u }));

            RequireNear(650.0, tracker.MaximumSurfaceSpeedMetersPerSecond,
                "The first actively controlled split branch should continue the parent attempt.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot(
                    "split-booster",
                    650.0,
                    656.0,
                    1000.0,
                    100.0,
                    0.4,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 201u }));

            RequireNear(100.0, tracker.MaximumSurfaceSpeedMetersPerSecond,
                "A detached branch with no remaining continuing lineage must start its own attempt even when launch time matches.");
            Require(
                !tracker.RecordSurfaceImpact(
                    player,
                    ObjectiveCatalogue.PreOrbitContracts,
                    "split-booster",
                    "Kerbin",
                    657.0),
                "The detached branch must not inherit the parent's qualifying Directed Power maximum.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot(
                    "split-upper",
                    650.0,
                    658.0,
                    26000.0,
                    450.0,
                    0.6,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 202u, 203u }));

            RequireNear(650.0, tracker.MaximumSurfaceSpeedMetersPerSecond,
                "Returning to the continuing branch should recover its original history after the detached branch was observed.");
        }

        private static void DockingKeepsAttemptHistoriesSeparate()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();
            var noActiveContracts = new List<ObjectiveDefinition>();

            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "dock-a",
                    1000.0,
                    1000.0,
                    20000.0,
                    650.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 501u, 502u },
                    referencePartPersistentId: 501u));
            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "dock-b",
                    1100.0,
                    1100.0,
                    10000.0,
                    350.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 601u, 602u },
                    referencePartPersistentId: 601u));
            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "dock-a",
                    1000.0,
                    1200.0,
                    22000.0,
                    500.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 501u, 502u },
                    referencePartPersistentId: 501u));

            // Deliberately reuse Craft B's vessel ID for the combined assembly. Reference-part
            // lineage must keep Craft A selected because the player is still controlling from A.
            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "dock-b",
                    1000.0,
                    1201.0,
                    22000.0,
                    500.0,
                    2.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 501u, 502u, 601u, 602u },
                    referencePartPersistentId: 501u));

            RequireNear(650.0, tracker.MaximumSurfaceSpeedMetersPerSecond,
                "Docking must keep Craft A's history selected when the reference part belongs to A even if KSP reuses Craft B's vessel ID.");

            // Switching Control From Here to a Craft B part should select B's independent attempt,
            // not merge A's earlier 650 m/s maximum into B.
            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "dock-b",
                    1000.0,
                    1202.0,
                    22000.0,
                    450.0,
                    2.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 501u, 502u, 601u, 602u },
                    referencePartPersistentId: 601u));

            RequireNear(450.0, tracker.MaximumSurfaceSpeedMetersPerSecond,
                "A docked Craft B reference part should recover and update B's own history without inheriting Craft A's maximum.");

            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "dock-a-undocked",
                    1000.0,
                    1203.0,
                    23000.0,
                    400.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 501u, 502u },
                    referencePartPersistentId: 501u));

            RequireNear(650.0, tracker.MaximumSurfaceSpeedMetersPerSecond,
                "After undocking, Craft A should recover the same independent history it had before docking.");

            tracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "dock-b-undocked",
                    1100.0,
                    1204.0,
                    12000.0,
                    300.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 601u, 602u },
                    referencePartPersistentId: 601u));

            RequireNear(450.0, tracker.MaximumSurfaceSpeedMetersPerSecond,
                "After undocking, Craft B should recover the independent history updated while docked.");
        }

        private static void SwitchingCraftPreservesIndependentAttempts()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot(
                    "switch-a",
                    1000.0,
                    1200.0,
                    20000.0,
                    650.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 301u, 302u }));
            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot(
                    "switch-b",
                    1100.0,
                    1210.0,
                    10000.0,
                    300.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 401u, 402u }));

            Require(tracker.VesselId == "switch-b",
                "The most recently sampled craft should remain the active Flight Contract attempt.");
            RequireNear(300.0, tracker.MaximumSurfaceSpeedMetersPerSecond,
                "Craft B must start with its own speed history rather than inherit Craft A progress.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot(
                    "switch-a",
                    1000.0,
                    1220.0,
                    25000.0,
                    500.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 301u, 302u }));

            Require(tracker.VesselId == "switch-a",
                "Returning to Craft A should select its remembered in-memory attempt.");
            RequireNear(650.0, tracker.MaximumSurfaceSpeedMetersPerSecond,
                "Craft A should retain its earlier maximum speed after temporarily flying Craft B.");
            RequireNear(25000.0, tracker.MaximumAltitudeMeters,
                "Craft A should continue updating its own remembered altitude history after returning.");

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot(
                    "switch-b",
                    1100.0,
                    1230.0,
                    12000.0,
                    350.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 401u, 402u }));
            RequireNear(350.0, tracker.MaximumSurfaceSpeedMetersPerSecond,
                "Craft B should keep an independent maximum instead of receiving Craft A's 650 m/s history.");

            tracker.RecordSurfaceImpact(
                player,
                new List<ObjectiveDefinition>(),
                "switch-b",
                "Kerbin",
                1231.0);
            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot(
                    "switch-a",
                    1000.0,
                    1240.0,
                    26000.0,
                    400.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 301u, 302u }));

            RequireNear(650.0, tracker.MaximumSurfaceSpeedMetersPerSecond,
                "Removing Craft B's attempt must not erase Craft A's remembered history.");
        }

        private static void MultipleFlightAttemptsSurviveSaveLoad()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var noActiveContracts = new List<ObjectiveDefinition>();
            var sourceTracker = new FlightContractTracker();

            sourceTracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "persist-a",
                    1300.0,
                    1300.0,
                    20000.0,
                    650.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 701u, 702u },
                    referencePartPersistentId: 701u));
            sourceTracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "persist-b",
                    1400.0,
                    1400.0,
                    10000.0,
                    350.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 801u, 802u },
                    referencePartPersistentId: 801u));

            // Give both attempts the same last KSP vessel ID while docked, then save with B selected.
            // Persistence must keep part lineage authoritative rather than collapsing them by vessel ID.
            sourceTracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "persist-docked",
                    1300.0,
                    1500.0,
                    22000.0,
                    500.0,
                    2.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 701u, 702u, 801u, 802u },
                    referencePartPersistentId: 701u));
            sourceTracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "persist-docked",
                    1300.0,
                    1501.0,
                    22000.0,
                    450.0,
                    2.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 701u, 702u, 801u, 802u },
                    referencePartPersistentId: 801u));

            var saveState = new FlightContractProgressSaveState();
            saveState.Capture(sourceTracker);
            var node = new ConfigNode();
            saveState.Save(node);

            ConfigNode[] attemptNodes = node.GetNodes("ATTEMPT");
            Require(attemptNodes.Length == 2,
                "The multi-attempt save format should write one ATTEMPT node for each remembered history.");
            Require(node.GetValue("active") == null,
                "The multi-attempt format should not write the removed single-attempt active flag.");

            int selectedAttemptCount = 0;
            int lineagePartCount = 0;
            for (int attemptIndex = 0; attemptIndex < attemptNodes.Length; attemptIndex++)
            {
                bool isSelected;
                Require(bool.TryParse(
                        attemptNodes[attemptIndex].GetValue("selected"),
                        out isSelected),
                    "Every ATTEMPT node should explicitly record whether it was selected.");
                if (isSelected)
                {
                    selectedAttemptCount++;
                }

                lineagePartCount += attemptNodes[attemptIndex].GetNodes("PART_LINEAGE").Length;
            }

            Require(selectedAttemptCount == 1,
                "Exactly one remembered attempt should be selected when the tracker had an active attempt at save time.");
            Require(lineagePartCount == 4,
                "Both remembered craft lineages should be serialized independently.");

            var loadedState = new FlightContractProgressSaveState();
            loadedState.Load(node);
            var restoredTracker = new FlightContractTracker();
            loadedState.ApplyTo(restoredTracker);

            RequireNear(450.0, restoredTracker.MaximumSurfaceSpeedMetersPerSecond,
                "The selected Craft B history should be restored immediately after load.");

            restoredTracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "persist-a-undocked",
                    1300.0,
                    1502.0,
                    23000.0,
                    400.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 701u, 702u },
                    referencePartPersistentId: 701u));
            RequireNear(650.0, restoredTracker.MaximumSurfaceSpeedMetersPerSecond,
                "After reload and undocking, Craft A should recover its saved independent maximum by lineage.");

            restoredTracker.EvaluateActiveFlightContracts(
                player,
                noActiveContracts,
                Snapshot(
                    "persist-b-undocked",
                    1400.0,
                    1503.0,
                    12000.0,
                    300.0,
                    1.0,
                    0,
                    null,
                    FlightSituation.Flying,
                    partPersistentIds: new uint[] { 801u, 802u },
                    referencePartPersistentId: 801u));
            RequireNear(450.0, restoredTracker.MaximumSurfaceSpeedMetersPerSecond,
                "After reload and undocking, Craft B should recover the history updated while docked.");
        }

        private static void PartialControlHoldSurvivesSaveLoad()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var sourceTracker = new FlightContractTracker();

            for (int elapsedSeconds = 0; elapsedSeconds <= 15; elapsedSeconds += 5)
            {
                sourceTracker.EvaluateActiveFlightContracts(
                    player,
                    ObjectiveCatalogue.PreOrbitContracts,
                    Snapshot(
                        "save-a",
                        700.0,
                        700.0 + elapsedSeconds,
                        3000.0,
                        120.0,
                        1.0,
                        1,
                        null,
                        FlightSituation.Flying));
            }

            var saveState = new FlightContractProgressSaveState();
            saveState.Capture(sourceTracker);
            var node = new ConfigNode();
            saveState.Save(node);

            var loadedState = new FlightContractProgressSaveState();
            loadedState.Load(node);
            var restoredTracker = new FlightContractTracker();
            loadedState.ApplyTo(restoredTracker);

            for (int elapsedSeconds = 20; elapsedSeconds <= 30; elapsedSeconds += 5)
            {
                restoredTracker.EvaluateActiveFlightContracts(
                    player,
                    ObjectiveCatalogue.PreOrbitContracts,
                    Snapshot(
                        "save-a",
                        700.0,
                        700.0 + elapsedSeconds,
                        3000.0,
                        120.0,
                        1.0,
                        1,
                        null,
                        FlightSituation.Flying));
            }

            restoredTracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("save-a", 700.0, 731.0, 80.0, 0.0, 1.0, 1, null, FlightSituation.Landed));

            Require(player.HasCompletedObjective(ObjectiveCatalogue.Control1Id),
                "A saved 15-second Control hold should resume and complete after another 15 seconds and landing.");
        }

        private static void MultipleControlStatesSurviveSaveLoad()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var activeControlContracts = new List<ObjectiveDefinition>
            {
                ObjectiveCatalogue.FindById(ObjectiveCatalogue.Control1Id),
                ObjectiveCatalogue.FindById(ObjectiveCatalogue.Control2Id)
            };
            var sourceTracker = new FlightContractTracker();

            for (int elapsedSeconds = 0; elapsedSeconds <= 30; elapsedSeconds += 5)
            {
                sourceTracker.EvaluateActiveFlightContracts(
                    player,
                    activeControlContracts,
                    Snapshot(
                        "save-multi-control",
                        1000.0,
                        1000.0 + elapsedSeconds,
                        3000.0,
                        150.0,
                        1.0,
                        1,
                        null,
                        FlightSituation.Flying));
            }

            Require(sourceTracker.IsControlObjectiveQualified(ObjectiveCatalogue.Control1Id),
                "Control I should be qualified before saving the multi-Control attempt.");

            for (int observationUniversalTime = 1035; observationUniversalTime <= 1050;
                observationUniversalTime += 5)
            {
                sourceTracker.EvaluateActiveFlightContracts(
                    player,
                    activeControlContracts,
                    Snapshot(
                        "save-multi-control",
                        1000.0,
                        observationUniversalTime,
                        10000.0,
                        150.0,
                        1.0,
                        1,
                        null,
                        FlightSituation.Flying));
            }

            RequireNear(15.0, sourceTracker.GetControlHoldSeconds(ObjectiveCatalogue.Control2Id),
                "Control II should have independent partial progress before save.");

            var saveState = new FlightContractProgressSaveState();
            saveState.Capture(sourceTracker);
            var node = new ConfigNode();
            saveState.Save(node);

            ConfigNode[] attemptNodes = node.GetNodes("ATTEMPT");
            Require(attemptNodes.Length == 1,
                "A single remembered craft should be written as one ATTEMPT node.");
            Require(attemptNodes[0].GetNodes("CONTROL_STATE").Length == 2,
                "The ATTEMPT node should write one CONTROL_STATE child per tracked Control contract.");
            Require(node.GetNodes("CONTROL_STATE").Length == 0,
                "CONTROL_STATE nodes should belong to their ATTEMPT rather than the progress root.");
            Require(attemptNodes[0].GetValue("controlHoldObjectiveId") == null,
                "The current save format should not write the removed single-Control objective field.");
            Require(attemptNodes[0].GetValue("completedControl") == null,
                "The current save format should not write obsolete per-line completion flags.");

            var loadedState = new FlightContractProgressSaveState();
            loadedState.Load(node);
            var restoredTracker = new FlightContractTracker();
            loadedState.ApplyTo(restoredTracker);

            Require(restoredTracker.IsControlObjectiveQualified(ObjectiveCatalogue.Control1Id),
                "A qualified Control I state should survive save/load independently.");
            RequireNear(15.0, restoredTracker.GetControlHoldSeconds(ObjectiveCatalogue.Control2Id),
                "Partial Control II progress should survive save/load independently.");
            Require(!restoredTracker.IsControlObjectiveQualified(ObjectiveCatalogue.Control2Id),
                "Partial Control II progress must not be promoted to qualified by persistence.");

            for (int observationUniversalTime = 1055; observationUniversalTime <= 1080;
                observationUniversalTime += 5)
            {
                restoredTracker.EvaluateActiveFlightContracts(
                    player,
                    activeControlContracts,
                    Snapshot(
                        "save-multi-control",
                        1000.0,
                        observationUniversalTime,
                        10000.0,
                        150.0,
                        1.0,
                        1,
                        null,
                        FlightSituation.Flying));
            }

            Require(restoredTracker.IsControlObjectiveQualified(ObjectiveCatalogue.Control2Id),
                "Control II should continue from its saved partial hold and qualify normally.");

            bool recorded = restoredTracker.EvaluateActiveFlightContracts(
                player,
                activeControlContracts,
                Snapshot(
                    "save-multi-control",
                    1000.0,
                    1081.0,
                    80.0,
                    0.0,
                    1.0,
                    1,
                    null,
                    FlightSituation.Landed));

            Require(recorded,
                "The restored landing should record the independently qualified Control contracts.");
            Require(player.HasCompletedObjective(ObjectiveCatalogue.Control1Id),
                "Restored Control I qualification should complete on landing.");
            Require(player.HasCompletedObjective(ObjectiveCatalogue.Control2Id),
                "Restored Control II qualification should complete on the same landing.");
        }

        private static void DirectedPowerDisqualificationSurvivesSaveLoad()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var sourceTracker = new FlightContractTracker();

            sourceTracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("save-power", 800.0, 800.0, 70010.0, 700.0, 1.0, 0, null, FlightSituation.SubOrbital));

            var saveState = new FlightContractProgressSaveState();
            saveState.Capture(sourceTracker);
            var node = new ConfigNode();
            saveState.Save(node);
            var loadedState = new FlightContractProgressSaveState();
            loadedState.Load(node);
            var restoredTracker = new FlightContractTracker();
            loadedState.ApplyTo(restoredTracker);

            Require(
                !restoredTracker.RecordSurfaceImpact(
                    player,
                    ObjectiveCatalogue.PreOrbitContracts,
                    "save-power",
                    "Kerbin",
                    801.0),
                "Saving and loading must not erase an earlier Directed Power altitude violation.");
        }

        private static void LiveProgressReflectsLatestSample()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();

            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("progress", 900.0, 900.0, 100.0, 0.0, 4.0, 0, "Shores", FlightSituation.Prelaunch, 0.0));
            tracker.EvaluateActiveFlightContracts(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("progress", 900.0, 910.0, 3500.0, 525.0, 3.2, 1, "Grasslands", FlightSituation.Flying, 2.0));

            RequireNear(3500.0, tracker.CurrentAltitudeMeters, "Current altitude should follow the latest sample.");
            RequireNear(525.0, tracker.CurrentSurfaceSpeedMetersPerSecond, "Current surface speed should follow the latest sample.");
            RequireNear(3.2, tracker.CurrentMassTonnes, "Current mass should follow the latest sample.");
            Require(tracker.CurrentDistanceMeters > 20000.0, "Current distance should be derived from the launch position.");
            Require(tracker.CurrentBiomeName == "Grasslands", "Current biome should follow the latest sample.");
            Require(tracker.CurrentCrewCount == 1, "Current crew count should follow the latest sample.");
            Require(tracker.CurrentSituation == FlightSituation.Flying, "Current situation should follow the latest sample.");
        }

        private static void MalformedActiveSaveIsDiscarded()
        {
            var node = new ConfigNode();
            ConfigNode attemptNode = node.AddNode("ATTEMPT");
            attemptNode.AddValue("selected", true);
            attemptNode.AddValue("vesselId", "broken-vessel");
            attemptNode.AddValue("body", "Kerbin");
            attemptNode.AddValue("launchUniversalTime", "1000");
            attemptNode.AddValue("startLatitude", "0");
            attemptNode.AddValue("startLongitude", "0");
            attemptNode.AddValue("lastSampleUniversalTime", "1010");
            attemptNode.AddValue("maximumAltitudeMeters", "1000");
            attemptNode.AddValue("maximumSurfaceSpeedMetersPerSecond", "650");
            attemptNode.AddValue("enteredOrbit", false);
            ConfigNode controlStateNode = attemptNode.AddNode("CONTROL_STATE");
            controlStateNode.AddValue("objectiveId", ObjectiveCatalogue.Control1Id);
            controlStateNode.AddValue("holdSeconds", "not-a-number");
            controlStateNode.AddValue("wasSampleInBand", true);
            controlStateNode.AddValue("qualified", false);

            var loadedState = new FlightContractProgressSaveState();
            loadedState.Load(node);
            var tracker = new FlightContractTracker();
            tracker.RestoreState(
                "old",
                "Kerbin",
                1.0,
                0.0,
                0.0,
                2.0,
                10.0,
                10.0,
                false);

            loadedState.ApplyTo(tracker);
            Require(!tracker.HasActiveAttempt,
                "Malformed per-attempt Control save data should clear all remembered attempts rather than restore invented progress.");
        }

        private static ActiveVesselSnapshot Snapshot(
            string vesselId,
            double launchUniversalTime,
            double observationUniversalTime,
            double altitudeMeters,
            double surfaceSpeedMetersPerSecond,
            double massTonnes,
            int crewCount,
            string biomeName,
            FlightSituation situation,
            double longitudeDegrees = 0.0,
            IList<uint> partPersistentIds = null,
            uint referencePartPersistentId = 0u)
        {
            return new ActiveVesselSnapshot(
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
                observationUniversalTime,
                partPersistentIds,
                referencePartPersistentId);
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

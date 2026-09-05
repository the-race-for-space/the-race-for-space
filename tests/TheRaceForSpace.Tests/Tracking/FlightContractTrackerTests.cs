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
            ControlRequiresContinuousCrewedHoldAndLanding();
            UnobservedControlGapResetsHold();
            BiomeAllowsOnlyOneLineMilestonePerLaunch();
            StagingPreservesDirectedPowerAttempt();
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

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("power-a", 0.0, 0.0, 100.0, 0.0, 0.1, 0, null, FlightSituation.Prelaunch));
            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("power-a", 0.0, 10.0, 50000.0, 650.0, 0.1, 0, null, FlightSituation.Flying));
            tracker.RefreshPlayerMilestones(
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
            overCeilingTracker.RefreshPlayerMilestones(
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

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-a", 100.0, 100.0, 100.0, 0.0, 2.0, 0, null, FlightSituation.Prelaunch, 0.0));
            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-a", 100.0, 110.0, 1000.0, 300.0, 1.1, 0, null, FlightSituation.Flying, 3.0));

            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Mass1Id),
                "Flying more than 25 km with enough mass must not complete Mass I before landing.");

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-a", 100.0, 111.0, 0.0, 0.0, 1.1, 0, null, FlightSituation.Landed, 3.0));
            Require(player.HasCompletedObjective(ObjectiveCatalogue.Mass1Id),
                "A finished landed craft retaining at least 1 t more than 25 km from launch should complete Mass I.");

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-a", 100.0, 120.0, 1000.0, 400.0, 3.0, 0, null, FlightSituation.Flying, 10.0));
            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Mass2Id),
                "One launch must not cascade through multiple Mass objectives.");

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-b", 200.0, 200.0, 100.0, 0.0, 3.0, 0, null, FlightSituation.Prelaunch, 0.0));
            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-b", 200.0, 210.0, 1000.0, 400.0, 2.6, 0, null, FlightSituation.Flying, 8.0));
            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Mass2Id),
                "Mass II must wait for the qualifying 2.5 t craft to land beyond 75 km.");

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("mass-b", 200.0, 211.0, 0.0, 0.0, 2.6, 0, null, FlightSituation.Landed, 8.0));
            Require(player.HasCompletedObjective(ObjectiveCatalogue.Mass2Id),
                "A fresh landed craft retaining at least 2.5 t beyond 75 km should complete Mass II.");
        }

        private static void ControlRequiresContinuousCrewedHoldAndLanding()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();

            for (int elapsedSeconds = 0; elapsedSeconds <= 30; elapsedSeconds += 5)
            {
                tracker.RefreshPlayerMilestones(
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
                "Completing the altitude hold alone must not award Control I before landing.");

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("control-a", 300.0, 331.0, 80.0, 0.0, 1.0, 1, null, FlightSituation.Landed));
            Require(player.HasCompletedObjective(ObjectiveCatalogue.Control1Id),
                "A qualified Control hold followed by a crewed Kerbin landing should complete the objective.");
        }

        private static void UnobservedControlGapResetsHold()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("control-gap", 350.0, 350.0, 3000.0, 150.0, 1.0, 1, null, FlightSituation.Flying));
            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("control-gap", 350.0, 354.0, 3000.0, 150.0, 1.0, 1, null, FlightSituation.Flying));
            RequireNear(
                4.0,
                tracker.GetControlHoldSeconds(ObjectiveCatalogue.Control1Id),
                "Closely spaced observed samples should accumulate Control hold time.");

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("control-gap", 350.0, 370.0, 3000.0, 150.0, 1.0, 1, null, FlightSituation.Flying));

            RequireNear(
                0.0,
                tracker.GetControlHoldSeconds(ObjectiveCatalogue.Control1Id),
                "A long unobserved gap must reset an unqualified continuous Control hold.");
            Require(
                !tracker.IsControlMilestoneQualified(ObjectiveCatalogue.Control1Id),
                "A long unobserved gap must not qualify Control from missing flight time.");
            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Control1Id),
                "An unobserved gap must not complete Control I.");
        }

        private static void BiomeAllowsOnlyOneLineMilestonePerLaunch()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-a", 400.0, 400.0, 500.0, 50.0, 1.0, 0, "Shores", FlightSituation.Flying));
            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-a", 400.0, 410.0, 500.0, 50.0, 1.0, 0, "Grasslands", FlightSituation.Flying));

            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Biome1Id),
                "Flying over Grasslands must not complete Biome I.");

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-a", 400.0, 411.0, 0.0, 0.0, 1.0, 0, "Grasslands", FlightSituation.Landed));
            Require(player.HasCompletedObjective(ObjectiveCatalogue.Biome1Id),
                "Landing in Grasslands should complete Biome I.");

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-a", 400.0, 420.0, 500.0, 50.0, 1.0, 0, "Highlands", FlightSituation.Flying));
            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Biome2Id),
                "A single exploration launch must not complete two Biome line levels.");

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-b", 500.0, 500.0, 100.0, 0.0, 1.0, 0, "Shores", FlightSituation.Prelaunch));
            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-b", 500.0, 510.0, 1000.0, 100.0, 1.0, 0, "Highlands", FlightSituation.Flying));
            Require(!player.HasCompletedObjective(ObjectiveCatalogue.Biome2Id),
                "Flying over Highlands must not complete Biome II.");

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("biome-b", 500.0, 511.0, 0.0, 0.0, 1.0, 0, "Highlands", FlightSituation.Landed));
            Require(player.HasCompletedObjective(ObjectiveCatalogue.Biome2Id),
                "A later launch landed in Highlands may complete the next unlocked Biome objective.");
        }

        private static void StagingPreservesDirectedPowerAttempt()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var tracker = new FlightContractTracker();

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("stage-a", 600.0, 600.0, 20000.0, 500.0, 1.0, 0, null, FlightSituation.Flying));
            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("stage-b", 600.0, 605.0, 30000.0, 650.0, 0.5, 0, null, FlightSituation.Flying));

            Require(
                tracker.RecordSurfaceImpact(
                    player,
                    ObjectiveCatalogue.PreOrbitContracts,
                    "stage-b",
                    "Kerbin",
                    606.0),
                "A stage keeping the same launch time should preserve Directed Power flight history.");
        }

        private static void PartialControlHoldSurvivesSaveLoad()
        {
            AgencyState player = new AgencyState("player", "Player", true);
            var sourceTracker = new FlightContractTracker();

            for (int elapsedSeconds = 0; elapsedSeconds <= 15; elapsedSeconds += 5)
            {
                sourceTracker.RefreshPlayerMilestones(
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

            var saveState = new ActiveContractProgressSaveState();
            saveState.Capture(sourceTracker);
            var node = new ConfigNode();
            saveState.Save(node);

            var loadedState = new ActiveContractProgressSaveState();
            loadedState.Load(node);
            var restoredTracker = new FlightContractTracker();
            loadedState.ApplyTo(restoredTracker);

            for (int elapsedSeconds = 20; elapsedSeconds <= 30; elapsedSeconds += 5)
            {
                restoredTracker.RefreshPlayerMilestones(
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

            restoredTracker.RefreshPlayerMilestones(
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
                sourceTracker.RefreshPlayerMilestones(
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

            Require(sourceTracker.IsControlMilestoneQualified(ObjectiveCatalogue.Control1Id),
                "Control I should be qualified before saving the multi-Control attempt.");

            for (int observationUniversalTime = 1035; observationUniversalTime <= 1050;
                observationUniversalTime += 5)
            {
                sourceTracker.RefreshPlayerMilestones(
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

            var saveState = new ActiveContractProgressSaveState();
            saveState.Capture(sourceTracker);
            var node = new ConfigNode();
            saveState.Save(node);

            Require(node.GetNodes("CONTROL_STATE").Length == 2,
                "The current save format should write one CONTROL_STATE node per tracked Control contract.");
            Require(node.GetValue("controlHoldObjectiveId") == null,
                "The current save format should not write the removed single-Control objective field.");
            Require(node.GetValue("completedControl") == null,
                "The current save format should not write obsolete per-line completion flags.");

            var loadedState = new ActiveContractProgressSaveState();
            loadedState.Load(node);
            var restoredTracker = new FlightContractTracker();
            loadedState.ApplyTo(restoredTracker);

            Require(restoredTracker.IsControlMilestoneQualified(ObjectiveCatalogue.Control1Id),
                "A qualified Control I state should survive save/load independently.");
            RequireNear(15.0, restoredTracker.GetControlHoldSeconds(ObjectiveCatalogue.Control2Id),
                "Partial Control II progress should survive save/load independently.");
            Require(!restoredTracker.IsControlMilestoneQualified(ObjectiveCatalogue.Control2Id),
                "Partial Control II progress must not be promoted to qualified by persistence.");

            for (int observationUniversalTime = 1055; observationUniversalTime <= 1080;
                observationUniversalTime += 5)
            {
                restoredTracker.RefreshPlayerMilestones(
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

            Require(restoredTracker.IsControlMilestoneQualified(ObjectiveCatalogue.Control2Id),
                "Control II should continue from its saved partial hold and qualify normally.");

            bool recorded = restoredTracker.RefreshPlayerMilestones(
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

            sourceTracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("save-power", 800.0, 800.0, 70010.0, 700.0, 1.0, 0, null, FlightSituation.SubOrbital));

            var saveState = new ActiveContractProgressSaveState();
            saveState.Capture(sourceTracker);
            var node = new ConfigNode();
            saveState.Save(node);
            var loadedState = new ActiveContractProgressSaveState();
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

            tracker.RefreshPlayerMilestones(
                player,
                ObjectiveCatalogue.PreOrbitContracts,
                Snapshot("progress", 900.0, 900.0, 100.0, 0.0, 4.0, 0, "Shores", FlightSituation.Prelaunch, 0.0));
            tracker.RefreshPlayerMilestones(
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
            node.AddValue("active", true);
            node.AddValue("vesselId", "broken-vessel");
            node.AddValue("body", "Kerbin");
            node.AddValue("launchUniversalTime", "1000");
            node.AddValue("startLatitude", "0");
            node.AddValue("startLongitude", "0");
            node.AddValue("lastSampleUniversalTime", "1010");
            node.AddValue("maximumAltitudeMeters", "1000");
            node.AddValue("maximumSurfaceSpeedMetersPerSecond", "650");
            node.AddValue("enteredOrbit", false);
            ConfigNode controlStateNode = node.AddNode("CONTROL_STATE");
            controlStateNode.AddValue("objectiveId", ObjectiveCatalogue.Control1Id);
            controlStateNode.AddValue("holdSeconds", "not-a-number");
            controlStateNode.AddValue("wasSampleInBand", true);
            controlStateNode.AddValue("qualified", false);

            var loadedState = new ActiveContractProgressSaveState();
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
                "Malformed per-contract Control save data should clear the active attempt rather than restore invented progress.");
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
            double longitudeDegrees = 0.0)
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
                observationUniversalTime);
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

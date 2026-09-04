using System;
using System.Collections.Generic;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Persistence;
using TheRaceForSpace.Programs;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.Tests.Tracking
{
    internal static class StarterFlightTrackerTests
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
            CurrentMilestoneUsesGlobalLineUnlocks();
            MalformedActiveSaveIsDiscarded();
        }

        private static void DirectedPowerRequiresImpactBelowCeiling()
        {
            SpaceProgramState player = new SpaceProgramState("player", "Player", true);
            var programs = new List<SpaceProgramState> { player };
            var tracker = new StarterFlightTracker();

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("power-a", 0.0, 0.0, 100.0, 0.0, 0.1, 0, null, TrackedFlightSituation.Prelaunch));
            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("power-a", 0.0, 10.0, 50000.0, 650.0, 0.1, 0, null, TrackedFlightSituation.Flying));
            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("power-a", 0.0, 10.5, 0.0, 0.0, 0.1, 0, null, TrackedFlightSituation.Landed));

            Require(
                !player.HasAchievement(PrototypeMilestones.DirectedPower1Id),
                "Directed Power must not complete before the expendable impact occurs.");
            Require(
                tracker.RecordSurfaceImpact(
                    player,
                    programs,
                    PrototypeMilestones.StarterContracts,
                    "power-a",
                    "Kerbin",
                    11.0),
                "A qualifying flight history should still complete when KSP reports the crash after the vessel briefly enters a landed state.");

            SpaceProgramState overCeilingPlayer = new SpaceProgramState("over", "Over", true);
            var overCeilingPrograms = new List<SpaceProgramState> { overCeilingPlayer };
            var overCeilingTracker = new StarterFlightTracker();
            overCeilingTracker.RefreshPlayerMilestones(
                overCeilingPlayer,
                overCeilingPrograms,
                PrototypeMilestones.StarterContracts,
                Snapshot("power-b", 20.0, 20.0, 70001.0, 700.0, 0.1, 0, null, TrackedFlightSituation.SubOrbital));

            Require(
                !overCeilingTracker.RecordSurfaceImpact(
                    overCeilingPlayer,
                    overCeilingPrograms,
                    PrototypeMilestones.StarterContracts,
                    "power-b",
                    "Kerbin",
                    21.0),
                "Ever exceeding 70 km must invalidate the Directed Power attempt.");
        }

        private static void MassUsesRemainingMassAndLaunchDistance()
        {
            SpaceProgramState player = new SpaceProgramState("player", "Player", true);
            var programs = new List<SpaceProgramState> { player };
            var tracker = new StarterFlightTracker();

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("mass-a", 100.0, 100.0, 100.0, 0.0, 2.0, 0, null, TrackedFlightSituation.Prelaunch, 0.0));
            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("mass-a", 100.0, 110.0, 1000.0, 300.0, 1.1, 0, null, TrackedFlightSituation.Flying, 3.0));

            Require(!player.HasAchievement(PrototypeMilestones.Mass1Id),
                "Flying more than 25 km with enough mass must not complete Mass I before landing.");

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("mass-a", 100.0, 111.0, 0.0, 0.0, 1.1, 0, null, TrackedFlightSituation.Landed, 3.0));
            Require(player.HasAchievement(PrototypeMilestones.Mass1Id),
                "A finished landed craft retaining at least 1 t more than 25 km from launch should complete Mass I.");

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("mass-a", 100.0, 120.0, 1000.0, 400.0, 3.0, 0, null, TrackedFlightSituation.Flying, 10.0));
            Require(!player.HasAchievement(PrototypeMilestones.Mass2Id),
                "One launch must not cascade through multiple Mass milestones.");

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("mass-b", 200.0, 200.0, 100.0, 0.0, 3.0, 0, null, TrackedFlightSituation.Prelaunch, 0.0));
            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("mass-b", 200.0, 210.0, 1000.0, 400.0, 2.6, 0, null, TrackedFlightSituation.Flying, 8.0));
            Require(!player.HasAchievement(PrototypeMilestones.Mass2Id),
                "Mass II must wait for the qualifying 2.5 t craft to land beyond 75 km.");

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("mass-b", 200.0, 211.0, 0.0, 0.0, 2.6, 0, null, TrackedFlightSituation.Landed, 8.0));
            Require(player.HasAchievement(PrototypeMilestones.Mass2Id),
                "A fresh landed craft retaining at least 2.5 t beyond 75 km should complete Mass II.");
        }

        private static void ControlRequiresContinuousCrewedHoldAndLanding()
        {
            SpaceProgramState player = new SpaceProgramState("player", "Player", true);
            var programs = new List<SpaceProgramState> { player };
            var tracker = new StarterFlightTracker();

            for (int elapsedSeconds = 0; elapsedSeconds <= 30; elapsedSeconds += 5)
            {
                tracker.RefreshPlayerMilestones(
                    player,
                    programs,
                    PrototypeMilestones.StarterContracts,
                    Snapshot(
                        "control-a",
                        300.0,
                        300.0 + elapsedSeconds,
                        3000.0,
                        150.0,
                        1.0,
                        1,
                        null,
                        TrackedFlightSituation.Flying));
            }

            Require(!player.HasAchievement(PrototypeMilestones.Control1Id),
                "Completing the altitude hold alone must not award Control I before landing.");

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("control-a", 300.0, 331.0, 80.0, 0.0, 1.0, 1, null, TrackedFlightSituation.Landed));
            Require(player.HasAchievement(PrototypeMilestones.Control1Id),
                "A qualified Control hold followed by a crewed Kerbin landing should complete the milestone.");
        }

        private static void UnobservedControlGapResetsHold()
        {
            SpaceProgramState player = new SpaceProgramState("player", "Player", true);
            var programs = new List<SpaceProgramState> { player };
            var tracker = new StarterFlightTracker();

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("control-gap", 350.0, 350.0, 3000.0, 150.0, 1.0, 1, null, TrackedFlightSituation.Flying));
            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("control-gap", 350.0, 354.0, 3000.0, 150.0, 1.0, 1, null, TrackedFlightSituation.Flying));
            RequireNear(4.0, tracker.ControlHoldSeconds,
                "Closely spaced observed samples should accumulate Control hold time.");

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("control-gap", 350.0, 370.0, 3000.0, 150.0, 1.0, 1, null, TrackedFlightSituation.Flying));

            RequireNear(0.0, tracker.ControlHoldSeconds,
                "A long unobserved gap must reset an unqualified continuous Control hold.");
            Require(string.IsNullOrEmpty(tracker.QualifiedControlMilestoneId),
                "A long unobserved gap must not qualify Control from missing flight time.");
            Require(!player.HasAchievement(PrototypeMilestones.Control1Id),
                "An unobserved gap must not complete Control I.");
        }

        private static void BiomeAllowsOnlyOneLineMilestonePerLaunch()
        {
            SpaceProgramState player = new SpaceProgramState("player", "Player", true);
            var programs = new List<SpaceProgramState> { player };
            var tracker = new StarterFlightTracker();

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("biome-a", 400.0, 400.0, 500.0, 50.0, 1.0, 0, "Shores", TrackedFlightSituation.Flying));
            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("biome-a", 400.0, 410.0, 500.0, 50.0, 1.0, 0, "Grasslands", TrackedFlightSituation.Flying));

            Require(!player.HasAchievement(PrototypeMilestones.Biome1Id),
                "Flying over Grasslands must not complete Biome I.");

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("biome-a", 400.0, 411.0, 0.0, 0.0, 1.0, 0, "Grasslands", TrackedFlightSituation.Landed));
            Require(player.HasAchievement(PrototypeMilestones.Biome1Id),
                "Landing in Grasslands should complete Biome I.");

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("biome-a", 400.0, 420.0, 500.0, 50.0, 1.0, 0, "Highlands", TrackedFlightSituation.Flying));
            Require(!player.HasAchievement(PrototypeMilestones.Biome2Id),
                "A single exploration launch must not complete two Biome line levels.");

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("biome-b", 500.0, 500.0, 100.0, 0.0, 1.0, 0, "Shores", TrackedFlightSituation.Prelaunch));
            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("biome-b", 500.0, 510.0, 1000.0, 100.0, 1.0, 0, "Highlands", TrackedFlightSituation.Flying));
            Require(!player.HasAchievement(PrototypeMilestones.Biome2Id),
                "Flying over Highlands must not complete Biome II.");

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("biome-b", 500.0, 511.0, 0.0, 0.0, 1.0, 0, "Highlands", TrackedFlightSituation.Landed));
            Require(player.HasAchievement(PrototypeMilestones.Biome2Id),
                "A later launch landed in Highlands may complete the next unlocked Biome milestone.");
        }

        private static void StagingPreservesDirectedPowerAttempt()
        {
            SpaceProgramState player = new SpaceProgramState("player", "Player", true);
            var programs = new List<SpaceProgramState> { player };
            var tracker = new StarterFlightTracker();

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("stage-a", 600.0, 600.0, 20000.0, 500.0, 1.0, 0, null, TrackedFlightSituation.Flying));
            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("stage-b", 600.0, 605.0, 30000.0, 650.0, 0.5, 0, null, TrackedFlightSituation.Flying));

            Require(
                tracker.RecordSurfaceImpact(
                    player,
                    programs,
                    PrototypeMilestones.StarterContracts,
                    "stage-b",
                    "Kerbin",
                    606.0),
                "A stage keeping the same launch time should preserve Directed Power flight history.");
        }

        private static void PartialControlHoldSurvivesSaveLoad()
        {
            SpaceProgramState player = new SpaceProgramState("player", "Player", true);
            var programs = new List<SpaceProgramState> { player };
            var sourceTracker = new StarterFlightTracker();

            for (int elapsedSeconds = 0; elapsedSeconds <= 15; elapsedSeconds += 5)
            {
                sourceTracker.RefreshPlayerMilestones(
                    player,
                    programs,
                    PrototypeMilestones.StarterContracts,
                    Snapshot(
                        "save-a",
                        700.0,
                        700.0 + elapsedSeconds,
                        3000.0,
                        120.0,
                        1.0,
                        1,
                        null,
                        TrackedFlightSituation.Flying));
            }

            var saveState = new ActiveContractProgressSaveState();
            saveState.Capture(sourceTracker);
            var node = new ConfigNode();
            saveState.Save(node);

            var loadedState = new ActiveContractProgressSaveState();
            loadedState.Load(node);
            var restoredTracker = new StarterFlightTracker();
            loadedState.ApplyTo(restoredTracker);

            for (int elapsedSeconds = 20; elapsedSeconds <= 30; elapsedSeconds += 5)
            {
                restoredTracker.RefreshPlayerMilestones(
                    player,
                    programs,
                    PrototypeMilestones.StarterContracts,
                    Snapshot(
                        "save-a",
                        700.0,
                        700.0 + elapsedSeconds,
                        3000.0,
                        120.0,
                        1.0,
                        1,
                        null,
                        TrackedFlightSituation.Flying));
            }

            restoredTracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("save-a", 700.0, 731.0, 80.0, 0.0, 1.0, 1, null, TrackedFlightSituation.Landed));

            Require(player.HasAchievement(PrototypeMilestones.Control1Id),
                "A saved 15-second Control hold should resume and complete after another 15 seconds and landing.");
        }

        private static void MultipleControlStatesSurviveSaveLoad()
        {
            SpaceProgramState player = new SpaceProgramState("player", "Player", true);
            var programs = new List<SpaceProgramState> { player };
            var activeControlContracts = new List<MilestoneDefinition>
            {
                PrototypeMilestones.FindById(PrototypeMilestones.Control1Id),
                PrototypeMilestones.FindById(PrototypeMilestones.Control2Id)
            };
            var sourceTracker = new StarterFlightTracker();

            for (int elapsedSeconds = 0; elapsedSeconds <= 30; elapsedSeconds += 5)
            {
                sourceTracker.RefreshPlayerMilestones(
                    player,
                    programs,
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
                        TrackedFlightSituation.Flying));
            }

            Require(sourceTracker.IsControlMilestoneQualified(PrototypeMilestones.Control1Id),
                "Control I should be qualified before saving the multi-Control attempt.");

            for (int observationUniversalTime = 1035; observationUniversalTime <= 1050;
                observationUniversalTime += 5)
            {
                sourceTracker.RefreshPlayerMilestones(
                    player,
                    programs,
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
                        TrackedFlightSituation.Flying));
            }

            RequireNear(15.0, sourceTracker.GetControlHoldSeconds(PrototypeMilestones.Control2Id),
                "Control II should have independent partial progress before save.");

            var saveState = new ActiveContractProgressSaveState();
            saveState.Capture(sourceTracker);
            var node = new ConfigNode();
            saveState.Save(node);

            Require(node.GetNodes("CONTROL_STATE").Length == 2,
                "The current save format should write one CONTROL_STATE node per tracked Control contract.");
            Require(node.GetValue("controlHoldMilestoneId") == null,
                "The current save format should not write the removed single-Control milestone field.");
            Require(node.GetValue("completedControl") == null,
                "The current save format should not write obsolete per-line completion flags.");

            var loadedState = new ActiveContractProgressSaveState();
            loadedState.Load(node);
            var restoredTracker = new StarterFlightTracker();
            loadedState.ApplyTo(restoredTracker);

            Require(restoredTracker.IsControlMilestoneQualified(PrototypeMilestones.Control1Id),
                "A qualified Control I state should survive save/load independently.");
            RequireNear(15.0, restoredTracker.GetControlHoldSeconds(PrototypeMilestones.Control2Id),
                "Partial Control II progress should survive save/load independently.");
            Require(!restoredTracker.IsControlMilestoneQualified(PrototypeMilestones.Control2Id),
                "Partial Control II progress must not be promoted to qualified by persistence.");

            for (int observationUniversalTime = 1055; observationUniversalTime <= 1080;
                observationUniversalTime += 5)
            {
                restoredTracker.RefreshPlayerMilestones(
                    player,
                    programs,
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
                        TrackedFlightSituation.Flying));
            }

            Require(restoredTracker.IsControlMilestoneQualified(PrototypeMilestones.Control2Id),
                "Control II should continue from its saved partial hold and qualify normally.");

            bool recorded = restoredTracker.RefreshPlayerMilestones(
                player,
                programs,
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
                    TrackedFlightSituation.Landed));

            Require(recorded,
                "The restored landing should record the independently qualified Control contracts.");
            Require(player.HasAchievement(PrototypeMilestones.Control1Id),
                "Restored Control I qualification should complete on landing.");
            Require(player.HasAchievement(PrototypeMilestones.Control2Id),
                "Restored Control II qualification should complete on the same landing.");
        }

        private static void DirectedPowerDisqualificationSurvivesSaveLoad()
        {
            SpaceProgramState player = new SpaceProgramState("player", "Player", true);
            var programs = new List<SpaceProgramState> { player };
            var sourceTracker = new StarterFlightTracker();

            sourceTracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("save-power", 800.0, 800.0, 70010.0, 700.0, 1.0, 0, null, TrackedFlightSituation.SubOrbital));

            var saveState = new ActiveContractProgressSaveState();
            saveState.Capture(sourceTracker);
            var node = new ConfigNode();
            saveState.Save(node);
            var loadedState = new ActiveContractProgressSaveState();
            loadedState.Load(node);
            var restoredTracker = new StarterFlightTracker();
            loadedState.ApplyTo(restoredTracker);

            Require(
                !restoredTracker.RecordSurfaceImpact(
                    player,
                    programs,
                    PrototypeMilestones.StarterContracts,
                    "save-power",
                    "Kerbin",
                    801.0),
                "Saving and loading must not erase an earlier Directed Power altitude violation.");
        }

        private static void LiveProgressReflectsLatestSample()
        {
            SpaceProgramState player = new SpaceProgramState("player", "Player", true);
            var programs = new List<SpaceProgramState> { player };
            var tracker = new StarterFlightTracker();

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("progress", 900.0, 900.0, 100.0, 0.0, 4.0, 0, "Shores", TrackedFlightSituation.Prelaunch, 0.0));
            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("progress", 900.0, 910.0, 3500.0, 525.0, 3.2, 1, "Grasslands", TrackedFlightSituation.Flying, 2.0));

            RequireNear(3500.0, tracker.CurrentAltitudeMeters, "Current altitude should follow the latest sample.");
            RequireNear(525.0, tracker.CurrentSurfaceSpeedMetersPerSecond, "Current surface speed should follow the latest sample.");
            RequireNear(3.2, tracker.CurrentMassTonnes, "Current mass should follow the latest sample.");
            Require(tracker.CurrentDistanceMeters > 20000.0, "Current distance should be derived from the launch position.");
            Require(tracker.CurrentBiomeName == "Grasslands", "Current biome should follow the latest sample.");
            Require(tracker.CurrentCrewCount == 1, "Current crew count should follow the latest sample.");
            Require(tracker.CurrentSituation == TrackedFlightSituation.Flying, "Current situation should follow the latest sample.");
        }

        private static void CurrentMilestoneUsesGlobalLineUnlocks()
        {
            SpaceProgramState player = new SpaceProgramState("player", "Player", true);
            SpaceProgramState rival = new SpaceProgramState("rival", "Rival", false);
            rival.RecordAchievement(PrototypeMilestones.Mass1Id, 50.0);
            var programs = new List<SpaceProgramState> { player, rival };

            MilestoneDefinition milestone = StarterFlightTracker.GetCurrentMilestone(
                StarterContractLine.Mass,
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                60.0);

            Require(milestone != null && milestone.Id == PrototypeMilestones.Mass2Id,
                "A rival completing Mass I should globally unlock Mass II as the player's current line target.");
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
            controlStateNode.AddValue("milestoneId", PrototypeMilestones.Control1Id);
            controlStateNode.AddValue("holdSeconds", "not-a-number");
            controlStateNode.AddValue("wasSampleInBand", true);
            controlStateNode.AddValue("qualified", false);

            var loadedState = new ActiveContractProgressSaveState();
            loadedState.Load(node);
            var tracker = new StarterFlightTracker();
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

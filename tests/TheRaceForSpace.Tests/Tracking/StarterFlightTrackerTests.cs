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
            BiomeAllowsOnlyOneLineMilestonePerLaunch();
            StagingPreservesDirectedPowerAttempt();
            PartialControlHoldSurvivesSaveLoad();
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
                "600 m/s below 70 km followed by a surface impact should complete Directed Power I.");

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

            Require(player.HasAchievement(PrototypeMilestones.Mass1Id),
                "At least 1 t more than 25 km from the launch point should complete Mass I.");

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
            Require(player.HasAchievement(PrototypeMilestones.Mass2Id),
                "A fresh launch carrying at least 2.5 t beyond 75 km should complete Mass II.");
        }

        private static void ControlRequiresContinuousCrewedHoldAndLanding()
        {
            SpaceProgramState player = new SpaceProgramState("player", "Player", true);
            var programs = new List<SpaceProgramState> { player };
            var tracker = new StarterFlightTracker();

            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("control-a", 300.0, 300.0, 3000.0, 150.0, 1.0, 1, null, TrackedFlightSituation.Flying));
            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("control-a", 300.0, 315.0, 3000.0, 150.0, 1.0, 1, null, TrackedFlightSituation.Flying));
            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("control-a", 300.0, 330.0, 3000.0, 150.0, 1.0, 1, null, TrackedFlightSituation.Flying));

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
            tracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("biome-a", 400.0, 420.0, 500.0, 50.0, 1.0, 0, "Highlands", TrackedFlightSituation.Flying));

            Require(player.HasAchievement(PrototypeMilestones.Biome1Id),
                "Visiting Grasslands should complete Biome I.");
            Require(!player.HasAchievement(PrototypeMilestones.Biome2Id),
                "A single exploration flight must not complete two Biome line levels.");

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
            Require(player.HasAchievement(PrototypeMilestones.Biome2Id),
                "A later launch may complete the next unlocked Biome milestone.");
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

            sourceTracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("save-a", 700.0, 700.0, 3000.0, 120.0, 1.0, 1, null, TrackedFlightSituation.Flying));
            sourceTracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("save-a", 700.0, 715.0, 3000.0, 120.0, 1.0, 1, null, TrackedFlightSituation.Flying));

            var saveState = new StarterFlightSaveState();
            saveState.Capture(sourceTracker);
            var node = new ConfigNode();
            saveState.Save(node);

            var loadedState = new StarterFlightSaveState();
            loadedState.Load(node);
            var restoredTracker = new StarterFlightTracker();
            loadedState.ApplyTo(restoredTracker);

            restoredTracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("save-a", 700.0, 730.0, 3000.0, 120.0, 1.0, 1, null, TrackedFlightSituation.Flying));
            restoredTracker.RefreshPlayerMilestones(
                player,
                programs,
                PrototypeMilestones.StarterContracts,
                Snapshot("save-a", 700.0, 731.0, 80.0, 0.0, 1.0, 1, null, TrackedFlightSituation.Landed));

            Require(player.HasAchievement(PrototypeMilestones.Control1Id),
                "A saved 15-second Control hold should resume and complete after another 15 seconds and landing.");
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

            var saveState = new StarterFlightSaveState();
            saveState.Capture(sourceTracker);
            var node = new ConfigNode();
            saveState.Save(node);
            var loadedState = new StarterFlightSaveState();
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
            node.AddValue("maximumAltitudeMeters", "not-a-number");
            node.AddValue("maximumSurfaceSpeedMetersPerSecond", "650");
            node.AddValue("controlHoldSeconds", "10");

            var loadedState = new StarterFlightSaveState();
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
                null,
                null,
                0.0,
                false,
                false,
                false,
                false,
                false,
                false);

            loadedState.ApplyTo(tracker);
            Require(!tracker.HasActiveAttempt,
                "Malformed active starter-flight nodes should clear the attempt rather than restore invented zero values.");
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

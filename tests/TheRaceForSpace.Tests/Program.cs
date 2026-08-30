using System;
using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Persistence;
using TheRaceForSpace.Programs;
using TheRaceForSpace.Simulation;

namespace TheRaceForSpace.Tests
{
    internal static class Program
    {
        private static int _failures;

        private static int Main()
        {
            Run("Satellite funding before saturation", SatelliteFundingBeforeSaturation);
            Run("Satellite funding after saturation", SatelliteFundingAfterSaturation);
            Run("Achievement payout declines and expires", AchievementPayoutDeclinesAndExpires);
            Run("Achievement restore normalizes lifecycle", AchievementRestoreNormalizesLifecycle);
            Run("Prototype milestone definitions match v0.3", PrototypeMilestoneDefinitionsMatchV03);
            Run("Prototype milestone ids are unique", PrototypeMilestoneIdsAreUnique);
            Run("Generic achievement state starts empty", GenericAchievementStateStartsEmpty);
            Run("Generic achievement state records first timestamp", GenericAchievementStateRecordsFirstTimestamp);
            Run("Generic achievement state preserves first timestamp", GenericAchievementStatePreservesFirstTimestamp);
            Run("Generic achievement state accepts arbitrary ids", GenericAchievementStateAcceptsArbitraryIds);
            Run("Generic achievement state validates timestamps", GenericAchievementStateValidatesTimestamps);
            Run("Rival launch costs match target type", RivalLaunchCostsMatchTargetType);
            Run("Rival ETA detects unaffordable mission", RivalEtaDetectsUnaffordableMission);
            Run("Unavailable rival target is abandoned", UnavailableRivalTargetIsAbandoned);
            Run("Rival selects the only available target", RivalSelectsOnlyAvailableTarget);
            Run("Rival completion uses replay timestamp", RivalCompletionUsesReplayTimestamp);
            Run("Race progress persistence round trip", RaceProgressPersistenceRoundTrip);
            Run("Rival persistence round trip", RivalPersistenceRoundTrip);

            Console.WriteLine();
            Console.WriteLine(_failures == 0
                ? "All prototype logic tests passed."
                : _failures + " prototype logic test(s) failed.");
            return _failures == 0 ? 0 : 1;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("PASS: " + name);
            }
            catch (Exception exception)
            {
                _failures++;
                Console.WriteLine("FAIL: " + name);
                Console.WriteLine("      " + exception.Message);
            }
        }

        private static void SatelliteFundingBeforeSaturation()
        {
            var programme = new FundingProgramme("network", "Network", "Kerbin", 10, 200000.0);
            AssertEqual(60000.0, programme.CalculateCurrentPayout(3, 7));
        }

        private static void SatelliteFundingAfterSaturation()
        {
            var programme = new FundingProgramme("network", "Network", "Kerbin", 10, 200000.0);
            AssertEqual(50000.0, programme.CalculateCurrentPayout(3, 12));
        }

        private static void AchievementPayoutDeclinesAndExpires()
        {
            var programme = new AchievementFundingProgramme("probe", "Probe", "Orbit", 100000.0);
            programme.Start();

            AssertEqual(50000.0, programme.CalculateCurrentPayout(true, 2));
            programme.AdvancePayout();
            AssertEqual(90, programme.CurrentInterestPercent);
            AssertEqual(45000.0, programme.CalculateCurrentPayout(true, 2));

            for (int payment = 1; payment < 10; payment++)
            {
                programme.AdvancePayout();
            }

            AssertTrue(programme.IsExpired, "Contract should expire after ten payments.");
            AssertEqual(0.0, programme.CalculateCurrentPayout(true, 1));
        }

        private static void AchievementRestoreNormalizesLifecycle()
        {
            var programme = new AchievementFundingProgramme("probe", "Probe", "Orbit", 100000.0);
            programme.RestoreState(false, 4);

            AssertTrue(programme.HasStarted, "Processed payments imply a started contract.");
            AssertEqual(4, programme.PaymentsProcessed);
            AssertEqual(60, programme.CurrentInterestPercent);
        }

        private static void PrototypeMilestoneDefinitionsMatchV03()
        {
            AssertEqual(6, PrototypeMilestones.All.Count);

            AssertMilestone(
                PrototypeMilestones.All[0],
                PrototypeMilestones.ProbeOrbitId,
                "Probe Orbit",
                "Kerbin",
                MilestoneCrewRequirement.UncrewedProbe,
                null);
            AssertMilestone(
                PrototypeMilestones.All[1],
                PrototypeMilestones.CrewedOrbitId,
                "Crewed Orbit",
                "Kerbin",
                MilestoneCrewRequirement.Crewed,
                null);
            AssertMilestone(
                PrototypeMilestones.All[2],
                PrototypeMilestones.MunProbeOrbitId,
                "Mun Probe Orbit",
                "Mun",
                MilestoneCrewRequirement.UncrewedProbe,
                PrototypeMilestones.ProbeOrbitId);
            AssertMilestone(
                PrototypeMilestones.All[3],
                PrototypeMilestones.MinmusProbeOrbitId,
                "Minmus Probe Orbit",
                "Minmus",
                MilestoneCrewRequirement.UncrewedProbe,
                PrototypeMilestones.ProbeOrbitId);
            AssertMilestone(
                PrototypeMilestones.All[4],
                PrototypeMilestones.MunCrewedOrbitId,
                "Mun Crewed Orbit",
                "Mun",
                MilestoneCrewRequirement.Crewed,
                PrototypeMilestones.CrewedOrbitId);
            AssertMilestone(
                PrototypeMilestones.All[5],
                PrototypeMilestones.MinmusCrewedOrbitId,
                "Minmus Crewed Orbit",
                "Minmus",
                MilestoneCrewRequirement.Crewed,
                PrototypeMilestones.CrewedOrbitId);
        }

        private static void PrototypeMilestoneIdsAreUnique()
        {
            var milestoneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int milestoneIndex = 0; milestoneIndex < PrototypeMilestones.All.Count; milestoneIndex++)
            {
                MilestoneDefinition milestone = PrototypeMilestones.All[milestoneIndex];
                AssertTrue(
                    milestoneIds.Add(milestone.Id),
                    "Duplicate milestone id found: " + milestone.Id);
            }
        }

        private static void GenericAchievementStateStartsEmpty()
        {
            var program = new SpaceProgramState("Program", false);

            AssertTrue(
                !program.HasAchievement(PrototypeMilestones.ProbeOrbitId),
                "A new program should not have recorded achievements.");
            AssertEqual(-1.0, program.GetAchievementUniversalTime(PrototypeMilestones.ProbeOrbitId));
            AssertTrue(!program.HasAchievement("not-defined"), "Unknown milestone IDs should not appear achieved.");
            AssertEqual(-1.0, program.GetAchievementUniversalTime("not-defined"));
        }

        private static void GenericAchievementStateRecordsFirstTimestamp()
        {
            var program = new SpaceProgramState("Program", false);

            AssertTrue(
                program.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 1234.0),
                "The first milestone observation should be recorded.");
            AssertTrue(
                program.HasAchievement("PROBE-ORBIT"),
                "Milestone IDs should be matched case-insensitively.");
            AssertEqual(1234.0, program.GetAchievementUniversalTime(PrototypeMilestones.ProbeOrbitId));
        }

        private static void GenericAchievementStatePreservesFirstTimestamp()
        {
            var program = new SpaceProgramState("Program", false);
            program.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 1234.0);

            AssertTrue(
                !program.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 5678.0),
                "A repeated observation should not replace the original milestone time.");
            AssertEqual(1234.0, program.GetAchievementUniversalTime(PrototypeMilestones.ProbeOrbitId));
        }

        private static void GenericAchievementStateAcceptsArbitraryIds()
        {
            var program = new SpaceProgramState("Program", false);

            AssertTrue(
                program.RecordAchievement("future-duna-probe-orbit", 4321.0),
                "Program state should accept milestone IDs that were not hardcoded into the class.");
            AssertTrue(program.HasAchievement("future-duna-probe-orbit"), "Arbitrary milestone should be recorded.");
            AssertEqual(4321.0, program.GetAchievementUniversalTime("future-duna-probe-orbit"));
        }

        private static void GenericAchievementStateValidatesTimestamps()
        {
            var program = new SpaceProgramState("Program", false);

            AssertTrue(!program.RecordAchievement(null, 10.0), "Null milestone IDs should be ignored.");
            AssertTrue(!program.RecordAchievement(string.Empty, 10.0), "Empty milestone IDs should be ignored.");
            AssertTrue(!program.RecordAchievement("nan", double.NaN), "NaN achievement times should be ignored.");
            AssertTrue(
                !program.RecordAchievement("infinite", double.PositiveInfinity),
                "Infinite achievement times should be ignored.");
            AssertTrue(program.RecordAchievement("early", -10.0), "Finite negative times should be normalized.");
            AssertEqual(0.0, program.GetAchievementUniversalTime("early"));
        }

        private static void RivalLaunchCostsMatchTargetType()
        {
            var rival = new SpaceProgramState("Rival", false);

            rival.NextLaunchBodyName = RivalSimulation.ProbeOrbitTargetName;
            AssertEqual(20000.0, RivalSimulation.CalculateLaunchProgressCost(rival));

            rival.NextLaunchBodyName = RivalSimulation.CrewedOrbitTargetName;
            AssertEqual(40000.0, RivalSimulation.CalculateLaunchProgressCost(rival));

            rival.NextLaunchBodyName = "Mun";
            AssertEqual(40000.0, RivalSimulation.CalculateLaunchProgressCost(rival));
        }

        private static void RivalEtaDetectsUnaffordableMission()
        {
            var rival = new SpaceProgramState("Rival", false)
            {
                NextLaunchBodyName = RivalSimulation.ProbeOrbitTargetName,
                Funds = 0.0,
                NextPayoutFunds = 0.0
            };

            int? estimatedDays = RivalSimulation.CalculateEstimatedLaunchDays(rival, 0.0, 90.0 * 21600.0, 90.0 * 21600.0);
            AssertTrue(!estimatedDays.HasValue, "A mission with no current or future funds should have no ETA.");
        }

        private static void UnavailableRivalTargetIsAbandoned()
        {
            SpaceProgramState player = new SpaceProgramState("Player", true);
            SpaceProgramState aster = new SpaceProgramState("Aster", false)
            {
                NextLaunchBodyName = RivalSimulation.MunProbeOrbitTargetName,
                LaunchProgressPercent = 50
            };
            SpaceProgramState cobalt = new SpaceProgramState("Cobalt", false);

            RivalSimulation.Refresh(
                player,
                aster,
                cobalt,
                0.0,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false);

            AssertEqual(null, aster.NextLaunchBodyName);
            AssertEqual(0, aster.LaunchProgressPercent);
        }

        private static void RivalSelectsOnlyAvailableTarget()
        {
            SpaceProgramState player = new SpaceProgramState("Player", true);
            SpaceProgramState aster = new SpaceProgramState("Aster", false)
            {
                HasAchievedCrewedOrbit = true,
                CrewedOrbitAchievementUniversalTime = 1.0,
                NextLaunchBodyName = null
            };
            SpaceProgramState cobalt = new SpaceProgramState("Cobalt", false);

            RivalSimulation.Refresh(
                player,
                aster,
                cobalt,
                0.0,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true);

            AssertEqual(RivalSimulation.MinmusCrewedOrbitTargetName, aster.NextLaunchBodyName);
        }

        private static void RivalCompletionUsesReplayTimestamp()
        {
            const double replayUniversalTime = 90.0 * 21600.0;
            SpaceProgramState player = new SpaceProgramState("Player", true);
            SpaceProgramState aster = new SpaceProgramState("Aster", false)
            {
                NextLaunchBodyName = RivalSimulation.ProbeOrbitTargetName,
                LaunchProgressPercent = 100
            };
            SpaceProgramState cobalt = new SpaceProgramState("Cobalt", false);

            RivalSimulation.Refresh(
                player,
                aster,
                cobalt,
                replayUniversalTime,
                false,
                false,
                false,
                true,
                false,
                false,
                false,
                false,
                false);

            AssertTrue(aster.HasAchievedProbeOrbit, "Completed rival mission should record its achievement.");
            AssertEqual(replayUniversalTime, aster.ProbeOrbitAchievementUniversalTime);
            AssertEqual(1, aster.GetSatelliteCount("Kerbin"));
        }

        private static void RaceProgressPersistenceRoundTrip()
        {
            SpaceProgramState sourcePlayer = new SpaceProgramState("Player", true)
            {
                HasAchievedProbeOrbit = true,
                ProbeOrbitAchievementUniversalTime = 1234.0,
                HasAchievedCrewedOrbit = true,
                CrewedOrbitAchievementUniversalTime = 5678.0
            };
            FundingProgramme sourceKerbin = Network("Kerbin", true);
            FundingProgramme sourceMun = Network("Mun", true);
            FundingProgramme sourceMinmus = Network("Minmus", false);
            AchievementFundingProgramme[] sourceAchievements = AchievementProgrammes();
            sourceAchievements[0].Start();
            sourceAchievements[0].AdvancePayout();
            sourceAchievements[0].AdvancePayout();
            sourceAchievements[1].Start();

            var saved = new RaceProgressSaveState();
            saved.Capture(
                sourcePlayer,
                sourceKerbin,
                sourceMun,
                sourceMinmus,
                sourceAchievements[0],
                sourceAchievements[1],
                sourceAchievements[2],
                sourceAchievements[3],
                sourceAchievements[4],
                sourceAchievements[5]);

            var node = new ConfigNode();
            saved.Save(node);
            var loaded = new RaceProgressSaveState();
            loaded.Load(node);

            SpaceProgramState restoredPlayer = new SpaceProgramState("Player", true);
            FundingProgramme restoredKerbin = Network("Kerbin", false);
            FundingProgramme restoredMun = Network("Mun", false);
            FundingProgramme restoredMinmus = Network("Minmus", false);
            AchievementFundingProgramme[] restoredAchievements = AchievementProgrammes();
            loaded.ApplyTo(
                restoredPlayer,
                restoredKerbin,
                restoredMun,
                restoredMinmus,
                restoredAchievements[0],
                restoredAchievements[1],
                restoredAchievements[2],
                restoredAchievements[3],
                restoredAchievements[4],
                restoredAchievements[5]);

            AssertTrue(restoredPlayer.HasAchievedProbeOrbit, "Probe Orbit should round trip.");
            AssertEqual(1234.0, restoredPlayer.ProbeOrbitAchievementUniversalTime);
            AssertTrue(restoredMun.IsAvailable, "Mun network unlock should round trip.");
            AssertTrue(!restoredMinmus.IsAvailable, "Locked Minmus network should remain locked.");
            AssertEqual(2, restoredAchievements[0].PaymentsProcessed);
            AssertTrue(restoredAchievements[1].HasStarted, "Crewed contract start should round trip.");
        }

        private static void RivalPersistenceRoundTrip()
        {
            var source = new SpaceProgramState("Aster", false)
            {
                Funds = 75000.0,
                HasAchievedProbeOrbit = true,
                ProbeOrbitAchievementUniversalTime = 2000.0,
                NextLaunchBodyName = RivalSimulation.MunProbeOrbitTargetName,
                LaunchProgressPercent = 40,
                NextLaunchProgressCheckUniversalTime = 9000.0
            };
            source.SetSatelliteCount("Kerbin", 3);
            source.SetSatelliteCount("Mun", 1);

            var saved = new RivalProgramSaveState();
            saved.Capture(source);
            var node = new ConfigNode();
            saved.Save(node);
            var loaded = new RivalProgramSaveState();
            loaded.Load(node);

            var restored = new SpaceProgramState("Aster", false);
            loaded.ApplyTo(restored);

            AssertEqual(75000.0, restored.Funds);
            AssertEqual(3, restored.GetSatelliteCount("Kerbin"));
            AssertEqual(1, restored.GetSatelliteCount("Mun"));
            AssertTrue(restored.HasAchievedProbeOrbit, "Rival achievement should round trip.");
            AssertEqual(RivalSimulation.MunProbeOrbitTargetName, restored.NextLaunchBodyName);
            AssertEqual(40, restored.LaunchProgressPercent);
            AssertEqual(9000.0, restored.NextLaunchProgressCheckUniversalTime);
        }

        private static FundingProgramme Network(string bodyName, bool available)
        {
            return new FundingProgramme(
                bodyName.ToLowerInvariant(),
                bodyName,
                bodyName,
                5,
                100000.0,
                available,
                null);
        }

        private static AchievementFundingProgramme[] AchievementProgrammes()
        {
            return new[]
            {
                new AchievementFundingProgramme("probe", "Probe", "Probe", 100000.0),
                new AchievementFundingProgramme("crewed", "Crewed", "Crewed", 200000.0),
                new AchievementFundingProgramme("mun-probe", "Mun Probe", "Mun Probe", 200000.0),
                new AchievementFundingProgramme("minmus-probe", "Minmus Probe", "Minmus Probe", 200000.0),
                new AchievementFundingProgramme("mun-crewed", "Mun Crewed", "Mun Crewed", 300000.0),
                new AchievementFundingProgramme("minmus-crewed", "Minmus Crewed", "Minmus Crewed", 300000.0)
            };
        }

        private static void AssertMilestone(
            MilestoneDefinition milestone,
            string expectedId,
            string expectedName,
            string expectedBodyName,
            MilestoneCrewRequirement expectedCrewRequirement,
            string expectedPrerequisiteId)
        {
            AssertEqual(expectedId, milestone.Id);
            AssertEqual(expectedName, milestone.Name);
            AssertEqual(expectedBodyName, milestone.CelestialBodyName);
            AssertEqual(MilestoneSituation.Orbit, milestone.Situation);
            AssertEqual(expectedCrewRequirement, milestone.CrewRequirement);
            AssertEqual(expectedPrerequisiteId, milestone.PrerequisiteMilestoneId);
            AssertTrue(
                !string.IsNullOrEmpty(milestone.ObjectiveDescription),
                "Milestone objective description should not be empty.");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    "Expected '" + expected + "' but got '" + actual + "'.");
            }
        }
    }
}

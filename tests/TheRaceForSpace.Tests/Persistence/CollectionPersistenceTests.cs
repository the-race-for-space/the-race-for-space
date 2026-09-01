using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Persistence;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Tests.Persistence
{
    internal static class CollectionPersistenceTests
    {
        public static void RaceProgressRoundTripsArbitraryIds()
        {
            const double nextFundingUniversalTime = 98765.0;
            var player = new SpaceProgramState("Player", true);
            player.RecordAchievement("duna-probe-orbit", 1234.0);

            var fundingProgrammes = new List<FundingProgramme>
            {
                new FundingProgramme(
                    "duna-network",
                    "Duna Network",
                    "Duna",
                    5,
                    100000.0,
                    true,
                    null)
            };
            var achievementProgrammes = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme(
                    "duna-probe-orbit",
                    "Duna Probe Orbit",
                    "Orbit Duna",
                    200000.0)
            };
            achievementProgrammes[0].Start();
            achievementProgrammes[0].AdvancePayout();
            achievementProgrammes[0].AdvancePayout();
            achievementProgrammes[0].AdvancePayout();

            var saved = new RaceProgressSaveState();
            saved.Capture(
                player,
                fundingProgrammes,
                achievementProgrammes,
                nextFundingUniversalTime);
            var node = new ConfigNode();
            saved.Save(node);

            var loaded = new RaceProgressSaveState();
            loaded.Load(node);
            var restoredPlayer = new SpaceProgramState("Player", true);
            var restoredFunding = new List<FundingProgramme>
            {
                new FundingProgramme(
                    "duna-network",
                    "Duna Network",
                    "Duna",
                    5,
                    100000.0,
                    false,
                    null)
            };
            var restoredAchievements = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme(
                    "duna-probe-orbit",
                    "Duna Probe Orbit",
                    "Orbit Duna",
                    200000.0)
            };

            loaded.ApplyTo(restoredPlayer, restoredFunding, restoredAchievements);

            Equal(1234.0, restoredPlayer.GetAchievementUniversalTime("duna-probe-orbit"));
            Require(restoredFunding[0].IsAvailable, "Arbitrary funding programme IDs should round trip.");
            Require(restoredAchievements[0].HasStarted, "Arbitrary achievement contracts should remain started.");
            Equal(3, restoredAchievements[0].PaymentsProcessed);
            Equal(nextFundingUniversalTime, loaded.NextFundingUniversalTime);
        }

        public static void RivalRoundTripsArbitraryBodyAndTargetId()
        {
            var aster = new SpaceProgramState("aster", "Aster", false)
            {
                Funds = 54321.0,
                NextMissionTargetId = "duna-network",
                LaunchProgressPercent = 60,
                NextLaunchProgressCheckUniversalTime = 9876.0
            };
            aster.RecordAchievement("duna-probe-orbit", 4321.0);
            aster.SetSatelliteCount("Duna", 4);

            var delta = new SpaceProgramState("delta", "Delta", false)
            {
                Funds = 22222.0,
                NextMissionTargetId = "eve-network",
                LaunchProgressPercent = 30,
                NextLaunchProgressCheckUniversalTime = 7654.0
            };
            delta.RecordAchievement("eve-probe-orbit", 3456.0);
            delta.SetSatelliteCount("Eve", 2);

            var saved = new RivalProgramsSaveState();
            saved.Capture(new List<SpaceProgramState> { aster, delta });
            var node = new ConfigNode();
            saved.Save(node);

            Equal(2, node.GetNodes("RIVAL").Length);

            var loaded = new RivalProgramsSaveState();
            loaded.Load(node);

            // Restore in a different order and with changed display names to prove identity comes
            // from stable IDs rather than fixed Aster/Cobalt slots or list position.
            var restoredDelta = new SpaceProgramState("delta", "Delta Renamed", false);
            var unsavedRival = new SpaceProgramState("echo", "Echo", false)
            {
                Funds = 777.0,
                NextMissionTargetId = "constructor-default",
                LaunchProgressPercent = 20
            };
            var restoredAster = new SpaceProgramState("aster", "Aster Renamed", false);

            loaded.ApplyTo(new List<SpaceProgramState>
            {
                restoredDelta,
                unsavedRival,
                restoredAster
            });

            Equal(54321.0, restoredAster.Funds);
            Equal(4321.0, restoredAster.GetAchievementUniversalTime("duna-probe-orbit"));
            Equal(4, restoredAster.GetSatelliteCount("Duna"));
            Equal("duna-network", restoredAster.NextMissionTargetId);
            Equal(60, restoredAster.LaunchProgressPercent);
            Equal(9876.0, restoredAster.NextLaunchProgressCheckUniversalTime);

            Equal(22222.0, restoredDelta.Funds);
            Equal(3456.0, restoredDelta.GetAchievementUniversalTime("eve-probe-orbit"));
            Equal(2, restoredDelta.GetSatelliteCount("Eve"));
            Equal("eve-network", restoredDelta.NextMissionTargetId);
            Equal(30, restoredDelta.LaunchProgressPercent);
            Equal(7654.0, restoredDelta.NextLaunchProgressCheckUniversalTime);

            Equal(777.0, unsavedRival.Funds);
            Equal("constructor-default", unsavedRival.NextMissionTargetId);
            Equal(20, unsavedRival.LaunchProgressPercent);
        }

        public static void MalformedCollectionNodesAreHandledSafely()
        {
            var raceNode = new ConfigNode();
            raceNode.AddValue("nextFundingUniversalTime", "NaN");
            ConfigNode badAchievement = raceNode.AddNode("ACHIEVEMENT");
            badAchievement.AddValue("id", "bad-time");
            badAchievement.AddValue("universalTime", "NaN");
            ConfigNode earlyAchievement = raceNode.AddNode("ACHIEVEMENT");
            earlyAchievement.AddValue("id", "early-milestone");
            earlyAchievement.AddValue("universalTime", "-12");
            ConfigNode contract = raceNode.AddNode("CONTRACT");
            contract.AddValue("id", "future-contract");
            contract.AddValue("started", "not-a-bool");
            contract.AddValue("paymentsProcessed", "99");

            var raceState = new RaceProgressSaveState();
            raceState.Load(raceNode);
            var player = new SpaceProgramState("Player", true);
            var achievementProgrammes = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme(
                    "future-contract",
                    "Future Contract",
                    "Objective",
                    100000.0)
            };
            raceState.ApplyTo(player, new List<FundingProgramme>(), achievementProgrammes);

            Require(!player.HasAchievement("bad-time"), "Non-finite achievement times should be ignored.");
            Equal(0.0, player.GetAchievementUniversalTime("early-milestone"));
            Equal(10, achievementProgrammes[0].PaymentsProcessed);
            Equal(-1.0, raceState.NextFundingUniversalTime);
            Require(
                achievementProgrammes[0].HasStarted,
                "Processed payments should normalize the restored contract to started.");

            var rivalNode = new ConfigNode();
            rivalNode.AddValue("funds", "NaN");
            rivalNode.AddValue("nextMissionTargetId", "future-target");
            rivalNode.AddValue("launchProgressPercent", "999");
            rivalNode.AddValue("nextLaunchProgressCheckUniversalTime", "-50");
            ConfigNode satellite = rivalNode.AddNode("SATELLITE");
            satellite.AddValue("body", "Duna");
            satellite.AddValue("count", "-4");

            var rivalState = new RivalProgramSaveState();
            rivalState.Load(rivalNode);
            var rival = new SpaceProgramState("Aster", false);
            rivalState.ApplyTo(rival);

            Equal(0.0, rival.Funds);
            Equal("future-target", rival.NextMissionTargetId);
            Equal(100, rival.LaunchProgressPercent);
            Equal(0.0, rival.NextLaunchProgressCheckUniversalTime);
            Equal(0, rival.GetSatelliteCount("Duna"));
        }

        public static void EmptyCollectionNodesRestoreWithoutInventingState()
        {
            var raceState = new RaceProgressSaveState();
            raceState.Load(new ConfigNode());
            var player = new SpaceProgramState("Player", true);
            player.RecordAchievement("stale-milestone", 55.0);
            var funding = new List<FundingProgramme>
            {
                new FundingProgramme("future-network", "Future", "Duna", 5, 100000.0, true, null)
            };
            var achievements = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme("future-contract", "Future", "Objective", 100000.0)
            };
            achievements[0].Start();
            achievements[0].AdvancePayout();
            raceState.ApplyTo(player, funding, achievements);

            Require(!player.HasAchievement("stale-milestone"), "Empty race state should clear stale achievements.");
            Require(!funding[0].IsAvailable, "Empty race state should restore funding programmes as locked.");
            Require(!achievements[0].HasStarted, "Empty race state should reset contract lifecycle state.");
            Equal(0, achievements[0].PaymentsProcessed);
            Equal(-1.0, raceState.NextFundingUniversalTime);

            var rivalState = new RivalProgramSaveState();
            rivalState.Load(new ConfigNode());
            var rival = new SpaceProgramState("Aster", false)
            {
                NextMissionTargetId = "constructor-default",
                Funds = 100.0,
                LaunchProgressPercent = 80
            };
            rival.RecordAchievement("stale-rival-achievement", 10.0);
            rival.SetSatelliteCount("Duna", 5);
            rivalState.ApplyTo(rival);

            Equal(0.0, rival.Funds);
            Equal(null, rival.NextMissionTargetId);
            Equal(0, rival.LaunchProgressPercent);
            Require(
                !rival.HasAchievement("stale-rival-achievement"),
                "Empty rival state should clear stale achievements.");
            Equal(0, rival.GetSatelliteCount("Duna"));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.InvalidOperationException(message);
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new System.InvalidOperationException(
                    "Expected '" + expected + "' but got '" + actual + "'.");
            }
        }
    }
}

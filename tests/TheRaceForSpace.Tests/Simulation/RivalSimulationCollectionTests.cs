using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;
using TheRaceForSpace.Simulation;

namespace TheRaceForSpace.Tests.Simulation
{
    internal static class RivalSimulationCollectionTests
    {
        public static void SelectsOnlyAvailableAchievementFromCollection()
        {
            var player = new SpaceProgramState("player", "Player", true);
            var aster = new SpaceProgramState("aster", "Aster", false);
            var cobalt = new SpaceProgramState("cobalt", "Cobalt", false);
            var delta = new SpaceProgramState("delta", "Delta", false);
            delta.RecordAchievement(PrototypeMilestones.CrewedOrbitId, 1.0);

            MilestoneDefinition milestone = PrototypeMilestones.FindById(
                PrototypeMilestones.MinmusCrewedOrbitId);
            var achievementProgrammes = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme(
                    milestone.Id,
                    milestone.Name,
                    milestone.ObjectiveDescription,
                    300000.0,
                    "Display text",
                    milestone.UnlockRule)
            };

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, aster, cobalt, delta },
                1.0,
                achievementProgrammes,
                new List<FundingProgramme>());

            TestAssert.Equal(PrototypeMilestones.MinmusCrewedOrbitId, aster.NextMissionTargetId);
            TestAssert.Equal("Minmus Crewed Orbit", aster.NextLaunchBodyName);
            TestAssert.Equal(PrototypeMilestones.MinmusCrewedOrbitId, cobalt.NextMissionTargetId);
        }

        public static void LockedAchievementIsExcludedFromCollection()
        {
            var player = new SpaceProgramState("Player", true);
            var aster = new SpaceProgramState("Aster", false);
            var cobalt = new SpaceProgramState("Cobalt", false);

            MilestoneDefinition milestone = PrototypeMilestones.FindById(
                PrototypeMilestones.MunProbeOrbitId);
            var achievementProgrammes = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme(
                    milestone.Id,
                    milestone.Name,
                    milestone.ObjectiveDescription,
                    200000.0,
                    "Display text",
                    milestone.UnlockRule)
            };

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, aster, cobalt },
                0.0,
                achievementProgrammes,
                new List<FundingProgramme>());

            TestAssert.Equal(null, aster.NextMissionTargetId);
        }

        public static void FlexibleRuleUsesRivalScopeCountAndHistoricalTime()
        {
            var player = new SpaceProgramState("player", "Player", true);
            var aster = new SpaceProgramState("aster", "Aster", false);
            var cobalt = new SpaceProgramState("cobalt", "Cobalt", false);
            var delta = new SpaceProgramState("delta", "Delta", false);
            aster.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 100.0);
            delta.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 200.0);

            MilestoneDefinition milestone = PrototypeMilestones.FindById(
                PrototypeMilestones.MinmusCrewedOrbitId);
            var achievementProgrammes = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme(
                    milestone.Id,
                    milestone.Name,
                    milestone.ObjectiveDescription,
                    300000.0,
                    "Display text",
                    new UnlockRuleDefinition(
                        new UnlockPathDefinition(
                            UnlockConditionDefinition.Achievement(
                                PrototypeMilestones.ProbeOrbitId,
                                UnlockProgramScope.AnyRival,
                                2))))
            };
            var programs = new List<SpaceProgramState> { player, aster, cobalt, delta };

            RivalSimulation.Refresh(
                programs,
                199.0,
                achievementProgrammes,
                new List<FundingProgramme>());
            TestAssert.Equal(
                null,
                cobalt.NextMissionTargetId);

            RivalSimulation.Refresh(
                programs,
                200.0,
                achievementProgrammes,
                new List<FundingProgramme>());
            TestAssert.Equal(
                PrototypeMilestones.MinmusCrewedOrbitId,
                cobalt.NextMissionTargetId);
        }

        public static void SatelliteProgrammeRemainsRepeatable()
        {
            var player = new SpaceProgramState("Player", true);
            var aster = new SpaceProgramState("Aster", false);
            var cobalt = new SpaceProgramState("Cobalt", false);
            aster.SetSatelliteCount("Duna", 3);

            var fundingProgrammes = new List<FundingProgramme>
            {
                new FundingProgramme(
                    "duna-network",
                    "Duna Orbital Network",
                    "Duna",
                    5,
                    100000.0,
                    true,
                    null,
                    (UnlockRuleDefinition)null)
            };

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, aster, cobalt },
                0.0,
                new List<AchievementFundingProgramme>(),
                fundingProgrammes);

            TestAssert.Equal("duna-network", aster.NextMissionTargetId);
            TestAssert.Equal("Duna", aster.NextLaunchBodyName);
        }

        public static void CompletesArbitrarySatelliteProgramme()
        {
            var player = new SpaceProgramState("player", "Player", true);
            var aster = new SpaceProgramState("aster", "Aster", false);
            var cobalt = new SpaceProgramState("cobalt", "Cobalt", false);
            var delta = new SpaceProgramState("delta", "Delta", false)
            {
                NextMissionTargetId = "duna-network",
                LaunchProgressPercent = 100
            };

            var fundingProgrammes = new List<FundingProgramme>
            {
                new FundingProgramme(
                    "duna-network",
                    "Duna Orbital Network",
                    "Duna",
                    5,
                    100000.0,
                    true,
                    null,
                    (UnlockRuleDefinition)null)
            };

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, aster, cobalt, delta },
                1234.0,
                new List<AchievementFundingProgramme>(),
                fundingProgrammes);

            TestAssert.Equal(1, delta.GetSatelliteCount("Duna"));
            TestAssert.Equal(0, delta.LaunchProgressPercent);
            TestAssert.Equal("duna-network", delta.NextMissionTargetId);
        }

        public static void CollectionCostUsesProgrammeBody()
        {
            var aster = new SpaceProgramState("Aster", false)
            {
                NextMissionTargetId = "duna-network",
                NextLaunchBodyName = "Stored presentation text"
            };
            var presentationOnlyTarget = new SpaceProgramState("PresentationOnly", false)
            {
                NextLaunchBodyName = "Duna Orbital Network"
            };
            var fundingProgrammes = new List<FundingProgramme>
            {
                new FundingProgramme(
                    "duna-network",
                    "Duna Orbital Network",
                    "Duna",
                    5,
                    100000.0,
                    true,
                    null,
                    (UnlockRuleDefinition)null)
            };
            var achievementProgrammes = new List<AchievementFundingProgramme>();

            double cost = RivalSimulation.CalculateLaunchProgressCost(
                aster,
                achievementProgrammes,
                fundingProgrammes);
            double presentationOnlyCost = RivalSimulation.CalculateLaunchProgressCost(
                presentationOnlyTarget,
                achievementProgrammes,
                fundingProgrammes);

            TestAssert.Equal(40000.0, cost);
            TestAssert.Equal(20000.0, presentationOnlyCost);
            TestAssert.Equal("duna-network", aster.NextMissionTargetId);
            TestAssert.Equal("Stored presentation text", aster.NextLaunchBodyName);
            TestAssert.Equal(null, presentationOnlyTarget.NextMissionTargetId);
            TestAssert.Equal("Duna Orbital Network", presentationOnlyTarget.NextLaunchBodyName);
        }

        public static void AchievementCompletionUsesMilestoneDefinition()
        {
            const double completionUniversalTime = 4321.0;
            var player = new SpaceProgramState("Player", true);
            player.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 1.0);
            var aster = new SpaceProgramState("Aster", false)
            {
                NextMissionTargetId = PrototypeMilestones.DunaProbeOrbitId,
                LaunchProgressPercent = 100
            };
            var cobalt = new SpaceProgramState("Cobalt", false);

            MilestoneDefinition milestone = PrototypeMilestones.FindById(
                PrototypeMilestones.DunaProbeOrbitId);
            var achievementProgrammes = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme(
                    milestone.Id,
                    milestone.Name,
                    milestone.ObjectiveDescription,
                    200000.0,
                    "Display text",
                    milestone.UnlockRule)
            };

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, aster, cobalt },
                completionUniversalTime,
                achievementProgrammes,
                new List<FundingProgramme>());

            TestAssert.True(
                aster.HasAchievement(PrototypeMilestones.DunaProbeOrbitId),
                "Duna achievement completion should record the milestone ID.");
            TestAssert.Equal(
                completionUniversalTime,
                aster.GetAchievementUniversalTime(PrototypeMilestones.DunaProbeOrbitId));
            TestAssert.Equal(1, aster.GetSatelliteCount("Duna"));
            TestAssert.Equal(null, aster.NextMissionTargetId);
        }
    }

    internal static class TestAssert
    {
        public static void True(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.InvalidOperationException(message);
            }
        }

        public static void Equal<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new System.InvalidOperationException(
                    "Expected '" + expected + "' but got '" + actual + "'.");
            }
        }
    }
}

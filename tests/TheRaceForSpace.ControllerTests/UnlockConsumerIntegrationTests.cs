using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;
using TheRaceForSpace.Simulation;

namespace TheRaceForSpace.ControllerTests
{
    internal static class UnlockConsumerIntegrationTests
    {
        public static void RivalSelectionUsesScopeCountAndHistoricalTime()
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
            RequireEqual(
                null,
                cobalt.NextMissionTargetId,
                "A future second rival achievement must not satisfy the rule early.");

            RivalSimulation.Refresh(
                programs,
                200.0,
                achievementProgrammes,
                new List<FundingProgramme>());
            RequireEqual(
                PrototypeMilestones.MinmusCrewedOrbitId,
                cobalt.NextMissionTargetId,
                "The rival target should unlock exactly when two rival agencies qualify.");
        }

        private static void RequireEqual<T>(T expected, T actual, string message)
        {
            if (!object.Equals(expected, actual))
            {
                throw new System.InvalidOperationException(
                    message + " Expected '" + expected + "', got '" + actual + "'.");
            }
        }
    }
}

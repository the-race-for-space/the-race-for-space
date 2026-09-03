using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;
using TheRaceForSpace.Simulation;

namespace TheRaceForSpace.ControllerTests
{
    internal static class UnlockConsumerIntegrationTests
    {
        public static void RivalSelectionRequiresOfferedContract()
        {
            var player = new SpaceProgramState("player", "Player", true);
            var aster = new SpaceProgramState("aster", "Aster", false);
            var cobalt = new SpaceProgramState("cobalt", "Cobalt", false);
            var delta = new SpaceProgramState("delta", "Delta", false);
            aster.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 100.0);
            delta.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 200.0);

            MilestoneDefinition milestone = PrototypeMilestones.FindById(
                PrototypeMilestones.MinmusCrewedOrbitId);
            var programme = new AchievementFundingProgramme(
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
                            2))));
            var achievementProgrammes = new List<AchievementFundingProgramme> { programme };
            var programs = new List<SpaceProgramState> { player, aster, cobalt, delta };

            // The unlock rule is satisfied at this time, but sponsors have not selected the
            // contract yet. Rivals must therefore leave it alone.
            RivalSimulation.Refresh(
                programs,
                200.0,
                achievementProgrammes,
                new List<FundingProgramme>());
            RequireEqual(
                null,
                cobalt.NextMissionTargetId,
                "An unlocked but unoffered target must not be selected by a rival.");

            programme.Offer();
            RivalSimulation.Refresh(
                programs,
                200.0,
                achievementProgrammes,
                new List<FundingProgramme>());
            RequireEqual(
                PrototypeMilestones.MinmusCrewedOrbitId,
                cobalt.NextMissionTargetId,
                "The same target should become selectable once sponsors mark it Offered.");
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

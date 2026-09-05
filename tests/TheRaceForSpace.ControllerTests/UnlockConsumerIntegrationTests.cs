using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Rivals;

namespace TheRaceForSpace.ControllerTests
{
    internal static class UnlockConsumerIntegrationTests
    {
        public static void RivalSelectionRequiresOfferedContract()
        {
            var player = new AgencyState("player", "Player", true);
            var aster = new AgencyState("aster", "Aster", false);
            var cobalt = new AgencyState("cobalt", "Cobalt", false);
            var delta = new AgencyState("delta", "Delta", false);
            aster.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 100.0);
            delta.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 200.0);

            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(
                ObjectiveCatalogue.MinmusCrewedOrbitId);
            var programme = new ObjectiveFundingContract(
                objective.Id,
                objective.Name,
                objective.ObjectiveDescription,
                300000.0,
                "Display text",
                new UnlockRuleDefinition(
                    new UnlockPathDefinition(
                        UnlockConditionDefinition.ObjectiveCompletion(
                            ObjectiveCatalogue.ProbeOrbitId,
                            UnlockAgencyScope.AnyRival,
                            2))));
            var achievementProgrammes = new List<ObjectiveFundingContract> { programme };
            var agencies = new List<AgencyState> { player, aster, cobalt, delta };

            // The unlock rule is satisfied at this time, but sponsors have not selected the
            // contract yet. Rivals must therefore leave it alone.
            RivalSimulation.Refresh(
                agencies,
                200.0,
                achievementProgrammes,
                new List<SatelliteNetworkFundingContract>());
            RequireEqual(
                null,
                cobalt.NextMissionTargetId,
                "An unlocked but unoffered target must not be selected by a rival.");

            programme.Offer();
            RivalSimulation.Refresh(
                agencies,
                200.0,
                achievementProgrammes,
                new List<SatelliteNetworkFundingContract>());
            RequireEqual(
                ObjectiveCatalogue.MinmusCrewedOrbitId,
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

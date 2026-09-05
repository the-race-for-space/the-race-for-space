using System;
using TheRaceForSpace.Objectives;

namespace TheRaceForSpace.Tests.Objectives
{
    internal static class MilestoneEvaluationTests
    {
        public static void ProbeObservationMatchesDefinition()
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(ObjectiveCatalogue.ProbeOrbitId);
            var observation = new OrbitalObjectiveObservation(
                "kerbin",
                ObjectiveSituation.Orbit,
                ObjectiveCrewRequirement.UncrewedProbe);

            Require(objective.IsSatisfiedBy(observation), "Kerbin Probe Orbit should accept an uncrewed probe observation.");
        }

        public static void CrewedObservationMatchesDefinition()
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(ObjectiveCatalogue.CrewedOrbitId);
            var observation = new OrbitalObjectiveObservation(
                "Kerbin",
                ObjectiveSituation.Orbit,
                ObjectiveCrewRequirement.Crewed);

            Require(objective.IsSatisfiedBy(observation), "Kerbin Crewed Orbit should accept a crewed observation.");
            Require(
                !ObjectiveCatalogue.FindById(ObjectiveCatalogue.ProbeOrbitId).IsSatisfiedBy(observation),
                "A crewed observation should not also satisfy the uncrewed probe objective.");
        }

        public static void WrongBodyOrSituationDoesNotMatch()
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(ObjectiveCatalogue.MunProbeOrbitId);
            var wrongBody = new OrbitalObjectiveObservation(
                "Minmus",
                ObjectiveSituation.Orbit,
                ObjectiveCrewRequirement.UncrewedProbe);
            var wrongSituation = new OrbitalObjectiveObservation(
                "Mun",
                (ObjectiveSituation)99,
                ObjectiveCrewRequirement.UncrewedProbe);

            Require(!objective.IsSatisfiedBy(wrongBody), "A objective should reject a vessel around the wrong body.");
            Require(!objective.IsSatisfiedBy(wrongSituation), "A objective should reject the wrong vessel situation.");
        }

        public static void ArbitraryBodyDefinitionUsesSameRule()
        {
            var objective = new ObjectiveDefinition(
                "future-eve-probe-orbit",
                "Eve Probe Orbit",
                "Eve",
                ObjectiveSituation.Orbit,
                ObjectiveCrewRequirement.UncrewedProbe,
                "Orbit Eve with an uncrewed probe.",
                UnlockRuleDefinition.AnyAgencyObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId));
            var observation = new OrbitalObjectiveObservation(
                "Eve",
                ObjectiveSituation.Orbit,
                ObjectiveCrewRequirement.UncrewedProbe);
            var unqualifiedObservation = new OrbitalObjectiveObservation(
                "Eve",
                ObjectiveSituation.Orbit,
                null);

            Require(objective.IsSatisfiedBy(observation), "A future body should use the same objective matching rule.");
            Require(!objective.IsSatisfiedBy(unqualifiedObservation), "An unqualified uncrewed vessel should not satisfy a probe objective.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

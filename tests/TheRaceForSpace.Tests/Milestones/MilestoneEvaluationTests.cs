using System;
using TheRaceForSpace.Milestones;

namespace TheRaceForSpace.Tests.Milestones
{
    internal static class MilestoneEvaluationTests
    {
        public static void ProbeObservationMatchesDefinition()
        {
            MilestoneDefinition milestone = PrototypeMilestones.FindById(PrototypeMilestones.ProbeOrbitId);
            var observation = new MilestoneVesselObservation(
                "kerbin",
                MilestoneSituation.Orbit,
                MilestoneCrewRequirement.UncrewedProbe);

            Require(milestone.IsSatisfiedBy(observation), "Kerbin Probe Orbit should accept an uncrewed probe observation.");
        }

        public static void CrewedObservationMatchesDefinition()
        {
            MilestoneDefinition milestone = PrototypeMilestones.FindById(PrototypeMilestones.CrewedOrbitId);
            var observation = new MilestoneVesselObservation(
                "Kerbin",
                MilestoneSituation.Orbit,
                MilestoneCrewRequirement.Crewed);

            Require(milestone.IsSatisfiedBy(observation), "Kerbin Crewed Orbit should accept a crewed observation.");
            Require(
                !PrototypeMilestones.FindById(PrototypeMilestones.ProbeOrbitId).IsSatisfiedBy(observation),
                "A crewed observation should not also satisfy the uncrewed probe milestone.");
        }

        public static void WrongBodyOrSituationDoesNotMatch()
        {
            MilestoneDefinition milestone = PrototypeMilestones.FindById(PrototypeMilestones.MunProbeOrbitId);
            var wrongBody = new MilestoneVesselObservation(
                "Minmus",
                MilestoneSituation.Orbit,
                MilestoneCrewRequirement.UncrewedProbe);
            var wrongSituation = new MilestoneVesselObservation(
                "Mun",
                (MilestoneSituation)99,
                MilestoneCrewRequirement.UncrewedProbe);

            Require(!milestone.IsSatisfiedBy(wrongBody), "A milestone should reject a vessel around the wrong body.");
            Require(!milestone.IsSatisfiedBy(wrongSituation), "A milestone should reject the wrong vessel situation.");
        }

        public static void ArbitraryBodyDefinitionUsesSameRule()
        {
            var milestone = new MilestoneDefinition(
                "future-duna-probe-orbit",
                "Duna Probe Orbit",
                "Duna",
                MilestoneSituation.Orbit,
                MilestoneCrewRequirement.UncrewedProbe,
                "Orbit Duna with an uncrewed probe.",
                PrototypeMilestones.MunProbeOrbitId);
            var observation = new MilestoneVesselObservation(
                "Duna",
                MilestoneSituation.Orbit,
                MilestoneCrewRequirement.UncrewedProbe);
            var unqualifiedObservation = new MilestoneVesselObservation(
                "Duna",
                MilestoneSituation.Orbit,
                null);

            Require(milestone.IsSatisfiedBy(observation), "A future body should use the same milestone matching rule.");
            Require(!milestone.IsSatisfiedBy(unqualifiedObservation), "An unqualified uncrewed vessel should not satisfy a probe milestone.");
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

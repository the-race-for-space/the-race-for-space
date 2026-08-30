using System.Collections.Generic;

namespace TheRaceForSpace.Milestones
{
    /// <summary>
    /// Defines the six achievement milestones used by the 0.3 prototype.
    /// These definitions are additive metadata for now; gameplay migration occurs in later passes.
    /// </summary>
    public static class PrototypeMilestones
    {
        public const string ProbeOrbitId = "probe-orbit";
        public const string CrewedOrbitId = "crewed-orbit";
        public const string MunProbeOrbitId = "mun-probe-orbit";
        public const string MinmusProbeOrbitId = "minmus-probe-orbit";
        public const string MunCrewedOrbitId = "mun-crewed-orbit";
        public const string MinmusCrewedOrbitId = "minmus-crewed-orbit";

        private static readonly IList<MilestoneDefinition> Definitions =
            new List<MilestoneDefinition>
            {
                new MilestoneDefinition(
                    ProbeOrbitId,
                    "Probe Orbit",
                    "Kerbin",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Kerbin with an uncrewed Probe or Relay vessel.",
                    null),
                new MilestoneDefinition(
                    CrewedOrbitId,
                    "Crewed Orbit",
                    "Kerbin",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Kerbin with at least one live Kerbal aboard.",
                    null),
                new MilestoneDefinition(
                    MunProbeOrbitId,
                    "Mun Probe Orbit",
                    "Mun",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Mun with an uncrewed Probe or Relay vessel.",
                    ProbeOrbitId),
                new MilestoneDefinition(
                    MinmusProbeOrbitId,
                    "Minmus Probe Orbit",
                    "Minmus",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Minmus with an uncrewed Probe or Relay vessel.",
                    ProbeOrbitId),
                new MilestoneDefinition(
                    MunCrewedOrbitId,
                    "Mun Crewed Orbit",
                    "Mun",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Mun with at least one live Kerbal aboard.",
                    CrewedOrbitId),
                new MilestoneDefinition(
                    MinmusCrewedOrbitId,
                    "Minmus Crewed Orbit",
                    "Minmus",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Minmus with at least one live Kerbal aboard.",
                    CrewedOrbitId)
            }.AsReadOnly();

        public static IList<MilestoneDefinition> All
        {
            get { return Definitions; }
        }
    }
}

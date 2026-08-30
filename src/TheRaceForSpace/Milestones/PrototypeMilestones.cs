using System;
using System.Collections.Generic;

namespace TheRaceForSpace.Milestones
{
    /// <summary>
    /// Defines the achievement milestones used by the current prototype.
    /// </summary>
    public static class PrototypeMilestones
    {
        public const string ProbeOrbitId = "probe-orbit";
        public const string CrewedOrbitId = "crewed-orbit";
        public const string MunProbeOrbitId = "mun-probe-orbit";
        public const string MinmusProbeOrbitId = "minmus-probe-orbit";
        public const string DunaProbeOrbitId = "duna-probe-orbit";
        public const string MunCrewedOrbitId = "mun-crewed-orbit";
        public const string MinmusCrewedOrbitId = "minmus-crewed-orbit";
        public const string DunaCrewedOrbitId = "duna-crewed-orbit";

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
                    DunaProbeOrbitId,
                    "Duna Probe Orbit",
                    "Duna",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.UncrewedProbe,
                    "Achieve orbit around Duna with an uncrewed Probe or Relay vessel.",
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
                    CrewedOrbitId),
                new MilestoneDefinition(
                    DunaCrewedOrbitId,
                    "Duna Crewed Orbit",
                    "Duna",
                    MilestoneSituation.Orbit,
                    MilestoneCrewRequirement.Crewed,
                    "Achieve orbit around Duna with at least one live Kerbal aboard.",
                    CrewedOrbitId)
            }.AsReadOnly();

        public static IList<MilestoneDefinition> All
        {
            get { return Definitions; }
        }

        /// <summary>
        /// Returns the prototype milestone with the supplied stable ID, or null when no definition exists.
        /// </summary>
        public static MilestoneDefinition FindById(string milestoneId)
        {
            if (string.IsNullOrEmpty(milestoneId))
            {
                return null;
            }

            for (int milestoneIndex = 0; milestoneIndex < Definitions.Count; milestoneIndex++)
            {
                MilestoneDefinition milestone = Definitions[milestoneIndex];
                if (string.Equals(milestone.Id, milestoneId, StringComparison.OrdinalIgnoreCase))
                {
                    return milestone;
                }
            }

            return null;
        }
    }
}

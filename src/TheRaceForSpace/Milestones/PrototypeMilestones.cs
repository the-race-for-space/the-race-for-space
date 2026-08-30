using System;
using System.Collections.Generic;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Milestones
{
    /// <summary>
    /// Defines the six achievement milestones used by the 0.3 prototype.
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

        /// <summary>
        /// Imports the current six prototype achievement fields into generic milestone state.
        /// Rival simulation and persistence still write those fields during the staged migration;
        /// this bridge can be removed once those subsystems write milestone IDs directly.
        /// </summary>
        public static void SynchronizeLegacyAchievementState(SpaceProgramState program)
        {
            if (program == null)
            {
                return;
            }

            if (program.HasAchievedProbeOrbit)
            {
                program.RecordAchievement(ProbeOrbitId, program.ProbeOrbitAchievementUniversalTime);
            }

            if (program.HasAchievedCrewedOrbit)
            {
                program.RecordAchievement(CrewedOrbitId, program.CrewedOrbitAchievementUniversalTime);
            }

            if (program.HasAchievedMunProbeOrbit)
            {
                program.RecordAchievement(MunProbeOrbitId, program.MunProbeOrbitAchievementUniversalTime);
            }

            if (program.HasAchievedMinmusProbeOrbit)
            {
                program.RecordAchievement(MinmusProbeOrbitId, program.MinmusProbeOrbitAchievementUniversalTime);
            }

            if (program.HasAchievedMunCrewedOrbit)
            {
                program.RecordAchievement(MunCrewedOrbitId, program.MunCrewedOrbitAchievementUniversalTime);
            }

            if (program.HasAchievedMinmusCrewedOrbit)
            {
                program.RecordAchievement(MinmusCrewedOrbitId, program.MinmusCrewedOrbitAchievementUniversalTime);
            }
        }

        /// <summary>
        /// Mirrors generic milestone state into the six temporary prototype fields still consumed
        /// by rival simulation and persistence. Generic milestone state remains the source of truth.
        /// </summary>
        public static void SynchronizeGenericAchievementStateToLegacy(SpaceProgramState program)
        {
            if (program == null)
            {
                return;
            }

            if (program.HasAchievement(ProbeOrbitId))
            {
                program.HasAchievedProbeOrbit = true;
                program.ProbeOrbitAchievementUniversalTime = program.GetAchievementUniversalTime(ProbeOrbitId);
            }

            if (program.HasAchievement(CrewedOrbitId))
            {
                program.HasAchievedCrewedOrbit = true;
                program.CrewedOrbitAchievementUniversalTime = program.GetAchievementUniversalTime(CrewedOrbitId);
            }

            if (program.HasAchievement(MunProbeOrbitId))
            {
                program.HasAchievedMunProbeOrbit = true;
                program.MunProbeOrbitAchievementUniversalTime = program.GetAchievementUniversalTime(MunProbeOrbitId);
            }

            if (program.HasAchievement(MinmusProbeOrbitId))
            {
                program.HasAchievedMinmusProbeOrbit = true;
                program.MinmusProbeOrbitAchievementUniversalTime = program.GetAchievementUniversalTime(MinmusProbeOrbitId);
            }

            if (program.HasAchievement(MunCrewedOrbitId))
            {
                program.HasAchievedMunCrewedOrbit = true;
                program.MunCrewedOrbitAchievementUniversalTime = program.GetAchievementUniversalTime(MunCrewedOrbitId);
            }

            if (program.HasAchievement(MinmusCrewedOrbitId))
            {
                program.HasAchievedMinmusCrewedOrbit = true;
                program.MinmusCrewedOrbitAchievementUniversalTime = program.GetAchievementUniversalTime(MinmusCrewedOrbitId);
            }
        }
    }
}

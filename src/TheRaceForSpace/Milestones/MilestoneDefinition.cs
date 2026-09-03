using System;

namespace TheRaceForSpace.Milestones
{
    /// <summary>
    /// Crew qualification required by one milestone objective.
    /// </summary>
    public enum MilestoneCrewRequirement
    {
        UncrewedProbe,
        Crewed
    }

    /// <summary>
    /// Vessel situation required by the orbital milestone objectives.
    /// Starter contracts are evaluated from flight-attempt state rather than this value.
    /// </summary>
    public enum MilestoneSituation
    {
        Orbit
    }

    /// <summary>
    /// Broad objective family used to distinguish orbital achievements from the four starter lines.
    /// </summary>
    public enum MilestoneObjectiveType
    {
        Orbit,
        DirectedPower,
        DeliveredMass,
        AltitudeHold,
        BiomeVisit
    }

    /// <summary>
    /// Special pre-orbit contract line. None identifies the normal space-race milestone catalogue.
    /// </summary>
    public enum StarterContractLine
    {
        None,
        DirectedPower,
        Mass,
        Control,
        Biome
    }

    /// <summary>
    /// KSP-independent facts about one observed vessel that are relevant to orbital milestone evaluation.
    /// A null crew qualification means the vessel does not fit a milestone crew category.
    /// </summary>
    public sealed class MilestoneVesselObservation
    {
        public MilestoneVesselObservation(
            string celestialBodyName,
            MilestoneSituation situation,
            MilestoneCrewRequirement? crewQualification)
        {
            CelestialBodyName = celestialBodyName;
            Situation = situation;
            CrewQualification = crewQualification;
        }

        public string CelestialBodyName { get; private set; }
        public MilestoneSituation Situation { get; private set; }
        public MilestoneCrewRequirement? CrewQualification { get; private set; }
    }

    /// <summary>
    /// Immutable definition of one race milestone. Gameplay state remains owned by space programs;
    /// this type describes the objective, starter-contract balance metadata, and campaign unlock rule.
    /// </summary>
    public sealed class MilestoneDefinition
    {
        public MilestoneDefinition(
            string id,
            string name,
            string celestialBodyName,
            MilestoneSituation situation,
            MilestoneCrewRequirement crewRequirement,
            string objectiveDescription,
            UnlockRuleDefinition unlockRule)
            : this(
                id,
                name,
                celestialBodyName,
                situation,
                crewRequirement,
                objectiveDescription,
                unlockRule,
                MilestoneObjectiveType.Orbit,
                StarterContractLine.None,
                0,
                0.0,
                0.0)
        {
        }

        public MilestoneDefinition(
            string id,
            string name,
            string celestialBodyName,
            MilestoneSituation situation,
            MilestoneCrewRequirement crewRequirement,
            string objectiveDescription,
            UnlockRuleDefinition unlockRule,
            MilestoneObjectiveType objectiveType,
            StarterContractLine starterLine,
            int starterLevel,
            double baseRewardFunds,
            double rivalProgressCostFunds)
        {
            Id = id;
            Name = name;
            CelestialBodyName = celestialBodyName;
            Situation = situation;
            CrewRequirement = crewRequirement;
            ObjectiveDescription = objectiveDescription;
            UnlockRule = unlockRule;
            ObjectiveType = objectiveType;
            StarterLine = starterLine;
            StarterLevel = Math.Max(0, starterLevel);
            BaseRewardFunds = Math.Max(0.0, baseRewardFunds);
            RivalProgressCostFunds = Math.Max(0.0, rivalProgressCostFunds);
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string CelestialBodyName { get; private set; }
        public MilestoneSituation Situation { get; private set; }
        public MilestoneCrewRequirement CrewRequirement { get; private set; }
        public string ObjectiveDescription { get; private set; }
        public UnlockRuleDefinition UnlockRule { get; private set; }
        public MilestoneObjectiveType ObjectiveType { get; private set; }
        public StarterContractLine StarterLine { get; private set; }
        public int StarterLevel { get; private set; }
        public double BaseRewardFunds { get; private set; }
        public double RivalProgressCostFunds { get; private set; }

        public bool IsStarterContract
        {
            get { return StarterLine != StarterContractLine.None && StarterLevel > 0; }
        }

        /// <summary>
        /// Returns whether one KSP-independent vessel observation satisfies this milestone.
        /// Starter milestones are evaluated by the flight-attempt tracker added in the gameplay batch.
        /// </summary>
        public bool IsSatisfiedBy(MilestoneVesselObservation observation)
        {
            if (ObjectiveType != MilestoneObjectiveType.Orbit
                || observation == null
                || string.IsNullOrEmpty(observation.CelestialBodyName)
                || !observation.CrewQualification.HasValue)
            {
                return false;
            }

            return string.Equals(
                    CelestialBodyName,
                    observation.CelestialBodyName,
                    StringComparison.OrdinalIgnoreCase)
                && Situation == observation.Situation
                && CrewRequirement == observation.CrewQualification.Value;
        }
    }
}

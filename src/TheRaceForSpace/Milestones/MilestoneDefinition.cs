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
    /// Vessel situation required by one milestone objective.
    /// Orbit is the only situation used by the 0.3 prototype; later milestone types can extend this enum.
    /// </summary>
    public enum MilestoneSituation
    {
        Orbit
    }

    /// <summary>
    /// Immutable definition of one race milestone. Gameplay state remains owned by space programs;
    /// this type describes what an achievement is and which earlier milestone unlocks it.
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
            string prerequisiteMilestoneId)
        {
            Id = id;
            Name = name;
            CelestialBodyName = celestialBodyName;
            Situation = situation;
            CrewRequirement = crewRequirement;
            ObjectiveDescription = objectiveDescription;
            PrerequisiteMilestoneId = prerequisiteMilestoneId;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string CelestialBodyName { get; private set; }
        public MilestoneSituation Situation { get; private set; }
        public MilestoneCrewRequirement CrewRequirement { get; private set; }
        public string ObjectiveDescription { get; private set; }
        public string PrerequisiteMilestoneId { get; private set; }
    }
}

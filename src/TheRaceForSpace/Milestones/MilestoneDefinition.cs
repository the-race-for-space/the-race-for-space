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
            UnlockRule = unlockRule;
            ObjectiveType = objectiveType;
            StarterLine = starterLine;
            StarterLevel = Math.Max(0, starterLevel);
            BaseRewardFunds = Math.Max(0.0, baseRewardFunds);
            RivalProgressCostFunds = Math.Max(0.0, rivalProgressCostFunds);

            RequiredSpeedMetersPerSecond = GetRequiredSpeedMetersPerSecond(starterLine, StarterLevel);
            RequiredMassTonnes = GetRequiredMassTonnes(starterLine, StarterLevel);
            RequiredDistanceMeters = GetRequiredDistanceMeters(starterLine, StarterLevel);
            MinimumAltitudeMeters = GetMinimumAltitudeMeters(starterLine, StarterLevel);
            MaximumAltitudeMeters = GetMaximumAltitudeMeters(starterLine, StarterLevel);
            RequiredDurationSeconds = GetRequiredDurationSeconds(starterLine, StarterLevel);
            RequiredBiomeName = GetRequiredBiomeName(starterLine, StarterLevel);

            // The measurable starter criteria are authoritative. Generate wording for objectives
            // whose completion rule changed from in-flight observation to a final landed state so
            // Space Race and Funding Targets cannot drift away from the tracker implementation.
            ObjectiveDescription = CreateObjectiveDescription(objectiveDescription);
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

        // The twenty starter contracts are deliberately fixed prototype content. Keeping their
        // measurable criteria on the definition lets Tracking evaluate them without parsing text
        // or knowing stable milestone IDs.
        public double RequiredSpeedMetersPerSecond { get; private set; }
        public double RequiredMassTonnes { get; private set; }
        public double RequiredDistanceMeters { get; private set; }
        public double MinimumAltitudeMeters { get; private set; }
        public double MaximumAltitudeMeters { get; private set; }
        public double RequiredDurationSeconds { get; private set; }
        public string RequiredBiomeName { get; private set; }

        public bool IsStarterContract
        {
            get { return StarterLine != StarterContractLine.None && StarterLevel > 0; }
        }

        /// <summary>
        /// Returns whether one KSP-independent vessel observation satisfies this milestone.
        /// Starter milestones are evaluated by the flight-attempt tracker.
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

        private string CreateObjectiveDescription(string configuredDescription)
        {
            if (ObjectiveType == MilestoneObjectiveType.DeliveredMass
                && RequiredMassTonnes > 0.0
                && RequiredDistanceMeters > 0.0)
            {
                return "Land on Kerbin at least "
                    + (RequiredDistanceMeters / 1000.0).ToString("0.#")
                    + " km from the launch point with at least "
                    + RequiredMassTonnes.ToString("0.#")
                    + " t of remaining vessel mass.";
            }

            if (ObjectiveType == MilestoneObjectiveType.BiomeVisit
                && !string.IsNullOrEmpty(RequiredBiomeName))
            {
                return "Land in Kerbin's "
                    + RequiredBiomeName
                    + " biome without entering orbit.";
            }

            return configuredDescription;
        }

        private static double GetRequiredSpeedMetersPerSecond(
            StarterContractLine starterLine,
            int starterLevel)
        {
            if (starterLine != StarterContractLine.DirectedPower)
            {
                return 0.0;
            }

            switch (starterLevel)
            {
                case 1: return 600.0;
                case 2: return 1100.0;
                case 3: return 1400.0;
                case 4: return 1700.0;
                case 5: return 2000.0;
                default: return 0.0;
            }
        }

        private static double GetRequiredMassTonnes(
            StarterContractLine starterLine,
            int starterLevel)
        {
            if (starterLine != StarterContractLine.Mass)
            {
                return 0.0;
            }

            switch (starterLevel)
            {
                case 1: return 1.0;
                case 2: return 2.5;
                case 3: return 5.0;
                case 4: return 10.0;
                case 5: return 20.0;
                default: return 0.0;
            }
        }

        private static double GetRequiredDistanceMeters(
            StarterContractLine starterLine,
            int starterLevel)
        {
            if (starterLine != StarterContractLine.Mass)
            {
                return 0.0;
            }

            switch (starterLevel)
            {
                case 1: return 25000.0;
                case 2: return 75000.0;
                case 3: return 150000.0;
                case 4: return 300000.0;
                case 5: return 600000.0;
                default: return 0.0;
            }
        }

        private static double GetMinimumAltitudeMeters(
            StarterContractLine starterLine,
            int starterLevel)
        {
            if (starterLine != StarterContractLine.Control)
            {
                return 0.0;
            }

            switch (starterLevel)
            {
                case 1: return 2000.0;
                case 2: return 8000.0;
                case 3: return 15000.0;
                case 4: return 30000.0;
                case 5: return 50000.0;
                default: return 0.0;
            }
        }

        private static double GetMaximumAltitudeMeters(
            StarterContractLine starterLine,
            int starterLevel)
        {
            if (starterLine == StarterContractLine.DirectedPower)
            {
                return starterLevel >= 1 && starterLevel <= 5 ? 70000.0 : 0.0;
            }

            if (starterLine != StarterContractLine.Control)
            {
                return 0.0;
            }

            switch (starterLevel)
            {
                case 1: return 5000.0;
                case 2: return 12000.0;
                case 3: return 25000.0;
                case 4: return 40000.0;
                case 5: return 65000.0;
                default: return 0.0;
            }
        }

        private static double GetRequiredDurationSeconds(
            StarterContractLine starterLine,
            int starterLevel)
        {
            if (starterLine != StarterContractLine.Control)
            {
                return 0.0;
            }

            switch (starterLevel)
            {
                case 1: return 30.0;
                case 2: return 45.0;
                case 3: return 60.0;
                case 4: return 75.0;
                case 5: return 90.0;
                default: return 0.0;
            }
        }

        private static string GetRequiredBiomeName(
            StarterContractLine starterLine,
            int starterLevel)
        {
            if (starterLine != StarterContractLine.Biome)
            {
                return null;
            }

            switch (starterLevel)
            {
                case 1: return "Grasslands";
                case 2: return "Highlands";
                case 3: return "Mountains";
                case 4: return "Deserts";
                case 5: return "Ice Caps";
                default: return null;
            }
        }
    }
}

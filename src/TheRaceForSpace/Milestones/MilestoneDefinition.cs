using System;
using System.Globalization;

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
    /// Immutable measurable criteria supplied by each starter milestone definition.
    /// Keeping these values as catalogue data prevents line/level metadata from secretly defining balance.
    /// </summary>
    internal sealed class StarterContractCriteria
    {
        public static readonly StarterContractCriteria None = new StarterContractCriteria(
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            null);

        private StarterContractCriteria(
            double requiredSpeedMetersPerSecond,
            double requiredMassTonnes,
            double requiredDistanceMeters,
            double minimumAltitudeMeters,
            double maximumAltitudeMeters,
            double requiredDurationSeconds,
            string requiredBiomeName)
        {
            RequiredSpeedMetersPerSecond = Math.Max(0.0, requiredSpeedMetersPerSecond);
            RequiredMassTonnes = Math.Max(0.0, requiredMassTonnes);
            RequiredDistanceMeters = Math.Max(0.0, requiredDistanceMeters);
            MinimumAltitudeMeters = Math.Max(0.0, minimumAltitudeMeters);
            MaximumAltitudeMeters = Math.Max(0.0, maximumAltitudeMeters);
            RequiredDurationSeconds = Math.Max(0.0, requiredDurationSeconds);
            RequiredBiomeName = requiredBiomeName;
        }

        public double RequiredSpeedMetersPerSecond { get; private set; }
        public double RequiredMassTonnes { get; private set; }
        public double RequiredDistanceMeters { get; private set; }
        public double MinimumAltitudeMeters { get; private set; }
        public double MaximumAltitudeMeters { get; private set; }
        public double RequiredDurationSeconds { get; private set; }
        public string RequiredBiomeName { get; private set; }

        public static StarterContractCriteria DirectedPower(
            double requiredSpeedMetersPerSecond,
            double maximumAltitudeMeters)
        {
            return new StarterContractCriteria(
                requiredSpeedMetersPerSecond,
                0.0,
                0.0,
                0.0,
                maximumAltitudeMeters,
                0.0,
                null);
        }

        public static StarterContractCriteria Mass(
            double requiredMassTonnes,
            double requiredDistanceMeters)
        {
            return new StarterContractCriteria(
                0.0,
                requiredMassTonnes,
                requiredDistanceMeters,
                0.0,
                0.0,
                0.0,
                null);
        }

        public static StarterContractCriteria Control(
            double minimumAltitudeMeters,
            double maximumAltitudeMeters,
            double requiredDurationSeconds)
        {
            return new StarterContractCriteria(
                0.0,
                0.0,
                0.0,
                minimumAltitudeMeters,
                maximumAltitudeMeters,
                requiredDurationSeconds,
                null);
        }

        public static StarterContractCriteria Biome(string requiredBiomeName)
        {
            return new StarterContractCriteria(
                0.0,
                0.0,
                0.0,
                0.0,
                0.0,
                0.0,
                requiredBiomeName);
        }
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
            : this(
                id,
                name,
                celestialBodyName,
                situation,
                crewRequirement,
                objectiveDescription,
                unlockRule,
                objectiveType,
                starterLine,
                starterLevel,
                baseRewardFunds,
                rivalProgressCostFunds,
                StarterContractCriteria.None)
        {
        }

        internal MilestoneDefinition(
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
            double rivalProgressCostFunds,
            StarterContractCriteria starterCriteria)
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

            StarterContractCriteria criteria = starterCriteria ?? StarterContractCriteria.None;
            RequiredSpeedMetersPerSecond = criteria.RequiredSpeedMetersPerSecond;
            RequiredMassTonnes = criteria.RequiredMassTonnes;
            RequiredDistanceMeters = criteria.RequiredDistanceMeters;
            MinimumAltitudeMeters = criteria.MinimumAltitudeMeters;
            MaximumAltitudeMeters = criteria.MaximumAltitudeMeters;
            RequiredDurationSeconds = criteria.RequiredDurationSeconds;
            RequiredBiomeName = criteria.RequiredBiomeName;

            // Starter criteria are the single source of truth for both evaluation and player-facing
            // wording. This prevents catalogue text from drifting away from the tracker thresholds.
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

        // Tracking and UI consume these values directly. PrototypeMilestones supplies them explicitly
        // for each starter definition instead of MilestoneDefinition inferring balance from line/level.
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
            if (ObjectiveType == MilestoneObjectiveType.DirectedPower
                && RequiredSpeedMetersPerSecond > 0.0
                && MaximumAltitudeMeters > 0.0)
            {
                return "Reach "
                    + RequiredSpeedMetersPerSecond.ToString("#,0", CultureInfo.InvariantCulture)
                    + " m/s without exceeding "
                    + (MaximumAltitudeMeters / 1000.0).ToString("0.#", CultureInfo.InvariantCulture)
                    + " km altitude, then impact Kerbin.";
            }

            if (ObjectiveType == MilestoneObjectiveType.DeliveredMass
                && RequiredMassTonnes > 0.0
                && RequiredDistanceMeters > 0.0)
            {
                return "Land on Kerbin at least "
                    + (RequiredDistanceMeters / 1000.0).ToString("0.#", CultureInfo.InvariantCulture)
                    + " km from the launch point with at least "
                    + RequiredMassTonnes.ToString("0.#", CultureInfo.InvariantCulture)
                    + " t of remaining vessel mass.";
            }

            if (ObjectiveType == MilestoneObjectiveType.AltitudeHold
                && MaximumAltitudeMeters > MinimumAltitudeMeters
                && RequiredDurationSeconds > 0.0)
            {
                return "With crew aboard, remain between "
                    + (MinimumAltitudeMeters / 1000.0).ToString("0.#", CultureInfo.InvariantCulture)
                    + "-"
                    + (MaximumAltitudeMeters / 1000.0).ToString("0.#", CultureInfo.InvariantCulture)
                    + " km for "
                    + RequiredDurationSeconds.ToString("0.#", CultureInfo.InvariantCulture)
                    + " seconds, then land safely on Kerbin.";
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
    }
}

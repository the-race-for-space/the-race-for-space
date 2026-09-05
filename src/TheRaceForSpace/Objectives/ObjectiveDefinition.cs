using System;
using System.Globalization;

namespace TheRaceForSpace.Objectives
{
    /// <summary>
    /// Crew qualification required by one objective objective.
    /// </summary>
    public enum ObjectiveCrewRequirement
    {
        UncrewedProbe,
        Crewed
    }

    /// <summary>
    /// Vessel situation required by the orbital objective objectives.
    /// PreOrbit contracts are evaluated from flight-attempt state rather than this value.
    /// </summary>
    public enum ObjectiveSituation
    {
        Orbit
    }

    /// <summary>
    /// Broad objective family used to distinguish orbital achievements from the four starter lines.
    /// </summary>
    public enum ObjectiveType
    {
        Orbit,
        DirectedPower,
        DeliveredMass,
        AltitudeHold,
        BiomeVisit
    }

    /// <summary>
    /// Special pre-orbit contract line. None identifies the normal space-race objective catalogue.
    /// </summary>
    public enum PreOrbitContractLine
    {
        None,
        DirectedPower,
        Mass,
        Control,
        Biome
    }

    /// <summary>
    /// Immutable measurable criteria supplied by each pre-orbit objective definition.
    /// Keeping these values as catalogue data prevents line/level metadata from secretly defining balance.
    /// </summary>
    internal sealed class PreOrbitContractCriteria
    {
        public static readonly PreOrbitContractCriteria None = new PreOrbitContractCriteria(
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            null);

        private PreOrbitContractCriteria(
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

        public static PreOrbitContractCriteria DirectedPower(
            double requiredSpeedMetersPerSecond,
            double maximumAltitudeMeters)
        {
            return new PreOrbitContractCriteria(
                requiredSpeedMetersPerSecond,
                0.0,
                0.0,
                0.0,
                maximumAltitudeMeters,
                0.0,
                null);
        }

        public static PreOrbitContractCriteria Mass(
            double requiredMassTonnes,
            double requiredDistanceMeters)
        {
            return new PreOrbitContractCriteria(
                0.0,
                requiredMassTonnes,
                requiredDistanceMeters,
                0.0,
                0.0,
                0.0,
                null);
        }

        public static PreOrbitContractCriteria Control(
            double minimumAltitudeMeters,
            double maximumAltitudeMeters,
            double requiredDurationSeconds)
        {
            return new PreOrbitContractCriteria(
                0.0,
                0.0,
                0.0,
                minimumAltitudeMeters,
                maximumAltitudeMeters,
                requiredDurationSeconds,
                null);
        }

        public static PreOrbitContractCriteria Biome(string requiredBiomeName)
        {
            return new PreOrbitContractCriteria(
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
    /// KSP-independent facts about one observed vessel that are relevant to orbital objective evaluation.
    /// A null crew qualification means the vessel does not fit a objective crew category.
    /// </summary>
    public sealed class OrbitalObjectiveObservation
    {
        public OrbitalObjectiveObservation(
            string celestialBodyName,
            ObjectiveSituation situation,
            ObjectiveCrewRequirement? crewQualification)
        {
            CelestialBodyName = celestialBodyName;
            Situation = situation;
            CrewQualification = crewQualification;
        }

        public string CelestialBodyName { get; private set; }
        public ObjectiveSituation Situation { get; private set; }
        public ObjectiveCrewRequirement? CrewQualification { get; private set; }
    }

    /// <summary>
    /// Immutable definition of one race objective. Gameplay state remains owned by space agencies;
    /// this type describes the objective, starter-contract balance metadata, and campaign unlock rule.
    /// </summary>
    public sealed class ObjectiveDefinition
    {
        public ObjectiveDefinition(
            string id,
            string name,
            string celestialBodyName,
            ObjectiveSituation situation,
            ObjectiveCrewRequirement crewRequirement,
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
                ObjectiveType.Orbit,
                PreOrbitContractLine.None,
                0,
                0.0,
                0.0)
        {
        }

        public ObjectiveDefinition(
            string id,
            string name,
            string celestialBodyName,
            ObjectiveSituation situation,
            ObjectiveCrewRequirement crewRequirement,
            string objectiveDescription,
            UnlockRuleDefinition unlockRule,
            ObjectiveType objectiveType,
            PreOrbitContractLine starterLine,
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
                PreOrbitContractCriteria.None)
        {
        }

        internal ObjectiveDefinition(
            string id,
            string name,
            string celestialBodyName,
            ObjectiveSituation situation,
            ObjectiveCrewRequirement crewRequirement,
            string objectiveDescription,
            UnlockRuleDefinition unlockRule,
            ObjectiveType objectiveType,
            PreOrbitContractLine starterLine,
            int starterLevel,
            double baseRewardFunds,
            double rivalProgressCostFunds,
            PreOrbitContractCriteria starterCriteria)
        {
            Id = id;
            Name = name;
            CelestialBodyName = celestialBodyName;
            Situation = situation;
            CrewRequirement = crewRequirement;
            UnlockRule = unlockRule;
            ObjectiveType = objectiveType;
            PreOrbitLine = starterLine;
            PreOrbitLevel = Math.Max(0, starterLevel);
            BaseRewardFunds = Math.Max(0.0, baseRewardFunds);
            RivalProgressCostFunds = Math.Max(0.0, rivalProgressCostFunds);

            PreOrbitContractCriteria criteria = starterCriteria ?? PreOrbitContractCriteria.None;
            RequiredSpeedMetersPerSecond = criteria.RequiredSpeedMetersPerSecond;
            RequiredMassTonnes = criteria.RequiredMassTonnes;
            RequiredDistanceMeters = criteria.RequiredDistanceMeters;
            MinimumAltitudeMeters = criteria.MinimumAltitudeMeters;
            MaximumAltitudeMeters = criteria.MaximumAltitudeMeters;
            RequiredDurationSeconds = criteria.RequiredDurationSeconds;
            RequiredBiomeName = criteria.RequiredBiomeName;

            // PreOrbit criteria are the single source of truth for both evaluation and player-facing
            // wording. This prevents catalogue text from drifting away from the tracker thresholds.
            ObjectiveDescription = CreateObjectiveDescription(objectiveDescription);
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string CelestialBodyName { get; private set; }
        public ObjectiveSituation Situation { get; private set; }
        public ObjectiveCrewRequirement CrewRequirement { get; private set; }
        public string ObjectiveDescription { get; private set; }
        public UnlockRuleDefinition UnlockRule { get; private set; }
        public ObjectiveType ObjectiveType { get; private set; }
        public PreOrbitContractLine PreOrbitLine { get; private set; }
        public int PreOrbitLevel { get; private set; }
        public double BaseRewardFunds { get; private set; }
        public double RivalProgressCostFunds { get; private set; }

        // Tracking and UI consume these values directly. ObjectiveCatalogue supplies them explicitly
        // for each starter definition instead of ObjectiveDefinition inferring balance from line/level.
        public double RequiredSpeedMetersPerSecond { get; private set; }
        public double RequiredMassTonnes { get; private set; }
        public double RequiredDistanceMeters { get; private set; }
        public double MinimumAltitudeMeters { get; private set; }
        public double MaximumAltitudeMeters { get; private set; }
        public double RequiredDurationSeconds { get; private set; }
        public string RequiredBiomeName { get; private set; }

        public bool IsPreOrbitContract
        {
            get { return PreOrbitLine != PreOrbitContractLine.None && PreOrbitLevel > 0; }
        }

        /// <summary>
        /// Returns whether one KSP-independent vessel observation satisfies this objective.
        /// PreOrbit objectives are evaluated by the flight-attempt tracker.
        /// </summary>
        public bool IsSatisfiedBy(OrbitalObjectiveObservation observation)
        {
            if (ObjectiveType != ObjectiveType.Orbit
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
            if (ObjectiveType == ObjectiveType.DirectedPower
                && RequiredSpeedMetersPerSecond > 0.0
                && MaximumAltitudeMeters > 0.0)
            {
                return "Reach "
                    + RequiredSpeedMetersPerSecond.ToString("#,0", CultureInfo.InvariantCulture)
                    + " m/s without exceeding "
                    + (MaximumAltitudeMeters / 1000.0).ToString("0.#", CultureInfo.InvariantCulture)
                    + " km altitude, then impact Kerbin.";
            }

            if (ObjectiveType == ObjectiveType.DeliveredMass
                && RequiredMassTonnes > 0.0
                && RequiredDistanceMeters > 0.0)
            {
                return "Land on Kerbin at least "
                    + (RequiredDistanceMeters / 1000.0).ToString("0.#", CultureInfo.InvariantCulture)
                    + " km from the launch point with at least "
                    + RequiredMassTonnes.ToString("0.#", CultureInfo.InvariantCulture)
                    + " t of remaining vessel mass.";
            }

            if (ObjectiveType == ObjectiveType.AltitudeHold
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

            if (ObjectiveType == ObjectiveType.BiomeVisit
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

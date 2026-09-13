using System;
using System.Globalization;
using TheRaceForSpace.Core;

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
    /// Broad objective family used to distinguish orbital objectives from the four pre-orbit lines.
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
    /// Special pre-orbit contract line. None identifies the normal space-campaign objective catalogue.
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
    /// Immutable definition of one campaign objective. Gameplay state remains owned by space agencies;
    /// this type describes the objective, pre-orbit contract criteria, and campaign unlock rule.
    /// Pre-Orbit reward and rival-cost values are read from CampaignSettings so user config remains authoritative.
    /// </summary>
    public sealed class ObjectiveDefinition
    {
        private readonly double _baseRewardFunds;
        private readonly double _rivalProgressCostFunds;

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
            PreOrbitContractLine preOrbitLine,
            int preOrbitLevel,
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
                preOrbitLine,
                preOrbitLevel,
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
            PreOrbitContractLine preOrbitLine,
            int preOrbitLevel,
            double baseRewardFunds,
            double rivalProgressCostFunds,
            PreOrbitContractCriteria preOrbitCriteria)
        {
            Id = id;
            Name = name;
            CelestialBodyName = celestialBodyName;
            Situation = situation;
            CrewRequirement = crewRequirement;
            UnlockRule = unlockRule;
            ObjectiveType = objectiveType;
            PreOrbitLine = preOrbitLine;
            PreOrbitLevel = Math.Max(0, preOrbitLevel);
            _baseRewardFunds = Math.Max(0.0, baseRewardFunds);
            _rivalProgressCostFunds = Math.Max(0.0, rivalProgressCostFunds);

            ObjectiveMissionProfile missionProfile = ObjectiveMissionProfileCatalogue.Resolve(id, crewRequirement);
            Difficulty = missionProfile.Difficulty;
            RequiredKerbalCount = missionProfile.RequiredKerbalCount;

            PreOrbitContractCriteria criteria = preOrbitCriteria ?? PreOrbitContractCriteria.None;
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

        /// <summary>
        /// Rival live-mission difficulty on the approved 1-10 scale.
        /// Current production objective IDs are assigned explicitly by ObjectiveMissionProfileCatalogue.
        /// </summary>
        public int Difficulty { get; private set; }

        /// <summary>
        /// Number of rival Kerbals required to launch this Contract. This remains independent from the
        /// broad crew-category enum so future Contracts can require more than one Kerbal.
        /// </summary>
        public int RequiredKerbalCount { get; private set; }

        public double BaseRewardFunds
        {
            get
            {
                return IsPreOrbitContract
                    ? CampaignSettings.GetPreOrbitRewardFunds(PreOrbitLevel)
                    : _baseRewardFunds;
            }
        }

        public double RivalProgressCostFunds
        {
            get
            {
                return IsPreOrbitContract
                    ? CampaignSettings.GetPreOrbitRivalProgressCostFunds(PreOrbitLevel)
                    : _rivalProgressCostFunds;
            }
        }

        // Tracking and UI consume these values directly. ObjectiveCatalogue supplies them explicitly
        // for each pre-orbit definition instead of ObjectiveDefinition inferring balance from line/level.
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

    internal struct ObjectiveMissionProfile
    {
        public ObjectiveMissionProfile(int difficulty, int requiredKerbalCount)
        {
            Difficulty = difficulty;
            RequiredKerbalCount = requiredKerbalCount;
        }

        public int Difficulty { get; private set; }
        public int RequiredKerbalCount { get; private set; }
    }

    /// <summary>
    /// Authoritative rival mission metadata for the current objective catalogue. Stable IDs, rather
    /// than display names or crew wording, define mission difficulty and exact crew requirement.
    /// </summary>
    internal static class ObjectiveMissionProfileCatalogue
    {
        public static ObjectiveMissionProfile Resolve(
            string objectiveId,
            ObjectiveCrewRequirement crewRequirement)
        {
            switch (objectiveId)
            {
                case ObjectiveCatalogue.DirectedPower1Id:
                case ObjectiveCatalogue.DirectedPower2Id:
                case ObjectiveCatalogue.Mass1Id:
                case ObjectiveCatalogue.Mass2Id:
                case ObjectiveCatalogue.Biome1Id:
                case ObjectiveCatalogue.Biome2Id:
                    return new ObjectiveMissionProfile(1, 0);

                case ObjectiveCatalogue.Control1Id:
                case ObjectiveCatalogue.Control2Id:
                    return new ObjectiveMissionProfile(1, 1);

                case ObjectiveCatalogue.DirectedPower3Id:
                case ObjectiveCatalogue.DirectedPower4Id:
                case ObjectiveCatalogue.Mass3Id:
                case ObjectiveCatalogue.Mass4Id:
                case ObjectiveCatalogue.Biome3Id:
                case ObjectiveCatalogue.Biome4Id:
                    return new ObjectiveMissionProfile(2, 0);

                case ObjectiveCatalogue.Control3Id:
                case ObjectiveCatalogue.Control4Id:
                    return new ObjectiveMissionProfile(2, 1);

                case ObjectiveCatalogue.DirectedPower5Id:
                case ObjectiveCatalogue.Mass5Id:
                case ObjectiveCatalogue.Biome5Id:
                    return new ObjectiveMissionProfile(3, 0);

                case ObjectiveCatalogue.Control5Id:
                    return new ObjectiveMissionProfile(3, 1);

                case ObjectiveCatalogue.ProbeOrbitId:
                    return new ObjectiveMissionProfile(4, 0);

                case ObjectiveCatalogue.CrewedOrbitId:
                    return new ObjectiveMissionProfile(5, 1);

                case ObjectiveCatalogue.MunProbeOrbitId:
                case ObjectiveCatalogue.MinmusProbeOrbitId:
                    return new ObjectiveMissionProfile(5, 0);

                case ObjectiveCatalogue.MunCrewedOrbitId:
                case ObjectiveCatalogue.MinmusCrewedOrbitId:
                    return new ObjectiveMissionProfile(6, 1);

                case ObjectiveCatalogue.DunaProbeOrbitId:
                case ObjectiveCatalogue.MohoProbeOrbitId:
                case ObjectiveCatalogue.GillyProbeOrbitId:
                case ObjectiveCatalogue.IkeProbeOrbitId:
                case ObjectiveCatalogue.DresProbeOrbitId:
                case ObjectiveCatalogue.JoolProbeOrbitId:
                case ObjectiveCatalogue.LaytheProbeOrbitId:
                case ObjectiveCatalogue.VallProbeOrbitId:
                case ObjectiveCatalogue.TyloProbeOrbitId:
                case ObjectiveCatalogue.BopProbeOrbitId:
                case ObjectiveCatalogue.PolProbeOrbitId:
                    return new ObjectiveMissionProfile(7, 0);

                case ObjectiveCatalogue.DunaCrewedOrbitId:
                case ObjectiveCatalogue.MohoCrewedOrbitId:
                case ObjectiveCatalogue.GillyCrewedOrbitId:
                case ObjectiveCatalogue.IkeCrewedOrbitId:
                case ObjectiveCatalogue.DresCrewedOrbitId:
                case ObjectiveCatalogue.JoolCrewedOrbitId:
                case ObjectiveCatalogue.LaytheCrewedOrbitId:
                case ObjectiveCatalogue.VallCrewedOrbitId:
                case ObjectiveCatalogue.TyloCrewedOrbitId:
                case ObjectiveCatalogue.BopCrewedOrbitId:
                case ObjectiveCatalogue.PolCrewedOrbitId:
                    return new ObjectiveMissionProfile(8, 1);

                case ObjectiveCatalogue.EveProbeOrbitId:
                case ObjectiveCatalogue.EelooProbeOrbitId:
                    return new ObjectiveMissionProfile(8, 0);

                case ObjectiveCatalogue.EveCrewedOrbitId:
                case ObjectiveCatalogue.EelooCrewedOrbitId:
                    return new ObjectiveMissionProfile(9, 1);

                default:
                    // Preserve the existing public constructor for ad-hoc/future definitions while current
                    // production catalogue IDs remain explicitly covered above. New production Contracts
                    // should add their approved profile here before they enter the catalogue.
                    return new ObjectiveMissionProfile(
                        1,
                        crewRequirement == ObjectiveCrewRequirement.Crewed ? 1 : 0);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using TheRaceForSpace.Objectives;

namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// KSP-independent situation values needed by the flight contract contracts.
    /// </summary>
    public enum FlightSituation
    {
        Other,
        Prelaunch,
        Flying,
        SubOrbital,
        Orbiting,
        Landed,
        Splashed
    }

    /// <summary>
    /// Condition-specific telemetry requested by the cached active flight-contract plan.
    /// Vessel identity, body, situation, launch time, coordinates, and observation time remain
    /// common attempt context because they are cheap direct values and preserve launch continuity.
    /// </summary>
    [Flags]
    public enum FlightTelemetryRequirement
    {
        None = 0,
        Altitude = 1 << 0,
        SurfaceSpeed = 1 << 1,
        Mass = 1 << 2,
        Biome = 1 << 3,
        Crew = 1 << 4,
        SurfaceImpact = 1 << 5,
        All = Altitude | SurfaceSpeed | Mass | Biome | Crew | SurfaceImpact
    }

    /// <summary>
    /// Converts the controller's cached active flight-contract set into the KSP telemetry fields
    /// required by those contracts. Callers can cache the result for as long as the active set instance
    /// remains unchanged.
    /// </summary>
    public static class FlightTelemetryPlan
    {
        public static FlightTelemetryRequirement GetRequirements(
            IList<ObjectiveDefinition> activeFlightContracts)
        {
            FlightTelemetryRequirement requirements = FlightTelemetryRequirement.None;
            if (activeFlightContracts == null)
            {
                return requirements;
            }

            for (int objectiveIndex = 0; objectiveIndex < activeFlightContracts.Count; objectiveIndex++)
            {
                ObjectiveDefinition objective = activeFlightContracts[objectiveIndex];
                if (objective == null)
                {
                    continue;
                }

                switch (objective.PreOrbitLine)
                {
                    case PreOrbitContractLine.DirectedPower:
                        requirements |= FlightTelemetryRequirement.Altitude
                            | FlightTelemetryRequirement.SurfaceSpeed
                            | FlightTelemetryRequirement.SurfaceImpact;
                        break;
                    case PreOrbitContractLine.Mass:
                        requirements |= FlightTelemetryRequirement.Mass;
                        break;
                    case PreOrbitContractLine.Control:
                        requirements |= FlightTelemetryRequirement.Altitude
                            | FlightTelemetryRequirement.Crew;
                        break;
                    case PreOrbitContractLine.Biome:
                        requirements |= FlightTelemetryRequirement.Biome;
                        break;
                }
            }

            return requirements;
        }
    }

    /// <summary>
    /// Lightweight snapshot of the currently controlled vessel. KSP-specific objects are converted
    /// by the integration layer before the flight-contract tracker consumes them.
    /// </summary>
    public sealed class ActiveVesselSnapshot
    {
        public ActiveVesselSnapshot(
            string vesselId,
            string celestialBodyName,
            FlightSituation situation,
            double altitudeMeters,
            double surfaceSpeedMetersPerSecond,
            double massTonnes,
            double latitudeDegrees,
            double longitudeDegrees,
            double bodyRadiusMeters,
            string biomeName,
            int crewCount,
            double launchUniversalTime,
            double observationUniversalTime)
        {
            VesselId = vesselId;
            CelestialBodyName = celestialBodyName;
            Situation = situation;
            AltitudeMeters = altitudeMeters;
            SurfaceSpeedMetersPerSecond = surfaceSpeedMetersPerSecond;
            MassTonnes = Math.Max(0.0, massTonnes);
            LatitudeDegrees = latitudeDegrees;
            LongitudeDegrees = longitudeDegrees;
            BodyRadiusMeters = Math.Max(0.0, bodyRadiusMeters);
            BiomeName = biomeName;
            CrewCount = Math.Max(0, crewCount);
            LaunchUniversalTime = launchUniversalTime;
            ObservationUniversalTime = observationUniversalTime;
        }

        public string VesselId { get; private set; }
        public string CelestialBodyName { get; private set; }
        public FlightSituation Situation { get; private set; }
        public double AltitudeMeters { get; private set; }
        public double SurfaceSpeedMetersPerSecond { get; private set; }
        public double MassTonnes { get; private set; }
        public double LatitudeDegrees { get; private set; }
        public double LongitudeDegrees { get; private set; }
        public double BodyRadiusMeters { get; private set; }
        public string BiomeName { get; private set; }
        public int CrewCount { get; private set; }
        public double LaunchUniversalTime { get; private set; }
        public double ObservationUniversalTime { get; private set; }
    }
}

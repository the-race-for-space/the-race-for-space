using System;

namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// KSP-independent situation values needed by the starter flight contracts.
    /// </summary>
    public enum TrackedFlightSituation
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
    /// Condition-specific telemetry requested by the cached active starter-contract plan.
    /// Vessel identity, body, situation, launch time, coordinates, and observation time remain
    /// common attempt context because they are cheap direct values and preserve launch continuity.
    /// </summary>
    [Flags]
    public enum StarterTelemetryRequirement
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
    /// Lightweight snapshot of the currently controlled vessel. KSP-specific objects are converted
    /// by the integration layer before the starter-flight tracker consumes them.
    /// </summary>
    public sealed class ActiveVesselTrackingSnapshot
    {
        public ActiveVesselTrackingSnapshot(
            string vesselId,
            string celestialBodyName,
            TrackedFlightSituation situation,
            double altitudeMeters,
            double surfaceSpeedMetersPerSecond,
            double massTonnes,
            double latitudeDegrees,
            double longitudeDegrees,
            double bodyRadiusMeters,
            string biomeName,
            int crewCount,
            double launchUniversalTime,
            double observationUniversalTime,
            StarterTelemetryRequirement telemetryRequirements = StarterTelemetryRequirement.All)
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
            TelemetryRequirements = telemetryRequirements;
        }

        public string VesselId { get; private set; }
        public string CelestialBodyName { get; private set; }
        public TrackedFlightSituation Situation { get; private set; }
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
        public StarterTelemetryRequirement TelemetryRequirements { get; private set; }
    }
}

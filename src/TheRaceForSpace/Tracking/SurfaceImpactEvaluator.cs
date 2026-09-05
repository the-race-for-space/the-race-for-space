using System;

namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// Evaluates normalized pre-destruction flight evidence without depending on KSP vessel APIs.
    /// KSP integration remains responsible for vessel identity, event subscription, and converting
    /// live vessel state into the values supplied here.
    /// </summary>
    public static class SurfaceImpactEvaluator
    {
        private const double SurfaceImpactProximityMeters = 100.0;
        private const double MinimumSurfaceImpactSpeedMetersPerSecond = 5.0;
        private const double MaximumFlightSampleAgeSeconds = 5.0;

        /// <summary>
        /// Returns whether a destruction event is consistent with a recent surface impact.
        /// The last genuine in-flight sample may be used when KSP has already changed the dying
        /// vessel to LANDED/SPLASHED or cleared its destruction-time speed.
        /// </summary>
        public static bool IsEligible(
            TrackedFlightSituation lastInFlightSituation,
            double lastInFlightSurfaceClearanceMeters,
            double lastInFlightSurfaceSpeedMetersPerSecond,
            double lastInFlightUniversalTime,
            double destructionSurfaceClearanceMeters,
            double destructionUniversalTime)
        {
            // Positive infinity is the deliberate "clearance unavailable" sentinel used by the
            // KSP adapter, but NaN/negative clearances and non-finite speed/time values are invalid.
            if (double.IsNaN(lastInFlightSurfaceClearanceMeters)
                || lastInFlightSurfaceClearanceMeters < 0.0
                || !IsFinite(lastInFlightSurfaceSpeedMetersPerSecond)
                || !IsFinite(lastInFlightUniversalTime)
                || lastInFlightUniversalTime < 0.0
                || double.IsNaN(destructionSurfaceClearanceMeters)
                || destructionSurfaceClearanceMeters < 0.0
                || !IsFinite(destructionUniversalTime)
                || destructionUniversalTime < 0.0)
            {
                return false;
            }

            double flightSampleAgeSeconds =
                destructionUniversalTime - lastInFlightUniversalTime;
            bool hasRecentInFlightSample = flightSampleAgeSeconds >= 0.0
                && flightSampleAgeSeconds <= MaximumFlightSampleAgeSeconds;
            bool wasInFlight = lastInFlightSituation == TrackedFlightSituation.Flying
                || lastInFlightSituation == TrackedFlightSituation.SubOrbital;

            if (!hasRecentInFlightSample
                || !wasInFlight
                || lastInFlightSurfaceSpeedMetersPerSecond
                    < MinimumSurfaceImpactSpeedMetersPerSecond)
            {
                return false;
            }

            bool isNearSurfaceAtDestruction =
                destructionSurfaceClearanceMeters <= SurfaceImpactProximityMeters;
            double sampleTravelAllowanceMeters = Math.Max(
                SurfaceImpactProximityMeters,
                lastInFlightSurfaceSpeedMetersPerSecond * flightSampleAgeSeconds);
            bool couldReachSurfaceSinceLastSample =
                lastInFlightSurfaceClearanceMeters <= sampleTravelAllowanceMeters;

            return isNearSurfaceAtDestruction || couldReachSurfaceSinceLastSample;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}

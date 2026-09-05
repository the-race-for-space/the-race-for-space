using System;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.Tests.Tracking
{
    internal static class SurfaceImpactEvaluatorTests
    {
        public static void RunAll()
        {
            RecentInFlightSurfaceImpactQualifies();
            DestructiveSplashTransitionUsesPriorFlightEvidence();
            PhysicsWarpUsesActualElapsedGameTime();
            StaleFlightSampleIsRejected();
            LowSpeedDeletionIsRejected();
            NonFlightSampleIsRejected();
            DestructionTooFarFromSurfaceIsRejected();
            InvalidNumericEvidenceIsRejected();
        }

        private static void RecentInFlightSurfaceImpactQualifies()
        {
            Require(
                SurfaceImpactEvaluator.IsEligible(
                    TrackedFlightSituation.Flying,
                    80.0,
                    100.0,
                    100.0,
                    0.0,
                    101.0),
                "A recent flying sample and near-surface destruction should qualify.");
        }

        private static void DestructiveSplashTransitionUsesPriorFlightEvidence()
        {
            Require(
                SurfaceImpactEvaluator.IsEligible(
                    TrackedFlightSituation.Flying,
                    400.0,
                    500.0,
                    200.0,
                    0.0,
                    201.0),
                "A destructive water/ground transition may use the preserved in-flight sample even when destruction happens at the surface.");
        }

        private static void PhysicsWarpUsesActualElapsedGameTime()
        {
            Require(
                SurfaceImpactEvaluator.IsEligible(
                    TrackedFlightSituation.SubOrbital,
                    1800.0,
                    500.0,
                    300.0,
                    double.PositiveInfinity,
                    304.0),
                "Four seconds of universal time at 500 m/s should allow a craft sampled 1.8 km above the surface to reach it before destruction.");
        }

        private static void StaleFlightSampleIsRejected()
        {
            Require(
                !SurfaceImpactEvaluator.IsEligible(
                    TrackedFlightSituation.Flying,
                    10.0,
                    500.0,
                    400.0,
                    0.0,
                    406.0),
                "A flight sample older than five universal-time seconds must not qualify a later deletion.");
        }

        private static void LowSpeedDeletionIsRejected()
        {
            Require(
                !SurfaceImpactEvaluator.IsEligible(
                    TrackedFlightSituation.Flying,
                    10.0,
                    4.0,
                    500.0,
                    0.0,
                    501.0),
                "A low-speed deletion near the surface must not be treated as an impact.");
        }

        private static void NonFlightSampleIsRejected()
        {
            Require(
                !SurfaceImpactEvaluator.IsEligible(
                    TrackedFlightSituation.Landed,
                    0.0,
                    500.0,
                    600.0,
                    0.0,
                    601.0),
                "A landed sample must not qualify as pre-impact flight evidence.");
        }

        private static void DestructionTooFarFromSurfaceIsRejected()
        {
            Require(
                !SurfaceImpactEvaluator.IsEligible(
                    TrackedFlightSituation.Flying,
                    2500.0,
                    500.0,
                    700.0,
                    double.PositiveInfinity,
                    704.0),
                "A destruction event must be near the surface or reachable from the last sample within the elapsed time.");
        }

        private static void InvalidNumericEvidenceIsRejected()
        {
            Require(
                !SurfaceImpactEvaluator.IsEligible(
                    TrackedFlightSituation.Flying,
                    1000.0,
                    double.PositiveInfinity,
                    800.0,
                    double.PositiveInfinity,
                    801.0),
                "Infinite speed must not make every surface clearance reachable.");
            Require(
                !SurfaceImpactEvaluator.IsEligible(
                    TrackedFlightSituation.Flying,
                    80.0,
                    100.0,
                    900.0,
                    double.NaN,
                    901.0),
                "NaN destruction clearance must not be treated as valid impact evidence.");
            Require(
                !SurfaceImpactEvaluator.IsEligible(
                    TrackedFlightSituation.Flying,
                    double.NaN,
                    100.0,
                    1000.0,
                    0.0,
                    1001.0),
                "NaN in-flight clearance must not be treated as valid impact evidence.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

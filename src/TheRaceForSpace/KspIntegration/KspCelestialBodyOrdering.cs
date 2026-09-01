using System;

namespace TheRaceForSpace.KspIntegration
{
    /// <summary>
    /// Provides a stable presentation distance for ordering funding objectives from Kerbin outward.
    /// The value is based on mean orbital radii rather than live body positions, so UI ordering does
    /// not move around as planets progress through their orbits.
    /// </summary>
    internal static class KspCelestialBodyOrdering
    {
        private const string HomeBodyName = "Kerbin";

        public static double GetSortDistanceFromKerbin(string celestialBodyName)
        {
            CelestialBody homeBody = FindBody(HomeBodyName);
            CelestialBody targetBody = FindBody(celestialBodyName);
            if (homeBody == null || targetBody == null)
            {
                return double.MaxValue;
            }

            if (ReferenceEquals(homeBody, targetBody))
            {
                return 0.0;
            }

            CelestialBody commonAncestor = FindCommonAncestor(homeBody, targetBody);
            if (commonAncestor == null)
            {
                return double.MaxValue;
            }

            if (ReferenceEquals(commonAncestor, homeBody))
            {
                return SumOrbitRadiiToAncestor(targetBody, homeBody);
            }

            if (ReferenceEquals(commonAncestor, targetBody))
            {
                return SumOrbitRadiiToAncestor(homeBody, targetBody);
            }

            CelestialBody homeBranch = FindBranchBelowAncestor(homeBody, commonAncestor);
            CelestialBody targetBranch = FindBranchBelowAncestor(targetBody, commonAncestor);
            if (homeBranch == null || targetBranch == null)
            {
                return double.MaxValue;
            }

            double homeBranchRadius = GetOrbitRadius(homeBranch);
            double targetBranchRadius = GetOrbitRadius(targetBranch);
            if (homeBranchRadius == double.MaxValue || targetBranchRadius == double.MaxValue)
            {
                return double.MaxValue;
            }

            double homeLocalDistance = SumOrbitRadiiToAncestor(homeBody, homeBranch);
            double targetLocalDistance = SumOrbitRadiiToAncestor(targetBody, targetBranch);
            if (homeLocalDistance == double.MaxValue || targetLocalDistance == double.MaxValue)
            {
                return double.MaxValue;
            }

            // Bodies on separate branches are ordered by the difference between the branches'
            // mean orbital radii, plus any moon-system distance below those branches. For the
            // stock system this keeps Kerbin first, then Mun, Minmus, nearby planets, and their
            // moons without depending on their changing live positions.
            return Math.Abs(targetBranchRadius - homeBranchRadius)
                + homeLocalDistance
                + targetLocalDistance;
        }

        private static CelestialBody FindBody(string celestialBodyName)
        {
            if (string.IsNullOrEmpty(celestialBodyName) || FlightGlobals.Bodies == null)
            {
                return null;
            }

            for (int bodyIndex = 0; bodyIndex < FlightGlobals.Bodies.Count; bodyIndex++)
            {
                CelestialBody body = FlightGlobals.Bodies[bodyIndex];
                if (body != null
                    && string.Equals(
                        body.bodyName,
                        celestialBodyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return body;
                }
            }

            return null;
        }

        private static CelestialBody FindCommonAncestor(CelestialBody firstBody, CelestialBody secondBody)
        {
            for (CelestialBody firstAncestor = firstBody;
                firstAncestor != null;
                firstAncestor = GetParent(firstAncestor))
            {
                for (CelestialBody secondAncestor = secondBody;
                    secondAncestor != null;
                    secondAncestor = GetParent(secondAncestor))
                {
                    if (ReferenceEquals(firstAncestor, secondAncestor))
                    {
                        return firstAncestor;
                    }
                }
            }

            return null;
        }

        private static CelestialBody FindBranchBelowAncestor(CelestialBody body, CelestialBody ancestor)
        {
            CelestialBody currentBody = body;
            CelestialBody parentBody = GetParent(currentBody);

            while (currentBody != null && parentBody != null && !ReferenceEquals(parentBody, ancestor))
            {
                currentBody = parentBody;
                parentBody = GetParent(currentBody);
            }

            return parentBody != null && ReferenceEquals(parentBody, ancestor)
                ? currentBody
                : null;
        }

        private static double SumOrbitRadiiToAncestor(CelestialBody body, CelestialBody ancestor)
        {
            if (ReferenceEquals(body, ancestor))
            {
                return 0.0;
            }

            double distance = 0.0;
            CelestialBody currentBody = body;
            while (currentBody != null && !ReferenceEquals(currentBody, ancestor))
            {
                double orbitRadius = GetOrbitRadius(currentBody);
                if (orbitRadius == double.MaxValue)
                {
                    return double.MaxValue;
                }

                distance += orbitRadius;
                if (double.IsNaN(distance) || double.IsInfinity(distance))
                {
                    return double.MaxValue;
                }

                currentBody = GetParent(currentBody);
            }

            return currentBody != null && ReferenceEquals(currentBody, ancestor)
                ? distance
                : double.MaxValue;
        }

        private static double GetOrbitRadius(CelestialBody body)
        {
            if (body == null || body.orbit == null)
            {
                return double.MaxValue;
            }

            double orbitRadius = Math.Abs(body.orbit.semiMajorAxis);
            return double.IsNaN(orbitRadius) || double.IsInfinity(orbitRadius)
                ? double.MaxValue
                : orbitRadius;
        }

        private static CelestialBody GetParent(CelestialBody body)
        {
            if (body == null
                || body.orbit == null
                || body.referenceBody == null
                || ReferenceEquals(body, body.referenceBody))
            {
                return null;
            }

            return body.referenceBody;
        }
    }
}

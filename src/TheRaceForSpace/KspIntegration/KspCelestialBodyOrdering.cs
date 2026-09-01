using System;
using System.Collections.Generic;

namespace TheRaceForSpace.KspIntegration
{
    /// <summary>
    /// Provides a stable presentation distance for ordering funding objectives from Kerbin outward.
    /// Mean orbital radii are used instead of live body positions so the UI order does not move as
    /// planets progress through their orbits.
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

            // This runs only when the cached funding catalogue is rebuilt, not on every IMGUI
            // repaint, so short path lists keep the orbital-tree comparison straightforward.
            IList<CelestialBody> homePath = CreatePathToRoot(homeBody);
            IList<CelestialBody> targetPath = CreatePathToRoot(targetBody);
            int homeBranchIndex = homePath.Count - 1;
            int targetBranchIndex = targetPath.Count - 1;
            bool foundCommonAncestor = false;

            while (homeBranchIndex >= 0
                && targetBranchIndex >= 0
                && ReferenceEquals(homePath[homeBranchIndex], targetPath[targetBranchIndex]))
            {
                foundCommonAncestor = true;
                homeBranchIndex--;
                targetBranchIndex--;
            }

            if (!foundCommonAncestor)
            {
                return double.MaxValue;
            }

            if (homeBranchIndex < 0)
            {
                return SumOrbitRadii(targetPath, targetBranchIndex);
            }

            if (targetBranchIndex < 0)
            {
                return SumOrbitRadii(homePath, homeBranchIndex);
            }

            double homeBranchRadius = GetOrbitRadius(homePath[homeBranchIndex]);
            double targetBranchRadius = GetOrbitRadius(targetPath[targetBranchIndex]);
            if (homeBranchRadius == double.MaxValue || targetBranchRadius == double.MaxValue)
            {
                return double.MaxValue;
            }

            double homeLocalDistance = SumOrbitRadii(homePath, homeBranchIndex - 1);
            double targetLocalDistance = SumOrbitRadii(targetPath, targetBranchIndex - 1);
            if (homeLocalDistance == double.MaxValue || targetLocalDistance == double.MaxValue)
            {
                return double.MaxValue;
            }

            // Separate planetary branches are compared by their mean orbital-radius difference.
            // Any moon-system distance below those branches is then added to keep nearby moons
            // grouped naturally with their parent body's part of the progression.
            return Math.Abs(targetBranchRadius - homeBranchRadius)
                + homeLocalDistance
                + targetLocalDistance;
        }

        private static IList<CelestialBody> CreatePathToRoot(CelestialBody body)
        {
            var path = new List<CelestialBody>();
            CelestialBody currentBody = body;

            while (currentBody != null)
            {
                path.Add(currentBody);
                currentBody = GetParent(currentBody);
            }

            return path;
        }

        private static double SumOrbitRadii(IList<CelestialBody> path, int lastIndexInclusive)
        {
            double distance = 0.0;
            for (int pathIndex = 0; pathIndex <= lastIndexInclusive; pathIndex++)
            {
                double orbitRadius = GetOrbitRadius(path[pathIndex]);
                if (orbitRadius == double.MaxValue)
                {
                    return double.MaxValue;
                }

                distance += orbitRadius;
                if (double.IsNaN(distance) || double.IsInfinity(distance))
                {
                    return double.MaxValue;
                }
            }

            return distance;
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

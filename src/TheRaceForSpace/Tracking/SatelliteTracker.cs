using System;
using System.Collections.Generic;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// Applies project-owned vessel snapshots to player satellite counts and milestone state.
    /// Raw KSP Vessel and ProtoVessel discovery is owned by the KSP integration layer.
    /// </summary>
    public static class SatelliteTracker
    {
        /// <summary>
        /// Compatibility overload for callers that still expose the two 0.3 lunar unlock groups.
        /// New tracking code should pass milestone definitions and available milestone IDs directly.
        /// </summary>
        public static void RefreshPlayerSatelliteCounts(
            SpaceProgramState playerProgram,
            bool lunarProbeAchievementsAvailable,
            bool lunarCrewedAchievementsAvailable)
        {
            var availableMilestoneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PrototypeMilestones.ProbeOrbitId,
                PrototypeMilestones.CrewedOrbitId
            };

            if (lunarProbeAchievementsAvailable)
            {
                availableMilestoneIds.Add(PrototypeMilestones.MunProbeOrbitId);
                availableMilestoneIds.Add(PrototypeMilestones.MinmusProbeOrbitId);
            }

            if (lunarCrewedAchievementsAvailable)
            {
                availableMilestoneIds.Add(PrototypeMilestones.MunCrewedOrbitId);
                availableMilestoneIds.Add(PrototypeMilestones.MinmusCrewedOrbitId);
            }

            RefreshPlayerSatelliteCounts(
                playerProgram,
                PrototypeMilestones.All,
                availableMilestoneIds);
        }

        /// <summary>
        /// Captures the current KSP vessel state through the integration boundary, then applies
        /// the normalized snapshots to the supplied player state and milestone definitions.
        /// </summary>
        public static void RefreshPlayerSatelliteCounts(
            SpaceProgramState playerProgram,
            IList<MilestoneDefinition> milestoneDefinitions,
            ISet<string> availableMilestoneIds)
        {
            if (playerProgram == null || milestoneDefinitions == null)
            {
                return;
            }

            IList<VesselTrackingSnapshot> vesselSnapshots;
            double currentUniversalTime;
            if (!KspVesselDiscovery.TryCaptureOrbitingVessels(
                out vesselSnapshots,
                out currentUniversalTime))
            {
                return;
            }

            RefreshPlayerSatelliteCounts(
                playerProgram,
                milestoneDefinitions,
                availableMilestoneIds,
                vesselSnapshots,
                currentUniversalTime);
        }

        /// <summary>
        /// Applies normalized orbiting-vessel snapshots without accessing KSP Vessel or ProtoVessel objects.
        /// </summary>
        public static void RefreshPlayerSatelliteCounts(
            SpaceProgramState playerProgram,
            IList<MilestoneDefinition> milestoneDefinitions,
            ISet<string> availableMilestoneIds,
            IList<VesselTrackingSnapshot> vesselSnapshots,
            double currentUniversalTime)
        {
            if (playerProgram == null
                || milestoneDefinitions == null
                || vesselSnapshots == null)
            {
                return;
            }

            var countsByBody = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int milestoneIndex = 0; milestoneIndex < milestoneDefinitions.Count; milestoneIndex++)
            {
                MilestoneDefinition milestone = milestoneDefinitions[milestoneIndex];
                if (milestone == null
                    || string.IsNullOrEmpty(milestone.CelestialBodyName)
                    || countsByBody.ContainsKey(milestone.CelestialBodyName))
                {
                    continue;
                }

                countsByBody.Add(milestone.CelestialBodyName, 0);
            }

            var observations = new List<MilestoneVesselObservation>();

            for (int vesselIndex = 0; vesselIndex < vesselSnapshots.Count; vesselIndex++)
            {
                VesselTrackingSnapshot vesselSnapshot = vesselSnapshots[vesselIndex];
                if (vesselSnapshot == null || string.IsNullOrEmpty(vesselSnapshot.CelestialBodyName))
                {
                    continue;
                }

                bool isPrototypeSatellite = vesselSnapshot.VesselType == TrackedVesselType.Probe
                    || vesselSnapshot.VesselType == TrackedVesselType.Relay;
                MilestoneCrewRequirement? crewQualification = null;

                if (vesselSnapshot.CrewCount > 0)
                {
                    crewQualification = MilestoneCrewRequirement.Crewed;
                }
                else if (isPrototypeSatellite)
                {
                    crewQualification = MilestoneCrewRequirement.UncrewedProbe;
                }

                if (crewQualification.HasValue)
                {
                    observations.Add(new MilestoneVesselObservation(
                        vesselSnapshot.CelestialBodyName,
                        MilestoneSituation.Orbit,
                        crewQualification));
                }

                // The prototype treats Probe and Relay vessel types as satellites. Only bodies
                // represented by milestone definitions need maintained counts in the current design.
                if (isPrototypeSatellite && countsByBody.ContainsKey(vesselSnapshot.CelestialBodyName))
                {
                    countsByBody[vesselSnapshot.CelestialBodyName] =
                        countsByBody[vesselSnapshot.CelestialBodyName] + 1;
                }
            }

            EvaluateMilestones(
                playerProgram,
                milestoneDefinitions,
                availableMilestoneIds,
                observations,
                currentUniversalTime);

            foreach (KeyValuePair<string, int> bodyCount in countsByBody)
            {
                playerProgram.SetSatelliteCount(bodyCount.Key, bodyCount.Value);
            }
        }

        private static void EvaluateMilestones(
            SpaceProgramState playerProgram,
            IList<MilestoneDefinition> milestoneDefinitions,
            ISet<string> availableMilestoneIds,
            IList<MilestoneVesselObservation> observations,
            double currentUniversalTime)
        {
            // Repeat only when this refresh records a new prerequisite. This makes chained
            // milestones independent of vessel ordering without introducing a rule engine.
            bool recordedAchievement;
            do
            {
                recordedAchievement = false;

                for (int milestoneIndex = 0; milestoneIndex < milestoneDefinitions.Count; milestoneIndex++)
                {
                    MilestoneDefinition milestone = milestoneDefinitions[milestoneIndex];
                    if (milestone == null
                        || playerProgram.HasAchievement(milestone.Id))
                    {
                        continue;
                    }

                    bool availableFromRace = availableMilestoneIds != null
                        && availableMilestoneIds.Contains(milestone.Id);
                    bool prerequisiteSatisfiedByPlayer = string.IsNullOrEmpty(milestone.PrerequisiteMilestoneId)
                        || playerProgram.HasAchievement(milestone.PrerequisiteMilestoneId);

                    if (!availableFromRace && !prerequisiteSatisfiedByPlayer)
                    {
                        continue;
                    }

                    for (int observationIndex = 0; observationIndex < observations.Count; observationIndex++)
                    {
                        if (!milestone.IsSatisfiedBy(observations[observationIndex]))
                        {
                            continue;
                        }

                        if (playerProgram.RecordAchievement(milestone.Id, currentUniversalTime))
                        {
                            recordedAchievement = true;
                        }

                        break;
                    }
                }
            }
            while (recordedAchievement);
        }
    }
}

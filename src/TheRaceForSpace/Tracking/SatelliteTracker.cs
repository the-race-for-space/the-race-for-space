using System;
using System.Collections.Generic;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// Applies KSP-independent vessel snapshots to player satellite counts and milestone state.
    /// Raw KSP vessel discovery is owned by the KSP integration layer.
    /// </summary>
    public static class SatelliteTracker
    {
        /// <summary>
        /// Refreshes player satellite counts from every observed Probe/Relay body and records
        /// available milestones from the same normalized orbiting-vessel snapshots.
        /// Satellite body tracking does not depend on the milestone catalogue.
        /// </summary>
        public static void RefreshPlayerSatelliteCounts(
            SpaceProgramState playerProgram,
            IList<MilestoneDefinition> milestoneDefinitions,
            ISet<string> availableMilestoneIds,
            IList<VesselTrackingSnapshot> vesselSnapshots,
            double currentUniversalTime)
        {
            if (playerProgram == null || vesselSnapshots == null)
            {
                return;
            }

            var countsByBody = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
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

                if (isPrototypeSatellite)
                {
                    int currentCount;
                    countsByBody.TryGetValue(vesselSnapshot.CelestialBodyName, out currentCount);
                    countsByBody[vesselSnapshot.CelestialBodyName] = currentCount + 1;
                }
            }

            if (milestoneDefinitions != null)
            {
                EvaluateMilestones(
                    playerProgram,
                    milestoneDefinitions,
                    availableMilestoneIds,
                    observations,
                    currentUniversalTime);
            }

            // A successful snapshot refresh is authoritative for player vessel presence. Clear
            // stale body counts first so removing the last satellite from any body returns it to zero,
            // including bodies that are not represented by the current milestone catalogue.
            playerProgram.ClearSatelliteCounts();
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

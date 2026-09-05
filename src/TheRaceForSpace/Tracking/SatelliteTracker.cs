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
        /// milestones whose shared campaign unlock rules are satisfied at the observation time.
        /// Satellite body tracking does not depend on the milestone catalogue.
        /// </summary>
        public static void RefreshPlayerSatelliteCounts(
            SpaceProgramState playerProgram,
            IList<SpaceProgramState> programs,
            IList<MilestoneDefinition> milestoneDefinitions,
            IList<VesselTrackingSnapshot> vesselSnapshots,
            double currentUniversalTime)
        {
            if (playerProgram == null || vesselSnapshots == null)
            {
                return;
            }

            var countsByBody = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var observations = new List<MilestoneVesselObservation>(vesselSnapshots.Count);

            for (int vesselIndex = 0; vesselIndex < vesselSnapshots.Count; vesselIndex++)
            {
                VesselTrackingSnapshot vesselSnapshot = vesselSnapshots[vesselIndex];
                if (vesselSnapshot == null || string.IsNullOrEmpty(vesselSnapshot.CelestialBodyName))
                {
                    continue;
                }

                bool isSatellite = vesselSnapshot.VesselType == TrackedVesselType.Probe
                    || vesselSnapshot.VesselType == TrackedVesselType.Relay;
                MilestoneCrewRequirement? crewQualification = null;

                if (vesselSnapshot.CrewCount > 0)
                {
                    crewQualification = MilestoneCrewRequirement.Crewed;
                }
                else if (vesselSnapshot.CrewCount == 0 && isSatellite)
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

                if (isSatellite)
                {
                    int currentCount;
                    countsByBody.TryGetValue(vesselSnapshot.CelestialBodyName, out currentCount);
                    countsByBody[vesselSnapshot.CelestialBodyName] = currentCount + 1;
                }
            }

            // A successful snapshot refresh is authoritative for player vessel presence. Apply the
            // new counts before evaluating milestone unlock rules so satellite-count prerequisites
            // see the same observation that may satisfy the milestone itself.
            playerProgram.ClearSatelliteCounts();
            foreach (KeyValuePair<string, int> bodyCount in countsByBody)
            {
                playerProgram.SetSatelliteCount(bodyCount.Key, bodyCount.Value);
            }

            if (milestoneDefinitions != null)
            {
                EvaluateMilestones(
                    playerProgram,
                    programs,
                    milestoneDefinitions,
                    observations,
                    currentUniversalTime);
            }
        }

        private static void EvaluateMilestones(
            SpaceProgramState playerProgram,
            IList<SpaceProgramState> programs,
            IList<MilestoneDefinition> milestoneDefinitions,
            IList<MilestoneVesselObservation> observations,
            double currentUniversalTime)
        {
            // Repeat only when this refresh records a new achievement. The player state in the
            // shared program collection changes immediately, so a newly satisfied unlock rule can
            // enable another milestone from the same vessel snapshot without depending on ordering.
            bool recordedAchievement;
            do
            {
                recordedAchievement = false;

                for (int milestoneIndex = 0; milestoneIndex < milestoneDefinitions.Count; milestoneIndex++)
                {
                    MilestoneDefinition milestone = milestoneDefinitions[milestoneIndex];
                    if (milestone == null
                        || playerProgram.HasAchievement(milestone.Id)
                        || !UnlockRuleEvaluator.IsSatisfied(
                            milestone.UnlockRule,
                            programs,
                            currentUniversalTime))
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

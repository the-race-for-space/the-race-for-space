using System;
using System.Collections.Generic;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Agencies;

namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// Applies KSP-independent vessel snapshots to player satellite counts and objective state.
    /// Raw KSP vessel discovery is owned by the KSP integration layer.
    /// </summary>
    public static class OrbitalVesselTracker
    {
        /// <summary>
        /// Refreshes player satellite counts from every observed Probe/Relay body and records
        /// objectives whose shared campaign unlock rules are satisfied at the observation time.
        /// Satellite body tracking does not depend on the objective catalogue.
        /// </summary>
        public static void RefreshOrbitalProgress(
            AgencyState playerAgency,
            IList<AgencyState> agencies,
            IList<ObjectiveDefinition> objectiveDefinitions,
            IList<OrbitingVesselSnapshot> vesselSnapshots,
            double currentUniversalTime)
        {
            if (playerAgency == null || vesselSnapshots == null)
            {
                return;
            }

            var countsByBody = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var observations = new List<OrbitalObjectiveObservation>(vesselSnapshots.Count);

            for (int vesselIndex = 0; vesselIndex < vesselSnapshots.Count; vesselIndex++)
            {
                OrbitingVesselSnapshot vesselSnapshot = vesselSnapshots[vesselIndex];
                if (vesselSnapshot == null || string.IsNullOrEmpty(vesselSnapshot.CelestialBodyName))
                {
                    continue;
                }

                bool isSatellite = vesselSnapshot.VesselType == OrbitalVesselType.Probe
                    || vesselSnapshot.VesselType == OrbitalVesselType.Relay;
                ObjectiveCrewRequirement? crewQualification = null;

                if (vesselSnapshot.CrewCount > 0)
                {
                    crewQualification = ObjectiveCrewRequirement.Crewed;
                }
                else if (vesselSnapshot.CrewCount == 0 && isSatellite)
                {
                    crewQualification = ObjectiveCrewRequirement.UncrewedProbe;
                }

                if (crewQualification.HasValue)
                {
                    observations.Add(new OrbitalObjectiveObservation(
                        vesselSnapshot.CelestialBodyName,
                        ObjectiveSituation.Orbit,
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
            // new counts before evaluating objective unlock rules so satellite-count prerequisites
            // see the same observation that may satisfy the objective itself.
            playerAgency.ClearSatelliteCounts();
            foreach (KeyValuePair<string, int> bodyCount in countsByBody)
            {
                playerAgency.SetSatelliteCount(bodyCount.Key, bodyCount.Value);
            }

            if (objectiveDefinitions != null)
            {
                EvaluateObjectives(
                    playerAgency,
                    agencies,
                    objectiveDefinitions,
                    observations,
                    currentUniversalTime);
            }
        }

        private static void EvaluateObjectives(
            AgencyState playerAgency,
            IList<AgencyState> agencies,
            IList<ObjectiveDefinition> objectiveDefinitions,
            IList<OrbitalObjectiveObservation> observations,
            double currentUniversalTime)
        {
            // Repeat only when this refresh records a new objective completion. The player state in the
            // shared agency collection changes immediately, so a newly satisfied unlock rule can
            // enable another objective from the same vessel snapshot without depending on ordering.
            bool recordedObjective;
            do
            {
                recordedObjective = false;

                for (int objectiveIndex = 0; objectiveIndex < objectiveDefinitions.Count; objectiveIndex++)
                {
                    ObjectiveDefinition objective = objectiveDefinitions[objectiveIndex];
                    if (objective == null
                        || playerAgency.HasCompletedObjective(objective.Id)
                        || !UnlockRuleEvaluator.IsSatisfied(
                            objective.UnlockRule,
                            agencies,
                            currentUniversalTime))
                    {
                        continue;
                    }

                    for (int observationIndex = 0; observationIndex < observations.Count; observationIndex++)
                    {
                        if (!objective.IsSatisfiedBy(observations[observationIndex]))
                        {
                            continue;
                        }

                        if (playerAgency.RecordObjectiveCompletion(objective.Id, currentUniversalTime))
                        {
                            recordedObjective = true;
                        }

                        break;
                    }
                }
            }
            while (recordedObjective);
        }
    }
}

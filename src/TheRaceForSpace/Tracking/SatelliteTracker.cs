using System;
using System.Collections.Generic;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// Reads persistent vessels so satellite progress and milestone achievements work for
    /// loaded and unloaded craft.
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
        /// Refreshes player satellite counts and records available milestones from the supplied
        /// definitions. KSP vessel objects are converted into project-owned observations before
        /// milestone rules are evaluated.
        /// </summary>
        public static void RefreshPlayerSatelliteCounts(
            SpaceProgramState playerProgram,
            IList<MilestoneDefinition> milestoneDefinitions,
            ISet<string> availableMilestoneIds)
        {
            if (playerProgram == null
                || milestoneDefinitions == null
                || HighLogic.CurrentGame == null
                || HighLogic.CurrentGame.flightState == null
                || Planetarium.fetch == null)
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
            List<ProtoVessel> protoVessels = HighLogic.CurrentGame.flightState.protoVessels;
            double currentUniversalTime = Planetarium.GetUniversalTime();

            for (int vesselIndex = 0; vesselIndex < protoVessels.Count; vesselIndex++)
            {
                ProtoVessel protoVessel = protoVessels[vesselIndex];
                if (protoVessel == null)
                {
                    continue;
                }

                string bodyName;
                VesselType vesselType;
                int crewCount;
                Vessel liveVessel = protoVessel.vesselRef;

                // KSP can leave a loaded vessel's ProtoVessel snapshot describing the state
                // from scene entry or the last save. When the live vessel exists, use its
                // current situation/body/type so an active craft that has just reached orbit
                // is recognised immediately. Unloaded vessels continue to use persistent data.
                if (liveVessel != null && liveVessel.loaded)
                {
                    if (liveVessel.situation != Vessel.Situations.ORBITING
                        || liveVessel.mainBody == null)
                    {
                        continue;
                    }

                    bodyName = liveVessel.mainBody.bodyName;
                    vesselType = liveVessel.vesselType;
                    crewCount = liveVessel.GetCrewCount();
                }
                else
                {
                    if (protoVessel.orbitSnapShot == null
                        || protoVessel.situation != Vessel.Situations.ORBITING)
                    {
                        continue;
                    }

                    int bodyIndex = protoVessel.orbitSnapShot.ReferenceBodyIndex;
                    if (bodyIndex < 0 || bodyIndex >= FlightGlobals.Bodies.Count)
                    {
                        continue;
                    }

                    bodyName = FlightGlobals.Bodies[bodyIndex].bodyName;
                    vesselType = protoVessel.vesselType;
                    crewCount = GetProtoCrewCount(protoVessel);
                }

                bool isPrototypeSatellite = vesselType == VesselType.Probe || vesselType == VesselType.Relay;
                MilestoneCrewRequirement? crewQualification = null;

                if (crewCount > 0)
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
                        bodyName,
                        MilestoneSituation.Orbit,
                        crewQualification));
                }

                // The prototype treats Probe and Relay vessel types as satellites. Only bodies
                // represented by milestone definitions need maintained counts in the current design.
                if (isPrototypeSatellite && countsByBody.ContainsKey(bodyName))
                {
                    countsByBody[bodyName] = countsByBody[bodyName] + 1;
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
            // milestones independent of KSP vessel ordering without introducing a rule engine.
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

        private static int GetProtoCrewCount(ProtoVessel protoVessel)
        {
            int crewCount = 0;

            if (protoVessel == null || protoVessel.protoPartSnapshots == null)
            {
                return crewCount;
            }

            for (int partIndex = 0; partIndex < protoVessel.protoPartSnapshots.Count; partIndex++)
            {
                ProtoPartSnapshot partSnapshot = protoVessel.protoPartSnapshots[partIndex];
                if (partSnapshot == null || partSnapshot.protoModuleCrew == null)
                {
                    continue;
                }

                for (int crewIndex = 0; crewIndex < partSnapshot.protoModuleCrew.Count; crewIndex++)
                {
                    if (partSnapshot.protoModuleCrew[crewIndex] != null)
                    {
                        crewCount++;
                    }
                }
            }

            return crewCount;
        }
    }
}

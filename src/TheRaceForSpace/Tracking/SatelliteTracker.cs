using System.Collections.Generic;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// Reads persistent vessels so satellite progress and 0.3 orbit achievements
    /// work for loaded and unloaded craft.
    /// </summary>
    public static class SatelliteTracker
    {
        public static void RefreshPlayerSatelliteCounts(SpaceProgramState playerProgram)
        {
            if (playerProgram == null
                || HighLogic.CurrentGame == null
                || HighLogic.CurrentGame.flightState == null
                || Planetarium.fetch == null)
            {
                return;
            }

            var countsByBody = new Dictionary<string, int>();
            List<ProtoVessel> protoVessels = HighLogic.CurrentGame.flightState.protoVessels;
            double currentUniversalTime = Planetarium.GetUniversalTime();

            for (int i = 0; i < protoVessels.Count; i++)
            {
                ProtoVessel protoVessel = protoVessels[i];
                if (protoVessel == null || protoVessel.orbitSnapShot == null)
                {
                    continue;
                }

                if (protoVessel.situation != Vessel.Situations.ORBITING)
                {
                    continue;
                }

                int bodyIndex = protoVessel.orbitSnapShot.ReferenceBodyIndex;
                if (bodyIndex < 0 || bodyIndex >= FlightGlobals.Bodies.Count)
                {
                    continue;
                }

                string bodyName = FlightGlobals.Bodies[bodyIndex].bodyName;
                bool isPrototypeSatellite =
                    protoVessel.vesselType == VesselType.Probe || protoVessel.vesselType == VesselType.Relay;

                if (bodyName == "Kerbin")
                {
                    int crewCount = GetProtoCrewCount(protoVessel);

                    // Orbit achievements are permanent once observed. Probe/Relay vessel types
                    // represent the prototype's uncrewed probe category, while any vessel with
                    // at least one valid ProtoCrewMember can satisfy the crewed-orbit objective.
                    if (crewCount > 0 && !playerProgram.HasAchievedCrewedOrbit)
                    {
                        playerProgram.HasAchievedCrewedOrbit = true;
                        playerProgram.CrewedOrbitAchievementUniversalTime = currentUniversalTime;
                    }
                    else if (isPrototypeSatellite && !playerProgram.HasAchievedProbeOrbit)
                    {
                        playerProgram.HasAchievedProbeOrbit = true;
                        playerProgram.ProbeOrbitAchievementUniversalTime = currentUniversalTime;
                    }
                }

                // The prototype treats probe and relay vessel types as satellites.
                if (!isPrototypeSatellite)
                {
                    continue;
                }

                int existingCount;
                countsByBody.TryGetValue(bodyName, out existingCount);
                countsByBody[bodyName] = existingCount + 1;
            }

            playerProgram.SetSatelliteCount("Kerbin", GetCount(countsByBody, "Kerbin"));
            playerProgram.SetSatelliteCount("Mun", GetCount(countsByBody, "Mun"));
            playerProgram.SetSatelliteCount("Minmus", GetCount(countsByBody, "Minmus"));
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

        private static int GetCount(Dictionary<string, int> countsByBody, string bodyName)
        {
            int count;
            return countsByBody.TryGetValue(bodyName, out count) ? count : 0;
        }
    }
}

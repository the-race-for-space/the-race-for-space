using System.Collections.Generic;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// Reads persistent vessels so satellite progress works for loaded and unloaded craft.
    /// </summary>
    public static class SatelliteTracker
    {
        public static void RefreshPlayerSatelliteCounts(SpaceProgramState playerProgram)
        {
            if (playerProgram == null || HighLogic.CurrentGame == null || HighLogic.CurrentGame.flightState == null)
            {
                return;
            }

            var countsByBody = new Dictionary<string, int>();
            List<ProtoVessel> protoVessels = HighLogic.CurrentGame.flightState.protoVessels;

            for (int i = 0; i < protoVessels.Count; i++)
            {
                ProtoVessel protoVessel = protoVessels[i];
                if (protoVessel == null || protoVessel.orbitSnapShot == null)
                {
                    continue;
                }

                // The prototype treats probe and relay vessel types as satellites.
                if (protoVessel.vesselType != VesselType.Probe && protoVessel.vesselType != VesselType.Relay)
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
                int existingCount;
                countsByBody.TryGetValue(bodyName, out existingCount);
                countsByBody[bodyName] = existingCount + 1;
            }

            playerProgram.SetSatelliteCount("Kerbin", GetCount(countsByBody, "Kerbin"));
            playerProgram.SetSatelliteCount("Mun", GetCount(countsByBody, "Mun"));
            playerProgram.SetSatelliteCount("Minmus", GetCount(countsByBody, "Minmus"));
        }

        private static int GetCount(Dictionary<string, int> countsByBody, string bodyName)
        {
            int count;
            return countsByBody.TryGetValue(bodyName, out count) ? count : 0;
        }
    }
}

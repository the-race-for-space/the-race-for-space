using System.Collections.Generic;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.KspIntegration
{
    /// <summary>
    /// Reads KSP vessel state and converts it into project-owned tracking snapshots.
    /// Loaded vessels use live state; unloaded vessels use persistent ProtoVessel state.
    /// </summary>
    public static class KspVesselDiscovery
    {
        /// <summary>
        /// Captures the orbiting vessels available in the current save. Returns false while
        /// KSP flight-state data is not ready so callers can preserve their last known counts.
        /// </summary>
        public static bool TryCaptureOrbitingVessels(out IList<VesselTrackingSnapshot> vesselSnapshots)
        {
            vesselSnapshots = null;

            if (HighLogic.CurrentGame == null
                || HighLogic.CurrentGame.flightState == null
                || HighLogic.CurrentGame.flightState.protoVessels == null)
            {
                return false;
            }

            var snapshots = new List<VesselTrackingSnapshot>();
            List<ProtoVessel> protoVessels = HighLogic.CurrentGame.flightState.protoVessels;

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

                // A loaded vessel's ProtoVessel can lag behind the live craft after a scene
                // transition. Prefer live situation/body/type/crew so a newly reached orbit is
                // visible immediately. Unloaded vessels continue to use persistent state.
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
                        || protoVessel.situation != Vessel.Situations.ORBITING
                        || FlightGlobals.Bodies == null)
                    {
                        continue;
                    }

                    int bodyIndex = protoVessel.orbitSnapShot.ReferenceBodyIndex;
                    if (bodyIndex < 0 || bodyIndex >= FlightGlobals.Bodies.Count)
                    {
                        continue;
                    }

                    CelestialBody celestialBody = FlightGlobals.Bodies[bodyIndex];
                    if (celestialBody == null)
                    {
                        continue;
                    }

                    bodyName = celestialBody.bodyName;
                    vesselType = protoVessel.vesselType;
                    crewCount = GetProtoCrewCount(protoVessel);
                }

                if (string.IsNullOrEmpty(bodyName))
                {
                    continue;
                }

                snapshots.Add(new VesselTrackingSnapshot(
                    bodyName,
                    ConvertVesselType(vesselType),
                    crewCount));
            }

            vesselSnapshots = snapshots;
            return true;
        }

        private static TrackedVesselType ConvertVesselType(VesselType vesselType)
        {
            if (vesselType == VesselType.Probe)
            {
                return TrackedVesselType.Probe;
            }

            if (vesselType == VesselType.Relay)
            {
                return TrackedVesselType.Relay;
            }

            return TrackedVesselType.Other;
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

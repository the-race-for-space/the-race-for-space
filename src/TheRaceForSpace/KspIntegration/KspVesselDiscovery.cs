using System;
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
        private const double SurfaceImpactProximityMeters = 100.0;
        private const double MinimumSurfaceImpactSpeedMetersPerSecond = 5.0;

        private static Game _activeTrackingGame;
        private static Vessel _destructionTrackedVessel;
        private static Callback _destructionCallback;
        private static string _pendingImpactVesselId;
        private static string _pendingImpactBodyName;
        private static double _pendingImpactUniversalTime = -1.0;

        /// <summary>
        /// Captures the orbiting vessels available in the current save together with the KSP
        /// universal time for that observation. Returns false while required game state is not ready.
        /// </summary>
        public static bool TryCaptureOrbitingVessels(
            out IList<VesselTrackingSnapshot> vesselSnapshots,
            out double currentUniversalTime)
        {
            vesselSnapshots = null;
            currentUniversalTime = -1.0;

            if (HighLogic.CurrentGame == null
                || HighLogic.CurrentGame.flightState == null
                || HighLogic.CurrentGame.flightState.protoVessels == null
                || Planetarium.fetch == null)
            {
                return false;
            }

            // Capture the observation time before walking vessel state, matching the previous
            // tracker behaviour so all milestones found in one refresh share one stable timestamp.
            currentUniversalTime = Planetarium.GetUniversalTime();

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

        /// <summary>
        /// Captures only the currently controlled loaded vessel for the frequent starter-contract
        /// path. This avoids repeating the expensive all-vessel scan every second.
        /// </summary>
        public static bool TryCaptureActiveVessel(out ActiveVesselTrackingSnapshot vesselSnapshot)
        {
            vesselSnapshot = null;
            EnsureActiveTrackingGame();

            if (!HighLogic.LoadedSceneIsFlight
                || FlightGlobals.ActiveVessel == null
                || Planetarium.fetch == null)
            {
                DetachDestructionCallback();
                return false;
            }

            Vessel vessel = FlightGlobals.ActiveVessel;
            if (!vessel.loaded
                || vessel.isEVA
                || vessel.mainBody == null
                || string.IsNullOrEmpty(vessel.mainBody.bodyName))
            {
                DetachDestructionCallback();
                return false;
            }

            TrackActiveVesselDestruction(vessel);

            string biomeName = null;
            if (string.Equals(vessel.mainBody.bodyName, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                biomeName = ScienceUtil.GetExperimentBiome(
                    vessel.mainBody,
                    vessel.latitude,
                    vessel.longitude);
            }

            vesselSnapshot = new ActiveVesselTrackingSnapshot(
                vessel.id.ToString("D"),
                vessel.mainBody.bodyName,
                ConvertSituation(vessel.situation),
                vessel.altitude,
                vessel.srfSpeed,
                vessel.GetTotalMass(),
                vessel.latitude,
                vessel.longitude,
                vessel.mainBody.Radius,
                biomeName,
                vessel.GetCrewCount(),
                vessel.launchTime,
                Planetarium.GetUniversalTime());
            return true;
        }

        /// <summary>
        /// Returns one pending destruction event only when the actively tracked vessel was moving
        /// and close enough to terrain or sea level to represent a Kerbin surface impact.
        /// </summary>
        public static bool TryConsumeActiveVesselSurfaceImpact(
            out string vesselId,
            out string celestialBodyName,
            out double impactUniversalTime)
        {
            EnsureActiveTrackingGame();

            vesselId = _pendingImpactVesselId;
            celestialBodyName = _pendingImpactBodyName;
            impactUniversalTime = _pendingImpactUniversalTime;

            bool hasImpact = !string.IsNullOrEmpty(vesselId)
                && !string.IsNullOrEmpty(celestialBodyName)
                && impactUniversalTime >= 0.0;

            _pendingImpactVesselId = null;
            _pendingImpactBodyName = null;
            _pendingImpactUniversalTime = -1.0;
            return hasImpact;
        }

        /// <summary>
        /// Clears active-vessel callback state when a different KSP save becomes current.
        /// </summary>
        public static void ResetActiveVesselTracking()
        {
            DetachDestructionCallback();
            _activeTrackingGame = null;
            _pendingImpactVesselId = null;
            _pendingImpactBodyName = null;
            _pendingImpactUniversalTime = -1.0;
        }

        private static void EnsureActiveTrackingGame()
        {
            if (_activeTrackingGame == HighLogic.CurrentGame)
            {
                return;
            }

            ResetActiveVesselTracking();
            _activeTrackingGame = HighLogic.CurrentGame;
        }

        private static void TrackActiveVesselDestruction(Vessel vessel)
        {
            if (_destructionTrackedVessel == vessel)
            {
                return;
            }

            DetachDestructionCallback();
            if (vessel == null)
            {
                return;
            }

            Vessel trackedVessel = vessel;
            _destructionTrackedVessel = vessel;
            _destructionCallback = delegate
            {
                RecordPotentialSurfaceImpact(trackedVessel);
                _destructionTrackedVessel = null;
                _destructionCallback = null;
            };
            vessel.OnJustAboutToBeDestroyed += _destructionCallback;
        }

        private static void DetachDestructionCallback()
        {
            if (_destructionTrackedVessel != null && _destructionCallback != null)
            {
                _destructionTrackedVessel.OnJustAboutToBeDestroyed -= _destructionCallback;
            }

            _destructionTrackedVessel = null;
            _destructionCallback = null;
        }

        private static void RecordPotentialSurfaceImpact(Vessel vessel)
        {
            if (vessel == null
                || vessel.mainBody == null
                || vessel.LandedOrSplashed
                || vessel.srfSpeed < MinimumSurfaceImpactSpeedMetersPerSecond)
            {
                return;
            }

            bool isNearTerrain = vessel.heightFromTerrain >= 0.0f
                && vessel.heightFromTerrain <= SurfaceImpactProximityMeters;
            bool isNearSeaLevel = vessel.altitude <= SurfaceImpactProximityMeters;
            if (!isNearTerrain && !isNearSeaLevel)
            {
                return;
            }

            _pendingImpactVesselId = vessel.id.ToString("D");
            _pendingImpactBodyName = vessel.mainBody.bodyName;
            _pendingImpactUniversalTime = Planetarium.fetch == null
                ? 0.0
                : Planetarium.GetUniversalTime();
        }

        private static TrackedFlightSituation ConvertSituation(Vessel.Situations situation)
        {
            switch (situation)
            {
                case Vessel.Situations.PRELAUNCH:
                    return TrackedFlightSituation.Prelaunch;
                case Vessel.Situations.FLYING:
                    return TrackedFlightSituation.Flying;
                case Vessel.Situations.SUB_ORBITAL:
                    return TrackedFlightSituation.SubOrbital;
                case Vessel.Situations.ORBITING:
                    return TrackedFlightSituation.Orbiting;
                case Vessel.Situations.LANDED:
                    return TrackedFlightSituation.Landed;
                case Vessel.Situations.SPLASHED:
                    return TrackedFlightSituation.Splashed;
                default:
                    return TrackedFlightSituation.Other;
            }
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

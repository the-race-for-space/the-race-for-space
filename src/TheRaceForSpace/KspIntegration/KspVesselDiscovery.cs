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
        private const double SurfaceImpactSampleTravelAllowanceSeconds = 2.0;
        private const double SurfaceImpactFlightSampleMaximumAgeSeconds = 3.0;

        private static Game _activeTrackingGame;
        private static Vessel _destructionTrackedVessel;
        private static Callback _destructionCallback;
        private static string _destructionTrackedVesselId;
        private static string _lastTrackedBodyName;
        private static double _lastTrackedSurfaceClearanceMeters = double.PositiveInfinity;
        private static double _lastTrackedSurfaceSpeedMetersPerSecond;
        private static TrackedFlightSituation _lastTrackedSituation = TrackedFlightSituation.Other;
        private static double _lastTrackedInFlightUniversalTime = -1.0;
        private static bool _isVesselWillDestroySubscribed;
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
            CaptureDestructionTelemetry(vessel);

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
        /// Returns one pending destruction event only when the actively tracked vessel was close
        /// enough to the surface, or its last reliable one-second sample shows that it could have
        /// reached the surface before KSP destroyed the vessel object.
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
            if (_isVesselWillDestroySubscribed)
            {
                GameEvents.onVesselWillDestroy.Remove(OnVesselWillDestroy);
                _isVesselWillDestroySubscribed = false;
            }

            _activeTrackingGame = null;
            _destructionTrackedVesselId = null;
            _lastTrackedBodyName = null;
            _lastTrackedSurfaceClearanceMeters = double.PositiveInfinity;
            _lastTrackedSurfaceSpeedMetersPerSecond = 0.0;
            _lastTrackedSituation = TrackedFlightSituation.Other;
            _lastTrackedInFlightUniversalTime = -1.0;
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
            if (_activeTrackingGame != null)
            {
                // Vessel.OnJustAboutToBeDestroyed can be missed during some breakup sequences.
                // KSP's global event is an additional last-chance notification for the same vessel.
                GameEvents.onVesselWillDestroy.Add(OnVesselWillDestroy);
                _isVesselWillDestroySubscribed = true;
            }
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

        private static void CaptureDestructionTelemetry(Vessel vessel)
        {
            if (vessel == null || vessel.mainBody == null)
            {
                return;
            }

            string vesselId = vessel.id.ToString("D");
            if (!string.Equals(
                _destructionTrackedVesselId,
                vesselId,
                StringComparison.OrdinalIgnoreCase))
            {
                // A replacement active vessel must never inherit the previous craft's pre-impact
                // telemetry, especially when the new vessel is still PRELAUNCH or already landed.
                _lastTrackedSurfaceClearanceMeters = double.PositiveInfinity;
                _lastTrackedSurfaceSpeedMetersPerSecond = 0.0;
                _lastTrackedSituation = TrackedFlightSituation.Other;
                _lastTrackedInFlightUniversalTime = -1.0;
            }

            _destructionTrackedVesselId = vesselId;
            _lastTrackedBodyName = vessel.mainBody.bodyName;

            TrackedFlightSituation currentSituation = ConvertSituation(vessel.situation);
            if (currentSituation != TrackedFlightSituation.Flying
                && currentSituation != TrackedFlightSituation.SubOrbital)
            {
                // KSP can report SPLASHED or LANDED for a fraction of a second before a violent
                // impact finishes destroying the vessel. Preserve the immediately preceding genuine
                // in-flight sample so that transition cannot erase an otherwise valid crash.
                return;
            }

            _lastTrackedSurfaceClearanceMeters = GetSurfaceClearanceMeters(vessel);
            _lastTrackedSurfaceSpeedMetersPerSecond = Math.Max(0.0, vessel.srfSpeed);
            _lastTrackedSituation = currentSituation;
            _lastTrackedInFlightUniversalTime = Planetarium.fetch == null
                ? -1.0
                : Planetarium.GetUniversalTime();
        }

        private static void OnVesselWillDestroy(Vessel vessel)
        {
            RecordPotentialSurfaceImpact(vessel);
        }

        private static void RecordPotentialSurfaceImpact(Vessel vessel)
        {
            if (vessel == null
                || vessel.mainBody == null
                || string.IsNullOrEmpty(_destructionTrackedVesselId)
                || !string.Equals(
                    _destructionTrackedVesselId,
                    vessel.id.ToString("D"),
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Destruction-time vessel values are not stable during a violent breakup: KSP may
            // already report LANDED/SPLASHED and zero surface speed by the time the death callback
            // runs. Use the most recent genuine in-flight sample, but only for a short window so a
            // later recovery, termination, or unrelated deletion cannot reuse stale crash telemetry.
            double currentUniversalTime = Planetarium.fetch == null
                ? -1.0
                : Planetarium.GetUniversalTime();
            bool hasRecentInFlightSample = _lastTrackedInFlightUniversalTime >= 0.0
                && currentUniversalTime >= _lastTrackedInFlightUniversalTime
                && currentUniversalTime - _lastTrackedInFlightUniversalTime
                    <= SurfaceImpactFlightSampleMaximumAgeSeconds;
            bool wasInFlight = _lastTrackedSituation == TrackedFlightSituation.Flying
                || _lastTrackedSituation == TrackedFlightSituation.SubOrbital;
            if (!hasRecentInFlightSample
                || !wasInFlight
                || _lastTrackedSurfaceSpeedMetersPerSecond < MinimumSurfaceImpactSpeedMetersPerSecond)
            {
                return;
            }

            double currentSurfaceClearanceMeters = GetSurfaceClearanceMeters(vessel);
            bool isNearSurfaceNow = currentSurfaceClearanceMeters <= SurfaceImpactProximityMeters;
            double sampleTravelAllowanceMeters = Math.Max(
                SurfaceImpactProximityMeters,
                _lastTrackedSurfaceSpeedMetersPerSecond * SurfaceImpactSampleTravelAllowanceSeconds);
            bool couldReachSurfaceSinceLastSample =
                _lastTrackedSurfaceClearanceMeters <= sampleTravelAllowanceMeters;

            if (!isNearSurfaceNow && !couldReachSurfaceSinceLastSample)
            {
                return;
            }

            _pendingImpactVesselId = _destructionTrackedVesselId;
            _pendingImpactBodyName = string.IsNullOrEmpty(_lastTrackedBodyName)
                ? vessel.mainBody.bodyName
                : _lastTrackedBodyName;
            _pendingImpactUniversalTime = currentUniversalTime;
        }

        private static double GetSurfaceClearanceMeters(Vessel vessel)
        {
            if (vessel == null)
            {
                return double.PositiveInfinity;
            }

            double clearanceMeters = double.PositiveInfinity;
            if (vessel.heightFromTerrain >= 0.0f)
            {
                clearanceMeters = vessel.heightFromTerrain;
            }

            if (!double.IsNaN(vessel.altitude)
                && !double.IsInfinity(vessel.altitude)
                && vessel.altitude >= 0.0)
            {
                clearanceMeters = Math.Min(clearanceMeters, vessel.altitude);
            }

            return clearanceMeters;
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

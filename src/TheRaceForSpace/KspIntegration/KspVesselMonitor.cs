using System;
using System.Collections.Generic;
using TheRaceForSpace.Tracking;
using UnityEngine;

namespace TheRaceForSpace.KspIntegration
{
    /// <summary>
    /// Reads KSP vessel state and converts it into project-owned tracking snapshots.
    /// Loaded vessels use live state; unloaded vessels use persistent ProtoVessel state.
    /// </summary>
    public static class KspVesselMonitor
    {
        private static string _activeTrackingSaveFolder;
        private static Vessel _destructionTrackedVessel;
        private static Callback _destructionCallback;
        private static string _destructionTrackedVesselId;
        private static string _lastTrackedBodyName;
        private static double _lastTrackedSurfaceClearanceMeters = double.PositiveInfinity;
        private static double _lastTrackedSurfaceSpeedMetersPerSecond;
        private static FlightSituation _lastTrackedSituation = FlightSituation.Other;
        private static double _lastTrackedInFlightUniversalTime = -1.0;
        private static bool _isVesselWillDestroySubscribed;
        private static string _pendingImpactVesselId;
        private static string _pendingImpactBodyName;
        private static double _pendingImpactUniversalTime = -1.0;
        private static string _lastActiveVesselTelemetryStatus;

        /// <summary>
        /// Captures the orbiting vessels available in the current save together with the KSP
        /// universal time for that observation. Returns false while required game state is not ready.
        /// </summary>
        public static bool TryCaptureOrbitingVesselSnapshots(
            out IList<OrbitingVesselSnapshot> vesselSnapshots,
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
            // tracker behaviour so all objectives found in one refresh share one stable timestamp.
            double observationUniversalTime = Planetarium.GetUniversalTime();
            if (!IsFinite(observationUniversalTime) || observationUniversalTime < 0.0)
            {
                return false;
            }

            currentUniversalTime = observationUniversalTime;
            List<ProtoVessel> protoVessels = HighLogic.CurrentGame.flightState.protoVessels;
            var snapshots = new List<OrbitingVesselSnapshot>(protoVessels.Count);

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

                snapshots.Add(new OrbitingVesselSnapshot(
                    bodyName,
                    ConvertVesselType(vesselType),
                    Math.Max(0, crewCount)));
            }

            vesselSnapshots = snapshots;
            return true;
        }

        /// <summary>
        /// Captures only the currently controlled loaded vessel for the frequent flight-contract
        /// path. Condition-specific KSP calls are made only when the cached active-contract plan
        /// requests them; identity, situation, launch data, and coordinates remain cheap common context.
        /// </summary>
        public static bool TryCaptureActiveVesselSnapshot(
            FlightTelemetryRequirement telemetryRequirements,
            out ActiveVesselSnapshot vesselSnapshot)
        {
            vesselSnapshot = null;
            EnsureActiveTrackingSaveFolder();

            if (string.IsNullOrEmpty(_activeTrackingSaveFolder))
            {
                LogActiveVesselTelemetryStatus("blocked because KSP save identity is not ready");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (telemetryRequirements == FlightTelemetryRequirement.None)
            {
                LogActiveVesselTelemetryStatus("blocked because the active contract plan requests no telemetry fields");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (!HighLogic.LoadedSceneIsFlight)
            {
                LogActiveVesselTelemetryStatus("blocked because KSP is not reporting the Flight scene");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (FlightGlobals.ActiveVessel == null)
            {
                LogActiveVesselTelemetryStatus("blocked because KSP has no active vessel");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (Planetarium.fetch == null)
            {
                LogActiveVesselTelemetryStatus("blocked because KSP universal time is not ready");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            Vessel vessel = FlightGlobals.ActiveVessel;
            if (!vessel.loaded)
            {
                LogActiveVesselTelemetryStatus("blocked because the active vessel is not loaded");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (vessel.isEVA)
            {
                LogActiveVesselTelemetryStatus("blocked because the active vessel is an EVA Kerbal");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (vessel.mainBody == null)
            {
                LogActiveVesselTelemetryStatus("blocked because the active vessel has no main body");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (string.IsNullOrEmpty(vessel.mainBody.bodyName))
            {
                LogActiveVesselTelemetryStatus("blocked because the active vessel body name is unavailable");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            bool needsSurfaceImpact = (telemetryRequirements & FlightTelemetryRequirement.SurfaceImpact) != 0;
            bool needsAltitude = needsSurfaceImpact
                || (telemetryRequirements & FlightTelemetryRequirement.Altitude) != 0;
            bool needsSurfaceSpeed = needsSurfaceImpact
                || (telemetryRequirements & FlightTelemetryRequirement.SurfaceSpeed) != 0;
            bool needsMass = (telemetryRequirements & FlightTelemetryRequirement.Mass) != 0;
            bool needsBiome = (telemetryRequirements & FlightTelemetryRequirement.Biome) != 0;
            bool needsCrew = (telemetryRequirements & FlightTelemetryRequirement.Crew) != 0;

            double observationUniversalTime = Planetarium.GetUniversalTime();
            double altitudeMeters = needsAltitude ? vessel.altitude : 0.0;
            double surfaceSpeedMetersPerSecond = needsSurfaceSpeed ? vessel.srfSpeed : 0.0;
            double massTonnes = needsMass ? vessel.GetTotalMass() : 0.0;
            double bodyRadiusMeters = needsMass ? vessel.mainBody.Radius : 0.0;
            double latitudeDegrees = vessel.latitude;
            double longitudeDegrees = vessel.longitude;
            double launchUniversalTime = vessel.launchTime;

            // KSP normally supplies finite values, but scene transitions and damaged vessels can
            // expose transient invalid telemetry. Reject the whole sample rather than letting NaN
            // comparisons accidentally satisfy a contract or poison persisted attempt history.
            if (!IsFinite(observationUniversalTime) || observationUniversalTime < 0.0)
            {
                LogActiveVesselTelemetryStatus("blocked because KSP returned invalid universal time");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (!IsFinite(latitudeDegrees) || !IsFinite(longitudeDegrees))
            {
                LogActiveVesselTelemetryStatus("blocked because KSP returned invalid vessel coordinates");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (!IsFinite(launchUniversalTime))
            {
                LogActiveVesselTelemetryStatus("blocked because KSP returned invalid vessel launch time");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (needsAltitude && !IsFinite(altitudeMeters))
            {
                LogActiveVesselTelemetryStatus("blocked because KSP returned invalid vessel altitude");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (needsSurfaceSpeed
                && (!IsFinite(surfaceSpeedMetersPerSecond)
                    || surfaceSpeedMetersPerSecond < 0.0))
            {
                LogActiveVesselTelemetryStatus("blocked because KSP returned invalid surface speed");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (needsMass && (!IsFinite(massTonnes) || massTonnes < 0.0))
            {
                LogActiveVesselTelemetryStatus("blocked because KSP returned invalid vessel mass");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (needsMass && (!IsFinite(bodyRadiusMeters) || bodyRadiusMeters <= 0.0))
            {
                LogActiveVesselTelemetryStatus("blocked because KSP returned an invalid body radius");
                DisableActiveVesselSurfaceImpactTracking();
                return false;
            }

            if (needsSurfaceImpact)
            {
                EnsureVesselWillDestroySubscription();
                TrackActiveVesselDestruction(vessel);
                CaptureDestructionTelemetry(
                    vessel,
                    surfaceSpeedMetersPerSecond,
                    observationUniversalTime);
            }
            else
            {
                DisableActiveVesselSurfaceImpactTracking();
            }

            string biomeName = null;
            if (needsBiome
                && string.Equals(vessel.mainBody.bodyName, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                biomeName = ScienceUtil.GetExperimentBiome(
                    vessel.mainBody,
                    latitudeDegrees,
                    longitudeDegrees);
            }

            vesselSnapshot = new ActiveVesselSnapshot(
                vessel.id.ToString("D"),
                vessel.mainBody.bodyName,
                ConvertSituation(vessel.situation),
                altitudeMeters,
                surfaceSpeedMetersPerSecond,
                massTonnes,
                latitudeDegrees,
                longitudeDegrees,
                bodyRadiusMeters,
                biomeName,
                needsCrew ? vessel.GetCrewCount() : 0,
                launchUniversalTime,
                observationUniversalTime);

            LogActiveVesselTelemetryStatus(
                "captured active vessel " + vessel.id.ToString("D") + " on " + vessel.mainBody.bodyName);
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
            EnsureActiveTrackingSaveFolder();

            vesselId = _pendingImpactVesselId;
            celestialBodyName = _pendingImpactBodyName;
            impactUniversalTime = _pendingImpactUniversalTime;

            bool hasImpact = !string.IsNullOrEmpty(vesselId)
                && !string.IsNullOrEmpty(celestialBodyName)
                && IsFinite(impactUniversalTime)
                && impactUniversalTime >= 0.0;

            _pendingImpactVesselId = null;
            _pendingImpactBodyName = null;
            _pendingImpactUniversalTime = -1.0;
            return hasImpact;
        }

        /// <summary>
        /// Removes Directed Power destruction callbacks and cached crash telemetry when no active
        /// pre-orbit contract currently requires surface-impact observation.
        /// </summary>
        public static void DisableActiveVesselSurfaceImpactTracking()
        {
            DetachDestructionCallback();
            if (_isVesselWillDestroySubscribed)
            {
                // KSP can tear down the static event object before our persistent runtime leaves
                // the scene. The subscription flag still belongs to us, so clear it even when the
                // event itself is temporarily unavailable.
                if (GameEvents.onVesselWillDestroy != null)
                {
                    GameEvents.onVesselWillDestroy.Remove(OnVesselWillDestroy);
                }

                _isVesselWillDestroySubscribed = false;
            }

            _destructionTrackedVesselId = null;
            _lastTrackedBodyName = null;
            _lastTrackedSurfaceClearanceMeters = double.PositiveInfinity;
            _lastTrackedSurfaceSpeedMetersPerSecond = 0.0;
            _lastTrackedSituation = FlightSituation.Other;
            _lastTrackedInFlightUniversalTime = -1.0;
            _pendingImpactVesselId = null;
            _pendingImpactBodyName = null;
            _pendingImpactUniversalTime = -1.0;
        }

        /// <summary>
        /// Clears active-vessel callback state when a different KSP save becomes current.
        /// </summary>
        public static void ResetActiveVesselTracking()
        {
            DisableActiveVesselSurfaceImpactTracking();
            _activeTrackingSaveFolder = null;
            _lastActiveVesselTelemetryStatus = null;
        }

        private static void EnsureActiveTrackingSaveFolder()
        {
            string currentSaveFolder = HighLogic.SaveFolder;
            if (string.IsNullOrEmpty(currentSaveFolder)
                || string.Equals(
                    _activeTrackingSaveFolder,
                    currentSaveFolder,
                    StringComparison.Ordinal))
            {
                return;
            }

            // HighLogic.CurrentGame can be replaced during ordinary scene changes. Only a genuine
            // save-folder change should discard pending impact callbacks and active-flight telemetry.
            ResetActiveVesselTracking();
            _activeTrackingSaveFolder = currentSaveFolder;
        }

        private static void LogActiveVesselTelemetryStatus(string status)
        {
            if (string.Equals(
                _lastActiveVesselTelemetryStatus,
                status,
                StringComparison.Ordinal))
            {
                return;
            }

            _lastActiveVesselTelemetryStatus = status;
            Debug.Log("[TheRaceForSpace] Flight telemetry: " + status + ".");
        }

        private static void EnsureVesselWillDestroySubscription()
        {
            if (_isVesselWillDestroySubscribed || string.IsNullOrEmpty(_activeTrackingSaveFolder))
            {
                return;
            }

            // KSP can expose the active vessel before every global GameEvents object has finished
            // initializing for Flight. The vessel-specific destruction callback below is sufficient
            // for normal tracking, so defer this additional fallback instead of blocking telemetry.
            if (GameEvents.onVesselWillDestroy == null)
            {
                return;
            }

            // Vessel.OnJustAboutToBeDestroyed can be missed during some breakup sequences.
            // KSP's global event is an additional last-chance notification for the same vessel,
            // but it is subscribed only while a Directed Power contract actually needs impact data.
            GameEvents.onVesselWillDestroy.Add(OnVesselWillDestroy);
            _isVesselWillDestroySubscribed = true;
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

        private static void CaptureDestructionTelemetry(
            Vessel vessel,
            double surfaceSpeedMetersPerSecond,
            double observationUniversalTime)
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
                _lastTrackedSituation = FlightSituation.Other;
                _lastTrackedInFlightUniversalTime = -1.0;
            }

            _destructionTrackedVesselId = vesselId;
            _lastTrackedBodyName = vessel.mainBody.bodyName;

            FlightSituation currentSituation = ConvertSituation(vessel.situation);
            if (currentSituation != FlightSituation.Flying
                && currentSituation != FlightSituation.SubOrbital)
            {
                // KSP can report SPLASHED or LANDED for a fraction of a second before a violent
                // impact finishes destroying the vessel. Preserve the immediately preceding genuine
                // in-flight sample so that transition cannot erase an otherwise valid crash.
                return;
            }

            _lastTrackedSurfaceClearanceMeters = GetSurfaceClearanceMeters(vessel);
            _lastTrackedSurfaceSpeedMetersPerSecond = surfaceSpeedMetersPerSecond;
            _lastTrackedSituation = currentSituation;
            _lastTrackedInFlightUniversalTime = observationUniversalTime;
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
            // runs. Keep KSP event and vessel handling here, but delegate the normalized impact
            // decision to the KSP-independent evaluator used by the standalone regression suite.
            double currentUniversalTime = Planetarium.fetch == null
                ? -1.0
                : Planetarium.GetUniversalTime();
            if (!IsFinite(currentUniversalTime) || currentUniversalTime < 0.0)
            {
                return;
            }

            double currentSurfaceClearanceMeters = GetSurfaceClearanceMeters(vessel);
            if (!SurfaceImpactEvaluator.IsEligible(
                _lastTrackedSituation,
                _lastTrackedSurfaceClearanceMeters,
                _lastTrackedSurfaceSpeedMetersPerSecond,
                _lastTrackedInFlightUniversalTime,
                currentSurfaceClearanceMeters,
                currentUniversalTime))
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

            if (IsFinite(vessel.altitude) && vessel.altitude >= 0.0)
            {
                clearanceMeters = Math.Min(clearanceMeters, vessel.altitude);
            }

            return clearanceMeters;
        }

        private static FlightSituation ConvertSituation(Vessel.Situations situation)
        {
            switch (situation)
            {
                case Vessel.Situations.PRELAUNCH:
                    return FlightSituation.Prelaunch;
                case Vessel.Situations.FLYING:
                    return FlightSituation.Flying;
                case Vessel.Situations.SUB_ORBITAL:
                    return FlightSituation.SubOrbital;
                case Vessel.Situations.ORBITING:
                    return FlightSituation.Orbiting;
                case Vessel.Situations.LANDED:
                    return FlightSituation.Landed;
                case Vessel.Situations.SPLASHED:
                    return FlightSituation.Splashed;
                default:
                    return FlightSituation.Other;
            }
        }

        private static OrbitalVesselType ConvertVesselType(VesselType vesselType)
        {
            if (vesselType == VesselType.Probe)
            {
                return OrbitalVesselType.Probe;
            }

            if (vesselType == VesselType.Relay)
            {
                return OrbitalVesselType.Relay;
            }

            return OrbitalVesselType.Other;
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

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}

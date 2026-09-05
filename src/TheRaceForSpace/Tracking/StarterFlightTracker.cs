using System;
using System.Collections.Generic;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Agencies;

namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// Maintains one player flight attempt and records the four special starter contract lines
    /// from KSP-independent active-vessel snapshots.
    /// </summary>
    public sealed class StarterFlightTracker
    {
        private const double LaunchTimeMatchToleranceSeconds = 1.0;
        private const double MaximumContinuousSampleGapSeconds = 5.0;

        private readonly Dictionary<string, ControlContractState> _controlStates =
            new Dictionary<string, ControlContractState>(StringComparer.OrdinalIgnoreCase);

        private string _vesselId;
        private string _celestialBodyName;
        private double _launchUniversalTime = -1.0;
        private double _startLatitudeDegrees;
        private double _startLongitudeDegrees;
        private double _lastSampleUniversalTime = -1.0;
        private double _maximumAltitudeMeters;
        private double _maximumSurfaceSpeedMetersPerSecond;
        private double _currentAltitudeMeters;
        private double _currentSurfaceSpeedMetersPerSecond;
        private double _currentMassTonnes;
        private double _currentDistanceMeters;
        private string _currentBiomeName;
        private int _currentCrewCount;
        private TrackedFlightSituation _currentSituation = TrackedFlightSituation.Other;
        private bool _enteredOrbit;

        public bool HasActiveAttempt { get { return !string.IsNullOrEmpty(_vesselId); } }
        public string VesselId { get { return _vesselId; } }
        public string CelestialBodyName { get { return _celestialBodyName; } }
        public double LaunchUniversalTime { get { return _launchUniversalTime; } }
        public double StartLatitudeDegrees { get { return _startLatitudeDegrees; } }
        public double StartLongitudeDegrees { get { return _startLongitudeDegrees; } }
        public double LastSampleUniversalTime { get { return _lastSampleUniversalTime; } }
        public double MaximumAltitudeMeters { get { return _maximumAltitudeMeters; } }
        public double MaximumSurfaceSpeedMetersPerSecond { get { return _maximumSurfaceSpeedMetersPerSecond; } }
        public double CurrentAltitudeMeters { get { return _currentAltitudeMeters; } }
        public double CurrentSurfaceSpeedMetersPerSecond { get { return _currentSurfaceSpeedMetersPerSecond; } }
        public double CurrentMassTonnes { get { return _currentMassTonnes; } }
        public double CurrentDistanceMeters { get { return _currentDistanceMeters; } }
        public string CurrentBiomeName { get { return _currentBiomeName; } }
        public int CurrentCrewCount { get { return _currentCrewCount; } }
        public TrackedFlightSituation CurrentSituation { get { return _currentSituation; } }
        public bool EnteredOrbit { get { return _enteredOrbit; } }

        // Persistence captures this live key collection directly, avoiding a temporary list allocation
        // on the once-per-second starter-state capture path.
        internal ICollection<string> ControlStateObjectiveIds { get { return _controlStates.Keys; } }

        /// <summary>
        /// Returns the accumulated continuous hold time for one active Control contract.
        /// </summary>
        public double GetControlHoldSeconds(string objectiveId)
        {
            ControlContractState state;
            return !string.IsNullOrEmpty(objectiveId)
                && _controlStates.TryGetValue(objectiveId, out state)
                ? state.HoldSeconds
                : 0.0;
        }

        /// <summary>
        /// Returns whether one Control contract has completed its altitude hold and is waiting
        /// for the required crewed Kerbin landing.
        /// </summary>
        public bool IsControlMilestoneQualified(string objectiveId)
        {
            ControlContractState state;
            return !string.IsNullOrEmpty(objectiveId)
                && _controlStates.TryGetValue(objectiveId, out state)
                && state.IsQualified;
        }

        /// <summary>
        /// Returns whether the most recent observed sample was inside one Control contract's band.
        /// </summary>
        public bool IsControlSampleInBand(string objectiveId)
        {
            ControlContractState state;
            return !string.IsNullOrEmpty(objectiveId)
                && _controlStates.TryGetValue(objectiveId, out state)
                && state.WasSampleInBand;
        }

        /// <summary>
        /// Restores one persisted Control contract state after the common flight attempt has been
        /// restored. Invalid values are ignored so malformed save data cannot invent hold progress.
        /// </summary>
        internal void RestoreControlState(
            string objectiveId,
            double holdSeconds,
            bool wasSampleInBand,
            bool isQualified)
        {
            if (string.IsNullOrEmpty(objectiveId)
                || !IsFinite(holdSeconds)
                || holdSeconds < 0.0)
            {
                return;
            }

            ControlContractState state = GetOrCreateControlState(objectiveId);
            state.HoldSeconds = holdSeconds;
            state.WasSampleInBand = wasSampleInBand;
            state.IsQualified = isQualified;
        }

        /// <summary>
        /// Applies one active-vessel observation against the supplied active starter-contract set.
        /// Every supplied Mass, Biome, and Control contract is evaluated independently so one flight
        /// may satisfy multiple separately offered levels.
        /// </summary>
        public bool RefreshPlayerMilestones(
            AgencyState playerAgency,
            IList<ObjectiveDefinition> starterMilestones,
            ActiveVesselTrackingSnapshot snapshot)
        {
            if (playerAgency == null
                || starterMilestones == null
                || snapshot == null
                || string.IsNullOrEmpty(snapshot.VesselId)
                || string.IsNullOrEmpty(snapshot.CelestialBodyName))
            {
                return false;
            }

            if (!IsSameAttempt(snapshot))
            {
                BeginAttempt(snapshot);
            }
            else
            {
                // Staging can replace the active Vessel object while preserving the launch time.
                // Keep the mission history but follow the newly controlled vessel ID for impact detection.
                _vesselId = snapshot.VesselId;
            }

            double sampleDeltaSeconds = 0.0;
            if (_lastSampleUniversalTime >= 0.0
                && snapshot.ObservationUniversalTime >= _lastSampleUniversalTime)
            {
                sampleDeltaSeconds = snapshot.ObservationUniversalTime - _lastSampleUniversalTime;
            }

            // Each Control contract is explicitly a continuous hold. A large gap means the vessel
            // was not observed closely enough to prove any unqualified hold continued throughout
            // the missing time. Qualified contracts keep their completed hold while awaiting landing.
            if (sampleDeltaSeconds > MaximumContinuousSampleGapSeconds)
            {
                sampleDeltaSeconds = 0.0;
                ResetUnqualifiedControlStates();
            }

            _currentAltitudeMeters = snapshot.AltitudeMeters;
            _currentSurfaceSpeedMetersPerSecond = Math.Max(0.0, snapshot.SurfaceSpeedMetersPerSecond);
            _currentMassTonnes = Math.Max(0.0, snapshot.MassTonnes);
            _currentDistanceMeters = CalculateSurfaceDistanceMeters(snapshot);
            _currentBiomeName = snapshot.BiomeName;
            _currentCrewCount = Math.Max(0, snapshot.CrewCount);
            _currentSituation = snapshot.Situation;

            _maximumAltitudeMeters = Math.Max(_maximumAltitudeMeters, snapshot.AltitudeMeters);
            _maximumSurfaceSpeedMetersPerSecond = Math.Max(
                _maximumSurfaceSpeedMetersPerSecond,
                snapshot.SurfaceSpeedMetersPerSecond);

            if (snapshot.Situation == TrackedFlightSituation.Orbiting)
            {
                _enteredOrbit = true;
            }

            bool recordedAchievement = false;
            bool isKerbin = string.Equals(
                snapshot.CelestialBodyName,
                "Kerbin",
                StringComparison.OrdinalIgnoreCase);

            if (isKerbin && !_enteredOrbit)
            {
                // The controller already filtered this collection to Offered, unexpired contracts
                // the player has not completed. Evaluate each supplied definition on its own terms
                // so earlier and later offered levels remain genuinely independent.
                for (int milestoneIndex = 0; milestoneIndex < starterMilestones.Count; milestoneIndex++)
                {
                    ObjectiveDefinition objective = starterMilestones[milestoneIndex];
                    if (objective == null || playerAgency.HasCompletedObjective(objective.Id))
                    {
                        continue;
                    }

                    if (objective.PreOrbitLine == PreOrbitContractLine.Mass
                        && snapshot.Situation == TrackedFlightSituation.Landed
                        && snapshot.MassTonnes >= objective.RequiredMassTonnes
                        && _currentDistanceMeters >= objective.RequiredDistanceMeters)
                    {
                        // Mass represents delivery of a finished craft, so the final landed vessel
                        // must still meet both the mass and distance requirement for this contract.
                        recordedAchievement |= playerAgency.RecordObjectiveCompletion(
                            objective.Id,
                            snapshot.ObservationUniversalTime);
                        continue;
                    }

                    if (objective.PreOrbitLine == PreOrbitContractLine.Biome
                        && snapshot.Situation == TrackedFlightSituation.Landed
                        && !string.IsNullOrEmpty(snapshot.BiomeName)
                        && string.Equals(
                            snapshot.BiomeName,
                            objective.RequiredBiomeName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        // Flying over a biome is not enough; this individual contract completes
                        // only when the active craft finishes landed in its requested biome.
                        recordedAchievement |= playerAgency.RecordObjectiveCompletion(
                            objective.Id,
                            snapshot.ObservationUniversalTime);
                    }
                }

                recordedAchievement |= EvaluateControlMilestones(
                    playerAgency,
                    starterMilestones,
                    snapshot,
                    sampleDeltaSeconds);
            }
            else
            {
                ResetUnqualifiedControlStates();
            }

            _lastSampleUniversalTime = snapshot.ObservationUniversalTime;
            return recordedAchievement;
        }

        /// <summary>
        /// Consumes a KSP integration signal that the tracked active vessel was destroyed at the
        /// Kerbin surface. Every supplied active Directed Power contract is evaluated independently
        /// against the same completed flight history before the destroyed attempt is cleared.
        /// </summary>
        public bool RecordSurfaceImpact(
            AgencyState playerAgency,
            IList<ObjectiveDefinition> starterMilestones,
            string vesselId,
            string celestialBodyName,
            double impactUniversalTime)
        {
            if (playerAgency == null
                || starterMilestones == null
                || !HasActiveAttempt
                || string.IsNullOrEmpty(vesselId)
                || !string.Equals(_vesselId, vesselId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool recordedAchievement = false;
            if (!_enteredOrbit
                && string.Equals(celestialBodyName, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                for (int milestoneIndex = 0; milestoneIndex < starterMilestones.Count; milestoneIndex++)
                {
                    ObjectiveDefinition objective = starterMilestones[milestoneIndex];
                    if (objective == null
                        || objective.PreOrbitLine != PreOrbitContractLine.DirectedPower
                        || playerAgency.HasCompletedObjective(objective.Id)
                        || _maximumAltitudeMeters > objective.MaximumAltitudeMeters
                        || _maximumSurfaceSpeedMetersPerSecond
                            < objective.RequiredSpeedMetersPerSecond)
                    {
                        continue;
                    }

                    recordedAchievement |= playerAgency.RecordObjectiveCompletion(
                        objective.Id,
                        impactUniversalTime);
                }
            }

            ClearAttempt();
            return recordedAchievement;
        }

        /// <summary>
        /// Restores the common historical fields for one persisted starter-flight attempt. Current
        /// Control states are restored separately by stable contract ID through RestoreControlState.
        /// </summary>
        public void RestoreState(
            string vesselId,
            string celestialBodyName,
            double launchUniversalTime,
            double startLatitudeDegrees,
            double startLongitudeDegrees,
            double lastSampleUniversalTime,
            double maximumAltitudeMeters,
            double maximumSurfaceSpeedMetersPerSecond,
            bool enteredOrbit)
        {
            if (string.IsNullOrEmpty(vesselId)
                || string.IsNullOrEmpty(celestialBodyName)
                || !IsFinite(launchUniversalTime)
                || launchUniversalTime < 0.0
                || !IsFinite(startLatitudeDegrees)
                || startLatitudeDegrees < -90.0
                || startLatitudeDegrees > 90.0
                || !IsFinite(startLongitudeDegrees)
                || !IsFinite(lastSampleUniversalTime)
                || lastSampleUniversalTime < 0.0
                || !IsFinite(maximumAltitudeMeters)
                || maximumAltitudeMeters < 0.0
                || !IsFinite(maximumSurfaceSpeedMetersPerSecond)
                || maximumSurfaceSpeedMetersPerSecond < 0.0)
            {
                ClearAttempt();
                return;
            }

            // Instantaneous telemetry is intentionally not persisted. Clear any previous live
            // values before applying historical state so a scene/save restore cannot display stale data.
            _currentAltitudeMeters = 0.0;
            _currentSurfaceSpeedMetersPerSecond = 0.0;
            _currentMassTonnes = 0.0;
            _currentDistanceMeters = 0.0;
            _currentBiomeName = null;
            _currentCrewCount = 0;
            _currentSituation = TrackedFlightSituation.Other;

            _vesselId = vesselId;
            _celestialBodyName = celestialBodyName;
            _launchUniversalTime = launchUniversalTime;
            _startLatitudeDegrees = startLatitudeDegrees;
            _startLongitudeDegrees = startLongitudeDegrees;
            _lastSampleUniversalTime = lastSampleUniversalTime;
            _maximumAltitudeMeters = maximumAltitudeMeters;
            _maximumSurfaceSpeedMetersPerSecond = maximumSurfaceSpeedMetersPerSecond;
            _enteredOrbit = enteredOrbit;
            _controlStates.Clear();
        }

        public void ClearAttempt()
        {
            _vesselId = null;
            _celestialBodyName = null;
            _launchUniversalTime = -1.0;
            _startLatitudeDegrees = 0.0;
            _startLongitudeDegrees = 0.0;
            _lastSampleUniversalTime = -1.0;
            _maximumAltitudeMeters = 0.0;
            _maximumSurfaceSpeedMetersPerSecond = 0.0;
            _currentAltitudeMeters = 0.0;
            _currentSurfaceSpeedMetersPerSecond = 0.0;
            _currentMassTonnes = 0.0;
            _currentDistanceMeters = 0.0;
            _currentBiomeName = null;
            _currentCrewCount = 0;
            _currentSituation = TrackedFlightSituation.Other;
            _controlStates.Clear();
            _enteredOrbit = false;
        }

        private bool EvaluateControlMilestones(
            AgencyState playerAgency,
            IList<ObjectiveDefinition> starterMilestones,
            ActiveVesselTrackingSnapshot snapshot,
            double sampleDeltaSeconds)
        {
            bool recordedAchievement = false;

            for (int milestoneIndex = 0; milestoneIndex < starterMilestones.Count; milestoneIndex++)
            {
                ObjectiveDefinition controlMilestone = starterMilestones[milestoneIndex];
                if (controlMilestone == null
                    || controlMilestone.PreOrbitLine != PreOrbitContractLine.Control
                    || playerAgency.HasCompletedObjective(controlMilestone.Id))
                {
                    continue;
                }

                ControlContractState state = GetOrCreateControlState(controlMilestone.Id);
                if (state.IsQualified
                    && snapshot.Situation == TrackedFlightSituation.Landed
                    && snapshot.CrewCount > 0)
                {
                    recordedAchievement |= playerAgency.RecordObjectiveCompletion(
                        controlMilestone.Id,
                        snapshot.ObservationUniversalTime);
                    continue;
                }

                bool isInBand = snapshot.CrewCount > 0
                    && snapshot.AltitudeMeters >= controlMilestone.MinimumAltitudeMeters
                    && snapshot.AltitudeMeters <= controlMilestone.MaximumAltitudeMeters;
                if (!isInBand)
                {
                    state.WasSampleInBand = false;
                    if (!state.IsQualified)
                    {
                        state.HoldSeconds = 0.0;
                    }
                    continue;
                }

                if (state.WasSampleInBand && sampleDeltaSeconds > 0.0)
                {
                    state.HoldSeconds += sampleDeltaSeconds;
                }

                state.WasSampleInBand = true;
                if (state.HoldSeconds >= controlMilestone.RequiredDurationSeconds)
                {
                    state.IsQualified = true;
                }
            }

            return recordedAchievement;
        }

        private ControlContractState GetOrCreateControlState(string objectiveId)
        {
            ControlContractState state;
            if (_controlStates.TryGetValue(objectiveId, out state))
            {
                return state;
            }

            state = new ControlContractState();
            _controlStates.Add(objectiveId, state);
            return state;
        }

        private void ResetUnqualifiedControlStates()
        {
            foreach (KeyValuePair<string, ControlContractState> entry in _controlStates)
            {
                ControlContractState state = entry.Value;
                if (state == null || state.IsQualified)
                {
                    continue;
                }

                state.HoldSeconds = 0.0;
                state.WasSampleInBand = false;
            }
        }

        private bool IsSameAttempt(ActiveVesselTrackingSnapshot snapshot)
        {
            if (!HasActiveAttempt)
            {
                return false;
            }

            if (string.Equals(_vesselId, snapshot.VesselId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // KSP can assign a new vessel ID to a separated stage. Shared launch time and body
            // provide a narrow continuation rule without treating an unrelated later launch as
            // the same contract attempt.
            return _launchUniversalTime >= 0.0
                && snapshot.LaunchUniversalTime >= 0.0
                && Math.Abs(_launchUniversalTime - snapshot.LaunchUniversalTime)
                    <= LaunchTimeMatchToleranceSeconds
                && string.Equals(
                    _celestialBodyName,
                    snapshot.CelestialBodyName,
                    StringComparison.OrdinalIgnoreCase);
        }

        private void BeginAttempt(ActiveVesselTrackingSnapshot snapshot)
        {
            ClearAttempt();
            _vesselId = snapshot.VesselId;
            _celestialBodyName = snapshot.CelestialBodyName;
            _launchUniversalTime = snapshot.LaunchUniversalTime;
            _startLatitudeDegrees = snapshot.LatitudeDegrees;
            _startLongitudeDegrees = snapshot.LongitudeDegrees;
            _lastSampleUniversalTime = snapshot.ObservationUniversalTime;
            _maximumAltitudeMeters = Math.Max(0.0, snapshot.AltitudeMeters);
            _maximumSurfaceSpeedMetersPerSecond = Math.Max(
                0.0,
                snapshot.SurfaceSpeedMetersPerSecond);
            _enteredOrbit = snapshot.Situation == TrackedFlightSituation.Orbiting;
        }

        private double CalculateSurfaceDistanceMeters(ActiveVesselTrackingSnapshot snapshot)
        {
            if (snapshot.BodyRadiusMeters <= 0.0)
            {
                return 0.0;
            }

            double degreesToRadians = Math.PI / 180.0;
            double startLatitude = _startLatitudeDegrees * degreesToRadians;
            double endLatitude = snapshot.LatitudeDegrees * degreesToRadians;
            double latitudeDifference = (snapshot.LatitudeDegrees - _startLatitudeDegrees)
                * degreesToRadians;
            double longitudeDifference = (snapshot.LongitudeDegrees - _startLongitudeDegrees)
                * degreesToRadians;

            double sinHalfLatitude = Math.Sin(latitudeDifference * 0.5);
            double sinHalfLongitude = Math.Sin(longitudeDifference * 0.5);
            double haversine = sinHalfLatitude * sinHalfLatitude
                + Math.Cos(startLatitude)
                * Math.Cos(endLatitude)
                * sinHalfLongitude
                * sinHalfLongitude;
            haversine = Math.Max(0.0, Math.Min(1.0, haversine));

            double centralAngle = 2.0 * Math.Atan2(
                Math.Sqrt(haversine),
                Math.Sqrt(1.0 - haversine));
            return snapshot.BodyRadiusMeters * centralAngle;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private sealed class ControlContractState
        {
            public double HoldSeconds;
            public bool WasSampleInBand;
            public bool IsQualified;
        }
    }
}

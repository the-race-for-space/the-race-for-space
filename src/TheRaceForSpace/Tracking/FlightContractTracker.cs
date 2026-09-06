using System;
using System.Collections.Generic;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Agencies;

namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// Evaluates one player flight attempt and records the four special pre-orbit contract lines
    /// from KSP-independent active-vessel snapshots. Mutable attempt state is held separately by
    /// FlightAttemptState so later work can add multiple remembered attempts without mixing state
    /// ownership into the contract evaluation rules.
    /// </summary>
    public sealed class FlightContractTracker
    {
        private const double LaunchTimeMatchToleranceSeconds = 1.0;
        private const double MaximumContinuousSampleGapSeconds = 5.0;

        private readonly FlightAttemptState _attempt = new FlightAttemptState();

        public bool HasActiveAttempt { get { return _attempt.HasActiveAttempt; } }
        public string VesselId { get { return _attempt.VesselId; } }
        public string CelestialBodyName { get { return _attempt.CelestialBodyName; } }
        public double LaunchUniversalTime { get { return _attempt.LaunchUniversalTime; } }
        public double StartLatitudeDegrees { get { return _attempt.StartLatitudeDegrees; } }
        public double StartLongitudeDegrees { get { return _attempt.StartLongitudeDegrees; } }
        public double LastSampleUniversalTime { get { return _attempt.LastSampleUniversalTime; } }
        public double MaximumAltitudeMeters { get { return _attempt.MaximumAltitudeMeters; } }
        public double MaximumSurfaceSpeedMetersPerSecond
        {
            get { return _attempt.MaximumSurfaceSpeedMetersPerSecond; }
        }
        public double CurrentAltitudeMeters { get { return _attempt.CurrentAltitudeMeters; } }
        public double CurrentSurfaceSpeedMetersPerSecond
        {
            get { return _attempt.CurrentSurfaceSpeedMetersPerSecond; }
        }
        public double CurrentMassTonnes { get { return _attempt.CurrentMassTonnes; } }
        public double CurrentDistanceMeters { get { return _attempt.CurrentDistanceMeters; } }
        public string CurrentBiomeName { get { return _attempt.CurrentBiomeName; } }
        public int CurrentCrewCount { get { return _attempt.CurrentCrewCount; } }
        public FlightSituation CurrentSituation { get { return _attempt.CurrentSituation; } }
        public bool EnteredOrbit { get { return _attempt.EnteredOrbit; } }

        internal ICollection<string> ControlStateObjectiveIds
        {
            get { return _attempt.ControlStateObjectiveIds; }
        }

        /// <summary>
        /// Returns the accumulated continuous hold time for one active Control contract.
        /// </summary>
        public double GetControlHoldSeconds(string objectiveId)
        {
            return _attempt.GetControlHoldSeconds(objectiveId);
        }

        /// <summary>
        /// Returns whether one Control contract has completed its altitude hold and is waiting
        /// for the required crewed Kerbin landing or splashdown.
        /// </summary>
        public bool IsControlObjectiveQualified(string objectiveId)
        {
            return _attempt.IsControlObjectiveQualified(objectiveId);
        }

        /// <summary>
        /// Returns whether the most recent observed sample was inside one Control contract's band.
        /// </summary>
        public bool IsControlSampleInBand(string objectiveId)
        {
            return _attempt.IsControlSampleInBand(objectiveId);
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

            _attempt.RestoreControlState(
                objectiveId,
                holdSeconds,
                wasSampleInBand,
                isQualified);
        }

        /// <summary>
        /// Applies one active-vessel observation against the supplied active flight-contract set.
        /// Every supplied Mass, Biome, and Control contract is evaluated independently so one flight
        /// may satisfy multiple separately offered levels.
        /// </summary>
        public bool EvaluateActiveFlightContracts(
            AgencyState playerAgency,
            IList<ObjectiveDefinition> activeFlightContracts,
            ActiveVesselSnapshot snapshot)
        {
            if (playerAgency == null
                || activeFlightContracts == null
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
                _attempt.VesselId = snapshot.VesselId;
            }

            double sampleDeltaSeconds = 0.0;
            if (_attempt.LastSampleUniversalTime >= 0.0
                && snapshot.ObservationUniversalTime >= _attempt.LastSampleUniversalTime)
            {
                sampleDeltaSeconds = snapshot.ObservationUniversalTime
                    - _attempt.LastSampleUniversalTime;
            }

            // Each Control contract is explicitly a continuous hold. A large gap means the vessel
            // was not observed closely enough to prove any unqualified hold continued throughout
            // the missing time. Qualified contracts keep their completed hold while awaiting landing.
            if (sampleDeltaSeconds > MaximumContinuousSampleGapSeconds)
            {
                sampleDeltaSeconds = 0.0;
                ResetUnqualifiedControlStates();
            }

            _attempt.CurrentAltitudeMeters = snapshot.AltitudeMeters;
            _attempt.CurrentSurfaceSpeedMetersPerSecond = Math.Max(
                0.0,
                snapshot.SurfaceSpeedMetersPerSecond);
            _attempt.CurrentMassTonnes = Math.Max(0.0, snapshot.MassTonnes);
            _attempt.CurrentDistanceMeters = CalculateSurfaceDistanceMeters(snapshot);
            _attempt.CurrentBiomeName = snapshot.BiomeName;
            _attempt.CurrentCrewCount = Math.Max(0, snapshot.CrewCount);
            _attempt.CurrentSituation = snapshot.Situation;

            _attempt.MaximumAltitudeMeters = Math.Max(
                _attempt.MaximumAltitudeMeters,
                snapshot.AltitudeMeters);
            _attempt.MaximumSurfaceSpeedMetersPerSecond = Math.Max(
                _attempt.MaximumSurfaceSpeedMetersPerSecond,
                snapshot.SurfaceSpeedMetersPerSecond);

            if (snapshot.Situation == FlightSituation.Orbiting)
            {
                _attempt.EnteredOrbit = true;
            }

            bool recordedObjective = false;
            bool isKerbin = string.Equals(
                snapshot.CelestialBodyName,
                "Kerbin",
                StringComparison.OrdinalIgnoreCase);

            if (isKerbin && !_attempt.EnteredOrbit)
            {
                // The controller already filtered this collection to Offered, unexpired contracts
                // the player has not completed. Evaluate each supplied definition on its own terms
                // so earlier and later offered levels remain genuinely independent.
                for (int objectiveIndex = 0; objectiveIndex < activeFlightContracts.Count; objectiveIndex++)
                {
                    ObjectiveDefinition objective = activeFlightContracts[objectiveIndex];
                    if (objective == null || playerAgency.HasCompletedObjective(objective.Id))
                    {
                        continue;
                    }

                    if (objective.PreOrbitLine == PreOrbitContractLine.Mass
                        && IsLandedOrSplashed(snapshot.Situation)
                        && snapshot.MassTonnes >= objective.RequiredMassTonnes
                        && _attempt.CurrentDistanceMeters >= objective.RequiredDistanceMeters)
                    {
                        // Mass represents delivery of a finished craft, so the final recovered vessel
                        // must still meet both the mass and distance requirement for this contract.
                        recordedObjective |= playerAgency.RecordObjectiveCompletion(
                            objective.Id,
                            snapshot.ObservationUniversalTime);
                        continue;
                    }

                    if (objective.PreOrbitLine == PreOrbitContractLine.Biome
                        && IsLandedOrSplashed(snapshot.Situation)
                        && !string.IsNullOrEmpty(snapshot.BiomeName)
                        && string.Equals(
                            snapshot.BiomeName,
                            objective.RequiredBiomeName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        // Flying over a biome is not enough; this individual contract completes only
                        // when the active craft finishes landed or splashed in its requested biome.
                        recordedObjective |= playerAgency.RecordObjectiveCompletion(
                            objective.Id,
                            snapshot.ObservationUniversalTime);
                    }
                }

                recordedObjective |= EvaluateControlObjectives(
                    playerAgency,
                    activeFlightContracts,
                    snapshot,
                    sampleDeltaSeconds);
            }
            else
            {
                ResetUnqualifiedControlStates();
            }

            _attempt.LastSampleUniversalTime = snapshot.ObservationUniversalTime;
            return recordedObjective;
        }

        /// <summary>
        /// Consumes a KSP integration signal that the tracked active vessel was destroyed at the
        /// Kerbin surface. Every supplied active Directed Power contract is evaluated independently
        /// against the same completed flight history before the destroyed attempt is cleared.
        /// </summary>
        public bool RecordSurfaceImpact(
            AgencyState playerAgency,
            IList<ObjectiveDefinition> activeFlightContracts,
            string vesselId,
            string celestialBodyName,
            double impactUniversalTime)
        {
            if (playerAgency == null
                || activeFlightContracts == null
                || !HasActiveAttempt
                || string.IsNullOrEmpty(vesselId)
                || !string.Equals(
                    _attempt.VesselId,
                    vesselId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool recordedObjective = false;
            if (!_attempt.EnteredOrbit
                && string.Equals(celestialBodyName, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                for (int objectiveIndex = 0; objectiveIndex < activeFlightContracts.Count; objectiveIndex++)
                {
                    ObjectiveDefinition objective = activeFlightContracts[objectiveIndex];
                    if (objective == null
                        || objective.PreOrbitLine != PreOrbitContractLine.DirectedPower
                        || playerAgency.HasCompletedObjective(objective.Id)
                        || _attempt.MaximumAltitudeMeters > objective.MaximumAltitudeMeters
                        || _attempt.MaximumSurfaceSpeedMetersPerSecond
                            < objective.RequiredSpeedMetersPerSecond)
                    {
                        continue;
                    }

                    recordedObjective |= playerAgency.RecordObjectiveCompletion(
                        objective.Id,
                        impactUniversalTime);
                }
            }

            ClearAttempt();
            return recordedObjective;
        }

        /// <summary>
        /// Restores the common historical fields for one persisted flight-contract attempt. Current
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

            // Clear all live values and prior Control state before applying the persisted history.
            // Instantaneous telemetry is rebuilt from the next active-vessel sample as before.
            _attempt.Clear();
            _attempt.VesselId = vesselId;
            _attempt.CelestialBodyName = celestialBodyName;
            _attempt.LaunchUniversalTime = launchUniversalTime;
            _attempt.StartLatitudeDegrees = startLatitudeDegrees;
            _attempt.StartLongitudeDegrees = startLongitudeDegrees;
            _attempt.LastSampleUniversalTime = lastSampleUniversalTime;
            _attempt.MaximumAltitudeMeters = maximumAltitudeMeters;
            _attempt.MaximumSurfaceSpeedMetersPerSecond = maximumSurfaceSpeedMetersPerSecond;
            _attempt.EnteredOrbit = enteredOrbit;
        }

        public void ClearAttempt()
        {
            _attempt.Clear();
        }

        private bool EvaluateControlObjectives(
            AgencyState playerAgency,
            IList<ObjectiveDefinition> activeFlightContracts,
            ActiveVesselSnapshot snapshot,
            double sampleDeltaSeconds)
        {
            bool recordedObjective = false;

            for (int objectiveIndex = 0; objectiveIndex < activeFlightContracts.Count; objectiveIndex++)
            {
                ObjectiveDefinition controlObjective = activeFlightContracts[objectiveIndex];
                if (controlObjective == null
                    || controlObjective.PreOrbitLine != PreOrbitContractLine.Control
                    || playerAgency.HasCompletedObjective(controlObjective.Id))
                {
                    continue;
                }

                FlightAttemptState.ControlContractState state =
                    _attempt.GetOrCreateControlState(controlObjective.Id);
                if (state.IsQualified
                    && IsLandedOrSplashed(snapshot.Situation)
                    && snapshot.CrewCount > 0)
                {
                    recordedObjective |= playerAgency.RecordObjectiveCompletion(
                        controlObjective.Id,
                        snapshot.ObservationUniversalTime);
                    continue;
                }

                bool isInBand = snapshot.CrewCount > 0
                    && snapshot.AltitudeMeters >= controlObjective.MinimumAltitudeMeters
                    && snapshot.AltitudeMeters <= controlObjective.MaximumAltitudeMeters;
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
                if (state.HoldSeconds >= controlObjective.RequiredDurationSeconds)
                {
                    state.IsQualified = true;
                }
            }

            return recordedObjective;
        }

        private void ResetUnqualifiedControlStates()
        {
            _attempt.ResetUnqualifiedControlStates();
        }

        private bool IsSameAttempt(ActiveVesselSnapshot snapshot)
        {
            if (!HasActiveAttempt)
            {
                return false;
            }

            if (string.Equals(
                _attempt.VesselId,
                snapshot.VesselId,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // KSP can assign a new vessel ID to a separated stage. Shared launch time and body
            // provide a narrow continuation rule without treating an unrelated later launch as
            // the same contract attempt.
            return _attempt.LaunchUniversalTime >= 0.0
                && snapshot.LaunchUniversalTime >= 0.0
                && Math.Abs(_attempt.LaunchUniversalTime - snapshot.LaunchUniversalTime)
                    <= LaunchTimeMatchToleranceSeconds
                && string.Equals(
                    _attempt.CelestialBodyName,
                    snapshot.CelestialBodyName,
                    StringComparison.OrdinalIgnoreCase);
        }

        private void BeginAttempt(ActiveVesselSnapshot snapshot)
        {
            _attempt.Clear();
            _attempt.VesselId = snapshot.VesselId;
            _attempt.CelestialBodyName = snapshot.CelestialBodyName;
            _attempt.LaunchUniversalTime = snapshot.LaunchUniversalTime;
            _attempt.StartLatitudeDegrees = snapshot.LatitudeDegrees;
            _attempt.StartLongitudeDegrees = snapshot.LongitudeDegrees;
            _attempt.LastSampleUniversalTime = snapshot.ObservationUniversalTime;
            _attempt.MaximumAltitudeMeters = Math.Max(0.0, snapshot.AltitudeMeters);
            _attempt.MaximumSurfaceSpeedMetersPerSecond = Math.Max(
                0.0,
                snapshot.SurfaceSpeedMetersPerSecond);
            _attempt.EnteredOrbit = snapshot.Situation == FlightSituation.Orbiting;
        }

        private double CalculateSurfaceDistanceMeters(ActiveVesselSnapshot snapshot)
        {
            if (snapshot.BodyRadiusMeters <= 0.0)
            {
                return 0.0;
            }

            double degreesToRadians = Math.PI / 180.0;
            double startLatitude = _attempt.StartLatitudeDegrees * degreesToRadians;
            double endLatitude = snapshot.LatitudeDegrees * degreesToRadians;
            double latitudeDifference = (snapshot.LatitudeDegrees - _attempt.StartLatitudeDegrees)
                * degreesToRadians;
            double longitudeDifference = (snapshot.LongitudeDegrees - _attempt.StartLongitudeDegrees)
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

        private static bool IsLandedOrSplashed(FlightSituation situation)
        {
            return situation == FlightSituation.Landed || situation == FlightSituation.Splashed;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}

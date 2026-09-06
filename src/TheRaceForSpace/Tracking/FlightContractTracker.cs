using System;
using System.Collections.Generic;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Agencies;

namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// Evaluates active player flight attempts and records the four special pre-orbit contract lines
    /// from KSP-independent active-vessel snapshots. Mutable state is retained per remembered craft
    /// so switching vessels during the same session does not discard unfinished contract history.
    /// </summary>
    public sealed class FlightContractTracker
    {
        private const double LaunchTimeMatchToleranceSeconds = 1.0;
        private const double MaximumContinuousSampleGapSeconds = 5.0;

        // The list is the authoritative in-memory attempt collection. Vessel ID remains a fast lookup
        // for normal samples, while persistent part lineage can recover an attempt after KSP changes
        // vessel identity during staging or other topology changes.
        private readonly List<FlightAttemptState> _attempts = new List<FlightAttemptState>();
        private readonly Dictionary<string, FlightAttemptState> _attemptsByVesselId =
            new Dictionary<string, FlightAttemptState>(StringComparer.OrdinalIgnoreCase);

        private FlightAttemptState _attempt = new FlightAttemptState();

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

            SelectAttempt(snapshot);

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
        /// Consumes a KSP integration signal that a remembered vessel was destroyed at the Kerbin
        /// surface. Only that vessel's attempt is evaluated and removed; other remembered craft keep
        /// their unfinished histories.
        /// </summary>
        public bool RecordSurfaceImpact(
            AgencyState playerAgency,
            IList<ObjectiveDefinition> activeFlightContracts,
            string vesselId,
            string celestialBodyName,
            double impactUniversalTime)
        {
            FlightAttemptState impactedAttempt;
            if (playerAgency == null
                || activeFlightContracts == null
                || string.IsNullOrEmpty(vesselId)
                || !_attemptsByVesselId.TryGetValue(vesselId, out impactedAttempt))
            {
                return false;
            }

            bool recordedObjective = false;
            if (!impactedAttempt.EnteredOrbit
                && string.Equals(celestialBodyName, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                for (int objectiveIndex = 0; objectiveIndex < activeFlightContracts.Count; objectiveIndex++)
                {
                    ObjectiveDefinition objective = activeFlightContracts[objectiveIndex];
                    if (objective == null
                        || objective.PreOrbitLine != PreOrbitContractLine.DirectedPower
                        || playerAgency.HasCompletedObjective(objective.Id)
                        || impactedAttempt.MaximumAltitudeMeters > objective.MaximumAltitudeMeters
                        || impactedAttempt.MaximumSurfaceSpeedMetersPerSecond
                            < objective.RequiredSpeedMetersPerSecond)
                    {
                        continue;
                    }

                    recordedObjective |= playerAgency.RecordObjectiveCompletion(
                        objective.Id,
                        impactUniversalTime);
                }
            }

            RemoveAttempt(impactedAttempt);
            return recordedObjective;
        }

        /// <summary>
        /// Restores the common historical fields for the one attempt supported by the current save
        /// format. In-memory attempts are replaced because persistence is authoritative on load.
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
                ClearAllAttempts();
                return;
            }

            ClearAllAttempts();
            var restoredAttempt = new FlightAttemptState
            {
                VesselId = vesselId,
                CelestialBodyName = celestialBodyName,
                LaunchUniversalTime = launchUniversalTime,
                StartLatitudeDegrees = startLatitudeDegrees,
                StartLongitudeDegrees = startLongitudeDegrees,
                LastSampleUniversalTime = lastSampleUniversalTime,
                MaximumAltitudeMeters = maximumAltitudeMeters,
                MaximumSurfaceSpeedMetersPerSecond = maximumSurfaceSpeedMetersPerSecond,
                EnteredOrbit = enteredOrbit
            };

            _attempts.Add(restoredAttempt);
            _attemptsByVesselId.Add(vesselId, restoredAttempt);
            _attempt = restoredAttempt;
        }

        /// <summary>
        /// Clears only the currently selected attempt. Other remembered craft remain available for
        /// later vessel switching during the same session.
        /// </summary>
        public void ClearAttempt()
        {
            RemoveAttempt(_attempt);
        }

        /// <summary>
        /// Clears all remembered in-memory attempts before applying persisted state.
        /// </summary>
        internal void ClearAllAttempts()
        {
            _attempts.Clear();
            _attemptsByVesselId.Clear();
            _attempt = new FlightAttemptState();
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

        private void SelectAttempt(ActiveVesselSnapshot snapshot)
        {
            bool snapshotHasLineage = snapshot.PartPersistentIds != null
                && snapshot.PartPersistentIds.Count > 0;

            FlightAttemptState selectedAttempt;
            if (_attemptsByVesselId.TryGetValue(snapshot.VesselId, out selectedAttempt))
            {
                bool lineageCompatible = !snapshotHasLineage
                    || !selectedAttempt.HasPartLineage
                    || selectedAttempt.SharesPartLineage(snapshot.PartPersistentIds);
                if (lineageCompatible)
                {
                    selectedAttempt.ReconcilePartLineage(snapshot.PartPersistentIds);
                    _attempt = selectedAttempt;
                    return;
                }

                // Vessel IDs are not permanent craft identity. If KSP reuses an ID for a vessel that
                // shares no remembered parts, drop only the cache entry and keep the old attempt alive
                // in the authoritative collection so its lineage can be found again later.
                _attemptsByVesselId.Remove(snapshot.VesselId);
            }

            selectedAttempt = snapshotHasLineage
                ? FindLineageContinuation(snapshot)
                : FindLaunchContinuation(snapshot);
            if (selectedAttempt != null)
            {
                if (!string.IsNullOrEmpty(selectedAttempt.VesselId))
                {
                    FlightAttemptState mappedAttempt;
                    if (_attemptsByVesselId.TryGetValue(
                            selectedAttempt.VesselId,
                            out mappedAttempt)
                        && ReferenceEquals(mappedAttempt, selectedAttempt))
                    {
                        _attemptsByVesselId.Remove(selectedAttempt.VesselId);
                    }
                }

                selectedAttempt.VesselId = snapshot.VesselId;
                selectedAttempt.ReconcilePartLineage(snapshot.PartPersistentIds);
                _attemptsByVesselId[snapshot.VesselId] = selectedAttempt;
                _attempt = selectedAttempt;
                return;
            }

            selectedAttempt = new FlightAttemptState();
            BeginAttempt(selectedAttempt, snapshot);
            _attempts.Add(selectedAttempt);
            _attemptsByVesselId[snapshot.VesselId] = selectedAttempt;
            _attempt = selectedAttempt;
        }

        private FlightAttemptState FindLineageContinuation(ActiveVesselSnapshot snapshot)
        {
            FlightAttemptState singleMatch = null;
            int matchCount = 0;

            for (int attemptIndex = 0; attemptIndex < _attempts.Count; attemptIndex++)
            {
                FlightAttemptState rememberedAttempt = _attempts[attemptIndex];
                if (rememberedAttempt == null
                    || !rememberedAttempt.SharesPartLineage(snapshot.PartPersistentIds))
                {
                    continue;
                }

                // If a future docked vessel contains several remembered lineages, keep the attempt
                // the player was already controlling rather than arbitrarily merging histories.
                // Full multi-lineage docking/undocking reconciliation is Step 5.
                if (ReferenceEquals(rememberedAttempt, _attempt))
                {
                    return rememberedAttempt;
                }

                singleMatch = rememberedAttempt;
                matchCount++;
            }

            return matchCount == 1 ? singleMatch : null;
        }

        private FlightAttemptState FindLaunchContinuation(ActiveVesselSnapshot snapshot)
        {
            for (int attemptIndex = 0; attemptIndex < _attempts.Count; attemptIndex++)
            {
                FlightAttemptState rememberedAttempt = _attempts[attemptIndex];
                if (rememberedAttempt == null
                    || rememberedAttempt.HasPartLineage
                    || rememberedAttempt.LaunchUniversalTime < 0.0
                    || snapshot.LaunchUniversalTime < 0.0
                    || Math.Abs(
                        rememberedAttempt.LaunchUniversalTime - snapshot.LaunchUniversalTime)
                        > LaunchTimeMatchToleranceSeconds
                    || !string.Equals(
                        rememberedAttempt.CelestialBodyName,
                        snapshot.CelestialBodyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return rememberedAttempt;
            }

            return null;
        }

        private static void BeginAttempt(
            FlightAttemptState attempt,
            ActiveVesselSnapshot snapshot)
        {
            attempt.Clear();
            attempt.VesselId = snapshot.VesselId;
            attempt.CelestialBodyName = snapshot.CelestialBodyName;
            attempt.LaunchUniversalTime = snapshot.LaunchUniversalTime;
            attempt.StartLatitudeDegrees = snapshot.LatitudeDegrees;
            attempt.StartLongitudeDegrees = snapshot.LongitudeDegrees;
            attempt.LastSampleUniversalTime = snapshot.ObservationUniversalTime;
            attempt.MaximumAltitudeMeters = Math.Max(0.0, snapshot.AltitudeMeters);
            attempt.MaximumSurfaceSpeedMetersPerSecond = Math.Max(
                0.0,
                snapshot.SurfaceSpeedMetersPerSecond);
            attempt.EnteredOrbit = snapshot.Situation == FlightSituation.Orbiting;
            attempt.ReconcilePartLineage(snapshot.PartPersistentIds);
        }

        private void RemoveAttempt(FlightAttemptState attempt)
        {
            if (attempt == null)
            {
                return;
            }

            _attempts.Remove(attempt);
            if (!string.IsNullOrEmpty(attempt.VesselId))
            {
                FlightAttemptState mappedAttempt;
                if (_attemptsByVesselId.TryGetValue(attempt.VesselId, out mappedAttempt)
                    && ReferenceEquals(mappedAttempt, attempt))
                {
                    _attemptsByVesselId.Remove(attempt.VesselId);
                }
            }

            if (ReferenceEquals(_attempt, attempt))
            {
                _attempt = new FlightAttemptState();
            }
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

using System;
using System.Collections.Generic;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;

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
        private string _controlHoldMilestoneId;
        private string _qualifiedControlMilestoneId;
        private double _controlHoldSeconds;
        private bool _wasControlSampleInBand;
        private bool _enteredOrbit;
        private bool _completedDirectedPowerThisAttempt;
        private bool _completedMassThisAttempt;
        private bool _completedControlThisAttempt;
        private bool _completedBiomeThisAttempt;

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
        public string ControlHoldMilestoneId { get { return _controlHoldMilestoneId; } }
        public string QualifiedControlMilestoneId { get { return _qualifiedControlMilestoneId; } }
        public double ControlHoldSeconds { get { return _controlHoldSeconds; } }
        public bool WasControlSampleInBand { get { return _wasControlSampleInBand; } }
        public bool EnteredOrbit { get { return _enteredOrbit; } }
        public bool CompletedDirectedPowerThisAttempt { get { return _completedDirectedPowerThisAttempt; } }
        public bool CompletedMassThisAttempt { get { return _completedMassThisAttempt; } }
        public bool CompletedControlThisAttempt { get { return _completedControlThisAttempt; } }
        public bool CompletedBiomeThisAttempt { get { return _completedBiomeThisAttempt; } }

        /// <summary>
        /// Applies one active-vessel observation and records any newly completed starter milestones.
        /// At most one milestone from each line can be completed during the same launch attempt.
        /// </summary>
        public bool RefreshPlayerMilestones(
            SpaceProgramState playerProgram,
            IList<SpaceProgramState> programs,
            IList<MilestoneDefinition> starterMilestones,
            ActiveVesselTrackingSnapshot snapshot)
        {
            if (playerProgram == null
                || programs == null
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

            // Control is explicitly a continuous hold. A large gap means the active vessel was not
            // observed closely enough to prove that it remained inside the band for the missing time.
            // Five seconds allows normal one-second sampling jitter and 4x physics warp without
            // awarding time skipped while the vessel was packed or another scene was active.
            if (sampleDeltaSeconds > MaximumContinuousSampleGapSeconds)
            {
                sampleDeltaSeconds = 0.0;
                _wasControlSampleInBand = false;
                if (string.IsNullOrEmpty(_qualifiedControlMilestoneId))
                {
                    _controlHoldSeconds = 0.0;
                }
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
                if (!_completedMassThisAttempt)
                {
                    MilestoneDefinition massMilestone = GetCurrentMilestone(
                        StarterContractLine.Mass,
                        playerProgram,
                        programs,
                        starterMilestones,
                        snapshot.ObservationUniversalTime);
                    if (massMilestone != null
                        && snapshot.Situation == TrackedFlightSituation.Landed
                        && snapshot.MassTonnes >= massMilestone.RequiredMassTonnes
                        && _currentDistanceMeters >= massMilestone.RequiredDistanceMeters)
                    {
                        // Mass represents delivery of a finished craft, so the final landed vessel
                        // must still meet both the mass and distance requirement.
                        _completedMassThisAttempt = playerProgram.RecordAchievement(
                            massMilestone.Id,
                            snapshot.ObservationUniversalTime);
                        recordedAchievement |= _completedMassThisAttempt;
                    }
                }

                if (!_completedBiomeThisAttempt)
                {
                    MilestoneDefinition biomeMilestone = GetCurrentMilestone(
                        StarterContractLine.Biome,
                        playerProgram,
                        programs,
                        starterMilestones,
                        snapshot.ObservationUniversalTime);
                    if (biomeMilestone != null
                        && snapshot.Situation == TrackedFlightSituation.Landed
                        && !string.IsNullOrEmpty(snapshot.BiomeName)
                        && string.Equals(
                            snapshot.BiomeName,
                            biomeMilestone.RequiredBiomeName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        // Flying over a biome is not enough; the craft must finish landed there.
                        _completedBiomeThisAttempt = playerProgram.RecordAchievement(
                            biomeMilestone.Id,
                            snapshot.ObservationUniversalTime);
                        recordedAchievement |= _completedBiomeThisAttempt;
                    }
                }

                if (!_completedControlThisAttempt)
                {
                    recordedAchievement |= EvaluateControlMilestone(
                        playerProgram,
                        programs,
                        starterMilestones,
                        snapshot,
                        sampleDeltaSeconds);
                }
            }
            else
            {
                _wasControlSampleInBand = false;
                _controlHoldSeconds = 0.0;
            }

            _lastSampleUniversalTime = snapshot.ObservationUniversalTime;
            return recordedAchievement;
        }

        /// <summary>
        /// Consumes a KSP integration signal that the tracked active vessel was destroyed at the
        /// Kerbin surface. Directed Power is only awarded here because surviving or manually
        /// recovering the vehicle is not the required expendable impact outcome.
        /// </summary>
        public bool RecordSurfaceImpact(
            SpaceProgramState playerProgram,
            IList<SpaceProgramState> programs,
            IList<MilestoneDefinition> starterMilestones,
            string vesselId,
            string celestialBodyName,
            double impactUniversalTime)
        {
            if (playerProgram == null
                || programs == null
                || starterMilestones == null
                || !HasActiveAttempt
                || string.IsNullOrEmpty(vesselId)
                || !string.Equals(_vesselId, vesselId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool recordedAchievement = false;
            if (!_completedDirectedPowerThisAttempt
                && !_enteredOrbit
                && string.Equals(celestialBodyName, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                MilestoneDefinition directedPowerMilestone = GetCurrentMilestone(
                    StarterContractLine.DirectedPower,
                    playerProgram,
                    programs,
                    starterMilestones,
                    impactUniversalTime);
                if (directedPowerMilestone != null
                    && _maximumAltitudeMeters <= directedPowerMilestone.MaximumAltitudeMeters
                    && _maximumSurfaceSpeedMetersPerSecond
                        >= directedPowerMilestone.RequiredSpeedMetersPerSecond)
                {
                    _completedDirectedPowerThisAttempt = playerProgram.RecordAchievement(
                        directedPowerMilestone.Id,
                        impactUniversalTime);
                    recordedAchievement = _completedDirectedPowerThisAttempt;
                }
            }

            ClearAttempt();
            return recordedAchievement;
        }

        /// <summary>
        /// Restores an in-progress flight attempt from persistence. Invalid or incomplete state
        /// is discarded so older or malformed saves safely begin with no active starter-flight history.
        /// Live current-sample fields are deliberately rebuilt from the next KSP observation.
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
            string controlHoldMilestoneId,
            string qualifiedControlMilestoneId,
            double controlHoldSeconds,
            bool wasControlSampleInBand,
            bool enteredOrbit,
            bool completedDirectedPowerThisAttempt,
            bool completedMassThisAttempt,
            bool completedControlThisAttempt,
            bool completedBiomeThisAttempt)
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
                || maximumSurfaceSpeedMetersPerSecond < 0.0
                || !IsFinite(controlHoldSeconds)
                || controlHoldSeconds < 0.0)
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
            _controlHoldMilestoneId = controlHoldMilestoneId;
            _qualifiedControlMilestoneId = qualifiedControlMilestoneId;
            _controlHoldSeconds = controlHoldSeconds;
            _wasControlSampleInBand = wasControlSampleInBand;
            _enteredOrbit = enteredOrbit;
            _completedDirectedPowerThisAttempt = completedDirectedPowerThisAttempt;
            _completedMassThisAttempt = completedMassThisAttempt;
            _completedControlThisAttempt = completedControlThisAttempt;
            _completedBiomeThisAttempt = completedBiomeThisAttempt;
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
            _controlHoldMilestoneId = null;
            _qualifiedControlMilestoneId = null;
            _controlHoldSeconds = 0.0;
            _wasControlSampleInBand = false;
            _enteredOrbit = false;
            _completedDirectedPowerThisAttempt = false;
            _completedMassThisAttempt = false;
            _completedControlThisAttempt = false;
            _completedBiomeThisAttempt = false;
        }

        /// <summary>
        /// Returns the highest unlocked, not-yet-achieved milestone in one starter line for the player.
        /// Unlock scope remains global, so a rival can make a later level become the player's current target.
        /// </summary>
        public static MilestoneDefinition GetCurrentMilestone(
            StarterContractLine starterLine,
            SpaceProgramState playerProgram,
            IList<SpaceProgramState> programs,
            IList<MilestoneDefinition> starterMilestones,
            double evaluationUniversalTime)
        {
            if (starterLine == StarterContractLine.None
                || playerProgram == null
                || programs == null
                || starterMilestones == null)
            {
                return null;
            }

            MilestoneDefinition currentMilestone = null;

            for (int milestoneIndex = 0; milestoneIndex < starterMilestones.Count; milestoneIndex++)
            {
                MilestoneDefinition milestone = starterMilestones[milestoneIndex];
                if (milestone == null
                    || milestone.StarterLine != starterLine
                    || playerProgram.HasAchievement(milestone.Id)
                    || !UnlockRuleEvaluator.IsSatisfied(
                        milestone.UnlockRule,
                        programs,
                        evaluationUniversalTime))
                {
                    continue;
                }

                if (currentMilestone == null
                    || milestone.StarterLevel > currentMilestone.StarterLevel)
                {
                    currentMilestone = milestone;
                }
            }

            return currentMilestone;
        }

        private bool EvaluateControlMilestone(
            SpaceProgramState playerProgram,
            IList<SpaceProgramState> programs,
            IList<MilestoneDefinition> starterMilestones,
            ActiveVesselTrackingSnapshot snapshot,
            double sampleDeltaSeconds)
        {
            MilestoneDefinition controlMilestone = GetCurrentMilestone(
                StarterContractLine.Control,
                playerProgram,
                programs,
                starterMilestones,
                snapshot.ObservationUniversalTime);
            if (controlMilestone == null)
            {
                _controlHoldMilestoneId = null;
                _qualifiedControlMilestoneId = null;
                _controlHoldSeconds = 0.0;
                _wasControlSampleInBand = false;
                return false;
            }

            if (!string.Equals(
                _controlHoldMilestoneId,
                controlMilestone.Id,
                StringComparison.OrdinalIgnoreCase))
            {
                _controlHoldMilestoneId = controlMilestone.Id;
                _qualifiedControlMilestoneId = null;
                _controlHoldSeconds = 0.0;
                _wasControlSampleInBand = false;
            }

            if (string.Equals(
                    _qualifiedControlMilestoneId,
                    controlMilestone.Id,
                    StringComparison.OrdinalIgnoreCase)
                && snapshot.Situation == TrackedFlightSituation.Landed
                && snapshot.CrewCount > 0)
            {
                _completedControlThisAttempt = playerProgram.RecordAchievement(
                    controlMilestone.Id,
                    snapshot.ObservationUniversalTime);
                return _completedControlThisAttempt;
            }

            bool isInBand = snapshot.CrewCount > 0
                && snapshot.AltitudeMeters >= controlMilestone.MinimumAltitudeMeters
                && snapshot.AltitudeMeters <= controlMilestone.MaximumAltitudeMeters;
            if (!isInBand)
            {
                _wasControlSampleInBand = false;
                if (string.IsNullOrEmpty(_qualifiedControlMilestoneId))
                {
                    _controlHoldSeconds = 0.0;
                }
                return false;
            }

            if (_wasControlSampleInBand && sampleDeltaSeconds > 0.0)
            {
                _controlHoldSeconds += sampleDeltaSeconds;
            }

            _wasControlSampleInBand = true;
            if (_controlHoldSeconds >= controlMilestone.RequiredDurationSeconds)
            {
                _qualifiedControlMilestoneId = controlMilestone.Id;
            }

            return false;
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
    }
}

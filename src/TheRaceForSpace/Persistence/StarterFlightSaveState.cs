using System;
using System.Globalization;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.Persistence
{
    /// <summary>
    /// Serializable state for the one active starter-contract flight attempt. Older saves without
    /// this node safely restore with no attempt in progress.
    /// </summary>
    public sealed class StarterFlightSaveState
    {
        private const string ActiveValueName = "active";
        private const string VesselIdValueName = "vesselId";
        private const string BodyValueName = "body";
        private const string LaunchUniversalTimeValueName = "launchUniversalTime";
        private const string StartLatitudeValueName = "startLatitude";
        private const string StartLongitudeValueName = "startLongitude";
        private const string LastSampleUniversalTimeValueName = "lastSampleUniversalTime";
        private const string MaximumAltitudeValueName = "maximumAltitudeMeters";
        private const string MaximumSurfaceSpeedValueName = "maximumSurfaceSpeedMetersPerSecond";
        private const string ControlHoldMilestoneIdValueName = "controlHoldMilestoneId";
        private const string QualifiedControlMilestoneIdValueName = "qualifiedControlMilestoneId";
        private const string ControlHoldSecondsValueName = "controlHoldSeconds";
        private const string WasControlSampleInBandValueName = "wasControlSampleInBand";
        private const string EnteredOrbitValueName = "enteredOrbit";
        private const string CompletedDirectedPowerValueName = "completedDirectedPower";
        private const string CompletedMassValueName = "completedMass";
        private const string CompletedControlValueName = "completedControl";
        private const string CompletedBiomeValueName = "completedBiome";

        private bool _hasActiveAttempt;
        private string _vesselId;
        private string _celestialBodyName;
        private double _launchUniversalTime;
        private double _startLatitudeDegrees;
        private double _startLongitudeDegrees;
        private double _lastSampleUniversalTime;
        private double _maximumAltitudeMeters;
        private double _maximumSurfaceSpeedMetersPerSecond;
        private string _controlHoldMilestoneId;
        private string _qualifiedControlMilestoneId;
        private double _controlHoldSeconds;
        private bool _wasControlSampleInBand;
        private bool _enteredOrbit;
        private bool _completedDirectedPowerThisAttempt;
        private bool _completedMassThisAttempt;
        private bool _completedControlThisAttempt;
        private bool _completedBiomeThisAttempt;

        public bool HasData { get; private set; }

        public void Capture(StarterFlightTracker tracker)
        {
            ClearState();
            if (tracker == null)
            {
                return;
            }

            HasData = true;
            _hasActiveAttempt = tracker.HasActiveAttempt;
            if (!_hasActiveAttempt)
            {
                return;
            }

            _vesselId = tracker.VesselId;
            _celestialBodyName = tracker.CelestialBodyName;
            _launchUniversalTime = tracker.LaunchUniversalTime;
            _startLatitudeDegrees = tracker.StartLatitudeDegrees;
            _startLongitudeDegrees = tracker.StartLongitudeDegrees;
            _lastSampleUniversalTime = tracker.LastSampleUniversalTime;
            _maximumAltitudeMeters = tracker.MaximumAltitudeMeters;
            _maximumSurfaceSpeedMetersPerSecond = tracker.MaximumSurfaceSpeedMetersPerSecond;
            _controlHoldMilestoneId = tracker.ControlHoldMilestoneId;
            _qualifiedControlMilestoneId = tracker.QualifiedControlMilestoneId;
            _controlHoldSeconds = tracker.ControlHoldSeconds;
            _wasControlSampleInBand = tracker.WasControlSampleInBand;
            _enteredOrbit = tracker.EnteredOrbit;
            _completedDirectedPowerThisAttempt = tracker.CompletedDirectedPowerThisAttempt;
            _completedMassThisAttempt = tracker.CompletedMassThisAttempt;
            _completedControlThisAttempt = tracker.CompletedControlThisAttempt;
            _completedBiomeThisAttempt = tracker.CompletedBiomeThisAttempt;
        }

        public void ApplyTo(StarterFlightTracker tracker)
        {
            if (tracker == null)
            {
                return;
            }

            if (!HasData || !_hasActiveAttempt)
            {
                tracker.ClearAttempt();
                return;
            }

            tracker.RestoreState(
                _vesselId,
                _celestialBodyName,
                _launchUniversalTime,
                _startLatitudeDegrees,
                _startLongitudeDegrees,
                _lastSampleUniversalTime,
                _maximumAltitudeMeters,
                _maximumSurfaceSpeedMetersPerSecond,
                _controlHoldMilestoneId,
                _qualifiedControlMilestoneId,
                _controlHoldSeconds,
                _wasControlSampleInBand,
                _enteredOrbit,
                _completedDirectedPowerThisAttempt,
                _completedMassThisAttempt,
                _completedControlThisAttempt,
                _completedBiomeThisAttempt);
        }

        public void Load(ConfigNode node)
        {
            ClearState();
            HasData = node != null;
            if (!HasData)
            {
                return;
            }

            _hasActiveAttempt = ParseBool(node.GetValue(ActiveValueName));
            if (!_hasActiveAttempt)
            {
                return;
            }

            _vesselId = node.GetValue(VesselIdValueName);
            _celestialBodyName = node.GetValue(BodyValueName);

            // An active node must contain every numeric field needed to reconstruct the attempt.
            // Treat partial or malformed nodes as no active attempt rather than inventing zeroes
            // that could erase a ceiling violation or grant free Control hold time after loading.
            if (string.IsNullOrEmpty(_vesselId)
                || string.IsNullOrEmpty(_celestialBodyName)
                || !TryParseFiniteDouble(node.GetValue(LaunchUniversalTimeValueName), out _launchUniversalTime)
                || _launchUniversalTime < 0.0
                || !TryParseFiniteDouble(node.GetValue(StartLatitudeValueName), out _startLatitudeDegrees)
                || _startLatitudeDegrees < -90.0
                || _startLatitudeDegrees > 90.0
                || !TryParseFiniteDouble(node.GetValue(StartLongitudeValueName), out _startLongitudeDegrees)
                || !TryParseFiniteDouble(node.GetValue(LastSampleUniversalTimeValueName), out _lastSampleUniversalTime)
                || _lastSampleUniversalTime < 0.0
                || !TryParseFiniteDouble(node.GetValue(MaximumAltitudeValueName), out _maximumAltitudeMeters)
                || _maximumAltitudeMeters < 0.0
                || !TryParseFiniteDouble(
                    node.GetValue(MaximumSurfaceSpeedValueName),
                    out _maximumSurfaceSpeedMetersPerSecond)
                || _maximumSurfaceSpeedMetersPerSecond < 0.0
                || !TryParseFiniteDouble(node.GetValue(ControlHoldSecondsValueName), out _controlHoldSeconds)
                || _controlHoldSeconds < 0.0)
            {
                _hasActiveAttempt = false;
                return;
            }

            _controlHoldMilestoneId = node.GetValue(ControlHoldMilestoneIdValueName);
            _qualifiedControlMilestoneId = node.GetValue(QualifiedControlMilestoneIdValueName);
            _wasControlSampleInBand = ParseBool(node.GetValue(WasControlSampleInBandValueName));
            _enteredOrbit = ParseBool(node.GetValue(EnteredOrbitValueName));
            _completedDirectedPowerThisAttempt = ParseBool(node.GetValue(CompletedDirectedPowerValueName));
            _completedMassThisAttempt = ParseBool(node.GetValue(CompletedMassValueName));
            _completedControlThisAttempt = ParseBool(node.GetValue(CompletedControlValueName));
            _completedBiomeThisAttempt = ParseBool(node.GetValue(CompletedBiomeValueName));
        }

        public void Save(ConfigNode node)
        {
            if (!HasData || node == null)
            {
                return;
            }

            node.AddValue(ActiveValueName, _hasActiveAttempt);
            if (!_hasActiveAttempt)
            {
                return;
            }

            node.AddValue(VesselIdValueName, _vesselId);
            node.AddValue(BodyValueName, _celestialBodyName);
            AddDouble(node, LaunchUniversalTimeValueName, _launchUniversalTime);
            AddDouble(node, StartLatitudeValueName, _startLatitudeDegrees);
            AddDouble(node, StartLongitudeValueName, _startLongitudeDegrees);
            AddDouble(node, LastSampleUniversalTimeValueName, _lastSampleUniversalTime);
            AddDouble(node, MaximumAltitudeValueName, _maximumAltitudeMeters);
            AddDouble(node, MaximumSurfaceSpeedValueName, _maximumSurfaceSpeedMetersPerSecond);
            if (!string.IsNullOrEmpty(_controlHoldMilestoneId))
            {
                node.AddValue(ControlHoldMilestoneIdValueName, _controlHoldMilestoneId);
            }
            if (!string.IsNullOrEmpty(_qualifiedControlMilestoneId))
            {
                node.AddValue(QualifiedControlMilestoneIdValueName, _qualifiedControlMilestoneId);
            }
            AddDouble(node, ControlHoldSecondsValueName, _controlHoldSeconds);
            node.AddValue(WasControlSampleInBandValueName, _wasControlSampleInBand);
            node.AddValue(EnteredOrbitValueName, _enteredOrbit);
            node.AddValue(CompletedDirectedPowerValueName, _completedDirectedPowerThisAttempt);
            node.AddValue(CompletedMassValueName, _completedMassThisAttempt);
            node.AddValue(CompletedControlValueName, _completedControlThisAttempt);
            node.AddValue(CompletedBiomeValueName, _completedBiomeThisAttempt);
        }

        private void ClearState()
        {
            HasData = false;
            _hasActiveAttempt = false;
            _vesselId = null;
            _celestialBodyName = null;
            _launchUniversalTime = -1.0;
            _startLatitudeDegrees = 0.0;
            _startLongitudeDegrees = 0.0;
            _lastSampleUniversalTime = -1.0;
            _maximumAltitudeMeters = 0.0;
            _maximumSurfaceSpeedMetersPerSecond = 0.0;
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

        private static void AddDouble(ConfigNode node, string valueName, double value)
        {
            node.AddValue(valueName, value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static bool ParseBool(string value)
        {
            bool parsedValue;
            return !string.IsNullOrEmpty(value) && bool.TryParse(value, out parsedValue) && parsedValue;
        }

        private static bool TryParseFiniteDouble(string value, out double parsedValue)
        {
            parsedValue = 0.0;
            return !string.IsNullOrEmpty(value)
                && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue)
                && !double.IsNaN(parsedValue)
                && !double.IsInfinity(parsedValue);
        }
    }
}

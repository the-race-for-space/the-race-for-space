using System.Collections.Generic;
using System.Globalization;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.Persistence
{
    /// <summary>
    /// Persists temporary progress used to evaluate active contract conditions across save/load.
    /// The current implementation stores one tracked flight attempt plus independent Control
    /// contract hold/qualification state; funding contract lifecycle is persisted separately.
    /// </summary>
    public sealed class ActiveContractProgressSaveState
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
        private const string EnteredOrbitValueName = "enteredOrbit";

        private const string ControlStateNodeName = "CONTROL_STATE";
        private const string ControlMilestoneIdValueName = "milestoneId";
        private const string ControlHoldSecondsValueName = "holdSeconds";
        private const string ControlWasSampleInBandValueName = "wasSampleInBand";
        private const string ControlQualifiedValueName = "qualified";

        private readonly List<SavedControlContractProgress> _controlStates =
            new List<SavedControlContractProgress>();

        private bool _hasActiveAttempt;
        private string _vesselId;
        private string _celestialBodyName;
        private double _launchUniversalTime;
        private double _startLatitudeDegrees;
        private double _startLongitudeDegrees;
        private double _lastSampleUniversalTime;
        private double _maximumAltitudeMeters;
        private double _maximumSurfaceSpeedMetersPerSecond;
        private bool _enteredOrbit;

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
            _enteredOrbit = tracker.EnteredOrbit;

            // The tracker owns the active Control dictionary. Iterate its live key collection
            // directly so the once-per-second capture path does not allocate a temporary ID list.
            foreach (string milestoneId in tracker.ControlStateMilestoneIds)
            {
                _controlStates.Add(new SavedControlContractProgress(
                    milestoneId,
                    tracker.GetControlHoldSeconds(milestoneId),
                    tracker.IsControlSampleInBand(milestoneId),
                    tracker.IsControlMilestoneQualified(milestoneId)));
            }
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
                _enteredOrbit);

            for (int stateIndex = 0; stateIndex < _controlStates.Count; stateIndex++)
            {
                SavedControlContractProgress state = _controlStates[stateIndex];
                tracker.RestoreControlState(
                    state.MilestoneId,
                    state.HoldSeconds,
                    state.WasSampleInBand,
                    state.IsQualified);
            }
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
            // that could erase a ceiling violation or create continuity that was never observed.
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
                || _maximumSurfaceSpeedMetersPerSecond < 0.0)
            {
                _hasActiveAttempt = false;
                return;
            }

            _enteredOrbit = ParseBool(node.GetValue(EnteredOrbitValueName));

            // Current-format progress is stored per Control contract ID. Malformed or duplicate
            // entries invalidate the active attempt rather than manufacturing condition progress.
            var restoredMilestoneIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            ConfigNode[] controlStateNodes = node.GetNodes(ControlStateNodeName);
            for (int stateIndex = 0; stateIndex < controlStateNodes.Length; stateIndex++)
            {
                ConfigNode controlStateNode = controlStateNodes[stateIndex];
                string milestoneId = controlStateNode == null
                    ? null
                    : controlStateNode.GetValue(ControlMilestoneIdValueName);
                double holdSeconds;
                bool wasSampleInBand;
                bool isQualified;

                if (string.IsNullOrEmpty(milestoneId)
                    || !restoredMilestoneIds.Add(milestoneId)
                    || !TryParseFiniteDouble(
                        controlStateNode.GetValue(ControlHoldSecondsValueName),
                        out holdSeconds)
                    || holdSeconds < 0.0
                    || !TryParseBool(
                        controlStateNode.GetValue(ControlWasSampleInBandValueName),
                        out wasSampleInBand)
                    || !TryParseBool(
                        controlStateNode.GetValue(ControlQualifiedValueName),
                        out isQualified))
                {
                    _hasActiveAttempt = false;
                    _controlStates.Clear();
                    return;
                }

                _controlStates.Add(new SavedControlContractProgress(
                    milestoneId,
                    holdSeconds,
                    wasSampleInBand,
                    isQualified));
            }
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
            node.AddValue(EnteredOrbitValueName, _enteredOrbit);

            for (int stateIndex = 0; stateIndex < _controlStates.Count; stateIndex++)
            {
                SavedControlContractProgress state = _controlStates[stateIndex];
                ConfigNode controlStateNode = node.AddNode(ControlStateNodeName);
                controlStateNode.AddValue(ControlMilestoneIdValueName, state.MilestoneId);
                AddDouble(controlStateNode, ControlHoldSecondsValueName, state.HoldSeconds);
                controlStateNode.AddValue(ControlWasSampleInBandValueName, state.WasSampleInBand);
                controlStateNode.AddValue(ControlQualifiedValueName, state.IsQualified);
            }
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
            _enteredOrbit = false;
            _controlStates.Clear();
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

        private static bool TryParseBool(string value, out bool parsedValue)
        {
            parsedValue = false;
            return !string.IsNullOrEmpty(value) && bool.TryParse(value, out parsedValue);
        }

        private static bool TryParseFiniteDouble(string value, out double parsedValue)
        {
            parsedValue = 0.0;
            return !string.IsNullOrEmpty(value)
                && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue)
                && !double.IsNaN(parsedValue)
                && !double.IsInfinity(parsedValue);
        }

        private sealed class SavedControlContractProgress
        {
            public SavedControlContractProgress(
                string milestoneId,
                double holdSeconds,
                bool wasSampleInBand,
                bool isQualified)
            {
                MilestoneId = milestoneId;
                HoldSeconds = holdSeconds;
                WasSampleInBand = wasSampleInBand;
                IsQualified = isQualified;
            }

            public string MilestoneId { get; private set; }
            public double HoldSeconds { get; private set; }
            public bool WasSampleInBand { get; private set; }
            public bool IsQualified { get; private set; }
        }
    }
}

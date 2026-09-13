using System.Collections.Generic;
using System.Globalization;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.Persistence
{
    /// <summary>
    /// Persists remembered Flight Contract attempts across save/load. Each attempt keeps its own
    /// historical telemetry, persistent-part lineage, and independent Control contract state.
    /// Funding contract lifecycle is persisted separately.
    /// </summary>
    public sealed class FlightContractProgressSaveState
    {
        private const string AttemptNodeName = "ATTEMPT";
        private const string SelectedValueName = "selected";
        private const string VesselIdValueName = "vesselId";
        private const string BodyValueName = "body";
        private const string LaunchUniversalTimeValueName = "launchUniversalTime";
        private const string StartLatitudeValueName = "startLatitude";
        private const string StartLongitudeValueName = "startLongitude";
        private const string LastSampleUniversalTimeValueName = "lastSampleUniversalTime";
        private const string MaximumAltitudeValueName = "maximumAltitudeMeters";
        private const string MaximumSurfaceSpeedValueName = "maximumSurfaceSpeedMetersPerSecond";
        private const string EnteredOrbitValueName = "enteredOrbit";

        private const string PartLineageNodeName = "PART_LINEAGE";
        private const string PartPersistentIdValueName = "partPersistentId";

        private const string ControlStateNodeName = "CONTROL_STATE";
        private const string ControlObjectiveIdValueName = "objectiveId";
        private const string ControlHoldSecondsValueName = "holdSeconds";
        private const string ControlWasSampleInBandValueName = "wasSampleInBand";
        private const string ControlQualifiedValueName = "qualified";

        private readonly List<SavedFlightAttemptProgress> _attempts =
            new List<SavedFlightAttemptProgress>();

        public bool HasData { get; private set; }

        public void Capture(FlightContractTracker tracker)
        {
            ClearState();
            if (tracker == null)
            {
                return;
            }

            HasData = true;
            foreach (FlightAttemptState attempt in tracker.RememberedAttempts)
            {
                if (attempt == null || !attempt.HasActiveAttempt)
                {
                    continue;
                }

                var savedAttempt = new SavedFlightAttemptProgress(
                    tracker.IsSelectedAttempt(attempt),
                    attempt.VesselId,
                    attempt.CelestialBodyName,
                    attempt.LaunchUniversalTime,
                    attempt.StartLatitudeDegrees,
                    attempt.StartLongitudeDegrees,
                    attempt.LastSampleUniversalTime,
                    attempt.MaximumAltitudeMeters,
                    attempt.MaximumSurfaceSpeedMetersPerSecond,
                    attempt.EnteredOrbit);

                foreach (uint partPersistentId in attempt.PartPersistentIds)
                {
                    savedAttempt.PartPersistentIds.Add(partPersistentId);
                }

                // HashSet enumeration order is not guaranteed. Stable ordering keeps the KSP save
                // easier to inspect without changing the lineage semantics.
                savedAttempt.PartPersistentIds.Sort();

                foreach (string objectiveId in attempt.ControlStateObjectiveIds)
                {
                    savedAttempt.ControlStates.Add(new SavedControlContractProgress(
                        objectiveId,
                        attempt.GetControlHoldSeconds(objectiveId),
                        attempt.IsControlSampleInBand(objectiveId),
                        attempt.IsControlObjectiveQualified(objectiveId)));
                }

                _attempts.Add(savedAttempt);
            }
        }

        public void ApplyTo(FlightContractTracker tracker)
        {
            if (tracker == null)
            {
                return;
            }

            tracker.ClearAllAttempts();
            if (!HasData)
            {
                return;
            }

            for (int attemptIndex = 0; attemptIndex < _attempts.Count; attemptIndex++)
            {
                SavedFlightAttemptProgress savedAttempt = _attempts[attemptIndex];
                FlightAttemptState restoredAttempt = tracker.RestoreAttemptState(
                    savedAttempt.VesselId,
                    savedAttempt.CelestialBodyName,
                    savedAttempt.LaunchUniversalTime,
                    savedAttempt.StartLatitudeDegrees,
                    savedAttempt.StartLongitudeDegrees,
                    savedAttempt.LastSampleUniversalTime,
                    savedAttempt.MaximumAltitudeMeters,
                    savedAttempt.MaximumSurfaceSpeedMetersPerSecond,
                    savedAttempt.EnteredOrbit,
                    savedAttempt.PartPersistentIds,
                    savedAttempt.IsSelected);

                if (restoredAttempt == null)
                {
                    // Persistence is authoritative on load. Never keep a partially reconstructed set
                    // when one saved attempt fails the tracker's defensive validation.
                    tracker.ClearAllAttempts();
                    return;
                }

                for (int stateIndex = 0;
                    stateIndex < savedAttempt.ControlStates.Count;
                    stateIndex++)
                {
                    SavedControlContractProgress state = savedAttempt.ControlStates[stateIndex];
                    tracker.RestoreControlState(
                        restoredAttempt,
                        state.ObjectiveId,
                        state.HoldSeconds,
                        state.WasSampleInBand,
                        state.IsQualified);
                }
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

            var claimedPartPersistentIds = new HashSet<uint>();
            bool hasSelectedAttempt = false;
            ConfigNode[] attemptNodes = node.GetNodes(AttemptNodeName);
            for (int attemptIndex = 0; attemptIndex < attemptNodes.Length; attemptIndex++)
            {
                SavedFlightAttemptProgress savedAttempt;
                if (!TryLoadAttempt(
                        attemptNodes[attemptIndex],
                        claimedPartPersistentIds,
                        out savedAttempt)
                    || (savedAttempt.IsSelected && hasSelectedAttempt))
                {
                    _attempts.Clear();
                    return;
                }

                if (savedAttempt.IsSelected)
                {
                    hasSelectedAttempt = true;
                }

                _attempts.Add(savedAttempt);
            }
        }

        public void Save(ConfigNode node)
        {
            if (!HasData || node == null)
            {
                return;
            }

            for (int attemptIndex = 0; attemptIndex < _attempts.Count; attemptIndex++)
            {
                SavedFlightAttemptProgress attempt = _attempts[attemptIndex];
                ConfigNode attemptNode = node.AddNode(AttemptNodeName);
                attemptNode.AddValue(SelectedValueName, attempt.IsSelected);
                attemptNode.AddValue(VesselIdValueName, attempt.VesselId);
                attemptNode.AddValue(BodyValueName, attempt.CelestialBodyName);
                AddDouble(
                    attemptNode,
                    LaunchUniversalTimeValueName,
                    attempt.LaunchUniversalTime);
                AddDouble(attemptNode, StartLatitudeValueName, attempt.StartLatitudeDegrees);
                AddDouble(attemptNode, StartLongitudeValueName, attempt.StartLongitudeDegrees);
                AddDouble(
                    attemptNode,
                    LastSampleUniversalTimeValueName,
                    attempt.LastSampleUniversalTime);
                AddDouble(attemptNode, MaximumAltitudeValueName, attempt.MaximumAltitudeMeters);
                AddDouble(
                    attemptNode,
                    MaximumSurfaceSpeedValueName,
                    attempt.MaximumSurfaceSpeedMetersPerSecond);
                attemptNode.AddValue(EnteredOrbitValueName, attempt.EnteredOrbit);

                for (int partIndex = 0;
                    partIndex < attempt.PartPersistentIds.Count;
                    partIndex++)
                {
                    ConfigNode partNode = attemptNode.AddNode(PartLineageNodeName);
                    partNode.AddValue(
                        PartPersistentIdValueName,
                        attempt.PartPersistentIds[partIndex].ToString(
                            CultureInfo.InvariantCulture));
                }

                for (int stateIndex = 0;
                    stateIndex < attempt.ControlStates.Count;
                    stateIndex++)
                {
                    SavedControlContractProgress state = attempt.ControlStates[stateIndex];
                    ConfigNode controlStateNode = attemptNode.AddNode(ControlStateNodeName);
                    controlStateNode.AddValue(ControlObjectiveIdValueName, state.ObjectiveId);
                    AddDouble(controlStateNode, ControlHoldSecondsValueName, state.HoldSeconds);
                    controlStateNode.AddValue(
                        ControlWasSampleInBandValueName,
                        state.WasSampleInBand);
                    controlStateNode.AddValue(ControlQualifiedValueName, state.IsQualified);
                }
            }
        }

        private static bool TryLoadAttempt(
            ConfigNode attemptNode,
            ISet<uint> claimedPartPersistentIds,
            out SavedFlightAttemptProgress savedAttempt)
        {
            savedAttempt = null;
            if (attemptNode == null || claimedPartPersistentIds == null)
            {
                return false;
            }

            bool isSelected;
            bool enteredOrbit;
            string vesselId = attemptNode.GetValue(VesselIdValueName);
            string celestialBodyName = attemptNode.GetValue(BodyValueName);
            double launchUniversalTime;
            double startLatitudeDegrees;
            double startLongitudeDegrees;
            double lastSampleUniversalTime;
            double maximumAltitudeMeters;
            double maximumSurfaceSpeedMetersPerSecond;

            if (!TryParseBool(attemptNode.GetValue(SelectedValueName), out isSelected)
                || string.IsNullOrEmpty(vesselId)
                || string.IsNullOrEmpty(celestialBodyName)
                || !TryParseFiniteDouble(
                    attemptNode.GetValue(LaunchUniversalTimeValueName),
                    out launchUniversalTime)
                || launchUniversalTime < 0.0
                || !TryParseFiniteDouble(
                    attemptNode.GetValue(StartLatitudeValueName),
                    out startLatitudeDegrees)
                || startLatitudeDegrees < -90.0
                || startLatitudeDegrees > 90.0
                || !TryParseFiniteDouble(
                    attemptNode.GetValue(StartLongitudeValueName),
                    out startLongitudeDegrees)
                || !TryParseFiniteDouble(
                    attemptNode.GetValue(LastSampleUniversalTimeValueName),
                    out lastSampleUniversalTime)
                || lastSampleUniversalTime < 0.0
                || !TryParseFiniteDouble(
                    attemptNode.GetValue(MaximumAltitudeValueName),
                    out maximumAltitudeMeters)
                || maximumAltitudeMeters < 0.0
                || !TryParseFiniteDouble(
                    attemptNode.GetValue(MaximumSurfaceSpeedValueName),
                    out maximumSurfaceSpeedMetersPerSecond)
                || maximumSurfaceSpeedMetersPerSecond < 0.0
                || !TryParseBool(
                    attemptNode.GetValue(EnteredOrbitValueName),
                    out enteredOrbit))
            {
                return false;
            }

            savedAttempt = new SavedFlightAttemptProgress(
                isSelected,
                vesselId,
                celestialBodyName,
                launchUniversalTime,
                startLatitudeDegrees,
                startLongitudeDegrees,
                lastSampleUniversalTime,
                maximumAltitudeMeters,
                maximumSurfaceSpeedMetersPerSecond,
                enteredOrbit);

            ConfigNode[] partNodes = attemptNode.GetNodes(PartLineageNodeName);
            for (int partIndex = 0; partIndex < partNodes.Length; partIndex++)
            {
                ConfigNode partNode = partNodes[partIndex];
                uint partPersistentId;
                if (partNode == null
                    || !uint.TryParse(
                        partNode.GetValue(PartPersistentIdValueName),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out partPersistentId)
                    || partPersistentId == 0u
                    || !claimedPartPersistentIds.Add(partPersistentId))
                {
                    savedAttempt = null;
                    return false;
                }

                savedAttempt.PartPersistentIds.Add(partPersistentId);
            }

            var restoredObjectiveIds = new HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase);
            ConfigNode[] controlStateNodes = attemptNode.GetNodes(ControlStateNodeName);
            for (int stateIndex = 0; stateIndex < controlStateNodes.Length; stateIndex++)
            {
                ConfigNode controlStateNode = controlStateNodes[stateIndex];
                string objectiveId = controlStateNode == null
                    ? null
                    : controlStateNode.GetValue(ControlObjectiveIdValueName);
                double holdSeconds;
                bool wasSampleInBand;
                bool isQualified;

                if (string.IsNullOrEmpty(objectiveId)
                    || !restoredObjectiveIds.Add(objectiveId)
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
                    savedAttempt = null;
                    return false;
                }

                savedAttempt.ControlStates.Add(new SavedControlContractProgress(
                    objectiveId,
                    holdSeconds,
                    wasSampleInBand,
                    isQualified));
            }

            return true;
        }

        private void ClearState()
        {
            HasData = false;
            _attempts.Clear();
        }

        private static void AddDouble(ConfigNode node, string valueName, double value)
        {
            node.AddValue(valueName, value.ToString("R", CultureInfo.InvariantCulture));
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

        private sealed class SavedFlightAttemptProgress
        {
            public SavedFlightAttemptProgress(
                bool isSelected,
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
                IsSelected = isSelected;
                VesselId = vesselId;
                CelestialBodyName = celestialBodyName;
                LaunchUniversalTime = launchUniversalTime;
                StartLatitudeDegrees = startLatitudeDegrees;
                StartLongitudeDegrees = startLongitudeDegrees;
                LastSampleUniversalTime = lastSampleUniversalTime;
                MaximumAltitudeMeters = maximumAltitudeMeters;
                MaximumSurfaceSpeedMetersPerSecond = maximumSurfaceSpeedMetersPerSecond;
                EnteredOrbit = enteredOrbit;
                PartPersistentIds = new List<uint>();
                ControlStates = new List<SavedControlContractProgress>();
            }

            public bool IsSelected { get; private set; }
            public string VesselId { get; private set; }
            public string CelestialBodyName { get; private set; }
            public double LaunchUniversalTime { get; private set; }
            public double StartLatitudeDegrees { get; private set; }
            public double StartLongitudeDegrees { get; private set; }
            public double LastSampleUniversalTime { get; private set; }
            public double MaximumAltitudeMeters { get; private set; }
            public double MaximumSurfaceSpeedMetersPerSecond { get; private set; }
            public bool EnteredOrbit { get; private set; }
            public List<uint> PartPersistentIds { get; private set; }
            public List<SavedControlContractProgress> ControlStates { get; private set; }
        }

        private sealed class SavedControlContractProgress
        {
            public SavedControlContractProgress(
                string objectiveId,
                double holdSeconds,
                bool wasSampleInBand,
                bool isQualified)
            {
                ObjectiveId = objectiveId;
                HoldSeconds = holdSeconds;
                WasSampleInBand = wasSampleInBand;
                IsQualified = isQualified;
            }

            public string ObjectiveId { get; private set; }
            public double HoldSeconds { get; private set; }
            public bool WasSampleInBand { get; private set; }
            public bool IsQualified { get; private set; }
        }
    }
}

using System;
using System.Collections.Generic;

namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// Holds the mutable state for one remembered Flight Contract attempt. FlightContractTracker
    /// owns the collection of attempts and keeps contract evaluation separate from this state model.
    /// </summary>
    internal sealed class FlightAttemptState
    {
        private readonly Dictionary<string, ControlContractState> _controlStates =
            new Dictionary<string, ControlContractState>(StringComparer.OrdinalIgnoreCase);

        internal FlightAttemptState()
        {
            Clear();
        }

        internal bool HasActiveAttempt { get { return !string.IsNullOrEmpty(VesselId); } }
        internal string VesselId { get; set; }
        internal string CelestialBodyName { get; set; }
        internal double LaunchUniversalTime { get; set; }
        internal double StartLatitudeDegrees { get; set; }
        internal double StartLongitudeDegrees { get; set; }
        internal double LastSampleUniversalTime { get; set; }
        internal double MaximumAltitudeMeters { get; set; }
        internal double MaximumSurfaceSpeedMetersPerSecond { get; set; }
        internal double CurrentAltitudeMeters { get; set; }
        internal double CurrentSurfaceSpeedMetersPerSecond { get; set; }
        internal double CurrentMassTonnes { get; set; }
        internal double CurrentDistanceMeters { get; set; }
        internal string CurrentBiomeName { get; set; }
        internal int CurrentCrewCount { get; set; }
        internal FlightSituation CurrentSituation { get; set; }
        internal bool EnteredOrbit { get; set; }

        // Persistence captures this live key collection directly, avoiding a temporary list allocation
        // on the once-per-second flight-contract capture path.
        internal ICollection<string> ControlStateObjectiveIds { get { return _controlStates.Keys; } }

        internal double GetControlHoldSeconds(string objectiveId)
        {
            ControlContractState state;
            return !string.IsNullOrEmpty(objectiveId)
                && _controlStates.TryGetValue(objectiveId, out state)
                ? state.HoldSeconds
                : 0.0;
        }

        internal bool IsControlObjectiveQualified(string objectiveId)
        {
            ControlContractState state;
            return !string.IsNullOrEmpty(objectiveId)
                && _controlStates.TryGetValue(objectiveId, out state)
                && state.IsQualified;
        }

        internal bool IsControlSampleInBand(string objectiveId)
        {
            ControlContractState state;
            return !string.IsNullOrEmpty(objectiveId)
                && _controlStates.TryGetValue(objectiveId, out state)
                && state.WasSampleInBand;
        }

        internal void RestoreControlState(
            string objectiveId,
            double holdSeconds,
            bool wasSampleInBand,
            bool isQualified)
        {
            ControlContractState state = GetOrCreateControlState(objectiveId);
            state.HoldSeconds = holdSeconds;
            state.WasSampleInBand = wasSampleInBand;
            state.IsQualified = isQualified;
        }

        internal ControlContractState GetOrCreateControlState(string objectiveId)
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

        internal void ResetUnqualifiedControlStates()
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

        internal void Clear()
        {
            VesselId = null;
            CelestialBodyName = null;
            LaunchUniversalTime = -1.0;
            StartLatitudeDegrees = 0.0;
            StartLongitudeDegrees = 0.0;
            LastSampleUniversalTime = -1.0;
            MaximumAltitudeMeters = 0.0;
            MaximumSurfaceSpeedMetersPerSecond = 0.0;
            CurrentAltitudeMeters = 0.0;
            CurrentSurfaceSpeedMetersPerSecond = 0.0;
            CurrentMassTonnes = 0.0;
            CurrentDistanceMeters = 0.0;
            CurrentBiomeName = null;
            CurrentCrewCount = 0;
            CurrentSituation = FlightSituation.Other;
            EnteredOrbit = false;
            _controlStates.Clear();
        }

        internal sealed class ControlContractState
        {
            internal double HoldSeconds;
            internal bool WasSampleInBand;
            internal bool IsQualified;
        }
    }
}

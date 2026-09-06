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
        private readonly HashSet<uint> _partPersistentIds = new HashSet<uint>();
        private readonly HashSet<uint> _attachedPartPersistentIds = new HashSet<uint>();
        private bool _hasObservedAttachedPartTopology;

        internal FlightAttemptState()
        {
            Clear();
        }

        internal bool HasActiveAttempt { get { return !string.IsNullOrEmpty(VesselId); } }
        internal bool HasPartLineage { get { return _partPersistentIds.Count > 0; } }
        internal bool HasAttachedParts { get { return _attachedPartPersistentIds.Count > 0; } }
        internal IEnumerable<uint> PartPersistentIds { get { return _partPersistentIds; } }
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

        internal bool ContainsPartPersistentId(uint partPersistentId)
        {
            return partPersistentId != 0u && _partPersistentIds.Contains(partPersistentId);
        }

        internal bool SharesPartLineage(IReadOnlyList<uint> partPersistentIds)
        {
            return CountSharedPartLineage(partPersistentIds) > 0;
        }

        internal int CountSharedPartLineage(IReadOnlyList<uint> partPersistentIds)
        {
            if (_partPersistentIds.Count == 0
                || partPersistentIds == null
                || partPersistentIds.Count == 0)
            {
                return 0;
            }

            int sharedPartCount = 0;
            for (int partIndex = 0; partIndex < partPersistentIds.Count; partIndex++)
            {
                uint partPersistentId = partPersistentIds[partIndex];
                if (partPersistentId != 0u && _partPersistentIds.Contains(partPersistentId))
                {
                    sharedPartCount++;
                }
            }

            return sharedPartCount;
        }

        /// <summary>
        /// Seeds a new attempt from its first persistent-part set, then narrows that lineage when
        /// staging removes parts. Newly attached parts are deliberately not absorbed: docking keeps
        /// each remembered Flight Attempt separate even while several lineages share one KSP vessel.
        /// </summary>
        internal void ReconcilePartLineage(IReadOnlyList<uint> partPersistentIds)
        {
            if (partPersistentIds == null || partPersistentIds.Count == 0)
            {
                return;
            }

            if (_partPersistentIds.Count == 0)
            {
                for (int partIndex = 0; partIndex < partPersistentIds.Count; partIndex++)
                {
                    uint partPersistentId = partPersistentIds[partIndex];
                    if (partPersistentId != 0u)
                    {
                        _partPersistentIds.Add(partPersistentId);
                    }
                }

                return;
            }

            List<uint> removedPartPersistentIds = null;
            foreach (uint rememberedPartPersistentId in _partPersistentIds)
            {
                if (ContainsPartPersistentId(partPersistentIds, rememberedPartPersistentId))
                {
                    continue;
                }

                if (removedPartPersistentIds == null)
                {
                    removedPartPersistentIds = new List<uint>();
                }

                removedPartPersistentIds.Add(rememberedPartPersistentId);
            }

            if (removedPartPersistentIds == null)
            {
                return;
            }

            for (int partIndex = 0; partIndex < removedPartPersistentIds.Count; partIndex++)
            {
                _partPersistentIds.Remove(removedPartPersistentIds[partIndex]);
            }
        }

        /// <summary>
        /// Observes parts currently attached to the selected attempt but not owned by its lineage.
        /// The first usable observation only establishes a baseline, which lets a saved partial
        /// Control hold resume after load without inventing a topology change. Later additions or
        /// removals represent docking, undocking, construction, or another external attachment change.
        /// </summary>
        internal bool ObserveAttachedPartTopology(IReadOnlyList<uint> vesselPartPersistentIds)
        {
            if (!HasPartLineage
                || vesselPartPersistentIds == null
                || vesselPartPersistentIds.Count == 0)
            {
                _attachedPartPersistentIds.Clear();
                _hasObservedAttachedPartTopology = false;
                return false;
            }

            int attachedPartCount = 0;
            bool topologyChanged = false;
            for (int partIndex = 0; partIndex < vesselPartPersistentIds.Count; partIndex++)
            {
                uint partPersistentId = vesselPartPersistentIds[partIndex];
                if (partPersistentId == 0u || _partPersistentIds.Contains(partPersistentId))
                {
                    continue;
                }

                attachedPartCount++;
                if (!_attachedPartPersistentIds.Contains(partPersistentId))
                {
                    topologyChanged = true;
                }
            }

            if (attachedPartCount != _attachedPartPersistentIds.Count)
            {
                topologyChanged = true;
            }

            bool didTopologyChange = _hasObservedAttachedPartTopology && topologyChanged;

            _attachedPartPersistentIds.Clear();
            for (int partIndex = 0; partIndex < vesselPartPersistentIds.Count; partIndex++)
            {
                uint partPersistentId = vesselPartPersistentIds[partIndex];
                if (partPersistentId != 0u && !_partPersistentIds.Contains(partPersistentId))
                {
                    _attachedPartPersistentIds.Add(partPersistentId);
                }
            }

            _hasObservedAttachedPartTopology = true;
            return didTopologyChange;
        }

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
            _partPersistentIds.Clear();
            _attachedPartPersistentIds.Clear();
            _hasObservedAttachedPartTopology = false;
            _controlStates.Clear();
        }

        private static bool ContainsPartPersistentId(
            IReadOnlyList<uint> partPersistentIds,
            uint partPersistentId)
        {
            for (int partIndex = 0; partIndex < partPersistentIds.Count; partIndex++)
            {
                if (partPersistentIds[partIndex] == partPersistentId)
                {
                    return true;
                }
            }

            return false;
        }

        internal sealed class ControlContractState
        {
            internal double HoldSeconds;
            internal bool WasSampleInBand;
            internal bool IsQualified;
        }
    }
}

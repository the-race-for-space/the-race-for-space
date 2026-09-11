using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;

namespace TheRaceForSpace.Persistence
{
    /// <summary>
    /// Persists all simulated rival agencies by stable agency ID. The collection owns matching and
    /// deterministic save order; RivalProgramSaveState owns the mutable state for one rival.
    /// </summary>
    public sealed class RivalAgenciesSaveState
    {
        private const string RivalNodeName = "RIVAL";

        private readonly Dictionary<string, RivalProgramSaveState> _statesByAgencyId =
            new Dictionary<string, RivalProgramSaveState>(StringComparer.OrdinalIgnoreCase);

        public bool HasData
        {
            get { return _statesByAgencyId.Count > 0; }
        }

        public void Capture(IList<AgencyState> rivalAgencies)
        {
            _statesByAgencyId.Clear();
            if (rivalAgencies == null)
            {
                return;
            }

            for (int agencyIndex = 0; agencyIndex < rivalAgencies.Count; agencyIndex++)
            {
                AgencyState agency = rivalAgencies[agencyIndex];
                if (agency == null || agency.IsPlayer || string.IsNullOrEmpty(agency.Id))
                {
                    continue;
                }

                var state = new RivalProgramSaveState();
                state.Capture(agency);
                if (state.HasData)
                {
                    _statesByAgencyId[agency.Id] = state;
                }
            }
        }

        public void ApplyTo(IList<AgencyState> rivalAgencies)
        {
            if (rivalAgencies == null)
            {
                return;
            }

            for (int agencyIndex = 0; agencyIndex < rivalAgencies.Count; agencyIndex++)
            {
                AgencyState agency = rivalAgencies[agencyIndex];
                if (agency == null || agency.IsPlayer || string.IsNullOrEmpty(agency.Id))
                {
                    continue;
                }

                RivalProgramSaveState state;
                if (_statesByAgencyId.TryGetValue(agency.Id, out state))
                {
                    state.ApplyTo(agency);
                }
            }
        }

        public void Load(ConfigNode node)
        {
            _statesByAgencyId.Clear();
            if (node == null)
            {
                return;
            }

            ConfigNode[] rivalNodes = node.GetNodes(RivalNodeName);
            for (int nodeIndex = 0; nodeIndex < rivalNodes.Length; nodeIndex++)
            {
                var state = new RivalProgramSaveState();
                state.Load(rivalNodes[nodeIndex]);
                if (!state.HasData || string.IsNullOrEmpty(state.AgencyId))
                {
                    continue;
                }

                // Stable IDs are unique in the runtime collection. If malformed save data repeats
                // an ID, the last valid node wins rather than inventing another rival identity.
                _statesByAgencyId[state.AgencyId] = state;
            }
        }

        public void Save(ConfigNode node)
        {
            if (node == null)
            {
                return;
            }

            var programIds = new List<string>(_statesByAgencyId.Keys);
            programIds.Sort(StringComparer.OrdinalIgnoreCase);

            for (int agencyIndex = 0; agencyIndex < programIds.Count; agencyIndex++)
            {
                RivalProgramSaveState state = _statesByAgencyId[programIds[agencyIndex]];
                state.Save(node.AddNode(RivalNodeName));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using TheRaceForSpace.Agencies;

namespace TheRaceForSpace.Persistence
{
    /// <summary>
    /// Persists all simulated rival agencies by stable agency ID. Each saved rival keeps only
    /// mutable campaign state; display names and other presentation values remain code-owned.
    /// </summary>
    public sealed class RivalAgenciesSaveState
    {
        private const string RivalNodeName = "RIVAL";

        private readonly Dictionary<string, SavedRivalProgram> _statesByAgencyId =
            new Dictionary<string, SavedRivalProgram>(StringComparer.OrdinalIgnoreCase);

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

                var state = new SavedRivalProgram();
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

                SavedRivalProgram state;
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
                var state = new SavedRivalProgram();
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
                SavedRivalProgram state = _statesByAgencyId[programIds[agencyIndex]];
                state.Save(node.AddNode(RivalNodeName));
            }
        }

        /// <summary>
        /// One persisted rival entry. Kept private because the runtime always captures/restores the
        /// rival collection as a unit and never needs an independent public rival save-state API.
        /// </summary>
        private sealed class SavedRivalProgram
        {
            private const string ObjectiveCompletionNodeName = "OBJECTIVE_COMPLETION";
            private const string SatelliteNodeName = "SATELLITE";
            private const string IdValueName = "id";
            private const string UniversalTimeValueName = "universalTime";
            private const string BodyValueName = "body";
            private const string CountValueName = "count";
            private const string AgencyIdValueName = "programId";
            private const string FundsValueName = "funds";
            private const string NextMissionTargetIdValueName = "nextMissionTargetId";
            private const string MissionProgressPercentValueName = "launchProgressPercent";
            private const string NextMissionProgressCheckUniversalTimeValueName =
                "nextLaunchProgressCheckUniversalTime";

            private readonly Dictionary<string, double> _objectiveCompletionTimesById =
                new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, int> _satellitesByBody =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            public bool HasData { get; private set; }
            public string AgencyId { get; private set; }
            public double Funds { get; private set; }
            public string NextMissionTargetId { get; private set; }
            public int MissionProgressPercent { get; private set; }
            public double NextMissionProgressCheckUniversalTime { get; private set; }

            public void Capture(AgencyState agency)
            {
                ClearState();
                if (agency == null || agency.IsPlayer || string.IsNullOrEmpty(agency.Id))
                {
                    return;
                }

                HasData = true;
                AgencyId = agency.Id;
                Funds = Math.Max(0.0, agency.Funds);
                NextMissionTargetId = agency.NextMissionTargetId;
                MissionProgressPercent = Math.Max(0, Math.Min(100, agency.MissionProgressPercent));
                NextMissionProgressCheckUniversalTime = Math.Max(
                    0.0,
                    agency.NextMissionProgressCheckUniversalTime);

                foreach (KeyValuePair<string, double> objectiveCompletion in agency.ObjectiveCompletionTimes)
                {
                    if (!string.IsNullOrEmpty(objectiveCompletion.Key)
                        && IsFinite(objectiveCompletion.Value))
                    {
                        _objectiveCompletionTimesById[objectiveCompletion.Key] = Math.Max(0.0, objectiveCompletion.Value);
                    }
                }

                foreach (KeyValuePair<string, int> bodyCount in agency.SatelliteCountsByBody)
                {
                    if (!string.IsNullOrEmpty(bodyCount.Key))
                    {
                        _satellitesByBody[bodyCount.Key] = Math.Max(0, bodyCount.Value);
                    }
                }
            }

            public void ApplyTo(AgencyState agency)
            {
                if (!HasData
                    || agency == null
                    || agency.IsPlayer
                    || (!string.IsNullOrEmpty(AgencyId)
                        && !string.Equals(AgencyId, agency.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                agency.Funds = Math.Max(0.0, Funds);
                agency.ClearSatelliteCounts();
                agency.ClearObjectiveCompletionTimes();

                foreach (KeyValuePair<string, int> bodyCount in _satellitesByBody)
                {
                    agency.SetSatelliteCount(bodyCount.Key, bodyCount.Value);
                }

                foreach (KeyValuePair<string, double> objectiveCompletion in _objectiveCompletionTimesById)
                {
                    agency.RecordObjectiveCompletion(objectiveCompletion.Key, objectiveCompletion.Value);
                }

                agency.NextMissionTargetId = NextMissionTargetId;
                // Presentation text is derived from the live target collections on the next rival
                // simulation refresh. Persistence stores only the stable mission identity.
                agency.NextMissionDisplayName = null;
                agency.MissionProgressPercent = Math.Max(0, Math.Min(100, MissionProgressPercent));
                agency.NextMissionProgressCheckUniversalTime = Math.Max(
                    0.0,
                    NextMissionProgressCheckUniversalTime);
            }

            public void Load(ConfigNode node)
            {
                ClearState();
                HasData = node != null;
                if (!HasData)
                {
                    return;
                }

                AgencyId = node.GetValue(AgencyIdValueName);

                double parsedDouble;
                if (TryParseFiniteDouble(node.GetValue(FundsValueName), out parsedDouble))
                {
                    Funds = Math.Max(0.0, parsedDouble);
                }

                string targetId = node.GetValue(NextMissionTargetIdValueName);
                if (!string.IsNullOrEmpty(targetId))
                {
                    NextMissionTargetId = targetId;
                }

                int parsedInt;
                if (int.TryParse(
                    node.GetValue(MissionProgressPercentValueName),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out parsedInt))
                {
                    MissionProgressPercent = Math.Max(0, Math.Min(100, parsedInt));
                }

                if (TryParseFiniteDouble(
                    node.GetValue(NextMissionProgressCheckUniversalTimeValueName),
                    out parsedDouble))
                {
                    NextMissionProgressCheckUniversalTime = Math.Max(0.0, parsedDouble);
                }

                ConfigNode[] completionNodes = node.GetNodes(ObjectiveCompletionNodeName);
                for (int nodeIndex = 0; nodeIndex < completionNodes.Length; nodeIndex++)
                {
                    ConfigNode completionNode = completionNodes[nodeIndex];
                    string id = completionNode.GetValue(IdValueName);
                    double universalTime;
                    if (string.IsNullOrEmpty(id)
                        || !TryParseFiniteDouble(
                            completionNode.GetValue(UniversalTimeValueName),
                            out universalTime))
                    {
                        continue;
                    }

                    StoreObjectiveCompletion(id, universalTime);
                }

                ConfigNode[] satelliteNodes = node.GetNodes(SatelliteNodeName);
                for (int nodeIndex = 0; nodeIndex < satelliteNodes.Length; nodeIndex++)
                {
                    ConfigNode satelliteNode = satelliteNodes[nodeIndex];
                    string bodyName = satelliteNode.GetValue(BodyValueName);
                    int count;
                    if (string.IsNullOrEmpty(bodyName)
                        || !int.TryParse(
                            satelliteNode.GetValue(CountValueName),
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out count))
                    {
                        continue;
                    }

                    _satellitesByBody[bodyName] = Math.Max(0, count);
                }
            }

            public void Save(ConfigNode node)
            {
                if (!HasData || node == null)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(AgencyId))
                {
                    node.AddValue(AgencyIdValueName, AgencyId);
                }

                node.AddValue(FundsValueName, Funds.ToString("R", CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(NextMissionTargetId))
                {
                    node.AddValue(NextMissionTargetIdValueName, NextMissionTargetId);
                }

                node.AddValue(
                    MissionProgressPercentValueName,
                    MissionProgressPercent.ToString(CultureInfo.InvariantCulture));
                node.AddValue(
                    NextMissionProgressCheckUniversalTimeValueName,
                    NextMissionProgressCheckUniversalTime.ToString("R", CultureInfo.InvariantCulture));

                var objectiveIds = new List<string>(_objectiveCompletionTimesById.Keys);
                objectiveIds.Sort(StringComparer.OrdinalIgnoreCase);
                for (int idIndex = 0; idIndex < objectiveIds.Count; idIndex++)
                {
                    string id = objectiveIds[idIndex];
                    ConfigNode completionNode = node.AddNode(ObjectiveCompletionNodeName);
                    completionNode.AddValue(IdValueName, id);
                    completionNode.AddValue(
                        UniversalTimeValueName,
                        _objectiveCompletionTimesById[id].ToString("R", CultureInfo.InvariantCulture));
                }

                var bodyNames = new List<string>(_satellitesByBody.Keys);
                bodyNames.Sort(StringComparer.OrdinalIgnoreCase);
                for (int bodyIndex = 0; bodyIndex < bodyNames.Count; bodyIndex++)
                {
                    string bodyName = bodyNames[bodyIndex];
                    ConfigNode satelliteNode = node.AddNode(SatelliteNodeName);
                    satelliteNode.AddValue(BodyValueName, bodyName);
                    satelliteNode.AddValue(
                        CountValueName,
                        _satellitesByBody[bodyName].ToString(CultureInfo.InvariantCulture));
                }
            }

            private void ClearState()
            {
                HasData = false;
                AgencyId = null;
                Funds = 0.0;
                NextMissionTargetId = null;
                MissionProgressPercent = 0;
                NextMissionProgressCheckUniversalTime = 0.0;
                _objectiveCompletionTimesById.Clear();
                _satellitesByBody.Clear();
            }

            private void StoreObjectiveCompletion(string id, double universalTime)
            {
                universalTime = Math.Max(0.0, universalTime);
                double existingTime;
                if (!_objectiveCompletionTimesById.TryGetValue(id, out existingTime)
                    || universalTime < existingTime)
                {
                    _objectiveCompletionTimesById[id] = universalTime;
                }
            }

            private static bool TryParseFiniteDouble(string value, out double parsedValue)
            {
                parsedValue = 0.0;
                return !string.IsNullOrEmpty(value)
                    && double.TryParse(
                        value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out parsedValue)
                    && IsFinite(parsedValue);
            }

            private static bool IsFinite(double value)
            {
                return !double.IsNaN(value) && !double.IsInfinity(value);
            }
        }
    }
}

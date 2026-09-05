using System;
using System.Collections.Generic;

namespace TheRaceForSpace.Agencies
{
    /// <summary>
    /// Campaign state for one player or rival space agency.
    /// </summary>
    public sealed class AgencyState
    {
        private readonly Dictionary<string, double> _objectiveCompletionTimesById =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _satellitesByBody =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public AgencyState(string name, bool isPlayer)
            : this(name, name, isPlayer)
        {
        }

        public AgencyState(string id, string name, bool isPlayer)
        {
            Id = string.IsNullOrEmpty(id) ? name : id;
            Name = name;
            IsPlayer = isPlayer;
        }

        /// <summary>
        /// Stable agency identity used by collection-driven campaign logic. Display names may change
        /// independently without changing which agency a piece of state belongs to.
        /// </summary>
        public string Id { get; private set; }
        public string Name { get; private set; }
        public bool IsPlayer { get; private set; }

        // Rival Funds is their simulated spendable balance; the player's real balance remains owned by KSP.
        public double Funds { get; set; }
        public double NextPayoutFunds { get; set; }

        public string NextMissionTargetId { get; set; }
        public string NextMissionDisplayName { get; set; }
        public int MissionProgressPercent { get; set; }
        public double NextMissionProgressCheckUniversalTime { get; set; }

        // Persistence enumerates these project-owned collections directly so future objective IDs
        // and celestial bodies do not require another fixed field on AgencyState.
        internal IEnumerable<KeyValuePair<string, double>> ObjectiveCompletionTimes
        {
            get { return _objectiveCompletionTimesById; }
        }

        internal IEnumerable<KeyValuePair<string, int>> SatelliteCountsByBody
        {
            get { return _satellitesByBody; }
        }

        internal void ClearObjectiveCompletionTimes()
        {
            _objectiveCompletionTimesById.Clear();
        }

        internal void ClearSatelliteCounts()
        {
            _satellitesByBody.Clear();
        }

        /// <summary>
        /// Returns whether this agency has permanently recorded the objective ID.
        /// </summary>
        public bool HasCompletedObjective(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId))
            {
                return false;
            }

            return _objectiveCompletionTimesById.ContainsKey(objectiveId);
        }

        /// <summary>
        /// Returns the first recorded universal time for a objective, or -1 when it has not been achieved.
        /// </summary>
        public double GetObjectiveCompletionTime(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId))
            {
                return -1.0;
            }

            double completionUniversalTime;
            return _objectiveCompletionTimesById.TryGetValue(objectiveId, out completionUniversalTime)
                ? completionUniversalTime
                : -1.0;
        }

        /// <summary>
        /// Permanently records the first observation of a objective. Later observations do not
        /// replace its timestamp because funding eligibility depends on the original objective completion time.
        /// </summary>
        public bool RecordObjectiveCompletion(string objectiveId, double universalTime)
        {
            if (string.IsNullOrEmpty(objectiveId)
                || double.IsNaN(universalTime)
                || double.IsInfinity(universalTime)
                || _objectiveCompletionTimesById.ContainsKey(objectiveId))
            {
                return false;
            }

            _objectiveCompletionTimesById[objectiveId] = Math.Max(0.0, universalTime);
            return true;
        }

        public int GetSatelliteCount(string celestialBodyName)
        {
            if (string.IsNullOrEmpty(celestialBodyName))
            {
                return 0;
            }

            int count;
            return _satellitesByBody.TryGetValue(celestialBodyName, out count) ? count : 0;
        }

        public void SetSatelliteCount(string celestialBodyName, int count)
        {
            if (string.IsNullOrEmpty(celestialBodyName))
            {
                return;
            }

            if (count <= 0)
            {
                _satellitesByBody.Remove(celestialBodyName);
                return;
            }

            _satellitesByBody[celestialBodyName] = count;
        }
    }
}

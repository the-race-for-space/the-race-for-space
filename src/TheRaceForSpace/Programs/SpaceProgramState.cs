using System;
using System.Collections.Generic;

namespace TheRaceForSpace.Programs
{
    /// <summary>
    /// Campaign state for one player or rival space program.
    /// </summary>
    public sealed class SpaceProgramState
    {
        private readonly Dictionary<string, double> _achievementTimesById =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _satellitesByBody =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public SpaceProgramState(string name, bool isPlayer)
            : this(name, name, isPlayer)
        {
        }

        public SpaceProgramState(string id, string name, bool isPlayer)
        {
            Id = string.IsNullOrEmpty(id) ? name : id;
            Name = name;
            IsPlayer = isPlayer;
        }

        /// <summary>
        /// Stable program identity used by collection-driven race logic. Display names may change
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

        // In-assembly bridge for the remaining presentation/test call sites while the public API
        // uses the clearer mission-display terminology. This is deliberately not public API.
        internal string NextLaunchBodyName
        {
            get { return NextMissionDisplayName; }
            set { NextMissionDisplayName = value; }
        }

        public int LaunchProgressPercent { get; set; }
        public double NextLaunchProgressCheckUniversalTime { get; set; }

        // Persistence enumerates these project-owned collections directly so future milestone IDs
        // and celestial bodies do not require another fixed field on SpaceProgramState.
        internal IEnumerable<KeyValuePair<string, double>> RecordedAchievements
        {
            get { return _achievementTimesById; }
        }

        internal IEnumerable<KeyValuePair<string, int>> SatelliteCountsByBody
        {
            get { return _satellitesByBody; }
        }

        internal void ClearRecordedAchievements()
        {
            _achievementTimesById.Clear();
        }

        internal void ClearSatelliteCounts()
        {
            _satellitesByBody.Clear();
        }

        /// <summary>
        /// Returns whether this program has permanently recorded the milestone ID.
        /// </summary>
        public bool HasAchievement(string milestoneId)
        {
            if (string.IsNullOrEmpty(milestoneId))
            {
                return false;
            }

            return _achievementTimesById.ContainsKey(milestoneId);
        }

        /// <summary>
        /// Returns the first recorded universal time for a milestone, or -1 when it has not been achieved.
        /// </summary>
        public double GetAchievementUniversalTime(string milestoneId)
        {
            if (string.IsNullOrEmpty(milestoneId))
            {
                return -1.0;
            }

            double achievementUniversalTime;
            return _achievementTimesById.TryGetValue(milestoneId, out achievementUniversalTime)
                ? achievementUniversalTime
                : -1.0;
        }

        /// <summary>
        /// Permanently records the first observation of a milestone. Later observations do not
        /// replace its timestamp because funding eligibility depends on the original achievement time.
        /// </summary>
        public bool RecordAchievement(string milestoneId, double universalTime)
        {
            if (string.IsNullOrEmpty(milestoneId)
                || double.IsNaN(universalTime)
                || double.IsInfinity(universalTime)
                || _achievementTimesById.ContainsKey(milestoneId))
            {
                return false;
            }

            _achievementTimesById[milestoneId] = Math.Max(0.0, universalTime);
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

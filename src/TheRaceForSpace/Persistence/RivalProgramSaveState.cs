using System;
using System.Collections.Generic;
using System.Globalization;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Persistence
{
    /// <summary>
    /// Serializable rival-program values required to resume a simulated agency. Achievements,
    /// satellite bodies, and the active mission are stored by stable identifiers rather than a
    /// fixed prototype field list.
    /// </summary>
    public sealed class RivalProgramSaveState
    {
        private const string AchievementNodeName = "ACHIEVEMENT";
        private const string SatelliteNodeName = "SATELLITE";
        private const string IdValueName = "id";
        private const string UniversalTimeValueName = "universalTime";
        private const string BodyValueName = "body";
        private const string CountValueName = "count";
        private const string ProgramIdValueName = "programId";
        private const string FundsValueName = "funds";
        private const string NextMissionTargetIdValueName = "nextMissionTargetId";
        private const string LaunchProgressPercentValueName = "launchProgressPercent";
        private const string NextLaunchProgressCheckUniversalTimeValueName =
            "nextLaunchProgressCheckUniversalTime";

        private readonly Dictionary<string, double> _achievementTimesById =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _satellitesByBody =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public bool HasData { get; private set; }
        public string ProgramId { get; private set; }
        public double Funds { get; private set; }
        public string NextMissionTargetId { get; private set; }
        public int LaunchProgressPercent { get; private set; }
        public double NextLaunchProgressCheckUniversalTime { get; private set; }

        public void Capture(SpaceProgramState program)
        {
            if (program == null || program.IsPlayer || string.IsNullOrEmpty(program.Id))
            {
                return;
            }

            ClearState();
            HasData = true;
            ProgramId = program.Id;
            Funds = Math.Max(0.0, program.Funds);
            NextMissionTargetId = program.NextMissionTargetId;
            LaunchProgressPercent = Math.Max(0, Math.Min(100, program.LaunchProgressPercent));
            NextLaunchProgressCheckUniversalTime = Math.Max(
                0.0,
                program.NextLaunchProgressCheckUniversalTime);

            foreach (KeyValuePair<string, double> achievement in program.RecordedAchievements)
            {
                if (!string.IsNullOrEmpty(achievement.Key)
                    && !double.IsNaN(achievement.Value)
                    && !double.IsInfinity(achievement.Value))
                {
                    _achievementTimesById[achievement.Key] = Math.Max(0.0, achievement.Value);
                }
            }

            foreach (KeyValuePair<string, int> bodyCount in program.SatelliteCountsByBody)
            {
                if (!string.IsNullOrEmpty(bodyCount.Key))
                {
                    _satellitesByBody[bodyCount.Key] = Math.Max(0, bodyCount.Value);
                }
            }
        }

        public void ApplyTo(SpaceProgramState program)
        {
            if (!HasData
                || program == null
                || program.IsPlayer
                || (!string.IsNullOrEmpty(ProgramId)
                    && !string.Equals(ProgramId, program.Id, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            program.Funds = Math.Max(0.0, Funds);
            program.ClearSatelliteCounts();
            program.ClearRecordedAchievements();

            foreach (KeyValuePair<string, int> bodyCount in _satellitesByBody)
            {
                program.SetSatelliteCount(bodyCount.Key, bodyCount.Value);
            }

            foreach (KeyValuePair<string, double> achievement in _achievementTimesById)
            {
                program.RecordAchievement(achievement.Key, achievement.Value);
            }

            program.NextMissionTargetId = NextMissionTargetId;
            // Presentation text is derived from the live target collections on the next rival
            // simulation refresh. Persistence stores only the stable mission identity.
            program.NextLaunchBodyName = null;
            program.LaunchProgressPercent = Math.Max(0, Math.Min(100, LaunchProgressPercent));
            program.NextLaunchProgressCheckUniversalTime = Math.Max(
                0.0,
                NextLaunchProgressCheckUniversalTime);
        }

        public void Load(ConfigNode node)
        {
            ClearState();
            HasData = node != null;
            if (!HasData)
            {
                return;
            }

            ProgramId = node.GetValue(ProgramIdValueName);

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
                node.GetValue(LaunchProgressPercentValueName),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out parsedInt))
            {
                LaunchProgressPercent = Math.Max(0, Math.Min(100, parsedInt));
            }

            if (TryParseFiniteDouble(
                node.GetValue(NextLaunchProgressCheckUniversalTimeValueName),
                out parsedDouble))
            {
                NextLaunchProgressCheckUniversalTime = Math.Max(0.0, parsedDouble);
            }

            ConfigNode[] achievementNodes = node.GetNodes(AchievementNodeName);
            for (int nodeIndex = 0; nodeIndex < achievementNodes.Length; nodeIndex++)
            {
                ConfigNode achievementNode = achievementNodes[nodeIndex];
                string id = achievementNode.GetValue(IdValueName);
                double universalTime;
                if (string.IsNullOrEmpty(id)
                    || !TryParseFiniteDouble(achievementNode.GetValue(UniversalTimeValueName), out universalTime))
                {
                    continue;
                }

                StoreAchievement(id, universalTime);
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

            if (!string.IsNullOrEmpty(ProgramId))
            {
                node.AddValue(ProgramIdValueName, ProgramId);
            }

            node.AddValue(FundsValueName, Funds.ToString("R", CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(NextMissionTargetId))
            {
                node.AddValue(NextMissionTargetIdValueName, NextMissionTargetId);
            }

            node.AddValue(
                LaunchProgressPercentValueName,
                LaunchProgressPercent.ToString(CultureInfo.InvariantCulture));
            node.AddValue(
                NextLaunchProgressCheckUniversalTimeValueName,
                NextLaunchProgressCheckUniversalTime.ToString("R", CultureInfo.InvariantCulture));

            var achievementIds = new List<string>(_achievementTimesById.Keys);
            achievementIds.Sort(StringComparer.OrdinalIgnoreCase);
            for (int idIndex = 0; idIndex < achievementIds.Count; idIndex++)
            {
                string id = achievementIds[idIndex];
                ConfigNode achievementNode = node.AddNode(AchievementNodeName);
                achievementNode.AddValue(IdValueName, id);
                achievementNode.AddValue(
                    UniversalTimeValueName,
                    _achievementTimesById[id].ToString("R", CultureInfo.InvariantCulture));
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
            ProgramId = null;
            Funds = 0.0;
            NextMissionTargetId = null;
            LaunchProgressPercent = 0;
            NextLaunchProgressCheckUniversalTime = 0.0;
            _achievementTimesById.Clear();
            _satellitesByBody.Clear();
        }

        private void StoreAchievement(string id, double universalTime)
        {
            universalTime = Math.Max(0.0, universalTime);
            double existingTime;
            if (!_achievementTimesById.TryGetValue(id, out existingTime) || universalTime < existingTime)
            {
                _achievementTimesById[id] = universalTime;
            }
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

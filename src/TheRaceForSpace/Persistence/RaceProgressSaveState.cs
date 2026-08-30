using System;
using System.Collections.Generic;
using System.Globalization;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Persistence
{
    /// <summary>
    /// Serializable campaign progression that is not owned directly by KSP vessel state.
    /// Stable IDs are persisted in repeated child nodes so new milestones and programmes do not
    /// require another fixed save field.
    /// </summary>
    public sealed class RaceProgressSaveState
    {
        private const string AchievementNodeName = "ACHIEVEMENT";
        private const string FundingProgrammeNodeName = "FUNDING_PROGRAMME";
        private const string ContractNodeName = "CONTRACT";
        private const string IdValueName = "id";
        private const string UniversalTimeValueName = "universalTime";
        private const string StartedValueName = "started";
        private const string PaymentsProcessedValueName = "paymentsProcessed";

        private readonly Dictionary<string, double> _achievementTimesById =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _unlockedFundingProgrammeIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _contractStartedById =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _contractPaymentsProcessedById =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public bool HasData { get; private set; }

        public void Capture(
            SpaceProgramState playerProgram,
            IList<FundingProgramme> fundingProgrammes,
            IList<AchievementFundingProgramme> achievementProgrammes)
        {
            if (playerProgram == null || fundingProgrammes == null || achievementProgrammes == null)
            {
                return;
            }

            ClearState();
            HasData = true;

            foreach (KeyValuePair<string, double> achievement in playerProgram.RecordedAchievements)
            {
                if (!string.IsNullOrEmpty(achievement.Key)
                    && !double.IsNaN(achievement.Value)
                    && !double.IsInfinity(achievement.Value))
                {
                    _achievementTimesById[achievement.Key] = Math.Max(0.0, achievement.Value);
                }
            }

            for (int programmeIndex = 0; programmeIndex < fundingProgrammes.Count; programmeIndex++)
            {
                FundingProgramme programme = fundingProgrammes[programmeIndex];
                if (programme != null && programme.IsAvailable && !string.IsNullOrEmpty(programme.Id))
                {
                    _unlockedFundingProgrammeIds.Add(programme.Id);
                }
            }

            for (int programmeIndex = 0; programmeIndex < achievementProgrammes.Count; programmeIndex++)
            {
                AchievementFundingProgramme programme = achievementProgrammes[programmeIndex];
                if (programme == null || string.IsNullOrEmpty(programme.Id))
                {
                    continue;
                }

                _contractStartedById[programme.Id] = programme.HasStarted;
                _contractPaymentsProcessedById[programme.Id] = Math.Max(
                    0,
                    Math.Min(10, programme.PaymentsProcessed));
            }
        }

        public void ApplyTo(
            SpaceProgramState playerProgram,
            IList<FundingProgramme> fundingProgrammes,
            IList<AchievementFundingProgramme> achievementProgrammes)
        {
            if (!HasData || playerProgram == null || fundingProgrammes == null || achievementProgrammes == null)
            {
                return;
            }

            foreach (KeyValuePair<string, double> achievement in _achievementTimesById)
            {
                playerProgram.RecordAchievement(achievement.Key, achievement.Value);
            }

            for (int programmeIndex = 0; programmeIndex < fundingProgrammes.Count; programmeIndex++)
            {
                FundingProgramme programme = fundingProgrammes[programmeIndex];
                if (programme != null
                    && !string.IsNullOrEmpty(programme.Id)
                    && _unlockedFundingProgrammeIds.Contains(programme.Id))
                {
                    programme.Unlock();
                }
            }

            for (int programmeIndex = 0; programmeIndex < achievementProgrammes.Count; programmeIndex++)
            {
                AchievementFundingProgramme programme = achievementProgrammes[programmeIndex];
                if (programme == null || string.IsNullOrEmpty(programme.Id))
                {
                    continue;
                }

                bool started;
                int paymentsProcessed;
                if (_contractStartedById.TryGetValue(programme.Id, out started)
                    && _contractPaymentsProcessedById.TryGetValue(programme.Id, out paymentsProcessed))
                {
                    programme.RestoreState(started, paymentsProcessed);
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

                universalTime = Math.Max(0.0, universalTime);
                double existingTime;
                if (!_achievementTimesById.TryGetValue(id, out existingTime) || universalTime < existingTime)
                {
                    _achievementTimesById[id] = universalTime;
                }
            }

            ConfigNode[] fundingNodes = node.GetNodes(FundingProgrammeNodeName);
            for (int nodeIndex = 0; nodeIndex < fundingNodes.Length; nodeIndex++)
            {
                string id = fundingNodes[nodeIndex].GetValue(IdValueName);
                if (!string.IsNullOrEmpty(id))
                {
                    _unlockedFundingProgrammeIds.Add(id);
                }
            }

            ConfigNode[] contractNodes = node.GetNodes(ContractNodeName);
            for (int nodeIndex = 0; nodeIndex < contractNodes.Length; nodeIndex++)
            {
                ConfigNode contractNode = contractNodes[nodeIndex];
                string id = contractNode.GetValue(IdValueName);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                _contractStartedById[id] = ParseBool(contractNode.GetValue(StartedValueName));
                _contractPaymentsProcessedById[id] = ParsePaymentCount(
                    contractNode.GetValue(PaymentsProcessedValueName));
            }
        }

        public void Save(ConfigNode node)
        {
            if (!HasData || node == null)
            {
                return;
            }

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

            var fundingProgrammeIds = new List<string>(_unlockedFundingProgrammeIds);
            fundingProgrammeIds.Sort(StringComparer.OrdinalIgnoreCase);
            for (int idIndex = 0; idIndex < fundingProgrammeIds.Count; idIndex++)
            {
                ConfigNode fundingNode = node.AddNode(FundingProgrammeNodeName);
                fundingNode.AddValue(IdValueName, fundingProgrammeIds[idIndex]);
            }

            var contractIds = new List<string>(_contractStartedById.Keys);
            contractIds.Sort(StringComparer.OrdinalIgnoreCase);
            for (int idIndex = 0; idIndex < contractIds.Count; idIndex++)
            {
                string id = contractIds[idIndex];
                ConfigNode contractNode = node.AddNode(ContractNodeName);
                contractNode.AddValue(IdValueName, id);
                contractNode.AddValue(StartedValueName, _contractStartedById[id]);
                contractNode.AddValue(
                    PaymentsProcessedValueName,
                    _contractPaymentsProcessedById[id].ToString(CultureInfo.InvariantCulture));
            }
        }

        private void ClearState()
        {
            HasData = false;
            _achievementTimesById.Clear();
            _unlockedFundingProgrammeIds.Clear();
            _contractStartedById.Clear();
            _contractPaymentsProcessedById.Clear();
        }

        private static bool ParseBool(string value)
        {
            bool parsedValue;
            return !string.IsNullOrEmpty(value) && bool.TryParse(value, out parsedValue) && parsedValue;
        }

        private static int ParsePaymentCount(string value)
        {
            int parsedValue;
            if (string.IsNullOrEmpty(value)
                || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue))
            {
                return 0;
            }

            return Math.Max(0, Math.Min(10, parsedValue));
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

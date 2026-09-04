using System;
using System.Collections.Generic;
using System.Globalization;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Persistence
{
    /// <summary>
    /// Persists the player's achievement history and the lifecycle state of every configured
    /// achievement and satellite funding contract. Contract definitions remain code-owned; only
    /// mutable campaign state is stored by stable ID.
    /// </summary>
    public sealed class FundingContractsSaveState
    {
        private const string PlayerAchievementNodeName = "PLAYER_ACHIEVEMENT";
        private const string AchievementContractNodeName = "ACHIEVEMENT_CONTRACT";
        private const string SatelliteContractNodeName = "SATELLITE_CONTRACT";
        private const string IdValueName = "id";
        private const string UniversalTimeValueName = "universalTime";
        private const string AvailableValueName = "available";
        private const string OfferedValueName = "offered";
        private const string StartedValueName = "started";
        private const string PaymentsProcessedValueName = "paymentsProcessed";
        private const string SatelliteTargetReachedValueName = "targetReached";
        private const string NextFundingUniversalTimeValueName = "nextFundingUniversalTime";

        private readonly Dictionary<string, double> _playerAchievementTimesById =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SavedAchievementContract> _achievementContractsById =
            new Dictionary<string, SavedAchievementContract>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SavedSatelliteContract> _satelliteContractsById =
            new Dictionary<string, SavedSatelliteContract>(StringComparer.OrdinalIgnoreCase);

        public bool HasData { get; private set; }
        public double NextFundingUniversalTime { get; private set; }

        public void Capture(
            SpaceProgramState playerProgram,
            IList<FundingProgramme> satelliteContracts,
            IList<AchievementFundingProgramme> achievementContracts)
        {
            Capture(playerProgram, satelliteContracts, achievementContracts, -1.0);
        }

        public void Capture(
            SpaceProgramState playerProgram,
            IList<FundingProgramme> satelliteContracts,
            IList<AchievementFundingProgramme> achievementContracts,
            double nextFundingUniversalTime)
        {
            if (playerProgram == null || satelliteContracts == null || achievementContracts == null)
            {
                return;
            }

            ClearState();
            HasData = true;

            if (IsFinite(nextFundingUniversalTime) && nextFundingUniversalTime >= 0.0)
            {
                NextFundingUniversalTime = nextFundingUniversalTime;
            }

            foreach (KeyValuePair<string, double> achievement in playerProgram.RecordedAchievements)
            {
                if (string.IsNullOrEmpty(achievement.Key) || !IsFinite(achievement.Value))
                {
                    continue;
                }

                _playerAchievementTimesById[achievement.Key] = Math.Max(0.0, achievement.Value);
            }

            for (int contractIndex = 0; contractIndex < achievementContracts.Count; contractIndex++)
            {
                AchievementFundingProgramme contract = achievementContracts[contractIndex];
                if (contract == null || string.IsNullOrEmpty(contract.Id))
                {
                    continue;
                }

                _achievementContractsById[contract.Id] = new SavedAchievementContract(
                    contract.IsOffered,
                    contract.HasStarted,
                    Math.Max(0, Math.Min(10, contract.PaymentsProcessed)));
            }

            for (int contractIndex = 0; contractIndex < satelliteContracts.Count; contractIndex++)
            {
                FundingProgramme contract = satelliteContracts[contractIndex];
                if (contract == null || string.IsNullOrEmpty(contract.Id))
                {
                    continue;
                }

                _satelliteContractsById[contract.Id] = new SavedSatelliteContract(
                    contract.IsAvailable,
                    contract.IsOffered,
                    contract.HasReachedSatelliteTarget);
            }
        }

        public void ApplyTo(
            SpaceProgramState playerProgram,
            IList<FundingProgramme> satelliteContracts,
            IList<AchievementFundingProgramme> achievementContracts)
        {
            if (!HasData || playerProgram == null || satelliteContracts == null || achievementContracts == null)
            {
                return;
            }

            playerProgram.ClearRecordedAchievements();
            foreach (KeyValuePair<string, double> achievement in _playerAchievementTimesById)
            {
                playerProgram.RecordAchievement(achievement.Key, achievement.Value);
            }

            for (int contractIndex = 0; contractIndex < achievementContracts.Count; contractIndex++)
            {
                AchievementFundingProgramme contract = achievementContracts[contractIndex];
                if (contract == null || string.IsNullOrEmpty(contract.Id))
                {
                    continue;
                }

                SavedAchievementContract savedContract;
                if (!_achievementContractsById.TryGetValue(contract.Id, out savedContract))
                {
                    contract.RestoreState(false, 0);
                    contract.RestoreOfferState(false);
                    continue;
                }

                contract.RestoreState(savedContract.HasStarted, savedContract.PaymentsProcessed);
                contract.RestoreOfferState(savedContract.IsOffered);
            }

            for (int contractIndex = 0; contractIndex < satelliteContracts.Count; contractIndex++)
            {
                FundingProgramme contract = satelliteContracts[contractIndex];
                if (contract == null || string.IsNullOrEmpty(contract.Id))
                {
                    continue;
                }

                SavedSatelliteContract savedContract;
                if (!_satelliteContractsById.TryGetValue(contract.Id, out savedContract))
                {
                    contract.RestoreAvailability(false);
                    contract.RestoreOfferState(false, false);
                    continue;
                }

                contract.RestoreAvailability(savedContract.IsAvailable);
                contract.RestoreOfferState(
                    savedContract.IsOffered,
                    savedContract.HasReachedSatelliteTarget);
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

            double nextFundingUniversalTime;
            if (TryParseFiniteDouble(
                node.GetValue(NextFundingUniversalTimeValueName),
                out nextFundingUniversalTime)
                && nextFundingUniversalTime >= 0.0)
            {
                NextFundingUniversalTime = nextFundingUniversalTime;
            }

            ConfigNode[] achievementNodes = node.GetNodes(PlayerAchievementNodeName);
            for (int nodeIndex = 0; nodeIndex < achievementNodes.Length; nodeIndex++)
            {
                ConfigNode achievementNode = achievementNodes[nodeIndex];
                string id = achievementNode.GetValue(IdValueName);
                double universalTime;
                if (string.IsNullOrEmpty(id)
                    || !TryParseFiniteDouble(
                        achievementNode.GetValue(UniversalTimeValueName),
                        out universalTime))
                {
                    continue;
                }

                StorePlayerAchievement(id, universalTime);
            }

            ConfigNode[] achievementContractNodes = node.GetNodes(AchievementContractNodeName);
            for (int nodeIndex = 0; nodeIndex < achievementContractNodes.Length; nodeIndex++)
            {
                ConfigNode contractNode = achievementContractNodes[nodeIndex];
                string id = contractNode.GetValue(IdValueName);
                bool isOffered;
                bool hasStarted;
                int paymentsProcessed;
                if (string.IsNullOrEmpty(id)
                    || !TryParseBool(contractNode.GetValue(OfferedValueName), out isOffered)
                    || !TryParseBool(contractNode.GetValue(StartedValueName), out hasStarted)
                    || !TryParsePaymentCount(
                        contractNode.GetValue(PaymentsProcessedValueName),
                        out paymentsProcessed))
                {
                    continue;
                }

                _achievementContractsById[id] = new SavedAchievementContract(
                    isOffered,
                    hasStarted,
                    paymentsProcessed);
            }

            ConfigNode[] satelliteContractNodes = node.GetNodes(SatelliteContractNodeName);
            for (int nodeIndex = 0; nodeIndex < satelliteContractNodes.Length; nodeIndex++)
            {
                ConfigNode contractNode = satelliteContractNodes[nodeIndex];
                string id = contractNode.GetValue(IdValueName);
                bool isAvailable;
                bool isOffered;
                bool hasReachedSatelliteTarget;
                if (string.IsNullOrEmpty(id)
                    || !TryParseBool(contractNode.GetValue(AvailableValueName), out isAvailable)
                    || !TryParseBool(contractNode.GetValue(OfferedValueName), out isOffered)
                    || !TryParseBool(
                        contractNode.GetValue(SatelliteTargetReachedValueName),
                        out hasReachedSatelliteTarget))
                {
                    continue;
                }

                _satelliteContractsById[id] = new SavedSatelliteContract(
                    isAvailable,
                    isOffered,
                    hasReachedSatelliteTarget);
            }
        }

        public void Save(ConfigNode node)
        {
            if (!HasData || node == null)
            {
                return;
            }

            if (NextFundingUniversalTime >= 0.0)
            {
                node.AddValue(
                    NextFundingUniversalTimeValueName,
                    NextFundingUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            }

            var achievementIds = new List<string>(_playerAchievementTimesById.Keys);
            achievementIds.Sort(StringComparer.OrdinalIgnoreCase);
            for (int idIndex = 0; idIndex < achievementIds.Count; idIndex++)
            {
                string id = achievementIds[idIndex];
                ConfigNode achievementNode = node.AddNode(PlayerAchievementNodeName);
                achievementNode.AddValue(IdValueName, id);
                achievementNode.AddValue(
                    UniversalTimeValueName,
                    _playerAchievementTimesById[id].ToString("R", CultureInfo.InvariantCulture));
            }

            var achievementContractIds = new List<string>(_achievementContractsById.Keys);
            achievementContractIds.Sort(StringComparer.OrdinalIgnoreCase);
            for (int idIndex = 0; idIndex < achievementContractIds.Count; idIndex++)
            {
                string id = achievementContractIds[idIndex];
                SavedAchievementContract contract = _achievementContractsById[id];
                ConfigNode contractNode = node.AddNode(AchievementContractNodeName);
                contractNode.AddValue(IdValueName, id);
                contractNode.AddValue(OfferedValueName, contract.IsOffered);
                contractNode.AddValue(StartedValueName, contract.HasStarted);
                contractNode.AddValue(
                    PaymentsProcessedValueName,
                    contract.PaymentsProcessed.ToString(CultureInfo.InvariantCulture));
            }

            var satelliteContractIds = new List<string>(_satelliteContractsById.Keys);
            satelliteContractIds.Sort(StringComparer.OrdinalIgnoreCase);
            for (int idIndex = 0; idIndex < satelliteContractIds.Count; idIndex++)
            {
                string id = satelliteContractIds[idIndex];
                SavedSatelliteContract contract = _satelliteContractsById[id];
                ConfigNode contractNode = node.AddNode(SatelliteContractNodeName);
                contractNode.AddValue(IdValueName, id);
                contractNode.AddValue(AvailableValueName, contract.IsAvailable);
                contractNode.AddValue(OfferedValueName, contract.IsOffered);
                contractNode.AddValue(
                    SatelliteTargetReachedValueName,
                    contract.HasReachedSatelliteTarget);
            }
        }

        private void ClearState()
        {
            HasData = false;
            NextFundingUniversalTime = -1.0;
            _playerAchievementTimesById.Clear();
            _achievementContractsById.Clear();
            _satelliteContractsById.Clear();
        }

        private void StorePlayerAchievement(string id, double universalTime)
        {
            universalTime = Math.Max(0.0, universalTime);
            double existingTime;
            if (!_playerAchievementTimesById.TryGetValue(id, out existingTime)
                || universalTime < existingTime)
            {
                _playerAchievementTimesById[id] = universalTime;
            }
        }

        private static bool TryParseBool(string value, out bool parsedValue)
        {
            parsedValue = false;
            return !string.IsNullOrEmpty(value) && bool.TryParse(value, out parsedValue);
        }

        private static bool TryParsePaymentCount(string value, out int paymentsProcessed)
        {
            paymentsProcessed = 0;
            int parsedValue;
            if (string.IsNullOrEmpty(value)
                || !int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out parsedValue))
            {
                return false;
            }

            paymentsProcessed = Math.Max(0, Math.Min(10, parsedValue));
            return true;
        }

        private static bool TryParseFiniteDouble(string value, out double parsedValue)
        {
            parsedValue = 0.0;
            return !string.IsNullOrEmpty(value)
                && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue)
                && IsFinite(parsedValue);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private sealed class SavedAchievementContract
        {
            public SavedAchievementContract(bool isOffered, bool hasStarted, int paymentsProcessed)
            {
                IsOffered = isOffered;
                HasStarted = hasStarted;
                PaymentsProcessed = paymentsProcessed;
            }

            public bool IsOffered { get; private set; }
            public bool HasStarted { get; private set; }
            public int PaymentsProcessed { get; private set; }
        }

        private sealed class SavedSatelliteContract
        {
            public SavedSatelliteContract(
                bool isAvailable,
                bool isOffered,
                bool hasReachedSatelliteTarget)
            {
                IsAvailable = isAvailable;
                IsOffered = isOffered;
                HasReachedSatelliteTarget = hasReachedSatelliteTarget;
            }

            public bool IsAvailable { get; private set; }
            public bool IsOffered { get; private set; }
            public bool HasReachedSatelliteTarget { get; private set; }
        }
    }
}

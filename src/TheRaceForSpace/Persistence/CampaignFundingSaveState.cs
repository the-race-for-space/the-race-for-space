using System;
using System.Collections.Generic;
using System.Globalization;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Agencies;

namespace TheRaceForSpace.Persistence
{
    /// <summary>
    /// Persists the player's objective completion history and the lifecycle state of every configured
    /// objective completion and satellite funding contract. Contract definitions remain code-owned; only
    /// mutable campaign state is stored by stable ID.
    /// </summary>
    public sealed class CampaignFundingSaveState
    {
        private const string PlayerObjectiveCompletionNodeName = "PLAYER_OBJECTIVE_COMPLETION";
        private const string ObjectiveFundingContractNodeName = "OBJECTIVE_FUNDING_CONTRACT";
        private const string SatelliteContractNodeName = "SATELLITE_NETWORK_FUNDING_CONTRACT";
        private const string IdValueName = "id";
        private const string UniversalTimeValueName = "universalTime";
        private const string AvailableValueName = "available";
        private const string OfferedValueName = "offered";
        private const string StartedValueName = "started";
        private const string PaymentsProcessedValueName = "paymentsProcessed";
        private const string SatelliteTargetReachedValueName = "targetReached";
        private const string NextFundingUniversalTimeValueName = "nextFundingUniversalTime";

        private readonly Dictionary<string, double> _playerObjectiveCompletionTimesById =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SavedObjectiveFundingContract> _objectiveFundingContractsById =
            new Dictionary<string, SavedObjectiveFundingContract>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SavedSatelliteContract> _satelliteContractsById =
            new Dictionary<string, SavedSatelliteContract>(StringComparer.OrdinalIgnoreCase);

        public bool HasData { get; private set; }
        public double NextFundingUniversalTime { get; private set; }

        public void Capture(
            AgencyState playerAgency,
            IList<SatelliteNetworkFundingContract> satelliteContracts,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            double nextFundingUniversalTime)
        {
            if (playerAgency == null || satelliteContracts == null || objectiveFundingContracts == null)
            {
                return;
            }

            ClearState();
            HasData = true;

            if (IsFinite(nextFundingUniversalTime) && nextFundingUniversalTime >= 0.0)
            {
                NextFundingUniversalTime = nextFundingUniversalTime;
            }

            foreach (KeyValuePair<string, double> objectiveCompletion in playerAgency.ObjectiveCompletionTimes)
            {
                if (string.IsNullOrEmpty(objectiveCompletion.Key) || !IsFinite(objectiveCompletion.Value))
                {
                    continue;
                }

                _playerObjectiveCompletionTimesById[objectiveCompletion.Key] = Math.Max(0.0, objectiveCompletion.Value);
            }

            for (int contractIndex = 0; contractIndex < objectiveFundingContracts.Count; contractIndex++)
            {
                ObjectiveFundingContract contract = objectiveFundingContracts[contractIndex];
                if (contract == null || string.IsNullOrEmpty(contract.Id))
                {
                    continue;
                }

                _objectiveFundingContractsById[contract.Id] = new SavedObjectiveFundingContract(
                    contract.IsOffered,
                    contract.HasStarted,
                    Math.Max(0, Math.Min(10, contract.PaymentsProcessed)));
            }

            for (int contractIndex = 0; contractIndex < satelliteContracts.Count; contractIndex++)
            {
                SatelliteNetworkFundingContract contract = satelliteContracts[contractIndex];
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
            AgencyState playerAgency,
            IList<SatelliteNetworkFundingContract> satelliteContracts,
            IList<ObjectiveFundingContract> objectiveFundingContracts)
        {
            if (!HasData || playerAgency == null || satelliteContracts == null || objectiveFundingContracts == null)
            {
                return;
            }

            playerAgency.ClearObjectiveCompletionTimes();
            foreach (KeyValuePair<string, double> objectiveCompletion in _playerObjectiveCompletionTimesById)
            {
                playerAgency.RecordObjectiveCompletion(objectiveCompletion.Key, objectiveCompletion.Value);
            }

            for (int contractIndex = 0; contractIndex < objectiveFundingContracts.Count; contractIndex++)
            {
                ObjectiveFundingContract contract = objectiveFundingContracts[contractIndex];
                if (contract == null || string.IsNullOrEmpty(contract.Id))
                {
                    continue;
                }

                SavedObjectiveFundingContract savedContract;
                if (!_objectiveFundingContractsById.TryGetValue(contract.Id, out savedContract))
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
                SatelliteNetworkFundingContract contract = satelliteContracts[contractIndex];
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

            ConfigNode[] completionNodes = node.GetNodes(PlayerObjectiveCompletionNodeName);
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

                StorePlayerObjectiveCompletion(id, universalTime);
            }

            ConfigNode[] objectiveFundingContractNodes = node.GetNodes(ObjectiveFundingContractNodeName);
            for (int nodeIndex = 0; nodeIndex < objectiveFundingContractNodes.Length; nodeIndex++)
            {
                ConfigNode contractNode = objectiveFundingContractNodes[nodeIndex];
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

                _objectiveFundingContractsById[id] = new SavedObjectiveFundingContract(
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

            var objectiveIds = new List<string>(_playerObjectiveCompletionTimesById.Keys);
            objectiveIds.Sort(StringComparer.OrdinalIgnoreCase);
            for (int idIndex = 0; idIndex < objectiveIds.Count; idIndex++)
            {
                string id = objectiveIds[idIndex];
                ConfigNode completionNode = node.AddNode(PlayerObjectiveCompletionNodeName);
                completionNode.AddValue(IdValueName, id);
                completionNode.AddValue(
                    UniversalTimeValueName,
                    _playerObjectiveCompletionTimesById[id].ToString("R", CultureInfo.InvariantCulture));
            }

            var objectiveFundingContractIds = new List<string>(_objectiveFundingContractsById.Keys);
            objectiveFundingContractIds.Sort(StringComparer.OrdinalIgnoreCase);
            for (int idIndex = 0; idIndex < objectiveFundingContractIds.Count; idIndex++)
            {
                string id = objectiveFundingContractIds[idIndex];
                SavedObjectiveFundingContract contract = _objectiveFundingContractsById[id];
                ConfigNode contractNode = node.AddNode(ObjectiveFundingContractNodeName);
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
            _playerObjectiveCompletionTimesById.Clear();
            _objectiveFundingContractsById.Clear();
            _satelliteContractsById.Clear();
        }

        private void StorePlayerObjectiveCompletion(string id, double universalTime)
        {
            universalTime = Math.Max(0.0, universalTime);
            double existingTime;
            if (!_playerObjectiveCompletionTimesById.TryGetValue(id, out existingTime)
                || universalTime < existingTime)
            {
                _playerObjectiveCompletionTimesById[id] = universalTime;
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

        private sealed class SavedObjectiveFundingContract
        {
            public SavedObjectiveFundingContract(bool isOffered, bool hasStarted, int paymentsProcessed)
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

using System;
using System.Collections.Generic;
using System.Globalization;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Milestones;
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
        private const string OfferedValueName = "offered";
        private const string SatelliteTargetReachedValueName = "targetReached";
        private const string NextFundingUniversalTimeValueName = "nextFundingUniversalTime";

        private readonly Dictionary<string, double> _achievementTimesById =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _unlockedFundingProgrammeIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _fundingProgrammeOfferedById =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _fundingProgrammeTargetReachedById =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _contractStartedById =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _contractPaymentsProcessedById =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> _contractOfferedById =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public bool HasData { get; private set; }
        public double NextFundingUniversalTime { get; private set; }

        public void Capture(
            SpaceProgramState playerProgram,
            IList<FundingProgramme> fundingProgrammes,
            IList<AchievementFundingProgramme> achievementProgrammes)
        {
            Capture(playerProgram, fundingProgrammes, achievementProgrammes, -1.0);
        }

        public void Capture(
            SpaceProgramState playerProgram,
            IList<FundingProgramme> fundingProgrammes,
            IList<AchievementFundingProgramme> achievementProgrammes,
            double nextFundingUniversalTime)
        {
            if (playerProgram == null || fundingProgrammes == null || achievementProgrammes == null)
            {
                return;
            }

            ClearState();
            HasData = true;

            if (!double.IsNaN(nextFundingUniversalTime)
                && !double.IsInfinity(nextFundingUniversalTime)
                && nextFundingUniversalTime >= 0.0)
            {
                NextFundingUniversalTime = nextFundingUniversalTime;
            }

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
                    _fundingProgrammeOfferedById[programme.Id] = programme.IsOffered;
                    _fundingProgrammeTargetReachedById[programme.Id] =
                        programme.HasReachedSatelliteTarget;
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
                _contractOfferedById[programme.Id] = programme.IsOffered;
            }
        }

        /// <summary>
        /// Compatibility overload for code compiled against the fixed 0.3 persistence API.
        /// The live controller uses the collection overload above.
        /// </summary>
        public void Capture(
            SpaceProgramState playerProgram,
            FundingProgramme kerbinProgramme,
            FundingProgramme munProgramme,
            FundingProgramme minmusProgramme,
            AchievementFundingProgramme probeOrbitProgramme,
            AchievementFundingProgramme crewedOrbitProgramme,
            AchievementFundingProgramme munProbeOrbitProgramme,
            AchievementFundingProgramme minmusProbeOrbitProgramme,
            AchievementFundingProgramme munCrewedOrbitProgramme,
            AchievementFundingProgramme minmusCrewedOrbitProgramme)
        {
            Capture(
                playerProgram,
                new List<FundingProgramme> { kerbinProgramme, munProgramme, minmusProgramme },
                new List<AchievementFundingProgramme>
                {
                    probeOrbitProgramme,
                    crewedOrbitProgramme,
                    munProbeOrbitProgramme,
                    minmusProbeOrbitProgramme,
                    munCrewedOrbitProgramme,
                    minmusCrewedOrbitProgramme
                });
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

            playerProgram.ClearRecordedAchievements();
            foreach (KeyValuePair<string, double> achievement in _achievementTimesById)
            {
                playerProgram.RecordAchievement(achievement.Key, achievement.Value);
            }

            for (int programmeIndex = 0; programmeIndex < fundingProgrammes.Count; programmeIndex++)
            {
                FundingProgramme programme = fundingProgrammes[programmeIndex];
                if (programme == null || string.IsNullOrEmpty(programme.Id))
                {
                    continue;
                }

                programme.RestoreAvailability(_unlockedFundingProgrammeIds.Contains(programme.Id));

                bool isOffered;
                bool hasReachedSatelliteTarget;
                _fundingProgrammeOfferedById.TryGetValue(programme.Id, out isOffered);
                _fundingProgrammeTargetReachedById.TryGetValue(
                    programme.Id,
                    out hasReachedSatelliteTarget);
                programme.RestoreOfferState(isOffered, hasReachedSatelliteTarget);
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
                bool hasSavedContractState = _contractStartedById.TryGetValue(programme.Id, out started)
                    && _contractPaymentsProcessedById.TryGetValue(programme.Id, out paymentsProcessed);
                if (!hasSavedContractState)
                {
                    programme.RestoreState(false, 0);
                    programme.RestoreOfferState(false);
                    continue;
                }

                programme.RestoreState(started, paymentsProcessed);

                bool isOffered;
                if (_contractOfferedById.TryGetValue(programme.Id, out isOffered))
                {
                    // A started contract was globally active before offer-state enforcement. Keep
                    // that invariant even if a Step 1 save captured it before Step 2 used IsOffered.
                    programme.RestoreOfferState(isOffered || programme.HasStarted);
                }
                else if (programme.HasStarted)
                {
                    // Saves from before offer-state persistence already treated started contracts
                    // as globally active, so preserve that status when the new field is absent.
                    programme.RestoreOfferState(true);
                }
            }
        }

        /// <summary>
        /// Compatibility overload for code compiled against the fixed 0.3 persistence API.
        /// </summary>
        public void ApplyTo(
            SpaceProgramState playerProgram,
            FundingProgramme kerbinProgramme,
            FundingProgramme munProgramme,
            FundingProgramme minmusProgramme,
            AchievementFundingProgramme probeOrbitProgramme,
            AchievementFundingProgramme crewedOrbitProgramme,
            AchievementFundingProgramme munProbeOrbitProgramme,
            AchievementFundingProgramme minmusProbeOrbitProgramme,
            AchievementFundingProgramme munCrewedOrbitProgramme,
            AchievementFundingProgramme minmusCrewedOrbitProgramme)
        {
            ApplyTo(
                playerProgram,
                new List<FundingProgramme> { kerbinProgramme, munProgramme, minmusProgramme },
                new List<AchievementFundingProgramme>
                {
                    probeOrbitProgramme,
                    crewedOrbitProgramme,
                    munProbeOrbitProgramme,
                    minmusProbeOrbitProgramme,
                    munCrewedOrbitProgramme,
                    minmusCrewedOrbitProgramme
                });
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

            ConfigNode[] fundingNodes = node.GetNodes(FundingProgrammeNodeName);
            for (int nodeIndex = 0; nodeIndex < fundingNodes.Length; nodeIndex++)
            {
                ConfigNode fundingNode = fundingNodes[nodeIndex];
                string id = fundingNode.GetValue(IdValueName);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                _unlockedFundingProgrammeIds.Add(id);

                string offeredValue = fundingNode.GetValue(OfferedValueName);
                _fundingProgrammeOfferedById[id] = string.IsNullOrEmpty(offeredValue)
                    ? true
                    : ParseBool(offeredValue);
                _fundingProgrammeTargetReachedById[id] = ParseBool(
                    fundingNode.GetValue(SatelliteTargetReachedValueName));
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

                string offeredValue = contractNode.GetValue(OfferedValueName);
                if (!string.IsNullOrEmpty(offeredValue))
                {
                    _contractOfferedById[id] = ParseBool(offeredValue);
                }
            }

            // Read-only migration from the fixed 0.3 schema. New saves write only the collection format.
            LoadLegacyAchievement(
                node,
                "playerProbeOrbit",
                "playerProbeOrbitUniversalTime",
                PrototypeMilestones.ProbeOrbitId);
            LoadLegacyAchievement(
                node,
                "playerCrewedOrbit",
                "playerCrewedOrbitUniversalTime",
                PrototypeMilestones.CrewedOrbitId);
            LoadLegacyAchievement(
                node,
                "playerMunProbeOrbit",
                "playerMunProbeOrbitUniversalTime",
                PrototypeMilestones.MunProbeOrbitId);
            LoadLegacyAchievement(
                node,
                "playerMinmusProbeOrbit",
                "playerMinmusProbeOrbitUniversalTime",
                PrototypeMilestones.MinmusProbeOrbitId);
            LoadLegacyAchievement(
                node,
                "playerMunCrewedOrbit",
                "playerMunCrewedOrbitUniversalTime",
                PrototypeMilestones.MunCrewedOrbitId);
            LoadLegacyAchievement(
                node,
                "playerMinmusCrewedOrbit",
                "playerMinmusCrewedOrbitUniversalTime",
                PrototypeMilestones.MinmusCrewedOrbitId);

            LoadLegacyFundingUnlock(node, "kerbinNetworkUnlocked", "kerbin-network");
            LoadLegacyFundingUnlock(node, "munNetworkUnlocked", "mun-survey");
            LoadLegacyFundingUnlock(node, "minmusNetworkUnlocked", "minmus-relay");

            LoadLegacyContract(node, "probeContractStarted", "probePaymentsProcessed", PrototypeMilestones.ProbeOrbitId);
            LoadLegacyContract(node, "crewedContractStarted", "crewedPaymentsProcessed", PrototypeMilestones.CrewedOrbitId);
            LoadLegacyContract(node, "munProbeContractStarted", "munProbePaymentsProcessed", PrototypeMilestones.MunProbeOrbitId);
            LoadLegacyContract(node, "minmusProbeContractStarted", "minmusProbePaymentsProcessed", PrototypeMilestones.MinmusProbeOrbitId);
            LoadLegacyContract(node, "munCrewedContractStarted", "munCrewedPaymentsProcessed", PrototypeMilestones.MunCrewedOrbitId);
            LoadLegacyContract(node, "minmusCrewedContractStarted", "minmusCrewedPaymentsProcessed", PrototypeMilestones.MinmusCrewedOrbitId);
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
                string id = fundingProgrammeIds[idIndex];
                ConfigNode fundingNode = node.AddNode(FundingProgrammeNodeName);
                fundingNode.AddValue(IdValueName, id);

                bool isOffered;
                bool hasReachedSatelliteTarget;
                _fundingProgrammeOfferedById.TryGetValue(id, out isOffered);
                _fundingProgrammeTargetReachedById.TryGetValue(
                    id,
                    out hasReachedSatelliteTarget);
                fundingNode.AddValue(OfferedValueName, isOffered);
                fundingNode.AddValue(SatelliteTargetReachedValueName, hasReachedSatelliteTarget);
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

                bool isOffered;
                if (_contractOfferedById.TryGetValue(id, out isOffered))
                {
                    contractNode.AddValue(OfferedValueName, isOffered);
                }
            }
        }

        private void ClearState()
        {
            HasData = false;
            NextFundingUniversalTime = -1.0;
            _achievementTimesById.Clear();
            _unlockedFundingProgrammeIds.Clear();
            _fundingProgrammeOfferedById.Clear();
            _fundingProgrammeTargetReachedById.Clear();
            _contractStartedById.Clear();
            _contractPaymentsProcessedById.Clear();
            _contractOfferedById.Clear();
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

        private void LoadLegacyAchievement(
            ConfigNode node,
            string achievedValueName,
            string universalTimeValueName,
            string milestoneId)
        {
            if (!ParseBool(node.GetValue(achievedValueName)))
            {
                return;
            }

            double universalTime;
            if (!TryParseFiniteDouble(node.GetValue(universalTimeValueName), out universalTime))
            {
                universalTime = 0.0;
            }

            StoreAchievement(milestoneId, universalTime);
        }

        private void LoadLegacyFundingUnlock(ConfigNode node, string valueName, string programmeId)
        {
            if (ParseBool(node.GetValue(valueName)))
            {
                _unlockedFundingProgrammeIds.Add(programmeId);
                _fundingProgrammeOfferedById[programmeId] = true;
                _fundingProgrammeTargetReachedById[programmeId] = false;
            }
        }

        private void LoadLegacyContract(
            ConfigNode node,
            string startedValueName,
            string paymentsValueName,
            string programmeId)
        {
            string startedValue = node.GetValue(startedValueName);
            string paymentsValue = node.GetValue(paymentsValueName);
            if (string.IsNullOrEmpty(startedValue) && string.IsNullOrEmpty(paymentsValue))
            {
                return;
            }

            _contractStartedById[programmeId] = ParseBool(startedValue);
            _contractPaymentsProcessedById[programmeId] = ParsePaymentCount(paymentsValue);
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

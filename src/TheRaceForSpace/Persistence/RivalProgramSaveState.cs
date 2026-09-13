using System;
using System.Collections.Generic;
using System.Globalization;
using TheRaceForSpace.Agencies;

namespace TheRaceForSpace.Persistence
{
    /// <summary>
    /// Captured save state for one rival programme. This class only transforms project-owned mutable
    /// state to and from ConfigNode data; gameplay calculations remain in the rival/campaign modules.
    /// </summary>
    internal sealed class RivalProgramSaveState
    {
        private const string ObjectiveCompletionNodeName = "OBJECTIVE_COMPLETION";
        private const string SatelliteNodeName = "SATELLITE";
        private const string ScienceLaunchNodeName = "SCIENCE_LAUNCH";
        private const string LiveMissionNodeName = "LIVE_MISSION";
        private const string FacilityNodeName = "FACILITY";
        private const string ConstructionNodeName = "CONSTRUCTION";
        private const string ResearchedTechNodeName = "RESEARCHED_TECH";
        private const string ResearchNodeName = "RESEARCH";
        private const string CompletedScienceNodeName = "COMPLETED_SCIENCE";

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
        private const string NextMissionReadyUniversalTimeValueName = "launchReadyUniversalTime";
        private const string StoredScienceValueName = "storedScience";
        private const string KerbalsEmployedValueName = "kerbalsEmployed";
        private const string PendingInsuranceFundsValueName = "pendingInsuranceFunds";

        private const string ExperimentIdValueName = "experimentId";
        private const string ScienceBodyNameValueName = "bodyName";
        private const string SituationValueName = "situation";
        private const string BiomeNameValueName = "biomeName";
        private const string PlannedScienceRewardValueName = "plannedScienceReward";
        private const string RequiredKerbalsValueName = "requiredKerbals";
        private const string ReadyUniversalTimeValueName = "readyUniversalTime";
        private const string NextProgressCheckUniversalTimeValueName = "nextProgressCheckUniversalTime";

        private const string MissionSequenceValueName = "missionSequence";
        private const string MissionTypeValueName = "missionType";
        private const string ContractIdValueName = "contractId";
        private const string LocationIdValueName = "locationId";
        private const string LaunchUniversalTimeValueName = "launchUniversalTime";
        private const string DurationDaysValueName = "durationDays";
        private const string CompletionUniversalTimeValueName = "completionUniversalTime";
        private const string DifficultyValueName = "difficulty";
        private const string SuccessChancePercentValueName = "successChancePercent";
        private const string AssignedKerbalCountValueName = "assignedKerbalCount";
        private const string OutcomeSeedValueName = "outcomeSeed";

        private const string FacilityValueName = "facility";
        private const string LevelValueName = "level";
        private const string SourceLevelValueName = "sourceLevel";
        private const string TargetLevelValueName = "targetLevel";
        private const string StartUniversalTimeValueName = "startUniversalTime";
        private const string CostPaidFundsValueName = "costPaidFunds";

        private const string TechIdValueName = "techId";
        private const string ScienceCostPaidValueName = "scienceCostPaid";
        private const string ResearchReadyUniversalTimeValueName = "researchReadyUniversalTime";
        private const string EligibleCompletionFundingUniversalTimeValueName =
            "eligibleCompletionFundingUniversalTime";

        private readonly Dictionary<string, double> _objectiveCompletionTimesById =
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _satellitesByBody =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly List<RivalLiveMissionState> _liveMissions =
            new List<RivalLiveMissionState>();
        private readonly Dictionary<RivalFacilityType, int> _facilityLevels =
            new Dictionary<RivalFacilityType, int>();
        private readonly List<RivalFacilityConstructionState> _facilityConstruction =
            new List<RivalFacilityConstructionState>();
        private readonly HashSet<string> _researchedTechIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<ScienceSubjectKey> _completedScienceSubjects =
            new HashSet<ScienceSubjectKey>();

        internal bool HasData { get; private set; }
        internal string AgencyId { get; private set; }

        private double Funds { get; set; }
        private string NextMissionTargetId { get; set; }
        private int MissionProgressPercent { get; set; }
        private double NextMissionProgressCheckUniversalTime { get; set; }
        private double NextMissionReadyUniversalTime { get; set; }
        private double StoredScience { get; set; }
        private int KerbalsEmployed { get; set; }
        private double PendingInsuranceFunds { get; set; }
        private ScienceLaunchPreparationState ScienceLaunchPreparation { get; set; }
        private RivalResearchProjectState CurrentResearch { get; set; }

        internal RivalProgramSaveState()
        {
            ClearState();
        }

        internal void Capture(AgencyState agency)
        {
            ClearState();
            if (agency == null || agency.IsPlayer || string.IsNullOrEmpty(agency.Id))
            {
                return;
            }

            HasData = true;
            AgencyId = agency.Id;
            Funds = IsFinite(agency.Funds) ? agency.Funds : 0.0;
            NextMissionTargetId = agency.NextMissionTargetId;
            MissionProgressPercent = ClampPercent(agency.MissionProgressPercent);
            NextMissionProgressCheckUniversalTime = NormalizeNonNegativeTime(
                agency.NextMissionProgressCheckUniversalTime);
            NextMissionReadyUniversalTime = NormalizeReadyTime(agency.NextMissionReadyUniversalTime);

            foreach (KeyValuePair<string, double> objectiveCompletion in agency.ObjectiveCompletionTimes)
            {
                if (!string.IsNullOrEmpty(objectiveCompletion.Key)
                    && IsFinite(objectiveCompletion.Value))
                {
                    _objectiveCompletionTimesById[objectiveCompletion.Key] =
                        Math.Max(0.0, objectiveCompletion.Value);
                }
            }

            foreach (KeyValuePair<string, int> bodyCount in agency.SatelliteCountsByBody)
            {
                if (!string.IsNullOrEmpty(bodyCount.Key))
                {
                    _satellitesByBody[bodyCount.Key] = Math.Max(0, bodyCount.Value);
                }
            }

            RivalProgramState programme = agency.RivalProgram;
            if (programme == null)
            {
                return;
            }

            StoredScience = NormalizeNonNegative(programme.StoredScience);
            KerbalsEmployed = Math.Max(0, programme.KerbalsEmployed);
            PendingInsuranceFunds = NormalizeNonNegative(programme.PendingInsuranceFunds);
            ScienceLaunchPreparation = CloneSciencePreparation(programme.ScienceLaunchPreparation);
            CurrentResearch = CloneResearch(programme.CurrentResearch);

            for (int missionIndex = 0; missionIndex < programme.LiveMissions.Count; missionIndex++)
            {
                RivalLiveMissionState mission = CloneLiveMission(programme.LiveMissions[missionIndex]);
                if (mission != null)
                {
                    _liveMissions.Add(mission);
                }
            }

            foreach (KeyValuePair<RivalFacilityType, int> facility in programme.FacilityLevels)
            {
                if (facility.Value >= 1 && facility.Value <= 3)
                {
                    _facilityLevels[facility.Key] = facility.Value;
                }
            }

            for (int constructionIndex = 0;
                constructionIndex < programme.FacilityConstruction.Count;
                constructionIndex++)
            {
                RivalFacilityConstructionState construction = CloneConstruction(
                    programme.FacilityConstruction[constructionIndex]);
                if (construction != null)
                {
                    _facilityConstruction.Add(construction);
                }
            }

            foreach (string techId in programme.ResearchedTechIds)
            {
                if (!string.IsNullOrEmpty(techId))
                {
                    _researchedTechIds.Add(techId);
                }
            }

            foreach (ScienceSubjectKey subject in programme.CompletedScienceSubjects)
            {
                ScienceSubjectKey subjectCopy = CloneScienceSubject(subject);
                if (subjectCopy != null)
                {
                    _completedScienceSubjects.Add(subjectCopy);
                }
            }
        }

        internal void ApplyTo(AgencyState agency)
        {
            if (!HasData
                || agency == null
                || agency.IsPlayer
                || (!string.IsNullOrEmpty(AgencyId)
                    && !string.Equals(AgencyId, agency.Id, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            // Rival funding may legitimately be negative after payroll/insurance deductions.
            agency.Funds = IsFinite(Funds) ? Funds : 0.0;
            agency.ClearSatelliteCounts();
            agency.ClearObjectiveCompletionTimes();

            foreach (KeyValuePair<string, int> bodyCount in _satellitesByBody)
            {
                agency.SetSatelliteCount(bodyCount.Key, bodyCount.Value);
            }

            foreach (KeyValuePair<string, double> objectiveCompletion in _objectiveCompletionTimesById)
            {
                // Persistence restoration is intentionally silent. Loading historical rival progress
                // must not emit the live completion notification signal.
                agency.RestoreObjectiveCompletion(objectiveCompletion.Key, objectiveCompletion.Value);
            }

            agency.NextMissionTargetId = NextMissionTargetId;
            // Presentation text is derived from the live target collections on the next rival refresh.
            agency.NextMissionDisplayName = null;
            agency.MissionProgressPercent = ClampPercent(MissionProgressPercent);
            agency.NextMissionProgressCheckUniversalTime = NormalizeNonNegativeTime(
                NextMissionProgressCheckUniversalTime);
            agency.NextMissionReadyUniversalTime = NormalizeReadyTime(NextMissionReadyUniversalTime);

            RivalProgramState programme = agency.RivalProgram;
            if (programme == null)
            {
                return;
            }

            programme.StoredScience = NormalizeNonNegative(StoredScience);
            programme.KerbalsEmployed = Math.Max(0, KerbalsEmployed);
            programme.PendingInsuranceFunds = NormalizeNonNegative(PendingInsuranceFunds);
            programme.ScienceLaunchPreparation = CloneSciencePreparation(ScienceLaunchPreparation);
            programme.CurrentResearch = CloneResearch(CurrentResearch);

            programme.LiveMissions.Clear();
            for (int missionIndex = 0; missionIndex < _liveMissions.Count; missionIndex++)
            {
                RivalLiveMissionState mission = CloneLiveMission(_liveMissions[missionIndex]);
                if (mission != null)
                {
                    programme.LiveMissions.Add(mission);
                }
            }

            programme.FacilityLevels.Clear();
            Array facilityValues = Enum.GetValues(typeof(RivalFacilityType));
            for (int facilityIndex = 0; facilityIndex < facilityValues.Length; facilityIndex++)
            {
                RivalFacilityType facility = (RivalFacilityType)facilityValues.GetValue(facilityIndex);
                int level;
                programme.FacilityLevels[facility] =
                    _facilityLevels.TryGetValue(facility, out level) && level >= 1 && level <= 3
                        ? level
                        : 1;
            }

            programme.FacilityConstruction.Clear();
            for (int constructionIndex = 0;
                constructionIndex < _facilityConstruction.Count;
                constructionIndex++)
            {
                RivalFacilityConstructionState construction = CloneConstruction(
                    _facilityConstruction[constructionIndex]);
                if (construction != null)
                {
                    programme.FacilityConstruction.Add(construction);
                }
            }

            programme.ResearchedTechIds.Clear();
            programme.ResearchedTechIds.Add(RivalProgramState.StartingTechId);
            foreach (string techId in _researchedTechIds)
            {
                if (!string.IsNullOrEmpty(techId))
                {
                    programme.ResearchedTechIds.Add(techId);
                }
            }

            programme.CompletedScienceSubjects.Clear();
            foreach (ScienceSubjectKey subject in _completedScienceSubjects)
            {
                ScienceSubjectKey subjectCopy = CloneScienceSubject(subject);
                if (subjectCopy != null)
                {
                    programme.CompletedScienceSubjects.Add(subjectCopy);
                }
            }
        }

        internal void Load(ConfigNode node)
        {
            ClearState();
            if (node == null)
            {
                return;
            }

            AgencyId = node.GetValue(AgencyIdValueName);
            if (string.IsNullOrEmpty(AgencyId))
            {
                return;
            }

            HasData = true;

            double parsedDouble;
            if (TryParseFiniteDouble(node.GetValue(FundsValueName), out parsedDouble))
            {
                Funds = parsedDouble;
            }

            string targetId = node.GetValue(NextMissionTargetIdValueName);
            if (!string.IsNullOrEmpty(targetId))
            {
                NextMissionTargetId = targetId;
            }

            int parsedInt;
            if (TryParseInt(node.GetValue(MissionProgressPercentValueName), out parsedInt))
            {
                MissionProgressPercent = ClampPercent(parsedInt);
            }

            if (TryParseFiniteDouble(
                node.GetValue(NextMissionProgressCheckUniversalTimeValueName),
                out parsedDouble))
            {
                NextMissionProgressCheckUniversalTime = NormalizeNonNegativeTime(parsedDouble);
            }

            if (TryParseFiniteDouble(
                node.GetValue(NextMissionReadyUniversalTimeValueName),
                out parsedDouble))
            {
                NextMissionReadyUniversalTime = NormalizeReadyTime(parsedDouble);
            }

            if (TryParseFiniteDouble(node.GetValue(StoredScienceValueName), out parsedDouble)
                && parsedDouble >= 0.0)
            {
                StoredScience = parsedDouble;
            }

            if (TryParseInt(node.GetValue(KerbalsEmployedValueName), out parsedInt)
                && parsedInt >= 0)
            {
                KerbalsEmployed = parsedInt;
            }

            if (TryParseFiniteDouble(node.GetValue(PendingInsuranceFundsValueName), out parsedDouble)
                && parsedDouble >= 0.0)
            {
                PendingInsuranceFunds = parsedDouble;
            }

            LoadObjectiveCompletions(node);
            LoadSatellites(node);
            ScienceLaunchPreparation = LoadSciencePreparation(node.GetNode(ScienceLaunchNodeName));
            LoadLiveMissions(node);
            LoadFacilities(node);
            LoadConstruction(node);
            LoadResearchedTech(node);
            CurrentResearch = LoadResearch(node.GetNode(ResearchNodeName));
            LoadCompletedScience(node);
        }

        internal void Save(ConfigNode node)
        {
            if (!HasData || node == null)
            {
                return;
            }

            node.AddValue(AgencyIdValueName, AgencyId);
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
            node.AddValue(
                NextMissionReadyUniversalTimeValueName,
                NextMissionReadyUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(StoredScienceValueName, StoredScience.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(KerbalsEmployedValueName, KerbalsEmployed.ToString(CultureInfo.InvariantCulture));
            node.AddValue(
                PendingInsuranceFundsValueName,
                PendingInsuranceFunds.ToString("R", CultureInfo.InvariantCulture));

            SaveObjectiveCompletions(node);
            SaveSatellites(node);

            if (ScienceLaunchPreparation != null)
            {
                SaveSciencePreparation(node.AddNode(ScienceLaunchNodeName), ScienceLaunchPreparation);
            }

            var liveMissions = new List<RivalLiveMissionState>(_liveMissions);
            liveMissions.Sort(CompareLiveMissions);
            for (int missionIndex = 0; missionIndex < liveMissions.Count; missionIndex++)
            {
                SaveLiveMission(node.AddNode(LiveMissionNodeName), liveMissions[missionIndex]);
            }

            Array facilityValues = Enum.GetValues(typeof(RivalFacilityType));
            for (int facilityIndex = 0; facilityIndex < facilityValues.Length; facilityIndex++)
            {
                RivalFacilityType facility = (RivalFacilityType)facilityValues.GetValue(facilityIndex);
                int level;
                if (!_facilityLevels.TryGetValue(facility, out level))
                {
                    level = 1;
                }

                ConfigNode facilityNode = node.AddNode(FacilityNodeName);
                facilityNode.AddValue(FacilityValueName, facility.ToString());
                facilityNode.AddValue(LevelValueName, level.ToString(CultureInfo.InvariantCulture));
            }

            for (int constructionIndex = 0;
                constructionIndex < _facilityConstruction.Count;
                constructionIndex++)
            {
                SaveConstruction(
                    node.AddNode(ConstructionNodeName),
                    _facilityConstruction[constructionIndex]);
            }

            var techIds = new List<string>(_researchedTechIds);
            techIds.Sort(StringComparer.OrdinalIgnoreCase);
            for (int techIndex = 0; techIndex < techIds.Count; techIndex++)
            {
                ConfigNode techNode = node.AddNode(ResearchedTechNodeName);
                techNode.AddValue(IdValueName, techIds[techIndex]);
            }

            if (CurrentResearch != null)
            {
                SaveResearch(node.AddNode(ResearchNodeName), CurrentResearch);
            }

            var completedSubjects = new List<ScienceSubjectKey>(_completedScienceSubjects);
            completedSubjects.Sort(CompareScienceSubjects);
            for (int subjectIndex = 0; subjectIndex < completedSubjects.Count; subjectIndex++)
            {
                SaveScienceSubject(
                    node.AddNode(CompletedScienceNodeName),
                    completedSubjects[subjectIndex]);
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
            NextMissionReadyUniversalTime = -1.0;
            StoredScience = 0.0;
            KerbalsEmployed = 1;
            PendingInsuranceFunds = 0.0;
            ScienceLaunchPreparation = null;
            CurrentResearch = null;

            _objectiveCompletionTimesById.Clear();
            _satellitesByBody.Clear();
            _liveMissions.Clear();
            _facilityConstruction.Clear();
            _completedScienceSubjects.Clear();

            _researchedTechIds.Clear();
            _researchedTechIds.Add(RivalProgramState.StartingTechId);

            _facilityLevels.Clear();
            Array facilityValues = Enum.GetValues(typeof(RivalFacilityType));
            for (int facilityIndex = 0; facilityIndex < facilityValues.Length; facilityIndex++)
            {
                RivalFacilityType facility = (RivalFacilityType)facilityValues.GetValue(facilityIndex);
                _facilityLevels[facility] = 1;
            }
        }

        private void LoadObjectiveCompletions(ConfigNode node)
        {
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

                universalTime = Math.Max(0.0, universalTime);
                double existingTime;
                if (!_objectiveCompletionTimesById.TryGetValue(id, out existingTime)
                    || universalTime < existingTime)
                {
                    _objectiveCompletionTimesById[id] = universalTime;
                }
            }
        }

        private void LoadSatellites(ConfigNode node)
        {
            ConfigNode[] satelliteNodes = node.GetNodes(SatelliteNodeName);
            for (int nodeIndex = 0; nodeIndex < satelliteNodes.Length; nodeIndex++)
            {
                ConfigNode satelliteNode = satelliteNodes[nodeIndex];
                string bodyName = satelliteNode.GetValue(BodyValueName);
                int count;
                if (string.IsNullOrEmpty(bodyName)
                    || !TryParseInt(satelliteNode.GetValue(CountValueName), out count))
                {
                    continue;
                }

                _satellitesByBody[bodyName] = Math.Max(0, count);
            }
        }

        private void LoadLiveMissions(ConfigNode node)
        {
            ConfigNode[] missionNodes = node.GetNodes(LiveMissionNodeName);
            for (int missionIndex = 0; missionIndex < missionNodes.Length; missionIndex++)
            {
                RivalLiveMissionState mission;
                if (TryLoadLiveMission(missionNodes[missionIndex], out mission))
                {
                    _liveMissions.Add(mission);
                }
            }
        }

        private void LoadFacilities(ConfigNode node)
        {
            ConfigNode[] facilityNodes = node.GetNodes(FacilityNodeName);
            for (int facilityIndex = 0; facilityIndex < facilityNodes.Length; facilityIndex++)
            {
                RivalFacilityType facility;
                int level;
                if (!TryParseDefinedEnum(
                        facilityNodes[facilityIndex].GetValue(FacilityValueName),
                        out facility)
                    || !TryParseInt(facilityNodes[facilityIndex].GetValue(LevelValueName), out level)
                    || level < 1
                    || level > 3)
                {
                    continue;
                }

                _facilityLevels[facility] = level;
            }
        }

        private void LoadConstruction(ConfigNode node)
        {
            ConfigNode[] constructionNodes = node.GetNodes(ConstructionNodeName);
            for (int constructionIndex = 0;
                constructionIndex < constructionNodes.Length;
                constructionIndex++)
            {
                RivalFacilityConstructionState construction;
                if (TryLoadConstruction(constructionNodes[constructionIndex], out construction))
                {
                    _facilityConstruction.Add(construction);
                }
            }
        }

        private void LoadResearchedTech(ConfigNode node)
        {
            ConfigNode[] techNodes = node.GetNodes(ResearchedTechNodeName);
            for (int techIndex = 0; techIndex < techNodes.Length; techIndex++)
            {
                string techId = techNodes[techIndex].GetValue(IdValueName);
                if (!string.IsNullOrEmpty(techId))
                {
                    _researchedTechIds.Add(techId);
                }
            }
        }

        private void LoadCompletedScience(ConfigNode node)
        {
            ConfigNode[] scienceNodes = node.GetNodes(CompletedScienceNodeName);
            for (int scienceIndex = 0; scienceIndex < scienceNodes.Length; scienceIndex++)
            {
                ScienceSubjectKey subject;
                if (TryLoadScienceSubject(scienceNodes[scienceIndex], out subject))
                {
                    _completedScienceSubjects.Add(subject);
                }
            }
        }

        private void SaveObjectiveCompletions(ConfigNode node)
        {
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
        }

        private void SaveSatellites(ConfigNode node)
        {
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

        private static ScienceLaunchPreparationState LoadSciencePreparation(ConfigNode node)
        {
            if (node == null)
            {
                return null;
            }

            ScienceSubjectKey subject;
            if (!TryLoadScienceSubject(node, out subject))
            {
                return null;
            }

            var state = new ScienceLaunchPreparationState { Subject = subject };
            double parsedDouble;
            int parsedInt;

            if (TryParseFiniteDouble(node.GetValue(PlannedScienceRewardValueName), out parsedDouble)
                && parsedDouble >= 0.0)
            {
                state.PlannedScienceReward = parsedDouble;
            }

            if (TryParseInt(node.GetValue(MissionProgressPercentValueName), out parsedInt))
            {
                state.LaunchProgressPercent = ClampPercent(parsedInt);
            }

            if (TryParseFiniteDouble(
                    node.GetValue(NextProgressCheckUniversalTimeValueName),
                    out parsedDouble))
            {
                state.NextProgressCheckUniversalTime = NormalizeNonNegativeTime(parsedDouble);
            }

            if (TryParseInt(node.GetValue(RequiredKerbalsValueName), out parsedInt)
                && parsedInt > 0)
            {
                state.RequiredKerbals = parsedInt;
            }

            if (TryParseFiniteDouble(node.GetValue(ReadyUniversalTimeValueName), out parsedDouble))
            {
                state.ReadyUniversalTime = NormalizeReadyTime(parsedDouble);
            }

            return state;
        }

        private static void SaveSciencePreparation(
            ConfigNode node,
            ScienceLaunchPreparationState state)
        {
            SaveScienceSubject(node, state.Subject);
            node.AddValue(
                PlannedScienceRewardValueName,
                state.PlannedScienceReward.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(
                MissionProgressPercentValueName,
                state.LaunchProgressPercent.ToString(CultureInfo.InvariantCulture));
            node.AddValue(
                NextProgressCheckUniversalTimeValueName,
                state.NextProgressCheckUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(
                RequiredKerbalsValueName,
                state.RequiredKerbals.ToString(CultureInfo.InvariantCulture));
            node.AddValue(
                ReadyUniversalTimeValueName,
                state.ReadyUniversalTime.ToString("R", CultureInfo.InvariantCulture));
        }

        private static bool TryLoadLiveMission(ConfigNode node, out RivalLiveMissionState mission)
        {
            mission = null;
            if (node == null)
            {
                return false;
            }

            long missionSequence;
            RivalMissionType missionType;
            double launchUniversalTime;
            double durationDays;
            double completionUniversalTime;
            int difficulty;
            double successChancePercent;
            int assignedKerbalCount;
            int outcomeSeed;
            string locationId = node.GetValue(LocationIdValueName);

            if (!TryParseLong(node.GetValue(MissionSequenceValueName), out missionSequence)
                || missionSequence < 0
                || !TryParseDefinedEnum(node.GetValue(MissionTypeValueName), out missionType)
                || string.IsNullOrEmpty(locationId)
                || !TryParseFiniteDouble(node.GetValue(LaunchUniversalTimeValueName), out launchUniversalTime)
                || launchUniversalTime < 0.0
                || !TryParseFiniteDouble(node.GetValue(DurationDaysValueName), out durationDays)
                || durationDays <= 0.0
                || !TryParseFiniteDouble(
                    node.GetValue(CompletionUniversalTimeValueName),
                    out completionUniversalTime)
                || completionUniversalTime < launchUniversalTime
                || !TryParseInt(node.GetValue(DifficultyValueName), out difficulty)
                || difficulty < 1
                || difficulty > 10
                || !TryParseFiniteDouble(
                    node.GetValue(SuccessChancePercentValueName),
                    out successChancePercent)
                || successChancePercent < 0.0
                || successChancePercent > 100.0
                || !TryParseInt(node.GetValue(AssignedKerbalCountValueName), out assignedKerbalCount)
                || assignedKerbalCount < 0
                || !TryParseInt(node.GetValue(OutcomeSeedValueName), out outcomeSeed))
            {
                return false;
            }

            string contractId = node.GetValue(ContractIdValueName);
            ScienceSubjectKey scienceSubject = null;
            if (missionType == RivalMissionType.Contract)
            {
                if (string.IsNullOrEmpty(contractId))
                {
                    return false;
                }
            }
            else if (!TryLoadScienceSubject(node, out scienceSubject))
            {
                return false;
            }

            double plannedScienceReward = 0.0;
            double parsedDouble;
            if (TryParseFiniteDouble(node.GetValue(PlannedScienceRewardValueName), out parsedDouble)
                && parsedDouble >= 0.0)
            {
                plannedScienceReward = parsedDouble;
            }

            mission = new RivalLiveMissionState
            {
                MissionSequence = missionSequence,
                MissionType = missionType,
                ContractId = contractId,
                ScienceSubject = scienceSubject,
                LocationId = locationId,
                LaunchUniversalTime = launchUniversalTime,
                DurationDays = durationDays,
                CompletionUniversalTime = completionUniversalTime,
                Difficulty = difficulty,
                SuccessChancePercent = successChancePercent,
                AssignedKerbalCount = assignedKerbalCount,
                PlannedScienceReward = plannedScienceReward,
                OutcomeSeed = outcomeSeed
            };
            return true;
        }

        private static void SaveLiveMission(ConfigNode node, RivalLiveMissionState mission)
        {
            node.AddValue(
                MissionSequenceValueName,
                mission.MissionSequence.ToString(CultureInfo.InvariantCulture));
            node.AddValue(MissionTypeValueName, mission.MissionType.ToString());
            if (!string.IsNullOrEmpty(mission.ContractId))
            {
                node.AddValue(ContractIdValueName, mission.ContractId);
            }
            if (mission.ScienceSubject != null)
            {
                SaveScienceSubject(node, mission.ScienceSubject);
            }

            node.AddValue(LocationIdValueName, mission.LocationId);
            node.AddValue(
                LaunchUniversalTimeValueName,
                mission.LaunchUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(DurationDaysValueName, mission.DurationDays.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(
                CompletionUniversalTimeValueName,
                mission.CompletionUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(DifficultyValueName, mission.Difficulty.ToString(CultureInfo.InvariantCulture));
            node.AddValue(
                SuccessChancePercentValueName,
                mission.SuccessChancePercent.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(
                AssignedKerbalCountValueName,
                mission.AssignedKerbalCount.ToString(CultureInfo.InvariantCulture));
            node.AddValue(
                PlannedScienceRewardValueName,
                mission.PlannedScienceReward.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(OutcomeSeedValueName, mission.OutcomeSeed.ToString(CultureInfo.InvariantCulture));
        }

        private static bool TryLoadConstruction(
            ConfigNode node,
            out RivalFacilityConstructionState construction)
        {
            construction = null;
            RivalFacilityType facility;
            int sourceLevel;
            int targetLevel;
            double startUniversalTime;
            double completionUniversalTime;
            double costPaidFunds;

            if (node == null
                || !TryParseDefinedEnum(node.GetValue(FacilityValueName), out facility)
                || !TryParseInt(node.GetValue(SourceLevelValueName), out sourceLevel)
                || !TryParseInt(node.GetValue(TargetLevelValueName), out targetLevel)
                || sourceLevel < 1
                || sourceLevel > 2
                || targetLevel != sourceLevel + 1
                || targetLevel > 3
                || !TryParseFiniteDouble(node.GetValue(StartUniversalTimeValueName), out startUniversalTime)
                || startUniversalTime < 0.0
                || !TryParseFiniteDouble(
                    node.GetValue(CompletionUniversalTimeValueName),
                    out completionUniversalTime)
                || completionUniversalTime < startUniversalTime
                || !TryParseFiniteDouble(node.GetValue(CostPaidFundsValueName), out costPaidFunds)
                || costPaidFunds < 0.0)
            {
                return false;
            }

            construction = new RivalFacilityConstructionState
            {
                Facility = facility,
                SourceLevel = sourceLevel,
                TargetLevel = targetLevel,
                StartUniversalTime = startUniversalTime,
                CompletionUniversalTime = completionUniversalTime,
                CostPaidFunds = costPaidFunds
            };
            return true;
        }

        private static void SaveConstruction(
            ConfigNode node,
            RivalFacilityConstructionState construction)
        {
            node.AddValue(FacilityValueName, construction.Facility.ToString());
            node.AddValue(SourceLevelValueName, construction.SourceLevel.ToString(CultureInfo.InvariantCulture));
            node.AddValue(TargetLevelValueName, construction.TargetLevel.ToString(CultureInfo.InvariantCulture));
            node.AddValue(
                StartUniversalTimeValueName,
                construction.StartUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(
                CompletionUniversalTimeValueName,
                construction.CompletionUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(
                CostPaidFundsValueName,
                construction.CostPaidFunds.ToString("R", CultureInfo.InvariantCulture));
        }

        private static RivalResearchProjectState LoadResearch(ConfigNode node)
        {
            if (node == null)
            {
                return null;
            }

            string techId = node.GetValue(TechIdValueName);
            double scienceCostPaid;
            double startUniversalTime;
            double researchReadyUniversalTime;
            double eligibleCompletionFundingUniversalTime;
            if (string.IsNullOrEmpty(techId)
                || !TryParseFiniteDouble(node.GetValue(ScienceCostPaidValueName), out scienceCostPaid)
                || scienceCostPaid < 0.0
                || !TryParseFiniteDouble(node.GetValue(StartUniversalTimeValueName), out startUniversalTime)
                || startUniversalTime < 0.0
                || !TryParseFiniteDouble(
                    node.GetValue(ResearchReadyUniversalTimeValueName),
                    out researchReadyUniversalTime)
                || researchReadyUniversalTime < startUniversalTime
                || !TryParseFiniteDouble(
                    node.GetValue(EligibleCompletionFundingUniversalTimeValueName),
                    out eligibleCompletionFundingUniversalTime)
                || eligibleCompletionFundingUniversalTime < researchReadyUniversalTime)
            {
                return null;
            }

            return new RivalResearchProjectState
            {
                TechId = techId,
                ScienceCostPaid = scienceCostPaid,
                StartUniversalTime = startUniversalTime,
                ResearchReadyUniversalTime = researchReadyUniversalTime,
                EligibleCompletionFundingUniversalTime = eligibleCompletionFundingUniversalTime
            };
        }

        private static void SaveResearch(ConfigNode node, RivalResearchProjectState research)
        {
            node.AddValue(TechIdValueName, research.TechId);
            node.AddValue(
                ScienceCostPaidValueName,
                research.ScienceCostPaid.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(
                StartUniversalTimeValueName,
                research.StartUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(
                ResearchReadyUniversalTimeValueName,
                research.ResearchReadyUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue(
                EligibleCompletionFundingUniversalTimeValueName,
                research.EligibleCompletionFundingUniversalTime.ToString("R", CultureInfo.InvariantCulture));
        }

        private static bool TryLoadScienceSubject(ConfigNode node, out ScienceSubjectKey subject)
        {
            subject = null;
            if (node == null)
            {
                return false;
            }

            string experimentId = node.GetValue(ExperimentIdValueName);
            string bodyName = node.GetValue(ScienceBodyNameValueName);
            string situation = node.GetValue(SituationValueName);
            if (string.IsNullOrEmpty(experimentId)
                || string.IsNullOrEmpty(bodyName)
                || string.IsNullOrEmpty(situation))
            {
                return false;
            }

            subject = new ScienceSubjectKey(
                experimentId,
                bodyName,
                situation,
                node.GetValue(BiomeNameValueName));
            return true;
        }

        private static void SaveScienceSubject(ConfigNode node, ScienceSubjectKey subject)
        {
            node.AddValue(ExperimentIdValueName, subject.ExperimentId);
            node.AddValue(ScienceBodyNameValueName, subject.BodyName);
            node.AddValue(SituationValueName, subject.Situation);
            if (!string.IsNullOrEmpty(subject.BiomeName))
            {
                node.AddValue(BiomeNameValueName, subject.BiomeName);
            }
        }

        private static ScienceSubjectKey CloneScienceSubject(ScienceSubjectKey subject)
        {
            if (subject == null
                || string.IsNullOrEmpty(subject.ExperimentId)
                || string.IsNullOrEmpty(subject.BodyName)
                || string.IsNullOrEmpty(subject.Situation))
            {
                return null;
            }

            return new ScienceSubjectKey(
                subject.ExperimentId,
                subject.BodyName,
                subject.Situation,
                subject.BiomeName);
        }

        private static ScienceLaunchPreparationState CloneSciencePreparation(
            ScienceLaunchPreparationState state)
        {
            ScienceSubjectKey subject = state == null ? null : CloneScienceSubject(state.Subject);
            if (state == null || subject == null)
            {
                return null;
            }

            return new ScienceLaunchPreparationState
            {
                Subject = subject,
                PlannedScienceReward = NormalizeNonNegative(state.PlannedScienceReward),
                LaunchProgressPercent = ClampPercent(state.LaunchProgressPercent),
                NextProgressCheckUniversalTime = NormalizeNonNegativeTime(
                    state.NextProgressCheckUniversalTime),
                RequiredKerbals = Math.Max(1, state.RequiredKerbals),
                ReadyUniversalTime = NormalizeReadyTime(state.ReadyUniversalTime)
            };
        }

        private static RivalLiveMissionState CloneLiveMission(RivalLiveMissionState mission)
        {
            if (mission == null
                || mission.MissionSequence < 0
                || string.IsNullOrEmpty(mission.LocationId)
                || !IsFinite(mission.LaunchUniversalTime)
                || mission.LaunchUniversalTime < 0.0
                || !IsFinite(mission.DurationDays)
                || mission.DurationDays <= 0.0
                || !IsFinite(mission.CompletionUniversalTime)
                || mission.CompletionUniversalTime < mission.LaunchUniversalTime
                || mission.Difficulty < 1
                || mission.Difficulty > 10
                || !IsFinite(mission.SuccessChancePercent)
                || mission.SuccessChancePercent < 0.0
                || mission.SuccessChancePercent > 100.0
                || mission.AssignedKerbalCount < 0)
            {
                return null;
            }

            ScienceSubjectKey scienceSubject = CloneScienceSubject(mission.ScienceSubject);
            if (mission.MissionType == RivalMissionType.Contract && string.IsNullOrEmpty(mission.ContractId))
            {
                return null;
            }
            if (mission.MissionType == RivalMissionType.Science && scienceSubject == null)
            {
                return null;
            }

            return new RivalLiveMissionState
            {
                MissionSequence = mission.MissionSequence,
                MissionType = mission.MissionType,
                ContractId = mission.ContractId,
                ScienceSubject = scienceSubject,
                LocationId = mission.LocationId,
                LaunchUniversalTime = mission.LaunchUniversalTime,
                DurationDays = mission.DurationDays,
                CompletionUniversalTime = mission.CompletionUniversalTime,
                Difficulty = mission.Difficulty,
                SuccessChancePercent = mission.SuccessChancePercent,
                AssignedKerbalCount = mission.AssignedKerbalCount,
                PlannedScienceReward = NormalizeNonNegative(mission.PlannedScienceReward),
                OutcomeSeed = mission.OutcomeSeed
            };
        }

        private static RivalFacilityConstructionState CloneConstruction(
            RivalFacilityConstructionState construction)
        {
            if (construction == null
                || construction.SourceLevel < 1
                || construction.SourceLevel > 2
                || construction.TargetLevel != construction.SourceLevel + 1
                || construction.TargetLevel > 3
                || !IsFinite(construction.StartUniversalTime)
                || construction.StartUniversalTime < 0.0
                || !IsFinite(construction.CompletionUniversalTime)
                || construction.CompletionUniversalTime < construction.StartUniversalTime
                || !IsFinite(construction.CostPaidFunds)
                || construction.CostPaidFunds < 0.0)
            {
                return null;
            }

            return new RivalFacilityConstructionState
            {
                Facility = construction.Facility,
                SourceLevel = construction.SourceLevel,
                TargetLevel = construction.TargetLevel,
                StartUniversalTime = construction.StartUniversalTime,
                CompletionUniversalTime = construction.CompletionUniversalTime,
                CostPaidFunds = construction.CostPaidFunds
            };
        }

        private static RivalResearchProjectState CloneResearch(RivalResearchProjectState research)
        {
            if (research == null
                || string.IsNullOrEmpty(research.TechId)
                || !IsFinite(research.ScienceCostPaid)
                || research.ScienceCostPaid < 0.0
                || !IsFinite(research.StartUniversalTime)
                || research.StartUniversalTime < 0.0
                || !IsFinite(research.ResearchReadyUniversalTime)
                || research.ResearchReadyUniversalTime < research.StartUniversalTime
                || !IsFinite(research.EligibleCompletionFundingUniversalTime)
                || research.EligibleCompletionFundingUniversalTime < research.ResearchReadyUniversalTime)
            {
                return null;
            }

            return new RivalResearchProjectState
            {
                TechId = research.TechId,
                ScienceCostPaid = research.ScienceCostPaid,
                StartUniversalTime = research.StartUniversalTime,
                ResearchReadyUniversalTime = research.ResearchReadyUniversalTime,
                EligibleCompletionFundingUniversalTime = research.EligibleCompletionFundingUniversalTime
            };
        }

        private static int CompareLiveMissions(RivalLiveMissionState left, RivalLiveMissionState right)
        {
            int sequenceComparison = left.MissionSequence.CompareTo(right.MissionSequence);
            if (sequenceComparison != 0)
            {
                return sequenceComparison;
            }

            int completionComparison = left.CompletionUniversalTime.CompareTo(right.CompletionUniversalTime);
            if (completionComparison != 0)
            {
                return completionComparison;
            }

            return StringComparer.OrdinalIgnoreCase.Compare(left.LocationId, right.LocationId);
        }

        private static int CompareScienceSubjects(ScienceSubjectKey left, ScienceSubjectKey right)
        {
            int comparison = StringComparer.OrdinalIgnoreCase.Compare(left.ExperimentId, right.ExperimentId);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.OrdinalIgnoreCase.Compare(left.BodyName, right.BodyName);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.OrdinalIgnoreCase.Compare(left.Situation, right.Situation);
            return comparison != 0
                ? comparison
                : StringComparer.OrdinalIgnoreCase.Compare(left.BiomeName, right.BiomeName);
        }

        private static int ClampPercent(int value)
        {
            return Math.Max(0, Math.Min(100, value));
        }

        private static double NormalizeNonNegative(double value)
        {
            return IsFinite(value) ? Math.Max(0.0, value) : 0.0;
        }

        private static double NormalizeNonNegativeTime(double value)
        {
            return IsFinite(value) ? Math.Max(0.0, value) : 0.0;
        }

        private static double NormalizeReadyTime(double value)
        {
            return IsFinite(value) && value >= 0.0 ? value : -1.0;
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

        private static bool TryParseInt(string value, out int parsedValue)
        {
            return int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out parsedValue);
        }

        private static bool TryParseLong(string value, out long parsedValue)
        {
            return long.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out parsedValue);
        }

        private static bool TryParseDefinedEnum<TEnum>(string value, out TEnum parsedValue)
            where TEnum : struct
        {
            return Enum.TryParse(value, true, out parsedValue)
                && Enum.IsDefined(typeof(TEnum), parsedValue);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}

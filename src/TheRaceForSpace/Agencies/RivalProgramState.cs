using System;
using System.Collections.Generic;

namespace TheRaceForSpace.Agencies
{
    /// <summary>
    /// Player-facing category for one launched rival mission.
    /// </summary>
    public enum RivalMissionType
    {
        Contract,
        Science
    }

    /// <summary>
    /// Stable identity for the nine rival Space Centre facilities simulated by the campaign.
    /// </summary>
    public enum RivalFacilityType
    {
        Administration,
        AstronautComplex,
        MissionControl,
        ResearchAndDevelopment,
        VehicleAssemblyBuilding,
        LaunchPad,
        SpaceplaneHangar,
        Runway,
        TrackingStation
    }

    /// <summary>
    /// Project-owned identity for one stock Science subject. KSP Science objects remain at the
    /// integration boundary; rival state persists only these stable subject components.
    /// </summary>
    public sealed class ScienceSubjectKey : IEquatable<ScienceSubjectKey>
    {
        public ScienceSubjectKey(
            string experimentId,
            string bodyName,
            string situation,
            string biomeName)
        {
            ExperimentId = experimentId ?? string.Empty;
            BodyName = bodyName ?? string.Empty;
            Situation = situation ?? string.Empty;
            BiomeName = biomeName ?? string.Empty;
        }

        public string ExperimentId { get; private set; }
        public string BodyName { get; private set; }
        public string Situation { get; private set; }
        public string BiomeName { get; private set; }

        public bool Equals(ScienceSubjectKey other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return StringComparer.OrdinalIgnoreCase.Equals(ExperimentId, other.ExperimentId)
                && StringComparer.OrdinalIgnoreCase.Equals(BodyName, other.BodyName)
                && StringComparer.OrdinalIgnoreCase.Equals(Situation, other.Situation)
                && StringComparer.OrdinalIgnoreCase.Equals(BiomeName, other.BiomeName);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ScienceSubjectKey);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 17;
                hashCode = (hashCode * 31) + StringComparer.OrdinalIgnoreCase.GetHashCode(ExperimentId);
                hashCode = (hashCode * 31) + StringComparer.OrdinalIgnoreCase.GetHashCode(BodyName);
                hashCode = (hashCode * 31) + StringComparer.OrdinalIgnoreCase.GetHashCode(Situation);
                hashCode = (hashCode * 31) + StringComparer.OrdinalIgnoreCase.GetHashCode(BiomeName);
                return hashCode;
            }
        }
    }

    /// <summary>
    /// Mutable launch-preparation state for the rival's one current Science Expedition.
    /// </summary>
    public sealed class ScienceLaunchPreparationState
    {
        public ScienceLaunchPreparationState()
        {
            RequiredKerbals = 1;
            ReadyUniversalTime = -1.0;
        }

        public ScienceSubjectKey Subject { get; set; }
        public double PlannedScienceReward { get; set; }
        public int LaunchProgressPercent { get; set; }
        public double NextProgressCheckUniversalTime { get; set; }
        public int RequiredKerbals { get; set; }
        public double ReadyUniversalTime { get; set; }
    }

    /// <summary>
    /// Authoritative state snapshotted when a rival Contract or Science mission launches.
    /// Presentation progress and ETA are derived from the stored dates rather than persisted here.
    /// </summary>
    public sealed class RivalLiveMissionState
    {
        public long MissionSequence { get; set; }
        public RivalMissionType MissionType { get; set; }
        public string ContractId { get; set; }
        public ScienceSubjectKey ScienceSubject { get; set; }
        public string LocationId { get; set; }
        public double LaunchUniversalTime { get; set; }
        public double DurationDays { get; set; }
        public double CompletionUniversalTime { get; set; }
        public int Difficulty { get; set; }
        public double SuccessChancePercent { get; set; }
        public int AssignedKerbalCount { get; set; }
        public double PlannedScienceReward { get; set; }
        public int OutcomeSeed { get; set; }
    }

    /// <summary>
    /// One rival technology project selected and paid for at a funding boundary.
    /// </summary>
    public sealed class RivalResearchProjectState
    {
        public string TechId { get; set; }
        public double ScienceCostPaid { get; set; }
        public double StartUniversalTime { get; set; }
        public double ResearchReadyUniversalTime { get; set; }
        public double EligibleCompletionFundingUniversalTime { get; set; }
    }

    /// <summary>
    /// One paid rival facility upgrade. The source level remains active until completion.
    /// </summary>
    public sealed class RivalFacilityConstructionState
    {
        public RivalFacilityType Facility { get; set; }
        public int SourceLevel { get; set; }
        public int TargetLevel { get; set; }
        public double StartUniversalTime { get; set; }
        public double CompletionUniversalTime { get; set; }
        public double CostPaidFunds { get; set; }
    }

    /// <summary>
    /// Rival-only mutable programme state composed onto an AgencyState. Common agency Funds,
    /// objectives, satellite counts, and normal Contract preparation remain on AgencyState.
    /// </summary>
    public sealed class RivalProgramState
    {
        public const string StartingTechId = "start";

        private readonly List<RivalLiveMissionState> _liveMissions =
            new List<RivalLiveMissionState>();
        private readonly HashSet<ScienceSubjectKey> _completedScienceSubjects =
            new HashSet<ScienceSubjectKey>();
        private readonly HashSet<string> _researchedTechIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<RivalFacilityType, int> _facilityLevels =
            new Dictionary<RivalFacilityType, int>();
        private readonly List<RivalFacilityConstructionState> _facilityConstruction =
            new List<RivalFacilityConstructionState>();

        public RivalProgramState()
        {
            KerbalsEmployed = 1;
            _researchedTechIds.Add(StartingTechId);

            Array facilityValues = Enum.GetValues(typeof(RivalFacilityType));
            for (int facilityIndex = 0; facilityIndex < facilityValues.Length; facilityIndex++)
            {
                RivalFacilityType facility = (RivalFacilityType)facilityValues.GetValue(facilityIndex);
                _facilityLevels[facility] = 1;
            }
        }

        public double StoredScience { get; set; }
        public ScienceLaunchPreparationState ScienceLaunchPreparation { get; set; }
        public IList<RivalLiveMissionState> LiveMissions { get { return _liveMissions; } }
        public ISet<ScienceSubjectKey> CompletedScienceSubjects { get { return _completedScienceSubjects; } }
        public ISet<string> ResearchedTechIds { get { return _researchedTechIds; } }
        public RivalResearchProjectState CurrentResearch { get; set; }
        public IDictionary<RivalFacilityType, int> FacilityLevels { get { return _facilityLevels; } }
        public IList<RivalFacilityConstructionState> FacilityConstruction { get { return _facilityConstruction; } }
        public int KerbalsEmployed { get; set; }
        public double PendingInsuranceFunds { get; set; }
    }
}

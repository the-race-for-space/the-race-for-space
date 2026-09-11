using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Objectives;

namespace TheRaceForSpace.Rivals
{
    /// <summary>
    /// Chronological coordinator for rival Contract preparation, Science preparation, and live missions.
    /// The runtime owns the five-second cadence; this class advances stored universal-time events only.
    /// </summary>
    public static class RivalSimulation
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double SatelliteMissionSelectionChance = 0.60;

        private static readonly Random SharedRandom = new Random();

        private enum RivalScheduledEventType
        {
            LiveMissionCompletion,
            ContractReadyLaunch,
            ScienceReadyLaunch,
            ContractProgressCheck,
            ScienceProgressCheck
        }

        private struct RivalScheduledEvent
        {
            public RivalScheduledEvent(
                AgencyState agency,
                RivalScheduledEventType eventType,
                double universalTime,
                RivalLiveMissionState liveMission)
            {
                Agency = agency;
                EventType = eventType;
                UniversalTime = universalTime;
                LiveMission = liveMission;
            }

            public readonly AgencyState Agency;
            public readonly RivalScheduledEventType EventType;
            public readonly double UniversalTime;
            public readonly RivalLiveMissionState LiveMission;
        }

        private sealed class RivalSimulationContext
        {
            private readonly Dictionary<AgencyState, IList<RivalScienceSubjectCandidate>> _scienceCandidatesByAgency =
                new Dictionary<AgencyState, IList<RivalScienceSubjectCandidate>>();

            public RivalSimulationContext(
                double targetUniversalTime,
                IList<ObjectiveFundingContract> objectiveFundingContracts,
                IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts,
                Func<AgencyState, IList<RivalScienceSubjectCandidate>> captureScienceCandidates,
                Func<ScienceSubjectKey, double> consumeRemainingScience,
                Random random)
            {
                TargetUniversalTime = targetUniversalTime;
                ObjectiveFundingContracts = objectiveFundingContracts;
                SatelliteNetworkFundingContracts = satelliteNetworkFundingContracts;
                CaptureScienceCandidates = captureScienceCandidates;
                ConsumeRemainingScience = consumeRemainingScience;
                Random = random ?? SharedRandom;
            }

            public readonly double TargetUniversalTime;
            public readonly IList<ObjectiveFundingContract> ObjectiveFundingContracts;
            public readonly IList<SatelliteNetworkFundingContract> SatelliteNetworkFundingContracts;
            public readonly Func<AgencyState, IList<RivalScienceSubjectCandidate>> CaptureScienceCandidates;
            public readonly Func<ScienceSubjectKey, double> ConsumeRemainingScience;
            public readonly Random Random;

            public IList<RivalScienceSubjectCandidate> GetScienceCandidates(AgencyState agency)
            {
                if (agency == null || CaptureScienceCandidates == null)
                {
                    return null;
                }

                IList<RivalScienceSubjectCandidate> candidates;
                if (_scienceCandidatesByAgency.TryGetValue(agency, out candidates))
                {
                    return candidates;
                }

                candidates = CaptureScienceCandidates(agency);
                _scienceCandidatesByAgency[agency] = candidates;
                return candidates;
            }

            public double ConsumeScienceAndInvalidate(ScienceSubjectKey subject)
            {
                if (ConsumeRemainingScience == null)
                {
                    return 0.0;
                }

                double scienceAwarded = ConsumeRemainingScience(subject);
                _scienceCandidatesByAgency.Clear();
                return scienceAwarded;
            }

            public void InvalidateScienceCandidates()
            {
                _scienceCandidatesByAgency.Clear();
            }
        }

        /// <summary>
        /// Advances every non-player agency in the supplied collection. The controller's sponsor
        /// review owns unlock-rule evaluation; rivals can only select contracts already marked Offered.
        /// This overload keeps KSP-independent callers usable when no stock Science boundary is supplied.
        /// </summary>
        public static void Refresh(
            IList<AgencyState> agencies,
            double currentUniversalTime,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            Refresh(
                agencies,
                currentUniversalTime,
                objectiveFundingContracts,
                satelliteNetworkFundingContracts,
                null,
                null);
        }

        /// <summary>
        /// Advances all rivals through the earliest stored event until the requested UT is reached.
        /// Science callbacks exchange only project-owned state; raw KSP Science objects remain in KspIntegration.
        /// </summary>
        internal static void Refresh(
            IList<AgencyState> agencies,
            double currentUniversalTime,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts,
            Func<AgencyState, IList<RivalScienceSubjectCandidate>> captureScienceCandidates,
            Func<ScienceSubjectKey, double> consumeRemainingScience)
        {
            Refresh(
                agencies,
                currentUniversalTime,
                objectiveFundingContracts,
                satelliteNetworkFundingContracts,
                captureScienceCandidates,
                consumeRemainingScience,
                SharedRandom);
        }

        /// <summary>
        /// Deterministic overload used by the KSP-independent regression suite. Production callers
        /// use the shared random source through the overload above.
        /// </summary>
        internal static void Refresh(
            IList<AgencyState> agencies,
            double currentUniversalTime,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts,
            Func<AgencyState, IList<RivalScienceSubjectCandidate>> captureScienceCandidates,
            Func<ScienceSubjectKey, double> consumeRemainingScience,
            Random random)
        {
            if (agencies == null
                || objectiveFundingContracts == null
                || satelliteNetworkFundingContracts == null
                || !IsFiniteNonNegative(currentUniversalTime))
            {
                return;
            }

            List<AgencyState> rivals = GetSortedRivals(agencies);
            var context = new RivalSimulationContext(
                currentUniversalTime,
                objectiveFundingContracts,
                satelliteNetworkFundingContracts,
                captureScienceCandidates,
                consumeRemainingScience,
                random);

            for (int rivalIndex = 0; rivalIndex < rivals.Count; rivalIndex++)
            {
                PrepareProgrammeForRefresh(rivals[rivalIndex], context);
            }

            double eventCursorUniversalTime = 0.0;
            RivalScheduledEvent scheduledEvent;
            while (TryFindNextScheduledEvent(
                rivals,
                eventCursorUniversalTime,
                context,
                out scheduledEvent))
            {
                eventCursorUniversalTime = Math.Max(
                    eventCursorUniversalTime,
                    scheduledEvent.UniversalTime);
                ProcessScheduledEvent(scheduledEvent, eventCursorUniversalTime, context);
            }
        }

        /// <summary>
        /// Returns presentation text for a stable mission target ID using the live contract
        /// collections. Mission identity is never inferred from this display text.
        /// </summary>
        public static string GetMissionTargetDisplayName(
            string targetId,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            ObjectiveFundingContract objectiveFundingContract = FindObjectiveFundingContract(
                targetId,
                objectiveFundingContracts);
            if (objectiveFundingContract != null)
            {
                return objectiveFundingContract.Name;
            }

            SatelliteNetworkFundingContract networkContract = FindSatelliteNetworkFundingContract(
                targetId,
                satelliteNetworkFundingContracts);
            return networkContract == null ? null : networkContract.CelestialBodyName;
        }

        /// <summary>
        /// Returns the Launch Progress gained by a successful normal rival preparation check.
        /// </summary>
        public static int CalculateLaunchProgressIncrementPercent(AgencyState agency)
        {
            ObjectiveDefinition objective = agency == null
                ? null
                : ObjectiveCatalogue.FindById(agency.NextMissionTargetId);
            return objective != null && objective.IsPreOrbitContract
                ? Math.Max(1, CampaignSettings.RivalPreOrbitLaunchProgressStepPercent)
                : Math.Max(1, CampaignSettings.RivalNormalLaunchProgressStepPercent);
        }

        /// <summary>
        /// Returns the funds required for the rival's next successful normal Launch Progress step.
        /// Stable mission target IDs are authoritative; presentation text is not used as a fallback.
        /// </summary>
        public static double CalculateMissionProgressCost(
            AgencyState agency,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            if (agency == null)
            {
                return CampaignSettings.Kerbin.ProbeProgressCostFunds;
            }

            return CalculateMissionProgressCostForTarget(
                agency.NextMissionTargetId,
                satelliteNetworkFundingContracts);
        }

        /// <summary>
        /// Estimates average Kerbin days until the current normal preparation reaches launch readiness.
        /// Returns null when current funds and projected scheduled payouts cannot finance all remaining
        /// successful progress steps, or when the supplied simulation state is invalid.
        /// </summary>
        public static int? CalculateEstimatedLaunchDays(
            AgencyState agency,
            double currentUniversalTime,
            double nextFundingUniversalTime,
            double fundingIntervalSeconds,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            return CalculateEstimatedLaunchDays(
                agency,
                currentUniversalTime,
                nextFundingUniversalTime,
                fundingIntervalSeconds,
                CalculateMissionProgressCost(agency, objectiveFundingContracts, satelliteNetworkFundingContracts));
        }

        private static int? CalculateEstimatedLaunchDays(
            AgencyState agency,
            double currentUniversalTime,
            double nextFundingUniversalTime,
            double fundingIntervalSeconds,
            double launchProgressCostFunds)
        {
            double progressChance = RivalDevelopmentSimulation.GetNormalLaunchProgressChance(agency);
            double launchProgressIntervalSeconds = GetNormalLaunchProgressIntervalSeconds();
            if (agency == null
                || agency.IsPlayer
                || string.IsNullOrEmpty(agency.NextMissionTargetId)
                || !IsFiniteNonNegative(currentUniversalTime)
                || !IsFinite(nextFundingUniversalTime)
                || !IsFinite(fundingIntervalSeconds)
                || fundingIntervalSeconds <= 0.0
                || !IsFinite(launchProgressCostFunds)
                || launchProgressCostFunds < 0.0
                || !IsFinite(agency.Funds)
                || !IsFinite(agency.NextPayoutFunds)
                || !IsFinite(progressChance)
                || progressChance <= 0.0
                || progressChance > 1.0
                || launchProgressIntervalSeconds <= 0.0)
            {
                return null;
            }

            int launchProgressIncrementPercent = CalculateLaunchProgressIncrementPercent(agency);
            int currentProgressPercent = Math.Max(0, Math.Min(100, agency.MissionProgressPercent));
            int remainingProgressPercent = 100 - currentProgressPercent;
            int remainingProgressSteps = (remainingProgressPercent + launchProgressIncrementPercent - 1)
                / launchProgressIncrementPercent;
            if (remainingProgressSteps <= 0)
            {
                return 0;
            }

            double availableFunds = Math.Max(0.0, agency.Funds);
            double projectedPayoutFunds = Math.Max(0.0, agency.NextPayoutFunds);
            double expectedDaysPerSuccessfulStep =
                (launchProgressIntervalSeconds / KerbinDaySeconds) / progressChance;
            double fundingIntervalDays = fundingIntervalSeconds / KerbinDaySeconds;
            double nextFundingInDays = nextFundingUniversalTime >= 0.0
                ? Math.Max(0.0, (nextFundingUniversalTime - currentUniversalTime) / KerbinDaySeconds)
                : double.PositiveInfinity;
            double elapsedDays = 0.0;

            if (!IsFinite(expectedDaysPerSuccessfulStep) || !IsFinite(fundingIntervalDays))
            {
                return null;
            }

            for (int stepIndex = 0; stepIndex < remainingProgressSteps; stepIndex++)
            {
                double expectedStepDay = elapsedDays + expectedDaysPerSuccessfulStep;

                while (projectedPayoutFunds > 0.0
                    && fundingIntervalDays > 0.0
                    && nextFundingInDays < expectedStepDay)
                {
                    availableFunds += projectedPayoutFunds;
                    nextFundingInDays += fundingIntervalDays;
                }

                if (availableFunds < launchProgressCostFunds)
                {
                    if (projectedPayoutFunds <= 0.0
                        || fundingIntervalDays <= 0.0
                        || double.IsPositiveInfinity(nextFundingInDays))
                    {
                        return null;
                    }

                    while (availableFunds < launchProgressCostFunds)
                    {
                        elapsedDays = Math.Max(elapsedDays, nextFundingInDays);
                        availableFunds += projectedPayoutFunds;
                        nextFundingInDays += fundingIntervalDays;
                    }

                    expectedStepDay = elapsedDays + expectedDaysPerSuccessfulStep;
                    while (nextFundingInDays < expectedStepDay)
                    {
                        availableFunds += projectedPayoutFunds;
                        nextFundingInDays += fundingIntervalDays;
                    }
                }

                availableFunds -= launchProgressCostFunds;
                elapsedDays = expectedStepDay;
            }

            double roundedDays = Math.Ceiling(elapsedDays);
            if (!IsFinite(roundedDays) || roundedDays > int.MaxValue)
            {
                return null;
            }

            return (int)roundedDays;
        }

        private static List<AgencyState> GetSortedRivals(IList<AgencyState> agencies)
        {
            var rivals = new List<AgencyState>();
            for (int agencyIndex = 0; agencyIndex < agencies.Count; agencyIndex++)
            {
                AgencyState agency = agencies[agencyIndex];
                if (agency != null && !agency.IsPlayer && agency.RivalProgram != null)
                {
                    rivals.Add(agency);
                }
            }

            rivals.Sort(CompareAgenciesByStableId);
            return rivals;
        }

        private static int CompareAgenciesByStableId(AgencyState first, AgencyState second)
        {
            if (object.ReferenceEquals(first, second))
            {
                return 0;
            }
            if (first == null)
            {
                return -1;
            }
            if (second == null)
            {
                return 1;
            }

            int idComparison = StringComparer.OrdinalIgnoreCase.Compare(first.Id, second.Id);
            return idComparison != 0
                ? idComparison
                : StringComparer.OrdinalIgnoreCase.Compare(first.Name, second.Name);
        }

        private static void PrepareProgrammeForRefresh(
            AgencyState agency,
            RivalSimulationContext context)
        {
            if (agency == null)
            {
                return;
            }

            agency.MissionProgressPercent = Math.Max(0, Math.Min(100, agency.MissionProgressPercent));
            string targetId = agency.NextMissionTargetId;
            if (!string.IsNullOrEmpty(targetId))
            {
                agency.NextMissionDisplayName = GetMissionTargetDisplayName(
                    targetId,
                    context.ObjectiveFundingContracts,
                    context.SatelliteNetworkFundingContracts);
            }

            if (!string.IsNullOrEmpty(targetId)
                && !IsTargetAvailable(targetId, agency, context))
            {
                ClearNormalPreparation(agency);
            }

            if (string.IsNullOrEmpty(agency.NextMissionTargetId))
            {
                SelectNextNormalPreparation(agency, context.TargetUniversalTime, context);
            }
            else if (agency.MissionProgressPercent >= 100)
            {
                agency.MissionProgressPercent = 100;
                if (!IsFiniteNonNegative(agency.NextMissionReadyUniversalTime))
                {
                    // v0.5 saves had no ready timestamp. Treat a restored 100% preparation as ready
                    // now rather than fabricating an earlier launch order that the save never stored.
                    agency.NextMissionReadyUniversalTime = context.TargetUniversalTime;
                }
                agency.NextMissionProgressCheckUniversalTime = 0.0;
            }
            else
            {
                agency.NextMissionReadyUniversalTime = -1.0;
                if (!IsFiniteNonNegative(agency.NextMissionProgressCheckUniversalTime)
                    || agency.NextMissionProgressCheckUniversalTime <= 0.0)
                {
                    agency.NextMissionProgressCheckUniversalTime =
                        CalculateNextNormalProgressCheckUniversalTime(context.TargetUniversalTime);
                }
            }

            RivalProgramState programme = agency.RivalProgram;
            if (context.CaptureScienceCandidates == null || programme == null)
            {
                return;
            }

            ScienceLaunchPreparationState sciencePreparation = programme.ScienceLaunchPreparation;
            if (sciencePreparation != null)
            {
                sciencePreparation.LaunchProgressPercent = Math.Max(
                    0,
                    Math.Min(100, sciencePreparation.LaunchProgressPercent));
                sciencePreparation.RequiredKerbals = Math.Max(1, sciencePreparation.RequiredKerbals);

                if (sciencePreparation.LaunchProgressPercent >= 100)
                {
                    if (!IsFiniteNonNegative(sciencePreparation.ReadyUniversalTime))
                    {
                        // Early v0.6 development saves may contain 100% Science preparation without
                        // the later ready timestamp. As with normal Contract preparation, treat it as
                        // ready now rather than inventing a historical crew-arbitration order.
                        sciencePreparation.ReadyUniversalTime = context.TargetUniversalTime;
                    }

                    sciencePreparation.NextProgressCheckUniversalTime = 0.0;
                    return;
                }

                sciencePreparation.ReadyUniversalTime = -1.0;
                if (sciencePreparation.Subject != null
                    && IsFiniteNonNegative(sciencePreparation.NextProgressCheckUniversalTime)
                    && sciencePreparation.NextProgressCheckUniversalTime > 0.0)
                {
                    return;
                }

                // Invalid/incomplete preparation timing needs one stock revalidation so the Science
                // specialist can repair its cadence or replace an unusable subject safely.
                IList<RivalScienceSubjectCandidate> repairCandidates = context.GetScienceCandidates(agency);
                if (repairCandidates != null)
                {
                    RivalScienceSimulation.RefreshPreparationTarget(
                        agency,
                        context.TargetUniversalTime,
                        repairCandidates,
                        context.Random);
                }
                return;
            }

            // Building stock Science candidates can inspect many experiment/body/situation combinations.
            // Do it when a rival actually needs a new preparation; existing preparations are revalidated
            // only on their daily check or ready-to-launch event rather than every five-second refresh.
            IList<RivalScienceSubjectCandidate> scienceCandidates = context.GetScienceCandidates(agency);
            if (scienceCandidates != null)
            {
                RivalScienceSimulation.TrySelectNextPreparation(
                    agency,
                    context.TargetUniversalTime,
                    scienceCandidates,
                    context.Random);
            }
        }

        private static bool TryFindNextScheduledEvent(
            IList<AgencyState> rivals,
            double eventCursorUniversalTime,
            RivalSimulationContext context,
            out RivalScheduledEvent scheduledEvent)
        {
            scheduledEvent = default(RivalScheduledEvent);
            bool hasEvent = false;

            for (int rivalIndex = 0; rivalIndex < rivals.Count; rivalIndex++)
            {
                AgencyState agency = rivals[rivalIndex];
                RivalLiveMissionState liveMission = GetNextResolvableLiveMission(agency, context);
                if (liveMission != null)
                {
                    ConsiderScheduledEvent(
                        new RivalScheduledEvent(
                            agency,
                            RivalScheduledEventType.LiveMissionCompletion,
                            liveMission.CompletionUniversalTime,
                            liveMission),
                        ref hasEvent,
                        ref scheduledEvent);
                }

                if (IsContractPreparationReady(agency)
                    && agency.NextMissionReadyUniversalTime <= context.TargetUniversalTime)
                {
                    bool targetAvailable = IsTargetAvailable(
                        agency.NextMissionTargetId,
                        agency,
                        context);
                    if (!targetAvailable || CanLaunchContractPreparation(agency, context))
                    {
                        ConsiderScheduledEvent(
                            new RivalScheduledEvent(
                                agency,
                                RivalScheduledEventType.ContractReadyLaunch,
                                agency.NextMissionReadyUniversalTime,
                                null),
                            ref hasEvent,
                            ref scheduledEvent);
                    }
                }

                RivalProgramState programme = agency.RivalProgram;
                ScienceLaunchPreparationState sciencePreparation = programme == null
                    ? null
                    : programme.ScienceLaunchPreparation;
                if (sciencePreparation != null
                    && sciencePreparation.LaunchProgressPercent >= 100
                    && IsFiniteNonNegative(sciencePreparation.ReadyUniversalTime)
                    && sciencePreparation.ReadyUniversalTime <= context.TargetUniversalTime
                    && context.CaptureScienceCandidates != null)
                {
                    IList<RivalScienceSubjectCandidate> scienceCandidates = context.GetScienceCandidates(agency);
                    if (scienceCandidates != null)
                    {
                        RivalScienceSubjectCandidate currentCandidate =
                            RivalScienceSimulation.FindCurrentPreparationCandidate(
                                agency,
                                scienceCandidates);
                        if (currentCandidate == null || CanLaunchSciencePreparation(agency))
                        {
                            ConsiderScheduledEvent(
                                new RivalScheduledEvent(
                                    agency,
                                    RivalScheduledEventType.ScienceReadyLaunch,
                                    sciencePreparation.ReadyUniversalTime,
                                    null),
                                ref hasEvent,
                                ref scheduledEvent);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(agency.NextMissionTargetId)
                    && agency.MissionProgressPercent < 100
                    && IsFiniteNonNegative(agency.NextMissionProgressCheckUniversalTime)
                    && agency.NextMissionProgressCheckUniversalTime > 0.0
                    && agency.NextMissionProgressCheckUniversalTime <= context.TargetUniversalTime)
                {
                    ConsiderScheduledEvent(
                        new RivalScheduledEvent(
                            agency,
                            RivalScheduledEventType.ContractProgressCheck,
                            agency.NextMissionProgressCheckUniversalTime,
                            null),
                        ref hasEvent,
                        ref scheduledEvent);
                }

                if (sciencePreparation != null
                    && sciencePreparation.LaunchProgressPercent < 100
                    && IsFiniteNonNegative(sciencePreparation.NextProgressCheckUniversalTime)
                    && sciencePreparation.NextProgressCheckUniversalTime > 0.0
                    && sciencePreparation.NextProgressCheckUniversalTime <= context.TargetUniversalTime
                    && context.CaptureScienceCandidates != null
                    && context.GetScienceCandidates(agency) != null)
                {
                    ConsiderScheduledEvent(
                        new RivalScheduledEvent(
                            agency,
                            RivalScheduledEventType.ScienceProgressCheck,
                            sciencePreparation.NextProgressCheckUniversalTime,
                            null),
                        ref hasEvent,
                        ref scheduledEvent);
                }
            }

            return hasEvent;
        }

        private static RivalLiveMissionState GetNextResolvableLiveMission(
            AgencyState agency,
            RivalSimulationContext context)
        {
            RivalProgramState programme = agency == null ? null : agency.RivalProgram;
            if (programme == null)
            {
                return null;
            }

            RivalLiveMissionState selectedMission = null;
            for (int missionIndex = 0; missionIndex < programme.LiveMissions.Count; missionIndex++)
            {
                RivalLiveMissionState mission = programme.LiveMissions[missionIndex];
                if (mission == null
                    || !IsFiniteNonNegative(mission.CompletionUniversalTime)
                    || mission.CompletionUniversalTime > context.TargetUniversalTime
                    || (mission.MissionType == RivalMissionType.Science
                        && (context.ConsumeRemainingScience == null
                            || !ContainsScienceSubjectCandidate(
                                context.GetScienceCandidates(agency),
                                mission.ScienceSubject))))
                {
                    continue;
                }

                if (selectedMission == null
                    || mission.CompletionUniversalTime < selectedMission.CompletionUniversalTime
                    || (mission.CompletionUniversalTime == selectedMission.CompletionUniversalTime
                        && mission.MissionSequence < selectedMission.MissionSequence))
                {
                    selectedMission = mission;
                }
            }

            return selectedMission;
        }

        private static bool ContainsScienceSubjectCandidate(
            IList<RivalScienceSubjectCandidate> candidates,
            ScienceSubjectKey subject)
        {
            if (candidates == null || subject == null)
            {
                return false;
            }

            for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
            {
                RivalScienceSubjectCandidate candidate = candidates[candidateIndex];
                if (candidate != null
                    && candidate.Subject != null
                    && subject.Equals(candidate.Subject))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ConsiderScheduledEvent(
            RivalScheduledEvent candidate,
            ref bool hasEvent,
            ref RivalScheduledEvent selectedEvent)
        {
            if (!hasEvent || CompareScheduledEvents(candidate, selectedEvent) < 0)
            {
                hasEvent = true;
                selectedEvent = candidate;
            }
        }

        private static int CompareScheduledEvents(
            RivalScheduledEvent first,
            RivalScheduledEvent second)
        {
            int timeComparison = first.UniversalTime.CompareTo(second.UniversalTime);
            if (timeComparison != 0)
            {
                return timeComparison;
            }

            int typeComparison = GetEventPriority(first.EventType).CompareTo(GetEventPriority(second.EventType));
            if (typeComparison != 0)
            {
                return typeComparison;
            }

            int agencyComparison = CompareAgenciesByStableId(first.Agency, second.Agency);
            if (agencyComparison != 0)
            {
                return agencyComparison;
            }

            long firstSequence = first.LiveMission == null ? 0 : first.LiveMission.MissionSequence;
            long secondSequence = second.LiveMission == null ? 0 : second.LiveMission.MissionSequence;
            return firstSequence.CompareTo(secondSequence);
        }

        private static int GetEventPriority(RivalScheduledEventType eventType)
        {
            switch (eventType)
            {
                case RivalScheduledEventType.LiveMissionCompletion:
                    return 0;
                case RivalScheduledEventType.ContractReadyLaunch:
                    return 1;
                case RivalScheduledEventType.ScienceReadyLaunch:
                    return 2;
                case RivalScheduledEventType.ContractProgressCheck:
                    return 3;
                case RivalScheduledEventType.ScienceProgressCheck:
                    return 4;
                default:
                    return int.MaxValue;
            }
        }

        private static void ProcessScheduledEvent(
            RivalScheduledEvent scheduledEvent,
            double eventUniversalTime,
            RivalSimulationContext context)
        {
            switch (scheduledEvent.EventType)
            {
                case RivalScheduledEventType.LiveMissionCompletion:
                    ProcessLiveMissionCompletion(
                        scheduledEvent.Agency,
                        scheduledEvent.LiveMission,
                        eventUniversalTime,
                        context);
                    break;

                case RivalScheduledEventType.ContractReadyLaunch:
                    TryLaunchReadyContract(scheduledEvent.Agency, eventUniversalTime, context);
                    break;

                case RivalScheduledEventType.ScienceReadyLaunch:
                    TryLaunchReadyScience(scheduledEvent.Agency, eventUniversalTime, context);
                    break;

                case RivalScheduledEventType.ContractProgressCheck:
                    ProcessNormalProgressCheck(scheduledEvent.Agency, eventUniversalTime, context);
                    break;

                case RivalScheduledEventType.ScienceProgressCheck:
                    ProcessScienceProgressCheck(scheduledEvent.Agency, eventUniversalTime, context);
                    break;
            }
        }

        private static void ProcessLiveMissionCompletion(
            AgencyState agency,
            RivalLiveMissionState mission,
            double eventUniversalTime,
            RivalSimulationContext context)
        {
            Func<ScienceSubjectKey, double> scienceConsumer = mission != null
                && mission.MissionType == RivalMissionType.Science
                ? new Func<ScienceSubjectKey, double>(context.ConsumeScienceAndInvalidate)
                : null;

            RivalLiveMissionResolution resolution = RivalLiveMissionSimulation.ResolveMission(
                agency,
                mission,
                eventUniversalTime,
                context.SatelliteNetworkFundingContracts,
                scienceConsumer);
            if (resolution != null && resolution.ObjectiveRecorded)
            {
                // A successful Probe Orbit can immediately widen this rival's Science situation access.
                context.InvalidateScienceCandidates();
            }
        }

        private static void ProcessNormalProgressCheck(
            AgencyState agency,
            double eventUniversalTime,
            RivalSimulationContext context)
        {
            if (agency == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(agency.NextMissionTargetId))
            {
                SelectNextNormalPreparation(agency, eventUniversalTime, context);
                return;
            }

            if (!IsTargetAvailable(agency.NextMissionTargetId, agency, context))
            {
                ClearNormalPreparation(agency);
                SelectNextNormalPreparation(agency, eventUniversalTime, context);
                return;
            }

            if (agency.MissionProgressPercent >= 100)
            {
                return;
            }

            double scheduledCheckUniversalTime = agency.NextMissionProgressCheckUniversalTime;
            if (!IsFiniteNonNegative(scheduledCheckUniversalTime)
                || scheduledCheckUniversalTime <= 0.0
                || eventUniversalTime < scheduledCheckUniversalTime)
            {
                return;
            }

            double launchProgressCostFunds = CalculateMissionProgressCost(
                agency,
                context.ObjectiveFundingContracts,
                context.SatelliteNetworkFundingContracts);
            double progressChance = RivalDevelopmentSimulation.GetNormalLaunchProgressChance(agency);
            if (IsFinite(launchProgressCostFunds)
                && launchProgressCostFunds >= 0.0
                && IsFinite(agency.Funds)
                && agency.Funds >= launchProgressCostFunds
                && progressChance > 0.0
                && context.Random.NextDouble() < progressChance)
            {
                agency.Funds -= launchProgressCostFunds;
                agency.MissionProgressPercent = Math.Min(
                    100,
                    agency.MissionProgressPercent + CalculateLaunchProgressIncrementPercent(agency));
            }

            if (agency.MissionProgressPercent >= 100)
            {
                agency.MissionProgressPercent = 100;
                agency.NextMissionReadyUniversalTime = scheduledCheckUniversalTime;
                agency.NextMissionProgressCheckUniversalTime = 0.0;
                return;
            }

            agency.NextMissionReadyUniversalTime = -1.0;
            double intervalSeconds = GetNormalLaunchProgressIntervalSeconds();
            double nextCheckUniversalTime = scheduledCheckUniversalTime + intervalSeconds;
            agency.NextMissionProgressCheckUniversalTime =
                intervalSeconds > 0.0 && IsFiniteNonNegative(nextCheckUniversalTime)
                    ? nextCheckUniversalTime
                    : 0.0;
        }

        private static void ProcessScienceProgressCheck(
            AgencyState agency,
            double eventUniversalTime,
            RivalSimulationContext context)
        {
            IList<RivalScienceSubjectCandidate> scienceCandidates = context.GetScienceCandidates(agency);
            if (scienceCandidates == null)
            {
                return;
            }

            RivalScienceSimulation.ProcessProgressCheck(
                agency,
                eventUniversalTime,
                scienceCandidates,
                context.Random);
        }

        private static bool TryLaunchReadyContract(
            AgencyState agency,
            double launchUniversalTime,
            RivalSimulationContext context)
        {
            if (!IsContractPreparationReady(agency))
            {
                return false;
            }

            if (!IsTargetAvailable(agency.NextMissionTargetId, agency, context))
            {
                ClearNormalPreparation(agency);
                SelectNextNormalPreparation(agency, launchUniversalTime, context);
                return false;
            }

            if (!HasSatelliteCapacityForTarget(agency, agency.NextMissionTargetId, context))
            {
                return false;
            }

            int requiredKerbals = GetRequiredKerbalsForTarget(
                agency.NextMissionTargetId,
                context.ObjectiveFundingContracts,
                context.SatelliteNetworkFundingContracts);
            int hiredKerbals;
            if (!RivalDevelopmentSimulation.TryHireMissingKerbals(
                agency,
                requiredKerbals,
                out hiredKerbals))
            {
                return false;
            }

            RivalLiveMissionState mission = RivalLiveMissionSimulation.TryCreateContractMission(
                agency,
                agency.NextMissionTargetId,
                launchUniversalTime,
                context.ObjectiveFundingContracts,
                context.SatelliteNetworkFundingContracts,
                context.Random.Next());
            if (mission == null)
            {
                // Hiring belongs to the launch attempt. If malformed/custom mission data still prevents
                // creation after the preflight gates, undo those hires before abandoning the target.
                RollbackHiredKerbals(agency, hiredKerbals);
                ClearNormalPreparation(agency);
                SelectNextNormalPreparation(agency, launchUniversalTime, context);
                return false;
            }

            ClearNormalPreparation(agency);
            SelectNextNormalPreparation(agency, launchUniversalTime, context);
            return true;
        }

        private static bool TryLaunchReadyScience(
            AgencyState agency,
            double launchUniversalTime,
            RivalSimulationContext context)
        {
            if (agency == null
                || agency.RivalProgram == null
                || agency.RivalProgram.ScienceLaunchPreparation == null)
            {
                return false;
            }

            IList<RivalScienceSubjectCandidate> scienceCandidates = context.GetScienceCandidates(agency);
            if (scienceCandidates == null)
            {
                return false;
            }

            RivalScienceSimulation.RefreshPreparationTarget(
                agency,
                launchUniversalTime,
                scienceCandidates,
                context.Random);
            ScienceLaunchPreparationState preparation = agency.RivalProgram.ScienceLaunchPreparation;
            if (preparation == null || preparation.LaunchProgressPercent < 100)
            {
                return false;
            }

            RivalScienceSubjectCandidate candidate = RivalScienceSimulation.FindCurrentPreparationCandidate(
                agency,
                scienceCandidates);
            if (candidate == null)
            {
                return false;
            }

            // Preparation may have survived partial player/rival depletion. Snapshot the latest
            // remaining shared pool at launch; final completion will still re-check it chronologically.
            preparation.PlannedScienceReward = Math.Max(0.0, candidate.RemainingScience);

            int hiredKerbals;
            if (!RivalDevelopmentSimulation.TryHireMissingKerbals(
                agency,
                preparation.RequiredKerbals,
                out hiredKerbals))
            {
                return false;
            }

            RivalLiveMissionState mission = RivalLiveMissionSimulation.TryCreateScienceMission(
                agency,
                preparation,
                candidate.LocationId,
                launchUniversalTime,
                context.Random.Next());
            if (mission == null)
            {
                // Hiring belongs to the launch attempt. If malformed/custom state still prevents mission
                // creation, undo those hires before reselecting instead of charging for a launch that failed.
                RollbackHiredKerbals(agency, hiredKerbals);
                RivalScienceSimulation.ClearPreparationAfterLaunch(agency);
                RivalScienceSimulation.TrySelectNextPreparation(
                    agency,
                    launchUniversalTime,
                    scienceCandidates,
                    context.Random);
                return false;
            }

            RivalScienceSimulation.ClearPreparationAfterLaunch(agency);

            // Science preparation is independent from live mission travel. Start the next valid target
            // immediately so time-warp catch-up can continue its daily checks while this mission is live.
            RivalScienceSimulation.TrySelectNextPreparation(
                agency,
                launchUniversalTime,
                scienceCandidates,
                context.Random);
            return true;
        }

        private static void RollbackHiredKerbals(AgencyState agency, int hiredKerbals)
        {
            if (agency == null || agency.RivalProgram == null || hiredKerbals <= 0)
            {
                return;
            }

            agency.RivalProgram.KerbalsEmployed = Math.Max(
                0,
                agency.RivalProgram.KerbalsEmployed - hiredKerbals);
            double refundFunds = hiredKerbals * Math.Max(0.0, CampaignSettings.RivalKerbalHireCostFunds);
            if (IsFinite(refundFunds) && IsFinite(agency.Funds + refundFunds))
            {
                agency.Funds += refundFunds;
            }
        }

        private static bool IsContractPreparationReady(AgencyState agency)
        {
            return agency != null
                && !string.IsNullOrEmpty(agency.NextMissionTargetId)
                && agency.MissionProgressPercent >= 100
                && IsFiniteNonNegative(agency.NextMissionReadyUniversalTime);
        }

        private static bool CanLaunchContractPreparation(
            AgencyState agency,
            RivalSimulationContext context)
        {
            if (!IsContractPreparationReady(agency)
                || !HasSatelliteCapacityForTarget(agency, agency.NextMissionTargetId, context))
            {
                return false;
            }

            int requiredKerbals = GetRequiredKerbalsForTarget(
                agency.NextMissionTargetId,
                context.ObjectiveFundingContracts,
                context.SatelliteNetworkFundingContracts);
            return RivalDevelopmentSimulation.GetKerbalsAvailable(agency) >= requiredKerbals
                || RivalDevelopmentSimulation.CanHireMissingKerbals(agency, requiredKerbals);
        }

        private static bool CanLaunchSciencePreparation(AgencyState agency)
        {
            ScienceLaunchPreparationState preparation = agency == null || agency.RivalProgram == null
                ? null
                : agency.RivalProgram.ScienceLaunchPreparation;
            if (preparation == null || preparation.LaunchProgressPercent < 100)
            {
                return false;
            }

            return RivalDevelopmentSimulation.GetKerbalsAvailable(agency) >= preparation.RequiredKerbals
                || RivalDevelopmentSimulation.CanHireMissingKerbals(agency, preparation.RequiredKerbals);
        }

        private static bool HasSatelliteCapacityForTarget(
            AgencyState agency,
            string targetId,
            RivalSimulationContext context)
        {
            if (!TargetProducesSatellite(targetId, context.SatelliteNetworkFundingContracts))
            {
                return true;
            }

            int satelliteLimit = RivalDevelopmentSimulation.GetSatelliteLimit(agency);
            return satelliteLimit == 0
                || RivalLiveMissionSimulation.GetSatelliteCapacityUsed(
                    agency,
                    context.SatelliteNetworkFundingContracts) < satelliteLimit;
        }

        private static bool TargetProducesSatellite(
            string targetId,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(targetId);
            if (objective != null)
            {
                return objective.ObjectiveType == ObjectiveType.Orbit
                    && objective.CrewRequirement == ObjectiveCrewRequirement.UncrewedProbe;
            }

            return FindSatelliteNetworkFundingContract(targetId, satelliteNetworkFundingContracts) != null;
        }

        private static int GetRequiredKerbalsForTarget(
            string targetId,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            ObjectiveFundingContract objectiveFundingContract = FindObjectiveFundingContract(
                targetId,
                objectiveFundingContracts);
            if (objectiveFundingContract != null)
            {
                ObjectiveDefinition objective = ObjectiveCatalogue.FindById(objectiveFundingContract.Id);
                return objective == null ? 0 : Math.Max(0, objective.RequiredKerbalCount);
            }

            SatelliteNetworkFundingContract networkContract = FindSatelliteNetworkFundingContract(
                targetId,
                satelliteNetworkFundingContracts);
            return networkContract == null ? 0 : Math.Max(0, networkContract.RequiredKerbalCount);
        }

        private static bool IsTargetAvailable(
            string targetId,
            AgencyState agency,
            RivalSimulationContext context)
        {
            if (agency == null || string.IsNullOrEmpty(targetId))
            {
                return false;
            }

            ObjectiveFundingContract objectiveFundingContract = FindObjectiveFundingContract(
                targetId,
                context.ObjectiveFundingContracts);
            if (objectiveFundingContract != null)
            {
                ObjectiveDefinition objective = ObjectiveCatalogue.FindById(objectiveFundingContract.Id);
                return objective != null
                    && objectiveFundingContract.IsOffered
                    && !objectiveFundingContract.IsExpired
                    && !agency.HasCompletedObjective(objective.Id)
                    && !RivalLiveMissionSimulation.HasLiveContractTarget(agency, objective.Id)
                    && IsDestinationAccessible(agency, objective.CelestialBodyName);
            }

            SatelliteNetworkFundingContract networkContract = FindSatelliteNetworkFundingContract(
                targetId,
                context.SatelliteNetworkFundingContracts);
            return networkContract != null
                && !string.IsNullOrEmpty(networkContract.CelestialBodyName)
                && networkContract.IsAvailable
                && networkContract.IsOffered
                && IsDestinationAccessible(agency, networkContract.CelestialBodyName);
        }

        private static bool IsDestinationAccessible(AgencyState agency, string celestialBodyName)
        {
            int requiredTrackingStationLevel = GetRequiredTrackingStationLevel(celestialBodyName);
            return requiredTrackingStationLevel > 0
                && RivalDevelopmentSimulation.GetTrackingStationLevel(agency) >= requiredTrackingStationLevel;
        }

        private static int GetRequiredTrackingStationLevel(string celestialBodyName)
        {
            if (string.Equals(celestialBodyName, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(1, CampaignSettings.RivalTrackingStationKerbinLevel);
            }

            if (string.Equals(celestialBodyName, "Mun", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Minmus", StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(1, CampaignSettings.RivalTrackingStationKerbinMoonsLevel);
            }

            if (!string.IsNullOrEmpty(celestialBodyName)
                && CampaignSettings.GetRivalMissionLocationSettings("body:" + celestialBodyName) != null)
            {
                return Math.Max(1, CampaignSettings.RivalTrackingStationInterplanetaryLevel);
            }

            return 0;
        }

        private static string ChooseNextMissionTarget(
            AgencyState agency,
            RivalSimulationContext context)
        {
            var availableOneOffTargets = new List<string>();
            var availableSatelliteTargets = new List<string>();

            for (int contractIndex = 0;
                contractIndex < context.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract = context.ObjectiveFundingContracts[contractIndex];
                if (contract == null
                    || ObjectiveCatalogue.FindById(contract.Id) == null
                    || !IsTargetAvailable(contract.Id, agency, context))
                {
                    continue;
                }

                availableOneOffTargets.Add(contract.Id);
            }

            for (int contractIndex = 0;
                contractIndex < context.SatelliteNetworkFundingContracts.Count;
                contractIndex++)
            {
                SatelliteNetworkFundingContract contract = context.SatelliteNetworkFundingContracts[contractIndex];
                if (contract == null
                    || string.IsNullOrEmpty(contract.Id)
                    || string.IsNullOrEmpty(contract.CelestialBodyName)
                    || !IsTargetAvailable(contract.Id, agency, context))
                {
                    continue;
                }

                availableSatelliteTargets.Add(contract.Id);
            }

            if (availableOneOffTargets.Count == 0 && availableSatelliteTargets.Count == 0)
            {
                return null;
            }

            List<string> selectedTargetType;
            if (availableSatelliteTargets.Count == 0)
            {
                selectedTargetType = availableOneOffTargets;
            }
            else if (availableOneOffTargets.Count == 0)
            {
                selectedTargetType = availableSatelliteTargets;
            }
            else
            {
                selectedTargetType = context.Random.NextDouble() < SatelliteMissionSelectionChance
                    ? availableSatelliteTargets
                    : availableOneOffTargets;
            }

            return selectedTargetType[context.Random.Next(selectedTargetType.Count)];
        }

        private static void SelectNextNormalPreparation(
            AgencyState agency,
            double preparationStartUniversalTime,
            RivalSimulationContext context)
        {
            if (agency == null)
            {
                return;
            }

            string targetId = ChooseNextMissionTarget(agency, context);
            agency.NextMissionTargetId = targetId;
            agency.NextMissionDisplayName = GetMissionTargetDisplayName(
                targetId,
                context.ObjectiveFundingContracts,
                context.SatelliteNetworkFundingContracts);
            agency.MissionProgressPercent = 0;
            agency.NextMissionReadyUniversalTime = -1.0;
            agency.NextMissionProgressCheckUniversalTime = string.IsNullOrEmpty(targetId)
                ? 0.0
                : CalculateNextNormalProgressCheckUniversalTime(preparationStartUniversalTime);
        }

        private static void ClearNormalPreparation(AgencyState agency)
        {
            if (agency == null)
            {
                return;
            }

            agency.NextMissionTargetId = null;
            agency.NextMissionDisplayName = null;
            agency.MissionProgressPercent = 0;
            agency.NextMissionProgressCheckUniversalTime = 0.0;
            agency.NextMissionReadyUniversalTime = -1.0;
        }

        private static double CalculateNextNormalProgressCheckUniversalTime(double currentUniversalTime)
        {
            double intervalSeconds = GetNormalLaunchProgressIntervalSeconds();
            if (!IsFiniteNonNegative(currentUniversalTime) || intervalSeconds <= 0.0)
            {
                return 0.0;
            }

            double nextCheckUniversalTime =
                (Math.Floor(currentUniversalTime / intervalSeconds) + 1.0) * intervalSeconds;
            return IsFiniteNonNegative(nextCheckUniversalTime) ? nextCheckUniversalTime : 0.0;
        }

        private static double GetNormalLaunchProgressIntervalSeconds()
        {
            double intervalDays = CampaignSettings.RivalNormalLaunchProgressCheckIntervalDays;
            if (!IsFinite(intervalDays) || intervalDays <= 0.0)
            {
                return 0.0;
            }

            double intervalSeconds = intervalDays * KerbinDaySeconds;
            return IsFinite(intervalSeconds) && intervalSeconds > 0.0
                ? intervalSeconds
                : 0.0;
        }

        private static double CalculateMissionProgressCostForTarget(
            string targetId,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(targetId);
            if (objective != null)
            {
                if (objective.IsPreOrbitContract && objective.RivalProgressCostFunds > 0.0)
                {
                    return objective.RivalProgressCostFunds;
                }

                BodyBalanceSettings bodySettings = CampaignSettings.GetBodySettings(objective.CelestialBodyName);
                return objective.CrewRequirement == ObjectiveCrewRequirement.Crewed
                    ? bodySettings.CrewedProgressCostFunds
                    : bodySettings.ProbeProgressCostFunds;
            }

            SatelliteNetworkFundingContract networkContract = FindSatelliteNetworkFundingContract(
                targetId,
                satelliteNetworkFundingContracts);
            if (networkContract != null && !string.IsNullOrEmpty(networkContract.CelestialBodyName))
            {
                return CampaignSettings.GetBodySettings(networkContract.CelestialBodyName)
                    .SatelliteProgressCostFunds;
            }

            return CampaignSettings.Kerbin.ProbeProgressCostFunds;
        }

        private static ObjectiveFundingContract FindObjectiveFundingContract(
            string targetId,
            IList<ObjectiveFundingContract> objectiveFundingContracts)
        {
            if (string.IsNullOrEmpty(targetId) || objectiveFundingContracts == null)
            {
                return null;
            }

            for (int contractIndex = 0; contractIndex < objectiveFundingContracts.Count; contractIndex++)
            {
                ObjectiveFundingContract contract = objectiveFundingContracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    return contract;
                }
            }

            return null;
        }

        private static SatelliteNetworkFundingContract FindSatelliteNetworkFundingContract(
            string targetId,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            if (string.IsNullOrEmpty(targetId) || satelliteNetworkFundingContracts == null)
            {
                return null;
            }

            for (int contractIndex = 0;
                contractIndex < satelliteNetworkFundingContracts.Count;
                contractIndex++)
            {
                SatelliteNetworkFundingContract contract = satelliteNetworkFundingContracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    return contract;
                }
            }

            return null;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return IsFinite(value) && value >= 0.0;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}

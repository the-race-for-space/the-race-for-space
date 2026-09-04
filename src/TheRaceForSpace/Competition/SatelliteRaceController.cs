using System;
using System.Collections.Generic;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;
using TheRaceForSpace.Simulation;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.Competition
{
    /// <summary>
    /// Coordinates the current race prototype without owning the code-defined funding catalogue.
    /// </summary>
    public sealed class SatelliteRaceController
    {
        public const string PlayerProgramId = "player";
        public const string AsterProgramId = "aster";
        public const string CobaltProgramId = "cobalt";

        private const double KerbinDaySeconds = 21600.0;
        private const int KerbinDaysPerYear = 426;
        private const double RivalBaseIncomeFunds = 20000.0;
        private const int MaximumUncompletedNormalAchievementOffers = 2;
        private const int MaximumUnfulfilledSatelliteOffers = 2;

        private static readonly Random OfferRandomGenerator = new Random();

        private readonly List<SpaceProgramState> _programs = new List<SpaceProgramState>();
        private readonly List<SpaceProgramState> _rivalPrograms = new List<SpaceProgramState>();
        private readonly List<FundingProgramme> _fundingProgrammes = new List<FundingProgramme>();
        private readonly List<AchievementFundingProgramme> _achievementFundingProgrammes =
            new List<AchievementFundingProgramme>();
        private readonly IList<SpaceProgramState> _programsView;
        private readonly IList<SpaceProgramState> _rivalProgramsView;
        private readonly IList<FundingProgramme> _fundingProgrammesView;
        private readonly IList<AchievementFundingProgramme> _achievementFundingProgrammesView;
        private IList<MilestoneDefinition> _activeStarterContracts =
            new List<MilestoneDefinition>().AsReadOnly();
        private readonly double[,] _satellitePayoutCache;
        private readonly double[,] _achievementPayoutCache;
        private readonly double _fundingIntervalSeconds;
        private double _nextFundingUniversalTime = -1.0;
        private bool _hasFundingPayoutCache;
        private bool _hasRestoredPersistentState;
        private bool _isActiveStarterContractPlanDirty = true;

        public SatelliteRaceController()
        {
            _fundingIntervalSeconds = Math.Max(1.0, RaceSettings.FundingIntervalDays) * KerbinDaySeconds;
            PlayerProgram = new SpaceProgramState(PlayerProgramId, "Kerbal Space Agency", true);
            _programs.Add(PlayerProgram);

            // Rivals begin without a fixed target. Their first refresh chooses from the four
            // offered starter lines rather than assuming Probe Orbit is available at campaign start.
            int configuredRivalCount = Math.Max(0, RaceSettings.NumberOfRivals);
            for (int rivalIndex = 0; rivalIndex < configuredRivalCount; rivalIndex++)
            {
                string rivalId;
                string rivalName;
                if (rivalIndex == 0)
                {
                    rivalId = AsterProgramId;
                    rivalName = "Aster Aerospace Directorate";
                }
                else if (rivalIndex == 1)
                {
                    rivalId = CobaltProgramId;
                    rivalName = "Cobalt Orbital Bureau";
                }
                else
                {
                    int rivalNumber = rivalIndex + 1;
                    rivalId = "rival-" + rivalNumber;
                    rivalName = "Rival Agency " + rivalNumber;
                }

                var rivalProgram = new SpaceProgramState(rivalId, rivalName, false);
                rivalProgram.Funds = Math.Max(0.0, RaceSettings.RivalStartingFunds);

                if (rivalIndex == 0)
                {
                    AsterProgram = rivalProgram;
                }
                else if (rivalIndex == 1)
                {
                    CobaltProgram = rivalProgram;
                }

                _programs.Add(rivalProgram);
                _rivalPrograms.Add(rivalProgram);
            }

            // Target content remains code-defined, while the catalogue reads user-configured
            // reward and network balance values before constructing fresh campaign state.
            IList<AchievementFundingProgramme> achievementProgrammes =
                PrototypeFundingCatalogue.CreateAchievementProgrammes();
            for (int programmeIndex = 0; programmeIndex < achievementProgrammes.Count; programmeIndex++)
            {
                _achievementFundingProgrammes.Add(achievementProgrammes[programmeIndex]);
            }

            IList<FundingProgramme> fundingProgrammes =
                PrototypeFundingCatalogue.CreateSatelliteProgrammes();
            for (int programmeIndex = 0; programmeIndex < fundingProgrammes.Count; programmeIndex++)
            {
                _fundingProgrammes.Add(fundingProgrammes[programmeIndex]);
            }

            // Programme and program collections are fixed for one controller lifetime. Cache arrays
            // therefore need no recurring allocation and can be rebuilt in place after each refresh.
            _satellitePayoutCache = new double[_programs.Count, _fundingProgrammes.Count];
            _achievementPayoutCache = new double[_programs.Count, _achievementFundingProgrammes.Count];

            _programsView = _programs.AsReadOnly();
            _rivalProgramsView = _rivalPrograms.AsReadOnly();
            _fundingProgrammesView = _fundingProgrammes.AsReadOnly();
            _achievementFundingProgrammesView = _achievementFundingProgrammes.AsReadOnly();
        }

        public SpaceProgramState PlayerProgram { get; private set; }
        public SpaceProgramState AsterProgram { get; private set; }
        public SpaceProgramState CobaltProgram { get; private set; }
        public IList<SpaceProgramState> Programs { get { return _programsView; } }
        public IList<SpaceProgramState> RivalPrograms { get { return _rivalProgramsView; } }
        public IList<FundingProgramme> FundingProgrammes { get { return _fundingProgrammesView; } }
        public IList<AchievementFundingProgramme> AchievementFundingProgrammes
        {
            get { return _achievementFundingProgrammesView; }
        }

        /// <summary>
        /// Cached Offered, unexpired starter contracts the player has not personally completed.
        /// The controller replaces this read-only collection only after relevant contract state changes.
        /// </summary>
        internal IList<MilestoneDefinition> ActiveStarterContracts
        {
            get { return _activeStarterContracts; }
        }

        /// <summary>
        /// Marks the cached active starter set dirty after the flight tracker records a player
        /// starter achievement. The following controller refresh rebuilds the set once after all
        /// resulting unlock/offer/funding changes have been processed.
        /// </summary>
        internal void NotifyPlayerStarterAchievementRecorded()
        {
            _isActiveStarterContractPlanDirty = true;
        }

        /// <summary>
        /// Returns the program with the supplied stable ID, or null when no current program matches.
        /// </summary>
        public SpaceProgramState FindProgramById(string programId)
        {
            if (string.IsNullOrEmpty(programId))
            {
                return null;
            }

            for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
            {
                SpaceProgramState program = _programs[programIndex];
                if (program != null
                    && string.Equals(program.Id, programId, StringComparison.OrdinalIgnoreCase))
                {
                    return program;
                }
            }

            return null;
        }

        /// <summary>
        /// Guaranteed funds each rival receives on every shared funding date.
        /// </summary>
        public double RivalBaseIncomePerFundingPeriod { get { return RivalBaseIncomeFunds; } }

        public double NextFundingUniversalTime { get { return _nextFundingUniversalTime; } }

        public int NextFundingYear
        {
            get { return GetKerbinYear(_nextFundingUniversalTime); }
        }

        public int NextFundingDay
        {
            get { return GetKerbinDay(_nextFundingUniversalTime); }
        }

        /// <summary>
        /// Whole Kerbin days remaining until the shared funding date, rounded upward so a
        /// partial final day remains visible until the funding boundary is actually reached.
        /// </summary>
        public int DaysUntilNextFunding
        {
            get
            {
                if (_nextFundingUniversalTime < 0.0 || Planetarium.fetch == null)
                {
                    return 0;
                }

                double remainingSeconds = Math.Max(
                    0.0,
                    _nextFundingUniversalTime - Planetarium.GetUniversalTime());
                return (int)Math.Ceiling(remainingSeconds / KerbinDaySeconds);
            }
        }

        public int GetKerbinYear(double universalTime)
        {
            if (universalTime < 0.0)
            {
                return 0;
            }

            int totalKerbinDays = (int)Math.Floor(universalTime / KerbinDaySeconds);
            return (totalKerbinDays / KerbinDaysPerYear) + 1;
        }

        public int GetKerbinDay(double universalTime)
        {
            if (universalTime < 0.0)
            {
                return 0;
            }

            int totalKerbinDays = (int)Math.Floor(universalTime / KerbinDaySeconds);
            return (totalKerbinDays % KerbinDaysPerYear) + 1;
        }

        /// <summary>
        /// Returns the funds required for the rival's next successful mission-progress step.
        /// </summary>
        public double GetRivalLaunchProgressCost(SpaceProgramState program)
        {
            if (program == null || program.IsPlayer)
            {
                return 0.0;
            }

            return RivalSimulation.CalculateLaunchProgressCost(
                program,
                _achievementFundingProgrammes,
                _fundingProgrammes);
        }

        /// <summary>
        /// Returns the current expected Kerbin days until a rival mission completes, using the
        /// same configured funding date shown in the command center for projected income.
        /// </summary>
        public int? GetEstimatedRivalLaunchDays(SpaceProgramState program)
        {
            if (program == null || program.IsPlayer || Planetarium.fetch == null)
            {
                return null;
            }

            return RivalSimulation.CalculateEstimatedLaunchDays(
                program,
                Planetarium.GetUniversalTime(),
                _nextFundingUniversalTime,
                _fundingIntervalSeconds,
                _achievementFundingProgrammes,
                _fundingProgrammes);
        }

        public bool HasProgramAchieved(
            SpaceProgramState program,
            AchievementFundingProgramme achievementProgramme)
        {
            return program != null
                && achievementProgramme != null
                && program.HasAchievement(achievementProgramme.Id);
        }

        /// <summary>
        /// Returns whether an achievement funding target is unlocked at the current campaign time.
        /// Unlock state is intentionally separate from whether sponsors have offered the contract.
        /// </summary>
        public bool IsAchievementProgrammeAvailable(AchievementFundingProgramme achievementProgramme)
        {
            if (achievementProgramme == null)
            {
                return false;
            }

            double evaluationUniversalTime = Planetarium.fetch == null
                ? 0.0
                : Planetarium.GetUniversalTime();
            return IsAchievementProgrammeAvailableAtTime(
                achievementProgramme,
                evaluationUniversalTime);
        }

        public int GetAchievementAgencyCount(AchievementFundingProgramme achievementProgramme)
        {
            if (achievementProgramme == null)
            {
                return 0;
            }

            return GetAchievementAgencyCountAtTime(
                achievementProgramme.Id,
                double.PositiveInfinity);
        }

        /// <summary>
        /// Returns this agency's projected share of one satellite contract. After the first
        /// completed controller refresh, the value comes from that refresh's payout snapshot so
        /// repeated UI queries do not re-scan every agency's satellite count.
        /// </summary>
        public double GetSatelliteCurrentPayout(
            SpaceProgramState program,
            FundingProgramme fundingProgramme)
        {
            if (program == null
                || fundingProgramme == null
                || !fundingProgramme.IsAvailable
                || !fundingProgramme.IsOffered)
            {
                return 0.0;
            }

            if (_hasFundingPayoutCache)
            {
                int programIndex = _programs.IndexOf(program);
                int programmeIndex = _fundingProgrammes.IndexOf(fundingProgramme);
                if (programIndex >= 0 && programmeIndex >= 0)
                {
                    return _satellitePayoutCache[programIndex, programmeIndex];
                }
            }

            return CalculateSatelliteCurrentPayout(program, fundingProgramme);
        }

        /// <summary>
        /// Returns this agency's projected share of the achievement contract on the next global
        /// funding date. After a completed refresh the value is served from the same payout snapshot
        /// used to build NextPayoutFunds, avoiding repeated agency-count calculations in the UI.
        /// </summary>
        public double GetAchievementCurrentPayout(
            SpaceProgramState program,
            AchievementFundingProgramme achievementProgramme)
        {
            if (program == null || achievementProgramme == null || !achievementProgramme.IsOffered)
            {
                return 0.0;
            }

            if (_hasFundingPayoutCache)
            {
                int programIndex = _programs.IndexOf(program);
                int programmeIndex = _achievementFundingProgrammes.IndexOf(achievementProgramme);
                if (programIndex >= 0 && programmeIndex >= 0)
                {
                    return _achievementPayoutCache[programIndex, programmeIndex];
                }
            }

            return CalculateAchievementCurrentPayout(program, achievementProgramme);
        }

        public void Refresh()
        {
            Refresh(true);
        }

        internal bool Refresh(bool refreshPlayerVessels)
        {
            if (Planetarium.fetch == null)
            {
                return false;
            }

            double currentUniversalTime = Planetarium.GetUniversalTime();

            // Do not advance gameplay until the ScenarioModule has loaded the current save.
            // A new game has no saved Race for Space data, so the constructor defaults remain in use.
            if (!_hasRestoredPersistentState)
            {
                bool restoredRivals = RacePersistenceScenario.TryRestoreRivalState(_rivalPrograms);
                double restoredNextFundingUniversalTime;
                bool restoredRaceProgress = RacePersistenceScenario.TryRestoreRaceProgress(
                    PlayerProgram,
                    _fundingProgrammes,
                    _achievementFundingProgrammes,
                    out restoredNextFundingUniversalTime);

                if (!restoredRivals || !restoredRaceProgress)
                {
                    return false;
                }

                if (restoredNextFundingUniversalTime >= 0.0)
                {
                    _nextFundingUniversalTime = restoredNextFundingUniversalTime;
                }

                _hasRestoredPersistentState = true;
            }

            if (_nextFundingUniversalTime < 0.0)
            {
                _nextFundingUniversalTime =
                    (Math.Floor(currentUniversalTime / _fundingIntervalSeconds) + 1.0)
                    * _fundingIntervalSeconds;
            }

            bool hasDueFunding = currentUniversalTime >= _nextFundingUniversalTime;

            if (hasDueFunding)
            {
                ProcessDueFunding(currentUniversalTime);
                RefreshRivals(currentUniversalTime);
                UpdateFundingAvailability(currentUniversalTime);
            }

            double stateEvaluationUniversalTime = currentUniversalTime;
            bool didRefreshPlayerVessels = false;
            if (refreshPlayerVessels)
            {
                IList<VesselTrackingSnapshot> vesselSnapshots;
                double vesselObservationUniversalTime;
                if (KspVesselDiscovery.TryCaptureOrbitingVessels(
                    out vesselSnapshots,
                    out vesselObservationUniversalTime))
                {
                    didRefreshPlayerVessels = true;
                    stateEvaluationUniversalTime = Math.Max(
                        stateEvaluationUniversalTime,
                        vesselObservationUniversalTime);
                    SatelliteTracker.RefreshPlayerSatelliteCounts(
                        PlayerProgram,
                        _programs,
                        PrototypeMilestones.All,
                        vesselSnapshots,
                        vesselObservationUniversalTime);
                }
            }

            UpdateFundingAvailability(stateEvaluationUniversalTime);

            if (!hasDueFunding)
            {
                RefreshRivals(stateEvaluationUniversalTime);
                UpdateFundingAvailability(stateEvaluationUniversalTime);
            }

            // Probe Orbit remains an immediate shared-race unlock once any starter line reaches
            // Level V. Starter Levels II-V wait for the funding-day review, where every unlocked
            // starter contract is offered without consuming normal one-off achievement slots.
            UpdateSpecialAchievementOffers(stateEvaluationUniversalTime);
            UpdateSatelliteTargetReachedState();
            StartAchievementContracts(stateEvaluationUniversalTime);
            RebuildActiveStarterContractPlanIfNeeded();
            EvaluateFundingProgrammes();

            RacePersistenceScenario.CaptureRivalState(_rivalPrograms);
            RacePersistenceScenario.CaptureRaceProgress(
                PlayerProgram,
                _fundingProgrammes,
                _achievementFundingProgrammes,
                _nextFundingUniversalTime);

            return didRefreshPlayerVessels;
        }

        private void RefreshRivals(double currentUniversalTime)
        {
            RivalSimulation.Refresh(
                _programs,
                currentUniversalTime,
                _achievementFundingProgrammes,
                _fundingProgrammes);
        }

        private void UpdateFundingAvailability(double evaluationUniversalTime)
        {
            for (int programmeIndex = 0; programmeIndex < _fundingProgrammes.Count; programmeIndex++)
            {
                FundingProgramme programme = _fundingProgrammes[programmeIndex];
                if (programme.IsAvailable)
                {
                    continue;
                }

                if (UnlockRuleEvaluator.IsSatisfied(
                    programme.UnlockRule,
                    _programs,
                    evaluationUniversalTime))
                {
                    programme.Unlock();
                }
            }
        }

        /// <summary>
        /// Offers Probe Orbit immediately when any agency completes a Level V starter contract.
        /// Starter Levels II-V otherwise wait in Unlocked until the funding-day sponsor review,
        /// where every unlocked starter contract is offered independently of the normal offer cap.
        /// </summary>
        private void UpdateSpecialAchievementOffers(double evaluationUniversalTime)
        {
            for (int programmeIndex = 0;
                programmeIndex < _achievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];
                if (programme == null || programme.IsOffered || programme.IsExpired)
                {
                    continue;
                }

                if (!string.Equals(
                    programme.Id,
                    PrototypeMilestones.ProbeOrbitId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (IsAchievementProgrammeAvailableAtTime(programme, evaluationUniversalTime))
                {
                    programme.Offer();
                }
            }
        }

        private void UpdateSatelliteTargetReachedState()
        {
            for (int programmeIndex = 0; programmeIndex < _fundingProgrammes.Count; programmeIndex++)
            {
                FundingProgramme programme = _fundingProgrammes[programmeIndex];
                if (programme == null
                    || programme.HasReachedSatelliteTarget
                    || programme.RequiredSatellites <= 0)
                {
                    continue;
                }

                if (GetCollectiveSatelliteCount(programme.CelestialBodyName) >= programme.RequiredSatellites)
                {
                    programme.MarkSatelliteTargetReached();
                }
            }
        }

        private void StartAchievementContracts(double evaluationUniversalTime)
        {
            for (int programmeIndex = 0; programmeIndex < _achievementFundingProgrammes.Count; programmeIndex++)
            {
                AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];
                if (programme.IsOffered
                    && !programme.HasStarted
                    && !programme.IsExpired
                    && GetAchievementAgencyCountAtTime(programme.Id, evaluationUniversalTime) > 0)
                {
                    programme.Start();
                }
            }
        }

        /// <summary>
        /// Processes every crossed global funding boundary. Rivals are advanced only to each
        /// boundary before that boundary pays, so they cannot spend funding before receiving it.
        /// Existing offers pay first; the sponsor review then fills vacancies for the next period.
        /// </summary>
        private void ProcessDueFunding(double currentUniversalTime)
        {
            while (_nextFundingUniversalTime >= 0.0 && currentUniversalTime >= _nextFundingUniversalTime)
            {
                double payoutUniversalTime = _nextFundingUniversalTime;

                RefreshRivals(payoutUniversalTime);
                UpdateFundingAvailability(payoutUniversalTime);
                UpdateSpecialAchievementOffers(payoutUniversalTime);
                UpdateSatelliteTargetReachedState();
                StartAchievementContracts(payoutUniversalTime);

                for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                {
                    SpaceProgramState program = _programs[programIndex];
                    double payout = CalculateSatelliteFundingForProgram(program);

                    if (!program.IsPlayer)
                    {
                        payout += RivalBaseIncomeFunds;
                    }

                    AwardProgramFunds(program, payout);
                }

                for (int programmeIndex = 0; programmeIndex < _achievementFundingProgrammes.Count; programmeIndex++)
                {
                    AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];
                    if (!programme.IsOffered || !programme.HasStarted || programme.IsExpired)
                    {
                        continue;
                    }

                    int eligibleAgencyCount = GetAchievementAgencyCountAtTime(
                        programme.Id,
                        payoutUniversalTime);
                    if (eligibleAgencyCount <= 0)
                    {
                        continue;
                    }

                    for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                    {
                        SpaceProgramState program = _programs[programIndex];
                        bool isEligible = HasProgramAchievedByTime(
                            program,
                            programme.Id,
                            payoutUniversalTime);
                        double payout = programme.CalculateCurrentPayout(isEligible, eligibleAgencyCount);
                        AwardProgramFunds(program, payout);
                    }

                    programme.AdvancePayout();
                    if (programme.IsExpired)
                    {
                        MilestoneDefinition expiredMilestone = PrototypeMilestones.FindById(programme.Id);
                        if (expiredMilestone != null && expiredMilestone.IsStarterContract)
                        {
                            _isActiveStarterContractPlanDirty = true;
                        }
                    }
                }

                ReviewFundingOffers(payoutUniversalTime);
                _nextFundingUniversalTime += _fundingIntervalSeconds;
            }
        }

        private void ReviewFundingOffers(double evaluationUniversalTime)
        {
            UpdateSpecialAchievementOffers(evaluationUniversalTime);

            var normalAchievementCandidates = new List<AchievementFundingProgramme>();
            int uncompletedNormalAchievementOfferCount = 0;

            for (int programmeIndex = 0;
                programmeIndex < _achievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];
                if (programme == null || programme.IsExpired)
                {
                    continue;
                }

                MilestoneDefinition milestone = PrototypeMilestones.FindById(programme.Id);
                bool isStarterContract = milestone != null && milestone.IsStarterContract;

                if (programme.IsOffered)
                {
                    if (!programme.HasStarted && !isStarterContract)
                    {
                        uncompletedNormalAchievementOfferCount++;
                    }
                    continue;
                }

                if (!IsAchievementProgrammeAvailableAtTime(programme, evaluationUniversalTime))
                {
                    continue;
                }

                // Starter contracts are the four parallel pre-orbit development lines. Every
                // unlocked starter target is offered at the funding review, and starter offers do
                // not consume the two unfinished slots reserved for Probe Orbit and later targets.
                if (isStarterContract)
                {
                    programme.Offer();
                    _isActiveStarterContractPlanDirty = true;
                    continue;
                }

                normalAchievementCandidates.Add(programme);
            }

            int achievementVacancies = Math.Max(
                0,
                MaximumUncompletedNormalAchievementOffers - uncompletedNormalAchievementOfferCount);
            for (int offerIndex = 0;
                offerIndex < achievementVacancies && normalAchievementCandidates.Count > 0;
                offerIndex++)
            {
                int candidateIndex = OfferRandomGenerator.Next(normalAchievementCandidates.Count);
                normalAchievementCandidates[candidateIndex].Offer();
                normalAchievementCandidates.RemoveAt(candidateIndex);
            }

            var satelliteCandidates = new List<FundingProgramme>();
            int unfulfilledSatelliteOfferCount = 0;

            for (int programmeIndex = 0; programmeIndex < _fundingProgrammes.Count; programmeIndex++)
            {
                FundingProgramme programme = _fundingProgrammes[programmeIndex];
                if (programme == null)
                {
                    continue;
                }

                if (programme.IsOffered)
                {
                    if (!programme.HasReachedSatelliteTarget)
                    {
                        unfulfilledSatelliteOfferCount++;
                    }
                    continue;
                }

                if (programme.IsAvailable)
                {
                    satelliteCandidates.Add(programme);
                }
            }

            int satelliteVacancies = Math.Max(
                0,
                MaximumUnfulfilledSatelliteOffers - unfulfilledSatelliteOfferCount);
            for (int offerIndex = 0;
                offerIndex < satelliteVacancies && satelliteCandidates.Count > 0;
                offerIndex++)
            {
                int candidateIndex = OfferRandomGenerator.Next(satelliteCandidates.Count);
                satelliteCandidates[candidateIndex].Offer();
                satelliteCandidates.RemoveAt(candidateIndex);
            }
        }

        private void RebuildActiveStarterContractPlanIfNeeded()
        {
            if (!_isActiveStarterContractPlanDirty)
            {
                return;
            }

            var activeStarterContracts = new List<MilestoneDefinition>();
            for (int programmeIndex = 0;
                programmeIndex < _achievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];
                if (programme == null
                    || !programme.IsOffered
                    || programme.IsExpired
                    || PlayerProgram.HasAchievement(programme.Id))
                {
                    continue;
                }

                MilestoneDefinition milestone = PrototypeMilestones.FindById(programme.Id);
                if (milestone != null && milestone.IsStarterContract)
                {
                    activeStarterContracts.Add(milestone);
                }
            }

            // Replacing the read-only list rather than mutating it lets frequent readers keep a
            // stable snapshot until a real offer/completion/expiry transition invalidates the plan.
            _activeStarterContracts = activeStarterContracts.AsReadOnly();
            _isActiveStarterContractPlanDirty = false;
        }

        private void AwardProgramFunds(SpaceProgramState program, double payout)
        {
            if (program == null || payout <= 0.0)
            {
                return;
            }

            if (program.IsPlayer)
            {
                CareerFundingAdapter.TryAddFunds(payout);
                return;
            }

            program.Funds += payout;
        }

        private void EvaluateFundingProgrammes()
        {
            _hasFundingPayoutCache = false;
            Array.Clear(_satellitePayoutCache, 0, _satellitePayoutCache.Length);
            Array.Clear(_achievementPayoutCache, 0, _achievementPayoutCache.Length);

            for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
            {
                SpaceProgramState program = _programs[programIndex];
                double nextPayoutFunds = 0.0;

                for (int programmeIndex = 0; programmeIndex < _fundingProgrammes.Count; programmeIndex++)
                {
                    double payout = CalculateSatelliteCurrentPayout(
                        program,
                        _fundingProgrammes[programmeIndex]);
                    _satellitePayoutCache[programIndex, programmeIndex] = payout;
                    nextPayoutFunds += payout;
                }

                program.NextPayoutFunds = nextPayoutFunds;

                if (!program.IsPlayer)
                {
                    program.NextPayoutFunds += RivalBaseIncomeFunds;
                }
            }

            if (_nextFundingUniversalTime >= 0.0)
            {
                for (int programmeIndex = 0;
                    programmeIndex < _achievementFundingProgrammes.Count;
                    programmeIndex++)
                {
                    AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];
                    if (!programme.IsOffered || !programme.HasStarted || programme.IsExpired)
                    {
                        continue;
                    }

                    int eligibleAgencyCount = GetAchievementAgencyCountAtTime(
                        programme.Id,
                        _nextFundingUniversalTime);

                    for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                    {
                        SpaceProgramState program = _programs[programIndex];
                        double payout = programme.CalculateCurrentPayout(
                            HasProgramAchievedByTime(
                                program,
                                programme.Id,
                                _nextFundingUniversalTime),
                            eligibleAgencyCount);
                        _achievementPayoutCache[programIndex, programmeIndex] = payout;
                        program.NextPayoutFunds += payout;
                    }
                }
            }

            _hasFundingPayoutCache = true;
        }

        private double CalculateSatelliteFundingForProgram(SpaceProgramState program)
        {
            if (program == null)
            {
                return 0.0;
            }

            double payout = 0.0;

            for (int programmeIndex = 0; programmeIndex < _fundingProgrammes.Count; programmeIndex++)
            {
                payout += CalculateSatelliteCurrentPayout(program, _fundingProgrammes[programmeIndex]);
            }

            return payout;
        }

        private double CalculateSatelliteCurrentPayout(
            SpaceProgramState program,
            FundingProgramme fundingProgramme)
        {
            if (program == null
                || fundingProgramme == null
                || !fundingProgramme.IsAvailable
                || !fundingProgramme.IsOffered)
            {
                return 0.0;
            }

            int totalSatelliteCount = GetCollectiveSatelliteCount(fundingProgramme.CelestialBodyName);

            return fundingProgramme.CalculateCurrentPayout(
                program.GetSatelliteCount(fundingProgramme.CelestialBodyName),
                totalSatelliteCount);
        }

        private int GetCollectiveSatelliteCount(string celestialBodyName)
        {
            if (string.IsNullOrEmpty(celestialBodyName))
            {
                return 0;
            }

            int totalSatelliteCount = 0;
            for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
            {
                totalSatelliteCount += _programs[programIndex].GetSatelliteCount(celestialBodyName);
            }

            return totalSatelliteCount;
        }

        private double CalculateAchievementCurrentPayout(
            SpaceProgramState program,
            AchievementFundingProgramme achievementProgramme)
        {
            if (program == null
                || achievementProgramme == null
                || !achievementProgramme.IsOffered
                || _nextFundingUniversalTime < 0.0)
            {
                return 0.0;
            }

            int eligibleAgencyCount = GetAchievementAgencyCountAtTime(
                achievementProgramme.Id,
                _nextFundingUniversalTime);

            return achievementProgramme.CalculateCurrentPayout(
                HasProgramAchievedByTime(
                    program,
                    achievementProgramme.Id,
                    _nextFundingUniversalTime),
                eligibleAgencyCount);
        }

        private bool IsAchievementProgrammeAvailableAtTime(
            AchievementFundingProgramme achievementProgramme,
            double evaluationUniversalTime)
        {
            return achievementProgramme != null
                && UnlockRuleEvaluator.IsSatisfied(
                    achievementProgramme.UnlockRule,
                    _programs,
                    evaluationUniversalTime);
        }

        private int GetAchievementAgencyCountAtTime(
            string milestoneId,
            double payoutUniversalTime)
        {
            if (string.IsNullOrEmpty(milestoneId))
            {
                return 0;
            }

            int achievedAgencyCount = 0;
            for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
            {
                if (HasProgramAchievedByTime(
                    _programs[programIndex],
                    milestoneId,
                    payoutUniversalTime))
                {
                    achievedAgencyCount++;
                }
            }

            return achievedAgencyCount;
        }

        private bool HasProgramAchievedByTime(
            SpaceProgramState program,
            string milestoneId,
            double payoutUniversalTime)
        {
            if (program == null || string.IsNullOrEmpty(milestoneId))
            {
                return false;
            }

            double achievementUniversalTime = program.GetAchievementUniversalTime(milestoneId);
            return achievementUniversalTime >= 0.0 && achievementUniversalTime <= payoutUniversalTime;
        }
    }
}

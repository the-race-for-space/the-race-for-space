using System;
using System.Collections.Generic;
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
        private const double FundingIntervalSeconds = 90.0 * KerbinDaySeconds;
        private const double RivalStartingFunds = 200000.0;
        private const double RivalBaseIncomeFunds = 20000.0;

        private readonly List<SpaceProgramState> _programs = new List<SpaceProgramState>();
        private readonly List<SpaceProgramState> _rivalPrograms = new List<SpaceProgramState>();
        private readonly List<FundingProgramme> _fundingProgrammes = new List<FundingProgramme>();
        private readonly List<AchievementFundingProgramme> _achievementFundingProgrammes =
            new List<AchievementFundingProgramme>();
        private readonly IList<SpaceProgramState> _programsView;
        private readonly IList<SpaceProgramState> _rivalProgramsView;
        private readonly IList<FundingProgramme> _fundingProgrammesView;
        private readonly IList<AchievementFundingProgramme> _achievementFundingProgrammesView;
        private readonly double[,] _satellitePayoutCache;
        private readonly double[,] _achievementPayoutCache;
        private double _nextFundingUniversalTime = -1.0;
        private bool _hasFundingPayoutCache;
        private bool _hasRestoredPersistentState;

        public SatelliteRaceController()
        {
            PlayerProgram = new SpaceProgramState(PlayerProgramId, "Kerbal Space Agency", true);
            AsterProgram = new SpaceProgramState(AsterProgramId, "Aster Aerospace Directorate", false);
            CobaltProgram = new SpaceProgramState(CobaltProgramId, "Cobalt Orbital Bureau", false);

            // New games begin with enough simulated cash for one complete Probe Orbit
            // development cycle. Probe Orbit is deliberately the fixed opening rival mission.
            AsterProgram.Funds = RivalStartingFunds;
            CobaltProgram.Funds = RivalStartingFunds;
            AsterProgram.NextMissionTargetId = PrototypeMilestones.ProbeOrbitId;
            CobaltProgram.NextMissionTargetId = PrototypeMilestones.ProbeOrbitId;

            MilestoneDefinition openingMilestone = PrototypeMilestones.FindById(
                PrototypeMilestones.ProbeOrbitId);
            string openingMissionName = openingMilestone == null ? null : openingMilestone.Name;
            AsterProgram.NextLaunchBodyName = openingMissionName;
            CobaltProgram.NextLaunchBodyName = openingMissionName;

            _programs.Add(PlayerProgram);
            _programs.Add(AsterProgram);
            _programs.Add(CobaltProgram);
            _rivalPrograms.Add(AsterProgram);
            _rivalPrograms.Add(CobaltProgram);

            // Funding content is code-defined for 0.4, but the catalogue owns the complete target
            // set. The controller only consumes collections, so adding another target does not
            // require another constructor branch here.
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
        /// Guaranteed funds each rival receives on every shared 90-day funding date.
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
        /// Returns the funds required for the rival's next successful 10% mission-progress step.
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
        /// same shared 90-day funding date shown in the command center for projected income.
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
                FundingIntervalSeconds,
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
            if (program == null || fundingProgramme == null || !fundingProgramme.IsAvailable)
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
            if (program == null || achievementProgramme == null)
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
            if (Planetarium.fetch == null)
            {
                return;
            }

            double currentUniversalTime = Planetarium.GetUniversalTime();

            // Do not advance gameplay until the ScenarioModule has loaded the current save.
            // A new game has no saved Race for Space data, so the constructor defaults remain in use.
            if (!_hasRestoredPersistentState)
            {
                bool restoredRivals = RacePersistenceScenario.TryRestoreRivalState(_rivalPrograms);
                bool restoredRaceProgress = RacePersistenceScenario.TryRestoreRaceProgress(
                    PlayerProgram,
                    _fundingProgrammes,
                    _achievementFundingProgrammes);

                if (!restoredRivals || !restoredRaceProgress)
                {
                    return;
                }

                _hasRestoredPersistentState = true;
            }

            if (_nextFundingUniversalTime < 0.0)
            {
                // Every contract uses the same 90-day Kerbin calendar boundary. Orbit
                // achievements change eligibility and interest but never create their own date.
                _nextFundingUniversalTime =
                    (Math.Floor(currentUniversalTime / FundingIntervalSeconds) + 1.0)
                    * FundingIntervalSeconds;
            }

            bool hasDueFunding = currentUniversalTime >= _nextFundingUniversalTime;

            if (hasDueFunding)
            {
                // Replay crossed funding boundaries before observing new player vessel state.
                // KSP exposes current vessel state but not the exact time each vessel entered
                // orbit, so using the last observed player counts avoids retroactively paying
                // newly detected satellites at historical funding dates.
                ProcessDueFunding(currentUniversalTime);
                RefreshRivals(currentUniversalTime);
                UpdateFundingAvailability(currentUniversalTime);
            }

            double stateEvaluationUniversalTime = currentUniversalTime;
            IList<VesselTrackingSnapshot> vesselSnapshots;
            double vesselObservationUniversalTime;
            if (KspVesselDiscovery.TryCaptureOrbitingVessels(
                out vesselSnapshots,
                out vesselObservationUniversalTime))
            {
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

            UpdateFundingAvailability(stateEvaluationUniversalTime);

            if (!hasDueFunding)
            {
                // Without a crossed funding boundary, keep the normal immediate-response path:
                // current player achievements can influence rival target selection this refresh.
                RefreshRivals(stateEvaluationUniversalTime);
                UpdateFundingAvailability(stateEvaluationUniversalTime);
            }

            StartAchievementContracts(stateEvaluationUniversalTime);
            EvaluateFundingProgrammes();

            RacePersistenceScenario.CaptureRivalState(_rivalPrograms);
            RacePersistenceScenario.CaptureRaceProgress(
                PlayerProgram,
                _fundingProgrammes,
                _achievementFundingProgrammes);
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

        private void StartAchievementContracts(double evaluationUniversalTime)
        {
            for (int programmeIndex = 0; programmeIndex < _achievementFundingProgrammes.Count; programmeIndex++)
            {
                AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];
                if (IsAchievementProgrammeAvailableAtTime(programme, evaluationUniversalTime)
                    && !programme.HasStarted
                    && GetAchievementAgencyCountAtTime(programme.Id, evaluationUniversalTime) > 0)
                {
                    programme.Start();
                }
            }
        }

        /// <summary>
        /// Processes every crossed global 90-day funding boundary. Rivals are advanced only to
        /// each boundary before that boundary pays, so they cannot spend funding before receiving
        /// it and their satellite/achievement state is evaluated at the correct historical time.
        /// Satellite and achievement programmes then pay on the same shared date.
        /// </summary>
        private void ProcessDueFunding(double currentUniversalTime)
        {
            while (_nextFundingUniversalTime >= 0.0 && currentUniversalTime >= _nextFundingUniversalTime)
            {
                double payoutUniversalTime = _nextFundingUniversalTime;

                RefreshRivals(payoutUniversalTime);
                UpdateFundingAvailability(payoutUniversalTime);
                StartAchievementContracts(payoutUniversalTime);

                for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                {
                    SpaceProgramState program = _programs[programIndex];
                    double payout = CalculateSatelliteFundingForProgram(program);

                    // Rival agencies always retain a minimal national/base budget even when
                    // they currently qualify for no competitive contract funding.
                    if (!program.IsPlayer)
                    {
                        payout += RivalBaseIncomeFunds;
                    }

                    AwardProgramFunds(program, payout);
                }

                for (int programmeIndex = 0; programmeIndex < _achievementFundingProgrammes.Count; programmeIndex++)
                {
                    AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];
                    if (!IsAchievementProgrammeAvailableAtTime(programme, payoutUniversalTime)
                        || !programme.HasStarted
                        || programme.IsExpired)
                    {
                        continue;
                    }

                    int eligibleAgencyCount = GetAchievementAgencyCountAtTime(
                        programme.Id,
                        payoutUniversalTime);
                    if (eligibleAgencyCount <= 0)
                    {
                        // The first achievement occurred after this historical funding boundary,
                        // so this contract must wait for the next global date at the same 100% stage.
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
                }

                _nextFundingUniversalTime += FundingIntervalSeconds;
            }
        }

        private void AwardProgramFunds(SpaceProgramState program, double payout)
        {
            if (program == null || payout <= 0.0)
            {
                return;
            }

            if (program.IsPlayer)
            {
                // The player receives real KSP Career funds. In non-Career modes the
                // adapter deliberately leaves KSP's economy untouched.
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

                // Show the guaranteed rival base income in projected funding so the displayed
                // next payout and rival launch ETA use the same value that will actually be paid.
                if (!program.IsPlayer)
                {
                    program.NextPayoutFunds += RivalBaseIncomeFunds;
                }
            }

            if (_nextFundingUniversalTime >= 0.0)
            {
                // All projected amounts below are due on the same next funding date. Achievement
                // eligibility and unlock rules are evaluated against that date, matching payment.
                for (int programmeIndex = 0;
                    programmeIndex < _achievementFundingProgrammes.Count;
                    programmeIndex++)
                {
                    AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];
                    if (!IsAchievementProgrammeAvailableAtTime(
                            programme,
                            _nextFundingUniversalTime)
                        || !programme.HasStarted
                        || programme.IsExpired)
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

            // UI can draw several times per rendered frame. Publish the complete cache only after
            // every programme has been evaluated so presentation never observes a partial rebuild.
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
                // Historical funding replay must use state at that boundary rather than the cached
                // projection from the previous completed refresh.
                payout += CalculateSatelliteCurrentPayout(program, _fundingProgrammes[programmeIndex]);
            }

            return payout;
        }

        private double CalculateSatelliteCurrentPayout(
            SpaceProgramState program,
            FundingProgramme fundingProgramme)
        {
            if (program == null || fundingProgramme == null || !fundingProgramme.IsAvailable)
            {
                return 0.0;
            }

            int totalSatelliteCount = 0;
            for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
            {
                totalSatelliteCount += _programs[programIndex].GetSatelliteCount(
                    fundingProgramme.CelestialBodyName);
            }

            return fundingProgramme.CalculateCurrentPayout(
                program.GetSatelliteCount(fundingProgramme.CelestialBodyName),
                totalSatelliteCount);
        }

        private double CalculateAchievementCurrentPayout(
            SpaceProgramState program,
            AchievementFundingProgramme achievementProgramme)
        {
            if (program == null
                || achievementProgramme == null
                || _nextFundingUniversalTime < 0.0
                || !IsAchievementProgrammeAvailableAtTime(
                    achievementProgramme,
                    _nextFundingUniversalTime))
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

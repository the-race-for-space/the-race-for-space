using System;
using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Programs;
using TheRaceForSpace.Simulation;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.Competition
{
    /// <summary>
    /// Coordinates the narrow 0.3 race prototype without introducing a general mission framework.
    /// </summary>
    public sealed class SatelliteRaceController
    {
        private const double KerbinDaySeconds = 21600.0;
        private const int KerbinDaysPerYear = 426;
        private const double FundingIntervalSeconds = 90.0 * KerbinDaySeconds;
        private const double RivalStartingFunds = 200000.0;
        private const double RivalBaseIncomeFunds = 20000.0;
        private const string ProbeOrbitProgrammeId = "probe-orbit";
        private const string CrewedOrbitProgrammeId = "crewed-orbit";
        private const string MunProbeOrbitProgrammeId = "mun-probe-orbit";
        private const string MinmusProbeOrbitProgrammeId = "minmus-probe-orbit";
        private const string MunCrewedOrbitProgrammeId = "mun-crewed-orbit";
        private const string MinmusCrewedOrbitProgrammeId = "minmus-crewed-orbit";

        private readonly List<SpaceProgramState> _programs = new List<SpaceProgramState>();
        private readonly List<FundingProgramme> _fundingProgrammes = new List<FundingProgramme>();
        private readonly List<AchievementFundingProgramme> _achievementFundingProgrammes =
            new List<AchievementFundingProgramme>();
        private readonly IList<FundingProgramme> _fundingProgrammesView;
        private readonly IList<AchievementFundingProgramme> _achievementFundingProgrammesView;
        private double _nextFundingUniversalTime = -1.0;
        private bool _hasRestoredPersistentState;

        public SatelliteRaceController()
        {
            PlayerProgram = new SpaceProgramState("Kerbal Space Agency", true);
            AsterProgram = new SpaceProgramState("Aster Aerospace Directorate", false);
            CobaltProgram = new SpaceProgramState("Cobalt Orbital Bureau", false);

            // New games begin with enough simulated cash for one complete Probe Orbit
            // development cycle. Probe Orbit is deliberately the fixed opening rival mission.
            AsterProgram.Funds = RivalStartingFunds;
            CobaltProgram.Funds = RivalStartingFunds;
            AsterProgram.NextLaunchBodyName = RivalSimulation.ProbeOrbitTargetName;
            CobaltProgram.NextLaunchBodyName = RivalSimulation.ProbeOrbitTargetName;

            _programs.Add(PlayerProgram);
            _programs.Add(AsterProgram);
            _programs.Add(CobaltProgram);

            ProbeOrbitProgramme = new AchievementFundingProgramme(
                ProbeOrbitProgrammeId,
                "Probe Orbit",
                "Achieve orbit around Kerbin with an uncrewed Probe or Relay vessel.",
                100000.0);
            CrewedOrbitProgramme = new AchievementFundingProgramme(
                CrewedOrbitProgrammeId,
                "Crewed Orbit",
                "Achieve orbit around Kerbin with at least one live Kerbal aboard.",
                200000.0);
            MunProbeOrbitProgramme = new AchievementFundingProgramme(
                MunProbeOrbitProgrammeId,
                "Mun Probe Orbit",
                "Achieve orbit around Mun with an uncrewed Probe or Relay vessel.",
                200000.0,
                "Any agency must achieve Probe Orbit.");
            MinmusProbeOrbitProgramme = new AchievementFundingProgramme(
                MinmusProbeOrbitProgrammeId,
                "Minmus Probe Orbit",
                "Achieve orbit around Minmus with an uncrewed Probe or Relay vessel.",
                200000.0,
                "Any agency must achieve Probe Orbit.");
            MunCrewedOrbitProgramme = new AchievementFundingProgramme(
                MunCrewedOrbitProgrammeId,
                "Mun Crewed Orbit",
                "Achieve orbit around Mun with at least one live Kerbal aboard.",
                300000.0,
                "Any agency must achieve Crewed Orbit.");
            MinmusCrewedOrbitProgramme = new AchievementFundingProgramme(
                MinmusCrewedOrbitProgrammeId,
                "Minmus Crewed Orbit",
                "Achieve orbit around Minmus with at least one live Kerbal aboard.",
                300000.0,
                "Any agency must achieve Crewed Orbit.");

            _achievementFundingProgrammes.Add(ProbeOrbitProgramme);
            _achievementFundingProgrammes.Add(CrewedOrbitProgramme);
            _achievementFundingProgrammes.Add(MunProbeOrbitProgramme);
            _achievementFundingProgrammes.Add(MinmusProbeOrbitProgramme);
            _achievementFundingProgrammes.Add(MunCrewedOrbitProgramme);
            _achievementFundingProgrammes.Add(MinmusCrewedOrbitProgramme);

            KerbinNetworkProgramme = new FundingProgramme(
                "kerbin-network",
                "Kerbin Orbital Network",
                "Kerbin",
                10,
                200000.0,
                false,
                "Any agency must achieve Probe Orbit.");
            MunNetworkProgramme = new FundingProgramme(
                "mun-survey",
                "Mun Survey Network",
                "Mun",
                5,
                100000.0,
                false,
                "Any agency must achieve Mun Probe Orbit.");
            MinmusNetworkProgramme = new FundingProgramme(
                "minmus-relay",
                "Minmus Relay Initiative",
                "Minmus",
                5,
                100000.0,
                false,
                "Any agency must achieve Minmus Probe Orbit.");

            _fundingProgrammes.Add(KerbinNetworkProgramme);
            _fundingProgrammes.Add(MunNetworkProgramme);
            _fundingProgrammes.Add(MinmusNetworkProgramme);

            _fundingProgrammesView = _fundingProgrammes.AsReadOnly();
            _achievementFundingProgrammesView = _achievementFundingProgrammes.AsReadOnly();
        }

        public SpaceProgramState PlayerProgram { get; private set; }
        public SpaceProgramState AsterProgram { get; private set; }
        public SpaceProgramState CobaltProgram { get; private set; }
        public FundingProgramme KerbinNetworkProgramme { get; private set; }
        public FundingProgramme MunNetworkProgramme { get; private set; }
        public FundingProgramme MinmusNetworkProgramme { get; private set; }
        public AchievementFundingProgramme ProbeOrbitProgramme { get; private set; }
        public AchievementFundingProgramme CrewedOrbitProgramme { get; private set; }
        public AchievementFundingProgramme MunProbeOrbitProgramme { get; private set; }
        public AchievementFundingProgramme MinmusProbeOrbitProgramme { get; private set; }
        public AchievementFundingProgramme MunCrewedOrbitProgramme { get; private set; }
        public AchievementFundingProgramme MinmusCrewedOrbitProgramme { get; private set; }
        public IList<FundingProgramme> FundingProgrammes { get { return _fundingProgrammesView; } }
        public IList<AchievementFundingProgramme> AchievementFundingProgrammes
        {
            get { return _achievementFundingProgrammesView; }
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

            return RivalSimulation.CalculateLaunchProgressCost(program);
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
                FundingIntervalSeconds);
        }

        public bool HasProgramAchieved(
            SpaceProgramState program,
            AchievementFundingProgramme achievementProgramme)
        {
            return GetAchievementUniversalTime(program, achievementProgramme) >= 0.0;
        }

        /// <summary>
        /// Returns whether an achievement funding target is currently unlocked. Kerbin Probe
        /// and Crewed Orbit are available from the start. Lunar probe targets require any
        /// agency's Kerbin Probe Orbit; lunar crewed targets require any agency's Kerbin Crewed Orbit.
        /// </summary>
        public bool IsAchievementProgrammeAvailable(AchievementFundingProgramme achievementProgramme)
        {
            if (achievementProgramme == null)
            {
                return false;
            }

            if (string.Equals(achievementProgramme.Id, MunProbeOrbitProgrammeId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    achievementProgramme.Id,
                    MinmusProbeOrbitProgrammeId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return GetAchievementAgencyCount(ProbeOrbitProgramme) > 0;
            }

            if (string.Equals(achievementProgramme.Id, MunCrewedOrbitProgrammeId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    achievementProgramme.Id,
                    MinmusCrewedOrbitProgrammeId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return GetAchievementAgencyCount(CrewedOrbitProgramme) > 0;
            }

            return true;
        }

        public int GetAchievementAgencyCount(AchievementFundingProgramme achievementProgramme)
        {
            return GetAchievementAgencyCountAtTime(achievementProgramme, double.PositiveInfinity);
        }

        /// <summary>
        /// Returns this agency's expected share of one satellite contract using the current
        /// qualifying satellite counts across all programmes.
        /// </summary>
        public double GetSatelliteCurrentPayout(
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

        /// <summary>
        /// Returns this agency's expected share of the achievement contract on the next global
        /// funding date. All live contracts pay together on that shared 90-day schedule.
        /// </summary>
        public double GetAchievementCurrentPayout(
            SpaceProgramState program,
            AchievementFundingProgramme achievementProgramme)
        {
            if (achievementProgramme == null
                || !IsAchievementProgrammeAvailable(achievementProgramme)
                || _nextFundingUniversalTime < 0.0)
            {
                return 0.0;
            }

            int eligibleAgencyCount = GetAchievementAgencyCountAtTime(
                achievementProgramme,
                _nextFundingUniversalTime);

            return achievementProgramme.CalculateCurrentPayout(
                HasProgramAchievedByTime(program, achievementProgramme, _nextFundingUniversalTime),
                eligibleAgencyCount);
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
                bool restoredRivals = RacePersistenceScenario.TryRestoreRivalState(AsterProgram, CobaltProgram);
                bool restoredRaceProgress = RacePersistenceScenario.TryRestoreRaceProgress(
                    PlayerProgram,
                    KerbinNetworkProgramme,
                    MunNetworkProgramme,
                    MinmusNetworkProgramme,
                    ProbeOrbitProgramme,
                    CrewedOrbitProgramme,
                    MunProbeOrbitProgramme,
                    MinmusProbeOrbitProgramme,
                    MunCrewedOrbitProgramme,
                    MinmusCrewedOrbitProgramme);

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

            bool lunarProbeAchievementsAvailable = IsAchievementProgrammeAvailable(MunProbeOrbitProgramme);
            bool lunarCrewedAchievementsAvailable = IsAchievementProgrammeAvailable(MunCrewedOrbitProgramme);
            SatelliteTracker.RefreshPlayerSatelliteCounts(
                PlayerProgram,
                lunarProbeAchievementsAvailable,
                lunarCrewedAchievementsAvailable);
            UpdateFundingAvailability();

            // Catch rivals up before processing funding so their recorded achievement timestamps
            // determine whether they were eligible at each shared funding boundary crossed by time-warp.
            RivalSimulation.Refresh(
                PlayerProgram,
                AsterProgram,
                CobaltProgram,
                currentUniversalTime,
                KerbinNetworkProgramme.IsAvailable,
                MunNetworkProgramme.IsAvailable,
                MinmusNetworkProgramme.IsAvailable,
                !ProbeOrbitProgramme.IsExpired,
                !CrewedOrbitProgramme.IsExpired,
                !MunProbeOrbitProgramme.IsExpired,
                !MinmusProbeOrbitProgramme.IsExpired,
                !MunCrewedOrbitProgramme.IsExpired,
                !MinmusCrewedOrbitProgramme.IsExpired);

            UpdateFundingAvailability();
            StartAchievementContracts();
            ProcessDueFunding(currentUniversalTime);
            EvaluateFundingProgrammes();

            RacePersistenceScenario.CaptureRivalState(AsterProgram, CobaltProgram);
            RacePersistenceScenario.CaptureRaceProgress(
                PlayerProgram,
                KerbinNetworkProgramme,
                MunNetworkProgramme,
                MinmusNetworkProgramme,
                ProbeOrbitProgramme,
                CrewedOrbitProgramme,
                MunProbeOrbitProgramme,
                MinmusProbeOrbitProgramme,
                MunCrewedOrbitProgramme,
                MinmusCrewedOrbitProgramme);
        }

        private void UpdateFundingAvailability()
        {
            if (!KerbinNetworkProgramme.IsAvailable
                && GetAchievementAgencyCount(ProbeOrbitProgramme) > 0)
            {
                KerbinNetworkProgramme.Unlock();
            }

            if (!MunNetworkProgramme.IsAvailable
                && GetAchievementAgencyCount(MunProbeOrbitProgramme) > 0)
            {
                MunNetworkProgramme.Unlock();
            }

            if (!MinmusNetworkProgramme.IsAvailable
                && GetAchievementAgencyCount(MinmusProbeOrbitProgramme) > 0)
            {
                MinmusNetworkProgramme.Unlock();
            }
        }

        private void StartAchievementContracts()
        {
            for (int programmeIndex = 0; programmeIndex < _achievementFundingProgrammes.Count; programmeIndex++)
            {
                AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];
                if (IsAchievementProgrammeAvailable(programme)
                    && !programme.HasStarted
                    && GetAchievementAgencyCount(programme) > 0)
                {
                    programme.Start();
                }
            }
        }

        /// <summary>
        /// Processes every crossed global 90-day funding boundary. Satellite programmes and
        /// achievement programmes are deliberately paid in the same loop so every contract
        /// shares one funding date. Rivals also receive their guaranteed base income at each
        /// boundary. Achievement interest only advances when at least one agency had qualified
        /// by that exact boundary.
        /// </summary>
        private void ProcessDueFunding(double currentUniversalTime)
        {
            while (_nextFundingUniversalTime >= 0.0 && currentUniversalTime >= _nextFundingUniversalTime)
            {
                double payoutUniversalTime = _nextFundingUniversalTime;

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
                    if (!IsAchievementProgrammeAvailable(programme)
                        || !programme.HasStarted
                        || programme.IsExpired)
                    {
                        continue;
                    }

                    int eligibleAgencyCount = GetAchievementAgencyCountAtTime(programme, payoutUniversalTime);
                    if (eligibleAgencyCount <= 0)
                    {
                        // The first achievement occurred after this historical funding boundary,
                        // so this contract must wait for the next global date at the same 100% stage.
                        continue;
                    }

                    for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                    {
                        SpaceProgramState program = _programs[programIndex];
                        bool isEligible = HasProgramAchievedByTime(program, programme, payoutUniversalTime);
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
            for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
            {
                SpaceProgramState program = _programs[programIndex];
                program.NextPayoutFunds = CalculateSatelliteFundingForProgram(program);

                // Show the guaranteed rival base income in projected funding so the displayed
                // next payout and rival launch ETA use the same value that will actually be paid.
                if (!program.IsPlayer)
                {
                    program.NextPayoutFunds += RivalBaseIncomeFunds;
                }
            }

            if (_nextFundingUniversalTime < 0.0)
            {
                return;
            }

            // All projected amounts below are due on the same next funding date. Achievement
            // eligibility is evaluated against that date, matching the actual payment path.
            for (int programmeIndex = 0; programmeIndex < _achievementFundingProgrammes.Count; programmeIndex++)
            {
                AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];
                if (!IsAchievementProgrammeAvailable(programme)
                    || !programme.HasStarted
                    || programme.IsExpired)
                {
                    continue;
                }

                int eligibleAgencyCount = GetAchievementAgencyCountAtTime(
                    programme,
                    _nextFundingUniversalTime);

                for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                {
                    SpaceProgramState program = _programs[programIndex];
                    program.NextPayoutFunds += programme.CalculateCurrentPayout(
                        HasProgramAchievedByTime(program, programme, _nextFundingUniversalTime),
                        eligibleAgencyCount);
                }
            }
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
                payout += GetSatelliteCurrentPayout(program, _fundingProgrammes[programmeIndex]);
            }

            return payout;
        }

        private int GetAchievementAgencyCountAtTime(
            AchievementFundingProgramme achievementProgramme,
            double payoutUniversalTime)
        {
            if (achievementProgramme == null)
            {
                return 0;
            }

            int achievedAgencyCount = 0;
            for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
            {
                if (HasProgramAchievedByTime(
                    _programs[programIndex],
                    achievementProgramme,
                    payoutUniversalTime))
                {
                    achievedAgencyCount++;
                }
            }

            return achievedAgencyCount;
        }

        private bool HasProgramAchievedByTime(
            SpaceProgramState program,
            AchievementFundingProgramme achievementProgramme,
            double payoutUniversalTime)
        {
            double achievementUniversalTime = GetAchievementUniversalTime(program, achievementProgramme);
            return achievementUniversalTime >= 0.0 && achievementUniversalTime <= payoutUniversalTime;
        }

        private double GetAchievementUniversalTime(
            SpaceProgramState program,
            AchievementFundingProgramme achievementProgramme)
        {
            if (program == null || achievementProgramme == null)
            {
                return -1.0;
            }

            if (string.Equals(achievementProgramme.Id, ProbeOrbitProgrammeId, StringComparison.OrdinalIgnoreCase))
            {
                return program.HasAchievedProbeOrbit
                    ? Math.Max(0.0, program.ProbeOrbitAchievementUniversalTime)
                    : -1.0;
            }

            if (string.Equals(achievementProgramme.Id, CrewedOrbitProgrammeId, StringComparison.OrdinalIgnoreCase))
            {
                return program.HasAchievedCrewedOrbit
                    ? Math.Max(0.0, program.CrewedOrbitAchievementUniversalTime)
                    : -1.0;
            }

            if (string.Equals(achievementProgramme.Id, MunProbeOrbitProgrammeId, StringComparison.OrdinalIgnoreCase))
            {
                return program.HasAchievedMunProbeOrbit
                    ? Math.Max(0.0, program.MunProbeOrbitAchievementUniversalTime)
                    : -1.0;
            }

            if (string.Equals(achievementProgramme.Id, MinmusProbeOrbitProgrammeId, StringComparison.OrdinalIgnoreCase))
            {
                return program.HasAchievedMinmusProbeOrbit
                    ? Math.Max(0.0, program.MinmusProbeOrbitAchievementUniversalTime)
                    : -1.0;
            }

            if (string.Equals(achievementProgramme.Id, MunCrewedOrbitProgrammeId, StringComparison.OrdinalIgnoreCase))
            {
                return program.HasAchievedMunCrewedOrbit
                    ? Math.Max(0.0, program.MunCrewedOrbitAchievementUniversalTime)
                    : -1.0;
            }

            if (string.Equals(achievementProgramme.Id, MinmusCrewedOrbitProgrammeId, StringComparison.OrdinalIgnoreCase))
            {
                return program.HasAchievedMinmusCrewedOrbit
                    ? Math.Max(0.0, program.MinmusCrewedOrbitAchievementUniversalTime)
                    : -1.0;
            }

            return -1.0;
        }
    }
}

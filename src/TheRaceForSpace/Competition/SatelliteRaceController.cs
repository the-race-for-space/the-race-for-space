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
        private const int LunarNetworkUnlockKerbinSatelliteCount = 6;
        private const string ProbeOrbitProgrammeId = "probe-orbit";
        private const string CrewedOrbitProgrammeId = "crewed-orbit";

        private readonly List<SpaceProgramState> _programs = new List<SpaceProgramState>();
        private readonly List<FundingProgramme> _fundingProgrammes = new List<FundingProgramme>();
        private readonly List<AchievementFundingProgramme> _achievementFundingProgrammes =
            new List<AchievementFundingProgramme>();
        private double _nextFundingUniversalTime = -1.0;
        private bool _hasRestoredPersistentState;

        public SatelliteRaceController()
        {
            PlayerProgram = new SpaceProgramState("Kerbal Space Agency", true);
            AsterProgram = new SpaceProgramState("Aster Aerospace Directorate", false);
            CobaltProgram = new SpaceProgramState("Cobalt Orbital Bureau", false);

            // New/older saves begin with enough simulated cash for one complete Probe Orbit
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

            _achievementFundingProgrammes.Add(ProbeOrbitProgramme);
            _achievementFundingProgrammes.Add(CrewedOrbitProgramme);

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
                300000.0,
                false,
                "Reach 6 combined qualifying satellites in Kerbin orbit.");
            MinmusNetworkProgramme = new FundingProgramme(
                "minmus-relay",
                "Minmus Relay Initiative",
                "Minmus",
                5,
                300000.0,
                false,
                "Reach 6 combined qualifying satellites in Kerbin orbit.");

            _fundingProgrammes.Add(KerbinNetworkProgramme);
            _fundingProgrammes.Add(MunNetworkProgramme);
            _fundingProgrammes.Add(MinmusNetworkProgramme);
        }

        public SpaceProgramState PlayerProgram { get; private set; }
        public SpaceProgramState AsterProgram { get; private set; }
        public SpaceProgramState CobaltProgram { get; private set; }
        public FundingProgramme KerbinNetworkProgramme { get; private set; }
        public FundingProgramme MunNetworkProgramme { get; private set; }
        public FundingProgramme MinmusNetworkProgramme { get; private set; }
        public AchievementFundingProgramme ProbeOrbitProgramme { get; private set; }
        public AchievementFundingProgramme CrewedOrbitProgramme { get; private set; }
        public IList<FundingProgramme> FundingProgrammes { get { return _fundingProgrammes.AsReadOnly(); } }
        public IList<AchievementFundingProgramme> AchievementFundingProgrammes
        {
            get { return _achievementFundingProgrammes.AsReadOnly(); }
        }

        public double NextFundingUniversalTime { get { return _nextFundingUniversalTime; } }

        public int NextFundingYear
        {
            get { return GetKerbinYear(_nextFundingUniversalTime); }
        }

        public int NextFundingDay
        {
            get { return GetKerbinDay(_nextFundingUniversalTime); }
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
        /// Returns the current expected Kerbin days until a rival mission completes. The ETA
        /// uses the existing projected-income approximation rather than modelling every orbit-contract
        /// payment timestamp separately.
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

        public int GetAchievementAgencyCount(AchievementFundingProgramme achievementProgramme)
        {
            return GetAchievementAgencyCountAtTime(achievementProgramme, double.PositiveInfinity);
        }

        public double GetAchievementCurrentPayout(
            SpaceProgramState program,
            AchievementFundingProgramme achievementProgramme)
        {
            if (achievementProgramme == null)
            {
                return 0.0;
            }

            return achievementProgramme.CalculateCurrentPayout(
                HasProgramAchieved(program, achievementProgramme),
                GetAchievementAgencyCount(achievementProgramme));
        }

        public void Refresh()
        {
            if (Planetarium.fetch == null)
            {
                return;
            }

            double currentUniversalTime = Planetarium.GetUniversalTime();

            // Do not advance gameplay until the ScenarioModule has loaded the current save.
            // Old saves without 0.3 fields retain safe defaults and are migrated below.
            if (!_hasRestoredPersistentState)
            {
                bool restoredRivals = RacePersistenceScenario.TryRestoreRivalState(AsterProgram, CobaltProgram);
                bool restoredRaceProgress = RacePersistenceScenario.TryRestoreRaceProgress(
                    PlayerProgram,
                    KerbinNetworkProgramme,
                    MunNetworkProgramme,
                    MinmusNetworkProgramme,
                    ProbeOrbitProgramme,
                    CrewedOrbitProgramme);

                if (!restoredRivals || !restoredRaceProgress)
                {
                    return;
                }

                ApplyLegacySaveCompatibility(AsterProgram, currentUniversalTime);
                ApplyLegacySaveCompatibility(CobaltProgram, currentUniversalTime);
                _hasRestoredPersistentState = true;
            }

            if (_nextFundingUniversalTime < 0.0)
            {
                // Permanent satellite-network funding remains aligned to the global 90-day
                // Kerbin calendar boundaries used by the 0.2 prototype.
                _nextFundingUniversalTime =
                    (Math.Floor(currentUniversalTime / FundingIntervalSeconds) + 1.0)
                    * FundingIntervalSeconds;
            }

            SatelliteTracker.RefreshPlayerSatelliteCounts(PlayerProgram);
            UpdateFundingAvailability();
            StartAchievementContracts();

            // Catch rivals up before paying achievement contracts. Their simulated completion
            // timestamps may fall before one or more overdue payout dates, and those agencies
            // must receive only the historical payments for which they were already eligible.
            RivalSimulation.Refresh(
                PlayerProgram,
                AsterProgram,
                CobaltProgram,
                currentUniversalTime,
                KerbinNetworkProgramme.IsAvailable,
                MunNetworkProgramme.IsAvailable,
                MinmusNetworkProgramme.IsAvailable,
                IsAchievementTargetStillLive(ProbeOrbitProgramme, currentUniversalTime),
                IsAchievementTargetStillLive(CrewedOrbitProgramme, currentUniversalTime));

            UpdateFundingAvailability();
            StartAchievementContracts();
            ProcessDueAchievementFunding(currentUniversalTime);
            ProcessDueNetworkFunding(currentUniversalTime);
            EvaluateFundingProgrammes();

            RacePersistenceScenario.CaptureRivalState(AsterProgram, CobaltProgram);
            RacePersistenceScenario.CaptureRaceProgress(
                PlayerProgram,
                KerbinNetworkProgramme,
                MunNetworkProgramme,
                MinmusNetworkProgramme,
                ProbeOrbitProgramme,
                CrewedOrbitProgramme);
        }

        private void ApplyLegacySaveCompatibility(SpaceProgramState program, double currentUniversalTime)
        {
            if (program == null || program.IsPlayer)
            {
                return;
            }

            int existingSatelliteCount = program.GetSatelliteCount("Kerbin")
                + program.GetSatelliteCount("Mun")
                + program.GetSatelliteCount("Minmus");

            // A 0.2 rival that already owns an orbital satellite necessarily demonstrated
            // probe orbit. Treat the 0.3 achievement as newly recognised at the current save time
            // instead of retroactively paying years of a contract that did not exist in 0.2.
            if (!program.HasAchievedProbeOrbit && existingSatelliteCount > 0)
            {
                program.HasAchievedProbeOrbit = true;
                program.ProbeOrbitAchievementUniversalTime = Math.Max(0.0, currentUniversalTime);
            }

            if (program.HasAchievedProbeOrbit && program.ProbeOrbitAchievementUniversalTime < 0.0)
            {
                program.ProbeOrbitAchievementUniversalTime = Math.Max(0.0, currentUniversalTime);
            }

            if (program.HasAchievedCrewedOrbit && program.CrewedOrbitAchievementUniversalTime < 0.0)
            {
                program.CrewedOrbitAchievementUniversalTime = Math.Max(0.0, currentUniversalTime);
            }

            // If an old rival had no completed satellites, reinterpret its current development
            // progress as the new opening Probe Orbit mission instead of discarding that spending.
            if (!program.HasAchievedProbeOrbit
                && !string.Equals(
                    program.NextLaunchBodyName,
                    RivalSimulation.ProbeOrbitTargetName,
                    StringComparison.OrdinalIgnoreCase))
            {
                program.NextLaunchBodyName = RivalSimulation.ProbeOrbitTargetName;
            }
        }

        private void UpdateFundingAvailability()
        {
            if (!KerbinNetworkProgramme.IsAvailable
                && (PlayerProgram.HasAchievedProbeOrbit
                    || AsterProgram.HasAchievedProbeOrbit
                    || CobaltProgram.HasAchievedProbeOrbit))
            {
                KerbinNetworkProgramme.Unlock();
            }

            int combinedKerbinSatelliteCount = PlayerProgram.GetSatelliteCount("Kerbin")
                + AsterProgram.GetSatelliteCount("Kerbin")
                + CobaltProgram.GetSatelliteCount("Kerbin");

            if (combinedKerbinSatelliteCount >= LunarNetworkUnlockKerbinSatelliteCount)
            {
                MunNetworkProgramme.Unlock();
                MinmusNetworkProgramme.Unlock();
            }
        }

        private void StartAchievementContracts()
        {
            for (int programmeIndex = 0; programmeIndex < _achievementFundingProgrammes.Count; programmeIndex++)
            {
                AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];
                double firstAchievementUniversalTime = GetFirstAchievementUniversalTime(programme);

                if (!programme.HasStarted && firstAchievementUniversalTime >= 0.0)
                {
                    programme.Start(firstAchievementUniversalTime);
                    continue;
                }

                // Repair the short-lived first 0.3 implementation-pass format, which persisted
                // a started/payment count before independent next-payout timestamps were added.
                if (programme.HasStarted
                    && !programme.IsExpired
                    && programme.NextPayoutUniversalTime < 0.0
                    && firstAchievementUniversalTime >= 0.0)
                {
                    programme.RestoreState(
                        true,
                        programme.PaymentsProcessed,
                        firstAchievementUniversalTime + (programme.PaymentsProcessed * FundingIntervalSeconds));
                }
            }
        }

        private bool IsAchievementTargetStillLive(
            AchievementFundingProgramme programme,
            double currentUniversalTime)
        {
            if (programme == null || programme.IsExpired)
            {
                return false;
            }

            if (!programme.HasStarted || programme.NextPayoutUniversalTime < 0.0)
            {
                return true;
            }

            int paymentsRemaining = 10 - programme.PaymentsProcessed;
            double finalPayoutUniversalTime = programme.NextPayoutUniversalTime
                + ((paymentsRemaining - 1) * FundingIntervalSeconds);

            // A mission completed exactly on the final payout boundary may still qualify
            // for that payment. After that timestamp the contract is no longer a valid target.
            return currentUniversalTime <= finalPayoutUniversalTime;
        }

        private void ProcessDueAchievementFunding(double currentUniversalTime)
        {
            for (int programmeIndex = 0; programmeIndex < _achievementFundingProgrammes.Count; programmeIndex++)
            {
                AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];

                while (programme.HasStarted
                    && !programme.IsExpired
                    && programme.NextPayoutUniversalTime >= 0.0
                    && currentUniversalTime >= programme.NextPayoutUniversalTime)
                {
                    double payoutUniversalTime = programme.NextPayoutUniversalTime;
                    int eligibleAgencyCount = GetAchievementAgencyCountAtTime(programme, payoutUniversalTime);

                    for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                    {
                        SpaceProgramState program = _programs[programIndex];
                        bool isEligible = HasProgramAchievedByTime(program, programme, payoutUniversalTime);
                        double payout = programme.CalculateCurrentPayout(isEligible, eligibleAgencyCount);
                        AwardProgramFunds(program, payout);
                    }

                    programme.AdvancePayout(FundingIntervalSeconds);
                }
            }
        }

        private void ProcessDueNetworkFunding(double currentUniversalTime)
        {
            while (_nextFundingUniversalTime >= 0.0 && currentUniversalTime >= _nextFundingUniversalTime)
            {
                for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                {
                    SpaceProgramState program = _programs[programIndex];
                    AwardProgramFunds(program, CalculateSatelliteFundingForProgram(program));
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
                if (CareerFundingAdapter.TryAddFunds(payout))
                {
                    program.AwardedFunds += payout;
                }

                return;
            }

            program.Funds += payout;
            program.AwardedFunds += payout;
        }

        private void EvaluateFundingProgrammes()
        {
            EvaluateSatelliteRaceClaims();

            for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
            {
                SpaceProgramState program = _programs[programIndex];
                program.NextPayoutFunds = CalculateSatelliteFundingForProgram(program);
            }

            // NextPayoutFunds is a convenient current projection for the command center and
            // rival ETA. Orbit contracts have independent dates, so this is deliberately an
            // aggregate estimate rather than a promise that every component pays simultaneously.
            for (int programmeIndex = 0; programmeIndex < _achievementFundingProgrammes.Count; programmeIndex++)
            {
                AchievementFundingProgramme programme = _achievementFundingProgrammes[programmeIndex];
                int achievedAgencyCount = GetAchievementAgencyCount(programme);

                for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                {
                    SpaceProgramState program = _programs[programIndex];
                    program.NextPayoutFunds += programme.CalculateCurrentPayout(
                        HasProgramAchieved(program, programme),
                        achievedAgencyCount);
                }
            }
        }

        private void EvaluateSatelliteRaceClaims()
        {
            for (int programmeIndex = 0; programmeIndex < _fundingProgrammes.Count; programmeIndex++)
            {
                FundingProgramme programme = _fundingProgrammes[programmeIndex];
                if (!programme.IsAvailable || programme.IsClaimed)
                {
                    continue;
                }

                for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                {
                    SpaceProgramState program = _programs[programIndex];
                    if (program.GetSatelliteCount(programme.CelestialBodyName) < programme.RequiredSatellites)
                    {
                        continue;
                    }

                    programme.WinnerProgramName = program.Name;
                    program.RacePoints += 1;
                    break;
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
                FundingProgramme programme = _fundingProgrammes[programmeIndex];
                if (!programme.IsAvailable)
                {
                    continue;
                }

                int totalSatelliteCount = 0;
                for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                {
                    totalSatelliteCount += _programs[programIndex].GetSatelliteCount(programme.CelestialBodyName);
                }

                payout += programme.CalculateCurrentPayout(
                    program.GetSatelliteCount(programme.CelestialBodyName),
                    totalSatelliteCount);
            }

            return payout;
        }

        private double GetFirstAchievementUniversalTime(AchievementFundingProgramme achievementProgramme)
        {
            double earliestUniversalTime = double.PositiveInfinity;

            for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
            {
                double achievementUniversalTime = GetAchievementUniversalTime(
                    _programs[programIndex],
                    achievementProgramme);

                if (achievementUniversalTime >= 0.0 && achievementUniversalTime < earliestUniversalTime)
                {
                    earliestUniversalTime = achievementUniversalTime;
                }
            }

            return double.IsPositiveInfinity(earliestUniversalTime) ? -1.0 : earliestUniversalTime;
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

            return -1.0;
        }
    }
}

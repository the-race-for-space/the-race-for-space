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
    /// Coordinates the narrow satellite race prototype without introducing a general race framework.
    /// </summary>
    public sealed class SatelliteRaceController
    {
        private const double KerbinDaySeconds = 21600.0;
        private const int KerbinDaysPerYear = 426;
        private const double FundingIntervalSeconds = 90.0 * KerbinDaySeconds;
        private const double RivalStartingFunds = 200000.0;
        private const string RivalInitialLaunchBodyName = "Kerbin";

        private readonly List<SpaceProgramState> _programs = new List<SpaceProgramState>();
        private readonly List<FundingProgramme> _fundingProgrammes = new List<FundingProgramme>();
        private double _nextFundingUniversalTime = -1.0;
        private bool _hasRestoredPersistentRivalState;

        public SatelliteRaceController()
        {
            PlayerProgram = new SpaceProgramState("Kerbal Space Agency", true);
            AsterProgram = new SpaceProgramState("Aster Aerospace Directorate", false);
            CobaltProgram = new SpaceProgramState("Cobalt Orbital Bureau", false);

            // New/older saves without Race for Space persistence begin with enough simulated
            // cash for one complete Kerbin development cycle, so Kerbin is always the first
            // planned launch. Persisted saves overwrite these defaults with their saved state.
            AsterProgram.Funds = RivalStartingFunds;
            CobaltProgram.Funds = RivalStartingFunds;
            AsterProgram.NextLaunchBodyName = RivalInitialLaunchBodyName;
            CobaltProgram.NextLaunchBodyName = RivalInitialLaunchBodyName;

            _programs.Add(PlayerProgram);
            _programs.Add(AsterProgram);
            _programs.Add(CobaltProgram);

            _fundingProgrammes.Add(new FundingProgramme("kerbin-network", "Kerbin Orbital Network", "Kerbin", 10, 200000.0));
            _fundingProgrammes.Add(new FundingProgramme("mun-survey", "Mun Survey Network", "Mun", 5, 300000.0));
            _fundingProgrammes.Add(new FundingProgramme("minmus-relay", "Minmus Relay Initiative", "Minmus", 5, 300000.0));
        }

        public SpaceProgramState PlayerProgram { get; private set; }
        public SpaceProgramState AsterProgram { get; private set; }
        public SpaceProgramState CobaltProgram { get; private set; }
        public IList<FundingProgramme> FundingProgrammes { get { return _fundingProgrammes.AsReadOnly(); } }
        public double NextFundingUniversalTime { get { return _nextFundingUniversalTime; } }

        public int NextFundingYear
        {
            get
            {
                if (_nextFundingUniversalTime < 0.0)
                {
                    return 0;
                }

                int totalKerbinDays = (int)Math.Floor(_nextFundingUniversalTime / KerbinDaySeconds);
                return (totalKerbinDays / KerbinDaysPerYear) + 1;
            }
        }

        public int NextFundingDay
        {
            get
            {
                if (_nextFundingUniversalTime < 0.0)
                {
                    return 0;
                }

                int totalKerbinDays = (int)Math.Floor(_nextFundingUniversalTime / KerbinDaySeconds);
                return (totalKerbinDays % KerbinDaysPerYear) + 1;
            }
        }

        /// <summary>
        /// Returns the funds required for the rival's next successful 10% launch-progress step.
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
        /// Returns the current expected Kerbin days until a rival launch, including projected
        /// funding waits. A null result means the rival cannot currently finance completion.
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

        public void Refresh()
        {
            if (Planetarium.fetch == null)
            {
                return;
            }

            // Do not advance rivals until the ScenarioModule has loaded the current save.
            // Old saves without persisted rival data retain the 200,000/Kerbin defaults.
            if (!_hasRestoredPersistentRivalState)
            {
                _hasRestoredPersistentRivalState =
                    RacePersistenceScenario.TryRestoreRivalState(AsterProgram, CobaltProgram);

                if (!_hasRestoredPersistentRivalState)
                {
                    return;
                }
            }

            double currentUniversalTime = Planetarium.GetUniversalTime();
            if (_nextFundingUniversalTime < 0.0)
            {
                // Funding dates align to the next 90-day Kerbin calendar boundary so the
                // displayed Year/Day remains stable instead of depending on scene load time.
                _nextFundingUniversalTime =
                    (Math.Floor(currentUniversalTime / FundingIntervalSeconds) + 1.0)
                    * FundingIntervalSeconds;
            }

            SatelliteTracker.RefreshPlayerSatelliteCounts(PlayerProgram);
            RivalSimulation.Refresh(AsterProgram, CobaltProgram, currentUniversalTime);
            EvaluateFundingProgrammes();
            ProcessDueFunding(currentUniversalTime);

            // ScenarioModule.OnSave can occur after any gameplay scene. Keep its in-memory
            // snapshot synchronized with every rival change rather than reconstructing later.
            RacePersistenceScenario.CaptureRivalState(AsterProgram, CobaltProgram);
        }

        private bool ProcessDueFunding(double currentUniversalTime)
        {
            bool processedFundingDate = false;

            while (_nextFundingUniversalTime >= 0.0 && currentUniversalTime >= _nextFundingUniversalTime)
            {
                for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                {
                    SpaceProgramState program = _programs[programIndex];
                    double payout = program.NextPayoutFunds;
                    if (payout <= 0.0)
                    {
                        continue;
                    }

                    if (program.IsPlayer)
                    {
                        // The player receives real KSP Career funds. In non-Career modes the
                        // adapter deliberately leaves KSP's economy untouched.
                        if (CareerFundingAdapter.TryAddFunds(payout))
                        {
                            program.AwardedFunds += payout;
                        }
                    }
                    else
                    {
                        program.Funds += payout;
                        program.AwardedFunds += payout;
                    }
                }

                _nextFundingUniversalTime += FundingIntervalSeconds;
                processedFundingDate = true;
            }

            return processedFundingDate;
        }

        private void EvaluateFundingProgrammes()
        {
            // NextPayoutFunds is the amount currently due on the next scheduled funding date.
            // Recalculate it on each controlled refresh because rival progress can dilute shares.
            for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
            {
                _programs[programIndex].NextPayoutFunds = 0.0;
            }

            for (int programmeIndex = 0; programmeIndex < _fundingProgrammes.Count; programmeIndex++)
            {
                FundingProgramme programme = _fundingProgrammes[programmeIndex];

                // First-to-requirement remains a separate race achievement, but it no longer
                // grants exclusive ownership of the funding pool.
                if (!programme.IsClaimed)
                {
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

                int totalSatelliteCount = 0;
                for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                {
                    totalSatelliteCount += _programs[programIndex].GetSatelliteCount(programme.CelestialBodyName);
                }

                for (int programIndex = 0; programIndex < _programs.Count; programIndex++)
                {
                    SpaceProgramState program = _programs[programIndex];
                    int programSatelliteCount = program.GetSatelliteCount(programme.CelestialBodyName);
                    program.NextPayoutFunds += programme.CalculateCurrentPayout(programSatelliteCount, totalSatelliteCount);
                }
            }
        }
    }
}

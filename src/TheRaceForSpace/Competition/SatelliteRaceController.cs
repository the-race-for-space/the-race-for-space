using System.Collections.Generic;
using TheRaceForSpace.Funding;
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
        private readonly List<SpaceProgramState> _programs = new List<SpaceProgramState>();
        private readonly List<FundingProgramme> _fundingProgrammes = new List<FundingProgramme>();
        private double _campaignStartUniversalTime = -1.0;

        public SatelliteRaceController()
        {
            PlayerProgram = new SpaceProgramState("Kerbal Space Agency", true);
            AsterProgram = new SpaceProgramState("Aster Aerospace Directorate", false);
            CobaltProgram = new SpaceProgramState("Cobalt Orbital Bureau", false);

            _programs.Add(PlayerProgram);
            _programs.Add(AsterProgram);
            _programs.Add(CobaltProgram);

            _fundingProgrammes.Add(new FundingProgramme("kerbin-network", "Kerbin Orbital Network", "Kerbin", 2, 25000.0));
            _fundingProgrammes.Add(new FundingProgramme("mun-survey", "Mun Survey Network", "Mun", 1, 40000.0));
            _fundingProgrammes.Add(new FundingProgramme("minmus-relay", "Minmus Relay Initiative", "Minmus", 1, 50000.0));
        }

        public SpaceProgramState PlayerProgram { get; private set; }
        public SpaceProgramState AsterProgram { get; private set; }
        public SpaceProgramState CobaltProgram { get; private set; }
        public IList<FundingProgramme> FundingProgrammes { get { return _fundingProgrammes.AsReadOnly(); } }

        public void Refresh()
        {
            if (Planetarium.fetch == null)
            {
                return;
            }

            double currentUniversalTime = Planetarium.GetUniversalTime();
            if (_campaignStartUniversalTime < 0.0)
            {
                _campaignStartUniversalTime = currentUniversalTime;
            }

            SatelliteTracker.RefreshPlayerSatelliteCounts(PlayerProgram);
            RivalSimulation.Refresh(AsterProgram, CobaltProgram, currentUniversalTime - _campaignStartUniversalTime);
            EvaluateFundingProgrammes();
        }

        private void EvaluateFundingProgrammes()
        {
            for (int programmeIndex = 0; programmeIndex < _fundingProgrammes.Count; programmeIndex++)
            {
                FundingProgramme programme = _fundingProgrammes[programmeIndex];
                if (programme.IsClaimed)
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
                    if (program.IsPlayer)
                    {
                        program.AwardedFunds += programme.RewardFunds;
                    }

                    break;
                }
            }
        }
    }
}

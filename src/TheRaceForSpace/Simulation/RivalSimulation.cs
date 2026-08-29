using System;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Simulation
{
    /// <summary>
    /// Lightweight rival launch simulation used by the current satellite prototype.
    /// </summary>
    public static class RivalSimulation
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double LaunchProgressIntervalSeconds = 5.0 * KerbinDaySeconds;
        private const double LaunchProgressChance = 0.50;
        private const int LaunchProgressIncrementPercent = 10;
        private const double LaunchProgressCostFunds = 20000.0;

        private static readonly string[] LaunchBodies = { "Kerbin", "Mun", "Minmus" };
        private static readonly Random RandomGenerator = new Random();

        public static void Refresh(SpaceProgramState asterProgram, SpaceProgramState cobaltProgram, double currentUniversalTime)
        {
            if (asterProgram == null || cobaltProgram == null)
            {
                return;
            }

            RefreshProgram(asterProgram, currentUniversalTime);
            RefreshProgram(cobaltProgram, currentUniversalTime);
        }

        private static void RefreshProgram(SpaceProgramState program, double currentUniversalTime)
        {
            if (string.IsNullOrEmpty(program.NextLaunchBodyName))
            {
                program.NextLaunchBodyName = ChooseNextLaunchBody();
            }

            if (program.NextLaunchProgressCheckUniversalTime <= 0.0)
            {
                // Align checks to the next five-day Kerbin calendar boundary so each rival
                // advances on a predictable cadence even when the controller starts mid-save.
                program.NextLaunchProgressCheckUniversalTime =
                    (Math.Floor(currentUniversalTime / LaunchProgressIntervalSeconds) + 1.0)
                    * LaunchProgressIntervalSeconds;
            }

            TryCompleteLaunch(program);

            while (currentUniversalTime >= program.NextLaunchProgressCheckUniversalTime)
            {
                // Each successful 10% development step costs 20,000 funds. With ten steps
                // required for a complete launch, the current prototype launch costs 200,000.
                // A rival without enough funds for the next step cannot make progress.
                if (program.LaunchProgressPercent < 100
                    && program.Funds >= LaunchProgressCostFunds
                    && RandomGenerator.NextDouble() < LaunchProgressChance)
                {
                    program.Funds -= LaunchProgressCostFunds;
                    program.LaunchProgressPercent = Math.Min(
                        100,
                        program.LaunchProgressPercent + LaunchProgressIncrementPercent);
                }

                TryCompleteLaunch(program);
                program.NextLaunchProgressCheckUniversalTime += LaunchProgressIntervalSeconds;
            }
        }

        private static void TryCompleteLaunch(SpaceProgramState program)
        {
            if (program.LaunchProgressPercent < 100)
            {
                return;
            }

            program.SetSatelliteCount(
                program.NextLaunchBodyName,
                program.GetSatelliteCount(program.NextLaunchBodyName) + 1);

            program.LaunchProgressPercent = 0;
            program.NextLaunchBodyName = ChooseNextLaunchBody();
        }

        private static string ChooseNextLaunchBody()
        {
            return LaunchBodies[RandomGenerator.Next(LaunchBodies.Length)];
        }
    }
}

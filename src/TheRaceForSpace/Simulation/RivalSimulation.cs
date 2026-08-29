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
        private const double LaunchProgressChance = 0.25;
        private const int LaunchProgressIncrementPercent = 25;
        private const double LaunchCostFunds = 50000.0;

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

            // A rival that previously reached 100% but lacked funds should launch as soon as
            // a scheduled payout makes the launch affordable, without waiting another five days.
            TryCompleteLaunch(program);

            while (currentUniversalTime >= program.NextLaunchProgressCheckUniversalTime)
            {
                if (program.LaunchProgressPercent < 100 && RandomGenerator.NextDouble() < LaunchProgressChance)
                {
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
            if (program.LaunchProgressPercent < 100 || program.Funds < LaunchCostFunds)
            {
                return;
            }

            program.Funds -= LaunchCostFunds;
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

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
        private const double MunMinmusLaunchProgressCostMultiplier = 1.50;

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

        /// <summary>
        /// Returns the funds required for the rival's next successful 10% launch-progress step.
        /// Mun and Minmus launches currently cost 50% more than the Kerbin base cost.
        /// </summary>
        public static double CalculateLaunchProgressCost(SpaceProgramState program)
        {
            if (program != null
                && (string.Equals(program.NextLaunchBodyName, "Mun", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(program.NextLaunchBodyName, "Minmus", StringComparison.OrdinalIgnoreCase)))
            {
                return LaunchProgressCostFunds * MunMinmusLaunchProgressCostMultiplier;
            }

            return LaunchProgressCostFunds;
        }

        /// <summary>
        /// Estimates the average Kerbin days until a rival completes its planned launch.
        /// Returns null when current funds and projected scheduled payouts cannot finance
        /// all remaining development steps.
        /// </summary>
        public static int? CalculateEstimatedLaunchDays(
            SpaceProgramState program,
            double currentUniversalTime,
            double nextFundingUniversalTime,
            double fundingIntervalSeconds)
        {
            if (program == null)
            {
                return null;
            }

            int remainingProgressPercent = Math.Max(0, 100 - program.LaunchProgressPercent);
            int remainingProgressSteps = (remainingProgressPercent + LaunchProgressIncrementPercent - 1)
                / LaunchProgressIncrementPercent;

            if (remainingProgressSteps <= 0)
            {
                return 0;
            }

            double launchProgressCostFunds = CalculateLaunchProgressCost(program);
            double availableFunds = Math.Max(0.0, program.Funds);
            double projectedPayoutFunds = Math.Max(0.0, program.NextPayoutFunds);
            double expectedDaysPerSuccessfulStep =
                (LaunchProgressIntervalSeconds / KerbinDaySeconds) / LaunchProgressChance;
            double fundingIntervalDays = fundingIntervalSeconds / KerbinDaySeconds;
            double nextFundingInDays = nextFundingUniversalTime >= 0.0
                ? Math.Max(0.0, (nextFundingUniversalTime - currentUniversalTime) / KerbinDaySeconds)
                : double.PositiveInfinity;
            double elapsedDays = 0.0;

            for (int stepIndex = 0; stepIndex < remainingProgressSteps; stepIndex++)
            {
                double expectedStepDay = elapsedDays + expectedDaysPerSuccessfulStep;

                // Include scheduled payouts that arrive before the next average successful
                // roll. A payout on the exact same game-time boundary is processed after the
                // rival roll by the controller, so it cannot finance that particular check.
                // The current Next Payout is reused as a projection and recalculated normally
                // as satellite ownership changes.
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

                    // If development is cash-limited, wait for enough scheduled payouts to
                    // finance the next body-adjusted progress step, then allow another average roll period.
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

            return (int)Math.Ceiling(elapsedDays);
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
                double launchProgressCostFunds = CalculateLaunchProgressCost(program);

                // Kerbin uses the 20,000 base cost for each successful 10% step. Mun and Minmus
                // cost 50% more, or 30,000. A rival without enough funds cannot progress.
                if (program.LaunchProgressPercent < 100
                    && program.Funds >= launchProgressCostFunds
                    && RandomGenerator.NextDouble() < LaunchProgressChance)
                {
                    program.Funds -= launchProgressCostFunds;
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

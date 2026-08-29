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

                // Include any scheduled payouts expected to arrive before the next average
                // successful roll. The current Next Payout is used as a rolling projection;
                // the estimate is recalculated every normal controller refresh as ownership changes.
                while (projectedPayoutFunds > 0.0
                    && fundingIntervalDays > 0.0
                    && nextFundingInDays <= expectedStepDay)
                {
                    availableFunds += projectedPayoutFunds;
                    nextFundingInDays += fundingIntervalDays;
                }

                if (availableFunds < LaunchProgressCostFunds)
                {
                    if (projectedPayoutFunds <= 0.0
                        || fundingIntervalDays <= 0.0
                        || double.IsPositiveInfinity(nextFundingInDays))
                    {
                        return null;
                    }

                    // If development is cash-limited, wait for enough scheduled payouts to
                    // finance the next 20,000 step, then allow another average roll period.
                    while (availableFunds < LaunchProgressCostFunds)
                    {
                        elapsedDays = Math.Max(elapsedDays, nextFundingInDays);
                        availableFunds += projectedPayoutFunds;
                        nextFundingInDays += fundingIntervalDays;
                    }

                    expectedStepDay = elapsedDays + expectedDaysPerSuccessfulStep;

                    while (nextFundingInDays <= expectedStepDay)
                    {
                        availableFunds += projectedPayoutFunds;
                        nextFundingInDays += fundingIntervalDays;
                    }
                }

                availableFunds -= LaunchProgressCostFunds;
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

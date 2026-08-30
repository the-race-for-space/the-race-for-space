using System;
using System.Collections.Generic;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Simulation
{
    /// <summary>
    /// Lightweight rival mission simulation used by the current prototype.
    /// </summary>
    public static class RivalSimulation
    {
        public const string ProbeOrbitTargetName = "Probe Orbit";
        public const string CrewedOrbitTargetName = "Crewed Orbit";
        public const string MunProbeOrbitTargetName = "Mun Probe Orbit";
        public const string MinmusProbeOrbitTargetName = "Minmus Probe Orbit";
        public const string MunCrewedOrbitTargetName = "Mun Crewed Orbit";
        public const string MinmusCrewedOrbitTargetName = "Minmus Crewed Orbit";

        private const double KerbinDaySeconds = 21600.0;
        private const double LaunchProgressIntervalSeconds = 5.0 * KerbinDaySeconds;
        private const double LaunchProgressChance = 0.25;
        private const int LaunchProgressIncrementPercent = 10;
        private const double LaunchProgressCostFunds = 20000.0;
        private const double MunMinmusLaunchProgressCostMultiplier = 2.0;
        private const double CrewedOrbitLaunchProgressCostMultiplier = 2.0;

        private static readonly Random RandomGenerator = new Random();

        public static void Refresh(
            SpaceProgramState playerProgram,
            SpaceProgramState asterProgram,
            SpaceProgramState cobaltProgram,
            double currentUniversalTime,
            bool kerbinNetworkAvailable,
            bool munNetworkAvailable,
            bool minmusNetworkAvailable,
            bool probeOrbitContractLive,
            bool crewedOrbitContractLive,
            bool munProbeOrbitContractLive,
            bool minmusProbeOrbitContractLive,
            bool munCrewedOrbitContractLive,
            bool minmusCrewedOrbitContractLive)
        {
            if (playerProgram == null || asterProgram == null || cobaltProgram == null)
            {
                return;
            }

            RefreshProgram(
                asterProgram,
                playerProgram,
                asterProgram,
                cobaltProgram,
                currentUniversalTime,
                kerbinNetworkAvailable,
                munNetworkAvailable,
                minmusNetworkAvailable,
                probeOrbitContractLive,
                crewedOrbitContractLive,
                munProbeOrbitContractLive,
                minmusProbeOrbitContractLive,
                munCrewedOrbitContractLive,
                minmusCrewedOrbitContractLive);

            RefreshProgram(
                cobaltProgram,
                playerProgram,
                asterProgram,
                cobaltProgram,
                currentUniversalTime,
                kerbinNetworkAvailable,
                munNetworkAvailable,
                minmusNetworkAvailable,
                probeOrbitContractLive,
                crewedOrbitContractLive,
                munProbeOrbitContractLive,
                minmusProbeOrbitContractLive,
                munCrewedOrbitContractLive,
                minmusCrewedOrbitContractLive);
        }

        /// <summary>
        /// Returns the funds required for the rival's next successful 10% mission-progress step.
        /// Probe Orbit uses the Kerbin satellite cost, while Crewed Orbit and all Mun/Minmus
        /// achievement or satellite launches cost twice the Kerbin base cost.
        /// </summary>
        public static double CalculateLaunchProgressCost(SpaceProgramState program)
        {
            if (program == null)
            {
                return LaunchProgressCostFunds;
            }

            if (string.Equals(program.NextLaunchBodyName, CrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return LaunchProgressCostFunds * CrewedOrbitLaunchProgressCostMultiplier;
            }

            if (string.Equals(program.NextLaunchBodyName, MunProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(program.NextLaunchBodyName, MinmusProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(program.NextLaunchBodyName, MunCrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(program.NextLaunchBodyName, MinmusCrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(program.NextLaunchBodyName, "Mun", StringComparison.OrdinalIgnoreCase)
                || string.Equals(program.NextLaunchBodyName, "Minmus", StringComparison.OrdinalIgnoreCase))
            {
                return LaunchProgressCostFunds * MunMinmusLaunchProgressCostMultiplier;
            }

            return LaunchProgressCostFunds;
        }

        /// <summary>
        /// Estimates the average Kerbin days until a rival completes its planned mission.
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
                // roll. Achievement-contract payouts are intentionally treated as part of the
                // rival's current projected income estimate rather than an exact forecast here.
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

        private static void RefreshProgram(
            SpaceProgramState program,
            SpaceProgramState playerProgram,
            SpaceProgramState asterProgram,
            SpaceProgramState cobaltProgram,
            double currentUniversalTime,
            bool kerbinNetworkAvailable,
            bool munNetworkAvailable,
            bool minmusNetworkAvailable,
            bool probeOrbitContractLive,
            bool crewedOrbitContractLive,
            bool munProbeOrbitContractLive,
            bool minmusProbeOrbitContractLive,
            bool munCrewedOrbitContractLive,
            bool minmusCrewedOrbitContractLive)
        {
            if (!IsCurrentTargetAvailable(
                program,
                playerProgram,
                asterProgram,
                cobaltProgram,
                kerbinNetworkAvailable,
                munNetworkAvailable,
                minmusNetworkAvailable,
                probeOrbitContractLive,
                crewedOrbitContractLive,
                munProbeOrbitContractLive,
                minmusProbeOrbitContractLive,
                munCrewedOrbitContractLive,
                minmusCrewedOrbitContractLive))
            {
                // If an achievement contract expires or becomes unavailable before this rival
                // completes it, abandon that development rather than spending simulated funds
                // on an objective that is not currently in play.
                program.NextLaunchBodyName = null;
                program.LaunchProgressPercent = 0;
            }

            if (string.IsNullOrEmpty(program.NextLaunchBodyName))
            {
                program.NextLaunchBodyName = ChooseNextLaunchTarget(
                    program,
                    playerProgram,
                    asterProgram,
                    cobaltProgram,
                    kerbinNetworkAvailable,
                    munNetworkAvailable,
                    minmusNetworkAvailable,
                    probeOrbitContractLive,
                    crewedOrbitContractLive,
                    munProbeOrbitContractLive,
                    minmusProbeOrbitContractLive,
                    munCrewedOrbitContractLive,
                    minmusCrewedOrbitContractLive);
            }

            if (program.NextLaunchProgressCheckUniversalTime <= 0.0)
            {
                // Align checks to the next five-day Kerbin calendar boundary so each rival
                // advances on a predictable cadence even when the controller starts mid-save.
                program.NextLaunchProgressCheckUniversalTime =
                    (Math.Floor(currentUniversalTime / LaunchProgressIntervalSeconds) + 1.0)
                    * LaunchProgressIntervalSeconds;
            }

            if (TryCompleteLaunch(program, currentUniversalTime))
            {
                program.NextLaunchBodyName = ChooseNextLaunchTarget(
                    program,
                    playerProgram,
                    asterProgram,
                    cobaltProgram,
                    kerbinNetworkAvailable,
                    munNetworkAvailable,
                    minmusNetworkAvailable,
                    probeOrbitContractLive,
                    crewedOrbitContractLive,
                    munProbeOrbitContractLive,
                    minmusProbeOrbitContractLive,
                    munCrewedOrbitContractLive,
                    minmusCrewedOrbitContractLive);
            }

            while (currentUniversalTime >= program.NextLaunchProgressCheckUniversalTime)
            {
                if (string.IsNullOrEmpty(program.NextLaunchBodyName))
                {
                    program.NextLaunchBodyName = ChooseNextLaunchTarget(
                        program,
                        playerProgram,
                        asterProgram,
                        cobaltProgram,
                        kerbinNetworkAvailable,
                        munNetworkAvailable,
                        minmusNetworkAvailable,
                        probeOrbitContractLive,
                        crewedOrbitContractLive,
                        munProbeOrbitContractLive,
                        minmusProbeOrbitContractLive,
                        munCrewedOrbitContractLive,
                        minmusCrewedOrbitContractLive);
                }

                if (!string.IsNullOrEmpty(program.NextLaunchBodyName))
                {
                    double launchProgressCostFunds = CalculateLaunchProgressCost(program);

                    if (program.LaunchProgressPercent < 100
                        && program.Funds >= launchProgressCostFunds
                        && RandomGenerator.NextDouble() < LaunchProgressChance)
                    {
                        program.Funds -= launchProgressCostFunds;
                        program.LaunchProgressPercent = Math.Min(
                            100,
                            program.LaunchProgressPercent + LaunchProgressIncrementPercent);
                    }

                    if (TryCompleteLaunch(program, program.NextLaunchProgressCheckUniversalTime))
                    {
                        program.NextLaunchBodyName = ChooseNextLaunchTarget(
                            program,
                            playerProgram,
                            asterProgram,
                            cobaltProgram,
                            kerbinNetworkAvailable,
                            munNetworkAvailable,
                            minmusNetworkAvailable,
                            probeOrbitContractLive,
                            crewedOrbitContractLive,
                            munProbeOrbitContractLive,
                            minmusProbeOrbitContractLive,
                            munCrewedOrbitContractLive,
                            minmusCrewedOrbitContractLive);
                    }
                }

                program.NextLaunchProgressCheckUniversalTime += LaunchProgressIntervalSeconds;
            }
        }

        private static bool TryCompleteLaunch(SpaceProgramState program, double completionUniversalTime)
        {
            if (program.LaunchProgressPercent < 100 || string.IsNullOrEmpty(program.NextLaunchBodyName))
            {
                return false;
            }

            if (string.Equals(program.NextLaunchBodyName, ProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                if (!program.HasAchievedProbeOrbit)
                {
                    program.HasAchievedProbeOrbit = true;
                    program.ProbeOrbitAchievementUniversalTime = Math.Max(0.0, completionUniversalTime);

                    // The rival's first Probe Orbit mission represents a real unmanned probe in
                    // Kerbin orbit, matching the player's qualifying probe which satisfies both
                    // the achievement and one satellite-network slot.
                    program.SetSatelliteCount(
                        "Kerbin",
                        program.GetSatelliteCount("Kerbin") + 1);
                }
            }
            else if (string.Equals(program.NextLaunchBodyName, CrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                if (!program.HasAchievedCrewedOrbit)
                {
                    program.HasAchievedCrewedOrbit = true;
                    program.CrewedOrbitAchievementUniversalTime = Math.Max(0.0, completionUniversalTime);
                }
            }
            else if (string.Equals(program.NextLaunchBodyName, MunProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                if (!program.HasAchievedMunProbeOrbit)
                {
                    program.HasAchievedMunProbeOrbit = true;
                    program.MunProbeOrbitAchievementUniversalTime = Math.Max(0.0, completionUniversalTime);
                    program.SetSatelliteCount("Mun", program.GetSatelliteCount("Mun") + 1);
                }
            }
            else if (string.Equals(program.NextLaunchBodyName, MinmusProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                if (!program.HasAchievedMinmusProbeOrbit)
                {
                    program.HasAchievedMinmusProbeOrbit = true;
                    program.MinmusProbeOrbitAchievementUniversalTime = Math.Max(0.0, completionUniversalTime);
                    program.SetSatelliteCount("Minmus", program.GetSatelliteCount("Minmus") + 1);
                }
            }
            else if (string.Equals(program.NextLaunchBodyName, MunCrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                if (!program.HasAchievedMunCrewedOrbit)
                {
                    program.HasAchievedMunCrewedOrbit = true;
                    program.MunCrewedOrbitAchievementUniversalTime = Math.Max(0.0, completionUniversalTime);
                }
            }
            else if (string.Equals(program.NextLaunchBodyName, MinmusCrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                if (!program.HasAchievedMinmusCrewedOrbit)
                {
                    program.HasAchievedMinmusCrewedOrbit = true;
                    program.MinmusCrewedOrbitAchievementUniversalTime = Math.Max(0.0, completionUniversalTime);
                }
            }
            else
            {
                program.SetSatelliteCount(
                    program.NextLaunchBodyName,
                    program.GetSatelliteCount(program.NextLaunchBodyName) + 1);
            }

            program.LaunchProgressPercent = 0;
            program.NextLaunchBodyName = null;
            return true;
        }

        private static bool IsCurrentTargetAvailable(
            SpaceProgramState program,
            SpaceProgramState playerProgram,
            SpaceProgramState asterProgram,
            SpaceProgramState cobaltProgram,
            bool kerbinNetworkAvailable,
            bool munNetworkAvailable,
            bool minmusNetworkAvailable,
            bool probeOrbitContractLive,
            bool crewedOrbitContractLive,
            bool munProbeOrbitContractLive,
            bool minmusProbeOrbitContractLive,
            bool munCrewedOrbitContractLive,
            bool minmusCrewedOrbitContractLive)
        {
            if (program == null || string.IsNullOrEmpty(program.NextLaunchBodyName))
            {
                return true;
            }

            bool lunarCrewedAchievementsAvailable = playerProgram.HasAchievedCrewedOrbit
                || asterProgram.HasAchievedCrewedOrbit
                || cobaltProgram.HasAchievedCrewedOrbit;

            if (string.Equals(program.NextLaunchBodyName, ProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return probeOrbitContractLive && !program.HasAchievedProbeOrbit;
            }

            if (string.Equals(program.NextLaunchBodyName, CrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return crewedOrbitContractLive && !program.HasAchievedCrewedOrbit;
            }

            if (string.Equals(program.NextLaunchBodyName, MunProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return IsKerbinNetworkAvailable(
                        kerbinNetworkAvailable,
                        playerProgram,
                        asterProgram,
                        cobaltProgram)
                    && munProbeOrbitContractLive
                    && !program.HasAchievedMunProbeOrbit;
            }

            if (string.Equals(program.NextLaunchBodyName, MinmusProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return IsKerbinNetworkAvailable(
                        kerbinNetworkAvailable,
                        playerProgram,
                        asterProgram,
                        cobaltProgram)
                    && minmusProbeOrbitContractLive
                    && !program.HasAchievedMinmusProbeOrbit;
            }

            if (string.Equals(program.NextLaunchBodyName, MunCrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return lunarCrewedAchievementsAvailable
                    && munCrewedOrbitContractLive
                    && !program.HasAchievedMunCrewedOrbit;
            }

            if (string.Equals(program.NextLaunchBodyName, MinmusCrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return lunarCrewedAchievementsAvailable
                    && minmusCrewedOrbitContractLive
                    && !program.HasAchievedMinmusCrewedOrbit;
            }

            if (string.Equals(program.NextLaunchBodyName, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                return IsKerbinNetworkAvailable(
                    kerbinNetworkAvailable,
                    playerProgram,
                    asterProgram,
                    cobaltProgram);
            }

            if (string.Equals(program.NextLaunchBodyName, "Mun", StringComparison.OrdinalIgnoreCase))
            {
                return IsMunNetworkAvailable(
                    munNetworkAvailable,
                    playerProgram,
                    asterProgram,
                    cobaltProgram);
            }

            if (string.Equals(program.NextLaunchBodyName, "Minmus", StringComparison.OrdinalIgnoreCase))
            {
                return IsMinmusNetworkAvailable(
                    minmusNetworkAvailable,
                    playerProgram,
                    asterProgram,
                    cobaltProgram);
            }

            return false;
        }

        private static string ChooseNextLaunchTarget(
            SpaceProgramState program,
            SpaceProgramState playerProgram,
            SpaceProgramState asterProgram,
            SpaceProgramState cobaltProgram,
            bool kerbinNetworkAvailable,
            bool munNetworkAvailable,
            bool minmusNetworkAvailable,
            bool probeOrbitContractLive,
            bool crewedOrbitContractLive,
            bool munProbeOrbitContractLive,
            bool minmusProbeOrbitContractLive,
            bool munCrewedOrbitContractLive,
            bool minmusCrewedOrbitContractLive)
        {
            var availableTargets = new List<string>();
            bool lunarProbeAchievementsAvailable = IsKerbinNetworkAvailable(
                kerbinNetworkAvailable,
                playerProgram,
                asterProgram,
                cobaltProgram);
            bool lunarCrewedAchievementsAvailable = playerProgram.HasAchievedCrewedOrbit
                || asterProgram.HasAchievedCrewedOrbit
                || cobaltProgram.HasAchievedCrewedOrbit;

            if (probeOrbitContractLive && !program.HasAchievedProbeOrbit)
            {
                availableTargets.Add(ProbeOrbitTargetName);
            }

            if (crewedOrbitContractLive && !program.HasAchievedCrewedOrbit)
            {
                availableTargets.Add(CrewedOrbitTargetName);
            }

            if (lunarProbeAchievementsAvailable
                && munProbeOrbitContractLive
                && !program.HasAchievedMunProbeOrbit)
            {
                availableTargets.Add(MunProbeOrbitTargetName);
            }

            if (lunarProbeAchievementsAvailable
                && minmusProbeOrbitContractLive
                && !program.HasAchievedMinmusProbeOrbit)
            {
                availableTargets.Add(MinmusProbeOrbitTargetName);
            }

            if (lunarCrewedAchievementsAvailable
                && munCrewedOrbitContractLive
                && !program.HasAchievedMunCrewedOrbit)
            {
                availableTargets.Add(MunCrewedOrbitTargetName);
            }

            if (lunarCrewedAchievementsAvailable
                && minmusCrewedOrbitContractLive
                && !program.HasAchievedMinmusCrewedOrbit)
            {
                availableTargets.Add(MinmusCrewedOrbitTargetName);
            }

            if (IsKerbinNetworkAvailable(kerbinNetworkAvailable, playerProgram, asterProgram, cobaltProgram))
            {
                availableTargets.Add("Kerbin");
            }

            if (IsMunNetworkAvailable(munNetworkAvailable, playerProgram, asterProgram, cobaltProgram))
            {
                availableTargets.Add("Mun");
            }

            if (IsMinmusNetworkAvailable(minmusNetworkAvailable, playerProgram, asterProgram, cobaltProgram))
            {
                availableTargets.Add("Minmus");
            }

            if (availableTargets.Count == 0)
            {
                return null;
            }

            return availableTargets[RandomGenerator.Next(availableTargets.Count)];
        }

        private static bool IsKerbinNetworkAvailable(
            bool savedAvailability,
            SpaceProgramState playerProgram,
            SpaceProgramState asterProgram,
            SpaceProgramState cobaltProgram)
        {
            return savedAvailability
                || playerProgram.HasAchievedProbeOrbit
                || asterProgram.HasAchievedProbeOrbit
                || cobaltProgram.HasAchievedProbeOrbit;
        }

        private static bool IsMunNetworkAvailable(
            bool savedAvailability,
            SpaceProgramState playerProgram,
            SpaceProgramState asterProgram,
            SpaceProgramState cobaltProgram)
        {
            return savedAvailability
                || playerProgram.HasAchievedMunProbeOrbit
                || asterProgram.HasAchievedMunProbeOrbit
                || cobaltProgram.HasAchievedMunProbeOrbit;
        }

        private static bool IsMinmusNetworkAvailable(
            bool savedAvailability,
            SpaceProgramState playerProgram,
            SpaceProgramState asterProgram,
            SpaceProgramState cobaltProgram)
        {
            return savedAvailability
                || playerProgram.HasAchievedMinmusProbeOrbit
                || asterProgram.HasAchievedMinmusProbeOrbit
                || cobaltProgram.HasAchievedMinmusProbeOrbit;
        }
    }
}

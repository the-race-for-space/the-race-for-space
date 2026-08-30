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
        private const double LaunchProgressChance = 0.30;
        private const double SatelliteMissionSelectionChance = 0.60;
        private const int LaunchProgressIncrementPercent = 10;
        private const double LaunchProgressCostFunds = 20000.0;
        private const double MunMinmusLaunchProgressCostMultiplier = 2.0;
        private const double CrewedOrbitLaunchProgressCostMultiplier = 2.0;

        private static readonly Random RandomGenerator = new Random();

        private struct RivalSimulationContext
        {
            public RivalSimulationContext(
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
                PlayerProgram = playerProgram;
                AsterProgram = asterProgram;
                CobaltProgram = cobaltProgram;
                CurrentUniversalTime = currentUniversalTime;
                KerbinNetworkAvailable = kerbinNetworkAvailable;
                MunNetworkAvailable = munNetworkAvailable;
                MinmusNetworkAvailable = minmusNetworkAvailable;
                ProbeOrbitContractLive = probeOrbitContractLive;
                CrewedOrbitContractLive = crewedOrbitContractLive;
                MunProbeOrbitContractLive = munProbeOrbitContractLive;
                MinmusProbeOrbitContractLive = minmusProbeOrbitContractLive;
                MunCrewedOrbitContractLive = munCrewedOrbitContractLive;
                MinmusCrewedOrbitContractLive = minmusCrewedOrbitContractLive;
            }

            public readonly SpaceProgramState PlayerProgram;
            public readonly SpaceProgramState AsterProgram;
            public readonly SpaceProgramState CobaltProgram;
            public readonly double CurrentUniversalTime;
            public readonly bool KerbinNetworkAvailable;
            public readonly bool MunNetworkAvailable;
            public readonly bool MinmusNetworkAvailable;
            public readonly bool ProbeOrbitContractLive;
            public readonly bool CrewedOrbitContractLive;
            public readonly bool MunProbeOrbitContractLive;
            public readonly bool MinmusProbeOrbitContractLive;
            public readonly bool MunCrewedOrbitContractLive;
            public readonly bool MinmusCrewedOrbitContractLive;
        }

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

            // Keep the public prototype call shape stable while passing one immutable snapshot
            // through the private simulation methods instead of repeating the full argument list.
            var context = new RivalSimulationContext(
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

            RefreshProgram(asterProgram, context);
            RefreshProgram(cobaltProgram, context);
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
            RivalSimulationContext context)
        {
            if (!IsTargetAvailable(program.NextLaunchBodyName, program, context))
            {
                // If an achievement contract expires or becomes unavailable before this rival
                // completes it, abandon that development rather than spending simulated funds
                // on an objective that is not currently in play.
                program.NextLaunchBodyName = null;
                program.LaunchProgressPercent = 0;
            }

            if (string.IsNullOrEmpty(program.NextLaunchBodyName))
            {
                program.NextLaunchBodyName = ChooseNextLaunchTarget(program, context);
            }

            if (program.NextLaunchProgressCheckUniversalTime <= 0.0)
            {
                // Align checks to the next five-day Kerbin calendar boundary so each rival
                // advances on a predictable cadence even when the controller starts mid-save.
                program.NextLaunchProgressCheckUniversalTime =
                    (Math.Floor(context.CurrentUniversalTime / LaunchProgressIntervalSeconds) + 1.0)
                    * LaunchProgressIntervalSeconds;
            }

            if (TryCompleteLaunch(program, context.CurrentUniversalTime))
            {
                program.NextLaunchBodyName = ChooseNextLaunchTarget(program, context);
            }

            while (context.CurrentUniversalTime >= program.NextLaunchProgressCheckUniversalTime)
            {
                if (string.IsNullOrEmpty(program.NextLaunchBodyName))
                {
                    program.NextLaunchBodyName = ChooseNextLaunchTarget(program, context);
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
                        program.NextLaunchBodyName = ChooseNextLaunchTarget(program, context);
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

        /// <summary>
        /// Keeps the availability rules for saved/current targets and newly selected targets
        /// identical. The target list remains deliberately explicit for the narrow 0.3 prototype.
        /// </summary>
        private static bool IsTargetAvailable(
            string targetName,
            SpaceProgramState program,
            RivalSimulationContext context)
        {
            if (program == null || string.IsNullOrEmpty(targetName))
            {
                return true;
            }

            bool lunarCrewedAchievementsAvailable = context.PlayerProgram.HasAchievedCrewedOrbit
                || context.AsterProgram.HasAchievedCrewedOrbit
                || context.CobaltProgram.HasAchievedCrewedOrbit;

            if (string.Equals(targetName, ProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return context.ProbeOrbitContractLive && !program.HasAchievedProbeOrbit;
            }

            if (string.Equals(targetName, CrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return context.CrewedOrbitContractLive && !program.HasAchievedCrewedOrbit;
            }

            if (string.Equals(targetName, MunProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return IsKerbinNetworkAvailable(context)
                    && context.MunProbeOrbitContractLive
                    && !program.HasAchievedMunProbeOrbit;
            }

            if (string.Equals(targetName, MinmusProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return IsKerbinNetworkAvailable(context)
                    && context.MinmusProbeOrbitContractLive
                    && !program.HasAchievedMinmusProbeOrbit;
            }

            if (string.Equals(targetName, MunCrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return lunarCrewedAchievementsAvailable
                    && context.MunCrewedOrbitContractLive
                    && !program.HasAchievedMunCrewedOrbit;
            }

            if (string.Equals(targetName, MinmusCrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return lunarCrewedAchievementsAvailable
                    && context.MinmusCrewedOrbitContractLive
                    && !program.HasAchievedMinmusCrewedOrbit;
            }

            if (string.Equals(targetName, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                return IsKerbinNetworkAvailable(context);
            }

            if (string.Equals(targetName, "Mun", StringComparison.OrdinalIgnoreCase))
            {
                return IsMunNetworkAvailable(context);
            }

            if (string.Equals(targetName, "Minmus", StringComparison.OrdinalIgnoreCase))
            {
                return IsMinmusNetworkAvailable(context);
            }

            return false;
        }

        private static string ChooseNextLaunchTarget(
            SpaceProgramState program,
            RivalSimulationContext context)
        {
            var availableOneOffTargets = new List<string>();
            var availableSatelliteTargets = new List<string>();

            if (IsTargetAvailable(ProbeOrbitTargetName, program, context))
            {
                availableOneOffTargets.Add(ProbeOrbitTargetName);
            }

            if (IsTargetAvailable(CrewedOrbitTargetName, program, context))
            {
                availableOneOffTargets.Add(CrewedOrbitTargetName);
            }

            if (IsTargetAvailable(MunProbeOrbitTargetName, program, context))
            {
                availableOneOffTargets.Add(MunProbeOrbitTargetName);
            }

            if (IsTargetAvailable(MinmusProbeOrbitTargetName, program, context))
            {
                availableOneOffTargets.Add(MinmusProbeOrbitTargetName);
            }

            if (IsTargetAvailable(MunCrewedOrbitTargetName, program, context))
            {
                availableOneOffTargets.Add(MunCrewedOrbitTargetName);
            }

            if (IsTargetAvailable(MinmusCrewedOrbitTargetName, program, context))
            {
                availableOneOffTargets.Add(MinmusCrewedOrbitTargetName);
            }

            if (IsTargetAvailable("Kerbin", program, context))
            {
                availableSatelliteTargets.Add("Kerbin");
            }

            if (IsTargetAvailable("Mun", program, context))
            {
                availableSatelliteTargets.Add("Mun");
            }

            if (IsTargetAvailable("Minmus", program, context))
            {
                availableSatelliteTargets.Add("Minmus");
            }

            if (availableOneOffTargets.Count == 0 && availableSatelliteTargets.Count == 0)
            {
                return null;
            }

            List<string> selectedTargetType;
            if (availableSatelliteTargets.Count == 0)
            {
                selectedTargetType = availableOneOffTargets;
            }
            else if (availableOneOffTargets.Count == 0)
            {
                selectedTargetType = availableSatelliteTargets;
            }
            else
            {
                // Mission type is selected first so repeatable satellite work has a stable 60%
                // share regardless of how many one-off contracts happen to be live at the time.
                selectedTargetType = RandomGenerator.NextDouble() < SatelliteMissionSelectionChance
                    ? availableSatelliteTargets
                    : availableOneOffTargets;
            }

            return selectedTargetType[RandomGenerator.Next(selectedTargetType.Count)];
        }

        private static bool IsKerbinNetworkAvailable(RivalSimulationContext context)
        {
            return context.KerbinNetworkAvailable
                || context.PlayerProgram.HasAchievedProbeOrbit
                || context.AsterProgram.HasAchievedProbeOrbit
                || context.CobaltProgram.HasAchievedProbeOrbit;
        }

        private static bool IsMunNetworkAvailable(RivalSimulationContext context)
        {
            return context.MunNetworkAvailable
                || context.PlayerProgram.HasAchievedMunProbeOrbit
                || context.AsterProgram.HasAchievedMunProbeOrbit
                || context.CobaltProgram.HasAchievedMunProbeOrbit;
        }

        private static bool IsMinmusNetworkAvailable(RivalSimulationContext context)
        {
            return context.MinmusNetworkAvailable
                || context.PlayerProgram.HasAchievedMinmusProbeOrbit
                || context.AsterProgram.HasAchievedMinmusProbeOrbit
                || context.CobaltProgram.HasAchievedMinmusProbeOrbit;
        }
    }
}

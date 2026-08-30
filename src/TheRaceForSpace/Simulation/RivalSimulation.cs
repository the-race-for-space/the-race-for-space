using System;
using System.Collections.Generic;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Simulation
{
    /// <summary>
    /// Lightweight rival mission simulation used by the current prototype.
    /// </summary>
    public static class RivalSimulation
    {
        // Satellite mission IDs deliberately match the funding-programme IDs so later rival
        // selection can consume the same programme collection without translating identities.
        public const string KerbinSatelliteTargetId = "kerbin-network";
        public const string MunSatelliteTargetId = "mun-survey";
        public const string MinmusSatelliteTargetId = "minmus-relay";

        // These names remain for the current save format and UI compatibility only. Rival
        // simulation decisions use stable IDs rather than comparing presentation strings.
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
        /// Returns the display text for a stable rival mission target ID, or null for an unknown ID.
        /// </summary>
        public static string GetMissionTargetDisplayName(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                return null;
            }

            MilestoneDefinition milestone = PrototypeMilestones.FindById(targetId);
            if (milestone != null)
            {
                return milestone.Name;
            }

            if (string.Equals(targetId, KerbinSatelliteTargetId, StringComparison.OrdinalIgnoreCase))
            {
                return "Kerbin";
            }

            if (string.Equals(targetId, MunSatelliteTargetId, StringComparison.OrdinalIgnoreCase))
            {
                return "Mun";
            }

            if (string.Equals(targetId, MinmusSatelliteTargetId, StringComparison.OrdinalIgnoreCase))
            {
                return "Minmus";
            }

            return null;
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

            string targetId = SynchronizeMissionTargetIdentity(program);

            if (string.Equals(targetId, PrototypeMilestones.CrewedOrbitId, StringComparison.OrdinalIgnoreCase))
            {
                return LaunchProgressCostFunds * CrewedOrbitLaunchProgressCostMultiplier;
            }

            if (string.Equals(targetId, PrototypeMilestones.MunProbeOrbitId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(targetId, PrototypeMilestones.MinmusProbeOrbitId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(targetId, PrototypeMilestones.MunCrewedOrbitId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(targetId, PrototypeMilestones.MinmusCrewedOrbitId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(targetId, MunSatelliteTargetId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(targetId, MinmusSatelliteTargetId, StringComparison.OrdinalIgnoreCase))
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
            bool hadStoredTarget = !string.IsNullOrEmpty(program.NextMissionTargetId)
                || !string.IsNullOrEmpty(program.NextLaunchBodyName);
            string targetId = SynchronizeMissionTargetIdentity(program);

            if ((hadStoredTarget && string.IsNullOrEmpty(targetId))
                || !IsTargetAvailable(targetId, program, context))
            {
                // Invalid legacy targets and targets whose contracts have expired are abandoned
                // before any more simulated funds can be spent on them.
                SetMissionTarget(program, null);
                program.LaunchProgressPercent = 0;
            }

            if (string.IsNullOrEmpty(program.NextMissionTargetId))
            {
                SetMissionTarget(program, ChooseNextLaunchTarget(program, context));
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
                SetMissionTarget(program, ChooseNextLaunchTarget(program, context));
            }

            while (context.CurrentUniversalTime >= program.NextLaunchProgressCheckUniversalTime)
            {
                if (string.IsNullOrEmpty(program.NextMissionTargetId))
                {
                    SetMissionTarget(program, ChooseNextLaunchTarget(program, context));
                }

                if (!string.IsNullOrEmpty(program.NextMissionTargetId))
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
                        SetMissionTarget(program, ChooseNextLaunchTarget(program, context));
                    }
                }

                program.NextLaunchProgressCheckUniversalTime += LaunchProgressIntervalSeconds;
            }
        }

        private static bool TryCompleteLaunch(SpaceProgramState program, double completionUniversalTime)
        {
            string targetId = SynchronizeMissionTargetIdentity(program);
            if (program.LaunchProgressPercent < 100 || string.IsNullOrEmpty(targetId))
            {
                return false;
            }

            if (string.Equals(targetId, PrototypeMilestones.ProbeOrbitId, StringComparison.OrdinalIgnoreCase))
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
            else if (string.Equals(targetId, PrototypeMilestones.CrewedOrbitId, StringComparison.OrdinalIgnoreCase))
            {
                if (!program.HasAchievedCrewedOrbit)
                {
                    program.HasAchievedCrewedOrbit = true;
                    program.CrewedOrbitAchievementUniversalTime = Math.Max(0.0, completionUniversalTime);
                }
            }
            else if (string.Equals(targetId, PrototypeMilestones.MunProbeOrbitId, StringComparison.OrdinalIgnoreCase))
            {
                if (!program.HasAchievedMunProbeOrbit)
                {
                    program.HasAchievedMunProbeOrbit = true;
                    program.MunProbeOrbitAchievementUniversalTime = Math.Max(0.0, completionUniversalTime);
                    program.SetSatelliteCount("Mun", program.GetSatelliteCount("Mun") + 1);
                }
            }
            else if (string.Equals(targetId, PrototypeMilestones.MinmusProbeOrbitId, StringComparison.OrdinalIgnoreCase))
            {
                if (!program.HasAchievedMinmusProbeOrbit)
                {
                    program.HasAchievedMinmusProbeOrbit = true;
                    program.MinmusProbeOrbitAchievementUniversalTime = Math.Max(0.0, completionUniversalTime);
                    program.SetSatelliteCount("Minmus", program.GetSatelliteCount("Minmus") + 1);
                }
            }
            else if (string.Equals(targetId, PrototypeMilestones.MunCrewedOrbitId, StringComparison.OrdinalIgnoreCase))
            {
                if (!program.HasAchievedMunCrewedOrbit)
                {
                    program.HasAchievedMunCrewedOrbit = true;
                    program.MunCrewedOrbitAchievementUniversalTime = Math.Max(0.0, completionUniversalTime);
                }
            }
            else if (string.Equals(targetId, PrototypeMilestones.MinmusCrewedOrbitId, StringComparison.OrdinalIgnoreCase))
            {
                if (!program.HasAchievedMinmusCrewedOrbit)
                {
                    program.HasAchievedMinmusCrewedOrbit = true;
                    program.MinmusCrewedOrbitAchievementUniversalTime = Math.Max(0.0, completionUniversalTime);
                }
            }
            else if (string.Equals(targetId, KerbinSatelliteTargetId, StringComparison.OrdinalIgnoreCase))
            {
                program.SetSatelliteCount("Kerbin", program.GetSatelliteCount("Kerbin") + 1);
            }
            else if (string.Equals(targetId, MunSatelliteTargetId, StringComparison.OrdinalIgnoreCase))
            {
                program.SetSatelliteCount("Mun", program.GetSatelliteCount("Mun") + 1);
            }
            else if (string.Equals(targetId, MinmusSatelliteTargetId, StringComparison.OrdinalIgnoreCase))
            {
                program.SetSatelliteCount("Minmus", program.GetSatelliteCount("Minmus") + 1);
            }
            else
            {
                return false;
            }

            program.LaunchProgressPercent = 0;
            SetMissionTarget(program, null);
            return true;
        }

        /// <summary>
        /// Keeps the availability rules for saved/current targets and newly selected targets
        /// identical. The target list remains deliberately explicit for the narrow 0.3 prototype.
        /// </summary>
        private static bool IsTargetAvailable(
            string targetId,
            SpaceProgramState program,
            RivalSimulationContext context)
        {
            if (program == null || string.IsNullOrEmpty(targetId))
            {
                return true;
            }

            bool lunarCrewedAchievementsAvailable = context.PlayerProgram.HasAchievedCrewedOrbit
                || context.AsterProgram.HasAchievedCrewedOrbit
                || context.CobaltProgram.HasAchievedCrewedOrbit;

            if (string.Equals(targetId, PrototypeMilestones.ProbeOrbitId, StringComparison.OrdinalIgnoreCase))
            {
                return context.ProbeOrbitContractLive && !program.HasAchievedProbeOrbit;
            }

            if (string.Equals(targetId, PrototypeMilestones.CrewedOrbitId, StringComparison.OrdinalIgnoreCase))
            {
                return context.CrewedOrbitContractLive && !program.HasAchievedCrewedOrbit;
            }

            if (string.Equals(targetId, PrototypeMilestones.MunProbeOrbitId, StringComparison.OrdinalIgnoreCase))
            {
                return IsKerbinNetworkAvailable(context)
                    && context.MunProbeOrbitContractLive
                    && !program.HasAchievedMunProbeOrbit;
            }

            if (string.Equals(targetId, PrototypeMilestones.MinmusProbeOrbitId, StringComparison.OrdinalIgnoreCase))
            {
                return IsKerbinNetworkAvailable(context)
                    && context.MinmusProbeOrbitContractLive
                    && !program.HasAchievedMinmusProbeOrbit;
            }

            if (string.Equals(targetId, PrototypeMilestones.MunCrewedOrbitId, StringComparison.OrdinalIgnoreCase))
            {
                return lunarCrewedAchievementsAvailable
                    && context.MunCrewedOrbitContractLive
                    && !program.HasAchievedMunCrewedOrbit;
            }

            if (string.Equals(targetId, PrototypeMilestones.MinmusCrewedOrbitId, StringComparison.OrdinalIgnoreCase))
            {
                return lunarCrewedAchievementsAvailable
                    && context.MinmusCrewedOrbitContractLive
                    && !program.HasAchievedMinmusCrewedOrbit;
            }

            if (string.Equals(targetId, KerbinSatelliteTargetId, StringComparison.OrdinalIgnoreCase))
            {
                return IsKerbinNetworkAvailable(context);
            }

            if (string.Equals(targetId, MunSatelliteTargetId, StringComparison.OrdinalIgnoreCase))
            {
                return IsMunNetworkAvailable(context);
            }

            if (string.Equals(targetId, MinmusSatelliteTargetId, StringComparison.OrdinalIgnoreCase))
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

            if (IsTargetAvailable(PrototypeMilestones.ProbeOrbitId, program, context))
            {
                availableOneOffTargets.Add(PrototypeMilestones.ProbeOrbitId);
            }

            if (IsTargetAvailable(PrototypeMilestones.CrewedOrbitId, program, context))
            {
                availableOneOffTargets.Add(PrototypeMilestones.CrewedOrbitId);
            }

            if (IsTargetAvailable(PrototypeMilestones.MunProbeOrbitId, program, context))
            {
                availableOneOffTargets.Add(PrototypeMilestones.MunProbeOrbitId);
            }

            if (IsTargetAvailable(PrototypeMilestones.MinmusProbeOrbitId, program, context))
            {
                availableOneOffTargets.Add(PrototypeMilestones.MinmusProbeOrbitId);
            }

            if (IsTargetAvailable(PrototypeMilestones.MunCrewedOrbitId, program, context))
            {
                availableOneOffTargets.Add(PrototypeMilestones.MunCrewedOrbitId);
            }

            if (IsTargetAvailable(PrototypeMilestones.MinmusCrewedOrbitId, program, context))
            {
                availableOneOffTargets.Add(PrototypeMilestones.MinmusCrewedOrbitId);
            }

            if (IsTargetAvailable(KerbinSatelliteTargetId, program, context))
            {
                availableSatelliteTargets.Add(KerbinSatelliteTargetId);
            }

            if (IsTargetAvailable(MunSatelliteTargetId, program, context))
            {
                availableSatelliteTargets.Add(MunSatelliteTargetId);
            }

            if (IsTargetAvailable(MinmusSatelliteTargetId, program, context))
            {
                availableSatelliteTargets.Add(MinmusSatelliteTargetId);
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

        private static string SynchronizeMissionTargetIdentity(SpaceProgramState program)
        {
            if (program == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(program.NextMissionTargetId))
            {
                // Stable IDs are authoritative. If the current save/display mirror disagrees,
                // regenerate it rather than allowing presentation text to change simulation state.
                program.NextLaunchBodyName = GetMissionTargetDisplayName(program.NextMissionTargetId);
                return program.NextMissionTargetId;
            }

            string targetId = ResolveLegacyMissionTargetId(program.NextLaunchBodyName);
            if (!string.IsNullOrEmpty(targetId))
            {
                program.NextMissionTargetId = targetId;
                program.NextLaunchBodyName = GetMissionTargetDisplayName(targetId);
            }

            return targetId;
        }

        private static string ResolveLegacyMissionTargetId(string legacyTargetName)
        {
            if (string.IsNullOrEmpty(legacyTargetName))
            {
                return null;
            }

            MilestoneDefinition milestone = PrototypeMilestones.FindById(legacyTargetName);
            if (milestone != null)
            {
                return milestone.Id;
            }

            if (string.Equals(legacyTargetName, ProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return PrototypeMilestones.ProbeOrbitId;
            }

            if (string.Equals(legacyTargetName, CrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return PrototypeMilestones.CrewedOrbitId;
            }

            if (string.Equals(legacyTargetName, MunProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return PrototypeMilestones.MunProbeOrbitId;
            }

            if (string.Equals(legacyTargetName, MinmusProbeOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return PrototypeMilestones.MinmusProbeOrbitId;
            }

            if (string.Equals(legacyTargetName, MunCrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return PrototypeMilestones.MunCrewedOrbitId;
            }

            if (string.Equals(legacyTargetName, MinmusCrewedOrbitTargetName, StringComparison.OrdinalIgnoreCase))
            {
                return PrototypeMilestones.MinmusCrewedOrbitId;
            }

            if (string.Equals(legacyTargetName, KerbinSatelliteTargetId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(legacyTargetName, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                return KerbinSatelliteTargetId;
            }

            if (string.Equals(legacyTargetName, MunSatelliteTargetId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(legacyTargetName, "Mun", StringComparison.OrdinalIgnoreCase))
            {
                return MunSatelliteTargetId;
            }

            if (string.Equals(legacyTargetName, MinmusSatelliteTargetId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(legacyTargetName, "Minmus", StringComparison.OrdinalIgnoreCase))
            {
                return MinmusSatelliteTargetId;
            }

            return null;
        }

        private static void SetMissionTarget(SpaceProgramState program, string targetId)
        {
            if (program == null)
            {
                return;
            }

            program.NextMissionTargetId = targetId;
            program.NextLaunchBodyName = GetMissionTargetDisplayName(targetId);
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

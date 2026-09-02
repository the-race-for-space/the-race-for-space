using System;
using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Simulation
{
    /// <summary>
    /// Lightweight rival mission simulation used by the current prototype.
    /// </summary>
    public static class RivalSimulation
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double LaunchProgressIntervalSeconds = 5.0 * KerbinDaySeconds;
        private const double LaunchProgressChance = 0.30;
        private const double SatelliteMissionSelectionChance = 0.60;
        private const int LaunchProgressIncrementPercent = 10;
        private const double KerbinProbeLaunchProgressCostFunds = 20000.0;
        private const double KerbinCrewedLaunchProgressCostFunds = 40000.0;
        private const double KerbinMoonProbeLaunchProgressCostFunds = 40000.0;
        private const double KerbinMoonCrewedLaunchProgressCostFunds = 60000.0;
        private const double InterplanetaryProbeLaunchProgressCostFunds = 60000.0;
        private const double InterplanetaryCrewedLaunchProgressCostFunds = 100000.0;
        private const double KerbinNetworkLaunchProgressCostFunds = 20000.0;
        private const double KerbinMoonNetworkLaunchProgressCostFunds = 40000.0;
        private const double InterplanetaryNetworkLaunchProgressCostFunds = 80000.0;

        private static readonly Random RandomGenerator = new Random();

        private struct RivalSimulationContext
        {
            public RivalSimulationContext(
                IList<SpaceProgramState> programs,
                double currentUniversalTime,
                IList<AchievementFundingProgramme> achievementProgrammes,
                IList<FundingProgramme> fundingProgrammes)
            {
                Programs = programs;
                CurrentUniversalTime = currentUniversalTime;
                AchievementProgrammes = achievementProgrammes;
                FundingProgrammes = fundingProgrammes;
            }

            public readonly IList<SpaceProgramState> Programs;
            public readonly double CurrentUniversalTime;
            public readonly IList<AchievementFundingProgramme> AchievementProgrammes;
            public readonly IList<FundingProgramme> FundingProgrammes;
        }

        /// <summary>
        /// Advances every non-player program in the supplied collection. Target availability
        /// checks use the shared campaign unlock evaluator and the supplied historical time, so
        /// adding another rival or target does not require another simulation-specific rule path.
        /// </summary>
        public static void Refresh(
            IList<SpaceProgramState> programs,
            double currentUniversalTime,
            IList<AchievementFundingProgramme> achievementProgrammes,
            IList<FundingProgramme> fundingProgrammes)
        {
            if (programs == null || achievementProgrammes == null || fundingProgrammes == null)
            {
                return;
            }

            var context = new RivalSimulationContext(
                programs,
                currentUniversalTime,
                achievementProgrammes,
                fundingProgrammes);

            for (int programIndex = 0; programIndex < programs.Count; programIndex++)
            {
                SpaceProgramState program = programs[programIndex];
                if (program == null || program.IsPlayer)
                {
                    continue;
                }

                RefreshProgram(program, context);
            }
        }

        /// <summary>
        /// Returns presentation text for a stable mission target ID using the live programme
        /// collections. Mission identity is never inferred from this display text.
        /// </summary>
        public static string GetMissionTargetDisplayName(
            string targetId,
            IList<AchievementFundingProgramme> achievementProgrammes,
            IList<FundingProgramme> fundingProgrammes)
        {
            AchievementFundingProgramme achievementProgramme = FindAchievementProgramme(
                targetId,
                achievementProgrammes);
            if (achievementProgramme != null)
            {
                return achievementProgramme.Name;
            }

            FundingProgramme fundingProgramme = FindFundingProgramme(targetId, fundingProgrammes);
            return fundingProgramme == null ? null : fundingProgramme.CelestialBodyName;
        }

        /// <summary>
        /// Returns the funds required for the rival's next successful 10% mission-progress step.
        /// Stable mission target IDs are authoritative; presentation text is not used as a fallback.
        /// </summary>
        public static double CalculateLaunchProgressCost(
            SpaceProgramState program,
            IList<AchievementFundingProgramme> achievementProgrammes,
            IList<FundingProgramme> fundingProgrammes)
        {
            if (program == null)
            {
                return KerbinProbeLaunchProgressCostFunds;
            }

            return CalculateLaunchProgressCostForTarget(
                program.NextMissionTargetId,
                fundingProgrammes);
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
            double fundingIntervalSeconds,
            IList<AchievementFundingProgramme> achievementProgrammes,
            IList<FundingProgramme> fundingProgrammes)
        {
            return CalculateEstimatedLaunchDays(
                program,
                currentUniversalTime,
                nextFundingUniversalTime,
                fundingIntervalSeconds,
                CalculateLaunchProgressCost(program, achievementProgrammes, fundingProgrammes));
        }

        private static int? CalculateEstimatedLaunchDays(
            SpaceProgramState program,
            double currentUniversalTime,
            double nextFundingUniversalTime,
            double fundingIntervalSeconds,
            double launchProgressCostFunds)
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
            string targetId = program.NextMissionTargetId;
            if (!string.IsNullOrEmpty(targetId))
            {
                // Stable IDs are authoritative. Keep the display mirror synchronized from the
                // live definitions rather than allowing presentation text to define simulation state.
                program.NextMissionDisplayName = GetMissionTargetDisplayName(
                    targetId,
                    context.AchievementProgrammes,
                    context.FundingProgrammes);
            }

            if (!IsTargetAvailable(targetId, program, context))
            {
                // Invalid saved targets and targets whose contracts have expired are abandoned
                // before any more simulated funds can be spent on them.
                SetMissionTarget(program, null, context);
                program.LaunchProgressPercent = 0;
            }

            if (string.IsNullOrEmpty(program.NextMissionTargetId))
            {
                SetMissionTarget(program, ChooseNextLaunchTarget(program, context), context);
            }

            if (program.NextLaunchProgressCheckUniversalTime <= 0.0)
            {
                // Align checks to the next five-day Kerbin calendar boundary so each rival
                // advances on a predictable cadence even when the controller starts mid-save.
                program.NextLaunchProgressCheckUniversalTime =
                    (Math.Floor(context.CurrentUniversalTime / LaunchProgressIntervalSeconds) + 1.0)
                    * LaunchProgressIntervalSeconds;
            }

            if (TryCompleteLaunch(program, context.CurrentUniversalTime, context))
            {
                SetMissionTarget(program, ChooseNextLaunchTarget(program, context), context);
            }

            while (context.CurrentUniversalTime >= program.NextLaunchProgressCheckUniversalTime)
            {
                if (string.IsNullOrEmpty(program.NextMissionTargetId))
                {
                    SetMissionTarget(program, ChooseNextLaunchTarget(program, context), context);
                }

                if (!string.IsNullOrEmpty(program.NextMissionTargetId))
                {
                    double launchProgressCostFunds = CalculateLaunchProgressCost(
                        program,
                        context.AchievementProgrammes,
                        context.FundingProgrammes);

                    if (program.LaunchProgressPercent < 100
                        && program.Funds >= launchProgressCostFunds
                        && RandomGenerator.NextDouble() < LaunchProgressChance)
                    {
                        program.Funds -= launchProgressCostFunds;
                        program.LaunchProgressPercent = Math.Min(
                            100,
                            program.LaunchProgressPercent + LaunchProgressIncrementPercent);
                    }

                    if (TryCompleteLaunch(program, program.NextLaunchProgressCheckUniversalTime, context))
                    {
                        SetMissionTarget(program, ChooseNextLaunchTarget(program, context), context);
                    }
                }

                program.NextLaunchProgressCheckUniversalTime += LaunchProgressIntervalSeconds;
            }
        }

        private static bool TryCompleteLaunch(
            SpaceProgramState program,
            double completionUniversalTime,
            RivalSimulationContext context)
        {
            string targetId = program.NextMissionTargetId;
            if (program.LaunchProgressPercent < 100 || string.IsNullOrEmpty(targetId))
            {
                return false;
            }

            AchievementFundingProgramme achievementProgramme = FindAchievementProgramme(
                targetId,
                context.AchievementProgrammes);
            if (achievementProgramme != null)
            {
                MilestoneDefinition milestone = PrototypeMilestones.FindById(achievementProgramme.Id);
                if (milestone == null)
                {
                    return false;
                }

                bool recordedAchievement = program.RecordAchievement(
                    milestone.Id,
                    Math.Max(0.0, completionUniversalTime));
                if (recordedAchievement
                    && milestone.CrewRequirement == MilestoneCrewRequirement.UncrewedProbe)
                {
                    // An uncrewed achievement mission represents a real probe occupying one
                    // satellite slot around the milestone body, matching player tracking rules.
                    program.SetSatelliteCount(
                        milestone.CelestialBodyName,
                        program.GetSatelliteCount(milestone.CelestialBodyName) + 1);
                }
            }
            else
            {
                FundingProgramme fundingProgramme = FindFundingProgramme(
                    targetId,
                    context.FundingProgrammes);
                if (fundingProgramme == null || string.IsNullOrEmpty(fundingProgramme.CelestialBodyName))
                {
                    return false;
                }

                program.SetSatelliteCount(
                    fundingProgramme.CelestialBodyName,
                    program.GetSatelliteCount(fundingProgramme.CelestialBodyName) + 1);
            }

            program.LaunchProgressPercent = 0;
            SetMissionTarget(program, null, context);
            return true;
        }

        private static bool IsTargetAvailable(
            string targetId,
            SpaceProgramState program,
            RivalSimulationContext context)
        {
            if (program == null || string.IsNullOrEmpty(targetId))
            {
                return true;
            }

            AchievementFundingProgramme achievementProgramme = FindAchievementProgramme(
                targetId,
                context.AchievementProgrammes);
            if (achievementProgramme != null)
            {
                if (achievementProgramme.IsExpired || program.HasAchievement(achievementProgramme.Id))
                {
                    return false;
                }

                return UnlockRuleEvaluator.IsSatisfied(
                    achievementProgramme.UnlockRule,
                    context.Programs,
                    context.CurrentUniversalTime);
            }

            FundingProgramme fundingProgramme = FindFundingProgramme(targetId, context.FundingProgrammes);
            if (fundingProgramme != null)
            {
                return fundingProgramme.IsAvailable
                    || UnlockRuleEvaluator.IsSatisfied(
                        fundingProgramme.UnlockRule,
                        context.Programs,
                        context.CurrentUniversalTime);
            }

            return false;
        }

        private static string ChooseNextLaunchTarget(
            SpaceProgramState program,
            RivalSimulationContext context)
        {
            var availableOneOffTargets = new List<string>();
            var availableSatelliteTargets = new List<string>();

            for (int programmeIndex = 0;
                programmeIndex < context.AchievementProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme = context.AchievementProgrammes[programmeIndex];
                if (programme == null
                    || PrototypeMilestones.FindById(programme.Id) == null
                    || !IsTargetAvailable(programme.Id, program, context))
                {
                    continue;
                }

                availableOneOffTargets.Add(programme.Id);
            }

            for (int programmeIndex = 0; programmeIndex < context.FundingProgrammes.Count; programmeIndex++)
            {
                FundingProgramme programme = context.FundingProgrammes[programmeIndex];
                if (programme == null
                    || string.IsNullOrEmpty(programme.Id)
                    || string.IsNullOrEmpty(programme.CelestialBodyName)
                    || !IsTargetAvailable(programme.Id, program, context))
                {
                    continue;
                }

                availableSatelliteTargets.Add(programme.Id);
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
                // Mission type is selected first so repeatable satellite work keeps a stable 60%
                // share regardless of how many one-off contracts are live at the time.
                selectedTargetType = RandomGenerator.NextDouble() < SatelliteMissionSelectionChance
                    ? availableSatelliteTargets
                    : availableOneOffTargets;
            }

            return selectedTargetType[RandomGenerator.Next(selectedTargetType.Count)];
        }

        private static double CalculateLaunchProgressCostForTarget(
            string targetId,
            IList<FundingProgramme> fundingProgrammes)
        {
            MilestoneDefinition milestone = PrototypeMilestones.FindById(targetId);
            if (milestone != null)
            {
                bool isKerbin = string.Equals(
                    milestone.CelestialBodyName,
                    "Kerbin",
                    StringComparison.OrdinalIgnoreCase);
                bool isKerbinMoon = string.Equals(
                        milestone.CelestialBodyName,
                        "Mun",
                        StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        milestone.CelestialBodyName,
                        "Minmus",
                        StringComparison.OrdinalIgnoreCase);

                if (milestone.CrewRequirement == MilestoneCrewRequirement.Crewed)
                {
                    if (isKerbin)
                    {
                        return KerbinCrewedLaunchProgressCostFunds;
                    }

                    return isKerbinMoon
                        ? KerbinMoonCrewedLaunchProgressCostFunds
                        : InterplanetaryCrewedLaunchProgressCostFunds;
                }

                if (isKerbin)
                {
                    return KerbinProbeLaunchProgressCostFunds;
                }

                return isKerbinMoon
                    ? KerbinMoonProbeLaunchProgressCostFunds
                    : InterplanetaryProbeLaunchProgressCostFunds;
            }

            FundingProgramme fundingProgramme = FindFundingProgramme(targetId, fundingProgrammes);
            if (fundingProgramme != null)
            {
                if (string.Equals(
                    fundingProgramme.CelestialBodyName,
                    "Kerbin",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return KerbinNetworkLaunchProgressCostFunds;
                }

                if (string.Equals(
                        fundingProgramme.CelestialBodyName,
                        "Mun",
                        StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        fundingProgramme.CelestialBodyName,
                        "Minmus",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return KerbinMoonNetworkLaunchProgressCostFunds;
                }

                return InterplanetaryNetworkLaunchProgressCostFunds;
            }

            return KerbinProbeLaunchProgressCostFunds;
        }

        private static void SetMissionTarget(
            SpaceProgramState program,
            string targetId,
            RivalSimulationContext context)
        {
            if (program == null)
            {
                return;
            }

            program.NextMissionTargetId = targetId;
            program.NextMissionDisplayName = GetMissionTargetDisplayName(
                targetId,
                context.AchievementProgrammes,
                context.FundingProgrammes);
        }

        private static AchievementFundingProgramme FindAchievementProgramme(
            string targetId,
            IList<AchievementFundingProgramme> achievementProgrammes)
        {
            if (string.IsNullOrEmpty(targetId) || achievementProgrammes == null)
            {
                return null;
            }

            for (int programmeIndex = 0; programmeIndex < achievementProgrammes.Count; programmeIndex++)
            {
                AchievementFundingProgramme programme = achievementProgrammes[programmeIndex];
                if (programme != null
                    && string.Equals(programme.Id, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    return programme;
                }
            }

            return null;
        }

        private static FundingProgramme FindFundingProgramme(
            string targetId,
            IList<FundingProgramme> fundingProgrammes)
        {
            if (string.IsNullOrEmpty(targetId) || fundingProgrammes == null)
            {
                return null;
            }

            for (int programmeIndex = 0; programmeIndex < fundingProgrammes.Count; programmeIndex++)
            {
                FundingProgramme programme = fundingProgrammes[programmeIndex];
                if (programme != null
                    && string.Equals(programme.Id, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    return programme;
                }
            }

            return null;
        }
    }
}

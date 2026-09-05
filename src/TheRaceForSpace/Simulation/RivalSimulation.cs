using System;
using System.Collections.Generic;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Simulation
{
    /// <summary>
    /// Lightweight rival-program mission simulation.
    /// </summary>
    public static class RivalSimulation
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double LaunchProgressIntervalSeconds = 5.0 * KerbinDaySeconds;
        private const double SatelliteMissionSelectionChance = 0.60;
        private const int StandardLaunchProgressIncrementPercent = 10;
        private const int StarterLaunchProgressIncrementPercent = 20;

        private static readonly Random RandomGenerator = new Random();

        private struct RivalSimulationContext
        {
            public RivalSimulationContext(
                double currentUniversalTime,
                IList<AchievementFundingProgramme> achievementProgrammes,
                IList<FundingProgramme> fundingProgrammes)
            {
                CurrentUniversalTime = currentUniversalTime;
                AchievementProgrammes = achievementProgrammes;
                FundingProgrammes = fundingProgrammes;
            }

            public readonly double CurrentUniversalTime;
            public readonly IList<AchievementFundingProgramme> AchievementProgrammes;
            public readonly IList<FundingProgramme> FundingProgrammes;
        }

        /// <summary>
        /// Advances every non-player program in the supplied collection. The controller's sponsor
        /// review owns unlock-rule evaluation; rivals can only select contracts already marked Offered.
        /// </summary>
        public static void Refresh(
            IList<SpaceProgramState> programs,
            double currentUniversalTime,
            IList<AchievementFundingProgramme> achievementProgrammes,
            IList<FundingProgramme> fundingProgrammes)
        {
            if (programs == null
                || achievementProgrammes == null
                || fundingProgrammes == null
                || !IsFinite(currentUniversalTime)
                || currentUniversalTime < 0.0)
            {
                return;
            }

            var context = new RivalSimulationContext(
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
        /// Returns the mission-progress percentage gained by the rival on a successful progress check.
        /// Starter contracts are smaller pre-orbit launches and advance at 20%; all other missions use 10%.
        /// </summary>
        public static int CalculateLaunchProgressIncrementPercent(SpaceProgramState program)
        {
            if (program == null)
            {
                return StandardLaunchProgressIncrementPercent;
            }

            MilestoneDefinition milestone = PrototypeMilestones.FindById(program.NextMissionTargetId);
            return milestone != null && milestone.IsStarterContract
                ? StarterLaunchProgressIncrementPercent
                : StandardLaunchProgressIncrementPercent;
        }

        /// <summary>
        /// Returns the funds required for the rival's next successful mission-progress step.
        /// Starter steps advance 20%; orbital and satellite mission steps advance 10%.
        /// Stable mission target IDs are authoritative; presentation text is not used as a fallback.
        /// </summary>
        public static double CalculateLaunchProgressCost(
            SpaceProgramState program,
            IList<AchievementFundingProgramme> achievementProgrammes,
            IList<FundingProgramme> fundingProgrammes)
        {
            if (program == null)
            {
                return RaceSettings.Kerbin.ProbeProgressCostFunds;
            }

            return CalculateLaunchProgressCostForTarget(
                program.NextMissionTargetId,
                fundingProgrammes);
        }

        /// <summary>
        /// Estimates the average Kerbin days until a rival completes its planned mission.
        /// Returns null when current funds and projected scheduled payouts cannot finance
        /// all remaining development steps, or when the supplied simulation state is invalid.
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
            double rivalProgressChance = RaceSettings.RivalProgressChance;
            if (program == null
                || !IsFinite(currentUniversalTime)
                || currentUniversalTime < 0.0
                || !IsFinite(nextFundingUniversalTime)
                || !IsFinite(fundingIntervalSeconds)
                || fundingIntervalSeconds <= 0.0
                || !IsFinite(launchProgressCostFunds)
                || launchProgressCostFunds < 0.0
                || !IsFinite(program.Funds)
                || !IsFinite(program.NextPayoutFunds)
                || !IsFinite(rivalProgressChance)
                || rivalProgressChance <= 0.0
                || rivalProgressChance > 1.0)
            {
                return null;
            }

            int launchProgressIncrementPercent = CalculateLaunchProgressIncrementPercent(program);
            int currentProgressPercent = Math.Max(0, Math.Min(100, program.LaunchProgressPercent));
            int remainingProgressPercent = 100 - currentProgressPercent;
            int remainingProgressSteps = (remainingProgressPercent + launchProgressIncrementPercent - 1)
                / launchProgressIncrementPercent;

            if (remainingProgressSteps <= 0)
            {
                return 0;
            }

            double availableFunds = Math.Max(0.0, program.Funds);
            double projectedPayoutFunds = Math.Max(0.0, program.NextPayoutFunds);
            double expectedDaysPerSuccessfulStep =
                (LaunchProgressIntervalSeconds / KerbinDaySeconds) / rivalProgressChance;
            double fundingIntervalDays = fundingIntervalSeconds / KerbinDaySeconds;
            double nextFundingInDays = nextFundingUniversalTime >= 0.0
                ? Math.Max(0.0, (nextFundingUniversalTime - currentUniversalTime) / KerbinDaySeconds)
                : double.PositiveInfinity;
            double elapsedDays = 0.0;

            if (!IsFinite(expectedDaysPerSuccessfulStep) || !IsFinite(fundingIntervalDays))
            {
                return null;
            }

            for (int stepIndex = 0; stepIndex < remainingProgressSteps; stepIndex++)
            {
                double expectedStepDay = elapsedDays + expectedDaysPerSuccessfulStep;

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

            double roundedDays = Math.Ceiling(elapsedDays);
            if (!IsFinite(roundedDays) || roundedDays > int.MaxValue)
            {
                return null;
            }

            return (int)roundedDays;
        }

        private static void RefreshProgram(
            SpaceProgramState program,
            RivalSimulationContext context)
        {
            string targetId = program.NextMissionTargetId;
            if (!string.IsNullOrEmpty(targetId))
            {
                program.NextMissionDisplayName = GetMissionTargetDisplayName(
                    targetId,
                    context.AchievementProgrammes,
                    context.FundingProgrammes);
            }

            if (!IsTargetAvailable(targetId, program, context))
            {
                SetMissionTarget(program, null, context);
                program.LaunchProgressPercent = 0;
            }

            if (string.IsNullOrEmpty(program.NextMissionTargetId))
            {
                SetMissionTarget(program, ChooseNextMissionTarget(program, context), context);
            }

            if (!IsFinite(program.NextLaunchProgressCheckUniversalTime)
                || program.NextLaunchProgressCheckUniversalTime <= 0.0)
            {
                program.NextLaunchProgressCheckUniversalTime =
                    (Math.Floor(context.CurrentUniversalTime / LaunchProgressIntervalSeconds) + 1.0)
                    * LaunchProgressIntervalSeconds;
            }

            if (TryCompleteLaunch(program, context.CurrentUniversalTime, context))
            {
                SetMissionTarget(program, ChooseNextMissionTarget(program, context), context);
            }

            while (context.CurrentUniversalTime >= program.NextLaunchProgressCheckUniversalTime)
            {
                if (string.IsNullOrEmpty(program.NextMissionTargetId))
                {
                    SetMissionTarget(program, ChooseNextMissionTarget(program, context), context);
                }

                if (!string.IsNullOrEmpty(program.NextMissionTargetId))
                {
                    double launchProgressCostFunds = CalculateLaunchProgressCost(
                        program,
                        context.AchievementProgrammes,
                        context.FundingProgrammes);
                    int launchProgressIncrementPercent = CalculateLaunchProgressIncrementPercent(program);

                    if (program.LaunchProgressPercent < 100
                        && program.Funds >= launchProgressCostFunds
                        && RandomGenerator.NextDouble() < RaceSettings.RivalProgressChance)
                    {
                        program.Funds -= launchProgressCostFunds;
                        program.LaunchProgressPercent = Math.Min(
                            100,
                            program.LaunchProgressPercent + launchProgressIncrementPercent);
                    }

                    if (TryCompleteLaunch(program, program.NextLaunchProgressCheckUniversalTime, context))
                    {
                        SetMissionTarget(program, ChooseNextMissionTarget(program, context), context);
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
            if (program.LaunchProgressPercent < 100
                || string.IsNullOrEmpty(targetId)
                || !IsFinite(completionUniversalTime)
                || completionUniversalTime < 0.0)
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
                    completionUniversalTime);
                if (recordedAchievement
                    && milestone.ObjectiveType == MilestoneObjectiveType.Orbit
                    && milestone.CrewRequirement == MilestoneCrewRequirement.UncrewedProbe)
                {
                    // Only an actual uncrewed orbital milestone creates persistent satellite presence.
                    // Starter contracts can be uncrewed but represent atmospheric/ballistic work.
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
                return PrototypeMilestones.FindById(achievementProgramme.Id) != null
                    && achievementProgramme.IsOffered
                    && !achievementProgramme.IsExpired
                    && !program.HasAchievement(achievementProgramme.Id);
            }

            FundingProgramme fundingProgramme = FindFundingProgramme(targetId, context.FundingProgrammes);
            if (fundingProgramme != null)
            {
                return !string.IsNullOrEmpty(fundingProgramme.CelestialBodyName)
                    && fundingProgramme.IsAvailable
                    && fundingProgramme.IsOffered;
            }

            return false;
        }

        private static string ChooseNextMissionTarget(
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
                if (milestone.IsStarterContract && milestone.RivalProgressCostFunds > 0.0)
                {
                    return milestone.RivalProgressCostFunds;
                }

                RaceBodySettings bodySettings = RaceSettings.GetBodySettings(milestone.CelestialBodyName);
                return milestone.CrewRequirement == MilestoneCrewRequirement.Crewed
                    ? bodySettings.CrewedProgressCostFunds
                    : bodySettings.ProbeProgressCostFunds;
            }

            FundingProgramme fundingProgramme = FindFundingProgramme(targetId, fundingProgrammes);
            if (fundingProgramme != null && !string.IsNullOrEmpty(fundingProgramme.CelestialBodyName))
            {
                return RaceSettings.GetBodySettings(fundingProgramme.CelestialBodyName)
                    .SatelliteProgressCostFunds;
            }

            return RaceSettings.Kerbin.ProbeProgressCostFunds;
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

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}

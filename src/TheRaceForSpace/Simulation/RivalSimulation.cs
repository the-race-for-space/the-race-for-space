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
        // These IDs match the current funding-programme IDs. They remain public for the
        // compatibility path; the main simulation discovers satellite targets from programmes.
        public const string KerbinSatelliteTargetId = "kerbin-network";
        public const string MunSatelliteTargetId = "mun-survey";
        public const string MinmusSatelliteTargetId = "minmus-relay";

        // These names remain for UI and staged-call compatibility only. Rival simulation
        // decisions use stable IDs rather than comparing presentation strings.
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
        private const double DistantLaunchProgressCostMultiplier = 2.0;
        private const double CrewedOrbitLaunchProgressCostMultiplier = 2.0;

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
        /// Compatibility overload retained for tests and callers using the original 0.3 boolean
        /// snapshot. It converts that snapshot into the same programme collections used by the
        /// main simulation path, so selection and completion logic remains collection-driven.
        /// </summary>
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
            var achievementProgrammes = new List<AchievementFundingProgramme>
            {
                CreateCompatibilityAchievementProgramme(PrototypeMilestones.ProbeOrbitId, probeOrbitContractLive),
                CreateCompatibilityAchievementProgramme(PrototypeMilestones.CrewedOrbitId, crewedOrbitContractLive),
                CreateCompatibilityAchievementProgramme(PrototypeMilestones.MunProbeOrbitId, munProbeOrbitContractLive),
                CreateCompatibilityAchievementProgramme(PrototypeMilestones.MinmusProbeOrbitId, minmusProbeOrbitContractLive),
                CreateCompatibilityAchievementProgramme(PrototypeMilestones.MunCrewedOrbitId, munCrewedOrbitContractLive),
                CreateCompatibilityAchievementProgramme(PrototypeMilestones.MinmusCrewedOrbitId, minmusCrewedOrbitContractLive)
            };

            var fundingProgrammes = new List<FundingProgramme>
            {
                new FundingProgramme(
                    KerbinSatelliteTargetId,
                    "Kerbin Orbital Network",
                    "Kerbin",
                    10,
                    0.0,
                    kerbinNetworkAvailable,
                    null,
                    PrototypeMilestones.ProbeOrbitId),
                new FundingProgramme(
                    MunSatelliteTargetId,
                    "Mun Survey Network",
                    "Mun",
                    5,
                    0.0,
                    munNetworkAvailable,
                    null,
                    PrototypeMilestones.MunProbeOrbitId),
                new FundingProgramme(
                    MinmusSatelliteTargetId,
                    "Minmus Relay Initiative",
                    "Minmus",
                    5,
                    0.0,
                    minmusNetworkAvailable,
                    null,
                    PrototypeMilestones.MinmusProbeOrbitId)
            };

            Refresh(
                playerProgram,
                asterProgram,
                cobaltProgram,
                currentUniversalTime,
                achievementProgrammes,
                fundingProgrammes);
        }

        /// <summary>
        /// Compatibility overload for the original three-program prototype call shape.
        /// New code should pass the complete program collection.
        /// </summary>
        public static void Refresh(
            SpaceProgramState playerProgram,
            SpaceProgramState asterProgram,
            SpaceProgramState cobaltProgram,
            double currentUniversalTime,
            IList<AchievementFundingProgramme> achievementProgrammes,
            IList<FundingProgramme> fundingProgrammes)
        {
            if (playerProgram == null || asterProgram == null || cobaltProgram == null)
            {
                return;
            }

            Refresh(
                new List<SpaceProgramState> { playerProgram, asterProgram, cobaltProgram },
                currentUniversalTime,
                achievementProgrammes,
                fundingProgrammes);
        }

        /// <summary>
        /// Advances every non-player program in the supplied collection. Target availability
        /// checks also use the same collection, so adding another rival requires no simulation branch.
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
        /// Returns the display text for a stable rival mission target ID, or null for an unknown ID.
        /// This overload remains for compatibility with the three prototype networks.
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
        /// Returns display text using the supplied live programme collections, including targets
        /// added after the original Kerbin/Mun/Minmus prototype set.
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
            if (fundingProgramme != null)
            {
                return fundingProgramme.CelestialBodyName;
            }

            return GetMissionTargetDisplayName(targetId);
        }

        /// <summary>
        /// Returns the funds required for the rival's next successful 10% mission-progress step.
        /// This compatibility overload understands the original prototype target set.
        /// </summary>
        public static double CalculateLaunchProgressCost(SpaceProgramState program)
        {
            if (program == null)
            {
                return LaunchProgressCostFunds;
            }

            string targetId = SynchronizeMissionTargetIdentity(program);
            return CalculateLaunchProgressCostForTarget(targetId, null);
        }

        /// <summary>
        /// Returns mission-progress cost using the live funding collection so newly added bodies
        /// follow the same Kerbin-versus-distant cost rule without another ID branch.
        /// </summary>
        public static double CalculateLaunchProgressCost(
            SpaceProgramState program,
            IList<AchievementFundingProgramme> achievementProgrammes,
            IList<FundingProgramme> fundingProgrammes)
        {
            if (program == null)
            {
                return LaunchProgressCostFunds;
            }

            string targetId = SynchronizeMissionTargetIdentity(
                program,
                achievementProgrammes,
                fundingProgrammes);
            return CalculateLaunchProgressCostForTarget(targetId, fundingProgrammes);
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
            return CalculateEstimatedLaunchDays(
                program,
                currentUniversalTime,
                nextFundingUniversalTime,
                fundingIntervalSeconds,
                CalculateLaunchProgressCost(program));
        }

        /// <summary>
        /// Collection-aware ETA used by the live controller so target cost stays correct for
        /// satellite programmes added beyond the original three-body prototype.
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
            bool hadStoredTarget = !string.IsNullOrEmpty(program.NextMissionTargetId)
                || !string.IsNullOrEmpty(program.NextLaunchBodyName);
            string targetId = SynchronizeMissionTargetIdentity(
                program,
                context.AchievementProgrammes,
                context.FundingProgrammes);

            if ((hadStoredTarget && string.IsNullOrEmpty(targetId))
                || !IsTargetAvailable(targetId, program, context))
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
            string targetId = SynchronizeMissionTargetIdentity(
                program,
                context.AchievementProgrammes,
                context.FundingProgrammes);
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

                return string.IsNullOrEmpty(achievementProgramme.PrerequisiteMilestoneId)
                    || HasAnyProgramAchieved(achievementProgramme.PrerequisiteMilestoneId, context);
            }

            FundingProgramme fundingProgramme = FindFundingProgramme(targetId, context.FundingProgrammes);
            if (fundingProgramme != null)
            {
                return fundingProgramme.IsAvailable
                    || string.IsNullOrEmpty(fundingProgramme.PrerequisiteMilestoneId)
                    || HasAnyProgramAchieved(fundingProgramme.PrerequisiteMilestoneId, context);
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
                if (milestone.CrewRequirement == MilestoneCrewRequirement.Crewed)
                {
                    return LaunchProgressCostFunds * CrewedOrbitLaunchProgressCostMultiplier;
                }

                if (!string.Equals(
                    milestone.CelestialBodyName,
                    "Kerbin",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return LaunchProgressCostFunds * DistantLaunchProgressCostMultiplier;
                }

                return LaunchProgressCostFunds;
            }

            FundingProgramme fundingProgramme = FindFundingProgramme(targetId, fundingProgrammes);
            if (fundingProgramme != null)
            {
                return string.Equals(
                    fundingProgramme.CelestialBodyName,
                    "Kerbin",
                    StringComparison.OrdinalIgnoreCase)
                    ? LaunchProgressCostFunds
                    : LaunchProgressCostFunds * DistantLaunchProgressCostMultiplier;
            }

            // Compatibility fallback for the original three network IDs when no collection was supplied.
            if (string.Equals(targetId, MunSatelliteTargetId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(targetId, MinmusSatelliteTargetId, StringComparison.OrdinalIgnoreCase))
            {
                return LaunchProgressCostFunds * DistantLaunchProgressCostMultiplier;
            }

            return LaunchProgressCostFunds;
        }

        private static bool HasAnyProgramAchieved(
            string milestoneId,
            RivalSimulationContext context)
        {
            if (string.IsNullOrEmpty(milestoneId) || context.Programs == null)
            {
                return false;
            }

            for (int programIndex = 0; programIndex < context.Programs.Count; programIndex++)
            {
                SpaceProgramState program = context.Programs[programIndex];
                if (program != null && program.HasAchievement(milestoneId))
                {
                    return true;
                }
            }

            return false;
        }

        private static string SynchronizeMissionTargetIdentity(SpaceProgramState program)
        {
            if (program == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(program.NextMissionTargetId))
            {
                program.NextLaunchBodyName = GetMissionTargetDisplayName(program.NextMissionTargetId);
                return program.NextMissionTargetId;
            }

            string targetId = ResolveLegacyMissionTargetId(program.NextLaunchBodyName, null, null);
            if (!string.IsNullOrEmpty(targetId))
            {
                program.NextMissionTargetId = targetId;
                program.NextLaunchBodyName = GetMissionTargetDisplayName(targetId);
            }

            return targetId;
        }

        private static string SynchronizeMissionTargetIdentity(
            SpaceProgramState program,
            IList<AchievementFundingProgramme> achievementProgrammes,
            IList<FundingProgramme> fundingProgrammes)
        {
            if (program == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(program.NextMissionTargetId))
            {
                // Stable IDs are authoritative. Presentation text is regenerated from the live
                // definitions rather than being allowed to change simulation identity.
                program.NextLaunchBodyName = GetMissionTargetDisplayName(
                    program.NextMissionTargetId,
                    achievementProgrammes,
                    fundingProgrammes);
                return program.NextMissionTargetId;
            }

            string targetId = ResolveLegacyMissionTargetId(
                program.NextLaunchBodyName,
                achievementProgrammes,
                fundingProgrammes);
            if (!string.IsNullOrEmpty(targetId))
            {
                program.NextMissionTargetId = targetId;
                program.NextLaunchBodyName = GetMissionTargetDisplayName(
                    targetId,
                    achievementProgrammes,
                    fundingProgrammes);
            }

            return targetId;
        }

        private static string ResolveLegacyMissionTargetId(
            string legacyTargetName,
            IList<AchievementFundingProgramme> achievementProgrammes,
            IList<FundingProgramme> fundingProgrammes)
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

            if (achievementProgrammes != null)
            {
                for (int programmeIndex = 0; programmeIndex < achievementProgrammes.Count; programmeIndex++)
                {
                    AchievementFundingProgramme programme = achievementProgrammes[programmeIndex];
                    if (programme != null
                        && (string.Equals(programme.Id, legacyTargetName, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(programme.Name, legacyTargetName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return programme.Id;
                    }
                }
            }

            if (fundingProgrammes != null)
            {
                for (int programmeIndex = 0; programmeIndex < fundingProgrammes.Count; programmeIndex++)
                {
                    FundingProgramme programme = fundingProgrammes[programmeIndex];
                    if (programme != null
                        && (string.Equals(programme.Id, legacyTargetName, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(programme.Name, legacyTargetName, StringComparison.OrdinalIgnoreCase)
                            || string.Equals(
                                programme.CelestialBodyName,
                                legacyTargetName,
                                StringComparison.OrdinalIgnoreCase)))
                    {
                        return programme.Id;
                    }
                }
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
            program.NextLaunchBodyName = GetMissionTargetDisplayName(
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

        private static AchievementFundingProgramme CreateCompatibilityAchievementProgramme(
            string milestoneId,
            bool isLive)
        {
            MilestoneDefinition milestone = PrototypeMilestones.FindById(milestoneId);
            var programme = new AchievementFundingProgramme(
                milestone.Id,
                milestone.Name,
                milestone.ObjectiveDescription,
                0.0,
                null,
                milestone.PrerequisiteMilestoneId);

            if (!isLive)
            {
                programme.RestoreState(true, 10);
            }

            return programme;
        }
    }
}

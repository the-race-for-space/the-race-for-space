using System;
using System.Collections.Generic;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Agencies;

namespace TheRaceForSpace.Rivals
{
    /// <summary>
    /// Lightweight rival-agency mission simulation.
    /// </summary>
    public static class RivalSimulation
    {
        private const double KerbinDaySeconds = 21600.0;
        private const double LaunchProgressIntervalSeconds = 5.0 * KerbinDaySeconds;
        private const double SatelliteMissionSelectionChance = 0.60;
        private const int StandardLaunchProgressIncrementPercent = 10;
        private const int PreOrbitLaunchProgressIncrementPercent = 20;

        private static readonly Random RandomGenerator = new Random();

        private struct RivalSimulationContext
        {
            public RivalSimulationContext(
                double currentUniversalTime,
                IList<ObjectiveFundingContract> achievementProgrammes,
                IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
            {
                CurrentUniversalTime = currentUniversalTime;
                AchievementProgrammes = achievementProgrammes;
                SatelliteNetworkFundingContracts = satelliteNetworkFundingContracts;
            }

            public readonly double CurrentUniversalTime;
            public readonly IList<ObjectiveFundingContract> AchievementProgrammes;
            public readonly IList<SatelliteNetworkFundingContract> SatelliteNetworkFundingContracts;
        }

        /// <summary>
        /// Advances every non-player agency in the supplied collection. The controller's sponsor
        /// review owns unlock-rule evaluation; rivals can only select contracts already marked Offered.
        /// </summary>
        public static void Refresh(
            IList<AgencyState> agencies,
            double currentUniversalTime,
            IList<ObjectiveFundingContract> achievementProgrammes,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            if (agencies == null
                || achievementProgrammes == null
                || satelliteNetworkFundingContracts == null
                || !IsFinite(currentUniversalTime)
                || currentUniversalTime < 0.0)
            {
                return;
            }

            var context = new RivalSimulationContext(
                currentUniversalTime,
                achievementProgrammes,
                satelliteNetworkFundingContracts);

            for (int agencyIndex = 0; agencyIndex < agencies.Count; agencyIndex++)
            {
                AgencyState agency = agencies[agencyIndex];
                if (agency == null || agency.IsPlayer)
                {
                    continue;
                }

                RefreshProgram(agency, context);
            }
        }

        /// <summary>
        /// Returns presentation text for a stable mission target ID using the live programme
        /// collections. Mission identity is never inferred from this display text.
        /// </summary>
        public static string GetMissionTargetDisplayName(
            string targetId,
            IList<ObjectiveFundingContract> achievementProgrammes,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            ObjectiveFundingContract achievementProgramme = FindAchievementProgramme(
                targetId,
                achievementProgrammes);
            if (achievementProgramme != null)
            {
                return achievementProgramme.Name;
            }

            SatelliteNetworkFundingContract fundingProgramme = FindSatelliteNetworkFundingContract(targetId, satelliteNetworkFundingContracts);
            return fundingProgramme == null ? null : fundingProgramme.CelestialBodyName;
        }

        /// <summary>
        /// Returns the mission-progress percentage gained by the rival on a successful progress check.
        /// PreOrbit contracts are smaller pre-orbit launches and advance at 20%; all other missions use 10%.
        /// </summary>
        public static int CalculateLaunchProgressIncrementPercent(AgencyState agency)
        {
            if (agency == null)
            {
                return StandardLaunchProgressIncrementPercent;
            }

            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(agency.NextMissionTargetId);
            return objective != null && objective.IsPreOrbitContract
                ? PreOrbitLaunchProgressIncrementPercent
                : StandardLaunchProgressIncrementPercent;
        }

        /// <summary>
        /// Returns the funds required for the rival's next successful mission-progress step.
        /// PreOrbit steps advance 20%; orbital and satellite mission steps advance 10%.
        /// Stable mission target IDs are authoritative; presentation text is not used as a fallback.
        /// </summary>
        public static double CalculateLaunchProgressCost(
            AgencyState agency,
            IList<ObjectiveFundingContract> achievementProgrammes,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            if (agency == null)
            {
                return CampaignSettings.Kerbin.ProbeProgressCostFunds;
            }

            return CalculateLaunchProgressCostForTarget(
                agency.NextMissionTargetId,
                satelliteNetworkFundingContracts);
        }

        /// <summary>
        /// Estimates the average Kerbin days until a rival completes its planned mission.
        /// Returns null when current funds and projected scheduled payouts cannot finance
        /// all remaining development steps, or when the supplied simulation state is invalid.
        /// </summary>
        public static int? CalculateEstimatedLaunchDays(
            AgencyState agency,
            double currentUniversalTime,
            double nextFundingUniversalTime,
            double fundingIntervalSeconds,
            IList<ObjectiveFundingContract> achievementProgrammes,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            return CalculateEstimatedLaunchDays(
                agency,
                currentUniversalTime,
                nextFundingUniversalTime,
                fundingIntervalSeconds,
                CalculateLaunchProgressCost(agency, achievementProgrammes, satelliteNetworkFundingContracts));
        }

        private static int? CalculateEstimatedLaunchDays(
            AgencyState agency,
            double currentUniversalTime,
            double nextFundingUniversalTime,
            double fundingIntervalSeconds,
            double launchProgressCostFunds)
        {
            double rivalProgressChance = CampaignSettings.RivalProgressChance;
            if (agency == null
                || !IsFinite(currentUniversalTime)
                || currentUniversalTime < 0.0
                || !IsFinite(nextFundingUniversalTime)
                || !IsFinite(fundingIntervalSeconds)
                || fundingIntervalSeconds <= 0.0
                || !IsFinite(launchProgressCostFunds)
                || launchProgressCostFunds < 0.0
                || !IsFinite(agency.Funds)
                || !IsFinite(agency.NextPayoutFunds)
                || !IsFinite(rivalProgressChance)
                || rivalProgressChance <= 0.0
                || rivalProgressChance > 1.0)
            {
                return null;
            }

            int launchProgressIncrementPercent = CalculateLaunchProgressIncrementPercent(agency);
            int currentProgressPercent = Math.Max(0, Math.Min(100, agency.MissionProgressPercent));
            int remainingProgressPercent = 100 - currentProgressPercent;
            int remainingProgressSteps = (remainingProgressPercent + launchProgressIncrementPercent - 1)
                / launchProgressIncrementPercent;

            if (remainingProgressSteps <= 0)
            {
                return 0;
            }

            double availableFunds = Math.Max(0.0, agency.Funds);
            double projectedPayoutFunds = Math.Max(0.0, agency.NextPayoutFunds);
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
            AgencyState agency,
            RivalSimulationContext context)
        {
            string targetId = agency.NextMissionTargetId;
            if (!string.IsNullOrEmpty(targetId))
            {
                agency.NextMissionDisplayName = GetMissionTargetDisplayName(
                    targetId,
                    context.AchievementProgrammes,
                    context.SatelliteNetworkFundingContracts);
            }

            if (!IsTargetAvailable(targetId, agency, context))
            {
                SetMissionTarget(agency, null, context);
                agency.MissionProgressPercent = 0;
            }

            if (string.IsNullOrEmpty(agency.NextMissionTargetId))
            {
                SetMissionTarget(agency, ChooseNextMissionTarget(agency, context), context);
            }

            if (!IsFinite(agency.NextMissionProgressCheckUniversalTime)
                || agency.NextMissionProgressCheckUniversalTime <= 0.0)
            {
                agency.NextMissionProgressCheckUniversalTime =
                    (Math.Floor(context.CurrentUniversalTime / LaunchProgressIntervalSeconds) + 1.0)
                    * LaunchProgressIntervalSeconds;
            }

            if (TryCompleteLaunch(agency, context.CurrentUniversalTime, context))
            {
                SetMissionTarget(agency, ChooseNextMissionTarget(agency, context), context);
            }

            while (context.CurrentUniversalTime >= agency.NextMissionProgressCheckUniversalTime)
            {
                if (string.IsNullOrEmpty(agency.NextMissionTargetId))
                {
                    SetMissionTarget(agency, ChooseNextMissionTarget(agency, context), context);
                }

                if (!string.IsNullOrEmpty(agency.NextMissionTargetId))
                {
                    double launchProgressCostFunds = CalculateLaunchProgressCost(
                        agency,
                        context.AchievementProgrammes,
                        context.SatelliteNetworkFundingContracts);
                    int launchProgressIncrementPercent = CalculateLaunchProgressIncrementPercent(agency);

                    if (agency.MissionProgressPercent < 100
                        && agency.Funds >= launchProgressCostFunds
                        && RandomGenerator.NextDouble() < CampaignSettings.RivalProgressChance)
                    {
                        agency.Funds -= launchProgressCostFunds;
                        agency.MissionProgressPercent = Math.Min(
                            100,
                            agency.MissionProgressPercent + launchProgressIncrementPercent);
                    }

                    if (TryCompleteLaunch(agency, agency.NextMissionProgressCheckUniversalTime, context))
                    {
                        SetMissionTarget(agency, ChooseNextMissionTarget(agency, context), context);
                    }
                }

                agency.NextMissionProgressCheckUniversalTime += LaunchProgressIntervalSeconds;
            }
        }

        private static bool TryCompleteLaunch(
            AgencyState agency,
            double completionUniversalTime,
            RivalSimulationContext context)
        {
            string targetId = agency.NextMissionTargetId;
            if (agency.MissionProgressPercent < 100
                || string.IsNullOrEmpty(targetId)
                || !IsFinite(completionUniversalTime)
                || completionUniversalTime < 0.0)
            {
                return false;
            }

            ObjectiveFundingContract achievementProgramme = FindAchievementProgramme(
                targetId,
                context.AchievementProgrammes);
            if (achievementProgramme != null)
            {
                ObjectiveDefinition objective = ObjectiveCatalogue.FindById(achievementProgramme.Id);
                if (objective == null)
                {
                    return false;
                }

                bool recordedAchievement = agency.RecordObjectiveCompletion(
                    objective.Id,
                    completionUniversalTime);
                if (recordedAchievement
                    && objective.ObjectiveType == ObjectiveType.Orbit
                    && objective.CrewRequirement == ObjectiveCrewRequirement.UncrewedProbe)
                {
                    // Only an actual uncrewed orbital objective creates persistent satellite presence.
                    // PreOrbit contracts can be uncrewed but represent atmospheric/ballistic work.
                    agency.SetSatelliteCount(
                        objective.CelestialBodyName,
                        agency.GetSatelliteCount(objective.CelestialBodyName) + 1);
                }
            }
            else
            {
                SatelliteNetworkFundingContract fundingProgramme = FindSatelliteNetworkFundingContract(
                    targetId,
                    context.SatelliteNetworkFundingContracts);
                if (fundingProgramme == null || string.IsNullOrEmpty(fundingProgramme.CelestialBodyName))
                {
                    return false;
                }

                agency.SetSatelliteCount(
                    fundingProgramme.CelestialBodyName,
                    agency.GetSatelliteCount(fundingProgramme.CelestialBodyName) + 1);
            }

            agency.MissionProgressPercent = 0;
            SetMissionTarget(agency, null, context);
            return true;
        }

        private static bool IsTargetAvailable(
            string targetId,
            AgencyState agency,
            RivalSimulationContext context)
        {
            if (agency == null || string.IsNullOrEmpty(targetId))
            {
                return true;
            }

            ObjectiveFundingContract achievementProgramme = FindAchievementProgramme(
                targetId,
                context.AchievementProgrammes);
            if (achievementProgramme != null)
            {
                return ObjectiveCatalogue.FindById(achievementProgramme.Id) != null
                    && achievementProgramme.IsOffered
                    && !achievementProgramme.IsExpired
                    && !agency.HasCompletedObjective(achievementProgramme.Id);
            }

            SatelliteNetworkFundingContract fundingProgramme = FindSatelliteNetworkFundingContract(targetId, context.SatelliteNetworkFundingContracts);
            if (fundingProgramme != null)
            {
                return !string.IsNullOrEmpty(fundingProgramme.CelestialBodyName)
                    && fundingProgramme.IsAvailable
                    && fundingProgramme.IsOffered;
            }

            return false;
        }

        private static string ChooseNextMissionTarget(
            AgencyState agency,
            RivalSimulationContext context)
        {
            var availableOneOffTargets = new List<string>();
            var availableSatelliteTargets = new List<string>();

            for (int programmeIndex = 0;
                programmeIndex < context.AchievementProgrammes.Count;
                programmeIndex++)
            {
                ObjectiveFundingContract programme = context.AchievementProgrammes[programmeIndex];
                if (programme == null
                    || ObjectiveCatalogue.FindById(programme.Id) == null
                    || !IsTargetAvailable(programme.Id, agency, context))
                {
                    continue;
                }

                availableOneOffTargets.Add(programme.Id);
            }

            for (int programmeIndex = 0; programmeIndex < context.SatelliteNetworkFundingContracts.Count; programmeIndex++)
            {
                SatelliteNetworkFundingContract programme = context.SatelliteNetworkFundingContracts[programmeIndex];
                if (programme == null
                    || string.IsNullOrEmpty(programme.Id)
                    || string.IsNullOrEmpty(programme.CelestialBodyName)
                    || !IsTargetAvailable(programme.Id, agency, context))
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
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(targetId);
            if (objective != null)
            {
                if (objective.IsPreOrbitContract && objective.RivalProgressCostFunds > 0.0)
                {
                    return objective.RivalProgressCostFunds;
                }

                BodyBalanceSettings bodySettings = CampaignSettings.GetBodySettings(objective.CelestialBodyName);
                return objective.CrewRequirement == ObjectiveCrewRequirement.Crewed
                    ? bodySettings.CrewedProgressCostFunds
                    : bodySettings.ProbeProgressCostFunds;
            }

            SatelliteNetworkFundingContract fundingProgramme = FindSatelliteNetworkFundingContract(targetId, satelliteNetworkFundingContracts);
            if (fundingProgramme != null && !string.IsNullOrEmpty(fundingProgramme.CelestialBodyName))
            {
                return CampaignSettings.GetBodySettings(fundingProgramme.CelestialBodyName)
                    .SatelliteProgressCostFunds;
            }

            return CampaignSettings.Kerbin.ProbeProgressCostFunds;
        }

        private static void SetMissionTarget(
            AgencyState agency,
            string targetId,
            RivalSimulationContext context)
        {
            if (agency == null)
            {
                return;
            }

            agency.NextMissionTargetId = targetId;
            agency.NextMissionDisplayName = GetMissionTargetDisplayName(
                targetId,
                context.AchievementProgrammes,
                context.SatelliteNetworkFundingContracts);
        }

        private static ObjectiveFundingContract FindAchievementProgramme(
            string targetId,
            IList<ObjectiveFundingContract> achievementProgrammes)
        {
            if (string.IsNullOrEmpty(targetId) || achievementProgrammes == null)
            {
                return null;
            }

            for (int programmeIndex = 0; programmeIndex < achievementProgrammes.Count; programmeIndex++)
            {
                ObjectiveFundingContract programme = achievementProgrammes[programmeIndex];
                if (programme != null
                    && string.Equals(programme.Id, targetId, StringComparison.OrdinalIgnoreCase))
                {
                    return programme;
                }
            }

            return null;
        }

        private static SatelliteNetworkFundingContract FindSatelliteNetworkFundingContract(
            string targetId,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts)
        {
            if (string.IsNullOrEmpty(targetId) || satelliteNetworkFundingContracts == null)
            {
                return null;
            }

            for (int programmeIndex = 0; programmeIndex < satelliteNetworkFundingContracts.Count; programmeIndex++)
            {
                SatelliteNetworkFundingContract programme = satelliteNetworkFundingContracts[programmeIndex];
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

using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;

namespace TheRaceForSpace.Objectives
{
    /// <summary>
    /// Evaluates code-defined campaign unlock rules against project-owned agency state and an
    /// explicit universal time. The evaluator is KSP-independent so historical controller replay,
    /// rival target selection, player objective checks, and UI presentation can share one meaning.
    /// </summary>
    public static class UnlockRuleEvaluator
    {
        /// <summary>
        /// Returns true when the supplied rule is satisfied. A null rule means the target is
        /// available from the start of the campaign. Empty/malformed rules fail closed.
        /// </summary>
        public static bool IsSatisfied(
            UnlockRuleDefinition rule,
            IList<AgencyState> agencies,
            double evaluationUniversalTime)
        {
            if (rule == null)
            {
                return true;
            }

            if (!IsValidEvaluationTime(evaluationUniversalTime) || rule.Paths.Count == 0)
            {
                return false;
            }

            for (int pathIndex = 0; pathIndex < rule.Paths.Count; pathIndex++)
            {
                UnlockPathDefinition path = rule.Paths[pathIndex];
                if (IsPathSatisfied(path, agencies, evaluationUniversalTime))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns whether one unlock condition is satisfied at the supplied time. UI progress
        /// uses this same method as the full-rule evaluator rather than reimplementing semantics.
        /// </summary>
        public static bool IsConditionSatisfied(
            UnlockConditionDefinition condition,
            IList<AgencyState> agencies,
            double evaluationUniversalTime)
        {
            if (condition == null || !IsValidEvaluationTime(evaluationUniversalTime))
            {
                return false;
            }

            if (condition.ConditionType == UnlockConditionType.UniversalTime)
            {
                return evaluationUniversalTime >= condition.RequiredUniversalTime;
            }

            if (condition.ConditionType == UnlockConditionType.SatelliteCount)
            {
                return GetSatelliteCount(condition, agencies) >= condition.RequiredSatelliteCount;
            }

            if (condition.ConditionType != UnlockConditionType.ObjectiveCompletion)
            {
                return false;
            }

            return GetSatisfiedProgramCount(
                condition,
                agencies,
                evaluationUniversalTime) >= condition.RequiredAgencyCount;
        }

        /// <summary>
        /// Counts agencies that satisfy an objectiveCompletion condition at the supplied time. Non-
        /// objectiveCompletion or malformed conditions return zero.
        /// </summary>
        public static int GetSatisfiedProgramCount(
            UnlockConditionDefinition condition,
            IList<AgencyState> agencies,
            double evaluationUniversalTime)
        {
            if (condition == null
                || condition.ConditionType != UnlockConditionType.ObjectiveCompletion
                || agencies == null
                || string.IsNullOrEmpty(condition.ObjectiveId)
                || !IsValidEvaluationTime(evaluationUniversalTime))
            {
                return 0;
            }

            int achievedProgramCount = 0;
            for (int agencyIndex = 0; agencyIndex < agencies.Count; agencyIndex++)
            {
                if (DoesProgramSatisfyAchievementCondition(
                    agencies[agencyIndex],
                    condition,
                    evaluationUniversalTime))
                {
                    achievedProgramCount++;
                }
            }

            return achievedProgramCount;
        }

        /// <summary>
        /// Returns the collective qualifying satellite count for a satellite-count condition.
        /// Satellite state is a current project-owned snapshot rather than timestamped history;
        /// historical callers preserve ordering by evaluating before observing newer player vessels.
        /// </summary>
        public static int GetSatelliteCount(
            UnlockConditionDefinition condition,
            IList<AgencyState> agencies)
        {
            if (condition == null
                || condition.ConditionType != UnlockConditionType.SatelliteCount
                || agencies == null
                || string.IsNullOrEmpty(condition.CelestialBodyName))
            {
                return 0;
            }

            int satelliteCount = 0;
            for (int agencyIndex = 0; agencyIndex < agencies.Count; agencyIndex++)
            {
                AgencyState agency = agencies[agencyIndex];
                if (agency == null)
                {
                    continue;
                }

                satelliteCount += agency.GetSatelliteCount(condition.CelestialBodyName);
            }

            return satelliteCount;
        }

        /// <summary>
        /// Returns whether one agency satisfies the scope, objective and historical-time parts of
        /// an objectiveCompletion condition. This supports read-only UI attribution without duplicating rules.
        /// </summary>
        public static bool DoesProgramSatisfyAchievementCondition(
            AgencyState agency,
            UnlockConditionDefinition condition,
            double evaluationUniversalTime)
        {
            if (agency == null
                || condition == null
                || condition.ConditionType != UnlockConditionType.ObjectiveCompletion
                || string.IsNullOrEmpty(condition.ObjectiveId)
                || !IsValidEvaluationTime(evaluationUniversalTime)
                || !ProgramMatchesScope(agency, condition.AgencyScope))
            {
                return false;
            }

            double achievementUniversalTime = agency.GetObjectiveCompletionTime(condition.ObjectiveId);
            return achievementUniversalTime >= 0.0
                && achievementUniversalTime <= evaluationUniversalTime;
        }

        private static bool IsPathSatisfied(
            UnlockPathDefinition path,
            IList<AgencyState> agencies,
            double evaluationUniversalTime)
        {
            if (path == null || path.Conditions.Count == 0)
            {
                return false;
            }

            for (int conditionIndex = 0; conditionIndex < path.Conditions.Count; conditionIndex++)
            {
                UnlockConditionDefinition condition = path.Conditions[conditionIndex];
                if (!IsConditionSatisfied(condition, agencies, evaluationUniversalTime))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ProgramMatchesScope(
            AgencyState agency,
            UnlockAgencyScope agencyScope)
        {
            if (agency == null)
            {
                return false;
            }

            switch (agencyScope)
            {
                case UnlockAgencyScope.AnyAgency:
                    return true;

                case UnlockAgencyScope.Player:
                    return agency.IsPlayer;

                case UnlockAgencyScope.AnyRival:
                    return !agency.IsPlayer;

                default:
                    return false;
            }
        }

        private static bool IsValidEvaluationTime(double evaluationUniversalTime)
        {
            return !double.IsNaN(evaluationUniversalTime)
                && !double.IsInfinity(evaluationUniversalTime)
                && evaluationUniversalTime >= 0.0;
        }
    }
}

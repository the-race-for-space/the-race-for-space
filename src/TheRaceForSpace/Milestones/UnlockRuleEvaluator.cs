using System;
using System.Collections.Generic;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Milestones
{
    /// <summary>
    /// Evaluates code-defined campaign unlock rules against project-owned program state and an
    /// explicit universal time. The evaluator is KSP-independent so historical controller replay,
    /// rival target selection, player milestone checks, and UI presentation can share one meaning.
    /// </summary>
    public static class UnlockRuleEvaluator
    {
        /// <summary>
        /// Returns true when the supplied rule is satisfied. A null rule means the target is
        /// available from the start of the campaign. Empty/malformed rules fail closed.
        /// </summary>
        public static bool IsSatisfied(
            UnlockRuleDefinition rule,
            IList<SpaceProgramState> programs,
            double evaluationUniversalTime)
        {
            if (rule == null)
            {
                return true;
            }

            if (double.IsNaN(evaluationUniversalTime) || rule.Paths.Count == 0)
            {
                return false;
            }

            for (int pathIndex = 0; pathIndex < rule.Paths.Count; pathIndex++)
            {
                UnlockPathDefinition path = rule.Paths[pathIndex];
                if (IsPathSatisfied(path, programs, evaluationUniversalTime))
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
            IList<SpaceProgramState> programs,
            double evaluationUniversalTime)
        {
            if (condition == null || double.IsNaN(evaluationUniversalTime))
            {
                return false;
            }

            if (condition.ConditionType == UnlockConditionType.UniversalTime)
            {
                return evaluationUniversalTime >= condition.RequiredUniversalTime;
            }

            if (condition.ConditionType != UnlockConditionType.Achievement)
            {
                return false;
            }

            return GetSatisfiedProgramCount(
                condition,
                programs,
                evaluationUniversalTime) >= condition.RequiredProgramCount;
        }

        /// <summary>
        /// Counts programs that satisfy an achievement condition at the supplied time. Non-
        /// achievement or malformed conditions return zero.
        /// </summary>
        public static int GetSatisfiedProgramCount(
            UnlockConditionDefinition condition,
            IList<SpaceProgramState> programs,
            double evaluationUniversalTime)
        {
            if (condition == null
                || condition.ConditionType != UnlockConditionType.Achievement
                || programs == null
                || string.IsNullOrEmpty(condition.MilestoneId)
                || double.IsNaN(evaluationUniversalTime))
            {
                return 0;
            }

            int achievedProgramCount = 0;
            for (int programIndex = 0; programIndex < programs.Count; programIndex++)
            {
                if (DoesProgramSatisfyAchievementCondition(
                    programs[programIndex],
                    condition,
                    evaluationUniversalTime))
                {
                    achievedProgramCount++;
                }
            }

            return achievedProgramCount;
        }

        /// <summary>
        /// Returns whether one program satisfies the scope, milestone and historical-time parts of
        /// an achievement condition. This supports read-only UI attribution without duplicating rules.
        /// </summary>
        public static bool DoesProgramSatisfyAchievementCondition(
            SpaceProgramState program,
            UnlockConditionDefinition condition,
            double evaluationUniversalTime)
        {
            if (program == null
                || condition == null
                || condition.ConditionType != UnlockConditionType.Achievement
                || string.IsNullOrEmpty(condition.MilestoneId)
                || double.IsNaN(evaluationUniversalTime)
                || !ProgramMatchesScope(program, condition.ProgramScope))
            {
                return false;
            }

            double achievementUniversalTime = program.GetAchievementUniversalTime(condition.MilestoneId);
            return achievementUniversalTime >= 0.0
                && achievementUniversalTime <= evaluationUniversalTime;
        }

        private static bool IsPathSatisfied(
            UnlockPathDefinition path,
            IList<SpaceProgramState> programs,
            double evaluationUniversalTime)
        {
            if (path == null || path.Conditions.Count == 0)
            {
                return false;
            }

            for (int conditionIndex = 0; conditionIndex < path.Conditions.Count; conditionIndex++)
            {
                UnlockConditionDefinition condition = path.Conditions[conditionIndex];
                if (!IsConditionSatisfied(condition, programs, evaluationUniversalTime))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ProgramMatchesScope(
            SpaceProgramState program,
            UnlockProgramScope programScope)
        {
            if (program == null)
            {
                return false;
            }

            switch (programScope)
            {
                case UnlockProgramScope.AnyAgency:
                    return true;

                case UnlockProgramScope.Player:
                    return program.IsPlayer;

                case UnlockProgramScope.AnyRival:
                    return !program.IsPlayer;

                default:
                    return false;
            }
        }
    }
}

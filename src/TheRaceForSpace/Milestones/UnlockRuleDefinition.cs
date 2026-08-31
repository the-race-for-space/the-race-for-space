using System;
using System.Collections.Generic;

namespace TheRaceForSpace.Milestones
{
    /// <summary>
    /// Supported requirement types for the first flexible unlock-rule implementation.
    /// Additional condition types should be added only when a concrete campaign need exists.
    /// </summary>
    public enum UnlockConditionType
    {
        Achievement,
        UniversalTime
    }

    /// <summary>
    /// Which space programs may satisfy an achievement-based unlock condition.
    /// </summary>
    public enum UnlockProgramScope
    {
        AnyAgency,
        Player,
        AnyRival
    }

    /// <summary>
    /// Immutable requirement inside one unlock path. Achievement conditions may require one or
    /// more qualifying agencies; universal-time conditions become true at a fixed campaign time.
    /// </summary>
    public sealed class UnlockConditionDefinition
    {
        private UnlockConditionDefinition(
            UnlockConditionType conditionType,
            UnlockProgramScope programScope,
            string milestoneId,
            int requiredProgramCount,
            double requiredUniversalTime)
        {
            ConditionType = conditionType;
            ProgramScope = programScope;
            MilestoneId = milestoneId;
            RequiredProgramCount = requiredProgramCount;
            RequiredUniversalTime = requiredUniversalTime;
        }

        public UnlockConditionType ConditionType { get; private set; }
        public UnlockProgramScope ProgramScope { get; private set; }
        public string MilestoneId { get; private set; }
        public int RequiredProgramCount { get; private set; }
        public double RequiredUniversalTime { get; private set; }

        /// <summary>
        /// Creates an achievement condition satisfied by one qualifying agency.
        /// </summary>
        public static UnlockConditionDefinition Achievement(
            string milestoneId,
            UnlockProgramScope programScope)
        {
            return Achievement(milestoneId, programScope, 1);
        }

        /// <summary>
        /// Creates an achievement condition requiring the supplied number of qualifying agencies.
        /// </summary>
        public static UnlockConditionDefinition Achievement(
            string milestoneId,
            UnlockProgramScope programScope,
            int requiredProgramCount)
        {
            if (string.IsNullOrEmpty(milestoneId))
            {
                throw new ArgumentException("An achievement unlock condition requires a milestone ID.", "milestoneId");
            }

            if (requiredProgramCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "requiredProgramCount",
                    "An achievement unlock condition must require at least one agency.");
            }

            return new UnlockConditionDefinition(
                UnlockConditionType.Achievement,
                programScope,
                milestoneId,
                requiredProgramCount,
                -1.0);
        }

        /// <summary>
        /// Creates a campaign-time condition that becomes satisfied at the supplied universal time.
        /// </summary>
        public static UnlockConditionDefinition AfterUniversalTime(double requiredUniversalTime)
        {
            if (double.IsNaN(requiredUniversalTime)
                || double.IsInfinity(requiredUniversalTime)
                || requiredUniversalTime < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    "requiredUniversalTime",
                    "An unlock time must be a finite, non-negative universal time.");
            }

            return new UnlockConditionDefinition(
                UnlockConditionType.UniversalTime,
                UnlockProgramScope.AnyAgency,
                null,
                0,
                requiredUniversalTime);
        }
    }

    /// <summary>
    /// One valid way to unlock a target. Every condition in the path must be satisfied (AND).
    /// </summary>
    public sealed class UnlockPathDefinition
    {
        private readonly IList<UnlockConditionDefinition> _conditions;

        public UnlockPathDefinition(params UnlockConditionDefinition[] conditions)
        {
            UnlockConditionDefinition[] copiedConditions = conditions == null
                ? new UnlockConditionDefinition[0]
                : (UnlockConditionDefinition[])conditions.Clone();
            _conditions = Array.AsReadOnly(copiedConditions);
        }

        public IList<UnlockConditionDefinition> Conditions
        {
            get { return _conditions; }
        }
    }

    /// <summary>
    /// Immutable target unlock definition. Any path may satisfy the rule (OR), while every
    /// condition inside the selected path must be satisfied (AND). A null rule is used by callers
    /// to represent a target that is available from the start of the campaign.
    /// </summary>
    public sealed class UnlockRuleDefinition
    {
        private readonly IList<UnlockPathDefinition> _paths;

        public UnlockRuleDefinition(params UnlockPathDefinition[] paths)
        {
            UnlockPathDefinition[] copiedPaths = paths == null
                ? new UnlockPathDefinition[0]
                : (UnlockPathDefinition[])paths.Clone();
            _paths = Array.AsReadOnly(copiedPaths);
        }

        public IList<UnlockPathDefinition> Paths
        {
            get { return _paths; }
        }

        /// <summary>
        /// Creates the simple one-condition rule used by every locked target in the current 0.4
        /// prototype: one achievement by any agency unlocks the target.
        /// </summary>
        public static UnlockRuleDefinition AnyAgencyAchievement(string milestoneId)
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        milestoneId,
                        UnlockProgramScope.AnyAgency)));
        }

        /// <summary>
        /// Returns the milestone ID when this rule is exactly the current prototype's simple
        /// one-agency achievement rule. Item 14B uses this as a temporary bridge for consumers
        /// that still read PrerequisiteMilestoneId; Item 14C will move those consumers to the
        /// shared evaluator and remove the bridge.
        /// </summary>
        internal bool TryGetSingleAnyAgencyAchievementMilestoneId(out string milestoneId)
        {
            milestoneId = null;
            if (_paths.Count != 1 || _paths[0] == null || _paths[0].Conditions.Count != 1)
            {
                return false;
            }

            UnlockConditionDefinition condition = _paths[0].Conditions[0];
            if (condition == null
                || condition.ConditionType != UnlockConditionType.Achievement
                || condition.ProgramScope != UnlockProgramScope.AnyAgency
                || condition.RequiredProgramCount != 1
                || string.IsNullOrEmpty(condition.MilestoneId))
            {
                return false;
            }

            milestoneId = condition.MilestoneId;
            return true;
        }
    }
}

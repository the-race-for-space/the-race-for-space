using System;
using System.Collections.Generic;

namespace TheRaceForSpace.Objectives
{
    /// <summary>
    /// Supported requirement types for the first flexible unlock-rule implementation.
    /// Additional condition types should be added only when a concrete campaign need exists.
    /// </summary>
    public enum UnlockConditionType
    {
        ObjectiveCompletion,
        UniversalTime,
        SatelliteCount
    }

    /// <summary>
    /// Which space agencies may satisfy an objective completion-based unlock condition.
    /// </summary>
    public enum UnlockAgencyScope
    {
        AnyAgency,
        Player,
        AnyRival
    }

    /// <summary>
    /// Immutable requirement inside one unlock path. ObjectiveCompletion conditions may require one or
    /// more qualifying agencies; universal-time conditions become true at a fixed campaign time;
    /// satellite-count conditions require a collective number of qualifying satellites around a body.
    /// </summary>
    public sealed class UnlockConditionDefinition
    {
        private UnlockConditionDefinition(
            UnlockConditionType conditionType,
            UnlockAgencyScope agencyScope,
            string objectiveId,
            int requiredAgencyCount,
            double requiredUniversalTime,
            string celestialBodyName,
            int requiredSatelliteCount)
        {
            ConditionType = conditionType;
            AgencyScope = agencyScope;
            ObjectiveId = objectiveId;
            RequiredAgencyCount = requiredAgencyCount;
            RequiredUniversalTime = requiredUniversalTime;
            CelestialBodyName = celestialBodyName;
            RequiredSatelliteCount = requiredSatelliteCount;
        }

        public UnlockConditionType ConditionType { get; private set; }
        public UnlockAgencyScope AgencyScope { get; private set; }
        public string ObjectiveId { get; private set; }
        public int RequiredAgencyCount { get; private set; }
        public double RequiredUniversalTime { get; private set; }
        public string CelestialBodyName { get; private set; }
        public int RequiredSatelliteCount { get; private set; }

        /// <summary>
        /// Creates an objective completion condition satisfied by one qualifying agency.
        /// </summary>
        public static UnlockConditionDefinition ObjectiveCompletion(
            string objectiveId,
            UnlockAgencyScope agencyScope)
        {
            return ObjectiveCompletion(objectiveId, agencyScope, 1);
        }

        /// <summary>
        /// Creates an objective completion condition requiring the supplied number of qualifying agencies.
        /// </summary>
        public static UnlockConditionDefinition ObjectiveCompletion(
            string objectiveId,
            UnlockAgencyScope agencyScope,
            int requiredAgencyCount)
        {
            if (string.IsNullOrEmpty(objectiveId))
            {
                throw new ArgumentException("An objective completion unlock condition requires a objective ID.", "objectiveId");
            }

            if (requiredAgencyCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "requiredAgencyCount",
                    "An objective completion unlock condition must require at least one agency.");
            }

            return new UnlockConditionDefinition(
                UnlockConditionType.ObjectiveCompletion,
                agencyScope,
                objectiveId,
                requiredAgencyCount,
                -1.0,
                null,
                0);
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
                UnlockAgencyScope.AnyAgency,
                null,
                0,
                requiredUniversalTime,
                null,
                0);
        }

        /// <summary>
        /// Creates a collective satellite-count condition across all campaign agencies for one body.
        /// </summary>
        public static UnlockConditionDefinition SatelliteCount(
            string celestialBodyName,
            int requiredSatelliteCount)
        {
            if (string.IsNullOrEmpty(celestialBodyName))
            {
                throw new ArgumentException(
                    "A satellite-count unlock condition requires a celestial body name.",
                    "celestialBodyName");
            }

            if (requiredSatelliteCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "requiredSatelliteCount",
                    "A satellite-count unlock condition must require at least one satellite.");
            }

            return new UnlockConditionDefinition(
                UnlockConditionType.SatelliteCount,
                UnlockAgencyScope.AnyAgency,
                null,
                0,
                -1.0,
                celestialBodyName,
                requiredSatelliteCount);
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
        /// Creates a simple one-condition rule where one objective completion by any agency unlocks the target.
        /// </summary>
        public static UnlockRuleDefinition AnyAgencyObjectiveCompletion(string objectiveId)
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        objectiveId,
                        UnlockAgencyScope.AnyAgency)));
        }
    }
}

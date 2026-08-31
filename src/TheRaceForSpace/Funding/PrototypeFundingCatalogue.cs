using System;
using System.Collections.Generic;
using TheRaceForSpace.Milestones;

namespace TheRaceForSpace.Funding
{
    /// <summary>
    /// Defines the code-owned funding programme set for the current prototype.
    /// Adding a new prototype funding target belongs here rather than in the race controller.
    /// </summary>
    public static class PrototypeFundingCatalogue
    {
        public const string KerbinNetworkId = "kerbin-network";
        public const string MunNetworkId = "mun-survey";
        public const string MinmusNetworkId = "minmus-relay";
        public const string DunaNetworkId = "duna-network";

        /// <summary>
        /// Creates fresh achievement funding state for a new race controller from the milestone catalogue.
        /// </summary>
        public static IList<AchievementFundingProgramme> CreateAchievementProgrammes()
        {
            var programmes = new List<AchievementFundingProgramme>();

            AddAchievementProgramme(programmes, PrototypeMilestones.ProbeOrbitId, 100000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.CrewedOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.MunProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.MinmusProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.DunaProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.MunCrewedOrbitId, 300000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.MinmusCrewedOrbitId, 300000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.DunaCrewedOrbitId, 300000.0);

            return programmes;
        }

        /// <summary>
        /// Creates fresh satellite-network funding state for a new race controller.
        /// </summary>
        public static IList<FundingProgramme> CreateSatelliteProgrammes()
        {
            return new List<FundingProgramme>
            {
                CreateSatelliteProgramme(
                    KerbinNetworkId,
                    "Kerbin Orbital Network",
                    "Kerbin",
                    10,
                    200000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.ProbeOrbitId)),
                CreateSatelliteProgramme(
                    MunNetworkId,
                    "Mun Survey Network",
                    "Mun",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.MunProbeOrbitId)),
                CreateSatelliteProgramme(
                    MinmusNetworkId,
                    "Minmus Relay Initiative",
                    "Minmus",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.MinmusProbeOrbitId)),
                CreateSatelliteProgramme(
                    DunaNetworkId,
                    "Duna Orbital Network",
                    "Duna",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.DunaProbeOrbitId))
            };
        }

        private static void AddAchievementProgramme(
            IList<AchievementFundingProgramme> programmes,
            string milestoneId,
            double baseRewardFunds)
        {
            MilestoneDefinition milestone = FindRequiredMilestone(milestoneId);
            if (milestone.UnlockRule == null)
            {
                programmes.Add(new AchievementFundingProgramme(
                    milestone.Id,
                    milestone.Name,
                    milestone.ObjectiveDescription,
                    baseRewardFunds));
                return;
            }

            programmes.Add(new AchievementFundingProgramme(
                milestone.Id,
                milestone.Name,
                milestone.ObjectiveDescription,
                baseRewardFunds,
                CreateCurrentPrototypeUnlockRequirement(milestone.UnlockRule),
                milestone.UnlockRule));
        }

        private static FundingProgramme CreateSatelliteProgramme(
            string id,
            string name,
            string celestialBodyName,
            int requiredSatellites,
            double rewardFunds,
            UnlockRuleDefinition unlockRule)
        {
            return new FundingProgramme(
                id,
                name,
                celestialBodyName,
                requiredSatellites,
                rewardFunds,
                false,
                CreateCurrentPrototypeUnlockRequirement(unlockRule),
                unlockRule);
        }

        private static string CreateCurrentPrototypeUnlockRequirement(UnlockRuleDefinition unlockRule)
        {
            if (unlockRule == null)
            {
                return "Available from the start of the campaign";
            }

            // Item 14C keeps the current 0.4 presentation text unchanged while gameplay rules are
            // evaluated only by UnlockRuleEvaluator. Item 14D will provide richer formatting for
            // arbitrary OR/AND rule paths in the strategic UI.
            if (unlockRule.Paths.Count != 1
                || unlockRule.Paths[0] == null
                || unlockRule.Paths[0].Conditions.Count != 1)
            {
                throw new InvalidOperationException(
                    "Current prototype funding text requires one simple achievement unlock condition.");
            }

            UnlockConditionDefinition condition = unlockRule.Paths[0].Conditions[0];
            if (condition == null
                || condition.ConditionType != UnlockConditionType.Achievement
                || condition.ProgramScope != UnlockProgramScope.AnyAgency
                || condition.RequiredProgramCount != 1
                || string.IsNullOrEmpty(condition.MilestoneId))
            {
                throw new InvalidOperationException(
                    "Current prototype funding text requires one AnyAgency achievement condition.");
            }

            MilestoneDefinition prerequisite = FindRequiredMilestone(condition.MilestoneId);
            return "Any agency must achieve " + prerequisite.Name + ".";
        }

        private static MilestoneDefinition FindRequiredMilestone(string milestoneId)
        {
            MilestoneDefinition milestone = PrototypeMilestones.FindById(milestoneId);
            if (milestone == null)
            {
                throw new InvalidOperationException(
                    "Prototype funding catalogue references unknown milestone '" + milestoneId + "'.");
            }

            return milestone;
        }
    }
}

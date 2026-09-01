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
        public const string MohoNetworkId = "moho-network";
        public const string EveNetworkId = "eve-network";
        public const string GillyNetworkId = "gilly-network";
        public const string IkeNetworkId = "ike-network";
        public const string DresNetworkId = "dres-network";
        public const string JoolNetworkId = "jool-network";
        public const string LaytheNetworkId = "laythe-network";
        public const string VallNetworkId = "vall-network";
        public const string TyloNetworkId = "tylo-network";
        public const string BopNetworkId = "bop-network";
        public const string PolNetworkId = "pol-network";
        public const string EelooNetworkId = "eeloo-network";

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
            AddAchievementProgramme(programmes, PrototypeMilestones.MohoProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.MohoCrewedOrbitId, 300000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.EveProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.EveCrewedOrbitId, 300000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.GillyProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.GillyCrewedOrbitId, 300000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.IkeProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.IkeCrewedOrbitId, 300000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.DresProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.DresCrewedOrbitId, 300000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.JoolProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.JoolCrewedOrbitId, 300000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.LaytheProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.LaytheCrewedOrbitId, 300000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.VallProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.VallCrewedOrbitId, 300000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.TyloProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.TyloCrewedOrbitId, 300000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.BopProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.BopCrewedOrbitId, 300000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.PolProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.PolCrewedOrbitId, 300000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.EelooProbeOrbitId, 200000.0);
            AddAchievementProgramme(programmes, PrototypeMilestones.EelooCrewedOrbitId, 300000.0);

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
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.DunaProbeOrbitId)),
                CreateSatelliteProgramme(
                    MohoNetworkId,
                    "Moho Orbital Network",
                    "Moho",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.MohoProbeOrbitId)),
                CreateSatelliteProgramme(
                    EveNetworkId,
                    "Eve Orbital Network",
                    "Eve",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.EveProbeOrbitId)),
                CreateSatelliteProgramme(
                    GillyNetworkId,
                    "Gilly Relay/Survey Network",
                    "Gilly",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.GillyProbeOrbitId)),
                CreateSatelliteProgramme(
                    IkeNetworkId,
                    "Ike Relay/Survey Network",
                    "Ike",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.IkeProbeOrbitId)),
                CreateSatelliteProgramme(
                    DresNetworkId,
                    "Dres Orbital Network",
                    "Dres",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.DresProbeOrbitId)),
                CreateSatelliteProgramme(
                    JoolNetworkId,
                    "Jool Orbital Network",
                    "Jool",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.JoolProbeOrbitId)),
                CreateSatelliteProgramme(
                    LaytheNetworkId,
                    "Laythe Orbital Network",
                    "Laythe",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.LaytheProbeOrbitId)),
                CreateSatelliteProgramme(
                    VallNetworkId,
                    "Vall Relay/Survey Network",
                    "Vall",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.VallProbeOrbitId)),
                CreateSatelliteProgramme(
                    TyloNetworkId,
                    "Tylo Relay/Survey Network",
                    "Tylo",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.TyloProbeOrbitId)),
                CreateSatelliteProgramme(
                    BopNetworkId,
                    "Bop Relay/Survey Network",
                    "Bop",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.BopProbeOrbitId)),
                CreateSatelliteProgramme(
                    PolNetworkId,
                    "Pol Relay/Survey Network",
                    "Pol",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.PolProbeOrbitId)),
                CreateSatelliteProgramme(
                    EelooNetworkId,
                    "Eeloo Orbital Network",
                    "Eeloo",
                    5,
                    100000.0,
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.EelooProbeOrbitId))
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

            // Availability is always decided by UnlockRuleEvaluator. This text is only a concise
            // description for funding-programme presentation paths that still expose UnlockRequirement.
            if (unlockRule.Paths.Count != 1
                || unlockRule.Paths[0] == null
                || unlockRule.Paths[0].Conditions.Count == 0)
            {
                throw new InvalidOperationException(
                    "Current prototype funding text requires one AnyAgency achievement unlock path.");
            }

            UnlockPathDefinition path = unlockRule.Paths[0];
            var prerequisiteNames = new string[path.Conditions.Count];
            for (int conditionIndex = 0; conditionIndex < path.Conditions.Count; conditionIndex++)
            {
                UnlockConditionDefinition condition = path.Conditions[conditionIndex];
                if (condition == null
                    || condition.ConditionType != UnlockConditionType.Achievement
                    || condition.ProgramScope != UnlockProgramScope.AnyAgency
                    || condition.RequiredProgramCount != 1
                    || string.IsNullOrEmpty(condition.MilestoneId))
                {
                    throw new InvalidOperationException(
                        "Current prototype funding text requires AnyAgency achievement conditions.");
                }

                prerequisiteNames[conditionIndex] = FindRequiredMilestone(condition.MilestoneId).Name;
            }

            return "Any agency must achieve " + string.Join(" and ", prerequisiteNames) + ".";
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

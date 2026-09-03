using System;
using System.Collections.Generic;
using TheRaceForSpace.Core;
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

        private const double NetworkUnlockCompletionRatio = 0.60;

        /// <summary>
        /// Creates fresh achievement funding state for a new race controller from the milestone catalogue.
        /// Level-one starter milestones are the four opening sponsor offers for a new campaign.
        /// </summary>
        public static IList<AchievementFundingProgramme> CreateAchievementProgrammes()
        {
            var programmes = new List<AchievementFundingProgramme>();

            for (int milestoneIndex = 0; milestoneIndex < PrototypeMilestones.All.Count; milestoneIndex++)
            {
                AddAchievementProgramme(programmes, PrototypeMilestones.All[milestoneIndex]);
            }

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
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.ProbeOrbitId)),
                CreateSatelliteProgramme(
                    MunNetworkId,
                    "Mun Survey Network",
                    "Mun",
                    CreateKerbinMoonNetworkUnlockRule(PrototypeMilestones.MunProbeOrbitId)),
                CreateSatelliteProgramme(
                    MinmusNetworkId,
                    "Minmus Relay Initiative",
                    "Minmus",
                    CreateKerbinMoonNetworkUnlockRule(PrototypeMilestones.MinmusProbeOrbitId)),
                CreateSatelliteProgramme(
                    DunaNetworkId,
                    "Duna Orbital Network",
                    "Duna",
                    CreateInterplanetaryPlanetNetworkUnlockRule(PrototypeMilestones.DunaProbeOrbitId)),
                CreateSatelliteProgramme(
                    MohoNetworkId,
                    "Moho Orbital Network",
                    "Moho",
                    CreateInterplanetaryPlanetNetworkUnlockRule(PrototypeMilestones.MohoProbeOrbitId)),
                CreateSatelliteProgramme(
                    EveNetworkId,
                    "Eve Orbital Network",
                    "Eve",
                    CreateInterplanetaryPlanetNetworkUnlockRule(PrototypeMilestones.EveProbeOrbitId)),
                CreateSatelliteProgramme(
                    GillyNetworkId,
                    "Gilly Relay/Survey Network",
                    "Gilly",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        PrototypeMilestones.GillyProbeOrbitId,
                        "Eve")),
                CreateSatelliteProgramme(
                    IkeNetworkId,
                    "Ike Relay/Survey Network",
                    "Ike",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        PrototypeMilestones.IkeProbeOrbitId,
                        "Duna")),
                CreateSatelliteProgramme(
                    DresNetworkId,
                    "Dres Orbital Network",
                    "Dres",
                    CreateInterplanetaryPlanetNetworkUnlockRule(PrototypeMilestones.DresProbeOrbitId)),
                CreateSatelliteProgramme(
                    JoolNetworkId,
                    "Jool Orbital Network",
                    "Jool",
                    CreateInterplanetaryPlanetNetworkUnlockRule(PrototypeMilestones.JoolProbeOrbitId)),
                CreateSatelliteProgramme(
                    LaytheNetworkId,
                    "Laythe Orbital Network",
                    "Laythe",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        PrototypeMilestones.LaytheProbeOrbitId,
                        "Jool")),
                CreateSatelliteProgramme(
                    VallNetworkId,
                    "Vall Relay/Survey Network",
                    "Vall",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        PrototypeMilestones.VallProbeOrbitId,
                        "Jool")),
                CreateSatelliteProgramme(
                    TyloNetworkId,
                    "Tylo Relay/Survey Network",
                    "Tylo",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        PrototypeMilestones.TyloProbeOrbitId,
                        "Jool")),
                CreateSatelliteProgramme(
                    BopNetworkId,
                    "Bop Relay/Survey Network",
                    "Bop",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        PrototypeMilestones.BopProbeOrbitId,
                        "Jool")),
                CreateSatelliteProgramme(
                    PolNetworkId,
                    "Pol Relay/Survey Network",
                    "Pol",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        PrototypeMilestones.PolProbeOrbitId,
                        "Jool")),
                CreateSatelliteProgramme(
                    EelooNetworkId,
                    "Eeloo Orbital Network",
                    "Eeloo",
                    CreateInterplanetaryPlanetNetworkUnlockRule(PrototypeMilestones.EelooProbeOrbitId))
            };
        }

        private static void AddAchievementProgramme(
            IList<AchievementFundingProgramme> programmes,
            MilestoneDefinition milestone)
        {
            RaceBodySettings bodySettings = RaceSettings.GetBodySettings(milestone.CelestialBodyName);
            double baseRewardFunds = milestone.IsStarterContract
                ? milestone.BaseRewardFunds
                : milestone.CrewRequirement == MilestoneCrewRequirement.Crewed
                    ? bodySettings.CrewedRewardFunds
                    : bodySettings.ProbeRewardFunds;

            AchievementFundingProgramme programme;
            if (milestone.UnlockRule == null)
            {
                programme = new AchievementFundingProgramme(
                    milestone.Id,
                    milestone.Name,
                    milestone.ObjectiveDescription,
                    baseRewardFunds);
            }
            else
            {
                programme = new AchievementFundingProgramme(
                    milestone.Id,
                    milestone.Name,
                    milestone.ObjectiveDescription,
                    baseRewardFunds,
                    CreateCurrentPrototypeUnlockRequirement(milestone.UnlockRule),
                    milestone.UnlockRule);
            }

            // The four starter lines remain outside the normal two-offer sponsor pool. Their first
            // milestones are bootstrap offers; later levels are offered deterministically by the controller.
            if (milestone.IsStarterContract && milestone.StarterLevel == 1)
            {
                programme.Offer();
            }

            programmes.Add(programme);
        }

        private static FundingProgramme CreateSatelliteProgramme(
            string id,
            string name,
            string celestialBodyName,
            UnlockRuleDefinition unlockRule)
        {
            RaceBodySettings bodySettings = RaceSettings.GetBodySettings(celestialBodyName);
            return new FundingProgramme(
                id,
                name,
                celestialBodyName,
                bodySettings.SatelliteNetworkSize,
                bodySettings.SatelliteNetworkValueFunds,
                false,
                CreateCurrentPrototypeUnlockRequirement(unlockRule),
                unlockRule);
        }

        private static UnlockRuleDefinition CreateKerbinMoonNetworkUnlockRule(
            string probeMilestoneId)
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        probeMilestoneId,
                        UnlockProgramScope.AnyAgency),
                    CreateNetworkProgressCondition("Kerbin")));
        }

        private static UnlockRuleDefinition CreateInterplanetaryPlanetNetworkUnlockRule(
            string probeMilestoneId)
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        probeMilestoneId,
                        UnlockProgramScope.AnyAgency),
                    CreateNetworkProgressCondition("Mun"),
                    CreateNetworkProgressCondition("Minmus")));
        }

        private static UnlockRuleDefinition CreatePlanetaryMoonNetworkUnlockRule(
            string probeMilestoneId,
            string parentPlanetName)
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        probeMilestoneId,
                        UnlockProgramScope.AnyAgency),
                    CreateNetworkProgressCondition(parentPlanetName)));
        }

        private static UnlockConditionDefinition CreateNetworkProgressCondition(
            string celestialBodyName)
        {
            RaceBodySettings bodySettings = RaceSettings.GetBodySettings(celestialBodyName);
            int requiredSatelliteCount = Math.Max(
                1,
                (int)Math.Ceiling(
                    bodySettings.SatelliteNetworkSize * NetworkUnlockCompletionRatio));

            return UnlockConditionDefinition.SatelliteCount(
                celestialBodyName,
                requiredSatelliteCount);
        }

        private static string CreateCurrentPrototypeUnlockRequirement(UnlockRuleDefinition unlockRule)
        {
            if (unlockRule == null)
            {
                return "Available from the start of the campaign";
            }

            // Probe Orbit is the first rule with several one-condition OR paths. Keep the existing
            // presentation formatter deliberately narrow: multi-path text supports only the simple
            // AnyAgency achievement alternatives used by that gate.
            if (unlockRule.Paths.Count > 1)
            {
                string alternatives = null;
                for (int pathIndex = 0; pathIndex < unlockRule.Paths.Count; pathIndex++)
                {
                    UnlockPathDefinition alternativePath = unlockRule.Paths[pathIndex];
                    if (alternativePath == null || alternativePath.Conditions.Count != 1)
                    {
                        throw new InvalidOperationException(
                            "Current prototype OR funding text requires one condition per unlock path.");
                    }

                    UnlockConditionDefinition condition = alternativePath.Conditions[0];
                    if (!IsSingleAgencyAchievementCondition(condition))
                    {
                        throw new InvalidOperationException(
                            "Current prototype OR funding text supports AnyAgency achievements only.");
                    }

                    if (!string.IsNullOrEmpty(alternatives))
                    {
                        alternatives += " or ";
                    }

                    alternatives += FindRequiredMilestone(condition.MilestoneId).Name;
                }

                return "Any agency must achieve " + alternatives + ".";
            }

            if (unlockRule.Paths.Count != 1
                || unlockRule.Paths[0] == null
                || unlockRule.Paths[0].Conditions.Count == 0)
            {
                throw new InvalidOperationException(
                    "Current prototype funding text requires at least one unlock path.");
            }

            UnlockPathDefinition path = unlockRule.Paths[0];
            UnlockConditionDefinition firstCondition = path.Conditions[0];
            if (!IsSingleAgencyAchievementCondition(firstCondition))
            {
                throw new InvalidOperationException(
                    "Current prototype funding text requires an AnyAgency achievement as its first condition.");
            }

            string unlockRequirement =
                "Any agency must achieve " + FindRequiredMilestone(firstCondition.MilestoneId).Name;

            for (int conditionIndex = 1; conditionIndex < path.Conditions.Count; conditionIndex++)
            {
                UnlockConditionDefinition condition = path.Conditions[conditionIndex];
                if (IsSingleAgencyAchievementCondition(condition))
                {
                    unlockRequirement += " and " + FindRequiredMilestone(condition.MilestoneId).Name;
                    continue;
                }

                if (condition != null
                    && condition.ConditionType == UnlockConditionType.SatelliteCount
                    && !string.IsNullOrEmpty(condition.CelestialBodyName)
                    && condition.RequiredSatelliteCount > 0)
                {
                    unlockRequirement +=
                        " and the "
                        + condition.CelestialBodyName
                        + " satellite network must reach "
                        + condition.RequiredSatelliteCount
                        + " qualifying satellites";
                    continue;
                }

                throw new InvalidOperationException(
                    "Current prototype funding text supports AnyAgency achievements and satellite-count conditions.");
            }

            return unlockRequirement + ".";
        }

        private static bool IsSingleAgencyAchievementCondition(UnlockConditionDefinition condition)
        {
            return condition != null
                && condition.ConditionType == UnlockConditionType.Achievement
                && condition.ProgramScope == UnlockProgramScope.AnyAgency
                && condition.RequiredProgramCount == 1
                && !string.IsNullOrEmpty(condition.MilestoneId);
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

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
        private const double KerbinProbeRewardFunds = 75000.0;
        private const double KerbinCrewedRewardFunds = 150000.0;
        private const double KerbinMoonProbeRewardFunds = 150000.0;
        private const double KerbinMoonCrewedRewardFunds = 300000.0;
        private const double InterplanetaryProbeRewardFunds = 300000.0;
        private const double InterplanetaryCrewedRewardFunds = 500000.0;
        private const int PlanetNetworkRequiredSatellites = 10;
        private const int MoonNetworkRequiredSatellites = 5;
        private const double PlanetNetworkRewardFunds = 200000.0;
        private const double MoonNetworkRewardFunds = 100000.0;

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

            // Achievement rewards are derived from the milestone body and crew requirement so a
            // newly added milestone automatically uses the current campaign balance defaults.
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
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.MunProbeOrbitId)),
                CreateSatelliteProgramme(
                    MinmusNetworkId,
                    "Minmus Relay Initiative",
                    "Minmus",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.MinmusProbeOrbitId)),
                CreateSatelliteProgramme(
                    DunaNetworkId,
                    "Duna Orbital Network",
                    "Duna",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.DunaProbeOrbitId)),
                CreateSatelliteProgramme(
                    MohoNetworkId,
                    "Moho Orbital Network",
                    "Moho",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.MohoProbeOrbitId)),
                CreateSatelliteProgramme(
                    EveNetworkId,
                    "Eve Orbital Network",
                    "Eve",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.EveProbeOrbitId)),
                CreateSatelliteProgramme(
                    GillyNetworkId,
                    "Gilly Relay/Survey Network",
                    "Gilly",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.GillyProbeOrbitId)),
                CreateSatelliteProgramme(
                    IkeNetworkId,
                    "Ike Relay/Survey Network",
                    "Ike",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.IkeProbeOrbitId)),
                CreateSatelliteProgramme(
                    DresNetworkId,
                    "Dres Orbital Network",
                    "Dres",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.DresProbeOrbitId)),
                CreateSatelliteProgramme(
                    JoolNetworkId,
                    "Jool Orbital Network",
                    "Jool",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.JoolProbeOrbitId)),
                CreateSatelliteProgramme(
                    LaytheNetworkId,
                    "Laythe Orbital Network",
                    "Laythe",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.LaytheProbeOrbitId)),
                CreateSatelliteProgramme(
                    VallNetworkId,
                    "Vall Relay/Survey Network",
                    "Vall",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.VallProbeOrbitId)),
                CreateSatelliteProgramme(
                    TyloNetworkId,
                    "Tylo Relay/Survey Network",
                    "Tylo",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.TyloProbeOrbitId)),
                CreateSatelliteProgramme(
                    BopNetworkId,
                    "Bop Relay/Survey Network",
                    "Bop",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.BopProbeOrbitId)),
                CreateSatelliteProgramme(
                    PolNetworkId,
                    "Pol Relay/Survey Network",
                    "Pol",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.PolProbeOrbitId)),
                CreateSatelliteProgramme(
                    EelooNetworkId,
                    "Eeloo Orbital Network",
                    "Eeloo",
                    UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.EelooProbeOrbitId))
            };
        }

        private static void AddAchievementProgramme(
            IList<AchievementFundingProgramme> programmes,
            MilestoneDefinition milestone)
        {
            double baseRewardFunds = GetAchievementRewardFunds(milestone);
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

        private static double GetAchievementRewardFunds(MilestoneDefinition milestone)
        {
            bool isCrewed = milestone.CrewRequirement == MilestoneCrewRequirement.Crewed;
            if (string.Equals(milestone.CelestialBodyName, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                return isCrewed ? KerbinCrewedRewardFunds : KerbinProbeRewardFunds;
            }

            if (string.Equals(milestone.CelestialBodyName, "Mun", StringComparison.OrdinalIgnoreCase)
                || string.Equals(milestone.CelestialBodyName, "Minmus", StringComparison.OrdinalIgnoreCase))
            {
                return isCrewed ? KerbinMoonCrewedRewardFunds : KerbinMoonProbeRewardFunds;
            }

            return isCrewed ? InterplanetaryCrewedRewardFunds : InterplanetaryProbeRewardFunds;
        }

        private static FundingProgramme CreateSatelliteProgramme(
            string id,
            string name,
            string celestialBodyName,
            UnlockRuleDefinition unlockRule)
        {
            bool isMoon = IsStockMoon(celestialBodyName);
            return new FundingProgramme(
                id,
                name,
                celestialBodyName,
                isMoon ? MoonNetworkRequiredSatellites : PlanetNetworkRequiredSatellites,
                isMoon ? MoonNetworkRewardFunds : PlanetNetworkRewardFunds,
                false,
                CreateCurrentPrototypeUnlockRequirement(unlockRule),
                unlockRule);
        }

        private static bool IsStockMoon(string celestialBodyName)
        {
            return string.Equals(celestialBodyName, "Mun", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Minmus", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Gilly", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Ike", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Laythe", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Vall", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Tylo", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Bop", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Pol", StringComparison.OrdinalIgnoreCase);
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

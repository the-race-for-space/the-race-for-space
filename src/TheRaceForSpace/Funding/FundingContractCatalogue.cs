using System;
using System.Collections.Generic;
using TheRaceForSpace.Core;
using TheRaceForSpace.Objectives;

namespace TheRaceForSpace.Funding
{
    /// <summary>
    /// Defines the code-owned funding programme set for the current prototype.
    /// Adding a new prototype funding target belongs here rather than in the race controller.
    /// </summary>
    public static class FundingContractCatalogue
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
        /// Creates fresh objectiveCompletion funding state for a new race controller from both the special
        /// starter catalogue and the normal orbital objective catalogue.
        /// </summary>
        public static IList<ObjectiveFundingContract> CreateAchievementProgrammes()
        {
            var programmes = new List<ObjectiveFundingContract>(
                ObjectiveCatalogue.PreOrbitContracts.Count + ObjectiveCatalogue.All.Count);

            for (int milestoneIndex = 0;
                milestoneIndex < ObjectiveCatalogue.PreOrbitContracts.Count;
                milestoneIndex++)
            {
                AddAchievementProgramme(programmes, ObjectiveCatalogue.PreOrbitContracts[milestoneIndex]);
            }

            for (int milestoneIndex = 0; milestoneIndex < ObjectiveCatalogue.All.Count; milestoneIndex++)
            {
                AddAchievementProgramme(programmes, ObjectiveCatalogue.All[milestoneIndex]);
            }

            return programmes;
        }

        /// <summary>
        /// Creates fresh satellite-network funding state for a new race controller.
        /// </summary>
        public static IList<SatelliteNetworkFundingContract> CreateSatelliteProgrammes()
        {
            return new List<SatelliteNetworkFundingContract>
            {
                CreateSatelliteProgramme(
                    KerbinNetworkId,
                    "Kerbin Orbital Network",
                    "Kerbin",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId)),
                CreateSatelliteProgramme(
                    MunNetworkId,
                    "Mun Survey Network",
                    "Mun",
                    CreateKerbinMoonNetworkUnlockRule(ObjectiveCatalogue.MunProbeOrbitId)),
                CreateSatelliteProgramme(
                    MinmusNetworkId,
                    "Minmus Relay Initiative",
                    "Minmus",
                    CreateKerbinMoonNetworkUnlockRule(ObjectiveCatalogue.MinmusProbeOrbitId)),
                CreateSatelliteProgramme(
                    DunaNetworkId,
                    "Duna Orbital Network",
                    "Duna",
                    CreateInterplanetaryPlanetNetworkUnlockRule(ObjectiveCatalogue.DunaProbeOrbitId)),
                CreateSatelliteProgramme(
                    MohoNetworkId,
                    "Moho Orbital Network",
                    "Moho",
                    CreateInterplanetaryPlanetNetworkUnlockRule(ObjectiveCatalogue.MohoProbeOrbitId)),
                CreateSatelliteProgramme(
                    EveNetworkId,
                    "Eve Orbital Network",
                    "Eve",
                    CreateInterplanetaryPlanetNetworkUnlockRule(ObjectiveCatalogue.EveProbeOrbitId)),
                CreateSatelliteProgramme(
                    GillyNetworkId,
                    "Gilly Relay/Survey Network",
                    "Gilly",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.GillyProbeOrbitId,
                        "Eve")),
                CreateSatelliteProgramme(
                    IkeNetworkId,
                    "Ike Relay/Survey Network",
                    "Ike",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.IkeProbeOrbitId,
                        "Duna")),
                CreateSatelliteProgramme(
                    DresNetworkId,
                    "Dres Orbital Network",
                    "Dres",
                    CreateInterplanetaryPlanetNetworkUnlockRule(ObjectiveCatalogue.DresProbeOrbitId)),
                CreateSatelliteProgramme(
                    JoolNetworkId,
                    "Jool Orbital Network",
                    "Jool",
                    CreateInterplanetaryPlanetNetworkUnlockRule(ObjectiveCatalogue.JoolProbeOrbitId)),
                CreateSatelliteProgramme(
                    LaytheNetworkId,
                    "Laythe Orbital Network",
                    "Laythe",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.LaytheProbeOrbitId,
                        "Jool")),
                CreateSatelliteProgramme(
                    VallNetworkId,
                    "Vall Relay/Survey Network",
                    "Vall",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.VallProbeOrbitId,
                        "Jool")),
                CreateSatelliteProgramme(
                    TyloNetworkId,
                    "Tylo Relay/Survey Network",
                    "Tylo",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.TyloProbeOrbitId,
                        "Jool")),
                CreateSatelliteProgramme(
                    BopNetworkId,
                    "Bop Relay/Survey Network",
                    "Bop",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.BopProbeOrbitId,
                        "Jool")),
                CreateSatelliteProgramme(
                    PolNetworkId,
                    "Pol Relay/Survey Network",
                    "Pol",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.PolProbeOrbitId,
                        "Jool")),
                CreateSatelliteProgramme(
                    EelooNetworkId,
                    "Eeloo Orbital Network",
                    "Eeloo",
                    CreateInterplanetaryPlanetNetworkUnlockRule(ObjectiveCatalogue.EelooProbeOrbitId))
            };
        }

        private static void AddAchievementProgramme(
            IList<ObjectiveFundingContract> programmes,
            ObjectiveDefinition objective)
        {
            double baseRewardFunds = objective.BaseRewardFunds;
            if (!objective.IsPreOrbitContract)
            {
                BodyBalanceSettings bodySettings = CampaignSettings.GetBodySettings(objective.CelestialBodyName);
                baseRewardFunds = objective.CrewRequirement == ObjectiveCrewRequirement.Crewed
                    ? bodySettings.CrewedRewardFunds
                    : bodySettings.ProbeRewardFunds;
            }

            var programme = new ObjectiveFundingContract(
                objective.Id,
                objective.Name,
                objective.ObjectiveDescription,
                baseRewardFunds,
                CreateUnlockRequirementText(objective.UnlockRule),
                objective.UnlockRule);

            // The four starter lines remain outside the normal two-offer sponsor pool. Their first
            // objectives are bootstrap offers; later levels are offered deterministically by the controller.
            if (objective.IsPreOrbitContract && objective.PreOrbitLevel == 1)
            {
                programme.Offer();
            }

            programmes.Add(programme);
        }

        private static SatelliteNetworkFundingContract CreateSatelliteProgramme(
            string id,
            string name,
            string celestialBodyName,
            UnlockRuleDefinition unlockRule)
        {
            BodyBalanceSettings bodySettings = CampaignSettings.GetBodySettings(celestialBodyName);
            return new SatelliteNetworkFundingContract(
                id,
                name,
                celestialBodyName,
                bodySettings.SatelliteNetworkSize,
                bodySettings.SatelliteNetworkValueFunds,
                false,
                CreateUnlockRequirementText(unlockRule),
                unlockRule);
        }

        private static UnlockRuleDefinition CreateKerbinMoonNetworkUnlockRule(
            string probeObjectiveId)
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        probeObjectiveId,
                        UnlockAgencyScope.AnyAgency),
                    CreateNetworkProgressCondition("Kerbin")));
        }

        private static UnlockRuleDefinition CreateInterplanetaryPlanetNetworkUnlockRule(
            string probeObjectiveId)
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        probeObjectiveId,
                        UnlockAgencyScope.AnyAgency),
                    CreateNetworkProgressCondition("Mun"),
                    CreateNetworkProgressCondition("Minmus")));
        }

        private static UnlockRuleDefinition CreatePlanetaryMoonNetworkUnlockRule(
            string probeObjectiveId,
            string parentPlanetName)
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        probeObjectiveId,
                        UnlockAgencyScope.AnyAgency),
                    CreateNetworkProgressCondition(parentPlanetName)));
        }

        private static UnlockConditionDefinition CreateNetworkProgressCondition(
            string celestialBodyName)
        {
            BodyBalanceSettings bodySettings = CampaignSettings.GetBodySettings(celestialBodyName);
            int requiredSatelliteCount = Math.Max(
                1,
                (int)Math.Ceiling(
                    bodySettings.SatelliteNetworkSize * NetworkUnlockCompletionRatio));

            return UnlockConditionDefinition.SatelliteCount(
                celestialBodyName,
                requiredSatelliteCount);
        }

        private static string CreateUnlockRequirementText(UnlockRuleDefinition unlockRule)
        {
            if (unlockRule == null)
            {
                return "Available from the start of the campaign";
            }

            if (unlockRule.Paths.Count > 1)
            {
                string alternatives = null;
                for (int pathIndex = 0; pathIndex < unlockRule.Paths.Count; pathIndex++)
                {
                    UnlockPathDefinition alternativePath = unlockRule.Paths[pathIndex];
                    if (alternativePath == null || alternativePath.Conditions.Count != 1)
                    {
                        throw new InvalidOperationException(
                            "Funding OR text requires one condition per unlock path.");
                    }

                    UnlockConditionDefinition condition = alternativePath.Conditions[0];
                    if (!IsSingleAgencyAchievementCondition(condition))
                    {
                        throw new InvalidOperationException(
                            "Funding OR text supports AnyAgency achievements only.");
                    }

                    if (!string.IsNullOrEmpty(alternatives))
                    {
                        alternatives += " or ";
                    }

                    alternatives += FindRequiredMilestone(condition.ObjectiveId).Name;
                }

                return "Any agency must achieve " + alternatives + ".";
            }

            if (unlockRule.Paths.Count != 1
                || unlockRule.Paths[0] == null
                || unlockRule.Paths[0].Conditions.Count == 0)
            {
                throw new InvalidOperationException(
                    "Funding unlock text requires at least one unlock path.");
            }

            UnlockPathDefinition path = unlockRule.Paths[0];
            UnlockConditionDefinition firstCondition = path.Conditions[0];
            if (!IsSingleAgencyAchievementCondition(firstCondition))
            {
                throw new InvalidOperationException(
                    "Funding unlock text requires an AnyAgency objectiveCompletion as its first condition.");
            }

            string unlockRequirement =
                "Any agency must achieve " + FindRequiredMilestone(firstCondition.ObjectiveId).Name;

            for (int conditionIndex = 1; conditionIndex < path.Conditions.Count; conditionIndex++)
            {
                UnlockConditionDefinition condition = path.Conditions[conditionIndex];
                if (IsSingleAgencyAchievementCondition(condition))
                {
                    unlockRequirement += " and " + FindRequiredMilestone(condition.ObjectiveId).Name;
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
                    "Funding unlock text supports AnyAgency achievements and satellite-count conditions.");
            }

            return unlockRequirement + ".";
        }

        private static bool IsSingleAgencyAchievementCondition(UnlockConditionDefinition condition)
        {
            return condition != null
                && condition.ConditionType == UnlockConditionType.ObjectiveCompletion
                && condition.AgencyScope == UnlockAgencyScope.AnyAgency
                && condition.RequiredAgencyCount == 1
                && !string.IsNullOrEmpty(condition.ObjectiveId);
        }

        private static ObjectiveDefinition FindRequiredMilestone(string objectiveId)
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(objectiveId);
            if (objective == null)
            {
                throw new InvalidOperationException(
                    "Prototype funding catalogue references unknown objective '" + objectiveId + "'.");
            }

            return objective;
        }
    }
}

using System;
using System.Collections.Generic;
using TheRaceForSpace.Core;
using TheRaceForSpace.Objectives;

namespace TheRaceForSpace.Funding
{
    /// <summary>
    /// Defines the code-owned funding contract set for the current prototype.
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
        public static IList<ObjectiveFundingContract> CreateObjectiveFundingContracts()
        {
            var contracts = new List<ObjectiveFundingContract>(
                ObjectiveCatalogue.PreOrbitContracts.Count + ObjectiveCatalogue.All.Count);

            for (int objectiveIndex = 0;
                objectiveIndex < ObjectiveCatalogue.PreOrbitContracts.Count;
                objectiveIndex++)
            {
                AddObjectiveFundingContract(contracts, ObjectiveCatalogue.PreOrbitContracts[objectiveIndex]);
            }

            for (int objectiveIndex = 0; objectiveIndex < ObjectiveCatalogue.All.Count; objectiveIndex++)
            {
                AddObjectiveFundingContract(contracts, ObjectiveCatalogue.All[objectiveIndex]);
            }

            return contracts;
        }

        /// <summary>
        /// Creates fresh satellite-network funding state for a new race controller.
        /// </summary>
        public static IList<SatelliteNetworkFundingContract> CreateSatelliteNetworkFundingContracts()
        {
            return new List<SatelliteNetworkFundingContract>
            {
                CreateSatelliteNetworkFundingContract(
                    KerbinNetworkId,
                    "Kerbin Orbital Network",
                    "Kerbin",
                    UnlockRuleDefinition.AnyAgencyObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId)),
                CreateSatelliteNetworkFundingContract(
                    MunNetworkId,
                    "Mun Survey Network",
                    "Mun",
                    CreateKerbinMoonNetworkUnlockRule(ObjectiveCatalogue.MunProbeOrbitId)),
                CreateSatelliteNetworkFundingContract(
                    MinmusNetworkId,
                    "Minmus Relay Initiative",
                    "Minmus",
                    CreateKerbinMoonNetworkUnlockRule(ObjectiveCatalogue.MinmusProbeOrbitId)),
                CreateSatelliteNetworkFundingContract(
                    DunaNetworkId,
                    "Duna Orbital Network",
                    "Duna",
                    CreateInterplanetaryPlanetNetworkUnlockRule(ObjectiveCatalogue.DunaProbeOrbitId)),
                CreateSatelliteNetworkFundingContract(
                    MohoNetworkId,
                    "Moho Orbital Network",
                    "Moho",
                    CreateInterplanetaryPlanetNetworkUnlockRule(ObjectiveCatalogue.MohoProbeOrbitId)),
                CreateSatelliteNetworkFundingContract(
                    EveNetworkId,
                    "Eve Orbital Network",
                    "Eve",
                    CreateInterplanetaryPlanetNetworkUnlockRule(ObjectiveCatalogue.EveProbeOrbitId)),
                CreateSatelliteNetworkFundingContract(
                    GillyNetworkId,
                    "Gilly Relay/Survey Network",
                    "Gilly",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.GillyProbeOrbitId,
                        "Eve")),
                CreateSatelliteNetworkFundingContract(
                    IkeNetworkId,
                    "Ike Relay/Survey Network",
                    "Ike",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.IkeProbeOrbitId,
                        "Duna")),
                CreateSatelliteNetworkFundingContract(
                    DresNetworkId,
                    "Dres Orbital Network",
                    "Dres",
                    CreateInterplanetaryPlanetNetworkUnlockRule(ObjectiveCatalogue.DresProbeOrbitId)),
                CreateSatelliteNetworkFundingContract(
                    JoolNetworkId,
                    "Jool Orbital Network",
                    "Jool",
                    CreateInterplanetaryPlanetNetworkUnlockRule(ObjectiveCatalogue.JoolProbeOrbitId)),
                CreateSatelliteNetworkFundingContract(
                    LaytheNetworkId,
                    "Laythe Orbital Network",
                    "Laythe",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.LaytheProbeOrbitId,
                        "Jool")),
                CreateSatelliteNetworkFundingContract(
                    VallNetworkId,
                    "Vall Relay/Survey Network",
                    "Vall",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.VallProbeOrbitId,
                        "Jool")),
                CreateSatelliteNetworkFundingContract(
                    TyloNetworkId,
                    "Tylo Relay/Survey Network",
                    "Tylo",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.TyloProbeOrbitId,
                        "Jool")),
                CreateSatelliteNetworkFundingContract(
                    BopNetworkId,
                    "Bop Relay/Survey Network",
                    "Bop",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.BopProbeOrbitId,
                        "Jool")),
                CreateSatelliteNetworkFundingContract(
                    PolNetworkId,
                    "Pol Relay/Survey Network",
                    "Pol",
                    CreatePlanetaryMoonNetworkUnlockRule(
                        ObjectiveCatalogue.PolProbeOrbitId,
                        "Jool")),
                CreateSatelliteNetworkFundingContract(
                    EelooNetworkId,
                    "Eeloo Orbital Network",
                    "Eeloo",
                    CreateInterplanetaryPlanetNetworkUnlockRule(ObjectiveCatalogue.EelooProbeOrbitId))
            };
        }

        private static void AddObjectiveFundingContract(
            IList<ObjectiveFundingContract> contracts,
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

            var contract = new ObjectiveFundingContract(
                objective.Id,
                objective.Name,
                objective.ObjectiveDescription,
                baseRewardFunds,
                CreateUnlockRequirementText(objective.UnlockRule),
                objective.UnlockRule);

            // The four pre-orbit lines remain outside the normal two-offer sponsor pool. Their first
            // objectives are bootstrap offers; later levels are offered deterministically by the controller.
            if (objective.IsPreOrbitContract && objective.PreOrbitLevel == 1)
            {
                contract.Offer();
            }

            contracts.Add(contract);
        }

        private static SatelliteNetworkFundingContract CreateSatelliteNetworkFundingContract(
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
                    if (!IsSingleAgencyObjectiveCompletionCondition(condition))
                    {
                        throw new InvalidOperationException(
                            "Funding OR text supports AnyAgency objectives only.");
                    }

                    if (!string.IsNullOrEmpty(alternatives))
                    {
                        alternatives += " or ";
                    }

                    alternatives += FindRequiredObjective(condition.ObjectiveId).Name;
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
            if (!IsSingleAgencyObjectiveCompletionCondition(firstCondition))
            {
                throw new InvalidOperationException(
                    "Funding unlock text requires an AnyAgency objectiveCompletion as its first condition.");
            }

            string unlockRequirement =
                "Any agency must achieve " + FindRequiredObjective(firstCondition.ObjectiveId).Name;

            for (int conditionIndex = 1; conditionIndex < path.Conditions.Count; conditionIndex++)
            {
                UnlockConditionDefinition condition = path.Conditions[conditionIndex];
                if (IsSingleAgencyObjectiveCompletionCondition(condition))
                {
                    unlockRequirement += " and " + FindRequiredObjective(condition.ObjectiveId).Name;
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
                    "Funding unlock text supports AnyAgency objectives and satellite-count conditions.");
            }

            return unlockRequirement + ".";
        }

        private static bool IsSingleAgencyObjectiveCompletionCondition(UnlockConditionDefinition condition)
        {
            return condition != null
                && condition.ConditionType == UnlockConditionType.ObjectiveCompletion
                && condition.AgencyScope == UnlockAgencyScope.AnyAgency
                && condition.RequiredAgencyCount == 1
                && !string.IsNullOrEmpty(condition.ObjectiveId);
        }

        private static ObjectiveDefinition FindRequiredObjective(string objectiveId)
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

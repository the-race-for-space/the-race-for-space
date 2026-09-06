using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Rivals;

namespace TheRaceForSpace.Tests.Objectives
{
    internal static class ObjectiveEvaluationTests
    {
        public static void ProbeObservationMatchesDefinition()
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(ObjectiveCatalogue.ProbeOrbitId);
            var observation = new OrbitalObjectiveObservation(
                "kerbin",
                ObjectiveSituation.Orbit,
                ObjectiveCrewRequirement.UncrewedProbe);

            Require(objective.IsSatisfiedBy(observation), "Kerbin Probe Orbit should accept an uncrewed probe observation.");
        }

        public static void CrewedObservationMatchesDefinition()
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(ObjectiveCatalogue.CrewedOrbitId);
            var observation = new OrbitalObjectiveObservation(
                "Kerbin",
                ObjectiveSituation.Orbit,
                ObjectiveCrewRequirement.Crewed);

            Require(objective.IsSatisfiedBy(observation), "Kerbin Crewed Orbit should accept a crewed observation.");
            Require(
                !ObjectiveCatalogue.FindById(ObjectiveCatalogue.ProbeOrbitId).IsSatisfiedBy(observation),
                "A crewed observation should not also satisfy the uncrewed probe objective.");
        }

        public static void WrongBodyOrSituationDoesNotMatch()
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(ObjectiveCatalogue.MunProbeOrbitId);
            var wrongBody = new OrbitalObjectiveObservation(
                "Minmus",
                ObjectiveSituation.Orbit,
                ObjectiveCrewRequirement.UncrewedProbe);
            var wrongSituation = new OrbitalObjectiveObservation(
                "Mun",
                (ObjectiveSituation)99,
                ObjectiveCrewRequirement.UncrewedProbe);

            Require(!objective.IsSatisfiedBy(wrongBody), "A objective should reject a vessel around the wrong body.");
            Require(!objective.IsSatisfiedBy(wrongSituation), "A objective should reject the wrong vessel situation.");
        }

        public static void ArbitraryBodyDefinitionUsesSameRule()
        {
            var objective = new ObjectiveDefinition(
                "future-eve-probe-orbit",
                "Eve Probe Orbit",
                "Eve",
                ObjectiveSituation.Orbit,
                ObjectiveCrewRequirement.UncrewedProbe,
                "Orbit Eve with an uncrewed probe.",
                UnlockRuleDefinition.AnyAgencyObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId));
            var observation = new OrbitalObjectiveObservation(
                "Eve",
                ObjectiveSituation.Orbit,
                ObjectiveCrewRequirement.UncrewedProbe);
            var unqualifiedObservation = new OrbitalObjectiveObservation(
                "Eve",
                ObjectiveSituation.Orbit,
                null);

            Require(objective.IsSatisfiedBy(observation), "A future body should use the same objective matching rule.");
            Require(!objective.IsSatisfiedBy(unqualifiedObservation), "An unqualified uncrewed vessel should not satisfy a probe objective.");

            PreOrbitBalanceUsesCampaignSettings();
        }

        private static void PreOrbitBalanceUsesCampaignSettings()
        {
            CampaignSettings.ResetToDefaults();

            try
            {
                const double configuredRewardFunds = 34567.0;
                const double configuredRivalProgressCostFunds = 4567.0;
                CampaignSettings.SetPreOrbitRewardFunds(3, configuredRewardFunds);
                CampaignSettings.SetPreOrbitRivalProgressCostFunds(
                    3,
                    configuredRivalProgressCostFunds);

                ObjectiveDefinition control3 = ObjectiveCatalogue.FindById(ObjectiveCatalogue.Control3Id);
                Require(
                    control3.BaseRewardFunds == configuredRewardFunds,
                    "Pre-Orbit objective rewards should use CampaignSettings values.");
                Require(
                    control3.RivalProgressCostFunds == configuredRivalProgressCostFunds,
                    "Pre-Orbit rival progress costs should use CampaignSettings values.");

                IList<ObjectiveFundingContract> fundingContracts =
                    FundingContractCatalogue.CreateObjectiveFundingContracts();
                ObjectiveFundingContract control3Funding = null;
                for (int contractIndex = 0; contractIndex < fundingContracts.Count; contractIndex++)
                {
                    if (string.Equals(
                        fundingContracts[contractIndex].Id,
                        ObjectiveCatalogue.Control3Id,
                        StringComparison.Ordinal))
                    {
                        control3Funding = fundingContracts[contractIndex];
                        break;
                    }
                }

                Require(
                    control3Funding != null
                    && control3Funding.BaseRewardFunds == configuredRewardFunds,
                    "Pre-Orbit funding contracts should use the configured reward for their level.");

                var rival = new AgencyState("Rival", false)
                {
                    NextMissionTargetId = ObjectiveCatalogue.Control3Id
                };
                double rivalProgressCostFunds = RivalSimulation.CalculateMissionProgressCost(
                    rival,
                    fundingContracts,
                    new List<SatelliteNetworkFundingContract>());
                Require(
                    rivalProgressCostFunds == configuredRivalProgressCostFunds,
                    "Rival Pre-Orbit progress should use the configured cost for its level.");
            }
            finally
            {
                CampaignSettings.ResetToDefaults();
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

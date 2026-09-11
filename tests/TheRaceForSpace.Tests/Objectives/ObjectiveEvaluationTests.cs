using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Rivals;
using TheRaceForSpace.Tests.Agencies;

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
            ContractMissionMetadataMatchesApprovedDatabase();
            RivalProgramStateTests.RunAll();
        }

        private static void ContractMissionMetadataMatchesApprovedDatabase()
        {
            AssertObjectiveMissionProfile(ObjectiveCatalogue.DirectedPower1Id, 1, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.DirectedPower2Id, 1, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.DirectedPower3Id, 2, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.DirectedPower4Id, 2, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.DirectedPower5Id, 3, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Mass1Id, 1, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Mass2Id, 1, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Mass3Id, 2, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Mass4Id, 2, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Mass5Id, 3, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Control1Id, 1, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Control2Id, 1, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Control3Id, 2, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Control4Id, 2, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Control5Id, 3, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Biome1Id, 1, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Biome2Id, 1, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Biome3Id, 2, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Biome4Id, 2, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.Biome5Id, 3, 0);

            AssertObjectiveMissionProfile(ObjectiveCatalogue.ProbeOrbitId, 4, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.CrewedOrbitId, 5, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.MunProbeOrbitId, 5, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.MunCrewedOrbitId, 6, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.MinmusProbeOrbitId, 5, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.MinmusCrewedOrbitId, 6, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.DunaProbeOrbitId, 7, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.DunaCrewedOrbitId, 8, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.MohoProbeOrbitId, 7, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.MohoCrewedOrbitId, 8, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.EveProbeOrbitId, 8, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.EveCrewedOrbitId, 9, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.GillyProbeOrbitId, 7, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.GillyCrewedOrbitId, 8, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.IkeProbeOrbitId, 7, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.IkeCrewedOrbitId, 8, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.DresProbeOrbitId, 7, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.DresCrewedOrbitId, 8, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.JoolProbeOrbitId, 7, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.JoolCrewedOrbitId, 8, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.LaytheProbeOrbitId, 7, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.LaytheCrewedOrbitId, 8, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.VallProbeOrbitId, 7, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.VallCrewedOrbitId, 8, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.TyloProbeOrbitId, 7, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.TyloCrewedOrbitId, 8, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.BopProbeOrbitId, 7, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.BopCrewedOrbitId, 8, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.PolProbeOrbitId, 7, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.PolCrewedOrbitId, 8, 1);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.EelooProbeOrbitId, 8, 0);
            AssertObjectiveMissionProfile(ObjectiveCatalogue.EelooCrewedOrbitId, 9, 1);

            IList<SatelliteNetworkFundingContract> networks =
                FundingContractCatalogue.CreateSatelliteNetworkFundingContracts();
            Require(networks.Count == 16, "Expected all 16 current satellite-network Contract definitions.");
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.KerbinNetworkId, 4);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.MunNetworkId, 5);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.MinmusNetworkId, 5);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.DunaNetworkId, 7);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.MohoNetworkId, 7);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.EveNetworkId, 8);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.GillyNetworkId, 7);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.IkeNetworkId, 7);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.DresNetworkId, 7);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.JoolNetworkId, 7);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.LaytheNetworkId, 7);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.VallNetworkId, 7);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.TyloNetworkId, 7);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.BopNetworkId, 7);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.PolNetworkId, 7);
            AssertNetworkMissionProfile(networks, FundingContractCatalogue.EelooNetworkId, 8);
        }

        private static void AssertObjectiveMissionProfile(
            string objectiveId,
            int expectedDifficulty,
            int expectedRequiredKerbalCount)
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(objectiveId);
            Require(objective != null, "Missing objective mission profile for '" + objectiveId + "'.");
            Require(
                objective.Difficulty == expectedDifficulty,
                objectiveId + " should use Difficulty " + expectedDifficulty + ".");
            Require(
                objective.RequiredKerbalCount == expectedRequiredKerbalCount,
                objectiveId + " should require " + expectedRequiredKerbalCount + " rival Kerbals.");
        }

        private static void AssertNetworkMissionProfile(
            IList<SatelliteNetworkFundingContract> networks,
            string contractId,
            int expectedDifficulty)
        {
            SatelliteNetworkFundingContract network = null;
            for (int contractIndex = 0; contractIndex < networks.Count; contractIndex++)
            {
                if (string.Equals(networks[contractIndex].Id, contractId, StringComparison.Ordinal))
                {
                    network = networks[contractIndex];
                    break;
                }
            }

            Require(network != null, "Missing satellite-network mission profile for '" + contractId + "'.");
            Require(
                network.Difficulty == expectedDifficulty,
                contractId + " should use Difficulty " + expectedDifficulty + ".");
            Require(
                network.RequiredKerbalCount == 0,
                contractId + " satellite-network launches should require 0 rival Kerbals.");
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

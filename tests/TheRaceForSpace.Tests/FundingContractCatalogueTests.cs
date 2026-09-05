using System;
using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Objectives;

namespace TheRaceForSpace.Tests
{
    internal static class FundingContractCatalogueTests
    {
        public static void CatalogueMatchesCurrentPrototype()
        {
            IList<ObjectiveFundingContract> objectives =
                FundingContractCatalogue.CreateObjectiveFundingContracts();
            IList<SatelliteNetworkFundingContract> satelliteContracts =
                FundingContractCatalogue.CreateSatelliteNetworkFundingContracts();

            Equal(
                ObjectiveCatalogue.All.Count + ObjectiveCatalogue.PreOrbitContracts.Count,
                objectives.Count);
            Equal(20, ObjectiveCatalogue.PreOrbitContracts.Count);
            Equal(16, satelliteContracts.Count);

            AssertPreOrbitLine(
                objectives,
                PreOrbitContractLine.DirectedPower,
                new[]
                {
                    ObjectiveCatalogue.DirectedPower1Id,
                    ObjectiveCatalogue.DirectedPower2Id,
                    ObjectiveCatalogue.DirectedPower3Id,
                    ObjectiveCatalogue.DirectedPower4Id,
                    ObjectiveCatalogue.DirectedPower5Id
                });
            AssertPreOrbitLine(
                objectives,
                PreOrbitContractLine.Mass,
                new[]
                {
                    ObjectiveCatalogue.Mass1Id,
                    ObjectiveCatalogue.Mass2Id,
                    ObjectiveCatalogue.Mass3Id,
                    ObjectiveCatalogue.Mass4Id,
                    ObjectiveCatalogue.Mass5Id
                });
            AssertPreOrbitLine(
                objectives,
                PreOrbitContractLine.Control,
                new[]
                {
                    ObjectiveCatalogue.Control1Id,
                    ObjectiveCatalogue.Control2Id,
                    ObjectiveCatalogue.Control3Id,
                    ObjectiveCatalogue.Control4Id,
                    ObjectiveCatalogue.Control5Id
                });
            AssertPreOrbitLine(
                objectives,
                PreOrbitContractLine.Biome,
                new[]
                {
                    ObjectiveCatalogue.Biome1Id,
                    ObjectiveCatalogue.Biome2Id,
                    ObjectiveCatalogue.Biome3Id,
                    ObjectiveCatalogue.Biome4Id,
                    ObjectiveCatalogue.Biome5Id
                });
            AssertPreOrbitCriteriaValues();

            ObjectiveFundingContract probeOrbit = FindObjectiveFundingContract(
                objectives,
                ObjectiveCatalogue.ProbeOrbitId);
            Require(probeOrbit != null, "Missing Probe Orbit objectiveCompletion funding contract.");
            Equal(75000.0, probeOrbit.BaseRewardFunds);
            AssertProbeOrbitUnlockRule(probeOrbit.UnlockRule);
            Equal(
                "Any agency must achieve Directed Power V or Mass V or Control V or Biome V - Ice Caps.",
                probeOrbit.UnlockRequirement);

            AssertObjectiveFundingContract(
                objectives,
                ObjectiveCatalogue.CrewedOrbitId,
                150000.0,
                ObjectiveCatalogue.ProbeOrbitId);
            AssertObjectiveFundingContract(
                objectives,
                ObjectiveCatalogue.MunProbeOrbitId,
                150000.0,
                ObjectiveCatalogue.ProbeOrbitId);
            AssertObjectiveFundingContract(
                objectives,
                ObjectiveCatalogue.MinmusProbeOrbitId,
                150000.0,
                ObjectiveCatalogue.ProbeOrbitId);
            AssertObjectiveFundingContract(
                objectives,
                ObjectiveCatalogue.DunaProbeOrbitId,
                300000.0,
                ObjectiveCatalogue.MunProbeOrbitId,
                ObjectiveCatalogue.MinmusProbeOrbitId);
            AssertObjectiveFundingContract(
                objectives,
                ObjectiveCatalogue.MunCrewedOrbitId,
                300000.0,
                ObjectiveCatalogue.CrewedOrbitId);
            AssertObjectiveFundingContract(
                objectives,
                ObjectiveCatalogue.MinmusCrewedOrbitId,
                300000.0,
                ObjectiveCatalogue.CrewedOrbitId);
            AssertObjectiveFundingContract(
                objectives,
                ObjectiveCatalogue.DunaCrewedOrbitId,
                500000.0,
                ObjectiveCatalogue.MunCrewedOrbitId,
                ObjectiveCatalogue.MinmusCrewedOrbitId);

            string[] interplanetaryProbePrerequisites =
                { ObjectiveCatalogue.MunProbeOrbitId, ObjectiveCatalogue.MinmusProbeOrbitId };
            string[] interplanetaryCrewedPrerequisites =
                { ObjectiveCatalogue.MunCrewedOrbitId, ObjectiveCatalogue.MinmusCrewedOrbitId };
            string[] interplanetaryNetworkPrerequisiteBodies = { "Mun", "Minmus" };
            int[] interplanetaryNetworkPrerequisiteCounts = { 3, 3 };

            AssertBodyFundingSet(
                objectives,
                satelliteContracts,
                ObjectiveCatalogue.MohoProbeOrbitId,
                ObjectiveCatalogue.MohoCrewedOrbitId,
                FundingContractCatalogue.MohoNetworkId,
                "Moho",
                10,
                200000.0,
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites,
                interplanetaryNetworkPrerequisiteBodies,
                interplanetaryNetworkPrerequisiteCounts);
            AssertBodyFundingSet(
                objectives,
                satelliteContracts,
                ObjectiveCatalogue.EveProbeOrbitId,
                ObjectiveCatalogue.EveCrewedOrbitId,
                FundingContractCatalogue.EveNetworkId,
                "Eve",
                10,
                200000.0,
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites,
                interplanetaryNetworkPrerequisiteBodies,
                interplanetaryNetworkPrerequisiteCounts);
            AssertBodyFundingSet(
                objectives,
                satelliteContracts,
                ObjectiveCatalogue.GillyProbeOrbitId,
                ObjectiveCatalogue.GillyCrewedOrbitId,
                FundingContractCatalogue.GillyNetworkId,
                "Gilly",
                5,
                100000.0,
                new[] { ObjectiveCatalogue.EveProbeOrbitId },
                new[] { ObjectiveCatalogue.EveCrewedOrbitId },
                new[] { "Eve" },
                new[] { 6 });
            AssertBodyFundingSet(
                objectives,
                satelliteContracts,
                ObjectiveCatalogue.IkeProbeOrbitId,
                ObjectiveCatalogue.IkeCrewedOrbitId,
                FundingContractCatalogue.IkeNetworkId,
                "Ike",
                5,
                100000.0,
                new[] { ObjectiveCatalogue.DunaProbeOrbitId },
                new[] { ObjectiveCatalogue.DunaCrewedOrbitId },
                new[] { "Duna" },
                new[] { 6 });
            AssertBodyFundingSet(
                objectives,
                satelliteContracts,
                ObjectiveCatalogue.DresProbeOrbitId,
                ObjectiveCatalogue.DresCrewedOrbitId,
                FundingContractCatalogue.DresNetworkId,
                "Dres",
                10,
                200000.0,
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites,
                interplanetaryNetworkPrerequisiteBodies,
                interplanetaryNetworkPrerequisiteCounts);
            AssertBodyFundingSet(
                objectives,
                satelliteContracts,
                ObjectiveCatalogue.JoolProbeOrbitId,
                ObjectiveCatalogue.JoolCrewedOrbitId,
                FundingContractCatalogue.JoolNetworkId,
                "Jool",
                10,
                200000.0,
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites,
                interplanetaryNetworkPrerequisiteBodies,
                interplanetaryNetworkPrerequisiteCounts);
            AssertBodyFundingSet(
                objectives,
                satelliteContracts,
                ObjectiveCatalogue.LaytheProbeOrbitId,
                ObjectiveCatalogue.LaytheCrewedOrbitId,
                FundingContractCatalogue.LaytheNetworkId,
                "Laythe",
                5,
                100000.0,
                new[] { ObjectiveCatalogue.JoolProbeOrbitId },
                new[] { ObjectiveCatalogue.JoolCrewedOrbitId },
                new[] { "Jool" },
                new[] { 6 });
            AssertBodyFundingSet(
                objectives,
                satelliteContracts,
                ObjectiveCatalogue.VallProbeOrbitId,
                ObjectiveCatalogue.VallCrewedOrbitId,
                FundingContractCatalogue.VallNetworkId,
                "Vall",
                5,
                100000.0,
                new[] { ObjectiveCatalogue.JoolProbeOrbitId },
                new[] { ObjectiveCatalogue.JoolCrewedOrbitId },
                new[] { "Jool" },
                new[] { 6 });
            AssertBodyFundingSet(
                objectives,
                satelliteContracts,
                ObjectiveCatalogue.TyloProbeOrbitId,
                ObjectiveCatalogue.TyloCrewedOrbitId,
                FundingContractCatalogue.TyloNetworkId,
                "Tylo",
                5,
                100000.0,
                new[] { ObjectiveCatalogue.JoolProbeOrbitId },
                new[] { ObjectiveCatalogue.JoolCrewedOrbitId },
                new[] { "Jool" },
                new[] { 6 });
            AssertBodyFundingSet(
                objectives,
                satelliteContracts,
                ObjectiveCatalogue.BopProbeOrbitId,
                ObjectiveCatalogue.BopCrewedOrbitId,
                FundingContractCatalogue.BopNetworkId,
                "Bop",
                5,
                100000.0,
                new[] { ObjectiveCatalogue.JoolProbeOrbitId },
                new[] { ObjectiveCatalogue.JoolCrewedOrbitId },
                new[] { "Jool" },
                new[] { 6 });
            AssertBodyFundingSet(
                objectives,
                satelliteContracts,
                ObjectiveCatalogue.PolProbeOrbitId,
                ObjectiveCatalogue.PolCrewedOrbitId,
                FundingContractCatalogue.PolNetworkId,
                "Pol",
                5,
                100000.0,
                new[] { ObjectiveCatalogue.JoolProbeOrbitId },
                new[] { ObjectiveCatalogue.JoolCrewedOrbitId },
                new[] { "Jool" },
                new[] { 6 });
            AssertBodyFundingSet(
                objectives,
                satelliteContracts,
                ObjectiveCatalogue.EelooProbeOrbitId,
                ObjectiveCatalogue.EelooCrewedOrbitId,
                FundingContractCatalogue.EelooNetworkId,
                "Eeloo",
                10,
                200000.0,
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites,
                interplanetaryNetworkPrerequisiteBodies,
                interplanetaryNetworkPrerequisiteCounts);

            Equal(
                "Any agency must achieve Probe Orbit.",
                FindObjectiveFundingContract(objectives, ObjectiveCatalogue.CrewedOrbitId).UnlockRequirement);
            Equal(
                "Any agency must achieve Mun Probe Orbit and Minmus Probe Orbit.",
                FindObjectiveFundingContract(objectives, ObjectiveCatalogue.DunaProbeOrbitId).UnlockRequirement);
            Equal(
                "Any agency must achieve Mun Crewed Orbit and Minmus Crewed Orbit.",
                FindObjectiveFundingContract(objectives, ObjectiveCatalogue.DunaCrewedOrbitId).UnlockRequirement);

            AssertSatellite(
                satelliteContracts,
                FundingContractCatalogue.KerbinNetworkId,
                "Kerbin",
                10,
                200000.0,
                ObjectiveCatalogue.ProbeOrbitId,
                new string[0],
                new int[0]);
            AssertSatellite(
                satelliteContracts,
                FundingContractCatalogue.MunNetworkId,
                "Mun",
                5,
                100000.0,
                ObjectiveCatalogue.MunProbeOrbitId,
                new[] { "Kerbin" },
                new[] { 6 });
            AssertSatellite(
                satelliteContracts,
                FundingContractCatalogue.MinmusNetworkId,
                "Minmus",
                5,
                100000.0,
                ObjectiveCatalogue.MinmusProbeOrbitId,
                new[] { "Kerbin" },
                new[] { 6 });
            AssertSatellite(
                satelliteContracts,
                FundingContractCatalogue.DunaNetworkId,
                "Duna",
                10,
                200000.0,
                ObjectiveCatalogue.DunaProbeOrbitId,
                interplanetaryNetworkPrerequisiteBodies,
                interplanetaryNetworkPrerequisiteCounts);

            Equal(
                "Any agency must achieve Mun Probe Orbit and the Kerbin satellite network must reach 6 qualifying satellites.",
                FindSatellite(satelliteContracts, FundingContractCatalogue.MunNetworkId).UnlockRequirement);
            Equal(
                "Any agency must achieve Minmus Probe Orbit and the Kerbin satellite network must reach 6 qualifying satellites.",
                FindSatellite(satelliteContracts, FundingContractCatalogue.MinmusNetworkId).UnlockRequirement);
            Equal(
                "Any agency must achieve Duna Probe Orbit and the Mun satellite network must reach 3 qualifying satellites and the Minmus satellite network must reach 3 qualifying satellites.",
                FindSatellite(satelliteContracts, FundingContractCatalogue.DunaNetworkId).UnlockRequirement);
            Equal(
                "Any agency must achieve Ike Probe Orbit and the Duna satellite network must reach 6 qualifying satellites.",
                FindSatellite(satelliteContracts, FundingContractCatalogue.IkeNetworkId).UnlockRequirement);
        }

        public static void CatalogueCreatesFreshCampaignState()
        {
            IList<ObjectiveFundingContract> firstObjectives =
                FundingContractCatalogue.CreateObjectiveFundingContracts();
            IList<SatelliteNetworkFundingContract> firstSatellites =
                FundingContractCatalogue.CreateSatelliteNetworkFundingContracts();

            ObjectiveFundingContract directedPower1 = FindObjectiveFundingContract(
                firstObjectives,
                ObjectiveCatalogue.DirectedPower1Id);
            directedPower1.Start();
            directedPower1.AdvancePayout();
            firstSatellites[0].Unlock();

            IList<ObjectiveFundingContract> secondObjectives =
                FundingContractCatalogue.CreateObjectiveFundingContracts();
            IList<SatelliteNetworkFundingContract> secondSatellites =
                FundingContractCatalogue.CreateSatelliteNetworkFundingContracts();

            ObjectiveFundingContract freshDirectedPower1 = FindObjectiveFundingContract(
                secondObjectives,
                ObjectiveCatalogue.DirectedPower1Id);
            Require(!freshDirectedPower1.HasStarted, "A new catalogue build must not reuse objectiveCompletion campaign state.");
            Equal(0, freshDirectedPower1.PaymentsProcessed);

            Require(FindObjectiveFundingContract(secondObjectives, ObjectiveCatalogue.DirectedPower1Id).IsOffered,
                "Directed Power I should be an opening starter offer.");
            Require(FindObjectiveFundingContract(secondObjectives, ObjectiveCatalogue.Mass1Id).IsOffered,
                "Mass I should be an opening starter offer.");
            Require(FindObjectiveFundingContract(secondObjectives, ObjectiveCatalogue.Control1Id).IsOffered,
                "Control I should be an opening starter offer.");
            Require(FindObjectiveFundingContract(secondObjectives, ObjectiveCatalogue.Biome1Id).IsOffered,
                "Biome I should be an opening starter offer.");
            Require(!FindObjectiveFundingContract(secondObjectives, ObjectiveCatalogue.DirectedPower2Id).IsOffered,
                "Directed Power II should wait for Directed Power I.");
            Require(!FindObjectiveFundingContract(secondObjectives, ObjectiveCatalogue.ProbeOrbitId).IsOffered,
                "Probe Orbit should remain locked until a pre-orbit line reaches level five.");
            Require(!secondSatellites[0].IsAvailable, "A new catalogue build must not reuse satellite unlock state.");
        }

        private static void AssertPreOrbitLine(
            IList<ObjectiveFundingContract> objectives,
            PreOrbitContractLine preOrbitLine,
            string[] objectiveIds)
        {
            Equal(5, objectiveIds.Length);

            for (int objectiveIndex = 0; objectiveIndex < objectiveIds.Length; objectiveIndex++)
            {
                int level = objectiveIndex + 1;
                string objectiveId = objectiveIds[objectiveIndex];
                ObjectiveDefinition objective = ObjectiveCatalogue.FindById(objectiveId);
                ObjectiveFundingContract contract = FindObjectiveFundingContract(objectives, objectiveId);

                Require(objective != null, "Missing pre-orbit objective '" + objectiveId + "'.");
                Require(contract != null, "Missing starter funding contract '" + objectiveId + "'.");
                Require(objective.IsPreOrbitContract, "PreOrbit objective should be marked special.");
                Equal(preOrbitLine, objective.PreOrbitLine);
                Equal(level, objective.PreOrbitLevel);
                Equal(level * 10000.0, objective.BaseRewardFunds);
                Equal((level + 1) * 2000.0, objective.RivalProgressCostFunds);
                Equal(level * 10000.0, contract.BaseRewardFunds);
                Equal(level == 1, contract.IsOffered);

                if (level == 1)
                {
                    Equal(null, objective.UnlockRule);
                }
                else
                {
                    AssertAnyAgencyRule(objective.UnlockRule, objectiveIds[objectiveIndex - 1]);
                }
            }
        }

        private static void AssertPreOrbitCriteriaValues()
        {
            AssertPreOrbitCriteria(ObjectiveCatalogue.DirectedPower1Id, 600.0, 0.0, 0.0, 0.0, 70000.0, 0.0, null);
            AssertPreOrbitCriteria(ObjectiveCatalogue.DirectedPower2Id, 1100.0, 0.0, 0.0, 0.0, 70000.0, 0.0, null);
            AssertPreOrbitCriteria(ObjectiveCatalogue.DirectedPower3Id, 1400.0, 0.0, 0.0, 0.0, 70000.0, 0.0, null);
            AssertPreOrbitCriteria(ObjectiveCatalogue.DirectedPower4Id, 1700.0, 0.0, 0.0, 0.0, 70000.0, 0.0, null);
            AssertPreOrbitCriteria(ObjectiveCatalogue.DirectedPower5Id, 2000.0, 0.0, 0.0, 0.0, 70000.0, 0.0, null);

            AssertPreOrbitCriteria(ObjectiveCatalogue.Mass1Id, 0.0, 1.0, 25000.0, 0.0, 0.0, 0.0, null);
            AssertPreOrbitCriteria(ObjectiveCatalogue.Mass2Id, 0.0, 2.5, 75000.0, 0.0, 0.0, 0.0, null);
            AssertPreOrbitCriteria(ObjectiveCatalogue.Mass3Id, 0.0, 5.0, 150000.0, 0.0, 0.0, 0.0, null);
            AssertPreOrbitCriteria(ObjectiveCatalogue.Mass4Id, 0.0, 10.0, 300000.0, 0.0, 0.0, 0.0, null);
            AssertPreOrbitCriteria(ObjectiveCatalogue.Mass5Id, 0.0, 20.0, 600000.0, 0.0, 0.0, 0.0, null);

            AssertPreOrbitCriteria(ObjectiveCatalogue.Control1Id, 0.0, 0.0, 0.0, 2000.0, 5000.0, 30.0, null);
            AssertPreOrbitCriteria(ObjectiveCatalogue.Control2Id, 0.0, 0.0, 0.0, 8000.0, 12000.0, 45.0, null);
            AssertPreOrbitCriteria(ObjectiveCatalogue.Control3Id, 0.0, 0.0, 0.0, 15000.0, 25000.0, 60.0, null);
            AssertPreOrbitCriteria(ObjectiveCatalogue.Control4Id, 0.0, 0.0, 0.0, 30000.0, 40000.0, 75.0, null);
            AssertPreOrbitCriteria(ObjectiveCatalogue.Control5Id, 0.0, 0.0, 0.0, 50000.0, 65000.0, 90.0, null);

            AssertPreOrbitCriteria(ObjectiveCatalogue.Biome1Id, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, "Grasslands");
            AssertPreOrbitCriteria(ObjectiveCatalogue.Biome2Id, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, "Highlands");
            AssertPreOrbitCriteria(ObjectiveCatalogue.Biome3Id, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, "Mountains");
            AssertPreOrbitCriteria(ObjectiveCatalogue.Biome4Id, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, "Deserts");
            AssertPreOrbitCriteria(ObjectiveCatalogue.Biome5Id, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, "Ice Caps");
        }

        private static void AssertPreOrbitCriteria(
            string objectiveId,
            double requiredSpeedMetersPerSecond,
            double requiredMassTonnes,
            double requiredDistanceMeters,
            double minimumAltitudeMeters,
            double maximumAltitudeMeters,
            double requiredDurationSeconds,
            string requiredBiomeName)
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(objectiveId);
            Require(objective != null, "Missing pre-orbit objective '" + objectiveId + "'.");
            Equal(requiredSpeedMetersPerSecond, objective.RequiredSpeedMetersPerSecond);
            Equal(requiredMassTonnes, objective.RequiredMassTonnes);
            Equal(requiredDistanceMeters, objective.RequiredDistanceMeters);
            Equal(minimumAltitudeMeters, objective.MinimumAltitudeMeters);
            Equal(maximumAltitudeMeters, objective.MaximumAltitudeMeters);
            Equal(requiredDurationSeconds, objective.RequiredDurationSeconds);
            Equal(requiredBiomeName, objective.RequiredBiomeName);
        }

        private static void AssertProbeOrbitUnlockRule(UnlockRuleDefinition rule)
        {
            Require(rule != null, "Probe Orbit should have a starter-line unlock rule.");
            Equal(4, rule.Paths.Count);

            string[] expectedObjectiveIds =
            {
                ObjectiveCatalogue.DirectedPower5Id,
                ObjectiveCatalogue.Mass5Id,
                ObjectiveCatalogue.Control5Id,
                ObjectiveCatalogue.Biome5Id
            };

            for (int pathIndex = 0; pathIndex < expectedObjectiveIds.Length; pathIndex++)
            {
                Require(rule.Paths[pathIndex] != null, "Probe Orbit unlock path should not be null.");
                Equal(1, rule.Paths[pathIndex].Conditions.Count);
                AssertAnyAgencyObjectiveCompletionCondition(
                    rule.Paths[pathIndex].Conditions[0],
                    expectedObjectiveIds[pathIndex]);
            }
        }

        private static void AssertBodyFundingSet(
            IList<ObjectiveFundingContract> objectives,
            IList<SatelliteNetworkFundingContract> satelliteContracts,
            string probeObjectiveId,
            string crewedObjectiveId,
            string networkId,
            string celestialBodyName,
            int requiredSatellites,
            double networkRewardFunds,
            string[] probePrerequisiteObjectiveIds,
            string[] crewedPrerequisiteObjectiveIds,
            string[] networkPrerequisiteBodyNames,
            int[] networkPrerequisiteSatelliteCounts)
        {
            AssertObjectiveFundingContract(
                objectives,
                probeObjectiveId,
                300000.0,
                probePrerequisiteObjectiveIds);
            AssertObjectiveFundingContract(
                objectives,
                crewedObjectiveId,
                500000.0,
                crewedPrerequisiteObjectiveIds);
            AssertSatellite(
                satelliteContracts,
                networkId,
                celestialBodyName,
                requiredSatellites,
                networkRewardFunds,
                probeObjectiveId,
                networkPrerequisiteBodyNames,
                networkPrerequisiteSatelliteCounts);
        }

        private static void AssertObjectiveFundingContract(
            IList<ObjectiveFundingContract> contracts,
            string id,
            double rewardFunds,
            params string[] prerequisiteObjectiveIds)
        {
            ObjectiveFundingContract contract = FindObjectiveFundingContract(contracts, id);
            Require(contract != null, "Missing objectiveCompletion funding contract '" + id + "'.");
            Equal(rewardFunds, contract.BaseRewardFunds);
            AssertAnyAgencyRule(contract.UnlockRule, prerequisiteObjectiveIds);
        }

        private static void AssertSatellite(
            IList<SatelliteNetworkFundingContract> contracts,
            string id,
            string celestialBodyName,
            int requiredSatellites,
            double rewardFunds,
            string prerequisiteObjectiveId,
            string[] prerequisiteSatelliteBodyNames,
            int[] prerequisiteSatelliteCounts)
        {
            SatelliteNetworkFundingContract contract = FindSatellite(contracts, id);
            Require(contract != null, "Missing satellite funding contract '" + id + "'.");
            Equal(celestialBodyName, contract.CelestialBodyName);
            Equal(requiredSatellites, contract.RequiredSatellites);
            Equal(rewardFunds, contract.RewardFunds);
            AssertSatelliteRule(
                contract.UnlockRule,
                prerequisiteObjectiveId,
                prerequisiteSatelliteBodyNames,
                prerequisiteSatelliteCounts);
            Require(!contract.IsAvailable, "Prototype satellite contracts should begin locked.");
        }

        private static void AssertSatelliteRule(
            UnlockRuleDefinition rule,
            string expectedObjectiveId,
            string[] expectedSatelliteBodyNames,
            int[] expectedSatelliteCounts)
        {
            Require(rule != null, "Satellite funding targets should carry an unlock rule.");
            Require(
                expectedSatelliteBodyNames != null
                && expectedSatelliteCounts != null
                && expectedSatelliteBodyNames.Length == expectedSatelliteCounts.Length,
                "Satellite prerequisite expectations must use matching body/count arrays.");
            Equal(1, rule.Paths.Count);
            Require(rule.Paths[0] != null, "Satellite unlock path should not be null.");
            Equal(1 + expectedSatelliteBodyNames.Length, rule.Paths[0].Conditions.Count);

            AssertAnyAgencyObjectiveCompletionCondition(
                rule.Paths[0].Conditions[0],
                expectedObjectiveId);

            for (int prerequisiteIndex = 0;
                prerequisiteIndex < expectedSatelliteBodyNames.Length;
                prerequisiteIndex++)
            {
                UnlockConditionDefinition condition =
                    rule.Paths[0].Conditions[prerequisiteIndex + 1];
                Require(condition != null, "Satellite-count unlock condition should not be null.");
                Equal(UnlockConditionType.SatelliteCount, condition.ConditionType);
                Equal(expectedSatelliteBodyNames[prerequisiteIndex], condition.CelestialBodyName);
                Equal(expectedSatelliteCounts[prerequisiteIndex], condition.RequiredSatelliteCount);
            }
        }

        private static void AssertAnyAgencyRule(
            UnlockRuleDefinition rule,
            params string[] expectedObjectiveIds)
        {
            if (expectedObjectiveIds == null || expectedObjectiveIds.Length == 0)
            {
                Equal(null, rule);
                return;
            }

            Require(rule != null, "Locked prototype targets should carry an unlock rule.");
            Equal(1, rule.Paths.Count);
            Require(rule.Paths[0] != null, "Prototype unlock path should not be null.");
            Equal(expectedObjectiveIds.Length, rule.Paths[0].Conditions.Count);

            for (int conditionIndex = 0;
                conditionIndex < expectedObjectiveIds.Length;
                conditionIndex++)
            {
                AssertAnyAgencyObjectiveCompletionCondition(
                    rule.Paths[0].Conditions[conditionIndex],
                    expectedObjectiveIds[conditionIndex]);
            }
        }

        private static void AssertAnyAgencyObjectiveCompletionCondition(
            UnlockConditionDefinition condition,
            string expectedObjectiveId)
        {
            Require(condition != null, "Prototype unlock condition should not be null.");
            Equal(UnlockConditionType.ObjectiveCompletion, condition.ConditionType);
            Equal(UnlockAgencyScope.AnyAgency, condition.AgencyScope);
            Equal(1, condition.RequiredAgencyCount);
            Equal(expectedObjectiveId, condition.ObjectiveId);
        }

        private static ObjectiveFundingContract FindObjectiveFundingContract(
            IList<ObjectiveFundingContract> contracts,
            string id)
        {
            for (int contractIndex = 0; contractIndex < contracts.Count; contractIndex++)
            {
                ObjectiveFundingContract contract = contracts[contractIndex];
                if (string.Equals(contract.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return contract;
                }
            }

            return null;
        }

        private static SatelliteNetworkFundingContract FindSatellite(IList<SatelliteNetworkFundingContract> contracts, string id)
        {
            for (int contractIndex = 0; contractIndex < contracts.Count; contractIndex++)
            {
                SatelliteNetworkFundingContract contract = contracts[contractIndex];
                if (string.Equals(contract.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return contract;
                }
            }

            return null;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    "Expected '" + expected + "' but got '" + actual + "'.");
            }
        }
    }
}

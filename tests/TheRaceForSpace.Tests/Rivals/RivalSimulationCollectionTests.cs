using System.Collections.Generic;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Rivals;

namespace TheRaceForSpace.Tests.Rivals
{
    internal static class RivalSimulationCollectionTests
    {
        public static void SelectsOnlyAvailableAchievementFromCollection()
        {
            var player = new AgencyState("player", "Player", true);
            var aster = new AgencyState("aster", "Aster", false)
            {
                NextMissionProgressCheckUniversalTime = double.NaN
            };
            var cobalt = new AgencyState("cobalt", "Cobalt", false);
            var delta = new AgencyState("delta", "Delta", false);
            delta.RecordObjectiveCompletion(ObjectiveCatalogue.CrewedOrbitId, 1.0);

            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(
                ObjectiveCatalogue.MinmusCrewedOrbitId);
            var achievementProgrammes = new List<ObjectiveFundingContract>
            {
                new ObjectiveFundingContract(
                    objective.Id,
                    objective.Name,
                    objective.ObjectiveDescription,
                    300000.0,
                    "Display text",
                    objective.UnlockRule)
            };
            achievementProgrammes[0].Offer();

            RivalSimulation.Refresh(
                new List<AgencyState> { player, aster, cobalt, delta },
                1.0,
                achievementProgrammes,
                new List<SatelliteNetworkFundingContract>());

            TestAssert.Equal(ObjectiveCatalogue.MinmusCrewedOrbitId, aster.NextMissionTargetId);
            TestAssert.Equal("Minmus Crewed Orbit", aster.NextMissionDisplayName);
            TestAssert.Equal(ObjectiveCatalogue.MinmusCrewedOrbitId, cobalt.NextMissionTargetId);
            TestAssert.Equal(5.0 * 21600.0, aster.NextMissionProgressCheckUniversalTime);
        }

        public static void LockedAchievementIsExcludedFromCollection()
        {
            var player = new AgencyState("Player", true);
            var aster = new AgencyState("Aster", false);
            var cobalt = new AgencyState("Cobalt", false);

            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(
                ObjectiveCatalogue.MunProbeOrbitId);
            var achievementProgrammes = new List<ObjectiveFundingContract>
            {
                new ObjectiveFundingContract(
                    objective.Id,
                    objective.Name,
                    objective.ObjectiveDescription,
                    200000.0,
                    "Display text",
                    objective.UnlockRule)
            };

            RivalSimulation.Refresh(
                new List<AgencyState> { player, aster, cobalt },
                0.0,
                achievementProgrammes,
                new List<SatelliteNetworkFundingContract>());

            TestAssert.Equal(null, aster.NextMissionTargetId);

            var malformedRival = new AgencyState("malformed", "Malformed", false)
            {
                NextMissionTargetId = "missing-objective",
                MissionProgressPercent = 100
            };
            var malformedProgramme = new ObjectiveFundingContract(
                "missing-objective",
                "Missing Objective",
                "This target has no objective definition.",
                1000.0);
            malformedProgramme.Offer();

            RivalSimulation.Refresh(
                new List<AgencyState> { player, malformedRival },
                1.0,
                new List<ObjectiveFundingContract> { malformedProgramme },
                new List<SatelliteNetworkFundingContract>());

            TestAssert.Equal(null, malformedRival.NextMissionTargetId);
            TestAssert.Equal(0, malformedRival.MissionProgressPercent);
        }

        public static void FlexibleRuleUsesRivalScopeCountAndHistoricalTime()
        {
            var player = new AgencyState("player", "Player", true);
            var aster = new AgencyState("aster", "Aster", false);
            var cobalt = new AgencyState("cobalt", "Cobalt", false);
            var delta = new AgencyState("delta", "Delta", false);
            aster.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 100.0);
            delta.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 200.0);

            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(
                ObjectiveCatalogue.MinmusCrewedOrbitId);
            var achievementProgrammes = new List<ObjectiveFundingContract>
            {
                new ObjectiveFundingContract(
                    objective.Id,
                    objective.Name,
                    objective.ObjectiveDescription,
                    300000.0,
                    "Display text",
                    new UnlockRuleDefinition(
                        new UnlockPathDefinition(
                            UnlockConditionDefinition.ObjectiveCompletion(
                                ObjectiveCatalogue.ProbeOrbitId,
                                UnlockAgencyScope.AnyRival,
                                2))))
            };
            var agencies = new List<AgencyState> { player, aster, cobalt, delta };

            RivalSimulation.Refresh(
                agencies,
                199.0,
                achievementProgrammes,
                new List<SatelliteNetworkFundingContract>());
            TestAssert.Equal(
                null,
                cobalt.NextMissionTargetId);

            // Sponsor selection is now a controller responsibility. Once a contract is Offered,
            // rival simulation consumes that stable state rather than reevaluating its unlock rule.
            achievementProgrammes[0].Offer();
            RivalSimulation.Refresh(
                agencies,
                200.0,
                achievementProgrammes,
                new List<SatelliteNetworkFundingContract>());
            TestAssert.Equal(
                ObjectiveCatalogue.MinmusCrewedOrbitId,
                cobalt.NextMissionTargetId);
        }

        public static void SatelliteProgrammeRemainsRepeatable()
        {
            var player = new AgencyState("Player", true);
            var aster = new AgencyState("Aster", false);
            var cobalt = new AgencyState("Cobalt", false);
            aster.SetSatelliteCount("Duna", 3);

            var satelliteNetworkFundingContracts = new List<SatelliteNetworkFundingContract>
            {
                new SatelliteNetworkFundingContract(
                    "duna-network",
                    "Duna Orbital Network",
                    "Duna",
                    5,
                    100000.0,
                    true,
                    null,
                    (UnlockRuleDefinition)null)
            };
            satelliteNetworkFundingContracts[0].Offer();

            RivalSimulation.Refresh(
                new List<AgencyState> { player, aster, cobalt },
                0.0,
                new List<ObjectiveFundingContract>(),
                satelliteNetworkFundingContracts);

            TestAssert.Equal("duna-network", aster.NextMissionTargetId);
            TestAssert.Equal("Duna", aster.NextMissionDisplayName);
        }

        public static void CompletesArbitrarySatelliteProgramme()
        {
            var player = new AgencyState("player", "Player", true);
            var aster = new AgencyState("aster", "Aster", false);
            var cobalt = new AgencyState("cobalt", "Cobalt", false);
            var delta = new AgencyState("delta", "Delta", false)
            {
                NextMissionTargetId = "duna-network",
                MissionProgressPercent = 100
            };

            var satelliteNetworkFundingContracts = new List<SatelliteNetworkFundingContract>
            {
                new SatelliteNetworkFundingContract(
                    "duna-network",
                    "Duna Orbital Network",
                    "Duna",
                    5,
                    100000.0,
                    true,
                    null,
                    (UnlockRuleDefinition)null)
            };
            satelliteNetworkFundingContracts[0].Offer();

            RivalSimulation.Refresh(
                new List<AgencyState> { player, aster, cobalt, delta },
                1234.0,
                new List<ObjectiveFundingContract>(),
                satelliteNetworkFundingContracts);

            TestAssert.Equal(1, delta.GetSatelliteCount("Duna"));
            TestAssert.Equal(0, delta.MissionProgressPercent);
            TestAssert.Equal("duna-network", delta.NextMissionTargetId);

            var malformedRival = new AgencyState("broken", "Broken", false)
            {
                NextMissionTargetId = "broken-network",
                MissionProgressPercent = 100
            };
            var malformedSatelliteNetworkFundingContract = new SatelliteNetworkFundingContract(
                "broken-network",
                "Broken Network",
                string.Empty,
                5,
                1000.0,
                true,
                null,
                (UnlockRuleDefinition)null);
            malformedSatelliteNetworkFundingContract.Offer();

            RivalSimulation.Refresh(
                new List<AgencyState> { player, malformedRival },
                1235.0,
                new List<ObjectiveFundingContract>(),
                new List<SatelliteNetworkFundingContract> { malformedSatelliteNetworkFundingContract });

            TestAssert.Equal(null, malformedRival.NextMissionTargetId);
            TestAssert.Equal(0, malformedRival.MissionProgressPercent);
        }

        public static void CollectionCostUsesProgrammeBody()
        {
            var aster = new AgencyState("Aster", false)
            {
                NextMissionTargetId = FundingContractCatalogue.DunaNetworkId,
                NextMissionDisplayName = "Stored presentation text"
            };
            var presentationOnlyTarget = new AgencyState("PresentationOnly", false)
            {
                NextMissionDisplayName = "Duna Orbital Network"
            };
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts =
                FundingContractCatalogue.CreateSatelliteProgrammes();
            IList<ObjectiveFundingContract> achievementProgrammes =
                FundingContractCatalogue.CreateAchievementProgrammes();

            double cost = RivalSimulation.CalculateLaunchProgressCost(
                aster,
                achievementProgrammes,
                satelliteNetworkFundingContracts);
            double presentationOnlyCost = RivalSimulation.CalculateLaunchProgressCost(
                presentationOnlyTarget,
                achievementProgrammes,
                satelliteNetworkFundingContracts);

            TestAssert.Equal(80000.0, cost);
            TestAssert.Equal(20000.0, presentationOnlyCost);
            TestAssert.Equal(10, RivalSimulation.CalculateLaunchProgressIncrementPercent(aster));
            TestAssert.Equal(FundingContractCatalogue.DunaNetworkId, aster.NextMissionTargetId);
            TestAssert.Equal("Stored presentation text", aster.NextMissionDisplayName);
            TestAssert.Equal(null, presentationOnlyTarget.NextMissionTargetId);
            TestAssert.Equal("Duna Orbital Network", presentationOnlyTarget.NextMissionDisplayName);

            aster.NextMissionTargetId = ObjectiveCatalogue.MunCrewedOrbitId;
            TestAssert.Equal(
                60000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    aster,
                    achievementProgrammes,
                    satelliteNetworkFundingContracts));
            TestAssert.Equal(10, RivalSimulation.CalculateLaunchProgressIncrementPercent(aster));

            aster.NextMissionTargetId = ObjectiveCatalogue.DunaProbeOrbitId;
            TestAssert.Equal(
                60000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    aster,
                    achievementProgrammes,
                    satelliteNetworkFundingContracts));

            aster.NextMissionTargetId = ObjectiveCatalogue.DunaCrewedOrbitId;
            TestAssert.Equal(
                100000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    aster,
                    achievementProgrammes,
                    satelliteNetworkFundingContracts));

            aster.NextMissionTargetId = ObjectiveCatalogue.DirectedPower1Id;
            TestAssert.Equal(
                4000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    aster,
                    achievementProgrammes,
                    satelliteNetworkFundingContracts));
            TestAssert.Equal(20, RivalSimulation.CalculateLaunchProgressIncrementPercent(aster));

            aster.NextMissionTargetId = ObjectiveCatalogue.Biome5Id;
            TestAssert.Equal(
                12000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    aster,
                    achievementProgrammes,
                    satelliteNetworkFundingContracts));
            TestAssert.Equal(20, RivalSimulation.CalculateLaunchProgressIncrementPercent(aster));

            double originalProgressChance = CampaignSettings.RivalProgressChance;
            try
            {
                CampaignSettings.RivalProgressChance = 1.0;
                ObjectiveDefinition starterMilestone = ObjectiveCatalogue.FindById(
                    ObjectiveCatalogue.DirectedPower1Id);
                var starterProgramme = new ObjectiveFundingContract(
                    starterMilestone.Id,
                    starterMilestone.Name,
                    starterMilestone.ObjectiveDescription,
                    starterMilestone.BaseRewardFunds);
                starterProgramme.Offer();

                var player = new AgencyState("Player", true);
                var starterRival = new AgencyState("PreOrbitRival", false)
                {
                    NextMissionTargetId = ObjectiveCatalogue.DirectedPower1Id,
                    Funds = 100000.0,
                    NextMissionProgressCheckUniversalTime = 5.0 * 21600.0
                };
                RivalSimulation.Refresh(
                    new List<AgencyState> { player, starterRival },
                    5.0 * 21600.0,
                    new List<ObjectiveFundingContract> { starterProgramme },
                    new List<SatelliteNetworkFundingContract>());

                TestAssert.Equal(20, starterRival.MissionProgressPercent);
                TestAssert.Equal(96000.0, starterRival.Funds);

                var starterEtaRival = new AgencyState("PreOrbitEta", false)
                {
                    NextMissionTargetId = ObjectiveCatalogue.DirectedPower1Id,
                    Funds = 1000000.0
                };
                var orbitEtaRival = new AgencyState("OrbitEta", false)
                {
                    NextMissionTargetId = ObjectiveCatalogue.ProbeOrbitId,
                    Funds = 1000000.0
                };

                TestAssert.Equal(
                    25,
                    RivalSimulation.CalculateEstimatedLaunchDays(
                        starterEtaRival,
                        0.0,
                        90.0 * 21600.0,
                        90.0 * 21600.0,
                        achievementProgrammes,
                        satelliteNetworkFundingContracts));
                TestAssert.Equal(
                    50,
                    RivalSimulation.CalculateEstimatedLaunchDays(
                        orbitEtaRival,
                        0.0,
                        90.0 * 21600.0,
                        90.0 * 21600.0,
                        achievementProgrammes,
                        satelliteNetworkFundingContracts));
                TestAssert.Equal(
                    null,
                    RivalSimulation.CalculateEstimatedLaunchDays(
                        starterEtaRival,
                        double.NaN,
                        90.0 * 21600.0,
                        90.0 * 21600.0,
                        achievementProgrammes,
                        satelliteNetworkFundingContracts));
                TestAssert.Equal(
                    null,
                    RivalSimulation.CalculateEstimatedLaunchDays(
                        starterEtaRival,
                        0.0,
                        90.0 * 21600.0,
                        double.PositiveInfinity,
                        achievementProgrammes,
                        satelliteNetworkFundingContracts));

                CampaignSettings.RivalProgressChance = double.Epsilon;
                TestAssert.Equal(
                    null,
                    RivalSimulation.CalculateEstimatedLaunchDays(
                        starterEtaRival,
                        0.0,
                        90.0 * 21600.0,
                        90.0 * 21600.0,
                        achievementProgrammes,
                        satelliteNetworkFundingContracts));
            }
            finally
            {
                CampaignSettings.RivalProgressChance = originalProgressChance;
            }
        }

        public static void AchievementCompletionUsesObjectiveDefinition()
        {
            const double completionUniversalTime = 4321.0;
            var player = new AgencyState("Player", true);
            player.RecordObjectiveCompletion(ObjectiveCatalogue.MunProbeOrbitId, 1.0);
            var aster = new AgencyState("Aster", false)
            {
                NextMissionTargetId = ObjectiveCatalogue.DunaProbeOrbitId,
                MissionProgressPercent = 100
            };
            var cobalt = new AgencyState("Cobalt", false);
            cobalt.RecordObjectiveCompletion(ObjectiveCatalogue.MinmusProbeOrbitId, 2.0);

            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(
                ObjectiveCatalogue.DunaProbeOrbitId);
            var achievementProgrammes = new List<ObjectiveFundingContract>
            {
                new ObjectiveFundingContract(
                    objective.Id,
                    objective.Name,
                    objective.ObjectiveDescription,
                    200000.0,
                    "Display text",
                    objective.UnlockRule)
            };
            achievementProgrammes[0].Offer();

            RivalSimulation.Refresh(
                new List<AgencyState> { player, aster, cobalt },
                double.NaN,
                achievementProgrammes,
                new List<SatelliteNetworkFundingContract>());

            TestAssert.True(
                !aster.HasCompletedObjective(ObjectiveCatalogue.DunaProbeOrbitId),
                "Invalid simulation time must not complete a rival mission.");
            TestAssert.Equal(100, aster.MissionProgressPercent);
            TestAssert.Equal(ObjectiveCatalogue.DunaProbeOrbitId, aster.NextMissionTargetId);

            RivalSimulation.Refresh(
                new List<AgencyState> { player, aster, cobalt },
                completionUniversalTime,
                achievementProgrammes,
                new List<SatelliteNetworkFundingContract>());

            TestAssert.True(
                aster.HasCompletedObjective(ObjectiveCatalogue.DunaProbeOrbitId),
                "Duna objectiveCompletion completion should record the objective ID.");
            TestAssert.Equal(
                completionUniversalTime,
                aster.GetObjectiveCompletionTime(ObjectiveCatalogue.DunaProbeOrbitId));
            TestAssert.Equal(1, aster.GetSatelliteCount("Duna"));
            TestAssert.Equal(null, aster.NextMissionTargetId);
        }
    }

    internal static class TestAssert
    {
        public static void True(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.InvalidOperationException(message);
            }
        }

        public static void Equal<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new System.InvalidOperationException(
                    "Expected '" + expected + "' but got '" + actual + "'.");
            }
        }
    }
}

using System.Collections.Generic;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;
using TheRaceForSpace.Simulation;

namespace TheRaceForSpace.Tests.Simulation
{
    internal static class RivalSimulationCollectionTests
    {
        public static void SelectsOnlyAvailableAchievementFromCollection()
        {
            var player = new SpaceProgramState("player", "Player", true);
            var aster = new SpaceProgramState("aster", "Aster", false)
            {
                NextLaunchProgressCheckUniversalTime = double.NaN
            };
            var cobalt = new SpaceProgramState("cobalt", "Cobalt", false);
            var delta = new SpaceProgramState("delta", "Delta", false);
            delta.RecordAchievement(PrototypeMilestones.CrewedOrbitId, 1.0);

            MilestoneDefinition milestone = PrototypeMilestones.FindById(
                PrototypeMilestones.MinmusCrewedOrbitId);
            var achievementProgrammes = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme(
                    milestone.Id,
                    milestone.Name,
                    milestone.ObjectiveDescription,
                    300000.0,
                    "Display text",
                    milestone.UnlockRule)
            };
            achievementProgrammes[0].Offer();

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, aster, cobalt, delta },
                1.0,
                achievementProgrammes,
                new List<FundingProgramme>());

            TestAssert.Equal(PrototypeMilestones.MinmusCrewedOrbitId, aster.NextMissionTargetId);
            TestAssert.Equal("Minmus Crewed Orbit", aster.NextLaunchBodyName);
            TestAssert.Equal(PrototypeMilestones.MinmusCrewedOrbitId, cobalt.NextMissionTargetId);
            TestAssert.Equal(5.0 * 21600.0, aster.NextLaunchProgressCheckUniversalTime);
        }

        public static void LockedAchievementIsExcludedFromCollection()
        {
            var player = new SpaceProgramState("Player", true);
            var aster = new SpaceProgramState("Aster", false);
            var cobalt = new SpaceProgramState("Cobalt", false);

            MilestoneDefinition milestone = PrototypeMilestones.FindById(
                PrototypeMilestones.MunProbeOrbitId);
            var achievementProgrammes = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme(
                    milestone.Id,
                    milestone.Name,
                    milestone.ObjectiveDescription,
                    200000.0,
                    "Display text",
                    milestone.UnlockRule)
            };

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, aster, cobalt },
                0.0,
                achievementProgrammes,
                new List<FundingProgramme>());

            TestAssert.Equal(null, aster.NextMissionTargetId);

            var malformedRival = new SpaceProgramState("malformed", "Malformed", false)
            {
                NextMissionTargetId = "missing-milestone",
                LaunchProgressPercent = 100
            };
            var malformedProgramme = new AchievementFundingProgramme(
                "missing-milestone",
                "Missing Milestone",
                "This target has no milestone definition.",
                1000.0);
            malformedProgramme.Offer();

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, malformedRival },
                1.0,
                new List<AchievementFundingProgramme> { malformedProgramme },
                new List<FundingProgramme>());

            TestAssert.Equal(null, malformedRival.NextMissionTargetId);
            TestAssert.Equal(0, malformedRival.LaunchProgressPercent);
        }

        public static void FlexibleRuleUsesRivalScopeCountAndHistoricalTime()
        {
            var player = new SpaceProgramState("player", "Player", true);
            var aster = new SpaceProgramState("aster", "Aster", false);
            var cobalt = new SpaceProgramState("cobalt", "Cobalt", false);
            var delta = new SpaceProgramState("delta", "Delta", false);
            aster.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 100.0);
            delta.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 200.0);

            MilestoneDefinition milestone = PrototypeMilestones.FindById(
                PrototypeMilestones.MinmusCrewedOrbitId);
            var achievementProgrammes = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme(
                    milestone.Id,
                    milestone.Name,
                    milestone.ObjectiveDescription,
                    300000.0,
                    "Display text",
                    new UnlockRuleDefinition(
                        new UnlockPathDefinition(
                            UnlockConditionDefinition.Achievement(
                                PrototypeMilestones.ProbeOrbitId,
                                UnlockProgramScope.AnyRival,
                                2))))
            };
            var programs = new List<SpaceProgramState> { player, aster, cobalt, delta };

            RivalSimulation.Refresh(
                programs,
                199.0,
                achievementProgrammes,
                new List<FundingProgramme>());
            TestAssert.Equal(
                null,
                cobalt.NextMissionTargetId);

            // Sponsor selection is now a controller responsibility. Once a contract is Offered,
            // rival simulation consumes that stable state rather than reevaluating its unlock rule.
            achievementProgrammes[0].Offer();
            RivalSimulation.Refresh(
                programs,
                200.0,
                achievementProgrammes,
                new List<FundingProgramme>());
            TestAssert.Equal(
                PrototypeMilestones.MinmusCrewedOrbitId,
                cobalt.NextMissionTargetId);
        }

        public static void SatelliteProgrammeRemainsRepeatable()
        {
            var player = new SpaceProgramState("Player", true);
            var aster = new SpaceProgramState("Aster", false);
            var cobalt = new SpaceProgramState("Cobalt", false);
            aster.SetSatelliteCount("Duna", 3);

            var fundingProgrammes = new List<FundingProgramme>
            {
                new FundingProgramme(
                    "duna-network",
                    "Duna Orbital Network",
                    "Duna",
                    5,
                    100000.0,
                    true,
                    null,
                    (UnlockRuleDefinition)null)
            };
            fundingProgrammes[0].Offer();

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, aster, cobalt },
                0.0,
                new List<AchievementFundingProgramme>(),
                fundingProgrammes);

            TestAssert.Equal("duna-network", aster.NextMissionTargetId);
            TestAssert.Equal("Duna", aster.NextLaunchBodyName);
        }

        public static void CompletesArbitrarySatelliteProgramme()
        {
            var player = new SpaceProgramState("player", "Player", true);
            var aster = new SpaceProgramState("aster", "Aster", false);
            var cobalt = new SpaceProgramState("cobalt", "Cobalt", false);
            var delta = new SpaceProgramState("delta", "Delta", false)
            {
                NextMissionTargetId = "duna-network",
                LaunchProgressPercent = 100
            };

            var fundingProgrammes = new List<FundingProgramme>
            {
                new FundingProgramme(
                    "duna-network",
                    "Duna Orbital Network",
                    "Duna",
                    5,
                    100000.0,
                    true,
                    null,
                    (UnlockRuleDefinition)null)
            };
            fundingProgrammes[0].Offer();

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, aster, cobalt, delta },
                1234.0,
                new List<AchievementFundingProgramme>(),
                fundingProgrammes);

            TestAssert.Equal(1, delta.GetSatelliteCount("Duna"));
            TestAssert.Equal(0, delta.LaunchProgressPercent);
            TestAssert.Equal("duna-network", delta.NextMissionTargetId);

            var malformedRival = new SpaceProgramState("broken", "Broken", false)
            {
                NextMissionTargetId = "broken-network",
                LaunchProgressPercent = 100
            };
            var malformedFundingProgramme = new FundingProgramme(
                "broken-network",
                "Broken Network",
                string.Empty,
                5,
                1000.0,
                true,
                null,
                (UnlockRuleDefinition)null);
            malformedFundingProgramme.Offer();

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, malformedRival },
                1235.0,
                new List<AchievementFundingProgramme>(),
                new List<FundingProgramme> { malformedFundingProgramme });

            TestAssert.Equal(null, malformedRival.NextMissionTargetId);
            TestAssert.Equal(0, malformedRival.LaunchProgressPercent);
        }

        public static void CollectionCostUsesProgrammeBody()
        {
            var aster = new SpaceProgramState("Aster", false)
            {
                NextMissionTargetId = PrototypeFundingCatalogue.DunaNetworkId,
                NextLaunchBodyName = "Stored presentation text"
            };
            var presentationOnlyTarget = new SpaceProgramState("PresentationOnly", false)
            {
                NextLaunchBodyName = "Duna Orbital Network"
            };
            IList<FundingProgramme> fundingProgrammes =
                PrototypeFundingCatalogue.CreateSatelliteProgrammes();
            IList<AchievementFundingProgramme> achievementProgrammes =
                PrototypeFundingCatalogue.CreateAchievementProgrammes();

            double cost = RivalSimulation.CalculateLaunchProgressCost(
                aster,
                achievementProgrammes,
                fundingProgrammes);
            double presentationOnlyCost = RivalSimulation.CalculateLaunchProgressCost(
                presentationOnlyTarget,
                achievementProgrammes,
                fundingProgrammes);

            TestAssert.Equal(80000.0, cost);
            TestAssert.Equal(20000.0, presentationOnlyCost);
            TestAssert.Equal(10, RivalSimulation.CalculateLaunchProgressIncrementPercent(aster));
            TestAssert.Equal(PrototypeFundingCatalogue.DunaNetworkId, aster.NextMissionTargetId);
            TestAssert.Equal("Stored presentation text", aster.NextLaunchBodyName);
            TestAssert.Equal(null, presentationOnlyTarget.NextMissionTargetId);
            TestAssert.Equal("Duna Orbital Network", presentationOnlyTarget.NextLaunchBodyName);

            aster.NextMissionTargetId = PrototypeMilestones.MunCrewedOrbitId;
            TestAssert.Equal(
                60000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    aster,
                    achievementProgrammes,
                    fundingProgrammes));
            TestAssert.Equal(10, RivalSimulation.CalculateLaunchProgressIncrementPercent(aster));

            aster.NextMissionTargetId = PrototypeMilestones.DunaProbeOrbitId;
            TestAssert.Equal(
                60000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    aster,
                    achievementProgrammes,
                    fundingProgrammes));

            aster.NextMissionTargetId = PrototypeMilestones.DunaCrewedOrbitId;
            TestAssert.Equal(
                100000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    aster,
                    achievementProgrammes,
                    fundingProgrammes));

            aster.NextMissionTargetId = PrototypeMilestones.DirectedPower1Id;
            TestAssert.Equal(
                4000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    aster,
                    achievementProgrammes,
                    fundingProgrammes));
            TestAssert.Equal(20, RivalSimulation.CalculateLaunchProgressIncrementPercent(aster));

            aster.NextMissionTargetId = PrototypeMilestones.Biome5Id;
            TestAssert.Equal(
                12000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    aster,
                    achievementProgrammes,
                    fundingProgrammes));
            TestAssert.Equal(20, RivalSimulation.CalculateLaunchProgressIncrementPercent(aster));

            double originalProgressChance = RaceSettings.RivalProgressChance;
            try
            {
                RaceSettings.RivalProgressChance = 1.0;
                MilestoneDefinition starterMilestone = PrototypeMilestones.FindById(
                    PrototypeMilestones.DirectedPower1Id);
                var starterProgramme = new AchievementFundingProgramme(
                    starterMilestone.Id,
                    starterMilestone.Name,
                    starterMilestone.ObjectiveDescription,
                    starterMilestone.BaseRewardFunds);
                starterProgramme.Offer();

                var player = new SpaceProgramState("Player", true);
                var starterRival = new SpaceProgramState("StarterRival", false)
                {
                    NextMissionTargetId = PrototypeMilestones.DirectedPower1Id,
                    Funds = 100000.0,
                    NextLaunchProgressCheckUniversalTime = 5.0 * 21600.0
                };
                RivalSimulation.Refresh(
                    new List<SpaceProgramState> { player, starterRival },
                    5.0 * 21600.0,
                    new List<AchievementFundingProgramme> { starterProgramme },
                    new List<FundingProgramme>());

                TestAssert.Equal(20, starterRival.LaunchProgressPercent);
                TestAssert.Equal(96000.0, starterRival.Funds);

                var starterEtaRival = new SpaceProgramState("StarterEta", false)
                {
                    NextMissionTargetId = PrototypeMilestones.DirectedPower1Id,
                    Funds = 1000000.0
                };
                var orbitEtaRival = new SpaceProgramState("OrbitEta", false)
                {
                    NextMissionTargetId = PrototypeMilestones.ProbeOrbitId,
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
                        fundingProgrammes));
                TestAssert.Equal(
                    50,
                    RivalSimulation.CalculateEstimatedLaunchDays(
                        orbitEtaRival,
                        0.0,
                        90.0 * 21600.0,
                        90.0 * 21600.0,
                        achievementProgrammes,
                        fundingProgrammes));
                TestAssert.Equal(
                    null,
                    RivalSimulation.CalculateEstimatedLaunchDays(
                        starterEtaRival,
                        double.NaN,
                        90.0 * 21600.0,
                        90.0 * 21600.0,
                        achievementProgrammes,
                        fundingProgrammes));
                TestAssert.Equal(
                    null,
                    RivalSimulation.CalculateEstimatedLaunchDays(
                        starterEtaRival,
                        0.0,
                        90.0 * 21600.0,
                        double.PositiveInfinity,
                        achievementProgrammes,
                        fundingProgrammes));

                RaceSettings.RivalProgressChance = double.Epsilon;
                TestAssert.Equal(
                    null,
                    RivalSimulation.CalculateEstimatedLaunchDays(
                        starterEtaRival,
                        0.0,
                        90.0 * 21600.0,
                        90.0 * 21600.0,
                        achievementProgrammes,
                        fundingProgrammes));
            }
            finally
            {
                RaceSettings.RivalProgressChance = originalProgressChance;
            }
        }

        public static void AchievementCompletionUsesMilestoneDefinition()
        {
            const double completionUniversalTime = 4321.0;
            var player = new SpaceProgramState("Player", true);
            player.RecordAchievement(PrototypeMilestones.MunProbeOrbitId, 1.0);
            var aster = new SpaceProgramState("Aster", false)
            {
                NextMissionTargetId = PrototypeMilestones.DunaProbeOrbitId,
                LaunchProgressPercent = 100
            };
            var cobalt = new SpaceProgramState("Cobalt", false);
            cobalt.RecordAchievement(PrototypeMilestones.MinmusProbeOrbitId, 2.0);

            MilestoneDefinition milestone = PrototypeMilestones.FindById(
                PrototypeMilestones.DunaProbeOrbitId);
            var achievementProgrammes = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme(
                    milestone.Id,
                    milestone.Name,
                    milestone.ObjectiveDescription,
                    200000.0,
                    "Display text",
                    milestone.UnlockRule)
            };
            achievementProgrammes[0].Offer();

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, aster, cobalt },
                double.NaN,
                achievementProgrammes,
                new List<FundingProgramme>());

            TestAssert.True(
                !aster.HasAchievement(PrototypeMilestones.DunaProbeOrbitId),
                "Invalid simulation time must not complete a rival mission.");
            TestAssert.Equal(100, aster.LaunchProgressPercent);
            TestAssert.Equal(PrototypeMilestones.DunaProbeOrbitId, aster.NextMissionTargetId);

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, aster, cobalt },
                completionUniversalTime,
                achievementProgrammes,
                new List<FundingProgramme>());

            TestAssert.True(
                aster.HasAchievement(PrototypeMilestones.DunaProbeOrbitId),
                "Duna achievement completion should record the milestone ID.");
            TestAssert.Equal(
                completionUniversalTime,
                aster.GetAchievementUniversalTime(PrototypeMilestones.DunaProbeOrbitId));
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

using System;
using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Persistence;
using TheRaceForSpace.Programs;
using TheRaceForSpace.Simulation;
using TheRaceForSpace.Tests.Milestones;
using TheRaceForSpace.Tests.Persistence;
using TheRaceForSpace.Tests.Simulation;
using TheRaceForSpace.Tests.Tracking;

namespace TheRaceForSpace.Tests
{
    internal static class Program
    {
        private static int _failures;

        private static int Main()
        {
            Run("Satellite funding before saturation", SatelliteFundingBeforeSaturation);
            Run("Satellite funding after saturation", SatelliteFundingAfterSaturation);
            Run("Satellite funding stores unlock rule", SatelliteFundingStoresStructuredPrerequisite);
            Run("Achievement payout declines and expires", AchievementPayoutDeclinesAndExpires);
            Run("Achievement restore normalizes lifecycle", AchievementRestoreNormalizesLifecycle);
            Run("Achievement funding stores unlock rule", AchievementFundingStoresStructuredPrerequisite);
            Run("Prototype funding catalogue matches current campaign", PrototypeFundingCatalogueTests.CatalogueMatchesCurrentPrototype);
            Run("Prototype funding catalogue creates fresh state", PrototypeFundingCatalogueTests.CatalogueCreatesFreshCampaignState);
            Run("Prototype milestone definitions match current campaign", PrototypeMilestoneDefinitionsMatchCurrentCampaign);
            Run("Prototype milestone ids are unique", PrototypeMilestoneIdsAreUnique);
            Run("Prototype milestone lookup uses stable ids", PrototypeMilestoneLookupUsesStableIds);
            Run("Milestone probe observation matches definition", MilestoneEvaluationTests.ProbeObservationMatchesDefinition);
            Run("Milestone crewed observation matches definition", MilestoneEvaluationTests.CrewedObservationMatchesDefinition);
            Run("Milestone rejects wrong body or situation", MilestoneEvaluationTests.WrongBodyOrSituationDoesNotMatch);
            Run("Milestone arbitrary body uses same rule", MilestoneEvaluationTests.ArbitraryBodyDefinitionUsesSameRule);
            Run("Tracking snapshots update counts and milestones", SatelliteTrackerTests.NormalizedSnapshotsUpdateCountsAndMilestones);
            Run("Tracking empty snapshots reset body counts", SatelliteTrackerTests.EmptySnapshotsResetTrackedBodyCounts);
            Run("Tracking crewed probe keeps satellite classification", SatelliteTrackerTests.CrewedProbeCountsAsSatelliteButNotProbeMilestone);
            Run("Tracking flexible unlock uses race state and time", SatelliteTrackerTests.FlexibleUnlockRuleUsesRaceStateAndTime);
            Run("Starter flight contracts and persistence", StarterFlightTrackerTests.RunAll);
            Run("Generic achievement state starts empty", GenericAchievementStateStartsEmpty);
            Run("Generic achievement state records first timestamp", GenericAchievementStateRecordsFirstTimestamp);
            Run("Generic achievement state preserves first timestamp", GenericAchievementStatePreservesFirstTimestamp);
            Run("Generic achievement state accepts arbitrary ids", GenericAchievementStateAcceptsArbitraryIds);
            Run("Generic achievement state validates timestamps", GenericAchievementStateValidatesTimestamps);
            Run("Rival mission target ids map to display names", RivalMissionTargetIdsMapToDisplayNames);
            Run("Rival launch costs match target type", RivalLaunchCostsMatchTargetType);
            Run("Rival starter completion does not create satellite", RivalStarterCompletionDoesNotCreateSatellite);
            Run("Rival ETA detects unaffordable mission", RivalEtaDetectsUnaffordableMission);
            Run("Unavailable rival target is abandoned", UnavailableRivalTargetIsAbandoned);
            Run("Invalid rival target id is abandoned", InvalidRivalTargetIdIsAbandoned);
            Run("Rival selects the only offered target", RivalSelectsOnlyAvailableTarget);
            Run("Rival completion uses replay timestamp", RivalCompletionUsesReplayTimestamp);
            Run("Rival collection selects offered achievement", RivalSimulationCollectionTests.SelectsOnlyAvailableAchievementFromCollection);
            Run("Rival collection excludes locked achievement", RivalSimulationCollectionTests.LockedAchievementIsExcludedFromCollection);
            Run("Rival satellite programme remains repeatable", RivalSimulationCollectionTests.SatelliteProgrammeRemainsRepeatable);
            Run("Rival completes arbitrary satellite programme", RivalSimulationCollectionTests.CompletesArbitrarySatelliteProgramme);
            Run("Rival collection cost uses programme body", RivalSimulationCollectionTests.CollectionCostUsesProgrammeBody);
            Run("Rival achievement completion uses milestone definition", RivalSimulationCollectionTests.AchievementCompletionUsesMilestoneDefinition);
            Run("Legacy player save keys restore generic achievement", RaceProgressLegacySaveKeysRestoreGenericState);
            Run("Race progress persistence round trip", RaceProgressPersistenceRoundTrip);
            Run("Rival persistence round trip", RivalPersistenceRoundTrip);
            Run("Race progress persists arbitrary ids", CollectionPersistenceTests.RaceProgressRoundTripsArbitraryIds);
            Run("Rival persistence handles arbitrary body and target", CollectionPersistenceTests.RivalRoundTripsArbitraryBodyAndTargetId);
            Run("Malformed persistence nodes are safe", CollectionPersistenceTests.MalformedCollectionNodesAreHandledSafely);
            Run("Empty persistence nodes stay empty", CollectionPersistenceTests.EmptyCollectionNodesRestoreWithoutInventingState);

            Console.WriteLine();
            Console.WriteLine(_failures == 0
                ? "All prototype logic tests passed."
                : _failures + " prototype logic test(s) failed.");
            return _failures == 0 ? 0 : 1;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("PASS: " + name);
            }
            catch (Exception exception)
            {
                _failures++;
                Console.WriteLine("FAIL: " + name);
                Console.WriteLine("      " + exception.Message);
            }
        }

        private static void SatelliteFundingBeforeSaturation()
        {
            var programme = new FundingProgramme("network", "Network", "Kerbin", 10, 200000.0);
            AssertEqual(60000.0, programme.CalculateCurrentPayout(3, 7));
        }

        private static void SatelliteFundingAfterSaturation()
        {
            var programme = new FundingProgramme("network", "Network", "Kerbin", 10, 200000.0);
            AssertEqual(50000.0, programme.CalculateCurrentPayout(3, 12));
        }

        private static void SatelliteFundingStoresStructuredPrerequisite()
        {
            var programme = new FundingProgramme(
                "mun-network",
                "Mun Network",
                "Mun",
                5,
                100000.0,
                false,
                "Display-only unlock text",
                UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.MunProbeOrbitId));

            AssertAnyAgencyRule(programme.UnlockRule, PrototypeMilestones.MunProbeOrbitId);
            AssertEqual("Display-only unlock text", programme.UnlockRequirement);
            AssertTrue(!programme.IsAvailable, "Structured unlock rules should not change initial availability.");

            var startAvailableProgramme = new FundingProgramme(
                "start-network",
                "Start Network",
                "Kerbin",
                5,
                100000.0,
                false,
                "Display-only unlock text");
            AssertEqual(null, startAvailableProgramme.UnlockRule);
        }

        private static void AchievementPayoutDeclinesAndExpires()
        {
            var programme = new AchievementFundingProgramme("probe", "Probe", "Orbit", 100000.0);
            programme.Start();

            AssertEqual(50000.0, programme.CalculateCurrentPayout(true, 2));
            programme.AdvancePayout();
            AssertEqual(90, programme.CurrentInterestPercent);
            AssertEqual(45000.0, programme.CalculateCurrentPayout(true, 2));

            for (int payment = 1; payment < 10; payment++)
            {
                programme.AdvancePayout();
            }

            AssertTrue(programme.IsExpired, "Contract should expire after ten payments.");
            AssertEqual(0.0, programme.CalculateCurrentPayout(true, 1));
        }

        private static void AchievementRestoreNormalizesLifecycle()
        {
            var programme = new AchievementFundingProgramme("probe", "Probe", "Orbit", 100000.0);
            programme.RestoreState(false, 4);

            AssertTrue(programme.HasStarted, "Processed payments imply a started contract.");
            AssertEqual(4, programme.PaymentsProcessed);
            AssertEqual(60, programme.CurrentInterestPercent);
        }

        private static void AchievementFundingStoresStructuredPrerequisite()
        {
            var programme = new AchievementFundingProgramme(
                PrototypeMilestones.MunProbeOrbitId,
                "Mun Probe Orbit",
                "Orbit Mun",
                200000.0,
                "Display-only unlock text",
                UnlockRuleDefinition.AnyAgencyAchievement(PrototypeMilestones.ProbeOrbitId));

            AssertAnyAgencyRule(programme.UnlockRule, PrototypeMilestones.ProbeOrbitId);
            AssertEqual("Display-only unlock text", programme.UnlockRequirement);

            var startAvailableProgramme = new AchievementFundingProgramme(
                "start-achievement",
                "Start Achievement",
                "Objective",
                100000.0,
                "Display-only unlock text");
            AssertEqual(null, startAvailableProgramme.UnlockRule);
        }

        private static void PrototypeMilestoneDefinitionsMatchCurrentCampaign()
        {
            AssertEqual(32, PrototypeMilestones.All.Count);
            AssertEqual(20, PrototypeMilestones.StarterContracts.Count);

            AssertMilestone(
                PrototypeMilestones.All[0],
                PrototypeMilestones.ProbeOrbitId,
                "Probe Orbit",
                "Kerbin",
                MilestoneCrewRequirement.UncrewedProbe,
                PrototypeMilestones.DirectedPower5Id,
                PrototypeMilestones.Mass5Id,
                PrototypeMilestones.Control5Id,
                PrototypeMilestones.Biome5Id);
            AssertMilestone(
                PrototypeMilestones.All[1],
                PrototypeMilestones.CrewedOrbitId,
                "Crewed Orbit",
                "Kerbin",
                MilestoneCrewRequirement.Crewed,
                PrototypeMilestones.ProbeOrbitId);
            AssertMilestone(
                PrototypeMilestones.All[2],
                PrototypeMilestones.MunProbeOrbitId,
                "Mun Probe Orbit",
                "Mun",
                MilestoneCrewRequirement.UncrewedProbe,
                PrototypeMilestones.ProbeOrbitId);
            AssertMilestone(
                PrototypeMilestones.All[3],
                PrototypeMilestones.MinmusProbeOrbitId,
                "Minmus Probe Orbit",
                "Minmus",
                MilestoneCrewRequirement.UncrewedProbe,
                PrototypeMilestones.ProbeOrbitId);
            AssertMilestone(
                PrototypeMilestones.All[4],
                PrototypeMilestones.DunaProbeOrbitId,
                "Duna Probe Orbit",
                "Duna",
                MilestoneCrewRequirement.UncrewedProbe,
                PrototypeMilestones.MunProbeOrbitId,
                PrototypeMilestones.MinmusProbeOrbitId);
            AssertMilestone(
                PrototypeMilestones.All[5],
                PrototypeMilestones.MunCrewedOrbitId,
                "Mun Crewed Orbit",
                "Mun",
                MilestoneCrewRequirement.Crewed,
                PrototypeMilestones.CrewedOrbitId);
            AssertMilestone(
                PrototypeMilestones.All[6],
                PrototypeMilestones.MinmusCrewedOrbitId,
                "Minmus Crewed Orbit",
                "Minmus",
                MilestoneCrewRequirement.Crewed,
                PrototypeMilestones.CrewedOrbitId);
            AssertMilestone(
                PrototypeMilestones.All[7],
                PrototypeMilestones.DunaCrewedOrbitId,
                "Duna Crewed Orbit",
                "Duna",
                MilestoneCrewRequirement.Crewed,
                PrototypeMilestones.MunCrewedOrbitId,
                PrototypeMilestones.MinmusCrewedOrbitId);
        }

        private static void PrototypeMilestoneIdsAreUnique()
        {
            var milestoneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int milestoneIndex = 0; milestoneIndex < PrototypeMilestones.All.Count; milestoneIndex++)
            {
                MilestoneDefinition milestone = PrototypeMilestones.All[milestoneIndex];
                AssertTrue(
                    milestoneIds.Add(milestone.Id),
                    "Duplicate milestone id found: " + milestone.Id);
            }

            for (int milestoneIndex = 0; milestoneIndex < PrototypeMilestones.StarterContracts.Count; milestoneIndex++)
            {
                MilestoneDefinition milestone = PrototypeMilestones.StarterContracts[milestoneIndex];
                AssertTrue(
                    milestoneIds.Add(milestone.Id),
                    "Duplicate starter milestone id found: " + milestone.Id);
            }
        }

        private static void PrototypeMilestoneLookupUsesStableIds()
        {
            MilestoneDefinition milestone = PrototypeMilestones.FindById("MUN-PROBE-ORBIT");
            MilestoneDefinition dunaMilestone = PrototypeMilestones.FindById("DUNA-PROBE-ORBIT");
            MilestoneDefinition starterMilestone = PrototypeMilestones.FindById("DIRECTED-POWER-1");

            AssertTrue(milestone != null, "Known milestone IDs should resolve case-insensitively.");
            AssertEqual(PrototypeMilestones.MunProbeOrbitId, milestone.Id);
            AssertAnyAgencyRule(milestone.UnlockRule, PrototypeMilestones.ProbeOrbitId);
            AssertTrue(dunaMilestone != null, "Duna milestone should resolve through the shared catalogue.");
            AssertEqual("Duna", dunaMilestone.CelestialBodyName);
            AssertAnyAgencyRule(
                dunaMilestone.UnlockRule,
                PrototypeMilestones.MunProbeOrbitId,
                PrototypeMilestones.MinmusProbeOrbitId);
            AssertTrue(starterMilestone != null, "Starter milestone IDs should share the stable lookup path.");
            AssertEqual(StarterContractLine.DirectedPower, starterMilestone.StarterLine);
            AssertEqual(null, PrototypeMilestones.FindById("not-a-milestone"));
        }

        private static void GenericAchievementStateStartsEmpty()
        {
            var program = new SpaceProgramState("Program", false);

            AssertTrue(
                !program.HasAchievement(PrototypeMilestones.ProbeOrbitId),
                "A new program should not have recorded achievements.");
            AssertEqual(-1.0, program.GetAchievementUniversalTime(PrototypeMilestones.ProbeOrbitId));
            AssertTrue(!program.HasAchievement("not-defined"), "Unknown milestone IDs should not appear achieved.");
            AssertEqual(-1.0, program.GetAchievementUniversalTime("not-defined"));
        }

        private static void GenericAchievementStateRecordsFirstTimestamp()
        {
            var program = new SpaceProgramState("Program", false);

            AssertTrue(
                program.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 1234.0),
                "The first milestone observation should be recorded.");
            AssertTrue(
                program.HasAchievement("PROBE-ORBIT"),
                "Milestone IDs should be matched case-insensitively.");
            AssertEqual(1234.0, program.GetAchievementUniversalTime(PrototypeMilestones.ProbeOrbitId));
        }

        private static void GenericAchievementStatePreservesFirstTimestamp()
        {
            var program = new SpaceProgramState("Program", false);
            program.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 1234.0);

            AssertTrue(
                !program.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 5678.0),
                "A repeated observation should not replace the original milestone time.");
            AssertEqual(1234.0, program.GetAchievementUniversalTime(PrototypeMilestones.ProbeOrbitId));
        }

        private static void GenericAchievementStateAcceptsArbitraryIds()
        {
            var program = new SpaceProgramState("Program", false);

            AssertTrue(
                program.RecordAchievement("future-duna-probe-orbit", 4321.0),
                "Program state should accept milestone IDs that were not hardcoded into the class.");
            AssertTrue(program.HasAchievement("future-duna-probe-orbit"), "Arbitrary milestone should be recorded.");
            AssertEqual(4321.0, program.GetAchievementUniversalTime("future-duna-probe-orbit"));
        }

        private static void GenericAchievementStateValidatesTimestamps()
        {
            var program = new SpaceProgramState("Program", false);

            AssertTrue(!program.RecordAchievement(null, 10.0), "Null milestone IDs should be ignored.");
            AssertTrue(!program.RecordAchievement(string.Empty, 10.0), "Empty milestone IDs should be ignored.");
            AssertTrue(!program.RecordAchievement("nan", double.NaN), "NaN achievement times should be ignored.");
            AssertTrue(
                !program.RecordAchievement("infinite", double.PositiveInfinity),
                "Infinite achievement times should be ignored.");
            AssertTrue(program.RecordAchievement("early", -10.0), "Finite negative times should be normalized.");
            AssertEqual(0.0, program.GetAchievementUniversalTime("early"));
        }

        private static void RivalMissionTargetIdsMapToDisplayNames()
        {
            IList<AchievementFundingProgramme> achievementProgrammes =
                PrototypeFundingCatalogue.CreateAchievementProgrammes();
            IList<FundingProgramme> fundingProgrammes =
                PrototypeFundingCatalogue.CreateSatelliteProgrammes();

            AssertEqual(
                "Probe Orbit",
                RivalSimulation.GetMissionTargetDisplayName(
                    PrototypeMilestones.ProbeOrbitId,
                    achievementProgrammes,
                    fundingProgrammes));
            AssertEqual(
                "Mun",
                RivalSimulation.GetMissionTargetDisplayName(
                    PrototypeFundingCatalogue.MunNetworkId,
                    achievementProgrammes,
                    fundingProgrammes));
            AssertEqual(
                null,
                RivalSimulation.GetMissionTargetDisplayName(
                    "not-a-target",
                    achievementProgrammes,
                    fundingProgrammes));
        }

        private static void RivalLaunchCostsMatchTargetType()
        {
            var rival = new SpaceProgramState("Rival", false);
            IList<AchievementFundingProgramme> achievementProgrammes =
                PrototypeFundingCatalogue.CreateAchievementProgrammes();
            IList<FundingProgramme> fundingProgrammes =
                PrototypeFundingCatalogue.CreateSatelliteProgrammes();

            rival.NextMissionTargetId = PrototypeMilestones.DirectedPower1Id;
            AssertEqual(
                2000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    rival,
                    achievementProgrammes,
                    fundingProgrammes));

            rival.NextMissionTargetId = PrototypeMilestones.Biome5Id;
            AssertEqual(
                6000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    rival,
                    achievementProgrammes,
                    fundingProgrammes));

            rival.NextMissionTargetId = PrototypeMilestones.ProbeOrbitId;
            AssertEqual(
                20000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    rival,
                    achievementProgrammes,
                    fundingProgrammes));

            rival.NextMissionTargetId = PrototypeMilestones.CrewedOrbitId;
            AssertEqual(
                40000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    rival,
                    achievementProgrammes,
                    fundingProgrammes));

            rival.NextMissionTargetId = PrototypeFundingCatalogue.MunNetworkId;
            AssertEqual(
                40000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    rival,
                    achievementProgrammes,
                    fundingProgrammes));
        }

        private static void RivalStarterCompletionDoesNotCreateSatellite()
        {
            SpaceProgramState player = new SpaceProgramState("Player", true);
            SpaceProgramState rival = new SpaceProgramState("Rival", false)
            {
                NextMissionTargetId = PrototypeMilestones.DirectedPower1Id,
                LaunchProgressPercent = 100
            };
            MilestoneDefinition milestone = PrototypeMilestones.FindById(
                PrototypeMilestones.DirectedPower1Id);
            var achievementProgrammes = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme(
                    milestone.Id,
                    milestone.Name,
                    milestone.ObjectiveDescription,
                    milestone.BaseRewardFunds)
            };
            achievementProgrammes[0].Offer();

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, rival },
                100.0,
                achievementProgrammes,
                new List<FundingProgramme>());

            AssertTrue(
                rival.HasAchievement(PrototypeMilestones.DirectedPower1Id),
                "Completed starter mission should record its achievement.");
            AssertEqual(0, rival.GetSatelliteCount("Kerbin"));
        }

        private static void RivalEtaDetectsUnaffordableMission()
        {
            var rival = new SpaceProgramState("Rival", false)
            {
                NextMissionTargetId = PrototypeMilestones.ProbeOrbitId,
                Funds = 0.0,
                NextPayoutFunds = 0.0
            };
            IList<AchievementFundingProgramme> achievementProgrammes =
                PrototypeFundingCatalogue.CreateAchievementProgrammes();
            IList<FundingProgramme> fundingProgrammes =
                PrototypeFundingCatalogue.CreateSatelliteProgrammes();

            int? estimatedDays = RivalSimulation.CalculateEstimatedLaunchDays(
                rival,
                0.0,
                90.0 * 21600.0,
                90.0 * 21600.0,
                achievementProgrammes,
                fundingProgrammes);
            AssertTrue(!estimatedDays.HasValue, "A mission with no current or future funds should have no ETA.");
        }

        private static void UnavailableRivalTargetIsAbandoned()
        {
            SpaceProgramState player = new SpaceProgramState("Player", true);
            SpaceProgramState aster = new SpaceProgramState("Aster", false)
            {
                NextMissionTargetId = PrototypeMilestones.MunProbeOrbitId,
                LaunchProgressPercent = 50
            };
            SpaceProgramState cobalt = new SpaceProgramState("Cobalt", false);
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

            AssertEqual(null, aster.NextMissionTargetId);
            AssertEqual(null, aster.NextLaunchBodyName);
            AssertEqual(0, aster.LaunchProgressPercent);
        }

        private static void InvalidRivalTargetIdIsAbandoned()
        {
            SpaceProgramState player = new SpaceProgramState("Player", true);
            SpaceProgramState aster = new SpaceProgramState("Aster", false)
            {
                NextMissionTargetId = "not-a-target",
                NextLaunchBodyName = "Mun",
                LaunchProgressPercent = 50
            };
            SpaceProgramState cobalt = new SpaceProgramState("Cobalt", false);

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, aster, cobalt },
                0.0,
                new List<AchievementFundingProgramme>(),
                new List<FundingProgramme>());

            AssertEqual(null, aster.NextMissionTargetId);
            AssertEqual(null, aster.NextLaunchBodyName);
            AssertEqual(0, aster.LaunchProgressPercent);
        }

        private static void RivalSelectsOnlyAvailableTarget()
        {
            SpaceProgramState player = new SpaceProgramState("Player", true);
            SpaceProgramState aster = new SpaceProgramState("Aster", false);
            aster.RecordAchievement(PrototypeMilestones.CrewedOrbitId, 1.0);
            SpaceProgramState cobalt = new SpaceProgramState("Cobalt", false);
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
                new List<SpaceProgramState> { player, aster, cobalt },
                1.0,
                achievementProgrammes,
                new List<FundingProgramme>());

            AssertEqual(PrototypeMilestones.MinmusCrewedOrbitId, aster.NextMissionTargetId);
            AssertEqual("Minmus Crewed Orbit", aster.NextLaunchBodyName);
        }

        private static void RivalCompletionUsesReplayTimestamp()
        {
            const double replayUniversalTime = 90.0 * 21600.0;
            SpaceProgramState player = new SpaceProgramState("Player", true);
            SpaceProgramState aster = new SpaceProgramState("Aster", false)
            {
                NextMissionTargetId = PrototypeMilestones.ProbeOrbitId,
                LaunchProgressPercent = 100
            };
            SpaceProgramState cobalt = new SpaceProgramState("Cobalt", false);
            MilestoneDefinition milestone = PrototypeMilestones.FindById(
                PrototypeMilestones.ProbeOrbitId);
            var achievementProgrammes = new List<AchievementFundingProgramme>
            {
                new AchievementFundingProgramme(
                    milestone.Id,
                    milestone.Name,
                    milestone.ObjectiveDescription,
                    100000.0)
            };
            achievementProgrammes[0].Offer();

            RivalSimulation.Refresh(
                new List<SpaceProgramState> { player, aster, cobalt },
                replayUniversalTime,
                achievementProgrammes,
                new List<FundingProgramme>());

            AssertTrue(
                aster.HasAchievement(PrototypeMilestones.ProbeOrbitId),
                "Completed rival mission should record its achievement.");
            AssertEqual(
                replayUniversalTime,
                aster.GetAchievementUniversalTime(PrototypeMilestones.ProbeOrbitId));
            AssertEqual(1, aster.GetSatelliteCount("Kerbin"));
        }

        private static void RaceProgressLegacySaveKeysRestoreGenericState()
        {
            var node = new ConfigNode();
            node.AddValue("playerProbeOrbit", true);
            node.AddValue("playerProbeOrbitUniversalTime", "1234");

            var loaded = new RaceProgressSaveState();
            loaded.Load(node);

            SpaceProgramState player = new SpaceProgramState("Player", true);
            FundingProgramme kerbin = Network("Kerbin", false);
            FundingProgramme mun = Network("Mun", false);
            FundingProgramme minmus = Network("Minmus", false);
            AchievementFundingProgramme[] achievements = AchievementProgrammes();

            loaded.ApplyTo(
                player,
                kerbin,
                mun,
                minmus,
                achievements[0],
                achievements[1],
                achievements[2],
                achievements[3],
                achievements[4],
                achievements[5]);

            AssertTrue(
                player.HasAchievement(PrototypeMilestones.ProbeOrbitId),
                "Existing player achievement save keys should restore generic milestone state.");
            AssertEqual(
                1234.0,
                player.GetAchievementUniversalTime(PrototypeMilestones.ProbeOrbitId));
        }

        private static void RaceProgressPersistenceRoundTrip()
        {
            SpaceProgramState sourcePlayer = new SpaceProgramState("Player", true);
            sourcePlayer.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 1234.0);
            sourcePlayer.RecordAchievement(PrototypeMilestones.CrewedOrbitId, 5678.0);
            FundingProgramme sourceKerbin = Network("Kerbin", true);
            FundingProgramme sourceMun = Network("Mun", true);
            FundingProgramme sourceMinmus = Network("Minmus", false);
            AchievementFundingProgramme[] sourceAchievements = AchievementProgrammes();
            sourceAchievements[0].Start();
            sourceAchievements[0].AdvancePayout();
            sourceAchievements[0].AdvancePayout();
            sourceAchievements[1].Start();

            var saved = new RaceProgressSaveState();
            saved.Capture(
                sourcePlayer,
                sourceKerbin,
                sourceMun,
                sourceMinmus,
                sourceAchievements[0],
                sourceAchievements[1],
                sourceAchievements[2],
                sourceAchievements[3],
                sourceAchievements[4],
                sourceAchievements[5]);

            var node = new ConfigNode();
            saved.Save(node);
            var loaded = new RaceProgressSaveState();
            loaded.Load(node);

            SpaceProgramState restoredPlayer = new SpaceProgramState("Player", true);
            FundingProgramme restoredKerbin = Network("Kerbin", false);
            FundingProgramme restoredMun = Network("Mun", false);
            FundingProgramme restoredMinmus = Network("Minmus", false);
            AchievementFundingProgramme[] restoredAchievements = AchievementProgrammes();
            loaded.ApplyTo(
                restoredPlayer,
                restoredKerbin,
                restoredMun,
                restoredMinmus,
                restoredAchievements[0],
                restoredAchievements[1],
                restoredAchievements[2],
                restoredAchievements[3],
                restoredAchievements[4],
                restoredAchievements[5]);

            AssertTrue(
                restoredPlayer.HasAchievement(PrototypeMilestones.ProbeOrbitId),
                "Probe Orbit should round trip through generic milestone state.");
            AssertEqual(
                1234.0,
                restoredPlayer.GetAchievementUniversalTime(PrototypeMilestones.ProbeOrbitId));
            AssertTrue(restoredMun.IsAvailable, "Mun network unlock should round trip.");
            AssertTrue(!restoredMinmus.IsAvailable, "Locked Minmus network should remain locked.");
            AssertEqual(2, restoredAchievements[0].PaymentsProcessed);
            AssertTrue(restoredAchievements[1].HasStarted, "Crewed contract start should round trip.");
        }

        private static void RivalPersistenceRoundTrip()
        {
            var source = new SpaceProgramState("Aster", false)
            {
                Funds = 75000.0,
                NextMissionTargetId = PrototypeMilestones.MunProbeOrbitId,
                NextLaunchBodyName = "Presentation text is not persisted",
                LaunchProgressPercent = 40,
                NextLaunchProgressCheckUniversalTime = 9000.0
            };
            source.RecordAchievement(PrototypeMilestones.ProbeOrbitId, 2000.0);
            source.SetSatelliteCount("Kerbin", 3);
            source.SetSatelliteCount("Mun", 1);

            var saved = new RivalProgramSaveState();
            saved.Capture(source);
            var node = new ConfigNode();
            saved.Save(node);
            var loaded = new RivalProgramSaveState();
            loaded.Load(node);

            var restored = new SpaceProgramState("Aster", false);
            loaded.ApplyTo(restored);

            AssertEqual(75000.0, restored.Funds);
            AssertEqual(3, restored.GetSatelliteCount("Kerbin"));
            AssertEqual(1, restored.GetSatelliteCount("Mun"));
            AssertTrue(
                restored.HasAchievement(PrototypeMilestones.ProbeOrbitId),
                "Rival achievement should round trip through generic milestone state.");
            AssertEqual(
                2000.0,
                restored.GetAchievementUniversalTime(PrototypeMilestones.ProbeOrbitId));
            AssertEqual(null, restored.NextLaunchBodyName);
            AssertEqual(PrototypeMilestones.MunProbeOrbitId, restored.NextMissionTargetId);

            IList<AchievementFundingProgramme> achievementProgrammes =
                PrototypeFundingCatalogue.CreateAchievementProgrammes();
            IList<FundingProgramme> fundingProgrammes =
                PrototypeFundingCatalogue.CreateSatelliteProgrammes();
            AssertEqual(
                40000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    restored,
                    achievementProgrammes,
                    fundingProgrammes));
            AssertEqual(40, restored.LaunchProgressPercent);
            AssertEqual(9000.0, restored.NextLaunchProgressCheckUniversalTime);
        }

        private static FundingProgramme Network(string bodyName, bool available)
        {
            return new FundingProgramme(
                bodyName.ToLowerInvariant(),
                bodyName,
                bodyName,
                5,
                100000.0,
                available,
                null);
        }

        private static AchievementFundingProgramme[] AchievementProgrammes()
        {
            return new[]
            {
                new AchievementFundingProgramme("probe", "Probe", "Probe", 100000.0),
                new AchievementFundingProgramme("crewed", "Crewed", "Crewed", 200000.0),
                new AchievementFundingProgramme("mun-probe", "Mun Probe", "Mun Probe", 200000.0),
                new AchievementFundingProgramme("minmus-probe", "Minmus Probe", "Minmus Probe", 200000.0),
                new AchievementFundingProgramme("mun-crewed", "Mun Crewed", "Mun Crewed", 300000.0),
                new AchievementFundingProgramme("minmus-crewed", "Minmus Crewed", "Minmus Crewed", 300000.0)
            };
        }

        private static void AssertMilestone(
            MilestoneDefinition milestone,
            string expectedId,
            string expectedName,
            string expectedBodyName,
            MilestoneCrewRequirement expectedCrewRequirement,
            params string[] expectedUnlockMilestoneIds)
        {
            AssertEqual(expectedId, milestone.Id);
            AssertEqual(expectedName, milestone.Name);
            AssertEqual(expectedBodyName, milestone.CelestialBodyName);
            AssertEqual(MilestoneSituation.Orbit, milestone.Situation);
            AssertEqual(expectedCrewRequirement, milestone.CrewRequirement);
            AssertAnyAgencyRule(milestone.UnlockRule, expectedUnlockMilestoneIds);
            AssertTrue(
                !string.IsNullOrEmpty(milestone.ObjectiveDescription),
                "Milestone objective description should not be empty.");
        }

        private static void AssertAnyAgencyRule(
            UnlockRuleDefinition rule,
            params string[] expectedMilestoneIds)
        {
            if (expectedMilestoneIds == null || expectedMilestoneIds.Length == 0)
            {
                AssertEqual(null, rule);
                return;
            }

            AssertTrue(rule != null, "Expected a milestone unlock rule.");

            // Probe Orbit uses four alternative AnyAgency achievements. Existing campaign rules use
            // one path whose multiple achievement conditions are all required.
            if (expectedMilestoneIds.Length == 4)
            {
                AssertEqual(4, rule.Paths.Count);
                for (int pathIndex = 0; pathIndex < expectedMilestoneIds.Length; pathIndex++)
                {
                    AssertTrue(rule.Paths[pathIndex] != null, "Expected a non-null alternative unlock path.");
                    AssertEqual(1, rule.Paths[pathIndex].Conditions.Count);
                    UnlockConditionDefinition condition = rule.Paths[pathIndex].Conditions[0];
                    AssertTrue(condition != null, "Expected a non-null unlock condition.");
                    AssertEqual(UnlockConditionType.Achievement, condition.ConditionType);
                    AssertEqual(UnlockProgramScope.AnyAgency, condition.ProgramScope);
                    AssertEqual(1, condition.RequiredProgramCount);
                    AssertEqual(expectedMilestoneIds[pathIndex], condition.MilestoneId);
                }
                return;
            }

            AssertEqual(1, rule.Paths.Count);
            AssertTrue(rule.Paths[0] != null, "Expected a non-null unlock path.");
            AssertEqual(expectedMilestoneIds.Length, rule.Paths[0].Conditions.Count);

            for (int conditionIndex = 0;
                conditionIndex < expectedMilestoneIds.Length;
                conditionIndex++)
            {
                UnlockConditionDefinition condition = rule.Paths[0].Conditions[conditionIndex];
                AssertTrue(condition != null, "Expected a non-null unlock condition.");
                AssertEqual(UnlockConditionType.Achievement, condition.ConditionType);
                AssertEqual(UnlockProgramScope.AnyAgency, condition.ProgramScope);
                AssertEqual(1, condition.RequiredProgramCount);
                AssertEqual(expectedMilestoneIds[conditionIndex], condition.MilestoneId);
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    "Expected '" + expected + "' but got '" + actual + "'.");
            }
        }
    }
}

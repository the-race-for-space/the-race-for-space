using System;
using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Persistence;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Rivals;
using TheRaceForSpace.Tests.Objectives;
using TheRaceForSpace.Tests.Persistence;
using TheRaceForSpace.Tests.Rivals;
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
            Run("ObjectiveCompletion payout declines and expires", AchievementPayoutDeclinesAndExpires);
            Run("ObjectiveCompletion restore normalizes lifecycle", AchievementRestoreNormalizesLifecycle);
            Run("ObjectiveCompletion funding stores unlock rule", AchievementFundingStoresStructuredPrerequisite);
            Run("Prototype funding catalogue matches current campaign", FundingContractCatalogueTests.CatalogueMatchesCurrentPrototype);
            Run("Prototype funding catalogue creates fresh state", FundingContractCatalogueTests.CatalogueCreatesFreshCampaignState);
            Run("Prototype objective definitions match current campaign", PrototypeObjectiveDefinitionsMatchCurrentCampaign);
            Run("Prototype objective ids are unique", PrototypeObjectiveIdsAreUnique);
            Run("Prototype objective lookup uses stable ids", PrototypeMilestoneLookupUsesStableIds);
            Run("Objective probe observation matches definition", MilestoneEvaluationTests.ProbeObservationMatchesDefinition);
            Run("Objective crewed observation matches definition", MilestoneEvaluationTests.CrewedObservationMatchesDefinition);
            Run("Objective rejects wrong body or situation", MilestoneEvaluationTests.WrongBodyOrSituationDoesNotMatch);
            Run("Objective arbitrary body uses same rule", MilestoneEvaluationTests.ArbitraryBodyDefinitionUsesSameRule);
            Run("Tracking snapshots update counts and objectives", OrbitalVesselTrackerTests.NormalizedSnapshotsUpdateCountsAndMilestones);
            Run("Tracking empty snapshots reset body counts", OrbitalVesselTrackerTests.EmptySnapshotsResetTrackedBodyCounts);
            Run("Tracking crewed probe keeps satellite classification", OrbitalVesselTrackerTests.CrewedProbeCountsAsSatelliteButNotProbeMilestone);
            Run("Tracking flexible unlock uses race state and time", OrbitalVesselTrackerTests.FlexibleUnlockRuleUsesRaceStateAndTime);
            Run("PreOrbit flight contracts and persistence", FlightContractTrackerTests.RunAll);
            Run("Surface impact eligibility", SurfaceImpactEvaluatorTests.RunAll);
            Run("Generic objectiveCompletion state starts empty", GenericAchievementStateStartsEmpty);
            Run("Generic objectiveCompletion state records first timestamp", GenericAchievementStateRecordsFirstTimestamp);
            Run("Generic objectiveCompletion state preserves first timestamp", GenericAchievementStatePreservesFirstTimestamp);
            Run("Generic objectiveCompletion state accepts arbitrary ids", GenericAchievementStateAcceptsArbitraryIds);
            Run("Generic objectiveCompletion state validates timestamps", GenericAchievementStateValidatesTimestamps);
            Run("Rival mission target ids map to display names", RivalMissionTargetIdsMapToDisplayNames);
            Run("Rival launch costs match target type", RivalLaunchCostsMatchTargetType);
            Run("Rival starter completion does not create satellite", RivalPreOrbitCompletionDoesNotCreateSatellite);
            Run("Rival ETA detects unaffordable mission", RivalEtaDetectsUnaffordableMission);
            Run("Unavailable rival target is abandoned", UnavailableRivalTargetIsAbandoned);
            Run("Invalid rival target id is abandoned", InvalidRivalTargetIdIsAbandoned);
            Run("Rival selects the only offered target", RivalSelectsOnlyAvailableTarget);
            Run("Rival completion uses replay timestamp", RivalCompletionUsesReplayTimestamp);
            Run("Rival collection selects offered objectiveCompletion", RivalSimulationCollectionTests.SelectsOnlyAvailableAchievementFromCollection);
            Run("Rival collection excludes locked objectiveCompletion", RivalSimulationCollectionTests.LockedAchievementIsExcludedFromCollection);
            Run("Rival satellite programme remains repeatable", RivalSimulationCollectionTests.SatelliteProgrammeRemainsRepeatable);
            Run("Rival completes arbitrary satellite programme", RivalSimulationCollectionTests.CompletesArbitrarySatelliteProgramme);
            Run("Rival collection cost uses programme body", RivalSimulationCollectionTests.CollectionCostUsesProgrammeBody);
            Run("Rival objectiveCompletion completion uses objective definition", RivalSimulationCollectionTests.AchievementCompletionUsesObjectiveDefinition);
            Run("Funding contracts persist arbitrary ids", CollectionPersistenceTests.FundingContractsRoundTripArbitraryIds);
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
            var programme = new SatelliteNetworkFundingContract("network", "Network", "Kerbin", 10, 200000.0);
            AssertEqual(60000.0, programme.CalculateCurrentPayout(3, 7));
        }

        private static void SatelliteFundingAfterSaturation()
        {
            var programme = new SatelliteNetworkFundingContract("network", "Network", "Kerbin", 10, 200000.0);
            AssertEqual(50000.0, programme.CalculateCurrentPayout(3, 12));
        }

        private static void SatelliteFundingStoresStructuredPrerequisite()
        {
            var programme = new SatelliteNetworkFundingContract(
                "mun-network",
                "Mun Network",
                "Mun",
                5,
                100000.0,
                false,
                "Display-only unlock text",
                UnlockRuleDefinition.AnyAgencyObjectiveCompletion(ObjectiveCatalogue.MunProbeOrbitId));

            AssertAnyAgencyRule(programme.UnlockRule, ObjectiveCatalogue.MunProbeOrbitId);
            AssertEqual("Display-only unlock text", programme.UnlockRequirement);
            AssertTrue(!programme.IsAvailable, "Structured unlock rules should not change initial availability.");

            var startAvailableProgramme = new SatelliteNetworkFundingContract(
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
            var programme = new ObjectiveFundingContract("probe", "Probe", "Orbit", 100000.0);
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
            var programme = new ObjectiveFundingContract("probe", "Probe", "Orbit", 100000.0);
            programme.RestoreState(false, 4);

            AssertTrue(programme.HasStarted, "Processed payments imply a started contract.");
            AssertEqual(4, programme.PaymentsProcessed);
            AssertEqual(60, programme.CurrentInterestPercent);
        }

        private static void AchievementFundingStoresStructuredPrerequisite()
        {
            var programme = new ObjectiveFundingContract(
                ObjectiveCatalogue.MunProbeOrbitId,
                "Mun Probe Orbit",
                "Orbit Mun",
                200000.0,
                "Display-only unlock text",
                UnlockRuleDefinition.AnyAgencyObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId));

            AssertAnyAgencyRule(programme.UnlockRule, ObjectiveCatalogue.ProbeOrbitId);
            AssertEqual("Display-only unlock text", programme.UnlockRequirement);

            var startAvailableProgramme = new ObjectiveFundingContract(
                "start-objectiveCompletion",
                "Start ObjectiveCompletion",
                "Objective",
                100000.0,
                "Display-only unlock text");
            AssertEqual(null, startAvailableProgramme.UnlockRule);
        }

        private static void PrototypeObjectiveDefinitionsMatchCurrentCampaign()
        {
            AssertEqual(32, ObjectiveCatalogue.All.Count);
            AssertEqual(20, ObjectiveCatalogue.PreOrbitContracts.Count);

            AssertMilestone(
                ObjectiveCatalogue.All[0],
                ObjectiveCatalogue.ProbeOrbitId,
                "Probe Orbit",
                "Kerbin",
                ObjectiveCrewRequirement.UncrewedProbe,
                ObjectiveCatalogue.DirectedPower5Id,
                ObjectiveCatalogue.Mass5Id,
                ObjectiveCatalogue.Control5Id,
                ObjectiveCatalogue.Biome5Id);
            AssertMilestone(
                ObjectiveCatalogue.All[1],
                ObjectiveCatalogue.CrewedOrbitId,
                "Crewed Orbit",
                "Kerbin",
                ObjectiveCrewRequirement.Crewed,
                ObjectiveCatalogue.ProbeOrbitId);
            AssertMilestone(
                ObjectiveCatalogue.All[2],
                ObjectiveCatalogue.MunProbeOrbitId,
                "Mun Probe Orbit",
                "Mun",
                ObjectiveCrewRequirement.UncrewedProbe,
                ObjectiveCatalogue.ProbeOrbitId);
            AssertMilestone(
                ObjectiveCatalogue.All[3],
                ObjectiveCatalogue.MinmusProbeOrbitId,
                "Minmus Probe Orbit",
                "Minmus",
                ObjectiveCrewRequirement.UncrewedProbe,
                ObjectiveCatalogue.ProbeOrbitId);
            AssertMilestone(
                ObjectiveCatalogue.All[4],
                ObjectiveCatalogue.DunaProbeOrbitId,
                "Duna Probe Orbit",
                "Duna",
                ObjectiveCrewRequirement.UncrewedProbe,
                ObjectiveCatalogue.MunProbeOrbitId,
                ObjectiveCatalogue.MinmusProbeOrbitId);
            AssertMilestone(
                ObjectiveCatalogue.All[5],
                ObjectiveCatalogue.MunCrewedOrbitId,
                "Mun Crewed Orbit",
                "Mun",
                ObjectiveCrewRequirement.Crewed,
                ObjectiveCatalogue.CrewedOrbitId);
            AssertMilestone(
                ObjectiveCatalogue.All[6],
                ObjectiveCatalogue.MinmusCrewedOrbitId,
                "Minmus Crewed Orbit",
                "Minmus",
                ObjectiveCrewRequirement.Crewed,
                ObjectiveCatalogue.CrewedOrbitId);
            AssertMilestone(
                ObjectiveCatalogue.All[7],
                ObjectiveCatalogue.DunaCrewedOrbitId,
                "Duna Crewed Orbit",
                "Duna",
                ObjectiveCrewRequirement.Crewed,
                ObjectiveCatalogue.MunCrewedOrbitId,
                ObjectiveCatalogue.MinmusCrewedOrbitId);
        }

        private static void PrototypeObjectiveIdsAreUnique()
        {
            var objectiveIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int milestoneIndex = 0; milestoneIndex < ObjectiveCatalogue.All.Count; milestoneIndex++)
            {
                ObjectiveDefinition objective = ObjectiveCatalogue.All[milestoneIndex];
                AssertTrue(
                    objectiveIds.Add(objective.Id),
                    "Duplicate objective id found: " + objective.Id);
            }

            for (int milestoneIndex = 0; milestoneIndex < ObjectiveCatalogue.PreOrbitContracts.Count; milestoneIndex++)
            {
                ObjectiveDefinition objective = ObjectiveCatalogue.PreOrbitContracts[milestoneIndex];
                AssertTrue(
                    objectiveIds.Add(objective.Id),
                    "Duplicate pre-orbit objective id found: " + objective.Id);
            }
        }

        private static void PrototypeMilestoneLookupUsesStableIds()
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById("MUN-PROBE-ORBIT");
            ObjectiveDefinition dunaMilestone = ObjectiveCatalogue.FindById("DUNA-PROBE-ORBIT");
            ObjectiveDefinition starterMilestone = ObjectiveCatalogue.FindById("DIRECTED-POWER-1");

            AssertTrue(objective != null, "Known objective IDs should resolve case-insensitively.");
            AssertEqual(ObjectiveCatalogue.MunProbeOrbitId, objective.Id);
            AssertAnyAgencyRule(objective.UnlockRule, ObjectiveCatalogue.ProbeOrbitId);
            AssertTrue(dunaMilestone != null, "Duna objective should resolve through the shared catalogue.");
            AssertEqual("Duna", dunaMilestone.CelestialBodyName);
            AssertAnyAgencyRule(
                dunaMilestone.UnlockRule,
                ObjectiveCatalogue.MunProbeOrbitId,
                ObjectiveCatalogue.MinmusProbeOrbitId);
            AssertTrue(starterMilestone != null, "PreOrbit objective IDs should share the stable lookup path.");
            AssertEqual(PreOrbitContractLine.DirectedPower, starterMilestone.PreOrbitLine);
            AssertEqual(null, ObjectiveCatalogue.FindById("not-a-objective"));
        }

        private static void GenericAchievementStateStartsEmpty()
        {
            var agency = new AgencyState("Program", false);

            AssertTrue(
                !agency.HasCompletedObjective(ObjectiveCatalogue.ProbeOrbitId),
                "A new agency should not have recorded achievements.");
            AssertEqual(-1.0, agency.GetObjectiveCompletionTime(ObjectiveCatalogue.ProbeOrbitId));
            AssertTrue(!agency.HasCompletedObjective("not-defined"), "Unknown objective IDs should not appear achieved.");
            AssertEqual(-1.0, agency.GetObjectiveCompletionTime("not-defined"));
        }

        private static void GenericAchievementStateRecordsFirstTimestamp()
        {
            var agency = new AgencyState("Program", false);

            AssertTrue(
                agency.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 1234.0),
                "The first objective observation should be recorded.");
            AssertTrue(
                agency.HasCompletedObjective("PROBE-ORBIT"),
                "Objective IDs should be matched case-insensitively.");
            AssertEqual(1234.0, agency.GetObjectiveCompletionTime(ObjectiveCatalogue.ProbeOrbitId));
        }

        private static void GenericAchievementStatePreservesFirstTimestamp()
        {
            var agency = new AgencyState("Program", false);
            agency.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 1234.0);

            AssertTrue(
                !agency.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 5678.0),
                "A repeated observation should not replace the original objective time.");
            AssertEqual(1234.0, agency.GetObjectiveCompletionTime(ObjectiveCatalogue.ProbeOrbitId));
        }

        private static void GenericAchievementStateAcceptsArbitraryIds()
        {
            var agency = new AgencyState("Program", false);

            AssertTrue(
                agency.RecordObjectiveCompletion("future-duna-probe-orbit", 4321.0),
                "Program state should accept objective IDs that were not hardcoded into the class.");
            AssertTrue(agency.HasCompletedObjective("future-duna-probe-orbit"), "Arbitrary objective should be recorded.");
            AssertEqual(4321.0, agency.GetObjectiveCompletionTime("future-duna-probe-orbit"));
        }

        private static void GenericAchievementStateValidatesTimestamps()
        {
            var agency = new AgencyState("Program", false);

            AssertTrue(!agency.RecordObjectiveCompletion(null, 10.0), "Null objective IDs should be ignored.");
            AssertTrue(!agency.RecordObjectiveCompletion(string.Empty, 10.0), "Empty objective IDs should be ignored.");
            AssertTrue(!agency.RecordObjectiveCompletion("nan", double.NaN), "NaN objectiveCompletion times should be ignored.");
            AssertTrue(
                !agency.RecordObjectiveCompletion("infinite", double.PositiveInfinity),
                "Infinite objectiveCompletion times should be ignored.");
            AssertTrue(agency.RecordObjectiveCompletion("early", -10.0), "Finite negative times should be normalized.");
            AssertEqual(0.0, agency.GetObjectiveCompletionTime("early"));
        }

        private static void RivalMissionTargetIdsMapToDisplayNames()
        {
            IList<ObjectiveFundingContract> achievementProgrammes =
                FundingContractCatalogue.CreateAchievementProgrammes();
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts =
                FundingContractCatalogue.CreateSatelliteProgrammes();

            AssertEqual(
                "Probe Orbit",
                RivalSimulation.GetMissionTargetDisplayName(
                    ObjectiveCatalogue.ProbeOrbitId,
                    achievementProgrammes,
                    satelliteNetworkFundingContracts));
            AssertEqual(
                "Mun",
                RivalSimulation.GetMissionTargetDisplayName(
                    FundingContractCatalogue.MunNetworkId,
                    achievementProgrammes,
                    satelliteNetworkFundingContracts));
            AssertEqual(
                null,
                RivalSimulation.GetMissionTargetDisplayName(
                    "not-a-target",
                    achievementProgrammes,
                    satelliteNetworkFundingContracts));
        }

        private static void RivalLaunchCostsMatchTargetType()
        {
            var rival = new AgencyState("Rival", false);
            IList<ObjectiveFundingContract> achievementProgrammes =
                FundingContractCatalogue.CreateAchievementProgrammes();
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts =
                FundingContractCatalogue.CreateSatelliteProgrammes();

            rival.NextMissionTargetId = ObjectiveCatalogue.DirectedPower1Id;
            AssertEqual(
                4000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    rival,
                    achievementProgrammes,
                    satelliteNetworkFundingContracts));

            rival.NextMissionTargetId = ObjectiveCatalogue.Biome5Id;
            AssertEqual(
                12000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    rival,
                    achievementProgrammes,
                    satelliteNetworkFundingContracts));

            rival.NextMissionTargetId = ObjectiveCatalogue.ProbeOrbitId;
            AssertEqual(
                20000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    rival,
                    achievementProgrammes,
                    satelliteNetworkFundingContracts));

            rival.NextMissionTargetId = ObjectiveCatalogue.CrewedOrbitId;
            AssertEqual(
                40000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    rival,
                    achievementProgrammes,
                    satelliteNetworkFundingContracts));

            rival.NextMissionTargetId = FundingContractCatalogue.MunNetworkId;
            AssertEqual(
                40000.0,
                RivalSimulation.CalculateLaunchProgressCost(
                    rival,
                    achievementProgrammes,
                    satelliteNetworkFundingContracts));
        }

        private static void RivalPreOrbitCompletionDoesNotCreateSatellite()
        {
            AgencyState player = new AgencyState("Player", true);
            AgencyState rival = new AgencyState("Rival", false)
            {
                NextMissionTargetId = ObjectiveCatalogue.DirectedPower1Id,
                MissionProgressPercent = 100
            };
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(
                ObjectiveCatalogue.DirectedPower1Id);
            var achievementProgrammes = new List<ObjectiveFundingContract>
            {
                new ObjectiveFundingContract(
                    objective.Id,
                    objective.Name,
                    objective.ObjectiveDescription,
                    objective.BaseRewardFunds)
            };
            achievementProgrammes[0].Offer();

            RivalSimulation.Refresh(
                new List<AgencyState> { player, rival },
                100.0,
                achievementProgrammes,
                new List<SatelliteNetworkFundingContract>());

            AssertTrue(
                rival.HasCompletedObjective(ObjectiveCatalogue.DirectedPower1Id),
                "Completed starter mission should record its objectiveCompletion.");
            AssertEqual(0, rival.GetSatelliteCount("Kerbin"));
        }

        private static void RivalEtaDetectsUnaffordableMission()
        {
            var rival = new AgencyState("Rival", false)
            {
                NextMissionTargetId = ObjectiveCatalogue.ProbeOrbitId,
                Funds = 0.0,
                NextPayoutFunds = 0.0
            };
            IList<ObjectiveFundingContract> achievementProgrammes =
                FundingContractCatalogue.CreateAchievementProgrammes();
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts =
                FundingContractCatalogue.CreateSatelliteProgrammes();

            int? estimatedDays = RivalSimulation.CalculateEstimatedLaunchDays(
                rival,
                0.0,
                90.0 * 21600.0,
                90.0 * 21600.0,
                achievementProgrammes,
                satelliteNetworkFundingContracts);
            AssertTrue(!estimatedDays.HasValue, "A mission with no current or future funds should have no ETA.");
        }

        private static void UnavailableRivalTargetIsAbandoned()
        {
            AgencyState player = new AgencyState("Player", true);
            AgencyState aster = new AgencyState("Aster", false)
            {
                NextMissionTargetId = ObjectiveCatalogue.MunProbeOrbitId,
                MissionProgressPercent = 50
            };
            AgencyState cobalt = new AgencyState("Cobalt", false);
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

            AssertEqual(null, aster.NextMissionTargetId);
            AssertEqual(null, aster.NextMissionDisplayName);
            AssertEqual(0, aster.MissionProgressPercent);
        }

        private static void InvalidRivalTargetIdIsAbandoned()
        {
            AgencyState player = new AgencyState("Player", true);
            AgencyState aster = new AgencyState("Aster", false)
            {
                NextMissionTargetId = "not-a-target",
                NextMissionDisplayName = "Mun",
                MissionProgressPercent = 50
            };
            AgencyState cobalt = new AgencyState("Cobalt", false);

            RivalSimulation.Refresh(
                new List<AgencyState> { player, aster, cobalt },
                0.0,
                new List<ObjectiveFundingContract>(),
                new List<SatelliteNetworkFundingContract>());

            AssertEqual(null, aster.NextMissionTargetId);
            AssertEqual(null, aster.NextMissionDisplayName);
            AssertEqual(0, aster.MissionProgressPercent);
        }

        private static void RivalSelectsOnlyAvailableTarget()
        {
            AgencyState player = new AgencyState("Player", true);
            AgencyState aster = new AgencyState("Aster", false);
            aster.RecordObjectiveCompletion(ObjectiveCatalogue.CrewedOrbitId, 1.0);
            AgencyState cobalt = new AgencyState("Cobalt", false);
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
                new List<AgencyState> { player, aster, cobalt },
                1.0,
                achievementProgrammes,
                new List<SatelliteNetworkFundingContract>());

            AssertEqual(ObjectiveCatalogue.MinmusCrewedOrbitId, aster.NextMissionTargetId);
            AssertEqual("Minmus Crewed Orbit", aster.NextMissionDisplayName);
        }

        private static void RivalCompletionUsesReplayTimestamp()
        {
            const double replayUniversalTime = 90.0 * 21600.0;
            AgencyState player = new AgencyState("Player", true);
            AgencyState aster = new AgencyState("Aster", false)
            {
                NextMissionTargetId = ObjectiveCatalogue.ProbeOrbitId,
                MissionProgressPercent = 100
            };
            AgencyState cobalt = new AgencyState("Cobalt", false);
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(
                ObjectiveCatalogue.ProbeOrbitId);
            var achievementProgrammes = new List<ObjectiveFundingContract>
            {
                new ObjectiveFundingContract(
                    objective.Id,
                    objective.Name,
                    objective.ObjectiveDescription,
                    100000.0)
            };
            achievementProgrammes[0].Offer();

            RivalSimulation.Refresh(
                new List<AgencyState> { player, aster, cobalt },
                replayUniversalTime,
                achievementProgrammes,
                new List<SatelliteNetworkFundingContract>());

            AssertTrue(
                aster.HasCompletedObjective(ObjectiveCatalogue.ProbeOrbitId),
                "Completed rival mission should record its objectiveCompletion.");
            AssertEqual(
                replayUniversalTime,
                aster.GetObjectiveCompletionTime(ObjectiveCatalogue.ProbeOrbitId));
            AssertEqual(1, aster.GetSatelliteCount("Kerbin"));
        }

        private static void AssertMilestone(
            ObjectiveDefinition objective,
            string expectedId,
            string expectedName,
            string expectedBodyName,
            ObjectiveCrewRequirement expectedCrewRequirement,
            params string[] expectedUnlockObjectiveIds)
        {
            AssertEqual(expectedId, objective.Id);
            AssertEqual(expectedName, objective.Name);
            AssertEqual(expectedBodyName, objective.CelestialBodyName);
            AssertEqual(ObjectiveSituation.Orbit, objective.Situation);
            AssertEqual(expectedCrewRequirement, objective.CrewRequirement);
            AssertAnyAgencyRule(objective.UnlockRule, expectedUnlockObjectiveIds);
            AssertTrue(
                !string.IsNullOrEmpty(objective.ObjectiveDescription),
                "Objective objective description should not be empty.");
        }

        private static void AssertAnyAgencyRule(
            UnlockRuleDefinition rule,
            params string[] expectedObjectiveIds)
        {
            if (expectedObjectiveIds == null || expectedObjectiveIds.Length == 0)
            {
                AssertEqual(null, rule);
                return;
            }

            AssertTrue(rule != null, "Expected a objective unlock rule.");

            // Probe Orbit uses four alternative AnyAgency achievements. Existing campaign rules use
            // one path whose multiple objectiveCompletion conditions are all required.
            if (expectedObjectiveIds.Length == 4)
            {
                AssertEqual(4, rule.Paths.Count);
                for (int pathIndex = 0; pathIndex < expectedObjectiveIds.Length; pathIndex++)
                {
                    AssertTrue(rule.Paths[pathIndex] != null, "Expected a non-null alternative unlock path.");
                    AssertEqual(1, rule.Paths[pathIndex].Conditions.Count);
                    UnlockConditionDefinition condition = rule.Paths[pathIndex].Conditions[0];
                    AssertTrue(condition != null, "Expected a non-null unlock condition.");
                    AssertEqual(UnlockConditionType.ObjectiveCompletion, condition.ConditionType);
                    AssertEqual(UnlockAgencyScope.AnyAgency, condition.AgencyScope);
                    AssertEqual(1, condition.RequiredAgencyCount);
                    AssertEqual(expectedObjectiveIds[pathIndex], condition.ObjectiveId);
                }
                return;
            }

            AssertEqual(1, rule.Paths.Count);
            AssertTrue(rule.Paths[0] != null, "Expected a non-null unlock path.");
            AssertEqual(expectedObjectiveIds.Length, rule.Paths[0].Conditions.Count);

            for (int conditionIndex = 0;
                conditionIndex < expectedObjectiveIds.Length;
                conditionIndex++)
            {
                UnlockConditionDefinition condition = rule.Paths[0].Conditions[conditionIndex];
                AssertTrue(condition != null, "Expected a non-null unlock condition.");
                AssertEqual(UnlockConditionType.ObjectiveCompletion, condition.ConditionType);
                AssertEqual(UnlockAgencyScope.AnyAgency, condition.AgencyScope);
                AssertEqual(1, condition.RequiredAgencyCount);
                AssertEqual(expectedObjectiveIds[conditionIndex], condition.ObjectiveId);
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

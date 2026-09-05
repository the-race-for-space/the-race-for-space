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
            Run("ObjectiveCompletion payout declines and expires", ObjectiveFundingPayoutDeclinesAndExpires);
            Run("ObjectiveCompletion restore normalizes lifecycle", ObjectiveFundingRestoreNormalizesLifecycle);
            Run("ObjectiveCompletion funding stores unlock rule", ObjectiveFundingStoresStructuredPrerequisite);
            Run("Prototype funding catalogue matches current campaign", FundingContractCatalogueTests.CatalogueMatchesCurrentPrototype);
            Run("Prototype funding catalogue creates fresh state", FundingContractCatalogueTests.CatalogueCreatesFreshCampaignState);
            Run("Prototype objective definitions match current campaign", PrototypeObjectiveDefinitionsMatchCurrentCampaign);
            Run("Prototype objective ids are unique", PrototypeObjectiveIdsAreUnique);
            Run("Prototype objective lookup uses stable ids", ObjectiveLookupUsesStableIds);
            Run("Objective probe observation matches definition", ObjectiveEvaluationTests.ProbeObservationMatchesDefinition);
            Run("Objective crewed observation matches definition", ObjectiveEvaluationTests.CrewedObservationMatchesDefinition);
            Run("Objective rejects wrong body or situation", ObjectiveEvaluationTests.WrongBodyOrSituationDoesNotMatch);
            Run("Objective arbitrary body uses same rule", ObjectiveEvaluationTests.ArbitraryBodyDefinitionUsesSameRule);
            Run("Tracking snapshots update counts and objectives", OrbitalVesselTrackerTests.NormalizedSnapshotsUpdateCountsAndObjectives);
            Run("Tracking empty snapshots reset body counts", OrbitalVesselTrackerTests.EmptySnapshotsResetTrackedBodyCounts);
            Run("Tracking crewed probe keeps satellite classification", OrbitalVesselTrackerTests.CrewedProbeCountsAsSatelliteButNotProbeObjective);
            Run("Tracking flexible unlock uses campaign state and time", OrbitalVesselTrackerTests.FlexibleUnlockRuleUsesCampaignStateAndTime);
            Run("PreOrbit flight contracts and persistence", FlightContractTrackerTests.RunAll);
            Run("Surface impact eligibility", SurfaceImpactEvaluatorTests.RunAll);
            Run("Generic objective completion state starts empty", GenericObjectiveCompletionStateStartsEmpty);
            Run("Generic objective completion state records first timestamp", GenericObjectiveCompletionStateRecordsFirstTimestamp);
            Run("Generic objective completion state preserves first timestamp", GenericObjectiveCompletionStatePreservesFirstTimestamp);
            Run("Generic objective completion state accepts arbitrary ids", GenericObjectiveCompletionStateAcceptsArbitraryIds);
            Run("Generic objective completion state validates timestamps", GenericObjectiveCompletionStateValidatesTimestamps);
            Run("Rival mission target ids map to display names", RivalMissionTargetIdsMapToDisplayNames);
            Run("Rival launch costs match target type", RivalLaunchCostsMatchTargetType);
            Run("Rival pre-orbit completion does not create satellite", RivalPreOrbitCompletionDoesNotCreateSatellite);
            Run("Rival ETA detects unaffordable mission", RivalEtaDetectsUnaffordableMission);
            Run("Unavailable rival target is abandoned", UnavailableRivalTargetIsAbandoned);
            Run("Invalid rival target id is abandoned", InvalidRivalTargetIdIsAbandoned);
            Run("Rival selects the only offered target", RivalSelectsOnlyAvailableTarget);
            Run("Rival completion uses replay timestamp", RivalCompletionUsesReplayTimestamp);
            Run("Rival collection selects offered objective completion", RivalSimulationCollectionTests.SelectsOnlyAvailableObjectiveFromCollection);
            Run("Rival collection excludes locked objective completion", RivalSimulationCollectionTests.LockedObjectiveIsExcludedFromCollection);
            Run("Rival satellite contract remains repeatable", RivalSimulationCollectionTests.SatelliteNetworkContractRemainsRepeatable);
            Run("Rival completes arbitrary satellite contract", RivalSimulationCollectionTests.CompletesArbitrarySatelliteNetworkContract);
            Run("Rival collection cost uses contract body", RivalSimulationCollectionTests.CollectionCostUsesContractBody);
            Run("Rival objective completion uses objective definition", RivalSimulationCollectionTests.ObjectiveCompletionUsesObjectiveDefinition);
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
            var contract = new SatelliteNetworkFundingContract("network", "Network", "Kerbin", 10, 200000.0);
            AssertEqual(60000.0, contract.CalculateCurrentPayout(3, 7));
        }

        private static void SatelliteFundingAfterSaturation()
        {
            var contract = new SatelliteNetworkFundingContract("network", "Network", "Kerbin", 10, 200000.0);
            AssertEqual(50000.0, contract.CalculateCurrentPayout(3, 12));
        }

        private static void SatelliteFundingStoresStructuredPrerequisite()
        {
            var contract = new SatelliteNetworkFundingContract(
                "mun-network",
                "Mun Network",
                "Mun",
                5,
                100000.0,
                false,
                "Display-only unlock text",
                UnlockRuleDefinition.AnyAgencyObjectiveCompletion(ObjectiveCatalogue.MunProbeOrbitId));

            AssertAnyAgencyRule(contract.UnlockRule, ObjectiveCatalogue.MunProbeOrbitId);
            AssertEqual("Display-only unlock text", contract.UnlockRequirement);
            AssertTrue(!contract.IsAvailable, "Structured unlock rules should not change initial availability.");

            var startAvailableContract = new SatelliteNetworkFundingContract(
                "start-network",
                "Start Network",
                "Kerbin",
                5,
                100000.0,
                false,
                "Display-only unlock text");
            AssertEqual(null, startAvailableContract.UnlockRule);
        }

        private static void ObjectiveFundingPayoutDeclinesAndExpires()
        {
            var contract = new ObjectiveFundingContract("probe", "Probe", "Orbit", 100000.0);
            contract.Start();

            AssertEqual(50000.0, contract.CalculateCurrentPayout(true, 2));
            contract.AdvancePayout();
            AssertEqual(90, contract.CurrentInterestPercent);
            AssertEqual(45000.0, contract.CalculateCurrentPayout(true, 2));

            for (int payment = 1; payment < 10; payment++)
            {
                contract.AdvancePayout();
            }

            AssertTrue(contract.IsExpired, "Contract should expire after ten payments.");
            AssertEqual(0.0, contract.CalculateCurrentPayout(true, 1));
        }

        private static void ObjectiveFundingRestoreNormalizesLifecycle()
        {
            var contract = new ObjectiveFundingContract("probe", "Probe", "Orbit", 100000.0);
            contract.RestoreState(false, 4);

            AssertTrue(contract.HasStarted, "Processed payments imply a started contract.");
            AssertEqual(4, contract.PaymentsProcessed);
            AssertEqual(60, contract.CurrentInterestPercent);
        }

        private static void ObjectiveFundingStoresStructuredPrerequisite()
        {
            var contract = new ObjectiveFundingContract(
                ObjectiveCatalogue.MunProbeOrbitId,
                "Mun Probe Orbit",
                "Orbit Mun",
                200000.0,
                "Display-only unlock text",
                UnlockRuleDefinition.AnyAgencyObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId));

            AssertAnyAgencyRule(contract.UnlockRule, ObjectiveCatalogue.ProbeOrbitId);
            AssertEqual("Display-only unlock text", contract.UnlockRequirement);

            var startAvailableContract = new ObjectiveFundingContract(
                "start-objective-completion",
                "Start ObjectiveCompletion",
                "Objective",
                100000.0,
                "Display-only unlock text");
            AssertEqual(null, startAvailableContract.UnlockRule);
        }

        private static void PrototypeObjectiveDefinitionsMatchCurrentCampaign()
        {
            AssertEqual(32, ObjectiveCatalogue.All.Count);
            AssertEqual(20, ObjectiveCatalogue.PreOrbitContracts.Count);

            AssertObjective(
                ObjectiveCatalogue.All[0],
                ObjectiveCatalogue.ProbeOrbitId,
                "Probe Orbit",
                "Kerbin",
                ObjectiveCrewRequirement.UncrewedProbe,
                ObjectiveCatalogue.DirectedPower5Id,
                ObjectiveCatalogue.Mass5Id,
                ObjectiveCatalogue.Control5Id,
                ObjectiveCatalogue.Biome5Id);
            AssertObjective(
                ObjectiveCatalogue.All[1],
                ObjectiveCatalogue.CrewedOrbitId,
                "Crewed Orbit",
                "Kerbin",
                ObjectiveCrewRequirement.Crewed,
                ObjectiveCatalogue.ProbeOrbitId);
            AssertObjective(
                ObjectiveCatalogue.All[2],
                ObjectiveCatalogue.MunProbeOrbitId,
                "Mun Probe Orbit",
                "Mun",
                ObjectiveCrewRequirement.UncrewedProbe,
                ObjectiveCatalogue.ProbeOrbitId);
            AssertObjective(
                ObjectiveCatalogue.All[3],
                ObjectiveCatalogue.MinmusProbeOrbitId,
                "Minmus Probe Orbit",
                "Minmus",
                ObjectiveCrewRequirement.UncrewedProbe,
                ObjectiveCatalogue.ProbeOrbitId);
            AssertObjective(
                ObjectiveCatalogue.All[4],
                ObjectiveCatalogue.DunaProbeOrbitId,
                "Duna Probe Orbit",
                "Duna",
                ObjectiveCrewRequirement.UncrewedProbe,
                ObjectiveCatalogue.MunProbeOrbitId,
                ObjectiveCatalogue.MinmusProbeOrbitId);
            AssertObjective(
                ObjectiveCatalogue.All[5],
                ObjectiveCatalogue.MunCrewedOrbitId,
                "Mun Crewed Orbit",
                "Mun",
                ObjectiveCrewRequirement.Crewed,
                ObjectiveCatalogue.CrewedOrbitId);
            AssertObjective(
                ObjectiveCatalogue.All[6],
                ObjectiveCatalogue.MinmusCrewedOrbitId,
                "Minmus Crewed Orbit",
                "Minmus",
                ObjectiveCrewRequirement.Crewed,
                ObjectiveCatalogue.CrewedOrbitId);
            AssertObjective(
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
            for (int objectiveIndex = 0; objectiveIndex < ObjectiveCatalogue.All.Count; objectiveIndex++)
            {
                ObjectiveDefinition objective = ObjectiveCatalogue.All[objectiveIndex];
                AssertTrue(
                    objectiveIds.Add(objective.Id),
                    "Duplicate objective id found: " + objective.Id);
            }

            for (int objectiveIndex = 0; objectiveIndex < ObjectiveCatalogue.PreOrbitContracts.Count; objectiveIndex++)
            {
                ObjectiveDefinition objective = ObjectiveCatalogue.PreOrbitContracts[objectiveIndex];
                AssertTrue(
                    objectiveIds.Add(objective.Id),
                    "Duplicate pre-orbit objective id found: " + objective.Id);
            }
        }

        private static void ObjectiveLookupUsesStableIds()
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById("MUN-PROBE-ORBIT");
            ObjectiveDefinition dunaObjective = ObjectiveCatalogue.FindById("DUNA-PROBE-ORBIT");
            ObjectiveDefinition preOrbitObjective = ObjectiveCatalogue.FindById("DIRECTED-POWER-1");

            AssertTrue(objective != null, "Known objective IDs should resolve case-insensitively.");
            AssertEqual(ObjectiveCatalogue.MunProbeOrbitId, objective.Id);
            AssertAnyAgencyRule(objective.UnlockRule, ObjectiveCatalogue.ProbeOrbitId);
            AssertTrue(dunaObjective != null, "Duna objective should resolve through the shared catalogue.");
            AssertEqual("Duna", dunaObjective.CelestialBodyName);
            AssertAnyAgencyRule(
                dunaObjective.UnlockRule,
                ObjectiveCatalogue.MunProbeOrbitId,
                ObjectiveCatalogue.MinmusProbeOrbitId);
            AssertTrue(preOrbitObjective != null, "PreOrbit objective IDs should share the stable lookup path.");
            AssertEqual(PreOrbitContractLine.DirectedPower, preOrbitObjective.PreOrbitLine);
            AssertEqual(null, ObjectiveCatalogue.FindById("not-a-objective"));
        }

        private static void GenericObjectiveCompletionStateStartsEmpty()
        {
            var agency = new AgencyState("Program", false);

            AssertTrue(
                !agency.HasCompletedObjective(ObjectiveCatalogue.ProbeOrbitId),
                "A new agency should not have recorded objectives.");
            AssertEqual(-1.0, agency.GetObjectiveCompletionTime(ObjectiveCatalogue.ProbeOrbitId));
            AssertTrue(!agency.HasCompletedObjective("not-defined"), "Unknown objective IDs should not appear achieved.");
            AssertEqual(-1.0, agency.GetObjectiveCompletionTime("not-defined"));
        }

        private static void GenericObjectiveCompletionStateRecordsFirstTimestamp()
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

        private static void GenericObjectiveCompletionStatePreservesFirstTimestamp()
        {
            var agency = new AgencyState("Program", false);
            agency.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 1234.0);

            AssertTrue(
                !agency.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 5678.0),
                "A repeated observation should not replace the original objective time.");
            AssertEqual(1234.0, agency.GetObjectiveCompletionTime(ObjectiveCatalogue.ProbeOrbitId));
        }

        private static void GenericObjectiveCompletionStateAcceptsArbitraryIds()
        {
            var agency = new AgencyState("Program", false);

            AssertTrue(
                agency.RecordObjectiveCompletion("future-duna-probe-orbit", 4321.0),
                "Program state should accept objective IDs that were not hardcoded into the class.");
            AssertTrue(agency.HasCompletedObjective("future-duna-probe-orbit"), "Arbitrary objective should be recorded.");
            AssertEqual(4321.0, agency.GetObjectiveCompletionTime("future-duna-probe-orbit"));
        }

        private static void GenericObjectiveCompletionStateValidatesTimestamps()
        {
            var agency = new AgencyState("Program", false);

            AssertTrue(!agency.RecordObjectiveCompletion(null, 10.0), "Null objective IDs should be ignored.");
            AssertTrue(!agency.RecordObjectiveCompletion(string.Empty, 10.0), "Empty objective IDs should be ignored.");
            AssertTrue(!agency.RecordObjectiveCompletion("nan", double.NaN), "NaN objective completion times should be ignored.");
            AssertTrue(
                !agency.RecordObjectiveCompletion("infinite", double.PositiveInfinity),
                "Infinite objective completion times should be ignored.");
            AssertTrue(agency.RecordObjectiveCompletion("early", -10.0), "Finite negative times should be normalized.");
            AssertEqual(0.0, agency.GetObjectiveCompletionTime("early"));
        }

        private static void RivalMissionTargetIdsMapToDisplayNames()
        {
            IList<ObjectiveFundingContract> objectiveFundingContracts =
                FundingContractCatalogue.CreateObjectiveFundingContracts();
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts =
                FundingContractCatalogue.CreateSatelliteNetworkFundingContracts();

            AssertEqual(
                "Probe Orbit",
                RivalSimulation.GetMissionTargetDisplayName(
                    ObjectiveCatalogue.ProbeOrbitId,
                    objectiveFundingContracts,
                    satelliteNetworkFundingContracts));
            AssertEqual(
                "Mun",
                RivalSimulation.GetMissionTargetDisplayName(
                    FundingContractCatalogue.MunNetworkId,
                    objectiveFundingContracts,
                    satelliteNetworkFundingContracts));
            AssertEqual(
                null,
                RivalSimulation.GetMissionTargetDisplayName(
                    "not-a-target",
                    objectiveFundingContracts,
                    satelliteNetworkFundingContracts));
        }

        private static void RivalLaunchCostsMatchTargetType()
        {
            var rival = new AgencyState("Rival", false);
            IList<ObjectiveFundingContract> objectiveFundingContracts =
                FundingContractCatalogue.CreateObjectiveFundingContracts();
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts =
                FundingContractCatalogue.CreateSatelliteNetworkFundingContracts();

            rival.NextMissionTargetId = ObjectiveCatalogue.DirectedPower1Id;
            AssertEqual(
                4000.0,
                RivalSimulation.CalculateMissionProgressCost(
                    rival,
                    objectiveFundingContracts,
                    satelliteNetworkFundingContracts));

            rival.NextMissionTargetId = ObjectiveCatalogue.Biome5Id;
            AssertEqual(
                12000.0,
                RivalSimulation.CalculateMissionProgressCost(
                    rival,
                    objectiveFundingContracts,
                    satelliteNetworkFundingContracts));

            rival.NextMissionTargetId = ObjectiveCatalogue.ProbeOrbitId;
            AssertEqual(
                20000.0,
                RivalSimulation.CalculateMissionProgressCost(
                    rival,
                    objectiveFundingContracts,
                    satelliteNetworkFundingContracts));

            rival.NextMissionTargetId = ObjectiveCatalogue.CrewedOrbitId;
            AssertEqual(
                40000.0,
                RivalSimulation.CalculateMissionProgressCost(
                    rival,
                    objectiveFundingContracts,
                    satelliteNetworkFundingContracts));

            rival.NextMissionTargetId = FundingContractCatalogue.MunNetworkId;
            AssertEqual(
                40000.0,
                RivalSimulation.CalculateMissionProgressCost(
                    rival,
                    objectiveFundingContracts,
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
            var objectiveFundingContracts = new List<ObjectiveFundingContract>
            {
                new ObjectiveFundingContract(
                    objective.Id,
                    objective.Name,
                    objective.ObjectiveDescription,
                    objective.BaseRewardFunds)
            };
            objectiveFundingContracts[0].Offer();

            RivalSimulation.Refresh(
                new List<AgencyState> { player, rival },
                100.0,
                objectiveFundingContracts,
                new List<SatelliteNetworkFundingContract>());

            AssertTrue(
                rival.HasCompletedObjective(ObjectiveCatalogue.DirectedPower1Id),
                "Completed pre-orbit mission should record its objective completion.");
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
            IList<ObjectiveFundingContract> objectiveFundingContracts =
                FundingContractCatalogue.CreateObjectiveFundingContracts();
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts =
                FundingContractCatalogue.CreateSatelliteNetworkFundingContracts();

            int? estimatedDays = RivalSimulation.CalculateEstimatedLaunchDays(
                rival,
                0.0,
                90.0 * 21600.0,
                90.0 * 21600.0,
                objectiveFundingContracts,
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
            var objectiveFundingContracts = new List<ObjectiveFundingContract>
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
                objectiveFundingContracts,
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
            var objectiveFundingContracts = new List<ObjectiveFundingContract>
            {
                new ObjectiveFundingContract(
                    objective.Id,
                    objective.Name,
                    objective.ObjectiveDescription,
                    300000.0,
                    "Display text",
                    objective.UnlockRule)
            };
            objectiveFundingContracts[0].Offer();

            RivalSimulation.Refresh(
                new List<AgencyState> { player, aster, cobalt },
                1.0,
                objectiveFundingContracts,
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
            var objectiveFundingContracts = new List<ObjectiveFundingContract>
            {
                new ObjectiveFundingContract(
                    objective.Id,
                    objective.Name,
                    objective.ObjectiveDescription,
                    100000.0)
            };
            objectiveFundingContracts[0].Offer();

            RivalSimulation.Refresh(
                new List<AgencyState> { player, aster, cobalt },
                replayUniversalTime,
                objectiveFundingContracts,
                new List<SatelliteNetworkFundingContract>());

            AssertTrue(
                aster.HasCompletedObjective(ObjectiveCatalogue.ProbeOrbitId),
                "Completed rival mission should record its objective completion.");
            AssertEqual(
                replayUniversalTime,
                aster.GetObjectiveCompletionTime(ObjectiveCatalogue.ProbeOrbitId));
            AssertEqual(1, aster.GetSatelliteCount("Kerbin"));
        }

        private static void AssertObjective(
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

            // Probe Orbit uses four alternative AnyAgency objectives. Existing campaign rules use
            // one path whose multiple objective completion conditions are all required.
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

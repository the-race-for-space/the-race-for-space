using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Persistence;
using TheRaceForSpace.Agencies;

namespace TheRaceForSpace.Tests.Persistence
{
    internal static class CollectionPersistenceTests
    {
        public static void FundingContractsRoundTripArbitraryIds()
        {
            const double nextFundingUniversalTime = 98765.0;
            var player = new AgencyState("Player", true);
            player.RecordObjectiveCompletion("duna-probe-orbit", 1234.0);

            var satelliteContracts = new List<SatelliteNetworkFundingContract>
            {
                new SatelliteNetworkFundingContract(
                    "duna-network",
                    "Duna Network",
                    "Duna",
                    5,
                    100000.0,
                    true,
                    null),
                new SatelliteNetworkFundingContract(
                    "eve-network",
                    "Eve Network",
                    "Eve",
                    4,
                    90000.0,
                    false,
                    null)
            };
            satelliteContracts[0].Offer();
            satelliteContracts[0].MarkSatelliteTargetReached();

            var objectiveFundingContracts = new List<ObjectiveFundingContract>
            {
                new ObjectiveFundingContract(
                    "duna-probe-orbit",
                    "Duna Probe Orbit",
                    "Orbit Duna",
                    200000.0),
                new ObjectiveFundingContract(
                    "eve-probe-orbit",
                    "Eve Probe Orbit",
                    "Orbit Eve",
                    180000.0)
            };
            objectiveFundingContracts[0].Offer();
            objectiveFundingContracts[0].Start();
            objectiveFundingContracts[0].AdvancePayout();
            objectiveFundingContracts[0].AdvancePayout();
            objectiveFundingContracts[0].AdvancePayout();

            var saved = new CampaignFundingSaveState();
            saved.Capture(
                player,
                satelliteContracts,
                objectiveFundingContracts,
                nextFundingUniversalTime);
            var node = new ConfigNode();
            saved.Save(node);

            Equal(1, node.GetNodes("PLAYER_OBJECTIVE_COMPLETION").Length);
            Equal(2, node.GetNodes("OBJECTIVE_FUNDING_CONTRACT").Length);
            Equal(2, node.GetNodes("SATELLITE_NETWORK_FUNDING_CONTRACT").Length);

            var loaded = new CampaignFundingSaveState();
            loaded.Load(node);
            var restoredPlayer = new AgencyState("Player", true);
            var restoredSatelliteContracts = new List<SatelliteNetworkFundingContract>
            {
                new SatelliteNetworkFundingContract(
                    "duna-network",
                    "Duna Network",
                    "Duna",
                    5,
                    100000.0,
                    false,
                    null),
                new SatelliteNetworkFundingContract(
                    "eve-network",
                    "Eve Network",
                    "Eve",
                    4,
                    90000.0,
                    true,
                    null)
            };
            restoredSatelliteContracts[1].Offer();
            restoredSatelliteContracts[1].MarkSatelliteTargetReached();

            var restoredObjectiveFundingContracts = new List<ObjectiveFundingContract>
            {
                new ObjectiveFundingContract(
                    "duna-probe-orbit",
                    "Duna Probe Orbit",
                    "Orbit Duna",
                    200000.0),
                new ObjectiveFundingContract(
                    "eve-probe-orbit",
                    "Eve Probe Orbit",
                    "Orbit Eve",
                    180000.0)
            };
            restoredObjectiveFundingContracts[1].Offer();
            restoredObjectiveFundingContracts[1].Start();

            loaded.ApplyTo(
                restoredPlayer,
                restoredSatelliteContracts,
                restoredObjectiveFundingContracts);

            Equal(1234.0, restoredPlayer.GetObjectiveCompletionTime("duna-probe-orbit"));
            Require(restoredSatelliteContracts[0].IsAvailable,
                "Available satellite contract state should round trip.");
            Require(restoredSatelliteContracts[0].IsOffered,
                "Satellite offer state should round trip.");
            Require(restoredSatelliteContracts[0].HasReachedSatelliteTarget,
                "Satellite fulfilled state should round trip.");
            Require(!restoredSatelliteContracts[1].IsAvailable,
                "Locked satellite contracts should be persisted explicitly rather than by omission.");
            Require(!restoredSatelliteContracts[1].IsOffered,
                "Unoffered satellite contract state should replace stale runtime values.");
            Require(!restoredSatelliteContracts[1].HasReachedSatelliteTarget,
                "Unfulfilled satellite state should replace stale runtime values.");

            Require(restoredObjectiveFundingContracts[0].IsOffered,
                "ObjectiveCompletion offer state should round trip.");
            Require(restoredObjectiveFundingContracts[0].HasStarted,
                "Started objective completion contracts should remain started.");
            Equal(3, restoredObjectiveFundingContracts[0].PaymentsProcessed);
            Require(!restoredObjectiveFundingContracts[1].IsOffered,
                "Unoffered objective completion contracts should be persisted explicitly.");
            Require(!restoredObjectiveFundingContracts[1].HasStarted,
                "Unstarted objective completion contracts should replace stale runtime values.");
            Equal(0, restoredObjectiveFundingContracts[1].PaymentsProcessed);
            Equal(nextFundingUniversalTime, loaded.NextFundingUniversalTime);
        }

        public static void RivalRoundTripsArbitraryBodyAndTargetId()
        {
            var aster = new AgencyState("aster", "Aster", false)
            {
                Funds = 54321.0,
                NextMissionTargetId = "duna-network",
                MissionProgressPercent = 60,
                NextMissionProgressCheckUniversalTime = 9876.0
            };
            aster.RecordObjectiveCompletion("duna-probe-orbit", 4321.0);
            aster.SetSatelliteCount("Duna", 4);

            var delta = new AgencyState("delta", "Delta", false)
            {
                Funds = 22222.0,
                NextMissionTargetId = "eve-network",
                MissionProgressPercent = 30,
                NextMissionProgressCheckUniversalTime = 7654.0
            };
            delta.RecordObjectiveCompletion("eve-probe-orbit", 3456.0);
            delta.SetSatelliteCount("Eve", 2);

            var saved = new RivalAgenciesSaveState();
            saved.Capture(new List<AgencyState> { aster, delta });
            var node = new ConfigNode();
            saved.Save(node);

            Equal(2, node.GetNodes("RIVAL").Length);

            var loaded = new RivalAgenciesSaveState();
            loaded.Load(node);

            // Restore in a different order and with changed display names to prove identity comes
            // from stable IDs rather than fixed Aster/Cobalt slots or list position.
            var restoredDelta = new AgencyState("delta", "Delta Renamed", false);
            var unsavedRival = new AgencyState("echo", "Echo", false)
            {
                Funds = 777.0,
                NextMissionTargetId = "constructor-default",
                MissionProgressPercent = 20
            };
            var restoredAster = new AgencyState("aster", "Aster Renamed", false);

            loaded.ApplyTo(new List<AgencyState>
            {
                restoredDelta,
                unsavedRival,
                restoredAster
            });

            Equal(54321.0, restoredAster.Funds);
            Equal(4321.0, restoredAster.GetObjectiveCompletionTime("duna-probe-orbit"));
            Equal(4, restoredAster.GetSatelliteCount("Duna"));
            Equal("duna-network", restoredAster.NextMissionTargetId);
            Equal(60, restoredAster.MissionProgressPercent);
            Equal(9876.0, restoredAster.NextMissionProgressCheckUniversalTime);

            Equal(22222.0, restoredDelta.Funds);
            Equal(3456.0, restoredDelta.GetObjectiveCompletionTime("eve-probe-orbit"));
            Equal(2, restoredDelta.GetSatelliteCount("Eve"));
            Equal("eve-network", restoredDelta.NextMissionTargetId);
            Equal(30, restoredDelta.MissionProgressPercent);
            Equal(7654.0, restoredDelta.NextMissionProgressCheckUniversalTime);

            Equal(777.0, unsavedRival.Funds);
            Equal("constructor-default", unsavedRival.NextMissionTargetId);
            Equal(20, unsavedRival.MissionProgressPercent);
        }

        public static void MalformedCollectionNodesAreHandledSafely()
        {
            var contractsNode = new ConfigNode();
            contractsNode.AddValue("nextFundingUniversalTime", "NaN");

            ConfigNode badCompletion = contractsNode.AddNode("PLAYER_OBJECTIVE_COMPLETION");
            badCompletion.AddValue("id", "bad-time");
            badCompletion.AddValue("universalTime", "NaN");
            ConfigNode earlyCompletion = contractsNode.AddNode("PLAYER_OBJECTIVE_COMPLETION");
            earlyCompletion.AddValue("id", "early-objective");
            earlyCompletion.AddValue("universalTime", "-12");

            ConfigNode satelliteContract = contractsNode.AddNode("SATELLITE_NETWORK_FUNDING_CONTRACT");
            satelliteContract.AddValue("id", "future-network");
            satelliteContract.AddValue("available", true);
            satelliteContract.AddValue("offered", true);
            satelliteContract.AddValue("targetReached", false);

            ConfigNode objectiveFundingContract = contractsNode.AddNode("OBJECTIVE_FUNDING_CONTRACT");
            objectiveFundingContract.AddValue("id", "future-contract");
            objectiveFundingContract.AddValue("offered", true);
            objectiveFundingContract.AddValue("started", false);
            objectiveFundingContract.AddValue("paymentsProcessed", "99");

            ConfigNode malformedContract = contractsNode.AddNode("OBJECTIVE_FUNDING_CONTRACT");
            malformedContract.AddValue("id", "malformed-contract");
            malformedContract.AddValue("offered", "not-a-bool");
            malformedContract.AddValue("started", false);
            malformedContract.AddValue("paymentsProcessed", "1");

            var contractsState = new CampaignFundingSaveState();
            contractsState.Load(contractsNode);
            var player = new AgencyState("Player", true);
            var satelliteContracts = new List<SatelliteNetworkFundingContract>
            {
                new SatelliteNetworkFundingContract(
                    "future-network",
                    "Future Network",
                    "Duna",
                    5,
                    100000.0,
                    false,
                    null)
            };
            var objectiveFundingContracts = new List<ObjectiveFundingContract>
            {
                new ObjectiveFundingContract(
                    "future-contract",
                    "Future Contract",
                    "Objective",
                    100000.0),
                new ObjectiveFundingContract(
                    "malformed-contract",
                    "Malformed Contract",
                    "Objective",
                    100000.0)
            };
            objectiveFundingContracts[1].Offer();
            objectiveFundingContracts[1].Start();

            contractsState.ApplyTo(player, satelliteContracts, objectiveFundingContracts);

            Require(!player.HasCompletedObjective("bad-time"),
                "Non-finite objective completion times should be ignored.");
            Equal(0.0, player.GetObjectiveCompletionTime("early-objective"));
            Require(satelliteContracts[0].IsAvailable,
                "A valid satellite contract node should restore availability.");
            Require(satelliteContracts[0].IsOffered,
                "A valid satellite contract node should restore offer state.");
            Require(!satelliteContracts[0].HasReachedSatelliteTarget,
                "A valid satellite contract node should not invent target completion.");
            Equal(10, objectiveFundingContracts[0].PaymentsProcessed);
            Require(objectiveFundingContracts[0].HasStarted,
                "Processed payments should normalize the restored contract to started.");
            Require(objectiveFundingContracts[0].IsOffered,
                "A valid objective completion contract node should restore offer state.");
            Require(!objectiveFundingContracts[1].HasStarted,
                "Malformed contract state should fail closed instead of preserving stale runtime state.");
            Require(!objectiveFundingContracts[1].IsOffered,
                "Malformed contract state should fail closed instead of preserving a stale offer.");
            Equal(-1.0, contractsState.NextFundingUniversalTime);

            var rivalsNode = new ConfigNode();
            ConfigNode rivalNode = rivalsNode.AddNode("RIVAL");
            rivalNode.AddValue("programId", "aster");
            rivalNode.AddValue("funds", "NaN");
            rivalNode.AddValue("nextMissionTargetId", "future-target");
            rivalNode.AddValue("launchProgressPercent", "999");
            rivalNode.AddValue("nextLaunchProgressCheckUniversalTime", "-50");
            ConfigNode satellite = rivalNode.AddNode("SATELLITE");
            satellite.AddValue("body", "Duna");
            satellite.AddValue("count", "-4");

            var rivalState = new RivalAgenciesSaveState();
            rivalState.Load(rivalsNode);
            var rival = new AgencyState("aster", "Aster", false);
            rivalState.ApplyTo(new List<AgencyState> { rival });

            Equal(0.0, rival.Funds);
            Equal("future-target", rival.NextMissionTargetId);
            Equal(100, rival.MissionProgressPercent);
            Equal(0.0, rival.NextMissionProgressCheckUniversalTime);
            Equal(0, rival.GetSatelliteCount("Duna"));
        }

        public static void EmptyCollectionNodesRestoreWithoutInventingState()
        {
            var contractsState = new CampaignFundingSaveState();
            contractsState.Load(new ConfigNode());
            var player = new AgencyState("Player", true);
            player.RecordObjectiveCompletion("stale-objective", 55.0);
            var satelliteContracts = new List<SatelliteNetworkFundingContract>
            {
                new SatelliteNetworkFundingContract("future-network", "Future", "Duna", 5, 100000.0, true, null)
            };
            satelliteContracts[0].Offer();
            satelliteContracts[0].MarkSatelliteTargetReached();
            var objectiveFundingContracts = new List<ObjectiveFundingContract>
            {
                new ObjectiveFundingContract("future-contract", "Future", "Objective", 100000.0)
            };
            objectiveFundingContracts[0].Offer();
            objectiveFundingContracts[0].Start();
            objectiveFundingContracts[0].AdvancePayout();

            contractsState.ApplyTo(player, satelliteContracts, objectiveFundingContracts);

            Require(!player.HasCompletedObjective("stale-objective"),
                "Empty funding-contract state should clear stale player objectives.");
            Require(!satelliteContracts[0].IsAvailable,
                "Missing satellite contract state should restore the contract as locked.");
            Require(!satelliteContracts[0].IsOffered,
                "Missing satellite contract state should clear stale offer state.");
            Require(!satelliteContracts[0].HasReachedSatelliteTarget,
                "Missing satellite contract state should clear stale fulfilled state.");
            Require(!objectiveFundingContracts[0].HasStarted,
                "Missing objective completion contract state should reset lifecycle state.");
            Require(!objectiveFundingContracts[0].IsOffered,
                "Missing objective completion contract state should clear stale offer state.");
            Equal(0, objectiveFundingContracts[0].PaymentsProcessed);
            Equal(-1.0, contractsState.NextFundingUniversalTime);

            var rivalState = new RivalAgenciesSaveState();
            rivalState.Load(new ConfigNode());
            var rival = new AgencyState("aster", "Aster", false)
            {
                NextMissionTargetId = "constructor-default",
                Funds = 100.0,
                MissionProgressPercent = 80
            };
            rival.RecordObjectiveCompletion("existing-rival-objective-completion", 10.0);
            rival.SetSatelliteCount("Duna", 5);
            rivalState.ApplyTo(new List<AgencyState> { rival });

            Equal(100.0, rival.Funds);
            Equal("constructor-default", rival.NextMissionTargetId);
            Equal(80, rival.MissionProgressPercent);
            Require(rival.HasCompletedObjective("existing-rival-objective-completion"),
                "An unsaved rival should retain constructor/current state rather than receive invented data.");
            Equal(5, rival.GetSatelliteCount("Duna"));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.InvalidOperationException(message);
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new System.InvalidOperationException(
                    "Expected '" + expected + "' but got '" + actual + "'.");
            }
        }
    }
}

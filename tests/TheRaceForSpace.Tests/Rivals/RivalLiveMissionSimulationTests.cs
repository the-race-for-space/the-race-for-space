using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Rivals;

namespace TheRaceForSpace.Tests.Rivals
{
    internal static class RivalLiveMissionSimulationTests
    {
        private const double KerbinDaySeconds = 21600.0;

        public static void RunAll()
        {
            SuccessChanceMatchesApprovedDifficultyScale();
            ContractLaunchSnapshotsLocationDurationDifficultyAndCrew();
            ScienceLaunchUsesLocationDifficultyAndBlocksDuplicateSubject();
            SatelliteCapacityCountsLiveReservations();
            NetworkSuccessTurnsReservationIntoSatellite();
            LaunchedContractSurvivesSponsorExpiry();
            FailedCrewedMissionUsesDeterministicCasualtiesAndInsurance();
            ScienceSuccessUsesSharedPoolCompletionCallback();
            DueMissionOrderingUsesCompletionTimeThenSequence();
        }

        private static void SuccessChanceMatchesApprovedDifficultyScale()
        {
            for (int difficulty = 1; difficulty <= 10; difficulty++)
            {
                Near(
                    95.0 - (difficulty * 5.0),
                    RivalLiveMissionSimulation.CalculateSuccessChancePercent(difficulty));
            }

            Near(0.0, RivalLiveMissionSimulation.CalculateSuccessChancePercent(0));
            Near(0.0, RivalLiveMissionSimulation.CalculateSuccessChancePercent(11));
            Near(
                RivalLiveMissionSimulation.GetDeterministicRollPercent(12345, 0),
                RivalLiveMissionSimulation.GetDeterministicRollPercent(12345, 0));
            Require(
                RivalLiveMissionSimulation.GetDeterministicRollPercent(12345, 0)
                    != RivalLiveMissionSimulation.GetDeterministicRollPercent(12345, 1),
                "Different deterministic roll sequences should not reuse the same mission roll.");
        }

        private static void ContractLaunchSnapshotsLocationDurationDifficultyAndCrew()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);
            IList<ObjectiveFundingContract> objectiveContracts =
                FundingContractCatalogue.CreateObjectiveFundingContracts();
            IList<SatelliteNetworkFundingContract> networkContracts =
                FundingContractCatalogue.CreateSatelliteNetworkFundingContracts();

            ObjectiveFundingContract directedPower = OfferObjective(
                objectiveContracts,
                ObjectiveCatalogue.DirectedPower1Id);
            RivalLiveMissionState localMission = RivalLiveMissionSimulation.TryCreateContractMission(
                rival,
                directedPower.Id,
                1000.0,
                objectiveContracts,
                networkContracts,
                1);
            Require(localMission != null, "An offered Directed Power Contract should launch.");
            Equal(1L, localMission.MissionSequence);
            Equal(RivalMissionType.Contract, localMission.MissionType);
            Equal(ObjectiveCatalogue.DirectedPower1Id, localMission.ContractId);
            Equal(CampaignSettings.RivalPreOrbitLocalLocationId, localMission.LocationId);
            Near(5.0, localMission.DurationDays);
            Equal(1, localMission.Difficulty);
            Near(90.0, localMission.SuccessChancePercent);
            Equal(0, localMission.AssignedKerbalCount);
            Near(1000.0 + (5.0 * KerbinDaySeconds), localMission.CompletionUniversalTime);
            Require(RivalLiveMissionSimulation.TryCreateContractMission(
                    rival,
                    directedPower.Id,
                    1001.0,
                    objectiveContracts,
                    networkContracts,
                    2) == null,
                "A rival must not launch the same one-off objective twice while it is live.");

            ObjectiveFundingContract biome = OfferObjective(
                objectiveContracts,
                ObjectiveCatalogue.Biome5Id);
            RivalLiveMissionState biomeMission = RivalLiveMissionSimulation.TryCreateContractMission(
                rival,
                biome.Id,
                2000.0,
                objectiveContracts,
                networkContracts,
                2);
            Require(biomeMission != null, "An offered Biome V Contract should launch.");
            Equal(2L, biomeMission.MissionSequence);
            Equal("kerbin:ice-caps", biomeMission.LocationId);
            Near(90.0, biomeMission.DurationDays);
            Equal(3, biomeMission.Difficulty);
            Equal(0, biomeMission.AssignedKerbalCount);

            ObjectiveFundingContract dunaCrewed = OfferObjective(
                objectiveContracts,
                ObjectiveCatalogue.DunaCrewedOrbitId);
            RivalLiveMissionState dunaMission = RivalLiveMissionSimulation.TryCreateContractMission(
                rival,
                dunaCrewed.Id,
                3000.0,
                objectiveContracts,
                networkContracts,
                3);
            Require(dunaMission != null, "The one available rival Kerbal should support a crewed Contract launch.");
            Equal(3L, dunaMission.MissionSequence);
            Equal("body:Duna", dunaMission.LocationId);
            Near(303.0, dunaMission.DurationDays);
            Equal(8, dunaMission.Difficulty);
            Near(55.0, dunaMission.SuccessChancePercent);
            Equal(1, dunaMission.AssignedKerbalCount);
            Equal(0, RivalDevelopmentSimulation.GetKerbalsAvailable(rival));
        }

        private static void ScienceLaunchUsesLocationDifficultyAndBlocksDuplicateSubject()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);
            var subject = new ScienceSubjectKey(
                RivalTechCatalogue.TemperatureScanExperimentId,
                "Kerbin",
                "Landed",
                "Highlands");
            var preparation = new ScienceLaunchPreparationState
            {
                Subject = subject,
                PlannedScienceReward = 8.5,
                LaunchProgressPercent = 100,
                RequiredKerbals = 1
            };

            RivalLiveMissionState mission = RivalLiveMissionSimulation.TryCreateScienceMission(
                rival,
                preparation,
                "kerbin:highlands",
                5000.0,
                1);
            Require(mission != null, "A ready valid Kerbin Science preparation should launch.");
            Equal(RivalMissionType.Science, mission.MissionType);
            Equal(null, mission.ContractId);
            Require(subject.Equals(mission.ScienceSubject), "The exact Science subject identity should be snapshotted.");
            Equal("kerbin:highlands", mission.LocationId);
            Near(10.0, mission.DurationDays);
            Equal(1, mission.Difficulty);
            Near(90.0, mission.SuccessChancePercent);
            Equal(1, mission.AssignedKerbalCount);
            Near(8.5, mission.PlannedScienceReward);
            Require(RivalLiveMissionSimulation.TryCreateScienceMission(
                    rival,
                    preparation,
                    "kerbin:highlands",
                    5001.0,
                    2) == null,
                "The same Science subject must not be launched twice within one rival programme.");

            var sunPreparation = new ScienceLaunchPreparationState
            {
                Subject = new ScienceSubjectKey("crewReport", "Sun", "LowSpace", null),
                PlannedScienceReward = 1.0,
                LaunchProgressPercent = 100,
                RequiredKerbals = 0
            };
            Require(RivalLiveMissionSimulation.TryCreateScienceMission(
                    new AgencyState("sun-test", "Sun Test", false),
                    sunPreparation,
                    "body:Sun",
                    0.0,
                    1) == null,
                "Sun Science should fail safely because no supported Sun mission location exists.");
        }

        private static void SatelliteCapacityCountsLiveReservations()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);
            rival.SetSatelliteCount("Kerbin", 2);
            IList<ObjectiveFundingContract> objectiveContracts =
                FundingContractCatalogue.CreateObjectiveFundingContracts();
            IList<SatelliteNetworkFundingContract> networkContracts =
                FundingContractCatalogue.CreateSatelliteNetworkFundingContracts();
            SatelliteNetworkFundingContract kerbinNetwork = OfferNetwork(
                networkContracts,
                FundingContractCatalogue.KerbinNetworkId);

            RivalLiveMissionState firstNetworkMission =
                RivalLiveMissionSimulation.TryCreateContractMission(
                    rival,
                    kerbinNetwork.Id,
                    1000.0,
                    objectiveContracts,
                    networkContracts,
                    1);
            Require(firstNetworkMission != null, "The final Level-1 satellite slot should be reservable.");
            Equal(3, RivalLiveMissionSimulation.GetSatelliteCapacityUsed(rival, networkContracts));
            Require(RivalLiveMissionSimulation.TryCreateContractMission(
                    rival,
                    kerbinNetwork.Id,
                    1001.0,
                    objectiveContracts,
                    networkContracts,
                    2) == null,
                "A launched satellite-producing mission should reserve Mission Control capacity.");

            rival.RivalProgram.FacilityLevels[RivalFacilityType.MissionControl] = 3;
            RivalLiveMissionState repeatedNetworkMission =
                RivalLiveMissionSimulation.TryCreateContractMission(
                    rival,
                    kerbinNetwork.Id,
                    1002.0,
                    objectiveContracts,
                    networkContracts,
                    2);
            Require(repeatedNetworkMission != null,
                "Satellite-network Contracts remain repeatable when capacity permits another launch.");
            Equal(4, RivalLiveMissionSimulation.GetSatelliteCapacityUsed(rival, networkContracts));

            var probeRival = new AgencyState("probe", "Probe", false);
            probeRival.SetSatelliteCount("Kerbin", 2);
            ObjectiveFundingContract probeOrbit = OfferObjective(
                objectiveContracts,
                ObjectiveCatalogue.ProbeOrbitId);
            RivalLiveMissionState probeMission = RivalLiveMissionSimulation.TryCreateContractMission(
                probeRival,
                probeOrbit.Id,
                2000.0,
                objectiveContracts,
                networkContracts,
                3);
            Require(probeMission != null, "Probe Orbit should reserve the final Level-1 satellite slot.");
            Equal(3, RivalLiveMissionSimulation.GetSatelliteCapacityUsed(probeRival, networkContracts));
        }

        private static void NetworkSuccessTurnsReservationIntoSatellite()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);
            rival.SetSatelliteCount("Kerbin", 2);
            IList<ObjectiveFundingContract> objectiveContracts =
                FundingContractCatalogue.CreateObjectiveFundingContracts();
            IList<SatelliteNetworkFundingContract> networkContracts =
                FundingContractCatalogue.CreateSatelliteNetworkFundingContracts();
            SatelliteNetworkFundingContract kerbinNetwork = OfferNetwork(
                networkContracts,
                FundingContractCatalogue.KerbinNetworkId);
            RivalLiveMissionState mission = RivalLiveMissionSimulation.TryCreateContractMission(
                rival,
                kerbinNetwork.Id,
                0.0,
                objectiveContracts,
                networkContracts,
                1);
            Require(mission != null, "Kerbin network mission should launch into the final slot.");
            Equal(3, RivalLiveMissionSimulation.GetSatelliteCapacityUsed(rival, networkContracts));

            RivalLiveMissionResolution resolution = RivalLiveMissionSimulation.ResolveMission(
                rival,
                mission,
                mission.CompletionUniversalTime,
                networkContracts,
                null);
            Require(resolution != null && resolution.WasValid && resolution.Succeeded,
                "Seed 1 should succeed for the Difficulty-4 Kerbin network mission.");
            Equal("Kerbin", resolution.SatelliteBodyName);
            Equal(3, rival.GetSatelliteCount("Kerbin"));
            Equal(0, rival.RivalProgram.LiveMissions.Count);
            Equal(3, RivalLiveMissionSimulation.GetSatelliteCapacityUsed(rival, networkContracts));
        }

        private static void LaunchedContractSurvivesSponsorExpiry()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false)
            {
                Funds = 12345.0
            };
            IList<ObjectiveFundingContract> objectiveContracts =
                FundingContractCatalogue.CreateObjectiveFundingContracts();
            IList<SatelliteNetworkFundingContract> networkContracts =
                FundingContractCatalogue.CreateSatelliteNetworkFundingContracts();
            ObjectiveFundingContract contract = OfferObjective(
                objectiveContracts,
                ObjectiveCatalogue.DirectedPower1Id);
            RivalLiveMissionState mission = RivalLiveMissionSimulation.TryCreateContractMission(
                rival,
                contract.Id,
                100.0,
                objectiveContracts,
                networkContracts,
                1);
            Require(mission != null, "The Contract should launch while its sponsor offer is active.");

            contract.Start();
            for (int payment = 0; payment < 10; payment++)
            {
                contract.AdvancePayout();
            }
            Require(contract.IsExpired, "Test setup should expire the sponsor Contract after launch.");

            RivalLiveMissionResolution resolution = RivalLiveMissionSimulation.ResolveMission(
                rival,
                mission,
                mission.CompletionUniversalTime + 1000.0,
                networkContracts,
                null);
            Require(resolution != null && resolution.Succeeded,
                "A launched Contract should still resolve after its funding lifecycle expires.");
            Require(resolution.ObjectiveRecorded,
                "Successful post-expiry completion should still record campaign progression.");
            Require(rival.HasCompletedObjective(ObjectiveCatalogue.DirectedPower1Id),
                "The rival should retain the successful objective result.");
            Near(mission.CompletionUniversalTime,
                rival.GetObjectiveCompletionTime(ObjectiveCatalogue.DirectedPower1Id));
            Near(12345.0, rival.Funds);
            Equal(0, rival.GetSatelliteCount("Kerbin"));
        }

        private static void FailedCrewedMissionUsesDeterministicCasualtiesAndInsurance()
        {
            CampaignSettings.ResetToDefaults();
            AgencyState firstRival = CreateThreeCrewEveScienceRival("first");
            RivalLiveMissionState firstMission = firstRival.RivalProgram.LiveMissions[0];
            RivalLiveMissionResolution firstResolution = RivalLiveMissionSimulation.ResolveMission(
                firstRival,
                firstMission,
                firstMission.CompletionUniversalTime,
                new List<SatelliteNetworkFundingContract>(),
                null);

            Require(firstResolution != null && firstResolution.WasValid && !firstResolution.Succeeded,
                "Seed 6 should fail the Difficulty-10 Eve Science mission.");
            Equal(2, firstResolution.LostKerbalCount);
            Equal(1, firstRival.RivalProgram.KerbalsEmployed);
            Near(100000.0, firstRival.RivalProgram.PendingInsuranceFunds);
            Near(0.0, firstRival.RivalProgram.StoredScience);
            Equal(0, firstRival.RivalProgram.LiveMissions.Count);

            AgencyState secondRival = CreateThreeCrewEveScienceRival("second");
            RivalLiveMissionState secondMission = secondRival.RivalProgram.LiveMissions[0];
            RivalLiveMissionResolution secondResolution = RivalLiveMissionSimulation.ResolveMission(
                secondRival,
                secondMission,
                secondMission.CompletionUniversalTime,
                new List<SatelliteNetworkFundingContract>(),
                null);
            Equal(firstResolution.LostKerbalCount, secondResolution.LostKerbalCount);
            Near(
                firstRival.RivalProgram.PendingInsuranceFunds,
                secondRival.RivalProgram.PendingInsuranceFunds);
        }

        private static void ScienceSuccessUsesSharedPoolCompletionCallback()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);
            rival.RivalProgram.StoredScience = 5.0;
            ScienceSubjectKey subject = new ScienceSubjectKey(
                RivalTechCatalogue.CrewReportExperimentId,
                "Kerbin",
                "Landed",
                "KSC");
            RivalLiveMissionState mission = CreateReadyScienceMission(
                rival,
                subject,
                "ksc:ksc",
                1);
            Require(mission != null, "KSC Science mission should launch for shared-pool completion testing.");

            bool callbackCalled = false;
            RivalLiveMissionResolution resolution = RivalLiveMissionSimulation.ResolveMission(
                rival,
                mission,
                mission.CompletionUniversalTime,
                new List<SatelliteNetworkFundingContract>(),
                delegate(ScienceSubjectKey resolvedSubject)
                {
                    callbackCalled = true;
                    Require(subject.Equals(resolvedSubject),
                        "The shared-pool callback should receive the exact launched Science subject.");
                    return 12.5;
                });
            Require(callbackCalled, "Successful Science completion should consult the shared pool exactly at completion.");
            Require(resolution != null && resolution.Succeeded, "Seed 1 should succeed for Difficulty-1 KSC Science.");
            Near(12.5, resolution.ScienceAwarded);
            Near(17.5, rival.RivalProgram.StoredScience);
            Require(rival.RivalProgram.CompletedScienceSubjects.Contains(subject),
                "Successful Science should record the subject even though the shared pool is external.");

            ScienceSubjectKey depletedSubject = new ScienceSubjectKey(
                RivalTechCatalogue.MysteryGooExperimentId,
                "Kerbin",
                "Landed",
                "KSC");
            RivalLiveMissionState depletedMission = CreateReadyScienceMission(
                rival,
                depletedSubject,
                "ksc:ksc",
                1);
            RivalLiveMissionResolution depletedResolution = RivalLiveMissionSimulation.ResolveMission(
                rival,
                depletedMission,
                depletedMission.CompletionUniversalTime,
                new List<SatelliteNetworkFundingContract>(),
                delegate { return 0.0; });
            Require(depletedResolution != null && depletedResolution.Succeeded,
                "A successful mission should still resolve when another agency already depleted the subject.");
            Near(0.0, depletedResolution.ScienceAwarded);
            Near(17.5, rival.RivalProgram.StoredScience);
            Require(rival.RivalProgram.CompletedScienceSubjects.Contains(depletedSubject),
                "A successful zero-Science result still counts as completed for that rival.");

            ScienceSubjectKey waitingSubject = new ScienceSubjectKey(
                RivalTechCatalogue.TemperatureScanExperimentId,
                "Kerbin",
                "Landed",
                "KSC");
            RivalLiveMissionState waitingMission = CreateReadyScienceMission(
                rival,
                waitingSubject,
                "ksc:ksc",
                1);
            RivalLiveMissionResolution missingBoundaryResolution = RivalLiveMissionSimulation.ResolveMission(
                rival,
                waitingMission,
                waitingMission.CompletionUniversalTime,
                new List<SatelliteNetworkFundingContract>(),
                null);
            Require(missingBoundaryResolution == null,
                "A successful Science mission must wait rather than silently bypass the shared Science boundary.");
            Require(rival.RivalProgram.LiveMissions.Contains(waitingMission),
                "The due Science mission should remain live until a shared-pool callback is available.");
        }

        private static void DueMissionOrderingUsesCompletionTimeThenSequence()
        {
            var rival = new AgencyState("aster", "Aster", false);
            rival.RivalProgram.LiveMissions.Add(new RivalLiveMissionState
            {
                MissionSequence = 3,
                CompletionUniversalTime = 20.0
            });
            rival.RivalProgram.LiveMissions.Add(new RivalLiveMissionState
            {
                MissionSequence = 5,
                CompletionUniversalTime = 10.0
            });
            rival.RivalProgram.LiveMissions.Add(new RivalLiveMissionState
            {
                MissionSequence = 2,
                CompletionUniversalTime = 10.0
            });

            Require(RivalLiveMissionSimulation.GetNextDueMission(rival, 9.0) == null,
                "No live mission should be due before its completion UT.");
            RivalLiveMissionState due = RivalLiveMissionSimulation.GetNextDueMission(rival, 10.0);
            Require(due != null, "A mission should become due exactly at its stored completion UT.");
            Equal(2L, due.MissionSequence);
        }

        private static AgencyState CreateThreeCrewEveScienceRival(string id)
        {
            var rival = new AgencyState(id, id, false);
            rival.RivalProgram.KerbalsEmployed = 3;
            var preparation = new ScienceLaunchPreparationState
            {
                Subject = new ScienceSubjectKey("crewReport", "Eve", "LowSpace", null),
                PlannedScienceReward = 50.0,
                LaunchProgressPercent = 100,
                RequiredKerbals = 3
            };
            RivalLiveMissionState mission = RivalLiveMissionSimulation.TryCreateScienceMission(
                rival,
                preparation,
                "body:Eve",
                0.0,
                6);
            Require(mission != null, "Three employed Kerbals should support the future multi-crew Science test mission.");
            Equal(10, mission.Difficulty);
            Near(45.0, mission.SuccessChancePercent);
            return rival;
        }

        private static RivalLiveMissionState CreateReadyScienceMission(
            AgencyState rival,
            ScienceSubjectKey subject,
            string locationId,
            int outcomeSeed)
        {
            var preparation = new ScienceLaunchPreparationState
            {
                Subject = subject,
                PlannedScienceReward = 20.0,
                LaunchProgressPercent = 100,
                RequiredKerbals = 1
            };
            return RivalLiveMissionSimulation.TryCreateScienceMission(
                rival,
                preparation,
                locationId,
                0.0,
                outcomeSeed);
        }

        private static ObjectiveFundingContract OfferObjective(
            IList<ObjectiveFundingContract> contracts,
            string contractId)
        {
            for (int contractIndex = 0; contractIndex < contracts.Count; contractIndex++)
            {
                ObjectiveFundingContract contract = contracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, contractId, StringComparison.OrdinalIgnoreCase))
                {
                    contract.Offer();
                    return contract;
                }
            }

            throw new InvalidOperationException("Missing objective funding Contract '" + contractId + "'.");
        }

        private static SatelliteNetworkFundingContract OfferNetwork(
            IList<SatelliteNetworkFundingContract> contracts,
            string contractId)
        {
            for (int contractIndex = 0; contractIndex < contracts.Count; contractIndex++)
            {
                SatelliteNetworkFundingContract contract = contracts[contractIndex];
                if (contract != null
                    && string.Equals(contract.Id, contractId, StringComparison.OrdinalIgnoreCase))
                {
                    contract.Unlock();
                    contract.Offer();
                    return contract;
                }
            }

            throw new InvalidOperationException("Missing satellite-network Contract '" + contractId + "'.");
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 0.000001)
            {
                throw new InvalidOperationException(
                    "Expected '" + expected + "' but got '" + actual + "'.");
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

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

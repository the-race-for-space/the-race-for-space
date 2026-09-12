using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Rivals;

namespace TheRaceForSpace.Tests.Rivals
{
    internal static class RivalSimulationChronologyTests
    {
        private const double KerbinDaySeconds = 21600.0;

        public static void RunAll()
        {
            NormalPreparationUsesFacilitiesAndLaunchesLiveMission();
            TrackingStationGatesContractsWithoutTechRequirements();
            SatelliteCapacityIsRecheckedAtReadyLaunch();
            ReadyCrewContentionUsesReadyTimeAndContractTieBreak();
            EarlierReadyScienceWinsWhenCrewReturnsLater();
            SharedScienceTieUsesStableAgencyIdOrder();
            SignedNegativeFundingProjectionCanMakeLaunchUnaffordable();
            LargeTimeJumpProcessesStoredEventsChronologically();
        }

        private static void NormalPreparationUsesFacilitiesAndLaunchesLiveMission()
        {
            CampaignSettings.ResetToDefaults();
            double originalFacilityChance = CampaignSettings.RivalNormalLaunchFacilityLevel1Chance;
            try
            {
                CampaignSettings.RivalNormalLaunchFacilityLevel1Chance = 0.50;

                ObjectiveFundingContract contract = CreateOfferedObjectiveContract(ObjectiveCatalogue.DirectedPower1Id);
                var rival = new AgencyState("aster", "Aster", false)
                {
                    Funds = 100000.0,
                    NextMissionTargetId = ObjectiveCatalogue.DirectedPower1Id,
                    MissionProgressPercent = 80,
                    NextMissionProgressCheckUniversalTime = 5.0 * KerbinDaySeconds
                };

                RivalSimulation.Refresh(new List<AgencyState> { rival }, 5.0 * KerbinDaySeconds,
                    new List<ObjectiveFundingContract> { contract }, new List<SatelliteNetworkFundingContract>(),
                    null, null, new ZeroRandom());

                Equal(96000.0, rival.Funds);
                Require(!rival.HasCompletedObjective(ObjectiveCatalogue.DirectedPower1Id),
                    "100% Launch Progress should launch a live mission rather than complete the Contract.");
                Equal(1, rival.RivalProgram.LiveMissions.Count);
                RivalLiveMissionState mission = rival.RivalProgram.LiveMissions[0];
                Equal(RivalMissionType.Contract, mission.MissionType);
                Equal(ObjectiveCatalogue.DirectedPower1Id, mission.ContractId);
                Equal(5.0 * KerbinDaySeconds, mission.LaunchUniversalTime);
                Equal(5.0, mission.DurationDays);
                Equal(10.0 * KerbinDaySeconds, mission.CompletionUniversalTime);
                Equal(0, rival.GetSatelliteCount("Kerbin"));
                Equal(null, rival.NextMissionTargetId);

                RivalSimulation.Refresh(new List<AgencyState> { rival }, mission.CompletionUniversalTime,
                    new List<ObjectiveFundingContract> { contract }, new List<SatelliteNetworkFundingContract>(),
                    null, null, new ZeroRandom());

                Require(rival.HasCompletedObjective(ObjectiveCatalogue.DirectedPower1Id),
                    "The successful live Contract should record its result at the stored completion UT.");
                Equal(mission.CompletionUniversalTime,
                    rival.GetObjectiveCompletionTime(ObjectiveCatalogue.DirectedPower1Id));
                Equal(0, rival.GetSatelliteCount("Kerbin"));
                Equal(0, rival.RivalProgram.LiveMissions.Count);
            }
            finally
            {
                CampaignSettings.RivalNormalLaunchFacilityLevel1Chance = originalFacilityChance;
            }
        }

        private static void TrackingStationGatesContractsWithoutTechRequirements()
        {
            CampaignSettings.ResetToDefaults();
            ObjectiveFundingContract munContract = CreateOfferedObjectiveContract(ObjectiveCatalogue.MunProbeOrbitId);
            var rival = new AgencyState("aster", "Aster", false);
            rival.RivalProgram.ResearchedTechIds.Clear();
            rival.RivalProgram.ResearchedTechIds.Add(RivalProgramState.StartingTechId);

            RivalSimulation.Refresh(new List<AgencyState> { rival }, 0.0,
                new List<ObjectiveFundingContract> { munContract }, new List<SatelliteNetworkFundingContract>(),
                null, null, new ZeroRandom());
            Equal(null, rival.NextMissionTargetId);

            rival.RivalProgram.FacilityLevels[RivalFacilityType.TrackingStation] = 2;
            RivalSimulation.Refresh(new List<AgencyState> { rival }, 1.0,
                new List<ObjectiveFundingContract> { munContract }, new List<SatelliteNetworkFundingContract>(),
                null, null, new ZeroRandom());

            Equal(ObjectiveCatalogue.MunProbeOrbitId, rival.NextMissionTargetId);
            Require(!rival.RivalProgram.ResearchedTechIds.Contains("basicRocketry"),
                "Contract selection must not gain an implicit rival-tech requirement.");
        }

        private static void SatelliteCapacityIsRecheckedAtReadyLaunch()
        {
            CampaignSettings.ResetToDefaults();
            ObjectiveFundingContract probeOrbitContract = CreateOfferedObjectiveContract(ObjectiveCatalogue.ProbeOrbitId);
            IList<SatelliteNetworkFundingContract> networkContracts = FundingContractCatalogue.CreateSatelliteNetworkFundingContracts();
            SatelliteNetworkFundingContract kerbinNetwork = FindNetwork(networkContracts, FundingContractCatalogue.KerbinNetworkId);
            Require(kerbinNetwork != null, "Kerbin network Contract should exist for the capacity test.");

            var rival = new AgencyState("aster", "Aster", false)
            {
                NextMissionTargetId = ObjectiveCatalogue.ProbeOrbitId,
                MissionProgressPercent = 100,
                NextMissionReadyUniversalTime = 5.0 * KerbinDaySeconds
            };
            rival.SetSatelliteCount("Kerbin", 2);
            rival.RivalProgram.LiveMissions.Add(new RivalLiveMissionState
            {
                MissionSequence = 1,
                MissionType = RivalMissionType.Contract,
                ContractId = kerbinNetwork.Id,
                LocationId = CampaignSettings.RivalKerbinOrbitLocationId,
                LaunchUniversalTime = 0.0,
                DurationDays = 10.0,
                CompletionUniversalTime = 10.0 * KerbinDaySeconds,
                Difficulty = 4,
                SuccessChancePercent = 0.0,
                AssignedKerbalCount = 0,
                OutcomeSeed = 0
            });

            RivalSimulation.Refresh(new List<AgencyState> { rival }, 9.0 * KerbinDaySeconds,
                new List<ObjectiveFundingContract> { probeOrbitContract }, networkContracts,
                null, null, new ZeroRandom());

            Equal(100, rival.MissionProgressPercent);
            Equal(1, rival.RivalProgram.LiveMissions.Count);
            Equal(ObjectiveCatalogue.ProbeOrbitId, rival.NextMissionTargetId);

            RivalSimulation.Refresh(new List<AgencyState> { rival }, 10.0 * KerbinDaySeconds,
                new List<ObjectiveFundingContract> { probeOrbitContract }, networkContracts,
                null, null, new ZeroRandom());

            Equal(1, rival.RivalProgram.LiveMissions.Count);
            RivalLiveMissionState probeMission = rival.RivalProgram.LiveMissions[0];
            Equal(ObjectiveCatalogue.ProbeOrbitId, probeMission.ContractId);
            Equal(10.0 * KerbinDaySeconds, probeMission.LaunchUniversalTime);
            Equal(2, rival.GetSatelliteCount("Kerbin"));
        }

        private static void ReadyCrewContentionUsesReadyTimeAndContractTieBreak()
        {
            CampaignSettings.ResetToDefaults();
            ObjectiveFundingContract controlContract = CreateOfferedObjectiveContract(ObjectiveCatalogue.Control1Id);
            ScienceSubjectKey scienceSubject = new ScienceSubjectKey(
                RivalTechCatalogue.CrewReportExperimentId, "Kerbin", "Landed", "Grasslands");
            var scienceCandidates = new List<RivalScienceSubjectCandidate>
            {
                new RivalScienceSubjectCandidate(scienceSubject, "kerbin:grasslands", 5.0)
            };
            var rival = new AgencyState("aster", "Aster", false)
            {
                Funds = 0.0,
                NextMissionTargetId = ObjectiveCatalogue.Control1Id,
                MissionProgressPercent = 100,
                NextMissionReadyUniversalTime = 100.0
            };
            rival.RivalProgram.ScienceLaunchPreparation = new ScienceLaunchPreparationState
            {
                Subject = scienceSubject,
                PlannedScienceReward = 5.0,
                LaunchProgressPercent = 100,
                RequiredKerbals = 1,
                ReadyUniversalTime = 100.0
            };

            Func<AgencyState, IList<RivalScienceSubjectCandidate>> captureScience = agency => scienceCandidates;
            RivalSimulation.Refresh(new List<AgencyState> { rival }, 100.0,
                new List<ObjectiveFundingContract> { controlContract }, new List<SatelliteNetworkFundingContract>(),
                captureScience, subject => 5.0, new ZeroRandom());

            Equal(1, rival.RivalProgram.LiveMissions.Count);
            RivalLiveMissionState controlMission = rival.RivalProgram.LiveMissions[0];
            Equal(RivalMissionType.Contract, controlMission.MissionType);
            Equal(ObjectiveCatalogue.Control1Id, controlMission.ContractId);
            Equal(1, controlMission.AssignedKerbalCount);
            Require(RivalScienceSimulation.IsPreparationReady(rival),
                "Science should keep waiting when the same ready-time Contract uses the only Kerbal.");

            RivalSimulation.Refresh(new List<AgencyState> { rival }, controlMission.CompletionUniversalTime,
                new List<ObjectiveFundingContract> { controlContract }, new List<SatelliteNetworkFundingContract>(),
                captureScience, subject => 5.0, new ZeroRandom());

            Require(rival.HasCompletedObjective(ObjectiveCatalogue.Control1Id),
                "The deterministic Control mission should succeed and return its surviving Kerbal.");
            Equal(1, rival.RivalProgram.LiveMissions.Count);
            RivalLiveMissionState scienceMission = rival.RivalProgram.LiveMissions[0];
            Equal(RivalMissionType.Science, scienceMission.MissionType);
            Equal(controlMission.CompletionUniversalTime, scienceMission.LaunchUniversalTime);
            Equal(1, scienceMission.AssignedKerbalCount);
            Require(rival.RivalProgram.ScienceLaunchPreparation == null,
                "The waiting Science preparation should move into Live Mission Progress once crew returns.");
        }

        private static void EarlierReadyScienceWinsWhenCrewReturnsLater()
        {
            CampaignSettings.ResetToDefaults();
            ObjectiveFundingContract waitingContract = CreateOfferedObjectiveContract(ObjectiveCatalogue.Control2Id);
            ScienceSubjectKey scienceSubject = new ScienceSubjectKey(
                RivalTechCatalogue.CrewReportExperimentId, "Kerbin", "Landed", "Grasslands");
            var scienceCandidates = new List<RivalScienceSubjectCandidate>
            {
                new RivalScienceSubjectCandidate(scienceSubject, "kerbin:grasslands", 5.0)
            };
            var rival = new AgencyState("aster", "Aster", false)
            {
                Funds = 0.0,
                NextMissionTargetId = ObjectiveCatalogue.Control2Id,
                MissionProgressPercent = 100,
                NextMissionReadyUniversalTime = 2.0 * KerbinDaySeconds
            };
            rival.RivalProgram.ScienceLaunchPreparation = new ScienceLaunchPreparationState
            {
                Subject = scienceSubject,
                PlannedScienceReward = 5.0,
                LaunchProgressPercent = 100,
                RequiredKerbals = 1,
                ReadyUniversalTime = KerbinDaySeconds
            };
            rival.RivalProgram.LiveMissions.Add(new RivalLiveMissionState
            {
                MissionSequence = 1,
                MissionType = RivalMissionType.Contract,
                ContractId = ObjectiveCatalogue.Control1Id,
                LocationId = CampaignSettings.RivalPreOrbitLocalLocationId,
                LaunchUniversalTime = 0.0,
                DurationDays = 5.0,
                CompletionUniversalTime = 5.0 * KerbinDaySeconds,
                Difficulty = 1,
                SuccessChancePercent = 90.0,
                AssignedKerbalCount = 1,
                OutcomeSeed = 0
            });

            RivalSimulation.Refresh(new List<AgencyState> { rival }, 5.0 * KerbinDaySeconds,
                new List<ObjectiveFundingContract> { waitingContract }, new List<SatelliteNetworkFundingContract>(),
                agency => scienceCandidates, subject => 5.0, new ZeroRandom());

            Equal(1, rival.RivalProgram.LiveMissions.Count);
            RivalLiveMissionState launchedMission = rival.RivalProgram.LiveMissions[0];
            Equal(RivalMissionType.Science, launchedMission.MissionType);
            Equal(5.0 * KerbinDaySeconds, launchedMission.LaunchUniversalTime);
            Equal(ObjectiveCatalogue.Control2Id, rival.NextMissionTargetId);
            Equal(100, rival.MissionProgressPercent);
            Equal(2.0 * KerbinDaySeconds, rival.NextMissionReadyUniversalTime);
        }

        private static void SharedScienceTieUsesStableAgencyIdOrder()
        {
            CampaignSettings.ResetToDefaults();
            ScienceSubjectKey subject = new ScienceSubjectKey(
                RivalTechCatalogue.CrewReportExperimentId, "Kerbin", "Landed", "Grasslands");
            AgencyState aster = CreateLiveScienceRival("aster", "Aster", subject, 100.0);
            AgencyState cobalt = CreateLiveScienceRival("cobalt", "Cobalt", subject, 100.0);
            double sharedRemainingScience = 10.0;

            Func<AgencyState, IList<RivalScienceSubjectCandidate>> captureScience = agency =>
                new List<RivalScienceSubjectCandidate>
                {
                    new RivalScienceSubjectCandidate(subject, "kerbin:grasslands", sharedRemainingScience)
                };
            Func<ScienceSubjectKey, double> consumeScience = consumedSubject =>
            {
                double awarded = sharedRemainingScience;
                sharedRemainingScience = 0.0;
                return awarded;
            };

            RivalSimulation.Refresh(new List<AgencyState> { cobalt, aster }, 100.0,
                new List<ObjectiveFundingContract>(), new List<SatelliteNetworkFundingContract>(),
                captureScience, consumeScience, new ZeroRandom());

            Equal(10.0, aster.RivalProgram.StoredScience);
            Equal(0.0, cobalt.RivalProgram.StoredScience);
            Require(aster.RivalProgram.CompletedScienceSubjects.Contains(subject),
                "The first rival should record the successful Science subject.");
            Require(cobalt.RivalProgram.CompletedScienceSubjects.Contains(subject),
                "The later exact-time rival should still record success even when zero Science remains.");
            Equal(0.0, sharedRemainingScience);
        }

        private static void SignedNegativeFundingProjectionCanMakeLaunchUnaffordable()
        {
            CampaignSettings.ResetToDefaults();
            double originalFacilityChance = CampaignSettings.RivalNormalLaunchFacilityLevel1Chance;
            try
            {
                // With both Level 1 facilities contributing 50%, expected successful checks are five days apart.
                CampaignSettings.RivalNormalLaunchFacilityLevel1Chance = 0.50;
                var rival = new AgencyState("aster", "Aster", false)
                {
                    Funds = 8000.0,
                    NextPayoutFunds = -5000.0,
                    NextMissionTargetId = ObjectiveCatalogue.DirectedPower1Id,
                    MissionProgressPercent = 60
                };

                int? estimatedDays = RivalSimulation.CalculateEstimatedLaunchDays(
                    rival,
                    0.0,
                    6.0 * KerbinDaySeconds,
                    90.0 * KerbinDaySeconds,
                    new List<ObjectiveFundingContract>(),
                    new List<SatelliteNetworkFundingContract>());

                Require(!estimatedDays.HasValue,
                    "A negative funding boundary before the second required progress step should make the launch unaffordable.");
            }
            finally
            {
                CampaignSettings.RivalNormalLaunchFacilityLevel1Chance = originalFacilityChance;
            }
        }

        private static void LargeTimeJumpProcessesStoredEventsChronologically()
        {
            CampaignSettings.ResetToDefaults();
            double originalNormalChance = CampaignSettings.RivalNormalLaunchFacilityLevel1Chance;
            double originalScienceChance = CampaignSettings.RivalScienceLaunchFacilityLevel1Chance;
            try
            {
                CampaignSettings.RivalNormalLaunchFacilityLevel1Chance = 0.50;
                CampaignSettings.RivalScienceLaunchFacilityLevel1Chance = 0.50;

                ObjectiveFundingContract directedPower = CreateOfferedObjectiveContract(ObjectiveCatalogue.DirectedPower1Id);
                ScienceSubjectKey subject = new ScienceSubjectKey(
                    RivalTechCatalogue.CrewReportExperimentId, "Kerbin", "Landed", "Grasslands");
                var scienceCandidates = new List<RivalScienceSubjectCandidate>
                {
                    new RivalScienceSubjectCandidate(subject, "kerbin:grasslands", 5.0)
                };
                var rival = new AgencyState("aster", "Aster", false)
                {
                    Funds = 100000.0,
                    NextMissionTargetId = ObjectiveCatalogue.DirectedPower1Id,
                    MissionProgressPercent = 0,
                    NextMissionProgressCheckUniversalTime = 5.0 * KerbinDaySeconds
                };
                rival.RivalProgram.ScienceLaunchPreparation = new ScienceLaunchPreparationState
                {
                    Subject = subject,
                    PlannedScienceReward = 5.0,
                    LaunchProgressPercent = 0,
                    NextProgressCheckUniversalTime = KerbinDaySeconds,
                    RequiredKerbals = 1,
                    ReadyUniversalTime = -1.0
                };

                RivalSimulation.Refresh(new List<AgencyState> { rival }, 10.0 * KerbinDaySeconds,
                    new List<ObjectiveFundingContract> { directedPower }, new List<SatelliteNetworkFundingContract>(),
                    agency => scienceCandidates, consumedSubject => 5.0, new ZeroRandom());

                Equal(40, rival.MissionProgressPercent);
                Equal(92000.0, rival.Funds);
                Equal(1, rival.RivalProgram.LiveMissions.Count);
                RivalLiveMissionState scienceMission = rival.RivalProgram.LiveMissions[0];
                Equal(RivalMissionType.Science, scienceMission.MissionType);
                Equal(10.0 * KerbinDaySeconds, scienceMission.LaunchUniversalTime);
                Require(rival.RivalProgram.ScienceLaunchPreparation == null,
                    "Ten successful daily checks should move Science preparation into a live mission.");
            }
            finally
            {
                CampaignSettings.RivalNormalLaunchFacilityLevel1Chance = originalNormalChance;
                CampaignSettings.RivalScienceLaunchFacilityLevel1Chance = originalScienceChance;
            }
        }

        private static ObjectiveFundingContract CreateOfferedObjectiveContract(string objectiveId)
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(objectiveId);
            Require(objective != null, "Expected objective definition: " + objectiveId);
            var contract = new ObjectiveFundingContract(objective.Id, objective.Name,
                objective.ObjectiveDescription, objective.BaseRewardFunds, null, objective.UnlockRule);
            contract.Offer();
            return contract;
        }

        private static SatelliteNetworkFundingContract FindNetwork(
            IList<SatelliteNetworkFundingContract> contracts, string contractId)
        {
            for (int contractIndex = 0; contractIndex < contracts.Count; contractIndex++)
            {
                SatelliteNetworkFundingContract contract = contracts[contractIndex];
                if (contract != null && string.Equals(contract.Id, contractId, StringComparison.OrdinalIgnoreCase))
                {
                    return contract;
                }
            }
            return null;
        }

        private static AgencyState CreateLiveScienceRival(
            string id, string name, ScienceSubjectKey subject, double completionUniversalTime)
        {
            var rival = new AgencyState(id, name, false);
            rival.RivalProgram.LiveMissions.Add(new RivalLiveMissionState
            {
                MissionSequence = 1,
                MissionType = RivalMissionType.Science,
                ScienceSubject = subject,
                LocationId = "kerbin:grasslands",
                LaunchUniversalTime = 0.0,
                DurationDays = 10.0,
                CompletionUniversalTime = completionUniversalTime,
                Difficulty = 1,
                SuccessChancePercent = 90.0,
                AssignedKerbalCount = 1,
                PlannedScienceReward = 10.0,
                OutcomeSeed = 0
            });
            return rival;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException("Expected '" + expected + "' but got '" + actual + "'.");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class ZeroRandom : Random
        {
            public override int Next()
            {
                return 0;
            }

            public override int Next(int maxValue)
            {
                return 0;
            }

            public override int Next(int minValue, int maxValue)
            {
                return minValue;
            }

            public override double NextDouble()
            {
                return 0.0;
            }

            protected override double Sample()
            {
                return 0.0;
            }
        }
    }
}

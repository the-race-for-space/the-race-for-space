using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Persistence;

namespace TheRaceForSpace.Tests.Persistence
{
    internal static class RivalProgrammePersistenceTests
    {
        public static void RunAll()
        {
            ExpandedProgrammeRoundTripsAndRestoresSilently();
            LegacyRivalNodeReceivesProgrammeDefaults();
            MalformedExpandedProgrammeStateFailsSafely();
        }

        private static void ExpandedProgrammeRoundTripsAndRestoresSilently()
        {
            var source = new AgencyState("aster", "Aster", false)
            {
                Funds = -12500.5,
                NextMissionTargetId = "mun-probe-orbit",
                MissionProgressPercent = 100,
                NextMissionProgressCheckUniversalTime = 9876.0,
                NextMissionReadyUniversalTime = 9500.0
            };
            source.RecordObjectiveCompletion("kerbin-probe-orbit", 4321.0);
            source.SetSatelliteCount("Kerbin", 2);

            RivalProgramState programme = source.RivalProgram;
            programme.StoredScience = 72.5;
            programme.KerbalsEmployed = 4;
            programme.PendingInsuranceFunds = 100000.0;
            programme.ScienceLaunchPreparation = new ScienceLaunchPreparationState
            {
                Subject = new ScienceSubjectKey("temperatureScan", "Kerbin", "Landed", "Grasslands"),
                PlannedScienceReward = 2.4,
                LaunchProgressPercent = 100,
                NextProgressCheckUniversalTime = 10100.0,
                RequiredKerbals = 1,
                ReadyUniversalTime = 10050.0
            };
            programme.LiveMissions.Add(new RivalLiveMissionState
            {
                MissionSequence = 7,
                MissionType = RivalMissionType.Contract,
                ContractId = "mun-probe-orbit",
                LocationId = "body:Mun",
                LaunchUniversalTime = 11000.0,
                DurationDays = 30.0,
                CompletionUniversalTime = 12000.0,
                Difficulty = 5,
                SuccessChancePercent = 70.0,
                AssignedKerbalCount = 0,
                PlannedScienceReward = 0.0,
                OutcomeSeed = 123456
            });
            programme.LiveMissions.Add(new RivalLiveMissionState
            {
                MissionSequence = 8,
                MissionType = RivalMissionType.Science,
                ScienceSubject = new ScienceSubjectKey(
                    "crewReport",
                    "Kerbin",
                    "FlyingHigh",
                    "Highlands"),
                LocationId = "kerbin:highlands",
                LaunchUniversalTime = 11100.0,
                DurationDays = 10.0,
                CompletionUniversalTime = 12100.0,
                Difficulty = 1,
                SuccessChancePercent = 90.0,
                AssignedKerbalCount = 1,
                PlannedScienceReward = 5.5,
                OutcomeSeed = -654321
            });
            programme.FacilityLevels[RivalFacilityType.ResearchAndDevelopment] = 2;
            programme.FacilityLevels[RivalFacilityType.MissionControl] = 2;
            programme.FacilityLevels[RivalFacilityType.TrackingStation] = 3;
            programme.FacilityConstruction.Add(new RivalFacilityConstructionState
            {
                Facility = RivalFacilityType.VehicleAssemblyBuilding,
                SourceLevel = 1,
                TargetLevel = 2,
                StartUniversalTime = 13000.0,
                CompletionUniversalTime = 14000.0,
                CostPaidFunds = 100000.0
            });
            programme.ResearchedTechIds.Add("basicRocketry");
            programme.ResearchedTechIds.Add("engineering101");
            programme.CurrentResearch = new RivalResearchProjectState
            {
                TechId = "advancedRocketry",
                ScienceCostPaid = 45.0,
                StartUniversalTime = 15000.0,
                ResearchReadyUniversalTime = 16000.0,
                EligibleCompletionFundingUniversalTime = 17000.0
            };
            programme.CompletedScienceSubjects.Add(new ScienceSubjectKey(
                "mysteryGoo",
                "Kerbin",
                "Landed",
                "Shores"));

            var saved = new RivalAgenciesSaveState();
            saved.Capture(new List<AgencyState> { source });
            var node = new ConfigNode();
            saved.Save(node);

            ConfigNode rivalNode = node.GetNode("RIVAL");
            Require(rivalNode != null, "Expanded rival state should write one RIVAL node.");
            Equal(1, rivalNode.GetNodes("SCIENCE_LAUNCH").Length);
            Equal(2, rivalNode.GetNodes("LIVE_MISSION").Length);
            Equal(9, rivalNode.GetNodes("FACILITY").Length);
            Equal(1, rivalNode.GetNodes("CONSTRUCTION").Length);
            Equal(3, rivalNode.GetNodes("RESEARCHED_TECH").Length);
            Equal(1, rivalNode.GetNodes("RESEARCH").Length);
            Equal(1, rivalNode.GetNodes("COMPLETED_SCIENCE").Length);

            var loaded = new RivalAgenciesSaveState();
            loaded.Load(node);
            var restored = new AgencyState("aster", "Aster Renamed", false)
            {
                Funds = 999.0,
                NextMissionTargetId = "stale-target",
                MissionProgressPercent = 10,
                NextMissionReadyUniversalTime = 22.0
            };
            restored.RecordObjectiveCompletion("stale-objective", 1.0);
            restored.SetSatelliteCount("Duna", 9);
            restored.RivalProgram.StoredScience = 999.0;
            restored.RivalProgram.KerbalsEmployed = 9;
            restored.RivalProgram.ResearchedTechIds.Add("stale-tech");
            restored.RivalProgram.FacilityLevels[RivalFacilityType.TrackingStation] = 2;

            int completionSignals = 0;
            Action<AgencyState, string> completionHandler = (agency, objectiveId) =>
            {
                if (object.ReferenceEquals(agency, restored))
                {
                    completionSignals++;
                }
            };
            AgencyState.ObjectiveCompletionRecorded += completionHandler;
            try
            {
                loaded.ApplyTo(new List<AgencyState> { restored });
            }
            finally
            {
                AgencyState.ObjectiveCompletionRecorded -= completionHandler;
            }

            Equal(0, completionSignals);
            Equal(-12500.5, restored.Funds);
            Equal(4321.0, restored.GetObjectiveCompletionTime("kerbin-probe-orbit"));
            Require(!restored.HasCompletedObjective("stale-objective"),
                "Restored rival objectives should replace stale runtime values.");
            Equal(2, restored.GetSatelliteCount("Kerbin"));
            Equal(0, restored.GetSatelliteCount("Duna"));
            Equal("mun-probe-orbit", restored.NextMissionTargetId);
            Equal(100, restored.MissionProgressPercent);
            Equal(9876.0, restored.NextMissionProgressCheckUniversalTime);
            Equal(9500.0, restored.NextMissionReadyUniversalTime);

            RivalProgramState restoredProgramme = restored.RivalProgram;
            Equal(72.5, restoredProgramme.StoredScience);
            Equal(4, restoredProgramme.KerbalsEmployed);
            Equal(100000.0, restoredProgramme.PendingInsuranceFunds);
            Require(restoredProgramme.ScienceLaunchPreparation != null,
                "Science preparation should round trip.");
            Require(restoredProgramme.ScienceLaunchPreparation.Subject.Equals(
                    new ScienceSubjectKey("temperatureScan", "Kerbin", "Landed", "Grasslands")),
                "Science preparation subject should round trip by explicit identity fields.");
            Equal(2.4, restoredProgramme.ScienceLaunchPreparation.PlannedScienceReward);
            Equal(100, restoredProgramme.ScienceLaunchPreparation.LaunchProgressPercent);
            Equal(10100.0, restoredProgramme.ScienceLaunchPreparation.NextProgressCheckUniversalTime);
            Equal(1, restoredProgramme.ScienceLaunchPreparation.RequiredKerbals);
            Equal(10050.0, restoredProgramme.ScienceLaunchPreparation.ReadyUniversalTime);

            Equal(2, restoredProgramme.LiveMissions.Count);
            RivalLiveMissionState contractMission = restoredProgramme.LiveMissions[0];
            Equal(7L, contractMission.MissionSequence);
            Equal(RivalMissionType.Contract, contractMission.MissionType);
            Equal("mun-probe-orbit", contractMission.ContractId);
            Equal("body:Mun", contractMission.LocationId);
            Equal(30.0, contractMission.DurationDays);
            Equal(5, contractMission.Difficulty);
            Equal(70.0, contractMission.SuccessChancePercent);
            Equal(123456, contractMission.OutcomeSeed);

            RivalLiveMissionState scienceMission = restoredProgramme.LiveMissions[1];
            Equal(8L, scienceMission.MissionSequence);
            Equal(RivalMissionType.Science, scienceMission.MissionType);
            Require(scienceMission.ScienceSubject.Equals(new ScienceSubjectKey(
                    "crewReport",
                    "Kerbin",
                    "FlyingHigh",
                    "Highlands")),
                "Live Science mission subject should round trip.");
            Equal(1, scienceMission.AssignedKerbalCount);
            Equal(5.5, scienceMission.PlannedScienceReward);
            Equal(-654321, scienceMission.OutcomeSeed);

            Equal(2, restoredProgramme.FacilityLevels[RivalFacilityType.ResearchAndDevelopment]);
            Equal(2, restoredProgramme.FacilityLevels[RivalFacilityType.MissionControl]);
            Equal(3, restoredProgramme.FacilityLevels[RivalFacilityType.TrackingStation]);
            Equal(1, restoredProgramme.FacilityLevels[RivalFacilityType.Runway]);
            Equal(1, restoredProgramme.FacilityConstruction.Count);
            RivalFacilityConstructionState construction = restoredProgramme.FacilityConstruction[0];
            Equal(RivalFacilityType.VehicleAssemblyBuilding, construction.Facility);
            Equal(1, construction.SourceLevel);
            Equal(2, construction.TargetLevel);
            Equal(13000.0, construction.StartUniversalTime);
            Equal(14000.0, construction.CompletionUniversalTime);
            Equal(100000.0, construction.CostPaidFunds);

            Equal(3, restoredProgramme.ResearchedTechIds.Count);
            Require(restoredProgramme.ResearchedTechIds.Contains(RivalProgramState.StartingTechId),
                "Start must remain researched after restore.");
            Require(restoredProgramme.ResearchedTechIds.Contains("basicRocketry"),
                "Researched tech should round trip.");
            Require(restoredProgramme.ResearchedTechIds.Contains("engineering101"),
                "Multiple researched techs should round trip.");
            Require(!restoredProgramme.ResearchedTechIds.Contains("stale-tech"),
                "Restore should replace stale researched-tech state.");

            Require(restoredProgramme.CurrentResearch != null,
                "Current research should round trip.");
            Equal("advancedRocketry", restoredProgramme.CurrentResearch.TechId);
            Equal(45.0, restoredProgramme.CurrentResearch.ScienceCostPaid);
            Equal(15000.0, restoredProgramme.CurrentResearch.StartUniversalTime);
            Equal(16000.0, restoredProgramme.CurrentResearch.ResearchReadyUniversalTime);
            Equal(17000.0, restoredProgramme.CurrentResearch.EligibleCompletionFundingUniversalTime);

            Equal(1, restoredProgramme.CompletedScienceSubjects.Count);
            Require(restoredProgramme.CompletedScienceSubjects.Contains(new ScienceSubjectKey(
                    "mysteryGoo",
                    "Kerbin",
                    "Landed",
                    "Shores")),
                "Completed Science subjects should round trip.");
        }

        private static void LegacyRivalNodeReceivesProgrammeDefaults()
        {
            var node = new ConfigNode();
            ConfigNode rivalNode = node.AddNode("RIVAL");
            rivalNode.AddValue("programId", "legacy");
            rivalNode.AddValue("funds", "25000");
            rivalNode.AddValue("nextMissionTargetId", "legacy-target");
            rivalNode.AddValue("launchProgressPercent", "40");
            rivalNode.AddValue("nextLaunchProgressCheckUniversalTime", "5000");
            ConfigNode objectiveNode = rivalNode.AddNode("OBJECTIVE_COMPLETION");
            objectiveNode.AddValue("id", "legacy-objective");
            objectiveNode.AddValue("universalTime", "1234");
            ConfigNode satelliteNode = rivalNode.AddNode("SATELLITE");
            satelliteNode.AddValue("body", "Kerbin");
            satelliteNode.AddValue("count", "2");

            var loaded = new RivalAgenciesSaveState();
            loaded.Load(node);
            var restored = new AgencyState("legacy", "Legacy Rival", false)
            {
                NextMissionReadyUniversalTime = 9999.0
            };
            RivalProgramState programme = restored.RivalProgram;
            programme.StoredScience = 50.0;
            programme.KerbalsEmployed = 3;
            programme.PendingInsuranceFunds = 50000.0;
            programme.ResearchedTechIds.Add("stale-tech");
            programme.FacilityLevels[RivalFacilityType.TrackingStation] = 3;
            programme.CompletedScienceSubjects.Add(new ScienceSubjectKey(
                "crewReport",
                "Kerbin",
                "Landed",
                "Grasslands"));
            programme.ScienceLaunchPreparation = new ScienceLaunchPreparationState
            {
                Subject = new ScienceSubjectKey("crewReport", "Kerbin", "Landed", "Shores")
            };
            programme.CurrentResearch = new RivalResearchProjectState
            {
                TechId = "stale-tech",
                ScienceCostPaid = 5.0,
                StartUniversalTime = 1.0,
                ResearchReadyUniversalTime = 2.0,
                EligibleCompletionFundingUniversalTime = 3.0
            };
            programme.FacilityConstruction.Add(new RivalFacilityConstructionState
            {
                Facility = RivalFacilityType.Runway,
                SourceLevel = 1,
                TargetLevel = 2,
                StartUniversalTime = 1.0,
                CompletionUniversalTime = 2.0,
                CostPaidFunds = 100000.0
            });

            loaded.ApplyTo(new List<AgencyState> { restored });

            Equal(25000.0, restored.Funds);
            Equal("legacy-target", restored.NextMissionTargetId);
            Equal(40, restored.MissionProgressPercent);
            Equal(5000.0, restored.NextMissionProgressCheckUniversalTime);
            Equal(-1.0, restored.NextMissionReadyUniversalTime);
            Equal(1234.0, restored.GetObjectiveCompletionTime("legacy-objective"));
            Equal(2, restored.GetSatelliteCount("Kerbin"));

            Equal(0.0, programme.StoredScience);
            Equal(1, programme.KerbalsEmployed);
            Equal(0.0, programme.PendingInsuranceFunds);
            Require(programme.ScienceLaunchPreparation == null,
                "Old saves should default to no Science preparation.");
            Equal(0, programme.LiveMissions.Count);
            Require(programme.CurrentResearch == null,
                "Old saves should default to no active research.");
            Equal(0, programme.FacilityConstruction.Count);
            Equal(0, programme.CompletedScienceSubjects.Count);
            Equal(1, programme.ResearchedTechIds.Count);
            Require(programme.ResearchedTechIds.Contains(RivalProgramState.StartingTechId),
                "Old saves should receive Start as the only researched tech.");

            Array facilities = Enum.GetValues(typeof(RivalFacilityType));
            for (int facilityIndex = 0; facilityIndex < facilities.Length; facilityIndex++)
            {
                RivalFacilityType facility = (RivalFacilityType)facilities.GetValue(facilityIndex);
                Equal(1, programme.FacilityLevels[facility]);
            }
        }

        private static void MalformedExpandedProgrammeStateFailsSafely()
        {
            var node = new ConfigNode();
            ConfigNode rivalNode = node.AddNode("RIVAL");
            rivalNode.AddValue("programId", "aster");
            rivalNode.AddValue("funds", "-500");
            rivalNode.AddValue("storedScience", "-4");
            rivalNode.AddValue("kerbalsEmployed", "-2");
            rivalNode.AddValue("pendingInsuranceFunds", "NaN");
            rivalNode.AddValue("launchReadyUniversalTime", "NaN");

            ConfigNode scienceLaunch = rivalNode.AddNode("SCIENCE_LAUNCH");
            scienceLaunch.AddValue("experimentId", "crewReport");
            scienceLaunch.AddValue("bodyName", "Kerbin");
            // Missing situation makes this preparation identity unusable.

            ConfigNode badLiveMission = rivalNode.AddNode("LIVE_MISSION");
            badLiveMission.AddValue("missionSequence", "1");
            badLiveMission.AddValue("missionType", "Contract");
            badLiveMission.AddValue("contractId", "bad-contract");
            badLiveMission.AddValue("locationId", "body:Mun");
            badLiveMission.AddValue("launchUniversalTime", "100");
            badLiveMission.AddValue("durationDays", "30");
            badLiveMission.AddValue("completionUniversalTime", "200");
            badLiveMission.AddValue("difficulty", "99");
            badLiveMission.AddValue("successChancePercent", "70");
            badLiveMission.AddValue("assignedKerbalCount", "0");
            badLiveMission.AddValue("outcomeSeed", "5");

            ConfigNode validFacility = rivalNode.AddNode("FACILITY");
            validFacility.AddValue("facility", "TrackingStation");
            validFacility.AddValue("level", "2");
            ConfigNode badFacility = rivalNode.AddNode("FACILITY");
            badFacility.AddValue("facility", "MissionControl");
            badFacility.AddValue("level", "99");

            ConfigNode badConstruction = rivalNode.AddNode("CONSTRUCTION");
            badConstruction.AddValue("facility", "Runway");
            badConstruction.AddValue("sourceLevel", "3");
            badConstruction.AddValue("targetLevel", "4");
            badConstruction.AddValue("startUniversalTime", "100");
            badConstruction.AddValue("completionUniversalTime", "200");
            badConstruction.AddValue("costPaidFunds", "250000");

            ConfigNode researchedTech = rivalNode.AddNode("RESEARCHED_TECH");
            researchedTech.AddValue("id", "engineering101");
            rivalNode.AddNode("RESEARCHED_TECH").AddValue("id", string.Empty);

            ConfigNode badResearch = rivalNode.AddNode("RESEARCH");
            badResearch.AddValue("techId", "advancedRocketry");
            badResearch.AddValue("scienceCostPaid", "45");
            badResearch.AddValue("startUniversalTime", "300");
            badResearch.AddValue("researchReadyUniversalTime", "200");
            badResearch.AddValue("eligibleCompletionFundingUniversalTime", "400");

            ConfigNode badCompletedScience = rivalNode.AddNode("COMPLETED_SCIENCE");
            badCompletedScience.AddValue("experimentId", "mysteryGoo");
            badCompletedScience.AddValue("bodyName", "Kerbin");
            // Missing situation makes this completed subject unusable.

            var loaded = new RivalAgenciesSaveState();
            loaded.Load(node);
            var restored = new AgencyState("aster", "Aster", false);
            loaded.ApplyTo(new List<AgencyState> { restored });

            // Negative Funds is valid rival state; malformed new programme values fall back safely.
            Equal(-500.0, restored.Funds);
            Equal(-1.0, restored.NextMissionReadyUniversalTime);
            Equal(0.0, restored.RivalProgram.StoredScience);
            Equal(1, restored.RivalProgram.KerbalsEmployed);
            Equal(0.0, restored.RivalProgram.PendingInsuranceFunds);
            Require(restored.RivalProgram.ScienceLaunchPreparation == null,
                "Malformed Science preparation should be ignored.");
            Equal(0, restored.RivalProgram.LiveMissions.Count);
            Equal(2, restored.RivalProgram.FacilityLevels[RivalFacilityType.TrackingStation]);
            Equal(1, restored.RivalProgram.FacilityLevels[RivalFacilityType.MissionControl]);
            Equal(0, restored.RivalProgram.FacilityConstruction.Count);
            Require(restored.RivalProgram.CurrentResearch == null,
                "Chronologically invalid research state should be ignored.");
            Equal(0, restored.RivalProgram.CompletedScienceSubjects.Count);
            Require(restored.RivalProgram.ResearchedTechIds.Contains(RivalProgramState.StartingTechId),
                "Malformed save data must not remove the compatibility Start tech.");
            Require(restored.RivalProgram.ResearchedTechIds.Contains("engineering101"),
                "Valid researched-tech nodes should survive beside malformed neighbours.");
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

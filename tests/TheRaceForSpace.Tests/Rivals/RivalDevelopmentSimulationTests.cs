using System;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Core;
using TheRaceForSpace.Rivals;

namespace TheRaceForSpace.Tests.Rivals
{
    internal static class RivalDevelopmentSimulationTests
    {
        private const double KerbinDaySeconds = 21600.0;

        public static void RunAll()
        {
            FacilityCapabilitiesFollowApprovedLevels();
            CrewAvailabilityHiringAndPayrollUseRosterState();
            FundingBreakdownIsAuthoritativeAndAllowsNegativeNet();
            FacilityConstructionSelectsChargesWaitsAndCompletes();
            ResearchSelectsCheapestEligibleTierAndCompletes();
            ResearchRespectsFacilityLimitsAndFundingBoundaryCompletion();
            CompletionSignalsReportOnlyRealDevelopment();
        }

        private static void FacilityCapabilitiesFollowApprovedLevels()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);

            Equal(1, RivalDevelopmentSimulation.GetFacilityLevel(
                rival,
                RivalFacilityType.Administration));
            Near(10000.0, RivalDevelopmentSimulation.GetAdministrationBaseIncomeFunds(rival));
            Equal(3, RivalDevelopmentSimulation.GetKerbalRosterLimit(rival));
            Equal(3, RivalDevelopmentSimulation.GetSatelliteLimit(rival));
            Near(90.0, RivalDevelopmentSimulation.GetResearchScienceCostLimit(rival));
            Equal(1, RivalDevelopmentSimulation.GetTrackingStationLevel(rival));
            Near(0.30, RivalDevelopmentSimulation.GetNormalLaunchProgressChance(rival));
            Near(0.40, RivalDevelopmentSimulation.GetScienceLaunchProgressChance(rival));

            RivalProgramState programme = rival.RivalProgram;
            programme.FacilityLevels[RivalFacilityType.Administration] = 2;
            programme.FacilityLevels[RivalFacilityType.AstronautComplex] = 2;
            programme.FacilityLevels[RivalFacilityType.MissionControl] = 2;
            programme.FacilityLevels[RivalFacilityType.ResearchAndDevelopment] = 2;
            programme.FacilityLevels[RivalFacilityType.TrackingStation] = 2;
            programme.FacilityLevels[RivalFacilityType.VehicleAssemblyBuilding] = 2;
            programme.FacilityLevels[RivalFacilityType.LaunchPad] = 3;
            programme.FacilityLevels[RivalFacilityType.SpaceplaneHangar] = 2;
            programme.FacilityLevels[RivalFacilityType.Runway] = 3;

            Near(20000.0, RivalDevelopmentSimulation.GetAdministrationBaseIncomeFunds(rival));
            Equal(8, RivalDevelopmentSimulation.GetKerbalRosterLimit(rival));
            Equal(8, RivalDevelopmentSimulation.GetSatelliteLimit(rival));
            Near(300.0, RivalDevelopmentSimulation.GetResearchScienceCostLimit(rival));
            Equal(2, RivalDevelopmentSimulation.GetTrackingStationLevel(rival));
            Near(0.39, RivalDevelopmentSimulation.GetNormalLaunchProgressChance(rival));
            Near(0.49, RivalDevelopmentSimulation.GetScienceLaunchProgressChance(rival));

            programme.FacilityLevels[RivalFacilityType.Administration] = 3;
            programme.FacilityLevels[RivalFacilityType.AstronautComplex] = 3;
            programme.FacilityLevels[RivalFacilityType.MissionControl] = 3;
            programme.FacilityLevels[RivalFacilityType.ResearchAndDevelopment] = 3;
            programme.FacilityLevels[RivalFacilityType.TrackingStation] = 3;

            Near(40000.0, RivalDevelopmentSimulation.GetAdministrationBaseIncomeFunds(rival));
            Equal(0, RivalDevelopmentSimulation.GetKerbalRosterLimit(rival));
            Equal(0, RivalDevelopmentSimulation.GetSatelliteLimit(rival));
            Near(0.0, RivalDevelopmentSimulation.GetResearchScienceCostLimit(rival));
            Equal(3, RivalDevelopmentSimulation.GetTrackingStationLevel(rival));

            var player = new AgencyState("player", "Player", true);
            Equal(0, RivalDevelopmentSimulation.GetFacilityLevel(
                player,
                RivalFacilityType.Administration));
            Near(0.0, RivalDevelopmentSimulation.GetAdministrationBaseIncomeFunds(player));
        }

        private static void CrewAvailabilityHiringAndPayrollUseRosterState()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false)
            {
                Funds = 99999.0
            };
            RivalProgramState programme = rival.RivalProgram;
            programme.LiveMissions.Add(new RivalLiveMissionState
            {
                MissionSequence = 1,
                MissionType = RivalMissionType.Contract,
                ContractId = "kerbin-crewed-orbit",
                AssignedKerbalCount = 1
            });

            Equal(1, RivalDevelopmentSimulation.GetKerbalsOnMission(rival));
            Equal(0, RivalDevelopmentSimulation.GetKerbalsAvailable(rival));
            Equal(1, RivalDevelopmentSimulation.GetKerbalsRequiredToHire(rival, 1));
            Near(100000.0, RivalDevelopmentSimulation.GetKerbalHireCostFunds(rival, 1));
            Require(!RivalDevelopmentSimulation.CanHireMissingKerbals(rival, 1),
                "A rival should not hire a missing Kerbal without the full hire cost.");

            rival.Funds = 100000.0;
            Require(RivalDevelopmentSimulation.CanHireMissingKerbals(rival, 1),
                "A Level 1 Astronaut Complex should permit the second employed Kerbal.");
            int hiredKerbals;
            Require(RivalDevelopmentSimulation.TryHireMissingKerbals(rival, 1, out hiredKerbals),
                "A fully affordable missing Kerbal should be hired.");
            Equal(1, hiredKerbals);
            Equal(2, programme.KerbalsEmployed);
            Near(0.0, rival.Funds);
            Equal(1, RivalDevelopmentSimulation.GetKerbalsAvailable(rival));

            rival.Funds = 200000.0;
            Equal(2, RivalDevelopmentSimulation.GetKerbalsRequiredToHire(rival, 3));
            Require(!RivalDevelopmentSimulation.CanHireMissingKerbals(rival, 3),
                "Level 1's three-Kerbal roster cap should block a fourth employed Kerbal.");

            programme.FacilityLevels[RivalFacilityType.AstronautComplex] = 2;
            Require(RivalDevelopmentSimulation.TryHireMissingKerbals(rival, 3, out hiredKerbals),
                "Level 2 should allow hiring the two missing Kerbals for a future three-crew mission.");
            Equal(2, hiredKerbals);
            Equal(4, programme.KerbalsEmployed);
            Equal(3, RivalDevelopmentSimulation.GetKerbalsAvailable(rival));
            Near(0.0, rival.Funds);
            Near(40000.0, RivalDevelopmentSimulation.GetKerbalPayrollFunds(rival));
        }

        private static void FundingBreakdownIsAuthoritativeAndAllowsNegativeNet()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false)
            {
                Funds = 12345.0
            };
            rival.RivalProgram.FacilityLevels[RivalFacilityType.Administration] = 2;
            rival.RivalProgram.KerbalsEmployed = 3;
            rival.RivalProgram.PendingInsuranceFunds = 50000.0;

            RivalFundingBreakdown breakdown = RivalDevelopmentSimulation.CalculateFundingBreakdown(
                rival,
                10000.0,
                5000.0);

            Near(20000.0, breakdown.BaseIncome);
            Near(10000.0, breakdown.ObjectiveIncome);
            Near(5000.0, breakdown.SatelliteIncome);
            Near(35000.0, breakdown.GrossIncome);
            Near(30000.0, breakdown.KerbalPayroll);
            Near(50000.0, breakdown.InsuranceDeduction);
            Near(-45000.0, breakdown.NetPayout);

            Near(12345.0, rival.Funds);
            Near(50000.0, rival.RivalProgram.PendingInsuranceFunds);
            Require(breakdown.NetPayout < 0.0,
                "The authoritative funding calculation must preserve signed negative payouts.");
        }

        private static void FacilityConstructionSelectsChargesWaitsAndCompletes()
        {
            CampaignSettings.ResetToDefaults();

            var selectionRival = new AgencyState("selector", "Selector", false)
            {
                Funds = 100000.0
            };
            RivalFacilityConstructionState selectedConstruction =
                RivalDevelopmentSimulation.TryStartFacilityConstruction(
                    selectionRival,
                    500.0,
                    new FixedRandom(8));
            Require(selectedConstruction != null, "An affordable Level 1 facility should be selected.");
            Equal(RivalFacilityType.TrackingStation, selectedConstruction.Facility);

            var rival = new AgencyState("aster", "Aster", false);
            RivalProgramState programme = rival.RivalProgram;
            SetAllFacilities(programme, 3);
            programme.FacilityLevels[RivalFacilityType.VehicleAssemblyBuilding] = 1;
            rival.Funds = 99999.0;
            Require(RivalDevelopmentSimulation.TryStartFacilityConstruction(
                    rival,
                    1000.0,
                    new FixedRandom(0)) == null,
                "Construction should not begin without the full upgrade cost.");

            rival.Funds = 100000.0;
            RivalFacilityConstructionState level2Construction =
                RivalDevelopmentSimulation.TryStartFacilityConstruction(
                    rival,
                    1000.0,
                    new FixedRandom(0));
            Require(level2Construction != null, "The one affordable upgrade should start.");
            Equal(RivalFacilityType.VehicleAssemblyBuilding, level2Construction.Facility);
            Equal(1, level2Construction.SourceLevel);
            Equal(2, level2Construction.TargetLevel);
            Near(100000.0, level2Construction.CostPaidFunds);
            Near(0.0, rival.Funds);
            Near(
                1000.0 + (180.0 * KerbinDaySeconds),
                level2Construction.CompletionUniversalTime);
            Equal(1, RivalDevelopmentSimulation.GetFacilityLevel(
                rival,
                RivalFacilityType.VehicleAssemblyBuilding));
            Near(0.36, RivalDevelopmentSimulation.GetNormalLaunchProgressChance(rival));

            rival.Funds = 1000000.0;
            Require(RivalDevelopmentSimulation.TryStartFacilityConstruction(
                    rival,
                    2000.0,
                    new FixedRandom(0)) == null,
                "Only one rival facility construction project may be active initially.");
            Require(!RivalDevelopmentSimulation.CompleteDueFacilityConstruction(
                    rival,
                    level2Construction.CompletionUniversalTime - 1.0),
                "The old facility level must remain active for the full construction period.");
            Require(RivalDevelopmentSimulation.CompleteDueFacilityConstruction(
                    rival,
                    level2Construction.CompletionUniversalTime),
                "Construction should complete on the first funding boundary at or after its completion UT.");
            Equal(2, RivalDevelopmentSimulation.GetFacilityLevel(
                rival,
                RivalFacilityType.VehicleAssemblyBuilding));
            Equal(0, programme.FacilityConstruction.Count);
            Near(0.39, RivalDevelopmentSimulation.GetNormalLaunchProgressChance(rival));

            SetAllFacilities(programme, 3);
            programme.FacilityLevels[RivalFacilityType.VehicleAssemblyBuilding] = 2;
            rival.Funds = 250000.0;
            RivalFacilityConstructionState level3Construction =
                RivalDevelopmentSimulation.TryStartFacilityConstruction(
                    rival,
                    5000.0,
                    new FixedRandom(0));
            Require(level3Construction != null, "The Level 2 to 3 upgrade should start when fully affordable.");
            Equal(2, level3Construction.SourceLevel);
            Equal(3, level3Construction.TargetLevel);
            Near(250000.0, level3Construction.CostPaidFunds);
            Near(
                5000.0 + (270.0 * KerbinDaySeconds),
                level3Construction.CompletionUniversalTime);
            Require(RivalDevelopmentSimulation.CompleteDueFacilityConstruction(
                    rival,
                    level3Construction.CompletionUniversalTime),
                "The Level 3 upgrade should apply when due.");
            Equal(3, RivalDevelopmentSimulation.GetFacilityLevel(
                rival,
                RivalFacilityType.VehicleAssemblyBuilding));
        }

        private static void ResearchSelectsCheapestEligibleTierAndCompletes()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);
            rival.RivalProgram.StoredScience = 5.0;

            const double fundingBoundaryUniversalTime = 2000.0;
            RivalResearchProjectState research = RivalDevelopmentSimulation.TryStartResearch(
                rival,
                fundingBoundaryUniversalTime,
                new FixedRandom(1));

            Require(research != null, "A rival with 5 Science should start one of the two 5-Science nodes.");
            Equal("engineering101", research.TechId);
            Near(5.0, research.ScienceCostPaid);
            Near(0.0, rival.RivalProgram.StoredScience);
            Near(
                fundingBoundaryUniversalTime + (90.0 * KerbinDaySeconds),
                research.ResearchReadyUniversalTime);
            Near(research.ResearchReadyUniversalTime, research.EligibleCompletionFundingUniversalTime);
            Require(RivalDevelopmentSimulation.TryStartResearch(
                    rival,
                    fundingBoundaryUniversalTime,
                    new FixedRandom(0)) == null,
                "A second research project must not start while one is active.");

            Require(!RivalDevelopmentSimulation.CompleteDueResearch(
                    rival,
                    research.EligibleCompletionFundingUniversalTime - 1.0),
                "Research should not complete before its eligible funding boundary.");
            Require(RivalDevelopmentSimulation.CompleteDueResearch(
                    rival,
                    research.EligibleCompletionFundingUniversalTime),
                "Research should complete at its eligible funding boundary.");
            Require(rival.RivalProgram.ResearchedTechIds.Contains("engineering101"),
                "Completed research should add the stable tech ID to the rival state.");
            Require(rival.RivalProgram.CurrentResearch == null,
                "Completed research should clear the active project.");

            rival.RivalProgram.StoredScience = 20.0;
            RivalResearchProjectState nextResearch = RivalDevelopmentSimulation.TryStartResearch(
                rival,
                research.EligibleCompletionFundingUniversalTime,
                new FixedRandom(0));
            Require(nextResearch != null, "A later funding boundary should be able to select another project.");
            Equal("basicRocketry", nextResearch.TechId);
            Near(5.0, nextResearch.ScienceCostPaid);
        }

        private static void ResearchRespectsFacilityLimitsAndFundingBoundaryCompletion()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);
            RivalProgramState programme = rival.RivalProgram;
            programme.StoredScience = 1000.0;
            programme.ResearchedTechIds.Add("scienceTech");

            RivalTechNodeDefinition advancedScience = RivalTechCatalogue.GetById("advScienceTech");
            programme.FacilityLevels[RivalFacilityType.ResearchAndDevelopment] = 2;
            Require(!RivalDevelopmentSimulation.IsTechEligibleForResearch(rival, advancedScience),
                "R&D Level 2 should block the 550-Science tier even when its ancestry is satisfied.");

            CampaignSettings.RivalResearchAndDevelopmentLevel2ScienceCostLimit = 0.0;
            Require(!RivalDevelopmentSimulation.IsTechEligibleForResearch(rival, advancedScience),
                "A zero lower-level cost limit must not be interpreted as the Level 3 unlimited sentinel.");

            programme.FacilityLevels[RivalFacilityType.ResearchAndDevelopment] = 3;
            Require(RivalDevelopmentSimulation.IsTechEligibleForResearch(rival, advancedScience),
                "R&D Level 3 should permit an affordable 550-Science node with satisfied ancestry.");

            CampaignSettings.ResetToDefaults();
            var timingRival = new AgencyState("delta", "Delta", false);
            timingRival.RivalProgram.StoredScience = 5.0;
            CampaignSettings.RivalResearchDurationDays = 100.0;
            CampaignSettings.FundingIntervalDays = 90.0;

            const double startFundingUniversalTime = 3000.0;
            RivalResearchProjectState timingResearch = RivalDevelopmentSimulation.TryStartResearch(
                timingRival,
                startFundingUniversalTime,
                new FixedRandom(0));
            Require(timingResearch != null, "Timing test should start a valid 5-Science project.");
            Near(
                startFundingUniversalTime + (100.0 * KerbinDaySeconds),
                timingResearch.ResearchReadyUniversalTime);
            Near(
                startFundingUniversalTime + (180.0 * KerbinDaySeconds),
                timingResearch.EligibleCompletionFundingUniversalTime);
            Require(!RivalDevelopmentSimulation.CompleteDueResearch(
                    timingRival,
                    timingResearch.ResearchReadyUniversalTime),
                "A ready project must still wait for the first eligible campaign funding boundary.");
            Require(RivalDevelopmentSimulation.CompleteDueResearch(
                    timingRival,
                    timingResearch.EligibleCompletionFundingUniversalTime),
                "The first funding boundary on or after readiness should complete research.");

            CampaignSettings.ResetToDefaults();
        }

        private static void CompletionSignalsReportOnlyRealDevelopment()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("signal", "Signal Agency", false);
            RivalProgramState programme = rival.RivalProgram;
            SetAllFacilities(programme, 3);
            programme.FacilityLevels[RivalFacilityType.TrackingStation] = 1;

            var construction = new RivalFacilityConstructionState
            {
                Facility = RivalFacilityType.TrackingStation,
                SourceLevel = 1,
                TargetLevel = 2,
                StartUniversalTime = 0.0,
                CompletionUniversalTime = 100.0,
                CostPaidFunds = 100000.0
            };
            programme.FacilityConstruction.Add(construction);
            programme.CurrentResearch = new RivalResearchProjectState
            {
                TechId = "engineering101",
                ScienceCostPaid = 5.0,
                StartUniversalTime = 0.0,
                ResearchReadyUniversalTime = 100.0,
                EligibleCompletionFundingUniversalTime = 100.0
            };

            int researchEventCount = 0;
            int facilityEventCount = 0;
            AgencyState observedResearchAgency = null;
            RivalTechNodeDefinition observedTech = null;
            RivalFacilityConstructionState observedConstruction = null;
            bool researchStateWasFinalized = false;
            bool constructionStateWasFinalized = false;

            Action<AgencyState, RivalTechNodeDefinition> researchHandler =
                delegate(AgencyState completedAgency, RivalTechNodeDefinition techNode)
                {
                    researchEventCount++;
                    observedResearchAgency = completedAgency;
                    observedTech = techNode;
                    researchStateWasFinalized = completedAgency.RivalProgram.CurrentResearch == null
                        && completedAgency.RivalProgram.ResearchedTechIds.Contains(techNode.Id);
                };
            Action<AgencyState, RivalFacilityConstructionState> constructionHandler =
                delegate(AgencyState completedAgency, RivalFacilityConstructionState completedConstruction)
                {
                    facilityEventCount++;
                    observedConstruction = completedConstruction;
                    constructionStateWasFinalized = RivalDevelopmentSimulation.GetFacilityLevel(
                            completedAgency,
                            completedConstruction.Facility) == completedConstruction.TargetLevel
                        && !completedAgency.RivalProgram.FacilityConstruction.Contains(completedConstruction);
                };

            RivalDevelopmentSimulation.ResearchCompleted += researchHandler;
            RivalDevelopmentSimulation.FacilityConstructionCompleted += constructionHandler;
            try
            {
                Require(!RivalDevelopmentSimulation.CompleteDueResearch(rival, 99.0),
                    "Research should not signal before its eligible completion boundary.");
                Require(!RivalDevelopmentSimulation.CompleteDueFacilityConstruction(rival, 99.0),
                    "Construction should not signal before its completion boundary.");
                Equal(0, researchEventCount);
                Equal(0, facilityEventCount);

                Require(RivalDevelopmentSimulation.CompleteDueResearch(rival, 100.0),
                    "Due research should complete for the signal test.");
                Require(RivalDevelopmentSimulation.CompleteDueFacilityConstruction(rival, 100.0),
                    "Due construction should complete for the signal test.");
                Equal(1, researchEventCount);
                Equal(1, facilityEventCount);
                Require(object.ReferenceEquals(rival, observedResearchAgency),
                    "The research completion signal should identify the rival agency.");
                Equal("engineering101", observedTech == null ? null : observedTech.Id);
                Require(object.ReferenceEquals(construction, observedConstruction),
                    "The facility completion signal should retain the completed construction snapshot.");
                Require(researchStateWasFinalized,
                    "Research observers should see the finalized researched-tech state.");
                Require(constructionStateWasFinalized,
                    "Facility observers should see the finalized level and cleared construction state.");

                Require(!RivalDevelopmentSimulation.CompleteDueResearch(rival, 100.0),
                    "Completed research must not signal again at the same boundary.");
                Require(!RivalDevelopmentSimulation.CompleteDueFacilityConstruction(rival, 100.0),
                    "Completed construction must not signal again at the same boundary.");

                programme.CurrentResearch = new RivalResearchProjectState
                {
                    TechId = "missing-tech",
                    ScienceCostPaid = 1.0,
                    StartUniversalTime = 100.0,
                    ResearchReadyUniversalTime = 200.0,
                    EligibleCompletionFundingUniversalTime = 200.0
                };
                programme.FacilityConstruction.Add(new RivalFacilityConstructionState
                {
                    Facility = RivalFacilityType.TrackingStation,
                    SourceLevel = 1,
                    TargetLevel = 2,
                    StartUniversalTime = 100.0,
                    CompletionUniversalTime = 200.0,
                    CostPaidFunds = 100000.0
                });

                Require(RivalDevelopmentSimulation.CompleteDueResearch(rival, 200.0),
                    "Malformed due research should still be cleared safely.");
                Require(RivalDevelopmentSimulation.CompleteDueFacilityConstruction(rival, 200.0),
                    "A stale due construction record should still be cleared safely.");
                Equal(1, researchEventCount);
                Equal(1, facilityEventCount);
            }
            finally
            {
                RivalDevelopmentSimulation.ResearchCompleted -= researchHandler;
                RivalDevelopmentSimulation.FacilityConstructionCompleted -= constructionHandler;
            }
        }

        private static void SetAllFacilities(RivalProgramState programme, int level)
        {
            Array facilities = Enum.GetValues(typeof(RivalFacilityType));
            for (int facilityIndex = 0; facilityIndex < facilities.Length; facilityIndex++)
            {
                programme.FacilityLevels[(RivalFacilityType)facilities.GetValue(facilityIndex)] = level;
            }
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

        private sealed class FixedRandom : Random
        {
            private readonly int _selectionIndex;

            internal FixedRandom(int selectionIndex)
            {
                _selectionIndex = Math.Max(0, selectionIndex);
            }

            public override int Next(int maxValue)
            {
                if (maxValue <= 0)
                {
                    return 0;
                }

                return _selectionIndex % maxValue;
            }
        }
    }
}

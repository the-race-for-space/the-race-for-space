using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Core;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Rivals;

namespace TheRaceForSpace.Tests.Rivals
{
    internal static class RivalScienceSimulationTests
    {
        private const double KerbinDaySeconds = 21600.0;

        public static void RunAll()
        {
            ExperimentAvailabilityFollowsTechAndFacilityGates();
            KerbinSurfaceAndKscScienceStartUnlocked();
            SituationAndDestinationAccessFollowProgression();
            SelectionUsesOnlyValidUniqueSubjectsWithoutSpendingFunds();
            CurrentPreparationRevalidationKeepsCanonicalLocation();
            DailyProgressUsesScienceFacilitiesAndSetsReadyTime();
            EstimatedLaunchTimeUsesRemainingChecksAndChance();
            DepletedPreparationIsReplacedBeforeLaunch();
            ExpeditionRangeFollowsTrackingStation();
        }

        private static void ExperimentAvailabilityFollowsTechAndFacilityGates()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);

            IList<string> experimentIds = RivalScienceSimulation.GetAvailableExperimentIds(rival);
            Require(Contains(experimentIds, RivalTechCatalogue.CrewReportExperimentId),
                "Start should unlock Crew Report for rival Science.");
            Require(Contains(experimentIds, RivalTechCatalogue.MysteryGooExperimentId),
                "Start should unlock Mystery Goo for rival Science.");
            Require(!Contains(experimentIds, RivalTechCatalogue.TemperatureScanExperimentId),
                "Temperature Scan should wait for Engineering 101.");
            Require(!Contains(experimentIds, RivalScienceSimulation.EvaReportExperimentId),
                "EVA Report should wait for Astronaut Complex Level 2.");
            Require(!Contains(experimentIds, RivalScienceSimulation.SurfaceSampleExperimentId),
                "Surface Sample should wait for R&D Level 2.");

            rival.RivalProgram.ResearchedTechIds.Add("engineering101");
            rival.RivalProgram.FacilityLevels[RivalFacilityType.AstronautComplex] = 2;
            rival.RivalProgram.FacilityLevels[RivalFacilityType.ResearchAndDevelopment] = 2;
            experimentIds = RivalScienceSimulation.GetAvailableExperimentIds(rival);

            Require(Contains(experimentIds, RivalTechCatalogue.TemperatureScanExperimentId),
                "Engineering 101 should unlock Temperature Scan.");
            Require(Contains(experimentIds, RivalScienceSimulation.EvaReportExperimentId),
                "Astronaut Complex Level 2 should expose EVA Report without a tech node.");
            Require(Contains(experimentIds, RivalScienceSimulation.SurfaceSampleExperimentId),
                "R&D Level 2 should expose Surface Sample without a tech node.");

            var evaCandidate = Candidate(
                RivalScienceSimulation.EvaReportExperimentId,
                "Kerbin",
                "Landed",
                "Grasslands",
                "kerbin:grasslands",
                5.0);
            var surfaceSampleCandidate = Candidate(
                RivalScienceSimulation.SurfaceSampleExperimentId,
                "Kerbin",
                "Landed",
                "Grasslands",
                "kerbin:grasslands",
                5.0);
            Require(RivalScienceSimulation.IsCandidateEligible(rival, evaCandidate),
                "EVA Report should become eligible once its facility and location gates are satisfied.");
            Require(RivalScienceSimulation.IsCandidateEligible(rival, surfaceSampleCandidate),
                "Surface Sample should become eligible once R&D Level 2 and surface access are satisfied.");
        }

        private static void KerbinSurfaceAndKscScienceStartUnlocked()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);

            RivalScienceSubjectCandidate grasslands = Candidate(
                RivalTechCatalogue.CrewReportExperimentId,
                "Kerbin",
                "Landed",
                "Grasslands",
                "kerbin:grasslands",
                5.0);
            RivalScienceSubjectCandidate iceCaps = Candidate(
                RivalTechCatalogue.MysteryGooExperimentId,
                "Kerbin",
                "Landed",
                "Ice Caps",
                "kerbin:ice-caps",
                5.0);
            RivalScienceSubjectCandidate kscVab = Candidate(
                RivalTechCatalogue.CrewReportExperimentId,
                "Kerbin",
                "Landed",
                "VAB",
                "ksc:vab",
                5.0);
            RivalScienceSubjectCandidate flyingHigh = Candidate(
                RivalTechCatalogue.CrewReportExperimentId,
                "Kerbin",
                "Flying High",
                "Highlands",
                "kerbin:highlands",
                5.0);

            Require(RivalScienceSimulation.IsCandidateEligible(rival, grasslands),
                "Kerbin Grasslands Science should be available from campaign start.");
            Require(RivalScienceSimulation.IsCandidateEligible(rival, iceCaps),
                "Kerbin Ice Caps Science should not require Biome Contract progression.");
            Require(RivalScienceSimulation.IsCandidateEligible(rival, kscVab),
                "Supported KSC mini-biome Science should be available from campaign start.");
            Require(RivalScienceSimulation.IsCandidateEligible(rival, flyingHigh),
                "Kerbin Flying High Science should be available from campaign start when stock-valid.");
            Require(!rival.HasCompletedObjective(ObjectiveCatalogue.Biome1Id),
                "The test rival intentionally has no Biome Contract completion.");
        }

        private static void SituationAndDestinationAccessFollowProgression()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);

            RivalScienceSubjectCandidate kerbinOrbit = Candidate(
                RivalTechCatalogue.CrewReportExperimentId,
                "Kerbin",
                "Low Space",
                null,
                CampaignSettings.RivalKerbinOrbitLocationId,
                5.0);
            Require(!RivalScienceSimulation.IsCandidateEligible(rival, kerbinOrbit),
                "Kerbin Low Space should wait for Probe Orbit.");
            rival.RecordObjectiveCompletion(ObjectiveCatalogue.ProbeOrbitId, 100.0);
            Require(RivalScienceSimulation.IsCandidateEligible(rival, kerbinOrbit),
                "Probe Orbit should unlock both Kerbin space situations.");

            RivalScienceSubjectCandidate munOrbit = Candidate(
                RivalTechCatalogue.CrewReportExperimentId,
                "Mun",
                "HighSpace",
                null,
                "body:Mun",
                5.0);
            rival.RecordObjectiveCompletion(ObjectiveCatalogue.MunProbeOrbitId, 200.0);
            Require(!RivalScienceSimulation.IsCandidateEligible(rival, munOrbit),
                "Mun Science should still be outside Tracking Station Level 1 range.");
            rival.RivalProgram.FacilityLevels[RivalFacilityType.TrackingStation] = 2;
            Require(RivalScienceSimulation.IsCandidateEligible(rival, munOrbit),
                "Tracking Station Level 2 plus Mun Probe Orbit should unlock Mun space Science.");

            RivalScienceSubjectCandidate dunaOrbit = Candidate(
                RivalTechCatalogue.CrewReportExperimentId,
                "Duna",
                "LowSpace",
                null,
                "body:Duna",
                5.0);
            rival.RecordObjectiveCompletion(ObjectiveCatalogue.DunaProbeOrbitId, 300.0);
            Require(!RivalScienceSimulation.IsCandidateEligible(rival, dunaOrbit),
                "Interplanetary Science should remain outside Tracking Station Level 2 range.");
            rival.RivalProgram.FacilityLevels[RivalFacilityType.TrackingStation] = 3;
            Require(RivalScienceSimulation.IsCandidateEligible(rival, dunaOrbit),
                "Tracking Station Level 3 plus Duna Probe Orbit should unlock Duna space Science.");

            RivalScienceSubjectCandidate dunaSurface = Candidate(
                RivalTechCatalogue.CrewReportExperimentId,
                "Duna",
                "Landed",
                "Midlands",
                "body:Duna",
                5.0);
            Require(!RivalScienceSimulation.IsCandidateEligible(rival, dunaSurface),
                "Non-Kerbin surface Science should stay locked until landing Contracts exist.");

            RivalScienceSubjectCandidate sun = Candidate(
                RivalTechCatalogue.CrewReportExperimentId,
                "Sun",
                "LowSpace",
                null,
                "body:Sun",
                5.0);
            Require(!RivalScienceSimulation.IsCandidateEligible(rival, sun),
                "The Sun should not be a rival Science destination in the current content.");
        }

        private static void SelectionUsesOnlyValidUniqueSubjectsWithoutSpendingFunds()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false)
            {
                Funds = 123456.0
            };
            ScienceSubjectKey completedSubject = new ScienceSubjectKey(
                RivalTechCatalogue.CrewReportExperimentId,
                "Kerbin",
                "Landed",
                "Grasslands");
            ScienceSubjectKey liveSubject = new ScienceSubjectKey(
                RivalTechCatalogue.MysteryGooExperimentId,
                "Kerbin",
                "Landed",
                "Highlands");
            ScienceSubjectKey eligibleSubject = new ScienceSubjectKey(
                RivalTechCatalogue.CrewReportExperimentId,
                "Kerbin",
                "FlyingLow",
                string.Empty);
            rival.RivalProgram.CompletedScienceSubjects.Add(completedSubject);
            rival.RivalProgram.LiveMissions.Add(new RivalLiveMissionState
            {
                MissionType = RivalMissionType.Science,
                ScienceSubject = liveSubject,
                AssignedKerbalCount = 1
            });

            var candidates = new List<RivalScienceSubjectCandidate>
            {
                new RivalScienceSubjectCandidate(completedSubject, "kerbin:grasslands", 5.0),
                new RivalScienceSubjectCandidate(liveSubject, "kerbin:highlands", 5.0),
                Candidate(
                    RivalTechCatalogue.MysteryGooExperimentId,
                    "Kerbin",
                    "Landed",
                    "Mountains",
                    "kerbin:mountains",
                    0.0),
                new RivalScienceSubjectCandidate(eligibleSubject, "kerbin:grasslands", 7.5),
                new RivalScienceSubjectCandidate(eligibleSubject, "kerbin:highlands", 7.5)
            };

            IList<RivalScienceSubjectCandidate> eligible =
                RivalScienceSimulation.GetEligibleCandidates(rival, candidates);
            Equal(1, eligible.Count);
            Require(eligible[0].Subject.Equals(eligibleSubject),
                "Completed, live and depleted subjects should be excluded from selection.");
            Equal("kerbin:grasslands", eligible[0].LocationId);

            ScienceLaunchPreparationState preparation =
                RivalScienceSimulation.TrySelectNextPreparation(rival, 0.0, candidates);
            Require(preparation != null, "The only remaining valid Science subject should be selected.");
            Require(preparation.Subject.Equals(eligibleSubject),
                "Selection should use the eligible stock subject identity.");
            Near(7.5, preparation.PlannedScienceReward);
            Equal(0, preparation.LaunchProgressPercent);
            Equal(1, preparation.RequiredKerbals);
            Near(KerbinDaySeconds, preparation.NextProgressCheckUniversalTime);
            Near(-1.0, preparation.ReadyUniversalTime);
            Near(123456.0, rival.Funds);
            Require(RivalScienceSimulation.TrySelectNextPreparation(rival, 1.0, candidates) == null,
                "A rival may prepare only one Science Expedition at a time.");
        }

        private static void CurrentPreparationRevalidationKeepsCanonicalLocation()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);
            ScienceSubjectKey preparedSubject = new ScienceSubjectKey(
                RivalTechCatalogue.CrewReportExperimentId,
                "Kerbin",
                "FlyingLow",
                string.Empty);
            rival.RivalProgram.ScienceLaunchPreparation = new ScienceLaunchPreparationState
            {
                Subject = preparedSubject,
                PlannedScienceReward = 7.5,
                LaunchProgressPercent = 40,
                NextProgressCheckUniversalTime = KerbinDaySeconds,
                RequiredKerbals = 1,
                ReadyUniversalTime = -1.0
            };

            var candidates = new List<RivalScienceSubjectCandidate>
            {
                Candidate(
                    RivalTechCatalogue.MysteryGooExperimentId,
                    "Kerbin",
                    "Landed",
                    "Mountains",
                    "kerbin:mountains",
                    5.0),
                new RivalScienceSubjectCandidate(preparedSubject, "kerbin:highlands", 7.5),
                Candidate(
                    RivalTechCatalogue.MysteryGooExperimentId,
                    "Kerbin",
                    "Landed",
                    "Grasslands",
                    "kerbin:grasslands",
                    5.0),
                new RivalScienceSubjectCandidate(preparedSubject, "kerbin:grasslands", 7.5),
                new RivalScienceSubjectCandidate(preparedSubject, "kerbin:mountains", 7.5)
            };

            RivalScienceSubjectCandidate currentCandidate =
                RivalScienceSimulation.FindCurrentPreparationCandidate(rival, candidates);
            Require(currentCandidate != null,
                "The current preparation subject should remain valid during direct revalidation.");
            Equal("kerbin:grasslands", currentCandidate.LocationId);
            Require(currentCandidate.Subject.Equals(preparedSubject),
                "Direct revalidation should return the persisted preparation subject.");
        }

        private static void DailyProgressUsesScienceFacilitiesAndSetsReadyTime()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false)
            {
                Funds = 50000.0,
                NextMissionTargetId = ObjectiveCatalogue.DirectedPower1Id,
                MissionProgressPercent = 60,
                NextMissionProgressCheckUniversalTime = 999999.0
            };
            var candidate = Candidate(
                RivalTechCatalogue.CrewReportExperimentId,
                "Kerbin",
                "Landed",
                "Grasslands",
                "kerbin:grasslands",
                5.0);
            var candidates = new List<RivalScienceSubjectCandidate> { candidate };
            ScienceLaunchPreparationState preparation =
                RivalScienceSimulation.TrySelectNextPreparation(rival, 0.0, candidates);
            Require(preparation != null, "Test Science preparation should be selected.");

            Require(!RivalScienceSimulation.ProcessProgressCheck(
                    rival,
                    KerbinDaySeconds - 1.0,
                    candidates,
                    new SequenceRandom(0.0)),
                "Science progress should not run before its stored daily check.");

            var progressRandom = new SequenceRandom(0.39, 0.41, 0.0);
            Require(RivalScienceSimulation.ProcessProgressCheck(
                    rival,
                    KerbinDaySeconds,
                    candidates,
                    progressRandom),
                "The first daily check should be processed.");
            Equal(10, preparation.LaunchProgressPercent);
            Near(2.0 * KerbinDaySeconds, preparation.NextProgressCheckUniversalTime);
            Near(-1.0, preparation.ReadyUniversalTime);

            RivalScienceSimulation.ProcessProgressCheck(
                rival,
                2.0 * KerbinDaySeconds,
                candidates,
                progressRandom);
            Equal(10, preparation.LaunchProgressPercent);
            Near(3.0 * KerbinDaySeconds, preparation.NextProgressCheckUniversalTime);

            preparation.LaunchProgressPercent = 90;
            RivalScienceSimulation.ProcessProgressCheck(
                rival,
                3.0 * KerbinDaySeconds,
                candidates,
                progressRandom);
            Equal(100, preparation.LaunchProgressPercent);
            Near(3.0 * KerbinDaySeconds, preparation.ReadyUniversalTime);
            Near(0.0, preparation.NextProgressCheckUniversalTime);
            Require(RivalScienceSimulation.IsPreparationReady(rival),
                "100% Science preparation should become ready rather than auto-completing.");

            Near(50000.0, rival.Funds);
            Equal(ObjectiveCatalogue.DirectedPower1Id, rival.NextMissionTargetId);
            Equal(60, rival.MissionProgressPercent);
            Near(999999.0, rival.NextMissionProgressCheckUniversalTime);
        }

        private static void EstimatedLaunchTimeUsesRemainingChecksAndChance()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);
            rival.RivalProgram.ScienceLaunchPreparation = new ScienceLaunchPreparationState
            {
                Subject = new ScienceSubjectKey(
                    RivalTechCatalogue.CrewReportExperimentId,
                    "Kerbin",
                    "Landed",
                    "Grasslands"),
                LaunchProgressPercent = 0,
                RequiredKerbals = 1
            };

            double? estimatedDays = RivalScienceSimulation.CalculateEstimatedLaunchDays(rival);
            Require(estimatedDays.HasValue, "Default Science preparation should have an ETA.");
            Near(25.0, estimatedDays.Value);

            rival.RivalProgram.ScienceLaunchPreparation.LaunchProgressPercent = 50;
            estimatedDays = RivalScienceSimulation.CalculateEstimatedLaunchDays(rival);
            Require(estimatedDays.HasValue, "Partially complete Science preparation should have an ETA.");
            Near(12.5, estimatedDays.Value);

            rival.RivalProgram.FacilityLevels[RivalFacilityType.SpaceplaneHangar] = 3;
            rival.RivalProgram.FacilityLevels[RivalFacilityType.Runway] = 3;
            estimatedDays = RivalScienceSimulation.CalculateEstimatedLaunchDays(rival);
            Require(estimatedDays.HasValue, "Upgraded Science launch facilities should retain an ETA.");
            Near(5.0 / 0.52, estimatedDays.Value);

            rival.RivalProgram.ScienceLaunchPreparation.LaunchProgressPercent = 100;
            estimatedDays = RivalScienceSimulation.CalculateEstimatedLaunchDays(rival);
            Require(estimatedDays.HasValue, "Ready Science preparation should report zero days remaining.");
            Near(0.0, estimatedDays.Value);
        }

        private static void DepletedPreparationIsReplacedBeforeLaunch()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);
            ScienceSubjectKey depletedSubject = new ScienceSubjectKey(
                RivalTechCatalogue.CrewReportExperimentId,
                "Kerbin",
                "Landed",
                "Grasslands");
            ScienceSubjectKey replacementSubject = new ScienceSubjectKey(
                RivalTechCatalogue.MysteryGooExperimentId,
                "Kerbin",
                "Landed",
                "Highlands");
            rival.RivalProgram.ScienceLaunchPreparation = new ScienceLaunchPreparationState
            {
                Subject = depletedSubject,
                PlannedScienceReward = 5.0,
                LaunchProgressPercent = 100,
                NextProgressCheckUniversalTime = 0.0,
                RequiredKerbals = 1,
                ReadyUniversalTime = 100.0
            };

            var candidates = new List<RivalScienceSubjectCandidate>
            {
                new RivalScienceSubjectCandidate(depletedSubject, "kerbin:grasslands", 0.0),
                new RivalScienceSubjectCandidate(replacementSubject, "kerbin:highlands", 6.0)
            };
            Require(RivalScienceSimulation.RefreshPreparationTarget(
                    rival,
                    200.0,
                    candidates),
                "A fully depleted subject should cancel the preparation before launch.");

            ScienceLaunchPreparationState replacement = rival.RivalProgram.ScienceLaunchPreparation;
            Require(replacement != null, "A new valid subject should be selected immediately after cancellation.");
            Require(replacement.Subject.Equals(replacementSubject),
                "Replacement selection should use a currently valid subject.");
            Equal(0, replacement.LaunchProgressPercent);
            Near(-1.0, replacement.ReadyUniversalTime);
            Near(6.0, replacement.PlannedScienceReward);
            Require(replacement.NextProgressCheckUniversalTime > 200.0,
                "The replacement should start a fresh daily progress cadence.");

            var liveRival = new AgencyState("live", "Live", false);
            liveRival.RivalProgram.LiveMissions.Add(new RivalLiveMissionState
            {
                MissionType = RivalMissionType.Science,
                ScienceSubject = depletedSubject,
                LocationId = "kerbin:grasslands",
                CompletionUniversalTime = 1000.0
            });
            Require(RivalScienceSimulation.RefreshPreparationTarget(
                    liveRival,
                    200.0,
                    new List<RivalScienceSubjectCandidate>() ) == false,
                "No preparation exists to cancel on a rival whose Science mission is already live.");
            Equal(1, liveRival.RivalProgram.LiveMissions.Count);
        }

        private static void ExpeditionRangeFollowsTrackingStation()
        {
            CampaignSettings.ResetToDefaults();
            var rival = new AgencyState("aster", "Aster", false);

            Equal("Kerbin only", RivalScienceSimulation.GetExpeditionRange(rival));
            rival.RivalProgram.FacilityLevels[RivalFacilityType.TrackingStation] = 2;
            Equal("Kerbin, Mun and Minmus", RivalScienceSimulation.GetExpeditionRange(rival));
            rival.RivalProgram.FacilityLevels[RivalFacilityType.TrackingStation] = 3;
            Equal("Planets available", RivalScienceSimulation.GetExpeditionRange(rival));
        }

        private static RivalScienceSubjectCandidate Candidate(
            string experimentId,
            string bodyName,
            string situation,
            string biomeName,
            string locationId,
            double remainingScience)
        {
            return new RivalScienceSubjectCandidate(
                new ScienceSubjectKey(experimentId, bodyName, situation, biomeName),
                locationId,
                remainingScience);
        }

        private static bool Contains(IList<string> values, string expected)
        {
            if (values == null)
            {
                return false;
            }

            for (int valueIndex = 0; valueIndex < values.Count; valueIndex++)
            {
                if (string.Equals(values[valueIndex], expected, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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

        private sealed class SequenceRandom : Random
        {
            private readonly Queue<double> _values = new Queue<double>();

            public SequenceRandom(params double[] values)
            {
                if (values == null)
                {
                    return;
                }

                for (int valueIndex = 0; valueIndex < values.Length; valueIndex++)
                {
                    _values.Enqueue(values[valueIndex]);
                }
            }

            protected override double Sample()
            {
                return _values.Count == 0 ? 0.0 : _values.Dequeue();
            }
        }
    }
}

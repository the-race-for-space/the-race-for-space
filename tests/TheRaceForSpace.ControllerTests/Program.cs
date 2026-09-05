using System;

namespace TheRaceForSpace.ControllerTests
{
    internal static class Program
    {
        private static int _failures;

        private static int Main()
        {
            Run(
                "Unlock null rule is available from start",
                UnlockRuleEvaluatorTests.NullRuleIsAvailableFromStart);
            Run(
                "Unlock path requires all conditions",
                UnlockRuleEvaluatorTests.ConditionsInOnePathRequireAll);
            Run(
                "Unlock alternative path can satisfy rule",
                UnlockRuleEvaluatorTests.AlternativePathCanUnlock);
            Run(
                "Unlock agency scopes are respected",
                UnlockRuleEvaluatorTests.AgencyScopesAreRespected);
            Run(
                "Unlock required agency count must be met",
                UnlockRuleEvaluatorTests.RequiredAgencyCountMustBeMet);
            Run(
                "Unlock objectiveCompletion timestamps respect evaluation time",
                UnlockRuleEvaluatorTests.ObjectiveCompletionTimestampUsesEvaluationTime);
            Run(
                "Unlock time condition uses exact boundary",
                UnlockRuleEvaluatorTests.UniversalTimeConditionUsesExactBoundary);
            Run(
                "Unlock satellite count uses collective agency state",
                UnlockRuleEvaluatorTests.SatelliteCountConditionUsesCollectiveProgramState);
            Run(
                "Unlock condition progress matches rule evaluation",
                UnlockRuleEvaluatorTests.ConditionProgressMatchesRuleEvaluation);
            Run(
                "Unlock agency progress respects scope and time",
                UnlockRuleEvaluatorTests.ProgramConditionProgressUsesScopeAndTime);
            Run(
                "Unlock malformed rules fail closed",
                UnlockRuleEvaluatorTests.MalformedRulesFailClosed);
            Run(
                "Unlock invalid condition definitions fail fast",
                UnlockRuleEvaluatorTests.InvalidConditionDefinitionsFailFast);
            Run(
                "Rival selection requires offered contract",
                UnlockConsumerIntegrationTests.RivalSelectionRequiresOfferedContract);

            Run(
                "Controller uses configured rival count and starting funds",
                CampaignControllerTests.ConfiguredRivalCountAndStartingFundsAreUsed);
            Run(
                "Controller uses configured funding interval",
                CampaignControllerTests.ConfiguredFundingIntervalSetsNextBoundary);
            Run(
                "Controller scheduled refresh can skip player vessel observation",
                CampaignControllerTests.ScheduledRefreshCanSkipPlayerVesselObservation);
            Run(
                "Controller probe observation unlocks funding flow",
                CampaignControllerTests.ProbeObservationUnlocksFundingFlow);
            Run(
                "Controller Kerbin network progress unlocks moon funding",
                CampaignControllerTests.KerbinNetworkProgressUnlocksMoonFunding);
            Run(
                "Controller existing state pays at shared funding boundary",
                CampaignControllerTests.ExistingStatePaysAtSharedFundingBoundary);
            Run(
                "Controller restored overdue funding boundary is processed",
                CampaignControllerTests.RestoredOverdueFundingBoundaryIsProcessed);
            Run(
                "Controller boundary observation is not paid retroactively",
                CampaignControllerTests.BoundaryObservationIsNotPaidRetroactively);
            Run(
                "Controller projected payout cache rebuilds on refresh",
                CampaignControllerTests.ProjectedPayoutCacheRebuildsOnRefresh);

            Run(
                "Active starter plan opens with four cached contracts and reuses stable refresh",
                ActiveFlightContractPlanTests.OpeningOffersBuildInitialPlanAndStableRefreshReusesIt);
            Run(
                "Active starter plan waits for sponsor offer after rival unlock",
                ActiveFlightContractPlanTests.RivalUnlockDoesNotChangePlanUntilSponsorOffersContract);
            Run(
                "Active starter plan removes player-completed contract",
                ActiveFlightContractPlanTests.PlayerCompletionInvalidatesAndRemovesOnlyCompletedContract);
            Run(
                "Active starter plan removes expired contract",
                ActiveFlightContractPlanTests.PreOrbitExpiryInvalidatesPlanWithoutAnotherPreOrbitOffer);
            Run(
                "Active pre-orbit contract types request only needed telemetry",
                FlightTelemetryPlanTests.ActiveContractTypesRequestOnlyNeededTelemetry);

            Run(
                "Active offered Mass levels complete independently",
                ActivePreOrbitEvaluationTests.OfferedMassLevelsCompleteIndependently);
            Run(
                "Active offered Directed Power levels complete independently",
                ActivePreOrbitEvaluationTests.OfferedDirectedPowerLevelsCompleteIndependently);
            Run(
                "Active offered Biome levels remain independent within one launch",
                ActivePreOrbitEvaluationTests.OfferedBiomeLevelsRemainIndependentWithinOneLaunch);
            Run(
                "Active offered Control levels track and complete independently",
                ActivePreOrbitEvaluationTests.OfferedControlLevelsTrackAndCompleteIndependently);

            Run(
                "PreOrbit contracts open four offers and lock the remaining sixteen",
                FundingOfferControllerTests.PreOrbitContractsOpenFourInitialOffersAndLockRemaining);
            Run(
                "Rival starter completion unlocks the next level for sponsor review",
                FundingOfferControllerTests.RivalPreOrbitCompletionUnlocksNextLevelForSponsorReview);
            Run(
                "All unlocked starter levels are offered at sponsor review",
                FundingOfferControllerTests.UnlockedPreOrbitLevelsJoinSponsorReview);
            Run(
                "PreOrbit offers do not consume the normal objectiveCompletion limit",
                FundingOfferControllerTests.PreOrbitOffersDoNotConsumeNormalObjectiveLimit);
            Run(
                "Any starter level five offers Probe Orbit",
                FundingOfferControllerTests.AnyPreOrbitLevelFiveOffersProbeOrbit);
            Run(
                "Funding offers wait for sponsor review",
                FundingOfferControllerTests.UnlockedFundingWaitsForFundingReview);
            Run(
                "Funding review does not cascade completed offers",
                FundingOfferControllerTests.FundingReviewDoesNotCascadeCompletedOffers);
            Run(
                "Satellite fulfilment waits for sponsor review",
                FundingOfferControllerTests.SatelliteFulfilmentWaitsForFundingReview);
            Run(
                "Satellite sponsor review caps unfinished offers",
                FundingOfferControllerTests.SatelliteReviewCapsUnfulfilledOffersAtTwo);
            Run(
                "Crossed funding boundaries each run sponsor review",
                FundingOfferControllerTests.CrossedFundingBoundariesEachRunSponsorReview);

            Console.WriteLine();
            Console.WriteLine(_failures == 0
                ? "All controller and unlock rule regression tests passed."
                : _failures + " controller/unlock regression test(s) failed.");
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
    }
}

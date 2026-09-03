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
                "Unlock program scopes are respected",
                UnlockRuleEvaluatorTests.ProgramScopesAreRespected);
            Run(
                "Unlock required agency count must be met",
                UnlockRuleEvaluatorTests.RequiredAgencyCountMustBeMet);
            Run(
                "Unlock achievement timestamps respect evaluation time",
                UnlockRuleEvaluatorTests.AchievementTimestampUsesEvaluationTime);
            Run(
                "Unlock time condition uses exact boundary",
                UnlockRuleEvaluatorTests.UniversalTimeConditionUsesExactBoundary);
            Run(
                "Unlock satellite count uses collective program state",
                UnlockRuleEvaluatorTests.SatelliteCountConditionUsesCollectiveProgramState);
            Run(
                "Unlock condition progress matches rule evaluation",
                UnlockRuleEvaluatorTests.ConditionProgressMatchesRuleEvaluation);
            Run(
                "Unlock program progress respects scope and time",
                UnlockRuleEvaluatorTests.ProgramConditionProgressUsesScopeAndTime);
            Run(
                "Unlock malformed rules fail closed",
                UnlockRuleEvaluatorTests.MalformedRulesFailClosed);
            Run(
                "Unlock invalid condition definitions fail fast",
                UnlockRuleEvaluatorTests.InvalidConditionDefinitionsFailFast);
            Run(
                "Rival selection uses shared scope/count/time rule",
                UnlockConsumerIntegrationTests.RivalSelectionUsesScopeCountAndHistoricalTime);

            Run(
                "Controller uses configured rival count and starting funds",
                SatelliteRaceControllerTests.ConfiguredRivalCountAndStartingFundsAreUsed);
            Run(
                "Controller uses configured funding interval",
                SatelliteRaceControllerTests.ConfiguredFundingIntervalSetsNextBoundary);
            Run(
                "Controller scheduled refresh can skip player vessel observation",
                SatelliteRaceControllerTests.ScheduledRefreshCanSkipPlayerVesselObservation);
            Run(
                "Controller probe observation unlocks funding flow",
                SatelliteRaceControllerTests.ProbeObservationUnlocksFundingFlow);
            Run(
                "Controller Kerbin network progress unlocks moon funding",
                SatelliteRaceControllerTests.KerbinNetworkProgressUnlocksMoonFunding);
            Run(
                "Controller existing state pays at shared funding boundary",
                SatelliteRaceControllerTests.ExistingStatePaysAtSharedFundingBoundary);
            Run(
                "Controller restored overdue funding boundary is processed",
                SatelliteRaceControllerTests.RestoredOverdueFundingBoundaryIsProcessed);
            Run(
                "Controller boundary observation is not paid retroactively",
                SatelliteRaceControllerTests.BoundaryObservationIsNotPaidRetroactively);
            Run(
                "Controller projected payout cache rebuilds on refresh",
                SatelliteRaceControllerTests.ProjectedPayoutCacheRebuildsOnRefresh);

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

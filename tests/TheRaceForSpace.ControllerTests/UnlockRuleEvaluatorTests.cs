using System;
using System.Collections.Generic;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Agencies;

namespace TheRaceForSpace.ControllerTests
{
    internal static class UnlockRuleEvaluatorTests
    {
        public static void NullRuleIsAvailableFromStart()
        {
            Require(
                UnlockRuleEvaluator.IsSatisfied(null, null, 0.0),
                "A null unlock rule should represent a target available from campaign start.");
        }

        public static void ConditionsInOnePathRequireAll()
        {
            IList<AgencyState> agencies = CreateAgencies();
            agencies[0].RecordObjectiveCompletion("probe-orbit", 100.0);

            var rule = new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        "probe-orbit",
                        UnlockAgencyScope.AnyAgency),
                    UnlockConditionDefinition.AfterUniversalTime(200.0)));

            Require(
                !UnlockRuleEvaluator.IsSatisfied(rule, agencies, 199.0),
                "A path should remain locked while any required condition is missing.");
            Require(
                UnlockRuleEvaluator.IsSatisfied(rule, agencies, 200.0),
                "Every condition in one path should combine with AND semantics.");
        }

        public static void AlternativePathCanUnlock()
        {
            IList<AgencyState> agencies = CreateAgencies();
            agencies[1].RecordObjectiveCompletion("rival-breakthrough", 150.0);

            var rule = new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        "normal-route",
                        UnlockAgencyScope.AnyAgency),
                    UnlockConditionDefinition.AfterUniversalTime(500.0)),
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        "rival-breakthrough",
                        UnlockAgencyScope.AnyRival)));

            Require(
                UnlockRuleEvaluator.IsSatisfied(rule, agencies, 150.0),
                "Any complete alternative path should satisfy the rule with OR semantics.");
        }

        public static void AgencyScopesAreRespected()
        {
            IList<AgencyState> agencies = CreateAgencies();
            agencies[1].RecordObjectiveCompletion("scoped-objectiveCompletion", 100.0);

            var playerRule = RuleForObjectiveCompletion("scoped-objectiveCompletion", UnlockAgencyScope.Player, 1);
            var rivalRule = RuleForObjectiveCompletion("scoped-objectiveCompletion", UnlockAgencyScope.AnyRival, 1);
            var anyAgencyRule = RuleForObjectiveCompletion("scoped-objectiveCompletion", UnlockAgencyScope.AnyAgency, 1);

            Require(
                !UnlockRuleEvaluator.IsSatisfied(playerRule, agencies, 100.0),
                "A rival objectiveCompletion must not satisfy a Player-scoped condition.");
            Require(
                UnlockRuleEvaluator.IsSatisfied(rivalRule, agencies, 100.0),
                "A non-player agency should satisfy an AnyRival condition.");
            Require(
                UnlockRuleEvaluator.IsSatisfied(anyAgencyRule, agencies, 100.0),
                "Either player or rival agencies should satisfy an AnyAgency condition.");

            agencies[0].RecordObjectiveCompletion("player-only", 100.0);
            var playerOnlyRivalRule = RuleForObjectiveCompletion("player-only", UnlockAgencyScope.AnyRival, 1);
            Require(
                !UnlockRuleEvaluator.IsSatisfied(playerOnlyRivalRule, agencies, 100.0),
                "A player objectiveCompletion must not satisfy an AnyRival condition.");
        }

        public static void RequiredAgencyCountMustBeMet()
        {
            IList<AgencyState> agencies = CreateAgencies();
            agencies[0].RecordObjectiveCompletion("common-orbit", 100.0);

            UnlockRuleDefinition rule = RuleForObjectiveCompletion(
                "common-orbit",
                UnlockAgencyScope.AnyAgency,
                2);

            Require(
                !UnlockRuleEvaluator.IsSatisfied(rule, agencies, 100.0),
                "One qualifying agency should not satisfy a two-agency requirement.");

            agencies[2].RecordObjectiveCompletion("common-orbit", 120.0);
            Require(
                UnlockRuleEvaluator.IsSatisfied(rule, agencies, 120.0),
                "The rule should unlock once the required number of agencies has achieved the objective.");
        }

        public static void ObjectiveCompletionTimestampUsesEvaluationTime()
        {
            IList<AgencyState> agencies = CreateAgencies();
            agencies[1].RecordObjectiveCompletion("future-objectiveCompletion", 250.0);
            UnlockRuleDefinition rule = RuleForObjectiveCompletion(
                "future-objectiveCompletion",
                UnlockAgencyScope.AnyAgency,
                1);

            Require(
                !UnlockRuleEvaluator.IsSatisfied(rule, agencies, 249.0),
                "An objectiveCompletion recorded after the evaluated historical time must not count yet.");
            Require(
                UnlockRuleEvaluator.IsSatisfied(rule, agencies, 250.0),
                "An objectiveCompletion should count exactly at its recorded universal time.");
        }

        public static void UniversalTimeConditionUsesExactBoundary()
        {
            var rule = new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.AfterUniversalTime(500.0)));

            Require(
                !UnlockRuleEvaluator.IsSatisfied(rule, null, 499.0),
                "A time condition should remain locked before its threshold.");
            Require(
                UnlockRuleEvaluator.IsSatisfied(rule, null, 500.0),
                "A time condition should unlock exactly at its threshold.");
        }

        public static void SatelliteCountConditionUsesCollectiveProgramState()
        {
            IList<AgencyState> agencies = CreateAgencies();
            agencies[0].SetSatelliteCount("Kerbin", 2);
            agencies[1].SetSatelliteCount("Kerbin", 3);

            UnlockConditionDefinition condition =
                UnlockConditionDefinition.SatelliteCount("Kerbin", 6);
            var rule = new UnlockRuleDefinition(new UnlockPathDefinition(condition));

            Require(
                UnlockRuleEvaluator.GetSatelliteCount(condition, agencies) == 5,
                "Satellite progress should sum qualifying satellites across all race agencies.");
            Require(
                !UnlockRuleEvaluator.IsConditionSatisfied(condition, agencies, 100.0),
                "Five collective Kerbin satellites should not satisfy a six-satellite threshold.");
            Require(
                !UnlockRuleEvaluator.IsSatisfied(rule, agencies, 100.0),
                "The containing rule should remain locked below the collective satellite threshold.");

            agencies[2].SetSatelliteCount("KERBIN", 1);

            Require(
                UnlockRuleEvaluator.GetSatelliteCount(condition, agencies) == 6,
                "Satellite progress should include another agency and match body names case-insensitively.");
            Require(
                UnlockRuleEvaluator.IsConditionSatisfied(condition, agencies, 100.0),
                "The satellite condition should complete when the race total reaches its threshold.");
            Require(
                UnlockRuleEvaluator.IsSatisfied(rule, agencies, 100.0),
                "The full rule should agree with the completed satellite-count condition.");
        }

        public static void ConditionProgressMatchesRuleEvaluation()
        {
            IList<AgencyState> agencies = CreateAgencies();
            agencies[1].RecordObjectiveCompletion("shared-progress", 100.0);
            agencies[2].RecordObjectiveCompletion("shared-progress", 200.0);

            UnlockConditionDefinition condition = UnlockConditionDefinition.ObjectiveCompletion(
                "shared-progress",
                UnlockAgencyScope.AnyRival,
                2);
            var rule = new UnlockRuleDefinition(new UnlockPathDefinition(condition));

            Require(
                UnlockRuleEvaluator.GetSatisfiedProgramCount(condition, agencies, 199.0) == 1,
                "Progress should count only rival objectives reached by the evaluation time.");
            Require(
                !UnlockRuleEvaluator.IsConditionSatisfied(condition, agencies, 199.0),
                "One of two rivals should leave the condition incomplete.");
            Require(
                !UnlockRuleEvaluator.IsSatisfied(rule, agencies, 199.0),
                "The containing rule should agree with the condition progress result.");

            Require(
                UnlockRuleEvaluator.GetSatisfiedProgramCount(condition, agencies, 200.0) == 2,
                "Both rivals should count exactly when the second objectiveCompletion occurs.");
            Require(
                UnlockRuleEvaluator.IsConditionSatisfied(condition, agencies, 200.0),
                "The condition should complete at the same time as its count reaches the requirement.");
            Require(
                UnlockRuleEvaluator.IsSatisfied(rule, agencies, 200.0),
                "The full rule should agree with the completed UI-facing condition result.");
        }

        public static void ProgramConditionProgressUsesScopeAndTime()
        {
            IList<AgencyState> agencies = CreateAgencies();
            agencies[0].RecordObjectiveCompletion("rival-only-progress", 50.0);
            agencies[1].RecordObjectiveCompletion("rival-only-progress", 100.0);
            UnlockConditionDefinition condition = UnlockConditionDefinition.ObjectiveCompletion(
                "rival-only-progress",
                UnlockAgencyScope.AnyRival);

            Require(
                !UnlockRuleEvaluator.DoesAgencySatisfyObjectiveCompletionCondition(
                    agencies[0],
                    condition,
                    100.0),
                "The player should not be attributed to an AnyRival condition.");
            Require(
                !UnlockRuleEvaluator.DoesAgencySatisfyObjectiveCompletionCondition(
                    agencies[1],
                    condition,
                    99.0),
                "A rival objectiveCompletion should not be attributed before its recorded time.");
            Require(
                UnlockRuleEvaluator.DoesAgencySatisfyObjectiveCompletionCondition(
                    agencies[1],
                    condition,
                    100.0),
                "A qualifying rival should be attributable exactly at its objectiveCompletion time.");
        }

        public static void MalformedRulesFailClosed()
        {
            var emptyRule = new UnlockRuleDefinition();
            var emptyPathRule = new UnlockRuleDefinition(new UnlockPathDefinition());
            UnlockRuleDefinition unknownObjectiveRule = RuleForObjectiveCompletion(
                "unknown-objective",
                UnlockAgencyScope.AnyAgency,
                1);
            UnlockRuleDefinition knownObjectiveRule = RuleForObjectiveCompletion(
                "probe-orbit",
                UnlockAgencyScope.AnyAgency,
                1);
            var satelliteRule = new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.SatelliteCount("Kerbin", 1)));
            IList<AgencyState> agencies = CreateAgencies();
            agencies[0].RecordObjectiveCompletion("probe-orbit", 100.0);
            agencies[0].SetSatelliteCount("Kerbin", 1);

            Require(
                !UnlockRuleEvaluator.IsSatisfied(emptyRule, agencies, 1000.0),
                "An empty rule should not accidentally unlock a target.");
            Require(
                !UnlockRuleEvaluator.IsSatisfied(emptyPathRule, agencies, 1000.0),
                "An empty path should not accidentally unlock a target.");
            Require(
                !UnlockRuleEvaluator.IsSatisfied(unknownObjectiveRule, agencies, 1000.0),
                "An objectiveCompletion ID absent from campaign state should fail safely.");
            Require(
                !UnlockRuleEvaluator.IsSatisfied(knownObjectiveRule, agencies, double.NaN),
                "NaN evaluation time should fail closed.");
            Require(
                !UnlockRuleEvaluator.IsSatisfied(knownObjectiveRule, agencies, double.PositiveInfinity),
                "Infinite evaluation time should fail closed.");
            Require(
                !UnlockRuleEvaluator.IsSatisfied(satelliteRule, agencies, -1.0),
                "A negative evaluation time must not let a current satellite count bypass time validation.");
        }

        public static void InvalidConditionDefinitionsFailFast()
        {
            RequireThrows<ArgumentException>(delegate
            {
                UnlockConditionDefinition.ObjectiveCompletion(string.Empty, UnlockAgencyScope.AnyAgency);
            });

            RequireThrows<ArgumentOutOfRangeException>(delegate
            {
                UnlockConditionDefinition.ObjectiveCompletion("probe-orbit", UnlockAgencyScope.AnyAgency, 0);
            });

            RequireThrows<ArgumentOutOfRangeException>(delegate
            {
                UnlockConditionDefinition.AfterUniversalTime(double.NaN);
            });

            RequireThrows<ArgumentOutOfRangeException>(delegate
            {
                UnlockConditionDefinition.AfterUniversalTime(double.PositiveInfinity);
            });

            RequireThrows<ArgumentOutOfRangeException>(delegate
            {
                UnlockConditionDefinition.AfterUniversalTime(-1.0);
            });

            RequireThrows<ArgumentException>(delegate
            {
                UnlockConditionDefinition.SatelliteCount(string.Empty, 6);
            });

            RequireThrows<ArgumentOutOfRangeException>(delegate
            {
                UnlockConditionDefinition.SatelliteCount("Kerbin", 0);
            });
        }

        private static UnlockRuleDefinition RuleForObjectiveCompletion(
            string objectiveId,
            UnlockAgencyScope agencyScope,
            int requiredAgencyCount)
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.ObjectiveCompletion(
                        objectiveId,
                        agencyScope,
                        requiredAgencyCount)));
        }

        private static IList<AgencyState> CreateAgencies()
        {
            return new List<AgencyState>
            {
                new AgencyState("player", "Player", true),
                new AgencyState("aster", "Aster", false),
                new AgencyState("cobalt", "Cobalt", false)
            };
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void RequireThrows<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(
                "Expected exception of type '" + typeof(TException).Name + "'.");
        }
    }
}

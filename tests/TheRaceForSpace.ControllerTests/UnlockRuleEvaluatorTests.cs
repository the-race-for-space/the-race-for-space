using System;
using System.Collections.Generic;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;

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
            IList<SpaceProgramState> programs = CreatePrograms();
            programs[0].RecordAchievement("probe-orbit", 100.0);

            var rule = new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        "probe-orbit",
                        UnlockProgramScope.AnyAgency),
                    UnlockConditionDefinition.AfterUniversalTime(200.0)));

            Require(
                !UnlockRuleEvaluator.IsSatisfied(rule, programs, 199.0),
                "A path should remain locked while any required condition is missing.");
            Require(
                UnlockRuleEvaluator.IsSatisfied(rule, programs, 200.0),
                "Every condition in one path should combine with AND semantics.");
        }

        public static void AlternativePathCanUnlock()
        {
            IList<SpaceProgramState> programs = CreatePrograms();
            programs[1].RecordAchievement("rival-breakthrough", 150.0);

            var rule = new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        "normal-route",
                        UnlockProgramScope.AnyAgency),
                    UnlockConditionDefinition.AfterUniversalTime(500.0)),
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        "rival-breakthrough",
                        UnlockProgramScope.AnyRival)));

            Require(
                UnlockRuleEvaluator.IsSatisfied(rule, programs, 150.0),
                "Any complete alternative path should satisfy the rule with OR semantics.");
        }

        public static void ProgramScopesAreRespected()
        {
            IList<SpaceProgramState> programs = CreatePrograms();
            programs[1].RecordAchievement("scoped-achievement", 100.0);

            var playerRule = RuleForAchievement("scoped-achievement", UnlockProgramScope.Player, 1);
            var rivalRule = RuleForAchievement("scoped-achievement", UnlockProgramScope.AnyRival, 1);
            var anyAgencyRule = RuleForAchievement("scoped-achievement", UnlockProgramScope.AnyAgency, 1);

            Require(
                !UnlockRuleEvaluator.IsSatisfied(playerRule, programs, 100.0),
                "A rival achievement must not satisfy a Player-scoped condition.");
            Require(
                UnlockRuleEvaluator.IsSatisfied(rivalRule, programs, 100.0),
                "A non-player program should satisfy an AnyRival condition.");
            Require(
                UnlockRuleEvaluator.IsSatisfied(anyAgencyRule, programs, 100.0),
                "Either player or rival programs should satisfy an AnyAgency condition.");

            programs[0].RecordAchievement("player-only", 100.0);
            var playerOnlyRivalRule = RuleForAchievement("player-only", UnlockProgramScope.AnyRival, 1);
            Require(
                !UnlockRuleEvaluator.IsSatisfied(playerOnlyRivalRule, programs, 100.0),
                "A player achievement must not satisfy an AnyRival condition.");
        }

        public static void RequiredAgencyCountMustBeMet()
        {
            IList<SpaceProgramState> programs = CreatePrograms();
            programs[0].RecordAchievement("common-orbit", 100.0);

            UnlockRuleDefinition rule = RuleForAchievement(
                "common-orbit",
                UnlockProgramScope.AnyAgency,
                2);

            Require(
                !UnlockRuleEvaluator.IsSatisfied(rule, programs, 100.0),
                "One qualifying agency should not satisfy a two-agency requirement.");

            programs[2].RecordAchievement("common-orbit", 120.0);
            Require(
                UnlockRuleEvaluator.IsSatisfied(rule, programs, 120.0),
                "The rule should unlock once the required number of agencies has achieved the milestone.");
        }

        public static void AchievementTimestampUsesEvaluationTime()
        {
            IList<SpaceProgramState> programs = CreatePrograms();
            programs[1].RecordAchievement("future-achievement", 250.0);
            UnlockRuleDefinition rule = RuleForAchievement(
                "future-achievement",
                UnlockProgramScope.AnyAgency,
                1);

            Require(
                !UnlockRuleEvaluator.IsSatisfied(rule, programs, 249.0),
                "An achievement recorded after the evaluated historical time must not count yet.");
            Require(
                UnlockRuleEvaluator.IsSatisfied(rule, programs, 250.0),
                "An achievement should count exactly at its recorded universal time.");
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
            IList<SpaceProgramState> programs = CreatePrograms();
            programs[0].SetSatelliteCount("Kerbin", 2);
            programs[1].SetSatelliteCount("Kerbin", 3);

            UnlockConditionDefinition condition =
                UnlockConditionDefinition.SatelliteCount("Kerbin", 6);
            var rule = new UnlockRuleDefinition(new UnlockPathDefinition(condition));

            Require(
                UnlockRuleEvaluator.GetSatelliteCount(condition, programs) == 5,
                "Satellite progress should sum qualifying satellites across all race programs.");
            Require(
                !UnlockRuleEvaluator.IsConditionSatisfied(condition, programs, 100.0),
                "Five collective Kerbin satellites should not satisfy a six-satellite threshold.");
            Require(
                !UnlockRuleEvaluator.IsSatisfied(rule, programs, 100.0),
                "The containing rule should remain locked below the collective satellite threshold.");

            programs[2].SetSatelliteCount("KERBIN", 1);

            Require(
                UnlockRuleEvaluator.GetSatelliteCount(condition, programs) == 6,
                "Satellite progress should include another agency and match body names case-insensitively.");
            Require(
                UnlockRuleEvaluator.IsConditionSatisfied(condition, programs, 100.0),
                "The satellite condition should complete when the race total reaches its threshold.");
            Require(
                UnlockRuleEvaluator.IsSatisfied(rule, programs, 100.0),
                "The full rule should agree with the completed satellite-count condition.");
        }

        public static void ConditionProgressMatchesRuleEvaluation()
        {
            IList<SpaceProgramState> programs = CreatePrograms();
            programs[1].RecordAchievement("shared-progress", 100.0);
            programs[2].RecordAchievement("shared-progress", 200.0);

            UnlockConditionDefinition condition = UnlockConditionDefinition.Achievement(
                "shared-progress",
                UnlockProgramScope.AnyRival,
                2);
            var rule = new UnlockRuleDefinition(new UnlockPathDefinition(condition));

            Require(
                UnlockRuleEvaluator.GetSatisfiedProgramCount(condition, programs, 199.0) == 1,
                "Progress should count only rival achievements reached by the evaluation time.");
            Require(
                !UnlockRuleEvaluator.IsConditionSatisfied(condition, programs, 199.0),
                "One of two rivals should leave the condition incomplete.");
            Require(
                !UnlockRuleEvaluator.IsSatisfied(rule, programs, 199.0),
                "The containing rule should agree with the condition progress result.");

            Require(
                UnlockRuleEvaluator.GetSatisfiedProgramCount(condition, programs, 200.0) == 2,
                "Both rivals should count exactly when the second achievement occurs.");
            Require(
                UnlockRuleEvaluator.IsConditionSatisfied(condition, programs, 200.0),
                "The condition should complete at the same time as its count reaches the requirement.");
            Require(
                UnlockRuleEvaluator.IsSatisfied(rule, programs, 200.0),
                "The full rule should agree with the completed UI-facing condition result.");
        }

        public static void ProgramConditionProgressUsesScopeAndTime()
        {
            IList<SpaceProgramState> programs = CreatePrograms();
            programs[0].RecordAchievement("rival-only-progress", 50.0);
            programs[1].RecordAchievement("rival-only-progress", 100.0);
            UnlockConditionDefinition condition = UnlockConditionDefinition.Achievement(
                "rival-only-progress",
                UnlockProgramScope.AnyRival);

            Require(
                !UnlockRuleEvaluator.DoesProgramSatisfyAchievementCondition(
                    programs[0],
                    condition,
                    100.0),
                "The player should not be attributed to an AnyRival condition.");
            Require(
                !UnlockRuleEvaluator.DoesProgramSatisfyAchievementCondition(
                    programs[1],
                    condition,
                    99.0),
                "A rival achievement should not be attributed before its recorded time.");
            Require(
                UnlockRuleEvaluator.DoesProgramSatisfyAchievementCondition(
                    programs[1],
                    condition,
                    100.0),
                "A qualifying rival should be attributable exactly at its achievement time.");
        }

        public static void MalformedRulesFailClosed()
        {
            var emptyRule = new UnlockRuleDefinition();
            var emptyPathRule = new UnlockRuleDefinition(new UnlockPathDefinition());
            UnlockRuleDefinition unknownAchievementRule = RuleForAchievement(
                "unknown-milestone",
                UnlockProgramScope.AnyAgency,
                1);

            Require(
                !UnlockRuleEvaluator.IsSatisfied(emptyRule, CreatePrograms(), 1000.0),
                "An empty rule should not accidentally unlock a target.");
            Require(
                !UnlockRuleEvaluator.IsSatisfied(emptyPathRule, CreatePrograms(), 1000.0),
                "An empty path should not accidentally unlock a target.");
            Require(
                !UnlockRuleEvaluator.IsSatisfied(unknownAchievementRule, CreatePrograms(), 1000.0),
                "An achievement ID absent from campaign state should fail safely.");
            Require(
                !UnlockRuleEvaluator.IsSatisfied(
                    RuleForAchievement("probe-orbit", UnlockProgramScope.AnyAgency, 1),
                    CreatePrograms(),
                    double.NaN),
                "An invalid evaluation time should fail closed.");
        }

        public static void InvalidConditionDefinitionsFailFast()
        {
            RequireThrows<ArgumentException>(delegate
            {
                UnlockConditionDefinition.Achievement(string.Empty, UnlockProgramScope.AnyAgency);
            });

            RequireThrows<ArgumentOutOfRangeException>(delegate
            {
                UnlockConditionDefinition.Achievement("probe-orbit", UnlockProgramScope.AnyAgency, 0);
            });

            RequireThrows<ArgumentOutOfRangeException>(delegate
            {
                UnlockConditionDefinition.AfterUniversalTime(double.NaN);
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

        private static UnlockRuleDefinition RuleForAchievement(
            string milestoneId,
            UnlockProgramScope programScope,
            int requiredProgramCount)
        {
            return new UnlockRuleDefinition(
                new UnlockPathDefinition(
                    UnlockConditionDefinition.Achievement(
                        milestoneId,
                        programScope,
                        requiredProgramCount)));
        }

        private static IList<SpaceProgramState> CreatePrograms()
        {
            return new List<SpaceProgramState>
            {
                new SpaceProgramState("player", "Player", true),
                new SpaceProgramState("aster", "Aster", false),
                new SpaceProgramState("cobalt", "Cobalt", false)
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

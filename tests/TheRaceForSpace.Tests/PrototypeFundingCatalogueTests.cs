using System;
using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Milestones;

namespace TheRaceForSpace.Tests
{
    internal static class PrototypeFundingCatalogueTests
    {
        public static void CatalogueMatchesCurrentPrototype()
        {
            IList<AchievementFundingProgramme> achievements =
                PrototypeFundingCatalogue.CreateAchievementProgrammes();
            IList<FundingProgramme> satelliteProgrammes =
                PrototypeFundingCatalogue.CreateSatelliteProgrammes();

            Equal(PrototypeMilestones.All.Count, achievements.Count);
            Equal(16, satelliteProgrammes.Count);

            AssertAchievement(
                achievements,
                PrototypeMilestones.ProbeOrbitId,
                75000.0);
            AssertAchievement(
                achievements,
                PrototypeMilestones.CrewedOrbitId,
                150000.0);
            AssertAchievement(
                achievements,
                PrototypeMilestones.MunProbeOrbitId,
                150000.0,
                PrototypeMilestones.ProbeOrbitId);
            AssertAchievement(
                achievements,
                PrototypeMilestones.MinmusProbeOrbitId,
                150000.0,
                PrototypeMilestones.ProbeOrbitId);
            AssertAchievement(
                achievements,
                PrototypeMilestones.DunaProbeOrbitId,
                300000.0,
                PrototypeMilestones.MunProbeOrbitId,
                PrototypeMilestones.MinmusProbeOrbitId);
            AssertAchievement(
                achievements,
                PrototypeMilestones.MunCrewedOrbitId,
                300000.0,
                PrototypeMilestones.CrewedOrbitId);
            AssertAchievement(
                achievements,
                PrototypeMilestones.MinmusCrewedOrbitId,
                300000.0,
                PrototypeMilestones.CrewedOrbitId);
            AssertAchievement(
                achievements,
                PrototypeMilestones.DunaCrewedOrbitId,
                500000.0,
                PrototypeMilestones.MunCrewedOrbitId,
                PrototypeMilestones.MinmusCrewedOrbitId);

            string[] interplanetaryProbePrerequisites =
                { PrototypeMilestones.MunProbeOrbitId, PrototypeMilestones.MinmusProbeOrbitId };
            string[] interplanetaryCrewedPrerequisites =
                { PrototypeMilestones.MunCrewedOrbitId, PrototypeMilestones.MinmusCrewedOrbitId };
            string[] interplanetaryNetworkPrerequisiteBodies = { "Mun", "Minmus" };
            int[] interplanetaryNetworkPrerequisiteCounts = { 3, 3 };

            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.MohoProbeOrbitId,
                PrototypeMilestones.MohoCrewedOrbitId,
                PrototypeFundingCatalogue.MohoNetworkId,
                "Moho",
                10,
                200000.0,
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites,
                interplanetaryNetworkPrerequisiteBodies,
                interplanetaryNetworkPrerequisiteCounts);
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.EveProbeOrbitId,
                PrototypeMilestones.EveCrewedOrbitId,
                PrototypeFundingCatalogue.EveNetworkId,
                "Eve",
                10,
                200000.0,
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites,
                interplanetaryNetworkPrerequisiteBodies,
                interplanetaryNetworkPrerequisiteCounts);
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.GillyProbeOrbitId,
                PrototypeMilestones.GillyCrewedOrbitId,
                PrototypeFundingCatalogue.GillyNetworkId,
                "Gilly",
                5,
                100000.0,
                new[] { PrototypeMilestones.EveProbeOrbitId },
                new[] { PrototypeMilestones.EveCrewedOrbitId },
                new[] { "Eve" },
                new[] { 6 });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.IkeProbeOrbitId,
                PrototypeMilestones.IkeCrewedOrbitId,
                PrototypeFundingCatalogue.IkeNetworkId,
                "Ike",
                5,
                100000.0,
                new[] { PrototypeMilestones.DunaProbeOrbitId },
                new[] { PrototypeMilestones.DunaCrewedOrbitId },
                new[] { "Duna" },
                new[] { 6 });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.DresProbeOrbitId,
                PrototypeMilestones.DresCrewedOrbitId,
                PrototypeFundingCatalogue.DresNetworkId,
                "Dres",
                10,
                200000.0,
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites,
                interplanetaryNetworkPrerequisiteBodies,
                interplanetaryNetworkPrerequisiteCounts);
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.JoolProbeOrbitId,
                PrototypeMilestones.JoolCrewedOrbitId,
                PrototypeFundingCatalogue.JoolNetworkId,
                "Jool",
                10,
                200000.0,
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites,
                interplanetaryNetworkPrerequisiteBodies,
                interplanetaryNetworkPrerequisiteCounts);
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.LaytheProbeOrbitId,
                PrototypeMilestones.LaytheCrewedOrbitId,
                PrototypeFundingCatalogue.LaytheNetworkId,
                "Laythe",
                5,
                100000.0,
                new[] { PrototypeMilestones.JoolProbeOrbitId },
                new[] { PrototypeMilestones.JoolCrewedOrbitId },
                new[] { "Jool" },
                new[] { 6 });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.VallProbeOrbitId,
                PrototypeMilestones.VallCrewedOrbitId,
                PrototypeFundingCatalogue.VallNetworkId,
                "Vall",
                5,
                100000.0,
                new[] { PrototypeMilestones.JoolProbeOrbitId },
                new[] { PrototypeMilestones.JoolCrewedOrbitId },
                new[] { "Jool" },
                new[] { 6 });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.TyloProbeOrbitId,
                PrototypeMilestones.TyloCrewedOrbitId,
                PrototypeFundingCatalogue.TyloNetworkId,
                "Tylo",
                5,
                100000.0,
                new[] { PrototypeMilestones.JoolProbeOrbitId },
                new[] { PrototypeMilestones.JoolCrewedOrbitId },
                new[] { "Jool" },
                new[] { 6 });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.BopProbeOrbitId,
                PrototypeMilestones.BopCrewedOrbitId,
                PrototypeFundingCatalogue.BopNetworkId,
                "Bop",
                5,
                100000.0,
                new[] { PrototypeMilestones.JoolProbeOrbitId },
                new[] { PrototypeMilestones.JoolCrewedOrbitId },
                new[] { "Jool" },
                new[] { 6 });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.PolProbeOrbitId,
                PrototypeMilestones.PolCrewedOrbitId,
                PrototypeFundingCatalogue.PolNetworkId,
                "Pol",
                5,
                100000.0,
                new[] { PrototypeMilestones.JoolProbeOrbitId },
                new[] { PrototypeMilestones.JoolCrewedOrbitId },
                new[] { "Jool" },
                new[] { 6 });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.EelooProbeOrbitId,
                PrototypeMilestones.EelooCrewedOrbitId,
                PrototypeFundingCatalogue.EelooNetworkId,
                "Eeloo",
                10,
                200000.0,
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites,
                interplanetaryNetworkPrerequisiteBodies,
                interplanetaryNetworkPrerequisiteCounts);

            Equal(
                "Any agency must achieve Mun Probe Orbit and Minmus Probe Orbit.",
                FindAchievement(achievements, PrototypeMilestones.DunaProbeOrbitId).UnlockRequirement);
            Equal(
                "Any agency must achieve Mun Crewed Orbit and Minmus Crewed Orbit.",
                FindAchievement(achievements, PrototypeMilestones.DunaCrewedOrbitId).UnlockRequirement);

            AssertSatellite(
                satelliteProgrammes,
                PrototypeFundingCatalogue.KerbinNetworkId,
                "Kerbin",
                10,
                200000.0,
                PrototypeMilestones.ProbeOrbitId,
                new string[0],
                new int[0]);
            AssertSatellite(
                satelliteProgrammes,
                PrototypeFundingCatalogue.MunNetworkId,
                "Mun",
                5,
                100000.0,
                PrototypeMilestones.MunProbeOrbitId,
                new[] { "Kerbin" },
                new[] { 6 });
            AssertSatellite(
                satelliteProgrammes,
                PrototypeFundingCatalogue.MinmusNetworkId,
                "Minmus",
                5,
                100000.0,
                PrototypeMilestones.MinmusProbeOrbitId,
                new[] { "Kerbin" },
                new[] { 6 });
            AssertSatellite(
                satelliteProgrammes,
                PrototypeFundingCatalogue.DunaNetworkId,
                "Duna",
                10,
                200000.0,
                PrototypeMilestones.DunaProbeOrbitId,
                interplanetaryNetworkPrerequisiteBodies,
                interplanetaryNetworkPrerequisiteCounts);

            Equal(
                "Any agency must achieve Mun Probe Orbit and the Kerbin satellite network must reach 6 qualifying satellites.",
                FindSatellite(satelliteProgrammes, PrototypeFundingCatalogue.MunNetworkId).UnlockRequirement);
            Equal(
                "Any agency must achieve Minmus Probe Orbit and the Kerbin satellite network must reach 6 qualifying satellites.",
                FindSatellite(satelliteProgrammes, PrototypeFundingCatalogue.MinmusNetworkId).UnlockRequirement);
            Equal(
                "Any agency must achieve Duna Probe Orbit and the Mun satellite network must reach 3 qualifying satellites and the Minmus satellite network must reach 3 qualifying satellites.",
                FindSatellite(satelliteProgrammes, PrototypeFundingCatalogue.DunaNetworkId).UnlockRequirement);
            Equal(
                "Any agency must achieve Ike Probe Orbit and the Duna satellite network must reach 6 qualifying satellites.",
                FindSatellite(satelliteProgrammes, PrototypeFundingCatalogue.IkeNetworkId).UnlockRequirement);
        }

        public static void CatalogueCreatesFreshCampaignState()
        {
            IList<AchievementFundingProgramme> firstAchievements =
                PrototypeFundingCatalogue.CreateAchievementProgrammes();
            IList<FundingProgramme> firstSatellites =
                PrototypeFundingCatalogue.CreateSatelliteProgrammes();

            firstAchievements[0].Start();
            firstAchievements[0].AdvancePayout();
            firstSatellites[0].Unlock();

            IList<AchievementFundingProgramme> secondAchievements =
                PrototypeFundingCatalogue.CreateAchievementProgrammes();
            IList<FundingProgramme> secondSatellites =
                PrototypeFundingCatalogue.CreateSatelliteProgrammes();

            Require(!secondAchievements[0].HasStarted, "A new catalogue build must not reuse achievement campaign state.");
            Equal(0, secondAchievements[0].PaymentsProcessed);
            Require(!secondSatellites[0].IsAvailable, "A new catalogue build must not reuse satellite unlock state.");
        }

        private static void AssertBodyFundingSet(
            IList<AchievementFundingProgramme> achievements,
            IList<FundingProgramme> satelliteProgrammes,
            string probeMilestoneId,
            string crewedMilestoneId,
            string networkId,
            string celestialBodyName,
            int requiredSatellites,
            double networkRewardFunds,
            string[] probePrerequisiteMilestoneIds,
            string[] crewedPrerequisiteMilestoneIds,
            string[] networkPrerequisiteBodyNames,
            int[] networkPrerequisiteSatelliteCounts)
        {
            AssertAchievement(
                achievements,
                probeMilestoneId,
                300000.0,
                probePrerequisiteMilestoneIds);
            AssertAchievement(
                achievements,
                crewedMilestoneId,
                500000.0,
                crewedPrerequisiteMilestoneIds);
            AssertSatellite(
                satelliteProgrammes,
                networkId,
                celestialBodyName,
                requiredSatellites,
                networkRewardFunds,
                probeMilestoneId,
                networkPrerequisiteBodyNames,
                networkPrerequisiteSatelliteCounts);
        }

        private static void AssertAchievement(
            IList<AchievementFundingProgramme> programmes,
            string id,
            double rewardFunds,
            params string[] prerequisiteMilestoneIds)
        {
            AchievementFundingProgramme programme = FindAchievement(programmes, id);
            Require(programme != null, "Missing achievement funding programme '" + id + "'.");
            Equal(rewardFunds, programme.BaseRewardFunds);
            AssertAnyAgencyRule(programme.UnlockRule, prerequisiteMilestoneIds);
        }

        private static void AssertSatellite(
            IList<FundingProgramme> programmes,
            string id,
            string celestialBodyName,
            int requiredSatellites,
            double rewardFunds,
            string prerequisiteMilestoneId,
            string[] prerequisiteSatelliteBodyNames,
            int[] prerequisiteSatelliteCounts)
        {
            FundingProgramme programme = FindSatellite(programmes, id);
            Require(programme != null, "Missing satellite funding programme '" + id + "'.");
            Equal(celestialBodyName, programme.CelestialBodyName);
            Equal(requiredSatellites, programme.RequiredSatellites);
            Equal(rewardFunds, programme.RewardFunds);
            AssertSatelliteRule(
                programme.UnlockRule,
                prerequisiteMilestoneId,
                prerequisiteSatelliteBodyNames,
                prerequisiteSatelliteCounts);
            Require(!programme.IsAvailable, "Prototype satellite programmes should begin locked.");
        }

        private static void AssertSatelliteRule(
            UnlockRuleDefinition rule,
            string expectedMilestoneId,
            string[] expectedSatelliteBodyNames,
            int[] expectedSatelliteCounts)
        {
            Require(rule != null, "Satellite funding targets should carry an unlock rule.");
            Require(
                expectedSatelliteBodyNames != null
                && expectedSatelliteCounts != null
                && expectedSatelliteBodyNames.Length == expectedSatelliteCounts.Length,
                "Satellite prerequisite expectations must use matching body/count arrays.");
            Equal(1, rule.Paths.Count);
            Require(rule.Paths[0] != null, "Satellite unlock path should not be null.");
            Equal(1 + expectedSatelliteBodyNames.Length, rule.Paths[0].Conditions.Count);

            AssertAnyAgencyAchievementCondition(
                rule.Paths[0].Conditions[0],
                expectedMilestoneId);

            for (int prerequisiteIndex = 0;
                prerequisiteIndex < expectedSatelliteBodyNames.Length;
                prerequisiteIndex++)
            {
                UnlockConditionDefinition condition =
                    rule.Paths[0].Conditions[prerequisiteIndex + 1];
                Require(condition != null, "Satellite-count unlock condition should not be null.");
                Equal(UnlockConditionType.SatelliteCount, condition.ConditionType);
                Equal(expectedSatelliteBodyNames[prerequisiteIndex], condition.CelestialBodyName);
                Equal(expectedSatelliteCounts[prerequisiteIndex], condition.RequiredSatelliteCount);
            }
        }

        private static void AssertAnyAgencyRule(
            UnlockRuleDefinition rule,
            params string[] expectedMilestoneIds)
        {
            if (expectedMilestoneIds == null || expectedMilestoneIds.Length == 0)
            {
                Equal(null, rule);
                return;
            }

            Require(rule != null, "Locked prototype targets should carry an unlock rule.");
            Equal(1, rule.Paths.Count);
            Require(rule.Paths[0] != null, "Prototype unlock path should not be null.");
            Equal(expectedMilestoneIds.Length, rule.Paths[0].Conditions.Count);

            for (int conditionIndex = 0;
                conditionIndex < expectedMilestoneIds.Length;
                conditionIndex++)
            {
                AssertAnyAgencyAchievementCondition(
                    rule.Paths[0].Conditions[conditionIndex],
                    expectedMilestoneIds[conditionIndex]);
            }
        }

        private static void AssertAnyAgencyAchievementCondition(
            UnlockConditionDefinition condition,
            string expectedMilestoneId)
        {
            Require(condition != null, "Prototype unlock condition should not be null.");
            Equal(UnlockConditionType.Achievement, condition.ConditionType);
            Equal(UnlockProgramScope.AnyAgency, condition.ProgramScope);
            Equal(1, condition.RequiredProgramCount);
            Equal(expectedMilestoneId, condition.MilestoneId);
        }

        private static AchievementFundingProgramme FindAchievement(
            IList<AchievementFundingProgramme> programmes,
            string id)
        {
            for (int programmeIndex = 0; programmeIndex < programmes.Count; programmeIndex++)
            {
                AchievementFundingProgramme programme = programmes[programmeIndex];
                if (string.Equals(programme.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return programme;
                }
            }

            return null;
        }

        private static FundingProgramme FindSatellite(IList<FundingProgramme> programmes, string id)
        {
            for (int programmeIndex = 0; programmeIndex < programmes.Count; programmeIndex++)
            {
                FundingProgramme programme = programmes[programmeIndex];
                if (string.Equals(programme.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return programme;
                }
            }

            return null;
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

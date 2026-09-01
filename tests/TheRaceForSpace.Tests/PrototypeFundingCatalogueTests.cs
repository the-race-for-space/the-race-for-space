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
                100000.0);
            AssertAchievement(
                achievements,
                PrototypeMilestones.CrewedOrbitId,
                200000.0);
            AssertAchievement(
                achievements,
                PrototypeMilestones.MunProbeOrbitId,
                200000.0,
                PrototypeMilestones.ProbeOrbitId);
            AssertAchievement(
                achievements,
                PrototypeMilestones.MinmusProbeOrbitId,
                200000.0,
                PrototypeMilestones.ProbeOrbitId);
            AssertAchievement(
                achievements,
                PrototypeMilestones.DunaProbeOrbitId,
                200000.0,
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
                300000.0,
                PrototypeMilestones.MunCrewedOrbitId,
                PrototypeMilestones.MinmusCrewedOrbitId);

            string[] interplanetaryProbePrerequisites =
                { PrototypeMilestones.MunProbeOrbitId, PrototypeMilestones.MinmusProbeOrbitId };
            string[] interplanetaryCrewedPrerequisites =
                { PrototypeMilestones.MunCrewedOrbitId, PrototypeMilestones.MinmusCrewedOrbitId };

            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.MohoProbeOrbitId,
                PrototypeMilestones.MohoCrewedOrbitId,
                PrototypeFundingCatalogue.MohoNetworkId,
                "Moho",
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites);
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.EveProbeOrbitId,
                PrototypeMilestones.EveCrewedOrbitId,
                PrototypeFundingCatalogue.EveNetworkId,
                "Eve",
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites);
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.GillyProbeOrbitId,
                PrototypeMilestones.GillyCrewedOrbitId,
                PrototypeFundingCatalogue.GillyNetworkId,
                "Gilly",
                new[] { PrototypeMilestones.EveProbeOrbitId },
                new[] { PrototypeMilestones.EveCrewedOrbitId });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.IkeProbeOrbitId,
                PrototypeMilestones.IkeCrewedOrbitId,
                PrototypeFundingCatalogue.IkeNetworkId,
                "Ike",
                new[] { PrototypeMilestones.DunaProbeOrbitId },
                new[] { PrototypeMilestones.DunaCrewedOrbitId });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.DresProbeOrbitId,
                PrototypeMilestones.DresCrewedOrbitId,
                PrototypeFundingCatalogue.DresNetworkId,
                "Dres",
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites);
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.JoolProbeOrbitId,
                PrototypeMilestones.JoolCrewedOrbitId,
                PrototypeFundingCatalogue.JoolNetworkId,
                "Jool",
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites);
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.LaytheProbeOrbitId,
                PrototypeMilestones.LaytheCrewedOrbitId,
                PrototypeFundingCatalogue.LaytheNetworkId,
                "Laythe",
                new[] { PrototypeMilestones.JoolProbeOrbitId },
                new[] { PrototypeMilestones.JoolCrewedOrbitId });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.VallProbeOrbitId,
                PrototypeMilestones.VallCrewedOrbitId,
                PrototypeFundingCatalogue.VallNetworkId,
                "Vall",
                new[] { PrototypeMilestones.JoolProbeOrbitId },
                new[] { PrototypeMilestones.JoolCrewedOrbitId });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.TyloProbeOrbitId,
                PrototypeMilestones.TyloCrewedOrbitId,
                PrototypeFundingCatalogue.TyloNetworkId,
                "Tylo",
                new[] { PrototypeMilestones.JoolProbeOrbitId },
                new[] { PrototypeMilestones.JoolCrewedOrbitId });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.BopProbeOrbitId,
                PrototypeMilestones.BopCrewedOrbitId,
                PrototypeFundingCatalogue.BopNetworkId,
                "Bop",
                new[] { PrototypeMilestones.JoolProbeOrbitId },
                new[] { PrototypeMilestones.JoolCrewedOrbitId });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.PolProbeOrbitId,
                PrototypeMilestones.PolCrewedOrbitId,
                PrototypeFundingCatalogue.PolNetworkId,
                "Pol",
                new[] { PrototypeMilestones.JoolProbeOrbitId },
                new[] { PrototypeMilestones.JoolCrewedOrbitId });
            AssertBodyFundingSet(
                achievements,
                satelliteProgrammes,
                PrototypeMilestones.EelooProbeOrbitId,
                PrototypeMilestones.EelooCrewedOrbitId,
                PrototypeFundingCatalogue.EelooNetworkId,
                "Eeloo",
                interplanetaryProbePrerequisites,
                interplanetaryCrewedPrerequisites);

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
                PrototypeMilestones.ProbeOrbitId);
            AssertSatellite(
                satelliteProgrammes,
                PrototypeFundingCatalogue.MunNetworkId,
                "Mun",
                5,
                100000.0,
                PrototypeMilestones.MunProbeOrbitId);
            AssertSatellite(
                satelliteProgrammes,
                PrototypeFundingCatalogue.MinmusNetworkId,
                "Minmus",
                5,
                100000.0,
                PrototypeMilestones.MinmusProbeOrbitId);
            AssertSatellite(
                satelliteProgrammes,
                PrototypeFundingCatalogue.DunaNetworkId,
                "Duna",
                5,
                100000.0,
                PrototypeMilestones.DunaProbeOrbitId);
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
            string[] probePrerequisiteMilestoneIds,
            string[] crewedPrerequisiteMilestoneIds)
        {
            AssertAchievement(
                achievements,
                probeMilestoneId,
                200000.0,
                probePrerequisiteMilestoneIds);
            AssertAchievement(
                achievements,
                crewedMilestoneId,
                300000.0,
                crewedPrerequisiteMilestoneIds);
            AssertSatellite(
                satelliteProgrammes,
                networkId,
                celestialBodyName,
                5,
                100000.0,
                probeMilestoneId);
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
            string prerequisiteMilestoneId)
        {
            FundingProgramme programme = FindSatellite(programmes, id);
            Require(programme != null, "Missing satellite funding programme '" + id + "'.");
            Equal(celestialBodyName, programme.CelestialBodyName);
            Equal(requiredSatellites, programme.RequiredSatellites);
            Equal(rewardFunds, programme.RewardFunds);
            AssertAnyAgencyRule(programme.UnlockRule, prerequisiteMilestoneId);
            Require(!programme.IsAvailable, "Prototype satellite programmes should begin locked.");
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
                UnlockConditionDefinition condition = rule.Paths[0].Conditions[conditionIndex];
                Require(condition != null, "Prototype unlock condition should not be null.");
                Equal(UnlockConditionType.Achievement, condition.ConditionType);
                Equal(UnlockProgramScope.AnyAgency, condition.ProgramScope);
                Equal(1, condition.RequiredProgramCount);
                Equal(expectedMilestoneIds[conditionIndex], condition.MilestoneId);
            }
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

using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Tests.Core;

namespace TheRaceForSpace.Tests.Agencies
{
    internal static class RivalProgramStateTests
    {
        public static void RunAll()
        {
            RivalAgencyGetsApprovedDefaults();
            PlayerAgencyDoesNotGetRivalProgrammeState();
            ScienceSubjectIdentityIsStable();
            StateKeepsExplicitCrewCounts();
            CampaignSettingsTests.RunAll();
        }

        private static void RivalAgencyGetsApprovedDefaults()
        {
            var rival = new AgencyState("rival-test", "Rival Test", false);
            RivalProgramState programme = rival.RivalProgram;

            Require(programme != null, "Rival agencies should own RivalProgramState.");
            Require(rival.NextMissionReadyUniversalTime == -1.0,
                "Normal rival Contract preparation should begin without a ready timestamp.");
            Require(programme.StoredScience == 0.0, "Rivals should begin with zero stored Science.");
            Require(programme.KerbalsEmployed == 1, "Rivals should begin with one employed Kerbal.");
            Require(programme.PendingInsuranceFunds == 0.0, "Rivals should begin without pending insurance.");
            Require(programme.ScienceLaunchPreparation == null, "Rivals should begin without Science preparation.");
            Require(programme.CurrentResearch == null, "Rivals should begin without active research.");
            Require(programme.LiveMissions.Count == 0, "Rivals should begin without live missions.");
            Require(programme.CompletedScienceSubjects.Count == 0,
                "Rivals should begin without completed Science subjects.");
            Require(programme.FacilityConstruction.Count == 0,
                "Rivals should begin without facility construction.");
            Require(programme.ResearchedTechIds.Count == 1
                && programme.ResearchedTechIds.Contains(RivalProgramState.StartingTechId),
                "Rivals should begin with only the Start tech researched.");

            Array facilities = Enum.GetValues(typeof(RivalFacilityType));
            Require(facilities.Length == 9, "The initial rival programme should model nine facilities.");
            Require(programme.FacilityLevels.Count == facilities.Length,
                "Every rival facility should have an initial level.");

            for (int facilityIndex = 0; facilityIndex < facilities.Length; facilityIndex++)
            {
                RivalFacilityType facility = (RivalFacilityType)facilities.GetValue(facilityIndex);
                int level;
                Require(programme.FacilityLevels.TryGetValue(facility, out level),
                    "Missing initial level for rival facility '" + facility + "'.");
                Require(level == 1, "Every rival facility should begin at Level 1.");
            }
        }

        private static void PlayerAgencyDoesNotGetRivalProgrammeState()
        {
            var player = new AgencyState("player", "Kerbal Space Agency", true);

            Require(player.RivalProgram == null,
                "Player agencies should not duplicate KSP-owned programme state.");
            Require(player.NextMissionReadyUniversalTime == -1.0,
                "The shared preparation-ready field should use the same not-ready default.");
        }

        private static void ScienceSubjectIdentityIsStable()
        {
            var first = new ScienceSubjectKey(
                "crewReport",
                "Kerbin",
                "Landed",
                "Grasslands");
            var same = new ScienceSubjectKey(
                "CREWREPORT",
                "kerbin",
                "LANDED",
                "grasslands");
            var differentBiome = new ScienceSubjectKey(
                "crewReport",
                "Kerbin",
                "Landed",
                "Highlands");
            var emptyBiome = new ScienceSubjectKey(
                "crewReport",
                "Kerbin",
                "LowSpace",
                null);
            var explicitEmptyBiome = new ScienceSubjectKey(
                "crewReport",
                "Kerbin",
                "LowSpace",
                string.Empty);

            Require(first.Equals(same),
                "Science subject identity should use stable case-insensitive components.");
            Require(first.GetHashCode() == same.GetHashCode(),
                "Equal Science subject identities must have the same hash code.");
            Require(!first.Equals(differentBiome),
                "Different Science biomes should remain different subjects.");
            Require(emptyBiome.Equals(explicitEmptyBiome),
                "Null and empty non-biome subject values should normalize to the same identity.");

            var subjects = new HashSet<ScienceSubjectKey>();
            subjects.Add(first);
            subjects.Add(same);
            Require(subjects.Count == 1,
                "Completed Science subject sets should not duplicate equivalent identities.");
        }

        private static void StateKeepsExplicitCrewCounts()
        {
            var preparation = new ScienceLaunchPreparationState();
            Require(preparation.RequiredKerbals == 1,
                "Current Science preparations should default to one required Kerbal.");
            Require(preparation.ReadyUniversalTime == -1.0,
                "Science preparation should begin without a ready timestamp.");

            preparation.RequiredKerbals = 3;
            var liveMission = new RivalLiveMissionState
            {
                MissionType = RivalMissionType.Contract,
                AssignedKerbalCount = 4
            };

            Require(preparation.RequiredKerbals == 3,
                "Preparation state should retain future multi-Kerbal requirements explicitly.");
            Require(liveMission.AssignedKerbalCount == 4,
                "Live mission state should retain explicit assigned Kerbal counts.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

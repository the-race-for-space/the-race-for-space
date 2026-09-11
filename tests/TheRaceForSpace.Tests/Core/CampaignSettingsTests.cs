using System;
using TheRaceForSpace.Core;

namespace TheRaceForSpace.Tests.Core
{
    internal static class CampaignSettingsTests
    {
        public static void RunAll()
        {
            RivalProgrammeDefaultsMatchDesign();
            RivalMissionLocationsMatchDesign();
            MissionLocationOverridesValidateAndReset();
        }

        private static void RivalProgrammeDefaultsMatchDesign()
        {
            CampaignSettings.ResetToDefaults();

            Require(CampaignSettings.RivalProgressChance == 0.30,
                "Task 3 should keep the legacy rival progress chance active until later integration.");

            Require(CampaignSettings.RivalKerbalHireCostFunds == 100000.0,
                "Rival Kerbal hire cost should default to 100,000 Funds.");
            Require(CampaignSettings.RivalKerbalPayrollFundsPerFundingBoundary == 10000.0,
                "Rival Kerbal payroll should default to 10,000 Funds per funding boundary.");
            Require(CampaignSettings.RivalInsuranceFundsPerLostKerbal == 50000.0,
                "Rival insurance should default to 50,000 Funds per lost Kerbal.");
            Require(CampaignSettings.RivalKerbalLossChance == 0.25,
                "Rival Kerbal loss chance should default to 25%.");

            Require(CampaignSettings.RivalResearchDurationDays == 90.0,
                "Rival research should default to 90 campaign days.");
            Require(CampaignSettings.RivalFacilityLevel1To2CostFunds == 100000.0,
                "Level 1 to 2 construction should cost 100,000 Funds.");
            Require(CampaignSettings.RivalFacilityLevel1To2ConstructionDays == 180.0,
                "Level 1 to 2 construction should take 180 campaign days.");
            Require(CampaignSettings.RivalFacilityLevel2To3CostFunds == 250000.0,
                "Level 2 to 3 construction should cost 250,000 Funds.");
            Require(CampaignSettings.RivalFacilityLevel2To3ConstructionDays == 270.0,
                "Level 2 to 3 construction should take 270 campaign days.");

            Require(CampaignSettings.RivalAdministrationLevel1BaseIncomeFunds == 10000.0,
                "Administration Level 1 should provide 10,000 Funds base income.");
            Require(CampaignSettings.RivalAdministrationLevel2BaseIncomeFunds == 20000.0,
                "Administration Level 2 should provide 20,000 Funds base income.");
            Require(CampaignSettings.RivalAdministrationLevel3BaseIncomeFunds == 40000.0,
                "Administration Level 3 should provide 40,000 Funds base income.");

            Require(CampaignSettings.RivalAstronautComplexLevel1KerbalLimit == 3,
                "Astronaut Complex Level 1 should allow three rival Kerbals.");
            Require(CampaignSettings.RivalAstronautComplexLevel2KerbalLimit == 8,
                "Astronaut Complex Level 2 should allow eight rival Kerbals.");
            Require(CampaignSettings.RivalAstronautComplexLevel3KerbalLimit == 0,
                "Astronaut Complex Level 3 should use zero as the unlimited config sentinel.");

            Require(CampaignSettings.RivalMissionControlLevel1SatelliteLimit == 3,
                "Mission Control Level 1 should allow three satellites.");
            Require(CampaignSettings.RivalMissionControlLevel2SatelliteLimit == 8,
                "Mission Control Level 2 should allow eight satellites.");
            Require(CampaignSettings.RivalMissionControlLevel3SatelliteLimit == 0,
                "Mission Control Level 3 should use zero as the unlimited config sentinel.");

            Require(CampaignSettings.RivalResearchAndDevelopmentLevel1ScienceCostLimit == 90.0,
                "R&D Level 1 should permit tech through 90 Science.");
            Require(CampaignSettings.RivalResearchAndDevelopmentLevel2ScienceCostLimit == 300.0,
                "R&D Level 2 should permit tech through 300 Science.");
            Require(CampaignSettings.RivalResearchAndDevelopmentLevel3ScienceCostLimit == 0.0,
                "R&D Level 3 should use zero as the no-ceiling config sentinel.");

            Require(CampaignSettings.RivalTrackingStationKerbinLevel == 1,
                "Tracking Station Level 1 should permit Kerbin missions.");
            Require(CampaignSettings.RivalTrackingStationKerbinMoonsLevel == 2,
                "Tracking Station Level 2 should permit Mun and Minmus missions.");
            Require(CampaignSettings.RivalTrackingStationInterplanetaryLevel == 3,
                "Tracking Station Level 3 should permit interplanetary missions.");

            Require(CampaignSettings.RivalNormalLaunchProgressCheckIntervalDays == 5.0,
                "Normal rival Launch Progress should check every five Kerbin days.");
            Require(CampaignSettings.RivalNormalLaunchProgressStepPercent == 10,
                "Normal rival Launch Progress should add 10% on success.");
            Require(CampaignSettings.RivalPreOrbitLaunchProgressStepPercent == 20,
                "Pre-Orbit rival Launch Progress should retain its 20% step.");
            Require(CampaignSettings.RivalNormalLaunchFacilityLevel1Chance == 0.15,
                "Each Level-1 VAB/Launch Pad contribution should be 15%.");
            Require(CampaignSettings.RivalNormalLaunchFacilityLevel2BonusChance == 0.03,
                "Each Level-2 VAB/Launch Pad bonus should be 3%.");
            Require(CampaignSettings.RivalNormalLaunchFacilityLevel3BonusChance == 0.03,
                "Each Level-3 VAB/Launch Pad bonus should be another 3%.");

            Require(CampaignSettings.RivalScienceLaunchProgressCheckIntervalDays == 1.0,
                "Science Launch Progress should check every Kerbin day.");
            Require(CampaignSettings.RivalScienceLaunchProgressStepPercent == 10,
                "Science Launch Progress should add 10% on success.");
            Require(CampaignSettings.RivalScienceLaunchFacilityLevel1Chance == 0.20,
                "Each Level-1 SPH/Runway contribution should be 20%.");
            Require(CampaignSettings.RivalScienceLaunchFacilityLevel2BonusChance == 0.03,
                "Each Level-2 SPH/Runway bonus should be 3%.");
            Require(CampaignSettings.RivalScienceLaunchFacilityLevel3BonusChance == 0.03,
                "Each Level-3 SPH/Runway bonus should be another 3%.");
        }

        private static void RivalMissionLocationsMatchDesign()
        {
            CampaignSettings.ResetToDefaults();

            int locationCount = 0;
            foreach (RivalMissionLocationSettings ignored in CampaignSettings.RivalMissionLocations)
            {
                locationCount++;
            }

            Require(locationCount == 61,
                "The v0.6 rival mission database should contain exactly 61 approved location records.");

            AssertLocation(CampaignSettings.RivalPreOrbitLocalLocationId, 5.0, 1);
            AssertLocation("kerbin:shores", 10.0, 1);
            AssertLocation("kerbin:water", 10.0, 1);
            AssertLocation("kerbin:grasslands", 10.0, 1);
            AssertLocation("kerbin:highlands", 10.0, 1);
            AssertLocation("kerbin:mountains", 20.0, 3);
            AssertLocation("kerbin:deserts", 30.0, 2);
            AssertLocation("kerbin:badlands", 70.0, 4);
            AssertLocation("kerbin:tundra", 80.0, 3);
            AssertLocation("kerbin:ice-caps", 90.0, 4);
            AssertLocation("kerbin:northern-ice-shelf", 100.0, 4);
            AssertLocation("kerbin:southern-ice-shelf", 100.0, 4);

            string[] kscLocationIds =
            {
                "ksc:ksc",
                "ksc:administration",
                "ksc:astronaut-complex",
                "ksc:crawlerway",
                "ksc:flag-pole",
                "ksc:launch-pad",
                "ksc:mission-control",
                "ksc:r-and-d",
                "ksc:r-and-d-central-building",
                "ksc:r-and-d-corner-lab",
                "ksc:r-and-d-main-building",
                "ksc:r-and-d-observatory",
                "ksc:r-and-d-side-lab",
                "ksc:r-and-d-small-lab",
                "ksc:r-and-d-tanks",
                "ksc:r-and-d-wind-tunnel",
                "ksc:runway",
                "ksc:sph",
                "ksc:sph-main-building",
                "ksc:sph-round-tank",
                "ksc:sph-tanks",
                "ksc:sph-water-tower",
                "ksc:tracking-station",
                "ksc:tracking-station-dish-east",
                "ksc:tracking-station-dish-north",
                "ksc:tracking-station-dish-south",
                "ksc:tracking-station-hub",
                "ksc:vab",
                "ksc:vab-main-building",
                "ksc:vab-pod-memorial",
                "ksc:vab-round-tank",
                "ksc:vab-south-complex",
                "ksc:vab-tanks"
            };
            for (int locationIndex = 0; locationIndex < kscLocationIds.Length; locationIndex++)
            {
                AssertLocation(kscLocationIds[locationIndex], 5.0, 1);
            }

            AssertLocation(CampaignSettings.RivalKerbinOrbitLocationId, 10.0, 3);
            AssertLocation("body:Mun", 30.0, 4);
            AssertLocation("body:Minmus", 50.0, 4);
            AssertLocation("body:Moho", 124.0, 9);
            AssertLocation("body:Eve", 171.0, 10);
            AssertLocation("body:Gilly", 174.0, 5);
            AssertLocation("body:Duna", 303.0, 7);
            AssertLocation("body:Ike", 303.0, 5);
            AssertLocation("body:Dres", 604.0, 5);
            AssertLocation("body:Jool", 1123.0, 7);
            AssertLocation("body:Laythe", 1124.0, 8);
            AssertLocation("body:Vall", 1124.0, 8);
            AssertLocation("body:Tylo", 1125.0, 8);
            AssertLocation("body:Bop", 1128.0, 8);
            AssertLocation("body:Pol", 1131.0, 8);
            AssertLocation("body:Eeloo", 1587.0, 10);

            Require(CampaignSettings.GetRivalMissionLocationSettings("BODY:mun") != null,
                "Mission-location lookup should use stable case-insensitive IDs.");
            Require(CampaignSettings.GetRivalMissionLocationSettings("body:Sun") == null,
                "The Sun must remain absent until explicit Sun mission content exists.");
            Require(CampaignSettings.GetRivalMissionLocationSettings("body:Unknown") == null,
                "Unknown mission locations should fail safely.");
        }

        private static void MissionLocationOverridesValidateAndReset()
        {
            CampaignSettings.ResetToDefaults();

            Require(CampaignSettings.SetRivalMissionLocationSettings("body:Mun", 42.0, 6),
                "A valid mission-location config override should be accepted.");
            AssertLocation("body:Mun", 42.0, 6);

            Require(!CampaignSettings.SetRivalMissionLocationSettings("body:Mun", 0.0, 6),
                "Zero-duration mission locations should be rejected.");
            Require(!CampaignSettings.SetRivalMissionLocationSettings("body:Mun", 42.0, 0),
                "Science difficulty below one should be rejected.");
            Require(!CampaignSettings.SetRivalMissionLocationSettings("body:Mun", 42.0, 11),
                "Science difficulty above ten should be rejected.");
            Require(!CampaignSettings.SetRivalMissionLocationSettings("body:Sun", 78.0, 5),
                "Unapproved location IDs should not be added by config overrides.");
            AssertLocation("body:Mun", 42.0, 6);

            CampaignSettings.RivalKerbalHireCostFunds = 1.0;
            CampaignSettings.ResetToDefaults();
            AssertLocation("body:Mun", 30.0, 4);
            Require(CampaignSettings.RivalKerbalHireCostFunds == 100000.0,
                "ResetToDefaults should restore rival programme scalar balance as well as location data.");
        }

        private static void AssertLocation(
            string locationId,
            double expectedDurationDays,
            int expectedScienceDifficulty)
        {
            RivalMissionLocationSettings settings =
                CampaignSettings.GetRivalMissionLocationSettings(locationId);
            Require(settings != null, "Missing rival mission location '" + locationId + "'.");
            Require(settings.DurationDays == expectedDurationDays,
                locationId + " should use " + expectedDurationDays + " duration days.");
            Require(settings.ScienceDifficulty == expectedScienceDifficulty,
                locationId + " should use Science Difficulty " + expectedScienceDifficulty + ".");
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

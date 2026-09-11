using System;
using System.Collections.Generic;

namespace TheRaceForSpace.Core
{
    /// <summary>
    /// One balance tier shared by objective completion rewards, rival mission costs, and network funding.
    /// Values are initialized to the built-in defaults and may be replaced once at KSP startup
    /// by the user-editable CampaignSettings.cfg file.
    /// </summary>
    internal sealed class BodyBalanceSettings
    {
        public BodyBalanceSettings(
            double probeProgressCostFunds,
            double crewedProgressCostFunds,
            double probeRewardFunds,
            double crewedRewardFunds,
            double satelliteProgressCostFunds,
            int satelliteNetworkSize,
            double satelliteNetworkValueFunds)
        {
            ProbeProgressCostFunds = probeProgressCostFunds;
            CrewedProgressCostFunds = crewedProgressCostFunds;
            ProbeRewardFunds = probeRewardFunds;
            CrewedRewardFunds = crewedRewardFunds;
            SatelliteProgressCostFunds = satelliteProgressCostFunds;
            SatelliteNetworkSize = satelliteNetworkSize;
            SatelliteNetworkValueFunds = satelliteNetworkValueFunds;
        }

        public double ProbeProgressCostFunds { get; set; }
        public double CrewedProgressCostFunds { get; set; }
        public double ProbeRewardFunds { get; set; }
        public double CrewedRewardFunds { get; set; }
        public double SatelliteProgressCostFunds { get; set; }
        public int SatelliteNetworkSize { get; set; }
        public double SatelliteNetworkValueFunds { get; set; }
    }

    /// <summary>
    /// Tuneable travel duration and Science difficulty for one stable rival mission location.
    /// The values are snapshotted into live missions at launch so later config edits do not change
    /// a mission that is already in progress.
    /// </summary>
    internal sealed class RivalMissionLocationSettings
    {
        public RivalMissionLocationSettings(string locationId, double durationDays, int scienceDifficulty)
        {
            LocationId = locationId;
            DurationDays = durationDays;
            ScienceDifficulty = scienceDifficulty;
        }

        public string LocationId { get; private set; }
        public double DurationDays { get; private set; }
        public int ScienceDifficulty { get; private set; }

        internal void Apply(double durationDays, int scienceDifficulty)
        {
            DurationDays = durationDays;
            ScienceDifficulty = scienceDifficulty;
        }
    }

    /// <summary>
    /// Current campaign-wide balance settings. Defaults match the approved campaign behaviour and are
    /// replaced from GameData/TheRaceForSpace/Config/CampaignSettings.cfg before controller creation.
    /// </summary>
    internal static class CampaignSettings
    {
        public const string RivalPreOrbitLocalLocationId = "kerbin:preorbit-local";
        public const string RivalKerbinOrbitLocationId = "kerbin:orbit";

        private static readonly double[] DefaultPreOrbitRewardFundsByLevel =
            { 0.0, 10000.0, 20000.0, 30000.0, 40000.0, 50000.0 };
        private static readonly double[] DefaultPreOrbitRivalProgressCostFundsByLevel =
            { 0.0, 4000.0, 6000.0, 8000.0, 10000.0, 12000.0 };

        private static readonly Dictionary<string, RivalMissionLocationSettings> RivalMissionLocationsById =
            new Dictionary<string, RivalMissionLocationSettings>(StringComparer.OrdinalIgnoreCase);

        private static double[] _preOrbitRewardFundsByLevel;
        private static double[] _preOrbitRivalProgressCostFundsByLevel;

        static CampaignSettings()
        {
            ResetToDefaults();
        }

        public static BodyBalanceSettings Kerbin { get; private set; }
        public static BodyBalanceSettings KerbinMoons { get; private set; }
        public static BodyBalanceSettings InterplanetaryPlanets { get; private set; }
        public static BodyBalanceSettings InterplanetaryMoons { get; private set; }

        public static double FundingIntervalDays { get; set; }
        public static double RivalStartingFunds { get; set; }
        public static double RivalProgressChance { get; set; }
        public static int NumberOfRivals { get; set; }

        public static double RivalKerbalHireCostFunds { get; set; }
        public static double RivalKerbalPayrollFundsPerFundingBoundary { get; set; }
        public static double RivalInsuranceFundsPerLostKerbal { get; set; }
        public static double RivalKerbalLossChance { get; set; }

        public static double RivalResearchDurationDays { get; set; }
        public static double RivalFacilityLevel1To2CostFunds { get; set; }
        public static double RivalFacilityLevel1To2ConstructionDays { get; set; }
        public static double RivalFacilityLevel2To3CostFunds { get; set; }
        public static double RivalFacilityLevel2To3ConstructionDays { get; set; }

        public static double RivalAdministrationLevel1BaseIncomeFunds { get; set; }
        public static double RivalAdministrationLevel2BaseIncomeFunds { get; set; }
        public static double RivalAdministrationLevel3BaseIncomeFunds { get; set; }
        public static int RivalAstronautComplexLevel1KerbalLimit { get; set; }
        public static int RivalAstronautComplexLevel2KerbalLimit { get; set; }
        public static int RivalAstronautComplexLevel3KerbalLimit { get; set; }
        public static int RivalMissionControlLevel1SatelliteLimit { get; set; }
        public static int RivalMissionControlLevel2SatelliteLimit { get; set; }
        public static int RivalMissionControlLevel3SatelliteLimit { get; set; }
        public static double RivalResearchAndDevelopmentLevel1ScienceCostLimit { get; set; }
        public static double RivalResearchAndDevelopmentLevel2ScienceCostLimit { get; set; }
        public static double RivalResearchAndDevelopmentLevel3ScienceCostLimit { get; set; }
        public static int RivalTrackingStationKerbinLevel { get; set; }
        public static int RivalTrackingStationKerbinMoonsLevel { get; set; }
        public static int RivalTrackingStationInterplanetaryLevel { get; set; }

        public static double RivalNormalLaunchProgressCheckIntervalDays { get; set; }
        public static int RivalNormalLaunchProgressStepPercent { get; set; }
        public static int RivalPreOrbitLaunchProgressStepPercent { get; set; }
        public static double RivalNormalLaunchFacilityLevel1Chance { get; set; }
        public static double RivalNormalLaunchFacilityLevel2BonusChance { get; set; }
        public static double RivalNormalLaunchFacilityLevel3BonusChance { get; set; }
        public static double RivalScienceLaunchProgressCheckIntervalDays { get; set; }
        public static int RivalScienceLaunchProgressStepPercent { get; set; }
        public static double RivalScienceLaunchFacilityLevel1Chance { get; set; }
        public static double RivalScienceLaunchFacilityLevel2BonusChance { get; set; }
        public static double RivalScienceLaunchFacilityLevel3BonusChance { get; set; }

        internal static IEnumerable<RivalMissionLocationSettings> RivalMissionLocations
        {
            get { return RivalMissionLocationsById.Values; }
        }

        public static void ResetToDefaults()
        {
            Kerbin = new BodyBalanceSettings(
                20000.0,
                40000.0,
                75000.0,
                150000.0,
                20000.0,
                10,
                200000.0);
            KerbinMoons = new BodyBalanceSettings(
                40000.0,
                60000.0,
                150000.0,
                300000.0,
                40000.0,
                5,
                100000.0);
            InterplanetaryPlanets = new BodyBalanceSettings(
                60000.0,
                100000.0,
                300000.0,
                500000.0,
                80000.0,
                10,
                200000.0);
            InterplanetaryMoons = new BodyBalanceSettings(
                60000.0,
                100000.0,
                300000.0,
                500000.0,
                80000.0,
                5,
                100000.0);

            _preOrbitRewardFundsByLevel =
                (double[])DefaultPreOrbitRewardFundsByLevel.Clone();
            _preOrbitRivalProgressCostFundsByLevel =
                (double[])DefaultPreOrbitRivalProgressCostFundsByLevel.Clone();

            FundingIntervalDays = 90.0;
            RivalStartingFunds = 300000.0;

            // This legacy value remains active until the normal rival launch simulation is replaced
            // by the VAB + Launch Pad calculation in the later integration task.
            RivalProgressChance = 0.30;
            NumberOfRivals = 2;

            RivalKerbalHireCostFunds = 100000.0;
            RivalKerbalPayrollFundsPerFundingBoundary = 10000.0;
            RivalInsuranceFundsPerLostKerbal = 50000.0;
            RivalKerbalLossChance = 0.25;

            RivalResearchDurationDays = 90.0;
            RivalFacilityLevel1To2CostFunds = 100000.0;
            RivalFacilityLevel1To2ConstructionDays = 180.0;
            RivalFacilityLevel2To3CostFunds = 250000.0;
            RivalFacilityLevel2To3ConstructionDays = 270.0;

            RivalAdministrationLevel1BaseIncomeFunds = 10000.0;
            RivalAdministrationLevel2BaseIncomeFunds = 20000.0;
            RivalAdministrationLevel3BaseIncomeFunds = 40000.0;
            RivalAstronautComplexLevel1KerbalLimit = 3;
            RivalAstronautComplexLevel2KerbalLimit = 8;
            RivalAstronautComplexLevel3KerbalLimit = 0;
            RivalMissionControlLevel1SatelliteLimit = 3;
            RivalMissionControlLevel2SatelliteLimit = 8;
            RivalMissionControlLevel3SatelliteLimit = 0;
            RivalResearchAndDevelopmentLevel1ScienceCostLimit = 90.0;
            RivalResearchAndDevelopmentLevel2ScienceCostLimit = 300.0;
            RivalResearchAndDevelopmentLevel3ScienceCostLimit = 0.0;
            RivalTrackingStationKerbinLevel = 1;
            RivalTrackingStationKerbinMoonsLevel = 2;
            RivalTrackingStationInterplanetaryLevel = 3;

            RivalNormalLaunchProgressCheckIntervalDays = 5.0;
            RivalNormalLaunchProgressStepPercent = 10;
            RivalPreOrbitLaunchProgressStepPercent = 20;
            RivalNormalLaunchFacilityLevel1Chance = 0.15;
            RivalNormalLaunchFacilityLevel2BonusChance = 0.03;
            RivalNormalLaunchFacilityLevel3BonusChance = 0.03;
            RivalScienceLaunchProgressCheckIntervalDays = 1.0;
            RivalScienceLaunchProgressStepPercent = 10;
            RivalScienceLaunchFacilityLevel1Chance = 0.20;
            RivalScienceLaunchFacilityLevel2BonusChance = 0.03;
            RivalScienceLaunchFacilityLevel3BonusChance = 0.03;

            ResetRivalMissionLocations();
        }

        /// <summary>
        /// Returns the configured one-off funding reward for a Pre-Orbit level.
        /// Invalid levels return zero rather than borrowing another level's balance.
        /// </summary>
        public static double GetPreOrbitRewardFunds(int preOrbitLevel)
        {
            return IsValidPreOrbitLevel(preOrbitLevel)
                ? _preOrbitRewardFundsByLevel[preOrbitLevel]
                : 0.0;
        }

        /// <summary>
        /// Returns the configured rival cost for one successful 20% Pre-Orbit progress step.
        /// Invalid levels return zero rather than borrowing another level's balance.
        /// </summary>
        public static double GetPreOrbitRivalProgressCostFunds(int preOrbitLevel)
        {
            return IsValidPreOrbitLevel(preOrbitLevel)
                ? _preOrbitRivalProgressCostFundsByLevel[preOrbitLevel]
                : 0.0;
        }

        public static void SetPreOrbitRewardFunds(int preOrbitLevel, double rewardFunds)
        {
            if (IsValidPreOrbitLevel(preOrbitLevel))
            {
                _preOrbitRewardFundsByLevel[preOrbitLevel] = Math.Max(0.0, rewardFunds);
            }
        }

        public static void SetPreOrbitRivalProgressCostFunds(
            int preOrbitLevel,
            double progressCostFunds)
        {
            if (IsValidPreOrbitLevel(preOrbitLevel))
            {
                _preOrbitRivalProgressCostFundsByLevel[preOrbitLevel] =
                    Math.Max(0.0, progressCostFunds);
            }
        }

        /// <summary>
        /// Returns the stock-system balance tier for a funding target body. Unknown bodies fall
        /// back to the interplanetary-planet tier rather than receiving cheap Kerbin defaults.
        /// </summary>
        public static BodyBalanceSettings GetBodySettings(string celestialBodyName)
        {
            if (string.Equals(celestialBodyName, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                return Kerbin;
            }

            if (string.Equals(celestialBodyName, "Mun", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Minmus", StringComparison.OrdinalIgnoreCase))
            {
                return KerbinMoons;
            }

            if (string.Equals(celestialBodyName, "Gilly", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Ike", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Laythe", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Vall", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Tylo", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Bop", StringComparison.OrdinalIgnoreCase)
                || string.Equals(celestialBodyName, "Pol", StringComparison.OrdinalIgnoreCase))
            {
                return InterplanetaryMoons;
            }

            return InterplanetaryPlanets;
        }

        /// <summary>
        /// Returns configured rival mission-location balance by stable project-owned ID.
        /// Unknown or empty IDs return null so callers can fail safely rather than borrowing another location.
        /// </summary>
        public static RivalMissionLocationSettings GetRivalMissionLocationSettings(string locationId)
        {
            if (string.IsNullOrEmpty(locationId))
            {
                return null;
            }

            RivalMissionLocationSettings settings;
            return RivalMissionLocationsById.TryGetValue(locationId, out settings)
                ? settings
                : null;
        }

        /// <summary>
        /// Applies one validated config override to an existing approved mission location.
        /// Unknown IDs are rejected to make misspelled config locations visible instead of silently adding data.
        /// </summary>
        internal static bool SetRivalMissionLocationSettings(
            string locationId,
            double durationDays,
            int scienceDifficulty)
        {
            RivalMissionLocationSettings settings = GetRivalMissionLocationSettings(locationId);
            if (settings == null
                || double.IsNaN(durationDays)
                || double.IsInfinity(durationDays)
                || durationDays <= 0.0
                || scienceDifficulty < 1
                || scienceDifficulty > 10)
            {
                return false;
            }

            settings.Apply(durationDays, scienceDifficulty);
            return true;
        }

        private static bool IsValidPreOrbitLevel(int preOrbitLevel)
        {
            return preOrbitLevel > 0
                && _preOrbitRewardFundsByLevel != null
                && _preOrbitRivalProgressCostFundsByLevel != null
                && preOrbitLevel < _preOrbitRewardFundsByLevel.Length
                && preOrbitLevel < _preOrbitRivalProgressCostFundsByLevel.Length;
        }

        private static void ResetRivalMissionLocations()
        {
            RivalMissionLocationsById.Clear();

            // Contract-only local target. Science difficulty is retained as a valid value for the shared
            // location record but Contract missions continue to use their definition-owned difficulty.
            AddDefaultRivalMissionLocation(RivalPreOrbitLocalLocationId, 5.0, 1);

            AddDefaultRivalMissionLocation("kerbin:shores", 10.0, 1);
            AddDefaultRivalMissionLocation("kerbin:water", 10.0, 1);
            AddDefaultRivalMissionLocation("kerbin:grasslands", 10.0, 1);
            AddDefaultRivalMissionLocation("kerbin:highlands", 10.0, 1);
            AddDefaultRivalMissionLocation("kerbin:mountains", 20.0, 3);
            AddDefaultRivalMissionLocation("kerbin:deserts", 30.0, 2);
            AddDefaultRivalMissionLocation("kerbin:badlands", 70.0, 4);
            AddDefaultRivalMissionLocation("kerbin:tundra", 80.0, 3);
            AddDefaultRivalMissionLocation("kerbin:ice-caps", 90.0, 4);
            AddDefaultRivalMissionLocation("kerbin:northern-ice-shelf", 100.0, 4);
            AddDefaultRivalMissionLocation("kerbin:southern-ice-shelf", 100.0, 4);

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
                AddDefaultRivalMissionLocation(kscLocationIds[locationIndex], 5.0, 1);
            }

            AddDefaultRivalMissionLocation(RivalKerbinOrbitLocationId, 10.0, 3);
            AddDefaultRivalMissionLocation("body:Mun", 30.0, 4);
            AddDefaultRivalMissionLocation("body:Minmus", 50.0, 4);
            AddDefaultRivalMissionLocation("body:Moho", 124.0, 9);
            AddDefaultRivalMissionLocation("body:Eve", 171.0, 10);
            AddDefaultRivalMissionLocation("body:Gilly", 174.0, 5);
            AddDefaultRivalMissionLocation("body:Duna", 303.0, 7);
            AddDefaultRivalMissionLocation("body:Ike", 303.0, 5);
            AddDefaultRivalMissionLocation("body:Dres", 604.0, 5);
            AddDefaultRivalMissionLocation("body:Jool", 1123.0, 7);
            AddDefaultRivalMissionLocation("body:Laythe", 1124.0, 8);
            AddDefaultRivalMissionLocation("body:Vall", 1124.0, 8);
            AddDefaultRivalMissionLocation("body:Tylo", 1125.0, 8);
            AddDefaultRivalMissionLocation("body:Bop", 1128.0, 8);
            AddDefaultRivalMissionLocation("body:Pol", 1131.0, 8);
            AddDefaultRivalMissionLocation("body:Eeloo", 1587.0, 10);
        }

        private static void AddDefaultRivalMissionLocation(
            string locationId,
            double durationDays,
            int scienceDifficulty)
        {
            RivalMissionLocationsById.Add(
                locationId,
                new RivalMissionLocationSettings(locationId, durationDays, scienceDifficulty));
        }
    }
}

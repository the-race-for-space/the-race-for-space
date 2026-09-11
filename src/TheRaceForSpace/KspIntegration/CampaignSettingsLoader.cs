using System;
using System.Globalization;
using TheRaceForSpace.Core;
using UnityEngine;

namespace TheRaceForSpace.KspIntegration
{
    /// <summary>
    /// Loads the user-editable campaign balance config once before the first campaign controller is created.
    /// Missing or invalid values keep the built-in defaults from CampaignSettings.
    /// </summary>
    internal static class CampaignSettingsLoader
    {
        private const string RootNodeName = "THE_RACE_FOR_SPACE_SETTINGS";
        private const string ConfigRelativePath = "GameData/TheRaceForSpace/Config/CampaignSettings.cfg";
        private static bool _hasLoaded;

        public static void EnsureLoaded()
        {
            if (_hasLoaded)
            {
                return;
            }

            _hasLoaded = true;
            CampaignSettings.ResetToDefaults();

            ConfigNode configFile;
            try
            {
                configFile = ConfigNode.Load(KSPUtil.ApplicationRootPath + ConfigRelativePath);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[TheRaceForSpace] Could not read CampaignSettings.cfg; using defaults. "
                    + exception.Message);
                return;
            }

            ConfigNode rootNode = configFile == null ? null : configFile.GetNode(RootNodeName);
            if (rootNode == null)
            {
                Debug.LogWarning("[TheRaceForSpace] CampaignSettings.cfg missing settings node; using defaults.");
                return;
            }

            CampaignSettings.FundingIntervalDays = ReadDouble(
                rootNode,
                "fundingIntervalDays",
                CampaignSettings.FundingIntervalDays,
                0.000001,
                double.MaxValue);
            CampaignSettings.RivalStartingFunds = ReadDouble(
                rootNode,
                "rivalStartingFunds",
                CampaignSettings.RivalStartingFunds,
                0.0,
                double.MaxValue);

            // The legacy global progress chance remains live until the later rival-simulation task
            // replaces it with the configured VAB + Launch Pad facility calculation.
            CampaignSettings.RivalProgressChance = ReadDouble(
                rootNode,
                "rivalProgressChancePercent",
                CampaignSettings.RivalProgressChance * 100.0,
                0.0,
                100.0) / 100.0;
            CampaignSettings.NumberOfRivals = ReadInt(
                rootNode,
                "numberOfRivals",
                CampaignSettings.NumberOfRivals,
                0,
                int.MaxValue);

            ApplyRivalProgrammeSettings(rootNode.GetNode("RIVAL_PROGRAMME"));
            ApplyPreOrbitSettings(rootNode.GetNode("PRE_ORBIT"));
            ApplyBodySettings(rootNode.GetNode("KERBIN"), CampaignSettings.Kerbin);
            ApplyBodySettings(rootNode.GetNode("KERBIN_MOONS"), CampaignSettings.KerbinMoons);
            ApplyBodySettings(
                rootNode.GetNode("INTERPLANETARY_PLANETS"),
                CampaignSettings.InterplanetaryPlanets);
            ApplyBodySettings(
                rootNode.GetNode("INTERPLANETARY_MOONS"),
                CampaignSettings.InterplanetaryMoons);

            Debug.Log("[TheRaceForSpace] Loaded CampaignSettings.cfg.");
        }

        private static void ApplyRivalProgrammeSettings(ConfigNode node)
        {
            if (node == null)
            {
                return;
            }

            ApplyRivalKerbalSettings(node.GetNode("KERBALS"));
            ApplyRivalConstructionSettings(node.GetNode("CONSTRUCTION"));
            ApplyRivalResearchSettings(node.GetNode("RESEARCH"));
            ApplyRivalFacilitySettings(node.GetNode("FACILITIES"));
            ApplyRivalLaunchProgressSettings(node.GetNode("LAUNCH_PROGRESS"));
            ApplyRivalMissionLocationSettings(node.GetNode("MISSION_LOCATIONS"));
        }

        private static void ApplyRivalKerbalSettings(ConfigNode node)
        {
            if (node == null)
            {
                return;
            }

            CampaignSettings.RivalKerbalHireCostFunds = ReadDouble(
                node,
                "hireCost",
                CampaignSettings.RivalKerbalHireCostFunds,
                0.0,
                double.MaxValue);
            CampaignSettings.RivalKerbalPayrollFundsPerFundingBoundary = ReadDouble(
                node,
                "payrollPerFundingBoundary",
                CampaignSettings.RivalKerbalPayrollFundsPerFundingBoundary,
                0.0,
                double.MaxValue);
            CampaignSettings.RivalInsuranceFundsPerLostKerbal = ReadDouble(
                node,
                "insurancePerLostKerbal",
                CampaignSettings.RivalInsuranceFundsPerLostKerbal,
                0.0,
                double.MaxValue);
            CampaignSettings.RivalKerbalLossChance = ReadDouble(
                node,
                "lossChancePercent",
                CampaignSettings.RivalKerbalLossChance * 100.0,
                0.0,
                100.0) / 100.0;
        }

        private static void ApplyRivalConstructionSettings(ConfigNode node)
        {
            if (node == null)
            {
                return;
            }

            CampaignSettings.RivalFacilityLevel1To2CostFunds = ReadDouble(
                node,
                "level1To2Cost",
                CampaignSettings.RivalFacilityLevel1To2CostFunds,
                0.0,
                double.MaxValue);
            CampaignSettings.RivalFacilityLevel1To2ConstructionDays = ReadDouble(
                node,
                "level1To2DurationDays",
                CampaignSettings.RivalFacilityLevel1To2ConstructionDays,
                0.000001,
                double.MaxValue);
            CampaignSettings.RivalFacilityLevel2To3CostFunds = ReadDouble(
                node,
                "level2To3Cost",
                CampaignSettings.RivalFacilityLevel2To3CostFunds,
                0.0,
                double.MaxValue);
            CampaignSettings.RivalFacilityLevel2To3ConstructionDays = ReadDouble(
                node,
                "level2To3DurationDays",
                CampaignSettings.RivalFacilityLevel2To3ConstructionDays,
                0.000001,
                double.MaxValue);
        }

        private static void ApplyRivalResearchSettings(ConfigNode node)
        {
            if (node == null)
            {
                return;
            }

            CampaignSettings.RivalResearchDurationDays = ReadDouble(
                node,
                "durationDays",
                CampaignSettings.RivalResearchDurationDays,
                0.000001,
                double.MaxValue);
        }

        private static void ApplyRivalFacilitySettings(ConfigNode node)
        {
            if (node == null)
            {
                return;
            }

            CampaignSettings.RivalAdministrationLevel1BaseIncomeFunds = ReadDouble(
                node,
                "administrationLevel1BaseIncome",
                CampaignSettings.RivalAdministrationLevel1BaseIncomeFunds,
                0.0,
                double.MaxValue);
            CampaignSettings.RivalAdministrationLevel2BaseIncomeFunds = ReadDouble(
                node,
                "administrationLevel2BaseIncome",
                CampaignSettings.RivalAdministrationLevel2BaseIncomeFunds,
                0.0,
                double.MaxValue);
            CampaignSettings.RivalAdministrationLevel3BaseIncomeFunds = ReadDouble(
                node,
                "administrationLevel3BaseIncome",
                CampaignSettings.RivalAdministrationLevel3BaseIncomeFunds,
                0.0,
                double.MaxValue);

            CampaignSettings.RivalAstronautComplexLevel1KerbalLimit = ReadInt(
                node,
                "astronautComplexLevel1KerbalLimit",
                CampaignSettings.RivalAstronautComplexLevel1KerbalLimit,
                1,
                int.MaxValue);
            CampaignSettings.RivalAstronautComplexLevel2KerbalLimit = ReadInt(
                node,
                "astronautComplexLevel2KerbalLimit",
                CampaignSettings.RivalAstronautComplexLevel2KerbalLimit,
                1,
                int.MaxValue);
            CampaignSettings.RivalAstronautComplexLevel3KerbalLimit = ReadInt(
                node,
                "astronautComplexLevel3KerbalLimit",
                CampaignSettings.RivalAstronautComplexLevel3KerbalLimit,
                0,
                int.MaxValue);

            CampaignSettings.RivalMissionControlLevel1SatelliteLimit = ReadInt(
                node,
                "missionControlLevel1SatelliteLimit",
                CampaignSettings.RivalMissionControlLevel1SatelliteLimit,
                1,
                int.MaxValue);
            CampaignSettings.RivalMissionControlLevel2SatelliteLimit = ReadInt(
                node,
                "missionControlLevel2SatelliteLimit",
                CampaignSettings.RivalMissionControlLevel2SatelliteLimit,
                1,
                int.MaxValue);
            CampaignSettings.RivalMissionControlLevel3SatelliteLimit = ReadInt(
                node,
                "missionControlLevel3SatelliteLimit",
                CampaignSettings.RivalMissionControlLevel3SatelliteLimit,
                0,
                int.MaxValue);

            CampaignSettings.RivalResearchAndDevelopmentLevel1ScienceCostLimit = ReadDouble(
                node,
                "researchAndDevelopmentLevel1ScienceCostLimit",
                CampaignSettings.RivalResearchAndDevelopmentLevel1ScienceCostLimit,
                0.0,
                double.MaxValue);
            CampaignSettings.RivalResearchAndDevelopmentLevel2ScienceCostLimit = ReadDouble(
                node,
                "researchAndDevelopmentLevel2ScienceCostLimit",
                CampaignSettings.RivalResearchAndDevelopmentLevel2ScienceCostLimit,
                0.0,
                double.MaxValue);
            CampaignSettings.RivalResearchAndDevelopmentLevel3ScienceCostLimit = ReadDouble(
                node,
                "researchAndDevelopmentLevel3ScienceCostLimit",
                CampaignSettings.RivalResearchAndDevelopmentLevel3ScienceCostLimit,
                0.0,
                double.MaxValue);

            CampaignSettings.RivalTrackingStationKerbinLevel = ReadInt(
                node,
                "trackingStationKerbinLevel",
                CampaignSettings.RivalTrackingStationKerbinLevel,
                1,
                3);
            CampaignSettings.RivalTrackingStationKerbinMoonsLevel = ReadInt(
                node,
                "trackingStationKerbinMoonsLevel",
                CampaignSettings.RivalTrackingStationKerbinMoonsLevel,
                1,
                3);
            CampaignSettings.RivalTrackingStationInterplanetaryLevel = ReadInt(
                node,
                "trackingStationInterplanetaryLevel",
                CampaignSettings.RivalTrackingStationInterplanetaryLevel,
                1,
                3);
        }

        private static void ApplyRivalLaunchProgressSettings(ConfigNode node)
        {
            if (node == null)
            {
                return;
            }

            CampaignSettings.RivalNormalLaunchProgressCheckIntervalDays = ReadDouble(
                node,
                "normalCheckIntervalDays",
                CampaignSettings.RivalNormalLaunchProgressCheckIntervalDays,
                0.000001,
                double.MaxValue);
            CampaignSettings.RivalNormalLaunchProgressStepPercent = ReadInt(
                node,
                "normalProgressStepPercent",
                CampaignSettings.RivalNormalLaunchProgressStepPercent,
                1,
                100);
            CampaignSettings.RivalPreOrbitLaunchProgressStepPercent = ReadInt(
                node,
                "preOrbitProgressStepPercent",
                CampaignSettings.RivalPreOrbitLaunchProgressStepPercent,
                1,
                100);
            CampaignSettings.RivalNormalLaunchFacilityLevel1Chance = ReadDouble(
                node,
                "normalFacilityLevel1ChancePercent",
                CampaignSettings.RivalNormalLaunchFacilityLevel1Chance * 100.0,
                0.0,
                100.0) / 100.0;
            CampaignSettings.RivalNormalLaunchFacilityLevel2BonusChance = ReadDouble(
                node,
                "normalFacilityLevel2BonusChancePercent",
                CampaignSettings.RivalNormalLaunchFacilityLevel2BonusChance * 100.0,
                0.0,
                100.0) / 100.0;
            CampaignSettings.RivalNormalLaunchFacilityLevel3BonusChance = ReadDouble(
                node,
                "normalFacilityLevel3BonusChancePercent",
                CampaignSettings.RivalNormalLaunchFacilityLevel3BonusChance * 100.0,
                0.0,
                100.0) / 100.0;

            CampaignSettings.RivalScienceLaunchProgressCheckIntervalDays = ReadDouble(
                node,
                "scienceCheckIntervalDays",
                CampaignSettings.RivalScienceLaunchProgressCheckIntervalDays,
                0.000001,
                double.MaxValue);
            CampaignSettings.RivalScienceLaunchProgressStepPercent = ReadInt(
                node,
                "scienceProgressStepPercent",
                CampaignSettings.RivalScienceLaunchProgressStepPercent,
                1,
                100);
            CampaignSettings.RivalScienceLaunchFacilityLevel1Chance = ReadDouble(
                node,
                "scienceFacilityLevel1ChancePercent",
                CampaignSettings.RivalScienceLaunchFacilityLevel1Chance * 100.0,
                0.0,
                100.0) / 100.0;
            CampaignSettings.RivalScienceLaunchFacilityLevel2BonusChance = ReadDouble(
                node,
                "scienceFacilityLevel2BonusChancePercent",
                CampaignSettings.RivalScienceLaunchFacilityLevel2BonusChance * 100.0,
                0.0,
                100.0) / 100.0;
            CampaignSettings.RivalScienceLaunchFacilityLevel3BonusChance = ReadDouble(
                node,
                "scienceFacilityLevel3BonusChancePercent",
                CampaignSettings.RivalScienceLaunchFacilityLevel3BonusChance * 100.0,
                0.0,
                100.0) / 100.0;
        }

        private static void ApplyRivalMissionLocationSettings(ConfigNode node)
        {
            if (node == null)
            {
                return;
            }

            ConfigNode[] locationNodes = node.GetNodes("LOCATION");
            for (int locationIndex = 0; locationIndex < locationNodes.Length; locationIndex++)
            {
                ConfigNode locationNode = locationNodes[locationIndex];
                if (locationNode == null)
                {
                    continue;
                }

                string locationId = locationNode.GetValue("id");
                RivalMissionLocationSettings currentSettings =
                    CampaignSettings.GetRivalMissionLocationSettings(locationId);
                if (currentSettings == null)
                {
                    Debug.LogWarning(
                        "[TheRaceForSpace] Unknown rival mission location '"
                        + (locationId ?? string.Empty)
                        + "' in CampaignSettings.cfg; ignoring it.");
                    continue;
                }

                double durationDays = ReadDouble(
                    locationNode,
                    "durationDays",
                    currentSettings.DurationDays,
                    0.000001,
                    double.MaxValue);
                int scienceDifficulty = ReadInt(
                    locationNode,
                    "scienceDifficulty",
                    currentSettings.ScienceDifficulty,
                    1,
                    10);

                CampaignSettings.SetRivalMissionLocationSettings(
                    locationId,
                    durationDays,
                    scienceDifficulty);
            }
        }

        private static void ApplyPreOrbitSettings(ConfigNode node)
        {
            if (node == null)
            {
                return;
            }

            for (int level = 1; level <= 5; level++)
            {
                string levelText = level.ToString(CultureInfo.InvariantCulture);
                double rewardFunds = ReadDouble(
                    node,
                    "level" + levelText + "Reward",
                    CampaignSettings.GetPreOrbitRewardFunds(level),
                    0.0,
                    double.MaxValue);
                double rivalProgressCostFunds = ReadDouble(
                    node,
                    "level" + levelText + "RivalProgressCost",
                    CampaignSettings.GetPreOrbitRivalProgressCostFunds(level),
                    0.0,
                    double.MaxValue);

                CampaignSettings.SetPreOrbitRewardFunds(level, rewardFunds);
                CampaignSettings.SetPreOrbitRivalProgressCostFunds(level, rivalProgressCostFunds);
            }
        }

        private static void ApplyBodySettings(ConfigNode node, BodyBalanceSettings settings)
        {
            if (node == null)
            {
                return;
            }

            settings.ProbeProgressCostFunds = ReadDouble(
                node, "probeProgressCost", settings.ProbeProgressCostFunds, 0.0, double.MaxValue);
            settings.CrewedProgressCostFunds = ReadDouble(
                node, "crewedProgressCost", settings.CrewedProgressCostFunds, 0.0, double.MaxValue);
            settings.ProbeRewardFunds = ReadDouble(
                node, "probeReward", settings.ProbeRewardFunds, 0.0, double.MaxValue);
            settings.CrewedRewardFunds = ReadDouble(
                node, "crewedReward", settings.CrewedRewardFunds, 0.0, double.MaxValue);
            settings.SatelliteProgressCostFunds = ReadDouble(
                node, "satelliteProgressCost", settings.SatelliteProgressCostFunds, 0.0, double.MaxValue);
            settings.SatelliteNetworkSize = ReadInt(
                node, "satelliteNetworkSize", settings.SatelliteNetworkSize, 1, int.MaxValue);
            settings.SatelliteNetworkValueFunds = ReadDouble(
                node, "satelliteNetworkValue", settings.SatelliteNetworkValueFunds, 0.0, double.MaxValue);
        }

        private static double ReadDouble(
            ConfigNode node,
            string valueName,
            double defaultValue,
            double minimumValue,
            double maximumValue)
        {
            string text = node.GetValue(valueName);
            if (string.IsNullOrEmpty(text))
            {
                return defaultValue;
            }

            double parsedValue;
            if (double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out parsedValue)
                && !double.IsNaN(parsedValue)
                && !double.IsInfinity(parsedValue)
                && parsedValue >= minimumValue
                && parsedValue <= maximumValue)
            {
                return parsedValue;
            }

            LogInvalidValue(valueName, text, defaultValue);
            return defaultValue;
        }

        private static int ReadInt(
            ConfigNode node,
            string valueName,
            int defaultValue,
            int minimumValue,
            int maximumValue)
        {
            string text = node.GetValue(valueName);
            if (string.IsNullOrEmpty(text))
            {
                return defaultValue;
            }

            int parsedValue;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue)
                && parsedValue >= minimumValue
                && parsedValue <= maximumValue)
            {
                return parsedValue;
            }

            LogInvalidValue(valueName, text, defaultValue);
            return defaultValue;
        }

        private static void LogInvalidValue(string valueName, string suppliedValue, object defaultValue)
        {
            Debug.LogWarning(
                "[TheRaceForSpace] Invalid config "
                + valueName
                + "='"
                + suppliedValue
                + "'; using "
                + defaultValue
                + ".");
        }
    }
}

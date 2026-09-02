using System;
using System.Globalization;
using TheRaceForSpace.Core;
using UnityEngine;

namespace TheRaceForSpace.KspIntegration
{
    /// <summary>
    /// Loads the user-editable race balance config once after KSP's GameDatabase is ready.
    /// Missing or invalid values keep the built-in defaults from RaceSettings.
    /// </summary>
    internal static class RaceSettingsLoader
    {
        private const string RootNodeName = "THE_RACE_FOR_SPACE_SETTINGS";
        private static bool _hasLoaded;

        public static void EnsureLoaded()
        {
            if (_hasLoaded)
            {
                return;
            }

            _hasLoaded = true;
            RaceSettings.ResetToDefaults();

            if (GameDatabase.Instance == null)
            {
                Debug.LogWarning("[TheRaceForSpace] GameDatabase unavailable; using default race settings.");
                return;
            }

            ConfigNode[] settingsNodes = GameDatabase.Instance.GetConfigNodes(RootNodeName);
            if (settingsNodes == null || settingsNodes.Length == 0)
            {
                Debug.LogWarning("[TheRaceForSpace] RaceSettings.cfg not found; using default race settings.");
                return;
            }

            // If another config patch creates the same node, the last loaded node wins.
            ConfigNode rootNode = settingsNodes[settingsNodes.Length - 1];
            RaceSettings.FundingIntervalDays = ReadDouble(
                rootNode,
                "fundingIntervalDays",
                RaceSettings.FundingIntervalDays,
                0.000001,
                double.MaxValue);
            RaceSettings.RivalStartingFunds = ReadDouble(
                rootNode,
                "rivalStartingFunds",
                RaceSettings.RivalStartingFunds,
                0.0,
                double.MaxValue);
            RaceSettings.RivalProgressChance = ReadDouble(
                rootNode,
                "rivalProgressChancePercent",
                RaceSettings.RivalProgressChance * 100.0,
                0.0,
                100.0) / 100.0;
            RaceSettings.NumberOfRivals = ReadInt(
                rootNode,
                "numberOfRivals",
                RaceSettings.NumberOfRivals,
                0,
                int.MaxValue);

            ApplyBodySettings(rootNode.GetNode("KERBIN"), RaceSettings.Kerbin);
            ApplyBodySettings(rootNode.GetNode("KERBIN_MOONS"), RaceSettings.KerbinMoons);
            ApplyBodySettings(
                rootNode.GetNode("INTERPLANETARY_PLANETS"),
                RaceSettings.InterplanetaryPlanets);
            ApplyBodySettings(
                rootNode.GetNode("INTERPLANETARY_MOONS"),
                RaceSettings.InterplanetaryMoons);

            Debug.Log("[TheRaceForSpace] Loaded race settings from GameData config.");
        }

        private static void ApplyBodySettings(ConfigNode node, RaceBodySettings settings)
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

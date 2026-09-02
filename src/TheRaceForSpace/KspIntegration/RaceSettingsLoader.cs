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
                Debug.LogWarning("[TheRaceForSpace] GameDatabase was unavailable; using default race settings.");
                return;
            }

            ConfigNode[] settingsNodes = GameDatabase.Instance.GetConfigNodes(RootNodeName);
            if (settingsNodes == null || settingsNodes.Length == 0)
            {
                Debug.LogWarning("[TheRaceForSpace] RaceSettings.cfg was not found; using default race settings.");
                return;
            }

            // If another config patch creates the same node, the last loaded node wins.
            ConfigNode rootNode = settingsNodes[settingsNodes.Length - 1];
            RaceSettings.FundingIntervalDays = ReadPositiveDouble(
                rootNode,
                "fundingIntervalDays",
                RaceSettings.FundingIntervalDays);
            RaceSettings.RivalStartingFunds = ReadNonNegativeDouble(
                rootNode,
                "rivalStartingFunds",
                RaceSettings.RivalStartingFunds);
            RaceSettings.RivalProgressChance = ReadPercent(
                rootNode,
                "rivalProgressChancePercent",
                RaceSettings.RivalProgressChance);
            RaceSettings.NumberOfRivals = ReadNonNegativeInt(
                rootNode,
                "numberOfRivals",
                RaceSettings.NumberOfRivals);

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
            if (node == null || settings == null)
            {
                return;
            }

            settings.ProbeProgressCostFunds = ReadNonNegativeDouble(
                node,
                "probeProgressCost",
                settings.ProbeProgressCostFunds);
            settings.CrewedProgressCostFunds = ReadNonNegativeDouble(
                node,
                "crewedProgressCost",
                settings.CrewedProgressCostFunds);
            settings.ProbeRewardFunds = ReadNonNegativeDouble(
                node,
                "probeReward",
                settings.ProbeRewardFunds);
            settings.CrewedRewardFunds = ReadNonNegativeDouble(
                node,
                "crewedReward",
                settings.CrewedRewardFunds);
            settings.SatelliteProgressCostFunds = ReadNonNegativeDouble(
                node,
                "satelliteProgressCost",
                settings.SatelliteProgressCostFunds);
            settings.SatelliteNetworkSize = ReadPositiveInt(
                node,
                "satelliteNetworkSize",
                settings.SatelliteNetworkSize);
            settings.SatelliteNetworkValueFunds = ReadNonNegativeDouble(
                node,
                "satelliteNetworkValue",
                settings.SatelliteNetworkValueFunds);
        }

        private static double ReadNonNegativeDouble(ConfigNode node, string valueName, double defaultValue)
        {
            string text = node == null ? null : node.GetValue(valueName);
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
                && parsedValue >= 0.0)
            {
                return parsedValue;
            }

            LogInvalidValue(valueName, text, defaultValue);
            return defaultValue;
        }

        private static double ReadPositiveDouble(ConfigNode node, string valueName, double defaultValue)
        {
            double parsedValue = ReadNonNegativeDouble(node, valueName, defaultValue);
            if (parsedValue > 0.0)
            {
                return parsedValue;
            }

            string text = node == null ? null : node.GetValue(valueName);
            if (!string.IsNullOrEmpty(text))
            {
                LogInvalidValue(valueName, text, defaultValue);
            }

            return defaultValue;
        }

        private static int ReadNonNegativeInt(ConfigNode node, string valueName, int defaultValue)
        {
            string text = node == null ? null : node.GetValue(valueName);
            int parsedValue;
            if (string.IsNullOrEmpty(text))
            {
                return defaultValue;
            }

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue)
                && parsedValue >= 0)
            {
                return parsedValue;
            }

            LogInvalidValue(valueName, text, defaultValue);
            return defaultValue;
        }

        private static int ReadPositiveInt(ConfigNode node, string valueName, int defaultValue)
        {
            int parsedValue = ReadNonNegativeInt(node, valueName, defaultValue);
            if (parsedValue > 0)
            {
                return parsedValue;
            }

            string text = node == null ? null : node.GetValue(valueName);
            if (!string.IsNullOrEmpty(text))
            {
                LogInvalidValue(valueName, text, defaultValue);
            }

            return defaultValue;
        }

        private static double ReadPercent(ConfigNode node, string valueName, double defaultProbability)
        {
            double defaultPercent = defaultProbability * 100.0;
            double parsedPercent = ReadNonNegativeDouble(node, valueName, defaultPercent);
            if (parsedPercent <= 100.0)
            {
                return parsedPercent / 100.0;
            }

            string text = node == null ? null : node.GetValue(valueName);
            LogInvalidValue(valueName, text, defaultPercent);
            return defaultProbability;
        }

        private static void LogInvalidValue(string valueName, string suppliedValue, object defaultValue)
        {
            Debug.LogWarning(
                "[TheRaceForSpace] Invalid config value "
                + valueName
                + "='"
                + suppliedValue
                + "'. Using default "
                + defaultValue
                + ".");
        }
    }
}

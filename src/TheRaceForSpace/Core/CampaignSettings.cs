using System;

namespace TheRaceForSpace.Core
{
    /// <summary>
    /// One balance tier shared by objectiveCompletion rewards, rival mission costs, and network funding.
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
    /// Current campaign-wide balance settings. Defaults match version 0.4 behaviour and are
    /// replaced from GameData/TheRaceForSpace/Config/CampaignSettings.cfg before controller creation.
    /// </summary>
    internal static class CampaignSettings
    {
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

            FundingIntervalDays = 90.0;
            RivalStartingFunds = 300000.0;
            RivalProgressChance = 0.30;
            NumberOfRivals = 2;
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
    }
}

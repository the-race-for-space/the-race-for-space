using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Tracking;

/// <summary>
/// Test-only stand-in for KSP universal time. The production controller references Planetarium
/// directly, so the standalone regression project supplies only the members that controller uses.
/// </summary>
public static class Planetarium
{
    public static object fetch = new object();

    public static double CurrentUniversalTime { get; set; }

    public static double GetUniversalTime()
    {
        return CurrentUniversalTime;
    }

    public static void Reset()
    {
        fetch = new object();
        CurrentUniversalTime = 0.0;
    }
}

namespace TheRaceForSpace.KspIntegration
{
    /// <summary>
    /// Test-only Career funding boundary that records controller awards without a live KSP economy.
    /// </summary>
    public static class CareerFundingAdapter
    {
        public static double TotalAddedFunds { get; private set; }
        public static int AddFundsCalls { get; private set; }

        public static bool TryAddFunds(double amount)
        {
            if (amount <= 0.0)
            {
                return false;
            }

            TotalAddedFunds += amount;
            AddFundsCalls++;
            return true;
        }

        public static void Reset()
        {
            TotalAddedFunds = 0.0;
            AddFundsCalls = 0;
        }
    }

    /// <summary>
    /// Test-only vessel discovery boundary. Tests decide when a KSP snapshot is available and
    /// which observation time belongs to it, matching the production adapter contract.
    /// </summary>
    public static class KspVesselMonitor
    {
        private static IList<OrbitingVesselSnapshot> _vesselSnapshots;
        private static double _observationUniversalTime = -1.0;

        public static bool IsReady { get; private set; }
        public static int CaptureCalls { get; private set; }

        public static void SetSnapshots(
            IList<OrbitingVesselSnapshot> vesselSnapshots,
            double observationUniversalTime)
        {
            _vesselSnapshots = vesselSnapshots;
            _observationUniversalTime = observationUniversalTime;
            IsReady = true;
        }

        public static void SetUnavailable()
        {
            _vesselSnapshots = null;
            _observationUniversalTime = -1.0;
            IsReady = false;
        }

        public static bool TryCaptureOrbitingVesselSnapshots(
            out IList<OrbitingVesselSnapshot> vesselSnapshots,
            out double currentUniversalTime)
        {
            CaptureCalls++;
            vesselSnapshots = IsReady ? _vesselSnapshots : null;
            currentUniversalTime = IsReady ? _observationUniversalTime : -1.0;
            return IsReady;
        }

        public static void Reset()
        {
            SetUnavailable();
            CaptureCalls = 0;
        }
    }

    /// <summary>
    /// Test-only ScenarioModule boundary. It models readiness and records whether the controller
    /// reached the persistence capture point without serializing KSP ConfigNodes.
    /// </summary>
    public static class ModPersistenceScenario
    {
        public static bool IsReady { get; set; }
        public static int RivalCaptureCalls { get; private set; }
        public static int RaceProgressCaptureCalls { get; private set; }
        public static double RestoredNextFundingUniversalTime { get; set; }
        public static double LastCapturedNextFundingUniversalTime { get; private set; }

        public static bool TryRestoreRivalAgencyState(IList<AgencyState> rivalAgencies)
        {
            return IsReady && rivalAgencies != null;
        }

        public static bool TryRestoreRaceProgress(
            AgencyState playerAgency,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            out double nextFundingUniversalTime)
        {
            nextFundingUniversalTime = RestoredNextFundingUniversalTime;
            return IsReady
                && playerAgency != null
                && satelliteNetworkFundingContracts != null
                && objectiveFundingContracts != null;
        }

        public static void CaptureRivalAgencyState(IList<AgencyState> rivalAgencies)
        {
            if (IsReady && rivalAgencies != null)
            {
                RivalCaptureCalls++;
            }
        }

        public static void CaptureRaceProgress(
            AgencyState playerAgency,
            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            double nextFundingUniversalTime)
        {
            if (IsReady
                && playerAgency != null
                && satelliteNetworkFundingContracts != null
                && objectiveFundingContracts != null)
            {
                RaceProgressCaptureCalls++;
                LastCapturedNextFundingUniversalTime = nextFundingUniversalTime;
            }
        }

        public static void Reset()
        {
            IsReady = true;
            RivalCaptureCalls = 0;
            RaceProgressCaptureCalls = 0;
            RestoredNextFundingUniversalTime = -1.0;
            LastCapturedNextFundingUniversalTime = -1.0;
        }
    }
}

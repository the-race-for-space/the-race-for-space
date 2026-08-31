using System.Collections.Generic;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.KspIntegration
{
    /// <summary>
    /// Test-build placeholder for the KSP-only discovery adapter. Snapshot-based tracker tests
    /// call the KSP-independent overload directly, so this method should never be invoked.
    /// </summary>
    public static class KspVesselDiscovery
    {
        public static bool TryCaptureOrbitingVessels(
            out IList<VesselTrackingSnapshot> vesselSnapshots,
            out double currentUniversalTime)
        {
            vesselSnapshots = null;
            currentUniversalTime = -1.0;
            return false;
        }
    }
}

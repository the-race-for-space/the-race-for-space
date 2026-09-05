namespace TheRaceForSpace.Tracking
{
    /// <summary>
    /// Project-owned vessel types needed by the current tracking rules.
    /// KSP vessel types are converted to this enum by the KSP integration layer.
    /// </summary>
    public enum TrackedVesselType
    {
        Other,
        Probe,
        Relay
    }

    /// <summary>
    /// KSP-independent snapshot of one orbiting vessel used by tracking and objective logic.
    /// The KSP integration layer is responsible for resolving loaded and unloaded vessel state.
    /// </summary>
    public sealed class VesselTrackingSnapshot
    {
        public VesselTrackingSnapshot(
            string celestialBodyName,
            TrackedVesselType vesselType,
            int crewCount)
        {
            CelestialBodyName = celestialBodyName;
            VesselType = vesselType;
            CrewCount = crewCount;
        }

        public string CelestialBodyName { get; private set; }
        public TrackedVesselType VesselType { get; private set; }
        public int CrewCount { get; private set; }
    }
}

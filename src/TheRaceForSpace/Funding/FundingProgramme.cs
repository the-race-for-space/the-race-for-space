namespace TheRaceForSpace.Funding
{
    /// <summary>
    /// One first-to-achieve satellite funding programme.
    /// </summary>
    public sealed class FundingProgramme
    {
        public FundingProgramme(string id, string name, string celestialBodyName, int requiredSatellites, double rewardFunds)
        {
            Id = id;
            Name = name;
            CelestialBodyName = celestialBodyName;
            RequiredSatellites = requiredSatellites;
            RewardFunds = rewardFunds;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string CelestialBodyName { get; private set; }
        public int RequiredSatellites { get; private set; }
        public double RewardFunds { get; private set; }
        public string WinnerProgramName { get; set; }

        public bool IsClaimed
        {
            get { return !string.IsNullOrEmpty(WinnerProgramName); }
        }
    }
}

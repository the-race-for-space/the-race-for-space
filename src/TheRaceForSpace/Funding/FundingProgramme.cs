using System;

namespace TheRaceForSpace.Funding
{
    /// <summary>
    /// One satellite funding target with a fixed total payout pool.
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

        /// <summary>
        /// Calculates one programme's current share of the fixed funding pool.
        /// Before the target is saturated, payout follows completion percentage.
        /// Once all programmes collectively meet or exceed the target, the complete
        /// pool is distributed by each programme's share of the qualifying satellites.
        /// </summary>
        public double CalculateCurrentPayout(int programSatelliteCount, int totalSatelliteCount)
        {
            if (RewardFunds <= 0.0 || RequiredSatellites <= 0 || programSatelliteCount <= 0 || totalSatelliteCount <= 0)
            {
                return 0.0;
            }

            // A defensive floor prevents invalid caller data from producing more than
            // a 100% ownership share if the supplied total is smaller than this programme's count.
            int normalizedTotalSatelliteCount = Math.Max(totalSatelliteCount, programSatelliteCount);

            if (normalizedTotalSatelliteCount <= RequiredSatellites)
            {
                double completionRatio = Math.Min(1.0, programSatelliteCount / (double)RequiredSatellites);
                return RewardFunds * completionRatio;
            }

            double ownershipRatio = programSatelliteCount / (double)normalizedTotalSatelliteCount;
            return RewardFunds * ownershipRatio;
        }
    }
}

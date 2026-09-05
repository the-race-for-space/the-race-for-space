using System;
using TheRaceForSpace.Objectives;

namespace TheRaceForSpace.Funding
{
    /// <summary>
    /// One persistent satellite-network funding target with a fixed total payout pool.
    /// </summary>
    public sealed class SatelliteNetworkFundingContract
    {
        public SatelliteNetworkFundingContract(string id, string name, string celestialBodyName, int requiredSatellites, double rewardFunds)
            : this(
                id,
                name,
                celestialBodyName,
                requiredSatellites,
                rewardFunds,
                true,
                null,
                null)
        {
        }

        public SatelliteNetworkFundingContract(
            string id,
            string name,
            string celestialBodyName,
            int requiredSatellites,
            double rewardFunds,
            bool isAvailable,
            string unlockRequirement)
            : this(
                id,
                name,
                celestialBodyName,
                requiredSatellites,
                rewardFunds,
                isAvailable,
                unlockRequirement,
                null)
        {
        }

        public SatelliteNetworkFundingContract(
            string id,
            string name,
            string celestialBodyName,
            int requiredSatellites,
            double rewardFunds,
            bool isAvailable,
            string unlockRequirement,
            UnlockRuleDefinition unlockRule)
        {
            Id = id;
            Name = name;
            CelestialBodyName = celestialBodyName;
            RequiredSatellites = requiredSatellites;
            RewardFunds = rewardFunds;
            IsAvailable = isAvailable;
            UnlockRequirement = unlockRequirement;
            UnlockRule = unlockRule;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string CelestialBodyName { get; private set; }
        public int RequiredSatellites { get; private set; }
        public double RewardFunds { get; private set; }
        public string UnlockRequirement { get; private set; }
        public UnlockRuleDefinition UnlockRule { get; private set; }
        public bool IsAvailable { get; private set; }
        public bool IsOffered { get; private set; }
        public bool HasReachedSatelliteTarget { get; private set; }

        /// <summary>
        /// Permanently unlocks this satellite contract for the current campaign. Satellite contracts
        /// do not relock after becoming available.
        /// </summary>
        public void Unlock()
        {
            IsAvailable = true;
        }

        /// <summary>
        /// Marks this network as one of the sponsor offers active for all race agencies.
        /// Satellite funding remains offered permanently after sponsor selection.
        /// </summary>
        public void Offer()
        {
            IsOffered = true;
        }

        /// <summary>
        /// Permanently records that the shared race network has reached this contract's full
        /// satellite target at least once. Live satellite counts still determine funding shares.
        /// </summary>
        public void MarkSatelliteTargetReached()
        {
            HasReachedSatelliteTarget = true;
        }

        // Persistence replaces campaign state when another save is loaded. Gameplay still uses
        // Unlock() as the only normal transition and never relocks a contract during a campaign.
        internal void RestoreAvailability(bool isAvailable)
        {
            IsAvailable = isAvailable;
        }

        // Offer and fulfilled state are one-way during normal gameplay, but loading another save
        // must replace both values explicitly rather than leaking state between saves.
        internal void RestoreOfferState(bool isOffered, bool hasReachedSatelliteTarget)
        {
            IsOffered = isOffered;
            HasReachedSatelliteTarget = hasReachedSatelliteTarget;
        }

        /// <summary>
        /// Calculates one contract's current share of the fixed funding pool.
        /// Before the target is saturated, payout follows completion percentage.
        /// Once all contracts collectively meet or exceed the target, the complete
        /// pool is distributed by each contract's share of the qualifying satellites.
        /// </summary>
        public double CalculateCurrentPayout(int programSatelliteCount, int totalSatelliteCount)
        {
            if (!IsAvailable
                || RewardFunds <= 0.0
                || RequiredSatellites <= 0
                || programSatelliteCount <= 0
                || totalSatelliteCount <= 0)
            {
                return 0.0;
            }

            // A defensive floor prevents invalid caller data from producing more than
            // a 100% ownership share if the supplied total is smaller than this contract's count.
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

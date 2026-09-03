using System;
using TheRaceForSpace.Milestones;

namespace TheRaceForSpace.Funding
{
    /// <summary>
    /// One persistent satellite-network funding target with a fixed total payout pool.
    /// </summary>
    public sealed class FundingProgramme
    {
        public FundingProgramme(string id, string name, string celestialBodyName, int requiredSatellites, double rewardFunds)
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

        public FundingProgramme(
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

        public FundingProgramme(
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
        /// Permanently unlocks this satellite contract for the current campaign.
        /// Version 0.4 does not relock satellite contracts after they become available.
        /// </summary>
        public void Unlock()
        {
            IsAvailable = true;
        }

        /// <summary>
        /// Marks this network as one of the sponsor offers visible to all race agencies.
        /// Step 1 stores this state only; offer limits and funding-day selection are applied later.
        /// </summary>
        public void Offer()
        {
            IsOffered = true;
        }

        /// <summary>
        /// Permanently records that the shared race network has reached this programme's full
        /// satellite target at least once. Live satellite counts still determine funding shares.
        /// </summary>
        public void MarkSatelliteTargetReached()
        {
            HasReachedSatelliteTarget = true;
        }

        // Persistence replaces campaign state when another save is loaded. Gameplay still uses
        // Unlock() as the only normal transition and never relocks a programme during a campaign.
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
        /// Calculates one programme's current share of the fixed funding pool.
        /// Before the target is saturated, payout follows completion percentage.
        /// Once all programmes collectively meet or exceed the target, the complete
        /// pool is distributed by each programme's share of the qualifying satellites.
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

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

            SatelliteNetworkMissionProfile missionProfile = SatelliteNetworkMissionProfileCatalogue.Resolve(id);
            Difficulty = missionProfile.Difficulty;
            RequiredKerbalCount = missionProfile.RequiredKerbalCount;
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
        /// Rival live-mission difficulty on the approved 1-10 scale for one network launch.
        /// </summary>
        public int Difficulty { get; private set; }

        /// <summary>
        /// Exact rival Kerbals required for one network launch. Current network launches are uncrewed.
        /// </summary>
        public int RequiredKerbalCount { get; private set; }

        /// <summary>
        /// Permanently unlocks this satellite contract for the current campaign. Satellite contracts
        /// do not relock after becoming available.
        /// </summary>
        public void Unlock()
        {
            IsAvailable = true;
        }

        /// <summary>
        /// Marks this network as one of the sponsor offers active for all campaign agencies.
        /// Satellite funding remains offered permanently after sponsor selection.
        /// </summary>
        public void Offer()
        {
            IsOffered = true;
        }

        /// <summary>
        /// Permanently records that the shared campaign network has reached this contract's full
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

    internal struct SatelliteNetworkMissionProfile
    {
        public SatelliteNetworkMissionProfile(int difficulty, int requiredKerbalCount)
        {
            Difficulty = difficulty;
            RequiredKerbalCount = requiredKerbalCount;
        }

        public int Difficulty { get; private set; }
        public int RequiredKerbalCount { get; private set; }
    }

    /// <summary>
    /// Authoritative rival mission metadata for one launch toward each current satellite-network target.
    /// </summary>
    internal static class SatelliteNetworkMissionProfileCatalogue
    {
        public static SatelliteNetworkMissionProfile Resolve(string contractId)
        {
            switch (contractId)
            {
                case FundingContractCatalogue.KerbinNetworkId:
                    return new SatelliteNetworkMissionProfile(4, 0);

                case FundingContractCatalogue.MunNetworkId:
                case FundingContractCatalogue.MinmusNetworkId:
                    return new SatelliteNetworkMissionProfile(5, 0);

                case FundingContractCatalogue.DunaNetworkId:
                case FundingContractCatalogue.MohoNetworkId:
                case FundingContractCatalogue.GillyNetworkId:
                case FundingContractCatalogue.IkeNetworkId:
                case FundingContractCatalogue.DresNetworkId:
                case FundingContractCatalogue.JoolNetworkId:
                case FundingContractCatalogue.LaytheNetworkId:
                case FundingContractCatalogue.VallNetworkId:
                case FundingContractCatalogue.TyloNetworkId:
                case FundingContractCatalogue.BopNetworkId:
                case FundingContractCatalogue.PolNetworkId:
                    return new SatelliteNetworkMissionProfile(7, 0);

                case FundingContractCatalogue.EveNetworkId:
                case FundingContractCatalogue.EelooNetworkId:
                    return new SatelliteNetworkMissionProfile(8, 0);

                default:
                    // Keep ad-hoc test/custom contracts usable. New production network IDs must receive
                    // an explicit approved profile above before rival live missions consume them.
                    return new SatelliteNetworkMissionProfile(1, 0);
            }
        }
    }
}

using System;
using TheRaceForSpace.Milestones;

namespace TheRaceForSpace.Funding
{
    /// <summary>
    /// Competitive achievement contract paid on the shared funding calendar.
    /// The first eligible funding date pays 100%, then each later payment declines
    /// by 10 percentage points until the final 10% payment expires the contract.
    /// </summary>
    public sealed class AchievementFundingProgramme
    {
        private const int TotalPayments = 10;
        private const int InterestReductionPercentPerPayment = 10;
        private const string DefaultUnlockRequirement = "Available from the start of the campaign";

        public AchievementFundingProgramme(
            string id,
            string name,
            string objectiveDescription,
            double baseRewardFunds)
            : this(
                id,
                name,
                objectiveDescription,
                baseRewardFunds,
                DefaultUnlockRequirement,
                null)
        {
        }

        public AchievementFundingProgramme(
            string id,
            string name,
            string objectiveDescription,
            double baseRewardFunds,
            string unlockRequirement)
            : this(
                id,
                name,
                objectiveDescription,
                baseRewardFunds,
                unlockRequirement,
                null)
        {
        }

        public AchievementFundingProgramme(
            string id,
            string name,
            string objectiveDescription,
            double baseRewardFunds,
            string unlockRequirement,
            UnlockRuleDefinition unlockRule)
        {
            Id = id;
            Name = name;
            ObjectiveDescription = objectiveDescription;
            BaseRewardFunds = Math.Max(0.0, baseRewardFunds);
            UnlockRequirement = string.IsNullOrEmpty(unlockRequirement)
                ? DefaultUnlockRequirement
                : unlockRequirement;
            UnlockRule = unlockRule;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string ObjectiveDescription { get; private set; }
        public string UnlockRequirement { get; private set; }
        public UnlockRuleDefinition UnlockRule { get; private set; }
        public double BaseRewardFunds { get; private set; }
        public bool IsOffered { get; private set; }
        public bool HasStarted { get; private set; }
        public int PaymentsProcessed { get; private set; }

        public bool IsExpired
        {
            get { return PaymentsProcessed >= TotalPayments; }
        }

        public int CurrentInterestPercent
        {
            get
            {
                if (IsExpired)
                {
                    return 0;
                }

                return Math.Max(
                    0,
                    100 - (PaymentsProcessed * InterestReductionPercentPerPayment));
            }
        }

        public double CurrentTotalPayoutFunds
        {
            get { return BaseRewardFunds * (CurrentInterestPercent / 100.0); }
        }

        /// <summary>
        /// Marks this contract as one of the sponsor offers visible to all race agencies.
        /// Step 1 stores this state only; offer limits and funding-day selection are applied later.
        /// </summary>
        public void Offer()
        {
            if (IsExpired)
            {
                return;
            }

            IsOffered = true;
        }

        /// <summary>
        /// Marks the contract as active after the first agency achieves its objective.
        /// No funds are awarded here; all payouts occur on the shared funding calendar.
        /// </summary>
        public void Start()
        {
            if (HasStarted || IsExpired)
            {
                return;
            }

            HasStarted = true;
        }

        /// <summary>
        /// Returns an eligible agency's share of the next payment. The controller evaluates
        /// eligibility at the exact shared funding timestamp so agencies joining before that
        /// date share the same payment and agencies joining later do not receive it retroactively.
        /// </summary>
        public double CalculateCurrentPayout(bool agencyIsEligible, int eligibleAgencyCount)
        {
            if (!HasStarted || IsExpired || !agencyIsEligible || eligibleAgencyCount <= 0)
            {
                return 0.0;
            }

            return CurrentTotalPayoutFunds / eligibleAgencyCount;
        }

        /// <summary>
        /// Advances one shared funding payment through 100%, 90%, 80% ... 10%.
        /// </summary>
        public void AdvancePayout()
        {
            if (!HasStarted || IsExpired)
            {
                return;
            }

            PaymentsProcessed = Math.Min(TotalPayments, PaymentsProcessed + 1);
        }

        /// <summary>
        /// Restores persisted lifecycle state without performing gameplay calculations.
        /// Payout timing is intentionally not stored here because all contracts use the
        /// controller's single global funding date.
        /// </summary>
        public void RestoreState(bool hasStarted, int paymentsProcessed)
        {
            PaymentsProcessed = Math.Max(0, Math.Min(TotalPayments, paymentsProcessed));
            HasStarted = hasStarted || PaymentsProcessed > 0;
        }

        // Persistence may replace offer state when loading another save. Normal gameplay only
        // moves contracts into the offered state and never withdraws an existing offer.
        internal void RestoreOfferState(bool isOffered)
        {
            IsOffered = isOffered;
        }
    }
}

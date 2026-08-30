using System;

namespace TheRaceForSpace.Funding
{
    /// <summary>
    /// Competitive achievement contract whose shared payout loses 10% interest
    /// after each scheduled 90-day funding payment once the first agency qualifies.
    /// </summary>
    public sealed class AchievementFundingProgramme
    {
        private const int TotalPayments = 10;
        private const int InterestReductionPercentPerPayment = 10;

        public AchievementFundingProgramme(
            string id,
            string name,
            string objectiveDescription,
            double baseRewardFunds)
        {
            Id = id;
            Name = name;
            ObjectiveDescription = objectiveDescription;
            BaseRewardFunds = Math.Max(0.0, baseRewardFunds);
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string ObjectiveDescription { get; private set; }
        public double BaseRewardFunds { get; private set; }
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
        /// Starts the declining-interest sequence after the first agency achieves the objective.
        /// The first 100% payment is still made on the normal scheduled funding date.
        /// </summary>
        public void Start()
        {
            if (!IsExpired)
            {
                HasStarted = true;
            }
        }

        /// <summary>
        /// Returns this agency's share of the next scheduled payment.
        /// Agencies that qualify later join future payments only; earlier payments are not recreated.
        /// </summary>
        public double CalculateCurrentPayout(bool agencyHasAchieved, int achievedAgencyCount)
        {
            if (!HasStarted || IsExpired || !agencyHasAchieved || achievedAgencyCount <= 0)
            {
                return 0.0;
            }

            return CurrentTotalPayoutFunds / achievedAgencyCount;
        }

        /// <summary>
        /// Advances the contract after one scheduled funding payment has been processed.
        /// Ten payments are made in total: 100%, 90%, 80% ... 10%.
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
        /// </summary>
        public void RestoreState(bool hasStarted, int paymentsProcessed)
        {
            PaymentsProcessed = Math.Max(0, Math.Min(TotalPayments, paymentsProcessed));
            HasStarted = hasStarted || PaymentsProcessed > 0;
        }
    }
}

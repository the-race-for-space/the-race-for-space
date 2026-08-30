using System;

namespace TheRaceForSpace.Funding
{
    /// <summary>
    /// Competitive achievement contract with an immediate 100% payout followed by
    /// nine independent 90-day payments that decline from 90% through 10%.
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
            NextPayoutUniversalTime = -1.0;
        }

        public string Id { get; private set; }
        public string Name { get; private set; }
        public string ObjectiveDescription { get; private set; }
        public double BaseRewardFunds { get; private set; }
        public bool HasStarted { get; private set; }
        public int PaymentsProcessed { get; private set; }
        public double NextPayoutUniversalTime { get; private set; }

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
        /// Starts the contract at the first achievement time. The first payment is due
        /// immediately at 100%; later payments advance from this timestamp in 90-day steps.
        /// </summary>
        public void Start(double firstAchievementUniversalTime)
        {
            if (HasStarted || IsExpired)
            {
                return;
            }

            HasStarted = true;
            NextPayoutUniversalTime = Math.Max(0.0, firstAchievementUniversalTime);
        }

        /// <summary>
        /// Returns an eligible agency's share of the currently due interest stage.
        /// Eligibility is evaluated by the controller at the exact payout timestamp so
        /// agencies cannot receive retroactive shares of payments before their achievement.
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
        /// Advances one payment. The first call records the immediate 100% payment;
        /// subsequent calls move through 90%, 80% ... 10% at the supplied interval.
        /// </summary>
        public void AdvancePayout(double payoutIntervalSeconds)
        {
            if (!HasStarted || IsExpired)
            {
                return;
            }

            PaymentsProcessed = Math.Min(TotalPayments, PaymentsProcessed + 1);

            if (IsExpired)
            {
                NextPayoutUniversalTime = -1.0;
                return;
            }

            NextPayoutUniversalTime += Math.Max(0.0, payoutIntervalSeconds);
        }

        /// <summary>
        /// Restores persisted lifecycle state without performing gameplay calculations.
        /// A negative next-payout time is retained so the controller can migrate an older
        /// 0.3 save that predates independent achievement-contract schedules.
        /// </summary>
        public void RestoreState(bool hasStarted, int paymentsProcessed, double nextPayoutUniversalTime)
        {
            PaymentsProcessed = Math.Max(0, Math.Min(TotalPayments, paymentsProcessed));
            HasStarted = hasStarted || PaymentsProcessed > 0;

            if (!HasStarted || IsExpired)
            {
                NextPayoutUniversalTime = -1.0;
                return;
            }

            NextPayoutUniversalTime = nextPayoutUniversalTime < 0.0
                ? -1.0
                : nextPayoutUniversalTime;
        }
    }
}

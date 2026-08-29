namespace TheRaceForSpace.KspIntegration
{
    /// <summary>
    /// Keeps direct KSP Career funding access out of the race and simulation logic.
    /// </summary>
    public static class CareerFundingAdapter
    {
        public static bool TryAddFunds(double amount)
        {
            if (amount <= 0.0)
            {
                return false;
            }

            if (HighLogic.CurrentGame == null
                || HighLogic.CurrentGame.Mode != Game.Modes.CAREER
                || Funding.Instance == null)
            {
                return false;
            }

            Funding.Instance.AddFunds(amount, TransactionReasons.ContractReward);
            return true;
        }
    }
}

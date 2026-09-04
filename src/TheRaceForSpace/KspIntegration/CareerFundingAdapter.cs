namespace TheRaceForSpace.KspIntegration
{
    /// <summary>
    /// Keeps direct KSP Career funding access out of the race and simulation logic.
    /// </summary>
    public static class CareerFundingAdapter
    {
        public static bool TryAddFunds(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0.0)
            {
                return false;
            }

            // Qualify KSP's global Funding type explicitly because the mod also has a
            // TheRaceForSpace.Funding namespace, which would otherwise shadow this class.
            if (HighLogic.CurrentGame == null
                || HighLogic.CurrentGame.Mode != Game.Modes.CAREER
                || global::Funding.Instance == null)
            {
                return false;
            }

            global::Funding.Instance.AddFunds(amount, TransactionReasons.ContractReward);
            return true;
        }
    }
}

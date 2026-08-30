using System;
using System.Globalization;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Persistence
{
    /// <summary>
    /// Serializable 0.3 campaign progression that is not owned directly by KSP vessel state.
    /// This class stores values only; unlock and payout calculations remain in gameplay modules.
    /// </summary>
    public sealed class RaceProgressSaveState
    {
        public bool HasData { get; private set; }
        public bool PlayerProbeOrbit { get; private set; }
        public bool PlayerCrewedOrbit { get; private set; }
        public bool KerbinNetworkUnlocked { get; private set; }
        public bool MunNetworkUnlocked { get; private set; }
        public bool MinmusNetworkUnlocked { get; private set; }
        public bool ProbeContractStarted { get; private set; }
        public int ProbePaymentsProcessed { get; private set; }
        public bool CrewedContractStarted { get; private set; }
        public int CrewedPaymentsProcessed { get; private set; }

        public void Capture(
            SpaceProgramState playerProgram,
            FundingProgramme kerbinProgramme,
            FundingProgramme munProgramme,
            FundingProgramme minmusProgramme,
            AchievementFundingProgramme probeOrbitProgramme,
            AchievementFundingProgramme crewedOrbitProgramme)
        {
            if (playerProgram == null
                || kerbinProgramme == null
                || munProgramme == null
                || minmusProgramme == null
                || probeOrbitProgramme == null
                || crewedOrbitProgramme == null)
            {
                return;
            }

            HasData = true;
            PlayerProbeOrbit = playerProgram.HasAchievedProbeOrbit;
            PlayerCrewedOrbit = playerProgram.HasAchievedCrewedOrbit;
            KerbinNetworkUnlocked = kerbinProgramme.IsAvailable;
            MunNetworkUnlocked = munProgramme.IsAvailable;
            MinmusNetworkUnlocked = minmusProgramme.IsAvailable;
            ProbeContractStarted = probeOrbitProgramme.HasStarted;
            ProbePaymentsProcessed = probeOrbitProgramme.PaymentsProcessed;
            CrewedContractStarted = crewedOrbitProgramme.HasStarted;
            CrewedPaymentsProcessed = crewedOrbitProgramme.PaymentsProcessed;
        }

        public void ApplyTo(
            SpaceProgramState playerProgram,
            FundingProgramme kerbinProgramme,
            FundingProgramme munProgramme,
            FundingProgramme minmusProgramme,
            AchievementFundingProgramme probeOrbitProgramme,
            AchievementFundingProgramme crewedOrbitProgramme)
        {
            if (!HasData
                || playerProgram == null
                || kerbinProgramme == null
                || munProgramme == null
                || minmusProgramme == null
                || probeOrbitProgramme == null
                || crewedOrbitProgramme == null)
            {
                return;
            }

            playerProgram.HasAchievedProbeOrbit = PlayerProbeOrbit;
            playerProgram.HasAchievedCrewedOrbit = PlayerCrewedOrbit;

            if (KerbinNetworkUnlocked)
            {
                kerbinProgramme.Unlock();
            }

            if (MunNetworkUnlocked)
            {
                munProgramme.Unlock();
            }

            if (MinmusNetworkUnlocked)
            {
                minmusProgramme.Unlock();
            }

            probeOrbitProgramme.RestoreState(ProbeContractStarted, ProbePaymentsProcessed);
            crewedOrbitProgramme.RestoreState(CrewedContractStarted, CrewedPaymentsProcessed);
        }

        public void Load(ConfigNode node)
        {
            HasData = node != null;
            PlayerProbeOrbit = false;
            PlayerCrewedOrbit = false;
            KerbinNetworkUnlocked = false;
            MunNetworkUnlocked = false;
            MinmusNetworkUnlocked = false;
            ProbeContractStarted = false;
            ProbePaymentsProcessed = 0;
            CrewedContractStarted = false;
            CrewedPaymentsProcessed = 0;

            if (!HasData)
            {
                return;
            }

            PlayerProbeOrbit = ParseBool(node.GetValue("playerProbeOrbit"));
            PlayerCrewedOrbit = ParseBool(node.GetValue("playerCrewedOrbit"));
            KerbinNetworkUnlocked = ParseBool(node.GetValue("kerbinNetworkUnlocked"));
            MunNetworkUnlocked = ParseBool(node.GetValue("munNetworkUnlocked"));
            MinmusNetworkUnlocked = ParseBool(node.GetValue("minmusNetworkUnlocked"));
            ProbeContractStarted = ParseBool(node.GetValue("probeContractStarted"));
            CrewedContractStarted = ParseBool(node.GetValue("crewedContractStarted"));
            ProbePaymentsProcessed = ParsePaymentCount(node.GetValue("probePaymentsProcessed"));
            CrewedPaymentsProcessed = ParsePaymentCount(node.GetValue("crewedPaymentsProcessed"));
        }

        public void Save(ConfigNode node)
        {
            if (!HasData || node == null)
            {
                return;
            }

            node.AddValue("playerProbeOrbit", PlayerProbeOrbit);
            node.AddValue("playerCrewedOrbit", PlayerCrewedOrbit);
            node.AddValue("kerbinNetworkUnlocked", KerbinNetworkUnlocked);
            node.AddValue("munNetworkUnlocked", MunNetworkUnlocked);
            node.AddValue("minmusNetworkUnlocked", MinmusNetworkUnlocked);
            node.AddValue("probeContractStarted", ProbeContractStarted);
            node.AddValue("probePaymentsProcessed", ProbePaymentsProcessed.ToString(CultureInfo.InvariantCulture));
            node.AddValue("crewedContractStarted", CrewedContractStarted);
            node.AddValue("crewedPaymentsProcessed", CrewedPaymentsProcessed.ToString(CultureInfo.InvariantCulture));
        }

        private static bool ParseBool(string value)
        {
            bool parsedValue;
            return !string.IsNullOrEmpty(value) && bool.TryParse(value, out parsedValue) && parsedValue;
        }

        private static int ParsePaymentCount(string value)
        {
            int parsedValue;
            if (string.IsNullOrEmpty(value)
                || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue))
            {
                return 0;
            }

            return Math.Max(0, Math.Min(10, parsedValue));
        }
    }
}

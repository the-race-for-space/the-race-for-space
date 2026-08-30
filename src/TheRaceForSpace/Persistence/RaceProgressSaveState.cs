using System;
using System.Globalization;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Milestones;
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
        public double PlayerProbeOrbitUniversalTime { get; private set; }
        public bool PlayerCrewedOrbit { get; private set; }
        public double PlayerCrewedOrbitUniversalTime { get; private set; }
        public bool PlayerMunProbeOrbit { get; private set; }
        public double PlayerMunProbeOrbitUniversalTime { get; private set; }
        public bool PlayerMinmusProbeOrbit { get; private set; }
        public double PlayerMinmusProbeOrbitUniversalTime { get; private set; }
        public bool PlayerMunCrewedOrbit { get; private set; }
        public double PlayerMunCrewedOrbitUniversalTime { get; private set; }
        public bool PlayerMinmusCrewedOrbit { get; private set; }
        public double PlayerMinmusCrewedOrbitUniversalTime { get; private set; }
        public bool KerbinNetworkUnlocked { get; private set; }
        public bool MunNetworkUnlocked { get; private set; }
        public bool MinmusNetworkUnlocked { get; private set; }
        public bool ProbeContractStarted { get; private set; }
        public int ProbePaymentsProcessed { get; private set; }
        public bool CrewedContractStarted { get; private set; }
        public int CrewedPaymentsProcessed { get; private set; }
        public bool MunProbeContractStarted { get; private set; }
        public int MunProbePaymentsProcessed { get; private set; }
        public bool MinmusProbeContractStarted { get; private set; }
        public int MinmusProbePaymentsProcessed { get; private set; }
        public bool MunCrewedContractStarted { get; private set; }
        public int MunCrewedPaymentsProcessed { get; private set; }
        public bool MinmusCrewedContractStarted { get; private set; }
        public int MinmusCrewedPaymentsProcessed { get; private set; }

        public void Capture(
            SpaceProgramState playerProgram,
            FundingProgramme kerbinProgramme,
            FundingProgramme munProgramme,
            FundingProgramme minmusProgramme,
            AchievementFundingProgramme probeOrbitProgramme,
            AchievementFundingProgramme crewedOrbitProgramme,
            AchievementFundingProgramme munProbeOrbitProgramme,
            AchievementFundingProgramme minmusProbeOrbitProgramme,
            AchievementFundingProgramme munCrewedOrbitProgramme,
            AchievementFundingProgramme minmusCrewedOrbitProgramme)
        {
            if (playerProgram == null
                || kerbinProgramme == null
                || munProgramme == null
                || minmusProgramme == null
                || probeOrbitProgramme == null
                || crewedOrbitProgramme == null
                || munProbeOrbitProgramme == null
                || minmusProbeOrbitProgramme == null
                || munCrewedOrbitProgramme == null
                || minmusCrewedOrbitProgramme == null)
            {
                return;
            }

            HasData = true;
            PlayerProbeOrbit = playerProgram.HasAchievement(PrototypeMilestones.ProbeOrbitId);
            PlayerProbeOrbitUniversalTime = NormalizeAchievementTime(
                PlayerProbeOrbit,
                playerProgram.GetAchievementUniversalTime(PrototypeMilestones.ProbeOrbitId));
            PlayerCrewedOrbit = playerProgram.HasAchievement(PrototypeMilestones.CrewedOrbitId);
            PlayerCrewedOrbitUniversalTime = NormalizeAchievementTime(
                PlayerCrewedOrbit,
                playerProgram.GetAchievementUniversalTime(PrototypeMilestones.CrewedOrbitId));
            PlayerMunProbeOrbit = playerProgram.HasAchievement(PrototypeMilestones.MunProbeOrbitId);
            PlayerMunProbeOrbitUniversalTime = NormalizeAchievementTime(
                PlayerMunProbeOrbit,
                playerProgram.GetAchievementUniversalTime(PrototypeMilestones.MunProbeOrbitId));
            PlayerMinmusProbeOrbit = playerProgram.HasAchievement(PrototypeMilestones.MinmusProbeOrbitId);
            PlayerMinmusProbeOrbitUniversalTime = NormalizeAchievementTime(
                PlayerMinmusProbeOrbit,
                playerProgram.GetAchievementUniversalTime(PrototypeMilestones.MinmusProbeOrbitId));
            PlayerMunCrewedOrbit = playerProgram.HasAchievement(PrototypeMilestones.MunCrewedOrbitId);
            PlayerMunCrewedOrbitUniversalTime = NormalizeAchievementTime(
                PlayerMunCrewedOrbit,
                playerProgram.GetAchievementUniversalTime(PrototypeMilestones.MunCrewedOrbitId));
            PlayerMinmusCrewedOrbit = playerProgram.HasAchievement(PrototypeMilestones.MinmusCrewedOrbitId);
            PlayerMinmusCrewedOrbitUniversalTime = NormalizeAchievementTime(
                PlayerMinmusCrewedOrbit,
                playerProgram.GetAchievementUniversalTime(PrototypeMilestones.MinmusCrewedOrbitId));
            KerbinNetworkUnlocked = kerbinProgramme.IsAvailable;
            MunNetworkUnlocked = munProgramme.IsAvailable;
            MinmusNetworkUnlocked = minmusProgramme.IsAvailable;
            ProbeContractStarted = probeOrbitProgramme.HasStarted;
            ProbePaymentsProcessed = probeOrbitProgramme.PaymentsProcessed;
            CrewedContractStarted = crewedOrbitProgramme.HasStarted;
            CrewedPaymentsProcessed = crewedOrbitProgramme.PaymentsProcessed;
            MunProbeContractStarted = munProbeOrbitProgramme.HasStarted;
            MunProbePaymentsProcessed = munProbeOrbitProgramme.PaymentsProcessed;
            MinmusProbeContractStarted = minmusProbeOrbitProgramme.HasStarted;
            MinmusProbePaymentsProcessed = minmusProbeOrbitProgramme.PaymentsProcessed;
            MunCrewedContractStarted = munCrewedOrbitProgramme.HasStarted;
            MunCrewedPaymentsProcessed = munCrewedOrbitProgramme.PaymentsProcessed;
            MinmusCrewedContractStarted = minmusCrewedOrbitProgramme.HasStarted;
            MinmusCrewedPaymentsProcessed = minmusCrewedOrbitProgramme.PaymentsProcessed;
        }

        public void ApplyTo(
            SpaceProgramState playerProgram,
            FundingProgramme kerbinProgramme,
            FundingProgramme munProgramme,
            FundingProgramme minmusProgramme,
            AchievementFundingProgramme probeOrbitProgramme,
            AchievementFundingProgramme crewedOrbitProgramme,
            AchievementFundingProgramme munProbeOrbitProgramme,
            AchievementFundingProgramme minmusProbeOrbitProgramme,
            AchievementFundingProgramme munCrewedOrbitProgramme,
            AchievementFundingProgramme minmusCrewedOrbitProgramme)
        {
            if (!HasData
                || playerProgram == null
                || kerbinProgramme == null
                || munProgramme == null
                || minmusProgramme == null
                || probeOrbitProgramme == null
                || crewedOrbitProgramme == null
                || munProbeOrbitProgramme == null
                || minmusProbeOrbitProgramme == null
                || munCrewedOrbitProgramme == null
                || minmusCrewedOrbitProgramme == null)
            {
                return;
            }

            if (PlayerProbeOrbit)
            {
                playerProgram.RecordAchievement(PrototypeMilestones.ProbeOrbitId, PlayerProbeOrbitUniversalTime);
            }

            if (PlayerCrewedOrbit)
            {
                playerProgram.RecordAchievement(PrototypeMilestones.CrewedOrbitId, PlayerCrewedOrbitUniversalTime);
            }

            if (PlayerMunProbeOrbit)
            {
                playerProgram.RecordAchievement(PrototypeMilestones.MunProbeOrbitId, PlayerMunProbeOrbitUniversalTime);
            }

            if (PlayerMinmusProbeOrbit)
            {
                playerProgram.RecordAchievement(PrototypeMilestones.MinmusProbeOrbitId, PlayerMinmusProbeOrbitUniversalTime);
            }

            if (PlayerMunCrewedOrbit)
            {
                playerProgram.RecordAchievement(PrototypeMilestones.MunCrewedOrbitId, PlayerMunCrewedOrbitUniversalTime);
            }

            if (PlayerMinmusCrewedOrbit)
            {
                playerProgram.RecordAchievement(PrototypeMilestones.MinmusCrewedOrbitId, PlayerMinmusCrewedOrbitUniversalTime);
            }

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
            munProbeOrbitProgramme.RestoreState(MunProbeContractStarted, MunProbePaymentsProcessed);
            minmusProbeOrbitProgramme.RestoreState(MinmusProbeContractStarted, MinmusProbePaymentsProcessed);
            munCrewedOrbitProgramme.RestoreState(MunCrewedContractStarted, MunCrewedPaymentsProcessed);
            minmusCrewedOrbitProgramme.RestoreState(MinmusCrewedContractStarted, MinmusCrewedPaymentsProcessed);
        }

        public void Load(ConfigNode node)
        {
            HasData = node != null;
            PlayerProbeOrbit = false;
            PlayerProbeOrbitUniversalTime = -1.0;
            PlayerCrewedOrbit = false;
            PlayerCrewedOrbitUniversalTime = -1.0;
            PlayerMunProbeOrbit = false;
            PlayerMunProbeOrbitUniversalTime = -1.0;
            PlayerMinmusProbeOrbit = false;
            PlayerMinmusProbeOrbitUniversalTime = -1.0;
            PlayerMunCrewedOrbit = false;
            PlayerMunCrewedOrbitUniversalTime = -1.0;
            PlayerMinmusCrewedOrbit = false;
            PlayerMinmusCrewedOrbitUniversalTime = -1.0;
            KerbinNetworkUnlocked = false;
            MunNetworkUnlocked = false;
            MinmusNetworkUnlocked = false;
            ProbeContractStarted = false;
            ProbePaymentsProcessed = 0;
            CrewedContractStarted = false;
            CrewedPaymentsProcessed = 0;
            MunProbeContractStarted = false;
            MunProbePaymentsProcessed = 0;
            MinmusProbeContractStarted = false;
            MinmusProbePaymentsProcessed = 0;
            MunCrewedContractStarted = false;
            MunCrewedPaymentsProcessed = 0;
            MinmusCrewedContractStarted = false;
            MinmusCrewedPaymentsProcessed = 0;

            if (!HasData)
            {
                return;
            }

            PlayerProbeOrbit = ParseBool(node.GetValue("playerProbeOrbit"));
            PlayerProbeOrbitUniversalTime = ParseAchievementTime(
                node.GetValue("playerProbeOrbitUniversalTime"),
                PlayerProbeOrbit);
            PlayerCrewedOrbit = ParseBool(node.GetValue("playerCrewedOrbit"));
            PlayerCrewedOrbitUniversalTime = ParseAchievementTime(
                node.GetValue("playerCrewedOrbitUniversalTime"),
                PlayerCrewedOrbit);
            PlayerMunProbeOrbit = ParseBool(node.GetValue("playerMunProbeOrbit"));
            PlayerMunProbeOrbitUniversalTime = ParseAchievementTime(
                node.GetValue("playerMunProbeOrbitUniversalTime"),
                PlayerMunProbeOrbit);
            PlayerMinmusProbeOrbit = ParseBool(node.GetValue("playerMinmusProbeOrbit"));
            PlayerMinmusProbeOrbitUniversalTime = ParseAchievementTime(
                node.GetValue("playerMinmusProbeOrbitUniversalTime"),
                PlayerMinmusProbeOrbit);
            PlayerMunCrewedOrbit = ParseBool(node.GetValue("playerMunCrewedOrbit"));
            PlayerMunCrewedOrbitUniversalTime = ParseAchievementTime(
                node.GetValue("playerMunCrewedOrbitUniversalTime"),
                PlayerMunCrewedOrbit);
            PlayerMinmusCrewedOrbit = ParseBool(node.GetValue("playerMinmusCrewedOrbit"));
            PlayerMinmusCrewedOrbitUniversalTime = ParseAchievementTime(
                node.GetValue("playerMinmusCrewedOrbitUniversalTime"),
                PlayerMinmusCrewedOrbit);
            KerbinNetworkUnlocked = ParseBool(node.GetValue("kerbinNetworkUnlocked"));
            MunNetworkUnlocked = ParseBool(node.GetValue("munNetworkUnlocked"));
            MinmusNetworkUnlocked = ParseBool(node.GetValue("minmusNetworkUnlocked"));
            ProbeContractStarted = ParseBool(node.GetValue("probeContractStarted"));
            ProbePaymentsProcessed = ParsePaymentCount(node.GetValue("probePaymentsProcessed"));
            CrewedContractStarted = ParseBool(node.GetValue("crewedContractStarted"));
            CrewedPaymentsProcessed = ParsePaymentCount(node.GetValue("crewedPaymentsProcessed"));
            MunProbeContractStarted = ParseBool(node.GetValue("munProbeContractStarted"));
            MunProbePaymentsProcessed = ParsePaymentCount(node.GetValue("munProbePaymentsProcessed"));
            MinmusProbeContractStarted = ParseBool(node.GetValue("minmusProbeContractStarted"));
            MinmusProbePaymentsProcessed = ParsePaymentCount(node.GetValue("minmusProbePaymentsProcessed"));
            MunCrewedContractStarted = ParseBool(node.GetValue("munCrewedContractStarted"));
            MunCrewedPaymentsProcessed = ParsePaymentCount(node.GetValue("munCrewedPaymentsProcessed"));
            MinmusCrewedContractStarted = ParseBool(node.GetValue("minmusCrewedContractStarted"));
            MinmusCrewedPaymentsProcessed = ParsePaymentCount(node.GetValue("minmusCrewedPaymentsProcessed"));
        }

        public void Save(ConfigNode node)
        {
            if (!HasData || node == null)
            {
                return;
            }

            // Keep the 0.3 key names stable while the in-memory programme state is now ID-based.
            node.AddValue("playerProbeOrbit", PlayerProbeOrbit);
            node.AddValue(
                "playerProbeOrbitUniversalTime",
                PlayerProbeOrbitUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("playerCrewedOrbit", PlayerCrewedOrbit);
            node.AddValue(
                "playerCrewedOrbitUniversalTime",
                PlayerCrewedOrbitUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("playerMunProbeOrbit", PlayerMunProbeOrbit);
            node.AddValue(
                "playerMunProbeOrbitUniversalTime",
                PlayerMunProbeOrbitUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("playerMinmusProbeOrbit", PlayerMinmusProbeOrbit);
            node.AddValue(
                "playerMinmusProbeOrbitUniversalTime",
                PlayerMinmusProbeOrbitUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("playerMunCrewedOrbit", PlayerMunCrewedOrbit);
            node.AddValue(
                "playerMunCrewedOrbitUniversalTime",
                PlayerMunCrewedOrbitUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("playerMinmusCrewedOrbit", PlayerMinmusCrewedOrbit);
            node.AddValue(
                "playerMinmusCrewedOrbitUniversalTime",
                PlayerMinmusCrewedOrbitUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("kerbinNetworkUnlocked", KerbinNetworkUnlocked);
            node.AddValue("munNetworkUnlocked", MunNetworkUnlocked);
            node.AddValue("minmusNetworkUnlocked", MinmusNetworkUnlocked);
            node.AddValue("probeContractStarted", ProbeContractStarted);
            node.AddValue("probePaymentsProcessed", ProbePaymentsProcessed.ToString(CultureInfo.InvariantCulture));
            node.AddValue("crewedContractStarted", CrewedContractStarted);
            node.AddValue("crewedPaymentsProcessed", CrewedPaymentsProcessed.ToString(CultureInfo.InvariantCulture));
            node.AddValue("munProbeContractStarted", MunProbeContractStarted);
            node.AddValue(
                "munProbePaymentsProcessed",
                MunProbePaymentsProcessed.ToString(CultureInfo.InvariantCulture));
            node.AddValue("minmusProbeContractStarted", MinmusProbeContractStarted);
            node.AddValue(
                "minmusProbePaymentsProcessed",
                MinmusProbePaymentsProcessed.ToString(CultureInfo.InvariantCulture));
            node.AddValue("munCrewedContractStarted", MunCrewedContractStarted);
            node.AddValue(
                "munCrewedPaymentsProcessed",
                MunCrewedPaymentsProcessed.ToString(CultureInfo.InvariantCulture));
            node.AddValue("minmusCrewedContractStarted", MinmusCrewedContractStarted);
            node.AddValue(
                "minmusCrewedPaymentsProcessed",
                MinmusCrewedPaymentsProcessed.ToString(CultureInfo.InvariantCulture));
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

        private static double ParseAchievementTime(string value, bool hasAchievement)
        {
            if (!hasAchievement)
            {
                return -1.0;
            }

            double parsedValue;
            if (!string.IsNullOrEmpty(value)
                && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
            {
                return Math.Max(0.0, parsedValue);
            }

            return 0.0;
        }

        private static double NormalizeAchievementTime(bool hasAchievement, double achievementUniversalTime)
        {
            return hasAchievement ? Math.Max(0.0, achievementUniversalTime) : -1.0;
        }
    }
}

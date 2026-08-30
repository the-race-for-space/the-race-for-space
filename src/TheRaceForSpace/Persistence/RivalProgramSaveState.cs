using System;
using System.Globalization;
using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Persistence
{
    /// <summary>
    /// Serializable rival-program values that must survive KSP save/load cycles.
    /// Gameplay calculations remain in their owning modules; this class only captures,
    /// validates, applies, and serializes the state required to resume a rival programme.
    /// </summary>
    public sealed class RivalProgramSaveState
    {
        public bool HasData { get; private set; }
        public double Funds { get; private set; }
        public int KerbinSatellites { get; private set; }
        public int MunSatellites { get; private set; }
        public int MinmusSatellites { get; private set; }
        public bool HasAchievedProbeOrbit { get; private set; }
        public double ProbeOrbitAchievementUniversalTime { get; private set; }
        public bool HasAchievedCrewedOrbit { get; private set; }
        public double CrewedOrbitAchievementUniversalTime { get; private set; }
        public bool HasAchievedMunProbeOrbit { get; private set; }
        public double MunProbeOrbitAchievementUniversalTime { get; private set; }
        public bool HasAchievedMinmusProbeOrbit { get; private set; }
        public double MinmusProbeOrbitAchievementUniversalTime { get; private set; }
        public bool HasAchievedMunCrewedOrbit { get; private set; }
        public double MunCrewedOrbitAchievementUniversalTime { get; private set; }
        public bool HasAchievedMinmusCrewedOrbit { get; private set; }
        public double MinmusCrewedOrbitAchievementUniversalTime { get; private set; }
        public string NextLaunchBodyName { get; private set; }
        public int LaunchProgressPercent { get; private set; }
        public double NextLaunchProgressCheckUniversalTime { get; private set; }

        public void Capture(SpaceProgramState program)
        {
            if (program == null || program.IsPlayer)
            {
                return;
            }

            HasData = true;
            Funds = Math.Max(0.0, program.Funds);
            KerbinSatellites = Math.Max(0, program.GetSatelliteCount("Kerbin"));
            MunSatellites = Math.Max(0, program.GetSatelliteCount("Mun"));
            MinmusSatellites = Math.Max(0, program.GetSatelliteCount("Minmus"));
            HasAchievedProbeOrbit = program.HasAchievedProbeOrbit;
            ProbeOrbitAchievementUniversalTime = NormalizeAchievementTime(
                HasAchievedProbeOrbit,
                program.ProbeOrbitAchievementUniversalTime);
            HasAchievedCrewedOrbit = program.HasAchievedCrewedOrbit;
            CrewedOrbitAchievementUniversalTime = NormalizeAchievementTime(
                HasAchievedCrewedOrbit,
                program.CrewedOrbitAchievementUniversalTime);
            HasAchievedMunProbeOrbit = program.HasAchievedMunProbeOrbit;
            MunProbeOrbitAchievementUniversalTime = NormalizeAchievementTime(
                HasAchievedMunProbeOrbit,
                program.MunProbeOrbitAchievementUniversalTime);
            HasAchievedMinmusProbeOrbit = program.HasAchievedMinmusProbeOrbit;
            MinmusProbeOrbitAchievementUniversalTime = NormalizeAchievementTime(
                HasAchievedMinmusProbeOrbit,
                program.MinmusProbeOrbitAchievementUniversalTime);
            HasAchievedMunCrewedOrbit = program.HasAchievedMunCrewedOrbit;
            MunCrewedOrbitAchievementUniversalTime = NormalizeAchievementTime(
                HasAchievedMunCrewedOrbit,
                program.MunCrewedOrbitAchievementUniversalTime);
            HasAchievedMinmusCrewedOrbit = program.HasAchievedMinmusCrewedOrbit;
            MinmusCrewedOrbitAchievementUniversalTime = NormalizeAchievementTime(
                HasAchievedMinmusCrewedOrbit,
                program.MinmusCrewedOrbitAchievementUniversalTime);
            NextLaunchBodyName = program.NextLaunchBodyName;
            LaunchProgressPercent = Math.Max(0, Math.Min(100, program.LaunchProgressPercent));
            NextLaunchProgressCheckUniversalTime = Math.Max(0.0, program.NextLaunchProgressCheckUniversalTime);
        }

        public void ApplyTo(SpaceProgramState program)
        {
            if (!HasData || program == null || program.IsPlayer)
            {
                return;
            }

            program.Funds = Math.Max(0.0, Funds);
            program.SetSatelliteCount("Kerbin", KerbinSatellites);
            program.SetSatelliteCount("Mun", MunSatellites);
            program.SetSatelliteCount("Minmus", MinmusSatellites);
            program.HasAchievedProbeOrbit = HasAchievedProbeOrbit;
            program.ProbeOrbitAchievementUniversalTime = ProbeOrbitAchievementUniversalTime;
            program.HasAchievedCrewedOrbit = HasAchievedCrewedOrbit;
            program.CrewedOrbitAchievementUniversalTime = CrewedOrbitAchievementUniversalTime;
            program.HasAchievedMunProbeOrbit = HasAchievedMunProbeOrbit;
            program.MunProbeOrbitAchievementUniversalTime = MunProbeOrbitAchievementUniversalTime;
            program.HasAchievedMinmusProbeOrbit = HasAchievedMinmusProbeOrbit;
            program.MinmusProbeOrbitAchievementUniversalTime = MinmusProbeOrbitAchievementUniversalTime;
            program.HasAchievedMunCrewedOrbit = HasAchievedMunCrewedOrbit;
            program.MunCrewedOrbitAchievementUniversalTime = MunCrewedOrbitAchievementUniversalTime;
            program.HasAchievedMinmusCrewedOrbit = HasAchievedMinmusCrewedOrbit;
            program.MinmusCrewedOrbitAchievementUniversalTime = MinmusCrewedOrbitAchievementUniversalTime;
            program.NextLaunchBodyName = NextLaunchBodyName;
            program.LaunchProgressPercent = Math.Max(0, Math.Min(100, LaunchProgressPercent));
            program.NextLaunchProgressCheckUniversalTime = Math.Max(0.0, NextLaunchProgressCheckUniversalTime);
        }

        public void Load(ConfigNode node)
        {
            HasData = node != null;
            Funds = 0.0;
            KerbinSatellites = 0;
            MunSatellites = 0;
            MinmusSatellites = 0;
            HasAchievedProbeOrbit = false;
            ProbeOrbitAchievementUniversalTime = -1.0;
            HasAchievedCrewedOrbit = false;
            CrewedOrbitAchievementUniversalTime = -1.0;
            HasAchievedMunProbeOrbit = false;
            MunProbeOrbitAchievementUniversalTime = -1.0;
            HasAchievedMinmusProbeOrbit = false;
            MinmusProbeOrbitAchievementUniversalTime = -1.0;
            HasAchievedMunCrewedOrbit = false;
            MunCrewedOrbitAchievementUniversalTime = -1.0;
            HasAchievedMinmusCrewedOrbit = false;
            MinmusCrewedOrbitAchievementUniversalTime = -1.0;
            NextLaunchBodyName = null;
            LaunchProgressPercent = 0;
            NextLaunchProgressCheckUniversalTime = 0.0;

            if (!HasData)
            {
                return;
            }

            double parsedDouble;
            int parsedInt;
            string value = node.GetValue("funds");
            if (!string.IsNullOrEmpty(value)
                && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedDouble))
            {
                Funds = Math.Max(0.0, parsedDouble);
            }

            value = node.GetValue("kerbinSatellites");
            if (!string.IsNullOrEmpty(value)
                && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedInt))
            {
                KerbinSatellites = Math.Max(0, parsedInt);
            }

            value = node.GetValue("munSatellites");
            if (!string.IsNullOrEmpty(value)
                && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedInt))
            {
                MunSatellites = Math.Max(0, parsedInt);
            }

            value = node.GetValue("minmusSatellites");
            if (!string.IsNullOrEmpty(value)
                && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedInt))
            {
                MinmusSatellites = Math.Max(0, parsedInt);
            }

            HasAchievedProbeOrbit = ParseBool(node.GetValue("hasAchievedProbeOrbit"));
            ProbeOrbitAchievementUniversalTime = ParseAchievementTime(
                node.GetValue("probeOrbitAchievementUniversalTime"),
                HasAchievedProbeOrbit);
            HasAchievedCrewedOrbit = ParseBool(node.GetValue("hasAchievedCrewedOrbit"));
            CrewedOrbitAchievementUniversalTime = ParseAchievementTime(
                node.GetValue("crewedOrbitAchievementUniversalTime"),
                HasAchievedCrewedOrbit);
            HasAchievedMunProbeOrbit = ParseBool(node.GetValue("hasAchievedMunProbeOrbit"));
            MunProbeOrbitAchievementUniversalTime = ParseAchievementTime(
                node.GetValue("munProbeOrbitAchievementUniversalTime"),
                HasAchievedMunProbeOrbit);
            HasAchievedMinmusProbeOrbit = ParseBool(node.GetValue("hasAchievedMinmusProbeOrbit"));
            MinmusProbeOrbitAchievementUniversalTime = ParseAchievementTime(
                node.GetValue("minmusProbeOrbitAchievementUniversalTime"),
                HasAchievedMinmusProbeOrbit);
            HasAchievedMunCrewedOrbit = ParseBool(node.GetValue("hasAchievedMunCrewedOrbit"));
            MunCrewedOrbitAchievementUniversalTime = ParseAchievementTime(
                node.GetValue("munCrewedOrbitAchievementUniversalTime"),
                HasAchievedMunCrewedOrbit);
            HasAchievedMinmusCrewedOrbit = ParseBool(node.GetValue("hasAchievedMinmusCrewedOrbit"));
            MinmusCrewedOrbitAchievementUniversalTime = ParseAchievementTime(
                node.GetValue("minmusCrewedOrbitAchievementUniversalTime"),
                HasAchievedMinmusCrewedOrbit);

            value = node.GetValue("nextLaunchBodyName");
            if (string.Equals(value, "Kerbin", StringComparison.OrdinalIgnoreCase))
            {
                NextLaunchBodyName = "Kerbin";
            }
            else if (string.Equals(value, "Mun", StringComparison.OrdinalIgnoreCase))
            {
                NextLaunchBodyName = "Mun";
            }
            else if (string.Equals(value, "Minmus", StringComparison.OrdinalIgnoreCase))
            {
                NextLaunchBodyName = "Minmus";
            }
            else if (string.Equals(value, "Probe Orbit", StringComparison.OrdinalIgnoreCase))
            {
                NextLaunchBodyName = "Probe Orbit";
            }
            else if (string.Equals(value, "Crewed Orbit", StringComparison.OrdinalIgnoreCase))
            {
                NextLaunchBodyName = "Crewed Orbit";
            }
            else if (string.Equals(value, "Mun Probe Orbit", StringComparison.OrdinalIgnoreCase))
            {
                NextLaunchBodyName = "Mun Probe Orbit";
            }
            else if (string.Equals(value, "Minmus Probe Orbit", StringComparison.OrdinalIgnoreCase))
            {
                NextLaunchBodyName = "Minmus Probe Orbit";
            }
            else if (string.Equals(value, "Mun Crewed Orbit", StringComparison.OrdinalIgnoreCase))
            {
                NextLaunchBodyName = "Mun Crewed Orbit";
            }
            else if (string.Equals(value, "Minmus Crewed Orbit", StringComparison.OrdinalIgnoreCase))
            {
                NextLaunchBodyName = "Minmus Crewed Orbit";
            }

            value = node.GetValue("launchProgressPercent");
            if (!string.IsNullOrEmpty(value)
                && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedInt))
            {
                LaunchProgressPercent = Math.Max(0, Math.Min(100, parsedInt));
            }

            value = node.GetValue("nextLaunchProgressCheckUniversalTime");
            if (!string.IsNullOrEmpty(value)
                && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedDouble))
            {
                NextLaunchProgressCheckUniversalTime = Math.Max(0.0, parsedDouble);
            }
        }

        public void Save(ConfigNode node)
        {
            if (!HasData || node == null)
            {
                return;
            }

            node.AddValue("funds", Funds.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("kerbinSatellites", KerbinSatellites.ToString(CultureInfo.InvariantCulture));
            node.AddValue("munSatellites", MunSatellites.ToString(CultureInfo.InvariantCulture));
            node.AddValue("minmusSatellites", MinmusSatellites.ToString(CultureInfo.InvariantCulture));
            node.AddValue("hasAchievedProbeOrbit", HasAchievedProbeOrbit);
            node.AddValue(
                "probeOrbitAchievementUniversalTime",
                ProbeOrbitAchievementUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("hasAchievedCrewedOrbit", HasAchievedCrewedOrbit);
            node.AddValue(
                "crewedOrbitAchievementUniversalTime",
                CrewedOrbitAchievementUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("hasAchievedMunProbeOrbit", HasAchievedMunProbeOrbit);
            node.AddValue(
                "munProbeOrbitAchievementUniversalTime",
                MunProbeOrbitAchievementUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("hasAchievedMinmusProbeOrbit", HasAchievedMinmusProbeOrbit);
            node.AddValue(
                "minmusProbeOrbitAchievementUniversalTime",
                MinmusProbeOrbitAchievementUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("hasAchievedMunCrewedOrbit", HasAchievedMunCrewedOrbit);
            node.AddValue(
                "munCrewedOrbitAchievementUniversalTime",
                MunCrewedOrbitAchievementUniversalTime.ToString("R", CultureInfo.InvariantCulture));
            node.AddValue("hasAchievedMinmusCrewedOrbit", HasAchievedMinmusCrewedOrbit);
            node.AddValue(
                "minmusCrewedOrbitAchievementUniversalTime",
                MinmusCrewedOrbitAchievementUniversalTime.ToString("R", CultureInfo.InvariantCulture));

            if (!string.IsNullOrEmpty(NextLaunchBodyName))
            {
                node.AddValue("nextLaunchBodyName", NextLaunchBodyName);
            }

            node.AddValue("launchProgressPercent", LaunchProgressPercent.ToString(CultureInfo.InvariantCulture));
            node.AddValue(
                "nextLaunchProgressCheckUniversalTime",
                NextLaunchProgressCheckUniversalTime.ToString("R", CultureInfo.InvariantCulture));
        }

        private static bool ParseBool(string value)
        {
            bool parsedValue;
            return !string.IsNullOrEmpty(value) && bool.TryParse(value, out parsedValue) && parsedValue;
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

            // Version 0.3 state written before timestamps were added is treated as historical.
            return 0.0;
        }

        private static double NormalizeAchievementTime(bool hasAchievement, double achievementUniversalTime)
        {
            return hasAchievement ? Math.Max(0.0, achievementUniversalTime) : -1.0;
        }
    }
}

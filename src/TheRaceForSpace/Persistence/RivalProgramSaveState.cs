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

            if (!string.IsNullOrEmpty(NextLaunchBodyName))
            {
                node.AddValue("nextLaunchBodyName", NextLaunchBodyName);
            }

            node.AddValue("launchProgressPercent", LaunchProgressPercent.ToString(CultureInfo.InvariantCulture));
            node.AddValue(
                "nextLaunchProgressCheckUniversalTime",
                NextLaunchProgressCheckUniversalTime.ToString("R", CultureInfo.InvariantCulture));
        }
    }
}

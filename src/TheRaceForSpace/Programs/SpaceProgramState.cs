using System;
using System.Collections.Generic;

namespace TheRaceForSpace.Programs
{
    /// <summary>
    /// Minimal campaign state for one space program in the current prototype.
    /// </summary>
    public sealed class SpaceProgramState
    {
        private readonly Dictionary<string, int> _satellitesByBody = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public SpaceProgramState(string name, bool isPlayer)
        {
            Name = name;
            IsPlayer = isPlayer;
            ProbeOrbitAchievementUniversalTime = -1.0;
            CrewedOrbitAchievementUniversalTime = -1.0;
            MunProbeOrbitAchievementUniversalTime = -1.0;
            MinmusProbeOrbitAchievementUniversalTime = -1.0;
            MunCrewedOrbitAchievementUniversalTime = -1.0;
            MinmusCrewedOrbitAchievementUniversalTime = -1.0;
        }

        public string Name { get; private set; }
        public bool IsPlayer { get; private set; }

        // AwardedFunds is cumulative funding actually paid during this prototype session.
        // Rival Funds is their simulated spendable balance; the player's real balance remains owned by KSP.
        public double AwardedFunds { get; set; }
        public double Funds { get; set; }
        public double NextPayoutFunds { get; set; }
        public int RacePoints { get; set; }

        // Orbit achievements are permanent once observed. Their timestamps are retained so
        // declining-interest payouts can exclude agencies that qualified after an earlier payment.
        public bool HasAchievedProbeOrbit { get; set; }
        public double ProbeOrbitAchievementUniversalTime { get; set; }
        public bool HasAchievedCrewedOrbit { get; set; }
        public double CrewedOrbitAchievementUniversalTime { get; set; }
        public bool HasAchievedMunProbeOrbit { get; set; }
        public double MunProbeOrbitAchievementUniversalTime { get; set; }
        public bool HasAchievedMinmusProbeOrbit { get; set; }
        public double MinmusProbeOrbitAchievementUniversalTime { get; set; }
        public bool HasAchievedMunCrewedOrbit { get; set; }
        public double MunCrewedOrbitAchievementUniversalTime { get; set; }
        public bool HasAchievedMinmusCrewedOrbit { get; set; }
        public double MinmusCrewedOrbitAchievementUniversalTime { get; set; }

        // NextLaunchBodyName is retained for save compatibility with 0.2. In 0.3 it may also
        // contain a named one-off achievement mission while a rival develops it.
        public string NextLaunchBodyName { get; set; }
        public int LaunchProgressPercent { get; set; }
        public double NextLaunchProgressCheckUniversalTime { get; set; }

        public int GetSatelliteCount(string celestialBodyName)
        {
            int count;
            return _satellitesByBody.TryGetValue(celestialBodyName, out count) ? count : 0;
        }

        public void SetSatelliteCount(string celestialBodyName, int count)
        {
            _satellitesByBody[celestialBodyName] = Math.Max(0, count);
        }
    }
}

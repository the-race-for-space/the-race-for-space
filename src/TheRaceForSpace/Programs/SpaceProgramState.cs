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
        }

        public string Name { get; private set; }
        public bool IsPlayer { get; private set; }

        // AwardedFunds is cumulative funding actually paid during this prototype session.
        // Rival Funds is their simulated spendable balance; the player's real balance remains owned by KSP.
        public double AwardedFunds { get; set; }
        public double Funds { get; set; }
        public double NextPayoutFunds { get; set; }
        public int RacePoints { get; set; }

        // Orbit achievements are permanent once observed. Rival values and the player's
        // achievement flags are persisted so a completed race cannot be lost on save/load.
        public bool HasAchievedProbeOrbit { get; set; }
        public bool HasAchievedCrewedOrbit { get; set; }

        // NextLaunchBodyName is retained for save compatibility with 0.2. In 0.3 it may also
        // contain the named Probe Orbit or Crewed Orbit mission while a rival develops it.
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

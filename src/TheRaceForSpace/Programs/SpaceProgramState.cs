using System;
using System.Collections.Generic;

namespace TheRaceForSpace.Programs
{
    /// <summary>
    /// Minimal campaign state for one space program in the satellite prototype.
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

        // Rival launch planning remains session-only in 0.2. These values are intentionally
        // stored with programme state so scene changes do not reset an in-progress launch.
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

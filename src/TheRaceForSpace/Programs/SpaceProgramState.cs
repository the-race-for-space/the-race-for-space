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
        public double AwardedFunds { get; set; }
        public int RacePoints { get; set; }

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

using TheRaceForSpace.Programs;

namespace TheRaceForSpace.Simulation
{
    /// <summary>
    /// Deterministic rival progress used only by the first satellite prototype.
    /// </summary>
    public static class RivalSimulation
    {
        private const double KerbinDaySeconds = 21600.0;

        public static void Refresh(SpaceProgramState asterProgram, SpaceProgramState cobaltProgram, double elapsedUniversalTime)
        {
            if (asterProgram == null || cobaltProgram == null)
            {
                return;
            }

            asterProgram.SetSatelliteCount("Kerbin", elapsedUniversalTime >= 3 * KerbinDaySeconds ? 2 : 0);
            asterProgram.SetSatelliteCount("Mun", elapsedUniversalTime >= 20 * KerbinDaySeconds ? 1 : 0);
            asterProgram.SetSatelliteCount("Minmus", elapsedUniversalTime >= 45 * KerbinDaySeconds ? 1 : 0);

            cobaltProgram.SetSatelliteCount("Kerbin", elapsedUniversalTime >= 5 * KerbinDaySeconds ? 2 : 0);
            cobaltProgram.SetSatelliteCount("Mun", elapsedUniversalTime >= 15 * KerbinDaySeconds ? 1 : 0);
            cobaltProgram.SetSatelliteCount("Minmus", elapsedUniversalTime >= 35 * KerbinDaySeconds ? 1 : 0);
        }
    }
}

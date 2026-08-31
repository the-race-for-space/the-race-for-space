using System;

namespace TheRaceForSpace.ControllerTests
{
    internal static class Program
    {
        private static int _failures;

        private static int Main()
        {
            Run(
                "Controller probe observation unlocks funding flow",
                SatelliteRaceControllerTests.ProbeObservationUnlocksFundingFlow);
            Run(
                "Controller existing state pays at shared funding boundary",
                SatelliteRaceControllerTests.ExistingStatePaysAtSharedFundingBoundary);
            Run(
                "Controller boundary observation is not paid retroactively",
                SatelliteRaceControllerTests.BoundaryObservationIsNotPaidRetroactively);

            Console.WriteLine();
            Console.WriteLine(_failures == 0
                ? "All controller regression tests passed."
                : _failures + " controller regression test(s) failed.");
            return _failures == 0 ? 0 : 1;
        }

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                Console.WriteLine("PASS: " + name);
            }
            catch (Exception exception)
            {
                _failures++;
                Console.WriteLine("FAIL: " + name);
                Console.WriteLine("      " + exception.Message);
            }
        }
    }
}

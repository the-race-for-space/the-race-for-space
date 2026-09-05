using System;
using System.Collections.Generic;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.ControllerTests
{
    internal static class StarterTelemetryPlanTests
    {
        public static void ActiveContractTypesRequestOnlyNeededTelemetry()
        {
            Equal(
                StarterTelemetryRequirement.None,
                StarterTelemetryPlan.GetRequirements(new List<ObjectiveDefinition>()),
                "An empty active-contract plan should request no condition-specific telemetry.");

            Equal(
                StarterTelemetryRequirement.Altitude
                    | StarterTelemetryRequirement.SurfaceSpeed
                    | StarterTelemetryRequirement.SurfaceImpact,
                StarterTelemetryPlan.GetRequirements(new List<ObjectiveDefinition>
                {
                    ObjectiveCatalogue.FindById(ObjectiveCatalogue.DirectedPower1Id)
                }),
                "Directed Power should request only altitude, speed, and surface-impact tracking.");

            Equal(
                StarterTelemetryRequirement.Mass,
                StarterTelemetryPlan.GetRequirements(new List<ObjectiveDefinition>
                {
                    ObjectiveCatalogue.FindById(ObjectiveCatalogue.Mass1Id),
                    ObjectiveCatalogue.FindById(ObjectiveCatalogue.Mass2Id)
                }),
                "Multiple active Mass contracts should share one Mass telemetry requirement.");

            Equal(
                StarterTelemetryRequirement.Altitude | StarterTelemetryRequirement.Crew,
                StarterTelemetryPlan.GetRequirements(new List<ObjectiveDefinition>
                {
                    ObjectiveCatalogue.FindById(ObjectiveCatalogue.Control1Id)
                }),
                "Control should request altitude and crew telemetry only.");

            Equal(
                StarterTelemetryRequirement.Biome,
                StarterTelemetryPlan.GetRequirements(new List<ObjectiveDefinition>
                {
                    ObjectiveCatalogue.FindById(ObjectiveCatalogue.Biome1Id)
                }),
                "Biome should request only biome telemetry.");

            Equal(
                StarterTelemetryRequirement.All,
                StarterTelemetryPlan.GetRequirements(new List<ObjectiveDefinition>
                {
                    ObjectiveCatalogue.FindById(ObjectiveCatalogue.DirectedPower1Id),
                    ObjectiveCatalogue.FindById(ObjectiveCatalogue.Mass1Id),
                    ObjectiveCatalogue.FindById(ObjectiveCatalogue.Control1Id),
                    ObjectiveCatalogue.FindById(ObjectiveCatalogue.Biome1Id)
                }),
                "The four opening contract lines together should request the complete telemetry mask.");
        }

        private static void Equal(
            StarterTelemetryRequirement expected,
            StarterTelemetryRequirement actual,
            string message)
        {
            if (expected != actual)
            {
                throw new InvalidOperationException(
                    message + " Expected '" + expected + "' but got '" + actual + "'.");
            }
        }
    }
}

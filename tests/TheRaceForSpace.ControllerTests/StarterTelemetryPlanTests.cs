using System;
using System.Collections.Generic;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.ControllerTests
{
    internal static class StarterTelemetryPlanTests
    {
        public static void ActiveContractTypesRequestOnlyNeededTelemetry()
        {
            Equal(
                StarterTelemetryRequirement.None,
                StarterTelemetryPlan.GetRequirements(new List<MilestoneDefinition>()),
                "An empty active-contract plan should request no condition-specific telemetry.");

            Equal(
                StarterTelemetryRequirement.Altitude
                    | StarterTelemetryRequirement.SurfaceSpeed
                    | StarterTelemetryRequirement.SurfaceImpact,
                StarterTelemetryPlan.GetRequirements(new List<MilestoneDefinition>
                {
                    PrototypeMilestones.FindById(PrototypeMilestones.DirectedPower1Id)
                }),
                "Directed Power should request only altitude, speed, and surface-impact tracking.");

            Equal(
                StarterTelemetryRequirement.Mass,
                StarterTelemetryPlan.GetRequirements(new List<MilestoneDefinition>
                {
                    PrototypeMilestones.FindById(PrototypeMilestones.Mass1Id),
                    PrototypeMilestones.FindById(PrototypeMilestones.Mass2Id)
                }),
                "Multiple active Mass contracts should share one Mass telemetry requirement.");

            Equal(
                StarterTelemetryRequirement.Altitude | StarterTelemetryRequirement.Crew,
                StarterTelemetryPlan.GetRequirements(new List<MilestoneDefinition>
                {
                    PrototypeMilestones.FindById(PrototypeMilestones.Control1Id)
                }),
                "Control should request altitude and crew telemetry only.");

            Equal(
                StarterTelemetryRequirement.Biome,
                StarterTelemetryPlan.GetRequirements(new List<MilestoneDefinition>
                {
                    PrototypeMilestones.FindById(PrototypeMilestones.Biome1Id)
                }),
                "Biome should request only biome telemetry.");

            Equal(
                StarterTelemetryRequirement.All,
                StarterTelemetryPlan.GetRequirements(new List<MilestoneDefinition>
                {
                    PrototypeMilestones.FindById(PrototypeMilestones.DirectedPower1Id),
                    PrototypeMilestones.FindById(PrototypeMilestones.Mass1Id),
                    PrototypeMilestones.FindById(PrototypeMilestones.Control1Id),
                    PrototypeMilestones.FindById(PrototypeMilestones.Biome1Id)
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

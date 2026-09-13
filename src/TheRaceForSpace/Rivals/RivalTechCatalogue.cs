using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TheRaceForSpace.Agencies;

namespace TheRaceForSpace.Rivals
{
    /// <summary>
    /// One fixed stock technology node mirrored for rival research. Parent relationships preserve
    /// KSP's any-to-unlock semantics so research eligibility can match the stock tree without querying KSP.
    /// </summary>
    internal sealed class RivalTechNodeDefinition
    {
        private readonly ReadOnlyCollection<string> _prerequisiteTechIds;
        private readonly ReadOnlyCollection<string> _unlockedExperimentIds;

        internal RivalTechNodeDefinition(
            string id,
            string displayName,
            double scienceCost,
            bool anyPrerequisiteUnlocks,
            string[] prerequisiteTechIds,
            string[] unlockedExperimentIds)
        {
            Id = id;
            DisplayName = displayName;
            ScienceCost = scienceCost;
            AnyPrerequisiteUnlocks = anyPrerequisiteUnlocks;
            _prerequisiteTechIds = Array.AsReadOnly(
                prerequisiteTechIds == null
                    ? new string[0]
                    : (string[])prerequisiteTechIds.Clone());
            _unlockedExperimentIds = Array.AsReadOnly(
                unlockedExperimentIds == null
                    ? new string[0]
                    : (string[])unlockedExperimentIds.Clone());
        }

        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public double ScienceCost { get; private set; }
        public bool AnyPrerequisiteUnlocks { get; private set; }
        public ReadOnlyCollection<string> PrerequisiteTechIds { get { return _prerequisiteTechIds; } }
        public ReadOnlyCollection<string> UnlockedExperimentIds { get { return _unlockedExperimentIds; } }

        /// <summary>
        /// Returns whether the already-researched set satisfies this node's stock parent rule.
        /// This checks technology ancestry only; Science balance and R&D facility limits belong to
        /// the rival development simulation.
        /// </summary>
        public bool ArePrerequisitesSatisfied(ISet<string> researchedTechIds)
        {
            if (_prerequisiteTechIds.Count == 0)
            {
                return true;
            }

            if (researchedTechIds == null || researchedTechIds.Count == 0)
            {
                return false;
            }

            if (AnyPrerequisiteUnlocks)
            {
                for (int prerequisiteIndex = 0;
                    prerequisiteIndex < _prerequisiteTechIds.Count;
                    prerequisiteIndex++)
                {
                    if (RivalTechCatalogue.ContainsTechId(
                        researchedTechIds,
                        _prerequisiteTechIds[prerequisiteIndex]))
                    {
                        return true;
                    }
                }

                return false;
            }

            for (int prerequisiteIndex = 0;
                prerequisiteIndex < _prerequisiteTechIds.Count;
                prerequisiteIndex++)
            {
                if (!RivalTechCatalogue.ContainsTechId(
                    researchedTechIds,
                    _prerequisiteTechIds[prerequisiteIndex]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Project-owned mirror of the stock KSP 1.12 technology tree used by rival research.
    /// Technology gates Science experiments only in v0.6; Contract launches remain independent.
    /// </summary>
    internal static class RivalTechCatalogue
    {
        internal const string CrewReportExperimentId = "crewReport";
        internal const string MysteryGooExperimentId = "mysteryGoo";
        internal const string TemperatureScanExperimentId = "temperatureScan";
        internal const string AtmosphericPressureScanExperimentId = "barometerScan";
        internal const string MaterialsStudyExperimentId = "mobileMaterialsLab";
        internal const string EvaScienceExperimentId = "evaScience";
        internal const string AtmosphereAnalysisExperimentId = "atmosphereAnalysis";
        internal const string InfraredTelescopeExperimentId = "infraredTelescope";
        internal const string SeismicScanExperimentId = "seismicScan";
        internal const string MagnetometerReportExperimentId = "magnetometer";
        internal const string GravityScanExperimentId = "gravityScan";

        private static readonly string[] NoPrerequisiteTechIds = new string[0];
        private static readonly string[] NoExperimentIds = new string[0];
        private static readonly List<RivalTechNodeDefinition> TechNodes =
            new List<RivalTechNodeDefinition>();
        private static readonly Dictionary<string, RivalTechNodeDefinition> TechNodesById =
            new Dictionary<string, RivalTechNodeDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, RivalTechNodeDefinition> ExperimentUnlocksById =
            new Dictionary<string, RivalTechNodeDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly ReadOnlyCollection<RivalTechNodeDefinition> ReadOnlyTechNodes;

        static RivalTechCatalogue()
        {
            AddNode("start", "Start", 0.0, false,
                NoPrerequisiteTechIds,
                new[] { CrewReportExperimentId, MysteryGooExperimentId });
            AddNode("basicRocketry", "Basic Rocketry", 5.0, false,
                new[] { "start" },
                NoExperimentIds);
            AddNode("engineering101", "Engineering 101", 5.0, false,
                new[] { "start" },
                new[] { TemperatureScanExperimentId });
            AddNode("survivability", "Survivability", 15.0, false,
                new[] { "engineering101" },
                new[] { AtmosphericPressureScanExperimentId });
            AddNode("stability", "Stability", 18.0, true,
                new[] { "engineering101", "basicRocketry" },
                NoExperimentIds);
            AddNode("generalRocketry", "General Rocketry", 20.0, false,
                new[] { "basicRocketry" },
                NoExperimentIds);
            AddNode("aviation", "Aviation", 45.0, true,
                new[] { "stability" },
                NoExperimentIds);
            AddNode("basicScience", "Basic Science", 45.0, false,
                new[] { "survivability" },
                new[] { MaterialsStudyExperimentId });
            AddNode("flightControl", "Flight Control", 45.0, true,
                new[] { "survivability", "stability" },
                NoExperimentIds);
            AddNode("advRocketry", "Advanced Rocketry", 45.0, false,
                new[] { "generalRocketry" },
                NoExperimentIds);
            AddNode("generalConstruction", "General Construction", 45.0, true,
                new[] { "stability", "generalRocketry" },
                NoExperimentIds);
            AddNode("propulsionSystems", "Propulsion Systems", 90.0, false,
                new[] { "advRocketry" },
                NoExperimentIds);
            AddNode("spaceExploration", "Space Exploration", 90.0, false,
                new[] { "basicScience" },
                NoExperimentIds);
            AddNode("advFlightControl", "Advanced Flight Control", 90.0, false,
                new[] { "flightControl" },
                NoExperimentIds);
            AddNode("landing", "Landing", 90.0, true,
                new[] { "flightControl", "aviation" },
                NoExperimentIds);
            AddNode("aerodynamicSystems", "Aerodynamics", 90.0, true,
                new[] { "aviation", "generalConstruction" },
                NoExperimentIds);
            AddNode("electrics", "Electrics", 90.0, true,
                new[] { "basicScience" },
                NoExperimentIds);
            AddNode("heavyRocketry", "Heavy Rocketry", 90.0, false,
                new[] { "advRocketry" },
                NoExperimentIds);
            AddNode("fuelSystems", "Fuel Systems", 90.0, true,
                new[] { "advRocketry", "generalConstruction" },
                NoExperimentIds);
            AddNode("advConstruction", "Advanced Construction", 90.0, false,
                new[] { "generalConstruction" },
                NoExperimentIds);
            AddNode("miniaturization", "Miniaturization", 90.0, true,
                new[] { "basicScience" },
                new[] { EvaScienceExperimentId });
            AddNode("actuators", "Actuators", 160.0, true,
                new[] { "advConstruction" },
                NoExperimentIds);
            AddNode("commandModules", "Command Modules", 160.0, true,
                new[] { "spaceExploration", "advFlightControl" },
                NoExperimentIds);
            AddNode("heavierRocketry", "Heavier Rocketry", 160.0, true,
                new[] { "heavyRocketry" },
                NoExperimentIds);
            AddNode("precisionEngineering", "Precision Engineering", 160.0, true,
                new[] { "miniaturization", "electrics" },
                NoExperimentIds);
            AddNode("advExploration", "Advanced Exploration", 160.0, true,
                new[] { "spaceExploration" },
                NoExperimentIds);
            AddNode("specializedControl", "Specialized Control", 160.0, true,
                new[] { "advFlightControl" },
                NoExperimentIds);
            AddNode("advLanding", "Advanced Landing", 160.0, false,
                new[] { "landing" },
                NoExperimentIds);
            AddNode("supersonicFlight", "Supersonic Flight", 160.0, true,
                new[] { "aerodynamicSystems" },
                NoExperimentIds);
            AddNode("advFuelSystems", "Adv. Fuel Systems", 160.0, true,
                new[] { "propulsionSystems", "fuelSystems" },
                NoExperimentIds);
            AddNode("advElectrics", "Advanced Electrics", 160.0, false,
                new[] { "electrics" },
                NoExperimentIds);
            AddNode("specializedConstruction", "Specialized Construction", 160.0, true,
                new[] { "advConstruction" },
                NoExperimentIds);
            AddNode("precisionPropulsion", "Precision Propulsion", 160.0, false,
                new[] { "propulsionSystems" },
                NoExperimentIds);
            AddNode("advAerodynamics", "Advanced Aerodynamics", 160.0, true,
                new[] { "aerodynamicSystems" },
                NoExperimentIds);
            AddNode("heavyLanding", "Heavy Landing", 300.0, false,
                new[] { "advLanding" },
                NoExperimentIds);
            AddNode("scienceTech", "Scanning Tech", 300.0, true,
                new[] { "advExploration", "precisionEngineering" },
                new[] { AtmosphereAnalysisExperimentId, InfraredTelescopeExperimentId });
            AddNode("unmannedTech", "Unmanned Tech", 300.0, true,
                new[] { "precisionEngineering" },
                NoExperimentIds);
            AddNode("nuclearPropulsion", "Nuclear Propulsion", 300.0, false,
                new[] { "advFuelSystems", "heavierRocketry" },
                NoExperimentIds);
            AddNode("advMetalworks", "Advanced MetalWorks", 300.0, true,
                new[] { "specializedConstruction" },
                NoExperimentIds);
            AddNode("fieldScience", "Field Science", 300.0, true,
                new[] { "advLanding", "advExploration" },
                NoExperimentIds);
            AddNode("highAltitudeFlight", "High Altitude Flight", 300.0, true,
                new[] { "supersonicFlight" },
                NoExperimentIds);
            AddNode("largeVolumeContainment", "Large Volume Containment", 300.0, true,
                new[] { "advFuelSystems", "specializedConstruction" },
                NoExperimentIds);
            AddNode("composites", "Composites", 300.0, false,
                new[] { "specializedConstruction" },
                NoExperimentIds);
            AddNode("electronics", "Electronics", 300.0, true,
                new[] { "precisionEngineering", "advElectrics" },
                new[] { SeismicScanExperimentId, MagnetometerReportExperimentId });
            AddNode("largeElectrics", "High-Power Electrics", 300.0, false,
                new[] { "advElectrics" },
                NoExperimentIds);
            AddNode("heavyAerodynamics", "Heavy Aerodynamics", 300.0, true,
                new[] { "advAerodynamics" },
                NoExperimentIds);
            AddNode("ionPropulsion", "Ion Propulsion", 550.0, false,
                new[] { "scienceTech", "unmannedTech" },
                NoExperimentIds);
            AddNode("hypersonicFlight", "Hypersonic Flight", 550.0, true,
                new[] { "highAltitudeFlight" },
                NoExperimentIds);
            AddNode("nanolathing", "Nanolathing", 550.0, true,
                new[] { "advMetalworks" },
                NoExperimentIds);
            AddNode("advUnmanned", "Advanced Unmanned Tech", 550.0, true,
                new[] { "unmannedTech" },
                NoExperimentIds);
            AddNode("metaMaterials", "Meta-Materials", 550.0, true,
                new[] { "composites" },
                NoExperimentIds);
            AddNode("veryHeavyRocketry", "Very Heavy Rocketry", 550.0, true,
                new[] { "largeVolumeContainment", "heavierRocketry" },
                NoExperimentIds);
            AddNode("advScienceTech", "Advanced Science Tech", 550.0, true,
                new[] { "scienceTech", "fieldScience" },
                new[] { GravityScanExperimentId });
            AddNode("advancedMotors", "Advanced Motors", 550.0, false,
                new[] { "fieldScience" },
                NoExperimentIds);
            AddNode("specializedElectrics", "Specialized Electrics", 550.0, true,
                new[] { "largeElectrics" },
                NoExperimentIds);
            AddNode("highPerformanceFuelSystems", "High-Performance Fuel Systems", 550.0, true,
                new[] { "largeVolumeContainment" },
                NoExperimentIds);
            AddNode("experimentalAerodynamics", "Experimental Aerodynamics", 550.0, true,
                new[] { "heavyAerodynamics" },
                NoExperimentIds);
            AddNode("automation", "Automation", 550.0, true,
                new[] { "unmannedTech", "electronics" },
                NoExperimentIds);
            AddNode("aerospaceTech", "Aerospace Tech", 1000.0, true,
                new[] { "hypersonicFlight" },
                NoExperimentIds);
            AddNode("largeUnmanned", "Large Probes", 1000.0, true,
                new[] { "advUnmanned", "automation" },
                NoExperimentIds);
            AddNode("experimentalScience", "Experimental Science", 1000.0, false,
                new[] { "advScienceTech" },
                NoExperimentIds);
            AddNode("experimentalMotors", "Experimental Motors", 1000.0, false,
                new[] { "advancedMotors" },
                NoExperimentIds);
            AddNode("experimentalElectrics", "Experimental Electrics", 1000.0, false,
                new[] { "specializedElectrics" },
                NoExperimentIds);

            ReadOnlyTechNodes = TechNodes.AsReadOnly();
        }

        public static ReadOnlyCollection<RivalTechNodeDefinition> All
        {
            get { return ReadOnlyTechNodes; }
        }

        public static RivalTechNodeDefinition GetById(string techId)
        {
            if (string.IsNullOrEmpty(techId))
            {
                return null;
            }

            RivalTechNodeDefinition definition;
            return TechNodesById.TryGetValue(techId, out definition) ? definition : null;
        }

        /// <summary>
        /// Returns the technology node that unlocks the stock experiment, or null for experiments
        /// such as EVA Report and Surface Sample whose availability is controlled by separate programme gates.
        /// </summary>
        public static RivalTechNodeDefinition GetUnlockingTechForExperiment(string experimentId)
        {
            if (string.IsNullOrEmpty(experimentId))
            {
                return null;
            }

            RivalTechNodeDefinition definition;
            return ExperimentUnlocksById.TryGetValue(experimentId, out definition)
                ? definition
                : null;
        }

        public static bool IsExperimentUnlocked(
            string experimentId,
            ISet<string> researchedTechIds)
        {
            RivalTechNodeDefinition definition = GetUnlockingTechForExperiment(experimentId);
            return definition != null
                && ContainsTechId(researchedTechIds, definition.Id);
        }

        internal static bool ContainsTechId(ISet<string> researchedTechIds, string techId)
        {
            if (researchedTechIds == null || string.IsNullOrEmpty(techId))
            {
                return false;
            }

            if (researchedTechIds.Contains(techId))
            {
                return true;
            }

            // Persisted rival state is case-insensitive, but keep this helper safe if a caller supplies
            // another ISet implementation while evaluating project-owned test or migration state.
            foreach (string researchedTechId in researchedTechIds)
            {
                if (string.Equals(researchedTechId, techId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddNode(
            string id,
            string displayName,
            double scienceCost,
            bool anyPrerequisiteUnlocks,
            string[] prerequisiteTechIds,
            string[] unlockedExperimentIds)
        {
            var definition = new RivalTechNodeDefinition(
                id,
                displayName,
                scienceCost,
                anyPrerequisiteUnlocks,
                prerequisiteTechIds,
                unlockedExperimentIds);
            TechNodes.Add(definition);
            TechNodesById.Add(definition.Id, definition);

            for (int experimentIndex = 0;
                experimentIndex < definition.UnlockedExperimentIds.Count;
                experimentIndex++)
            {
                ExperimentUnlocksById.Add(
                    definition.UnlockedExperimentIds[experimentIndex],
                    definition);
            }
        }
    }
}

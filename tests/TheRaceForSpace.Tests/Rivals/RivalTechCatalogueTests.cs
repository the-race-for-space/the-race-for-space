using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Core;
using TheRaceForSpace.Rivals;

namespace TheRaceForSpace.Tests.Rivals
{
    internal static class RivalTechCatalogueTests
    {
        public static void RunAll()
        {
            CatalogueContainsApprovedStockNodes();
            PrerequisiteRulesPreserveStockAnyAndAllSemantics();
            ExperimentUnlocksMatchApprovedScienceDesign();
            ResearchAndDevelopmentCostTiersMatchApprovedLimits();
            LookupUsesStableCaseInsensitiveIds();
        }

        private static void CatalogueContainsApprovedStockNodes()
        {
            Equal(63, RivalTechCatalogue.All.Count);

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tierCounts = new Dictionary<double, int>();
            for (int nodeIndex = 0; nodeIndex < RivalTechCatalogue.All.Count; nodeIndex++)
            {
                RivalTechNodeDefinition node = RivalTechCatalogue.All[nodeIndex];
                Require(!string.IsNullOrEmpty(node.Id), "Every rival technology needs a stable ID.");
                Require(!string.IsNullOrEmpty(node.DisplayName), "Every rival technology needs a display name.");
                Require(seenIds.Add(node.Id), "Duplicate rival technology ID: " + node.Id);

                int count;
                tierCounts.TryGetValue(node.ScienceCost, out count);
                tierCounts[node.ScienceCost] = count + 1;

                for (int prerequisiteIndex = 0;
                    prerequisiteIndex < node.PrerequisiteTechIds.Count;
                    prerequisiteIndex++)
                {
                    RivalTechNodeDefinition prerequisite =
                        RivalTechCatalogue.GetById(node.PrerequisiteTechIds[prerequisiteIndex]);
                    Require(prerequisite != null,
                        "Unknown prerequisite '" + node.PrerequisiteTechIds[prerequisiteIndex]
                        + "' on " + node.Id + ".");
                    Require(prerequisite.ScienceCost < node.ScienceCost,
                        "Prerequisite costs should precede child technology costs for " + node.Id + ".");
                }
            }

            AssertTierCount(tierCounts, 0.0, 1);
            AssertTierCount(tierCounts, 5.0, 2);
            AssertTierCount(tierCounts, 15.0, 1);
            AssertTierCount(tierCounts, 18.0, 1);
            AssertTierCount(tierCounts, 20.0, 1);
            AssertTierCount(tierCounts, 45.0, 5);
            AssertTierCount(tierCounts, 90.0, 10);
            AssertTierCount(tierCounts, 160.0, 13);
            AssertTierCount(tierCounts, 300.0, 12);
            AssertTierCount(tierCounts, 550.0, 12);
            AssertTierCount(tierCounts, 1000.0, 5);

            RivalTechNodeDefinition start = RivalTechCatalogue.GetById(RivalProgramState.StartingTechId);
            Require(start != null, "The persisted starting technology ID must exist in the catalogue.");
            Equal(0.0, start.ScienceCost);
            Equal("Start", start.DisplayName);

            RivalTechNodeDefinition scanningTech = RivalTechCatalogue.GetById("scienceTech");
            Require(scanningTech != null, "The stock scienceTech ID should remain stable.");
            Equal("Scanning Tech", scanningTech.DisplayName);
            Equal(300.0, scanningTech.ScienceCost);

            Equal(1000.0, RivalTechCatalogue.GetById("experimentalScience").ScienceCost);
            Equal("Large Probes", RivalTechCatalogue.GetById("largeUnmanned").DisplayName);
        }

        private static void PrerequisiteRulesPreserveStockAnyAndAllSemantics()
        {
            RivalTechNodeDefinition start = RivalTechCatalogue.GetById("start");
            Require(start.ArePrerequisitesSatisfied(null),
                "Start has no parent technology and should satisfy ancestry by definition.");

            RivalTechNodeDefinition stability = RivalTechCatalogue.GetById("stability");
            Require(stability.AnyPrerequisiteUnlocks,
                "Stability should preserve stock any-parent unlock semantics.");
            Equal(2, stability.PrerequisiteTechIds.Count);
            Require(stability.ArePrerequisitesSatisfied(TechSet("engineering101")),
                "Either Stability parent should be sufficient.");
            Require(stability.ArePrerequisitesSatisfied(TechSet("basicRocketry")),
                "Either Stability parent should be sufficient.");
            Require(!stability.ArePrerequisitesSatisfied(TechSet("start")),
                "An unrelated researched node should not satisfy Stability.");

            RivalTechNodeDefinition nuclearPropulsion = RivalTechCatalogue.GetById("nuclearPropulsion");
            Require(!nuclearPropulsion.AnyPrerequisiteUnlocks,
                "Nuclear Propulsion should require all of its stock parents.");
            Equal(2, nuclearPropulsion.PrerequisiteTechIds.Count);
            Require(!nuclearPropulsion.ArePrerequisitesSatisfied(TechSet("advFuelSystems")),
                "One Nuclear Propulsion parent should not be enough.");
            Require(nuclearPropulsion.ArePrerequisitesSatisfied(
                    TechSet("advFuelSystems", "heavierRocketry")),
                "Both Nuclear Propulsion parents should satisfy the node.");

            RivalTechNodeDefinition scanningTech = RivalTechCatalogue.GetById("scienceTech");
            Require(scanningTech.AnyPrerequisiteUnlocks,
                "Scanning Tech should preserve stock any-parent unlock semantics.");
            Require(scanningTech.ArePrerequisitesSatisfied(TechSet("advExploration")),
                "Advanced Exploration should independently unlock Scanning Tech ancestry.");
            Require(scanningTech.ArePrerequisitesSatisfied(TechSet("precisionEngineering")),
                "Precision Engineering should independently unlock Scanning Tech ancestry.");

            RivalTechNodeDefinition largeProbes = RivalTechCatalogue.GetById("largeUnmanned");
            Require(largeProbes.AnyPrerequisiteUnlocks,
                "Large Probes should preserve stock any-parent unlock semantics.");
            Require(largeProbes.ArePrerequisitesSatisfied(TechSet("advUnmanned")),
                "Advanced Unmanned Tech should independently unlock Large Probes ancestry.");
            Require(largeProbes.ArePrerequisitesSatisfied(TechSet("automation")),
                "Automation should independently unlock Large Probes ancestry.");
        }

        private static void ExperimentUnlocksMatchApprovedScienceDesign()
        {
            AssertExperimentUnlock(RivalTechCatalogue.CrewReportExperimentId, "start");
            AssertExperimentUnlock(RivalTechCatalogue.MysteryGooExperimentId, "start");
            AssertExperimentUnlock(RivalTechCatalogue.TemperatureScanExperimentId, "engineering101");
            AssertExperimentUnlock(RivalTechCatalogue.AtmosphericPressureScanExperimentId, "survivability");
            AssertExperimentUnlock(RivalTechCatalogue.MaterialsStudyExperimentId, "basicScience");
            AssertExperimentUnlock(RivalTechCatalogue.EvaScienceExperimentId, "miniaturization");
            AssertExperimentUnlock(RivalTechCatalogue.AtmosphereAnalysisExperimentId, "scienceTech");
            AssertExperimentUnlock(RivalTechCatalogue.InfraredTelescopeExperimentId, "scienceTech");
            AssertExperimentUnlock(RivalTechCatalogue.SeismicScanExperimentId, "electronics");
            AssertExperimentUnlock(RivalTechCatalogue.MagnetometerReportExperimentId, "electronics");
            AssertExperimentUnlock(RivalTechCatalogue.GravityScanExperimentId, "advScienceTech");

            Require(RivalTechCatalogue.GetUnlockingTechForExperiment("evaReport") == null,
                "EVA Report is facility/access gated and must not be assigned to a tech node.");
            Require(RivalTechCatalogue.GetUnlockingTechForExperiment("surfaceSample") == null,
                "Surface Sample is R&D/access gated and must not be assigned to a tech node.");

            Require(RivalTechCatalogue.IsExperimentUnlocked(
                    RivalTechCatalogue.CrewReportExperimentId,
                    TechSet(RivalProgramState.StartingTechId)),
                "Start should make Crew Report available to a new rival.");
            Require(!RivalTechCatalogue.IsExperimentUnlocked(
                    RivalTechCatalogue.TemperatureScanExperimentId,
                    TechSet(RivalProgramState.StartingTechId)),
                "Start alone should not unlock Temperature Scan.");
            Require(RivalTechCatalogue.IsExperimentUnlocked(
                    RivalTechCatalogue.TemperatureScanExperimentId,
                    TechSet(RivalProgramState.StartingTechId, "ENGINEERING101")),
                "Experiment lookup should work with persisted case-insensitive tech IDs.");
        }

        private static void ResearchAndDevelopmentCostTiersMatchApprovedLimits()
        {
            CampaignSettings.ResetToDefaults();

            int level1Allowed = 0;
            int level2Allowed = 0;
            int level3Allowed = 0;
            for (int nodeIndex = 0; nodeIndex < RivalTechCatalogue.All.Count; nodeIndex++)
            {
                RivalTechNodeDefinition node = RivalTechCatalogue.All[nodeIndex];
                if (IsAllowedByScienceCostLimit(
                    node.ScienceCost,
                    CampaignSettings.RivalResearchAndDevelopmentLevel1ScienceCostLimit))
                {
                    level1Allowed++;
                }

                if (IsAllowedByScienceCostLimit(
                    node.ScienceCost,
                    CampaignSettings.RivalResearchAndDevelopmentLevel2ScienceCostLimit))
                {
                    level2Allowed++;
                }

                if (IsAllowedByScienceCostLimit(
                    node.ScienceCost,
                    CampaignSettings.RivalResearchAndDevelopmentLevel3ScienceCostLimit))
                {
                    level3Allowed++;
                }
            }

            Equal(21, level1Allowed);
            Equal(46, level2Allowed);
            Equal(63, level3Allowed);

            Require(IsAllowedByScienceCostLimit(
                    RivalTechCatalogue.GetById("miniaturization").ScienceCost,
                    CampaignSettings.RivalResearchAndDevelopmentLevel1ScienceCostLimit),
                "R&D Level 1 should permit the 90-Science tier.");
            Require(!IsAllowedByScienceCostLimit(
                    RivalTechCatalogue.GetById("precisionEngineering").ScienceCost,
                    CampaignSettings.RivalResearchAndDevelopmentLevel1ScienceCostLimit),
                "R&D Level 1 should block the 160-Science tier.");
            Require(IsAllowedByScienceCostLimit(
                    RivalTechCatalogue.GetById("scienceTech").ScienceCost,
                    CampaignSettings.RivalResearchAndDevelopmentLevel2ScienceCostLimit),
                "R&D Level 2 should permit the 300-Science tier.");
            Require(!IsAllowedByScienceCostLimit(
                    RivalTechCatalogue.GetById("advScienceTech").ScienceCost,
                    CampaignSettings.RivalResearchAndDevelopmentLevel2ScienceCostLimit),
                "R&D Level 2 should block the 550-Science tier.");
            Require(IsAllowedByScienceCostLimit(
                    RivalTechCatalogue.GetById("experimentalScience").ScienceCost,
                    CampaignSettings.RivalResearchAndDevelopmentLevel3ScienceCostLimit),
                "R&D Level 3 uses the configured unlimited sentinel.");
        }

        private static void LookupUsesStableCaseInsensitiveIds()
        {
            RivalTechNodeDefinition lower = RivalTechCatalogue.GetById("advScienceTech");
            RivalTechNodeDefinition upper = RivalTechCatalogue.GetById("ADVSCIENCETECH");
            Require(object.ReferenceEquals(lower, upper),
                "Technology lookup should use stable case-insensitive IDs.");

            RivalTechNodeDefinition experimentLower =
                RivalTechCatalogue.GetUnlockingTechForExperiment("magnetometer");
            RivalTechNodeDefinition experimentUpper =
                RivalTechCatalogue.GetUnlockingTechForExperiment("MAGNETOMETER");
            Require(object.ReferenceEquals(experimentLower, experimentUpper),
                "Experiment lookup should use stable case-insensitive stock IDs.");

            Require(RivalTechCatalogue.GetById("not-a-stock-tech") == null,
                "Unknown technology IDs should fail safely.");
            Require(RivalTechCatalogue.GetUnlockingTechForExperiment("not-an-experiment") == null,
                "Unknown experiment IDs should fail safely.");
        }

        private static bool IsAllowedByScienceCostLimit(double scienceCost, double limit)
        {
            return limit <= 0.0 || scienceCost <= limit;
        }

        private static HashSet<string> TechSet(params string[] techIds)
        {
            return new HashSet<string>(techIds, StringComparer.OrdinalIgnoreCase);
        }

        private static void AssertExperimentUnlock(string experimentId, string expectedTechId)
        {
            RivalTechNodeDefinition definition =
                RivalTechCatalogue.GetUnlockingTechForExperiment(experimentId);
            Require(definition != null,
                "Expected an unlocking technology for experiment '" + experimentId + "'.");
            Equal(expectedTechId, definition.Id);
        }

        private static void AssertTierCount(
            IDictionary<double, int> tierCounts,
            double scienceCost,
            int expectedCount)
        {
            int actualCount;
            Require(tierCounts.TryGetValue(scienceCost, out actualCount),
                "Missing rival technology Science tier " + scienceCost + ".");
            Equal(expectedCount, actualCount);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    "Expected '" + expected + "' but got '" + actual + "'.");
            }
        }
    }
}

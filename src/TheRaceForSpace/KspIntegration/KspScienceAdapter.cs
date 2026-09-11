using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using UnityEngine;

namespace TheRaceForSpace.KspIntegration
{
    /// <summary>
    /// Project-owned read model for one stock KSP Science subject. Raw KSP ScienceSubject objects
    /// never leave KspIntegration.
    /// </summary>
    internal sealed class KspScienceSubjectSnapshot
    {
        internal KspScienceSubjectSnapshot(
            ScienceSubjectKey subject,
            string experimentTitle,
            string subjectTitle,
            string biomeDisplayName,
            double remainingScience,
            double scienceCap)
        {
            Subject = subject;
            ExperimentTitle = experimentTitle;
            SubjectTitle = subjectTitle;
            BiomeDisplayName = biomeDisplayName;
            RemainingScience = remainingScience;
            ScienceCap = scienceCap;
        }

        public ScienceSubjectKey Subject { get; private set; }
        public string ExperimentTitle { get; private set; }
        public string SubjectTitle { get; private set; }
        public string BiomeDisplayName { get; private set; }
        public double RemainingScience { get; private set; }
        public double ScienceCap { get; private set; }
    }

    /// <summary>
    /// Project-owned biome identity returned from KSP for Science target generation.
    /// </summary>
    internal sealed class KspScienceBiomeSnapshot
    {
        internal KspScienceBiomeSnapshot(string biomeName, string displayName, bool isMiniBiome)
        {
            BiomeName = biomeName;
            DisplayName = displayName;
            IsMiniBiome = isMiniBiome;
        }

        public string BiomeName { get; private set; }
        public string DisplayName { get; private set; }
        public bool IsMiniBiome { get; private set; }
    }

    /// <summary>
    /// Stock KSP Science boundary for rival Science Expeditions. It resolves experiments, bodies,
    /// situations and biomes into project-owned snapshots, reads the player's shared subject pool,
    /// and exhausts the exact stock subject when a rival wins that Science chronologically.
    /// </summary>
    internal static class KspScienceAdapter
    {
        private const string LandedSituation = "Landed";
        private const string SplashedSituation = "Splashed";
        private const string FlyingLowSituation = "FlyingLow";
        private const string FlyingHighSituation = "FlyingHigh";
        private const string LowSpaceSituation = "LowSpace";
        private const string HighSpaceSituation = "HighSpace";

        /// <summary>
        /// Resolves one requested project-owned Science identity to the current stock subject state.
        /// Reading an untouched subject uses a temporary ScienceSubject rather than registering every
        /// possible expedition in the player's R&D archives during rival target selection.
        /// </summary>
        internal static bool TryCaptureSubject(
            ScienceSubjectKey requestedSubject,
            out KspScienceSubjectSnapshot snapshot)
        {
            snapshot = null;

            ScienceExperiment experiment;
            ScienceSubject scienceSubject;
            ScienceSubjectKey canonicalSubject;
            string biomeDisplayName;
            if (!TryResolveSubject(
                    requestedSubject,
                    false,
                    out experiment,
                    out scienceSubject,
                    out canonicalSubject,
                    out biomeDisplayName))
            {
                return false;
            }

            double remainingScience;
            double scienceCap;
            if (!TryCalculateRemainingScience(scienceSubject, out remainingScience, out scienceCap))
            {
                return false;
            }

            string experimentTitle = string.IsNullOrEmpty(experiment.experimentTitle)
                ? experiment.id
                : experiment.experimentTitle;
            string subjectTitle = string.IsNullOrEmpty(scienceSubject.title)
                ? experimentTitle
                : scienceSubject.title;

            snapshot = new KspScienceSubjectSnapshot(
                canonicalSubject,
                experimentTitle,
                subjectTitle,
                biomeDisplayName,
                remainingScience,
                scienceCap);
            return true;
        }

        internal static bool TryGetRemainingScience(
            ScienceSubjectKey subject,
            out double remainingScience)
        {
            remainingScience = 0.0;
            KspScienceSubjectSnapshot snapshot;
            if (!TryCaptureSubject(subject, out snapshot))
            {
                return false;
            }

            remainingScience = snapshot.RemainingScience;
            return true;
        }

        /// <summary>
        /// Exhausts the exact stock subject and returns the Science that was still available immediately
        /// before exhaustion. The player's banked R&D Science total is deliberately not changed.
        /// </summary>
        internal static double ConsumeRemainingScience(ScienceSubjectKey requestedSubject)
        {
            ScienceExperiment experiment;
            ScienceSubject scienceSubject;
            ScienceSubjectKey canonicalSubject;
            string biomeDisplayName;
            if (!TryResolveSubject(
                    requestedSubject,
                    true,
                    out experiment,
                    out scienceSubject,
                    out canonicalSubject,
                    out biomeDisplayName))
            {
                return 0.0;
            }

            double remainingScience;
            double scienceCap;
            if (!TryCalculateRemainingScience(scienceSubject, out remainingScience, out scienceCap))
            {
                return 0.0;
            }

            // SubmitScienceData cannot be used here: that path awards Science to the player's R&D
            // balance. A rival win must only consume the shared stock subject, so mutate the subject
            // fields that KSP itself persists under the R&D ScenarioModule and leave player Science alone.
            scienceSubject.science = scienceSubject.scienceCap;
            scienceSubject.scientificValue = 0.0f;

            if (remainingScience > 0.0)
            {
                Debug.Log(
                    "[TheRaceForSpace] Rival Science exhausted stock subject '"
                    + scienceSubject.id
                    + "' with "
                    + remainingScience.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                    + " Science remaining.");
            }

            return remainingScience;
        }

        /// <summary>
        /// Returns the canonical stock title for an experiment ID without exposing ScienceExperiment.
        /// </summary>
        internal static bool TryGetExperimentTitle(string experimentId, out string experimentTitle)
        {
            experimentTitle = null;

            ScienceExperiment experiment;
            if (!TryGetExperiment(experimentId, out experiment))
            {
                return false;
            }

            experimentTitle = string.IsNullOrEmpty(experiment.experimentTitle)
                ? experiment.id
                : experiment.experimentTitle;
            return true;
        }

        /// <summary>
        /// Captures stock biome IDs and display names for one body. The returned identities contain
        /// no KSP objects and can be used by later rival Science target-generation logic.
        /// </summary>
        internal static IList<KspScienceBiomeSnapshot> CaptureBodyBiomes(
            string bodyName,
            bool includeMiniBiomes)
        {
            var snapshots = new List<KspScienceBiomeSnapshot>();
            CelestialBody body = FindBody(bodyName);
            if (body == null)
            {
                return snapshots;
            }

            List<string> biomeTags = ResearchAndDevelopment.GetBiomeTags(body, includeMiniBiomes);
            if (biomeTags == null || biomeTags.Count == 0)
            {
                return snapshots;
            }

            var miniBiomeTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (includeMiniBiomes)
            {
                List<string> stockMiniBiomeTags = ResearchAndDevelopment.GetMiniBiomeTags(body);
                if (stockMiniBiomeTags != null)
                {
                    for (int biomeIndex = 0; biomeIndex < stockMiniBiomeTags.Count; biomeIndex++)
                    {
                        string miniBiomeTag = stockMiniBiomeTags[biomeIndex];
                        if (!string.IsNullOrEmpty(miniBiomeTag))
                        {
                            miniBiomeTags.Add(miniBiomeTag);
                        }
                    }
                }
            }

            var addedBiomeTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int biomeIndex = 0; biomeIndex < biomeTags.Count; biomeIndex++)
            {
                string biomeTag = biomeTags[biomeIndex];
                if (string.IsNullOrEmpty(biomeTag) || !addedBiomeTags.Add(biomeTag))
                {
                    continue;
                }

                bool isMiniBiome = miniBiomeTags.Contains(biomeTag);
                snapshots.Add(new KspScienceBiomeSnapshot(
                    biomeTag,
                    GetBiomeDisplayName(body, biomeTag, isMiniBiome),
                    isMiniBiome));
            }

            return snapshots;
        }

        private static bool TryResolveSubject(
            ScienceSubjectKey requestedSubject,
            bool createRegisteredSubject,
            out ScienceExperiment experiment,
            out ScienceSubject scienceSubject,
            out ScienceSubjectKey canonicalSubject,
            out string biomeDisplayName)
        {
            experiment = null;
            scienceSubject = null;
            canonicalSubject = null;
            biomeDisplayName = string.Empty;

            if (requestedSubject == null
                || ResearchAndDevelopment.Instance == null
                || string.IsNullOrEmpty(requestedSubject.ExperimentId)
                || string.IsNullOrEmpty(requestedSubject.BodyName)
                || string.IsNullOrEmpty(requestedSubject.Situation))
            {
                return false;
            }

            if (!TryGetExperiment(requestedSubject.ExperimentId, out experiment))
            {
                return false;
            }

            CelestialBody body = FindBody(requestedSubject.BodyName);
            if (body == null)
            {
                return false;
            }

            ExperimentSituations situation;
            string canonicalSituation;
            if (!TryConvertSituation(
                    requestedSubject.Situation,
                    out situation,
                    out canonicalSituation)
                || !experiment.IsAvailableWhile(situation, body))
            {
                return false;
            }

            string biomeTag = string.Empty;
            if (experiment.BiomeIsRelevantWhile(situation))
            {
                if (!TryResolveBiome(
                        body,
                        requestedSubject.BiomeName,
                        out biomeTag,
                        out biomeDisplayName))
                {
                    return false;
                }
            }
            else if (!string.IsNullOrEmpty(requestedSubject.BiomeName))
            {
                // A biome-bearing key must not alias a stock non-biome subject. Keeping the identity
                // canonical is important because the player and every rival share this exact pool.
                return false;
            }

            canonicalSubject = new ScienceSubjectKey(
                experiment.id,
                body.bodyName,
                canonicalSituation,
                biomeTag);

            if (createRegisteredSubject)
            {
                scienceSubject = ResearchAndDevelopment.GetExperimentSubject(
                    experiment,
                    situation,
                    body,
                    biomeTag,
                    biomeDisplayName);
                return scienceSubject != null;
            }

            // GetExperimentSubject registers a subject in R&D when it does not already exist. Target
            // discovery can probe many combinations, so construct a temporary subject first and only
            // read the persisted R&D subject when the player has already encountered that exact ID.
            ScienceSubject temporarySubject = new ScienceSubject(
                experiment,
                situation,
                body,
                biomeTag);
            if (temporarySubject == null || string.IsNullOrEmpty(temporarySubject.id))
            {
                return false;
            }

            scienceSubject = ResearchAndDevelopment.GetSubjectByID(temporarySubject.id)
                ?? temporarySubject;
            return true;
        }

        private static bool TryGetExperiment(
            string requestedExperimentId,
            out ScienceExperiment experiment)
        {
            experiment = null;
            if (string.IsNullOrEmpty(requestedExperimentId))
            {
                return false;
            }

            List<string> experimentIds = ResearchAndDevelopment.GetExperimentIDs();
            if (experimentIds == null)
            {
                return false;
            }

            string canonicalExperimentId = null;
            for (int experimentIndex = 0; experimentIndex < experimentIds.Count; experimentIndex++)
            {
                string experimentId = experimentIds[experimentIndex];
                if (string.Equals(
                    experimentId,
                    requestedExperimentId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    canonicalExperimentId = experimentId;
                    break;
                }
            }

            if (string.IsNullOrEmpty(canonicalExperimentId))
            {
                return false;
            }

            experiment = ResearchAndDevelopment.GetExperiment(canonicalExperimentId);
            return experiment != null;
        }

        private static CelestialBody FindBody(string requestedBodyName)
        {
            if (string.IsNullOrEmpty(requestedBodyName))
            {
                return null;
            }

            CelestialBody directBody = FlightGlobals.GetBodyByName(requestedBodyName);
            if (directBody != null)
            {
                return directBody;
            }

            if (FlightGlobals.Bodies == null)
            {
                return null;
            }

            for (int bodyIndex = 0; bodyIndex < FlightGlobals.Bodies.Count; bodyIndex++)
            {
                CelestialBody body = FlightGlobals.Bodies[bodyIndex];
                if (body != null
                    && string.Equals(
                        body.bodyName,
                        requestedBodyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return body;
                }
            }

            return null;
        }

        private static bool TryResolveBiome(
            CelestialBody body,
            string requestedBiomeName,
            out string biomeTag,
            out string biomeDisplayName)
        {
            biomeTag = string.Empty;
            biomeDisplayName = string.Empty;
            if (body == null || string.IsNullOrEmpty(requestedBiomeName))
            {
                return false;
            }

            List<string> biomeTags = ResearchAndDevelopment.GetBiomeTags(body, true);
            if (biomeTags == null || biomeTags.Count == 0)
            {
                return false;
            }

            var miniBiomeTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> stockMiniBiomeTags = ResearchAndDevelopment.GetMiniBiomeTags(body);
            if (stockMiniBiomeTags != null)
            {
                for (int biomeIndex = 0; biomeIndex < stockMiniBiomeTags.Count; biomeIndex++)
                {
                    string miniBiomeTag = stockMiniBiomeTags[biomeIndex];
                    if (!string.IsNullOrEmpty(miniBiomeTag))
                    {
                        miniBiomeTags.Add(miniBiomeTag);
                    }
                }
            }

            for (int biomeIndex = 0; biomeIndex < biomeTags.Count; biomeIndex++)
            {
                string candidateTag = biomeTags[biomeIndex];
                if (string.IsNullOrEmpty(candidateTag))
                {
                    continue;
                }

                bool isMiniBiome = miniBiomeTags.Contains(candidateTag);
                string candidateDisplayName = GetBiomeDisplayName(
                    body,
                    candidateTag,
                    isMiniBiome);
                if (!string.Equals(
                        candidateTag,
                        requestedBiomeName,
                        StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(
                        candidateDisplayName,
                        requestedBiomeName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                biomeTag = candidateTag;
                biomeDisplayName = candidateDisplayName;
                return true;
            }

            return false;
        }

        private static string GetBiomeDisplayName(
            CelestialBody body,
            string biomeTag,
            bool isMiniBiome)
        {
            if (body == null || string.IsNullOrEmpty(biomeTag))
            {
                return string.Empty;
            }

            string displayName = isMiniBiome
                ? ResearchAndDevelopment.GetMiniBiomedisplayNameByScienceID(biomeTag, true)
                : ScienceUtil.GetBiomedisplayName(body, biomeTag);
            return string.IsNullOrEmpty(displayName) ? biomeTag : displayName;
        }

        private static bool TryConvertSituation(
            string requestedSituation,
            out ExperimentSituations situation,
            out string canonicalSituation)
        {
            situation = ExperimentSituations.SrfLanded;
            canonicalSituation = null;
            if (string.IsNullOrEmpty(requestedSituation))
            {
                return false;
            }

            string normalizedSituation = requestedSituation
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty);

            if (string.Equals(normalizedSituation, "Landed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedSituation, "SrfLanded", StringComparison.OrdinalIgnoreCase))
            {
                situation = ExperimentSituations.SrfLanded;
                canonicalSituation = LandedSituation;
                return true;
            }

            if (string.Equals(normalizedSituation, "Splashed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedSituation, "SrfSplashed", StringComparison.OrdinalIgnoreCase))
            {
                situation = ExperimentSituations.SrfSplashed;
                canonicalSituation = SplashedSituation;
                return true;
            }

            if (string.Equals(normalizedSituation, "FlyingLow", StringComparison.OrdinalIgnoreCase))
            {
                situation = ExperimentSituations.FlyingLow;
                canonicalSituation = FlyingLowSituation;
                return true;
            }

            if (string.Equals(normalizedSituation, "FlyingHigh", StringComparison.OrdinalIgnoreCase))
            {
                situation = ExperimentSituations.FlyingHigh;
                canonicalSituation = FlyingHighSituation;
                return true;
            }

            if (string.Equals(normalizedSituation, "LowSpace", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedSituation, "InSpaceLow", StringComparison.OrdinalIgnoreCase))
            {
                situation = ExperimentSituations.InSpaceLow;
                canonicalSituation = LowSpaceSituation;
                return true;
            }

            if (string.Equals(normalizedSituation, "HighSpace", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalizedSituation, "InSpaceHigh", StringComparison.OrdinalIgnoreCase))
            {
                situation = ExperimentSituations.InSpaceHigh;
                canonicalSituation = HighSpaceSituation;
                return true;
            }

            return false;
        }

        private static bool TryCalculateRemainingScience(
            ScienceSubject scienceSubject,
            out double remainingScience,
            out double scienceCap)
        {
            remainingScience = 0.0;
            scienceCap = 0.0;
            if (scienceSubject == null
                || !IsFinite(scienceSubject.science)
                || scienceSubject.science < 0.0f
                || !IsFinite(scienceSubject.scienceCap)
                || scienceSubject.scienceCap < 0.0f)
            {
                return false;
            }

            scienceCap = scienceSubject.scienceCap;
            double scienceAlreadyEarned = Math.Min(scienceSubject.science, scienceSubject.scienceCap);
            remainingScience = Math.Max(0.0, scienceCap - scienceAlreadyEarned);
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

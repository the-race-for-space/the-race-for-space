using System;
using System.Collections.Generic;
using System.Text;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Core;
using TheRaceForSpace.Rivals;
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
        private const string KerbinBodyName = "Kerbin";
        private const string SunBodyName = "Sun";
        private const string LandedSituation = "Landed";
        private const string SplashedSituation = "Splashed";
        private const string FlyingLowSituation = "FlyingLow";
        private const string FlyingHighSituation = "FlyingHigh";
        private const string LowSpaceSituation = "LowSpace";
        private const string HighSpaceSituation = "HighSpace";
        private const double ScienceVerificationTolerance = 0.0001;

        private static readonly string[] RivalScienceSituations =
        {
            LandedSituation,
            SplashedSituation,
            FlyingLowSituation,
            FlyingHighSituation,
            LowSpaceSituation,
            HighSpaceSituation
        };

        /// <summary>
        /// Captures all stock-valid subjects for the supplied rival-unlocked experiment IDs. This method
        /// is called only when the chronological rival Science flow needs a target/revalidation snapshot;
        /// it does not create another realtime scheduler. A null result means KSP's Science boundary is
        /// not ready yet, while an empty list means it is ready but no supplied experiment has a subject.
        /// </summary>
        internal static IList<RivalScienceSubjectCandidate> CaptureScienceCandidates(
            IList<string> experimentIds)
        {
            if (ResearchAndDevelopment.Instance == null || FlightGlobals.Bodies == null)
            {
                return null;
            }

            var candidates = new List<RivalScienceSubjectCandidate>();
            if (experimentIds == null || experimentIds.Count == 0)
            {
                return candidates;
            }

            var bodyNames = GetSupportedBodyNames();
            var bodyBiomes = new Dictionary<string, IList<KspScienceBiomeSnapshot>>(
                StringComparer.OrdinalIgnoreCase);
            var candidateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int bodyIndex = 0; bodyIndex < bodyNames.Count; bodyIndex++)
            {
                string bodyName = bodyNames[bodyIndex];
                bodyBiomes[bodyName] = CaptureBodyBiomes(
                    bodyName,
                    string.Equals(bodyName, KerbinBodyName, StringComparison.OrdinalIgnoreCase));
            }

            for (int experimentIndex = 0; experimentIndex < experimentIds.Count; experimentIndex++)
            {
                string experimentId = experimentIds[experimentIndex];
                if (string.IsNullOrEmpty(experimentId))
                {
                    continue;
                }

                for (int bodyIndex = 0; bodyIndex < bodyNames.Count; bodyIndex++)
                {
                    string bodyName = bodyNames[bodyIndex];
                    IList<KspScienceBiomeSnapshot> biomes = bodyBiomes[bodyName];

                    for (int situationIndex = 0;
                        situationIndex < RivalScienceSituations.Length;
                        situationIndex++)
                    {
                        string situation = RivalScienceSituations[situationIndex];
                        ScienceSubjectKey nonBiomeSubject = new ScienceSubjectKey(
                            experimentId,
                            bodyName,
                            situation,
                            string.Empty);
                        KspScienceSubjectSnapshot nonBiomeSnapshot;
                        if (TryCaptureSubject(nonBiomeSubject, out nonBiomeSnapshot))
                        {
                            string locationId = GetScienceLocationId(
                                bodyName,
                                situation,
                                null);
                            AddScienceCandidate(
                                candidates,
                                candidateKeys,
                                nonBiomeSnapshot,
                                locationId);
                            continue;
                        }

                        for (int biomeIndex = 0; biomeIndex < biomes.Count; biomeIndex++)
                        {
                            KspScienceBiomeSnapshot biome = biomes[biomeIndex];
                            if (biome == null
                                || string.IsNullOrEmpty(biome.BiomeName)
                                || (biome.IsMiniBiome
                                    && !string.Equals(
                                        situation,
                                        LandedSituation,
                                        StringComparison.Ordinal)))
                            {
                                continue;
                            }

                            ScienceSubjectKey biomeSubject = new ScienceSubjectKey(
                                experimentId,
                                bodyName,
                                situation,
                                biome.BiomeName);
                            KspScienceSubjectSnapshot biomeSnapshot;
                            if (!TryCaptureSubject(biomeSubject, out biomeSnapshot))
                            {
                                continue;
                            }

                            string locationId = GetScienceLocationId(
                                bodyName,
                                situation,
                                biome);
                            AddScienceCandidate(
                                candidates,
                                candidateKeys,
                                biomeSnapshot,
                                locationId);
                        }
                    }
                }
            }

            return candidates;
        }

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
        /// Exhausts the exact registered stock subject and returns the Science that was still available
        /// immediately before exhaustion. The player's banked R&D Science total is deliberately not changed.
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

            // SubmitScienceData cannot be used here because it awards Science and fires the player's
            // normal stock Science-received path. A rival win must instead exhaust the registered R&D
            // subject directly while leaving the player's already-banked Science unchanged.
            scienceSubject.science = scienceSubject.scienceCap;
            scienceSubject.scientificValue = 0.0f;

            // Re-read through R&D rather than trusting the object returned by GetExperimentSubject.
            // Untouched subjects may not previously exist in the player's archive, and the competitive
            // Science rule only works if KSP's registered subject is the object that was exhausted.
            ScienceSubject registeredSubject = ResearchAndDevelopment.GetSubjectByID(scienceSubject.id);
            double verifiedRemainingScience;
            double verifiedScienceCap;
            if (registeredSubject == null
                || !TryCalculateRemainingScience(
                    registeredSubject,
                    out verifiedRemainingScience,
                    out verifiedScienceCap)
                || verifiedRemainingScience > ScienceVerificationTolerance)
            {
                Debug.LogError(
                    "[TheRaceForSpace] Rival Science could not verify exhaustion of stock subject '"
                    + scienceSubject.id
                    + "'. No Science will be awarded to the rival.");
                return 0.0;
            }

            if (remainingScience > 0.0)
            {
                Debug.Log(
                    "[TheRaceForSpace] Rival Science exhausted registered stock subject '"
                    + registeredSubject.id
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

        private static IList<string> GetSupportedBodyNames()
        {
            var bodyNames = new List<string> { KerbinBodyName };
            var seenBodyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                KerbinBodyName
            };

            foreach (RivalMissionLocationSettings location in CampaignSettings.RivalMissionLocations)
            {
                if (location == null
                    || string.IsNullOrEmpty(location.LocationId)
                    || !location.LocationId.StartsWith("body:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string bodyName = location.LocationId.Substring("body:".Length);
                if (!string.IsNullOrEmpty(bodyName)
                    && !string.Equals(bodyName, SunBodyName, StringComparison.OrdinalIgnoreCase)
                    && seenBodyNames.Add(bodyName))
                {
                    bodyNames.Add(bodyName);
                }
            }

            bodyNames.Sort(StringComparer.OrdinalIgnoreCase);
            return bodyNames;
        }

        private static void AddScienceCandidate(
            IList<RivalScienceSubjectCandidate> candidates,
            ISet<string> candidateKeys,
            KspScienceSubjectSnapshot snapshot,
            string locationId)
        {
            if (snapshot == null
                || snapshot.Subject == null
                || string.IsNullOrEmpty(locationId)
                || CampaignSettings.GetRivalMissionLocationSettings(locationId) == null)
            {
                return;
            }

            string candidateKey = snapshot.Subject.ExperimentId
                + "|" + snapshot.Subject.BodyName
                + "|" + snapshot.Subject.Situation
                + "|" + snapshot.Subject.BiomeName
                + "|" + locationId;
            if (!candidateKeys.Add(candidateKey))
            {
                return;
            }

            candidates.Add(new RivalScienceSubjectCandidate(
                snapshot.Subject,
                locationId,
                snapshot.RemainingScience));
        }

        private static string GetScienceLocationId(
            string bodyName,
            string situation,
            KspScienceBiomeSnapshot biome)
        {
            if (string.IsNullOrEmpty(bodyName) || string.IsNullOrEmpty(situation))
            {
                return null;
            }

            if (!string.Equals(bodyName, KerbinBodyName, StringComparison.OrdinalIgnoreCase))
            {
                string bodyLocationId = "body:" + bodyName;
                return CampaignSettings.GetRivalMissionLocationSettings(bodyLocationId) == null
                    ? null
                    : bodyLocationId;
            }

            if (string.Equals(situation, LowSpaceSituation, StringComparison.Ordinal)
                || string.Equals(situation, HighSpaceSituation, StringComparison.Ordinal))
            {
                return CampaignSettings.RivalKerbinOrbitLocationId;
            }

            if (biome == null)
            {
                // Stock non-biome Kerbin subjects have no more precise location identity. Shores is
                // the stable local balance location used for those surface/flight expeditions.
                return CampaignSettings.GetRivalMissionLocationSettings("kerbin:shores") == null
                    ? null
                    : "kerbin:shores";
            }

            string prefix = biome.IsMiniBiome ? "ksc:" : "kerbin:";
            string locationId = ResolveConfiguredBiomeLocationId(prefix, biome.BiomeName);
            if (locationId != null)
            {
                return locationId;
            }

            return ResolveConfiguredBiomeLocationId(prefix, biome.DisplayName);
        }

        private static string ResolveConfiguredBiomeLocationId(string prefix, string biomeName)
        {
            string slug = ToLocationSlug(biomeName);
            if (string.IsNullOrEmpty(slug))
            {
                return null;
            }

            string locationId = prefix + slug;
            if (CampaignSettings.GetRivalMissionLocationSettings(locationId) != null)
            {
                return locationId;
            }

            if (string.Equals(prefix, "ksc:", StringComparison.OrdinalIgnoreCase)
                && slug.StartsWith("ksc-", StringComparison.OrdinalIgnoreCase))
            {
                locationId = prefix + slug.Substring("ksc-".Length);
                if (CampaignSettings.GetRivalMissionLocationSettings(locationId) != null)
                {
                    return locationId;
                }
            }

            return null;
        }

        private static string ToLocationSlug(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            var builder = new StringBuilder();
            bool needsSeparator = false;
            for (int characterIndex = 0; characterIndex < value.Length; characterIndex++)
            {
                char character = value[characterIndex];
                if (char.IsLetterOrDigit(character))
                {
                    if (needsSeparator && builder.Length > 0 && builder[builder.Length - 1] != '-')
                    {
                        builder.Append('-');
                    }
                    builder.Append(char.ToLowerInvariant(character));
                    needsSeparator = false;
                    continue;
                }

                if (character == '&')
                {
                    if (builder.Length > 0 && builder[builder.Length - 1] != '-')
                    {
                        builder.Append('-');
                    }
                    builder.Append("and");
                    needsSeparator = true;
                    continue;
                }

                needsSeparator = true;
            }

            string slug = builder.ToString().Trim('-');
            return string.IsNullOrEmpty(slug) ? null : slug;
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
                ScienceSubject resolvedSubject = ResearchAndDevelopment.GetExperimentSubject(
                    experiment,
                    situation,
                    body,
                    biomeTag,
                    biomeDisplayName);
                if (resolvedSubject == null || string.IsNullOrEmpty(resolvedSubject.id))
                {
                    return false;
                }

                // KSP may return a valid subject object before that untouched subject is present in the
                // persistent R&D subject collection. Rival Science must consume the registered instance;
                // otherwise the player can later generate a fresh subject and earn the same Science.
                scienceSubject = ResearchAndDevelopment.GetSubjectByID(resolvedSubject.id);
                if (scienceSubject == null)
                {
                    List<ScienceSubject> registeredSubjects = ResearchAndDevelopment.GetSubjects();
                    if (registeredSubjects == null)
                    {
                        Debug.LogError(
                            "[TheRaceForSpace] R&D subject collection was unavailable while registering '"
                            + resolvedSubject.id
                            + "'.");
                        return false;
                    }

                    bool alreadyPresent = false;
                    for (int subjectIndex = 0; subjectIndex < registeredSubjects.Count; subjectIndex++)
                    {
                        ScienceSubject candidate = registeredSubjects[subjectIndex];
                        if (candidate != null
                            && string.Equals(
                                candidate.id,
                                resolvedSubject.id,
                                StringComparison.Ordinal))
                        {
                            alreadyPresent = true;
                            break;
                        }
                    }

                    if (!alreadyPresent)
                    {
                        registeredSubjects.Add(resolvedSubject);
                    }

                    scienceSubject = ResearchAndDevelopment.GetSubjectByID(resolvedSubject.id);
                }

                if (scienceSubject == null)
                {
                    Debug.LogError(
                        "[TheRaceForSpace] Could not register stock Science subject '"
                        + resolvedSubject.id
                        + "' in R&D. Rival Science consumption was cancelled.");
                    return false;
                }

                return true;
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

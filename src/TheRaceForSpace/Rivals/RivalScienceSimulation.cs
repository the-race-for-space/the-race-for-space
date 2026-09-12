using System;
using System.Collections.Generic;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Core;
using TheRaceForSpace.Objectives;

namespace TheRaceForSpace.Rivals
{
    /// <summary>
    /// Project-owned snapshot of one stock Science subject that is currently available from KSP.
    /// The integration layer supplies these values; rival gameplay never receives raw KSP Science objects.
    /// </summary>
    internal sealed class RivalScienceSubjectCandidate
    {
        internal RivalScienceSubjectCandidate(
            ScienceSubjectKey subject,
            string locationId,
            double remainingScience)
        {
            Subject = subject;
            LocationId = locationId;
            RemainingScience = remainingScience;
        }

        public ScienceSubjectKey Subject { get; private set; }
        public string LocationId { get; private set; }
        public double RemainingScience { get; private set; }
    }

    /// <summary>
    /// KSP-independent rival Launch Science Expedition rules. RivalSimulation owns the chronological
    /// event loop; this class selects/revalidates one preparation and processes one requested daily
    /// progress check at a time.
    /// </summary>
    internal static class RivalScienceSimulation
    {
        internal const string EvaReportExperimentId = "evaReport";
        internal const string SurfaceSampleExperimentId = "surfaceSample";

        private const string KerbinBodyName = "Kerbin";
        private const string MunBodyName = "Mun";
        private const string MinmusBodyName = "Minmus";
        private const string SunBodyName = "Sun";
        private const string LandedSituation = "Landed";
        private const string SplashedSituation = "Splashed";
        private const string FlyingLowSituation = "FlyingLow";
        private const string FlyingHighSituation = "FlyingHigh";
        private const string LowSpaceSituation = "LowSpace";
        private const string HighSpaceSituation = "HighSpace";
        private const string KerbinLocationPrefix = "kerbin:";
        private const string KscLocationPrefix = "ksc:";
        private const string BodyLocationPrefix = "body:";
        private const string KerbinOnlyRange = "Kerbin only";
        private const string KerbinMoonsRange = "Kerbin, Mun and Minmus";
        private const string InterplanetaryRange = "Planets available";
        private const string UnavailableRange = "Unavailable";
        private const double KerbinDaySeconds = 21600.0;
        private const int CurrentScienceRequiredKerbals = 1;

        private static readonly Random SharedRandom = new Random();

        /// <summary>
        /// Returns the experiment IDs the rival can currently consider. Stock situation/body validity
        /// is still checked by KspScienceAdapter before it supplies subject candidates.
        /// </summary>
        internal static IList<string> GetAvailableExperimentIds(AgencyState rivalAgency)
        {
            var experimentIds = new List<string>();
            RivalProgramState programme;
            if (!TryGetProgramme(rivalAgency, out programme))
            {
                return experimentIds;
            }

            var uniqueExperimentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int techIndex = 0; techIndex < RivalTechCatalogue.All.Count; techIndex++)
            {
                RivalTechNodeDefinition tech = RivalTechCatalogue.All[techIndex];
                if (tech == null || !RivalTechCatalogue.ContainsTechId(programme.ResearchedTechIds, tech.Id))
                {
                    continue;
                }

                for (int experimentIndex = 0;
                    experimentIndex < tech.UnlockedExperimentIds.Count;
                    experimentIndex++)
                {
                    string experimentId = tech.UnlockedExperimentIds[experimentIndex];
                    if (!string.IsNullOrEmpty(experimentId) && uniqueExperimentIds.Add(experimentId))
                    {
                        experimentIds.Add(experimentId);
                    }
                }
            }

            if (RivalDevelopmentSimulation.GetFacilityLevel(
                    rivalAgency,
                    RivalFacilityType.AstronautComplex) >= 2
                && uniqueExperimentIds.Add(EvaReportExperimentId))
            {
                experimentIds.Add(EvaReportExperimentId);
            }

            if (RivalDevelopmentSimulation.GetFacilityLevel(
                    rivalAgency,
                    RivalFacilityType.ResearchAndDevelopment) >= 2
                && uniqueExperimentIds.Add(SurfaceSampleExperimentId))
            {
                experimentIds.Add(SurfaceSampleExperimentId);
            }

            experimentIds.Sort(StringComparer.OrdinalIgnoreCase);
            return experimentIds;
        }

        /// <summary>
        /// Returns the player-facing broad expedition range from the Tracking Station. Detailed
        /// situation and objective gates are enforced separately when a Science subject is evaluated.
        /// </summary>
        internal static string GetExpeditionRange(AgencyState rivalAgency)
        {
            int trackingStationLevel = RivalDevelopmentSimulation.GetTrackingStationLevel(rivalAgency);
            if (trackingStationLevel <= 0)
            {
                return UnavailableRange;
            }

            if (trackingStationLevel >= Math.Max(1, CampaignSettings.RivalTrackingStationInterplanetaryLevel))
            {
                return InterplanetaryRange;
            }

            if (trackingStationLevel >= Math.Max(1, CampaignSettings.RivalTrackingStationKerbinMoonsLevel))
            {
                return KerbinMoonsRange;
            }

            return KerbinOnlyRange;
        }

        /// <summary>
        /// Returns currently valid subject candidates after rival tech/facility/access rules, shared-pool
        /// availability, and same-rival duplicate rules are applied. Duplicate stock subject identities are
        /// reduced to one stable location so random selection is not biased by how many locations an adapter
        /// happened to enumerate for a non-biome subject.
        /// </summary>
        internal static IList<RivalScienceSubjectCandidate> GetEligibleCandidates(
            AgencyState rivalAgency,
            IList<RivalScienceSubjectCandidate> candidates)
        {
            return BuildEligibleCandidates(rivalAgency, candidates, false);
        }

        internal static bool IsCandidateEligible(
            AgencyState rivalAgency,
            RivalScienceSubjectCandidate candidate)
        {
            return IsCandidateEligible(rivalAgency, candidate, false);
        }

        internal static ScienceLaunchPreparationState TrySelectNextPreparation(
            AgencyState rivalAgency,
            double currentUniversalTime,
            IList<RivalScienceSubjectCandidate> candidates)
        {
            return TrySelectNextPreparation(
                rivalAgency,
                currentUniversalTime,
                candidates,
                SharedRandom);
        }

        internal static ScienceLaunchPreparationState TrySelectNextPreparation(
            AgencyState rivalAgency,
            double currentUniversalTime,
            IList<RivalScienceSubjectCandidate> candidates,
            Random random)
        {
            RivalProgramState programme;
            if (!TryGetProgramme(rivalAgency, out programme)
                || programme.ScienceLaunchPreparation != null
                || !IsFiniteNonNegative(currentUniversalTime))
            {
                return null;
            }

            IList<RivalScienceSubjectCandidate> eligibleCandidates =
                BuildEligibleCandidates(rivalAgency, candidates, false);
            if (eligibleCandidates.Count == 0)
            {
                return null;
            }

            Random selectionRandom = random ?? SharedRandom;
            RivalScienceSubjectCandidate selectedCandidate =
                eligibleCandidates[selectionRandom.Next(eligibleCandidates.Count)];
            double nextCheckUniversalTime = CalculateNextProgressCheckUniversalTime(currentUniversalTime);
            if (!IsFiniteNonNegative(nextCheckUniversalTime))
            {
                return null;
            }

            var preparation = new ScienceLaunchPreparationState
            {
                Subject = selectedCandidate.Subject,
                PlannedScienceReward = selectedCandidate.RemainingScience,
                LaunchProgressPercent = 0,
                NextProgressCheckUniversalTime = nextCheckUniversalTime,
                RequiredKerbals = CurrentScienceRequiredKerbals,
                ReadyUniversalTime = -1.0
            };
            programme.ScienceLaunchPreparation = preparation;
            return preparation;
        }

        /// <summary>
        /// Revalidates the current preparation against the latest shared Science pool. If the selected
        /// subject has been fully depleted or otherwise becomes invalid before launch, it is cancelled and
        /// a fresh valid subject is selected. A launched live mission is intentionally not affected here.
        /// </summary>
        internal static bool RefreshPreparationTarget(
            AgencyState rivalAgency,
            double currentUniversalTime,
            IList<RivalScienceSubjectCandidate> candidates)
        {
            return RefreshPreparationTarget(
                rivalAgency,
                currentUniversalTime,
                candidates,
                SharedRandom);
        }

        internal static bool RefreshPreparationTarget(
            AgencyState rivalAgency,
            double currentUniversalTime,
            IList<RivalScienceSubjectCandidate> candidates,
            Random random)
        {
            RivalProgramState programme;
            if (!TryGetProgramme(rivalAgency, out programme)
                || !IsFiniteNonNegative(currentUniversalTime))
            {
                return false;
            }

            ScienceLaunchPreparationState currentPreparation = programme.ScienceLaunchPreparation;
            if (currentPreparation == null)
            {
                return TrySelectNextPreparation(
                    rivalAgency,
                    currentUniversalTime,
                    candidates,
                    random) != null;
            }

            RivalScienceSubjectCandidate currentCandidate =
                FindCurrentPreparationCandidate(rivalAgency, candidates);
            if (currentCandidate != null)
            {
                NormalizePreparationTiming(currentPreparation, currentUniversalTime);
                return false;
            }

            // Preparation does not reserve the stock Science pool. A player or another rival may
            // therefore exhaust the subject before this rival launches it.
            programme.ScienceLaunchPreparation = null;
            TrySelectNextPreparation(rivalAgency, currentUniversalTime, candidates, random);
            return true;
        }

        /// <summary>
        /// Returns the canonical currently-valid candidate for the persisted preparation subject. The
        /// coordinator can pass this candidate's location to the live-mission engine when launch
        /// arbitration permits launch.
        /// </summary>
        internal static RivalScienceSubjectCandidate FindCurrentPreparationCandidate(
            AgencyState rivalAgency,
            IList<RivalScienceSubjectCandidate> candidates)
        {
            RivalProgramState programme;
            if (!TryGetProgramme(rivalAgency, out programme)
                || programme.ScienceLaunchPreparation == null
                || programme.ScienceLaunchPreparation.Subject == null
                || candidates == null)
            {
                return null;
            }

            ScienceSubjectKey preparationSubject = programme.ScienceLaunchPreparation.Subject;
            RivalScienceSubjectCandidate canonicalCandidate = null;

            // Fast timewarp can call this once for every elapsed Science progress day. Revalidating one
            // persisted subject must not rebuild, de-duplicate, and sort the full candidate catalogue.
            for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
            {
                RivalScienceSubjectCandidate candidate = candidates[candidateIndex];
                if (candidate == null
                    || candidate.Subject == null
                    || !preparationSubject.Equals(candidate.Subject)
                    || !IsCandidateEligible(rivalAgency, candidate, true))
                {
                    continue;
                }

                if (canonicalCandidate == null
                    || StringComparer.OrdinalIgnoreCase.Compare(
                        candidate.LocationId,
                        canonicalCandidate.LocationId) < 0)
                {
                    canonicalCandidate = candidate;
                }
            }

            return canonicalCandidate;
        }

        /// <summary>
        /// Processes at most one stored daily Science Launch Progress check. Catch-up ordering belongs to
        /// RivalSimulation, which calls this once for each chronological due event.
        /// </summary>
        internal static bool ProcessProgressCheck(
            AgencyState rivalAgency,
            double currentUniversalTime,
            IList<RivalScienceSubjectCandidate> candidates)
        {
            return ProcessProgressCheck(
                rivalAgency,
                currentUniversalTime,
                candidates,
                SharedRandom);
        }

        internal static bool ProcessProgressCheck(
            AgencyState rivalAgency,
            double currentUniversalTime,
            IList<RivalScienceSubjectCandidate> candidates,
            Random random)
        {
            RivalProgramState programme;
            if (!TryGetProgramme(rivalAgency, out programme)
                || !IsFiniteNonNegative(currentUniversalTime))
            {
                return false;
            }

            ScienceLaunchPreparationState beforeRefresh = programme.ScienceLaunchPreparation;
            bool targetChanged = RefreshPreparationTarget(
                rivalAgency,
                currentUniversalTime,
                candidates,
                random);
            ScienceLaunchPreparationState preparation = programme.ScienceLaunchPreparation;
            if (preparation == null || preparation.LaunchProgressPercent >= 100)
            {
                return targetChanged;
            }

            // A replacement selected at this UT starts a fresh daily cadence rather than receiving the
            // depleted target's already-due progress roll.
            if (targetChanged && !object.ReferenceEquals(beforeRefresh, preparation))
            {
                return true;
            }

            NormalizePreparationTiming(preparation, currentUniversalTime);
            double scheduledCheckUniversalTime = preparation.NextProgressCheckUniversalTime;
            if (!IsFiniteNonNegative(scheduledCheckUniversalTime)
                || scheduledCheckUniversalTime <= 0.0
                || currentUniversalTime < scheduledCheckUniversalTime)
            {
                return targetChanged;
            }

            double progressChance = RivalDevelopmentSimulation.GetScienceLaunchProgressChance(rivalAgency);
            int progressStepPercent = Math.Max(1, CampaignSettings.RivalScienceLaunchProgressStepPercent);
            Random progressRandom = random ?? SharedRandom;
            if (progressChance > 0.0 && progressRandom.NextDouble() < progressChance)
            {
                preparation.LaunchProgressPercent = Math.Min(
                    100,
                    Math.Max(0, preparation.LaunchProgressPercent) + progressStepPercent);
            }

            if (preparation.LaunchProgressPercent >= 100)
            {
                preparation.LaunchProgressPercent = 100;
                if (!IsFiniteNonNegative(preparation.ReadyUniversalTime))
                {
                    preparation.ReadyUniversalTime = scheduledCheckUniversalTime;
                }
                preparation.NextProgressCheckUniversalTime = 0.0;
                return true;
            }

            preparation.ReadyUniversalTime = -1.0;
            double intervalSeconds = GetProgressCheckIntervalSeconds();
            double nextCheckUniversalTime = scheduledCheckUniversalTime + intervalSeconds;
            preparation.NextProgressCheckUniversalTime = IsFiniteNonNegative(nextCheckUniversalTime)
                ? nextCheckUniversalTime
                : 0.0;
            return true;
        }

        /// <summary>
        /// Returns the average remaining preparation time in Kerbin days from the authoritative SPH +
        /// Runway daily chance. This is an expectation, not a guaranteed launch date.
        /// </summary>
        internal static double? CalculateEstimatedLaunchDays(AgencyState rivalAgency)
        {
            RivalProgramState programme;
            if (!TryGetProgramme(rivalAgency, out programme)
                || programme.ScienceLaunchPreparation == null)
            {
                return null;
            }

            ScienceLaunchPreparationState preparation = programme.ScienceLaunchPreparation;
            int currentProgressPercent = Math.Max(0, Math.Min(100, preparation.LaunchProgressPercent));
            if (currentProgressPercent >= 100)
            {
                return 0.0;
            }

            int progressStepPercent = Math.Max(1, CampaignSettings.RivalScienceLaunchProgressStepPercent);
            int remainingProgressPercent = 100 - currentProgressPercent;
            int remainingSuccessfulChecks =
                (remainingProgressPercent + progressStepPercent - 1) / progressStepPercent;
            double progressChance = RivalDevelopmentSimulation.GetScienceLaunchProgressChance(rivalAgency);
            double intervalDays = CampaignSettings.RivalScienceLaunchProgressCheckIntervalDays;
            if (!IsFinite(progressChance)
                || progressChance <= 0.0
                || progressChance > 1.0
                || !IsFinite(intervalDays)
                || intervalDays <= 0.0)
            {
                return null;
            }

            double estimatedDays = (remainingSuccessfulChecks * intervalDays) / progressChance;
            return IsFinite(estimatedDays) ? (double?)estimatedDays : null;
        }

        internal static bool IsPreparationReady(AgencyState rivalAgency)
        {
            RivalProgramState programme;
            return TryGetProgramme(rivalAgency, out programme)
                && programme.ScienceLaunchPreparation != null
                && programme.ScienceLaunchPreparation.Subject != null
                && programme.ScienceLaunchPreparation.LaunchProgressPercent >= 100
                && IsFiniteNonNegative(programme.ScienceLaunchPreparation.ReadyUniversalTime);
        }

        internal static void ClearPreparationAfterLaunch(AgencyState rivalAgency)
        {
            RivalProgramState programme;
            if (TryGetProgramme(rivalAgency, out programme))
            {
                programme.ScienceLaunchPreparation = null;
            }
        }

        private static IList<RivalScienceSubjectCandidate> BuildEligibleCandidates(
            AgencyState rivalAgency,
            IList<RivalScienceSubjectCandidate> candidates,
            bool allowCurrentPreparationSubject)
        {
            var eligibleCandidates = new List<RivalScienceSubjectCandidate>();
            if (candidates == null)
            {
                return eligibleCandidates;
            }

            var canonicalCandidates = new Dictionary<ScienceSubjectKey, RivalScienceSubjectCandidate>();
            for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
            {
                RivalScienceSubjectCandidate candidate = candidates[candidateIndex];
                if (!IsCandidateEligible(rivalAgency, candidate, allowCurrentPreparationSubject))
                {
                    continue;
                }

                RivalScienceSubjectCandidate existingCandidate;
                if (!canonicalCandidates.TryGetValue(candidate.Subject, out existingCandidate)
                    || StringComparer.OrdinalIgnoreCase.Compare(
                        candidate.LocationId,
                        existingCandidate.LocationId) < 0)
                {
                    canonicalCandidates[candidate.Subject] = candidate;
                }
            }

            foreach (RivalScienceSubjectCandidate candidate in canonicalCandidates.Values)
            {
                eligibleCandidates.Add(candidate);
            }

            eligibleCandidates.Sort(CompareCandidates);
            return eligibleCandidates;
        }

        private static bool IsCandidateEligible(
            AgencyState rivalAgency,
            RivalScienceSubjectCandidate candidate,
            bool allowCurrentPreparationSubject)
        {
            RivalProgramState programme;
            if (!TryGetProgramme(rivalAgency, out programme)
                || candidate == null
                || candidate.Subject == null
                || string.IsNullOrEmpty(candidate.Subject.ExperimentId)
                || string.IsNullOrEmpty(candidate.Subject.BodyName)
                || string.IsNullOrEmpty(candidate.Subject.Situation)
                || string.IsNullOrEmpty(candidate.LocationId)
                || !IsFinite(candidate.RemainingScience)
                || candidate.RemainingScience <= 0.0
                || CampaignSettings.GetRivalMissionLocationSettings(candidate.LocationId) == null
                || programme.CompletedScienceSubjects.Contains(candidate.Subject)
                || RivalLiveMissionSimulation.HasLiveScienceSubject(rivalAgency, candidate.Subject)
                || !IsExperimentAvailable(rivalAgency, candidate.Subject.ExperimentId)
                || !IsLocationCompatible(candidate.Subject, candidate.LocationId)
                || !IsSituationAccessible(rivalAgency, candidate.Subject))
            {
                return false;
            }

            return allowCurrentPreparationSubject
                || programme.ScienceLaunchPreparation == null
                || programme.ScienceLaunchPreparation.Subject == null
                || !programme.ScienceLaunchPreparation.Subject.Equals(candidate.Subject);
        }

        private static bool IsExperimentAvailable(AgencyState rivalAgency, string experimentId)
        {
            if (string.Equals(experimentId, EvaReportExperimentId, StringComparison.OrdinalIgnoreCase))
            {
                return RivalDevelopmentSimulation.GetFacilityLevel(
                    rivalAgency,
                    RivalFacilityType.AstronautComplex) >= 2;
            }

            if (string.Equals(experimentId, SurfaceSampleExperimentId, StringComparison.OrdinalIgnoreCase))
            {
                return RivalDevelopmentSimulation.GetFacilityLevel(
                    rivalAgency,
                    RivalFacilityType.ResearchAndDevelopment) >= 2;
            }

            RivalProgramState programme = rivalAgency == null ? null : rivalAgency.RivalProgram;
            return programme != null
                && RivalTechCatalogue.IsExperimentUnlocked(
                    experimentId,
                    programme.ResearchedTechIds);
        }

        private static bool IsSituationAccessible(
            AgencyState rivalAgency,
            ScienceSubjectKey subject)
        {
            if (rivalAgency == null
                || subject == null
                || string.IsNullOrEmpty(subject.BodyName)
                || string.Equals(subject.BodyName, SunBodyName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int requiredTrackingLevel = GetRequiredTrackingStationLevel(subject.BodyName);
            if (requiredTrackingLevel <= 0
                || RivalDevelopmentSimulation.GetTrackingStationLevel(rivalAgency) < requiredTrackingLevel)
            {
                return false;
            }

            string normalizedSituation = NormalizeSituation(subject.Situation);
            if (string.IsNullOrEmpty(normalizedSituation))
            {
                return false;
            }

            bool isSurfaceOrFlight = string.Equals(normalizedSituation, LandedSituation, StringComparison.Ordinal)
                || string.Equals(normalizedSituation, SplashedSituation, StringComparison.Ordinal)
                || string.Equals(normalizedSituation, FlyingLowSituation, StringComparison.Ordinal)
                || string.Equals(normalizedSituation, FlyingHighSituation, StringComparison.Ordinal);
            bool isSpace = string.Equals(normalizedSituation, LowSpaceSituation, StringComparison.Ordinal)
                || string.Equals(normalizedSituation, HighSpaceSituation, StringComparison.Ordinal);

            if (string.Equals(subject.BodyName, KerbinBodyName, StringComparison.OrdinalIgnoreCase))
            {
                if (isSurfaceOrFlight)
                {
                    return true;
                }

                return isSpace && HasProbeOrbitCompletion(rivalAgency, KerbinBodyName);
            }

            if (isSpace)
            {
                return HasProbeOrbitCompletion(rivalAgency, subject.BodyName);
            }

            if (isSurfaceOrFlight)
            {
                // Current v0.6 content has no non-Kerbin landing Contract yet. This intentionally stays
                // closed until those Contract definitions provide an explicit campaign progression gate.
                return false;
            }

            return false;
        }

        private static int GetRequiredTrackingStationLevel(string bodyName)
        {
            if (string.IsNullOrEmpty(bodyName)
                || string.Equals(bodyName, SunBodyName, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (string.Equals(bodyName, KerbinBodyName, StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(1, CampaignSettings.RivalTrackingStationKerbinLevel);
            }

            if (string.Equals(bodyName, MunBodyName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(bodyName, MinmusBodyName, StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(1, CampaignSettings.RivalTrackingStationKerbinMoonsLevel);
            }

            return CampaignSettings.GetRivalMissionLocationSettings(BodyLocationPrefix + bodyName) != null
                ? Math.Max(1, CampaignSettings.RivalTrackingStationInterplanetaryLevel)
                : 0;
        }

        private static bool HasProbeOrbitCompletion(AgencyState rivalAgency, string bodyName)
        {
            if (rivalAgency == null || string.IsNullOrEmpty(bodyName))
            {
                return false;
            }

            for (int objectiveIndex = 0; objectiveIndex < ObjectiveCatalogue.All.Count; objectiveIndex++)
            {
                ObjectiveDefinition objective = ObjectiveCatalogue.All[objectiveIndex];
                if (objective != null
                    && objective.ObjectiveType == ObjectiveType.Orbit
                    && objective.CrewRequirement == ObjectiveCrewRequirement.UncrewedProbe
                    && string.Equals(
                        objective.CelestialBodyName,
                        bodyName,
                        StringComparison.OrdinalIgnoreCase)
                    && rivalAgency.HasCompletedObjective(objective.Id))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLocationCompatible(ScienceSubjectKey subject, string locationId)
        {
            if (subject == null || string.IsNullOrEmpty(locationId))
            {
                return false;
            }

            string normalizedSituation = NormalizeSituation(subject.Situation);
            if (string.Equals(subject.BodyName, KerbinBodyName, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(normalizedSituation, LowSpaceSituation, StringComparison.Ordinal)
                    || string.Equals(normalizedSituation, HighSpaceSituation, StringComparison.Ordinal))
                {
                    return string.Equals(
                        locationId,
                        CampaignSettings.RivalKerbinOrbitLocationId,
                        StringComparison.OrdinalIgnoreCase);
                }

                if (string.Equals(
                        locationId,
                        CampaignSettings.RivalKerbinOrbitLocationId,
                        StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        locationId,
                        CampaignSettings.RivalPreOrbitLocalLocationId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return locationId.StartsWith(KerbinLocationPrefix, StringComparison.OrdinalIgnoreCase)
                    || locationId.StartsWith(KscLocationPrefix, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(
                locationId,
                BodyLocationPrefix + subject.BodyName,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeSituation(string situation)
        {
            if (string.IsNullOrEmpty(situation))
            {
                return null;
            }

            string normalized = situation
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty);

            if (string.Equals(normalized, "Landed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "SrfLanded", StringComparison.OrdinalIgnoreCase))
            {
                return LandedSituation;
            }
            if (string.Equals(normalized, "Splashed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "SrfSplashed", StringComparison.OrdinalIgnoreCase))
            {
                return SplashedSituation;
            }
            if (string.Equals(normalized, FlyingLowSituation, StringComparison.OrdinalIgnoreCase))
            {
                return FlyingLowSituation;
            }
            if (string.Equals(normalized, FlyingHighSituation, StringComparison.OrdinalIgnoreCase))
            {
                return FlyingHighSituation;
            }
            if (string.Equals(normalized, LowSpaceSituation, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "InSpaceLow", StringComparison.OrdinalIgnoreCase))
            {
                return LowSpaceSituation;
            }
            if (string.Equals(normalized, HighSpaceSituation, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "InSpaceHigh", StringComparison.OrdinalIgnoreCase))
            {
                return HighSpaceSituation;
            }

            return null;
        }

        private static void NormalizePreparationTiming(
            ScienceLaunchPreparationState preparation,
            double currentUniversalTime)
        {
            if (preparation == null)
            {
                return;
            }

            preparation.LaunchProgressPercent = Math.Max(
                0,
                Math.Min(100, preparation.LaunchProgressPercent));
            preparation.RequiredKerbals = Math.Max(1, preparation.RequiredKerbals);

            if (preparation.LaunchProgressPercent >= 100)
            {
                if (!IsFiniteNonNegative(preparation.ReadyUniversalTime))
                {
                    preparation.ReadyUniversalTime = currentUniversalTime;
                }
                preparation.NextProgressCheckUniversalTime = 0.0;
                return;
            }

            preparation.ReadyUniversalTime = -1.0;
            if (!IsFiniteNonNegative(preparation.NextProgressCheckUniversalTime)
                || preparation.NextProgressCheckUniversalTime <= 0.0)
            {
                preparation.NextProgressCheckUniversalTime =
                    CalculateNextProgressCheckUniversalTime(currentUniversalTime);
            }
        }

        private static double CalculateNextProgressCheckUniversalTime(double currentUniversalTime)
        {
            double intervalSeconds = GetProgressCheckIntervalSeconds();
            if (!IsFiniteNonNegative(currentUniversalTime) || intervalSeconds <= 0.0)
            {
                return -1.0;
            }

            double nextCheckUniversalTime =
                (Math.Floor(currentUniversalTime / intervalSeconds) + 1.0) * intervalSeconds;
            return IsFiniteNonNegative(nextCheckUniversalTime)
                ? nextCheckUniversalTime
                : -1.0;
        }

        private static double GetProgressCheckIntervalSeconds()
        {
            double intervalDays = CampaignSettings.RivalScienceLaunchProgressCheckIntervalDays;
            if (!IsFinite(intervalDays) || intervalDays <= 0.0)
            {
                return 0.0;
            }

            double intervalSeconds = intervalDays * KerbinDaySeconds;
            return IsFinite(intervalSeconds) && intervalSeconds > 0.0
                ? intervalSeconds
                : 0.0;
        }

        private static int CompareCandidates(
            RivalScienceSubjectCandidate first,
            RivalScienceSubjectCandidate second)
        {
            if (object.ReferenceEquals(first, second))
            {
                return 0;
            }
            if (first == null)
            {
                return -1;
            }
            if (second == null)
            {
                return 1;
            }

            int comparison = CompareScienceSubjects(first.Subject, second.Subject);
            return comparison != 0
                ? comparison
                : StringComparer.OrdinalIgnoreCase.Compare(first.LocationId, second.LocationId);
        }

        private static int CompareScienceSubjects(ScienceSubjectKey first, ScienceSubjectKey second)
        {
            if (object.ReferenceEquals(first, second))
            {
                return 0;
            }
            if (first == null)
            {
                return -1;
            }
            if (second == null)
            {
                return 1;
            }

            int comparison = StringComparer.OrdinalIgnoreCase.Compare(first.ExperimentId, second.ExperimentId);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.OrdinalIgnoreCase.Compare(first.BodyName, second.BodyName);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.OrdinalIgnoreCase.Compare(first.Situation, second.Situation);
            return comparison != 0
                ? comparison
                : StringComparer.OrdinalIgnoreCase.Compare(first.BiomeName, second.BiomeName);
        }

        private static bool TryGetProgramme(AgencyState rivalAgency, out RivalProgramState programme)
        {
            programme = rivalAgency == null ? null : rivalAgency.RivalProgram;
            return rivalAgency != null && !rivalAgency.IsPlayer && programme != null;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return IsFinite(value) && value >= 0.0;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}

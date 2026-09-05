using System;
using System.Collections.Generic;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Rivals;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.Campaign
{
    /// <summary>
    /// Coordinates the current race prototype without owning the code-defined funding catalogue.
    /// </summary>
    public sealed class CampaignController
    {
        public const string PlayerAgencyId = "player";
        public const string AsterAgencyId = "aster";
        public const string CobaltAgencyId = "cobalt";

        private const double KerbinDaySeconds = 21600.0;
        private const int KerbinDaysPerYear = 426;
        private const double RivalBaseIncomeFunds = 20000.0;
        private const int MaximumUncompletedNormalAchievementOffers = 2;
        private const int MaximumUnfulfilledSatelliteOffers = 2;

        private static readonly Random OfferRandomGenerator = new Random();

        private readonly List<AgencyState> _agencies = new List<AgencyState>();
        private readonly List<AgencyState> _rivalAgencies = new List<AgencyState>();
        private readonly List<SatelliteNetworkFundingContract> _satelliteNetworkFundingContracts = new List<SatelliteNetworkFundingContract>();
        private readonly List<ObjectiveFundingContract> _achievementSatelliteNetworkFundingContracts =
            new List<ObjectiveFundingContract>();
        private readonly IList<AgencyState> _agenciesView;
        private readonly IList<AgencyState> _rivalAgenciesView;
        private readonly IList<SatelliteNetworkFundingContract> _satelliteNetworkFundingContractsView;
        private readonly IList<ObjectiveFundingContract> _achievementSatelliteNetworkFundingContractsView;
        private IList<ObjectiveDefinition> _activeFlightContracts =
            new List<ObjectiveDefinition>().AsReadOnly();
        private readonly double[,] _satellitePayoutCache;
        private readonly double[,] _achievementPayoutCache;
        private readonly double _fundingIntervalSeconds;
        private double _nextFundingUniversalTime = -1.0;
        private bool _hasFundingPayoutCache;
        private bool _hasRestoredPersistentState;
        private bool _isActiveFlightContractPlanDirty = true;

        public CampaignController()
        {
            _fundingIntervalSeconds = Math.Max(1.0, CampaignSettings.FundingIntervalDays) * KerbinDaySeconds;
            PlayerAgency = new AgencyState(PlayerAgencyId, "Kerbal Space Agency", true);
            _agencies.Add(PlayerAgency);

            // Rivals begin without a fixed target. Their first refresh chooses from the four
            // offered starter lines rather than assuming Probe Orbit is available at campaign start.
            int configuredRivalCount = Math.Max(0, CampaignSettings.NumberOfRivals);
            for (int rivalIndex = 0; rivalIndex < configuredRivalCount; rivalIndex++)
            {
                string rivalId;
                string rivalName;
                if (rivalIndex == 0)
                {
                    rivalId = AsterAgencyId;
                    rivalName = "Aster Aerospace Directorate";
                }
                else if (rivalIndex == 1)
                {
                    rivalId = CobaltAgencyId;
                    rivalName = "Cobalt Orbital Bureau";
                }
                else
                {
                    int rivalNumber = rivalIndex + 1;
                    rivalId = "rival-" + rivalNumber;
                    rivalName = "Rival Agency " + rivalNumber;
                }

                var rivalProgram = new AgencyState(rivalId, rivalName, false);
                rivalProgram.Funds = Math.Max(0.0, CampaignSettings.RivalStartingFunds);

                _agencies.Add(rivalProgram);
                _rivalAgencies.Add(rivalProgram);
            }

            // Target content remains code-defined, while the catalogue reads user-configured
            // reward and network balance values before constructing fresh campaign state.
            IList<ObjectiveFundingContract> achievementProgrammes =
                FundingContractCatalogue.CreateAchievementProgrammes();
            for (int programmeIndex = 0; programmeIndex < achievementProgrammes.Count; programmeIndex++)
            {
                _achievementSatelliteNetworkFundingContracts.Add(achievementProgrammes[programmeIndex]);
            }

            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts =
                FundingContractCatalogue.CreateSatelliteProgrammes();
            for (int programmeIndex = 0; programmeIndex < satelliteNetworkFundingContracts.Count; programmeIndex++)
            {
                _satelliteNetworkFundingContracts.Add(satelliteNetworkFundingContracts[programmeIndex]);
            }

            // Programme and agency collections are fixed for one controller lifetime. Cache arrays
            // therefore need no recurring allocation and can be rebuilt in place after each refresh.
            _satellitePayoutCache = new double[_agencies.Count, _satelliteNetworkFundingContracts.Count];
            _achievementPayoutCache = new double[_agencies.Count, _achievementSatelliteNetworkFundingContracts.Count];

            _agenciesView = _agencies.AsReadOnly();
            _rivalAgenciesView = _rivalAgencies.AsReadOnly();
            _satelliteNetworkFundingContractsView = _satelliteNetworkFundingContracts.AsReadOnly();
            _achievementSatelliteNetworkFundingContractsView = _achievementSatelliteNetworkFundingContracts.AsReadOnly();
        }

        public AgencyState PlayerAgency { get; private set; }
        public IList<AgencyState> Agencies { get { return _agenciesView; } }
        public IList<AgencyState> RivalAgencies { get { return _rivalAgenciesView; } }
        public IList<SatelliteNetworkFundingContract> SatelliteNetworkFundingContracts { get { return _satelliteNetworkFundingContractsView; } }
        public IList<ObjectiveFundingContract> ObjectiveFundingContracts
        {
            get { return _achievementSatelliteNetworkFundingContractsView; }
        }

        /// <summary>
        /// Cached Offered, unexpired pre-orbit contracts the player has not personally completed.
        /// The controller replaces this read-only collection only after relevant contract state changes.
        /// </summary>
        internal IList<ObjectiveDefinition> ActiveFlightContracts
        {
            get { return _activeFlightContracts; }
        }

        /// <summary>
        /// Marks the cached active starter set dirty after the flight tracker records a player
        /// starter objectiveCompletion. The following controller refresh rebuilds the set once after all
        /// resulting unlock/offer/funding changes have been processed.
        /// </summary>
        internal void NotifyPlayerPreOrbitAchievementRecorded()
        {
            _isActiveFlightContractPlanDirty = true;
        }

        /// <summary>
        /// Returns the agency with the supplied stable ID, or null when no current agency matches.
        /// </summary>
        public AgencyState FindAgencyById(string programId)
        {
            if (string.IsNullOrEmpty(programId))
            {
                return null;
            }

            for (int agencyIndex = 0; agencyIndex < _agencies.Count; agencyIndex++)
            {
                AgencyState agency = _agencies[agencyIndex];
                if (agency != null
                    && string.Equals(agency.Id, programId, StringComparison.OrdinalIgnoreCase))
                {
                    return agency;
                }
            }

            return null;
        }

        /// <summary>
        /// Guaranteed funds each rival receives on every shared funding date.
        /// </summary>
        public double RivalBaseIncomePerFundingPeriod { get { return RivalBaseIncomeFunds; } }

        public double NextFundingUniversalTime { get { return _nextFundingUniversalTime; } }

        public int NextFundingYear
        {
            get { return GetKerbinYear(_nextFundingUniversalTime); }
        }

        /// <summary>
        /// Whole Kerbin days remaining until the shared funding date, rounded upward so a
        /// partial final day remains visible until the funding boundary is actually reached.
        /// </summary>
        public int DaysUntilNextFunding
        {
            get
            {
                if (_nextFundingUniversalTime < 0.0 || Planetarium.fetch == null)
                {
                    return 0;
                }

                double remainingSeconds = Math.Max(
                    0.0,
                    _nextFundingUniversalTime - Planetarium.GetUniversalTime());
                return (int)Math.Ceiling(remainingSeconds / KerbinDaySeconds);
            }
        }

        public int GetKerbinYear(double universalTime)
        {
            if (universalTime < 0.0)
            {
                return 0;
            }

            int totalKerbinDays = (int)Math.Floor(universalTime / KerbinDaySeconds);
            return (totalKerbinDays / KerbinDaysPerYear) + 1;
        }

        public int GetKerbinDay(double universalTime)
        {
            if (universalTime < 0.0)
            {
                return 0;
            }

            int totalKerbinDays = (int)Math.Floor(universalTime / KerbinDaySeconds);
            return (totalKerbinDays % KerbinDaysPerYear) + 1;
        }

        /// <summary>
        /// Returns the funds required for the rival's next successful mission-progress step.
        /// </summary>
        public double GetRivalMissionProgressCost(AgencyState agency)
        {
            if (agency == null || agency.IsPlayer)
            {
                return 0.0;
            }

            return RivalSimulation.CalculateLaunchProgressCost(
                agency,
                _achievementSatelliteNetworkFundingContracts,
                _satelliteNetworkFundingContracts);
        }

        /// <summary>
        /// Returns the current expected Kerbin days until a rival mission completes, using the
        /// same configured funding date shown in the command center for projected income.
        /// </summary>
        public int? GetEstimatedRivalMissionDays(AgencyState agency)
        {
            if (agency == null || agency.IsPlayer || Planetarium.fetch == null)
            {
                return null;
            }

            return RivalSimulation.CalculateEstimatedLaunchDays(
                agency,
                Planetarium.GetUniversalTime(),
                _nextFundingUniversalTime,
                _fundingIntervalSeconds,
                _achievementSatelliteNetworkFundingContracts,
                _satelliteNetworkFundingContracts);
        }

        public bool HasProgramAchieved(
            AgencyState agency,
            ObjectiveFundingContract achievementProgramme)
        {
            return agency != null
                && achievementProgramme != null
                && agency.HasCompletedObjective(achievementProgramme.Id);
        }

        /// <summary>
        /// Returns whether an objectiveCompletion funding target is unlocked at the current campaign time.
        /// Unlock state is intentionally separate from whether sponsors have offered the contract.
        /// </summary>
        public bool IsAchievementProgrammeAvailable(ObjectiveFundingContract achievementProgramme)
        {
            if (achievementProgramme == null)
            {
                return false;
            }

            double evaluationUniversalTime = Planetarium.fetch == null
                ? 0.0
                : Planetarium.GetUniversalTime();
            return IsAchievementProgrammeAvailableAtTime(
                achievementProgramme,
                evaluationUniversalTime);
        }

        /// <summary>
        /// Returns this agency's projected share of one satellite contract. After the first
        /// completed controller refresh, the value comes from that refresh's payout snapshot so
        /// repeated UI queries do not re-scan every agency's satellite count.
        /// </summary>
        public double GetSatelliteCurrentPayout(
            AgencyState agency,
            SatelliteNetworkFundingContract fundingProgramme)
        {
            if (agency == null
                || fundingProgramme == null
                || !fundingProgramme.IsAvailable
                || !fundingProgramme.IsOffered)
            {
                return 0.0;
            }

            if (_hasFundingPayoutCache)
            {
                int agencyIndex = _agencies.IndexOf(agency);
                int programmeIndex = _satelliteNetworkFundingContracts.IndexOf(fundingProgramme);
                if (agencyIndex >= 0 && programmeIndex >= 0)
                {
                    return _satellitePayoutCache[agencyIndex, programmeIndex];
                }
            }

            return CalculateSatelliteCurrentPayout(agency, fundingProgramme);
        }

        /// <summary>
        /// Returns this agency's projected share of the objectiveCompletion contract on the next global
        /// funding date. After a completed refresh the value is served from the same payout snapshot
        /// used to build NextPayoutFunds, avoiding repeated agency-count calculations in the UI.
        /// </summary>
        public double GetAchievementCurrentPayout(
            AgencyState agency,
            ObjectiveFundingContract achievementProgramme)
        {
            if (agency == null || achievementProgramme == null || !achievementProgramme.IsOffered)
            {
                return 0.0;
            }

            if (_hasFundingPayoutCache)
            {
                int agencyIndex = _agencies.IndexOf(agency);
                int programmeIndex = _achievementSatelliteNetworkFundingContracts.IndexOf(achievementProgramme);
                if (agencyIndex >= 0 && programmeIndex >= 0)
                {
                    return _achievementPayoutCache[agencyIndex, programmeIndex];
                }
            }

            return CalculateAchievementCurrentPayout(agency, achievementProgramme);
        }

        public void Refresh()
        {
            Refresh(true);
        }

        internal bool Refresh(bool refreshPlayerVessels)
        {
            if (Planetarium.fetch == null)
            {
                return false;
            }

            double currentUniversalTime = Planetarium.GetUniversalTime();

            // Do not advance gameplay until the ScenarioModule has loaded the current save.
            // A new game has no saved Race for Space data, so the constructor defaults remain in use.
            if (!_hasRestoredPersistentState)
            {
                bool restoredRivals = ModPersistenceScenario.TryRestoreRivalAgencyState(_rivalAgencies);
                double restoredNextFundingUniversalTime;
                bool restoredRaceProgress = ModPersistenceScenario.TryRestoreRaceProgress(
                    PlayerAgency,
                    _satelliteNetworkFundingContracts,
                    _achievementSatelliteNetworkFundingContracts,
                    out restoredNextFundingUniversalTime);

                if (!restoredRivals || !restoredRaceProgress)
                {
                    return false;
                }

                if (restoredNextFundingUniversalTime >= 0.0)
                {
                    _nextFundingUniversalTime = restoredNextFundingUniversalTime;
                }

                _hasRestoredPersistentState = true;
            }

            if (_nextFundingUniversalTime < 0.0)
            {
                _nextFundingUniversalTime =
                    (Math.Floor(currentUniversalTime / _fundingIntervalSeconds) + 1.0)
                    * _fundingIntervalSeconds;
            }

            bool hasDueFunding = currentUniversalTime >= _nextFundingUniversalTime;

            if (hasDueFunding)
            {
                ProcessDueFunding(currentUniversalTime);
                RefreshRivals(currentUniversalTime);
            }

            double stateEvaluationUniversalTime = currentUniversalTime;
            bool didRefreshPlayerVessels = false;
            if (refreshPlayerVessels)
            {
                IList<OrbitingVesselSnapshot> vesselSnapshots;
                double vesselObservationUniversalTime;
                if (KspVesselMonitor.TryCaptureOrbitingVesselSnapshots(
                    out vesselSnapshots,
                    out vesselObservationUniversalTime))
                {
                    didRefreshPlayerVessels = true;
                    stateEvaluationUniversalTime = Math.Max(
                        stateEvaluationUniversalTime,
                        vesselObservationUniversalTime);
                    OrbitalVesselTracker.RefreshOrbitalProgress(
                        PlayerAgency,
                        _agencies,
                        ObjectiveCatalogue.All,
                        vesselSnapshots,
                        vesselObservationUniversalTime);
                }
            }

            UpdateFundingAvailability(stateEvaluationUniversalTime);

            if (!hasDueFunding)
            {
                RefreshRivals(stateEvaluationUniversalTime);
                UpdateFundingAvailability(stateEvaluationUniversalTime);
            }

            // Probe Orbit remains an immediate shared-race unlock once any starter line reaches
            // Level V. PreOrbit Levels II-V wait for the funding-day review, where every unlocked
            // pre-orbit contract is offered without consuming normal one-off objectiveCompletion slots.
            UpdateSpecialAchievementOffers(stateEvaluationUniversalTime);
            UpdateSatelliteTargetReachedState();
            StartAchievementContracts(stateEvaluationUniversalTime);
            RebuildActiveFlightContractPlanIfNeeded();
            EvaluateSatelliteNetworkFundingContracts();

            ModPersistenceScenario.CaptureRivalAgencyState(_rivalAgencies);
            ModPersistenceScenario.CaptureRaceProgress(
                PlayerAgency,
                _satelliteNetworkFundingContracts,
                _achievementSatelliteNetworkFundingContracts,
                _nextFundingUniversalTime);

            return didRefreshPlayerVessels;
        }

        private void RefreshRivals(double currentUniversalTime)
        {
            RivalSimulation.Refresh(
                _agencies,
                currentUniversalTime,
                _achievementSatelliteNetworkFundingContracts,
                _satelliteNetworkFundingContracts);
        }

        private void UpdateFundingAvailability(double evaluationUniversalTime)
        {
            for (int programmeIndex = 0; programmeIndex < _satelliteNetworkFundingContracts.Count; programmeIndex++)
            {
                SatelliteNetworkFundingContract programme = _satelliteNetworkFundingContracts[programmeIndex];
                if (programme.IsAvailable)
                {
                    continue;
                }

                if (UnlockRuleEvaluator.IsSatisfied(
                    programme.UnlockRule,
                    _agencies,
                    evaluationUniversalTime))
                {
                    programme.Unlock();
                }
            }
        }

        /// <summary>
        /// Offers Probe Orbit immediately when any agency completes a Level V pre-orbit contract.
        /// PreOrbit Levels II-V otherwise wait in Unlocked until the funding-day sponsor review,
        /// where every unlocked pre-orbit contract is offered independently of the normal offer cap.
        /// </summary>
        private void UpdateSpecialAchievementOffers(double evaluationUniversalTime)
        {
            for (int programmeIndex = 0;
                programmeIndex < _achievementSatelliteNetworkFundingContracts.Count;
                programmeIndex++)
            {
                ObjectiveFundingContract programme = _achievementSatelliteNetworkFundingContracts[programmeIndex];
                if (programme == null || programme.IsOffered || programme.IsExpired)
                {
                    continue;
                }

                if (!string.Equals(
                    programme.Id,
                    ObjectiveCatalogue.ProbeOrbitId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (IsAchievementProgrammeAvailableAtTime(programme, evaluationUniversalTime))
                {
                    programme.Offer();
                }
            }
        }

        private void UpdateSatelliteTargetReachedState()
        {
            for (int programmeIndex = 0; programmeIndex < _satelliteNetworkFundingContracts.Count; programmeIndex++)
            {
                SatelliteNetworkFundingContract programme = _satelliteNetworkFundingContracts[programmeIndex];
                if (programme == null
                    || programme.HasReachedSatelliteTarget
                    || programme.RequiredSatellites <= 0)
                {
                    continue;
                }

                if (GetCollectiveSatelliteCount(programme.CelestialBodyName) >= programme.RequiredSatellites)
                {
                    programme.MarkSatelliteTargetReached();
                }
            }
        }

        private void StartAchievementContracts(double evaluationUniversalTime)
        {
            for (int programmeIndex = 0; programmeIndex < _achievementSatelliteNetworkFundingContracts.Count; programmeIndex++)
            {
                ObjectiveFundingContract programme = _achievementSatelliteNetworkFundingContracts[programmeIndex];
                if (programme.IsOffered
                    && !programme.HasStarted
                    && !programme.IsExpired
                    && GetAchievementAgencyCountAtTime(programme.Id, evaluationUniversalTime) > 0)
                {
                    programme.Start();
                }
            }
        }

        /// <summary>
        /// Processes every crossed global funding boundary. Rivals are advanced only to each
        /// boundary before that boundary pays, so they cannot spend funding before receiving it.
        /// Existing offers pay first; the sponsor review then fills vacancies for the next period.
        /// </summary>
        private void ProcessDueFunding(double currentUniversalTime)
        {
            while (_nextFundingUniversalTime >= 0.0 && currentUniversalTime >= _nextFundingUniversalTime)
            {
                double payoutUniversalTime = _nextFundingUniversalTime;

                RefreshRivals(payoutUniversalTime);
                UpdateFundingAvailability(payoutUniversalTime);
                UpdateSpecialAchievementOffers(payoutUniversalTime);
                UpdateSatelliteTargetReachedState();
                StartAchievementContracts(payoutUniversalTime);

                for (int agencyIndex = 0; agencyIndex < _agencies.Count; agencyIndex++)
                {
                    AgencyState agency = _agencies[agencyIndex];
                    double payout = CalculateSatelliteFundingForProgram(agency);

                    if (!agency.IsPlayer)
                    {
                        payout += RivalBaseIncomeFunds;
                    }

                    AwardProgramFunds(agency, payout);
                }

                for (int programmeIndex = 0; programmeIndex < _achievementSatelliteNetworkFundingContracts.Count; programmeIndex++)
                {
                    ObjectiveFundingContract programme = _achievementSatelliteNetworkFundingContracts[programmeIndex];
                    if (!programme.IsOffered || !programme.HasStarted || programme.IsExpired)
                    {
                        continue;
                    }

                    int eligibleAgencyCount = GetAchievementAgencyCountAtTime(
                        programme.Id,
                        payoutUniversalTime);
                    if (eligibleAgencyCount <= 0)
                    {
                        continue;
                    }

                    for (int agencyIndex = 0; agencyIndex < _agencies.Count; agencyIndex++)
                    {
                        AgencyState agency = _agencies[agencyIndex];
                        bool isEligible = HasProgramAchievedByTime(
                            agency,
                            programme.Id,
                            payoutUniversalTime);
                        double payout = programme.CalculateCurrentPayout(isEligible, eligibleAgencyCount);
                        AwardProgramFunds(agency, payout);
                    }

                    programme.AdvancePayout();
                    if (programme.IsExpired)
                    {
                        ObjectiveDefinition expiredMilestone = ObjectiveCatalogue.FindById(programme.Id);
                        if (expiredMilestone != null && expiredMilestone.IsPreOrbitContract)
                        {
                            _isActiveFlightContractPlanDirty = true;
                        }
                    }
                }

                ReviewFundingOffers(payoutUniversalTime);
                _nextFundingUniversalTime += _fundingIntervalSeconds;
            }
        }

        private void ReviewFundingOffers(double evaluationUniversalTime)
        {
            var normalAchievementCandidates = new List<ObjectiveFundingContract>();
            int uncompletedNormalAchievementOfferCount = 0;

            for (int programmeIndex = 0;
                programmeIndex < _achievementSatelliteNetworkFundingContracts.Count;
                programmeIndex++)
            {
                ObjectiveFundingContract programme = _achievementSatelliteNetworkFundingContracts[programmeIndex];
                if (programme == null || programme.IsExpired)
                {
                    continue;
                }

                ObjectiveDefinition objective = ObjectiveCatalogue.FindById(programme.Id);
                bool isPreOrbitContract = objective != null && objective.IsPreOrbitContract;

                if (programme.IsOffered)
                {
                    if (!programme.HasStarted && !isPreOrbitContract)
                    {
                        uncompletedNormalAchievementOfferCount++;
                    }
                    continue;
                }

                if (!IsAchievementProgrammeAvailableAtTime(programme, evaluationUniversalTime))
                {
                    continue;
                }

                // PreOrbit contracts are the four parallel pre-orbit development lines. Every
                // unlocked starter target is offered at the funding review, and starter offers do
                // not consume the two unfinished slots reserved for Probe Orbit and later targets.
                if (isPreOrbitContract)
                {
                    programme.Offer();
                    _isActiveFlightContractPlanDirty = true;
                    continue;
                }

                normalAchievementCandidates.Add(programme);
            }

            int achievementVacancies = Math.Max(
                0,
                MaximumUncompletedNormalAchievementOffers - uncompletedNormalAchievementOfferCount);
            for (int offerIndex = 0;
                offerIndex < achievementVacancies && normalAchievementCandidates.Count > 0;
                offerIndex++)
            {
                int candidateIndex = OfferRandomGenerator.Next(normalAchievementCandidates.Count);
                normalAchievementCandidates[candidateIndex].Offer();
                normalAchievementCandidates.RemoveAt(candidateIndex);
            }

            var satelliteCandidates = new List<SatelliteNetworkFundingContract>();
            int unfulfilledSatelliteOfferCount = 0;

            for (int programmeIndex = 0; programmeIndex < _satelliteNetworkFundingContracts.Count; programmeIndex++)
            {
                SatelliteNetworkFundingContract programme = _satelliteNetworkFundingContracts[programmeIndex];
                if (programme == null)
                {
                    continue;
                }

                if (programme.IsOffered)
                {
                    if (!programme.HasReachedSatelliteTarget)
                    {
                        unfulfilledSatelliteOfferCount++;
                    }
                    continue;
                }

                if (programme.IsAvailable)
                {
                    satelliteCandidates.Add(programme);
                }
            }

            int satelliteVacancies = Math.Max(
                0,
                MaximumUnfulfilledSatelliteOffers - unfulfilledSatelliteOfferCount);
            for (int offerIndex = 0;
                offerIndex < satelliteVacancies && satelliteCandidates.Count > 0;
                offerIndex++)
            {
                int candidateIndex = OfferRandomGenerator.Next(satelliteCandidates.Count);
                satelliteCandidates[candidateIndex].Offer();
                satelliteCandidates.RemoveAt(candidateIndex);
            }
        }

        private void RebuildActiveFlightContractPlanIfNeeded()
        {
            if (!_isActiveFlightContractPlanDirty)
            {
                return;
            }

            var activeFlightContracts = new List<ObjectiveDefinition>();
            for (int programmeIndex = 0;
                programmeIndex < _achievementSatelliteNetworkFundingContracts.Count;
                programmeIndex++)
            {
                ObjectiveFundingContract programme = _achievementSatelliteNetworkFundingContracts[programmeIndex];
                if (programme == null
                    || !programme.IsOffered
                    || programme.IsExpired
                    || PlayerAgency.HasCompletedObjective(programme.Id))
                {
                    continue;
                }

                ObjectiveDefinition objective = ObjectiveCatalogue.FindById(programme.Id);
                if (objective != null && objective.IsPreOrbitContract)
                {
                    activeFlightContracts.Add(objective);
                }
            }

            // Replacing the read-only list rather than mutating it lets frequent readers keep a
            // stable snapshot until a real offer/completion/expiry transition invalidates the plan.
            _activeFlightContracts = activeFlightContracts.AsReadOnly();
            _isActiveFlightContractPlanDirty = false;
        }

        private void AwardProgramFunds(AgencyState agency, double payout)
        {
            if (agency == null || payout <= 0.0)
            {
                return;
            }

            if (agency.IsPlayer)
            {
                CareerFundingAdapter.TryAddFunds(payout);
                return;
            }

            agency.Funds += payout;
        }

        private void EvaluateSatelliteNetworkFundingContracts()
        {
            _hasFundingPayoutCache = false;
            Array.Clear(_satellitePayoutCache, 0, _satellitePayoutCache.Length);
            Array.Clear(_achievementPayoutCache, 0, _achievementPayoutCache.Length);

            for (int agencyIndex = 0; agencyIndex < _agencies.Count; agencyIndex++)
            {
                AgencyState agency = _agencies[agencyIndex];
                double nextPayoutFunds = 0.0;

                for (int programmeIndex = 0; programmeIndex < _satelliteNetworkFundingContracts.Count; programmeIndex++)
                {
                    double payout = CalculateSatelliteCurrentPayout(
                        agency,
                        _satelliteNetworkFundingContracts[programmeIndex]);
                    _satellitePayoutCache[agencyIndex, programmeIndex] = payout;
                    nextPayoutFunds += payout;
                }

                agency.NextPayoutFunds = nextPayoutFunds;

                if (!agency.IsPlayer)
                {
                    agency.NextPayoutFunds += RivalBaseIncomeFunds;
                }
            }

            if (_nextFundingUniversalTime >= 0.0)
            {
                for (int programmeIndex = 0;
                    programmeIndex < _achievementSatelliteNetworkFundingContracts.Count;
                    programmeIndex++)
                {
                    ObjectiveFundingContract programme = _achievementSatelliteNetworkFundingContracts[programmeIndex];
                    if (!programme.IsOffered || !programme.HasStarted || programme.IsExpired)
                    {
                        continue;
                    }

                    int eligibleAgencyCount = GetAchievementAgencyCountAtTime(
                        programme.Id,
                        _nextFundingUniversalTime);

                    for (int agencyIndex = 0; agencyIndex < _agencies.Count; agencyIndex++)
                    {
                        AgencyState agency = _agencies[agencyIndex];
                        double payout = programme.CalculateCurrentPayout(
                            HasProgramAchievedByTime(
                                agency,
                                programme.Id,
                                _nextFundingUniversalTime),
                            eligibleAgencyCount);
                        _achievementPayoutCache[agencyIndex, programmeIndex] = payout;
                        agency.NextPayoutFunds += payout;
                    }
                }
            }

            _hasFundingPayoutCache = true;
        }

        private double CalculateSatelliteFundingForProgram(AgencyState agency)
        {
            if (agency == null)
            {
                return 0.0;
            }

            double payout = 0.0;

            for (int programmeIndex = 0; programmeIndex < _satelliteNetworkFundingContracts.Count; programmeIndex++)
            {
                payout += CalculateSatelliteCurrentPayout(agency, _satelliteNetworkFundingContracts[programmeIndex]);
            }

            return payout;
        }

        private double CalculateSatelliteCurrentPayout(
            AgencyState agency,
            SatelliteNetworkFundingContract fundingProgramme)
        {
            if (agency == null
                || fundingProgramme == null
                || !fundingProgramme.IsAvailable
                || !fundingProgramme.IsOffered)
            {
                return 0.0;
            }

            int totalSatelliteCount = GetCollectiveSatelliteCount(fundingProgramme.CelestialBodyName);

            return fundingProgramme.CalculateCurrentPayout(
                agency.GetSatelliteCount(fundingProgramme.CelestialBodyName),
                totalSatelliteCount);
        }

        private int GetCollectiveSatelliteCount(string celestialBodyName)
        {
            if (string.IsNullOrEmpty(celestialBodyName))
            {
                return 0;
            }

            int totalSatelliteCount = 0;
            for (int agencyIndex = 0; agencyIndex < _agencies.Count; agencyIndex++)
            {
                totalSatelliteCount += _agencies[agencyIndex].GetSatelliteCount(celestialBodyName);
            }

            return totalSatelliteCount;
        }

        private double CalculateAchievementCurrentPayout(
            AgencyState agency,
            ObjectiveFundingContract achievementProgramme)
        {
            if (agency == null
                || achievementProgramme == null
                || !achievementProgramme.IsOffered
                || _nextFundingUniversalTime < 0.0)
            {
                return 0.0;
            }

            int eligibleAgencyCount = GetAchievementAgencyCountAtTime(
                achievementProgramme.Id,
                _nextFundingUniversalTime);

            return achievementProgramme.CalculateCurrentPayout(
                HasProgramAchievedByTime(
                    agency,
                    achievementProgramme.Id,
                    _nextFundingUniversalTime),
                eligibleAgencyCount);
        }

        private bool IsAchievementProgrammeAvailableAtTime(
            ObjectiveFundingContract achievementProgramme,
            double evaluationUniversalTime)
        {
            return achievementProgramme != null
                && UnlockRuleEvaluator.IsSatisfied(
                    achievementProgramme.UnlockRule,
                    _agencies,
                    evaluationUniversalTime);
        }

        private int GetAchievementAgencyCountAtTime(
            string objectiveId,
            double payoutUniversalTime)
        {
            if (string.IsNullOrEmpty(objectiveId))
            {
                return 0;
            }

            int achievedAgencyCount = 0;
            for (int agencyIndex = 0; agencyIndex < _agencies.Count; agencyIndex++)
            {
                if (HasProgramAchievedByTime(
                    _agencies[agencyIndex],
                    objectiveId,
                    payoutUniversalTime))
                {
                    achievedAgencyCount++;
                }
            }

            return achievedAgencyCount;
        }

        private bool HasProgramAchievedByTime(
            AgencyState agency,
            string objectiveId,
            double payoutUniversalTime)
        {
            if (agency == null || string.IsNullOrEmpty(objectiveId))
            {
                return false;
            }

            double achievementUniversalTime = agency.GetObjectiveCompletionTime(objectiveId);
            return achievementUniversalTime >= 0.0 && achievementUniversalTime <= payoutUniversalTime;
        }
    }
}

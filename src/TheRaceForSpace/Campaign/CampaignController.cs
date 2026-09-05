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
        private const int MaximumUncompletedNormalObjectiveOffers = 2;
        private const int MaximumUnfulfilledSatelliteOffers = 2;

        private static readonly Random OfferRandomGenerator = new Random();

        private readonly List<AgencyState> _agencies = new List<AgencyState>();
        private readonly List<AgencyState> _rivalAgencies = new List<AgencyState>();
        private readonly List<SatelliteNetworkFundingContract> _satelliteNetworkFundingContracts = new List<SatelliteNetworkFundingContract>();
        private readonly List<ObjectiveFundingContract> _objectiveFundingContracts =
            new List<ObjectiveFundingContract>();
        private readonly IList<AgencyState> _agenciesView;
        private readonly IList<AgencyState> _rivalAgenciesView;
        private readonly IList<SatelliteNetworkFundingContract> _satelliteNetworkFundingContractsView;
        private readonly IList<ObjectiveFundingContract> _objectiveFundingContractsView;
        private IList<ObjectiveDefinition> _activeFlightContracts =
            new List<ObjectiveDefinition>().AsReadOnly();
        private readonly double[,] _satellitePayoutCache;
        private readonly double[,] _objectivePayoutCache;
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
            // offered pre-orbit lines rather than assuming Probe Orbit is available at campaign start.
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
            IList<ObjectiveFundingContract> objectiveFundingContracts =
                FundingContractCatalogue.CreateObjectiveFundingContracts();
            for (int contractIndex = 0; contractIndex < objectiveFundingContracts.Count; contractIndex++)
            {
                _objectiveFundingContracts.Add(objectiveFundingContracts[contractIndex]);
            }

            IList<SatelliteNetworkFundingContract> satelliteNetworkFundingContracts =
                FundingContractCatalogue.CreateSatelliteNetworkFundingContracts();
            for (int contractIndex = 0; contractIndex < satelliteNetworkFundingContracts.Count; contractIndex++)
            {
                _satelliteNetworkFundingContracts.Add(satelliteNetworkFundingContracts[contractIndex]);
            }

            // Contract and agency collections are fixed for one controller lifetime. Cache arrays
            // therefore need no recurring allocation and can be rebuilt in place after each refresh.
            _satellitePayoutCache = new double[_agencies.Count, _satelliteNetworkFundingContracts.Count];
            _objectivePayoutCache = new double[_agencies.Count, _objectiveFundingContracts.Count];

            _agenciesView = _agencies.AsReadOnly();
            _rivalAgenciesView = _rivalAgencies.AsReadOnly();
            _satelliteNetworkFundingContractsView = _satelliteNetworkFundingContracts.AsReadOnly();
            _objectiveFundingContractsView = _objectiveFundingContracts.AsReadOnly();
        }

        public AgencyState PlayerAgency { get; private set; }
        public IList<AgencyState> Agencies { get { return _agenciesView; } }
        public IList<AgencyState> RivalAgencies { get { return _rivalAgenciesView; } }
        public IList<SatelliteNetworkFundingContract> SatelliteNetworkFundingContracts { get { return _satelliteNetworkFundingContractsView; } }
        public IList<ObjectiveFundingContract> ObjectiveFundingContracts
        {
            get { return _objectiveFundingContractsView; }
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
        /// pre-orbit objectiveCompletion. The following controller refresh rebuilds the set once after all
        /// resulting unlock/offer/funding changes have been processed.
        /// </summary>
        internal void NotifyPlayerPreOrbitObjectiveCompleted()
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

            return RivalSimulation.CalculateMissionProgressCost(
                agency,
                _objectiveFundingContracts,
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
                _objectiveFundingContracts,
                _satelliteNetworkFundingContracts);
        }

        public bool HasAgencyCompletedObjective(
            AgencyState agency,
            ObjectiveFundingContract objectiveFundingContract)
        {
            return agency != null
                && objectiveFundingContract != null
                && agency.HasCompletedObjective(objectiveFundingContract.Id);
        }

        /// <summary>
        /// Returns whether an objectiveCompletion funding target is unlocked at the current campaign time.
        /// Unlock state is intentionally separate from whether sponsors have offered the contract.
        /// </summary>
        public bool IsObjectiveFundingContractAvailable(ObjectiveFundingContract objectiveFundingContract)
        {
            if (objectiveFundingContract == null)
            {
                return false;
            }

            double evaluationUniversalTime = Planetarium.fetch == null
                ? 0.0
                : Planetarium.GetUniversalTime();
            return IsObjectiveFundingContractAvailableAtTime(
                objectiveFundingContract,
                evaluationUniversalTime);
        }

        /// <summary>
        /// Returns this agency's projected share of one satellite contract. After the first
        /// completed controller refresh, the value comes from that refresh's payout snapshot so
        /// repeated UI queries do not re-scan every agency's satellite count.
        /// </summary>
        public double GetSatelliteCurrentPayout(
            AgencyState agency,
            SatelliteNetworkFundingContract networkContract)
        {
            if (agency == null
                || networkContract == null
                || !networkContract.IsAvailable
                || !networkContract.IsOffered)
            {
                return 0.0;
            }

            if (_hasFundingPayoutCache)
            {
                int agencyIndex = _agencies.IndexOf(agency);
                int contractIndex = _satelliteNetworkFundingContracts.IndexOf(networkContract);
                if (agencyIndex >= 0 && contractIndex >= 0)
                {
                    return _satellitePayoutCache[agencyIndex, contractIndex];
                }
            }

            return CalculateSatelliteCurrentPayout(agency, networkContract);
        }

        /// <summary>
        /// Returns this agency's projected share of the objectiveCompletion contract on the next global
        /// funding date. After a completed refresh the value is served from the same payout snapshot
        /// used to build NextPayoutFunds, avoiding repeated agency-count calculations in the UI.
        /// </summary>
        public double GetObjectiveCurrentPayout(
            AgencyState agency,
            ObjectiveFundingContract objectiveFundingContract)
        {
            if (agency == null || objectiveFundingContract == null || !objectiveFundingContract.IsOffered)
            {
                return 0.0;
            }

            if (_hasFundingPayoutCache)
            {
                int agencyIndex = _agencies.IndexOf(agency);
                int contractIndex = _objectiveFundingContracts.IndexOf(objectiveFundingContract);
                if (agencyIndex >= 0 && contractIndex >= 0)
                {
                    return _objectivePayoutCache[agencyIndex, contractIndex];
                }
            }

            return CalculateObjectiveCurrentPayout(agency, objectiveFundingContract);
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
                    _objectiveFundingContracts,
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

            // Probe Orbit remains an immediate shared-race unlock once any pre-orbit line reaches
            // Level V. PreOrbit Levels II-V wait for the funding-day review, where every unlocked
            // pre-orbit contract is offered without consuming normal one-off objectiveCompletion slots.
            UpdateSpecialObjectiveOffers(stateEvaluationUniversalTime);
            UpdateSatelliteTargetReachedState();
            StartObjectiveFundingContracts(stateEvaluationUniversalTime);
            RebuildActiveFlightContractPlanIfNeeded();
            EvaluateSatelliteNetworkFundingContracts();

            ModPersistenceScenario.CaptureRivalAgencyState(_rivalAgencies);
            ModPersistenceScenario.CaptureRaceProgress(
                PlayerAgency,
                _satelliteNetworkFundingContracts,
                _objectiveFundingContracts,
                _nextFundingUniversalTime);

            return didRefreshPlayerVessels;
        }

        private void RefreshRivals(double currentUniversalTime)
        {
            RivalSimulation.Refresh(
                _agencies,
                currentUniversalTime,
                _objectiveFundingContracts,
                _satelliteNetworkFundingContracts);
        }

        private void UpdateFundingAvailability(double evaluationUniversalTime)
        {
            for (int contractIndex = 0; contractIndex < _satelliteNetworkFundingContracts.Count; contractIndex++)
            {
                SatelliteNetworkFundingContract contract = _satelliteNetworkFundingContracts[contractIndex];
                if (contract.IsAvailable)
                {
                    continue;
                }

                if (UnlockRuleEvaluator.IsSatisfied(
                    contract.UnlockRule,
                    _agencies,
                    evaluationUniversalTime))
                {
                    contract.Unlock();
                }
            }
        }

        /// <summary>
        /// Offers Probe Orbit immediately when any agency completes a Level V pre-orbit contract.
        /// PreOrbit Levels II-V otherwise wait in Unlocked until the funding-day sponsor review,
        /// where every unlocked pre-orbit contract is offered independently of the normal offer cap.
        /// </summary>
        private void UpdateSpecialObjectiveOffers(double evaluationUniversalTime)
        {
            for (int contractIndex = 0;
                contractIndex < _objectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract = _objectiveFundingContracts[contractIndex];
                if (contract == null || contract.IsOffered || contract.IsExpired)
                {
                    continue;
                }

                if (!string.Equals(
                    contract.Id,
                    ObjectiveCatalogue.ProbeOrbitId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (IsObjectiveFundingContractAvailableAtTime(contract, evaluationUniversalTime))
                {
                    contract.Offer();
                }
            }
        }

        private void UpdateSatelliteTargetReachedState()
        {
            for (int contractIndex = 0; contractIndex < _satelliteNetworkFundingContracts.Count; contractIndex++)
            {
                SatelliteNetworkFundingContract contract = _satelliteNetworkFundingContracts[contractIndex];
                if (contract == null
                    || contract.HasReachedSatelliteTarget
                    || contract.RequiredSatellites <= 0)
                {
                    continue;
                }

                if (GetCollectiveSatelliteCount(contract.CelestialBodyName) >= contract.RequiredSatellites)
                {
                    contract.MarkSatelliteTargetReached();
                }
            }
        }

        private void StartObjectiveFundingContracts(double evaluationUniversalTime)
        {
            for (int contractIndex = 0; contractIndex < _objectiveFundingContracts.Count; contractIndex++)
            {
                ObjectiveFundingContract contract = _objectiveFundingContracts[contractIndex];
                if (contract.IsOffered
                    && !contract.HasStarted
                    && !contract.IsExpired
                    && GetObjectiveCompletionAgencyCountAtTime(contract.Id, evaluationUniversalTime) > 0)
                {
                    contract.Start();
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
                UpdateSpecialObjectiveOffers(payoutUniversalTime);
                UpdateSatelliteTargetReachedState();
                StartObjectiveFundingContracts(payoutUniversalTime);

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

                for (int contractIndex = 0; contractIndex < _objectiveFundingContracts.Count; contractIndex++)
                {
                    ObjectiveFundingContract contract = _objectiveFundingContracts[contractIndex];
                    if (!contract.IsOffered || !contract.HasStarted || contract.IsExpired)
                    {
                        continue;
                    }

                    int eligibleAgencyCount = GetObjectiveCompletionAgencyCountAtTime(
                        contract.Id,
                        payoutUniversalTime);
                    if (eligibleAgencyCount <= 0)
                    {
                        continue;
                    }

                    for (int agencyIndex = 0; agencyIndex < _agencies.Count; agencyIndex++)
                    {
                        AgencyState agency = _agencies[agencyIndex];
                        bool isEligible = HasAgencyCompletedObjectiveByTime(
                            agency,
                            contract.Id,
                            payoutUniversalTime);
                        double payout = contract.CalculateCurrentPayout(isEligible, eligibleAgencyCount);
                        AwardProgramFunds(agency, payout);
                    }

                    contract.AdvancePayout();
                    if (contract.IsExpired)
                    {
                        ObjectiveDefinition expiredObjective = ObjectiveCatalogue.FindById(contract.Id);
                        if (expiredObjective != null && expiredObjective.IsPreOrbitContract)
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
            var normalObjectiveCandidates = new List<ObjectiveFundingContract>();
            int uncompletedNormalObjectiveOfferCount = 0;

            for (int contractIndex = 0;
                contractIndex < _objectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract = _objectiveFundingContracts[contractIndex];
                if (contract == null || contract.IsExpired)
                {
                    continue;
                }

                ObjectiveDefinition objective = ObjectiveCatalogue.FindById(contract.Id);
                bool isPreOrbitContract = objective != null && objective.IsPreOrbitContract;

                if (contract.IsOffered)
                {
                    if (!contract.HasStarted && !isPreOrbitContract)
                    {
                        uncompletedNormalObjectiveOfferCount++;
                    }
                    continue;
                }

                if (!IsObjectiveFundingContractAvailableAtTime(contract, evaluationUniversalTime))
                {
                    continue;
                }

                // PreOrbit contracts are the four parallel pre-orbit development lines. Every
                // unlocked starter target is offered at the funding review, and starter offers do
                // not consume the two unfinished slots reserved for Probe Orbit and later targets.
                if (isPreOrbitContract)
                {
                    contract.Offer();
                    _isActiveFlightContractPlanDirty = true;
                    continue;
                }

                normalObjectiveCandidates.Add(contract);
            }

            int objectiveVacancies = Math.Max(
                0,
                MaximumUncompletedNormalObjectiveOffers - uncompletedNormalObjectiveOfferCount);
            for (int offerIndex = 0;
                offerIndex < objectiveVacancies && normalObjectiveCandidates.Count > 0;
                offerIndex++)
            {
                int candidateIndex = OfferRandomGenerator.Next(normalObjectiveCandidates.Count);
                normalObjectiveCandidates[candidateIndex].Offer();
                normalObjectiveCandidates.RemoveAt(candidateIndex);
            }

            var satelliteCandidates = new List<SatelliteNetworkFundingContract>();
            int unfulfilledSatelliteOfferCount = 0;

            for (int contractIndex = 0; contractIndex < _satelliteNetworkFundingContracts.Count; contractIndex++)
            {
                SatelliteNetworkFundingContract contract = _satelliteNetworkFundingContracts[contractIndex];
                if (contract == null)
                {
                    continue;
                }

                if (contract.IsOffered)
                {
                    if (!contract.HasReachedSatelliteTarget)
                    {
                        unfulfilledSatelliteOfferCount++;
                    }
                    continue;
                }

                if (contract.IsAvailable)
                {
                    satelliteCandidates.Add(contract);
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
            for (int contractIndex = 0;
                contractIndex < _objectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract = _objectiveFundingContracts[contractIndex];
                if (contract == null
                    || !contract.IsOffered
                    || contract.IsExpired
                    || PlayerAgency.HasCompletedObjective(contract.Id))
                {
                    continue;
                }

                ObjectiveDefinition objective = ObjectiveCatalogue.FindById(contract.Id);
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
            Array.Clear(_objectivePayoutCache, 0, _objectivePayoutCache.Length);

            for (int agencyIndex = 0; agencyIndex < _agencies.Count; agencyIndex++)
            {
                AgencyState agency = _agencies[agencyIndex];
                double nextPayoutFunds = 0.0;

                for (int contractIndex = 0; contractIndex < _satelliteNetworkFundingContracts.Count; contractIndex++)
                {
                    double payout = CalculateSatelliteCurrentPayout(
                        agency,
                        _satelliteNetworkFundingContracts[contractIndex]);
                    _satellitePayoutCache[agencyIndex, contractIndex] = payout;
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
                for (int contractIndex = 0;
                    contractIndex < _objectiveFundingContracts.Count;
                    contractIndex++)
                {
                    ObjectiveFundingContract contract = _objectiveFundingContracts[contractIndex];
                    if (!contract.IsOffered || !contract.HasStarted || contract.IsExpired)
                    {
                        continue;
                    }

                    int eligibleAgencyCount = GetObjectiveCompletionAgencyCountAtTime(
                        contract.Id,
                        _nextFundingUniversalTime);

                    for (int agencyIndex = 0; agencyIndex < _agencies.Count; agencyIndex++)
                    {
                        AgencyState agency = _agencies[agencyIndex];
                        double payout = contract.CalculateCurrentPayout(
                            HasAgencyCompletedObjectiveByTime(
                                agency,
                                contract.Id,
                                _nextFundingUniversalTime),
                            eligibleAgencyCount);
                        _objectivePayoutCache[agencyIndex, contractIndex] = payout;
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

            for (int contractIndex = 0; contractIndex < _satelliteNetworkFundingContracts.Count; contractIndex++)
            {
                payout += CalculateSatelliteCurrentPayout(agency, _satelliteNetworkFundingContracts[contractIndex]);
            }

            return payout;
        }

        private double CalculateSatelliteCurrentPayout(
            AgencyState agency,
            SatelliteNetworkFundingContract networkContract)
        {
            if (agency == null
                || networkContract == null
                || !networkContract.IsAvailable
                || !networkContract.IsOffered)
            {
                return 0.0;
            }

            int totalSatelliteCount = GetCollectiveSatelliteCount(networkContract.CelestialBodyName);

            return networkContract.CalculateCurrentPayout(
                agency.GetSatelliteCount(networkContract.CelestialBodyName),
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

        private double CalculateObjectiveCurrentPayout(
            AgencyState agency,
            ObjectiveFundingContract objectiveFundingContract)
        {
            if (agency == null
                || objectiveFundingContract == null
                || !objectiveFundingContract.IsOffered
                || _nextFundingUniversalTime < 0.0)
            {
                return 0.0;
            }

            int eligibleAgencyCount = GetObjectiveCompletionAgencyCountAtTime(
                objectiveFundingContract.Id,
                _nextFundingUniversalTime);

            return objectiveFundingContract.CalculateCurrentPayout(
                HasAgencyCompletedObjectiveByTime(
                    agency,
                    objectiveFundingContract.Id,
                    _nextFundingUniversalTime),
                eligibleAgencyCount);
        }

        private bool IsObjectiveFundingContractAvailableAtTime(
            ObjectiveFundingContract objectiveFundingContract,
            double evaluationUniversalTime)
        {
            return objectiveFundingContract != null
                && UnlockRuleEvaluator.IsSatisfied(
                    objectiveFundingContract.UnlockRule,
                    _agencies,
                    evaluationUniversalTime);
        }

        private int GetObjectiveCompletionAgencyCountAtTime(
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
                if (HasAgencyCompletedObjectiveByTime(
                    _agencies[agencyIndex],
                    objectiveId,
                    payoutUniversalTime))
                {
                    achievedAgencyCount++;
                }
            }

            return achievedAgencyCount;
        }

        private bool HasAgencyCompletedObjectiveByTime(
            AgencyState agency,
            string objectiveId,
            double payoutUniversalTime)
        {
            if (agency == null || string.IsNullOrEmpty(objectiveId))
            {
                return false;
            }

            double completionUniversalTime = agency.GetObjectiveCompletionTime(objectiveId);
            return completionUniversalTime >= 0.0 && completionUniversalTime <= payoutUniversalTime;
        }
    }
}

using System;
using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Persistence;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.KspIntegration
{
    /// <summary>
    /// Stores Campaign for Space campaign state inside the active KSP save through ScenarioModule.
    /// Static state is retained across normal gameplay scene changes for the same save folder,
    /// then replaced when KSP loads a different save.
    /// </summary>
    [KSPScenario(
        ScenarioCreationOptions.AddToAllGames,
        new[] { GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT, GameScenes.EDITOR })]
    public sealed class ModPersistenceScenario : ScenarioModule
    {
        private const string FundingContractsNodeName = "CAMPAIGN_FUNDING";
        private const string RivalAgenciesNodeName = "RIVAL_AGENCIES";
        private const string FlightContractProgressNodeName = "FLIGHT_CONTRACT_PROGRESS";
        private const string CommandCenterVisibleValueName = "commandCenterVisible";

        private static readonly CampaignFundingSaveState FundingContractsState =
            new CampaignFundingSaveState();
        private static readonly RivalAgenciesSaveState RivalAgenciesState = new RivalAgenciesSaveState();
        private static readonly FlightContractProgressSaveState FlightContractProgressState =
            new FlightContractProgressSaveState();
        private static string _loadedSaveFolder;
        private static bool _commandCenterVisible;
        private static bool _stateReady;

        public override void OnLoad(ConfigNode node)
        {
            string currentSaveFolder = HighLogic.SaveFolder;
            if (string.IsNullOrEmpty(currentSaveFolder))
            {
                _stateReady = false;
                return;
            }

            // KSP can replace HighLogic.CurrentGame during an ordinary scene transition while the
            // player remains in the same save. SaveFolder is the stable campaign identity here, so
            // keep newer in-memory state unless KSP actually loads another save folder.
            if (!string.Equals(_loadedSaveFolder, currentSaveFolder, StringComparison.Ordinal))
            {
                FundingContractsState.Load(
                    node == null ? null : node.GetNode(FundingContractsNodeName));
                RivalAgenciesState.Load(node == null ? null : node.GetNode(RivalAgenciesNodeName));
                FlightContractProgressState.Load(
                    node == null ? null : node.GetNode(FlightContractProgressNodeName));

                bool parsedCommandCenterVisible;
                _commandCenterVisible = node != null
                    && bool.TryParse(
                        node.GetValue(CommandCenterVisibleValueName),
                        out parsedCommandCenterVisible)
                    && parsedCommandCenterVisible;

                _loadedSaveFolder = currentSaveFolder;
            }

            _stateReady = true;
        }

        public override void OnSave(ConfigNode node)
        {
            if (node == null || !IsCurrentSaveReady())
            {
                return;
            }

            node.AddValue(CommandCenterVisibleValueName, _commandCenterVisible);

            if (FundingContractsState.HasData)
            {
                FundingContractsState.Save(node.AddNode(FundingContractsNodeName));
            }

            if (RivalAgenciesState.HasData)
            {
                RivalAgenciesState.Save(node.AddNode(RivalAgenciesNodeName));
            }

            if (FlightContractProgressState.HasData)
            {
                FlightContractProgressState.Save(node.AddNode(FlightContractProgressNodeName));
            }
        }

        public static bool TryRestoreCommandCenterVisibility(out bool isVisible)
        {
            isVisible = false;
            if (!IsCurrentSaveReady())
            {
                return false;
            }

            isVisible = _commandCenterVisible;
            return true;
        }

        public static void CaptureCommandCenterVisibility(bool isVisible)
        {
            if (!IsCurrentSaveReady())
            {
                return;
            }

            _commandCenterVisible = isVisible;
        }

        /// <summary>
        /// Restores each saved rival by stable agency ID once KSP has finished loading the current save.
        /// Rivals with no matching saved state keep their constructor defaults, allowing newly added
        /// rivals to enter an existing save created with the current collection format.
        /// </summary>
        public static bool TryRestoreRivalAgencyState(IList<AgencyState> rivalAgencies)
        {
            if (!IsCurrentSaveReady() || rivalAgencies == null)
            {
                return false;
            }

            RivalAgenciesState.ApplyTo(rivalAgencies);
            return true;
        }

        /// <summary>
        /// Restores player objective completion history, all funding-contract lifecycle state, and the next
        /// shared funding boundary by stable persisted IDs.
        /// </summary>
        public static bool TryRestoreCampaignProgress(
            AgencyState playerAgency,
            IList<SatelliteNetworkFundingContract> satelliteContracts,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            out double nextFundingUniversalTime)
        {
            nextFundingUniversalTime = -1.0;
            if (!IsCurrentSaveReady() || playerAgency == null)
            {
                return false;
            }

            FundingContractsState.ApplyTo(
                playerAgency,
                satelliteContracts,
                objectiveFundingContracts);
            nextFundingUniversalTime = FundingContractsState.NextFundingUniversalTime;
            return true;
        }

        /// <summary>
        /// Restores every remembered Flight Attempt, including lineage, historical telemetry, and
        /// independent Control contract progress, once KSP has loaded the current save state.
        /// </summary>
        public static bool TryRestoreFlightContractProgress(FlightContractTracker flightContractTracker)
        {
            if (!IsCurrentSaveReady() || flightContractTracker == null)
            {
                return false;
            }

            FlightContractProgressState.ApplyTo(flightContractTracker);
            return true;
        }

        public static void CaptureRivalAgencyState(IList<AgencyState> rivalAgencies)
        {
            if (!IsCurrentSaveReady() || rivalAgencies == null)
            {
                return;
            }

            RivalAgenciesState.Capture(rivalAgencies);
        }

        /// <summary>
        /// Captures player objective completion history, all funding-contract lifecycle state, and the next
        /// shared funding boundary. Player satellite counts remain owned by live KSP vessel tracking.
        /// </summary>
        public static void CaptureCampaignProgress(
            AgencyState playerAgency,
            IList<SatelliteNetworkFundingContract> satelliteContracts,
            IList<ObjectiveFundingContract> objectiveFundingContracts,
            double nextFundingUniversalTime)
        {
            if (!IsCurrentSaveReady())
            {
                return;
            }

            FundingContractsState.Capture(
                playerAgency,
                satelliteContracts,
                objectiveFundingContracts,
                nextFundingUniversalTime);
        }

        /// <summary>
        /// Captures all remembered Flight Attempts into ScenarioModule-owned state. This does not
        /// query KSP vessels; KSP writes the captured project-owned data during its normal save path.
        /// </summary>
        public static void CaptureFlightContractProgress(FlightContractTracker flightContractTracker)
        {
            if (!IsCurrentSaveReady() || flightContractTracker == null)
            {
                return;
            }

            FlightContractProgressState.Capture(flightContractTracker);
        }

        /// <summary>
        /// Clears static ScenarioModule state after the persistent runtime has actually returned to
        /// KSP's main menu. This allows reloading the same save folder later to deserialize it again.
        /// </summary>
        internal static void ResetLoadedSaveState()
        {
            FundingContractsState.Load(null);
            RivalAgenciesState.Load(null);
            FlightContractProgressState.Load(null);
            _loadedSaveFolder = null;
            _commandCenterVisible = false;
            _stateReady = false;
        }

        private static bool IsCurrentSaveReady()
        {
            return _stateReady
                && HighLogic.LoadedSceneIsGame
                && HighLogic.CurrentGame != null
                && !string.IsNullOrEmpty(_loadedSaveFolder)
                && string.Equals(
                    _loadedSaveFolder,
                    HighLogic.SaveFolder,
                    StringComparison.Ordinal);
        }
    }
}

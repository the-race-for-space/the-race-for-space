using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Persistence;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Tracking;

namespace TheRaceForSpace.KspIntegration
{
    /// <summary>
    /// Stores Race for Space campaign state inside the active KSP save through ScenarioModule.
    /// Static state is retained across normal gameplay scene changes for the same Game object,
    /// then replaced when KSP loads a different save.
    /// </summary>
    [KSPScenario(
        ScenarioCreationOptions.AddToAllGames,
        new[] { GameScenes.SPACECENTER, GameScenes.TRACKSTATION, GameScenes.FLIGHT, GameScenes.EDITOR })]
    public sealed class RacePersistenceScenario : ScenarioModule
    {
        private const string FundingContractsNodeName = "FUNDING_CONTRACTS";
        private const string RivalAgenciesNodeName = "RIVALS";
        private const string ActiveContractProgressNodeName = "ACTIVE_CONTRACT_PROGRESS";
        private const string CommandCenterVisibleValueName = "commandCenterVisible";

        private static readonly FundingContractsSaveState FundingContractsState =
            new FundingContractsSaveState();
        private static readonly RivalAgenciesSaveState RivalAgenciesState = new RivalAgenciesSaveState();
        private static readonly ActiveContractProgressSaveState ActiveContractProgressState =
            new ActiveContractProgressSaveState();
        private static Game _loadedGame;
        private static bool _commandCenterVisible;
        private static bool _stateReady;

        public override void OnLoad(ConfigNode node)
        {
            // KSP can call ScenarioModule.OnLoad again during normal scene changes. Keep the
            // newer in-memory state in that case; only deserialize when a different Game is loaded.
            if (_loadedGame != HighLogic.CurrentGame)
            {
                FundingContractsState.Load(
                    node == null ? null : node.GetNode(FundingContractsNodeName));
                RivalAgenciesState.Load(node == null ? null : node.GetNode(RivalAgenciesNodeName));
                ActiveContractProgressState.Load(
                    node == null ? null : node.GetNode(ActiveContractProgressNodeName));

                bool parsedCommandCenterVisible;
                _commandCenterVisible = node != null
                    && bool.TryParse(
                        node.GetValue(CommandCenterVisibleValueName),
                        out parsedCommandCenterVisible)
                    && parsedCommandCenterVisible;

                _loadedGame = HighLogic.CurrentGame;
            }

            _stateReady = _loadedGame != null;
        }

        public override void OnSave(ConfigNode node)
        {
            if (node == null || !_stateReady || _loadedGame != HighLogic.CurrentGame)
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

            if (ActiveContractProgressState.HasData)
            {
                ActiveContractProgressState.Save(node.AddNode(ActiveContractProgressNodeName));
            }
        }

        public static bool TryRestoreCommandCenterVisibility(out bool isVisible)
        {
            isVisible = false;
            if (!_stateReady || _loadedGame == null || _loadedGame != HighLogic.CurrentGame)
            {
                return false;
            }

            isVisible = _commandCenterVisible;
            return true;
        }

        public static void CaptureCommandCenterVisibility(bool isVisible)
        {
            if (!_stateReady || _loadedGame == null || _loadedGame != HighLogic.CurrentGame)
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
        public static bool TryRestoreRivalState(IList<AgencyState> rivalAgencies)
        {
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || rivalAgencies == null)
            {
                return false;
            }

            RivalAgenciesState.ApplyTo(rivalAgencies);
            return true;
        }

        /// <summary>
        /// Restores player objectiveCompletion history, all funding-contract lifecycle state, and the next
        /// shared funding boundary by stable persisted IDs.
        /// </summary>
        public static bool TryRestoreRaceProgress(
            AgencyState playerAgency,
            IList<SatelliteNetworkFundingContract> satelliteContracts,
            IList<ObjectiveFundingContract> achievementContracts,
            out double nextFundingUniversalTime)
        {
            nextFundingUniversalTime = -1.0;
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || playerAgency == null)
            {
                return false;
            }

            FundingContractsState.ApplyTo(
                playerAgency,
                satelliteContracts,
                achievementContracts);
            nextFundingUniversalTime = FundingContractsState.NextFundingUniversalTime;
            return true;
        }

        /// <summary>
        /// Restores temporary progress used to continue evaluating active contract conditions,
        /// including the tracked flight history and independent Control contract hold state.
        /// </summary>
        public static bool TryRestoreActiveContractProgress(StarterFlightTracker starterFlightTracker)
        {
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || starterFlightTracker == null)
            {
                return false;
            }

            ActiveContractProgressState.ApplyTo(starterFlightTracker);
            return true;
        }

        public static void CaptureRivalState(IList<AgencyState> rivalAgencies)
        {
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || rivalAgencies == null)
            {
                return;
            }

            RivalAgenciesState.Capture(rivalAgencies);
        }

        /// <summary>
        /// Captures player objectiveCompletion history, all funding-contract lifecycle state, and the next
        /// shared funding boundary. Player satellite counts remain owned by live KSP vessel tracking.
        /// </summary>
        public static void CaptureRaceProgress(
            AgencyState playerAgency,
            IList<SatelliteNetworkFundingContract> satelliteContracts,
            IList<ObjectiveFundingContract> achievementContracts,
            double nextFundingUniversalTime)
        {
            if (!_stateReady || _loadedGame == null || _loadedGame != HighLogic.CurrentGame)
            {
                return;
            }

            FundingContractsState.Capture(
                playerAgency,
                satelliteContracts,
                achievementContracts,
                nextFundingUniversalTime);
        }

        /// <summary>
        /// Captures temporary active-condition progress. This only updates ScenarioModule state;
        /// KSP writes it to disk during the normal save path.
        /// </summary>
        public static void CaptureActiveContractProgress(StarterFlightTracker starterFlightTracker)
        {
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || starterFlightTracker == null)
            {
                return;
            }

            ActiveContractProgressState.Capture(starterFlightTracker);
        }
    }
}

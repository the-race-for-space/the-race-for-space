using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Persistence;
using TheRaceForSpace.Programs;
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
        private const string RivalProgramsNodeName = "RIVALS";
        private const string ActiveContractProgressNodeName = "ACTIVE_CONTRACT_PROGRESS";
        private const string CommandCenterVisibleValueName = "commandCenterVisible";

        private static readonly FundingContractsSaveState FundingContractsState =
            new FundingContractsSaveState();
        private static readonly RivalProgramsSaveState RivalProgramsState = new RivalProgramsSaveState();
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
                RivalProgramsState.Load(node == null ? null : node.GetNode(RivalProgramsNodeName));
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

            if (RivalProgramsState.HasData)
            {
                RivalProgramsState.Save(node.AddNode(RivalProgramsNodeName));
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
        /// Restores each saved rival by stable program ID once KSP has finished loading the current save.
        /// Rivals with no matching saved state keep their constructor defaults, allowing newly added
        /// rivals to enter an existing save created with the current collection format.
        /// </summary>
        public static bool TryRestoreRivalPrograms(IList<SpaceProgramState> rivalPrograms)
        {
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || rivalPrograms == null)
            {
                return false;
            }

            RivalProgramsState.ApplyTo(rivalPrograms);
            return true;
        }

        /// <summary>
        /// Restores player achievement history, all funding-contract lifecycle state, and the next
        /// shared funding boundary by stable persisted IDs.
        /// </summary>
        public static bool TryRestoreFundingContracts(
            SpaceProgramState playerProgram,
            IList<FundingProgramme> satelliteContracts,
            IList<AchievementFundingProgramme> achievementContracts,
            out double nextFundingUniversalTime)
        {
            nextFundingUniversalTime = -1.0;
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || playerProgram == null)
            {
                return false;
            }

            FundingContractsState.ApplyTo(
                playerProgram,
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

        public static void CaptureRivalPrograms(IList<SpaceProgramState> rivalPrograms)
        {
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || rivalPrograms == null)
            {
                return;
            }

            RivalProgramsState.Capture(rivalPrograms);
        }

        /// <summary>
        /// Captures player achievement history, all funding-contract lifecycle state, and the next
        /// shared funding boundary. Player satellite counts remain owned by live KSP vessel tracking.
        /// </summary>
        public static void CaptureFundingContracts(
            SpaceProgramState playerProgram,
            IList<FundingProgramme> satelliteContracts,
            IList<AchievementFundingProgramme> achievementContracts,
            double nextFundingUniversalTime)
        {
            if (!_stateReady || _loadedGame == null || _loadedGame != HighLogic.CurrentGame)
            {
                return;
            }

            FundingContractsState.Capture(
                playerProgram,
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

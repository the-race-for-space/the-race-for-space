using System.Collections.Generic;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Persistence;
using TheRaceForSpace.Programs;

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
        private const string RivalProgramsNodeName = "RIVALS";
        private const string RaceProgressNodeName = "RACE_PROGRESS";
        private const string CommandCenterVisibleValueName = "commandCenterVisible";

        private static readonly RivalProgramsSaveState RivalProgramsState = new RivalProgramsSaveState();
        private static readonly RaceProgressSaveState RaceProgressState = new RaceProgressSaveState();
        private static Game _loadedGame;
        private static bool _commandCenterVisible;
        private static bool _stateReady;

        public override void OnLoad(ConfigNode node)
        {
            // KSP can call ScenarioModule.OnLoad again during normal scene changes. Keep the
            // newer in-memory state in that case; only deserialize when a different Game is loaded.
            if (_loadedGame != HighLogic.CurrentGame)
            {
                RivalProgramsState.Load(node == null ? null : node.GetNode(RivalProgramsNodeName));
                RaceProgressState.Load(node == null ? null : node.GetNode(RaceProgressNodeName));

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

            if (RivalProgramsState.HasData)
            {
                RivalProgramsState.Save(node.AddNode(RivalProgramsNodeName));
            }

            if (RaceProgressState.HasData)
            {
                RaceProgressState.Save(node.AddNode(RaceProgressNodeName));
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
        /// rivals to enter an existing save created with this collection format.
        /// </summary>
        public static bool TryRestoreRivalState(IList<SpaceProgramState> rivalPrograms)
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
        /// Restores player achievements, programme unlocks, and achievement-contract lifecycle
        /// state by stable ID rather than by a fixed prototype parameter list.
        /// </summary>
        public static bool TryRestoreRaceProgress(
            SpaceProgramState playerProgram,
            IList<FundingProgramme> fundingProgrammes,
            IList<AchievementFundingProgramme> achievementProgrammes)
        {
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || playerProgram == null)
            {
                return false;
            }

            RaceProgressState.ApplyTo(playerProgram, fundingProgrammes, achievementProgrammes);
            return true;
        }

        public static void CaptureRivalState(IList<SpaceProgramState> rivalPrograms)
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
        /// Captures player achievement state and programme lifecycle state by stable ID. Player
        /// satellite counts remain owned by live KSP vessel tracking and are not persisted here.
        /// </summary>
        public static void CaptureRaceProgress(
            SpaceProgramState playerProgram,
            IList<FundingProgramme> fundingProgrammes,
            IList<AchievementFundingProgramme> achievementProgrammes)
        {
            if (!_stateReady || _loadedGame == null || _loadedGame != HighLogic.CurrentGame)
            {
                return;
            }

            RaceProgressState.Capture(playerProgram, fundingProgrammes, achievementProgrammes);
        }
    }
}

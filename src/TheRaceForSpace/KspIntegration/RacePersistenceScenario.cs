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
        private const string AsterNodeName = "ASTER";
        private const string CobaltNodeName = "COBALT";
        private const string RaceProgressNodeName = "RACE_PROGRESS";
        private const string CommandCenterVisibleValueName = "commandCenterVisible";

        private static readonly RivalProgramSaveState AsterState = new RivalProgramSaveState();
        private static readonly RivalProgramSaveState CobaltState = new RivalProgramSaveState();
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
                AsterState.Load(node == null ? null : node.GetNode(AsterNodeName));
                CobaltState.Load(node == null ? null : node.GetNode(CobaltNodeName));
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

            if (AsterState.HasData)
            {
                AsterState.Save(node.AddNode(AsterNodeName));
            }

            if (CobaltState.HasData)
            {
                CobaltState.Save(node.AddNode(CobaltNodeName));
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
        /// Restores saved rival state once KSP has finished loading the current save.
        /// A new game has no rival nodes, so the controller's constructor defaults remain in use.
        /// </summary>
        public static bool TryRestoreRivalState(SpaceProgramState asterProgram, SpaceProgramState cobaltProgram)
        {
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || asterProgram == null
                || cobaltProgram == null)
            {
                return false;
            }

            AsterState.ApplyTo(asterProgram);
            CobaltState.ApplyTo(cobaltProgram);
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

        public static void CaptureRivalState(SpaceProgramState asterProgram, SpaceProgramState cobaltProgram)
        {
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || asterProgram == null
                || cobaltProgram == null)
            {
                return;
            }

            AsterState.Capture(asterProgram);
            CobaltState.Capture(cobaltProgram);
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

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
        private const string RivalProgramsNodeName = "RIVALS";
        private const string RaceProgressNodeName = "RACE_PROGRESS";
        private const string StarterFlightNodeName = "STARTER_FLIGHT";
        private const string CommandCenterVisibleValueName = "commandCenterVisible";

        private static readonly RivalProgramsSaveState RivalProgramsState = new RivalProgramsSaveState();
        private static readonly RaceProgressSaveState RaceProgressState = new RaceProgressSaveState();
        private static readonly StarterFlightSaveState StarterFlightState = new StarterFlightSaveState();
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
                StarterFlightState.Load(node == null ? null : node.GetNode(StarterFlightNodeName));

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

            if (StarterFlightState.HasData)
            {
                StarterFlightState.Save(node.AddNode(StarterFlightNodeName));
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
        /// Restores player achievements, programme unlocks, achievement-contract lifecycle state,
        /// and the next shared funding boundary by stable persisted values.
        /// </summary>
        public static bool TryRestoreRaceProgress(
            SpaceProgramState playerProgram,
            IList<FundingProgramme> fundingProgrammes,
            IList<AchievementFundingProgramme> achievementProgrammes,
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

            RaceProgressState.ApplyTo(playerProgram, fundingProgrammes, achievementProgrammes);
            nextFundingUniversalTime = RaceProgressState.NextFundingUniversalTime;
            return true;
        }

        /// <summary>
        /// Restores the active pre-orbit flight attempt. Missing data in an older save simply clears
        /// the tracker so the next controlled vessel starts a fresh attempt.
        /// </summary>
        public static bool TryRestoreStarterFlightState(StarterFlightTracker starterFlightTracker)
        {
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || starterFlightTracker == null)
            {
                return false;
            }

            StarterFlightState.ApplyTo(starterFlightTracker);
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
        /// Captures player achievement state, programme lifecycle state, and the next shared
        /// funding boundary by stable values. Player satellite counts remain owned by live KSP
        /// vessel tracking and are not persisted here.
        /// </summary>
        public static void CaptureRaceProgress(
            SpaceProgramState playerProgram,
            IList<FundingProgramme> fundingProgrammes,
            IList<AchievementFundingProgramme> achievementProgrammes,
            double nextFundingUniversalTime)
        {
            if (!_stateReady || _loadedGame == null || _loadedGame != HighLogic.CurrentGame)
            {
                return;
            }

            RaceProgressState.Capture(
                playerProgram,
                fundingProgrammes,
                achievementProgrammes,
                nextFundingUniversalTime);
        }

        /// <summary>
        /// Captures the current in-memory starter flight attempt. This only updates ScenarioModule
        /// state; KSP writes it to disk during the normal save path.
        /// </summary>
        public static void CaptureStarterFlightState(StarterFlightTracker starterFlightTracker)
        {
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || starterFlightTracker == null)
            {
                return;
            }

            StarterFlightState.Capture(starterFlightTracker);
        }
    }
}

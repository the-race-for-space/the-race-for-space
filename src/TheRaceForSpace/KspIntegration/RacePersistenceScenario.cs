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
        private const string SchemaVersion = "2";

        private static readonly RivalProgramSaveState AsterState = new RivalProgramSaveState();
        private static readonly RivalProgramSaveState CobaltState = new RivalProgramSaveState();
        private static readonly RaceProgressSaveState RaceProgressState = new RaceProgressSaveState();
        private static Game _loadedGame;
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

            node.AddValue("schemaVersion", SchemaVersion);

            if (AsterState.HasData)
            {
                ConfigNode asterNode = node.AddNode(AsterNodeName);
                AsterState.Save(asterNode);
            }

            if (CobaltState.HasData)
            {
                ConfigNode cobaltNode = node.AddNode(CobaltNodeName);
                CobaltState.Save(cobaltNode);
            }

            if (RaceProgressState.HasData)
            {
                ConfigNode raceProgressNode = node.AddNode(RaceProgressNodeName);
                RaceProgressState.Save(raceProgressNode);
            }
        }

        /// <summary>
        /// Restores saved rival state once KSP has finished loading the current save.
        /// A true result also covers old saves with no Race for Space node, allowing the
        /// controller to keep its current safe defaults for that first persisted session.
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
        /// Restores 0.3 player achievements, satellite-contract unlocks, and declining-interest
        /// payment stages. Old 0.2 saves have no RACE_PROGRESS node and therefore keep safe defaults.
        /// </summary>
        public static bool TryRestoreRaceProgress(
            SpaceProgramState playerProgram,
            FundingProgramme kerbinProgramme,
            FundingProgramme munProgramme,
            FundingProgramme minmusProgramme,
            AchievementFundingProgramme probeOrbitProgramme,
            AchievementFundingProgramme crewedOrbitProgramme)
        {
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame
                || playerProgram == null)
            {
                return false;
            }

            RaceProgressState.ApplyTo(
                playerProgram,
                kerbinProgramme,
                munProgramme,
                minmusProgramme,
                probeOrbitProgramme,
                crewedOrbitProgramme);
            return true;
        }

        /// <summary>
        /// Captures the latest live rival values so KSP's next ScenarioModule save writes
        /// current balances, satellite counts, achievements, and in-progress launch state.
        /// </summary>
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
        /// Captures player achievement flags and global contract progression without duplicating
        /// satellite counts that remain owned by the live KSP vessel tracker.
        /// </summary>
        public static void CaptureRaceProgress(
            SpaceProgramState playerProgram,
            FundingProgramme kerbinProgramme,
            FundingProgramme munProgramme,
            FundingProgramme minmusProgramme,
            AchievementFundingProgramme probeOrbitProgramme,
            AchievementFundingProgramme crewedOrbitProgramme)
        {
            if (!_stateReady
                || _loadedGame == null
                || _loadedGame != HighLogic.CurrentGame)
            {
                return;
            }

            RaceProgressState.Capture(
                playerProgram,
                kerbinProgramme,
                munProgramme,
                minmusProgramme,
                probeOrbitProgramme,
                crewedOrbitProgramme);
        }
    }
}

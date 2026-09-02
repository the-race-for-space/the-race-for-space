using TheRaceForSpace.Competition;
using TheRaceForSpace.KspIntegration;
using UnityEngine;

namespace TheRaceForSpace.Core
{
    /// <summary>
    /// Owns the current race controller and advances race progression independently of the UI.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class RaceRuntime : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 5.0f;

        private static RaceRuntime _activeInstance;
        private static SatelliteRaceController _raceController;
        private static Game _controllerGame;

        private bool _isDuplicateInstance;
        private float _nextRefreshTime;

        /// <summary>
        /// Returns the controller owned by the runtime for the current game, or null while the
        /// runtime is not ready or KSP is not in a saved-game scene.
        /// </summary>
        public static SatelliteRaceController Controller
        {
            get
            {
                if (!HighLogic.LoadedSceneIsGame
                    || HighLogic.CurrentGame == null
                    || _controllerGame != HighLogic.CurrentGame)
                {
                    return null;
                }

                return _raceController;
            }
        }

        public void Awake()
        {
            // EveryScene also instantiates addons during loading and on the main menu. Race
            // progression only exists while KSP has an active saved game.
            if (!HighLogic.LoadedSceneIsGame || HighLogic.CurrentGame == null)
            {
                Destroy(this);
                return;
            }

            // KSP can briefly create duplicate EveryScene addons while editor scenes and
            // sub-scenes are loading. Only one runtime instance may advance the race at a time.
            if (_activeInstance != null && _activeInstance != this)
            {
                _isDuplicateInstance = true;
                Destroy(this);
                return;
            }

            _activeInstance = this;
            EnsureControllerForCurrentGame();
        }

        public void OnDestroy()
        {
            if (_activeInstance == this)
            {
                _activeInstance = null;
            }
        }

        public void Update()
        {
            if (_isDuplicateInstance
                || _activeInstance != this
                || !HighLogic.LoadedSceneIsGame
                || HighLogic.CurrentGame == null)
            {
                return;
            }

            if (_controllerGame != HighLogic.CurrentGame)
            {
                EnsureControllerForCurrentGame();
            }

            if (_raceController == null || Time.realtimeSinceStartup < _nextRefreshTime)
            {
                return;
            }

            _raceController.Refresh();
            _nextRefreshTime = Time.realtimeSinceStartup + RefreshIntervalSeconds;
        }

        private void EnsureControllerForCurrentGame()
        {
            if (!HighLogic.LoadedSceneIsGame || HighLogic.CurrentGame == null)
            {
                return;
            }

            if (_raceController != null && _controllerGame == HighLogic.CurrentGame)
            {
                return;
            }

            // KSP has finished loading GameData before saved-game scenes begin, so configuration
            // is read once here before any controller-owned funding or rival state is constructed.
            RaceSettingsLoader.EnsureLoaded();

            // Keep one controller across scene changes inside a save, but never carry race or
            // rival state into a different save loaded during the same KSP process.
            _raceController = new SatelliteRaceController();
            _controllerGame = HighLogic.CurrentGame;
            _nextRefreshTime = 0.0f;
        }
    }
}

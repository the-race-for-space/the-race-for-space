using System.Collections.Generic;
using TheRaceForSpace.Competition;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Tracking;
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
        private const float ActiveVesselRefreshIntervalSeconds = 1.0f;
        private const float PlayerVesselRefreshIntervalSeconds = 20.0f;

        private static RaceRuntime _activeInstance;
        private static SatelliteRaceController _raceController;
        private static StarterFlightTracker _starterFlightTracker;
        private static Game _controllerGame;

        private bool _isDuplicateInstance;
        private bool _hasRestoredActiveContractProgress;
        private float _nextRefreshTime;
        private float _nextActiveVesselRefreshTime;
        private float _nextPlayerVesselRefreshTime;
        private IList<MilestoneDefinition> _starterTelemetryPlanSource;
        private StarterTelemetryRequirement _starterTelemetryRequirements =
            StarterTelemetryRequirement.None;

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

        /// <summary>
        /// Read-only access to the runtime-owned starter-flight state for presentation. UI callers
        /// must not advance this tracker; the one-second runtime observation remains authoritative.
        /// </summary>
        public static StarterFlightTracker StarterFlightState
        {
            get
            {
                if (!HighLogic.LoadedSceneIsGame
                    || HighLogic.CurrentGame == null
                    || _controllerGame != HighLogic.CurrentGame)
                {
                    return null;
                }

                return _starterFlightTracker;
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

            if (_raceController == null || _starterFlightTracker == null)
            {
                return;
            }

            float currentRealtime = Time.realtimeSinceStartup;

            if (currentRealtime >= _nextRefreshTime)
            {
                bool shouldRefreshPlayerVessels = currentRealtime >= _nextPlayerVesselRefreshTime;
                bool didRefreshPlayerVessels = _raceController.Refresh(shouldRefreshPlayerVessels);

                // Full KSP vessel discovery is the most expensive recurring operation and does not
                // need five-second responsiveness. Only start the 20-second interval after a successful
                // observation so transient scene/startup readiness still retries on the normal cadence.
                if (didRefreshPlayerVessels)
                {
                    _nextPlayerVesselRefreshTime = currentRealtime + PlayerVesselRefreshIntervalSeconds;
                }

                _nextRefreshTime = currentRealtime + RefreshIntervalSeconds;
            }

            if (!_hasRestoredActiveContractProgress
                && RacePersistenceScenario.TryRestoreActiveContractProgress(_starterFlightTracker))
            {
                _hasRestoredActiveContractProgress = true;

                // Scenario state may become ready between scheduled five-second controller ticks.
                // Force the controller's normal non-vessel path once before active-flight evaluation
                // so persisted achievements are restored before a new starter result can be recorded.
                _raceController.Refresh(false);
            }

            if (_hasRestoredActiveContractProgress
                && currentRealtime >= _nextActiveVesselRefreshTime)
            {
                RefreshStarterFlightState();
                _nextActiveVesselRefreshTime = currentRealtime + ActiveVesselRefreshIntervalSeconds;
            }
        }

        private void RefreshStarterFlightState()
        {
            IList<MilestoneDefinition> activeStarterContracts = _raceController.ActiveStarterContracts;

            // The controller replaces its read-only active list only after an offer/completion/expiry
            // transition. Recompute telemetry needs only when that exact cached plan instance changes,
            // then reuse the bit mask on every one-second observation in between.
            if (!object.ReferenceEquals(_starterTelemetryPlanSource, activeStarterContracts))
            {
                _starterTelemetryPlanSource = activeStarterContracts;
                _starterTelemetryRequirements = StarterTelemetryPlan.GetRequirements(
                    activeStarterContracts);

                // Surface-impact callbacks are part of the telemetry plan, so remove them once when
                // a new plan no longer contains Directed Power rather than repeating the same cleanup
                // on every one-second observation.
                if ((_starterTelemetryRequirements & StarterTelemetryRequirement.SurfaceImpact) == 0)
                {
                    KspVesselDiscovery.DisableActiveVesselSurfaceImpactTracking();
                }
            }

            if (activeStarterContracts == null || activeStarterContracts.Count == 0)
            {
                // No active starter contract means the tracker cannot change on this tick. Its last
                // captured state is already sufficient for a later save or sponsor offer.
                return;
            }

            bool recordedAchievement = false;
            bool needsSurfaceImpact = (_starterTelemetryRequirements
                & StarterTelemetryRequirement.SurfaceImpact) != 0;

            // Consume destruction before observing a replacement active vessel. KSP can switch
            // control immediately after a crash, and beginning the next attempt first would discard
            // the just-finished Directed Power flight history.
            if (needsSurfaceImpact)
            {
                string impactVesselId;
                string impactBodyName;
                double impactUniversalTime;
                if (KspVesselDiscovery.TryConsumeActiveVesselSurfaceImpact(
                    out impactVesselId,
                    out impactBodyName,
                    out impactUniversalTime))
                {
                    recordedAchievement = _starterFlightTracker.RecordSurfaceImpact(
                        _raceController.PlayerProgram,
                        activeStarterContracts,
                        impactVesselId,
                        impactBodyName,
                        impactUniversalTime);
                }
            }

            ActiveVesselTrackingSnapshot activeVesselSnapshot;
            if (KspVesselDiscovery.TryCaptureActiveVessel(
                _starterTelemetryRequirements,
                out activeVesselSnapshot))
            {
                recordedAchievement |= _starterFlightTracker.RefreshPlayerMilestones(
                    _raceController.PlayerProgram,
                    activeStarterContracts,
                    activeVesselSnapshot);
            }

            if (recordedAchievement)
            {
                // Starter achievements can immediately unlock the next line level or Probe Orbit.
                // Mark the active-contract cache dirty before reusing the controller's normal
                // non-vessel refresh so completion and any resulting offer changes settle together.
                _raceController.NotifyPlayerStarterAchievementRecorded();
                _raceController.Refresh(false);
            }

            RacePersistenceScenario.CaptureActiveContractProgress(_starterFlightTracker);
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

            // Keep one controller and one active-flight tracker across scene changes inside a save,
            // but never carry race, vessel-callback, or contract-attempt state into another save.
            _raceController = new SatelliteRaceController();
            _starterFlightTracker = new StarterFlightTracker();
            _controllerGame = HighLogic.CurrentGame;
            KspVesselDiscovery.ResetActiveVesselTracking();
            _hasRestoredActiveContractProgress = false;
            _nextRefreshTime = 0.0f;
            _nextActiveVesselRefreshTime = 0.0f;
            _nextPlayerVesselRefreshTime = 0.0f;
            _starterTelemetryPlanSource = null;
            _starterTelemetryRequirements = StarterTelemetryRequirement.None;
        }
    }
}

using System.Collections.Generic;
using TheRaceForSpace.Campaign;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Tracking;
using UnityEngine;

namespace TheRaceForSpace.Core
{
    /// <summary>
    /// Owns the current race controller and advances race progression independently of the UI.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class ModRuntime : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 5.0f;
        private const float ActiveVesselRefreshIntervalSeconds = 1.0f;
        private const float PlayerVesselRefreshIntervalSeconds = 20.0f;

        private static ModRuntime _activeInstance;
        private static CampaignController _campaignController;
        private static FlightContractTracker _flightContractTracker;
        private static Game _controllerGame;

        private bool _isDuplicateInstance;
        private bool _hasRestoredActiveContractProgress;
        private float _nextRefreshTime;
        private float _nextActiveVesselRefreshTime;
        private float _nextPlayerVesselRefreshTime;
        private IList<ObjectiveDefinition> _flightTelemetryPlanSource;
        private FlightTelemetryRequirement _flightTelemetryRequirements =
            FlightTelemetryRequirement.None;

        /// <summary>
        /// Returns the controller owned by the runtime for the current game, or null while the
        /// runtime is not ready or KSP is not in a saved-game scene.
        /// </summary>
        public static CampaignController Controller
        {
            get
            {
                if (!HighLogic.LoadedSceneIsGame
                    || HighLogic.CurrentGame == null
                    || _controllerGame != HighLogic.CurrentGame)
                {
                    return null;
                }

                return _campaignController;
            }
        }

        /// <summary>
        /// Read-only access to the runtime-owned flight-contract state for presentation. UI callers
        /// must not advance this tracker; the one-second runtime observation remains authoritative.
        /// </summary>
        public static FlightContractTracker FlightContractTrackingState
        {
            get
            {
                if (!HighLogic.LoadedSceneIsGame
                    || HighLogic.CurrentGame == null
                    || _controllerGame != HighLogic.CurrentGame)
                {
                    return null;
                }

                return _flightContractTracker;
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

            if (_campaignController == null || _flightContractTracker == null)
            {
                return;
            }

            float currentRealtime = Time.realtimeSinceStartup;

            if (currentRealtime >= _nextRefreshTime)
            {
                bool shouldRefreshPlayerVessels = currentRealtime >= _nextPlayerVesselRefreshTime;
                bool didRefreshPlayerVessels = _campaignController.Refresh(shouldRefreshPlayerVessels);

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
                && ModPersistenceScenario.TryRestoreFlightContractProgress(_flightContractTracker))
            {
                _hasRestoredActiveContractProgress = true;

                // Scenario state may become ready between scheduled five-second controller ticks.
                // Force the controller's normal non-vessel path once before active-flight evaluation
                // so persisted objectives are restored before a new flight-contract result can be recorded.
                _campaignController.Refresh(false);
            }

            if (_hasRestoredActiveContractProgress
                && currentRealtime >= _nextActiveVesselRefreshTime)
            {
                RefreshFlightContractTrackingState();
                _nextActiveVesselRefreshTime = currentRealtime + ActiveVesselRefreshIntervalSeconds;
            }
        }

        private void RefreshFlightContractTrackingState()
        {
            IList<ObjectiveDefinition> activeFlightContracts = _campaignController.ActiveFlightContracts;

            // The controller replaces its read-only active list only after an offer/completion/expiry
            // transition. Recompute telemetry needs only when that exact cached plan instance changes,
            // then reuse the bit mask on every one-second observation in between.
            if (!object.ReferenceEquals(_flightTelemetryPlanSource, activeFlightContracts))
            {
                _flightTelemetryPlanSource = activeFlightContracts;
                _flightTelemetryRequirements = FlightTelemetryPlan.GetRequirements(
                    activeFlightContracts);

                // Surface-impact callbacks are part of the telemetry plan, so remove them once when
                // a new plan no longer contains Directed Power rather than repeating the same cleanup
                // on every one-second observation.
                if ((_flightTelemetryRequirements & FlightTelemetryRequirement.SurfaceImpact) == 0)
                {
                    KspVesselMonitor.DisableActiveVesselSurfaceImpactTracking();
                }
            }

            if (activeFlightContracts == null || activeFlightContracts.Count == 0)
            {
                // No active pre-orbit contract means the tracker cannot change on this tick. Its last
                // captured state is already sufficient for a later save or sponsor offer.
                return;
            }

            bool recordedObjective = false;
            bool needsSurfaceImpact = (_flightTelemetryRequirements
                & FlightTelemetryRequirement.SurfaceImpact) != 0;

            // Consume destruction before observing a replacement active vessel. KSP can switch
            // control immediately after a crash, and beginning the next attempt first would discard
            // the just-finished Directed Power flight history.
            if (needsSurfaceImpact)
            {
                string impactVesselId;
                string impactBodyName;
                double impactUniversalTime;
                if (KspVesselMonitor.TryConsumeActiveVesselSurfaceImpact(
                    out impactVesselId,
                    out impactBodyName,
                    out impactUniversalTime))
                {
                    recordedObjective = _flightContractTracker.RecordSurfaceImpact(
                        _campaignController.PlayerAgency,
                        activeFlightContracts,
                        impactVesselId,
                        impactBodyName,
                        impactUniversalTime);
                }
            }

            ActiveVesselSnapshot activeVesselSnapshot;
            if (KspVesselMonitor.TryCaptureActiveVesselSnapshot(
                _flightTelemetryRequirements,
                out activeVesselSnapshot))
            {
                recordedObjective |= _flightContractTracker.EvaluateActiveFlightContracts(
                    _campaignController.PlayerAgency,
                    activeFlightContracts,
                    activeVesselSnapshot);
            }

            if (recordedObjective)
            {
                // PreOrbit objectives can immediately unlock the next line level or Probe Orbit.
                // Mark the active-contract cache dirty before reusing the controller's normal
                // non-vessel refresh so completion and any resulting offer changes settle together.
                _campaignController.NotifyPlayerPreOrbitObjectiveCompleted();
                _campaignController.Refresh(false);
            }

            ModPersistenceScenario.CaptureFlightContractProgress(_flightContractTracker);
        }

        private void EnsureControllerForCurrentGame()
        {
            if (!HighLogic.LoadedSceneIsGame || HighLogic.CurrentGame == null)
            {
                return;
            }

            if (_campaignController != null && _controllerGame == HighLogic.CurrentGame)
            {
                return;
            }

            // KSP has finished loading GameData before saved-game scenes begin, so configuration
            // is read once here before any controller-owned funding or rival state is constructed.
            CampaignSettingsLoader.EnsureLoaded();

            // Keep one controller and one active-flight tracker across scene changes inside a save,
            // but never carry race, vessel-callback, or contract-attempt state into another save.
            _campaignController = new CampaignController();
            _flightContractTracker = new FlightContractTracker();
            _controllerGame = HighLogic.CurrentGame;
            KspVesselMonitor.ResetActiveVesselTracking();
            _hasRestoredActiveContractProgress = false;
            _nextRefreshTime = 0.0f;
            _nextActiveVesselRefreshTime = 0.0f;
            _nextPlayerVesselRefreshTime = 0.0f;
            _flightTelemetryPlanSource = null;
            _flightTelemetryRequirements = FlightTelemetryRequirement.None;
        }
    }
}

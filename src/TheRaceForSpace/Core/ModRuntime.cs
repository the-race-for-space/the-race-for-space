using System;
using System.Collections.Generic;
using TheRaceForSpace.Campaign;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Tracking;
using UnityEngine;

namespace TheRaceForSpace.Core
{
    /// <summary>
    /// Owns the current campaign controller and advances campaign progression independently of the UI.
    /// The runtime persists for the KSP session while campaign state is replaced per loaded save.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public sealed class ModRuntime : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 5.0f;
        private const float ActiveVesselRefreshIntervalSeconds = 1.0f;
        private const float PlayerVesselRefreshIntervalSeconds = 20.0f;

        private static ModRuntime _activeInstance;
        private static CampaignController _campaignController;
        private static FlightContractTracker _flightContractTracker;
        private static string _controllerSaveFolder;

        private bool _hasRestoredActiveContractProgress;
        private float _nextRefreshTime;
        private float _nextActiveVesselRefreshTime;
        private float _nextPlayerVesselRefreshTime;
        private IList<ObjectiveDefinition> _flightTelemetryPlanSource;
        private FlightTelemetryRequirement _flightTelemetryRequirements =
            FlightTelemetryRequirement.None;

        /// <summary>
        /// Returns the controller owned by the runtime for the current save, or null while the
        /// runtime is not ready or KSP is not in a saved-game scene.
        /// </summary>
        public static CampaignController Controller
        {
            get { return IsControllerForCurrentSave() ? _campaignController : null; }
        }

        /// <summary>
        /// Read-only access to the runtime-owned flight-contract state for presentation. UI callers
        /// must not advance this tracker; the one-second runtime observation remains authoritative.
        /// </summary>
        public static FlightContractTracker FlightContractTrackingState
        {
            get { return IsControllerForCurrentSave() ? _flightContractTracker : null; }
        }

        public void Awake()
        {
            // The once flag should create only one session runtime, but KSP 1.12.x can occasionally
            // instantiate a once addon more than once. Keep the first persistent owner and discard
            // any later duplicate instead of replacing the runtime during a scene transition.
            if (_activeInstance != null && _activeInstance != this)
            {
                Destroy(this);
                return;
            }

            _activeInstance = this;
        }

        public void Start()
        {
            if (_activeInstance != this)
            {
                return;
            }

            // ModRuntime is intentionally session-owned rather than scene-owned. It must survive
            // Space Center/editor/Flight transitions so a sampler that runs during Flight loading
            // can retry after KSP reports the final Flight scene and active vessel.
            DontDestroyOnLoad(this);
            Debug.Log("[TheRaceForSpace] Persistent ModRuntime started for the KSP session.");

            // Instantly starts before a save normally exists. This call is therefore expected to
            // do nothing at the main menu and will be retried from Update once a game is available.
            EnsureControllerForCurrentGame();
        }

        public void OnDestroy()
        {
            if (_activeInstance == this)
            {
                KspVesselMonitor.ResetActiveVesselTracking();
                _activeInstance = null;
            }
        }

        public void Update()
        {
            if (_activeInstance != this)
            {
                return;
            }

            // The runtime survives the whole KSP process, so reaching the main menu is the reliable
            // boundary that ends one loaded-save session. Do not use transient CurrentGame objects
            // for this decision because KSP can replace them during ordinary scene changes.
            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                ReleaseCurrentSaveState();
                return;
            }

            if (!HighLogic.LoadedSceneIsGame
                || HighLogic.CurrentGame == null
                || string.IsNullOrEmpty(HighLogic.SaveFolder))
            {
                return;
            }

            if (_campaignController == null
                || _flightContractTracker == null
                || !string.Equals(
                    _controllerSaveFolder,
                    HighLogic.SaveFolder,
                    StringComparison.Ordinal))
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
                Debug.Log("[TheRaceForSpace] Flight telemetry persistence gate is ready.");

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

                int activeFlightContractCount = activeFlightContracts == null
                    ? 0
                    : activeFlightContracts.Count;
                Debug.Log(
                    "[TheRaceForSpace] Flight telemetry plan: "
                    + activeFlightContractCount
                    + " active contract(s); requirements "
                    + _flightTelemetryRequirements
                    + ".");

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
            string currentSaveFolder = HighLogic.SaveFolder;
            if (!HighLogic.LoadedSceneIsGame
                || HighLogic.CurrentGame == null
                || string.IsNullOrEmpty(currentSaveFolder))
            {
                return;
            }

            if (_campaignController != null
                && _flightContractTracker != null
                && string.Equals(
                    _controllerSaveFolder,
                    currentSaveFolder,
                    StringComparison.Ordinal))
            {
                return;
            }

            // KSP has finished loading GameData before saved-game scenes begin, so configuration
            // is read once here before any controller-owned funding or rival state is constructed.
            CampaignSettingsLoader.EnsureLoaded();

            // HighLogic.CurrentGame is not a stable save identity: KSP can replace that object during
            // ordinary scene changes. SaveFolder remains stable for one campaign and changes only when
            // another save becomes current, which is the boundary that should replace runtime state.
            _campaignController = new CampaignController();
            _flightContractTracker = new FlightContractTracker();
            _controllerSaveFolder = currentSaveFolder;
            KspVesselMonitor.ResetActiveVesselTracking();
            _hasRestoredActiveContractProgress = false;
            _nextRefreshTime = 0.0f;
            _nextActiveVesselRefreshTime = 0.0f;
            _nextPlayerVesselRefreshTime = 0.0f;
            _flightTelemetryPlanSource = null;
            _flightTelemetryRequirements = FlightTelemetryRequirement.None;

            Debug.Log(
                "[TheRaceForSpace] ModRuntime initialized campaign state for save folder '"
                + currentSaveFolder
                + "'.");
        }

        private void ReleaseCurrentSaveState()
        {
            if (_campaignController == null
                && _flightContractTracker == null
                && string.IsNullOrEmpty(_controllerSaveFolder))
            {
                return;
            }

            _campaignController = null;
            _flightContractTracker = null;
            _controllerSaveFolder = null;
            _hasRestoredActiveContractProgress = false;
            _nextRefreshTime = 0.0f;
            _nextActiveVesselRefreshTime = 0.0f;
            _nextPlayerVesselRefreshTime = 0.0f;
            _flightTelemetryPlanSource = null;
            _flightTelemetryRequirements = FlightTelemetryRequirement.None;

            KspVesselMonitor.ResetActiveVesselTracking();
            ModPersistenceScenario.ResetLoadedSaveState();
            Debug.Log("[TheRaceForSpace] ModRuntime released current-save state at the main menu.");
        }

        private static bool IsControllerForCurrentSave()
        {
            return HighLogic.LoadedSceneIsGame
                && HighLogic.CurrentGame != null
                && !string.IsNullOrEmpty(_controllerSaveFolder)
                && !string.IsNullOrEmpty(HighLogic.SaveFolder)
                && string.Equals(
                    _controllerSaveFolder,
                    HighLogic.SaveFolder,
                    StringComparison.Ordinal);
        }
    }
}

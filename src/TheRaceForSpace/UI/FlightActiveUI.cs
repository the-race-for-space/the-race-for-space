using System;
using System.Collections.Generic;
using KSP.UI.Screens;
using TheRaceForSpace.Campaign;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Tracking;
using UnityEngine;

namespace TheRaceForSpace.UI
{
    /// <summary>
    /// Small Flight-only window for offered contract progress. This interface owns presentation
    /// only; campaign progression and active-vessel telemetry remain owned by the Core runtime.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class FlightActiveUI : MonoBehaviour
    {
        private const float WindowBackgroundOpacity = 0.82f;
        private const float WindowWidth = 380.0f;
        private const float WindowHeight = 220.0f;
        private const string LauncherIconTexturePath =
            "Squad/PartList/SimpleIcons/R&D_node_icon_flightControl";

        private static readonly GUILayoutOption[] ContractToggleOptions =
            { GUILayout.Width(24.0f), GUILayout.Height(22.0f) };
        private static readonly GUILayoutOption[] CompleteLabelOptions = { GUILayout.Width(72.0f) };
        private static readonly GUILayoutOption[] ContractListOptions = { GUILayout.Height(175.0f) };

        private static FlightActiveUI _activeInstance;

        private readonly HashSet<string> _expandedContractIds = new HashSet<string>();
        private ApplicationLauncherButton _launcherButton;
        private CampaignController _campaignController;
        private GUI.WindowFunction _drawWindowFunction;
        private Rect _windowRect;
        private Vector2 _contractScrollPosition;
        private bool _isDuplicateInstance;
        private bool _isVisible;

        public void Awake()
        {
            // Startup.Flight should create this only in Flight, but retain the scene check because
            // KSP/Unity objects can be instantiated during transitions before the scene is usable.
            if (!HighLogic.LoadedSceneIsFlight)
            {
                Destroy(this);
                return;
            }

            if (_activeInstance != null && _activeInstance != this)
            {
                _isDuplicateInstance = true;
                Destroy(this);
                return;
            }

            _activeInstance = this;
            _drawWindowFunction = DrawWindow;
            _windowRect = CreateCenteredWindowRect();
            _isVisible = false;
        }

        public void OnDestroy()
        {
            // ApplicationLauncher can be recreated as KSP leaves Flight. Remove our button while
            // this component still owns it so the Flight-only control cannot leak into other scenes.
            if (_launcherButton != null && ApplicationLauncher.Instance != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_launcherButton);
            }

            _launcherButton = null;
            _campaignController = null;
            _drawWindowFunction = null;

            if (_activeInstance == this)
            {
                _activeInstance = null;
            }
        }

        public void Update()
        {
            if (_isDuplicateInstance
                || _activeInstance != this
                || !HighLogic.LoadedSceneIsFlight)
            {
                return;
            }

            // The Core runtime owns this controller. FlightActiveUI only refreshes its non-owning
            // reference so a UI instance created before the runtime is ready can recover naturally.
            _campaignController = ModRuntime.Controller;

            // The stock launcher is not guaranteed to be ready during Awake, so create the button
            // lazily once KSP exposes it. No campaign or vessel work is performed from this Update.
            if (_launcherButton == null)
            {
                EnsureApplicationLauncherButton();
            }
        }

        public void OnGUI()
        {
            if (_isDuplicateInstance
                || _activeInstance != this
                || !HighLogic.LoadedSceneIsFlight
                || !_isVisible)
            {
                return;
            }

            Color previousGuiColor = GUI.color;
            GUI.color = new Color(0.0f, 0.0f, 0.0f, WindowBackgroundOpacity);
            GUI.DrawTexture(_windowRect, Texture2D.whiteTexture);
            GUI.color = previousGuiColor;

            _windowRect = GUILayout.Window(
                GetInstanceID(),
                _windowRect,
                _drawWindowFunction,
                "Offered Contracts");
        }

        private static Rect CreateCenteredWindowRect()
        {
            float windowX = Mathf.Max(0.0f, (Screen.width - WindowWidth) * 0.5f);
            float windowY = Mathf.Max(0.0f, (Screen.height - WindowHeight) * 0.5f);
            return new Rect(windowX, windowY, WindowWidth, WindowHeight);
        }

        private void EnsureApplicationLauncherButton()
        {
            if (_launcherButton != null
                || !ApplicationLauncher.Ready
                || ApplicationLauncher.Instance == null)
            {
                return;
            }

            // Use a stock Flight Control icon so this button is visually separate from the main
            // Command Center launcher without adding another image asset to the prototype.
            Texture launcherIconTexture = GameDatabase.Instance != null
                ? GameDatabase.Instance.GetTexture(LauncherIconTexturePath, false)
                : null;

            if (launcherIconTexture == null)
            {
                launcherIconTexture = Texture2D.whiteTexture;
            }

            _launcherButton = ApplicationLauncher.Instance.AddModApplication(
                delegate { SetVisible(true); },
                delegate { SetVisible(false); },
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.FLIGHT,
                launcherIconTexture);

            SetVisible(_isVisible);
        }

        private void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;

            if (_launcherButton == null)
            {
                return;
            }

            if (_isVisible)
            {
                _launcherButton.SetTrue(false);
            }
            else
            {
                _launcherButton.SetFalse(false);
            }
        }

        private void DrawWindow(int windowId)
        {
            if (_campaignController == null)
            {
                GUILayout.Label("Contract data is not available yet.");
                GUILayout.FlexibleSpace();
                GUI.DragWindow();
                return;
            }

            _contractScrollPosition = GUILayout.BeginScrollView(
                _contractScrollPosition,
                ContractListOptions);

            bool hasUncompletedContracts = false;
            for (int contractIndex = 0;
                contractIndex < _campaignController.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract =
                    _campaignController.ObjectiveFundingContracts[contractIndex];
                if (contract.IsExpired
                    || !contract.IsOffered
                    || _campaignController.HasAgencyCompletedObjective(
                        _campaignController.PlayerAgency,
                        contract))
                {
                    continue;
                }

                hasUncompletedContracts = true;
                bool isExpanded = _expandedContractIds.Contains(contract.Id);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(isExpanded ? "-" : "+", ContractToggleOptions))
                {
                    if (isExpanded)
                    {
                        _expandedContractIds.Remove(contract.Id);
                        isExpanded = false;
                    }
                    else
                    {
                        _expandedContractIds.Add(contract.Id);
                        isExpanded = true;
                    }
                }

                GUILayout.Label(contract.Name);
                GUILayout.EndHorizontal();

                if (isExpanded)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(28.0f);
                    GUILayout.BeginVertical();
                    DrawExpandedContractRequirements(contract);
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                    GUILayout.Space(4.0f);
                }
            }

            bool hasCompletedContracts = false;
            for (int contractIndex = 0;
                contractIndex < _campaignController.ObjectiveFundingContracts.Count;
                contractIndex++)
            {
                ObjectiveFundingContract contract =
                    _campaignController.ObjectiveFundingContracts[contractIndex];
                if (contract.IsExpired
                    || !contract.IsOffered
                    || !_campaignController.HasAgencyCompletedObjective(
                        _campaignController.PlayerAgency,
                        contract))
                {
                    continue;
                }

                if (!hasCompletedContracts && hasUncompletedContracts)
                {
                    GUILayout.Space(6.0f);
                }

                hasCompletedContracts = true;
                GUILayout.BeginHorizontal();
                GUILayout.Space(24.0f);
                GUILayout.Label(contract.Name);
                GUILayout.FlexibleSpace();
                GUILayout.Label("Complete", CompleteLabelOptions);
                GUILayout.EndHorizontal();
            }

            if (!hasUncompletedContracts && !hasCompletedContracts)
            {
                GUILayout.Label("No offered contracts.");
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void DrawExpandedContractRequirements(ObjectiveFundingContract contract)
        {
            ObjectiveDefinition objective = contract == null
                ? null
                : ObjectiveCatalogue.FindById(contract.Id);
            if (objective == null)
            {
                GUILayout.Label("Requirement details are not available.");
                return;
            }

            if (!objective.IsPreOrbitContract)
            {
                GUILayout.Label("Requirement: " + contract.ObjectiveDescription);
                return;
            }

            FlightContractTracker tracker = ModRuntime.FlightContractTrackingState;
            if (tracker == null
                || !tracker.HasActiveAttempt
                || !ModRuntime.HasCurrentFlightVesselTelemetry)
            {
                GUILayout.Label("Waiting for vessel telemetry...");
                return;
            }

            bool isKerbin = string.Equals(
                tracker.CelestialBodyName,
                "Kerbin",
                StringComparison.OrdinalIgnoreCase);
            if (!isKerbin)
            {
                GUILayout.Label("Attempt status: Invalid - active vessel is not on Kerbin.");
            }

            if (tracker.EnteredOrbit)
            {
                GUILayout.Label("Attempt status: Invalid - vessel entered orbit.");
            }

            if (objective.PreOrbitLine == PreOrbitContractLine.DirectedPower)
            {
                bool speedMet = tracker.MaximumSurfaceSpeedMetersPerSecond
                    >= objective.RequiredSpeedMetersPerSecond;
                bool altitudeValid = tracker.MaximumAltitudeMeters
                    <= objective.MaximumAltitudeMeters;

                GUILayout.Label(
                    "Current Speed: "
                    + tracker.CurrentSurfaceSpeedMetersPerSecond.ToString("N0")
                    + " m/s");
                GUILayout.Label(
                    "Max Speed: "
                    + tracker.MaximumSurfaceSpeedMetersPerSecond.ToString("N0")
                    + " / "
                    + objective.RequiredSpeedMetersPerSecond.ToString("N0")
                    + " m/s - "
                    + (speedMet ? "Met" : "Pending"));
                GUILayout.Label(
                    "Max Altitude: "
                    + (tracker.MaximumAltitudeMeters / 1000.0).ToString("N1")
                    + " / "
                    + (objective.MaximumAltitudeMeters / 1000.0).ToString("N0")
                    + " km - "
                    + (altitudeValid ? "Valid" : "Invalid"));
                GUILayout.Label(
                    "Kerbin Impact: "
                    + (isKerbin && !tracker.EnteredOrbit && speedMet && altitudeValid
                        ? "Ready - impact Kerbin to complete"
                        : "Pending"));
                return;
            }

            if (objective.PreOrbitLine == PreOrbitContractLine.Mass)
            {
                bool massMet = tracker.CurrentMassTonnes >= objective.RequiredMassTonnes;
                bool distanceMet = tracker.CurrentDistanceMeters >= objective.RequiredDistanceMeters;
                bool recovered = tracker.CurrentSituation == FlightSituation.Landed
                    || tracker.CurrentSituation == FlightSituation.Splashed;

                GUILayout.Label(
                    "Mass: "
                    + tracker.CurrentMassTonnes.ToString("N1")
                    + " / "
                    + objective.RequiredMassTonnes.ToString("N1")
                    + " t - "
                    + (massMet ? "Met" : "Pending"));
                GUILayout.Label(
                    "Distance: "
                    + (tracker.CurrentDistanceMeters / 1000.0).ToString("N0")
                    + " / "
                    + (objective.RequiredDistanceMeters / 1000.0).ToString("N0")
                    + " km - "
                    + (distanceMet ? "Met" : "Pending"));
                GUILayout.Label(
                    "Landed / splashed: " + (recovered ? "Yes - Met" : "No - Pending"));
                return;
            }

            if (objective.PreOrbitLine == PreOrbitContractLine.Control)
            {
                bool holdQualified = tracker.IsControlObjectiveQualified(objective.Id);
                bool sampleInBand = tracker.IsControlSampleInBand(objective.Id);
                bool hasCrew = tracker.CurrentCrewCount > 0;

                GUILayout.Label(
                    "Altitude: "
                    + (tracker.CurrentAltitudeMeters / 1000.0).ToString("N1")
                    + " km ("
                    + (objective.MinimumAltitudeMeters / 1000.0).ToString("N0")
                    + "-"
                    + (objective.MaximumAltitudeMeters / 1000.0).ToString("N0")
                    + " km) - "
                    + (holdQualified ? "Hold complete" : (sampleInBand ? "In band" : "Out of band")));
                GUILayout.Label(
                    "Hold: "
                    + tracker.GetControlHoldSeconds(objective.Id).ToString("N0")
                    + " / "
                    + objective.RequiredDurationSeconds.ToString("N0")
                    + " s - "
                    + (holdQualified ? "Met" : "Pending"));
                GUILayout.Label(
                    "Crew aboard: "
                    + tracker.CurrentCrewCount
                    + " - "
                    + (hasCrew ? "Met" : "Required"));
                GUILayout.Label(
                    "Safe Kerbin recovery: "
                    + (holdQualified && hasCrew
                        ? "Ready - land or splash down safely to complete"
                        : "Pending"));
                return;
            }

            if (objective.PreOrbitLine == PreOrbitContractLine.Biome)
            {
                string currentBiome = string.IsNullOrEmpty(tracker.CurrentBiomeName)
                    ? "Unknown"
                    : tracker.CurrentBiomeName;
                bool biomeMatched = string.Equals(
                    currentBiome,
                    objective.RequiredBiomeName,
                    StringComparison.OrdinalIgnoreCase);
                bool recovered = tracker.CurrentSituation == FlightSituation.Landed
                    || tracker.CurrentSituation == FlightSituation.Splashed;

                GUILayout.Label("Current Biome: " + currentBiome);
                GUILayout.Label("Target: " + objective.RequiredBiomeName);
                GUILayout.Label("Biome Match: " + (biomeMatched ? "Yes - Met" : "No - Pending"));
                GUILayout.Label(
                    "Landed / splashed: " + (recovered ? "Yes - Met" : "No - Pending"));
            }
        }
    }
}

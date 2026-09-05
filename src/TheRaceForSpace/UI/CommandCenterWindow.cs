using System;
using System.Collections.Generic;
using System.Text;
using KSP.UI.Screens;
using TheRaceForSpace.Campaign;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Objectives;
using TheRaceForSpace.Agencies;
using TheRaceForSpace.Tracking;
using UnityEngine;

namespace TheRaceForSpace.UI
{
    /// <summary>
    /// Command center with four switchable views inside one interface window.
    /// Press F8 or use the stock KSP application launcher button to show or hide the interface.
    /// Race progression and controller lifetime are owned by ModRuntime in the Core module.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class CommandCenterWindow : MonoBehaviour
    {
        private enum ActiveView
        {
            Overview,
            FundingTargets,
            RivalAgencies,
            ContractCatalogue
        }

        private enum ContractCatalogueFundingCategory
        {
            Offered,
            Unlocked,
            Locked,
            Expired
        }

        private sealed class ContractCatalogueFundingEntry
        {
            public ContractCatalogueFundingEntry(
                ObjectiveFundingContract achievementProgramme,
                SatelliteNetworkFundingContract satelliteProgramme,
                string celestialBodyName,
                double bodySortDistance,
                int catalogueOrder)
            {
                AchievementProgramme = achievementProgramme;
                SatelliteProgramme = satelliteProgramme;
                CelestialBodyName = celestialBodyName;
                BodySortDistance = bodySortDistance;
                CatalogueOrder = catalogueOrder;
            }

            public ObjectiveFundingContract AchievementProgramme { get; private set; }
            public SatelliteNetworkFundingContract SatelliteProgramme { get; private set; }
            public string CelestialBodyName { get; private set; }
            public double BodySortDistance { get; private set; }
            public int CatalogueOrder { get; private set; }

            public bool IsAchievement
            {
                get { return AchievementProgramme != null; }
            }

            public string Id
            {
                get { return IsAchievement ? AchievementProgramme.Id : SatelliteProgramme.Id; }
            }

            public string Name
            {
                get { return IsAchievement ? AchievementProgramme.Name : SatelliteProgramme.Name; }
            }
        }

        private const double KerbinDaySeconds = 21600.0;
        private const float WindowBackgroundOpacity = 0.82f;
        private const float WindowWidth = 900.0f;
        private const float WindowHeight = 720.0f;
        private const float ContractCatalogueFundingInfoHeight = 275.0f;
        private const int HighlightedCardTitleFontSize = 16;
        private const int ContractCatalogueFundingButtonsPerRow = 4;
        private const string LauncherIconTexturePath =
            "Squad/PartList/SimpleIcons/R&D_node_icon_basicprobes";

        // GUILayoutOption is immutable after creation. Reusing these arrays avoids the params-array
        // and option allocations that otherwise occur on every IMGUI layout/repaint event.
        private static readonly GUILayoutOption[] TabButtonOptions = { GUILayout.Height(40.0f) };
        private static readonly GUILayoutOption[] HelpButtonOptions =
            { GUILayout.Width(36.0f), GUILayout.Height(40.0f) };
        private static readonly GUILayoutOption[] OverviewObjectivesOptions = { GUILayout.Width(350.0f) };
        private static readonly GUILayoutOption[] FundingLabelOptions = { GUILayout.Width(245.0f) };
        private static readonly GUILayoutOption[] FundingAmountOptions = { GUILayout.Width(100.0f) };
        private static readonly GUILayoutOption[] FundingCardLeftOptions = { GUILayout.Width(450.0f) };
        private static readonly GUILayoutOption[] RivalDetailsOptions = { GUILayout.Width(320.0f) };
        private static readonly GUILayoutOption[] RivalIncomeLabelOptions = { GUILayout.Width(245.0f) };
        private static readonly GUILayoutOption[] RivalIncomeAmountOptions = { GUILayout.Width(125.0f) };
        private static readonly GUILayoutOption[] ContractCatalogueFundingButtonOptions =
            { GUILayout.Width(195.0f), GUILayout.Height(40.0f) };
        private static readonly GUILayoutOption[] ContractCatalogueSectionToggleOptions =
            { GUILayout.Width(32.0f), GUILayout.Height(24.0f) };
        private static readonly GUILayoutOption[] ContractCatalogueFundingInfoOptions =
            { GUILayout.Height(ContractCatalogueFundingInfoHeight) };
        private static readonly GUILayoutOption[] ExpandWidthOptions = { GUILayout.ExpandWidth(true) };
        private static readonly GUILayoutOption[] NoExpandWidthOptions = { GUILayout.ExpandWidth(false) };

        private static CommandCenterWindow _activeInstance;
        private static ActiveView _activeView = ActiveView.Overview;
        private static Game _windowPositionGame;
        private static Rect _windowRect;

        // This is a non-owning reference to the current Core runtime controller. CommandCenterWindow never
        // creates or advances the controller; it only reads the state needed for presentation.
        private CampaignController _campaignController;
        private Game _visibilityGame;

        private readonly StringBuilder _listTextBuilder = new StringBuilder(128);
        private readonly List<ContractCatalogueFundingEntry> _contractCatalogueFundingEntries =
            new List<ContractCatalogueFundingEntry>();
        private Vector2 _fundingScrollPosition;
        private Vector2 _rivalsScrollPosition;
        private Vector2 _contractCatalogueScrollPosition;
        private Vector2 _contractCatalogueFundingInfoScrollPosition;
        private Vector2 _helpScrollPosition;
        private ApplicationLauncherButton _launcherButton;
        private GUIStyle _highlightedCardTitleStyle;
        private GUIStyle _boldLabelStyle;
        private GUI.WindowFunction _drawWindowFunction;
        private double[] _payoutScratch;
        private string[] _agencyNameScratch;
        private CampaignController _contractCatalogueFundingEntriesController;
        private int _contractCatalogueAchievementFundingCount = -1;
        private int _contractCatalogueSatelliteFundingCount = -1;
        private string _selectedContractCatalogueFundingId;
        private bool _selectedContractCatalogueFundingIsAchievement;
        private bool _contractCatalogueOfferedExpanded = true;
        private bool _contractCatalogueUnlockedExpanded;
        private bool _contractCatalogueLockedExpanded;
        private bool _contractCatalogueExpiredExpanded;
        private bool _hasRestoredVisibilityState;
        private bool _isDuplicateInstance;
        private bool _isVisible;
        private bool _showHelpGuide;

        public void Awake()
        {
            // EveryScene also instantiates addons during loading and on the main menu. Do not
            // create command-center UI state until KSP has entered an actual saved-game scene.
            if (!HighLogic.LoadedSceneIsGame || HighLogic.CurrentGame == null)
            {
                Destroy(this);
                return;
            }

            // KSP can instantiate an EveryScene addon more than once while editor scenes and
            // sub-scenes are loading. Only one CommandCenterWindow may own Update/OnGUI at a time.
            if (_activeInstance != null && _activeInstance != this)
            {
                _isDuplicateInstance = true;
                Destroy(this);
                return;
            }

            _activeInstance = this;
            _drawWindowFunction = DrawWindow;

            // Scene changes recreate CommandCenterWindow, so position and selected tab are static for the
            // current game. A different save starts from the normal centered Overview state.
            if (_windowPositionGame != HighLogic.CurrentGame)
            {
                _windowPositionGame = HighLogic.CurrentGame;
                _windowRect = CreateCenteredWindowRect();
                _activeView = ActiveView.Overview;
            }

            _visibilityGame = HighLogic.CurrentGame;
            _campaignController = ModRuntime.Controller;
        }

        public void OnDestroy()
        {
            // ApplicationLauncher is recreated by KSP across some scene/menu transitions, so
            // remove our button whenever this CommandCenterWindow instance stops owning the interface.
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
                || !HighLogic.LoadedSceneIsGame
                || HighLogic.CurrentGame == null)
            {
                return;
            }

            if (_visibilityGame != HighLogic.CurrentGame)
            {
                _visibilityGame = HighLogic.CurrentGame;
                _windowPositionGame = HighLogic.CurrentGame;
                _windowRect = CreateCenteredWindowRect();
                _activeView = ActiveView.Overview;
                _hasRestoredVisibilityState = false;
                _isVisible = false;
                _showHelpGuide = false;
            }

            // The Core runtime owns controller creation and progression. A null controller here
            // simply means this UI instance started before the runtime was ready in the scene.
            _campaignController = ModRuntime.Controller;
            if (_campaignController == null)
            {
                return;
            }

            // Wait for the ScenarioModule before creating or synchronizing the launcher button.
            // This prevents the new-save default (closed) from overwriting a saved open state.
            if (!_hasRestoredVisibilityState)
            {
                bool savedVisibility;
                if (!ModPersistenceScenario.TryRestoreCommandCenterVisibility(out savedVisibility))
                {
                    return;
                }

                _hasRestoredVisibilityState = true;
                SetCommandCenterVisible(savedVisibility);
            }

            // The stock ApplicationLauncher may not be ready during Awake. This lightweight
            // check creates the button once KSP has made the launcher available, and recreates
            // it if KSP destroys the launcher during a scene transition.
            if (_launcherButton == null)
            {
                EnsureApplicationLauncherButton();
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                SetCommandCenterVisible(!_isVisible);
            }
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

            // Reuse a stock KSP probe icon for the prototype so the launcher integration does
            // not require an additional image asset. A custom Race for Space icon can replace
            // this texture later without changing the launcher behaviour.
            Texture launcherIconTexture = GameDatabase.Instance != null
                ? GameDatabase.Instance.GetTexture(LauncherIconTexturePath, false)
                : null;

            if (launcherIconTexture == null)
            {
                launcherIconTexture = Texture2D.whiteTexture;
            }

            _launcherButton = ApplicationLauncher.Instance.AddModApplication(
                delegate { SetCommandCenterVisible(true); },
                delegate { SetCommandCenterVisible(false); },
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.ALWAYS,
                launcherIconTexture);

            // F8 and the stock launcher control the same visibility state. Setting the button
            // without firing its callback keeps its highlighted state synchronized with F8.
            SetCommandCenterVisible(_isVisible);
        }

        private void SetCommandCenterVisible(bool isVisible)
        {
            _isVisible = isVisible;

            if (_hasRestoredVisibilityState)
            {
                ModPersistenceScenario.CaptureCommandCenterVisibility(_isVisible);
            }

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

        public void OnGUI()
        {
            if (_isDuplicateInstance
                || _activeInstance != this
                || !HighLogic.LoadedSceneIsGame
                || HighLogic.CurrentGame == null
                || !_isVisible
                || _campaignController == null)
            {
                return;
            }

            // KSP's stock window skin is translucent enough that bright scenes can reduce text
            // contrast. A dark overlay behind the existing window keeps the stock controls and
            // styling while making the interface background substantially less see-through.
            Color previousGuiColor = GUI.color;
            GUI.color = new Color(0.0f, 0.0f, 0.0f, WindowBackgroundOpacity);
            GUI.DrawTexture(_windowRect, Texture2D.whiteTexture);
            GUI.color = previousGuiColor;

            _windowRect = GUILayout.Window(
                GetInstanceID(),
                _windowRect,
                _drawWindowFunction,
                "The Race for Space - Command Center");
        }

        private void DrawWindow(int windowId)
        {
            if (_highlightedCardTitleStyle == null)
            {
                _highlightedCardTitleStyle = new GUIStyle(GUI.skin.label);
                _highlightedCardTitleStyle.fontSize = HighlightedCardTitleFontSize;
                _highlightedCardTitleStyle.normal.textColor = Color.red;
            }

            if (_boldLabelStyle == null)
            {
                _boldLabelStyle = new GUIStyle(GUI.skin.label);
                _boldLabelStyle.fontStyle = FontStyle.Bold;
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Overview", TabButtonOptions))
            {
                _activeView = ActiveView.Overview;
                _showHelpGuide = false;
            }

            if (GUILayout.Button("Funding Targets", TabButtonOptions))
            {
                _activeView = ActiveView.FundingTargets;
                _showHelpGuide = false;
            }

            if (GUILayout.Button("Rival Agencies", TabButtonOptions))
            {
                _activeView = ActiveView.RivalAgencies;
                _showHelpGuide = false;
            }

            if (GUILayout.Button("Contract Catalogue", TabButtonOptions))
            {
                _activeView = ActiveView.ContractCatalogue;
                _showHelpGuide = false;
            }

            GUILayout.Space(4.0f);
            if (GUILayout.Button("?", HelpButtonOptions))
            {
                _showHelpGuide = true;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(12.0f);

            if (_showHelpGuide)
            {
                DrawHelpGuide();
            }
            else
            {
                switch (_activeView)
                {
                    case ActiveView.Overview:
                        DrawOverview();
                        break;

                    case ActiveView.FundingTargets:
                        DrawFundingTargets();
                        break;

                    case ActiveView.RivalAgencies:
                        DrawRivalAgencies();
                        break;

                    case ActiveView.ContractCatalogue:
                        DrawContractCatalogue();
                        break;
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("F8: show/hide interface");
            GUI.DragWindow();
        }

        private void DrawOverview()
        {
            AgencyState player = _campaignController.PlayerAgency;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(OverviewObjectivesOptions);
            GUILayout.Label("Your Objectives", _boldLabelStyle);

            for (int programmeIndex = 0;
                programmeIndex < _campaignController.ObjectiveFundingContracts.Count;
                programmeIndex++)
            {
                ObjectiveFundingContract programme =
                    _campaignController.ObjectiveFundingContracts[programmeIndex];
                if (programme.IsExpired || !programme.IsOffered)
                {
                    continue;
                }

                GUILayout.Label(
                    programme.Name
                    + ": "
                    + (_campaignController.HasProgramAchieved(player, programme) ? "ACHIEVED" : "IN PROGRESS"));
            }

            GUILayout.Space(10.0f);
            GUILayout.Label("Your Satellite Networks", _boldLabelStyle);

            for (int programmeIndex = 0;
                programmeIndex < _campaignController.SatelliteNetworkFundingContracts.Count;
                programmeIndex++)
            {
                SatelliteNetworkFundingContract programme = _campaignController.SatelliteNetworkFundingContracts[programmeIndex];
                int satelliteCount = player.GetSatelliteCount(programme.CelestialBodyName);
                if (satelliteCount <= 0)
                {
                    continue;
                }

                GUILayout.Label(
                    programme.CelestialBodyName
                    + " orbit: "
                    + satelliteCount
                    + " qualifying satellite(s)");
            }

            GUILayout.EndVertical();

            GUILayout.Space(24.0f);
            GUILayout.BeginVertical(ExpandWidthOptions);

            // The funding heading and shared date span both funding sub-columns so the date
            // has the full remaining window width and does not wrap above the payout rows.
            GUILayout.Label("Funding Information", _boldLabelStyle);
            GUILayout.Label(FormatNextFundingDate());
            GUILayout.Space(10.0f);

            // Projected programme payouts are refresh-scoped controller values, so drawing this
            // view does not repeat the cross-agency funding calculations on every IMGUI event.
            for (int programmeIndex = 0;
                programmeIndex < _campaignController.ObjectiveFundingContracts.Count;
                programmeIndex++)
            {
                ObjectiveFundingContract programme =
                    _campaignController.ObjectiveFundingContracts[programmeIndex];
                double nextPayout = _campaignController.GetAchievementCurrentPayout(player, programme);
                if (nextPayout <= 0.0)
                {
                    continue;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(programme.Name + ": Completed", FundingLabelOptions);
                GUILayout.Space(2.0f);
                GUILayout.Label(nextPayout.ToString("N0"), FundingAmountOptions);
                GUILayout.EndHorizontal();
            }

            for (int programmeIndex = 0;
                programmeIndex < _campaignController.SatelliteNetworkFundingContracts.Count;
                programmeIndex++)
            {
                SatelliteNetworkFundingContract programme = _campaignController.SatelliteNetworkFundingContracts[programmeIndex];
                int satelliteCount = player.GetSatelliteCount(programme.CelestialBodyName);
                double nextPayout = _campaignController.GetSatelliteCurrentPayout(player, programme);
                if (nextPayout <= 0.0)
                {
                    continue;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    satelliteCount
                    + " "
                    + programme.CelestialBodyName
                    + " "
                    + (satelliteCount == 1 ? "Satellite" : "Satellites"),
                    FundingLabelOptions);
                GUILayout.Space(2.0f);
                GUILayout.Label(nextPayout.ToString("N0"), FundingAmountOptions);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6.0f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Total Next Payout", _boldLabelStyle, FundingLabelOptions);
            GUILayout.Space(2.0f);
            GUILayout.Label(
                player.NextPayoutFunds.ToString("N0"),
                _boldLabelStyle,
                FundingAmountOptions);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawFundingTargets()
        {
            GUILayout.Label("FUNDING TARGETS - " + FormatNextFundingDate());
            GUILayout.Space(8.0f);

            EnsurePayoutScratchBuffers();
            _fundingScrollPosition = GUILayout.BeginScrollView(_fundingScrollPosition);

            for (int i = 0; i < _campaignController.ObjectiveFundingContracts.Count; i++)
            {
                ObjectiveFundingContract programme = _campaignController.ObjectiveFundingContracts[i];
                if (programme.IsExpired || !programme.IsOffered)
                {
                    continue;
                }

                DrawAchievementFundingCard(programme, showPreOrbitLiveProgress: true);
                GUILayout.Space(8.0f);
            }

            for (int i = 0; i < _campaignController.SatelliteNetworkFundingContracts.Count; i++)
            {
                SatelliteNetworkFundingContract programme = _campaignController.SatelliteNetworkFundingContracts[i];
                if (!programme.IsOffered)
                {
                    continue;
                }

                DrawSatelliteFundingCard(programme);
                GUILayout.Space(8.0f);
            }

            GUILayout.EndScrollView();
        }

        private void DrawAchievementFundingCard(
            ObjectiveFundingContract programme,
            string stateLabel = null,
            double? unlockEvaluationUniversalTime = null,
            string stateMessage = null,
            bool showPreOrbitLiveProgress = false,
            bool showFundingLifecycleDetails = true)
        {
            _listTextBuilder.Length = 0;
            _listTextBuilder.Append("Completed by: ");
            bool hasCompletedAgency = false;

            for (int agencyIndex = 0; agencyIndex < _campaignController.Agencies.Count; agencyIndex++)
            {
                AgencyState agency = _campaignController.Agencies[agencyIndex];
                _payoutScratch[agencyIndex] = _campaignController.GetAchievementCurrentPayout(agency, programme);

                if (!_campaignController.HasProgramAchieved(agency, programme))
                {
                    continue;
                }

                if (hasCompletedAgency)
                {
                    _listTextBuilder.Append(", ");
                }

                _listTextBuilder.Append(GetProgramDisplayName(agency));
                hasCompletedAgency = true;
            }

            if (!hasCompletedAgency)
            {
                _listTextBuilder.Append("None");
            }

            GUILayout.BeginVertical("box");
            DrawCenteredCardTitle(programme.Name);

            if (!string.IsNullOrEmpty(stateLabel))
            {
                GUILayout.Label("State: " + stateLabel, _boldLabelStyle);
            }

            if (unlockEvaluationUniversalTime.HasValue)
            {
                DrawUnlockRuleProgress(programme.UnlockRule, unlockEvaluationUniversalTime.Value);
            }

            if (!string.IsNullOrEmpty(stateMessage))
            {
                GUILayout.Label(stateMessage);
            }

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(FundingCardLeftOptions);
            GUILayout.Label(
                "Objective: "
                + programme.ObjectiveDescription
                + " Interest in funding decreases by 10% after each payout.");
            GUILayout.Label("Base Payout: " + programme.BaseRewardFunds.ToString("N0"));
            if (showFundingLifecycleDetails)
            {
                GUILayout.Label("Current Interest in Contract: " + programme.CurrentInterestPercent + "%");
                GUILayout.Label("Next Total Payout: " + programme.CurrentTotalPayoutFunds.ToString("N0"));
            }
            GUILayout.Label(
                "Contract Status: "
                + (programme.HasStarted ? "Completed - Paying Out" : "Not yet Completed"));
            GUILayout.EndVertical();

            GUILayout.Space(24.0f);
            GUILayout.BeginVertical(ExpandWidthOptions);
            GUILayout.Label(_listTextBuilder.ToString());
            DrawPayoutLinesByAmount();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (showPreOrbitLiveProgress)
            {
                DrawFlightContractLiveProgress(programme);
            }

            GUILayout.EndVertical();
        }

        private void DrawFlightContractLiveProgress(ObjectiveFundingContract programme)
        {
            ObjectiveDefinition objective = programme == null
                ? null
                : ObjectiveCatalogue.FindById(programme.Id);
            if (objective == null
                || !objective.IsPreOrbitContract
                || _campaignController.HasProgramAchieved(_campaignController.PlayerAgency, programme))
            {
                return;
            }

            GUILayout.Space(6.0f);
            FlightContractTracker tracker = ModRuntime.FlightContractTrackingState;
            if (tracker == null)
            {
                GUILayout.Label("Live Flight", _boldLabelStyle);
                GUILayout.Label("Live telemetry is not available yet.");
                return;
            }

            if (!tracker.HasActiveAttempt)
            {
                GUILayout.Label("Live Flight", _boldLabelStyle);
                GUILayout.Label(
                    HighLogic.LoadedSceneIsFlight
                        ? "Waiting for active vessel telemetry..."
                        : "Live telemetry is available while flying a vessel.");
                return;
            }

            // Funding Targets deliberately compares the active craft against every offered,
            // unfinished pre-orbit contract, even when a rival unlocked a later level first.
            DrawFlightContractProgress(objective, tracker);
        }

        private void DrawSatelliteFundingCard(
            SatelliteNetworkFundingContract programme,
            string stateLabel = null,
            double? unlockEvaluationUniversalTime = null,
            string stateMessage = null)
        {
            _listTextBuilder.Length = 0;
            _listTextBuilder.Append("Satellites: ");
            bool hasSatelliteSummary = false;
            double claimedPayout = 0.0;

            for (int agencyIndex = 0; agencyIndex < _campaignController.Agencies.Count; agencyIndex++)
            {
                AgencyState agency = _campaignController.Agencies[agencyIndex];
                int satelliteCount = agency.GetSatelliteCount(programme.CelestialBodyName);
                double nextPayout = _campaignController.GetSatelliteCurrentPayout(agency, programme);
                _payoutScratch[agencyIndex] = nextPayout;
                claimedPayout += nextPayout;

                if (satelliteCount <= 0)
                {
                    continue;
                }

                if (hasSatelliteSummary)
                {
                    _listTextBuilder.Append(", ");
                }

                _listTextBuilder.Append(GetProgramDisplayName(agency));
                _listTextBuilder.Append(' ');
                _listTextBuilder.Append(satelliteCount);
                hasSatelliteSummary = true;
            }

            if (!hasSatelliteSummary)
            {
                _listTextBuilder.Append("None");
            }

            double unclaimedPayout = Math.Max(0.0, programme.RewardFunds - claimedPayout);

            GUILayout.BeginVertical("box");
            DrawCenteredCardTitle("Satellite Funding - " + programme.Name);

            if (!string.IsNullOrEmpty(stateLabel))
            {
                GUILayout.Label("State: " + stateLabel, _boldLabelStyle);
            }

            if (unlockEvaluationUniversalTime.HasValue)
            {
                DrawUnlockRuleProgress(programme.UnlockRule, unlockEvaluationUniversalTime.Value);
            }

            if (!string.IsNullOrEmpty(stateMessage))
            {
                GUILayout.Label(stateMessage);
            }

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(FundingCardLeftOptions);
            GUILayout.Label("Target: " + programme.CelestialBodyName);
            GUILayout.Label("Requirement: " + programme.RequiredSatellites + " qualifying satellite(s) in orbit");
            GUILayout.Label("Total Available Payout: " + programme.RewardFunds.ToString("N0"));
            GUILayout.EndVertical();

            GUILayout.Space(24.0f);
            GUILayout.BeginVertical(ExpandWidthOptions);
            GUILayout.Label(_listTextBuilder.ToString());
            DrawPayoutLinesByAmount();
            GUILayout.Label("Unclaimed Payout: " + unclaimedPayout.ToString("N0"));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawPayoutLinesByAmount()
        {
            for (int agencyIndex = 0; agencyIndex < _campaignController.Agencies.Count; agencyIndex++)
            {
                _agencyNameScratch[agencyIndex] = GetProgramDisplayName(_campaignController.Agencies[agencyIndex]);
            }

            // A small in-place sort keeps the agency with the strongest next payout first while
            // allowing the same UI path to handle any number of current agencies. The caller
            // refills the reusable payout buffer before each card, so no per-card array is needed.
            for (int payoutIndex = 0; payoutIndex < _payoutScratch.Length - 1; payoutIndex++)
            {
                for (int compareIndex = payoutIndex + 1; compareIndex < _payoutScratch.Length; compareIndex++)
                {
                    if (_payoutScratch[compareIndex] <= _payoutScratch[payoutIndex])
                    {
                        continue;
                    }

                    double payoutToSwap = _payoutScratch[payoutIndex];
                    _payoutScratch[payoutIndex] = _payoutScratch[compareIndex];
                    _payoutScratch[compareIndex] = payoutToSwap;

                    string agencyNameToSwap = _agencyNameScratch[payoutIndex];
                    _agencyNameScratch[payoutIndex] = _agencyNameScratch[compareIndex];
                    _agencyNameScratch[compareIndex] = agencyNameToSwap;
                }
            }

            for (int payoutIndex = 0; payoutIndex < _payoutScratch.Length; payoutIndex++)
            {
                if (_payoutScratch[payoutIndex] <= 0.0)
                {
                    continue;
                }

                GUILayout.Label(
                    _agencyNameScratch[payoutIndex]
                    + " Next Payout: "
                    + _payoutScratch[payoutIndex].ToString("N0"));
            }
        }

        private void DrawRivalAgencies()
        {
            GUILayout.Label("RIVAL AGENCIES - " + FormatNextFundingDate());
            GUILayout.Space(8.0f);

            _rivalsScrollPosition = GUILayout.BeginScrollView(_rivalsScrollPosition);

            for (int agencyIndex = 0; agencyIndex < _campaignController.RivalAgencies.Count; agencyIndex++)
            {
                AgencyState agency = _campaignController.RivalAgencies[agencyIndex];
                if (agencyIndex > 0)
                {
                    GUILayout.Space(10.0f);
                }

                DrawProgramCard(
                    agency,
                    _campaignController.GetEstimatedRivalMissionDays(agency),
                    _campaignController.GetRivalMissionProgressCost(agency));
            }

            GUILayout.EndScrollView();
        }

        private void DrawHelpGuide()
        {
            _helpScrollPosition = GUILayout.BeginScrollView(_helpScrollPosition);

            GUILayout.BeginVertical("box");
            GUILayout.Label("HELP / PLAYER GUIDE");
            GUILayout.Label(
                "Here below is a list of all the funding targets available. These are paid out by the Corporations and Nations of Kerbin. But there are rival companies out to get a piece of the funding. You will need to achieve the targets before they do to get the largest share of the funding.");

            GUILayout.Space(8.0f);
            GUILayout.Label("Types of Funding Contract", _boldLabelStyle);
            GUILayout.Label("1. One off achievements");
            GUILayout.Space(4.0f);
            GUILayout.Label(
                "Once any agency achieves this objectiveCompletion there are 10 payouts given. As interest in this objectiveCompletion is lost overtime each following payout is reduced by 10%. If multiple agencies complete the objectiveCompletion than the next payout is split between those agencies. Being the first to reach the objectiveCompletion will maximise your payout.");

            GUILayout.Space(8.0f);
            GUILayout.Label("2. Satellite Contracts");
            GUILayout.Space(4.0f);
            GUILayout.Label(
                "Funding is given for the number of satellite in orbit of the body. This is a fixed contract and will always pay out. Once the maximum number of satellites is meet which ever agencies has the biggest share of satellites will get the bigger share of the payout.");

            GUILayout.Space(8.0f);
            GUILayout.Label("PreOrbit Contracts", _boldLabelStyle);
            GUILayout.Label(
                "The campaign begins with Directed Power I, Mass I, Control I and Biome I offered. The remaining sixteen pre-orbit contracts use the normal Offered, Unlocked, Locked and Expired states. Any agency, including a rival, completing a level unlocks the next level in that same line. Every pre-orbit contract that is Offered and unfinished for you is active independently, so one flight may satisfy more than one offered contract. Completing level five in any one line unlocks Probe Orbit for the race.");

            GUILayout.Space(8.0f);
            GUILayout.Label("Unlocking New Funding Target", _boldLabelStyle);
            GUILayout.Label(
                "New funding targets can be unlocked by meeting the requirements of the funding targets currently available. Look down the list below to see all of the available funding targets:");
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }

        private void DrawContractCatalogue()
        {
            double currentUniversalTime = Planetarium.fetch == null
                ? -1.0
                : Planetarium.GetUniversalTime();
            if (double.IsNaN(currentUniversalTime)
                || double.IsInfinity(currentUniversalTime)
                || currentUniversalTime < 0.0)
            {
                currentUniversalTime = -1.0;
            }

            EnsurePayoutScratchBuffers();
            EnsureContractCatalogueFundingEntries();

            GUILayout.Label("Current Funding Info", _boldLabelStyle);
            ContractCatalogueFundingEntry selectedEntry = EnsureSelectedContractCatalogueFundingEntry();

            // Keep the selected contract area stable as users switch between contracts with
            // different amounts of descriptive or unlock text. Longer cards scroll internally
            // instead of pushing the funding button sections up and down.
            _contractCatalogueFundingInfoScrollPosition = GUILayout.BeginScrollView(
                _contractCatalogueFundingInfoScrollPosition,
                ContractCatalogueFundingInfoOptions);
            if (selectedEntry == null)
            {
                GUILayout.Label("No funding contracts configured.");
            }
            else
            {
                DrawSelectedContractCatalogueFundingEntry(selectedEntry, currentUniversalTime);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(12.0f);

            // Keep the selected funding information pinned while the catalogue sections below
            // scroll independently through the larger stock-body contract list.
            _contractCatalogueScrollPosition = GUILayout.BeginScrollView(_contractCatalogueScrollPosition);
            DrawContractCatalogueFundingSection(
                "Offered",
                ContractCatalogueFundingCategory.Offered,
                ref _contractCatalogueOfferedExpanded);
            DrawContractCatalogueFundingSection(
                "Unlocked",
                ContractCatalogueFundingCategory.Unlocked,
                ref _contractCatalogueUnlockedExpanded);
            DrawContractCatalogueFundingSection(
                "Locked",
                ContractCatalogueFundingCategory.Locked,
                ref _contractCatalogueLockedExpanded);
            DrawContractCatalogueFundingSection(
                "Expired",
                ContractCatalogueFundingCategory.Expired,
                ref _contractCatalogueExpiredExpanded);

            GUILayout.EndScrollView();
        }

        private void DrawFlightContractProgress(
            ObjectiveDefinition currentMilestone,
            FlightContractTracker tracker)
        {
            GUILayout.Space(4.0f);
            GUILayout.Label("Live Flight", _boldLabelStyle);

            bool isKerbin = string.Equals(
                tracker.CelestialBodyName,
                "Kerbin",
                StringComparison.OrdinalIgnoreCase);
            if (!isKerbin)
            {
                GUILayout.Label("Attempt status: INVALID - active vessel is not on Kerbin.");
            }

            if (tracker.EnteredOrbit)
            {
                GUILayout.Label("Attempt status: INVALID - vessel entered orbit.");
            }

            if (currentMilestone.PreOrbitLine == PreOrbitContractLine.DirectedPower)
            {
                bool speedMet = tracker.MaximumSurfaceSpeedMetersPerSecond
                    >= currentMilestone.RequiredSpeedMetersPerSecond;
                bool altitudeValid = tracker.MaximumAltitudeMeters
                    <= currentMilestone.MaximumAltitudeMeters;

                GUILayout.Label(
                    "Current Speed: "
                    + tracker.CurrentSurfaceSpeedMetersPerSecond.ToString("N0")
                    + " m/s");
                GUILayout.Label(
                    "Max Speed: "
                    + tracker.MaximumSurfaceSpeedMetersPerSecond.ToString("N0")
                    + " / "
                    + currentMilestone.RequiredSpeedMetersPerSecond.ToString("N0")
                    + " m/s - "
                    + (speedMet ? "MET" : "PENDING"));
                GUILayout.Label(
                    "Max Altitude: "
                    + (tracker.MaximumAltitudeMeters / 1000.0).ToString("N1")
                    + " / "
                    + (currentMilestone.MaximumAltitudeMeters / 1000.0).ToString("N0")
                    + " km - "
                    + (altitudeValid ? "VALID" : "INVALID"));
                GUILayout.Label(
                    "Kerbin Impact: "
                    + (isKerbin && !tracker.EnteredOrbit && speedMet && altitudeValid
                        ? "READY - impact Kerbin to complete"
                        : "PENDING"));
                return;
            }

            if (currentMilestone.PreOrbitLine == PreOrbitContractLine.Mass)
            {
                bool massMet = tracker.CurrentMassTonnes >= currentMilestone.RequiredMassTonnes;
                bool distanceMet = tracker.CurrentDistanceMeters >= currentMilestone.RequiredDistanceMeters;
                bool landed = tracker.CurrentSituation == FlightSituation.Landed;

                GUILayout.Label(
                    "Mass: "
                    + tracker.CurrentMassTonnes.ToString("N1")
                    + " / "
                    + currentMilestone.RequiredMassTonnes.ToString("N1")
                    + " t - "
                    + (massMet ? "MET" : "PENDING"));
                GUILayout.Label(
                    "Distance: "
                    + (tracker.CurrentDistanceMeters / 1000.0).ToString("N0")
                    + " / "
                    + (currentMilestone.RequiredDistanceMeters / 1000.0).ToString("N0")
                    + " km - "
                    + (distanceMet ? "MET" : "PENDING"));
                GUILayout.Label("Landed: " + (landed ? "YES - MET" : "NO - PENDING"));
                return;
            }

            if (currentMilestone.PreOrbitLine == PreOrbitContractLine.Control)
            {
                bool holdQualified = tracker.IsControlMilestoneQualified(currentMilestone.Id);
                bool sampleInBand = tracker.IsControlSampleInBand(currentMilestone.Id);
                bool hasCrew = tracker.CurrentCrewCount > 0;

                GUILayout.Label(
                    "Altitude: "
                    + (tracker.CurrentAltitudeMeters / 1000.0).ToString("N1")
                    + " km ("
                    + (currentMilestone.MinimumAltitudeMeters / 1000.0).ToString("N0")
                    + "-"
                    + (currentMilestone.MaximumAltitudeMeters / 1000.0).ToString("N0")
                    + " km) - "
                    + (holdQualified ? "HOLD COMPLETE" : (sampleInBand ? "IN BAND" : "OUT OF BAND")));
                GUILayout.Label(
                    "Hold: "
                    + tracker.GetControlHoldSeconds(currentMilestone.Id).ToString("N0")
                    + " / "
                    + currentMilestone.RequiredDurationSeconds.ToString("N0")
                    + " s - "
                    + (holdQualified ? "MET" : "PENDING"));
                GUILayout.Label(
                    "Crew aboard: "
                    + tracker.CurrentCrewCount
                    + " - "
                    + (hasCrew ? "MET" : "REQUIRED"));
                GUILayout.Label(
                    "Safe Kerbin landing: "
                    + (holdQualified && hasCrew
                        ? "READY - land safely to complete"
                        : "PENDING"));
                return;
            }

            if (currentMilestone.PreOrbitLine == PreOrbitContractLine.Biome)
            {
                string currentBiome = string.IsNullOrEmpty(tracker.CurrentBiomeName)
                    ? "Unknown"
                    : tracker.CurrentBiomeName;
                bool biomeMatched = string.Equals(
                    currentBiome,
                    currentMilestone.RequiredBiomeName,
                    StringComparison.OrdinalIgnoreCase);
                bool landed = tracker.CurrentSituation == FlightSituation.Landed;

                GUILayout.Label("Current Biome: " + currentBiome);
                GUILayout.Label("Target: " + currentMilestone.RequiredBiomeName);
                GUILayout.Label("Biome Match: " + (biomeMatched ? "YES - MET" : "NO - PENDING"));
                GUILayout.Label("Landed: " + (landed ? "YES - MET" : "NO - PENDING"));
            }
        }

        private void DrawSelectedContractCatalogueFundingEntry(
            ContractCatalogueFundingEntry entry,
            double evaluationUniversalTime)
        {
            ContractCatalogueFundingCategory category = GetContractCatalogueFundingCategory(entry);
            string stateLabel;
            string stateMessage = null;

            if (category == ContractCatalogueFundingCategory.Offered)
            {
                stateLabel = "Offered";
            }
            else if (category == ContractCatalogueFundingCategory.Unlocked)
            {
                stateLabel = "Unlocked";
                stateMessage = "Funding requirements have been met. Waiting for a future funding review.";
            }
            else if (category == ContractCatalogueFundingCategory.Locked)
            {
                stateLabel = "Locked";
            }
            else
            {
                stateLabel = "Expired";
            }

            double? unlockEvaluationUniversalTime = category == ContractCatalogueFundingCategory.Locked
                ? (double?)evaluationUniversalTime
                : null;

            if (entry.IsAchievement)
            {
                DrawAchievementFundingCard(
                    entry.AchievementProgramme,
                    stateLabel,
                    unlockEvaluationUniversalTime,
                    stateMessage,
                    showFundingLifecycleDetails: false);
            }
            else
            {
                DrawSatelliteFundingCard(
                    entry.SatelliteProgramme,
                    stateLabel,
                    unlockEvaluationUniversalTime,
                    stateMessage);
            }
        }

        private void DrawContractCatalogueFundingSection(
            string heading,
            ContractCatalogueFundingCategory category,
            ref bool isExpanded)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(heading, _boldLabelStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(isExpanded ? "-" : "+", ContractCatalogueSectionToggleOptions))
            {
                isExpanded = !isExpanded;
            }
            GUILayout.EndHorizontal();

            if (!isExpanded)
            {
                GUILayout.Space(8.0f);
                return;
            }

            int buttonCount = 0;
            bool hasOpenRow = false;

            for (int entryIndex = 0; entryIndex < _contractCatalogueFundingEntries.Count; entryIndex++)
            {
                ContractCatalogueFundingEntry entry = _contractCatalogueFundingEntries[entryIndex];
                if (GetContractCatalogueFundingCategory(entry) != category)
                {
                    continue;
                }

                if ((buttonCount % ContractCatalogueFundingButtonsPerRow) == 0)
                {
                    GUILayout.BeginHorizontal();
                    hasOpenRow = true;
                }

                bool isSelected = string.Equals(
                        _selectedContractCatalogueFundingId,
                        entry.Id,
                        StringComparison.Ordinal)
                    && _selectedContractCatalogueFundingIsAchievement == entry.IsAchievement;
                string buttonLabel = isSelected ? "> " + entry.Name : entry.Name;

                if (GUILayout.Button(buttonLabel, ContractCatalogueFundingButtonOptions))
                {
                    _selectedContractCatalogueFundingId = entry.Id;
                    _selectedContractCatalogueFundingIsAchievement = entry.IsAchievement;
                    _contractCatalogueFundingInfoScrollPosition = Vector2.zero;
                }

                buttonCount++;
                if ((buttonCount % ContractCatalogueFundingButtonsPerRow) == 0)
                {
                    GUILayout.EndHorizontal();
                    hasOpenRow = false;
                }
            }

            if (hasOpenRow)
            {
                GUILayout.EndHorizontal();
            }

            if (buttonCount == 0)
            {
                GUILayout.Label("None");
            }

            GUILayout.Space(8.0f);
        }

        private void EnsureContractCatalogueFundingEntries()
        {
            int achievementFundingCount = _campaignController.ObjectiveFundingContracts.Count;
            int satelliteFundingCount = _campaignController.SatelliteNetworkFundingContracts.Count;
            if (ReferenceEquals(_contractCatalogueFundingEntriesController, _campaignController)
                && _contractCatalogueAchievementFundingCount == achievementFundingCount
                && _contractCatalogueSatelliteFundingCount == satelliteFundingCount)
            {
                return;
            }

            _contractCatalogueFundingEntries.Clear();

            for (int programmeIndex = 0; programmeIndex < achievementFundingCount; programmeIndex++)
            {
                ObjectiveFundingContract programme =
                    _campaignController.ObjectiveFundingContracts[programmeIndex];
                ObjectiveDefinition objective = ObjectiveCatalogue.FindById(programme.Id);
                string celestialBodyName = objective == null ? null : objective.CelestialBodyName;
                _contractCatalogueFundingEntries.Add(new ContractCatalogueFundingEntry(
                    programme,
                    null,
                    celestialBodyName,
                    KspCelestialBodyOrdering.GetSortDistanceFromKerbin(celestialBodyName),
                    programmeIndex));
            }

            for (int programmeIndex = 0; programmeIndex < satelliteFundingCount; programmeIndex++)
            {
                SatelliteNetworkFundingContract programme = _campaignController.SatelliteNetworkFundingContracts[programmeIndex];
                _contractCatalogueFundingEntries.Add(new ContractCatalogueFundingEntry(
                    null,
                    programme,
                    programme.CelestialBodyName,
                    KspCelestialBodyOrdering.GetSortDistanceFromKerbin(programme.CelestialBodyName),
                    achievementFundingCount + programmeIndex));
            }

            _contractCatalogueFundingEntries.Sort(CompareContractCatalogueFundingEntries);
            _contractCatalogueFundingEntriesController = _campaignController;
            _contractCatalogueAchievementFundingCount = achievementFundingCount;
            _contractCatalogueSatelliteFundingCount = satelliteFundingCount;
        }

        private static int CompareContractCatalogueFundingEntries(
            ContractCatalogueFundingEntry firstEntry,
            ContractCatalogueFundingEntry secondEntry)
        {
            int distanceComparison = firstEntry.BodySortDistance.CompareTo(secondEntry.BodySortDistance);
            if (distanceComparison != 0)
            {
                return distanceComparison;
            }

            int bodyNameComparison = string.Compare(
                firstEntry.CelestialBodyName,
                secondEntry.CelestialBodyName,
                StringComparison.OrdinalIgnoreCase);
            if (bodyNameComparison != 0)
            {
                return bodyNameComparison;
            }

            return firstEntry.CatalogueOrder.CompareTo(secondEntry.CatalogueOrder);
        }

        private ContractCatalogueFundingEntry EnsureSelectedContractCatalogueFundingEntry()
        {
            for (int entryIndex = 0; entryIndex < _contractCatalogueFundingEntries.Count; entryIndex++)
            {
                ContractCatalogueFundingEntry entry = _contractCatalogueFundingEntries[entryIndex];
                if (string.Equals(
                        _selectedContractCatalogueFundingId,
                        entry.Id,
                        StringComparison.Ordinal)
                    && _selectedContractCatalogueFundingIsAchievement == entry.IsAchievement)
                {
                    return entry;
                }
            }

            for (int categoryIndex = (int)ContractCatalogueFundingCategory.Offered;
                categoryIndex <= (int)ContractCatalogueFundingCategory.Expired;
                categoryIndex++)
            {
                ContractCatalogueFundingCategory category = (ContractCatalogueFundingCategory)categoryIndex;
                for (int entryIndex = 0; entryIndex < _contractCatalogueFundingEntries.Count; entryIndex++)
                {
                    ContractCatalogueFundingEntry entry = _contractCatalogueFundingEntries[entryIndex];
                    if (GetContractCatalogueFundingCategory(entry) != category)
                    {
                        continue;
                    }

                    _selectedContractCatalogueFundingId = entry.Id;
                    _selectedContractCatalogueFundingIsAchievement = entry.IsAchievement;
                    return entry;
                }
            }

            _selectedContractCatalogueFundingId = null;
            return null;
        }

        private ContractCatalogueFundingCategory GetContractCatalogueFundingCategory(ContractCatalogueFundingEntry entry)
        {
            if (entry.IsAchievement)
            {
                ObjectiveFundingContract programme = entry.AchievementProgramme;
                if (programme.IsExpired)
                {
                    return ContractCatalogueFundingCategory.Expired;
                }

                if (programme.IsOffered)
                {
                    return ContractCatalogueFundingCategory.Offered;
                }

                return _campaignController.IsAchievementProgrammeAvailable(programme)
                    ? ContractCatalogueFundingCategory.Unlocked
                    : ContractCatalogueFundingCategory.Locked;
            }

            SatelliteNetworkFundingContract satelliteProgramme = entry.SatelliteProgramme;
            if (satelliteProgramme.IsOffered)
            {
                return ContractCatalogueFundingCategory.Offered;
            }

            return satelliteProgramme.IsAvailable
                ? ContractCatalogueFundingCategory.Unlocked
                : ContractCatalogueFundingCategory.Locked;
        }

        private void DrawUnlockRuleProgress(
            UnlockRuleDefinition rule,
            double evaluationUniversalTime)
        {
            GUILayout.Space(4.0f);
            GUILayout.Label("Unlock requirements:", _boldLabelStyle);

            if (rule == null)
            {
                GUILayout.Label("[x] Available from campaign start");
                return;
            }

            if (rule.Paths.Count == 0)
            {
                GUILayout.Label("[ ] No valid unlock paths");
                return;
            }

            for (int pathIndex = 0; pathIndex < rule.Paths.Count; pathIndex++)
            {
                if (pathIndex > 0)
                {
                    GUILayout.Space(4.0f);
                    GUILayout.Label("Or", _boldLabelStyle);
                }

                if (rule.Paths.Count > 1)
                {
                    GUILayout.Label("Unlock Path " + (pathIndex + 1), _boldLabelStyle);
                }

                UnlockPathDefinition path = rule.Paths[pathIndex];
                if (path == null || path.Conditions.Count == 0)
                {
                    GUILayout.Label("[ ] No valid unlock conditions");
                    continue;
                }

                for (int conditionIndex = 0; conditionIndex < path.Conditions.Count; conditionIndex++)
                {
                    DrawUnlockConditionProgress(
                        path.Conditions[conditionIndex],
                        evaluationUniversalTime);
                }
            }
        }

        private void DrawUnlockConditionProgress(
            UnlockConditionDefinition condition,
            double evaluationUniversalTime)
        {
            bool isSatisfied = UnlockRuleEvaluator.IsConditionSatisfied(
                condition,
                _campaignController.Agencies,
                evaluationUniversalTime);

            _listTextBuilder.Length = 0;
            _listTextBuilder.Append(isSatisfied ? "[x] " : "[ ] ");

            if (condition == null)
            {
                _listTextBuilder.Append("Invalid unlock condition");
            }
            else if (condition.ConditionType == UnlockConditionType.ObjectiveCompletion)
            {
                AppendAchievementConditionText(
                    condition,
                    isSatisfied,
                    evaluationUniversalTime);
            }
            else if (condition.ConditionType == UnlockConditionType.SatelliteCount)
            {
                int satelliteCount = UnlockRuleEvaluator.GetSatelliteCount(
                    condition,
                    _campaignController.Agencies);
                _listTextBuilder.Append(condition.CelestialBodyName);
                _listTextBuilder.Append(" satellite network: ");
                _listTextBuilder.Append(satelliteCount);
                _listTextBuilder.Append(" / ");
                _listTextBuilder.Append(condition.RequiredSatelliteCount);
                _listTextBuilder.Append(" qualifying satellites");
            }
            else if (condition.ConditionType == UnlockConditionType.UniversalTime)
            {
                if (isSatisfied)
                {
                    _listTextBuilder.Append("Campaign date reached: ");
                    _listTextBuilder.Append(FormatKerbinDate(condition.RequiredUniversalTime));
                }
                else
                {
                    int remainingDays = (int)Math.Ceiling(
                        Math.Max(
                            0.0,
                            condition.RequiredUniversalTime - evaluationUniversalTime)
                        / KerbinDaySeconds);
                    _listTextBuilder.Append("Available from ");
                    _listTextBuilder.Append(FormatKerbinDate(condition.RequiredUniversalTime));
                    _listTextBuilder.Append(" - ");
                    _listTextBuilder.Append(remainingDays);
                    _listTextBuilder.Append(remainingDays == 1 ? " day remaining" : " days remaining");
                }
            }
            else
            {
                _listTextBuilder.Append("Unknown unlock condition");
            }

            GUILayout.Label(_listTextBuilder.ToString());
        }

        private void AppendAchievementConditionText(
            UnlockConditionDefinition condition,
            bool isSatisfied,
            double evaluationUniversalTime)
        {
            int satisfiedProgramCount = UnlockRuleEvaluator.GetSatisfiedProgramCount(
                condition,
                _campaignController.Agencies,
                evaluationUniversalTime);
            string milestoneName = GetMilestoneDisplayName(condition.ObjectiveId);

            if (condition.RequiredAgencyCount <= 1)
            {
                switch (condition.AgencyScope)
                {
                    case UnlockAgencyScope.Player:
                        _listTextBuilder.Append("You achieve ");
                        break;

                    case UnlockAgencyScope.AnyRival:
                        _listTextBuilder.Append("Any rival agency achieves ");
                        break;

                    case UnlockAgencyScope.AnyAgency:
                    default:
                        _listTextBuilder.Append("Any agency achieves ");
                        break;
                }

                _listTextBuilder.Append(milestoneName);

                if (isSatisfied && condition.AgencyScope != UnlockAgencyScope.Player)
                {
                    AgencyState satisfyingProgram = FindFirstProgramSatisfyingCondition(
                        condition,
                        evaluationUniversalTime);
                    if (satisfyingProgram != null)
                    {
                        _listTextBuilder.Append(" - ");
                        _listTextBuilder.Append(GetProgramDisplayName(satisfyingProgram));
                    }
                }

                return;
            }

            _listTextBuilder.Append(satisfiedProgramCount);
            _listTextBuilder.Append(" / ");
            _listTextBuilder.Append(condition.RequiredAgencyCount);

            switch (condition.AgencyScope)
            {
                case UnlockAgencyScope.AnyRival:
                    _listTextBuilder.Append(" rival agencies have achieved ");
                    break;

                case UnlockAgencyScope.Player:
                    _listTextBuilder.Append(" player objectiveCompletion count for ");
                    break;

                case UnlockAgencyScope.AnyAgency:
                default:
                    _listTextBuilder.Append(" agencies have achieved ");
                    break;
            }

            _listTextBuilder.Append(milestoneName);
        }

        private AgencyState FindFirstProgramSatisfyingCondition(
            UnlockConditionDefinition condition,
            double evaluationUniversalTime)
        {
            for (int agencyIndex = 0; agencyIndex < _campaignController.Agencies.Count; agencyIndex++)
            {
                AgencyState agency = _campaignController.Agencies[agencyIndex];
                if (UnlockRuleEvaluator.DoesProgramSatisfyAchievementCondition(
                    agency,
                    condition,
                    evaluationUniversalTime))
                {
                    return agency;
                }
            }

            return null;
        }

        private static string GetMilestoneDisplayName(string objectiveId)
        {
            ObjectiveDefinition objective = ObjectiveCatalogue.FindById(objectiveId);
            return objective == null || string.IsNullOrEmpty(objective.Name)
                ? objectiveId
                : objective.Name;
        }

        private void DrawProgramCard(
            AgencyState agency,
            int? launchEtaDays,
            double launchProgressCostFunds)
        {
            GUILayout.BeginVertical("box");
            DrawCenteredCardTitle(agency.Name);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(RivalDetailsOptions);
            GUILayout.Label(string.Empty);
            GUILayout.Label("Funds: " + agency.Funds.ToString("N0"));
            GUILayout.Label(
                "Next Mission Planned: "
                + (string.IsNullOrEmpty(agency.NextMissionDisplayName) ? "Planning" : agency.NextMissionDisplayName));
            GUILayout.Label("Mission Progress: " + agency.MissionProgressPercent + "%");
            GUILayout.Label(
                "Mission Progress Cost: "
                + launchProgressCostFunds.ToString("N0")
                + " ("
                + TheRaceForSpace.Rivals.RivalSimulation.CalculateLaunchProgressIncrementPercent(agency)
                + "% +)");

            string launchEtaText = "Awaiting Funding";
            if (launchEtaDays.HasValue)
            {
                if (launchEtaDays.Value <= 20)
                {
                    launchEtaText = "Next launch imminent";
                }
                else
                {
                    // Rival funding arrives in large scheduled steps, so displaying a rounded-up
                    // ten-day estimate is clearer than implying day-level precision.
                    int roundedLaunchEtaDays = ((launchEtaDays.Value + 9) / 10) * 10;
                    launchEtaText = roundedLaunchEtaDays + " days";
                }
            }

            GUILayout.Label("ETA till Completion: " + launchEtaText);
            GUILayout.EndVertical();

            GUILayout.Space(24.0f);
            GUILayout.BeginVertical(RivalIncomeLabelOptions);
            GUILayout.Label(string.Empty);
            GUILayout.Label("Base Income");

            for (int programmeIndex = 0;
                programmeIndex < _campaignController.ObjectiveFundingContracts.Count;
                programmeIndex++)
            {
                ObjectiveFundingContract programme =
                    _campaignController.ObjectiveFundingContracts[programmeIndex];
                if (programme.IsExpired
                    || !_campaignController.IsAchievementProgrammeAvailable(programme)
                    || !_campaignController.HasProgramAchieved(agency, programme))
                {
                    continue;
                }

                GUILayout.Label(programme.Name + ": Completed");
            }

            for (int programmeIndex = 0;
                programmeIndex < _campaignController.SatelliteNetworkFundingContracts.Count;
                programmeIndex++)
            {
                SatelliteNetworkFundingContract programme = _campaignController.SatelliteNetworkFundingContracts[programmeIndex];
                int satelliteCount = agency.GetSatelliteCount(programme.CelestialBodyName);
                double nextPayout = _campaignController.GetSatelliteCurrentPayout(agency, programme);
                if (satelliteCount <= 0 || nextPayout <= 0.0)
                {
                    continue;
                }

                GUILayout.Label(programme.CelestialBodyName + " Satellites " + satelliteCount);
            }

            GUILayout.Space(6.0f);
            GUILayout.Label("Total Next Payout", _boldLabelStyle);
            GUILayout.EndVertical();

            GUILayout.Space(2.0f);
            GUILayout.BeginVertical(RivalIncomeAmountOptions);
            GUILayout.Label(string.Empty);
            GUILayout.Label(_campaignController.RivalBaseIncomePerFundingPeriod.ToString("N0"));

            for (int programmeIndex = 0;
                programmeIndex < _campaignController.ObjectiveFundingContracts.Count;
                programmeIndex++)
            {
                ObjectiveFundingContract programme =
                    _campaignController.ObjectiveFundingContracts[programmeIndex];
                if (programme.IsExpired
                    || !_campaignController.IsAchievementProgrammeAvailable(programme)
                    || !_campaignController.HasProgramAchieved(agency, programme))
                {
                    continue;
                }

                double nextPayout = _campaignController.GetAchievementCurrentPayout(agency, programme);
                GUILayout.Label(nextPayout > 0.0 ? nextPayout.ToString("N0") : string.Empty);
            }

            for (int programmeIndex = 0;
                programmeIndex < _campaignController.SatelliteNetworkFundingContracts.Count;
                programmeIndex++)
            {
                SatelliteNetworkFundingContract programme = _campaignController.SatelliteNetworkFundingContracts[programmeIndex];
                int satelliteCount = agency.GetSatelliteCount(programme.CelestialBodyName);
                double nextPayout = _campaignController.GetSatelliteCurrentPayout(agency, programme);
                if (satelliteCount <= 0 || nextPayout <= 0.0)
                {
                    continue;
                }

                GUILayout.Label(nextPayout.ToString("N0"));
            }

            GUILayout.Space(6.0f);
            GUILayout.Label(agency.NextPayoutFunds.ToString("N0"), _boldLabelStyle);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void EnsurePayoutScratchBuffers()
        {
            int programCount = _campaignController == null ? 0 : _campaignController.Agencies.Count;
            if (_payoutScratch == null || _payoutScratch.Length != programCount)
            {
                _payoutScratch = new double[programCount];
            }

            if (_agencyNameScratch == null || _agencyNameScratch.Length != programCount)
            {
                _agencyNameScratch = new string[programCount];
            }
        }

        private static string GetProgramDisplayName(AgencyState agency)
        {
            if (agency == null)
            {
                return "Unknown";
            }

            return agency.IsPlayer ? "Player" : agency.Name;
        }

        private void DrawCenteredCardTitle(string title)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(title, _highlightedCardTitleStyle, NoExpandWidthOptions);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private string FormatNextFundingDate()
        {
            if (_campaignController.NextFundingYear <= 0)
            {
                return "Next Funding Date: Pending";
            }

            int daysUntilNextFunding = _campaignController.DaysUntilNextFunding;
            string daysUntilNextFundingText = daysUntilNextFunding == 1
                ? "1 Day to go"
                : daysUntilNextFunding + " Days to go";

            return "Next Funding Date: "
                + FormatKerbinDate(_campaignController.NextFundingUniversalTime)
                + " - "
                + daysUntilNextFundingText;
        }

        private string FormatKerbinDate(double universalTime)
        {
            if (double.IsNaN(universalTime)
                || double.IsInfinity(universalTime)
                || universalTime < 0.0)
            {
                return "Pending";
            }

            return "Year "
                + _campaignController.GetKerbinYear(universalTime)
                + ", Day "
                + _campaignController.GetKerbinDay(universalTime);
        }
    }
}

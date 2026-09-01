using System;
using System.Collections.Generic;
using System.Text;
using KSP.UI.Screens;
using TheRaceForSpace.Competition;
using TheRaceForSpace.Core;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Milestones;
using TheRaceForSpace.Programs;
using UnityEngine;

namespace TheRaceForSpace.UI
{
    /// <summary>
    /// Prototype command center with four switchable views inside one interface window.
    /// Press F8 or use the stock KSP application launcher button to show or hide the interface.
    /// Race progression and controller lifetime are owned by RaceRuntime in the Core module.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class RaceWindow : MonoBehaviour
    {
        private enum ActiveView
        {
            Overview,
            FundingTargets,
            RivalAgencies,
            SpaceRace
        }

        private enum SpaceRaceFundingCategory
        {
            Available,
            Locked,
            Expired
        }

        private sealed class SpaceRaceFundingEntry
        {
            public SpaceRaceFundingEntry(
                AchievementFundingProgramme achievementProgramme,
                FundingProgramme satelliteProgramme,
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

            public AchievementFundingProgramme AchievementProgramme { get; private set; }
            public FundingProgramme SatelliteProgramme { get; private set; }
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
        private const float SpaceRaceFundingInfoHeight = 240.0f;
        private const int HighlightedCardTitleFontSize = 16;
        private const int SpaceRaceFundingButtonsPerRow = 4;
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
        private static readonly GUILayoutOption[] SpaceRaceFundingButtonOptions =
            { GUILayout.Width(195.0f), GUILayout.Height(40.0f) };
        private static readonly GUILayoutOption[] SpaceRaceSectionToggleOptions =
            { GUILayout.Width(32.0f), GUILayout.Height(24.0f) };
        private static readonly GUILayoutOption[] SpaceRaceFundingInfoOptions =
            { GUILayout.Height(SpaceRaceFundingInfoHeight) };
        private static readonly GUILayoutOption[] ExpandWidthOptions = { GUILayout.ExpandWidth(true) };
        private static readonly GUILayoutOption[] NoExpandWidthOptions = { GUILayout.ExpandWidth(false) };

        private static RaceWindow _activeInstance;
        private static ActiveView _activeView = ActiveView.Overview;
        private static Game _windowPositionGame;
        private static Rect _windowRect;

        // This is a non-owning reference to the current Core runtime controller. RaceWindow never
        // creates or advances the controller; it only reads the state needed for presentation.
        private SatelliteRaceController _raceController;
        private Game _visibilityGame;

        private readonly StringBuilder _listTextBuilder = new StringBuilder(128);
        private readonly List<SpaceRaceFundingEntry> _spaceRaceFundingEntries =
            new List<SpaceRaceFundingEntry>();
        private Vector2 _fundingScrollPosition;
        private Vector2 _rivalsScrollPosition;
        private Vector2 _spaceRaceScrollPosition;
        private Vector2 _spaceRaceFundingInfoScrollPosition;
        private Vector2 _helpScrollPosition;
        private ApplicationLauncherButton _launcherButton;
        private GUIStyle _highlightedCardTitleStyle;
        private GUIStyle _boldLabelStyle;
        private GUI.WindowFunction _drawWindowFunction;
        private double[] _payoutScratch;
        private string[] _agencyNameScratch;
        private SatelliteRaceController _spaceRaceFundingEntriesController;
        private int _spaceRaceAchievementFundingCount = -1;
        private int _spaceRaceSatelliteFundingCount = -1;
        private string _selectedSpaceRaceFundingId;
        private bool _selectedSpaceRaceFundingIsAchievement;
        private bool _spaceRaceAvailableExpanded = true;
        private bool _spaceRaceLockedExpanded;
        private bool _spaceRaceExpiredExpanded;
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
            // sub-scenes are loading. Only one RaceWindow may own Update/OnGUI at a time.
            if (_activeInstance != null && _activeInstance != this)
            {
                _isDuplicateInstance = true;
                Destroy(this);
                return;
            }

            _activeInstance = this;
            _drawWindowFunction = DrawWindow;

            // Scene changes recreate RaceWindow, so position and selected tab are static for the
            // current game. A different save starts from the normal centered Overview state.
            if (_windowPositionGame != HighLogic.CurrentGame)
            {
                _windowPositionGame = HighLogic.CurrentGame;
                _windowRect = CreateCenteredWindowRect();
                _activeView = ActiveView.Overview;
            }

            _visibilityGame = HighLogic.CurrentGame;
            _raceController = RaceRuntime.Controller;
        }

        public void OnDestroy()
        {
            // ApplicationLauncher is recreated by KSP across some scene/menu transitions, so
            // remove our button whenever this RaceWindow instance stops owning the interface.
            if (_launcherButton != null && ApplicationLauncher.Instance != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_launcherButton);
            }

            _launcherButton = null;
            _raceController = null;
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
            _raceController = RaceRuntime.Controller;
            if (_raceController == null)
            {
                return;
            }

            // Wait for the ScenarioModule before creating or synchronizing the launcher button.
            // This prevents the new-save default (closed) from overwriting a saved open state.
            if (!_hasRestoredVisibilityState)
            {
                bool savedVisibility;
                if (!RacePersistenceScenario.TryRestoreCommandCenterVisibility(out savedVisibility))
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
                RacePersistenceScenario.CaptureCommandCenterVisibility(_isVisible);
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
                || _raceController == null)
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

            if (GUILayout.Button("Space Race", TabButtonOptions))
            {
                _activeView = ActiveView.SpaceRace;
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

                    case ActiveView.SpaceRace:
                        DrawSpaceRace();
                        break;
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("F8: show/hide interface");
            GUI.DragWindow();
        }

        private void DrawOverview()
        {
            SpaceProgramState player = _raceController.PlayerProgram;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(OverviewObjectivesOptions);
            GUILayout.Label("Your Objectives", _boldLabelStyle);

            for (int programmeIndex = 0;
                programmeIndex < _raceController.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    _raceController.AchievementFundingProgrammes[programmeIndex];
                if (programme.IsExpired || !_raceController.IsAchievementProgrammeAvailable(programme))
                {
                    continue;
                }

                GUILayout.Label(
                    programme.Name
                    + ": "
                    + (_raceController.HasProgramAchieved(player, programme) ? "ACHIEVED" : "IN PROGRESS"));
            }

            GUILayout.Space(10.0f);
            GUILayout.Label("Your Satellite Networks", _boldLabelStyle);

            for (int programmeIndex = 0;
                programmeIndex < _raceController.FundingProgrammes.Count;
                programmeIndex++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[programmeIndex];
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
                programmeIndex < _raceController.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    _raceController.AchievementFundingProgrammes[programmeIndex];
                double nextPayout = _raceController.GetAchievementCurrentPayout(player, programme);
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
                programmeIndex < _raceController.FundingProgrammes.Count;
                programmeIndex++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[programmeIndex];
                int satelliteCount = player.GetSatelliteCount(programme.CelestialBodyName);
                double nextPayout = _raceController.GetSatelliteCurrentPayout(player, programme);
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

            for (int i = 0; i < _raceController.AchievementFundingProgrammes.Count; i++)
            {
                AchievementFundingProgramme programme = _raceController.AchievementFundingProgrammes[i];
                if (programme.IsExpired || !_raceController.IsAchievementProgrammeAvailable(programme))
                {
                    continue;
                }

                DrawAchievementFundingCard(programme);
                GUILayout.Space(8.0f);
            }

            for (int i = 0; i < _raceController.FundingProgrammes.Count; i++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[i];
                if (!programme.IsAvailable)
                {
                    continue;
                }

                DrawSatelliteFundingCard(programme);
                GUILayout.Space(8.0f);
            }

            GUILayout.EndScrollView();
        }

        private void DrawAchievementFundingCard(
            AchievementFundingProgramme programme,
            string stateLabel = null,
            double? unlockEvaluationUniversalTime = null)
        {
            _listTextBuilder.Length = 0;
            _listTextBuilder.Append("Completed by: ");
            bool hasCompletedAgency = false;

            for (int programIndex = 0; programIndex < _raceController.Programs.Count; programIndex++)
            {
                SpaceProgramState program = _raceController.Programs[programIndex];
                _payoutScratch[programIndex] = _raceController.GetAchievementCurrentPayout(program, programme);

                if (!_raceController.HasProgramAchieved(program, programme))
                {
                    continue;
                }

                if (hasCompletedAgency)
                {
                    _listTextBuilder.Append(", ");
                }

                _listTextBuilder.Append(GetProgramDisplayName(program));
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

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(FundingCardLeftOptions);
            GUILayout.Label(
                "Objective: "
                + programme.ObjectiveDescription
                + " Interest in funding decreases by 10% after each payout.");
            GUILayout.Label("Base Payout: " + programme.BaseRewardFunds.ToString("N0"));
            GUILayout.Label("Current Interest in Contract: " + programme.CurrentInterestPercent + "%");
            GUILayout.Label("Next Total Payout: " + programme.CurrentTotalPayoutFunds.ToString("N0"));
            GUILayout.Label(
                "Contract Status: "
                + (programme.HasStarted ? "Completed - Paying Out" : "Not yet Completed"));
            GUILayout.EndVertical();

            GUILayout.Space(24.0f);
            GUILayout.BeginVertical(ExpandWidthOptions);
            GUILayout.Label(_listTextBuilder.ToString());
            DrawPayoutLinesByAmount(_payoutScratch);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (unlockEvaluationUniversalTime.HasValue)
            {
                DrawUnlockRuleProgress(programme.UnlockRule, unlockEvaluationUniversalTime.Value);
            }

            GUILayout.EndVertical();
        }

        private void DrawSatelliteFundingCard(
            FundingProgramme programme,
            string stateLabel = null,
            double? unlockEvaluationUniversalTime = null)
        {
            _listTextBuilder.Length = 0;
            _listTextBuilder.Append("Satellites: ");
            bool hasSatelliteSummary = false;
            double claimedPayout = 0.0;

            for (int programIndex = 0; programIndex < _raceController.Programs.Count; programIndex++)
            {
                SpaceProgramState program = _raceController.Programs[programIndex];
                int satelliteCount = program.GetSatelliteCount(programme.CelestialBodyName);
                double nextPayout = _raceController.GetSatelliteCurrentPayout(program, programme);
                _payoutScratch[programIndex] = nextPayout;
                claimedPayout += nextPayout;

                if (satelliteCount <= 0)
                {
                    continue;
                }

                if (hasSatelliteSummary)
                {
                    _listTextBuilder.Append(", ");
                }

                _listTextBuilder.Append(GetProgramDisplayName(program));
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

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(FundingCardLeftOptions);
            GUILayout.Label("Target: " + programme.CelestialBodyName);
            GUILayout.Label("Requirement: " + programme.RequiredSatellites + " qualifying satellite(s) in orbit");
            GUILayout.Label("Total Available Payout: " + programme.RewardFunds.ToString("N0"));
            GUILayout.EndVertical();

            GUILayout.Space(24.0f);
            GUILayout.BeginVertical(ExpandWidthOptions);
            GUILayout.Label(_listTextBuilder.ToString());
            DrawPayoutLinesByAmount(_payoutScratch);
            GUILayout.Label("Unclaimed Payout: " + unclaimedPayout.ToString("N0"));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (unlockEvaluationUniversalTime.HasValue)
            {
                DrawUnlockRuleProgress(programme.UnlockRule, unlockEvaluationUniversalTime.Value);
            }

            GUILayout.EndVertical();
        }

        private void DrawPayoutLinesByAmount(double[] nextPayouts)
        {
            for (int programIndex = 0; programIndex < _raceController.Programs.Count; programIndex++)
            {
                _agencyNameScratch[programIndex] = GetProgramDisplayName(_raceController.Programs[programIndex]);
            }

            // A small in-place sort keeps the agency with the strongest next payout first while
            // allowing the same UI path to handle any number of current programs. The caller
            // refills the reusable payout buffer before each card, so no per-card array is needed.
            for (int payoutIndex = 0; payoutIndex < nextPayouts.Length - 1; payoutIndex++)
            {
                for (int compareIndex = payoutIndex + 1; compareIndex < nextPayouts.Length; compareIndex++)
                {
                    if (nextPayouts[compareIndex] <= nextPayouts[payoutIndex])
                    {
                        continue;
                    }

                    double payoutToSwap = nextPayouts[payoutIndex];
                    nextPayouts[payoutIndex] = nextPayouts[compareIndex];
                    nextPayouts[compareIndex] = payoutToSwap;

                    string agencyNameToSwap = _agencyNameScratch[payoutIndex];
                    _agencyNameScratch[payoutIndex] = _agencyNameScratch[compareIndex];
                    _agencyNameScratch[compareIndex] = agencyNameToSwap;
                }
            }

            for (int payoutIndex = 0; payoutIndex < nextPayouts.Length; payoutIndex++)
            {
                if (nextPayouts[payoutIndex] <= 0.0)
                {
                    continue;
                }

                GUILayout.Label(
                    _agencyNameScratch[payoutIndex]
                    + " Next Payout: "
                    + nextPayouts[payoutIndex].ToString("N0"));
            }
        }

        private void DrawRivalAgencies()
        {
            GUILayout.Label("RIVAL AGENCIES - " + FormatNextFundingDate());
            GUILayout.Space(8.0f);

            _rivalsScrollPosition = GUILayout.BeginScrollView(_rivalsScrollPosition);

            for (int programIndex = 0; programIndex < _raceController.RivalPrograms.Count; programIndex++)
            {
                SpaceProgramState program = _raceController.RivalPrograms[programIndex];
                if (programIndex > 0)
                {
                    GUILayout.Space(10.0f);
                }

                DrawProgramCard(
                    program,
                    _raceController.GetEstimatedRivalLaunchDays(program),
                    _raceController.GetRivalLaunchProgressCost(program));
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
                "Once any agency achieves this achievement there are 10 payouts given. As interest in this achievement is lost overtime each following payout is reduced by 10%. If multiple agencies complete the achievement than the next payout is split between those agencies. Being the first to reach the achievement will maximise your payout.");

            GUILayout.Space(8.0f);
            GUILayout.Label("2. Satellite Contracts");
            GUILayout.Space(4.0f);
            GUILayout.Label(
                "Funding is given for the number of satellite in orbit of the body. This is a fixed contract and will always pay out. Once the maximum number of satellites is meet which ever agencies has the biggest share of satellites will get the bigger share of the payout.");

            GUILayout.Space(8.0f);
            GUILayout.Label("Unlocking New Funding Target", _boldLabelStyle);
            GUILayout.Label(
                "New funding targets can be unlocked by meeting the requirements of the funding targets currently available. Look down the list below to see all of the available funding targets:");
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }

        private void DrawSpaceRace()
        {
            double currentUniversalTime = Planetarium.fetch == null
                ? 0.0
                : Planetarium.GetUniversalTime();
            SpaceProgramState player = _raceController.PlayerProgram;

            EnsurePayoutScratchBuffers();
            EnsureSpaceRaceFundingEntries();

            _spaceRaceScrollPosition = GUILayout.BeginScrollView(_spaceRaceScrollPosition);

            GUILayout.Label("CURRENT FUNDING INFO", _boldLabelStyle);
            SpaceRaceFundingEntry selectedEntry = EnsureSelectedSpaceRaceFundingEntry(player);

            // Keep the selected contract area stable as users switch between contracts with
            // different amounts of descriptive or unlock text. Longer cards scroll internally
            // instead of pushing the funding button sections up and down.
            _spaceRaceFundingInfoScrollPosition = GUILayout.BeginScrollView(
                _spaceRaceFundingInfoScrollPosition,
                SpaceRaceFundingInfoOptions);
            if (selectedEntry == null)
            {
                GUILayout.Label("No funding contracts configured.");
            }
            else
            {
                DrawSelectedSpaceRaceFundingEntry(selectedEntry, player, currentUniversalTime);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(12.0f);
            DrawSpaceRaceFundingSection(
                "AVAILABLE NOW",
                SpaceRaceFundingCategory.Available,
                player,
                ref _spaceRaceAvailableExpanded);
            DrawSpaceRaceFundingSection(
                "LOCKED",
                SpaceRaceFundingCategory.Locked,
                player,
                ref _spaceRaceLockedExpanded);
            DrawSpaceRaceFundingSection(
                "EXPIRED",
                SpaceRaceFundingCategory.Expired,
                player,
                ref _spaceRaceExpiredExpanded);

            GUILayout.EndScrollView();
        }

        private void DrawSelectedSpaceRaceFundingEntry(
            SpaceRaceFundingEntry entry,
            SpaceProgramState player,
            double evaluationUniversalTime)
        {
            SpaceRaceFundingCategory category = GetSpaceRaceFundingCategory(entry, player);
            string stateLabel;

            if (category == SpaceRaceFundingCategory.Available)
            {
                stateLabel = "Available";
            }
            else if (category == SpaceRaceFundingCategory.Locked)
            {
                stateLabel = "Locked";
            }
            else if (entry.IsAchievement && !entry.AchievementProgramme.IsExpired)
            {
                stateLabel = "Completed";
            }
            else
            {
                stateLabel = "Expired";
            }

            double? unlockEvaluationUniversalTime = category == SpaceRaceFundingCategory.Locked
                ? (double?)evaluationUniversalTime
                : null;

            if (entry.IsAchievement)
            {
                DrawAchievementFundingCard(
                    entry.AchievementProgramme,
                    stateLabel,
                    unlockEvaluationUniversalTime);
            }
            else
            {
                DrawSatelliteFundingCard(
                    entry.SatelliteProgramme,
                    stateLabel,
                    unlockEvaluationUniversalTime);
            }
        }

        private void DrawSpaceRaceFundingSection(
            string heading,
            SpaceRaceFundingCategory category,
            SpaceProgramState player,
            ref bool isExpanded)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(heading, _boldLabelStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(isExpanded ? "-" : "+", SpaceRaceSectionToggleOptions))
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

            for (int entryIndex = 0; entryIndex < _spaceRaceFundingEntries.Count; entryIndex++)
            {
                SpaceRaceFundingEntry entry = _spaceRaceFundingEntries[entryIndex];
                if (GetSpaceRaceFundingCategory(entry, player) != category)
                {
                    continue;
                }

                if ((buttonCount % SpaceRaceFundingButtonsPerRow) == 0)
                {
                    GUILayout.BeginHorizontal();
                    hasOpenRow = true;
                }

                bool isSelected = string.Equals(
                        _selectedSpaceRaceFundingId,
                        entry.Id,
                        StringComparison.Ordinal)
                    && _selectedSpaceRaceFundingIsAchievement == entry.IsAchievement;
                string buttonLabel = isSelected ? "> " + entry.Name : entry.Name;

                if (GUILayout.Button(buttonLabel, SpaceRaceFundingButtonOptions))
                {
                    _selectedSpaceRaceFundingId = entry.Id;
                    _selectedSpaceRaceFundingIsAchievement = entry.IsAchievement;
                    _spaceRaceFundingInfoScrollPosition = Vector2.zero;
                }

                buttonCount++;
                if ((buttonCount % SpaceRaceFundingButtonsPerRow) == 0)
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

        private void EnsureSpaceRaceFundingEntries()
        {
            int achievementFundingCount = _raceController.AchievementFundingProgrammes.Count;
            int satelliteFundingCount = _raceController.FundingProgrammes.Count;
            if (ReferenceEquals(_spaceRaceFundingEntriesController, _raceController)
                && _spaceRaceAchievementFundingCount == achievementFundingCount
                && _spaceRaceSatelliteFundingCount == satelliteFundingCount)
            {
                return;
            }

            _spaceRaceFundingEntries.Clear();

            for (int programmeIndex = 0; programmeIndex < achievementFundingCount; programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    _raceController.AchievementFundingProgrammes[programmeIndex];
                MilestoneDefinition milestone = PrototypeMilestones.FindById(programme.Id);
                string celestialBodyName = milestone == null ? null : milestone.CelestialBodyName;

                _spaceRaceFundingEntries.Add(new SpaceRaceFundingEntry(
                    programme,
                    null,
                    celestialBodyName,
                    KspCelestialBodyOrdering.GetSortDistanceFromKerbin(celestialBodyName),
                    programmeIndex));
            }

            for (int programmeIndex = 0; programmeIndex < satelliteFundingCount; programmeIndex++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[programmeIndex];
                _spaceRaceFundingEntries.Add(new SpaceRaceFundingEntry(
                    null,
                    programme,
                    programme.CelestialBodyName,
                    KspCelestialBodyOrdering.GetSortDistanceFromKerbin(programme.CelestialBodyName),
                    achievementFundingCount + programmeIndex));
            }

            _spaceRaceFundingEntries.Sort(CompareSpaceRaceFundingEntries);
            _spaceRaceFundingEntriesController = _raceController;
            _spaceRaceAchievementFundingCount = achievementFundingCount;
            _spaceRaceSatelliteFundingCount = satelliteFundingCount;
        }

        private static int CompareSpaceRaceFundingEntries(
            SpaceRaceFundingEntry firstEntry,
            SpaceRaceFundingEntry secondEntry)
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

        private SpaceRaceFundingEntry EnsureSelectedSpaceRaceFundingEntry(SpaceProgramState player)
        {
            for (int entryIndex = 0; entryIndex < _spaceRaceFundingEntries.Count; entryIndex++)
            {
                SpaceRaceFundingEntry entry = _spaceRaceFundingEntries[entryIndex];
                if (string.Equals(
                        _selectedSpaceRaceFundingId,
                        entry.Id,
                        StringComparison.Ordinal)
                    && _selectedSpaceRaceFundingIsAchievement == entry.IsAchievement)
                {
                    return entry;
                }
            }

            for (int categoryIndex = (int)SpaceRaceFundingCategory.Available;
                categoryIndex <= (int)SpaceRaceFundingCategory.Expired;
                categoryIndex++)
            {
                SpaceRaceFundingCategory category = (SpaceRaceFundingCategory)categoryIndex;
                for (int entryIndex = 0; entryIndex < _spaceRaceFundingEntries.Count; entryIndex++)
                {
                    SpaceRaceFundingEntry entry = _spaceRaceFundingEntries[entryIndex];
                    if (GetSpaceRaceFundingCategory(entry, player) != category)
                    {
                        continue;
                    }

                    _selectedSpaceRaceFundingId = entry.Id;
                    _selectedSpaceRaceFundingIsAchievement = entry.IsAchievement;
                    return entry;
                }
            }

            _selectedSpaceRaceFundingId = null;
            return null;
        }

        private SpaceRaceFundingCategory GetSpaceRaceFundingCategory(
            SpaceRaceFundingEntry entry,
            SpaceProgramState player)
        {
            if (entry.IsAchievement)
            {
                AchievementFundingProgramme programme = entry.AchievementProgramme;
                if (programme.IsExpired)
                {
                    return SpaceRaceFundingCategory.Expired;
                }

                return _raceController.IsAchievementProgrammeAvailable(programme)
                    ? SpaceRaceFundingCategory.Available
                    : SpaceRaceFundingCategory.Locked;
            }

            return entry.SatelliteProgramme.IsAvailable
                ? SpaceRaceFundingCategory.Available
                : SpaceRaceFundingCategory.Locked;
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
                _raceController.Programs,
                evaluationUniversalTime);

            _listTextBuilder.Length = 0;
            _listTextBuilder.Append(isSatisfied ? "[x] " : "[ ] ");

            if (condition == null)
            {
                _listTextBuilder.Append("Invalid unlock condition");
            }
            else if (condition.ConditionType == UnlockConditionType.Achievement)
            {
                AppendAchievementConditionText(
                    condition,
                    isSatisfied,
                    evaluationUniversalTime);
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
                _raceController.Programs,
                evaluationUniversalTime);
            string milestoneName = GetMilestoneDisplayName(condition.MilestoneId);

            if (condition.RequiredProgramCount <= 1)
            {
                switch (condition.ProgramScope)
                {
                    case UnlockProgramScope.Player:
                        _listTextBuilder.Append("You achieve ");
                        break;

                    case UnlockProgramScope.AnyRival:
                        _listTextBuilder.Append("Any rival agency achieves ");
                        break;

                    case UnlockProgramScope.AnyAgency:
                    default:
                        _listTextBuilder.Append("Any agency achieves ");
                        break;
                }

                _listTextBuilder.Append(milestoneName);

                if (isSatisfied && condition.ProgramScope != UnlockProgramScope.Player)
                {
                    SpaceProgramState satisfyingProgram = FindFirstProgramSatisfyingCondition(
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
            _listTextBuilder.Append(condition.RequiredProgramCount);

            switch (condition.ProgramScope)
            {
                case UnlockProgramScope.AnyRival:
                    _listTextBuilder.Append(" rival agencies have achieved ");
                    break;

                case UnlockProgramScope.Player:
                    _listTextBuilder.Append(" player achievement count for ");
                    break;

                case UnlockProgramScope.AnyAgency:
                default:
                    _listTextBuilder.Append(" agencies have achieved ");
                    break;
            }

            _listTextBuilder.Append(milestoneName);
        }

        private SpaceProgramState FindFirstProgramSatisfyingCondition(
            UnlockConditionDefinition condition,
            double evaluationUniversalTime)
        {
            for (int programIndex = 0; programIndex < _raceController.Programs.Count; programIndex++)
            {
                SpaceProgramState program = _raceController.Programs[programIndex];
                if (UnlockRuleEvaluator.DoesProgramSatisfyAchievementCondition(
                    program,
                    condition,
                    evaluationUniversalTime))
                {
                    return program;
                }
            }

            return null;
        }

        private static string GetMilestoneDisplayName(string milestoneId)
        {
            MilestoneDefinition milestone = PrototypeMilestones.FindById(milestoneId);
            return milestone == null || string.IsNullOrEmpty(milestone.Name)
                ? milestoneId
                : milestone.Name;
        }

        private void DrawProgramCard(
            SpaceProgramState program,
            int? launchEtaDays,
            double launchProgressCostFunds)
        {
            GUILayout.BeginVertical("box");
            DrawCenteredCardTitle(program.Name);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(RivalDetailsOptions);
            GUILayout.Label(string.Empty);
            GUILayout.Label("Funds: " + program.Funds.ToString("N0"));
            GUILayout.Label(
                "Next Mission Planned: "
                + (string.IsNullOrEmpty(program.NextLaunchBodyName) ? "Planning" : program.NextLaunchBodyName));
            GUILayout.Label("Mission Progress: " + program.LaunchProgressPercent + "%");
            GUILayout.Label("Mission Progress Cost: " + launchProgressCostFunds.ToString("N0") + " (10% +)");

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
                programmeIndex < _raceController.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    _raceController.AchievementFundingProgrammes[programmeIndex];
                if (programme.IsExpired
                    || !_raceController.IsAchievementProgrammeAvailable(programme)
                    || !_raceController.HasProgramAchieved(program, programme))
                {
                    continue;
                }

                GUILayout.Label(programme.Name + ": Completed");
            }

            for (int programmeIndex = 0;
                programmeIndex < _raceController.FundingProgrammes.Count;
                programmeIndex++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[programmeIndex];
                int satelliteCount = program.GetSatelliteCount(programme.CelestialBodyName);
                double nextPayout = _raceController.GetSatelliteCurrentPayout(program, programme);
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
            GUILayout.Label(_raceController.RivalBaseIncomePerFundingPeriod.ToString("N0"));

            for (int programmeIndex = 0;
                programmeIndex < _raceController.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    _raceController.AchievementFundingProgrammes[programmeIndex];
                if (programme.IsExpired
                    || !_raceController.IsAchievementProgrammeAvailable(programme)
                    || !_raceController.HasProgramAchieved(program, programme))
                {
                    continue;
                }

                double nextPayout = _raceController.GetAchievementCurrentPayout(program, programme);
                GUILayout.Label(nextPayout > 0.0 ? nextPayout.ToString("N0") : string.Empty);
            }

            for (int programmeIndex = 0;
                programmeIndex < _raceController.FundingProgrammes.Count;
                programmeIndex++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[programmeIndex];
                int satelliteCount = program.GetSatelliteCount(programme.CelestialBodyName);
                double nextPayout = _raceController.GetSatelliteCurrentPayout(program, programme);
                if (satelliteCount <= 0 || nextPayout <= 0.0)
                {
                    continue;
                }

                GUILayout.Label(nextPayout.ToString("N0"));
            }

            GUILayout.Space(6.0f);
            GUILayout.Label(program.NextPayoutFunds.ToString("N0"), _boldLabelStyle);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void EnsurePayoutScratchBuffers()
        {
            int programCount = _raceController == null ? 0 : _raceController.Programs.Count;
            if (_payoutScratch == null || _payoutScratch.Length != programCount)
            {
                _payoutScratch = new double[programCount];
            }

            if (_agencyNameScratch == null || _agencyNameScratch.Length != programCount)
            {
                _agencyNameScratch = new string[programCount];
            }
        }

        private static string GetProgramDisplayName(SpaceProgramState program)
        {
            if (program == null)
            {
                return "Unknown";
            }

            return program.IsPlayer ? "Player" : program.Name;
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
            if (_raceController.NextFundingYear <= 0)
            {
                return "Next Funding Date: Pending";
            }

            int daysUntilNextFunding = _raceController.DaysUntilNextFunding;
            string daysUntilNextFundingText = daysUntilNextFunding == 1
                ? "1 Day to go"
                : daysUntilNextFunding + " Days to go";

            return "Next Funding Date: "
                + FormatKerbinDate(_raceController.NextFundingUniversalTime)
                + " - "
                + daysUntilNextFundingText;
        }

        private string FormatKerbinDate(double universalTime)
        {
            if (universalTime < 0.0)
            {
                return "Pending";
            }

            return "Year "
                + _raceController.GetKerbinYear(universalTime)
                + ", Day "
                + _raceController.GetKerbinDay(universalTime);
        }
    }
}

using KSP.UI.Screens;
using TheRaceForSpace.Competition;
using TheRaceForSpace.Funding;
using TheRaceForSpace.KspIntegration;
using TheRaceForSpace.Programs;
using UnityEngine;

namespace TheRaceForSpace.UI
{
    /// <summary>
    /// Prototype command center with four switchable views inside one interface window.
    /// Press F8 or use the stock KSP application launcher button to show or hide the interface.
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

        private const float RefreshIntervalSeconds = 5.0f;
        private const float WindowBackgroundOpacity = 0.82f;
        private const int HighlightedCardTitleFontSize = 16;
        private const string LauncherIconTexturePath =
            "Squad/PartList/SimpleIcons/R&D_node_icon_basicprobes";
        private static SatelliteRaceController _raceController;
        private static RaceWindow _activeInstance;
        private static Game _controllerGame;

        // Desktop-oriented size keeps all four views readable without creating additional pop-out windows.
        private Rect _windowRect = new Rect(70.0f, 55.0f, 900.0f, 720.0f);
        private Vector2 _fundingScrollPosition;
        private Vector2 _rivalsScrollPosition;
        private Vector2 _spaceRaceScrollPosition;
        private ActiveView _activeView = ActiveView.Overview;
        private ApplicationLauncherButton _launcherButton;
        private GUIStyle _highlightedCardTitleStyle;
        private GUIStyle _boldLabelStyle;
        private bool _hasRestoredVisibilityState;
        private bool _isDuplicateInstance;
        private bool _isVisible;
        private float _nextRefreshTime;

        public void Awake()
        {
            // EveryScene also instantiates addons during loading and on the main menu. Do not
            // create command-center state until KSP has entered an actual saved-game scene.
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

            // Keep one controller across scene changes inside a save, but never carry rival
            // state into a different save loaded during the same KSP process.
            if (_raceController == null || _controllerGame != HighLogic.CurrentGame)
            {
                _raceController = new SatelliteRaceController();
                _controllerGame = HighLogic.CurrentGame;
            }
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
                _raceController = new SatelliteRaceController();
                _controllerGame = HighLogic.CurrentGame;
                _hasRestoredVisibilityState = false;
                _isVisible = false;
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

            if (Time.realtimeSinceStartup >= _nextRefreshTime)
            {
                _raceController.Refresh();
                _nextRefreshTime = Time.realtimeSinceStartup + RefreshIntervalSeconds;
            }
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
                DrawWindow,
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

            if (GUILayout.Button("Overview", GUILayout.Height(40.0f)))
            {
                _activeView = ActiveView.Overview;
            }

            if (GUILayout.Button("Funding Targets", GUILayout.Height(40.0f)))
            {
                _activeView = ActiveView.FundingTargets;
            }

            if (GUILayout.Button("Rival Agencies", GUILayout.Height(40.0f)))
            {
                _activeView = ActiveView.RivalAgencies;
            }

            if (GUILayout.Button("Space Race", GUILayout.Height(40.0f)))
            {
                _activeView = ActiveView.SpaceRace;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(12.0f);

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

            GUILayout.FlexibleSpace();
            GUILayout.Label("F8: show/hide interface");
            GUI.DragWindow();
        }

        private void DrawOverview()
        {
            SpaceProgramState player = _raceController.PlayerProgram;
            int kerbinSatelliteCount = player.GetSatelliteCount("Kerbin");
            int munSatelliteCount = player.GetSatelliteCount("Mun");
            int minmusSatelliteCount = player.GetSatelliteCount("Minmus");

            int totalKerbinSatelliteCount = kerbinSatelliteCount
                + _raceController.AsterProgram.GetSatelliteCount("Kerbin")
                + _raceController.CobaltProgram.GetSatelliteCount("Kerbin");
            int totalMunSatelliteCount = munSatelliteCount
                + _raceController.AsterProgram.GetSatelliteCount("Mun")
                + _raceController.CobaltProgram.GetSatelliteCount("Mun");
            int totalMinmusSatelliteCount = minmusSatelliteCount
                + _raceController.AsterProgram.GetSatelliteCount("Minmus")
                + _raceController.CobaltProgram.GetSatelliteCount("Minmus");

            double probeOrbitNextPayout = _raceController.GetAchievementCurrentPayout(
                player,
                _raceController.ProbeOrbitProgramme);
            double crewedOrbitNextPayout = _raceController.GetAchievementCurrentPayout(
                player,
                _raceController.CrewedOrbitProgramme);
            double munProbeOrbitNextPayout = _raceController.GetAchievementCurrentPayout(
                player,
                _raceController.MunProbeOrbitProgramme);
            double minmusProbeOrbitNextPayout = _raceController.GetAchievementCurrentPayout(
                player,
                _raceController.MinmusProbeOrbitProgramme);
            double kerbinSatelliteNextPayout = _raceController.KerbinNetworkProgramme.CalculateCurrentPayout(
                kerbinSatelliteCount,
                totalKerbinSatelliteCount);
            double munSatelliteNextPayout = _raceController.MunNetworkProgramme.CalculateCurrentPayout(
                munSatelliteCount,
                totalMunSatelliteCount);
            double minmusSatelliteNextPayout = _raceController.MinmusNetworkProgramme.CalculateCurrentPayout(
                minmusSatelliteCount,
                totalMinmusSatelliteCount);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(350.0f));
            GUILayout.Label("Your Objectives", _boldLabelStyle);

            if (!_raceController.ProbeOrbitProgramme.IsExpired)
            {
                GUILayout.Label("Probe Orbit: " + (player.HasAchievedProbeOrbit ? "ACHIEVED" : "IN PROGRESS"));
            }

            if (!_raceController.CrewedOrbitProgramme.IsExpired)
            {
                GUILayout.Label("Crewed Orbit: " + (player.HasAchievedCrewedOrbit ? "ACHIEVED" : "IN PROGRESS"));
            }

            if (_raceController.IsAchievementProgrammeAvailable(_raceController.MunProbeOrbitProgramme)
                && !_raceController.MunProbeOrbitProgramme.IsExpired)
            {
                GUILayout.Label(
                    "Mun Probe Orbit: "
                    + (player.HasAchievedMunProbeOrbit ? "ACHIEVED" : "IN PROGRESS"));
            }

            if (_raceController.IsAchievementProgrammeAvailable(_raceController.MinmusProbeOrbitProgramme)
                && !_raceController.MinmusProbeOrbitProgramme.IsExpired)
            {
                GUILayout.Label(
                    "Minmus Probe Orbit: "
                    + (player.HasAchievedMinmusProbeOrbit ? "ACHIEVED" : "IN PROGRESS"));
            }

            GUILayout.Space(10.0f);
            GUILayout.Label("Your Satellite Networks", _boldLabelStyle);

            if (kerbinSatelliteCount > 0)
            {
                GUILayout.Label("Kerbin orbit: " + kerbinSatelliteCount + " qualifying satellite(s)");
            }

            if (munSatelliteCount > 0)
            {
                GUILayout.Label("Mun orbit: " + munSatelliteCount + " qualifying satellite(s)");
            }

            if (minmusSatelliteCount > 0)
            {
                GUILayout.Label("Minmus orbit: " + minmusSatelliteCount + " qualifying satellite(s)");
            }

            GUILayout.EndVertical();

            GUILayout.Space(24.0f);
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            // The funding heading and shared date span both funding sub-columns so the date
            // has the full remaining window width and does not wrap above the payout rows.
            GUILayout.Label("Funding Information", _boldLabelStyle);
            GUILayout.Label(FormatNextFundingDate());
            GUILayout.Space(10.0f);

            // Draw each funding source and value in the same horizontal row. This keeps the
            // third-column amount aligned even when labels have different text lengths.
            if (probeOrbitNextPayout > 0.0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Probe Orbit: Completed", GUILayout.Width(245.0f));
                GUILayout.Space(2.0f);
                GUILayout.Label(probeOrbitNextPayout.ToString("N0"), GUILayout.Width(100.0f));
                GUILayout.EndHorizontal();
            }

            if (crewedOrbitNextPayout > 0.0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Crewed Orbit: Completed", GUILayout.Width(245.0f));
                GUILayout.Space(2.0f);
                GUILayout.Label(crewedOrbitNextPayout.ToString("N0"), GUILayout.Width(100.0f));
                GUILayout.EndHorizontal();
            }

            if (munProbeOrbitNextPayout > 0.0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Mun Probe Orbit: Completed", GUILayout.Width(245.0f));
                GUILayout.Space(2.0f);
                GUILayout.Label(munProbeOrbitNextPayout.ToString("N0"), GUILayout.Width(100.0f));
                GUILayout.EndHorizontal();
            }

            if (minmusProbeOrbitNextPayout > 0.0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Minmus Probe Orbit: Completed", GUILayout.Width(245.0f));
                GUILayout.Space(2.0f);
                GUILayout.Label(minmusProbeOrbitNextPayout.ToString("N0"), GUILayout.Width(100.0f));
                GUILayout.EndHorizontal();
            }

            if (kerbinSatelliteNextPayout > 0.0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    kerbinSatelliteCount
                    + " Kerbin "
                    + (kerbinSatelliteCount == 1 ? "Satellite" : "Satellites"),
                    GUILayout.Width(245.0f));
                GUILayout.Space(2.0f);
                GUILayout.Label(kerbinSatelliteNextPayout.ToString("N0"), GUILayout.Width(100.0f));
                GUILayout.EndHorizontal();
            }

            if (munSatelliteNextPayout > 0.0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    munSatelliteCount
                    + " Mun "
                    + (munSatelliteCount == 1 ? "Satellite" : "Satellites"),
                    GUILayout.Width(245.0f));
                GUILayout.Space(2.0f);
                GUILayout.Label(munSatelliteNextPayout.ToString("N0"), GUILayout.Width(100.0f));
                GUILayout.EndHorizontal();
            }

            if (minmusSatelliteNextPayout > 0.0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    minmusSatelliteCount
                    + " Minmus "
                    + (minmusSatelliteCount == 1 ? "Satellite" : "Satellites"),
                    GUILayout.Width(245.0f));
                GUILayout.Space(2.0f);
                GUILayout.Label(minmusSatelliteNextPayout.ToString("N0"), GUILayout.Width(100.0f));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6.0f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Total Next Payout", _boldLabelStyle, GUILayout.Width(245.0f));
            GUILayout.Space(2.0f);
            GUILayout.Label(
                player.NextPayoutFunds.ToString("N0"),
                _boldLabelStyle,
                GUILayout.Width(100.0f));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawFundingTargets()
        {
            GUILayout.Label("FUNDING TARGETS - " + FormatNextFundingDate());
            GUILayout.Space(8.0f);

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

                int playerProgress = _raceController.PlayerProgram.GetSatelliteCount(programme.CelestialBodyName);
                int asterProgress = _raceController.AsterProgram.GetSatelliteCount(programme.CelestialBodyName);
                int cobaltProgress = _raceController.CobaltProgram.GetSatelliteCount(programme.CelestialBodyName);
                int totalSatelliteCount = playerProgress + asterProgress + cobaltProgress;

                double playerNextPayout = programme.CalculateCurrentPayout(playerProgress, totalSatelliteCount);
                double asterNextPayout = programme.CalculateCurrentPayout(asterProgress, totalSatelliteCount);
                double cobaltNextPayout = programme.CalculateCurrentPayout(cobaltProgress, totalSatelliteCount);
                double unclaimedPayout = programme.RewardFunds
                    - playerNextPayout
                    - asterNextPayout
                    - cobaltNextPayout;
                if (unclaimedPayout < 0.0)
                {
                    unclaimedPayout = 0.0;
                }

                string satelliteSummary = string.Empty;
                if (playerProgress > 0)
                {
                    satelliteSummary = "Player " + playerProgress;
                }

                if (asterProgress > 0)
                {
                    satelliteSummary += (satelliteSummary.Length > 0 ? ", " : string.Empty)
                        + "Aster "
                        + asterProgress;
                }

                if (cobaltProgress > 0)
                {
                    satelliteSummary += (satelliteSummary.Length > 0 ? ", " : string.Empty)
                        + "Cobalt "
                        + cobaltProgress;
                }

                GUILayout.BeginVertical("box");
                DrawCenteredCardTitle("Satellite Funding - " + programme.Name);

                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(450.0f));
                GUILayout.Label("Target: " + programme.CelestialBodyName);
                GUILayout.Label("Requirement: " + programme.RequiredSatellites + " qualifying satellite(s) in orbit");
                GUILayout.Label("Total Available Payout: " + programme.RewardFunds.ToString("N0"));
                GUILayout.EndVertical();

                GUILayout.Space(24.0f);
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                GUILayout.Label("Satellites: " + (satelliteSummary.Length > 0 ? satelliteSummary : "None"));
                DrawPayoutLinesByAmount(playerNextPayout, asterNextPayout, cobaltNextPayout);
                GUILayout.Label("Unclaimed Payout: " + unclaimedPayout.ToString("N0"));
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(8.0f);
            }

            GUILayout.EndScrollView();
        }

        private void DrawAchievementFundingCard(AchievementFundingProgramme programme)
        {
            double playerNextPayout = _raceController.GetAchievementCurrentPayout(
                _raceController.PlayerProgram,
                programme);
            double asterNextPayout = _raceController.GetAchievementCurrentPayout(
                _raceController.AsterProgram,
                programme);
            double cobaltNextPayout = _raceController.GetAchievementCurrentPayout(
                _raceController.CobaltProgram,
                programme);

            GUILayout.BeginVertical("box");
            DrawCenteredCardTitle(programme.Name);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(450.0f));
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
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));

            string completedBy = string.Empty;
            if (_raceController.HasProgramAchieved(_raceController.PlayerProgram, programme))
            {
                completedBy = "Player";
            }

            if (_raceController.HasProgramAchieved(_raceController.AsterProgram, programme))
            {
                completedBy += (completedBy.Length > 0 ? ", " : string.Empty) + "Aster";
            }

            if (_raceController.HasProgramAchieved(_raceController.CobaltProgram, programme))
            {
                completedBy += (completedBy.Length > 0 ? ", " : string.Empty) + "Cobalt";
            }

            GUILayout.Label("Completed by: " + (completedBy.Length > 0 ? completedBy : "None"));
            DrawPayoutLinesByAmount(playerNextPayout, asterNextPayout, cobaltNextPayout);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void DrawPayoutLinesByAmount(
            double playerNextPayout,
            double asterNextPayout,
            double cobaltNextPayout)
        {
            string[] agencyNames = { "Player", "Aster", "Cobalt" };
            double[] nextPayouts = { playerNextPayout, asterNextPayout, cobaltNextPayout };

            // Only three agencies are shown in 0.3, so a small in-place sort keeps the display
            // straightforward while putting the agency with the strongest next payout first.
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

                    string agencyNameToSwap = agencyNames[payoutIndex];
                    agencyNames[payoutIndex] = agencyNames[compareIndex];
                    agencyNames[compareIndex] = agencyNameToSwap;
                }
            }

            for (int payoutIndex = 0; payoutIndex < nextPayouts.Length; payoutIndex++)
            {
                if (nextPayouts[payoutIndex] <= 0.0)
                {
                    continue;
                }

                GUILayout.Label(
                    agencyNames[payoutIndex]
                    + " Next Payout: "
                    + nextPayouts[payoutIndex].ToString("N0"));
            }
        }

        private void DrawRivalAgencies()
        {
            GUILayout.Label("RIVAL AGENCIES - " + FormatNextFundingDate());
            GUILayout.Space(8.0f);

            _rivalsScrollPosition = GUILayout.BeginScrollView(_rivalsScrollPosition);

            DrawProgramCard(
                _raceController.AsterProgram,
                _raceController.GetEstimatedRivalLaunchDays(_raceController.AsterProgram),
                _raceController.GetRivalLaunchProgressCost(_raceController.AsterProgram));
            GUILayout.Space(10.0f);
            DrawProgramCard(
                _raceController.CobaltProgram,
                _raceController.GetEstimatedRivalLaunchDays(_raceController.CobaltProgram),
                _raceController.GetRivalLaunchProgressCost(_raceController.CobaltProgram));

            GUILayout.EndScrollView();
        }

        private void DrawSpaceRace()
        {
            GUILayout.Label("SPACE RACE");
            GUILayout.Space(8.0f);

            _spaceRaceScrollPosition = GUILayout.BeginScrollView(_spaceRaceScrollPosition);

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

            GUILayout.Space(12.0f);
            GUILayout.Label("AVAILABLE FUNDING", _boldLabelStyle);
            bool hasAvailableFunding = false;

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

                if (hasAvailableFunding)
                {
                    GUILayout.Space(8.0f);
                }

                DrawAchievementInformationCard(programme);
                hasAvailableFunding = true;
            }

            for (int programmeIndex = 0;
                programmeIndex < _raceController.FundingProgrammes.Count;
                programmeIndex++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[programmeIndex];
                if (!programme.IsAvailable)
                {
                    continue;
                }

                if (hasAvailableFunding)
                {
                    GUILayout.Space(8.0f);
                }

                DrawSatelliteInformationCard(programme);
                hasAvailableFunding = true;
            }

            if (!hasAvailableFunding)
            {
                GUILayout.Label("None");
            }

            GUILayout.Space(12.0f);
            GUILayout.Label("LOCKED FUNDING", _boldLabelStyle);
            bool hasLockedFunding = false;

            for (int programmeIndex = 0;
                programmeIndex < _raceController.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    _raceController.AchievementFundingProgrammes[programmeIndex];
                if (programme.IsExpired || _raceController.IsAchievementProgrammeAvailable(programme))
                {
                    continue;
                }

                if (hasLockedFunding)
                {
                    GUILayout.Space(8.0f);
                }

                DrawAchievementInformationCard(programme);
                hasLockedFunding = true;
            }

            for (int programmeIndex = 0;
                programmeIndex < _raceController.FundingProgrammes.Count;
                programmeIndex++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[programmeIndex];
                if (programme.IsAvailable)
                {
                    continue;
                }

                if (hasLockedFunding)
                {
                    GUILayout.Space(8.0f);
                }

                DrawSatelliteInformationCard(programme);
                hasLockedFunding = true;
            }

            if (!hasLockedFunding)
            {
                GUILayout.Label("None");
            }

            GUILayout.Space(12.0f);
            GUILayout.Label("EXPIRED FUNDING", _boldLabelStyle);
            bool hasExpiredFunding = false;

            for (int programmeIndex = 0;
                programmeIndex < _raceController.AchievementFundingProgrammes.Count;
                programmeIndex++)
            {
                AchievementFundingProgramme programme =
                    _raceController.AchievementFundingProgrammes[programmeIndex];
                if (!programme.IsExpired)
                {
                    continue;
                }

                if (hasExpiredFunding)
                {
                    GUILayout.Space(8.0f);
                }

                DrawAchievementInformationCard(programme);
                hasExpiredFunding = true;
            }

            if (!hasExpiredFunding)
            {
                GUILayout.Label("None");
            }

            GUILayout.EndScrollView();
        }

        private void DrawAchievementInformationCard(AchievementFundingProgramme programme)
        {
            bool isAvailable = _raceController.IsAchievementProgrammeAvailable(programme);
            bool isLunarProbeProgramme = programme == _raceController.MunProbeOrbitProgramme
                || programme == _raceController.MinmusProbeOrbitProgramme;

            GUILayout.BeginVertical("box");
            GUILayout.Label(programme.Name);
            GUILayout.Label("Objective: " + programme.ObjectiveDescription);
            GUILayout.Label(
                isLunarProbeProgramme
                    ? "Unlock: Any agency must achieve Probe Orbit."
                    : "Unlock: Available from the start of the campaign");

            if (programme.IsExpired)
            {
                GUILayout.Label("State: EXPIRED");
            }
            else if (!isAvailable)
            {
                GUILayout.Label("State: LOCKED");
            }
            else if (!programme.HasStarted)
            {
                GUILayout.Label("State: ACTIVE - awaiting first achievement");
            }
            else
            {
                GUILayout.Label("State: ACTIVE - current interest " + programme.CurrentInterestPercent + "%");
                GUILayout.Label("Next payout: " + FormatKerbinDate(_raceController.NextFundingUniversalTime));
            }

            GUILayout.Label("Base payout: " + programme.BaseRewardFunds.ToString("N0"));
            GUILayout.Label("Funding: the first global funding date after the objective is achieved pays 100%. Later global funding dates fall by 10% each time. Each payment is shared by agencies that had achieved the objective by that date.");
            GUILayout.Label(
                "Player: "
                + (_raceController.HasProgramAchieved(_raceController.PlayerProgram, programme) ? "ACHIEVED" : "NOT ACHIEVED"));
            GUILayout.EndVertical();
        }

        private void DrawSatelliteInformationCard(FundingProgramme programme)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label(programme.Name);
            GUILayout.Label(
                "Objective: maintain "
                + programme.RequiredSatellites
                + " qualifying satellite(s) in orbit around "
                + programme.CelestialBodyName
                + ".");
            GUILayout.Label("State: " + (programme.IsAvailable ? "UNLOCKED" : "LOCKED"));

            if (!programme.IsAvailable && !string.IsNullOrEmpty(programme.UnlockRequirement))
            {
                GUILayout.Label("Unlock requirement: " + programme.UnlockRequirement);
            }

            GUILayout.Label("Total available payout: " + programme.RewardFunds.ToString("N0"));
            GUILayout.Label("Funding: paid on the shared 90-day funding date and permanent once unlocked; this contract does not lose interest over time.");
            GUILayout.EndVertical();
        }

        private void DrawProgramCard(
            SpaceProgramState program,
            int? launchEtaDays,
            double launchProgressCostFunds)
        {
            int kerbinSatelliteCount = program.GetSatelliteCount("Kerbin");
            int munSatelliteCount = program.GetSatelliteCount("Mun");
            int minmusSatelliteCount = program.GetSatelliteCount("Minmus");

            int totalKerbinSatelliteCount = _raceController.PlayerProgram.GetSatelliteCount("Kerbin")
                + _raceController.AsterProgram.GetSatelliteCount("Kerbin")
                + _raceController.CobaltProgram.GetSatelliteCount("Kerbin");
            int totalMunSatelliteCount = _raceController.PlayerProgram.GetSatelliteCount("Mun")
                + _raceController.AsterProgram.GetSatelliteCount("Mun")
                + _raceController.CobaltProgram.GetSatelliteCount("Mun");
            int totalMinmusSatelliteCount = _raceController.PlayerProgram.GetSatelliteCount("Minmus")
                + _raceController.AsterProgram.GetSatelliteCount("Minmus")
                + _raceController.CobaltProgram.GetSatelliteCount("Minmus");

            double probeOrbitNextPayout = _raceController.GetAchievementCurrentPayout(
                program,
                _raceController.ProbeOrbitProgramme);
            double crewedOrbitNextPayout = _raceController.GetAchievementCurrentPayout(
                program,
                _raceController.CrewedOrbitProgramme);
            double munProbeOrbitNextPayout = _raceController.GetAchievementCurrentPayout(
                program,
                _raceController.MunProbeOrbitProgramme);
            double minmusProbeOrbitNextPayout = _raceController.GetAchievementCurrentPayout(
                program,
                _raceController.MinmusProbeOrbitProgramme);
            double kerbinSatelliteNextPayout = _raceController.KerbinNetworkProgramme.CalculateCurrentPayout(
                kerbinSatelliteCount,
                totalKerbinSatelliteCount);
            double munSatelliteNextPayout = _raceController.MunNetworkProgramme.CalculateCurrentPayout(
                munSatelliteCount,
                totalMunSatelliteCount);
            double minmusSatelliteNextPayout = _raceController.MinmusNetworkProgramme.CalculateCurrentPayout(
                minmusSatelliteCount,
                totalMinmusSatelliteCount);

            bool showProbeOrbitFunding =
                program.HasAchievedProbeOrbit
                && !_raceController.ProbeOrbitProgramme.IsExpired;
            bool showCrewedOrbitFunding =
                program.HasAchievedCrewedOrbit
                && !_raceController.CrewedOrbitProgramme.IsExpired;
            bool showMunProbeOrbitFunding =
                program.HasAchievedMunProbeOrbit
                && _raceController.IsAchievementProgrammeAvailable(_raceController.MunProbeOrbitProgramme)
                && !_raceController.MunProbeOrbitProgramme.IsExpired;
            bool showMinmusProbeOrbitFunding =
                program.HasAchievedMinmusProbeOrbit
                && _raceController.IsAchievementProgrammeAvailable(_raceController.MinmusProbeOrbitProgramme)
                && !_raceController.MinmusProbeOrbitProgramme.IsExpired;
            bool showKerbinSatelliteFunding = kerbinSatelliteCount > 0 && kerbinSatelliteNextPayout > 0.0;
            bool showMunSatelliteFunding = munSatelliteCount > 0 && munSatelliteNextPayout > 0.0;
            bool showMinmusSatelliteFunding = minmusSatelliteCount > 0 && minmusSatelliteNextPayout > 0.0;

            GUILayout.BeginVertical("box");
            DrawCenteredCardTitle(program.Name);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(320.0f));
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
                if (launchEtaDays.Value <= 10)
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
            GUILayout.BeginVertical(GUILayout.Width(245.0f));
            GUILayout.Label(string.Empty);
            GUILayout.Label("Base Income");

            if (showProbeOrbitFunding)
            {
                GUILayout.Label("Probe Orbit: Completed");
            }

            if (showCrewedOrbitFunding)
            {
                GUILayout.Label("Crewed Orbit: Completed");
            }

            if (showMunProbeOrbitFunding)
            {
                GUILayout.Label("Mun Probe Orbit: Completed");
            }

            if (showMinmusProbeOrbitFunding)
            {
                GUILayout.Label("Minmus Probe Orbit: Completed");
            }

            if (showKerbinSatelliteFunding)
            {
                GUILayout.Label("Kerbin Satellites " + kerbinSatelliteCount);
            }

            if (showMunSatelliteFunding)
            {
                GUILayout.Label("Mun Satellites " + munSatelliteCount);
            }

            if (showMinmusSatelliteFunding)
            {
                GUILayout.Label("Minmus Satellites " + minmusSatelliteCount);
            }

            GUILayout.Space(6.0f);
            GUILayout.Label("Total Next Payout", _boldLabelStyle);
            GUILayout.EndVertical();

            GUILayout.Space(2.0f);
            GUILayout.BeginVertical(GUILayout.Width(125.0f));
            GUILayout.Label(string.Empty);
            GUILayout.Label(_raceController.RivalBaseIncomePerFundingPeriod.ToString("N0"));

            if (showProbeOrbitFunding)
            {
                GUILayout.Label(probeOrbitNextPayout > 0.0 ? probeOrbitNextPayout.ToString("N0") : string.Empty);
            }

            if (showCrewedOrbitFunding)
            {
                GUILayout.Label(crewedOrbitNextPayout > 0.0 ? crewedOrbitNextPayout.ToString("N0") : string.Empty);
            }

            if (showMunProbeOrbitFunding)
            {
                GUILayout.Label(munProbeOrbitNextPayout > 0.0 ? munProbeOrbitNextPayout.ToString("N0") : string.Empty);
            }

            if (showMinmusProbeOrbitFunding)
            {
                GUILayout.Label(
                    minmusProbeOrbitNextPayout > 0.0
                        ? minmusProbeOrbitNextPayout.ToString("N0")
                        : string.Empty);
            }

            if (showKerbinSatelliteFunding)
            {
                GUILayout.Label(kerbinSatelliteNextPayout.ToString("N0"));
            }

            if (showMunSatelliteFunding)
            {
                GUILayout.Label(munSatelliteNextPayout.ToString("N0"));
            }

            if (showMinmusSatelliteFunding)
            {
                GUILayout.Label(minmusSatelliteNextPayout.ToString("N0"));
            }

            GUILayout.Space(6.0f);
            GUILayout.Label(program.NextPayoutFunds.ToString("N0"), _boldLabelStyle);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawCenteredCardTitle(string title)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(title, _highlightedCardTitleStyle, GUILayout.ExpandWidth(false));
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

        private static string FormatSatelliteCount(int satelliteCount)
        {
            return satelliteCount + (satelliteCount == 1 ? " Satellite" : " Satellites");
        }
    }
}

using KSP.UI.Screens;
using TheRaceForSpace.Competition;
using TheRaceForSpace.Funding;
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
        private bool _isDuplicateInstance;
        private bool _isVisible = true;
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

            GUILayout.Label("OVERVIEW");
            GUILayout.Space(6.0f);

            GUILayout.Label("PROGRAM STATUS");
            if (_raceController.NextFundingYear > 0)
            {
                GUILayout.Label(
                    "Next funding date: Year "
                    + _raceController.NextFundingYear
                    + ", Day "
                    + _raceController.NextFundingDay);
            }
            else
            {
                GUILayout.Label("Next funding date: Pending");
            }

            // Every live contract is included because all funding now pays on the same date.
            GUILayout.Label("Next payout: " + player.NextPayoutFunds.ToString("N0"));

            GUILayout.Space(10.0f);
            GUILayout.Label("ORBIT ACHIEVEMENTS");
            GUILayout.Label("Probe Orbit: " + (player.HasAchievedProbeOrbit ? "ACHIEVED" : "IN PROGRESS"));
            GUILayout.Label("Crewed Orbit: " + (player.HasAchievedCrewedOrbit ? "ACHIEVED" : "IN PROGRESS"));

            GUILayout.Space(10.0f);
            GUILayout.Label("SATELLITE NETWORK");
            GUILayout.Label("Kerbin orbit: " + player.GetSatelliteCount("Kerbin") + " qualifying satellite(s)");
            GUILayout.Label("Mun orbit: " + player.GetSatelliteCount("Mun") + " qualifying satellite(s)");
            GUILayout.Label("Minmus orbit: " + player.GetSatelliteCount("Minmus") + " qualifying satellite(s)");
        }

        private void DrawFundingTargets()
        {
            GUILayout.Label("FUNDING TARGETS");
            GUILayout.Space(8.0f);

            _fundingScrollPosition = GUILayout.BeginScrollView(_fundingScrollPosition);

            for (int i = 0; i < _raceController.AchievementFundingProgrammes.Count; i++)
            {
                AchievementFundingProgramme programme = _raceController.AchievementFundingProgrammes[i];
                if (programme.IsExpired)
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

                GUILayout.BeginVertical("box");
                DrawCenteredCardTitle(programme.Name);

                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(400.0f));
                GUILayout.Label("Target: " + programme.CelestialBodyName);
                GUILayout.Label("Requirement: " + programme.RequiredSatellites + " qualifying satellite(s) in orbit");
                GUILayout.Label("Total Available Payout: " + programme.RewardFunds.ToString("N0"));
                GUILayout.Label("Contract type: Fixed Contract");
                GUILayout.Label("Next Payout: " + FormatKerbinDate(_raceController.NextFundingUniversalTime));
                GUILayout.EndVertical();

                GUILayout.Space(24.0f);
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                GUILayout.Label("Player Progress: " + FormatSatelliteCount(playerProgress));
                GUILayout.Label("Player Next Payout: " + playerNextPayout.ToString("N0"));
                GUILayout.Label("Aster Progress: " + FormatSatelliteCount(asterProgress));
                GUILayout.Label("Aster Next Payout: " + asterNextPayout.ToString("N0"));
                GUILayout.Label("Cobalt Progress: " + FormatSatelliteCount(cobaltProgress));
                GUILayout.Label("Cobalt Next Payout: " + cobaltNextPayout.ToString("N0"));
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(8.0f);
            }

            GUILayout.EndScrollView();
        }

        private void DrawAchievementFundingCard(AchievementFundingProgramme programme)
        {
            GUILayout.BeginVertical("box");
            DrawCenteredCardTitle(programme.Name);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(400.0f));
            GUILayout.Label("Objective: " + programme.ObjectiveDescription);
            GUILayout.Label("Base Payout: " + programme.BaseRewardFunds.ToString("N0"));
            GUILayout.Label("Current Interest: " + programme.CurrentInterestPercent + "%");
            GUILayout.Label("Current Total Payout: " + programme.CurrentTotalPayoutFunds.ToString("N0"));
            GUILayout.Label("Contract Status: " + (programme.HasStarted ? "Completed" : "Not yet Completed"));
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
            DrawAchievementAgencyLine("Player", _raceController.PlayerProgram, programme);
            DrawAchievementAgencyLine("Aster", _raceController.AsterProgram, programme);
            DrawAchievementAgencyLine("Cobalt", _raceController.CobaltProgram, programme);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawAchievementAgencyLine(
            string displayName,
            SpaceProgramState program,
            AchievementFundingProgramme programme)
        {
            GUILayout.Label(
                displayName
                + " Next Payout: "
                + _raceController.GetAchievementCurrentPayout(program, programme).ToString("N0"));
        }

        private void DrawRivalAgencies()
        {
            GUILayout.Label("RIVAL AGENCIES");
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

            GUILayout.Space(12.0f);
            GUILayout.Label("RACE COMPARISON");
            DrawComparisonRow(_raceController.PlayerProgram);
            DrawComparisonRow(_raceController.AsterProgram);
            DrawComparisonRow(_raceController.CobaltProgram);

            GUILayout.EndScrollView();
        }

        private void DrawSpaceRace()
        {
            GUILayout.Label("SPACE RACE");
            GUILayout.Space(8.0f);

            _spaceRaceScrollPosition = GUILayout.BeginScrollView(_spaceRaceScrollPosition);

            GUILayout.BeginVertical("box");
            GUILayout.Label("HELP / PLAYER GUIDE");
            GUILayout.Label("Version 0.3 guide text placeholder. Final player-facing wording will be added after the first gameplay test pass.");
            GUILayout.Label("All live contracts pay on the same 90-Kerbin-day funding date. Orbit contracts lose 10% interest after each payment; satellite contracts remain permanent once unlocked.");
            GUILayout.EndVertical();

            GUILayout.Space(12.0f);
            GUILayout.Label("ORBIT CONTRACTS");
            DrawAchievementInformationCard(_raceController.ProbeOrbitProgramme);
            GUILayout.Space(8.0f);
            DrawAchievementInformationCard(_raceController.CrewedOrbitProgramme);

            GUILayout.Space(12.0f);
            GUILayout.Label("SATELLITE CONTRACTS");
            DrawSatelliteInformationCard(_raceController.KerbinNetworkProgramme);
            GUILayout.Space(8.0f);
            DrawSatelliteInformationCard(_raceController.MunNetworkProgramme);
            GUILayout.Space(8.0f);
            DrawSatelliteInformationCard(_raceController.MinmusNetworkProgramme);

            GUILayout.EndScrollView();
        }

        private void DrawAchievementInformationCard(AchievementFundingProgramme programme)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label(programme.Name);
            GUILayout.Label("Objective: " + programme.ObjectiveDescription);
            GUILayout.Label("Unlock: Available from the start of the campaign");

            if (programme.IsExpired)
            {
                GUILayout.Label("State: EXPIRED");
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
            GUILayout.BeginVertical("box");
            DrawCenteredCardTitle(program.Name);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(430.0f));
            GUILayout.Label("Funds: " + program.Funds.ToString("N0"));
            GUILayout.Label("Next Payout: " + program.NextPayoutFunds.ToString("N0"));
            GUILayout.Label(
                "Next Mission Planned: "
                + (string.IsNullOrEmpty(program.NextLaunchBodyName) ? "Planning" : program.NextLaunchBodyName));
            GUILayout.Label("Mission Progress: " + program.LaunchProgressPercent + "%");
            GUILayout.Label("Mission Progress Cost: " + launchProgressCostFunds.ToString("N0") + " (10% +)");

            string launchEtaText = "Awaiting Funding";
            if (launchEtaDays.HasValue)
            {
                launchEtaText = launchEtaDays.Value <= 10
                    ? "Next launch imminent"
                    : launchEtaDays.Value + " days";
            }

            GUILayout.Label("ETA till Completion: " + launchEtaText);
            GUILayout.EndVertical();

            GUILayout.Space(24.0f);
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Probe Orbit: " + (program.HasAchievedProbeOrbit ? "ACHIEVED" : "NOT ACHIEVED"));
            GUILayout.Label("Crewed Orbit: " + (program.HasAchievedCrewedOrbit ? "ACHIEVED" : "NOT ACHIEVED"));
            GUILayout.Label("Kerbin satellites: " + program.GetSatelliteCount("Kerbin"));
            GUILayout.Label("Mun satellites: " + program.GetSatelliteCount("Mun"));
            GUILayout.Label("Minmus satellites: " + program.GetSatelliteCount("Minmus"));
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

        private static void DrawComparisonRow(SpaceProgramState program)
        {
            int totalSatellites = program.GetSatelliteCount("Kerbin")
                + program.GetSatelliteCount("Mun")
                + program.GetSatelliteCount("Minmus");

            if (program.IsPlayer)
            {
                GUILayout.Label(
                    program.Name
                    + " | next payout "
                    + program.NextPayoutFunds.ToString("N0")
                    + " | tracked satellites "
                    + totalSatellites);
                return;
            }

            GUILayout.Label(
                program.Name
                + " | funds "
                + program.Funds.ToString("N0")
                + " | next payout "
                + program.NextPayoutFunds.ToString("N0")
                + " | tracked satellites "
                + totalSatellites);
        }
    }
}

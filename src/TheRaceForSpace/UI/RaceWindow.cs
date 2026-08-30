using TheRaceForSpace.Competition;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Programs;
using UnityEngine;

namespace TheRaceForSpace.UI
{
    /// <summary>
    /// Prototype command center with four switchable views inside one interface window.
    /// Press F8 to show or hide the complete interface.
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
        private static SatelliteRaceController _raceController;
        private static RaceWindow _activeInstance;
        private static Game _controllerGame;

        // Desktop-oriented size keeps all four views readable without creating additional pop-out windows.
        private Rect _windowRect = new Rect(70.0f, 55.0f, 900.0f, 720.0f);
        private Vector2 _fundingScrollPosition;
        private Vector2 _rivalsScrollPosition;
        private Vector2 _spaceRaceScrollPosition;
        private ActiveView _activeView = ActiveView.Overview;
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
            // Release ownership when the active scene instance is genuinely destroyed so the
            // next KSP scene can create its replacement without being mistaken for a duplicate.
            if (_activeInstance == this)
            {
                _activeInstance = null;
            }
        }

        public void Update()
        {
            // Scene transitions can briefly leave an addon alive after KSP has stopped treating
            // the current scene as gameplay, so suppress input and refresh work immediately.
            if (_isDuplicateInstance
                || _activeInstance != this
                || !HighLogic.LoadedSceneIsGame
                || HighLogic.CurrentGame == null)
            {
                return;
            }

            // A save reload can replace HighLogic.CurrentGame without a useful UI transition.
            // Rebind defensively so persistence is always restored into a controller for that save.
            if (_controllerGame != HighLogic.CurrentGame)
            {
                _raceController = new SatelliteRaceController();
                _controllerGame = HighLogic.CurrentGame;
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                _isVisible = !_isVisible;
            }

            if (Time.realtimeSinceStartup >= _nextRefreshTime)
            {
                _raceController.Refresh();
                _nextRefreshTime = Time.realtimeSinceStartup + RefreshIntervalSeconds;
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

            // Use the normal KSP IMGUI window background without reducing its alpha.
            _windowRect = GUILayout.Window(
                GetInstanceID(),
                _windowRect,
                DrawWindow,
                "The Race for Space - Command Center");
        }

        private void DrawWindow(int windowId)
        {
            // Funding programme names and rival agency names intentionally share one cached style
            // so both card types use the same highlighted title treatment without per-frame allocations.
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

            GUILayout.Label("Next payout: " + player.NextPayoutFunds.ToString("N0"));

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

            for (int i = 0; i < _raceController.FundingProgrammes.Count; i++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[i];
                int playerProgress = _raceController.PlayerProgram.GetSatelliteCount(programme.CelestialBodyName);
                int asterProgress = _raceController.AsterProgram.GetSatelliteCount(programme.CelestialBodyName);
                int cobaltProgress = _raceController.CobaltProgram.GetSatelliteCount(programme.CelestialBodyName);
                int totalSatelliteCount = playerProgress + asterProgress + cobaltProgress;

                double playerCurrentPayout = programme.CalculateCurrentPayout(playerProgress, totalSatelliteCount);
                double asterCurrentPayout = programme.CalculateCurrentPayout(asterProgress, totalSatelliteCount);
                double cobaltCurrentPayout = programme.CalculateCurrentPayout(cobaltProgress, totalSatelliteCount);

                GUILayout.BeginVertical("box");

                // Center the highlighted title with layout spacing rather than GUIStyle.alignment.
                // This avoids requiring UnityEngine.TextRenderingModule only for TextAnchor.
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(programme.Name, _highlightedCardTitleStyle, GUILayout.ExpandWidth(false));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                // Keep programme details on the left while agency progress and projected payouts
                // use the previously empty right side of the desktop command-center window.
                GUILayout.BeginVertical(GUILayout.Width(400.0f));
                GUILayout.Label("Target: " + programme.CelestialBodyName);
                GUILayout.Label("Requirement: " + programme.RequiredSatellites + " qualifying satellite(s) in orbit");
                GUILayout.Label("Total Available Payout: " + programme.RewardFunds.ToString("N0"));
                GUILayout.EndVertical();

                GUILayout.Space(24.0f);
                GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                GUILayout.Label("Player Progress: " + playerProgress + (playerProgress == 1 ? " Satellite" : " Satellites"));
                GUILayout.Label("Player Current Payout: " + playerCurrentPayout.ToString("N0"));
                GUILayout.Label("Aster Progress: " + asterProgress + (asterProgress == 1 ? " Satellite" : " Satellites"));
                GUILayout.Label("Aster Current Payout: " + asterCurrentPayout.ToString("N0"));
                GUILayout.Label("Cobalt Progress: " + cobaltProgress + (cobaltProgress == 1 ? " Satellite" : " Satellites"));
                GUILayout.Label("Cobalt Current Payout: " + cobaltCurrentPayout.ToString("N0"));
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.Space(8.0f);
            }

            GUILayout.EndScrollView();
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

            for (int i = 0; i < _raceController.FundingProgrammes.Count; i++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[i];
                int satelliteCount = _raceController.PlayerProgram.GetSatelliteCount(programme.CelestialBodyName);
                bool requirementMet = satelliteCount >= programme.RequiredSatellites;

                GUILayout.BeginVertical("box");
                GUILayout.Label(programme.CelestialBodyName + " Orbital Milestone");
                GUILayout.Label("Progress: " + satelliteCount + "/" + programme.RequiredSatellites);
                GUILayout.Label("Milestone status: " + (requirementMet ? "COMPLETE" : "IN PROGRESS"));
                GUILayout.EndVertical();
                GUILayout.Space(8.0f);
            }

            GUILayout.Space(10.0f);
            GUILayout.Label("TRACKING RULES");
            GUILayout.Label("- Probe and Relay vessel types count as prototype satellites.");
            GUILayout.Label("- The vessel must be in the ORBITING situation.");
            GUILayout.Label("- Loaded and unloaded ProtoVessel records are scanned.");
            GUILayout.Label("- Counts refresh every 5 seconds rather than every frame.");

            GUILayout.EndScrollView();
        }

        private void DrawProgramCard(
            SpaceProgramState program,
            int? launchEtaDays,
            double launchProgressCostFunds)
        {
            GUILayout.BeginVertical("box");

            // Match the funding-card title treatment while keeping the project on its existing
            // Unity module references. Flexible spacing provides centering without TextAnchor.
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(program.Name, _highlightedCardTitleStyle, GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            // Launch and funding information stays on the left. Satellite coverage is grouped
            // into a second column so the wide desktop layout is used more efficiently.
            GUILayout.BeginVertical(GUILayout.Width(430.0f));
            GUILayout.Label("Funds: " + program.Funds.ToString("N0"));
            GUILayout.Label("Next Payout: " + program.NextPayoutFunds.ToString("N0"));
            GUILayout.Label(
                "Next Launch Planned: "
                + (string.IsNullOrEmpty(program.NextLaunchBodyName) ? "Planning" : program.NextLaunchBodyName));
            GUILayout.Label("Launch Progress: " + program.LaunchProgressPercent + "%");
            GUILayout.Label("Launch Progress Cost: " + launchProgressCostFunds.ToString("N0") + " (10% +)");
            GUILayout.Label(
                "ETA till Launch: "
                + (launchEtaDays.HasValue ? launchEtaDays.Value + " days" : "Awaiting Funding"));
            GUILayout.EndVertical();

            GUILayout.Space(24.0f);
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label("Kerbin satellites: " + program.GetSatelliteCount("Kerbin"));
            GUILayout.Label("Mun satellites: " + program.GetSatelliteCount("Mun"));
            GUILayout.Label("Minmus satellites: " + program.GetSatelliteCount("Minmus"));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
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

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
        private static SatelliteRaceController _raceController;
        private static RaceWindow _activeInstance;

        // Desktop-oriented size keeps all four views readable without creating additional pop-out windows.
        private Rect _windowRect = new Rect(70.0f, 55.0f, 900.0f, 720.0f);
        private Vector2 _fundingScrollPosition;
        private Vector2 _rivalsScrollPosition;
        private Vector2 _spaceRaceScrollPosition;
        private ActiveView _activeView = ActiveView.Overview;
        private bool _isDuplicateInstance;
        private bool _isVisible = true;
        private float _nextRefreshTime;

        public void Awake()
        {
            // KSP can instantiate an EveryScene addon more than once while editor scenes and
            // sub-scenes are loading. Only one RaceWindow may own Update/OnGUI at a time.
            if (_activeInstance != null && _activeInstance != this)
            {
                _isDuplicateInstance = true;
                Destroy(this);
                return;
            }

            _activeInstance = this;

            if (_raceController == null)
            {
                _raceController = new SatelliteRaceController();
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
            if (_isDuplicateInstance || _activeInstance != this)
            {
                return;
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
            if (_isDuplicateInstance || _activeInstance != this || !_isVisible || _raceController == null)
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
            GUILayout.Label("PROTOTYPE 0.2");
            GUILayout.Label("Kerbal Space Agency - Race Command Center");
            GUILayout.Space(8.0f);

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
            GUILayout.Label("F8: show/hide interface   |   tracking refresh: 5 seconds");
            GUI.DragWindow();
        }

        private void DrawOverview()
        {
            SpaceProgramState player = _raceController.PlayerProgram;
            int claimedProgrammes = 0;

            for (int i = 0; i < _raceController.FundingProgrammes.Count; i++)
            {
                if (_raceController.FundingProgrammes[i].IsClaimed)
                {
                    claimedProgrammes++;
                }
            }

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
            GUILayout.Label("Funding programmes decided: " + claimedProgrammes + "/" + _raceController.FundingProgrammes.Count);

            GUILayout.Space(10.0f);
            GUILayout.Label("SATELLITE NETWORK");
            GUILayout.Label("Kerbin orbit: " + player.GetSatelliteCount("Kerbin") + " qualifying satellite(s)");
            GUILayout.Label("Mun orbit: " + player.GetSatelliteCount("Mun") + " qualifying satellite(s)");
            GUILayout.Label("Minmus orbit: " + player.GetSatelliteCount("Minmus") + " qualifying satellite(s)");

            GUILayout.Space(12.0f);
            GUILayout.Label("RACE SUMMARY");
            for (int i = 0; i < _raceController.FundingProgrammes.Count; i++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[i];
                string status = programme.IsClaimed ? "Won by " + programme.WinnerProgramName : "Open";
                GUILayout.Label(programme.Name + ": " + status);
            }
        }

        private void DrawFundingTargets()
        {
            GUILayout.Label("FUNDING TARGETS");
            GUILayout.Space(5.0f);
            GUILayout.Label("Current prototype funding opportunities. No additional programmes are introduced in 0.2.");
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
                GUILayout.Label(programme.Name);

                if (programme.IsClaimed)
                {
                    GUILayout.Label("Winner: " + programme.WinnerProgramName);
                }

                GUILayout.Label("Target: " + programme.CelestialBodyName);
                GUILayout.Label("Requirement: " + programme.RequiredSatellites + " qualifying satellite(s) in orbit");
                GUILayout.Label("Total Available Payout: " + programme.RewardFunds.ToString("N0"));
                GUILayout.Label("Player Progress: " + playerProgress + "/" + programme.RequiredSatellites);
                GUILayout.Label("Player Current Payout: " + playerCurrentPayout.ToString("N0"));
                GUILayout.Label("Aster Progress: " + asterProgress + "/" + programme.RequiredSatellites);
                GUILayout.Label("Aster Current Payout: " + asterCurrentPayout.ToString("N0"));
                GUILayout.Label("Cobalt Progress: " + cobaltProgress + "/" + programme.RequiredSatellites);
                GUILayout.Label("Cobalt Current Payout: " + cobaltCurrentPayout.ToString("N0"));
                GUILayout.EndVertical();
                GUILayout.Space(8.0f);
            }

            GUILayout.EndScrollView();
        }

        private void DrawRivalAgencies()
        {
            GUILayout.Label("RIVAL AGENCIES");
            GUILayout.Space(5.0f);
            GUILayout.Label("Competitive intelligence for the two prototype rival agencies.");
            GUILayout.Space(8.0f);

            _rivalsScrollPosition = GUILayout.BeginScrollView(_rivalsScrollPosition);

            DrawProgramCard(_raceController.AsterProgram);
            GUILayout.Space(10.0f);
            DrawProgramCard(_raceController.CobaltProgram);

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
            GUILayout.Space(5.0f);
            GUILayout.Label("Live player tracking for the satellite milestones used by the current race.");
            GUILayout.Space(8.0f);

            _spaceRaceScrollPosition = GUILayout.BeginScrollView(_spaceRaceScrollPosition);

            for (int i = 0; i < _raceController.FundingProgrammes.Count; i++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[i];
                int satelliteCount = _raceController.PlayerProgram.GetSatelliteCount(programme.CelestialBodyName);
                bool requirementMet = satelliteCount >= programme.RequiredSatellites;

                GUILayout.BeginVertical("box");
                GUILayout.Label(programme.CelestialBodyName + " Orbital Milestone");
                GUILayout.Label("Associated programme: " + programme.Name);
                GUILayout.Label("Progress: " + satelliteCount + "/" + programme.RequiredSatellites);
                GUILayout.Label("Milestone status: " + (requirementMet ? "COMPLETE" : "IN PROGRESS"));
                GUILayout.Label("Race status: " + (programme.IsClaimed ? "Won by " + programme.WinnerProgramName : "Open"));
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

        private static void DrawProgramCard(SpaceProgramState program)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label(program.Name);
            GUILayout.Label("Funds: " + program.Funds.ToString("N0"));
            GUILayout.Label("Next Payout: " + program.NextPayoutFunds.ToString("N0"));
            GUILayout.Label(
                "Next Launch Planned: "
                + (string.IsNullOrEmpty(program.NextLaunchBodyName) ? "Planning" : program.NextLaunchBodyName));
            GUILayout.Label("Launch Progress %: " + program.LaunchProgressPercent + "%");
            GUILayout.Label("Kerbin satellites: " + program.GetSatelliteCount("Kerbin"));
            GUILayout.Label("Mun satellites: " + program.GetSatelliteCount("Mun"));
            GUILayout.Label("Minmus satellites: " + program.GetSatelliteCount("Minmus"));
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

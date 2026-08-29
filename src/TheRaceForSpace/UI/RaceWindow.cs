using TheRaceForSpace.Competition;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Programs;
using UnityEngine;

namespace TheRaceForSpace.UI
{
    /// <summary>
    /// Prototype command center with an overview and focused race-detail windows.
    /// Press F8 to show or hide the complete interface.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class RaceWindow : MonoBehaviour
    {
        private enum DetailWindow
        {
            None,
            Funding,
            Rivals,
            Milestones
        }

        private const float RefreshIntervalSeconds = 5.0f;
        private static SatelliteRaceController _raceController;

        private Rect _overviewWindowRect = new Rect(30.0f, 40.0f, 500.0f, 650.0f);
        private Rect _detailWindowRect = new Rect(550.0f, 40.0f, 690.0f, 650.0f);
        private Vector2 _fundingScrollPosition;
        private Vector2 _rivalsScrollPosition;
        private Vector2 _milestonesScrollPosition;
        private DetailWindow _activeDetailWindow;
        private bool _isVisible = true;
        private float _nextRefreshTime;

        public void Awake()
        {
            if (_raceController == null)
            {
                _raceController = new SatelliteRaceController();
            }
        }

        public void Update()
        {
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
            if (!_isVisible || _raceController == null)
            {
                return;
            }

            int overviewWindowId = GetInstanceID();
            _overviewWindowRect = GUILayout.Window(
                overviewWindowId,
                _overviewWindowRect,
                DrawOverviewWindow,
                "The Race for Space - Command Center");

            switch (_activeDetailWindow)
            {
                case DetailWindow.Funding:
                    _detailWindowRect = GUILayout.Window(
                        overviewWindowId + 1,
                        _detailWindowRect,
                        DrawFundingWindow,
                        "Funding Programmes");
                    break;

                case DetailWindow.Rivals:
                    _detailWindowRect = GUILayout.Window(
                        overviewWindowId + 2,
                        _detailWindowRect,
                        DrawRivalsWindow,
                        "Rival Agencies");
                    break;

                case DetailWindow.Milestones:
                    _detailWindowRect = GUILayout.Window(
                        overviewWindowId + 3,
                        _detailWindowRect,
                        DrawMilestonesWindow,
                        "Milestones & Satellite Tracking");
                    break;
            }
        }

        private void DrawOverviewWindow(int windowId)
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

            GUILayout.Label("PROTOTYPE 0.2");
            GUILayout.Label("Kerbal Space Agency - Race Command Center");
            GUILayout.Space(8.0f);

            GUILayout.Label("PROGRAM STATUS");
            GUILayout.Label("Race points: " + player.RacePoints);
            GUILayout.Label("Prototype funding won: " + player.AwardedFunds.ToString("N0"));
            GUILayout.Label("Funding programmes decided: " + claimedProgrammes + "/" + _raceController.FundingProgrammes.Count);

            GUILayout.Space(10.0f);
            GUILayout.Label("SATELLITE NETWORK");
            GUILayout.Label("Kerbin orbit: " + player.GetSatelliteCount("Kerbin") + " qualifying satellite(s)");
            GUILayout.Label("Mun orbit: " + player.GetSatelliteCount("Mun") + " qualifying satellite(s)");
            GUILayout.Label("Minmus orbit: " + player.GetSatelliteCount("Minmus") + " qualifying satellite(s)");

            GUILayout.Space(12.0f);
            GUILayout.Label("COMMAND CENTER");
            GUILayout.Label("Open one detailed view at a time:");

            if (GUILayout.Button("Funding Programmes", GUILayout.Height(42.0f)))
            {
                _activeDetailWindow = DetailWindow.Funding;
            }

            if (GUILayout.Button("Rival Agencies", GUILayout.Height(42.0f)))
            {
                _activeDetailWindow = DetailWindow.Rivals;
            }

            if (GUILayout.Button("Milestones & Satellite Tracking", GUILayout.Height(42.0f)))
            {
                _activeDetailWindow = DetailWindow.Milestones;
            }

            GUILayout.Space(12.0f);
            GUILayout.Label("RACE SUMMARY");
            for (int i = 0; i < _raceController.FundingProgrammes.Count; i++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[i];
                string status = programme.IsClaimed ? "Won by " + programme.WinnerProgramName : "Open";
                GUILayout.Label(programme.Name + ": " + status);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("F8: show/hide interface   |   tracking refresh: 5 seconds");
            GUI.DragWindow();
        }

        private void DrawFundingWindow(int windowId)
        {
            if (GUILayout.Button("Close", GUILayout.Width(90.0f)))
            {
                _activeDetailWindow = DetailWindow.None;
                return;
            }

            GUILayout.Space(5.0f);
            GUILayout.Label("Current prototype funding opportunities. No additional programmes are introduced in 0.2.");
            GUILayout.Space(8.0f);

            _fundingScrollPosition = GUILayout.BeginScrollView(_fundingScrollPosition);

            for (int i = 0; i < _raceController.FundingProgrammes.Count; i++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[i];
                int playerProgress = _raceController.PlayerProgram.GetSatelliteCount(programme.CelestialBodyName);
                string status = programme.IsClaimed ? "CLAIMED - " + programme.WinnerProgramName : "OPEN";

                GUILayout.BeginVertical("box");
                GUILayout.Label(programme.Name);
                GUILayout.Label("Status: " + status);
                GUILayout.Label("Target: " + programme.CelestialBodyName);
                GUILayout.Label("Requirement: " + programme.RequiredSatellites + " qualifying satellite(s) in orbit");
                GUILayout.Label("Player progress: " + playerProgress + "/" + programme.RequiredSatellites);
                GUILayout.Label("Prototype reward: " + programme.RewardFunds.ToString("N0"));
                GUILayout.Label("Aster progress: " + _raceController.AsterProgram.GetSatelliteCount(programme.CelestialBodyName));
                GUILayout.Label("Cobalt progress: " + _raceController.CobaltProgram.GetSatelliteCount(programme.CelestialBodyName));
                GUILayout.EndVertical();
                GUILayout.Space(8.0f);
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void DrawRivalsWindow(int windowId)
        {
            if (GUILayout.Button("Close", GUILayout.Width(90.0f)))
            {
                _activeDetailWindow = DetailWindow.None;
                return;
            }

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
            GUI.DragWindow();
        }

        private void DrawMilestonesWindow(int windowId)
        {
            if (GUILayout.Button("Close", GUILayout.Width(90.0f)))
            {
                _activeDetailWindow = DetailWindow.None;
                return;
            }

            GUILayout.Space(5.0f);
            GUILayout.Label("Live player tracking for the satellite milestones used by the current race.");
            GUILayout.Space(8.0f);

            _milestonesScrollPosition = GUILayout.BeginScrollView(_milestonesScrollPosition);

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
            GUI.DragWindow();
        }

        private static void DrawProgramCard(SpaceProgramState program)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label(program.Name);
            GUILayout.Label("Race points: " + program.RacePoints);
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

            GUILayout.Label(program.Name + " | points " + program.RacePoints + " | tracked satellites " + totalSatellites);
        }
    }
}

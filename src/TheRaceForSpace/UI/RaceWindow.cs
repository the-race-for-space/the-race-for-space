using TheRaceForSpace.Competition;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Programs;
using UnityEngine;

namespace TheRaceForSpace.UI
{
    /// <summary>
    /// Prototype command center with an overview and independently managed race-detail windows.
    /// Press F8 to show or hide the complete interface.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class RaceWindow : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 5.0f;
        private static SatelliteRaceController _raceController;

        // Desktop-oriented defaults keep the overview visible while leaving room for a larger detail window.
        // Every window remains draggable, so players can arrange the layout for their own resolution.
        private Rect _overviewWindowRect = new Rect(35.0f, 45.0f, 540.0f, 720.0f);
        private Rect _fundingWindowRect = new Rect(600.0f, 45.0f, 760.0f, 720.0f);
        private Rect _rivalsWindowRect = new Rect(650.0f, 95.0f, 720.0f, 620.0f);
        private Rect _milestonesWindowRect = new Rect(700.0f, 145.0f, 720.0f, 620.0f);

        private Vector2 _fundingScrollPosition;
        private Vector2 _rivalsScrollPosition;
        private Vector2 _milestonesScrollPosition;
        private bool _isFundingWindowVisible;
        private bool _isRivalsWindowVisible;
        private bool _isMilestonesWindowVisible;
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

            if (_isFundingWindowVisible)
            {
                _fundingWindowRect = GUILayout.Window(
                    overviewWindowId + 1,
                    _fundingWindowRect,
                    DrawFundingWindow,
                    "Funding Programmes");
            }

            if (_isRivalsWindowVisible)
            {
                _rivalsWindowRect = GUILayout.Window(
                    overviewWindowId + 2,
                    _rivalsWindowRect,
                    DrawRivalsWindow,
                    "Rival Agencies");
            }

            if (_isMilestonesWindowVisible)
            {
                _milestonesWindowRect = GUILayout.Window(
                    overviewWindowId + 3,
                    _milestonesWindowRect,
                    DrawMilestonesWindow,
                    "Milestones & Satellite Tracking");
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
            GUILayout.Label("COMMAND CENTER WINDOWS");
            GUILayout.Label("Open and arrange any combination of detailed views:");

            if (GUILayout.Button(_isFundingWindowVisible ? "Hide Funding Programmes" : "Open Funding Programmes", GUILayout.Height(42.0f)))
            {
                _isFundingWindowVisible = !_isFundingWindowVisible;
            }

            if (GUILayout.Button(_isRivalsWindowVisible ? "Hide Rival Agencies" : "Open Rival Agencies", GUILayout.Height(42.0f)))
            {
                _isRivalsWindowVisible = !_isRivalsWindowVisible;
            }

            if (GUILayout.Button(_isMilestonesWindowVisible ? "Hide Milestones & Satellite Tracking" : "Open Milestones & Satellite Tracking", GUILayout.Height(42.0f)))
            {
                _isMilestonesWindowVisible = !_isMilestonesWindowVisible;
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
                _isFundingWindowVisible = false;
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
                _isRivalsWindowVisible = false;
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
                _isMilestonesWindowVisible = false;
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

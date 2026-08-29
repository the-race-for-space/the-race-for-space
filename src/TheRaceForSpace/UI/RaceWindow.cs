using TheRaceForSpace.Competition;
using TheRaceForSpace.Funding;
using TheRaceForSpace.Programs;
using UnityEngine;

namespace TheRaceForSpace.UI
{
    /// <summary>
    /// Basic prototype command-center window. Press F8 to show or hide it.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public sealed class RaceWindow : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 5.0f;
        private static SatelliteRaceController _raceController;
        private Rect _windowRect = new Rect(90.0f, 90.0f, 520.0f, 620.0f);
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

            _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, "The Race for Space - Satellite Prototype");
        }

        private void DrawWindow(int windowId)
        {
            SpaceProgramState player = _raceController.PlayerProgram;

            GUILayout.Label("OVERVIEW");
            GUILayout.Label("Race points: " + player.RacePoints + "    Prototype funding won: " + player.AwardedFunds.ToString("N0"));
            GUILayout.Space(8.0f);

            GUILayout.Label("FUNDING PROGRAMMES");
            for (int i = 0; i < _raceController.FundingProgrammes.Count; i++)
            {
                FundingProgramme programme = _raceController.FundingProgrammes[i];
                string winner = programme.IsClaimed ? programme.WinnerProgramName : "Open";
                GUILayout.Label(programme.Name + " | " + programme.CelestialBodyName + " | " + programme.RequiredSatellites + " satellite(s) | " + programme.RewardFunds.ToString("N0") + " | " + winner);
            }

            GUILayout.Space(8.0f);
            GUILayout.Label("RIVAL AGENCIES");
            DrawProgram(_raceController.AsterProgram);
            DrawProgram(_raceController.CobaltProgram);

            GUILayout.Space(8.0f);
            GUILayout.Label("SATELLITE TRACKING");
            DrawProgram(player);

            GUILayout.Space(8.0f);
            GUILayout.Label("MILESTONES");
            GUILayout.Label("Kerbin network: " + player.GetSatelliteCount("Kerbin") + "/2");
            GUILayout.Label("Mun survey satellite: " + player.GetSatelliteCount("Mun") + "/1");
            GUILayout.Label("Minmus relay satellite: " + player.GetSatelliteCount("Minmus") + "/1");

            GUILayout.Space(10.0f);
            GUILayout.Label("F8: show/hide   |   vessel scan refresh: every 5 seconds");
            GUI.DragWindow();
        }

        private static void DrawProgram(SpaceProgramState program)
        {
            GUILayout.Label(program.Name + " - Kerbin " + program.GetSatelliteCount("Kerbin") + ", Mun " + program.GetSatelliteCount("Mun") + ", Minmus " + program.GetSatelliteCount("Minmus") + ", points " + program.RacePoints);
        }
    }
}

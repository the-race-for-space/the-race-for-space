using KSP.UI.Screens;
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
        private const float WindowHeight = 120.0f;
        private const string LauncherIconTexturePath =
            "Squad/PartList/SimpleIcons/R&D_node_icon_flightControl";

        private static FlightActiveUI _activeInstance;

        private ApplicationLauncherButton _launcherButton;
        private GUI.WindowFunction _drawWindowFunction;
        private Rect _windowRect;
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
            GUILayout.Label("Flight contract list will appear here.");
            GUILayout.FlexibleSpace();
            GUI.DragWindow();
        }
    }
}

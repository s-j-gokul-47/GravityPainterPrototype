using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple developer-only scene for jumping directly to a procedural menu level.
/// Open this scene in the editor, enter a level number (3+), and press OK.
/// </summary>
public class DeveloperProceduralLevelSelect : MonoBehaviour
{
    private const float PanelWidth = 480f;
    private const float PanelHeight = 220f;

    private string _levelInput;
    private string _message;
    private bool _requestedInitialFocus;

    private void Awake()
    {
        EnsureCameraExists();
        int selectedLevel = Mathf.Max(
            LevelProgress.ProceduralCampaignLevel,
            LevelProgress.GetSelectedMenuLevel());
        _levelInput = selectedLevel.ToString();
        _message = "Enter a procedural menu level (" + LevelProgress.ProceduralCampaignLevel + "+).";
    }

    private void OnGUI()
    {
        EnsureGuiStyle();

        Rect panelRect = new Rect(
            (Screen.width - PanelWidth) * 0.5f,
            (Screen.height - PanelHeight) * 0.5f,
            PanelWidth,
            PanelHeight);

        GUI.Box(panelRect, "Developer Procedural Level Select");

        GUILayout.BeginArea(new Rect(panelRect.x + 24f, panelRect.y + 44f, panelRect.width - 48f, panelRect.height - 68f));
        GUILayout.Label("Procedural Level Number");
        GUI.SetNextControlName("LevelInput");
        _levelInput = GUILayout.TextField(_levelInput ?? string.Empty, 8);

        GUILayout.Space(10f);
        GUILayout.Label(_message);

        GUILayout.Space(14f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("OK", GUILayout.Height(36f)))
        {
            OpenSelectedLevel();
        }

        if (GUILayout.Button("Main Menu", GUILayout.Height(36f)))
        {
            SceneManager.LoadScene("MainMenu");
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        if (!_requestedInitialFocus && Event.current.type == EventType.Layout)
        {
            GUI.FocusControl("LevelInput");
            _requestedInitialFocus = true;
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            OpenSelectedLevel();
            Event.current.Use();
        }
    }

    private void OpenSelectedLevel()
    {
        if (!int.TryParse(_levelInput, out int menuLevel))
        {
            _message = "Please enter a valid number.";
            return;
        }

        if (menuLevel < LevelProgress.ProceduralCampaignLevel)
        {
            _message = "Procedural levels start at Level " + LevelProgress.ProceduralCampaignLevel + ".";
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(LevelProgress.ProceduralSceneName))
        {
            _message = "Procedural scene is missing from Build Settings: " + LevelProgress.ProceduralSceneName;
            return;
        }

        LevelProgress.SetSelectedMenuLevel(menuLevel);
        ProceduralSession.MarkFreshRunFromMenu();
        SceneManager.LoadScene(LevelProgress.ProceduralSceneName);
    }

    private static void EnsureGuiStyle()
    {
        GUI.skin.box.fontSize = 22;
        GUI.skin.box.alignment = TextAnchor.UpperCenter;
        GUI.skin.label.fontSize = 18;
        GUI.skin.textField.fontSize = 20;
        GUI.skin.button.fontSize = 18;
    }

    private static void EnsureCameraExists()
    {
        if (Camera.main != null || Camera.allCamerasCount > 0)
        {
            return;
        }

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        var cameraComponent = cameraObject.AddComponent<Camera>();
        cameraComponent.clearFlags = CameraClearFlags.SolidColor;
        cameraComponent.backgroundColor = new Color(0.08f, 0.1f, 0.15f, 1f);
        cameraObject.AddComponent<AudioListener>();
    }
}

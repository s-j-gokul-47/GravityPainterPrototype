#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public static class SetupMainMenuVideoBackground
{
    private const string ScenePath = "Assets/Scenes/Menus/MainMenu.unity";
    private const string VideoPath = "Assets/Art/Video/Mainmenu.mp4";

    [MenuItem("Gravity Painter/Setup Main Menu Video Background")]
    public static void SetupFromMenu()
    {
        Setup();
        EditorUtility.DisplayDialog(
            "Main menu video",
            "BackGround now uses Mainmenu.mp4.\n\n"
            + "Select Canvas > BackGround to tweak resolution, invert, loop, and audio in the Inspector.",
            "OK");
    }

    public static void Setup()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        bool openedTempScene = false;
        if (scene.path != ScenePath)
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                Debug.LogError("SetupMainMenuVideoBackground: scene not found at " + ScenePath);
                return;
            }

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            openedTempScene = true;
        }

        Transform backgroundTransform = FindBackground();
        if (backgroundTransform == null)
        {
            Debug.LogError("SetupMainMenuVideoBackground: BackGround object not found under Canvas.");
            return;
        }

        GameObject backgroundObject = backgroundTransform.gameObject;
        Image legacyImage = backgroundObject.GetComponent<Image>();
        if (legacyImage != null)
        {
            Object.DestroyImmediate(legacyImage);
        }

        RawImage rawImage = backgroundObject.GetComponent<RawImage>();
        if (rawImage == null)
        {
            rawImage = backgroundObject.AddComponent<RawImage>();
        }

        VideoPlayer player = backgroundObject.GetComponent<VideoPlayer>();
        if (player == null)
        {
            player = backgroundObject.AddComponent<VideoPlayer>();
        }

        MainMenuVideoBackground background = backgroundObject.GetComponent<MainMenuVideoBackground>();
        if (background == null)
        {
            background = backgroundObject.AddComponent<MainMenuVideoBackground>();
        }

        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
        SerializedObject serialized = new SerializedObject(background);
        serialized.FindProperty("videoClip").objectReferenceValue = clip;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        RectTransform rect = backgroundObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.SetAsFirstSibling();

        background.ApplySettings();
        EditorUtility.SetDirty(backgroundObject);
        EditorSceneManager.MarkSceneDirty(scene);

        if (openedTempScene)
        {
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static Transform FindBackground()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return null;
        }

        Transform direct = canvas.transform.Find("BackGround");
        if (direct != null)
        {
            return direct;
        }

        foreach (Transform child in canvas.transform)
        {
            if (child.name == "BackGround")
            {
                return child;
            }
        }

        return null;
    }
}
#endif

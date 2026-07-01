#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupGameplayMusic
{
    private const string MusicResourcePath = "Audio/BeyondTheHighPass";

    private static readonly string[] ScenesWithGameplayMusic =
    {
        "Assets/Scenes/Menus/MainMenu.unity",
        "Assets/Scenes/LoadingScene.unity",
        "Assets/Scenes/Levels/Level 1.unity",
        "Assets/Scenes/Levels/Level 2.unity",
        "Assets/Scenes/Levels/Level 3.unity",
        "Assets/Scenes/Levels/Level 4.unity",
        "Assets/Scenes/Levels/Level 5.unity",
        "Assets/Procedural(test).unity",
        "Assets/Scenes/GameScene.unity",
        "Assets/Scenes/Menus/DeveloperProceduralLevelSelect.unity",
    };

    [MenuItem("Gravity Painter/Setup Gameplay Music In Open Scene")]
    public static void SetupOpenScene()
    {
        EnsureInActiveScene();
        EditorUtility.DisplayDialog(
            "Gameplay music",
            "Added or updated GameplayMusic in the open scene.\n\n"
            + "It stays visible in the Hierarchy and only plays during active gameplay.",
            "OK");
    }

    [MenuItem("Gravity Painter/Setup Gameplay Music In All Scenes")]
    public static void SetupAllScenes()
    {
        string originalScene = SceneManager.GetActiveScene().path;
        int updated = 0;

        foreach (string scenePath in ScenesWithGameplayMusic)
        {
            if (!File.Exists(scenePath))
                continue;

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (EnsureInActiveScene())
                updated++;
        }

        if (!string.IsNullOrEmpty(originalScene) && File.Exists(originalScene))
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Gameplay music",
                "Updated " + updated + " scene(s) with a permanent GameplayMusic object.",
                "OK");
        }
        else
        {
            Debug.Log("SetupGameplayMusic: updated " + updated + " scene(s).");
        }
    }

    public static void RunFromBatch()
    {
        SetupAllScenes();
        EditorSceneManager.SaveOpenScenes();
        EditorApplication.Exit(0);
    }

    public static bool EnsureInActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return false;

        GameplayMusicController existing = Object.FindFirstObjectByType<GameplayMusicController>();
        if (existing != null)
        {
            Configure(existing.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        GameObject musicObject = new GameObject(GameplayMusicController.ObjectName);
        SceneManager.MoveGameObjectToScene(musicObject, scene);
        Configure(musicObject);
        EditorSceneManager.MarkSceneDirty(scene);
        return true;
    }

    private static void Configure(GameObject musicObject)
    {
        musicObject.name = GameplayMusicController.ObjectName;

        AudioSource source = musicObject.GetComponent<AudioSource>();
        if (source == null)
            source = musicObject.AddComponent<AudioSource>();

        GameplayMusicController controller = musicObject.GetComponent<GameplayMusicController>();
        if (controller == null)
            controller = musicObject.AddComponent<GameplayMusicController>();

        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;

        AudioClip clip = Resources.Load<AudioClip>(MusicResourcePath);
        if (clip != null)
            source.clip = clip;

        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty clipProperty = serialized.FindProperty("musicClip");
        if (clipProperty != null && clip != null)
            clipProperty.objectReferenceValue = clip;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(musicObject);
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sets up the open scene for procedural Step 2 playtesting.
/// </summary>
public static class SetupProceduralLevelScene
{
    [MenuItem("Gravity Painter/Setup Procedural Level Scene (Step 2)")]
    public static void SetupOpenScene()
    {
        ProceduralPathGeneratorTest.WireConfigPrefabs();

        LevelGenConfig config = LoadDefaultConfig();
        if (config == null)
        {
            EditorUtility.DisplayDialog(
                "Missing config",
                "Create LevelGenConfig_Default first:\nGravity Painter → Create Level Gen Config",
                "OK");
            return;
        }

        WireGlbLayout(config);

        GameObject builderRoot = GameObject.Find("ProceduralLevel");
        if (builderRoot == null)
        {
            builderRoot = new GameObject("ProceduralLevel");
            Undo.RegisterCreatedObjectUndo(builderRoot, "Setup Procedural Level");
        }

        ProceduralLevelBuilder builder = builderRoot.GetComponent<ProceduralLevelBuilder>();
        if (builder == null)
        {
            builder = Undo.AddComponent<ProceduralLevelBuilder>(builderRoot);
        }

        Transform levelRoot = builderRoot.transform.Find("LevelRoot");
        if (levelRoot == null)
        {
            GameObject levelRootObject = new GameObject("LevelRoot");
            Undo.RegisterCreatedObjectUndo(levelRootObject, "Setup Procedural Level");
            levelRootObject.transform.SetParent(builderRoot.transform, false);
            levelRoot = levelRootObject.transform;
        }

        BallController ball = EnsureBall();
        EnsureCameraSystems();
        EnsureEventSystem();
        GameObject levelCompleteCanvas = EnsureLevelCompleteCanvas(builder);
        DisableLegacyPathTester();

        SerializedObject so = new SerializedObject(builder);
        so.FindProperty("config").objectReferenceValue = config;
        so.FindProperty("levelRoot").objectReferenceValue = levelRoot;
        so.FindProperty("ball").objectReferenceValue = ball;
        so.FindProperty("levelCompletePanel").objectReferenceValue = levelCompleteCanvas;
        so.FindProperty("buildOnStart").boolValue = true;
        so.FindProperty("seed").intValue = 12345;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(builderRoot);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Procedural Step 2 ready",
            "Scene setup complete.\n\n" +
            "Press Play to build from seed 12345.\n" +
            "Change seed on ProceduralLevel → Procedural Level Builder, then Play again.\n\n" +
            "Paint tiles, roll the ball to the last tile to win.\n" +
            "Level-complete screen (Restart / New Level / Home) is wired.",
            "OK");
    }

    private static GameObject EnsureLevelCompleteCanvas(ProceduralLevelBuilder builder)
    {
        GameObject canvas = LevelCompleteCanvasFactory.EnsureCanvas(builder);
        Undo.RegisterCreatedObjectUndo(canvas, "Setup Procedural Level");
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject(
            "EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Setup Procedural Level");
    }

    private static LevelGenConfig LoadDefaultConfig()
    {
        LevelGenConfig config = AssetDatabase.LoadAssetAtPath<LevelGenConfig>(
            "Assets/Settings/LevelGenConfig_Default.asset");
        if (config != null)
        {
            return config;
        }

        string[] guids = AssetDatabase.FindAssets("t:LevelGenConfig");
        if (guids.Length == 0)
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<LevelGenConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static void WireGlbLayout(LevelGenConfig config)
    {
        if (config.glbLayout != null)
        {
            return;
        }

        TileGlbReferenceLayoutAsset layout = AssetDatabase.LoadAssetAtPath<TileGlbReferenceLayoutAsset>(
            "Assets/Settings/TileGlbReferenceLayout.asset");
        if (layout == null)
        {
            return;
        }

        config.glbLayout = layout;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    private static BallController EnsureBall()
    {
        BallController existing = Object.FindFirstObjectByType<BallController>();
        if (existing != null)
        {
            return existing;
        }

        GameObject ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballObject.name = "Ball";
        Undo.RegisterCreatedObjectUndo(ballObject, "Setup Procedural Level");

        Object.DestroyImmediate(ballObject.GetComponent<MeshRenderer>());
        Object.DestroyImmediate(ballObject.GetComponent<MeshFilter>());

        SphereCollider sphere = ballObject.GetComponent<SphereCollider>();
        sphere.radius = 0.5f;

        Rigidbody body = ballObject.AddComponent<Rigidbody>();
        body.mass = 1f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;

        BallController controller = ballObject.AddComponent<BallController>();
        ballObject.transform.position = new Vector3(0f, 1f, 0f);
        return controller;
    }

    private static void EnsureCameraSystems()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        if (camera.GetComponent<CameraFollow>() == null)
        {
            Undo.AddComponent<CameraFollow>(camera.gameObject);
        }

        if (camera.GetComponent<InputManager>() == null)
        {
            Undo.AddComponent<InputManager>(camera.gameObject);
        }
    }

    private static void DisableLegacyPathTester()
    {
        ProceduralPathVisualizer visualizer = Object.FindFirstObjectByType<ProceduralPathVisualizer>();
        if (visualizer == null)
        {
            return;
        }

        visualizer.gameObject.SetActive(false);
    }
}
#endif

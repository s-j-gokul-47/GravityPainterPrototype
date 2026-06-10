#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds Assets/Prefabs/Visuals/SciFiBallVisual.prefab from the GLB model and wires it to Ball in levels.
/// Requires com.unity.cloud.gltfast so Unity can import .glb files.
/// </summary>
public static class ApplySciFiBall3DModel
{
    private const string GlbAssetPath = GlbModelPaths.SciFiBall;
    private const string VisualPrefabPath = "Assets/Prefabs/Visuals/SciFiBallVisual.prefab";
    private const string ResourcesPrefabPath = "Assets/Resources/Prefabs/SciFiBallVisual.prefab";

    private static readonly string[] LevelScenePaths =
    {
        "Assets/Scenes/Levels/Level 1.unity",
        "Assets/Scenes/Levels/Level 2.unity",
        "Assets/Scenes/Levels/Level 3.unity",
        "Assets/Scenes/Levels/Level 4.unity",
        "Assets/Scenes/Levels/Level 5.unity",
    };

    [MenuItem("Gravity Painter/Apply Sci-Fi Ball 3D Model (GLB) To Levels")]
    public static void ApplyToLevels()
    {
        GameObject visualPrefab = BuildOrUpdateVisualPrefab();
        if (visualPrefab == null)
        {
            return;
        }

        string previousScene = SceneManager.GetActiveScene().path;
        int updated = 0;

        foreach (string scenePath in LevelScenePaths)
        {
            if (!File.Exists(scenePath))
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            BallController[] balls = Object.FindObjectsByType<BallController>(FindObjectsSortMode.None);
            foreach (BallController ball in balls)
            {
                ApplyToBall(ball.gameObject, visualPrefab);
                updated++;
            }

            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
        {
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }

        EditorUtility.DisplayDialog(
            "Sci-Fi Ball 3D Model applied",
            "Created/updated prefab:\n" + VisualPrefabPath + "\n\n" +
            "Updated " + updated + " Ball object(s).\n\n" +
            "Physics unchanged (SphereCollider radius 0.5, diameter 1).",
            "OK");
    }

    [MenuItem("Gravity Painter/Apply Sci-Fi Ball 3D Model (GLB) To Selected Ball")]
    public static void ApplyToSelected()
    {
        GameObject ball = Selection.activeGameObject;
        if (ball == null || ball.GetComponent<BallController>() == null)
        {
            EditorUtility.DisplayDialog("Select Ball", "Select the Ball object with BallController.", "OK");
            return;
        }

        GameObject visualPrefab = BuildOrUpdateVisualPrefab();
        if (visualPrefab == null)
        {
            return;
        }

        ApplyToBall(ball, visualPrefab);
        EditorSceneManager.MarkSceneDirty(ball.scene);
    }

    [MenuItem("Gravity Painter/Reimport Sci-Fi Ball GLB")]
    public static void ReimportGlb()
    {
        string path = ResolveGlbAssetPath();
        if (path == null)
        {
            EditorUtility.DisplayDialog(
                "Missing GLB",
                "Could not find:\n" + GlbAssetPath + "\n\nPlace the file in your Assets folder.",
                "OK");
            return;
        }

        if (!EnsureGlbImporterReady(path, showDialog: true))
        {
            return;
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Reimported", "Reimported:\n" + path, "OK");
    }

    private static GameObject BuildOrUpdateVisualPrefab()
    {
        string glbPath = ResolveGlbAssetPath();
        if (glbPath == null)
        {
            ShowMissingGlbDialog(null);
            return null;
        }

        if (!EnsureGlbImporterReady(glbPath, showDialog: true))
        {
            return null;
        }

        ConfigureGlbImporter(glbPath);

        GameObject glbRoot = LoadGlbRoot(glbPath);
        if (glbRoot == null)
        {
            AssetDatabase.ImportAsset(glbPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            glbRoot = LoadGlbRoot(glbPath);
        }

        Directory.CreateDirectory("Assets/Prefabs/Visuals");
        Directory.CreateDirectory("Assets/Resources/Prefabs");

        GameObject instance;
        if (glbRoot != null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(glbRoot);
            if (instance == null)
            {
                instance = Object.Instantiate(glbRoot);
            }
        }
        else
        {
            instance = BuildVisualFromImportedAssets(glbPath);
            if (instance == null)
            {
                EditorUtility.DisplayDialog(
                    "GLB import failed",
                    "Could not load a mesh from:\n" + glbPath + "\n\n" +
                    "1. Wait for Package Manager to finish (glTFast 6.18+)\n" +
                    "2. Gravity Painter → Reimport Sci-Fi Ball GLB\n" +
                    "3. Run Apply Sci-Fi Ball 3D Model again",
                    "OK");
                return null;
            }
        }

        instance.name = "SciFiBallVisual";
        StripPhysics(instance);
        ConvertRenderersToUrp(instance);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, VisualPrefabPath);
        Object.DestroyImmediate(instance);

        GameObject resourcesCopy = PrefabUtility.SaveAsPrefabAsset(prefab, ResourcesPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Sci-Fi Ball: saved " + VisualPrefabPath + " and " + ResourcesPrefabPath);
        return prefab != null ? prefab : resourcesCopy;
    }

    private static string ResolveGlbAssetPath()
    {
        if (AssetExists(GlbAssetPath))
        {
            return GlbAssetPath;
        }

        string fileName = Path.GetFileName(GlbAssetPath);
        string fullPath = Path.Combine(Application.dataPath, "..", fileName);
        if (File.Exists(fullPath))
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/Models/GLB"));
            File.Copy(fullPath, Path.Combine(Application.dataPath, "Art/Models/GLB", fileName), overwrite: true);
            AssetDatabase.Refresh();
            if (AssetExists(GlbAssetPath))
            {
                return GlbAssetPath;
            }
        }

        string[] guids = AssetDatabase.FindAssets("Sci-Fi Ball");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        guids = AssetDatabase.FindAssets("t:GameObject SciFi Ball");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".glb", System.StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }
        }

        return null;
    }

    private static bool AssetExists(string assetPath)
    {
        return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath));
    }

    private static bool EnsureGlbImporterReady(string path, bool showDialog)
    {
        AssetImporter importer = AssetImporter.GetAtPath(path);
        if (IsDefaultImporter(importer))
        {
            if (showDialog)
            {
                ShowMissingGlbDialog(path);
            }

            return false;
        }

        return true;
    }

    private static bool IsDefaultImporter(AssetImporter importer)
    {
        return importer != null && importer.GetType().Name == "DefaultImporter";
    }

    private static void ShowMissingGlbDialog(string path)
    {
        bool hasGltfast = HasGltfastPackage();
        string message =
            "Unity cannot import this GLB yet.\n\n" +
            "File: " + (path ?? GlbAssetPath) + "\n\n";

        if (!hasGltfast)
        {
            message +=
                "This project needs the glTFast package (com.unity.cloud.gltfast).\n" +
                "It was added to Packages/manifest.json — wait for Package Manager to finish, then:\n\n" +
                "1. Gravity Painter → Reimport Sci-Fi Ball GLB\n" +
                "2. Gravity Painter → Apply Sci-Fi Ball 3D Model (GLB) To Levels";
        }
        else
        {
            message +=
                "glTFast is installed but the file may still be on the wrong importer.\n\n" +
                "Run: Gravity Painter → Reimport Sci-Fi Ball GLB\n" +
                "Then apply the ball to levels again.";
        }

        EditorUtility.DisplayDialog("Sci-Fi Ball GLB not ready", message, "OK");
    }

    private static bool HasGltfastPackage()
    {
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        if (!File.Exists(manifestPath))
        {
            return false;
        }

        string manifest = File.ReadAllText(manifestPath);
        return manifest.Contains("com.unity.cloud.gltfast");
    }

    private static void ConfigureGlbImporter(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            return;
        }

        importer.globalScale = 1f;
        importer.bakeAxisConversion = true;
        importer.importAnimation = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.SaveAndReimport();
    }

    private static GameObject LoadGlbRoot(string path)
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root != null)
        {
            return root;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        GameObject best = null;
        foreach (Object asset in assets)
        {
            if (asset is not GameObject go || string.IsNullOrEmpty(go.name))
            {
                continue;
            }

            if (go.GetComponentInChildren<Renderer>(true) != null)
            {
                return go;
            }

            best ??= go;
        }

        return best;
    }

    /// <summary>
    /// Fallback when the GLB main asset is not a GameObject but meshes/materials were imported.
    /// </summary>
    private static GameObject BuildVisualFromImportedAssets(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        Mesh mesh = null;
        Material material = null;

        foreach (Object asset in assets)
        {
            if (asset is Mesh m && (mesh == null || m.vertexCount > mesh.vertexCount))
            {
                mesh = m;
            }
            else if (asset is Material mat && material == null)
            {
                material = mat;
            }
        }

        if (mesh == null)
        {
            return null;
        }

        GameObject go = new GameObject("SciFiBallVisual");
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material != null ? material : GetDefaultMaterial();
        return go;
    }

    private static Material GetDefaultMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        return new Material(shader);
    }

    private static void StripPhysics(GameObject root)
    {
        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(col);
        }

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.DestroyImmediate(body);
        }
    }

    private static void ConvertRenderersToUrp(GameObject root)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            return;
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat == null || mat.shader == urpLit)
                {
                    continue;
                }

                mat.shader = urpLit;
                if (mat.HasProperty("_BaseMap") && mat.mainTexture != null)
                {
                    mat.SetTexture("_BaseMap", mat.mainTexture);
                }

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", mat.color);
                }
            }
        }
    }

    private static void ApplyToBall(GameObject ball, GameObject visualPrefab)
    {
        BallController controller = ball.GetComponent<BallController>();
        if (controller == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("useSciFiBallVisual").boolValue = true;
        so.FindProperty("sciFiBallVisualPrefab").objectReferenceValue = visualPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        controller.EnsureSciFiBallVisual(replaceExisting: true);

        MeshRenderer rootRenderer = ball.GetComponent<MeshRenderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        EditorUtility.SetDirty(ball);
    }
}
#endif

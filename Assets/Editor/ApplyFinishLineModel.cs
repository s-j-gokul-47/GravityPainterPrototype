#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Imports Finish_Line.glb, builds a runtime prefab, and wires FinishLineVisual on goal tiles.
/// </summary>
public static class ApplyFinishLineModel
{
    private const string GlbAssetPath = GlbModelPaths.FinishLine;
    private const string VisualPrefabPath = "Assets/Prefabs/Visuals/FinishLineVisual.prefab";
    private const string ResourcesPrefabPath = "Assets/Resources/Prefabs/FinishLineVisual.prefab";

    private static readonly string[] LevelScenePaths =
    {
        "Assets/Scenes/Levels/Level 1.unity",
        "Assets/Scenes/Levels/Level 2.unity",
        "Assets/Scenes/Levels/Level 3.unity",
        "Assets/Scenes/Levels/Level 4.unity",
        "Assets/Scenes/Levels/Level 5.unity",
        "Assets/Procedural(test).unity",
    };

    [MenuItem("Gravity Painter/Apply Finish Line GLB To Levels")]
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
            FinishLine[] finishLines = Object.FindObjectsByType<FinishLine>(FindObjectsSortMode.None);
            foreach (FinishLine finish in finishLines)
            {
                ApplyToFinishTile(finish.gameObject, visualPrefab);
                updated++;
            }

            EditorSceneManager.SaveScene(scene);
        }

        if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
        {
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }

        EditorUtility.DisplayDialog(
            "Finish line applied",
            "Created/updated prefab:\n" + ResourcesPrefabPath + "\n\n" +
            "Wired " + updated + " finish tile(s).\n" +
            "Procedural goal tiles spawn the model at runtime via FinishLine.",
            "OK");
    }

    [MenuItem("Gravity Painter/Reimport Finish Line GLB")]
    public static void ReimportGlb()
    {
        string path = ResolveGlbAssetPath();
        if (path == null)
        {
            EditorUtility.DisplayDialog("Missing GLB", "Could not find Finish_Line.glb under Assets.", "OK");
            return;
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Reimported", "Reimported:\n" + path, "OK");
    }

    private static void ApplyToFinishTile(GameObject tile, GameObject visualPrefab)
    {
        FinishLineVisual visual = tile.GetComponent<FinishLineVisual>();
        if (visual == null)
        {
            visual = tile.AddComponent<FinishLineVisual>();
        }

        SerializedObject so = new SerializedObject(visual);
        so.FindProperty("finishLinePrefab").objectReferenceValue = visualPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        visual.RebuildVisual();
        EditorUtility.SetDirty(tile);
    }

    private static GameObject BuildOrUpdateVisualPrefab()
    {
        string glbPath = ResolveGlbAssetPath();
        if (glbPath == null)
        {
            EditorUtility.DisplayDialog(
                "Missing GLB",
                "Place Finish_Line.glb in:\n" + GlbAssetPath,
                "OK");
            return null;
        }

        ConfigureGlbImporter(glbPath);

        GameObject glbRoot = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);
        if (glbRoot == null)
        {
            AssetDatabase.ImportAsset(glbPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            glbRoot = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);
        }

        if (glbRoot == null)
        {
            EditorUtility.DisplayDialog(
                "GLB import failed",
                "Could not load Finish_Line.glb.\nRun Gravity Painter → Reimport Finish Line GLB.",
                "OK");
            return null;
        }

        Directory.CreateDirectory("Assets/Prefabs/Visuals");
        Directory.CreateDirectory("Assets/Resources/Prefabs");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(glbRoot);
        if (instance == null)
        {
            instance = Object.Instantiate(glbRoot);
        }

        instance.name = "FinishLineVisual";
        StripPhysics(instance);
        TileMeshMaterialUtility.FixRenderersToUrpPreservingModelLook(instance);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, VisualPrefabPath);
        Object.DestroyImmediate(instance);

        GameObject resourcesCopy = PrefabUtility.SaveAsPrefabAsset(prefab, ResourcesPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return resourcesCopy != null ? resourcesCopy : prefab;
    }

    private static string ResolveGlbAssetPath()
    {
        if (AssetDatabase.AssetPathToGUID(GlbAssetPath).Length > 0)
        {
            return GlbAssetPath;
        }

        string fileName = Path.GetFileName(GlbAssetPath);
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string rootGlb = Path.Combine(projectRoot, fileName);
        if (File.Exists(rootGlb))
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/Models/GLB"));
            File.Copy(rootGlb, Path.Combine(Application.dataPath, "Art/Models/GLB", fileName), overwrite: true);
            AssetDatabase.Refresh();
            return GlbAssetPath;
        }

        string[] guids = AssetDatabase.FindAssets("Finish_Line t:GameObject");
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
}
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports coins.glb, builds a runtime prefab, and wires CoinVisual on the gameplay coin prefab.
/// </summary>
public static class ApplyCoinModel
{
    private const string GlbAssetPath = GlbModelPaths.Coins;
    private const string CoinPrefabPath = "Assets/Prefabs/Gameplay/Coin.prefab";
    private const string VisualPrefabPath = "Assets/Prefabs/Visuals/CoinVisual.prefab";
    private const string ResourcesPrefabPath = "Assets/Resources/Prefabs/CoinVisual.prefab";

    [MenuItem("Gravity Painter/Apply Coin GLB To Prefab")]
    public static void ApplyToCoinPrefab()
    {
        ApplyToCoinPrefabInternal(showDialog: true);
    }

    private static void ApplyToCoinPrefabInternal(bool showDialog)
    {
        GameObject visualPrefab = BuildOrUpdateVisualPrefab();
        if (visualPrefab == null)
        {
            return;
        }

        if (!File.Exists(CoinPrefabPath))
        {
            EditorUtility.DisplayDialog("Missing prefab", "Could not find:\n" + CoinPrefabPath, "OK");
            return;
        }

        GameObject coinRoot = PrefabUtility.LoadPrefabContents(CoinPrefabPath);
        RemoveLegacyCylinderMesh(coinRoot);
        WireCoinVisual(coinRoot, visualPrefab);

        Transform rootTransform = coinRoot.transform;
        rootTransform.localPosition = Vector3.zero;
        rootTransform.localRotation = Quaternion.identity;
        rootTransform.localScale = Vector3.one;

        PrefabUtility.SaveAsPrefabAsset(coinRoot, CoinPrefabPath);
        PrefabUtility.UnloadPrefabContents(coinRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Coin model applied",
                "Created/updated:\n" + ResourcesPrefabPath + "\n\n" +
                "Updated gameplay prefab:\n" + CoinPrefabPath + "\n\n" +
                "Procedural levels use the same Coin prefab at runtime.",
                "OK");
        }
    }

    [MenuItem("Gravity Painter/Reimport Coin GLB")]
    public static void ReimportGlb()
    {
        string path = ResolveGlbAssetPath();
        if (path == null)
        {
            EditorUtility.DisplayDialog("Missing GLB", "Could not find coins.glb under Assets.", "OK");
            return;
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Reimported", "Reimported:\n" + path, "OK");
    }

    private static void WireCoinVisual(GameObject coinRoot, GameObject visualPrefab)
    {
        CoinVisual visual = coinRoot.GetComponent<CoinVisual>();
        if (visual == null)
        {
            visual = coinRoot.AddComponent<CoinVisual>();
        }

        SerializedObject so = new SerializedObject(visual);
        so.FindProperty("coinModelPrefab").objectReferenceValue = visualPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        visual.RebuildVisual();
        EditorUtility.SetDirty(coinRoot);
    }

    private static void RemoveLegacyCylinderMesh(GameObject coinRoot)
    {
        MeshFilter meshFilter = coinRoot.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            Object.DestroyImmediate(meshFilter);
        }

        MeshRenderer meshRenderer = coinRoot.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            Object.DestroyImmediate(meshRenderer);
        }
    }

    private static GameObject BuildOrUpdateVisualPrefab()
    {
        string glbPath = ResolveGlbAssetPath();
        if (glbPath == null)
        {
            EditorUtility.DisplayDialog(
                "Missing GLB",
                "Place coins.glb in:\n" + GlbAssetPath,
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
                "Could not load coins.glb.\nRun Gravity Painter → Reimport Coin GLB.",
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

        instance.name = "CoinVisual";
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

        string[] guids = AssetDatabase.FindAssets("coins t:GameObject");
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
        AssetImporter importer = AssetImporter.GetAtPath(path);
        if (importer == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(importer);
        SerializedProperty globalScale = so.FindProperty("m_GlobalScale");
        if (globalScale != null)
        {
            globalScale.floatValue = 1f;
        }

        SerializedProperty bakeAxis = so.FindProperty("m_BakeAxisConversion");
        if (bakeAxis != null)
        {
            bakeAxis.boolValue = true;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
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

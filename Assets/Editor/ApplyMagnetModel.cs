#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports Magnet.glb, builds a runtime prefab, and wires PowerUpVisual on the Magnet pickup prefab.
/// </summary>
public static class ApplyMagnetModel
{
    private const string GlbAssetPath = GlbModelPaths.Magnet;
    private const string MagnetPrefabPath = "Assets/Prefabs/PowerUps/Magnet.prefab";
    private const string VisualPrefabPath = "Assets/Prefabs/Visuals/MagnetVisual.prefab";
    private const string ResourcesPrefabPath = "Assets/Resources/Prefabs/MagnetVisual.prefab";

    [MenuItem("Gravity Painter/Apply Magnet GLB To Prefab")]
    public static void ApplyToMagnetPrefab()
    {
        GameObject visualPrefab = BuildOrUpdateVisualPrefab();
        if (visualPrefab == null)
        {
            return;
        }

        if (!File.Exists(MagnetPrefabPath))
        {
            EditorUtility.DisplayDialog("Missing prefab", "Could not find:\n" + MagnetPrefabPath, "OK");
            return;
        }

        GameObject magnetRoot = PrefabUtility.LoadPrefabContents(MagnetPrefabPath);
        RemovePrimitiveMesh(magnetRoot);
        WirePowerUpVisual(magnetRoot, visualPrefab);

        Transform rootTransform = magnetRoot.transform;
        rootTransform.localPosition = Vector3.zero;
        rootTransform.localRotation = Quaternion.identity;
        rootTransform.localScale = Vector3.one * 2f;

        PrefabUtility.SaveAsPrefabAsset(magnetRoot, MagnetPrefabPath);
        PrefabUtility.UnloadPrefabContents(magnetRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Magnet model applied",
            "Created/updated:\n" + ResourcesPrefabPath + "\n\n" +
            "Updated gameplay prefab:\n" + MagnetPrefabPath,
            "OK");
    }

    private static void WirePowerUpVisual(GameObject magnetRoot, GameObject visualPrefab)
    {
        PowerUpVisual visual = magnetRoot.GetComponent<PowerUpVisual>();
        if (visual == null)
        {
            visual = magnetRoot.AddComponent<PowerUpVisual>();
        }

        SerializedObject so = new SerializedObject(visual);
        so.FindProperty("modelPrefab").objectReferenceValue = visualPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        Transform staleVisual = magnetRoot.transform.Find(PowerUpVisual.VisualRootName);
        if (staleVisual != null)
        {
            Object.DestroyImmediate(staleVisual.gameObject);
        }

        EditorUtility.SetDirty(magnetRoot);
    }

    private static void RemovePrimitiveMesh(GameObject root)
    {
        MeshFilter meshFilter = root.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            Object.DestroyImmediate(meshFilter);
        }

        MeshRenderer meshRenderer = root.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            Object.DestroyImmediate(meshRenderer);
        }
    }

    private static GameObject BuildOrUpdateVisualPrefab()
    {
        if (!File.Exists(GlbAssetPath))
        {
            EditorUtility.DisplayDialog(
                "Missing GLB",
                "Place Magnet.glb in:\n" + GlbAssetPath,
                "OK");
            return null;
        }

        GameObject glbRoot = AssetDatabase.LoadAssetAtPath<GameObject>(GlbAssetPath);
        if (glbRoot == null)
        {
            AssetDatabase.ImportAsset(GlbAssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            glbRoot = AssetDatabase.LoadAssetAtPath<GameObject>(GlbAssetPath);
        }

        if (glbRoot == null)
        {
            EditorUtility.DisplayDialog("GLB import failed", "Could not load Magnet.glb.", "OK");
            return null;
        }

        Directory.CreateDirectory("Assets/Prefabs/Visuals");
        Directory.CreateDirectory("Assets/Resources/Prefabs");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(glbRoot);
        if (instance == null)
        {
            instance = Object.Instantiate(glbRoot);
        }

        instance.name = "MagnetVisual";
        StripPhysics(instance);
        TileMeshMaterialUtility.FixRenderersToUrpPreservingModelLook(instance);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, VisualPrefabPath);
        Object.DestroyImmediate(instance);

        GameObject resourcesCopy = PrefabUtility.SaveAsPrefabAsset(prefab, ResourcesPrefabPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return resourcesCopy != null ? resourcesCopy : prefab;
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

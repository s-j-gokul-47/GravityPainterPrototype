#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports SpeedUp.glb, builds a runtime prefab, and wires PowerUpVisual on the SpeedCore pickup prefab.
/// </summary>
public static class ApplySpeedUpModel
{
    private const string GlbAssetPath = "Assets/Art/Models/GLB/SpeedUp.glb";
    private const string SpeedCorePrefabPath = "Assets/Prefabs/PowerUps/SpeedCore.prefab";
    private const string VisualPrefabPath = "Assets/Prefabs/Visuals/SpeedUpVisual.prefab";
    private const string ResourcesPrefabPath = "Assets/Resources/Prefabs/SpeedUpVisual.prefab";

    [MenuItem("Gravity Painter/Apply SpeedUp GLB To Prefab")]
    public static void ApplyToSpeedUpPrefab()
    {
        GameObject visualPrefab = BuildOrUpdateVisualPrefab();
        if (visualPrefab == null)
        {
            return;
        }

        if (!File.Exists(SpeedCorePrefabPath))
        {
            EditorUtility.DisplayDialog("Missing prefab", "Could not find:\n" + SpeedCorePrefabPath, "OK");
            return;
        }

        GameObject speedCoreRoot = PrefabUtility.LoadPrefabContents(SpeedCorePrefabPath);
        RemovePrimitiveMesh(speedCoreRoot);
        WirePowerUpVisual(speedCoreRoot, visualPrefab);

        Transform rootTransform = speedCoreRoot.transform;
        rootTransform.localPosition = Vector3.zero;
        rootTransform.localRotation = Quaternion.identity;
        // The scale could be adjusted by the user later if needed, leaving it as 1 or original
        // Let's set it to 2f for now as a default
        rootTransform.localScale = Vector3.one * 2f;

        PrefabUtility.SaveAsPrefabAsset(speedCoreRoot, SpeedCorePrefabPath);
        PrefabUtility.UnloadPrefabContents(speedCoreRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "SpeedUp model applied",
            "Created/updated:\n" + ResourcesPrefabPath + "\n\n" +
            "Updated gameplay prefab:\n" + SpeedCorePrefabPath,
            "OK");
    }

    private static void WirePowerUpVisual(GameObject root, GameObject visualPrefab)
    {
        PowerUpVisual visual = root.GetComponent<PowerUpVisual>();
        if (visual == null)
        {
            visual = root.AddComponent<PowerUpVisual>();
        }

        SerializedObject so = new SerializedObject(visual);
        so.FindProperty("modelPrefab").objectReferenceValue = visualPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        Transform staleVisual = root.transform.Find(PowerUpVisual.VisualRootName);
        if (staleVisual != null)
        {
            Object.DestroyImmediate(staleVisual.gameObject);
        }

        EditorUtility.SetDirty(root);
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
                "Place SpeedUp.glb in:\n" + GlbAssetPath,
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
            EditorUtility.DisplayDialog("GLB import failed", "Could not load SpeedUp.glb.", "OK");
            return null;
        }

        Directory.CreateDirectory("Assets/Prefabs/Visuals");
        Directory.CreateDirectory("Assets/Resources/Prefabs");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(glbRoot);
        if (instance == null)
        {
            instance = Object.Instantiate(glbRoot);
        }

        instance.name = "SpeedUpVisual";
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

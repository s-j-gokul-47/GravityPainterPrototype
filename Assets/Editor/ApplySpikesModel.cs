#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports spikes.glb, builds a runtime prefab, and wires SpikeVisual on the Spikes obstacle prefab.
/// </summary>
public static class ApplySpikesModel
{
    private const string GlbAssetPath = GlbModelPaths.Spikes;
    private const string SpikesPrefabPath = "Assets/Prefabs/Obstacles/Spikes.prefab";
    private const string VisualPrefabPath = "Assets/Prefabs/Visuals/SpikesVisual.prefab";
    private const string ResourcesPrefabPath = "Assets/Resources/Prefabs/SpikesVisual.prefab";

    [MenuItem("Gravity Painter/Apply Spikes GLB To Prefab")]
    public static void ApplyToSpikesPrefab()
    {
        GameObject visualPrefab = BuildOrUpdateVisualPrefab();
        if (visualPrefab == null)
        {
            return;
        }

        GameObject spikesRoot = EnsureSpikesGameplayPrefab();
        if (spikesRoot == null)
        {
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(SpikesPrefabPath);
        WireSpikeVisual(prefabRoot, visualPrefab);

        Transform staleVisual = prefabRoot.transform.Find(SpikeVisual.VisualRootName);
        if (staleVisual != null)
        {
            Object.DestroyImmediate(staleVisual.gameObject);
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, SpikesPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Spikes model applied",
            "Created/updated:\n" + ResourcesPrefabPath + "\n\n" +
            "Updated gameplay prefab:\n" + SpikesPrefabPath,
            "OK");
    }

    private static GameObject EnsureSpikesGameplayPrefab()
    {
        if (File.Exists(SpikesPrefabPath))
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(SpikesPrefabPath);
        }

        Directory.CreateDirectory("Assets/Prefabs/Obstacles");

        GameObject root = new GameObject("Spikes");
        root.AddComponent<SpikeVisual>();
        root.AddComponent<SpikeTrap>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, SpikesPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void WireSpikeVisual(GameObject spikesRoot, GameObject visualPrefab)
    {
        SpikeVisual visual = spikesRoot.GetComponent<SpikeVisual>();
        if (visual == null)
        {
            visual = spikesRoot.AddComponent<SpikeVisual>();
        }

        if (spikesRoot.GetComponent<SpikeTrap>() == null)
        {
            spikesRoot.AddComponent<SpikeTrap>();
        }

        SerializedObject so = new SerializedObject(visual);
        so.FindProperty("modelPrefab").objectReferenceValue = visualPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spikesRoot);
    }

    private static GameObject BuildOrUpdateVisualPrefab()
    {
        if (!File.Exists(GlbAssetPath))
        {
            EditorUtility.DisplayDialog(
                "Missing GLB",
                "Place spikes.glb in:\n" + GlbAssetPath,
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
            EditorUtility.DisplayDialog("GLB import failed", "Could not load spikes.glb.", "OK");
            return null;
        }

        Directory.CreateDirectory("Assets/Prefabs/Visuals");
        Directory.CreateDirectory("Assets/Resources/Prefabs");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(glbRoot);
        if (instance == null)
        {
            instance = Object.Instantiate(glbRoot);
        }

        instance.name = "SpikesVisual";
        StripPhysics(instance);

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

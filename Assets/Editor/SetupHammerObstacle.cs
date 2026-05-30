#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the swinging hammer prefab from hammer.glb and places it in Level 2 above Tile (1).
/// </summary>
public static class SetupHammerObstacle
{
    private const string PrefabPath = "Assets/Prefabs/Obstacles/Hammer.prefab";
    private const string Level2ScenePath = "Assets/Scenes/Levels/Level 2.unity";
    private const string TileName = "Tile (1)";

    public static void BatchSetup()
    {
        SetupInLevel2();
        EditorApplication.Exit(0);
    }

    [MenuItem("Gravity Painter/Reimport Hammer GLB")]
    public static void ReimportHammerGlb()
    {
        if (!File.Exists(HammerModelUtility.GlbPath))
        {
            EditorUtility.DisplayDialog("Missing file", "Not found:\n" + HammerModelUtility.GlbPath, "OK");
            return;
        }

        AssetDatabase.ImportAsset(HammerModelUtility.GlbPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Reimported", "Reimported hammer.glb", "OK");
    }

    [MenuItem("Gravity Painter/Setup Hammer in Level 2")]
    public static void SetupInLevel2()
    {
        if (!File.Exists(HammerModelUtility.GlbPath))
        {
            EditorUtility.DisplayDialog(
                "Missing hammer.glb",
                "Place hammer.glb at:\n" + HammerModelUtility.GlbPath,
                "OK");
            return;
        }

        if (!HammerModelUtility.EnsureGlbImporterReady(HammerModelUtility.GlbPath))
        {
            EditorUtility.DisplayDialog(
                "GLB not ready",
                "hammer.glb is not imported yet.\n\n" +
                "Wait for Package Manager (glTFast), then run:\n" +
                "Gravity Painter → Reimport Hammer GLB",
                "OK");
            return;
        }

        GameObject prefabRoot = BuildHammerPrefab();
        if (prefabRoot == null)
        {
            return;
        }

        string previousScene = SceneManager.GetActiveScene().path;
        Scene scene = EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Single);

        RemoveExistingHammers();
        PlaceHammerInScene(prefabRoot);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
        {
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }

        EditorUtility.DisplayDialog(
            "Hammer ready",
            "Swinging hammer added to Level 2 above \"" + TileName + "\".\n\n" +
            "Prefab: " + PrefabPath,
            "OK");
    }

    [MenuItem("Gravity Painter/Upgrade Hammer Model (replace cube)")]
    public static void UpgradeHammerInOpenScene()
    {
        HammerModelBinder[] binders = Object.FindObjectsByType<HammerModelBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        foreach (HammerModelBinder binder in binders)
        {
            if (HammerModelUtility.TryUpgradeHammerHierarchy(binder.transform))
            {
                count++;
            }
        }

        if (count == 0)
        {
            SwingingHammer[] swings = Object.FindObjectsByType<SwingingHammer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (SwingingHammer swing in swings)
            {
                if (HammerModelUtility.TryUpgradeHammerHierarchy(swing.transform))
                {
                    count++;
                }
            }
        }

        EditorUtility.DisplayDialog(
            "Upgrade hammer",
            count > 0
                ? "Upgraded " + count + " hammer(s) with hammer.glb."
                : "No placeholder hammer found, or GLB could not load.",
            "OK");
    }

    private static GameObject BuildHammerPrefab()
    {
        Directory.CreateDirectory("Assets/Prefabs/Obstacles");

        GameObject root = new GameObject("Hammer");
        SwingingHammer swing = root.AddComponent<SwingingHammer>();
        root.AddComponent<HammerModelBinder>();

        GameObject pivot = new GameObject("Pivot");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        if (!HammerModelUtility.TryUpgradeHammerHierarchy(root.transform))
        {
            Object.DestroyImmediate(root);
            EditorUtility.DisplayDialog(
                "Import failed",
                "Could not load hammer.glb.\nRun Gravity Painter → Reimport Hammer GLB, then try again.",
                "OK");
            return null;
        }

        SerializedObject so = new SerializedObject(swing);
        so.FindProperty("pivot").objectReferenceValue = pivot.transform;
        so.FindProperty("swingAngle").floatValue = 55f;
        so.FindProperty("swingSpeed").floatValue = -2f;
        so.FindProperty("swingAxisLocal").vector3Value = Vector3.forward;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return prefab;
    }

    private static void PlaceHammerInScene(GameObject prefab)
    {
        Vector3 position = ResolveHammerPosition();
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetPositionAndRotation(position, Quaternion.identity);
        instance.name = "Hammer";
        Undo.RegisterCreatedObjectUndo(instance, "Place Hammer");
    }

    private static Vector3 ResolveHammerPosition()
    {
        GameObject tile = GameObject.Find(TileName);
        if (tile != null)
        {
            Vector3 p = tile.transform.position;
            return new Vector3(p.x, p.y + 2.75f, p.z);
        }

        return new Vector3(2f, 2.75f, 1.1f);
    }

    private static void RemoveExistingHammers()
    {
        HammerStrike[] strikes = Object.FindObjectsByType<HammerStrike>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (HammerStrike strike in strikes)
        {
            Transform root = strike.transform;
            while (root.parent != null && root.GetComponent<SwingingHammer>() == null)
            {
                root = root.parent;
            }

            if (root.GetComponent<SwingingHammer>() != null || root.name == "Hammer")
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        SwingingHammer[] swings = Object.FindObjectsByType<SwingingHammer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SwingingHammer swing in swings)
        {
            if (swing != null && swing.gameObject != null)
            {
                Object.DestroyImmediate(swing.gameObject);
            }
        }
    }

    public static void EnsureStrikeCollidersOnModel(GameObject modelRoot)
    {
        Collider[] colliders = modelRoot.GetComponentsInChildren<Collider>(true);
        if (colliders.Length == 0)
        {
            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                BoxCollider box = modelRoot.AddComponent<BoxCollider>();
                box.center = modelRoot.transform.InverseTransformPoint(bounds.center);
                Vector3 size = bounds.size;
                size.x /= Mathf.Max(modelRoot.transform.lossyScale.x, 0.001f);
                size.y /= Mathf.Max(modelRoot.transform.lossyScale.y, 0.001f);
                size.z /= Mathf.Max(modelRoot.transform.lossyScale.z, 0.001f);
                box.size = size;
                colliders = new[] { box };
            }
        }

        foreach (Collider col in colliders)
        {
            if (col.GetComponent<HammerStrike>() == null)
            {
                col.gameObject.AddComponent<HammerStrike>();
            }

            col.isTrigger = false;
        }
    }
}
#endif

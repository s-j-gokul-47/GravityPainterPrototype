#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Places the RedLaserBeam GLB as a cycling Korrath Beam at Tile (42) in Level 2.
/// </summary>
public static class SetupLaserGateObstacle
{
    private const string PrefabPath = "Assets/Prefabs/Obstacles/KorrathBeam.prefab";
    private const string Level2ScenePath = "Assets/Scenes/Levels/Level 2.unity";
    private const string TileName = "Tile (42)";

    public static void BatchSetup()
    {
        SetupInLevel2();
        EditorApplication.Exit(0);
    }

    [MenuItem("Gravity Painter/Reimport Red Laser Beam GLB")]
    public static void ReimportLaserGlb()
    {
        if (!File.Exists(LaserGateModelUtility.GlbPath))
        {
            EditorUtility.DisplayDialog("Missing file", "Not found:\n" + LaserGateModelUtility.GlbPath, "OK");
            return;
        }

        AssetDatabase.ImportAsset(LaserGateModelUtility.GlbPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Reimported", "Reimported RedLaserBeam.glb", "OK");
    }

    [MenuItem("Gravity Painter/Setup Korrath Beam at Tile (42) in Level 2")]
    public static void SetupInLevel2()
    {
        if (!File.Exists(LaserGateModelUtility.GlbPath))
        {
            EditorUtility.DisplayDialog(
                "Missing RedLaserBeam.glb",
                "Expected:\n" + LaserGateModelUtility.GlbPath,
                "OK");
            return;
        }

        if (!LaserGateModelUtility.EnsureGlbImporterReady(LaserGateModelUtility.GlbPath))
        {
            EditorUtility.DisplayDialog(
                "GLB not ready",
                "RedLaserBeam.glb is not imported yet.\n\n" +
                "Wait for glTFast import, then run:\n" +
                "Gravity Painter → Reimport Red Laser Beam GLB",
                "OK");
            return;
        }

        GameObject prefabRoot = BuildLaserGatePrefab();
        if (prefabRoot == null)
        {
            return;
        }

        string previousScene = SceneManager.GetActiveScene().path;
        Scene scene = EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Single);

        RemoveExistingLaserGates();
        PlaceLaserAtTile42(prefabRoot);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
        {
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }

        EditorUtility.DisplayDialog(
            "Korrath Beam ready",
            "Red laser gate placed on \"" + TileName + "\" in Level 2.\n\nPrefab: " + PrefabPath,
            "OK");
    }

    [MenuItem("Gravity Painter/Upgrade Laser Model (use RedLaserBeam.glb)")]
    public static void UpgradeLaserInOpenScene()
    {
        LaserGate[] gates = Object.FindObjectsByType<LaserGate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;

        foreach (LaserGate gate in gates)
        {
            GameObject tile = FindTileForGate(gate.transform);
            if (LaserGateModelUtility.TryAttachModelToGate(gate.transform, tile != null ? tile.transform : null))
            {
                count++;
            }
        }

        EditorUtility.DisplayDialog(
            "Upgrade laser",
            count > 0
                ? "Attached RedLaserBeam.glb to " + count + " gate(s)."
                : "No Korrath Beam found, or GLB could not load.",
            "OK");
    }

    private static GameObject BuildLaserGatePrefab()
    {
        Directory.CreateDirectory("Assets/Prefabs/Obstacles");

        GameObject root = new GameObject("KorrathBeam");
        LaserGate gate = root.AddComponent<LaserGate>();

        if (!LaserGateModelUtility.TryAttachModelToGate(root.transform))
        {
            Object.DestroyImmediate(root);
            EditorUtility.DisplayDialog(
                "Import failed",
                "Could not load RedLaserBeam.glb.\nRun Gravity Painter → Reimport Red Laser Beam GLB.",
                "OK");
            return null;
        }

        SerializedObject so = new SerializedObject(gate);
        so.FindProperty("modelRoot").objectReferenceValue = root.transform.Find("LaserModel");
        so.FindProperty("onDuration").floatValue = 1.5f;
        so.FindProperty("offDuration").floatValue = 1.5f;
        so.FindProperty("startActive").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return prefab;
    }

    private static void PlaceLaserAtTile42(GameObject prefab)
    {
        GameObject tile = FindTileByName(TileName);
        Vector3 position;
        Quaternion rotation;

        if (tile != null)
        {
            Bounds bounds = LaserGateModelUtility.GetTileWorldBoundsForEditor(tile.transform);
            position = new Vector3(bounds.center.x, bounds.max.y + 0.25f, bounds.center.z);
            rotation = tile.transform.rotation;
        }
        else
        {
            position = new Vector3(2.61f, 0.35f, 4.84f);
            rotation = Quaternion.identity;
            Debug.LogWarning("Tile (42) not found — using default position.");
        }

        GameObject existingModel = FindExistingRedLaserModel();
        if (existingModel != null)
        {
            UpgradeExistingSceneModel(existingModel, tile);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.name = "KorrathBeam";

        if (tile != null)
        {
            LaserGate gate = instance.GetComponent<LaserGate>();
            Transform modelRoot = instance.transform.Find("LaserModel");
            if (modelRoot != null)
            {
                LaserGateModelUtility.FitModelToTile(modelRoot.gameObject, tile.transform);
                EnsureStrikeColliders(modelRoot.gameObject, gate);
            }
        }

        Undo.RegisterCreatedObjectUndo(instance, "Place Korrath Beam");
    }

    private static void UpgradeExistingSceneModel(GameObject existingRoot, GameObject tile)
    {
        GameObject gateRoot = existingRoot.transform.parent != null &&
                              existingRoot.transform.parent.name == "KorrathBeam"
            ? existingRoot.transform.parent.gameObject
            : existingRoot;

        if (gateRoot.GetComponent<LaserGate>() == null)
        {
            gateRoot.AddComponent<LaserGate>();
        }

        if (existingRoot.name != "LaserModel")
        {
            existingRoot.name = "LaserModel";
        }

        LaserGate gate = gateRoot.GetComponent<LaserGate>();
        SerializedObject so = new SerializedObject(gate);
        so.FindProperty("modelRoot").objectReferenceValue = existingRoot.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (tile != null)
        {
            Bounds bounds = LaserGateModelUtility.GetTileWorldBoundsForEditor(tile.transform);
            gateRoot.transform.position = new Vector3(bounds.center.x, bounds.max.y + 0.25f, bounds.center.z);
            gateRoot.transform.rotation = tile.transform.rotation;
            LaserGateModelUtility.FitModelToTile(existingRoot, tile.transform);
        }

        EnsureStrikeColliders(existingRoot, gate);
        gateRoot.name = "KorrathBeam";
        Undo.RegisterFullObjectHierarchyUndo(gateRoot, "Setup Korrath Beam");
    }

    private static GameObject FindExistingRedLaserModel()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                string lower = t.name.ToLowerInvariant();
                if (lower.Contains("redlaser") || lower.Contains("laserbeam") || lower == "korrathbeam")
                {
                    if (t.GetComponentInChildren<Renderer>(true) != null)
                    {
                        return t.gameObject;
                    }
                }
            }
        }

        return null;
    }

    public static void EnsureStrikeColliders(GameObject modelRoot, LaserGate gate)
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
                box.isTrigger = true;
                box.center = modelRoot.transform.InverseTransformPoint(bounds.center);
                Vector3 size = bounds.size;
                Vector3 lossy = modelRoot.transform.lossyScale;
                size.x /= Mathf.Max(lossy.x, 0.001f);
                size.y /= Mathf.Max(lossy.y, 0.001f);
                size.z /= Mathf.Max(lossy.z, 0.001f);
                box.size = size;
                colliders = new[] { box };
            }
        }

        foreach (Collider col in colliders)
        {
            col.isTrigger = true;
            LaserGateStrike strike = col.GetComponent<LaserGateStrike>();
            if (strike == null)
            {
                strike = col.gameObject.AddComponent<LaserGateStrike>();
            }

            SerializedObject so = new SerializedObject(strike);
            so.FindProperty("gate").objectReferenceValue = gate;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void RemoveExistingLaserGates()
    {
        LaserGate[] gates = Object.FindObjectsByType<LaserGate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (LaserGate gate in gates)
        {
            if (gate != null && gate.gameObject != null)
            {
                Object.DestroyImmediate(gate.gameObject);
            }
        }

        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "KorrathBeam" || root.name.ToLowerInvariant().Contains("redlaser"))
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    private static GameObject FindTileByName(string tileName)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                if (t.name == tileName)
                {
                    return t.gameObject;
                }
            }
        }

        return null;
    }

    private static GameObject FindTileForGate(Transform gate)
    {
        TileZone[] zones = Object.FindObjectsByType<TileZone>(FindObjectsSortMode.None);
        float bestDist = float.MaxValue;
        GameObject best = null;

        foreach (TileZone zone in zones)
        {
            float dist = Vector3.Distance(zone.transform.position, gate.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = zone.gameObject;
            }
        }

        return best;
    }
}
#endif

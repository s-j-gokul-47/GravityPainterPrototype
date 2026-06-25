#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bakes spike traps into Level 2 on selected tiles so they are visible in the Hierarchy without Play.
/// Tile 43 is the master spike; tile 41 never gets spikes.
/// </summary>
public static class PlaceLevel2Spikes
{
    private const string SpikesPrefabPath = "Assets/Prefabs/Obstacles/Spikes.prefab";
    private const string Level2ScenePath = "Assets/Scenes/Levels/Level 2.unity";
    private const string MasterTileName = "Tile (43)";
    private const string ExcludedTileName = "Tile (41)";

    private static readonly string[] DefaultTileNames =
    {
        MasterTileName,
        "Tile (45)",
        "Tile (47)",
    };

    private static readonly float[] DefaultStartDelays =
    {
        0f,
        0.9f,
        1.8f,
    };

    [MenuItem("Gravity Painter/Place Spikes In Level 2")]
    public static void PlaceSpikesMenu()
    {
        if (!File.Exists(SpikesPrefabPath))
        {
            bool runApply = EditorUtility.DisplayDialog(
                "Spikes prefab missing",
                "Run Gravity Painter → Apply Spikes GLB To Prefab first?",
                "Run now",
                "Cancel");

            if (runApply)
            {
                ApplySpikesModel.ApplyToSpikesPrefab();
            }

            if (!File.Exists(SpikesPrefabPath))
            {
                return;
            }
        }

        string previousScene = SceneManager.GetActiveScene().path;
        int placed = PlaceSpikesInScene(Level2ScenePath);

        if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
        {
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }

        EditorUtility.DisplayDialog(
            "Level 2 spikes placed",
            "Placed " + placed + " spike trap(s) in Level 2.\n\n" +
            "Edit Spike_Master_Tile(43), then use Gravity Painter → Publish Master Spike To All Spikes.",
            "OK");
    }

    private static int PlaceSpikesInScene(string scenePath)
    {
        GameObject spikesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpikesPrefabPath);
        if (spikesPrefab == null || !File.Exists(scenePath))
        {
            return 0;
        }

        SpikeTrapProfile profile = AssetDatabase.LoadAssetAtPath<SpikeTrapProfile>(
            "Assets/Resources/Settings/SpikeTrapProfile.asset");
        if (profile == null)
        {
            profile = SpikeTrapProfile.LoadOrDefault();
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        RemoveSpikesOnExcludedTiles();
        RemoveExistingSpikes();

        Transform spikesRoot = EnsureSpikesRoot();
        Dictionary<string, Transform> tilesByName = CollectTilesByName();
        int placed = 0;
        SpikeTrap masterTrap = null;

        for (int i = 0; i < DefaultTileNames.Length; i++)
        {
            string tileName = DefaultTileNames[i];
            if (!tilesByName.TryGetValue(tileName, out Transform tile))
            {
                Debug.LogWarning("PlaceLevel2Spikes: could not find " + tileName + " in Level 2.");
                continue;
            }

            GameObject spike = (GameObject)PrefabUtility.InstantiatePrefab(spikesPrefab, spikesRoot);
            bool isMaster = tileName == MasterTileName;
            spike.name = isMaster
                ? SpikeTrap.MasterSpikeName
                : "Spike_" + tile.name.Replace(" ", string.Empty);

            Vector3 position = tile.position;
            position.y = GetTileTopY(tile.gameObject) + 0.01f;
            spike.transform.SetPositionAndRotation(position, tile.rotation);

            SpikeVisual visual = spike.GetComponent<SpikeVisual>();
            if (visual != null)
            {
                visual.EnsureVisual();
            }

            SpikeTrap trap = spike.GetComponent<SpikeTrap>();
            if (trap != null)
            {
                float delay = i < DefaultStartDelays.Length ? DefaultStartDelays[i] : i * 0.9f;
                SerializedObject so = new SerializedObject(trap);
                so.FindProperty("startDelay").floatValue = delay;
                so.ApplyModifiedPropertiesWithoutUndo();

                if (isMaster)
                {
                    SpikeTrapSync.ConfigureMaster(trap, profile);
                    masterTrap = trap;
                }
                else
                {
                    SpikeTrapSync.ConfigureInstance(trap, profile);
                }
            }

            placed++;
        }

        if (masterTrap != null)
        {
            masterTrap.PublishFromHierarchy();
        }
        else
        {
            SpikeTrapSync.ApplyProfileEverywhere(profile);
        }

        SpikeTrapSync.FixAllTrapsInOpenScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return placed;
    }

    private static Transform EnsureSpikesRoot()
    {
        GameObject existing = GameObject.Find("Spikes");
        if (existing != null)
        {
            return existing.transform;
        }

        return new GameObject("Spikes").transform;
    }

    private static void RemoveSpikesOnExcludedTiles()
    {
        Transform excludedTile = FindTileByName(ExcludedTileName);
        if (excludedTile == null)
        {
            return;
        }

        SpikeTrap[] traps = Object.FindObjectsByType<SpikeTrap>(FindObjectsSortMode.None);
        foreach (SpikeTrap trap in traps)
        {
            if (trap == null)
            {
                continue;
            }

            if (IsSpikeOnTile(trap.transform, excludedTile))
            {
                Object.DestroyImmediate(trap.gameObject);
            }
        }
    }

    private static void RemoveExistingSpikes()
    {
        SpikeTrap[] traps = Object.FindObjectsByType<SpikeTrap>(FindObjectsSortMode.None);
        foreach (SpikeTrap trap in traps)
        {
            if (trap != null)
            {
                Object.DestroyImmediate(trap.gameObject);
            }
        }

        GameObject spikesRoot = GameObject.Find("Spikes");
        if (spikesRoot != null && spikesRoot.transform.childCount == 0)
        {
            Object.DestroyImmediate(spikesRoot);
        }
    }

    private static Transform FindTileByName(string tileName)
    {
        TileZone[] zones = Object.FindObjectsByType<TileZone>(FindObjectsSortMode.None);
        foreach (TileZone zone in zones)
        {
            if (zone != null && zone.name == tileName)
            {
                return zone.transform;
            }
        }

        return null;
    }

    private static bool IsSpikeOnTile(Transform spike, Transform tile)
    {
        Vector3 spikePos = spike.position;
        Collider[] colliders = tile.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger)
            {
                continue;
            }

            if (collider.bounds.Contains(spikePos))
            {
                return true;
            }
        }

        return Vector3.Distance(spike.position, tile.position) < 0.5f;
    }

    private static Dictionary<string, Transform> CollectTilesByName()
    {
        var tiles = new Dictionary<string, Transform>();
        TileZone[] zones = Object.FindObjectsByType<TileZone>(FindObjectsSortMode.None);
        foreach (TileZone zone in zones)
        {
            if (zone == null)
            {
                continue;
            }

            tiles[zone.name] = zone.transform;
        }

        return tiles;
    }

    private static float GetTileTopY(GameObject tile)
    {
        float maxY = float.NegativeInfinity;
        Collider[] colliders = tile.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger)
            {
                continue;
            }

            maxY = Mathf.Max(maxY, collider.bounds.max.y);
        }

        if (maxY > float.NegativeInfinity)
        {
            return maxY;
        }

        Renderer[] renderers = tile.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                maxY = Mathf.Max(maxY, renderers[i].bounds.max.y);
            }
        }

        return maxY > float.NegativeInfinity ? maxY : tile.transform.position.y;
    }
}
#endif

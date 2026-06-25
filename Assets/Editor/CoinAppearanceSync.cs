#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pushes CoinAppearanceProfile to every coin in all level scenes and the gameplay coin prefab.
/// Scene coins are rebuilt from the updated Coin prefab so visuals always match the master.
/// </summary>
public static class CoinAppearanceSync
{
    private const string CoinPrefabPath = "Assets/Prefabs/Gameplay/Coin.prefab";
    private const string LevelScenesFolder = "Assets/Scenes/Levels";

    private static bool _isApplying;

    private struct CoinPlacement
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public string Name;
        public Transform Parent;
        public bool IsMaster;
        public Transform Tile;
    }

    [InitializeOnLoadMethod]
    private static void SubscribeToProfilePublish()
    {
        CoinAppearance.ProfilePublished -= OnProfilePublished;
        CoinAppearance.ProfilePublished += OnProfilePublished;
    }

    private static void OnProfilePublished(CoinAppearance master, CoinAppearanceProfile profile)
    {
        ApplyProfileEverywhere(profile, skipMasterReplacement: true);
    }

    [MenuItem("Gravity Painter/Publish Master Coin To All Coins")]
    public static void PublishMasterCoinMenu()
    {
        CoinAppearance master = CoinAppearance.FindMasterInOpenScenes();
        if (master == null)
        {
            EditorUtility.DisplayDialog(
                "No master coin",
                "Open Level 2 and ensure Coin_Master_Tile(48) exists under the Coins object.",
                "OK");
            return;
        }

        if (!master.IsMaster)
        {
            ConfigureMasterCoin(master.gameObject, master.Profile ?? CoinAppearanceProfile.LoadOrDefault());
        }

        master.PublishFromHierarchy();

        EditorUtility.DisplayDialog(
            "Coins updated",
            "Published the master coin appearance to all campaign coins, level scenes, and the Coin prefab used by procedural levels.",
            "OK");
    }

    public static void ApplyProfileEverywhere(
        CoinAppearanceProfile profile,
        bool skipMasterReplacement = false)
    {
        if (profile == null || _isApplying)
        {
            return;
        }

        _isApplying = true;
        CoinAppearance.SetSyncInProgress(true);
        try
        {
            CoinAppearanceProfile.SetCached(profile);
            EditorUtility.SetDirty(profile);

            string activeScenePath = SceneManager.GetActiveScene().path;

            ApplyToCoinPrefab(profile);
            ApplyToAllLevelScenes(profile, skipMasterReplacement);

            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(activeScenePath) && File.Exists(activeScenePath))
            {
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
            }
        }
        finally
        {
            CoinAppearance.SetSyncInProgress(false);
            _isApplying = false;
        }
    }

    public static void ConfigureMasterCoin(GameObject masterCoin, CoinAppearanceProfile profile)
    {
        if (masterCoin == null || profile == null)
        {
            return;
        }

        masterCoin.name = CoinAppearance.MasterCoinName;
        ConfigureCoinAppearance(masterCoin, profile, isMaster: true);
        EditorUtility.SetDirty(masterCoin);
    }

    public static void ConfigureInstanceCoin(GameObject coin, CoinAppearanceProfile profile)
    {
        if (coin == null || profile == null)
        {
            return;
        }

        ConfigureCoinAppearance(coin, profile, isMaster: false);
        profile.ApplyToHierarchy(coin.transform);
        EditorUtility.SetDirty(coin);
    }

    private static void ConfigureCoinAppearance(GameObject coin, CoinAppearanceProfile profile, bool isMaster)
    {
        CoinAppearance appearance = coin.GetComponent<CoinAppearance>();
        if (appearance == null)
        {
            appearance = coin.AddComponent<CoinAppearance>();
        }

        SerializedObject so = new SerializedObject(appearance);
        so.FindProperty("profile").objectReferenceValue = profile;
        so.FindProperty("publishChangesToProfile").boolValue = isMaster;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyToAllLevelScenes(CoinAppearanceProfile profile, bool skipMasterReplacement)
    {
        if (!Directory.Exists(LevelScenesFolder))
        {
            return;
        }

        string[] scenePaths = Directory.GetFiles(LevelScenesFolder, "*.unity", SearchOption.TopDirectoryOnly);
        foreach (string scenePath in scenePaths)
        {
            if (File.Exists(scenePath))
            {
                RebuildCoinsInSceneFile(scenePath, profile, skipMasterReplacement);
            }
        }
    }

    private static void RebuildCoinsInSceneFile(
        string scenePath,
        CoinAppearanceProfile profile,
        bool skipMasterReplacement)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        List<CoinPlacement> placements = CollectCoinPlacements(scene, skipMasterReplacement);
        if (placements.Count == 0)
        {
            return;
        }

        DestroySceneCoins(scene, skipMasterReplacement);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("CoinAppearanceSync: missing Coin prefab at " + CoinPrefabPath);
            return;
        }

        for (int i = 0; i < placements.Count; i++)
        {
            CreateCoinFromPrefab(placements[i], prefab, profile);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static List<CoinPlacement> CollectCoinPlacements(Scene scene, bool skipMasterReplacement)
    {
        var placements = new List<CoinPlacement>();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                continue;
            }

            Coin[] coins = root.GetComponentsInChildren<Coin>(true);
            for (int j = 0; j < coins.Length; j++)
            {
                Coin coin = coins[j];
                if (coin == null || coin.gameObject.scene != scene)
                {
                    continue;
                }

                bool isMaster = coin.gameObject.name == CoinAppearance.MasterCoinName;
                if (skipMasterReplacement && isMaster)
                {
                    continue;
                }

                placements.Add(new CoinPlacement
                {
                    Position = coin.transform.position,
                    Rotation = coin.transform.rotation,
                    Name = coin.gameObject.name,
                    Parent = coin.transform.parent,
                    IsMaster = isMaster,
                    Tile = CampaignCoinPlacement.FindNearestTileForCoin(coin.transform),
                });
            }
        }

        return placements;
    }

    private static void DestroySceneCoins(Scene scene, bool skipMasterReplacement)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                continue;
            }

            Coin[] coins = root.GetComponentsInChildren<Coin>(true);
            for (int j = coins.Length - 1; j >= 0; j--)
            {
                Coin coin = coins[j];
                if (coin == null || coin.gameObject.scene != scene)
                {
                    continue;
                }

                bool isMaster = coin.gameObject.name == CoinAppearance.MasterCoinName;
                if (skipMasterReplacement && isMaster)
                {
                    continue;
                }

                Object.DestroyImmediate(coin.gameObject);
            }
        }
    }

    private static void CreateCoinFromPrefab(CoinPlacement placement, GameObject prefab, CoinAppearanceProfile profile)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, placement.Parent);
        instance.name = placement.Name;
        instance.transform.SetPositionAndRotation(placement.Position, placement.Rotation);

        if (placement.Name.StartsWith("Coin_Master_") && placement.Name != CoinAppearance.MasterCoinName)
        {
            instance.name = "Coin_" + placement.Name.Substring("Coin_Master_".Length);
        }

        ConfigureCoinAppearance(instance, profile, placement.IsMaster);

        if (!placement.IsMaster)
        {
            profile.ApplyToHierarchy(instance.transform);
        }

        if (placement.Tile != null)
        {
            CampaignCoinPlacement.SnapCoinToTile(instance.transform, placement.Tile);
        }

        EditorUtility.SetDirty(instance);
    }

    private static void ApplyToCoinPrefab(CoinAppearanceProfile profile)
    {
        if (!File.Exists(CoinPrefabPath))
        {
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(CoinPrefabPath);
        try
        {
            CoinAppearance appearance = prefabRoot.GetComponent<CoinAppearance>();
            if (appearance == null)
            {
                appearance = prefabRoot.AddComponent<CoinAppearance>();
            }

            SerializedObject so = new SerializedObject(appearance);
            so.FindProperty("profile").objectReferenceValue = profile;
            so.FindProperty("publishChangesToProfile").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            profile.ApplyToPrefabContents(prefabRoot.transform);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, CoinPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    [MenuItem("Gravity Painter/Sync All Coins From Profile")]
    public static void SyncAllFromProfileMenu()
    {
        CoinAppearanceProfile profile = CoinAppearanceProfile.LoadOrDefault();
        if (profile == null)
        {
            EditorUtility.DisplayDialog("Missing profile", "Could not load CoinAppearanceProfile.", "OK");
            return;
        }

        ApplyProfileEverywhere(profile);
        EditorUtility.DisplayDialog(
            "Coins synced",
            "Applied CoinAppearanceProfile to all campaign coins, level scenes, and the Coin prefab used by procedural levels.",
            "OK");
    }
}
#endif

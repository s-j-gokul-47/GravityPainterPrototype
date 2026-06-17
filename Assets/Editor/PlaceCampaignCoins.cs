#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bakes coin prefab instances into campaign Level 1 and Level 2 so models are visible in the Hierarchy without Play.
/// </summary>
public static class PlaceCampaignCoins
{
    private const string CoinPrefabPath = "Assets/Prefabs/Gameplay/Coin.prefab";
    private const string ProfileAssetPath = "Assets/Resources/Settings/CoinAppearanceProfile.asset";
    private const string Level1ScenePath = "Assets/Scenes/Levels/Level 1.unity";
    private const string Level2ScenePath = "Assets/Scenes/Levels/Level 2.unity";
    private const string MasterTileName = "Tile (46)";

    private static readonly string[] CampaignScenePaths =
    {
        Level1ScenePath,
        Level2ScenePath,
    };

    [MenuItem("Gravity Painter/Place Coins In Levels 1 And 2")]
    public static void PlaceCoinsMenu()
    {
        string previousScene = SceneManager.GetActiveScene().path;
        int totalPlaced = 0;

        foreach (string scenePath in CampaignScenePaths)
        {
            RemoveExistingCoinsInSceneFile(scenePath);
            totalPlaced += PlaceCoinsInScene(scenePath);
        }

        if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
        {
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }

        EditorUtility.DisplayDialog(
            "Campaign coins placed",
            "Placed " + totalPlaced + " baked coin(s) across Level 1 and Level 2.\n\n" +
            "Edit Coin_Master_Tile(46) on Level 1 to change every coin in the game.",
            "OK");
    }

    private static void RemoveExistingCoinsInSceneFile(string scenePath)
    {
        if (!File.Exists(scenePath))
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        RemoveExistingCoins();
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.CloseScene(scene, removeScene: true);
    }

    private static int PlaceCoinsInScene(string scenePath)
    {
        if (!File.Exists(scenePath))
        {
            return 0;
        }

        GameObject coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath);
        CoinAppearanceProfile profile = AssetDatabase.LoadAssetAtPath<CoinAppearanceProfile>(ProfileAssetPath);
        if (coinPrefab == null || profile == null)
        {
            return 0;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        int levelNumber = ResolveLevelNumber(scenePath);
        RemoveExistingCoins();

        Transform coinsRoot = EnsureCoinsRoot();
        BallController ball = Object.FindFirstObjectByType<BallController>();
        FinishLine finishLine = Object.FindFirstObjectByType<FinishLine>();
        List<Transform> tiles = CampaignCoinPlacement.CollectEligibleTiles(
            ball != null ? ball.transform : null,
            finishLine);

        GameObject masterCoin = null;
        int placed = 0;
        for (int i = 0; i < tiles.Count; i++)
        {
            Transform tile = tiles[i];
            int tileSortKey = CampaignCoinPlacement.ParseTileSortKey(tile.name);
            if (!CampaignCoinPlacement.ShouldSpawnCoin(
                    CampaignCoinPlacement.SeedForLevel(levelNumber),
                    tileSortKey))
            {
                continue;
            }

            Vector3 position = tile.position + Vector3.up * profile.spawnHeight;
            Quaternion rotation = CampaignCoinPlacement.RandomSpawnRotation(
                CampaignCoinPlacement.SeedForLevel(levelNumber),
                tileSortKey);

            GameObject coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, coinsRoot);
            coin.transform.SetPositionAndRotation(position, rotation);

            CoinAppearance appearance = coin.GetComponent<CoinAppearance>();
            if (appearance != null)
            {
                appearance.ApplyFromProfile(profile);
            }

            string coinName = "Coin_" + tile.name.Replace(" ", string.Empty);
            coin.name = coinName;

            if (levelNumber == 1 && tile.name == MasterTileName)
            {
                masterCoin = coin;
            }

            placed++;
        }

        ConfigureMasterCoin(masterCoin, profile);
        CoinAppearance masterAppearance = masterCoin != null ? masterCoin.GetComponent<CoinAppearance>() : null;
        CoinAppearanceSync.ApplyProfileEverywhere(profile, masterAppearance);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return placed;
    }

    private static void ConfigureMasterCoin(GameObject masterCoin, CoinAppearanceProfile profile)
    {
        if (masterCoin == null)
        {
            return;
        }

        masterCoin.name = CoinAppearance.MasterCoinName;

        CoinAppearance appearance = masterCoin.GetComponent<CoinAppearance>();
        if (appearance == null)
        {
            appearance = masterCoin.AddComponent<CoinAppearance>();
        }

        SerializedObject so = new SerializedObject(appearance);
        so.FindProperty("profile").objectReferenceValue = profile;
        so.FindProperty("publishChangesToProfile").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(masterCoin);
    }

    private static int ResolveLevelNumber(string scenePath)
    {
        Match match = Regex.Match(Path.GetFileNameWithoutExtension(scenePath), @"(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out int levelNumber)
            ? levelNumber
            : 1;
    }

    private static Transform EnsureCoinsRoot()
    {
        GameObject existing = GameObject.Find("Coins");
        if (existing != null)
        {
            return existing.transform;
        }

        GameObject root = new GameObject("Coins");
        return root.transform;
    }

    private static void RemoveExistingCoins()
    {
        Coin[] coins = Object.FindObjectsByType<Coin>(FindObjectsSortMode.None);
        foreach (Coin coin in coins)
        {
            if (coin == null)
            {
                continue;
            }

            Object.DestroyImmediate(coin.gameObject);
        }

        GameObject coinsRoot = GameObject.Find("Coins");
        if (coinsRoot != null)
        {
            Object.DestroyImmediate(coinsRoot);
        }
    }
}
#endif

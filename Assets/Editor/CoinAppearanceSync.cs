#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Pushes CoinAppearanceProfile to every coin instance and the gameplay coin prefab.
/// </summary>
public static class CoinAppearanceSync
{
    private const string CoinPrefabPath = "Assets/Prefabs/Gameplay/Coin.prefab";

    private static bool _isApplying;

    [InitializeOnLoadMethod]
    private static void SubscribeToProfilePublish()
    {
        CoinAppearance.ProfilePublished -= OnProfilePublished;
        CoinAppearance.ProfilePublished += OnProfilePublished;
    }

    private static void OnProfilePublished(CoinAppearance master, CoinAppearanceProfile profile)
    {
        ApplyProfileEverywhere(profile, master);
    }

    [MenuItem("Gravity Painter/Publish Master Coin To All Coins")]
    public static void PublishMasterCoinMenu()
    {
        CoinAppearance master = CoinAppearance.FindMasterInOpenScenes();
        if (master == null)
        {
            EditorUtility.DisplayDialog(
                "No master coin",
                "Open Level 1 and ensure Coin_Master_Tile(46) exists under the Coins object.",
                "OK");
            return;
        }

        master.PublishFromHierarchy();
        EditorUtility.DisplayDialog(
            "Coins updated",
            "Published the master coin appearance to all coins and the Coin prefab.",
            "OK");
    }

    public static void ApplyProfileEverywhere(CoinAppearanceProfile profile, CoinAppearance sourceMaster = null)
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

            CoinAppearance[] appearances = Object.FindObjectsByType<CoinAppearance>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (CoinAppearance appearance in appearances)
            {
                if (appearance == null || appearance == sourceMaster)
                {
                    continue;
                }

                profile.ApplyToHierarchy(appearance.transform);
                EditorUtility.SetDirty(appearance);
            }

            ApplyToCoinPrefab(profile);
        }
        finally
        {
            CoinAppearance.SetSyncInProgress(false);
            _isApplying = false;
        }
    }

    private static void ApplyToCoinPrefab(CoinAppearanceProfile profile)
    {
        if (!System.IO.File.Exists(CoinPrefabPath))
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

            profile.ApplyToHierarchy(prefabRoot.transform);
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
            "Applied CoinAppearanceProfile to all coins and the Coin prefab.",
            "OK");
    }
}
#endif

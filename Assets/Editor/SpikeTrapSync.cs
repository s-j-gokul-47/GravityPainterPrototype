#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Pushes SpikeTrapProfile to every spike instance and the Spikes prefab.
/// </summary>
public static class SpikeTrapSync
{
    private const string SpikesPrefabPath = "Assets/Prefabs/Obstacles/Spikes.prefab";

    private static bool _isApplying;

    [InitializeOnLoadMethod]
    private static void SubscribeToProfilePublish()
    {
        SpikeTrap.ProfilePublished -= OnProfilePublished;
        SpikeTrap.ProfilePublished += OnProfilePublished;
    }

    private static void OnProfilePublished(SpikeTrap master, SpikeTrapProfile profile)
    {
        ApplyProfileEverywhere(profile, master);
    }

    [MenuItem("Gravity Painter/Publish Master Spike To All Spikes")]
    public static void PublishMasterSpikeMenu()
    {
        SpikeTrap master = SpikeTrap.FindMasterInOpenScenes();
        if (master == null)
        {
            EditorUtility.DisplayDialog(
                "No master spike",
                "Open Level 2 and ensure Spike_Master_Tile(43) exists under the Spikes object.",
                "OK");
            return;
        }

        master.PublishFromHierarchy();
        EditorUtility.DisplayDialog(
            "Spikes updated",
            "Published the master spike settings to all spikes and the Spikes prefab.",
            "OK");
    }

    [MenuItem("Gravity Painter/Fix Spike Visual Roots In Open Scene")]
    public static void FixSpikeVisualRootsMenu()
    {
        int fixedCount = FixAllTrapsInOpenScene();
        EditorUtility.DisplayDialog(
            "Spike visuals fixed",
            "Cleaned duplicate SpikeVisualRoot objects and rebound " + fixedCount + " spike trap(s).",
            "OK");
    }

    public static void ApplyProfileEverywhere(SpikeTrapProfile profile, SpikeTrap sourceMaster = null)
    {
        if (profile == null || _isApplying)
        {
            return;
        }

        _isApplying = true;
        SpikeTrap.SetSyncInProgress(true);
        try
        {
            SpikeTrapProfile.SetCached(profile);

            SpikeTrap[] traps = Object.FindObjectsByType<SpikeTrap>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (SpikeTrap trap in traps)
            {
                if (trap == null)
                {
                    continue;
                }

                bool isMaster = trap == sourceMaster;
                if (!isMaster)
                {
                    float startDelay = trap.StartDelay;
                    trap.ApplyFromProfile(profile);
                    SetStartDelay(trap, startDelay);
                }

                FinalizeTrap(trap);
                EditorUtility.SetDirty(trap);
            }

            RemoveOrphanVisualRoots();
            ApplyToSpikesPrefab(profile);
        }
        finally
        {
            SpikeTrap.SetSyncInProgress(false);
            _isApplying = false;
        }
    }

    public static void ConfigureMaster(SpikeTrap trap, SpikeTrapProfile profile)
    {
        if (trap == null || profile == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(trap);
        so.FindProperty("profile").objectReferenceValue = profile;
        so.FindProperty("publishChangesToProfile").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        trap.gameObject.name = SpikeTrap.MasterSpikeName;
        trap.EnsureProfileReference();
        FinalizeTrap(trap);
        EditorUtility.SetDirty(trap);
    }

    public static void ConfigureInstance(SpikeTrap trap, SpikeTrapProfile profile)
    {
        if (trap == null || profile == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(trap);
        so.FindProperty("profile").objectReferenceValue = profile;
        so.FindProperty("publishChangesToProfile").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        trap.EnsureProfileReference();
        trap.ApplyFromProfile(profile);
        FinalizeTrap(trap);
        EditorUtility.SetDirty(trap);
    }

    public static int FixAllTrapsInOpenScene()
    {
        int fixedCount = 0;
        SpikeTrap[] traps = Object.FindObjectsByType<SpikeTrap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (SpikeTrap trap in traps)
        {
            if (trap == null)
            {
                continue;
            }

            FinalizeTrap(trap);
            EditorUtility.SetDirty(trap);
            fixedCount++;
        }

        RemoveOrphanVisualRoots();
        return fixedCount;
    }

    private static void FinalizeTrap(SpikeTrap trap)
    {
        SpikeVisual visual = trap.GetComponent<SpikeVisual>();
        if (visual != null)
        {
            visual.PruneExtraVisualRoots();
            visual.EnsureVisual();
        }

        trap.BindVisualRoot();
        trap.ResetInitialization();
        trap.RefreshVisualScaleFromProfile();
    }

    private static void RemoveOrphanVisualRoots()
    {
        GameObject[] roots = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (GameObject rootObject in roots)
        {
            if (rootObject == null || rootObject.name != SpikeVisual.VisualRootName)
            {
                continue;
            }

            if (rootObject.GetComponentInParent<SpikeTrap>() != null)
            {
                continue;
            }

            Object.DestroyImmediate(rootObject);
        }
    }

    private static void SetStartDelay(SpikeTrap trap, float startDelay)
    {
        SerializedObject so = new SerializedObject(trap);
        so.FindProperty("startDelay").floatValue = startDelay;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyToSpikesPrefab(SpikeTrapProfile profile)
    {
        if (!System.IO.File.Exists(SpikesPrefabPath))
        {
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(SpikesPrefabPath);
        try
        {
            SpikeTrap trap = prefabRoot.GetComponent<SpikeTrap>();
            if (trap == null)
            {
                trap = prefabRoot.AddComponent<SpikeTrap>();
            }

            SpikeVisual visual = prefabRoot.GetComponent<SpikeVisual>();
            if (visual == null)
            {
                visual = prefabRoot.AddComponent<SpikeVisual>();
            }

            SerializedObject so = new SerializedObject(trap);
            so.FindProperty("profile").objectReferenceValue = profile;
            so.FindProperty("publishChangesToProfile").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            profile.ApplyToTrap(trap, visual);
            visual.PruneExtraVisualRoots();
            visual.EnsureVisual();
            trap.BindVisualRoot();
            trap.ResetInitialization();
            trap.RefreshVisualScaleFromProfile();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, SpikesPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    [MenuItem("Gravity Painter/Sync All Spikes From Profile")]
    public static void SyncAllFromProfileMenu()
    {
        SpikeTrapProfile profile = SpikeTrapProfile.LoadOrDefault();
        if (profile == null)
        {
            EditorUtility.DisplayDialog("Missing profile", "Could not load SpikeTrapProfile.", "OK");
            return;
        }

        ApplyProfileEverywhere(profile);
        EditorUtility.DisplayDialog(
            "Spikes synced",
            "Applied SpikeTrapProfile to all spikes and the Spikes prefab.",
            "OK");
    }
}
#endif

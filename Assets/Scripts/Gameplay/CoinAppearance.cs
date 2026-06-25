using UnityEngine;

/// <summary>
/// Applies shared coin appearance from CoinAppearanceProfile.
/// The master coin (Tile 48 on Level 2) publishes edits back to the profile via the editor menu.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-60)]
public class CoinAppearance : MonoBehaviour
{
    public const string MasterCoinName = "Coin_Master_Tile(48)";
    public const string MasterTileName = "Tile (48)";
    public const int MasterLevelNumber = 2;

    [SerializeField] private CoinAppearanceProfile profile;
    [SerializeField] private bool publishChangesToProfile;

    private static bool _isPublishing;
    private static bool _syncInProgress;

    public bool IsMaster => publishChangesToProfile;
    public CoinAppearanceProfile Profile => profile;

    public static bool SyncInProgress => _syncInProgress;

    public static void SetSyncInProgress(bool inProgress)
    {
        _syncInProgress = inProgress;
    }

    private void OnEnable()
    {
        if (_syncInProgress)
        {
            return;
        }

        EnsureProfileReference();

        if (!publishChangesToProfile)
        {
            ApplyFromProfile();
        }
    }

    private void OnValidate()
    {
        if (_isPublishing || _syncInProgress)
        {
            return;
        }

        EnsureProfileReference();

        if (!publishChangesToProfile)
        {
            ApplyFromProfile();
        }
    }

    public void EnsureProfileReference()
    {
        if (profile != null)
        {
            CoinAppearanceProfile.SetCached(profile);
            return;
        }

        profile = CoinAppearanceProfile.LoadOrDefault();
    }

    public void ApplyFromProfile()
    {
        if (_isPublishing || _syncInProgress || profile == null)
        {
            return;
        }

        profile.ApplyToHierarchy(transform);
    }

    public void ApplyFromProfile(CoinAppearanceProfile sourceProfile)
    {
        profile = sourceProfile;
        ApplyFromProfile();
    }

#if UNITY_EDITOR
    public static event System.Action<CoinAppearance, CoinAppearanceProfile> ProfilePublished;

    public void PublishFromHierarchy()
    {
        if (profile == null || !publishChangesToProfile)
        {
            return;
        }

        _isPublishing = true;
        try
        {
            profile.CaptureFromHierarchy(transform);
            UnityEditor.EditorUtility.SetDirty(profile);
            ProfilePublished?.Invoke(this, profile);
        }
        finally
        {
            _isPublishing = false;
        }
    }

    public static CoinAppearance FindMasterInOpenScenes()
    {
        CoinAppearance[] appearances = Object.FindObjectsByType<CoinAppearance>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        CoinAppearance namedMaster = null;
        foreach (CoinAppearance appearance in appearances)
        {
            if (appearance == null || !appearance.IsMaster)
            {
                continue;
            }

            if (appearance.gameObject.name == MasterCoinName)
            {
                return appearance;
            }

            namedMaster ??= appearance;
        }

        if (namedMaster != null)
        {
            return namedMaster;
        }

        Coin[] coins = Object.FindObjectsByType<Coin>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Coin coin in coins)
        {
            if (coin == null || coin.name != MasterCoinName)
            {
                continue;
            }

            CoinAppearance appearance = coin.GetComponent<CoinAppearance>();
            if (appearance != null)
            {
                return appearance;
            }
        }

        return null;
    }
#endif
}

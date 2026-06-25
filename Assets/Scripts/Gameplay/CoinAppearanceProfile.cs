using UnityEngine;

/// <summary>
/// Global coin look + motion settings used by every coin in campaign and procedural levels.
/// Edit the master coin on Level 2 Tile (48), or this asset directly in the Project window.
/// </summary>
[CreateAssetMenu(fileName = "CoinAppearanceProfile", menuName = "Gravity Painter/Coin Appearance Profile")]
public class CoinAppearanceProfile : ScriptableObject
{
    public const string DefaultResourcePath = "Settings/CoinAppearanceProfile";

    [Header("Coin Root")]
    public Vector3 rootLocalScale = new Vector3(0.478894f, 6.39292f, 0.5270562f);

    [Header("CoinVisualRoot")]
    public Vector3 visualRootLocalPosition = Vector3.zero;
    public Vector3 visualRootLocalEuler = Vector3.zero;
    public Vector3 visualRootLocalScale = Vector3.one;

    [Header("Coin Model (GLB child)")]
    public Vector3 targetLocalBoundsSize = new Vector3(1f, 2f, 1f);
    public Vector3 modelLocalPosition = new Vector3(-0.0012304783f, 0.0009370446f, 0.0009524972f);
    public Vector3 modelLocalEuler = Vector3.zero;
    public Vector3 modelLocalScale = new Vector3(0.52705616f, 0.52705616f, 0.52705616f);
    public Vector3 modelSpawnEuler = Vector3.zero;

    [Header("Motion")]
    [Range(0f, 1080f)]
    public float rotationSpeed = 180f;

    [Header("Placement")]
    public float spawnHeight = 0.8f;

    private static CoinAppearanceProfile _cached;

    public static CoinAppearanceProfile LoadOrDefault()
    {
        if (_cached != null)
        {
            return _cached;
        }

        _cached = Resources.Load<CoinAppearanceProfile>(DefaultResourcePath);
        if (_cached != null)
        {
            return _cached;
        }

        _cached = CreateInstance<CoinAppearanceProfile>();
        return _cached;
    }

    public static void SetCached(CoinAppearanceProfile profile)
    {
        _cached = profile;
    }

    public void ApplyToHierarchy(Transform coinRoot)
    {
        if (coinRoot == null)
        {
            return;
        }

        CoinVisual visual = coinRoot.GetComponent<CoinVisual>();
        if (visual != null)
        {
            visual.ApplyFromProfile(this);
        }

        coinRoot.localScale = rootLocalScale;

        Transform visualRoot = coinRoot.Find(CoinVisual.VisualRootName);
        if (visualRoot != null)
        {
            visualRoot.localPosition = visualRootLocalPosition;
            visualRoot.localRotation = Quaternion.Euler(visualRootLocalEuler);
            visualRoot.localScale = visualRootLocalScale;

            Transform model = visualRoot.childCount > 0 ? visualRoot.GetChild(0) : null;
            if (model != null)
            {
                model.localPosition = modelLocalPosition;
                model.localRotation = Quaternion.Euler(modelLocalEuler);
                model.localScale = modelLocalScale;
            }
        }
    }

    public void ApplyToPrefabContents(Transform coinRoot)
    {
        ApplyToHierarchy(coinRoot);
    }

    public void CaptureFromHierarchy(Transform coinRoot)
    {
        if (coinRoot == null)
        {
            return;
        }

        rootLocalScale = coinRoot.localScale;

        CoinVisual visual = coinRoot.GetComponent<CoinVisual>();
        if (visual != null)
        {
            visual.CaptureToProfile(this);
        }

        Transform visualRoot = coinRoot.Find(CoinVisual.VisualRootName);
        if (visualRoot == null)
        {
            return;
        }

        visualRootLocalPosition = visualRoot.localPosition;
        visualRootLocalEuler = visualRoot.localEulerAngles;
        visualRootLocalScale = visualRoot.localScale;

        if (visualRoot.childCount == 0)
        {
            return;
        }

        Transform model = visualRoot.GetChild(0);
        modelLocalPosition = model.localPosition;
        modelLocalEuler = model.localEulerAngles;
        modelLocalScale = model.localScale;
    }
}

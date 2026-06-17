using UnityEngine;

/// <summary>
/// Spawns coins.glb as a child visual while gameplay collider + Coin logic stay on the root.
/// Model is scaled to match the old flattened cylinder footprint.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-55)]
public class CoinVisual : MonoBehaviour
{
    public const string VisualRootName = "CoinVisualRoot";
    public const string DefaultPrefabResourcePath = "Prefabs/CoinVisual";

    // Matches the old Unity cylinder coin (diameter 1, height 2) before root scale is applied.
    [SerializeField] private Vector3 targetLocalBoundsSize = new Vector3(1f, 2f, 1f);
    [SerializeField] private Vector3 modelLocalEuler = Vector3.zero;
    [SerializeField] private GameObject coinModelPrefab;

    private void Awake()
    {
        EnsureVisual();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            EnsureVisual();
        }
    }

    public void ConfigurePrefab(GameObject prefab)
    {
        coinModelPrefab = prefab;
    }

    public void EnsureVisual()
    {
        if (transform.Find(VisualRootName) != null)
        {
            return;
        }

        if (!TryResolvePrefab())
        {
            return;
        }

        GameObject root = new GameObject(VisualRootName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        GameObject model = Instantiate(coinModelPrefab, root.transform);
        model.name = coinModelPrefab.name;
        model.transform.localRotation = Quaternion.Euler(modelLocalEuler);
        model.transform.localPosition = Vector3.zero;
        model.transform.localScale = Vector3.one;

        StripPhysics(model);
        TileMeshMaterialUtility.FixRenderersToUrpPreservingModelLook(model);
        FitModelToTargetBounds(model, targetLocalBoundsSize);
    }

    public void RebuildVisual()
    {
        Transform existing = transform.Find(VisualRootName);
        if (existing != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }
        }

        EnsureVisual();
    }

    private bool TryResolvePrefab()
    {
        if (coinModelPrefab != null)
        {
            return true;
        }

        coinModelPrefab = Resources.Load<GameObject>(DefaultPrefabResourcePath);
        if (coinModelPrefab != null)
        {
            return true;
        }

        Debug.LogWarning(
            "CoinVisual: missing coin model prefab. Run Gravity Painter → Apply Coin GLB To Prefab.");
        return false;
    }

    private static void FitModelToTargetBounds(GameObject model, Vector3 targetSize)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 size = bounds.size;
        float scaleX = targetSize.x / Mathf.Max(size.x, 0.0001f);
        float scaleY = targetSize.y / Mathf.Max(size.y, 0.0001f);
        float scaleZ = targetSize.z / Mathf.Max(size.z, 0.0001f);
        float uniformScale = Mathf.Min(scaleX, scaleY, scaleZ);
        model.transform.localScale = Vector3.one * uniformScale;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 localCenter = model.transform.InverseTransformPoint(bounds.center);
        model.transform.localPosition -= localCenter;
    }

    private static void StripPhysics(GameObject root)
    {
        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
        {
            if (Application.isPlaying)
            {
                Destroy(col);
            }
            else
            {
                DestroyImmediate(col);
            }
        }

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
        {
            if (Application.isPlaying)
            {
                Destroy(body);
            }
            else
            {
                DestroyImmediate(body);
            }
        }
    }
}

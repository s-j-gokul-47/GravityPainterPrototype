using UnityEngine;

/// <summary>
/// Spawns a GLB model as a child visual while pickup collider + logic stay on the root.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-55)]
public class PowerUpVisual : MonoBehaviour
{
    public const string VisualRootName = "PowerUpVisualRoot";
    public const string DefaultResourcePath = "Prefabs/MagnetVisual";

    [SerializeField] private GameObject modelPrefab;
    [SerializeField] private Vector3 targetLocalBoundsSize = new Vector3(1.2f, 1.2f, 1.2f);

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
        modelPrefab = prefab;
    }

    public void EnsureVisual()
    {
        RemovePrimitiveMeshFromRoot();

        Transform existing = transform.Find(VisualRootName);
        if (existing != null)
        {
            if (!UsesBrokenVisual(existing))
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }
        }

        if (!TryResolvePrefab(out GameObject prefab))
        {
            return;
        }

        GameObject root = new GameObject(VisualRootName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        GameObject model = Instantiate(prefab, root.transform);
        model.name = prefab.name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
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

    private bool TryResolvePrefab(out GameObject prefab)
    {
        prefab = modelPrefab;
        if (prefab != null)
        {
            return true;
        }

        prefab = Resources.Load<GameObject>(DefaultResourcePath);
        if (prefab != null)
        {
            return true;
        }

#if UNITY_EDITOR
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(GlbModelPaths.Magnet);
        if (prefab != null)
        {
            return true;
        }
#endif

        Debug.LogWarning(
            "PowerUpVisual: missing model prefab. Run Gravity Painter → Apply Magnet GLB To Prefab.");
        return false;
    }

    private static bool UsesBrokenVisual(Transform visualRoot)
    {
        foreach (Renderer renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                return true;
            }

            foreach (Material material in materials)
            {
                if (material == null)
                {
                    return true;
                }

                if (material.shader == null)
                {
                    return true;
                }

                string shaderName = material.shader.name;
                if (shaderName.Contains("Hidden") || shaderName.Contains("Error"))
                {
                    return true;
                }

                if (material.name.StartsWith("MagnetMaterial"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void RemovePrimitiveMeshFromRoot()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            if (Application.isPlaying)
            {
                Destroy(meshFilter);
            }
            else
            {
                DestroyImmediate(meshFilter);
            }
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            if (Application.isPlaying)
            {
                Destroy(meshRenderer);
            }
            else
            {
                DestroyImmediate(meshRenderer);
            }
        }
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

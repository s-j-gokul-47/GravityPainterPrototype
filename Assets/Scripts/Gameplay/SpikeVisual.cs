using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns spikes.glb as a child visual aligned so the mesh bottom sits on the tile surface.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-55)]
public class SpikeVisual : MonoBehaviour
{
    public const string VisualRootName = "SpikeVisualRoot";
    public const string DefaultResourcePath = "Prefabs/SpikesVisual";

    [SerializeField] private GameObject modelPrefab;
    [SerializeField] private Vector3 targetLocalBoundsSize = new Vector3(0.9f, 0.9f, 0.9f);

    public Vector3 TargetLocalBoundsSize => targetLocalBoundsSize;
    public Transform VisualRoot => GetCanonicalVisualRoot();

    private void Awake()
    {
        EnsureVisual();
    }

    private void OnEnable()
    {
        if (SpikeTrap.SyncInProgress)
        {
            return;
        }

        EnsureVisual();
    }

    public void ConfigurePrefab(GameObject prefab)
    {
        modelPrefab = prefab;
    }

    public void ApplyFromProfile(SpikeTrapProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        Vector3 previousSize = targetLocalBoundsSize;
        targetLocalBoundsSize = profile.targetLocalBoundsSize;

        PruneExtraVisualRoots();
        Transform visualRoot = GetCanonicalVisualRoot();
        if (visualRoot != null)
        {
            ApplyTransformsToRoot(visualRoot, profile);
        }

        if (visualRoot == null || !Approximately(previousSize, targetLocalBoundsSize))
        {
            RebuildVisual();
            visualRoot = GetCanonicalVisualRoot();
            if (visualRoot != null)
            {
                ApplyTransformsToRoot(visualRoot, profile);
            }
        }
    }

    public void CaptureToProfile(SpikeTrapProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        profile.targetLocalBoundsSize = targetLocalBoundsSize;

        Transform visualRoot = GetCanonicalVisualRoot();
        if (visualRoot == null)
        {
            return;
        }

        profile.visualRootLocalScale = visualRoot.localScale;
        Transform model = FindModelTransform(visualRoot);
        if (model != null)
        {
            profile.modelLocalPosition = model.localPosition;
            profile.modelLocalEuler = model.localEulerAngles;
            profile.modelLocalScale = model.localScale;
        }
    }

    public Transform EnsureVisual()
    {
        PruneExtraVisualRoots();

        Transform existing = GetCanonicalVisualRoot();
        if (existing != null && !UsesBrokenVisual(existing))
        {
            return existing;
        }

        if (existing != null)
        {
            DestroyVisualRoot(existing);
        }

        if (!TryResolvePrefab(out GameObject prefab))
        {
            return null;
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
        return root.transform;
    }

    public void RebuildVisual()
    {
        DestroyAllVisualRoots();
        EnsureVisual();
    }

    public int PruneExtraVisualRoots()
    {
        List<Transform> roots = CollectVisualRoots();
        if (roots.Count <= 1)
        {
            return roots.Count;
        }

        Transform keep = ChooseBestVisualRoot(roots);
        int removed = 0;
        for (int i = 0; i < roots.Count; i++)
        {
            Transform root = roots[i];
            if (root == null || root == keep)
            {
                continue;
            }

            DestroyVisualRoot(root);
            removed++;
        }

        return roots.Count - removed;
    }

    public Transform GetCanonicalVisualRoot()
    {
        List<Transform> roots = CollectVisualRoots();
        if (roots.Count == 0)
        {
            return null;
        }

        if (roots.Count == 1)
        {
            return roots[0];
        }

        return ChooseBestVisualRoot(roots);
    }

    private List<Transform> CollectVisualRoots()
    {
        var roots = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null && child.name == VisualRootName)
            {
                roots.Add(child);
            }
        }

        return roots;
    }

    private static Transform ChooseBestVisualRoot(List<Transform> roots)
    {
        Transform best = null;
        int bestScore = int.MinValue;
        for (int i = 0; i < roots.Count; i++)
        {
            Transform root = roots[i];
            if (root == null)
            {
                continue;
            }

            int score = ScoreVisualRoot(root);
            if (score > bestScore)
            {
                bestScore = score;
                best = root;
            }
        }

        return best ?? roots[0];
    }

    private static int ScoreVisualRoot(Transform root)
    {
        if (root == null)
        {
            return int.MinValue;
        }

        if (UsesBrokenVisual(root))
        {
            return 0;
        }

        int score = 10;
        if (root.childCount > 0)
        {
            score += 5;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        score += renderers.Length;
        return score;
    }

    private void DestroyAllVisualRoots()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null && child.name == VisualRootName)
            {
                DestroyVisualRoot(child);
            }
        }
    }

    private static void DestroyVisualRoot(Transform root)
    {
        if (root == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(root.gameObject);
        }
        else
        {
            DestroyImmediate(root.gameObject);
        }
    }

    private static void ApplyTransformsToRoot(Transform visualRoot, SpikeTrapProfile profile)
    {
        visualRoot.localScale = profile.visualRootLocalScale;
        Transform model = FindModelTransform(visualRoot);
        if (model != null)
        {
            model.localPosition = profile.modelLocalPosition;
            model.localRotation = Quaternion.Euler(profile.modelLocalEuler);
            model.localScale = profile.modelLocalScale;
        }
    }

    private static Transform FindModelTransform(Transform visualRoot)
    {
        if (visualRoot.childCount == 0)
        {
            return null;
        }

        return visualRoot.GetChild(0);
    }

    private static bool Approximately(Vector3 a, Vector3 b)
    {
        return Vector3.SqrMagnitude(a - b) < 0.000001f;
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
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(GlbModelPaths.Spikes);
        if (prefab != null)
        {
            return true;
        }
#endif

        Debug.LogWarning(
            "SpikeVisual: missing model prefab. Run Gravity Painter → Apply Spikes GLB To Prefab.");
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
            }
        }

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

        Vector3 bottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        Vector3 localBottom = model.transform.InverseTransformPoint(bottomCenter);
        model.transform.localPosition -= localBottom;
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

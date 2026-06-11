using UnityEngine;

/// <summary>
/// Spawns the Finish_Line.glb model above the goal tile (campaign + procedural).
/// Default layout matches the tuned Level 2 finish tile (Tile 49).
/// </summary>
[DisallowMultipleComponent]
public class FinishLineVisual : MonoBehaviour
{
    public const string VisualRootName = "FinishLineVisualRoot";
    public const string DefaultPrefabResourcePath = "Prefabs/FinishLineVisual";

    [SerializeField] private GameObject finishLinePrefab;
    [SerializeField] private Vector3 rootLocalPosition = new Vector3(0f, 0.54f, 0f);
    [SerializeField] private Vector3 modelLocalPosition = new Vector3(0.0045299f, 11.7f, -0.0028114f);
    [SerializeField] private Vector3 modelLocalEuler = new Vector3(0f, 180f, 0f);
    [SerializeField] private Vector3 modelLocalScale = new Vector3(0.5152122f, 25.49126f, 0.9255861f);

    public void ConfigurePrefab(GameObject prefab)
    {
        finishLinePrefab = prefab;
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
        root.transform.localPosition = rootLocalPosition;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        GameObject model = Instantiate(finishLinePrefab, root.transform);
        model.name = finishLinePrefab.name;
        model.transform.localPosition = modelLocalPosition;
        model.transform.localRotation = Quaternion.Euler(modelLocalEuler);
        model.transform.localScale = modelLocalScale;

        ConfigureRigidPhysics(model);
        TileMeshMaterialUtility.FixRenderersToUrpPreservingModelLook(model);
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
        if (finishLinePrefab != null)
        {
            return true;
        }

        finishLinePrefab = Resources.Load<GameObject>(DefaultPrefabResourcePath);
        if (finishLinePrefab != null)
        {
            return true;
        }

        Debug.LogWarning(
            "FinishLineVisual: missing prefab. Run Gravity Painter → Apply Finish Line GLB To Levels, "
            + "or assign a prefab on FinishLineVisual.");
        return false;
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

    private static void ConfigureRigidPhysics(GameObject root)
    {
        // Strip existing to be safe
        StripPhysics(root);

        PhysicsMaterial bounceMat = new PhysicsMaterial("FinishGateBounce");
        bounceMat.bounciness = 1f; // Max bounciness allowed by Unity
        bounceMat.bounceCombine = PhysicsMaterialCombine.Maximum;
        bounceMat.dynamicFriction = 0f;
        bounceMat.staticFriction = 0f;
        bounceMat.frictionCombine = PhysicsMaterialCombine.Minimum;

        foreach (MeshRenderer mr in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            MeshCollider mc = mr.gameObject.AddComponent<MeshCollider>();
            mc.convex = false; 
            mc.sharedMaterial = bounceMat;
        }
    }
}

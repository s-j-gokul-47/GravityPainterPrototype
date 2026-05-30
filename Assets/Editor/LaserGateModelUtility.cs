#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class LaserGateModelUtility
{
    public const string GlbPath = "Assets/Art/Models/RedLaserBeam.glb";

    public static bool TryAttachModelToGate(Transform gateRoot, Transform alignToTile = null)
    {
        if (gateRoot == null)
        {
            return false;
        }

        if (!File.Exists(GlbPath))
        {
            Debug.LogWarning("RedLaserBeam.glb not found at " + GlbPath);
            return false;
        }

        if (!EnsureGlbImporterReady(GlbPath))
        {
            Debug.LogWarning("RedLaserBeam.glb is not imported yet. Use Gravity Painter → Reimport Red Laser Beam GLB.");
            return false;
        }

        AssetDatabase.ImportAsset(GlbPath, ImportAssetOptions.ForceUpdate);

        Transform existing = gateRoot.Find("LaserModel");
        if (existing != null && !IsPlaceholderModel(existing))
        {
            FitModelToTile(existing.gameObject, alignToTile);
            SetupLaserGateObstacle.EnsureStrikeColliders(existing.gameObject, gateRoot.GetComponent<LaserGate>());
            EditorUtility.SetDirty(gateRoot.gameObject);
            return true;
        }

        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        GameObject glbRoot = LoadGlbRoot(GlbPath);
        if (glbRoot == null)
        {
            glbRoot = BuildVisualFromImportedAssets(GlbPath);
        }

        if (glbRoot == null)
        {
            Debug.LogError("Could not load mesh from RedLaserBeam.glb");
            return false;
        }

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(glbRoot);
        if (model == null)
        {
            model = Object.Instantiate(glbRoot);
        }

        model.name = "LaserModel";
        model.transform.SetParent(gateRoot, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        StripPhysics(model);
        ConvertRenderersToUrp(model);
        FitModelToTile(model, alignToTile);
        SetupLaserGateObstacle.EnsureStrikeColliders(model, gateRoot.GetComponent<LaserGate>());
        EditorUtility.SetDirty(gateRoot.gameObject);
        return true;
    }

    public static void FitModelToTile(GameObject model, Transform tile)
    {
        if (tile == null)
        {
            FitModelScale(model, 2.5f);
            return;
        }

        Bounds tileBounds = GetTileWorldBounds(tile);
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds modelBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            modelBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 modelSize = modelBounds.size;
        float scaleX = modelSize.x > 0.001f ? tileBounds.size.x / modelSize.x : 1f;
        float scaleY = modelSize.y > 0.001f ? Mathf.Max(tileBounds.size.y * 3f, 0.35f) / modelSize.y : 1f;
        float scaleZ = modelSize.z > 0.001f ? tileBounds.size.z / modelSize.z : 1f;
        float uniform = Mathf.Max(scaleX, scaleZ);
        model.transform.localScale = Vector3.one * uniform;

        modelBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            modelBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 delta = tileBounds.center - modelBounds.center;
        model.transform.position += delta;
        model.transform.position = new Vector3(
            model.transform.position.x,
            tileBounds.max.y + modelBounds.extents.y * 0.15f,
            model.transform.position.z);
    }

    public static Bounds GetTileWorldBoundsForEditor(Transform tile)
    {
        return GetTileWorldBounds(tile);
    }

    private static Bounds GetTileWorldBounds(Transform tile)
    {
        Renderer[] renderers = tile.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = default;
        bool initialized = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (renderer.transform.IsChildOf(tile) && renderer.name.Contains("TilesGlbMesh"))
            {
                continue;
            }

            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!initialized && tile.TryGetComponent(out Collider col))
        {
            bounds = col.bounds;
            initialized = true;
        }

        if (!initialized)
        {
            bounds = new Bounds(tile.position, new Vector3(4f, 0.2f, 4f));
        }

        return bounds;
    }

    public static bool IsPlaceholderModel(Transform model)
    {
        if (model == null)
        {
            return true;
        }

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        return renderers.Length == 0;
    }

    public static bool EnsureGlbImporterReady(string path)
    {
        AssetImporter importer = AssetImporter.GetAtPath(path);
        return importer != null && importer.GetType().Name != "DefaultImporter";
    }

    public static GameObject LoadGlbRoot(string path)
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root != null && root.GetComponentInChildren<Renderer>(true) != null)
        {
            return root;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object asset in assets)
        {
            if (asset is GameObject go && go.GetComponentInChildren<Renderer>(true) != null)
            {
                return go;
            }
        }

        return null;
    }

    public static GameObject BuildVisualFromImportedAssets(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        Mesh mesh = null;
        Material material = null;

        foreach (Object asset in assets)
        {
            if (asset is Mesh m && (mesh == null || m.vertexCount > mesh.vertexCount))
            {
                mesh = m;
            }
            else if (asset is Material mat && material == null)
            {
                material = mat;
            }
        }

        if (mesh == null)
        {
            return null;
        }

        GameObject go = new GameObject("LaserModel");
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material != null ? material : GetDefaultMaterial();
        return go;
    }

    public static void FitModelScale(GameObject model, float targetSpan)
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

        float span = Mathf.Max(bounds.size.x, bounds.size.z, 0.001f);
        float uniform = targetSpan / span;
        model.transform.localScale = Vector3.one * uniform;
    }

    public static void StripPhysics(GameObject root)
    {
        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(col);
        }

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
        {
            Object.DestroyImmediate(body);
        }
    }

    public static void ConvertRenderersToUrp(GameObject root)
    {
        TileMeshMaterialUtility.FixRenderersToUrpPreservingModelLook(root);
    }

    private static Material GetDefaultMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        return new Material(shader);
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class LaserGateModelUtility
{
    public const string GlbPath = "Assets/Art/Models/LaserGate.glb";

    public static bool TryAttachModelToGate(Transform gateRoot, Transform alignToTile = null, bool forceReplace = false)
    {
        if (gateRoot == null)
        {
            return false;
        }

        if (!File.Exists(GlbPath))
        {
            Debug.LogWarning("LaserGate.glb not found at " + GlbPath);
            return false;
        }

        if (!EnsureGlbImporterReady(GlbPath))
        {
            Debug.LogWarning("LaserGate.glb is not imported yet. Use Gravity Painter → Reimport Laser Gate GLB.");
            return false;
        }

        AssetDatabase.ImportAsset(GlbPath, ImportAssetOptions.ForceUpdate);

        Transform existing = gateRoot.Find("LaserModel");
        if (!forceReplace && existing != null && HasValidSplitModel(existing))
        {
            FitModelToTile(existing.gameObject, alignToTile);
            LaserGate gate = gateRoot.GetComponent<LaserGate>();
            WireLaserGateReferences(gate, existing);
            SetupLaserGateObstacle.EnsureStrikeColliders(existing.gameObject, gate);
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
            Debug.LogError("Could not load mesh from LaserGate.glb");
            return false;
        }

        // Use Instantiate (not nested prefab) so GLB reimports do not break references.
        GameObject model = Object.Instantiate(glbRoot);
        model.name = "LaserModel";
        model.transform.SetParent(gateRoot, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        StripPhysics(model);
        ConvertRenderersToUrp(model);
        FitModelToTile(model, alignToTile);

        LaserGate gateComponent = gateRoot.GetComponent<LaserGate>();
        WireLaserGateReferences(gateComponent, model.transform);
        SetupLaserGateObstacle.EnsureStrikeColliders(model, gateComponent);
        EditorUtility.SetDirty(gateRoot.gameObject);
        return true;
    }

    public static bool HasValidSplitModel(Transform modelRoot)
    {
        if (modelRoot == null || IsPlaceholderModel(modelRoot) || IsModelBroken(modelRoot))
        {
            return false;
        }

        Transform beam = LaserGateMeshParts.FindBeamTransform(modelRoot);
        Transform support = LaserGateMeshParts.FindSupportTransform(modelRoot);
        return beam != null && support != null && beam != support;
    }

    public static bool IsModelBroken(Transform modelRoot)
    {
        if (modelRoot == null)
        {
            return true;
        }

        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return true;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                return true;
            }

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh == null)
            {
                return true;
            }
        }

        return false;
    }

    public static void WireLaserGateReferences(LaserGate gate, Transform modelRoot)
    {
        if (gate == null || modelRoot == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(gate);
        so.FindProperty("modelRoot").objectReferenceValue = modelRoot;
        so.FindProperty("beamRoot").objectReferenceValue = LaserGateMeshParts.FindBeamTransform(modelRoot);
        so.ApplyModifiedPropertiesWithoutUndo();
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
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        GameObject best = null;
        int bestRendererCount = 0;

        foreach (Object asset in assets)
        {
            if (asset is not GameObject go)
            {
                continue;
            }

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                continue;
            }

            Transform beam = LaserGateMeshParts.FindBeamTransform(go.transform);
            Transform support = LaserGateMeshParts.FindSupportTransform(go.transform);
            if (beam != null && support != null)
            {
                return go;
            }

            if (renderers.Length > bestRendererCount)
            {
                bestRendererCount = renderers.Length;
                best = go;
            }
        }

        GameObject atPath = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (atPath != null && atPath.GetComponentInChildren<Renderer>(true) != null)
        {
            best ??= atPath;
        }

        return best;
    }

    public static GameObject BuildVisualFromImportedAssets(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        Material material = null;
        Mesh supportMesh = null;
        Mesh beamMesh = null;

        foreach (Object asset in assets)
        {
            if (asset is Material mat && material == null)
            {
                material = mat;
            }
            else if (asset is Mesh mesh)
            {
                string meshName = mesh.name.ToLowerInvariant();
                if (meshName.Contains("0.001") || mesh.vertexCount < 50000)
                {
                    beamMesh ??= mesh;
                }
                else if (meshName == "mesh_0" || mesh.vertexCount > 50000)
                {
                    supportMesh ??= mesh;
                }
                else if (supportMesh == null || mesh.vertexCount > supportMesh.vertexCount)
                {
                    if (beamMesh == null && mesh.vertexCount < 10000)
                    {
                        beamMesh = mesh;
                    }
                    else
                    {
                        supportMesh = mesh;
                    }
                }
            }
        }

        if (supportMesh != null && beamMesh == null)
        {
            foreach (Object asset in assets)
            {
                if (asset is Mesh mesh && mesh != supportMesh && mesh.vertexCount < supportMesh.vertexCount)
                {
                    beamMesh = mesh;
                    break;
                }
            }
        }

        if (supportMesh == null && beamMesh == null)
        {
            return null;
        }

        GameObject root = new GameObject("LaserModel");
        Material matToUse = material != null ? material : GetDefaultMaterial();

        if (supportMesh != null)
        {
            AddMeshChild(root.transform, "LaserGate_Frame", supportMesh, matToUse);
        }

        if (beamMesh != null)
        {
            AddMeshChild(root.transform, "LaserBeam", beamMesh, matToUse);
        }

        return root;
    }

    private static void AddMeshChild(Transform parent, string childName, Mesh mesh, Material material)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        MeshFilter filter = child.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
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

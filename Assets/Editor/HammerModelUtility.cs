#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Loads hammer.glb and attaches it under an existing Hammer / Pivot hierarchy.
/// </summary>
public static class HammerModelUtility
{
    public const string GlbPath = GlbModelPaths.Hammer;

    public static bool TryUpgradeHammerHierarchy(Transform hammerRoot)
    {
        if (hammerRoot == null)
        {
            return false;
        }

        if (!File.Exists(GlbPath))
        {
            Debug.LogWarning("hammer.glb not found at " + GlbPath);
            return false;
        }

        if (!EnsureGlbImporterReady(GlbPath))
        {
            Debug.LogWarning("hammer.glb is not imported yet. Use Gravity Painter → Reimport Hammer GLB.");
            return false;
        }

        AssetDatabase.ImportAsset(GlbPath, ImportAssetOptions.ForceUpdate);

        Transform pivot = hammerRoot.Find("Pivot") ?? hammerRoot;
        Transform existingModel = pivot.Find("HammerModel");
        if (existingModel != null && !IsPlaceholderModel(existingModel))
        {
            return false;
        }

        GameObject glbRoot = LoadGlbRoot(GlbPath);
        if (glbRoot == null)
        {
            glbRoot = BuildVisualFromImportedAssets(GlbPath);
        }

        if (glbRoot == null)
        {
            Debug.LogError("Could not load mesh from hammer.glb");
            return false;
        }

        if (existingModel != null)
        {
            Object.DestroyImmediate(existingModel.gameObject);
        }

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(glbRoot);
        if (model == null)
        {
            model = Object.Instantiate(glbRoot);
        }

        model.name = "HammerModel";
        model.transform.SetParent(pivot, false);
        model.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        model.transform.localRotation = Quaternion.identity;
        FitModelScale(model, 2.2f);
        StripPhysics(model);
        ConvertRenderersToUrp(model);
        SetupHammerObstacle.EnsureStrikeCollidersOnModel(model);

        EditorUtility.SetDirty(hammerRoot.gameObject);
        return true;
    }

    public static bool IsPlaceholderModel(Transform model)
    {
        if (model == null)
        {
            return true;
        }

        MeshFilter filter = model.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
        {
            return model.GetComponentsInChildren<Renderer>(true).Length == 0;
        }

        string meshName = filter.sharedMesh.name;
        return meshName == "Cube" || meshName.StartsWith("Builtin");
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
        GameObject best = null;
        foreach (Object asset in assets)
        {
            if (asset is not GameObject go || string.IsNullOrEmpty(go.name))
            {
                continue;
            }

            if (go.GetComponentInChildren<Renderer>(true) != null)
            {
                return go;
            }

            best ??= go;
        }

        return best;
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

        GameObject go = new GameObject("HammerModel");
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material != null ? material : GetDefaultMaterial();
        return go;
    }

    public static void FitModelScale(GameObject model, float targetHeight)
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

        float height = Mathf.Max(bounds.size.y, 0.001f);
        float uniform = targetHeight / height;
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
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            return;
        }

        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = renderer.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat == null || mat.shader == urpLit)
                {
                    continue;
                }

                mat.shader = urpLit;
                if (mat.HasProperty("_BaseMap") && mat.mainTexture != null)
                {
                    mat.SetTexture("_BaseMap", mat.mainTexture);
                }

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", mat.color);
                }
            }
        }
    }

    private static Material GetDefaultMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        return new Material(shader);
    }
}
#endif

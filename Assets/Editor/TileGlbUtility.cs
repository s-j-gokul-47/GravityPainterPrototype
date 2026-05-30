#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class TileGlbUtility
{
    public const string GlbPath = "Assets/Art/Models/Tiles/tiles.glb";
    public const string PrefabPath = "Assets/Prefabs/Visuals/Tiles/TilesGlbMesh.prefab";
    public const string ResourcesPrefabPath = "Assets/Resources/Visuals/Tiles/TilesGlbMesh.prefab";

    public static bool EnsureGlbInProject()
    {
        if (File.Exists(GlbPath))
        {
            return true;
        }

        string rootGlb = Path.Combine(Application.dataPath, "../tiles.glb");
        if (!File.Exists(rootGlb))
        {
            return false;
        }

        string dir = Path.GetDirectoryName(GlbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.Copy(rootGlb, GlbPath, true);
        AssetDatabase.Refresh();
        return true;
    }

    public static bool TryBuildTilesMeshPrefab()
    {
        if (!EnsureGlbInProject())
        {
            Debug.LogError("tiles.glb not found. Place it in the project root or at " + GlbPath);
            return false;
        }

        if (!HammerModelUtility.EnsureGlbImporterReady(GlbPath))
        {
            AssetDatabase.ImportAsset(GlbPath, ImportAssetOptions.ForceUpdate);
        }

        AssetDatabase.ImportAsset(GlbPath, ImportAssetOptions.ForceUpdate);

        GameObject glbRoot = HammerModelUtility.LoadGlbRoot(GlbPath);
        if (glbRoot == null)
        {
            glbRoot = HammerModelUtility.BuildVisualFromImportedAssets(GlbPath);
        }

        if (glbRoot == null)
        {
            Debug.LogError("Could not load tiles.glb");
            return false;
        }

        EnsureDirectory(PrefabPath);
        EnsureDirectory(ResourcesPrefabPath);

        GameObject model = Object.Instantiate(glbRoot);
        model.name = "TilesGlbMesh";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;
        HammerModelUtility.StripPhysics(model);
        TileMeshMaterialUtility.FixRenderersToUrpPreservingModelLook(model);

        SavePrefab(model, PrefabPath);
        SavePrefab(model, ResourcesPrefabPath);
        Object.DestroyImmediate(model);
        AssetDatabase.SaveAssets();
        return true;
    }

    private static void SavePrefab(GameObject model, string path)
    {
        GameObject clone = Object.Instantiate(model);
        clone.name = "TilesGlbMesh";
        PrefabUtility.SaveAsPrefabAsset(clone, path);
        Object.DestroyImmediate(clone);
    }

    private static void EnsureDirectory(string assetPath)
    {
        string dir = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public static GameObject LoadTilesMeshPrefab()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }
}
#endif

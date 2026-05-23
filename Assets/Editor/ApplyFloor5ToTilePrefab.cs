#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Puts Floor5 mesh under Tile.prefab, scaled and centered to match the original cube tile (1 × 0.1 × 1).
/// </summary>
public static class ApplyFloor5ToTilePrefab
{
    private const string TilePrefabPath = "Assets/Prefabs/Tile.prefab";
    private const string Floor5PrefabPath = "Assets/Sci Fi Modular Pack/Prefabs/Floor5.prefab";
    private const string VisualChildName = "Floor5Visual";

    /// <summary>Original Tile.prefab cube: scale (1, 0.1, 1) on a 1×1×1 mesh = 1 wide, 0.1 tall, 1 deep.</summary>
    private static readonly Vector3 TileCubeScale = new Vector3(1f, 0.1f, 1f);

    [MenuItem("Gravity Painter/Apply Floor5 Visual To Tile Prefab")]
    public static void Apply()
    {
        ApplyInternal(recreateVisual: true);
    }

    [MenuItem("Gravity Painter/Align Floor5 Visual To Tile Size")]
    public static void AlignExisting()
    {
        ApplyInternal(recreateVisual: false);
    }

    private static void ApplyInternal(bool recreateVisual)
    {
        GameObject floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Floor5PrefabPath);
        if (floorPrefab == null)
        {
            EditorUtility.DisplayDialog("Missing prefab", "Could not find:\n" + Floor5PrefabPath, "OK");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(TilePrefabPath);
        root.transform.localScale = TileCubeScale;

        Transform visualTransform = root.transform.Find(VisualChildName);
        GameObject visual;

        if (recreateVisual || visualTransform == null)
        {
            if (visualTransform != null)
            {
                UnityEngine.Object.DestroyImmediate(visualTransform.gameObject);
            }

            MeshRenderer rootRenderer = root.GetComponent<MeshRenderer>();
            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            MeshFilter rootFilter = root.GetComponent<MeshFilter>();
            if (rootFilter != null)
            {
                UnityEngine.Object.DestroyImmediate(rootFilter);
            }

            visual = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab, root.transform);
            visual.name = VisualChildName;

            foreach (Collider col in visual.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(col);
            }
        }
        else
        {
            visual = visualTransform.gameObject;
        }

        FitVisualToTileCube(root, visual);

        TileZone zone = root.GetComponent<TileZone>();
        if (zone != null)
        {
            zone.UpdateVisual();
        }

        PrefabUtility.SaveAsPrefabAsset(root, TilePrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Tile prefab updated",
            "Floor5Visual now matches the old cube tile:\n" +
            "• Position (0, 0, 0) on Tile\n" +
            "• Scale (1, 0.1, 1) on Tile root\n" +
            "• Floor5 fitted inside that box",
            "OK");

        Debug.Log("Tile.prefab: Floor5 visual aligned to cube tile size 1 × 0.1 × 1.");
    }

    private static void FitVisualToTileCube(GameObject root, GameObject visual)
    {
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        Renderer renderer = visual.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Vector3 desiredWorldSize = new Vector3(
            TileCubeScale.x * Mathf.Abs(root.transform.lossyScale.x),
            TileCubeScale.y * Mathf.Abs(root.transform.lossyScale.y),
            TileCubeScale.z * Mathf.Abs(root.transform.lossyScale.z));

        Bounds bounds = renderer.bounds;
        visual.transform.localScale = new Vector3(
            desiredWorldSize.x / Mathf.Max(bounds.size.x, 0.0001f),
            desiredWorldSize.y / Mathf.Max(bounds.size.y, 0.0001f),
            desiredWorldSize.z / Mathf.Max(bounds.size.z, 0.0001f));

        bounds = renderer.bounds;
        Vector3 centerOffsetLocal = root.transform.InverseTransformPoint(bounds.center);
        visual.transform.localPosition -= centerOffsetLocal;
    }
}
#endif

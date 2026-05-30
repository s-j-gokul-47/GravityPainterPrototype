#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetupTilesGlb
{
    private const string Level2ScenePath = "Assets/Scenes/Levels/Level 2.unity";
    private const string ReferenceLayoutPath = "Assets/Settings/TileGlbReferenceLayout.asset";
    private const string ReferenceTileName = "Tile (40)";

    public static void BatchSetup()
    {
        if (!TileGlbUtility.TryBuildTilesMeshPrefab())
        {
            EditorApplication.Exit(1);
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Single);
        if (!ApplyToTileInScene("Tile (37)"))
        {
            EditorApplication.Exit(1);
            return;
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("Applied tiles.glb to Tile (37) in Level 2.");
        EditorApplication.Exit(0);
    }

    [MenuItem("Gravity Painter/Reimport tiles.glb")]
    public static void ReimportGlb()
    {
        if (!TileGlbUtility.EnsureGlbInProject())
        {
            EditorUtility.DisplayDialog("Missing file", "Place tiles.glb in the project root or at:\n" + TileGlbUtility.GlbPath, "OK");
            return;
        }

        AssetDatabase.ImportAsset(TileGlbUtility.GlbPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Reimported", "Reimported tiles.glb with glTFast.", "OK");
    }

    [MenuItem("Gravity Painter/Build Tiles GLB Mesh Prefab")]
    public static void BuildPrefab()
    {
        if (!TileGlbUtility.TryBuildTilesMeshPrefab())
        {
            EditorUtility.DisplayDialog("Build failed", "Could not build TilesGlbMesh.prefab. Check the Console.", "OK");
            return;
        }

        EditorUtility.DisplayDialog("Done", "Created:\n" + TileGlbUtility.PrefabPath, "OK");
    }

    [MenuItem("Gravity Painter/Capture Tile (40) GLB Layout To Reference Asset")]
    public static void CaptureTile40LayoutToAsset()
    {
        CaptureTileLayoutToAsset(ReferenceTileName);
    }

    [MenuItem("Gravity Painter/Apply Tile (40) Reference GLB Layout To All Tiles In Scene")]
    public static void ApplyTile40ReferenceToAllTiles()
    {
        TileGlbReferenceLayoutAsset referenceAsset = AssetDatabase.LoadAssetAtPath<TileGlbReferenceLayoutAsset>(ReferenceLayoutPath);
        if (referenceAsset != null && referenceAsset.layout.meshLocalScale.sqrMagnitude > 1e-8f)
        {
            ApplyLayoutToAllTilesInScene(referenceAsset.layout, referenceAsset.sourceTileName);
            return;
        }

        GameObject tileGo = FindTileByName(ReferenceTileName);
        if (tileGo == null)
        {
            EditorUtility.DisplayDialog("Tile not found", "Open Level 2 and ensure " + ReferenceTileName + " exists.", "OK");
            return;
        }

        TileGlbVisual visual = tileGo.GetComponent<TileGlbVisual>();
        if (visual == null || !visual.TryCaptureLayout(out TileGlbVisualLayout layout))
        {
            EditorUtility.DisplayDialog("Missing layout", ReferenceTileName + " has no tuned GLB hierarchy.", "OK");
            return;
        }

        ApplyLayoutToAllTilesInScene(layout, ReferenceTileName);
    }

    [MenuItem("Gravity Painter/Capture Selected Tile GLB Layout To Reference Asset")]
    public static void CaptureSelectedLayoutToAsset()
    {
        TileGlbVisual reference = GetSelectedTileGlbVisual();
        if (reference == null)
        {
            EditorUtility.DisplayDialog("Select a tile", "Select your tuned reference tile first.", "OK");
            return;
        }

        CaptureTileLayoutToAsset(reference.gameObject.name, reference);
    }

    private static void CaptureTileLayoutToAsset(string tileName, TileGlbVisual visualOverride = null)
    {
        TileGlbVisual visual = visualOverride;
        if (visual == null)
        {
            GameObject tileGo = FindTileByName(tileName);
            if (tileGo == null)
            {
                EditorUtility.DisplayDialog("Tile not found", "Could not find " + tileName + " in the open scene.", "OK");
                return;
            }

            visual = tileGo.GetComponent<TileGlbVisual>();
        }

        if (visual == null || !visual.TryCaptureLayout(out TileGlbVisualLayout layout))
        {
            EditorUtility.DisplayDialog("Missing layout", tileName + " has no TileGlbVisualRoot / TilesGlbMesh.", "OK");
            return;
        }

        TileGlbReferenceLayoutAsset asset = AssetDatabase.LoadAssetAtPath<TileGlbReferenceLayoutAsset>(ReferenceLayoutPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<TileGlbReferenceLayoutAsset>();
            AssetDatabase.CreateAsset(asset, ReferenceLayoutPath);
        }

        asset.sourceTileName = tileName;
        asset.layout = layout;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Saved reference",
            "Stored GLB layout from " + tileName + " in:\n" + ReferenceLayoutPath + "\n\n"
            + "TileGlbVisualRoot pos: " + layout.rootLocalPosition + "\n"
            + "TilesGlbMesh rot: " + layout.meshLocalRotation.eulerAngles + "\n"
            + "TilesGlbMesh scale: " + layout.meshLocalScale,
            "OK");
    }

    /// <summary>
    /// Tune one tile in the Scene view (Floor5Visual + TilesGlbMesh transforms), save, select that tile, run this.
    /// </summary>
    [MenuItem("Gravity Painter/Apply Selected Tile GLB Layout To All Tiles In Scene")]
    public static void ApplySelectedLayoutToAllTiles()
    {
        TileGlbVisual reference = GetSelectedTileGlbVisual();
        if (reference == null)
        {
            EditorUtility.DisplayDialog(
                "Select a reference tile",
                "Select the tile you tuned (with TileGlbVisual + TileGlbVisualRoot + TilesGlbMesh), then run this menu again.",
                "OK");
            return;
        }

        if (!reference.TryCaptureLayout(out TileGlbVisualLayout layout))
        {
            EditorUtility.DisplayDialog(
                "Missing GLB hierarchy",
                reference.gameObject.name + " has no TileGlbVisualRoot / TilesGlbMesh.\n"
                + "Add TileGlbVisual and tune the mesh first.",
                "OK");
            return;
        }

        ApplyLayoutToAllTilesInScene(layout, reference.gameObject.name);
    }

    private static void ApplyLayoutToAllTilesInScene(TileGlbVisualLayout layout, string sourceName)
    {
        GameObject prefab = TileGlbUtility.LoadTilesMeshPrefab();
        TileZone[] allTiles = Object.FindObjectsByType<TileZone>(FindObjectsSortMode.None);
        if (allTiles.Length == 0)
        {
            EditorUtility.DisplayDialog("No tiles", "No TileZone found in the open scene.", "OK");
            return;
        }

        int applied = 0;
        Undo.SetCurrentGroupName("Apply tile GLB layout to all tiles");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (TileZone zone in allTiles)
        {
            if (!ApplyLayoutToTile(zone.gameObject, layout, prefab))
            {
                continue;
            }

            applied++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Applied",
            "Copied GLB layout from " + sourceName + " to " + applied + " tile(s).\n\n"
            + "Root pos: " + layout.rootLocalPosition + "\n"
            + "Mesh rot: " + layout.meshLocalRotation.eulerAngles + "\n"
            + "Mesh scale: " + layout.meshLocalScale + "\n\n"
            + "Save the scene (Ctrl/Cmd+S).",
            "OK");
    }

    [MenuItem("Gravity Painter/Add TileGlbVisual To All Tiles In Scene")]
    public static void AddGlbVisualToAllTiles()
    {
        if (!TileGlbUtility.TryBuildTilesMeshPrefab())
        {
            EditorUtility.DisplayDialog("Build failed", "Could not build tiles mesh prefab.", "OK");
            return;
        }

        GameObject prefab = TileGlbUtility.LoadTilesMeshPrefab();
        TileZone[] allTiles = Object.FindObjectsByType<TileZone>(FindObjectsSortMode.None);
        int added = 0;

        foreach (TileZone zone in allTiles)
        {
            GameObject tileGo = zone.gameObject;
            if (tileGo.GetComponent<TileGlbVisual>() != null)
            {
                continue;
            }

            TileGlbVisual visual = Undo.AddComponent<TileGlbVisual>(tileGo);
            SerializedObject so = new SerializedObject(visual);
            so.FindProperty("tilesMeshPrefab").objectReferenceValue = prefab;
            so.FindProperty("useAutomaticLayout").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject zoneSo = new SerializedObject(zone);
            zoneSo.FindProperty("useEdgeIndicatorsWithFloorMesh").boolValue = false;
            zoneSo.ApplyModifiedPropertiesWithoutUndo();

            visual.RefreshVisual();
            added++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Done", "Added TileGlbVisual to " + added + " tile(s). Tune one tile, then use Apply Selected Tile GLB Layout To All Tiles.", "OK");
    }

    [MenuItem("Gravity Painter/Apply tiles.glb To Tile (37) in Level 2")]
    public static void ApplyToTile37()
    {
        ApplyToTileInLevel2("Tile (37)");
    }

    private static void ApplyToTileInLevel2(string tileName)
    {
        if (!TileGlbUtility.TryBuildTilesMeshPrefab())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Single);
        if (!ApplyToTileInScene(tileName))
        {
            EditorUtility.DisplayDialog("Failed", "Could not find " + tileName + ".", "OK");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static bool ApplyLayoutToTile(GameObject tileGo, TileGlbVisualLayout layout, GameObject prefab)
    {
        TileGlbVisual visual = tileGo.GetComponent<TileGlbVisual>();
        if (visual == null)
        {
            visual = Undo.AddComponent<TileGlbVisual>(tileGo);
        }

        SerializedObject so = new SerializedObject(visual);
        so.FindProperty("tilesMeshPrefab").objectReferenceValue = prefab;
        so.FindProperty("useAutomaticLayout").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        TileZone zone = TileZone.GetPrimaryZone(tileGo);
        if (zone != null)
        {
            SerializedObject zoneSo = new SerializedObject(zone);
            zoneSo.FindProperty("useEdgeIndicatorsWithFloorMesh").boolValue = false;
            zoneSo.ApplyModifiedPropertiesWithoutUndo();
        }

        Transform existingRoot = tileGo.transform.Find(TileGlbVisual.VisualRootName);
        if (existingRoot == null)
        {
            visual.RefreshVisual();
        }

        Undo.RecordObject(visual, "Apply tile GLB layout");
        visual.ApplyLayout(layout);

        Renderer rootRenderer = tileGo.GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            Undo.RecordObject(rootRenderer, "Apply tile GLB layout");
            rootRenderer.enabled = false;
        }

        EditorUtility.SetDirty(tileGo);
        return true;
    }

    private static bool ApplyToTileInScene(string tileName)
    {
        GameObject tileGo = FindTileByName(tileName);
        if (tileGo == null)
        {
            return false;
        }

        GameObject prefab = TileGlbUtility.LoadTilesMeshPrefab();
        if (prefab == null)
        {
            return false;
        }

        TileGlbVisual visual = tileGo.GetComponent<TileGlbVisual>();
        if (visual == null)
        {
            visual = Undo.AddComponent<TileGlbVisual>(tileGo);
        }

        SerializedObject visualSo = new SerializedObject(visual);
        visualSo.FindProperty("tilesMeshPrefab").objectReferenceValue = prefab;
        visualSo.FindProperty("useAutomaticLayout").boolValue = false;
        visualSo.ApplyModifiedPropertiesWithoutUndo();

        TileZone zone = TileZone.GetPrimaryZone(tileGo);
        if (zone != null)
        {
            SerializedObject zoneSo = new SerializedObject(zone);
            zoneSo.FindProperty("useEdgeIndicatorsWithFloorMesh").boolValue = false;
            zoneSo.ApplyModifiedPropertiesWithoutUndo();
        }

        if (tileGo.transform.Find(TileGlbVisual.VisualRootName) == null)
        {
            visual.RefreshVisual();
        }

        Renderer rootRenderer = tileGo.GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        EditorUtility.SetDirty(tileGo);
        return true;
    }

    private static TileGlbVisual GetSelectedTileGlbVisual()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            return null;
        }

        TileGlbVisual visual = selected.GetComponentInParent<TileGlbVisual>();
        return visual != null ? visual : selected.GetComponent<TileGlbVisual>();
    }

    private static GameObject FindTileByName(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
                if (t.name == name)
                {
                    return t.gameObject;
                }
            }
        }

        return null;
    }
}
#endif

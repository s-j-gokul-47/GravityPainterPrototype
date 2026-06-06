#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor menu tests for <see cref="ProceduralPathGenerator"/> Step 1 validation.
/// </summary>
public static class ProceduralPathGeneratorTest
{
    [MenuItem("Gravity Painter/Test Procedural Path")]
    public static void RunTests()
    {
        LevelGenConfig config = FindOrCreateConfig();
        if (config == null)
        {
            return;
        }

        var generator = new ProceduralPathGenerator();
        List<LevelCell> runA = generator.Generate(config, 12345);
        List<LevelCell> runB = generator.Generate(config, 12345);
        bool seedTestPassed = AreCellListsEqual(runA, runB);
        Debug.Log(seedTestPassed ? "SEED TEST PASSED" : "SEED TEST FAILED");

        List<LevelCell> differentSeedRun = generator.Generate(config, 99999);
        bool differentSeedTestPassed = !AreCellListsEqual(runA, differentSeedRun);
        Debug.Log(differentSeedTestPassed ? "DIFFERENT SEED TEST PASSED" : "DIFFERENT SEED TEST FAILED");

        for (int i = 0; i < runA.Count; i++)
        {
            LevelCell cell = runA[i];
            Debug.Log(
                $"[{cell.PathIndex}] GridPos=({cell.GridPos.x},{cell.GridPos.y}) Rot={cell.YRotation} Obstacle={cell.Obstacle}");
        }

        int pathLength = runA.Count;
        bool lengthInRange = pathLength >= config.minPathLength && pathLength <= config.maxPathLength;
        Debug.Log($"Path length: {pathLength} (expected {config.minPathLength}–{config.maxPathLength})");
        Debug.Log(lengthInRange ? "PATH LENGTH TEST PASSED" : "PATH LENGTH TEST FAILED");

        bool adjacencyPassed = ValidateAdjacency(runA);
        Debug.Log(adjacencyPassed ? "ADJACENCY TEST PASSED" : "ADJACENCY TEST FAILED");
    }

    [MenuItem("Gravity Painter/Wire Level Gen Config Prefabs")]
    public static void WireConfigPrefabs()
    {
        LevelGenConfig config = FindOrCreateConfig();
        if (config == null)
        {
            return;
        }

        GameObject tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Tile.prefab");
        if (tilePrefab != null)
        {
            config.tilePrefab = tilePrefab;
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log(
            tilePrefab != null
                ? "Wired Tile.prefab onto " + config.name
                : "Tile.prefab not found at Assets/Prefabs/Gameplay/Tile.prefab");
    }

    [MenuItem("Gravity Painter/Create Level Gen Config")]
    public static void CreateConfigAsset()
    {
        const string path = "Assets/Settings/LevelGenConfig_Default.asset";
        if (AssetDatabase.LoadAssetAtPath<LevelGenConfig>(path) != null)
        {
            Debug.Log("LevelGenConfig already exists at " + path);
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
        {
            AssetDatabase.CreateFolder("Assets", "Settings");
        }

        LevelGenConfig config = ScriptableObject.CreateInstance<LevelGenConfig>();
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = config;
        Debug.Log("Created LevelGenConfig at " + path);
    }

    private static LevelGenConfig FindOrCreateConfig()
    {
        string[] guids = AssetDatabase.FindAssets("t:LevelGenConfig");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<LevelGenConfig>(path);
        }

        CreateConfigAsset();
        return AssetDatabase.LoadAssetAtPath<LevelGenConfig>("Assets/Settings/LevelGenConfig_Default.asset");
    }

    private static bool AreCellListsEqual(List<LevelCell> a, List<LevelCell> b)
    {
        if (a == null || b == null || a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            LevelCell cellA = a[i];
            LevelCell cellB = b[i];
            if (cellA.GridPos != cellB.GridPos
                || !Mathf.Approximately(cellA.YRotation, cellB.YRotation)
                || cellA.PathIndex != cellB.PathIndex
                || cellA.IsMainPath != cellB.IsMainPath
                || cellA.Obstacle != cellB.Obstacle
                || cellA.PresetZone != cellB.PresetZone)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValidateAdjacency(List<LevelCell> cells)
    {
        if (cells.Count <= 1)
        {
            return true;
        }

        for (int i = 1; i < cells.Count; i++)
        {
            Vector2Int diff = cells[i].GridPos - cells[i - 1].GridPos;
            if (Mathf.Abs(diff.x) + Mathf.Abs(diff.y) != 1)
            {
                return false;
            }
        }

        return true;
    }
}
#endif

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple visual test for procedural Step 1.
/// Attach to an empty GameObject and generate a tile path from the Inspector button.
/// </summary>
public class ProceduralPathVisualizer : MonoBehaviour
{
    [Tooltip("Generation settings including tile prefab and spacing.")]
    public LevelGenConfig config;

    [Tooltip("Seed for reproducible path layout.")]
    public int seed = 12345;

    [Tooltip("Optional parent for spawned tiles. Uses this transform when empty.")]
    public Transform parent;

    /// <summary>
    /// Clears previous tiles and spawns a visual path from the configured seed.
    /// </summary>
    [ContextMenu("Generate Visual Path")]
    public void GenerateVisualPath()
    {
        if (!ValidateConfig())
        {
            return;
        }

        Transform spawnParent = parent != null ? parent : transform;
        ClearChildren(spawnParent);

        var generator = new ProceduralPathGenerator();
        List<LevelCell> cells = generator.GenerateWithRetry(config, seed);
        if (cells == null || cells.Count == 0)
        {
            Debug.LogError("Could not generate a valid path.");
            return;
        }

        int spawned = 0;
        for (int i = 0; i < cells.Count; i++)
        {
            LevelCell cell = cells[i];
            GameObject tile = SpawnTile(cell, i, cells, spawnParent);
            if (tile == null)
            {
                continue;
            }

            tile.name = $"Tile_{cell.PathIndex}_{cell.GridPos.x}_{cell.GridPos.y}";
            spawned++;
        }

        spawned += SpawnCornerPads(cells, spawnParent);

        Debug.Log($"Spawned {spawned} tiles for seed {seed} under '{spawnParent.name}'.");
    }

    private bool ValidateConfig()
    {
        if (config == null)
        {
            Debug.LogError("ProceduralPathVisualizer: Config is not assigned.");
            return false;
        }

        if (config.tilePrefab == null)
        {
            Debug.LogError(
                "ProceduralPathVisualizer: tilePrefab is missing on config '" + config.name + "'.\n" +
                "Open Assets/Settings/LevelGenConfig_Default and assign Assets/Prefabs/Gameplay/Tile.prefab.");
            return false;
        }

        return true;
    }

    private void ClearChildren(Transform spawnParent)
    {
        for (int i = spawnParent.childCount - 1; i >= 0; i--)
        {
            GameObject child = spawnParent.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Undo.DestroyObjectImmediate(child);
                continue;
            }
#endif
            Destroy(child);
        }
    }

    private GameObject SpawnTile(LevelCell cell, int index, IReadOnlyList<LevelCell> cells, Transform spawnParent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject tile = UnityEditor.PrefabUtility.InstantiatePrefab(config.tilePrefab, spawnParent) as GameObject;
            if (tile == null)
            {
                Debug.LogError("Failed to instantiate tile prefab in Edit mode.");
                return null;
            }

            ProceduralTilePlacement.ApplyPathTransform(tile.transform, index, cells, config);
            UnityEditor.Undo.RegisterCreatedObjectUndo(tile, "Generate Visual Path");
            return tile;
        }
#endif
        GameObject runtimeTile = Instantiate(config.tilePrefab, spawnParent);
        ProceduralTilePlacement.ApplyPathTransform(runtimeTile.transform, index, cells, config);
        return runtimeTile;
    }

    private int SpawnCornerPads(IReadOnlyList<LevelCell> cells, Transform spawnParent)
    {
        if (!config.addCornerPads || cells == null)
        {
            return 0;
        }

        int spawned = 0;
        for (int i = 2; i < cells.Count; i++)
        {
            if (!ProceduralTilePlacement.IsTurnIndex(i, cells))
            {
                continue;
            }

            int padCount = ProceduralTilePlacement.CountCornerPadsForTurn(i, cells, config);
            for (int padIndex = 0; padIndex < padCount; padIndex++)
            {
                GameObject pad = SpawnCornerPad(i, padIndex, padCount, cells, spawnParent);
                if (pad == null)
                {
                    continue;
                }

                pad.name = $"Tile_corner_{i}_{padIndex}_{cells[i - 1].GridPos.x}_{cells[i - 1].GridPos.y}";
                spawned++;
            }
        }

        return spawned;
    }

    private GameObject SpawnCornerPad(
        int turnIndex,
        int padIndex,
        int padCount,
        IReadOnlyList<LevelCell> cells,
        Transform spawnParent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject tile = UnityEditor.PrefabUtility.InstantiatePrefab(config.tilePrefab, spawnParent) as GameObject;
            if (tile == null)
            {
                return null;
            }

            ProceduralTilePlacement.ApplyCornerPadTransform(
                tile.transform, turnIndex, padIndex, padCount, cells, config);
            UnityEditor.Undo.RegisterCreatedObjectUndo(tile, "Generate Visual Path");
            return tile;
        }
#endif
        GameObject runtimeTile = Instantiate(config.tilePrefab, spawnParent);
        ProceduralTilePlacement.ApplyCornerPadTransform(
            runtimeTile.transform, turnIndex, padIndex, padCount, cells, config);
        return runtimeTile;
    }
}

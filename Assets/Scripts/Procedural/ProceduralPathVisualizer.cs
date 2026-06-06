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
        List<LevelCell> cells = generator.Generate(config, seed);
        if (cells == null || cells.Count == 0)
        {
            Debug.LogError("ProceduralPathGenerator returned no cells.");
            return;
        }

        int spawned = 0;
        foreach (LevelCell cell in cells)
        {
            Vector3 localPos = new Vector3(
                cell.GridPos.x * config.tileSpacing,
                0f,
                cell.GridPos.y * config.tileSpacing);

            GameObject tile = SpawnTile(localPos, Quaternion.Euler(0f, cell.YRotation, 0f), spawnParent);
            if (tile == null)
            {
                continue;
            }

            tile.name = $"Tile_{cell.PathIndex}_{cell.GridPos.x}_{cell.GridPos.y}";
            spawned++;
        }

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

    private GameObject SpawnTile(Vector3 localPos, Quaternion localRot, Transform spawnParent)
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

            tile.transform.localPosition = localPos;
            tile.transform.localRotation = localRot;
            UnityEditor.Undo.RegisterCreatedObjectUndo(tile, "Generate Visual Path");
            return tile;
        }
#endif
        GameObject runtimeTile = Instantiate(config.tilePrefab, spawnParent);
        runtimeTile.transform.localPosition = localPos;
        runtimeTile.transform.localRotation = localRot;
        return runtimeTile;
    }
}
